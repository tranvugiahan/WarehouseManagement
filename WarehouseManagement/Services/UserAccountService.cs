using Microsoft.AspNetCore.Identity;
using WarehouseManagement.Models;
using WarehouseManagement.Models.Requests;

namespace WarehouseManagement.Services;

public enum UpdateUserResult { Updated, NotFound, Conflict }
public enum DeactivateUserResult { Deactivated, AlreadyInactive, NotFound, SelfDenied }

public sealed class UserAccountService(IUserStore users, IPasswordHasher<User> passwordHasher)
{
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
        var user = await users.FindByIdAsync(userId);
        if (user is null)
            return UpdateUserResult.NotFound;

        user.FullName = request.FullName.Trim();
        user.Username = request.Username.Trim();
        user.Email = request.Email.Trim();
        user.Role = request.Role;
        return await users.UpdateAccountAsync(user, updatedBy)
            ? UpdateUserResult.Updated
            : UpdateUserResult.Conflict;
    }

    public Task<DeactivateUserResult> DeactivateAsync(int userId, int deactivatedBy) =>
        userId == deactivatedBy
            ? Task.FromResult(DeactivateUserResult.SelfDenied)
            : users.DeactivateAsync(userId, deactivatedBy);
}
