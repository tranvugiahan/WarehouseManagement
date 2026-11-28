using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WarehouseManagement.Models;
using WarehouseManagement.Models.Requests;
using WarehouseManagement.Services;

namespace WarehouseManagement.Controllers.Api;

[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Route("api/products")]
public sealed class ProductsController(ProductService products) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(List<ProductResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActive()
    {
        var activeProducts = await products.GetActiveAsync();
        return Ok(activeProducts.Select(ToResponse));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var product = await products.GetByIdAsync(id);
        return product is null ? NotFound() : Ok(ToResponse(product));
    }

    [HttpPost]
    [Authorize(Roles = "ADMIN,MANAGER")]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest request)
    {
        var result = await products.CreateAsync(request);
        return result.Error switch
        {
            CreateProductError.DuplicateSkuOrBarcode => Conflict(new { error = "SKU hoặc barcode đã tồn tại." }),
            CreateProductError.CategoryUnavailable => BadRequest(new { error = "Nhóm hàng không tồn tại hoặc đã ngừng sử dụng." }),
            CreateProductError.UnitUnavailable => BadRequest(new { error = "Đơn vị tính không tồn tại hoặc đã ngừng sử dụng." }),
            _ => CreatedAtAction(nameof(GetById), new { id = result.Product!.Id }, ToResponse(result.Product))
        };
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "ADMIN,MANAGER")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProductRequest request)
    {
        var result = await products.UpdateAsync(id, request);
        return result switch
        {
            UpdateProductResult.NotFound => NotFound(),
            UpdateProductResult.Inactive => Conflict(new { error = "Chỉ được cập nhật sản phẩm đang hoạt động." }),
            UpdateProductResult.DuplicateSkuOrBarcode => Conflict(new { error = "SKU hoặc barcode đã tồn tại." }),
            UpdateProductResult.CategoryUnavailable => BadRequest(new { error = "Nhóm hàng không tồn tại hoặc đã ngừng sử dụng." }),
            UpdateProductResult.UnitUnavailable => BadRequest(new { error = "Đơn vị tính không tồn tại hoặc đã ngừng sử dụng." }),
            _ => NoContent()
        };
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "ADMIN,MANAGER")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(int id)
    {
        return await products.DeactivateAsync(id) ? NoContent() : NotFound();
    }

    private static ProductResponse ToResponse(Product product) =>
        new(product.Id, product.Sku, product.Barcode, product.Name, product.CategoryId,
            product.UnitId, product.CostPrice, product.ExportPrice, product.MinimumStock,
            product.HasExpiry, product.Status, product.CreatedAt);
}
