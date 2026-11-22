using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WarehouseManagement.Models.Requests;
using WarehouseManagement.Services;

namespace WarehouseManagement.Controllers;

[Authorize(AuthenticationSchemes = "MvcCookie", Roles = "ADMIN")]
public class UserController(UserAccountService accounts) : Controller
{
    public async Task<IActionResult> Index()
    {
        Response.Headers.CacheControl = "no-store";
        return View(await accounts.ListAsync());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { error = "Thông tin tài khoản không hợp lệ." });
        if (!TryGetAdminId(out var adminId))
            return Unauthorized();

        var user = await accounts.CreateAsync(request, adminId);
        if (user is null)
            return Conflict(new { error = "Tên đăng nhập hoặc email đã tồn tại." });

        Response.Headers.CacheControl = "no-store";
        return StatusCode(StatusCodes.Status201Created, new CreatedUserResponse(
            user.Id, user.FullName, user.Username, user.Email, user.Role, user.Status));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUserRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { error = "Thông tin tài khoản không hợp lệ." });
        if (!TryGetAdminId(out var adminId))
            return Unauthorized();

        var result = await accounts.UpdateAsync(id, request, adminId);
        Response.Headers.CacheControl = "no-store";
        return result switch
        {
            UpdateUserResult.NotFound => NotFound(new { error = "Không tìm thấy tài khoản." }),
            UpdateUserResult.Conflict => Conflict(new { error = "Tên đăng nhập hoặc email đã tồn tại." }),
            UpdateUserResult.SelfPasswordDenied => Conflict(new { error = "Hãy đổi mật khẩu của bạn ở trang cá nhân." }),
            _ => NoContent()
        };
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activate(int id)
    {
        if (!TryGetAdminId(out var adminId))
            return Unauthorized();

        var result = await accounts.ActivateAsync(id, adminId);
        Response.Headers.CacheControl = "no-store";
        return result == ActivateUserResult.NotFound
            ? NotFound(new { error = "Không tìm thấy tài khoản." })
            : NoContent();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(int id)
    {
        if (!TryGetAdminId(out var adminId))
            return Unauthorized();

        var result = await accounts.DeactivateAsync(id, adminId);
        Response.Headers.CacheControl = "no-store";
        return result switch
        {
            DeactivateUserResult.NotFound => NotFound(new { error = "Không tìm thấy tài khoản." }),
            DeactivateUserResult.SelfDenied => Conflict(new { error = "Không thể vô hiệu hóa tài khoản đang sử dụng." }),
            _ => NoContent()
        };
    }

    private bool TryGetAdminId(out int adminId) =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out adminId);
}
