using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Linq;
using giao_dien_demo.Models;

namespace giao_dien_demo.Controllers
{
    public class EmployeeController : Controller
    {
        // 🔥 FIX: thêm public
        public static List<Employee> list = new List<Employee>()
        {
            new Employee { Id = 1, Name = "Nguyễn Văn A", Department = "IT", Position = "Dev", Salary = 1000 },
            new Employee { Id = 2, Name = "Trần Thị B", Department = "HR", Position = "HR", Salary = 900 }
        };

        private bool CheckLogin()
        {
            var user = HttpContext.Session.GetString("User");
            return !string.IsNullOrEmpty(user);
        }

        public IActionResult Index()
        {
            if (!CheckLogin())
                return RedirectToAction("Login", "Account");

            return View(list);
        }

        public IActionResult Create()
        {
            if (!CheckLogin())
                return RedirectToAction("Login", "Account");

            return View();
        }

        [HttpPost]
        public IActionResult Create(Employee emp)
        {
            if (!CheckLogin())
                return RedirectToAction("Login", "Account");

            emp.Id = list.Count + 1;
            list.Add(emp);
            return RedirectToAction("Index");
        }

        public IActionResult Edit(int id)
        {
            if (!CheckLogin())
                return RedirectToAction("Login", "Account");

            var emp = list.FirstOrDefault(x => x.Id == id);
            return View(emp);
        }

        [HttpPost]
        public IActionResult Edit(Employee emp)
        {
            if (!CheckLogin())
                return RedirectToAction("Login", "Account");

            var e = list.FirstOrDefault(x => x.Id == emp.Id);
            if (e != null)
            {
                e.Name = emp.Name;
                e.Department = emp.Department;
                e.Position = emp.Position;
                e.Salary = emp.Salary;
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            if (!CheckLogin())
                return Unauthorized();

            var emp = list.FirstOrDefault(x => x.Id == id);
            if (emp != null)
            {
                list.Remove(emp);
                return Ok(); // 🔥 cần cho fetch
            }

            return NotFound();
        }
    }
}