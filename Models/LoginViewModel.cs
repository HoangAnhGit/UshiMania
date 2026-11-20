using System.ComponentModel.DataAnnotations;

namespace UshiMania.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập mã hoặc tên đăng nhập")]
        [Display(Name = "Mã")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; } = string.Empty;

        public string? ReturnUrl { get; set; }
    }

    public class UserAccount
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty; // QUAN_LY, PHUC_VU, BEP, THU_NGAN
        public string RoleDisplayName { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string PlainPassword { get; set; } = string.Empty; // Để hỗ trợ đăng nhập test nhanh
        public string? QuickPin { get; set; } // Mã PIN bàn phím số POS (vd: 1234, 1111)
        public string Phone { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }
}
