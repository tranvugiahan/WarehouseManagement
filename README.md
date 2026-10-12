# WarehouseManagement

Khung ASP.NET Core MVC + REST API cho đồ án quản lý kho, theo SRS (1).docx.
Một project .NET 8, tổ chức ba lớp. Chưa triển khai nghiệp vụ hoặc đăng nhập.

## Chạy sau khi clone

Cần .NET SDK 8; nếu dùng Visual Studio, cài workload ASP.NET and web development.
Chạy các lệnh từ thư mục chứa WarehouseManagement.sln:

```powershell
dotnet restore WarehouseManagement.sln
dotnet build WarehouseManagement.sln --no-restore
dotnet run --project WarehouseManagement --launch-profile http
```

- Trang MVC: http://localhost:5265/
- API kiểm tra ứng dụng: GET http://localhost:5265/api/health
- Kết quả API: HTTP 200, JSON `{"status":"ok"}`.
- Ứng dụng khởi động được khi chưa có SQL Server. Health chỉ kiểm tra web, không kiểm tra database.
- Dùng Postman gửi GET tới URL API trên; không cần body.
- Muốn chạy HTTPS: dùng profile https, mở https://localhost:7166/.
  Nếu máy chưa tin chứng chỉ local, chạy `dotnet dev-certs https --trust`.
- Trong Visual Studio: mở solution, chọn WarehouseManagement làm Startup Project và chạy.
- Nếu trùng cổng, dừng ứng dụng dùng cổng đó hoặc đổi launchSettings.json trên máy của bạn.

## Cấu trúc

```text
WarehouseManagement.sln
warehouse_db_sqlserver.sql
SRS (1).docx
WarehouseManagement/
  AppData/
    Repositories/
  Areas/
  Components/
  Controllers/
    Api/
  Migrations/
  Models/
  Properties/
  Services/
  Views/
    Home/
    Shared/
      Components/
  wwwroot/
  Program.cs
  appsettings.json
```

| Lớp | Thư mục | Trách nhiệm |
| --- | --- | --- |
| Giao tiếp | Controllers, Views, Components, wwwroot | HTTP, JSON, giao diện |
| Nghiệp vụ | Services | Quyền nghiệp vụ, validation, điều phối thao tác |
| Dữ liệu | AppData, AppData/Repositories | DbContext, truy vấn, stored procedure |

Controllers → Services → AppData/Repositories → SQL Server.
Models chứa entity, DTO, ViewModel; chỉ chia nhỏ khi có nhiều file.
API dùng Controllers/Api và trả DTO; MVC controller trả View.
Interfaces đặt cùng thư mục với service/repository khi cần, chưa tạo thêm tầng thư mục.

Components dành cho View Component dùng chung. Areas là tùy chọn cho khu vực riêng.
Migrations để dành; không chạy migration khởi tạo trên schema SQL hiện có.
.gitkeep giúp giữ các thư mục trống khi clone. Có thể bỏ khi thư mục đã có code.

## Database và thông tin kết nối

SQL Server dự kiến: server `.`, database `warehouse_db`.
Dùng tài khoản SQL được cấp trên từng máy. Không commit mật khẩu vào appsettings.json.

Ví dụ cấu hình User Secrets, thay giá trị mẫu bằng thông tin máy bạn:

```powershell
dotnet user-secrets set "ConnectionStrings:WarehouseDb" "Server=.;Database=warehouse_db;User Id=YOUR_USER;Password=YOUR_PASSWORD;Encrypt=True;TrustServerCertificate=True" --project WarehouseManagement
```

UserSecretsId đã có trong csproj. Secrets được nạp ở môi trường Development.
Khung hiện chưa có DbContext hoặc đăng ký kết nối; cấu hình trên chuẩn bị cho bước triển khai Data.
Không dùng tài khoản sa như tài khoản đăng nhập giao diện.

File warehouse_db_sqlserver.sql là nguồn schema gồm bảng, trigger, procedure và view.
**Script có DROP DATABASE nếu warehouse_db tồn tại.** Chỉ chạy trên database phát triển
có thể tạo lại; sao lưu và kiểm tra trước khi chạy trong SSMS.
Ứng dụng không tự chạy script hoặc tự tạo database.

Khi bắt đầu phần Data, ánh xạ các bảng theo SQL, giữ khóa ghép, cột computed,
kiểu decimal/date, tên snake_case và cấu hình EF phù hợp trigger.
Bảng users dùng schema riêng; hash mẫu là placeholder, chưa đăng nhập được.
Không thêm IdentityDbContext mặc định mà chưa thiết kế tích hợp bảng users.

## Phạm vi nhóm

Ba vai trò Admin, Manager, Employee. Triển khai tài khoản, danh mục, sản phẩm,
nhà cung cấp, nhập/xuất có duyệt, tồn kho/cảnh báo, kiểm kê, báo cáo và audit log.
DELIVERY/DAMAGE/EXPIRED cùng module xuất. NSX/HSD nằm trên dòng nhập;
destination là văn bản trên phiếu xuất. Không có đa kho, POS, thanh toán hoặc Batch/Lot ở giai đoạn 1.

- Chỉ phiếu Nháp được sửa/xóa; giữ lịch sử giao dịch từ Chờ duyệt.
- Kiểm tra lại tồn ngay trước duyệt xuất; kiểm tra và cập nhật tồn/lịch sử cùng transaction.
- Chỉ thay đổi tồn qua nghiệp vụ hợp lệ và tạo StockMovement.
- Admin/Manager duyệt; backend/API luôn kiểm tra quyền.
- Sản phẩm/NCC ngừng sử dụng thay vì xóa lịch sử.
- Chưa có API sản phẩm, nhập/xuất, phân quyền hay chức năng database trong khung này.

## Quy ước Git cho nhóm

Repository gốc là thư mục chứa solution; không init Git trong project con.
.gitignore loại .vs, bin, obj, cấu hình cá nhân, FinalProject và template-backup.
Không dùng git add -f cho các thư mục tham khảo hoặc thông tin bí mật.
SRS và file SQL có thể commit cùng project để nhóm có cùng nguồn yêu cầu.

Mỗi chức năng làm trên nhánh feature/ten-chuc-nang, ví dụ feature/products.
Chia việc theo chức năng, thống nhất DTO/API và schema trước khi sửa chung.
Trước khi gửi PR: build thành công, chạy trang chủ và GET /api/health;
kiểm tra riêng các chức năng vừa triển khai.

Chuẩn bị commit đầu tiên (chưa được trợ lý commit/push):

```powershell
git status --short
git add .
git diff --cached --stat
git commit -m "Initialize warehouse MVC and API skeleton"
```

Sau khi tạo repository trống trên dịch vụ Git, thêm remote của nhóm rồi push.
Không có remote được cấu hình tự động. README này không chứa mật khẩu thực.
