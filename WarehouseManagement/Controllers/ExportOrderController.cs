using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WarehouseManagement.Controllers;

[Authorize(AuthenticationSchemes = "MvcCookie")]
public class ExportOrderController : Controller
{
    public IActionResult Index() => View();
    [Authorize(Roles = "ADMIN,MANAGER")]
    public IActionResult Damaged() => View();
}
