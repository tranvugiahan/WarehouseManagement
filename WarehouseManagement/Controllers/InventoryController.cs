using Microsoft.AspNetCore.Mvc;

namespace WarehouseManagement.Controllers;

public class InventoryController : Controller
{
    public IActionResult Index() => View();
}
