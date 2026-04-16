using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using giao_dien_demo.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using giao_dien_demo.Hubs;

namespace giao_dien_demo.Controllers
{
    public class AttendanceController : Controller
    {
        // Danh sách tĩnh lưu trữ dữ liệu chấm công
        public static List<Attendance> list = new List<Attendance>();

        private readonly IHubContext<DashboardHub> _hubContext;

        public AttendanceController(IHubContext<DashboardHub> hubContext)
        {
            _hubContext = hubContext;
        }

        private bool CheckLogin()
        {
            var user = HttpContext.Session.GetString("User");
            return !string.IsNullOrEmpty(user);
        }

        public IActionResult Index()
        {
            if (!CheckLogin())
                return RedirectToAction("Login", "Account");

            // Trả về danh sách để hiển thị trên bảng chấm công
            return View(list);
        }

        // --- HÀM CHECK-IN: LẤY HỌ TÊN THẬT, ĐỒNG BỘ BÁO CÁO & LƯU VẾT ĐI MUỘN ---
        public async Task<IActionResult> CheckIn()
        {
            if (!CheckLogin())
                return RedirectToAction("Login", "Account");

            // 1. LẤY HỌ TÊN THẬT TỪ SESSION
            var user = HttpContext.Session.GetString("User") ?? "";
            var realName = HttpContext.Session.GetString("RealName") ?? user;

            // Tìm lượt chấm công cuối cùng của người này
            var last = list.LastOrDefault(x => x.EmployeeName == realName);

            // Tránh check-in trùng khi chưa check-out
            if (last != null && last.CheckOut == null)
            {
                return RedirectToAction("Index");
            }

            // 🔥 2. LOGIC TÍNH TOÁN ĐI MUỘN
            int lateMin = 0;
            string note = "Đúng giờ";
            var timeIn = DateTime.Now.TimeOfDay;

            if (DateTime.Now.DayOfWeek == DayOfWeek.Sunday)
            {
                note = "Làm thêm Chủ Nhật";
            }
            else
            {
                var morningStart = new TimeSpan(7, 30, 0);
                var morningEnd = new TimeSpan(11, 30, 0);
                var afternoonStart = new TimeSpan(13, 0, 0);

                // Đi muộn ca sáng (Sau 7h30 và trước 11h30)
                if (timeIn > morningStart && timeIn <= morningEnd)
                {
                    lateMin = (int)(timeIn - morningStart).TotalMinutes;
                    note = $"Đi muộn sáng ({lateMin} phút)";
                }
                // Đi muộn ca chiều (Sau 13h00 và trước 17h00)
                else if (timeIn > afternoonStart && timeIn <= new TimeSpan(17, 0, 0))
                {
                    lateMin = (int)(timeIn - afternoonStart).TotalMinutes;
                    note = $"Đi muộn chiều ({lateMin} phút)";
                }
            }

            // 3. TẠO LƯỢT CHẤM CÔNG VỚI TÊN THẬT & TRẠNG THÁI MUỘN
            list.Add(new Attendance
            {
                Id = list.Count + 1,
                EmployeeName = realName,
                CheckIn = DateTime.Now,
                LateMinutes = lateMin,
                StatusNote = note
            });

            // ========================================================
            // 4. ĐỒNG BỘ LIÊN KẾT TỰ ĐỘNG SANG TRANG BÁO CÁO HR
            // ========================================================
            var existingReport = ReportController.list.FirstOrDefault(r => r.EmployeeName == realName);
            if (existingReport != null)
            {
                // Nếu ĐÃ CÓ tên trong báo cáo -> Cộng thêm 1 điểm vào cột Chuyên Cần
                if (int.TryParse(existingReport.DiligenceBonus, out int currentDiligence))
                {
                    existingReport.DiligenceBonus = (currentDiligence + 1).ToString();
                }
                else
                {
                    existingReport.DiligenceBonus = "1";
                }
            }
            else
            {
                // Nếu CHƯA CÓ tên trong báo cáo -> Tự động tạo dòng mới tinh cho nhân viên này
                var newReport = new Report
                {
                    Id = ReportController.list.Any() ? ReportController.list.Max(r => r.Id) + 1 : 1,
                    EmployeeName = realName,
                    HolidayBonus = "0",
                    DiligenceBonus = "1", // Điểm chuyên cần đầu tiên do vừa chấm công
                    Reward = "Chưa có",
                    Discipline = "Không",
                    LeaveDays = 0,
                    LeaveType = "---",
                    TaskStatus = "Đang làm việc"
                };
                ReportController.list.Add(newReport);
            }

            // 5. GỬI THÔNG BÁO REAL-TIME
            int todayCount = list.Count(x => x.CheckIn.Date == DateTime.Today);
            await _hubContext.Clients.All.SendAsync("UpdateAttendanceCount", todayCount, realName);

            return RedirectToAction("Index");
        }

