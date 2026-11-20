using System.Security.Cryptography;
using System.Text;
using UshiMania.Models;

namespace UshiMania.Services
{
    public interface IDichVuXacThuc
    {
        Task<TaiKhoanNguoiDung?> ValidateUserAsync(string username, string password);
        TaiKhoanNguoiDung? GetUserByUsername(string username);
        string ComputeSha256Hash(string rawData);
    }

    public class DichVuXacThuc : IDichVuXacThuc
    {
        private static readonly List<TaiKhoanNguoiDung> DanhSachTaiKhoanMau = new()
        {
            new TaiKhoanNguoiDung
            {
                Id = 1,
                Username = "quanly",
                FullName = "Nguyễn Minh Anh",
                Role = "QUAN_LY",
                RoleDisplayName = "Quản lý nhà hàng",
                PlainPassword = "QuanLy@123",
                QuickPin = "1234",
                Phone = "0901000001",
                PasswordHash = ComputeHashStatic("QuanLy@123")
            },
            new TaiKhoanNguoiDung
            {
                Id = 2,
                Username = "phucvu",
                FullName = "Trần Thảo Vy",
                Role = "PHUC_VU",
                RoleDisplayName = "Nhân viên phục vụ",
                PlainPassword = "PhucVu@123",
                QuickPin = "1111",
                Phone = "0901000002",
                PasswordHash = ComputeHashStatic("PhucVu@123")
            },
            new TaiKhoanNguoiDung
            {
                Id = 3,
                Username = "bep",
                FullName = "Lê Quốc Bảo",
                Role = "BEP",
                RoleDisplayName = "Đầu bếp chính",
                PlainPassword = "Bep@123",
                QuickPin = "3333",
                Phone = "0901000003",
                PasswordHash = ComputeHashStatic("Bep@123")
            },
            new TaiKhoanNguoiDung
            {
                Id = 4,
                Username = "thungan",
                FullName = "Phạm Ngọc Linh",
                Role = "THU_NGAN",
                RoleDisplayName = "Thu ngân",
                PlainPassword = "ThuNgan@123",
                QuickPin = "2222",
                Phone = "0901000004",
                PasswordHash = ComputeHashStatic("ThuNgan@123")
            }
        };

        public Task<TaiKhoanNguoiDung?> ValidateUserAsync(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return Task.FromResult<TaiKhoanNguoiDung?>(null);
            }

            var cleanUser = username.Trim().ToLowerInvariant();
            var cleanPass = password.Trim();
            var hashedInput = ComputeHashStatic(cleanPass);

            var user = DanhSachTaiKhoanMau.FirstOrDefault(u =>
                u.Username.Equals(cleanUser, StringComparison.OrdinalIgnoreCase) ||
                u.Id.ToString() == cleanUser ||
                u.QuickPin == cleanUser);

            if (user == null)
            {
                return Task.FromResult<TaiKhoanNguoiDung?>(null);
            }

            bool isPasswordValid =
                string.Equals(user.PasswordHash, hashedInput, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(user.PlainPassword, cleanPass, StringComparison.Ordinal) ||
                (user.QuickPin != null && string.Equals(user.QuickPin, cleanPass, StringComparison.Ordinal));

            return Task.FromResult(isPasswordValid ? user : null);
        }

        public TaiKhoanNguoiDung? GetUserByUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username)) return null;
            return DanhSachTaiKhoanMau.FirstOrDefault(u => u.Username.Equals(username.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        public string ComputeSha256Hash(string rawData)
        {
            return ComputeHashStatic(rawData);
        }

        private static string ComputeHashStatic(string rawData)
        {
            byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawData));
            return Convert.ToHexString(bytes).ToUpperInvariant();
        }
    }
}
