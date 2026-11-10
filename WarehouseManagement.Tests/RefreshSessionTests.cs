using WarehouseManagement.Models;
using WarehouseManagement.Services;

namespace WarehouseManagement.Tests;

public class RefreshSessionTests
{
    [Fact]
    public async Task RefreshRotatesTokenAndExtendsSession()
    {
        var user = new User { Id = 7, Username = "employee", FullName = "Employee",
            Role = "EMPLOYEE", Status = "ACTIVE" };
        var store = new AuthServiceTests.FakeRefreshStore { User = user };
        var service = new RefreshSessionService(store,
            new JwtTokenService("issuer", "audience", new string('k', 64)));
        var first = await service.CreateAsync(user);
        Assert.InRange(first.RefreshExpiresAt, DateTime.UtcNow.AddDays(6).AddHours(23),
            DateTime.UtcNow.AddDays(7).AddMinutes(1));

        var second = await service.RefreshAsync(first.RefreshToken);

        Assert.NotNull(second);
        Assert.NotEqual(first.RefreshToken, second.RefreshToken);
        Assert.NotEqual(first.AccessToken, second.AccessToken);
        Assert.True(second.RefreshExpiresAt >= first.RefreshExpiresAt);
        Assert.Null(await service.RefreshAsync(first.RefreshToken));
        Assert.NotNull(await service.RefreshAsync(second.RefreshToken));
    }

    [Fact]
    public async Task RevokedOrInactiveSessionCannotRefresh()
    {
        var user = new User { Id = 7, Username = "employee", FullName = "Employee",
            Role = "EMPLOYEE", Status = "ACTIVE" };
        var store = new AuthServiceTests.FakeRefreshStore { User = user };
        var service = new RefreshSessionService(store,
            new JwtTokenService("issuer", "audience", new string('k', 64)));
        var first = await service.CreateAsync(user);
        user.Status = "INACTIVE";
        Assert.Null(await service.RefreshAsync(first.RefreshToken));
        user.Status = "ACTIVE";
        await store.RevokeAsync(store.Session!.Id, user.Id);
        Assert.Null(await service.RefreshAsync(first.RefreshToken));
    }
}
