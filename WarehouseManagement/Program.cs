using Microsoft.EntityFrameworkCore;
using WarehouseManagement.AppData.Repositories;
using WarehouseManagement.Models;
using WarehouseManagement.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddProblemDetails();


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
