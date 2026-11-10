using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using WarehouseManagement.Models;
using WarehouseManagement.Services;

namespace WarehouseManagement.Tests;

public class AuthServiceTests
{
    private static readonly User ActiveUser = new()
    {
        Id = 7, Username = "employee", Email = "employee@example.test",
        FullName = "Test Employee", Role = "EMPLOYEE", Status = "ACTIVE"
    };

    [Theory]
    [InlineData("employee")]
    [InlineData("employee@example.test")]
    public async Task ValidCredentialsIssueToken(string identifier)
    {
        var user = new User { Id = ActiveUser.Id, Username = ActiveUser.Username,
            Email = ActiveUser.Email, FullName = ActiveUser.FullName,
            Role = ActiveUser.Role, Status = ActiveUser.Status };
        user.PasswordHash = new PasswordHasher<User>().HashPassword(user, "correct-password");
        var store = new FakeUserStore(user);
        var service = new AuthService(store, new PasswordHasher<User>(),
            new RefreshSessionService(new FakeRefreshStore(),
                new JwtTokenService("test-issuer", "test-audience", new string('k', 64))));

        var result = await service.LoginAsync(identifier, "correct-password");

        Assert.NotNull(result);
        Assert.NotEmpty(result.AccessToken);
        Assert.NotEmpty(result.RefreshToken);
        Assert.InRange(result.RefreshExpiresAt, DateTime.UtcNow.AddDays(6).AddHours(23),
            DateTime.UtcNow.AddDays(7).AddMinutes(1));
        Assert.InRange(result.ExpiresAt, DateTime.UtcNow.AddMinutes(14),
            DateTime.UtcNow.AddMinutes(15).AddSeconds(10));
        Assert.Equal("EMPLOYEE", result.Role);
        Assert.Equal(7, result.UserId);
        Assert.True(store.LoginRecorded);
        var principal = new JwtSecurityTokenHandler().ValidateToken(result.AccessToken,
            new TokenValidationParameters
            {
                ValidateIssuer = true, ValidIssuer = "test-issuer",
                ValidateAudience = true, ValidAudience = "test-audience",
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(new string('k', 64)))
            }, out _);
        Assert.Equal("EMPLOYEE", principal.FindFirst(ClaimTypes.Role)?.Value);
        Assert.False(string.IsNullOrWhiteSpace(principal.FindFirst(JwtRegisteredClaimNames.Jti)?.Value));
        Assert.False(string.IsNullOrWhiteSpace(principal.FindFirst("sid")?.Value));
    }

    [Theory]
    [InlineData("wrong-password", "ACTIVE")]
    [InlineData("correct-password", "INACTIVE")]
    public async Task InvalidCredentialsOrInactiveAccountNeverIssueToken(string password, string status)
    {
        var user = new User { Id = 7, Username = "employee", Email = "employee@example.test",
            FullName = "Test Employee", Role = "EMPLOYEE", Status = status };
        user.PasswordHash = new PasswordHasher<User>().HashPassword(user, "correct-password");
        var store = new FakeUserStore(user);
        var service = new AuthService(store, new PasswordHasher<User>(),
            new RefreshSessionService(new FakeRefreshStore(),
                new JwtTokenService("test-issuer", "test-audience", new string('k', 64))));

        Assert.Null(await service.LoginAsync("employee", password));
        Assert.False(store.LoginRecorded);
    }

    [Fact]
    public async Task PlaceholderHashCannotCrashLogin()
    {
        var user = new User { Id = 7, Username = "employee", Email = "employee@example.test",
            FullName = "Test Employee", Role = "EMPLOYEE", Status = "ACTIVE",
            PasswordHash = "not-a-password-hash" };
        var store = new FakeUserStore(user);
        var service = new AuthService(store, new PasswordHasher<User>(),
            new RefreshSessionService(new FakeRefreshStore(),
                new JwtTokenService("test-issuer", "test-audience", new string('k', 64))));

        Assert.Null(await service.LoginAsync("employee", "anything"));
        Assert.False(store.LoginRecorded);
    }

    private sealed class FakeUserStore(User user) : IUserStore
    {
        public bool LoginRecorded { get; private set; }
        public Task<User?> FindAsync(string identifier) => Task.FromResult<User?>(
            identifier == user.Username || identifier == user.Email ? user : null);
        public Task RecordLoginAsync(User _) { LoginRecorded = true; return Task.CompletedTask; }
    }

    internal sealed class FakeRefreshStore : IRefreshSessionStore
    {
        public RefreshSession? Session { get; private set; }
        public User? User { get; set; }
        public Task CreateAsync(RefreshSession session) { Session = session; return Task.CompletedTask; }
        public Task<RotatedSession?> RotateAsync(string hash, string newHash, DateTime expiresAt)
        {
            if (Session is null || Session.TokenHash != hash || Session.RevokedAt is not null ||
                Session.ExpiresAt <= DateTime.UtcNow || User?.Status != "ACTIVE")
                return Task.FromResult<RotatedSession?>(null);
            Session.TokenHash = newHash;
            Session.ExpiresAt = expiresAt;
            return Task.FromResult<RotatedSession?>(new RotatedSession(User, Session.Id));
        }
        public Task RevokeAsync(string sessionId, int userId)
        {
            if (Session?.Id == sessionId && Session.UserId == userId) Session.RevokedAt = DateTime.UtcNow;
            return Task.CompletedTask;
        }
        public Task<bool> IsActiveAsync(string sessionId, int userId) => Task.FromResult(
            Session?.Id == sessionId && Session.UserId == userId && Session.RevokedAt is null &&
            Session.ExpiresAt > DateTime.UtcNow);
    }
}
