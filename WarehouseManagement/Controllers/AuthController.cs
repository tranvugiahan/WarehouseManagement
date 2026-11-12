using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using WarehouseManagement.Services;

namespace WarehouseManagement.Controllers;

public class AuthController(AuthService auth, LogoutService logout) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Login()
    {
        if ((await HttpContext.AuthenticateAsync("MvcCookie")).Succeeded)
            return RedirectToAction("Index", "Home");

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string usernameOrEmail, string password)
    {
        var result = await auth.LoginAsync(usernameOrEmail, password);
        if (result is null)
        {
            ModelState.AddModelError(string.Empty, "Thông tin đăng nhập không hợp lệ.");
            return View();
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, result.UserId.ToString()),
            new Claim(ClaimTypes.Name, result.FullName),
            new Claim(ClaimTypes.Role, result.Role),
            new Claim("sid", new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler()
                .ReadJwtToken(result.AccessToken).Claims.First(claim => claim.Type == "sid").Value),
            new Claim("refresh_token", result.RefreshToken),
            new Claim("access_expires_at", result.ExpiresAt.ToString("O"))
        };
        await HttpContext.SignInAsync("MvcCookie",
            new ClaimsPrincipal(new ClaimsIdentity(claims, "MvcCookie")),
            new AuthenticationProperties { IsPersistent = true, ExpiresUtc = result.RefreshExpiresAt });
        Response.Headers.CacheControl = "no-store";
        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        var authentication = await HttpContext.AuthenticateAsync("MvcCookie");
        if (authentication.Succeeded && authentication.Principal is not null)
            await logout.LogoutAsync(authentication.Principal);

        await HttpContext.SignOutAsync("MvcCookie");
        Response.Headers.CacheControl = "no-store";
        return LocalRedirect(Url.Content("~/Auth/Login"));
    }
}
