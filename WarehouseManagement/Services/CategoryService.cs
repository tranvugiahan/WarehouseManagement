using WarehouseManagement.AppData.Repositories;
using WarehouseManagement.Models;

namespace WarehouseManagement.Services;

public enum CategoryWriteResult
{
    Success,
    NotFound,
    NameConflict
}

public class CategoryService
{
    private readonly CategoryRepository _repository;

    public CategoryService(CategoryRepository repository)
    {
        _repository = repository;
    }

    public Task<List<Category>> GetAllAsync() => _repository.GetAllAsync();

    public Task<Category?> GetByIdAsync(int id) => _repository.GetByIdAsync(id);

    public async Task<Category?> CreateAsync(string name)
    {
        name = name.Trim();
        if (await _repository.NameExistsAsync(name))
            return null;

        var category = new Category { Name = name, Status = "ACTIVE" };
        return await _repository.TryCreateAsync(category) ? category : null;
    }

    public async Task<CategoryWriteResult> UpdateAsync(int id, string name, string status)
    {
        var category = await _repository.GetForUpdateAsync(id);
        if (category is null)
            return CategoryWriteResult.NotFound;

        name = name.Trim();
        if (await _repository.NameExistsAsync(name, id))
            return CategoryWriteResult.NameConflict;

        category.Name = name;
        category.Status = status;
        return await _repository.TrySaveChangesAsync()
            ? CategoryWriteResult.Success
            : CategoryWriteResult.NameConflict;
    }

    public async Task<bool> DeactivateAsync(int id)
    {
        var category = await _repository.GetForUpdateAsync(id);
        if (category is null)
            return false;

        category.Status = "INACTIVE";
        await _repository.SaveChangesAsync();
        return true;
    }
}
