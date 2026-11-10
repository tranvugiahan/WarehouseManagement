using Microsoft.AspNetCore.Mvc;

namespace WarehouseManagement.Controllers;

public class StockTakeController : Controller
{
    public IActionResult Index() => View();
}
