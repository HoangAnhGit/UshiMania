using System.ComponentModel.DataAnnotations;

namespace UshiMania.Models
{
    public class DangNhapViewModel
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

    public class LoginViewModel : DangNhapViewModel { }

    public class TaiKhoanNguoiDung
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string RoleDisplayName { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string PlainPassword { get; set; } = string.Empty;
        public string? QuickPin { get; set; }
        public string Phone { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }

    public class UserAccount : TaiKhoanNguoiDung { }
}
