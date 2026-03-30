namespace giao_dien_demo.Models
{
    public class Leave
    {
        public int Id { get; set; }
        public string? EmployeeName { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string? Reason { get; set; }
    }
}