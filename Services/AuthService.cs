using System.Security.Cryptography;
using System.Text;
using UshiMania.Models;

namespace UshiMania.Services
{
    public interface IAuthService
    {
        Task<UserAccount?> ValidateUserAsync(string username, string password);
        UserAccount? GetUserByUsername(string username);
        string ComputeSha256Hash(string rawData);
    }

    public class AuthService : IAuthService
    {
        private static readonly List<UserAccount> SeedUsers = new()
        {
            new UserAccount
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
            new UserAccount
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
            new UserAccount
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
            new UserAccount
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

        public Task<UserAccount?> ValidateUserAsync(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return Task.FromResult<UserAccount?>(null);
            }

            var cleanUser = username.Trim().ToLowerInvariant();
            var cleanPass = password.Trim();
            var hashedInput = ComputeHashStatic(cleanPass);

            // Tìm theo username hoặc mã nhân viên
            var user = SeedUsers.FirstOrDefault(u =>
                u.Username.Equals(cleanUser, StringComparison.OrdinalIgnoreCase) ||
                u.Id.ToString() == cleanUser ||
                u.QuickPin == cleanUser);

            if (user == null)
            {
                return Task.FromResult<UserAccount?>(null);
            }

            // Kiểm tra mật khẩu (hỗ trợ hash SHA256, plaintext mật khẩu gốc, hoặc mã PIN nhanh)
            bool isPasswordValid =
                string.Equals(user.PasswordHash, hashedInput, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(user.PlainPassword, cleanPass, StringComparison.Ordinal) ||
                (user.QuickPin != null && string.Equals(user.QuickPin, cleanPass, StringComparison.Ordinal));

            return Task.FromResult(isPasswordValid ? user : null);
        }

        public UserAccount? GetUserByUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username)) return null;
            return SeedUsers.FirstOrDefault(u => u.Username.Equals(username.Trim(), StringComparison.OrdinalIgnoreCase));
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
