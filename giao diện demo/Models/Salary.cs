using System;

namespace giao_dien_demo.Models
{
    public class Salary
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }

        // Thêm dấu ? để sửa cảnh báo vàng "Non-nullable property"
        public string? EmployeeName { get; set; }

        // --- ĐẦU VÀO (INPUT) ---
        public double BasicSalary { get; set; } // Lương cơ bản để tính bảo hiểm
        public double HoursWorked { get; set; } // Số giờ làm thực tế

        // 🔥 THÊM BIẾN NÀY: Để lưu tổng tiền Thưởng & Phụ cấp gộp lại (hiển thị cho gọn trên bảng lương)
        public double BonusAndAllowance { get; set; }

        public int NumberOfDependents { get; set; } // Số người phụ thuộc (N)

        // Các khoản cộng thêm (Lưu chi tiết)
        public double HolidayBonus { get; set; } // Thưởng lễ
        public double DiligenceBonus { get; set; } // Thưởng chuyên cần
        public double Reward { get; set; } // Thưởng khác

        // Các khoản trừ khác
        public double Discipline { get; set; } // Kỷ luật/Trừ khác

        // --- KẾT QUẢ TÍNH TOÁN (OUTPUT) ---

        // 🔥 Đã đổi tên chuẩn thành TotalSalary để hết lỗi đỏ
        public double TotalSalary { get; set; } // Tổng thu nhập trước thuế (Gross)

        public double Insurance { get; set; } // Bảo hiểm (10.5%)
        public double TaxTNCN { get; set; } // Thuế TNCN lũy tiến 5 bậc
        public double NetSalary { get; set; } // Lương thực nhận cuối cùng

        public DateTime SalaryMonth { get; set; } // Tháng tính lương
    }
}