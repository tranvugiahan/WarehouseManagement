using Microsoft.AspNetCore.Mvc;

namespace WarehouseManagement.Controllers;

public class ExportOrderController : Controller
{
    public IActionResult Index() => View();
    public IActionResult Damaged() => View();
}
