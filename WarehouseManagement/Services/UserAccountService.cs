using Microsoft.AspNetCore.Identity;
using WarehouseManagement.Models;
using WarehouseManagement.Models.Requests;

namespace WarehouseManagement.Services;

public enum UpdateUserResult { Updated, NotFound, Conflict, SelfPasswordDenied }
public enum DeactivateUserResult { Deactivated, AlreadyInactive, NotFound, SelfDenied }
public enum ActivateUserResult { Activated, AlreadyActive, NotFound }
public enum ResetPasswordResult { Reset, NotFound, SelfDenied }

public sealed class UserAccountService(IUserStore users, IPasswordHasher<User> passwordHasher)
{
    public async Task<IReadOnlyList<UserSummary>> ListAsync() =>
        (await users.ListAsync()).Select(user => new UserSummary(
            user.Id, user.FullName, user.Username, user.Email, user.Role, user.Status, user.CreatedAt)).ToList();

    public async Task<User?> CreateAsync(CreateUserRequest request, int createdBy)
    {
        var user = new User
        {
            FullName = request.FullName.Trim(),
            Username = request.Username.Trim(),
            Email = request.Email.Trim(),
            Role = request.Role,
            Status = "ACTIVE"
        };
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
        return await users.CreateAsync(user, createdBy) ? user : null;
    }

    public async Task<UpdateUserResult> UpdateAsync(int userId, UpdateUserRequest request, int updatedBy)
    {
        var passwordChanged = !string.IsNullOrEmpty(request.NewPassword);
        if (passwordChanged && userId == updatedBy)
            return UpdateUserResult.SelfPasswordDenied;

        var user = await users.FindByIdAsync(userId);
        if (user is null)
            return UpdateUserResult.NotFound;

        user.FullName = request.FullName.Trim();
        user.Username = request.Username.Trim();
        user.Email = request.Email.Trim();
        user.Role = request.Role;
        if (passwordChanged)
            user.PasswordHash = passwordHasher.HashPassword(user, request.NewPassword!);

        return await users.UpdateAccountAsync(user, updatedBy, passwordChanged)
            ? UpdateUserResult.Updated
            : UpdateUserResult.Conflict;
    }

    public Task<DeactivateUserResult> DeactivateAsync(int userId, int deactivatedBy) =>
        userId == deactivatedBy
            ? Task.FromResult(DeactivateUserResult.SelfDenied)
            : users.DeactivateAsync(userId, deactivatedBy);

    public Task<ActivateUserResult> ActivateAsync(int userId, int activatedBy) =>
        users.ActivateAsync(userId, activatedBy);

    public async Task<ResetPasswordResult> ResetPasswordAsync(int userId, string newPassword, int resetBy)
    {
        if (userId == resetBy)
            return ResetPasswordResult.SelfDenied;

        var user = await users.FindByIdAsync(userId);
        if (user is null)
            return ResetPasswordResult.NotFound;

        user.PasswordHash = passwordHasher.HashPassword(user, newPassword);
        await users.ResetPasswordAsync(user, resetBy);
        return ResetPasswordResult.Reset;
    }
}

public sealed record UserSummary(int Id, string FullName, string Username, string Email,
    string Role, string Status, DateTime CreatedAt);
