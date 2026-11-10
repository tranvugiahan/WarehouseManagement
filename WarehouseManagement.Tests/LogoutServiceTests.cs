using System.Security.Claims;
using WarehouseManagement.Services;

namespace WarehouseManagement.Tests;

public class LogoutServiceTests
{
    [Fact]
    public async Task LogoutRevokesOnlyTheCurrentSession()
    {
        var sessions = new AuthServiceTests.FakeRefreshStore();
        await sessions.CreateAsync(new WarehouseManagement.Models.RefreshSession
        {
            Id = "session-one", UserId = 7, TokenHash = "hash", ExpiresAt = DateTime.UtcNow.AddDays(7)
        });
        var service = new LogoutService(sessions);
        var identity = new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, "7"),
            new Claim("sid", "session-one")
        ], "Bearer");

        await service.LogoutAsync(new ClaimsPrincipal(identity));

        Assert.False(await sessions.IsActiveAsync("session-one", 7));
    }

    [Fact]
    public async Task LogoutWithoutSessionIdIsRejected()
    {
        var service = new LogoutService(new AuthServiceTests.FakeRefreshStore());
        var principal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, "7")
        ], "Bearer"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.LogoutAsync(principal));
    }
}
