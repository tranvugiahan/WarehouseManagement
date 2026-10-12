using Microsoft.AspNetCore.Mvc;

namespace WarehouseManagement.Controllers.Api;

// Reports application availability, not database connectivity.
[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { status = "ok" });
}
