using System;

namespace giao_dien_demo.Models
{
    public class Contract
    {
        public int Id { get; set; }
        public string? EmployeeName { get; set; }
        public string? Duration { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; } = "Chờ duyệt";

        // Nội dung và chữ ký
        public string? Content { get; set; }
        public string? Signature { get; set; }
        public string? HrSignature { get; set; }
        public DateTime? ExtensionDate { get; set; }

        // 🔥 QUAN TRỌNG: Thêm dòng này để hết lỗi CreatedAt
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}