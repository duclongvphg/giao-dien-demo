using System;

namespace giao_dien_demo.Models
{
    public class Report
    {
        public int Id { get; set; }
        public string EmployeeName { get; set; }
        public string HolidayBonus { get; set; } // Thưởng lễ
        public string DiligenceBonus { get; set; } // Chuyên cần
        public string Reward { get; set; } // Khen thưởng
        public string Discipline { get; set; } // Kỷ luật
        public int LeaveDays { get; set; } // Số ngày nghỉ
        public string LeaveType { get; set; } // Có phép / Không phép
        public string TaskStatus { get; set; } // Hoàn thành nhiệm vụ
    }
}