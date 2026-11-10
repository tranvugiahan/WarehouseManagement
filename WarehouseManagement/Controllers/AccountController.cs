using Microsoft.AspNetCore.Mvc;

namespace WarehouseManagement.Controllers;

public class AccountController : Controller
{
    public IActionResult Profile() => View();
}
