using Microsoft.AspNetCore.Identity;
using WarehouseManagement.Models;

namespace WarehouseManagement.Services;

public interface IUserStore
{
    Task<User?> FindAsync(string identifier);
    Task<User?> FindActiveByIdAsync(int userId);
    Task RecordLoginAsync(User user);
    Task UpdatePasswordAsync(User user);
    Task<bool> CreateAsync(User user, int createdBy);
    Task<User?> FindByIdAsync(int userId);
    Task<bool> UpdateAccountAsync(User user, int updatedBy);
    Task<DeactivateUserResult> DeactivateAsync(int userId, int deactivatedBy);
}

public sealed record LoginResult(string AccessToken, DateTime ExpiresAt, int UserId, string FullName,
    string Role, string RefreshToken, DateTime RefreshExpiresAt);

public sealed class AuthService(IUserStore users, IPasswordHasher<User> passwordHasher,
    RefreshSessionService sessions, WarehouseDbContext db)
{
    public async Task<LoginResult?> LoginAsync(string identifier, string password)
    {
        if (string.IsNullOrWhiteSpace(identifier) || string.IsNullOrEmpty(password))
            return null;

        var user = await users.FindAsync(identifier.Trim());
        if (user is null || user.Status != "ACTIVE" ||
            user.Role is not ("ADMIN" or "MANAGER" or "EMPLOYEE"))
            return null;

        PasswordVerificationResult verification;
        try
        {
            verification = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        }
        catch (FormatException)
        {
            return null;
        }
        if (verification == PasswordVerificationResult.Failed)
            return null;

        await using var transaction = await db.Database.BeginTransactionAsync();
        var result = await sessions.CreateAsync(user);
        await users.RecordLoginAsync(user);
        await transaction.CommitAsync();
        return result;
    }

    public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
    {
        if (string.IsNullOrEmpty(currentPassword) || string.IsNullOrEmpty(newPassword))
            return false;

        var user = await users.FindActiveByIdAsync(userId);
        if (user is null)
            return false;

        PasswordVerificationResult verification;
        try
        {
            verification = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, currentPassword);
        }
        catch (FormatException)
        {
            return false;
        }
        if (verification == PasswordVerificationResult.Failed)
            return false;

        user.PasswordHash = passwordHasher.HashPassword(user, newPassword);
        await users.UpdatePasswordAsync(user);
        return true;
    }
}
