using Microsoft.EntityFrameworkCore;
using giao_dien_demo.Models;

namespace giao_dien_demo.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Attendance> Attendances { get; set; }
        public DbSet<Salary> Salaries { get; set; }
        public DbSet<Leave> Leaves { get; set; }

        // 🔥 THÊM DÒNG NÀY: Để hết lỗi "Contracts" ở Controller
        public DbSet<Contract> Contracts { get; set; }

        // 🔥 THÊM BẢNG NÀY: Quản lý danh sách chức vụ và lương chuẩn
        public DbSet<JobPosition> JobPositions { get; set; }

        // 🔥 TÍNH NĂNG MỚI: Tự động nạp dữ liệu (Seed Data) vào DB
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Bơm sẵn danh sách chức vụ chuẩn công ty IT vào bảng JobPositions
            modelBuilder.Entity<JobPosition>().HasData(
                new JobPosition { Id = 1, Group = "1. Ban Giám đốc", Name = "Giám đốc điều hành (CEO)", MinSalary = 60000000 },
                new JobPosition { Id = 2, Group = "1. Ban Giám đốc", Name = "Giám đốc Kỹ thuật (CTO)", MinSalary = 50000000 },
                new JobPosition { Id = 3, Group = "1. Ban Giám đốc", Name = "Giám đốc Nhân sự (CHRO)", MinSalary = 40000000 },
                new JobPosition { Id = 4, Group = "2. Cấp Quản lý", Name = "Quản lý Dự án (PM)", MinSalary = 30000000 },
                new JobPosition { Id = 5, Group = "2. Cấp Quản lý", Name = "Trưởng phòng Kỹ thuật", MinSalary = 30000000 },
                new JobPosition { Id = 6, Group = "2. Cấp Quản lý", Name = "Trưởng phòng HR / Sales", MinSalary = 20000000 },
                new JobPosition { Id = 7, Group = "3. Cấp Nhân viên", Name = "Lập trình viên (Senior)", MinSalary = 25000000 },
                new JobPosition { Id = 8, Group = "3. Cấp Nhân viên", Name = "Lập trình viên (Junior/Mid)", MinSalary = 12000000 },
                new JobPosition { Id = 9, Group = "3. Cấp Nhân viên", Name = "Phân tích nghiệp vụ (BA)", MinSalary = 15000000 },
                new JobPosition { Id = 10, Group = "3. Cấp Nhân viên", Name = "Kiểm thử phần mềm (Tester/QA)", MinSalary = 10000000 },
                new JobPosition { Id = 11, Group = "4. Khối Back-Office", Name = "Chuyên viên Nhân sự", MinSalary = 9000000 },
                new JobPosition { Id = 12, Group = "4. Khối Back-Office", Name = "Kế toán viên", MinSalary = 9000000 },
                new JobPosition { Id = 13, Group = "4. Khối Back-Office", Name = "Nhân viên Kinh doanh (Sales)", MinSalary = 7000000 },
                new JobPosition { Id = 14, Group = "5. Thực tập sinh", Name = "Thực tập sinh (Intern)", MinSalary = 3000000 }
            );
        }
    }
}