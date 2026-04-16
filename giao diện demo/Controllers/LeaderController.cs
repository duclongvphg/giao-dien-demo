using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Linq;
using giao_dien_demo.Models;
using System.Collections.Generic;
using System;

namespace giao_dien_demo.Controllers
{
    public class LeaderController : Controller
    {
        public IActionResult Index()
        {
            // 1. Kiểm tra quyền đăng nhập
            var role = HttpContext.Session.GetString("Role");
            if (role != "Leader")
            {
                return RedirectToAction("Login", "Account");
            }

            // ==========================================
            // PHẦN A: LẤY DỮ LIỆU TỰ ĐỘNG TỪ HỆ THỐNG (Chấm công, Duyệt hồ sơ...)
            // ==========================================
            int autoEmp = EmployeeController.list?.Count ?? 0;
            int autoAtt = AttendanceController.list?.Count ?? 0;
            int autoSal = SalaryController.BangLuongDaChot?.Count ?? 0;

            // ==========================================
            // PHẦN B: LẤY DỮ LIỆU TỪ FILE HR UPLOAD (PDF, Word)
            // ==========================================
            var fileList = ReportController.reportFileList ?? new List<ReportFile>();

            // 🔥 CẬP NHẬT THUẬT TOÁN ĐẾM THÔNG MINH (Tránh lỗi do dấu tiếng Việt)
            int fileSal = fileList.Count(f =>
                (!string.IsNullOrEmpty(f.Category) && f.Category.Contains("lương")) ||
                f.Title.ToLower().Contains("lương") || f.Title.ToLower().Contains("luong")
            );

            int fileAtt = fileList.Count(f =>
                (!string.IsNullOrEmpty(f.Category) && (f.Category.Contains("công") || f.Category.Contains("chấm"))) ||
                f.Title.ToLower().Contains("công") || f.Title.ToLower().Contains("cong") || f.Title.ToLower().Contains("chấm")
            );

            // Những file không thuộc 2 loại trên sẽ được tính vào báo cáo Nhân sự
            int fileEmp = fileList.Count - (fileSal + fileAtt);

            // ==========================================
            // PHẦN C: TỔNG HỢP & XUẤT RA VIEW
            // ==========================================
            // 🔥 Tổng cuối = Tự động + File upload
            ViewBag.EmpCount = autoEmp + fileEmp;
            ViewBag.AttCount = autoAtt + fileAtt;
            ViewBag.SalCount = autoSal + fileSal;

            // Lấy dữ liệu cho Biểu đồ tròn (Trạng thái báo cáo)
            var reports = ReportController.list ?? new List<Report>();
            int autoDone = reports.Count(x => x.TaskStatus == "Hoàn thành" || x.TaskStatus == "Hoàn tất");
            int autoPending = reports.Count(x => x.TaskStatus != "Hoàn thành" && x.TaskStatus != "Hoàn tất");

            // Mặc định các File HR up lên được tính là "Đã hoàn tất"
            ViewBag.DoneCount = autoDone + fileList.Count;
            ViewBag.PendingCount = autoPending;

            // Gửi danh sách File để hiển thị bảng ở trang chủ của Sếp
            ViewBag.ReportFiles = fileList.OrderByDescending(f => f.UploadedDate).ToList();

            return View();
        }
    }
}