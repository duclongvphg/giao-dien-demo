using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using giao_dien_demo.Models;
using giao_dien_demo.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using giao_dien_demo.Hubs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace giao_dien_demo.Controllers
{
    public class ContractController : Controller
    {
        // Danh sách tĩnh hỗ trợ các trang cũ (giữ đồng bộ với DB)
        public static List<Contract> list = new List<Contract>();

        private readonly ApplicationDbContext _context;
        private readonly IHubContext<DashboardHub> _hubContext;

        public ContractController(ApplicationDbContext context, IHubContext<DashboardHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        private bool CheckLogin()
        {
            var user = HttpContext.Session.GetString("User");
            return !string.IsNullOrEmpty(user);
        }

        // --- 1. TRANG DANH SÁCH HỢP ĐỒNG ---
        public async Task<IActionResult> Index()
        {
            if (!CheckLogin()) return RedirectToAction("Login", "Account");

            var role = HttpContext.Session.GetString("Role");
            var realName = (HttpContext.Session.GetString("RealName") ?? HttpContext.Session.GetString("User") ?? "").Trim().ToLower();

            ViewBag.EmployeeList = await _context.Employees.OrderBy(e => e.Name).ToListAsync();

            if (role == "HR" || role == "Admin" || role == "Leader")
            {
                var allContracts = await _context.Contracts.ToListAsync();
                return View(allContracts);
            }
            else
            {
                var allContracts = await _context.Contracts.ToListAsync();
                var myContracts = allContracts
                    .Where(c => !string.IsNullOrEmpty(c.EmployeeName) &&
                                c.EmployeeName.Trim().ToLower() == realName)
                    .ToList();

                return View(myContracts);
            }
        }

        // --- 2. TẠO HỢP ĐỒNG MỚI (HR) ---
        [HttpPost]
        public async Task<IActionResult> Create(Contract contract)
        {
            if (!CheckLogin()) return RedirectToAction("Login", "Account");

            if (contract != null)
            {
                if (!string.IsNullOrEmpty(contract.EmployeeName))
                {
                    contract.EmployeeName = contract.EmployeeName.Trim();
                }

                contract.CreatedAt = DateTime.Now;
                contract.Status = "Chờ nhân viên ký";
                if (contract.StartDate == default) contract.StartDate = DateTime.Now;
                contract.EndDate = contract.StartDate.AddYears(1);

                _context.Contracts.Add(contract);
                await _context.SaveChangesAsync();

                list.Add(contract);

                await _hubContext.Clients.All.SendAsync("ReceiveNotification", $"HR đã gửi hợp đồng mới cho {contract.EmployeeName}");
            }
            return RedirectToAction("Index");
        }

        // --- 3. NHÂN VIÊN KÝ XÁC NHẬN ---
        [HttpPost]
        public async Task<IActionResult> SubmitSignature(int Id, string Duration, string Signature, DateTime ExtensionDate)
        {
            var contract = await _context.Contracts.FindAsync(Id);
            if (contract != null)
            {
                contract.Duration = Duration;
                contract.Signature = Signature;
                contract.EndDate = ExtensionDate != default ? ExtensionDate : contract.StartDate.AddYears(1);
                contract.Status = "Đã ký (Chờ duyệt cuối)";

                await _context.SaveChangesAsync();

                var itemInList = list.FirstOrDefault(c => c.Id == Id);
                if (itemInList != null) itemInList.Status = contract.Status;

                await _hubContext.Clients.All.SendAsync("ReceiveNotification", $"Nhân viên {contract.EmployeeName} đã ký xác nhận.");
            }
            return RedirectToAction("Index");
        }

        // --- 4. DUYỆT HỢP ĐỒNG & TỰ ĐỘNG ĐỒNG BỘ NHÂN SỰ VÀ BÁO CÁO ---
        [HttpPost]
        public async Task<IActionResult> Approve(int id)
        {
            var contract = await _context.Contracts.FindAsync(id);
            if (contract == null) return NotFound();

            contract.Status = "Còn hiệu lực";

            var itemInList = list.FirstOrDefault(c => c.Id == id);
            if (itemInList != null) itemInList.Status = "Còn hiệu lực";

            // Kéo dữ liệu nhân viên về
            var allEmployees = await _context.Employees.ToListAsync();
            var emp = allEmployees.FirstOrDefault(e => !string.IsNullOrEmpty(e.Name) && e.Name.Trim().ToLower() == contract.EmployeeName.Trim().ToLower());

            // 🔥 LOGIC MỚI: Đếm xem hiện tại có bao nhiêu người ĐÃ CÓ MÃ để tạo mã tiếp theo
            int currentCodeCount = allEmployees.Count(e => !string.IsNullOrEmpty(e.EmployeeCode));
            string newEmpCode = "NV" + (currentCodeCount + 1).ToString("D3");

            if (emp != null)
            {
                emp.Position = "Chính thức";

                // 🔥 NẾU LÀ NHÂN VIÊN CŨ TỪ TRƯỚC (CHƯA CÓ MÃ) -> CẤP LUÔN MÃ MỚI BỔ SUNG
                if (string.IsNullOrEmpty(emp.EmployeeCode))
                {
                    emp.EmployeeCode = newEmpCode;
                }
            }
            else
            {
                // NẾU LÀ NGƯỜI MỚI TINH -> TẠO HỒ SƠ MỚI VÀ CẤP MÃ
                var newEmp = new Employee
                {
                    EmployeeCode = newEmpCode,
                    Name = contract.EmployeeName,
                    Department = "Phòng nhân sự",
                    Position = "Chính thức",
                    Salary = 10000000
                };
                _context.Employees.Add(newEmp);
            }

            // 3. TÍNH NĂNG MỚI: TỰ ĐỘNG TẠO BẢN GHI VÀO TRANG BÁO CÁO
            var newReport = new Report
            {
                Id = ReportController.list.Any() ? ReportController.list.Max(r => r.Id) + 1 : 1,
                EmployeeName = contract.EmployeeName,
                HolidayBonus = "0",
                DiligenceBonus = "0",
                Reward = "Chưa có",
                Discipline = "Không",
                LeaveDays = 0,
                LeaveType = "---",
                TaskStatus = "Mới tiếp nhận"
            };
            ReportController.list.Add(newReport);

            await _context.SaveChangesAsync();

            // 4. REAL-TIME: Cập nhật con số trên Dashboard và hiện Popup
            var totalOfficial = await _context.Employees.CountAsync(e => e.Position == "Chính thức");
            await _hubContext.Clients.All.SendAsync("UpdateEmployeeCount", totalOfficial);
            await _hubContext.Clients.All.SendAsync("ContractApproved", contract.EmployeeName);

            return Ok(new { success = true, message = "Duyệt thành công!" });
        }

        // --- 5. HỦY HỢP ĐỒNG ---
        [HttpPost]
        public async Task<IActionResult> Cancel(int id)
        {
            var contract = await _context.Contracts.FindAsync(id);
            if (contract != null)
            {
                _context.Contracts.Remove(contract);
                var itemInList = list.FirstOrDefault(c => c.Id == id);
                if (itemInList != null) list.Remove(itemInList);

                await _context.SaveChangesAsync();
                return Ok();
            }
            return NotFound();
        }

        // --- 6. XÓA HỢP ĐỒNG ---
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            return await Cancel(id); // Dùng chung logic với Cancel cho gọn
        }
    }
}