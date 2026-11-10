using System.Security.Cryptography;
using WarehouseManagement.Models;

namespace WarehouseManagement.Services;

public interface IRefreshSessionStore
{
    Task CreateAsync(RefreshSession session);
    Task<RotatedSession?> RotateAsync(string tokenHash, string newTokenHash, DateTime expiresAt);
    Task RevokeAsync(string sessionId, int userId);
    Task<bool> IsActiveAsync(string sessionId, int userId);
}

public sealed class RefreshSessionService(IRefreshSessionStore sessions, JwtTokenService jwt)
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromDays(7);

    public async Task<LoginResult> CreateAsync(User user)
    {
        var refreshToken = GenerateToken();
        var expiresAt = DateTime.UtcNow.Add(Lifetime);
        var sessionId = Guid.NewGuid().ToString("N");
        await sessions.CreateAsync(new RefreshSession
        {
            Id = sessionId, UserId = user.Id, TokenHash = Hash(refreshToken), ExpiresAt = expiresAt
        });
        return jwt.Create(user, sessionId, refreshToken, expiresAt);
    }

    public async Task<LoginResult?> RefreshAsync(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken) || refreshToken.Length > 256)
            return null;

        var replacement = GenerateToken();
        var expiresAt = DateTime.UtcNow.Add(Lifetime);
        var rotated = await sessions.RotateAsync(Hash(refreshToken), Hash(replacement), expiresAt);
        return rotated is null ? null : jwt.Create(rotated.User, rotated.SessionId, replacement, expiresAt);
    }

    private static string GenerateToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token)));
}

public sealed record RotatedSession(User User, string SessionId);
