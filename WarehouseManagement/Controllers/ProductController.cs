using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WarehouseManagement.Controllers;

[Authorize(AuthenticationSchemes = "MvcCookie", Roles = "ADMIN,MANAGER")]
public class ProductController : Controller
{
    public IActionResult Index() => View();
}
