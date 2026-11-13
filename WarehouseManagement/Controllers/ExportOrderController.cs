using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WarehouseManagement.Controllers;

[Authorize(AuthenticationSchemes = "MvcCookie")]
public class ExportOrderController : Controller
{
    public IActionResult Index() => View();
    public IActionResult Damaged() => View();
}
