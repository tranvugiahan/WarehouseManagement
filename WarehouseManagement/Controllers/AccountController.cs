using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WarehouseManagement.Services;
using WarehouseManagement.Models.Requests;

namespace WarehouseManagement.Controllers;

[Authorize(AuthenticationSchemes = "MvcCookie")]
public class AccountController(AuthService auth) : Controller
{
    public IActionResult Profile() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(
    [FromBody] ChangePasswordRequest request)
    {
        if (string.IsNullOrEmpty(request.CurrentPassword) ||
            string.IsNullOrEmpty(request.NewPassword))
        {
            return BadRequest(new { error = "Vui lòng nhập đủ các ô mật khẩu." });
        }

        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ||
            !await auth.ChangePasswordAsync(
                userId,
                request.CurrentPassword,
                request.NewPassword))
        {
            return BadRequest(new
            {
                error = "Mật khẩu hiện tại không đúng hoặc tài khoản không còn hoạt động."
            });
        }

        return NoContent();
    }

}
