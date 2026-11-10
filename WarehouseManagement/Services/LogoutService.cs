using System.Security.Claims;

namespace WarehouseManagement.Services;

public sealed class LogoutService(IRefreshSessionStore sessions)
{
    public async Task LogoutAsync(ClaimsPrincipal principal)
    {
        var userIdText = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub");
        var sessionId = principal.FindFirstValue("sid");
        if (string.IsNullOrWhiteSpace(sessionId) || !int.TryParse(userIdText, out var userId))
            throw new InvalidOperationException("JWT thiếu thông tin phiên hợp lệ.");

        await sessions.RevokeAsync(sessionId, userId);
    }
}
