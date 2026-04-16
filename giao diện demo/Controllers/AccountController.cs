using Microsoft.AspNetCore.SignalR;
using giao_dien_demo.Hubs;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using giao_dien_demo.Data;
using giao_dien_demo.Models;
using System.Linq;
using System;
using System.Net;
using System.Net.Mail;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace giao_dien_demo.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<DashboardHub> _hubContext;

        public AccountController(
            ApplicationDbContext context,
            IHubContext<DashboardHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        // ==========================================
        // 🔑 ĐĂNG NHẬP (LOGIN)
        // ==========================================
        [HttpGet]
        public IActionResult Login()
        {
            ViewBag.SavedUsername = Request.Cookies["HRMS_SavedUser"];
            return View();
        }

        [HttpPost]
        public IActionResult Login(string username, string password, string rememberMe)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "⚠️ Vui lòng nhập đầy đủ thông tin";
                return View();
            }

            var user = _context.Users.FirstOrDefault(x => x.Username == username && x.Password == password);

            if (user == null)
            {
                ViewBag.Error = "❌ Sai tài khoản hoặc mật khẩu";
                return View();
            }

            if (!user.CanLogin && user.Role != "Admin")
            {
                ViewBag.Error = "🔒 Tài khoản đang chờ Admin duyệt quyền truy cập";
                return View();
            }

            if (rememberMe == "true")
            {
                CookieOptions options = new CookieOptions { Expires = DateTime.Now.AddDays(30) };
                Response.Cookies.Append("HRMS_SavedUser", username, options);
            }
            else
            {
                Response.Cookies.Delete("HRMS_SavedUser");
            }

            user.IsOnline = true;
            user.LastActive = DateTime.Now;
            _context.SaveChanges();

            HttpContext.Session.SetString("User", user.Username);
            HttpContext.Session.SetString("Role", user.Role);

            string displayName = string.IsNullOrEmpty(user.FullName) ? user.Username : user.FullName;
            HttpContext.Session.SetString("RealName", displayName);

            return user.Role switch
            {
                "Admin" => RedirectToAction("Index", "Admin"),
                "HR" => RedirectToAction("Index", "HR"),
                "Employee" => RedirectToAction("Index", "NhanVien"),
                "Leader" => RedirectToAction("Index", "Leader"),
                _ => RedirectToAction("Login"),
            };
        }

        public IActionResult Logout()
        {
            var username = HttpContext.Session.GetString("User");
            if (!string.IsNullOrEmpty(username))
            {
                var user = _context.Users.FirstOrDefault(x => x.Username == username);
                if (user != null)
                {
                    user.IsOnline = false;
                    user.LastActive = DateTime.Now;
                    _context.SaveChanges();
                }
            }
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Account");
        }

        // ==========================================
        // 📝 TẠO TÀI KHOẢN (REGISTER) - ĐÃ CẬP NHẬT ĐIỀU HƯỚNG
        // ==========================================
        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        public async Task<IActionResult> Register(User user)
        {
            if (string.IsNullOrEmpty(user.Username) || string.IsNullOrEmpty(user.Password) || string.IsNullOrEmpty(user.Email))
            {
                ViewBag.Error = "⚠️ Vui lòng điền đầy đủ các thông tin bắt buộc!";
                return View(user);
            }

            var checkUser = await _context.Users.FirstOrDefaultAsync(x => x.Username == user.Username);
            if (checkUser != null) { ViewBag.Error = "❌ Tên đăng nhập đã tồn tại"; return View(user); }

            try
            {
                user.Role = "Employee";
                user.IsOnline = false;
                user.LastActive = DateTime.Now;
                user.CanLogin = false; // Admin vẫn phải duyệt thì mới đăng nhập được ở lần sau

                _context.Users.Add(user);

                var newEmp = new Employee
                {
                    Name = user.FullName ?? user.Username,
                    Department = "Chưa xếp phòng",
                    Position = "Thử việc",
                    JobTitle = "Thực tập sinh (Intern)",
                    Salary = 6000000
                };
                _context.Employees.Add(newEmp);

                await _context.SaveChangesAsync();

                await _hubContext.Clients.All.SendAsync("NewUserAdded", new
                {
                    id = user.Id,
                    fullName = user.FullName,
                    username = user.Username,
                    role = user.Role
                });

                // 🔥 ĐÃ ĐỔI CHỮ: Từ "Đăng ký" sang "Tạo tài khoản"
                TempData["SuccessMessage"] = "🎉 Tạo tài khoản thành công! Vui lòng chờ Admin cấp quyền.";

                // 🔥 ĐIỀU HƯỚNG: Quay về trang chủ Admin thay vì trang Login
                return RedirectToAction("Index", "Admin");
            }
            catch (Exception ex)
            {
                ViewBag.Error = "❌ Lỗi hệ thống: " + ex.Message;
                return View(user);
            }
        }

        // ==========================================
        // 📩 QUÊN MẬT KHẨU & KHÔI PHỤC (GMAIL)
        // ==========================================
        [HttpGet] public IActionResult ForgotPassword() => View();

        [HttpPost]
        public IActionResult ForgotPassword(string username, string email)
        {
            var user = _context.Users.FirstOrDefault(x => x.Username == username && x.Email == email);

            if (user == null)
            {
                ViewBag.Error = "❌ Thông tin Tên đăng nhập hoặc Email không chính xác!";
                return View();
            }

            string recoveryCode = new Random().Next(100000, 999999).ToString();
            HttpContext.Session.SetString("RecoveryCode", recoveryCode);
            HttpContext.Session.SetString("RecoveryUser", user.Username);

            try
            {
                var fromAddress = new MailAddress("duclongvphg@gmail.com", "HRMS Support System");
                const string fromPassword = "tucyevhczodephdr";

                var smtp = new SmtpClient
                {
                    Host = "smtp.gmail.com",
                    Port = 587,
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
                };

                using var message = new MailMessage(fromAddress, new MailAddress(email))
                {
                    Subject = "Mã xác thực khôi phục mật khẩu - HRMS",
                    Body = $"Chào {user.FullName},\n\nBạn đang thực hiện khôi phục mật khẩu cho tài khoản: {user.Username}.\nMã xác nhận của bạn là: {recoveryCode}\n\nMã có hiệu lực trong phiên làm việc này."
                };
                smtp.Send(message);

                return RedirectToAction("VerifyCode");
            }
            catch (Exception ex) { ViewBag.Error = "⚠️ Lỗi gửi mail: " + ex.Message; return View(); }
        }

        [HttpGet] public IActionResult VerifyCode() => View();

        [HttpPost]
        public IActionResult VerifyCode(string code)
        {
            var sessionCode = HttpContext.Session.GetString("RecoveryCode");
            if (!string.IsNullOrEmpty(code) && code.Trim() == sessionCode) return RedirectToAction("ResetPassword");
            ViewBag.Error = "❌ Mã xác nhận không chính xác!";
            return View();
        }

        [HttpGet]
        public IActionResult ResetPassword()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("RecoveryUser"))) return RedirectToAction("ForgotPassword");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(string newPassword, string confirmPassword)
        {
            if (string.IsNullOrEmpty(newPassword) || newPassword != confirmPassword)
            {
                ViewBag.Error = "❌ Mật khẩu không khớp!";
                return View();
            }

            var username = HttpContext.Session.GetString("RecoveryUser");
            var user = _context.Users.FirstOrDefault(x => x.Username == username);

            if (user != null)
            {
                user.Password = newPassword;
                _context.SaveChanges();
                HttpContext.Session.Remove("RecoveryCode");
                HttpContext.Session.Remove("RecoveryUser");
                TempData["SuccessMessage"] = "✅ Mật khẩu đã được cập nhật!";
                return RedirectToAction("Login");
            }
            return RedirectToAction("Login");
        }
    }
}