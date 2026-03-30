namespace giao_dien_demo.Models
{
    public class Attendance
    {
        public int Id { get; set; }
        public string? EmployeeName { get; set; }
        public DateTime CheckIn { get; set; }
        public DateTime? CheckOut { get; set; }
    }
}