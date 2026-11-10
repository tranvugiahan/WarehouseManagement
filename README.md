# WarehouseManagement

Khung ASP.NET Core MVC + REST API cho đồ án quản lý kho, theo SRS (1).docx.
Một project .NET 8, tổ chức ba lớp. Đã có 15 endpoints danh mục, đơn vị tính, nhà cung cấp và API xác thực JWT.

## Chạy sau khi clone

Cần .NET SDK 8; nếu dùng Visual Studio, cài workload ASP.NET and web development.
Chạy các lệnh từ thư mục chứa WarehouseManagement.sln:

```powershell
dotnet restore WarehouseManagement.sln
dotnet build WarehouseManagement.sln --no-restore
dotnet run --project WarehouseManagement --launch-profile http
```

- Trang MVC: http://localhost:8080/
- Swagger UI (Development): http://localhost:8080/swagger hoặc https://localhost:8443/swagger khi chạy profile https.
- Visual Studio mở Swagger khi Run. Chọn endpoint → Try it out → nhập ID/JSON → Execute.
- OpenAPI JSON: /swagger/v1/swagger.json. Swagger tự đọc các API controller và request DTO.
- API kiểm tra ứng dụng: GET http://localhost:8080/api/health
- Kết quả API: HTTP 200, JSON `{"status":"ok"}`.
- Ứng dụng khởi động được khi chưa có SQL Server. Health chỉ kiểm tra web, không kiểm tra database.
- Dùng Postman gửi GET tới URL API trên; không cần body.
- Muốn chạy HTTPS: dùng profile https, mở https://localhost:8443/.
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
| Dữ liệu | AppData/Repositories, Models/WarehouseDbContext.cs | DbContext, truy vấn, stored procedure |

Controllers → Services → AppData/Repositories → SQL Server.
Models chứa entity, DTO, ViewModel; chỉ chia nhỏ khi có nhiều file.
API dùng Controllers/Api, nhận request DTO tại Models/Requests; hiện trả entity danh mục. MVC controller trả View.
Interfaces đặt cùng thư mục với service/repository khi cần, chưa tạo thêm tầng thư mục.

Components dành cho View Component dùng chung. Areas là tùy chọn cho khu vực riêng.
Migrations để dành; không chạy migration khởi tạo trên schema SQL hiện có.
.gitkeep giúp giữ các thư mục trống khi clone. Có thể bỏ khi thư mục đã có code.

## API đăng nhập

`POST /api/auth/login` nhận JSON `{"usernameOrEmail":"employee","password":"..."}`. Đăng nhập thành công trả `accessToken`, `expiresAt`, `refreshToken`, `refreshExpiresAt` (UTC), `userId`, `fullName`, `role`; dùng access token trong header `Authorization: Bearer <accessToken>` cho các API cần xác thực. Username và email đều được chấp nhận. Sai thông tin hoặc tài khoản `INACTIVE` đều trả `401` với cùng thông báo.

`POST /api/auth/refresh` nhận JSON `{"refreshToken":"..."}` và không cần Bearer token. Thành công trả cùng cấu trúc với login, gồm access token và refresh token mới. Mỗi lần gọi, refresh token cũ mất hiệu lực và hạn phiên được gia hạn thêm 7 ngày; token cũ, hết hạn, đã đăng xuất hoặc tài khoản `INACTIVE` trả `401`. Access token có hạn 15 phút. Frontend cần lưu token an toàn và chủ động gọi refresh trước hoặc sau khi access token hết hạn; phiên chỉ tiếp tục khi được làm mới trong mỗi khoảng 7 ngày.

`POST /api/auth/logout` yêu cầu Bearer token còn hạn, không cần body và trả `204`. Backend thu hồi phiên hiện tại trong `refresh_sessions`; mọi access token và refresh token của phiên này sẽ nhận `401`. Mỗi request dùng Bearer token đều kiểm tra `sid` và trạng thái phiên trong database. JWT cũ không có `sid` sẽ không còn dùng được sau khi cập nhật ứng dụng.

