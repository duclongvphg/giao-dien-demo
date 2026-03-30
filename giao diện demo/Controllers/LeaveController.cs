using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using giao_dien_demo.Models;
using System.Collections.Generic;
using System.Linq;

namespace giao_dien_demo.Controllers
{
    public class LeaveController : Controller
    {
        public static List<Leave> list = new List<Leave>();

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

            return View(list);
        }

        // ===== CREATE =====
        public IActionResult Create()
        {
            if (!CheckLogin())
                return RedirectToAction("Login", "Account");

            return View();
        }

        [HttpPost]
        public IActionResult Create(Leave leave)
        {
            if (!CheckLogin())
                return RedirectToAction("Login", "Account");

            leave.Id = list.Count + 1;
            leave.EmployeeName = HttpContext.Session.GetString("User");

            list.Add(leave);

            return RedirectToAction("Index");
        }

        // ===== DELETE =====
        [HttpPost]
        public IActionResult Delete(int id)
        {
            var item = list.FirstOrDefault(x => x.Id == id);
            if (item != null)
            {
                list.Remove(item);
                return Ok();
            }

            return NotFound();
        }
    }
}