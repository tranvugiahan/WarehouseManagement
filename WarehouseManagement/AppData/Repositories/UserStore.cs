using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using WarehouseManagement.Models;
using WarehouseManagement.Services;

namespace WarehouseManagement.AppData.Repositories;

public sealed class UserStore(WarehouseDbContext db) : IUserStore
{
    public async Task<IReadOnlyList<User>> ListAsync() =>
        await db.Users.AsNoTracking().OrderBy(user => user.Id).ToListAsync();

    public async Task<User?> FindAsync(string identifier) =>
        await db.Users.FirstOrDefaultAsync(user => user.Username == identifier)
        ?? await db.Users.FirstOrDefaultAsync(user => user.Email == identifier);

    public Task<User?> FindActiveByIdAsync(int userId) =>
        db.Users.SingleOrDefaultAsync(user => user.Id == userId && user.Status == "ACTIVE");

    public Task<User?> FindByIdAsync(int userId) =>
        db.Users.SingleOrDefaultAsync(user => user.Id == userId);

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

    public async Task<bool> CreateAsync(User user, int createdBy)
    {
        if (await db.Users.AnyAsync(existing => existing.Username == user.Username || existing.Email == user.Email))
            return false;

        await using var transaction = await db.Database.BeginTransactionAsync();
        db.Users.Add(user);
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            return false;
        }

        db.AuditLogs.Add(new AuditLog
        {
            UserId = createdBy, Action = "CREATE_USER", Module = "USER", ReferenceId = user.Id
        });
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return true;
    }

    public async Task<bool> UpdateAccountAsync(User user, int updatedBy, bool passwordChanged)
    {
        if (await db.Users.AnyAsync(existing => existing.Id != user.Id &&
            (existing.Username == user.Username || existing.Email == user.Email)))
            return false;

        await using var transaction = await db.Database.BeginTransactionAsync();
        user.UpdatedAt = DateTime.UtcNow;
        db.AuditLogs.Add(new AuditLog
        {
            UserId = updatedBy, Action = "UPDATE_USER", Module = "USER", ReferenceId = user.Id
        });
        if (passwordChanged)
            db.AuditLogs.Add(new AuditLog
            {
                UserId = updatedBy, Action = "RESET_PASSWORD", Module = "USER", ReferenceId = user.Id
            });
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            return false;
        }

        if (passwordChanged)
            await db.RefreshSessions
                .Where(session => session.UserId == user.Id && session.RevokedAt == null)
                .ExecuteUpdateAsync(setters => setters.SetProperty(session => session.RevokedAt, DateTime.UtcNow));
        await transaction.CommitAsync();
        return true;
    }

    public async Task<ActivateUserResult> ActivateAsync(int userId, int activatedBy)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();
        var changed = await db.Users
            .Where(user => user.Id == userId && user.Status == "INACTIVE")
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(user => user.Status, "ACTIVE")
                .SetProperty(user => user.UpdatedAt, DateTime.UtcNow));
        if (changed == 0)
            return await db.Users.AnyAsync(user => user.Id == userId)
                ? ActivateUserResult.AlreadyActive
                : ActivateUserResult.NotFound;

        db.AuditLogs.Add(new AuditLog
        {
            UserId = activatedBy, Action = "ACTIVATE_USER", Module = "USER", ReferenceId = userId
        });
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return ActivateUserResult.Activated;
    }

    public async Task ResetPasswordAsync(User user, int resetBy)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();
        user.UpdatedAt = DateTime.UtcNow;
        db.AuditLogs.Add(new AuditLog
        {
            UserId = resetBy, Action = "RESET_PASSWORD", Module = "USER", ReferenceId = user.Id
        });
        await db.SaveChangesAsync();
        await db.RefreshSessions
            .Where(session => session.UserId == user.Id && session.RevokedAt == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(session => session.RevokedAt, DateTime.UtcNow));
        await transaction.CommitAsync();
    }

    public async Task<DeactivateUserResult> DeactivateAsync(int userId, int deactivatedBy)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();
        var changed = await db.Users
            .Where(user => user.Id == userId && user.Status == "ACTIVE")
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(user => user.Status, "INACTIVE")
                .SetProperty(user => user.UpdatedAt, DateTime.UtcNow));
        if (changed == 0)
            return await db.Users.AnyAsync(user => user.Id == userId)
                ? DeactivateUserResult.AlreadyInactive
                : DeactivateUserResult.NotFound;

        await db.RefreshSessions
            .Where(session => session.UserId == userId && session.RevokedAt == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(session => session.RevokedAt, DateTime.UtcNow));
        db.AuditLogs.Add(new AuditLog
        {
            UserId = deactivatedBy, Action = "DEACTIVATE_USER", Module = "USER", ReferenceId = userId
        });
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return DeactivateUserResult.Deactivated;
    }
}