Với database `warehouse_db` đã có bảng `users`, chạy duy nhất [warehouse_db_sqlserver.sql](warehouse_db_sqlserver.sql) để thêm bảng `refresh_sessions` nếu còn thiếu. Script có thể chạy lại, không xóa database, bảng hay dữ liệu. Bảng `revoked_tokens` cũ nếu đã tồn tại vẫn được giữ nguyên nhưng API hiện không dùng. Trong SSMS, mở file rồi Execute; hoặc chạy từ thư mục repo:

```powershell
sqlcmd -S . -E -b -i warehouse_db_sqlserver.sql
```

Thay `.` bằng tên SQL Server của thành viên nếu khác; `-E` dùng Windows Authentication. Chưa có frontend hoặc đổi mật khẩu trong phạm vi này.

JWT có hạn 15 phút. Cấu hình khóa ký bí mật (ít nhất 32 byte) bằng User Secrets hoặc biến môi trường `Jwt__Key`; không ghi khóa vào `appsettings.json`:

```powershell
dotnet user-secrets set "Jwt:Key" "YOUR_RANDOM_SECRET_AT_LEAST_32_BYTES" --project WarehouseManagement
```

Mật khẩu trong `users.password_hash` phải là hash do `Microsoft.AspNetCore.Identity.PasswordHasher<User>` tạo. Hash của các tài khoản mẫu trong schema gốc chỉ là placeholder và không thể đăng nhập. API chưa bảo vệ các endpoint danh mục hiện có bằng role; việc đó thuộc phần phân quyền tiếp theo.

## Database và thông tin kết nối

SQL Server dự kiến: server `.`, database `warehouse_db`.
Dùng tài khoản SQL được cấp trên từng máy. Không commit mật khẩu vào appsettings.json.

Ví dụ cấu hình User Secrets, thay giá trị mẫu bằng thông tin máy bạn:

```powershell
dotnet user-secrets set "ConnectionStrings:WarehouseDb" "Server=.;Database=warehouse_db;User Id=YOUR_USER;Password=YOUR_PASSWORD;Encrypt=True;TrustServerCertificate=True" --project WarehouseManagement
```

UserSecretsId đã có trong csproj. Secrets được nạp ở môi trường Development.
DbContext đã scaffold tại Models/WarehouseDbContext.cs. Appsettings dùng Windows Authentication; cấu hình User Secrets ở trên dùng cho SQL Authentication và được ưu tiên trong Development.
Không dùng tài khoản sa như tài khoản đăng nhập giao diện.

File warehouse_db_sqlserver.sql hiện là script cập nhật an toàn cho database đã có schema gốc; không dùng để tạo database từ đầu. Bản tạo schema gốc ở commit `ccae911` trong lịch sử Git. Nếu chưa có `warehouse_db`, cần tạo schema gốc trước rồi chạy file cập nhật này.
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
- Đã có CRUD danh mục, đơn vị tính, nhà cung cấp; chưa có API sản phẩm, nhập/xuất hay phân quyền.

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

## Demo API danh mục

Ba nhóm URL: /api/categories, /api/units, /api/suppliers.
Mỗi nhóm có GET danh sách, GET /{id}, POST, PUT /{id}, DELETE /{id}.
DELETE đổi status thành INACTIVE, không xóa vật lý; GET vẫn trả cả bản ghi INACTIVE.

POST categories/units: {"name":"Tên mới"} (service tự đặt ACTIVE).
PUT categories/units: {"name":"Tên sửa","status":"ACTIVE"}.
Supplier có thêm phone, email, address (không bắt buộc).
PUT thay thế các trường có thể sửa; các trường tùy chọn bị bỏ qua sẽ thành null.
Status trên PUT bắt buộc là ACTIVE hoặc INACTIVE.
Không gửi Products/ImportOrders; request DTO không nhận các quan hệ này.
POST trả 201; PUT/DELETE thành công trả 204, không có response body.

Scaffold lại: giữ CatalogEntities.Custom.cs, IEntity.cs và thư mục Requests.
Chỉ scaffold các file sinh tự động; không xóa cả thư mục Models.
Không sinh OnConfiguring chứa connection string; tiếp tục dùng DI trong Program.cs.
