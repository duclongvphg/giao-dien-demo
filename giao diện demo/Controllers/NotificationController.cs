using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using giao_dien_demo.Hubs;
using giao_dien_demo.Models;
using giao_dien_demo.Data;
using Microsoft.EntityFrameworkCore;

namespace giao_dien_demo.Controllers
{
    // Model tạm thời cho thông báo (Vì bạn đang dùng list tĩnh)
    public class ThongBaoTam
    {
        public int Id { get; set; }
        public string NoiDung { get; set; } = string.Empty;
        public string NguoiGui { get; set; } = string.Empty;
        public string NguoiNhan { get; set; } = string.Empty;
        public DateTime NgayTao { get; set; } = DateTime.Now;
    }

    public class NotificationController : Controller
    {
        // Danh sách tĩnh lưu trữ lịch sử thông báo
        public static List<ThongBaoTam> list = new List<ThongBaoTam>();

        private readonly ApplicationDbContext _context;
        private readonly IHubContext<DashboardHub> _hubContext;

        public NotificationController(ApplicationDbContext context, IHubContext<DashboardHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        private bool CheckLogin()
        {
            return !string.IsNullOrEmpty(HttpContext.Session.GetString("User"));
        }

        // --- 1. HIỂN THỊ DANH SÁCH THÔNG BÁO ---
        public async Task<IActionResult> Index(string? returnTo)
        {
            if (!CheckLogin()) return RedirectToAction("Login", "Account");

            var user = HttpContext.Session.GetString("User") ?? "";
            var role = HttpContext.Session.GetString("Role");
            var realName = HttpContext.Session.GetString("RealName") ?? "";

            // Lấy danh sách nhân viên "Chính thức" từ Database để HR chọn người nhận
            ViewBag.Employees = await _context.Employees
                                    .Where(e => e.Position == "Chính thức")
                                    .OrderBy(e => e.Name)
                                    .ToListAsync();

            ViewBag.ReturnTo = returnTo;

            List<ThongBaoTam> hienThi;

            // 🔥 LOGIC LỌC THÔNG MINH:
            if (role == "HR" || role == "Admin" || role == "Leader")
            {
                // Quản lý: Thấy toàn bộ thông báo đã gửi
                hienThi = list.OrderByDescending(x => x.NgayTao).ToList();
            }
            else
            {
                // Nhân viên: Thấy tin "Tất cả" HOẶC tin gửi đúng "Tên đăng nhập" HOẶC "Họ tên thật"
                hienThi = list.Where(x =>
                    x.NguoiNhan == "Tất cả" ||
                    x.NguoiNhan == "Tất cả nhân viên" ||
                    x.NguoiNhan == user ||
                    (!string.IsNullOrEmpty(realName) && x.NguoiNhan.Trim().ToLower() == realName.Trim().ToLower())
                ).OrderByDescending(x => x.NgayTao).ToList();
            }

            return View(hienThi);
        }

        // --- 2. GỬI THÔNG BÁO (TỪ HR/ADMIN) ---
        [HttpPost]
        public async Task<IActionResult> GuiThongBao(string noiDung, string nguoiNhan, string? returnTo)
        {
            if (!CheckLogin()) return RedirectToAction("Login", "Account");

            // Lấy tên người gửi (Ưu tiên tên thật)
            var userGui = HttpContext.Session.GetString("RealName") ?? HttpContext.Session.GetString("User") ?? "Hệ thống";
            var roleGui = HttpContext.Session.GetString("Role");

            if (roleGui != "HR" && roleGui != "Admin" && roleGui != "Leader")
            {
                return Json(new { success = false, message = "Bạn không có quyền gửi!" });
            }

            if (!string.IsNullOrWhiteSpace(noiDung))
            {
                var tbMoi = new ThongBaoTam
                {
                    Id = list.Any() ? list.Max(x => x.Id) + 1 : 1,
                    NoiDung = noiDung,
                    NguoiGui = userGui,
                    NguoiNhan = string.IsNullOrEmpty(nguoiNhan) ? "Tất cả nhân viên" : nguoiNhan,
                    NgayTao = DateTime.Now
                };
                list.Add(tbMoi);

                // 🔥 SIGNALR REAL-TIME: Bắn sóng cho toàn bộ hệ thống
                await _hubContext.Clients.All.SendAsync("ReceiveNotification", new
                {
                    content = tbMoi.NoiDung,
                    sender = tbMoi.NguoiGui,
                    receiver = tbMoi.NguoiNhan,
                    time = tbMoi.NgayTao.ToString("HH:mm")
                });

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return Ok();
            }

            return RedirectToAction("Index", new { returnTo = returnTo });
        }

        // --- 3. THU HỒI THÔNG BÁO ---
        [HttpPost]
        public async Task<IActionResult> RecallByName(string noiDung)
        {
            var role = HttpContext.Session.GetString("Role");
            if (role != "HR" && role != "Admin" && role != "Leader") return Forbid();

            var item = list.FirstOrDefault(x => x.NoiDung == noiDung);
            if (item != null)
            {
                list.Remove(item);
                await _hubContext.Clients.All.SendAsync("NotificationRecalled", noiDung);
                return Ok();
            }
            return NotFound();
        }

        // --- 4. XÓA THÔNG BÁO ---
        [HttpPost]
        public IActionResult Delete(int id)
        {
            var role = HttpContext.Session.GetString("Role");
            if (role != "HR" && role != "Admin" && role != "Leader") return Forbid();

            var item = list.FirstOrDefault(x => x.Id == id);
            if (item != null)
            {
                list.Remove(item);
                return Ok();
            }
            return NotFound();
        }

        // --- 5. LẤY SỐ LƯỢNG CHO CHUÔNG THÔNG BÁO (BADGE) ---
        public JsonResult GetNotifications()
        {
            var user = HttpContext.Session.GetString("User") ?? "";
            var role = HttpContext.Session.GetString("Role");
            var realName = HttpContext.Session.GetString("RealName") ?? "";

            int count;
            if (role == "HR" || role == "Admin" || role == "Leader")
            {
                count = list.Count;
            }
            else
            {
                count = list.Count(x =>
                    x.NguoiNhan == "Tất cả" ||
                    x.NguoiNhan == "Tất cả nhân viên" ||
                    x.NguoiNhan == user ||
                    (!string.IsNullOrEmpty(realName) && x.NguoiNhan.Trim().ToLower() == realName.Trim().ToLower())
                );
            }

            return Json(new { count = count });
        }
    }
}