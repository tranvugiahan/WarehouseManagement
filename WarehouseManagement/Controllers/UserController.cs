using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WarehouseManagement.Controllers;

[Authorize(AuthenticationSchemes = "MvcCookie", Roles = "ADMIN")]
public class UserController : Controller
{
    public IActionResult Index() => View();
}
