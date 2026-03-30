using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Collections.Generic;
using giao_dien_demo.Models;

namespace giao_dien_demo.Controllers
{
    public class DashboardController : Controller
    {
        // ================== DASHBOARD ==================
        public IActionResult Index()
        {
            var user = HttpContext.Session.GetString("User");

            if (string.IsNullOrEmpty(user))
                return RedirectToAction("Login", "Account");

            // 🔥 LẤY DỮ LIỆU THẬT (GIỮ NGUYÊN)
            var totalEmployees = EmployeeController.list.Count;

            var workingEmployees = AttendanceController.list
                .Select(x => x.EmployeeName)
                .Distinct()
                .Count();

            var nghỉ = totalEmployees - workingEmployees;

            // truyền qua View
            ViewBag.Total = totalEmployees;
            ViewBag.Working = workingEmployees;
            ViewBag.Leave = nghỉ;

            return View();
        }

        // ================== NEW FEATURE ==================
        public IActionResult EmployeeList(string status)
        {
            var user = HttpContext.Session.GetString("User");

            if (string.IsNullOrEmpty(user))
                return RedirectToAction("Login", "Account");

            var employees = EmployeeController.list;

            List<Employee> result = new List<Employee>();

            if (status == "working")
            {
                var workingNames = AttendanceController.list
                    .Select(x => x.EmployeeName)
                    .Distinct()
                    .ToList();

                result = employees
                    .Where(e => workingNames.Contains(e.Name))
                    .ToList();
            }
            else if (status == "quit")
            {
                var workingNames = AttendanceController.list
                    .Select(x => x.EmployeeName)
                    .Distinct()
                    .ToList();

                result = employees
                    .Where(e => !workingNames.Contains(e.Name))
                    .ToList();
            }
            else
            {
                // all
                result = employees;
            }

            return View(result);
        }
    }
}