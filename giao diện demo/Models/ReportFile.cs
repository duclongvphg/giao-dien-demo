using System;

namespace giao_dien_demo.Models
{
    public class ReportFile
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;     // Tên/Tiêu đề báo cáo

        // 🔥 ĐÃ THÊM: Cột lưu Phân loại báo cáo (Ví dụ: Báo cáo lương, Báo cáo nhân sự...)
        public string Category { get; set; } = string.Empty;

        public string FileName { get; set; } = string.Empty;  // Tên file gốc (VD: baocao.pdf)
        public string FilePath { get; set; } = string.Empty;  // Đường dẫn lưu trong máy
        public string UploadedBy { get; set; } = string.Empty;// Người up (HR)
        public DateTime UploadedDate { get; set; } = DateTime.Now;
    }
}