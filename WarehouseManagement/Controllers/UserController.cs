using Microsoft.AspNetCore.Mvc;

namespace WarehouseManagement.Controllers;

public class UserController : Controller
{
    public IActionResult Index() => View();
}
