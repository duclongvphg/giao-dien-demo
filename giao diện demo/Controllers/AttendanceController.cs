using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using giao_dien_demo.Models;
using System.Collections.Generic;
using System.Linq;

namespace giao_dien_demo.Controllers
{
    public class AttendanceController : Controller
    {
        // 🔥 giữ public static
        public static List<Attendance> list = new List<Attendance>();

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

        public IActionResult CheckIn()
        {
            if (!CheckLogin())
                return RedirectToAction("Login", "Account");

            var user = HttpContext.Session.GetString("User");

            var last = list.LastOrDefault(x => x.EmployeeName == user);

            // 🔥 tránh check-in khi chưa checkout
            if (last != null && last.CheckOut == null)
            {
                return RedirectToAction("Index");
            }

            list.Add(new Attendance
            {
                Id = list.Count + 1,
                EmployeeName = user,
                CheckIn = DateTime.Now
            });

            return RedirectToAction("Index");
        }

        public IActionResult CheckOut(int id)
        {
            if (!CheckLogin())
                return RedirectToAction("Login", "Account");

            var item = list.FirstOrDefault(x => x.Id == id);

            if (item != null && item.CheckOut == null)
            {
                item.CheckOut = DateTime.Now;
            }

            return RedirectToAction("Index");
        }
    }
}