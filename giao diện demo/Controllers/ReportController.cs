using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using giao_dien_demo.Hubs;
using giao_dien_demo.Models;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System;

namespace giao_dien_demo.Controllers
{
    public class ReportController : Controller
    {
        // 🟢 1. Danh sách báo cáo nhân sự dạng bảng (Tính năng CŨ - GIỮ NGUYÊN)
        public static List<Report> list = new List<Report>();

        // 🟢 2. Danh sách lưu thông tin File Upload (Tính năng MỚI)
        public static List<ReportFile> reportFileList = new List<ReportFile>();

        // Khai báo môi trường và Hub để xử lý File & SignalR
        private readonly IWebHostEnvironment _env;
        private readonly IHubContext<DashboardHub> _hubContext;

        // Constructor đã được tiêm thêm Dependency
        public ReportController(IWebHostEnvironment env, IHubContext<DashboardHub> hubContext)
        {
            _env = env;
            _hubContext = hubContext;
        }

        private bool IsHR() => HttpContext.Session.GetString("Role") == "HR";

        // ===== TRANG CHỦ BÁO CÁO (GIỮ NGUYÊN) =====
        public IActionResult Index()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("User")))
                return RedirectToAction("Login", "Account");

            // Truyền thêm danh sách File đã Up sang View (để Leader hoặc HR có thể xem danh sách file)
            ViewBag.ReportFiles = reportFileList.OrderByDescending(f => f.UploadedDate).ToList();

            // Sắp xếp ID mới nhất lên đầu để dễ theo dõi (Giữ nguyên)
            return View(list.OrderByDescending(x => x.Id).ToList());
        }

        // ===== TRANG XUẤT BÁO CÁO (XEM TRƯỚC BẢN IN & DUYỆT FILE) =====
        public IActionResult Export()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("User")))
                return RedirectToAction("Login", "Account");

            // 🔥 ĐÃ CẬP NHẬT: Truyền danh sách file HR đã up sang để sếp chọn đọc
            ViewBag.UploadedFiles = reportFileList.OrderByDescending(f => f.UploadedDate).ToList();

            // Lấy toàn bộ dữ liệu báo cáo để hiển thị lên tờ trình A4 (Mặc định)
            return View(list.OrderBy(x => x.Id).ToList());
        }

        // ===== TRANG SỬA BÁO CÁO (Chỉ HR mới vào được) (GIỮ NGUYÊN) =====
        public IActionResult Edit(int id)
        {
            if (!IsHR()) return RedirectToAction("Index");

            var item = list.FirstOrDefault(x => x.Id == id);
            if (item == null) return NotFound();

            return View(item);
        }

        [HttpPost]
        public IActionResult Edit(Report report)
        {
            if (!IsHR()) return Forbid();

            var item = list.FirstOrDefault(x => x.Id == report.Id);
            if (item != null)
            {
                item.HolidayBonus = report.HolidayBonus;
                item.DiligenceBonus = report.DiligenceBonus;
                item.Reward = report.Reward;
                item.Discipline = report.Discipline;
                item.LeaveDays = report.LeaveDays;
                item.LeaveType = report.LeaveType;
                item.TaskStatus = report.TaskStatus;
            }
            return RedirectToAction("Index");
        }

        // ================================================================
        // 🔥 PHẦN TÍNH NĂNG MỚI: UPLOAD FILE & GỬI BAN LÃNH ĐẠO 🔥
        // ================================================================

        [HttpPost]
        public async Task<IActionResult> UploadReport(IFormFile fileReport, string title, string category)
        {
            if (!IsHR()) return Forbid(); // Chỉ HR mới được up file

            if (fileReport != null && fileReport.Length > 0)
            {
                // 1. Kiểm tra định dạng (Chỉ nhận PDF, DOC, DOCX)
                string extension = Path.GetExtension(fileReport.FileName).ToLower();
                if (extension != ".pdf" && extension != ".doc" && extension != ".docx")
                {
                    TempData["Error"] = "Chỉ hỗ trợ up file Word hoặc PDF!";
                    return RedirectToAction("Index");
                }

                // 2. Tạo thư mục lưu trữ nếu chưa có
                string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "reports");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                // 3. Lưu file vật lý vào server (Chống trùng tên bằng Guid)
                string uniqueFileName = Guid.NewGuid().ToString() + "_" + fileReport.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await fileReport.CopyToAsync(fileStream);
                }

                // 4. Lưu thông tin file vào danh sách
                var newReport = new ReportFile
                {
                    Id = reportFileList.Any() ? reportFileList.Max(x => x.Id) + 1 : 1,
                    Title = title,

                    // Lưu thông tin phân loại báo cáo
                    Category = string.IsNullOrEmpty(category) ? "Khác" : category,

                    FileName = fileReport.FileName,
                    FilePath = "/uploads/reports/" + uniqueFileName,
                    UploadedBy = HttpContext.Session.GetString("RealName") ?? "Phòng Nhân Sự",
                    UploadedDate = DateTime.Now
                };
                reportFileList.Add(newReport);

                // 5. Bắn sóng SignalR cho màn hình Ban lãnh đạo (Gửi kèm Title để leader phân loại ô màu)
                await _hubContext.Clients.All.SendAsync("NewReportUploaded", reportFileList.Count, title);

                TempData["Success"] = "Đã gửi báo cáo thành công cho Ban lãnh đạo!";
            }
            else
            {
                TempData["Error"] = "Vui lòng chọn một file đính kèm.";
            }

            return RedirectToAction("Index");
        }

        // 🔥 XỬ LÝ TẢI FILE VỀ (Cho Ban Lãnh Đạo)
        public IActionResult Download(int id)
        {
            var report = reportFileList.FirstOrDefault(r => r.Id == id);
            if (report == null) return NotFound();

            var filepath = Path.Combine(_env.WebRootPath, report.FilePath.TrimStart('/'));
            if (!System.IO.File.Exists(filepath)) return NotFound("Không tìm thấy file trên máy chủ.");

            return PhysicalFile(filepath, "application/octet-stream", report.FileName);
        }
    }
}