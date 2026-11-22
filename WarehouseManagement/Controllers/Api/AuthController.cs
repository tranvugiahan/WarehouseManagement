using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WarehouseManagement.Models.Requests;
using WarehouseManagement.Services;

namespace WarehouseManagement.Controllers.Api;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(AuthService auth, RefreshSessionService sessions, LogoutService logout) : ControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var result = await auth.LoginAsync(request.UsernameOrEmail, request.Password);
        Response.Headers.CacheControl = "no-store";
        return result is null
            ? Unauthorized(new { error = "Thông tin đăng nhập không hợp lệ." })
            : Ok(result);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(RefreshTokenRequest request)
    {
        var result = await sessions.RefreshAsync(request.RefreshToken);
        Response.Headers.CacheControl = "no-store";
        return result is null
            ? Unauthorized(new { error = "Phiên đăng nhập không hợp lệ hoặc đã hết hạn." })
            : Ok(result);
    }

    [Authorize(Roles = "ADMIN,MANAGER,EMPLOYEE")]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
    {
        var userIdText = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");
        if (!int.TryParse(userIdText, out var userId))
            return Unauthorized();

        var changed = await auth.ChangePasswordAsync(userId, request.CurrentPassword, request.NewPassword);
        Response.Headers.CacheControl = "no-store";
        return changed
            ? NoContent()
            : BadRequest(new { error = "Mật khẩu hiện tại không hợp lệ hoặc tài khoản không hoạt động." });
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await logout.LogoutAsync(User);
        return NoContent();
    }
}
