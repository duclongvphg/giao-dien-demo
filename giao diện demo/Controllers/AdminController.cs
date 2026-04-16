using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using giao_dien_demo.Data;
using giao_dien_demo.Models;
using giao_dien_demo.Hubs;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace giao_dien_demo.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<DashboardHub> _hubContext;

        // inject database context + signalr hub
        public AdminController(
            ApplicationDbContext context,
            IHubContext<DashboardHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        // ===== DASHBOARD =====
        public IActionResult Index()
        {
            ViewBag.Total = _context.Users.Count();
            ViewBag.Working = _context.Users.Count(x => x.IsOnline);
            ViewBag.Leave = _context.Users.Count(x => !x.IsOnline);

            return View();
        }

        // ===== XEM THÔNG TIN CÁ NHÂN =====
        [HttpGet]
        public IActionResult Details(int id)
        {
            var user = _context.Users.FirstOrDefault(x => x.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // ===== CẤP / THU HỒI QUYỀN =====
        [HttpGet]
        public async Task<IActionResult> GrantPermission(int id)
        {
            var user = _context.Users.FirstOrDefault(x => x.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            user.CanLogin = !user.CanLogin;
            _context.SaveChanges();

            await _hubContext.Clients.All.SendAsync("UserUpdated", new
            {
                id = user.Id,
                fullName = user.FullName,
                username = user.Username,
                role = user.Role,
                canLogin = user.CanLogin
            });

            return RedirectToAction("Index");
        }

        // ===== XÓA USER =====
        [HttpGet]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = _context.Users.FirstOrDefault(x => x.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            var defaultUsers = new List<string>
            {
                "linh.admin",
                "long.hr",
                "dat.leader",
                "linh.employee",
                "chang.employee"
            };

            if (defaultUsers.Contains(user.Username))
            {
                TempData["Error"] = "Không được xóa tài khoản mặc định";
                return RedirectToAction("Index");
            }

            var deletedUserId = user.Id;

            _context.Users.Remove(user);
            _context.SaveChanges();

            await _hubContext.Clients.All.SendAsync("UserDeleted", new
            {
                id = deletedUserId,
                totalEmployees = _context.Users.Count()
            });

            TempData["Success"] = "Đã xóa tài khoản thành công";
            return RedirectToAction("Index");
        }

        // ===== FORM SỬA USER =====
        [HttpGet]
        public IActionResult EditUser(int id)
        {
            var user = _context.Users.FirstOrDefault(x => x.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // ===== LƯU SỬA USER =====
        [HttpPost]
        public async Task<IActionResult> EditUser(User user)
        {
            var existing = _context.Users.FirstOrDefault(x => x.Id == user.Id);

            if (existing == null)
            {
                return NotFound();
            }

            existing.FullName = user.FullName;
            existing.Username = user.Username;
            existing.Role = user.Role;

            _context.SaveChanges();

            await _hubContext.Clients.All.SendAsync("UserUpdated", new
            {
                id = existing.Id,
                fullName = existing.FullName,
                username = existing.Username,
                role = existing.Role,
                canLogin = existing.CanLogin,
                totalEmployees = _context.Users.Count()
            });

            TempData["Success"] = "Cập nhật tài khoản thành công";
            return RedirectToAction("Index");
        }

        // ===== THÊM USER =====
        [HttpPost]
        public async Task<IActionResult> CreateUser(User user)
        {
            user.IsOnline = false;
            user.CanLogin = true;

            _context.Users.Add(user);
            _context.SaveChanges();

            await _hubContext.Clients.All.SendAsync("NewUserAdded", new
            {
                id = user.Id,
                fullName = user.FullName,
                username = user.Username,
                role = user.Role,
                totalEmployees = _context.Users.Count()
            });

            TempData["Success"] = "Thêm tài khoản thành công";
            return RedirectToAction("Index");
        }
    }
}