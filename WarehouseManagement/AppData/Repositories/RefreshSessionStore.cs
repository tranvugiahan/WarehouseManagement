using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Models;
using WarehouseManagement.Services;

namespace WarehouseManagement.AppData.Repositories;

public sealed class RefreshSessionStore(WarehouseDbContext db) : IRefreshSessionStore
{
    public async Task CreateAsync(RefreshSession session)
    {
        db.RefreshSessions.Add(session);
        await db.SaveChangesAsync();
    }

    public async Task<RotatedSession?> RotateAsync(string tokenHash, string newTokenHash, DateTime expiresAt)
    {
        var now = DateTime.UtcNow;
        var session = await db.RefreshSessions.AsNoTracking()
            .SingleOrDefaultAsync(item => item.TokenHash == tokenHash && item.RevokedAt == null && item.ExpiresAt > now);
        if (session is null)
            return null;

        var user = await db.Users.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == session.UserId && item.Status == "ACTIVE");
        if (user is null)
            return null;

        var updated = await db.RefreshSessions
            .Where(item => item.Id == session.Id && item.TokenHash == tokenHash &&
                item.RevokedAt == null && item.ExpiresAt > now)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.TokenHash, newTokenHash)
                .SetProperty(item => item.ExpiresAt, expiresAt));
        return updated == 1 ? new RotatedSession(user, session.Id) : null;
    }

    public async Task RevokeAsync(string sessionId, int userId)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();
        var updated = await db.RefreshSessions
            .Where(item => item.Id == sessionId && item.UserId == userId && item.RevokedAt == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.RevokedAt, DateTime.UtcNow));
        if (updated == 1)
        {
            db.AuditLogs.Add(new AuditLog { UserId = userId, Action = "LOGOUT", Module = "AUTH" });
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
        }
    }

    public Task<bool> IsActiveAsync(string sessionId, int userId) =>
        db.RefreshSessions.AnyAsync(item => item.Id == sessionId && item.UserId == userId &&
            item.RevokedAt == null && item.ExpiresAt > DateTime.UtcNow &&
            db.Users.Any(user => user.Id == userId && user.Status == "ACTIVE"));
}
