using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using giao_dien_demo.Models;
using giao_dien_demo.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using giao_dien_demo.Hubs;
using Microsoft.AspNetCore.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace giao_dien_demo.Controllers
{
    public class EmployeeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<DashboardHub> _hubContext;
        private readonly IWebHostEnvironment _hostEnvironment;

        // ✅ KHAI BÁO BIẾN TĨNH: Đảm bảo các Controller khác không bị lỗi "Red line"
        public static List<Employee> list = new List<Employee>();

        public EmployeeController(ApplicationDbContext context,
                                  IHubContext<DashboardHub> hubContext,
                                  IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hubContext = hubContext;
            _hostEnvironment = hostEnvironment;
        }

        private bool CheckLogin()
        {
            var user = HttpContext.Session.GetString("User");
            return !string.IsNullOrEmpty(user);
        }

        // =========================================================================
        // 🔥 1. DANH SÁCH NHÂN SỰ - XỬ LÝ TRÙNG MÃ & TÌM KIẾM
        // =========================================================================
        public async Task<IActionResult> Index(string? searchName, string? department)
        {
            if (!CheckLogin()) return RedirectToAction("Login", "Account");

            // 🚀 BƯỚC "DỌN DẸP": RESET LẠI MÃ NẾU PHÁT HIỆN TRÙNG HOẶC THIẾU
            var allEmpsForFix = await _context.Employees.OrderBy(e => e.Id).ToListAsync();

            // Kiểm tra xem có bất kỳ mã nào bị trùng hoặc trống không
            bool needsFix = allEmpsForFix.Any(e => string.IsNullOrEmpty(e.EmployeeCode)) ||
                            allEmpsForFix.GroupBy(x => x.EmployeeCode).Any(g => g.Count() > 1);

            if (needsFix)
            {
                for (int i = 0; i < allEmpsForFix.Count; i++)
                {
                    // Đánh số lại toàn bộ theo thứ tự ID: NV001, NV002, NV003...
                    allEmpsForFix[i].EmployeeCode = "NV" + (i + 1).ToString("D3");
                    _context.Update(allEmpsForFix[i]);
                }
                await _context.SaveChangesAsync();
            }

            // --- TRUY VẤN TÌM KIẾM VÀ LỌC ---
            var query = _context.Employees.AsQueryable();

            if (!string.IsNullOrEmpty(searchName))
            {
                query = query.Where(e => (e.Name != null && e.Name.Contains(searchName)) ||
                                         (e.EmployeeCode != null && e.EmployeeCode.Contains(searchName)));
            }

            if (!string.IsNullOrEmpty(department))
                query = query.Where(e => e.Department == department);

            ViewBag.Departments = await _context.Employees
                                        .Where(e => e.Department != null)
                                        .Select(e => e.Department)
                                        .Distinct()
                                        .ToListAsync();

            ViewBag.CurrentSearch = searchName;
            ViewBag.CurrentDept = department;

            return View(await query.OrderBy(e => e.EmployeeCode).ToListAsync());
        }

        // --- 2. XEM CHI TIẾT HỒ SƠ ---
        public async Task<IActionResult> Details(int id)
        {
            if (!CheckLogin()) return RedirectToAction("Login", "Account");
            var emp = await _context.Employees.FindAsync(id);
            if (emp == null) return NotFound();
            return View(emp);
        }

        // --- 3. TRANG CÁ NHÂN: TỰ CẬP NHẬT & UPLOAD ẢNH ---
        public async Task<IActionResult> MyProfile()
        {
            if (!CheckLogin()) return RedirectToAction("Login", "Account");
            var realName = HttpContext.Session.GetString("RealName");
            if (string.IsNullOrEmpty(realName)) return RedirectToAction("Login", "Account");

            var emp = await _context.Employees.FirstOrDefaultAsync(e => e.Name == realName);
            if (emp == null) return NotFound("Hồ sơ không tồn tại.");
            return View(emp);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateMyProfile(Employee emp, IFormFile? AvatarFile)
        {
            if (!CheckLogin()) return RedirectToAction("Login", "Account");

            var existing = await _context.Employees.FindAsync(emp.Id);
            if (existing != null)
            {
                if (AvatarFile != null && AvatarFile.Length > 0)
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(AvatarFile.FileName);
                    string path = Path.Combine(_hostEnvironment.WebRootPath, "uploads", "avatars");
                    if (!Directory.Exists(path)) Directory.CreateDirectory(path);

                    if (!string.IsNullOrEmpty(existing.AvatarPath))
                    {
                        var oldPath = Path.Combine(_hostEnvironment.WebRootPath, existing.AvatarPath.TrimStart('/'));
                        if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                    }

                    using (var fileStream = new FileStream(Path.Combine(path, fileName), FileMode.Create))
                    {
                        await AvatarFile.CopyToAsync(fileStream);
                    }
                    existing.AvatarPath = "/uploads/avatars/" + fileName;
                }

                existing.Name = emp.Name;
                existing.PhoneNumber = emp.PhoneNumber;
                existing.Address = emp.Address;
                existing.Gender = emp.Gender;
                existing.BirthDate = emp.BirthDate;
                existing.CitizenId = emp.CitizenId;
                existing.BankName = emp.BankName;
                existing.BankAccountNumber = emp.BankAccountNumber;

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(MyProfile));
            }
            return View(emp);
        }

        // --- 4. SỬA THÔNG TIN (HR) ---
        public async Task<IActionResult> Edit(int id)
        {
            if (!CheckLogin()) return RedirectToAction("Login", "Account");
            var emp = await _context.Employees.FindAsync(id);
            if (emp == null) return NotFound();
            return View(emp);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Employee emp)
        {
            if (!CheckLogin()) return RedirectToAction("Login", "Account");
            var existing = await _context.Employees.FindAsync(emp.Id);
            if (existing != null)
            {
                existing.Name = emp.Name;
                existing.Department = emp.Department;
                existing.Position = emp.Position;
                existing.JobTitle = emp.JobTitle;
                existing.Email = emp.Email;
                existing.Salary = emp.Salary;
                existing.PositionAllowance = emp.PositionAllowance;
                existing.HazardousAllowance = emp.HazardousAllowance;
                existing.RegionalAllowanceSystem = emp.RegionalAllowanceSystem;

                if (!string.IsNullOrEmpty(emp.EmployeeCode)) existing.EmployeeCode = emp.EmployeeCode;

                await _context.SaveChangesAsync();
                await _hubContext.Clients.All.SendAsync("UserUpdated", new { fullName = emp.Name });
                return RedirectToAction(nameof(Index));
            }
            return View(emp);
        }

        // --- 5. XÓA ---
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            if (!CheckLogin()) return Unauthorized();
            var emp = await _context.Employees.FindAsync(id);
            if (emp != null)
            {
                _context.Employees.Remove(emp);
                await _context.SaveChangesAsync();
                var count = await _context.Employees.CountAsync();
                await _hubContext.Clients.All.SendAsync("UpdateEmployeeCount", count);
                return Ok();
            }
            return NotFound();
        }

        // --- 6. TẠO MỚI ---
        [HttpPost]
        public async Task<IActionResult> Create(Employee emp)
        {
            if (ModelState.IsValid)
            {
                _context.Add(emp);
                await _context.SaveChangesAsync();
                var count = await _context.Employees.CountAsync();
                await _hubContext.Clients.All.SendAsync("UpdateEmployeeCount", count);
            }
            return RedirectToAction(nameof(Index));
        }
    }
}