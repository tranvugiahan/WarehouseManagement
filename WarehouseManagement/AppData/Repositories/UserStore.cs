using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Models;
using WarehouseManagement.Services;

namespace WarehouseManagement.AppData.Repositories;

public sealed class UserStore(WarehouseDbContext db) : IUserStore
{
    public async Task<User?> FindAsync(string identifier) =>
        await db.Users.FirstOrDefaultAsync(user => user.Username == identifier)
        ?? await db.Users.FirstOrDefaultAsync(user => user.Email == identifier);

    public async Task RecordLoginAsync(User user)
    {
        user.LastLoginAt = DateTime.UtcNow;
        db.AuditLogs.Add(new AuditLog { UserId = user.Id, Action = "LOGIN", Module = "AUTH" });
        await db.SaveChangesAsync();
    }
}
