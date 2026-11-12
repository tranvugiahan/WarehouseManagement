using System.Text;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using WarehouseManagement.AppData.Repositories;
using WarehouseManagement.Models;
using WarehouseManagement.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddProblemDetails();
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Chưa cấu hình Jwt:Key trong User Secrets hoặc biến môi trường.");
if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
    throw new InvalidOperationException("Jwt:Key phải dài ít nhất 32 byte.");
const string jwtIssuer = "WarehouseManagement";
const string jwtAudience = "WarehouseManagement.Api";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true, ValidIssuer = jwtIssuer,
            ValidateAudience = true, ValidAudience = jwtAudience,
            ValidateLifetime = true, ClockSkew = TimeSpan.Zero,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var sessionId = context.Principal?.FindFirst("sid")?.Value;
                var userIdText = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                    ?? context.Principal?.FindFirst("sub")?.Value;
                if (string.IsNullOrWhiteSpace(sessionId) || !int.TryParse(userIdText, out var userId) ||
                    !await context.HttpContext.RequestServices.GetRequiredService<IRefreshSessionStore>()
                        .IsActiveAsync(sessionId, userId))
                    context.Fail("Phiên đăng nhập đã hết hạn hoặc bị thu hồi.");
            }
        };
    })
    .AddCookie("MvcCookie", options =>
    {
        options.LoginPath = "/Auth/Login";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = false;
        options.Events.OnValidatePrincipal = async context =>
        {
            var principal = context.Principal;
            var sessionId = principal?.FindFirstValue("sid");
            var userIdText = principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            var refreshToken = principal?.FindFirstValue("refresh_token");
            var accessExpiresText = principal?.FindFirstValue("access_expires_at");
            var store = context.HttpContext.RequestServices.GetRequiredService<IRefreshSessionStore>();

            if (string.IsNullOrEmpty(sessionId) || !int.TryParse(userIdText, out var userId) ||
                string.IsNullOrEmpty(refreshToken) || !DateTime.TryParse(accessExpiresText, out var accessExpiresAt) ||
                !await store.IsActiveAsync(sessionId, userId))
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync("MvcCookie");
                return;
            }

            if (accessExpiresAt > DateTime.UtcNow)
                return;

            var sessions = context.HttpContext.RequestServices.GetRequiredService<RefreshSessionService>();
            var refreshed = await sessions.RefreshAsync(refreshToken);
            if (refreshed is null)
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync("MvcCookie");
                return;
            }

            var identity = (ClaimsIdentity)principal!.Identity!;
            identity.RemoveClaim(identity.FindFirst("refresh_token")!);
            identity.RemoveClaim(identity.FindFirst("access_expires_at")!);
            identity.AddClaim(new Claim("refresh_token", refreshed.RefreshToken));
            identity.AddClaim(new Claim("access_expires_at", refreshed.ExpiresAt.ToString("O")));
            context.Properties.ExpiresUtc = refreshed.RefreshExpiresAt;
            context.ShouldRenew = true;
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddScoped<IUserStore, UserStore>();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<IRefreshSessionStore, RefreshSessionStore>();
builder.Services.AddScoped<RefreshSessionService>();
builder.Services.AddScoped<LogoutService>();
builder.Services.AddSingleton(new JwtTokenService(jwtIssuer, jwtAudience, jwtKey));
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
app.UseAuthentication();
app.UseAuthorization();

// Các API có [Route].
app.MapControllers();

// Trang MVC như trang chủ.
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
