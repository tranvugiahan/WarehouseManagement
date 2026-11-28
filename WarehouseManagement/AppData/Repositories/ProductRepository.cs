using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Models;

namespace WarehouseManagement.AppData.Repositories;

public sealed class ProductRepository(WarehouseDbContext context)
{
    public Task<List<Product>> GetActiveAsync() =>
        context.Products.AsNoTracking()
            .Where(product => product.Status == "ACTIVE")
            .OrderBy(product => product.Id)
            .ToListAsync();

    public Task<Product?> GetByIdAsync(int id) =>
        context.Products.AsNoTracking().SingleOrDefaultAsync(product => product.Id == id);

    public Task<Product?> GetForUpdateAsync(int id) =>
        context.Products.SingleOrDefaultAsync(product => product.Id == id);

    public Task<bool> SkuOrBarcodeExistsAsync(string sku, string? barcode, int? exceptId = null) =>
        context.Products.AnyAsync(product =>
            (exceptId == null || product.Id != exceptId) &&
            (product.Sku == sku || (barcode != null && product.Barcode == barcode)));

    public Task<bool> CategoryIsActiveAsync(int categoryId) =>
        context.Categories.AnyAsync(category => category.Id == categoryId && category.Status == "ACTIVE");

    public Task<bool> UnitIsActiveAsync(int unitId) =>
        context.Units.AnyAsync(unit => unit.Id == unitId && unit.Status == "ACTIVE");

    public async Task<bool> DeactivateAsync(int id)
    {
        var updated = await context.Products
            .Where(product => product.Id == id && product.Status == "ACTIVE")
            .ExecuteUpdateAsync(setters => setters.SetProperty(product => product.Status, "INACTIVE"));

        return updated > 0 || await context.Products.AnyAsync(product => product.Id == id);
    }

    public async Task<bool> TryCreateAsync(Product product)
    {
        context.Products.Add(product);
        return await TrySaveChangesAsync();
    }

    public async Task<bool> TrySaveChangesAsync()
    {
        try
        {
            await context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            return false;
        }
    }
}
