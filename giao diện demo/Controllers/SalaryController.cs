using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using giao_dien_demo.Models;
using giao_dien_demo.Data;
using System.Collections.Generic;
using System.Linq;
using System;

namespace giao_dien_demo.Controllers
{
    public class SalaryController : Controller
    {
        public static List<Salary> BangLuongDaChot = new List<Salary>();
        private readonly ApplicationDbContext _context;

        public SalaryController(ApplicationDbContext context)
        {
            _context = context;
        }

        private bool CheckLogin()
        {
            return !string.IsNullOrEmpty(HttpContext.Session.GetString("User"));
        }

        // ==========================================
        // 🔥 HÀM TÍNH LƯƠNG CHUẨN: LƯƠNG THEO CÔNG + PHỤ CẤP + THƯỞNG
        // ==========================================
        private void TuDongCapNhatBangLuong()
        {
            BangLuongDaChot.Clear();

            // Lấy dữ liệu chấm công hiện tại
            var dsChamCong = AttendanceController.list
                .Where(x => x.CheckOut.HasValue)
                .GroupBy(x => x.EmployeeName);

            int idCounter = 1;

            // 🔥 ĐÃ SỬA: Lấy toàn bộ nhân viên (bao gồm cả Thử việc) để tính lương
            var allEmployees = _context.Employees.ToList();

            foreach (var emp in allEmployees)
            {
                var g = dsChamCong.FirstOrDefault(x => x.Key == emp.Name);

                // --- 1. THÔNG SỐ CƠ BẢN ---
                double luongCoBan = (double)emp.Salary;
                double stdHours = g != null ? g.Sum(x => x.StandardHours) : 0;
                double normalOtHours = g != null ? g.Where(x => x.CheckIn.DayOfWeek != DayOfWeek.Sunday).Sum(x => x.OvertimeHours) : 0;
                double weekendOtHours = g != null ? g.Where(x => x.CheckIn.DayOfWeek == DayOfWeek.Sunday).Sum(x => x.OvertimeHours) : 0;
                double totalHoursWorked = stdHours + normalOtHours + weekendOtHours;

                // Quy đổi lương giờ (Dựa trên 26 ngày/tháng và 8 giờ/ngày = 208 giờ)
                double luongTheoGio = luongCoBan / 208.0;

                // --- 2. TÍNH LƯƠNG THEO CÔNG ---
                double luongHanhChinh = luongTheoGio * stdHours;
                double luongTangCa = (luongTheoGio * normalOtHours * 1.5) + (luongTheoGio * weekendOtHours * 2.0);

                // --- 3. PHỤ CẤP & THƯỞNG ---
                double phuCapChucVu = (double)emp.PositionAllowance;
                double phuCapDocHai = (double)emp.HazardousAllowance;
                double heSoVung = (double)emp.RegionalAllowanceSystem;
                double phuCapKhuVuc = luongCoBan * heSoVung;

                double tongPhuCapVaOT = phuCapChucVu + phuCapDocHai + phuCapKhuVuc + luongTangCa;

                // --- 4. TỔNG THU NHẬP (GROSS) ---
                double totalGross = luongHanhChinh + tongPhuCapVaOT;

                // --- 5. GIẢM TRỪ & THUẾ (🔥 LOGIC BẢO HIỂM MỚI Ở ĐÂY) ---
                double insurance = 0;

                // Giả sử Position lưu chức vụ hoặc loại nhân viên ("Thử việc", "Chính thức")
                string viTriLamViec = emp.Position ?? "";
                bool laThuViec = viTriLamViec.Contains("Thử việc") || viTriLamViec.Contains("Thực tập");

                // 👉 Chỉ đóng bảo hiểm nếu: Không phải Thử việc/Thực tập VÀ Có đi làm (Giờ làm > 0)
                if (!laThuViec && totalHoursWorked > 0)
                {
                    insurance = luongCoBan * 0.105;
                }

                // Thuế TNCN
                double giamTruBanThan = 15500000;
                double phanOTMienThue = (luongTheoGio * normalOtHours * 0.5) + (luongTheoGio * weekendOtHours * 1.0);
                double thuNhapTinhThue = totalGross - insurance - giamTruBanThan - phanOTMienThue;
                double thueTNCN = TinhThueLuyTien2026(Math.Max(0, thuNhapTinhThue));

                // --- 6. THỰC NHẬN (NET) ---
                double netSalary = totalGross - insurance - thueTNCN;

                BangLuongDaChot.Add(new Salary
                {
                    Id = idCounter++,
                    EmployeeName = emp.Name,
                    BasicSalary = luongCoBan,
                    HoursWorked = Math.Round(totalHoursWorked, 2),
                    BonusAndAllowance = Math.Round(tongPhuCapVaOT, 0),
                    TotalSalary = Math.Round(totalGross, 0),
                    Insurance = Math.Round(insurance, 0),
                    TaxTNCN = Math.Round(thueTNCN, 0),
                    NetSalary = Math.Round(netSalary, 0),
                    SalaryMonth = DateTime.Now
                });
            }
        }

        public IActionResult Index()
        {
            if (!CheckLogin()) return RedirectToAction("Login", "Account");
            TuDongCapNhatBangLuong();

            var role = HttpContext.Session.GetString("Role");
            var realName = (HttpContext.Session.GetString("RealName") ?? HttpContext.Session.GetString("User") ?? "").Trim().ToLower();

            if (role == "HR" || role == "Admin" || role == "Leader")
            {
                return View(BangLuongDaChot);
            }
            else
            {
                var mySalaryHistory = BangLuongDaChot
                    .Where(s => !string.IsNullOrEmpty(s.EmployeeName) && s.EmployeeName.Trim().ToLower() == realName)
                    .ToList();
                return View(mySalaryHistory);
            }
        }

        private static double TinhThueLuyTien2026(double thuNhap)
        {
            if (thuNhap <= 0) return 0;
            double thue = 0;
            if (thuNhap <= 10000000) return thuNhap * 0.05;
            thue += 10000000 * 0.05; thuNhap -= 10000000;
            if (thuNhap <= 20000000) return thue + (thuNhap * 0.1);
            thue += 20000000 * 0.1; thuNhap -= 20000000;
            if (thuNhap <= 30000000) return thue + (thuNhap * 0.2);
            thue += 30000000 * 0.2; thuNhap -= 30000000;
            if (thuNhap <= 40000000) return thue + (thuNhap * 0.3);
            thue += 40000000 * 0.3; thuNhap -= 40000000;
            return thue + (thuNhap * 0.35);
        }
    }
}