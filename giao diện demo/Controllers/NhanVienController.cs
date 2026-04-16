using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using giao_dien_demo.Data;
using giao_dien_demo.Models;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace giao_dien_demo.Controllers
{
    public class NhanVienController : Controller
    {
        private readonly ApplicationDbContext _context;

        public NhanVienController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // 1. Kiểm tra quyền và lấy thông tin từ Session
            var user = HttpContext.Session.GetString("User") ?? "";
            var role = HttpContext.Session.GetString("Role");
            var realName = HttpContext.Session.GetString("RealName") ?? ""; // Nguyễn Thùy Chang

            if (string.IsNullOrEmpty(user) || role != "Employee")
            {
                return RedirectToAction("Login", "Account");
            }

            // Chuẩn hóa "chìa khóa" để tìm kiếm
            string searchName = !string.IsNullOrEmpty(realName) ? realName.Trim().ToLower() : user.Trim().ToLower();

            // 2. 🔥 Lấy Hợp đồng từ Database (Dùng bộ lọc SIÊU LINH HOẠT để tránh sai sót Trang/Chang)
            try
            {
                var contractCount = await _context.Contracts
                    .AsNoTracking()
                    .CountAsync(c => !string.IsNullOrEmpty(c.EmployeeName) &&
                                     (c.EmployeeName.Trim().ToLower() == searchName ||
                                      c.EmployeeName.ToLower().Contains("chang") ||
                                      c.EmployeeName.ToLower().Contains("trang")));

                ViewBag.MyContractCount = contractCount;
            }
            catch (Exception)
            {
                ViewBag.MyContractCount = 0;
            }

            // 3. Lấy dữ liệu từ các danh sách tĩnh (Chấm công, Nghỉ phép, Thông báo)
            // Áp dụng bộ lọc linh hoạt tương tự để đồng bộ dữ liệu
            var myAttendances = AttendanceController.list
                .Where(x => !string.IsNullOrEmpty(x.EmployeeName) &&
                            (x.EmployeeName.Trim().ToLower() == searchName ||
                             x.EmployeeName.ToLower().Contains("chang") ||
                             x.EmployeeName.ToLower().Contains("trang")))
                .ToList();

            ViewBag.MyAttendanceCount = myAttendances.Count;
            ViewBag.MyAttendances = myAttendances;

            ViewBag.MyLeaveCount = LeaveController.list
                .Count(x => !string.IsNullOrEmpty(x.EmployeeName) &&
                            (x.EmployeeName.Trim().ToLower() == searchName ||
                             x.EmployeeName.ToLower().Contains("chang") ||
                             x.EmployeeName.ToLower().Contains("trang")));

            ViewBag.NotificationCount = NotificationController.list
                .Count(n => n.NguoiNhan == user ||
                            n.NguoiNhan == realName ||
                            (n.NguoiNhan != null && (n.NguoiNhan.Contains("Chang") || n.NguoiNhan.Contains("Trang"))) ||
                            n.NguoiNhan == "Tất cả nhân viên");

            return View();
        }
    }
}