        // --- HÀM CHECK-OUT: BÁO CÁO LƯƠNG REAL-TIME & TÍNH GIỜ CHUẨN/TĂNG CA ---
        public async Task<IActionResult> CheckOut(int id)
        {
            if (!CheckLogin())
                return RedirectToAction("Login", "Account");

            var item = list.FirstOrDefault(x => x.Id == id);

            if (item != null && item.CheckOut == null)
            {
                // 1. Cập nhật giờ Check Out
                item.CheckOut = DateTime.Now;
                DateTime ci = item.CheckIn;
                DateTime co = item.CheckOut.Value;

                // 🔥 2. LOGIC TÍNH GIỜ THEO CA HÀNH CHÍNH (LOẠI BỎ NGHỈ TRƯA)
                if (ci.DayOfWeek == DayOfWeek.Sunday)
                {
                    // Chủ nhật: 100% là tăng ca (Trừ 1.5h nếu làm xuyên trưa)
                    double total = (co - ci).TotalHours;
                    if (ci.TimeOfDay <= new TimeSpan(11, 30, 0) && co.TimeOfDay >= new TimeSpan(13, 0, 0))
                    {
                        total -= 1.5;
                    }
                    item.OvertimeHours = Math.Max(0, total);
                    item.StandardHours = 0;
                }
                else
                {
                    // Thứ 2 -> Thứ 7:
                    double stdHours = 0;
                    double otHours = 0;

                    // A. Ca Sáng (07:30 - 11:30)
                    DateTime mStart = ci.Date.AddHours(7).AddMinutes(30);
                    DateTime mEnd = ci.Date.AddHours(11).AddMinutes(30);
                    stdHours += CalculateOverlap(ci, co, mStart, mEnd);

                    // B. Ca Chiều (13:00 - 17:00)
                    DateTime aStart = ci.Date.AddHours(13);
                    DateTime aEnd = ci.Date.AddHours(17);
                    stdHours += CalculateOverlap(ci, co, aStart, aEnd);

                    // C. Tăng ca tối đa 2.5 tiếng (17:00 - 19:30)
                    DateTime otStart = ci.Date.AddHours(17);
                    DateTime otEnd = ci.Date.AddHours(19).AddMinutes(30);
                    otHours += CalculateOverlap(ci, co, otStart, otEnd);

                    item.StandardHours = stdHours;
                    item.OvertimeHours = otHours;
                }

                // 3. BẮN SÓNG REAL-TIME BÁO CHO HR
                int soBangLuongDaTinh = list.Count(x => x.CheckOut != null && x.CheckIn.Date == DateTime.Today);
                await _hubContext.Clients.All.SendAsync("UpdateSalaryCount", soBangLuongDaTinh);
            }

            return RedirectToAction("Index");
        }

        // ========================================================
        // 🔥 1. HÀM CUNG CẤP DỮ LIỆU TỔNG HỢP CHO LỊCH (GOM NHÓM THEO NGÀY)
        // ========================================================
        [HttpGet]
        public IActionResult GetAttendanceHistory()
        {
            if (!CheckLogin()) return Unauthorized();

            // Gom nhóm tất cả nhân viên theo Ngày, chỉ trả về Số lượng thay vì Tên từng người
            var events = list.GroupBy(a => a.CheckIn.Date).Select(g => new
            {
                title = $"{g.Count()} lượt chấm công",
                start = g.Key.ToString("yyyy-MM-dd"),
                color = "#0284c7", // Màu xanh dương chuyên nghiệp cho gọn gàng
                textColor = "white",
                allDay = true
            }).ToList();

            return Json(events);
        }

        // ========================================================
        // 🔥 2. HÀM MỚI: API LẤY DANH SÁCH NHÂN VIÊN THEO NGÀY CỤ THỂ
        // ========================================================
        [HttpGet]
        public IActionResult GetAttendanceDetailsByDate(string date)
        {
            if (!CheckLogin()) return Unauthorized();
            if (!DateTime.TryParse(date, out DateTime selectedDate)) return BadRequest();

            // Lọc ra danh sách những ai chấm công trong cái ngày mà HR vừa click
            var details = list.Where(a => a.CheckIn.Date == selectedDate.Date)
                .Select(a => new {
                    empName = a.EmployeeName,
                    checkIn = a.CheckIn.ToString("HH:mm:ss"),
                    checkOut = a.CheckOut.HasValue ? a.CheckOut.Value.ToString("HH:mm:ss") : "---",
                    isLate = a.LateMinutes > 0, // Gửi cờ boolean để frontend đổi màu đỏ
                    note = a.StatusNote
                }).ToList();

            return Json(details);
        }

        // ==========================================
        // 🔥 HÀM HỖ TRỢ: TÍNH SỐ GIỜ GIAO NHAU GIỮA 2 KHOẢNG THỜI GIAN
        // ==========================================
        private double CalculateOverlap(DateTime start1, DateTime end1, DateTime start2, DateTime end2)
        {
            DateTime overlapStart = start1 > start2 ? start1 : start2;
            DateTime overlapEnd = end1 < end2 ? end1 : end2;
            if (overlapStart < overlapEnd)
                return (overlapEnd - overlapStart).TotalHours;
            return 0;
        }
    }
}