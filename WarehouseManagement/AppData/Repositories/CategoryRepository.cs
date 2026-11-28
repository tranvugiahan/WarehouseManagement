using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Models;

namespace WarehouseManagement.AppData.Repositories;

public class CategoryRepository
{
    private readonly WarehouseDbContext _context;

    public CategoryRepository(WarehouseDbContext context)
    {
        _context = context;
    }

    public Task<List<Category>> GetAllAsync() =>
        _context.Categories.AsNoTracking().OrderBy(category => category.Id).ToListAsync();

    public Task<Category?> GetByIdAsync(int id) =>
        _context.Categories.AsNoTracking().SingleOrDefaultAsync(category => category.Id == id);

    public Task<Category?> GetForUpdateAsync(int id) =>
        _context.Categories.SingleOrDefaultAsync(category => category.Id == id);

    public Task<bool> NameExistsAsync(string name, int? exceptId = null) =>
        _context.Categories.AnyAsync(category => category.Name == name &&
            (exceptId == null || category.Id != exceptId));

    public async Task<bool> TryCreateAsync(Category category)
    {
        _context.Categories.Add(category);
        return await TrySaveChangesAsync();
    }

    public async Task<bool> TrySaveChangesAsync()
    {
        try
        {
            await _context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            return false;
        }
    }

    public Task SaveChangesAsync() => _context.SaveChangesAsync();
}
