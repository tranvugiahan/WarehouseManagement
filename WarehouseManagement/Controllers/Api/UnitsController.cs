using Microsoft.AspNetCore.Mvc;
using WarehouseManagement.Models;
using WarehouseManagement.Models.Requests;
using WarehouseManagement.Services;

namespace WarehouseManagement.Controllers.Api;

[ApiController]
[Route("api/units")]
public class UnitsController : ControllerBase
{
    private readonly CatalogService<Unit> _service;

    public UnitsController(CatalogService<Unit> service)
    {
        _service = service;
    }

    // GET /api/units
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        return Ok(await _service.GetAllAsync());
    }

    // GET /api/units/1
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var unit = await _service.GetByIdAsync(id);

        if (unit == null)
            return NotFound();

        return Ok(unit);
    }

    // POST /api/units
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUnitRequest request)
    {
        var created = await _service.CreateAsync(new Unit { Name = request.Name,  });

        return CreatedAtAction(
            nameof(GetById),
            new { id = created.Id },
            created);
    }

    // PUT /api/units/1
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id, [FromBody] UpdateUnitRequest request)
    {
        var success = await _service.UpdateAsync(id, new Unit { Name = request.Name, Status = request.Status,  });

        if (!success)
            return NotFound();

        return NoContent();
    }

    // DELETE /api/units/1
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var success = await _service.DeactivateAsync(id);

        if (!success)
            return NotFound();

        return NoContent();
    }
}