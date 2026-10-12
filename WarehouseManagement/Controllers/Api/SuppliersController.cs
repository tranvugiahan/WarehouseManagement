using Microsoft.AspNetCore.Mvc;
using WarehouseManagement.Models;
using WarehouseManagement.Models.Requests;
using WarehouseManagement.Services;

namespace WarehouseManagement.Controllers.Api;

[ApiController]
[Route("api/suppliers")]
public class SuppliersController : ControllerBase
{
    private readonly CatalogService<Supplier> _service;

    public SuppliersController(CatalogService<Supplier> service)
    {
        _service = service;
    }

    // GET /api/suppliers
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        return Ok(await _service.GetAllAsync());
    }

    // GET /api/suppliers/1
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var supplier = await _service.GetByIdAsync(id);

        if (supplier == null)
            return NotFound();

        return Ok(supplier);
    }

    // POST /api/suppliers
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSupplierRequest request)
    {
        var created = await _service.CreateAsync(new Supplier { Name = request.Name, Phone = request.Phone, Email = request.Email, Address = request.Address, });

        return CreatedAtAction(
            nameof(GetById),
            new { id = created.Id },
            created);
    }

    // PUT /api/suppliers/1
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id, [FromBody] UpdateSupplierRequest request)
    {
        var success = await _service.UpdateAsync(id, new Supplier { Name = request.Name, Status = request.Status, Phone = request.Phone, Email = request.Email, Address = request.Address, });

        if (!success)
            return NotFound();

        return NoContent();
    }

    // DELETE /api/suppliers/1
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var success = await _service.DeactivateAsync(id);

        if (!success)
            return NotFound();

        return NoContent();
    }
}