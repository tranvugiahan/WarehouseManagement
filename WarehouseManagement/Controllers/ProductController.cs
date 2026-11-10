using Microsoft.AspNetCore.Mvc;

namespace WarehouseManagement.Controllers;

public class ProductController : Controller
{
    public IActionResult Index() => View();
}
