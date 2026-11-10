using Microsoft.AspNetCore.Mvc;

namespace WarehouseManagement.Controllers;

public class CategoryController : Controller
{
    public IActionResult Index() => View();
}
