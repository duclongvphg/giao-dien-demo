using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using giao_dien_demo.Data;
using giao_dien_demo.Models;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace giao_dien_demo.Controllers
{
    public class HRController : Controller
    {
        private readonly ApplicationDbContext _context;

        // Tiêm Database Context vào để lấy dữ liệu thực tế từ SQL
        public HRController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // ==============================================================================
            // 1. ĐẾM CÁC THÔNG SỐ TỔNG QUAN (LẤY TỪ SQL ĐỂ KHÔNG BỊ HIỆN SỐ 0)
            // ==============================================================================

            // Đếm số nhân viên đã có "hộ khẩu" chính thức trong SQL
            ViewBag.TotalEmployees = await _context.Employees.CountAsync(e => e.Position == "Chính thức");

            // Đếm tổng số hợp đồng hiện có trong SQL
            ViewBag.ContractCount = await _context.Contracts.CountAsync();

            // Các thông số còn lại tạm thời lấy từ List tĩnh (Chấm công, Lương, Báo cáo)
            ViewBag.AttendanceCount = AttendanceController.list.Count(x => x.CheckIn.Date == DateTime.Today);
            ViewBag.SalaryCount = AttendanceController.list.Count(x => x.CheckOut != null && x.CheckIn.Date == DateTime.Today);
            ViewBag.LeaveCount = LeaveController.list.Count;
            ViewBag.ReportCount = ReportController.list.Count;

            // ==============================================================================
            // 2. LẤY DANH SÁCH THÔNG BÁO CHỜ DUYỆT (HIỂN THỊ CHUÔNG THÔNG BÁO)
            // ==============================================================================

            // Lấy đơn nghỉ phép đang chờ
            var pendingLeaves = LeaveController.list
                .Where(x => x.Status == "Chờ duyệt")
                .Select(x => new {
                    Id = x.Id,
                    Content = $"Nhân viên [{x.EmployeeName}] vừa gửi 1 đơn xin nghỉ phép mới!",
                    Type = "Nghỉ phép",
                    Time = DateTime.Now
                }).ToList();

            // Lấy hợp đồng chờ duyệt (Lấy từ SQL để đảm bảo Linh hiện ra)
            var contractsFromDb = await _context.Contracts
                                    .Where(x => x.Status.Contains("Chờ duyệt"))
                                    .ToListAsync();

            var pendingContracts = contractsFromDb.Select(x => new {
                Id = x.Id,
                Content = $"Nhân viên [{x.EmployeeName}] vừa hoàn tất ký tên, chờ bạn duyệt cuối!",
                Type = "Hợp đồng",
                Time = x.CreatedAt
            }).ToList();

            // Gộp và sắp xếp thông báo theo thời gian mới nhất
            var allNotifications = pendingLeaves.Cast<dynamic>()
                                    .Concat(pendingContracts.Cast<dynamic>())
                                    .OrderByDescending(x => (DateTime)x.Time)
                                    .ToList();

            ViewBag.PendingNotifications = allNotifications;
            ViewBag.NotificationCount = allNotifications.Count;

            return View();
        }
    }
}