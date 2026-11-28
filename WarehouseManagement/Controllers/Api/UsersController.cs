using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WarehouseManagement.Models.Requests;
using WarehouseManagement.Services;

namespace WarehouseManagement.Controllers.Api;

[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "ADMIN")]
[Route("api/users")]
public sealed class UsersController(UserAccountService accounts) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(CreatedUserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(CreateUserRequest request)
    {
        var adminIdText = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!int.TryParse(adminIdText, out var adminId))
            return Unauthorized();

        var user = await accounts.CreateAsync(request, adminId);
        if (user is null)
            return Conflict(new { error = "Username hoặc email đã tồn tại." });

        Response.Headers.CacheControl = "no-store";
        return StatusCode(StatusCodes.Status201Created, new CreatedUserResponse(
            user.Id, user.FullName, user.Username, user.Email, user.Role, user.Status));
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(int id, UpdateUserRequest request)
    {
        var adminIdText = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!int.TryParse(adminIdText, out var adminId))
            return Unauthorized();

        var result = await accounts.UpdateAsync(id, request, adminId);
        Response.Headers.CacheControl = "no-store";
        return result switch
        {
            UpdateUserResult.NotFound => NotFound(),
            UpdateUserResult.Conflict => Conflict(new { error = "Username hoặc email đã tồn tại." }),
            UpdateUserResult.SelfPasswordDenied => Conflict(new { error = "Hãy đổi mật khẩu của bạn ở trang cá nhân." }),
            UpdateUserResult.SelfRoleDenied => Conflict(new { error = "Không thể thay đổi vai trò của tài khoản đang sử dụng." }),
            _ => NoContent()
        };
    }

    [HttpPut("{id:int}/activate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Activate(int id)
    {
        var adminIdText = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!int.TryParse(adminIdText, out var adminId))
            return Unauthorized();

        var result = await accounts.ActivateAsync(id, adminId);
        Response.Headers.CacheControl = "no-store";
        return result == ActivateUserResult.NotFound ? NotFound() : NoContent();
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Deactivate(int id)
    {
        var adminIdText = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!int.TryParse(adminIdText, out var adminId))
            return Unauthorized();

        var result = await accounts.DeactivateAsync(id, adminId);
        Response.Headers.CacheControl = "no-store";
        return result switch
        {
            DeactivateUserResult.NotFound => NotFound(),
            DeactivateUserResult.SelfDenied => Conflict(new { error = "Không thể vô hiệu hóa tài khoản đang sử dụng." }),
            _ => NoContent()
        };
    }

    [HttpPost("{id:int}/reset-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ResetPassword(int id, ResetUserPasswordRequest request)
    {
        var adminIdText = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!int.TryParse(adminIdText, out var adminId))
            return Unauthorized();

        var result = await accounts.ResetPasswordAsync(id, request.NewPassword, adminId);
        Response.Headers.CacheControl = "no-store";
        return result switch
        {
            ResetPasswordResult.NotFound => NotFound(),
            ResetPasswordResult.SelfDenied => Conflict(new { error = "Hãy đổi mật khẩu của bạn ở trang cá nhân." }),
            _ => NoContent()
        };
    }
}
