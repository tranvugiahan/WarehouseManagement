using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WarehouseManagement.Controllers;

[Authorize(AuthenticationSchemes = "MvcCookie")]
public class HomeController : Controller
{
    public IActionResult Index() => View();
}
