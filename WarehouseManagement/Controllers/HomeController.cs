using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WarehouseManagement.Controllers;

[Authorize(AuthenticationSchemes = "MvcCookie")]
public class HomeController : Controller
{
    public IActionResult Index()
    {
        Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
        return View();
    }
}
