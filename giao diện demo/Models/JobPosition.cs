namespace giao_dien_demo.Models
{
    public class JobPosition
    {
        public int Id { get; set; }

        // Tên nhóm chức vụ (VD: Ban Giám đốc, Cấp Quản lý, Cấp Nhân viên)
        public string? Group { get; set; }

        // Tên chức vụ cụ thể (VD: Lập trình viên Senior)
        public string? Name { get; set; }

        // Mức lương cơ bản tối thiểu chuẩn (VNĐ)
        public decimal MinSalary { get; set; }
    }
}