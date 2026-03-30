using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace giao_dien_demo.Controllers
{
    public class AccountController : Controller
    {
        // ===== GET: LOGIN =====
        public IActionResult Login()
        {
            return View();
        }

        // ===== POST: LOGIN =====
        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            // ❌ Không nhập
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ thông tin";
                return View();
            }

            // ❌ Sai tài khoản
            if (username != "longdz" || password != "123")
            {
                ViewBag.Error = "Sai tài khoản hoặc mật khẩu";
                return View();
            }

            // ✅ Lưu session
            HttpContext.Session.SetString("User", username);

            // ✅ Redirect đúng
            return RedirectToAction("Index", "Dashboard");
        }

        // ===== LOGOUT =====
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Account");
        }
    }
}