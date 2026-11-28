using WarehouseManagement.AppData.Repositories;
using WarehouseManagement.Models;
using WarehouseManagement.Models.Requests;

namespace WarehouseManagement.Services;

public enum CreateProductError
{
    None,
    DuplicateSkuOrBarcode,
    CategoryUnavailable,
    UnitUnavailable
}

public sealed record CreateProductResult(Product? Product, CreateProductError Error);

public enum UpdateProductResult
{
    Updated,
    NotFound,
    Inactive,
    DuplicateSkuOrBarcode,
    CategoryUnavailable,
    UnitUnavailable
}

public sealed class ProductService(ProductRepository products)
{
    public Task<List<Product>> GetActiveAsync() => products.GetActiveAsync();

    public Task<Product?> GetByIdAsync(int id) => products.GetByIdAsync(id);

    public Task<bool> DeactivateAsync(int id) => products.DeactivateAsync(id);

    public async Task<CreateProductResult> CreateAsync(CreateProductRequest request)
    {
        var sku = request.Sku.Trim();
        var barcode = string.IsNullOrWhiteSpace(request.Barcode) ? null : request.Barcode.Trim();

        if (await products.SkuOrBarcodeExistsAsync(sku, barcode))
            return new(null, CreateProductError.DuplicateSkuOrBarcode);

        if (!await products.CategoryIsActiveAsync(request.CategoryId!.Value))
            return new(null, CreateProductError.CategoryUnavailable);

        if (!await products.UnitIsActiveAsync(request.UnitId!.Value))
            return new(null, CreateProductError.UnitUnavailable);

        var product = new Product { Status = "ACTIVE" };
        ApplyRequest(product, request, sku, barcode);

        return await products.TryCreateAsync(product)
            ? new(product, CreateProductError.None)
            : new(null, CreateProductError.DuplicateSkuOrBarcode);
    }

    public async Task<UpdateProductResult> UpdateAsync(int id, UpdateProductRequest request)
    {
        var product = await products.GetForUpdateAsync(id);
        if (product is null)
            return UpdateProductResult.NotFound;
        if (product.Status != "ACTIVE")
            return UpdateProductResult.Inactive;

        var sku = request.Sku.Trim();
        var barcode = string.IsNullOrWhiteSpace(request.Barcode) ? null : request.Barcode.Trim();

        if (await products.SkuOrBarcodeExistsAsync(sku, barcode, id))
            return UpdateProductResult.DuplicateSkuOrBarcode;
        if (!await products.CategoryIsActiveAsync(request.CategoryId!.Value))
            return UpdateProductResult.CategoryUnavailable;
        if (!await products.UnitIsActiveAsync(request.UnitId!.Value))
            return UpdateProductResult.UnitUnavailable;

        ApplyRequest(product, request, sku, barcode);
        return await products.TrySaveChangesAsync()
            ? UpdateProductResult.Updated
            : UpdateProductResult.DuplicateSkuOrBarcode;
    }

    private static void ApplyRequest(Product product, CreateProductRequest request, string sku, string? barcode)
    {
        product.Sku = sku;
        product.Barcode = barcode;
        product.Name = request.Name.Trim();
        product.CategoryId = request.CategoryId!.Value;
        product.UnitId = request.UnitId!.Value;
        product.CostPrice = request.CostPrice!.Value;
        product.ExportPrice = request.ExportPrice!.Value;
        product.MinimumStock = request.MinimumStock!.Value;
        product.HasExpiry = request.HasExpiry!.Value;
    }
}
