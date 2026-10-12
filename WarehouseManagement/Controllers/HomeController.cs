using Microsoft.AspNetCore.Mvc;

namespace WarehouseManagement.Controllers;

public class HomeController : Controller
{
    public IActionResult Index() => View();
}
