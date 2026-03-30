using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using giao_dien_demo.Models;
using System.Linq;

namespace giao_dien_demo.Controllers
{
    public class SalaryController : Controller
    {
        // 🔐 Check login
        private bool CheckLogin()
        {
            var user = HttpContext.Session.GetString("User");
            return !string.IsNullOrEmpty(user);
        }

        // ===== INDEX =====
        public IActionResult Index()
        {
            if (!CheckLogin())
                return RedirectToAction("Login", "Account");

            // 🔥 Lấy dữ liệu chấm công
            var attendances = AttendanceController.list;

            // 🔥 Tính lương theo giờ
            var data = attendances
                .Where(x => x.CheckOut != null) // chỉ tính khi đã checkout
                .GroupBy(x => x.EmployeeName)
                .Select((g, index) =>
                {
                    // 👉 tổng số giờ làm
                    double totalHours = g.Sum(x =>
    x.CheckOut.HasValue
        ? (x.CheckOut.Value - x.CheckIn).TotalHours
        : 0
);

                    return new Salary
                    {
                        Id = index + 1,
                        EmployeeName = g.Key,

                        // 🔥 Số giờ làm
                        HoursWorked = totalHours,

                        // 🔥 Lương (20k / giờ)
                        TotalSalary = totalHours * 20000
                    };
                })
                .ToList();

            return View(data);
        }
    }
}