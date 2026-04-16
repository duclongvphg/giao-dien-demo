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
            var role = HttpContext.Session.GetString("Role");

            // chưa đăng nhập
            if (string.IsNullOrEmpty(user))
                return RedirectToAction("Login", "Account");

            // chỉ HR được vào dashboard này
            if (role != "HR")
                return RedirectToAction("Login", "Account");

            // giữ nguyên dữ liệu cũ
            var totalEmployees = EmployeeController.list.Count;

            // danh sách người nghỉ việc từ LeaveController
            var resignedNames = LeaveController.list
                .Select(x => x.EmployeeName)
                .Distinct()
                .ToList();

            // số nghỉ việc
            var resignedEmployees = resignedNames.Count;

            // số đang làm = tổng - nghỉ việc
            var workingEmployees = totalEmployees - resignedEmployees;

            // tránh âm số
            if (workingEmployees < 0)
                workingEmployees = 0;

            // truyền qua view
            ViewBag.Total = totalEmployees;
            ViewBag.Working = workingEmployees;
            ViewBag.Leave = resignedEmployees;

            return View();
        }

        // ================== EMPLOYEE LIST ==================
        public IActionResult EmployeeList(string status)
        {
            var user = HttpContext.Session.GetString("User");
            var role = HttpContext.Session.GetString("Role");

            if (string.IsNullOrEmpty(user))
                return RedirectToAction("Login", "Account");

            // chỉ HR được xem
            if (role != "HR")
                return RedirectToAction("Login", "Account");

            var employees = EmployeeController.list;

            List<Employee> result = new List<Employee>();

            // lấy danh sách nghỉ việc
            var resignedNames = LeaveController.list
                .Select(x => x.EmployeeName)
                .Distinct()
                .ToList();

            if (status == "working")
            {
                result = employees
                    .Where(e => !resignedNames.Contains(e.Name))
                    .ToList();
            }
            else if (status == "quit")
            {
                result = employees
                    .Where(e => resignedNames.Contains(e.Name))
                    .ToList();
            }
            else
            {
                result = employees;
            }

            return View(result);
        }
    }
}