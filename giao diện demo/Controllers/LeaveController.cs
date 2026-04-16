using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using giao_dien_demo.Models;
using giao_dien_demo.Data; // 🔥 ĐÃ THÊM: Thư viện kết nối Database
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using giao_dien_demo.Hubs;

namespace giao_dien_demo.Controllers
{
    public class LeaveController : Controller
    {
        // Danh sách lưu tạm dữ liệu
        public static List<Leave> list = new List<Leave>();

        private readonly IHubContext<DashboardHub> _hubContext;
        private readonly ApplicationDbContext _context; // 🔥 ĐÃ THÊM: Biến gọi Database

        // 🔥 ĐÃ CẬP NHẬT: Tiêm Database vào Constructor
        public LeaveController(IHubContext<DashboardHub> hubContext, ApplicationDbContext context)
        {
            _hubContext = hubContext;
            _context = context;
        }

        private bool CheckLogin()
        {
            var user = HttpContext.Session.GetString("User");
            return !string.IsNullOrEmpty(user);
        }

        // hiển thị theo phân quyền
        public IActionResult Index()
        {
            if (!CheckLogin())
                return RedirectToAction("Login", "Account");

            var user = HttpContext.Session.GetString("User");
            var role = HttpContext.Session.GetString("Role");

            // 🔥 THÊM DÒNG NÀY: Kéo danh sách nhân viên từ DB để giao diện dò Mã NV
            ViewBag.EmployeeList = _context.Employees.ToList();

            if (role == "HR")
            {
                return View(list);
            }
            else
            {
                var myLeaves = list.Where(x => x.EmployeeName == user).ToList();
                return View(myLeaves);
            }
        }

        public IActionResult Create()
        {
            if (!CheckLogin())
                return RedirectToAction("Login", "Account");

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Leave leave)
        {
            if (!CheckLogin())
                return RedirectToAction("Login", "Account");

            if (leave == null)
                return View();

            var role = HttpContext.Session.GetString("Role");

            leave.Id = list.Count > 0 ? list.Max(x => x.Id) + 1 : 1;
            leave.Status = "Chờ duyệt";

            list.Add(leave);

            int targetEmployeeLeaveCount = list.Count(x => x.EmployeeName == leave.EmployeeName);

            // Bắn sóng cho HR và Nhân viên
            await _hubContext.Clients.All.SendAsync("UpdateLeaveCount", list.Count, leave.EmployeeName);
            await _hubContext.Clients.All.SendAsync("UpdateMyLeaveCount", targetEmployeeLeaveCount, leave.EmployeeName);

            if (role == "HR")
            {
                return RedirectToAction("Index");
            }
            return RedirectToAction("Index", "NhanVien");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var item = list.FirstOrDefault(x => x.Id == id);

            if (item != null)
            {
                string employeeName = item.EmployeeName ?? "Ẩn danh";
                list.Remove(item);

                int myLeaveCount = list.Count(x => x.EmployeeName == employeeName);

                await _hubContext.Clients.All.SendAsync("UpdateLeaveCount", list.Count, employeeName);
                await _hubContext.Clients.All.SendAsync("UpdateMyLeaveCount", myLeaveCount, employeeName);

                return Ok();
            }

            return NotFound();
        }

        // ===== 🔥 ĐÃ CẬP NHẬT: HR DUYỆT ĐƠN (Thêm SignalR báo về nhân viên) =====
        [HttpPost]
        public async Task<IActionResult> Approve(int id)
        {
            var item = list.FirstOrDefault(x => x.Id == id);
            if (item != null)
            {
                item.Status = "Đã duyệt";

                // Bắn sóng báo cho nhân viên biết đơn đã được duyệt
                await _hubContext.Clients.All.SendAsync("LeaveApproved", item.EmployeeName);

                return Ok();
            }
            return NotFound();
        }

        // ===== 🔥 HÀM MỚI: HR TỪ CHỐI ĐƠN NGHỈ PHÉP =====
        [HttpPost]
        public async Task<IActionResult> Reject(int id)
        {
            var item = list.FirstOrDefault(x => x.Id == id);
            if (item != null)
            {
                // Đổi trạng thái sang Từ chối
                item.Status = "Từ chối";

                // Bắn sóng báo cho nhân viên biết đơn đã bị từ chối
                await _hubContext.Clients.All.SendAsync("LeaveRejected", item.EmployeeName);

                return Ok();
            }
            return NotFound();
        }
    }
}