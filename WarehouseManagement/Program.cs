using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using WarehouseManagement.AppData.Repositories;
using WarehouseManagement.Models;
using WarehouseManagement.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddProblemDetails();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "WarehouseManagement API",
        Version = "v1",
        Description = "API quản lý kho: danh mục, đơn vị tính và nhà cung cấp. DELETE là xóa mềm (Status = INACTIVE). Khi PUT, gửi Status là ACTIVE hoặc INACTIVE."
    });
});


var connectionString = builder.Configuration
    .GetConnectionString("WarehouseDb")
    ?? throw new InvalidOperationException(
        "Chưa cấu hình ConnectionStrings:WarehouseDb.");


builder.Services.AddDbContext<WarehouseDbContext>(options =>
    options.UseSqlServer(connectionString));


builder.Services.AddScoped(
    typeof(CatalogRepository<>),
    typeof(CatalogRepository<>));

builder.Services.AddScoped(
    typeof(CatalogService<>),
    typeof(CatalogService<>));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("v1/swagger.json", "WarehouseManagement API v1");
        options.DocumentTitle = "WarehouseManagement - Swagger UI";
    });
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler();
    app.UseHsts();

    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

// Các API có [Route].
app.MapControllers();

// Trang MVC như trang chủ.
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
