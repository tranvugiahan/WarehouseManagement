using Microsoft.AspNetCore.Mvc;

namespace WarehouseManagement.Controllers;

public class AuthController : Controller
{
    // GET: /Auth/Login - Màn hình đăng nhập UI-01
    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }
    
    [HttpPost]
    public IActionResult Login(string username, string password)
    {
        // Tạm thời cho đăng nhập thành công ngay để kiểm tra giao diện
        // Lệnh này sẽ chuyển hướng sang trang Home (Dashboard có _Layout):
        return RedirectToAction("Index", "Home");
    }


    // GET: /Auth/Logout - Đăng xuất và quay về trang Login
    [HttpGet]
    public IActionResult Logout()
    {
        // Sau này xoá session / cookie / JWT token tại đây
        return RedirectToAction("Login");
    }
}
