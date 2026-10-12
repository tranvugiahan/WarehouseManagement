using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Models;

namespace WarehouseManagement.AppData.Repositories;

public class CatalogRepository<T> where T : class, IEntity
{
    private readonly WarehouseDbContext _context;
    private readonly DbSet<T> _table;

    public CatalogRepository(WarehouseDbContext context)
    {
        _context = context;
        _table = context.Set<T>();
    }

    public async Task<List<T>> GetAllAsync()
    {
        return await _table
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .ToListAsync();
    }

    public async Task<T?> GetByIdAsync(int id)
    {
        return await _table.FindAsync(id);
    }

    public async Task<T> AddAsync(T entity)
    {
        await _table.AddAsync(entity);
        await _context.SaveChangesAsync();

        return entity;
    }

    public async Task UpdateAsync(T current, T updated)
    {
        // Chép các thuộc tính mới vào bản ghi đang được EF theo dõi.
        _context.Entry(current).CurrentValues.SetValues(updated);

        await _context.SaveChangesAsync();
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
