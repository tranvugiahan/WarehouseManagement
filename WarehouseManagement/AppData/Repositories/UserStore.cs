using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Models;
using WarehouseManagement.Services;

namespace WarehouseManagement.AppData.Repositories;

public sealed class UserStore(WarehouseDbContext db) : IUserStore
{
    public async Task<User?> FindAsync(string identifier) =>
        await db.Users.FirstOrDefaultAsync(user => user.Username == identifier)
        ?? await db.Users.FirstOrDefaultAsync(user => user.Email == identifier);

    public Task<User?> FindActiveByIdAsync(int userId) =>
        db.Users.SingleOrDefaultAsync(user => user.Id == userId && user.Status == "ACTIVE");

    public async Task RecordLoginAsync(User user)
    {
        user.LastLoginAt = DateTime.UtcNow;
        db.AuditLogs.Add(new AuditLog { UserId = user.Id, Action = "LOGIN", Module = "AUTH" });
        await db.SaveChangesAsync();
    }

    public async Task UpdatePasswordAsync(User user)
    {
        db.AuditLogs.Add(new AuditLog { UserId = user.Id, Action = "CHANGE_PASSWORD", Module = "AUTH" });
        await db.SaveChangesAsync();
    }
}
