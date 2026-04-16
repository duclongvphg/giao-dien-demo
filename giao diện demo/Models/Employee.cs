using System;

namespace giao_dien_demo.Models
{
    public class Employee
    {
        public int Id { get; set; }
        public string? EmployeeCode { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }

        // --- HỒ SƠ CHI TIẾT ---
        public string? AvatarPath { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public string? Gender { get; set; }
        public DateTime? BirthDate { get; set; }
        public string? CitizenId { get; set; }
        public string? BankName { get; set; }
        public string? BankAccountNumber { get; set; }

        // --- CÔNG VIỆC ---
        public string? Department { get; set; }
        public string? Position { get; set; } // Trạng thái: Chính thức, Thử việc...
        public string? JobTitle { get; set; } // Vai trò: Admin, HR, Leader, Nhân viên...

        // --- LƯƠNG & PHỤ CẤP ---
        public decimal Salary { get; set; }
        public decimal PositionAllowance { get; set; } = 0;
        public decimal HazardousAllowance { get; set; } = 0;
        public decimal RegionalAllowanceSystem { get; set; } = 0;

        // --- LOGIC TỰ ĐỘNG ---
        public decimal TotalGrossSalary => Salary + PositionAllowance + HazardousAllowance + (Salary * RegionalAllowanceSystem);
        public decimal HourlyRate => Salary > 0 ? Math.Round(Salary / 208m, 0) : 0;
    }
}