using Microsoft.AspNetCore.Identity;
using WarehouseManagement.Models;

namespace WarehouseManagement.Services;

public interface IUserStore
{
    Task<User?> FindAsync(string identifier);
    Task RecordLoginAsync(User user);
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
        if (user is null || user.Status != "ACTIVE")
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
}
