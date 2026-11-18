using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WarehouseManagement.Services;

namespace WarehouseManagement.Controllers;

[Authorize(AuthenticationSchemes = "MvcCookie")]
public class AccountController(AuthService auth) : Controller
{
    public IActionResult Profile() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
    {
        if (string.IsNullOrEmpty(currentPassword) || string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmPassword))
        {
            TempData["PasswordChangeError"] = "Vui lòng nhập đầy đủ các ô mật khẩu.";
        }
        else if (newPassword != confirmPassword)
        {
            TempData["PasswordChangeError"] = "Mật khẩu xác nhận không khớp.";
        }
        else if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            || !await auth.ChangePasswordAsync(userId, currentPassword, newPassword))
        {
            TempData["PasswordChangeError"] = "Mật khẩu hiện tại không đúng hoặc tài khoản không còn hoạt động.";
        }
        else
        {
            TempData["PasswordChangeSuccess"] = "Đổi mật khẩu thành công. Hãy dùng mật khẩu mới trong lần đăng nhập tiếp theo.";
        }

        return RedirectToAction(nameof(Profile));
    }
}
