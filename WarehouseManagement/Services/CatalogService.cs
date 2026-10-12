using WarehouseManagement.AppData.Repositories;
using WarehouseManagement.Models;

namespace WarehouseManagement.Services;

public class CatalogService<T> where T : class, IEntity
{
    private readonly CatalogRepository<T> _repository;

    public CatalogService(CatalogRepository<T> repository)
    {
        _repository = repository;
    }

    public Task<List<T>> GetAllAsync()
    {
        return _repository.GetAllAsync();
    }

    public Task<T?> GetByIdAsync(int id)
    {
        return _repository.GetByIdAsync(id);
    }

    public Task<T> CreateAsync(T entity)
    {
        entity.Id = 0;


        entity.Status = "ACTIVE";

        return _repository.AddAsync(entity);
    }

    public async Task<bool> UpdateAsync(int id, T updated)
    {
        var current = await _repository.GetByIdAsync(id);

        if (current == null)
            return false;

        updated.Id = id;

        await _repository.UpdateAsync(current, updated);

        return true;
    }

    public async Task<bool> DeactivateAsync(int id)
    {
        var current = await _repository.GetByIdAsync(id);

        if (current == null)
            return false;

        current.Status = "INACTIVE";

        await _repository.SaveChangesAsync();

        return true;
    }
}