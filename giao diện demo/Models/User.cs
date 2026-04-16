using System;
using System.ComponentModel.DataAnnotations;

namespace giao_dien_demo.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên đăng nhập không được để trống")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu không được để trống")]
        public string Password { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        // ✅ EMAIL DÙNG ĐỂ NHẬN MÃ KHÔI PHỤC
        [Required(ErrorMessage = "Vui lòng nhập Email")]
        [EmailAddress(ErrorMessage = "Địa chỉ Email không hợp lệ")]
        public string Email { get; set; } = string.Empty;

        public string Role { get; set; } = "Employee";

        public bool IsOnline { get; set; } = false;

        public DateTime? LastActive { get; set; }

        // ✅ QUYỀN ĐĂNG NHẬP (Mặc định là false, Admin phải duyệt)
        public bool CanLogin { get; set; } = false;
    }
}