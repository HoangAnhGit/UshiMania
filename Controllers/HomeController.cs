using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UshiMania.Models;
using UshiMania.Services;

namespace UshiMania.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IAuthService _authService;

        public HomeController(ILogger<HomeController> logger, IAuthService authService)
        {
            _logger = logger;
            _authService = authService;
        }

        // GET: / hoặc /Home/Index -> Điều hướng đến màn hình chính theo quyền hạn nhân viên
        [Authorize]
        [HttpGet]
        public IActionResult Index()
        {
            if (User.IsInRole("BEP"))
            {
                return RedirectToAction(nameof(ManHinhBep));
            }

            // Quản lý, Phục vụ, Thu ngân mặc định vào Sơ đồ bàn
            return RedirectToAction(nameof(SoDoBan));
        }

        // GET: /Home/DangNhap -> Giao diện POS Đăng nhập
        [AllowAnonymous]
        [HttpGet]
        public IActionResult DangNhap([FromQuery] string? returnUrl = null)
        {
            // Nếu đã đăng nhập thì chuyển hướng thẳng đến trang được phép truy cập
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                if (User.IsInRole("BEP"))
                {
                    return RedirectToAction(nameof(ManHinhBep));
                }

                return RedirectToAction(nameof(SoDoBan));
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        // POST: Xử lý Đăng nhập qua Form
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.ErrorMessage = "Vui lòng nhập đầy đủ mã và mật khẩu.";
                return View("DangNhap", model);
            }

            var user = await _authService.ValidateUserAsync(model.Username, model.Password);
            if (user == null)
            {
                ViewBag.ErrorMessage = "Mã nhân viên hoặc mật khẩu không chính xác.";
                return View("DangNhap", model);
            }

            await SignInUserAsync(user);

            _logger.LogInformation("Nhân viên {Username} ({Role}) đăng nhập thành công.", user.Username, user.Role);

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }

            return Redirect(GetDefaultRedirectUrlForRole(user.Role));
        }

        // POST: Xử lý Đăng nhập qua AJAX (Dành cho màn hình cảm ứng POS Kiosk)
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> LoginApi([FromBody] LoginViewModel model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Username) || string.IsNullOrWhiteSpace(model.Password))
            {
                return Json(new { success = false, message = "Vui lòng nhập mã và mật khẩu." });
            }

            var user = await _authService.ValidateUserAsync(model.Username, model.Password);
            if (user == null)
            {
                return Json(new { success = false, message = "Mã nhân viên hoặc mật khẩu không đúng." });
            }

            await SignInUserAsync(user);

            _logger.LogInformation("Nhân viên {Username} ({Role}) đăng nhập qua POS API.", user.Username, user.Role);

            var targetUrl = !string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl)
                ? model.ReturnUrl
                : GetDefaultRedirectUrlForRole(user.Role);

            return Json(new
            {
                success = true,
                message = $"Đăng nhập thành công! Xin chào {user.FullName}",
                fullName = user.FullName,
                role = user.Role,
                roleName = user.RoleDisplayName,
                redirectUrl = targetUrl
            });
        }

        // 1. Sơ đồ bàn: Quản lý, Phục vụ, Thu ngân
        [Authorize(Roles = "QUAN_LY,PHUC_VU,THU_NGAN")]
        [HttpGet]
        public IActionResult SoDoBan()
        {
            return View();
        }

        // 2. Order POS: Quản lý, Phục vụ, Thu ngân
        [Authorize(Roles = "QUAN_LY,PHUC_VU,THU_NGAN")]
        [HttpGet]
        public IActionResult OrderPOS()
        {
            return View();
        }

        // 3. Màn hình Bếp KDS: Quản lý, Bếp
        [Authorize(Roles = "QUAN_LY,BEP")]
        [HttpGet]
        public IActionResult ManHinhBep()
        {
            return View();
        }

        // 4. Kho & Báo cáo: Quản lý, Thu ngân, Bếp
        [Authorize(Roles = "QUAN_LY,THU_NGAN,BEP")]
        [HttpGet]
        public IActionResult KhoBaoCao()
        {
            return View();
        }

        // Khách gọi món qua QR (Khách hàng dùng, không yêu cầu nhân viên đăng nhập)
        [AllowAnonymous]
        [HttpGet]
        public IActionResult GuestOrder([FromQuery] int maBan)
        {
            if (maBan <= 0)
            {
                return BadRequest();
            }

            return View();
        }

        // Đăng xuất khỏi hệ thống POS
        [Authorize]
        [HttpPost]
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            return RedirectToAction(nameof(DangNhap));
        }

        // Trang thông báo không đủ quyền hạn truy cập
        [AllowAnonymous]
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [AllowAnonymous]
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        // Helper cấp phát Cookie xác thực kèm Role
        private async Task SignInUserAsync(UserAccount user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim("Username", user.Username),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("RoleDisplayName", user.RoleDisplayName),
                new Claim(ClaimTypes.MobilePhone, user.Phone ?? "")
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);
        }

        // Helper xác định trang mặc định theo vai trò nhân viên
        private string GetDefaultRedirectUrlForRole(string role)
        {
            return role switch
            {
                "BEP" => Url.Action(nameof(ManHinhBep), "Home") ?? "/Home/ManHinhBep",
                _ => Url.Action(nameof(SoDoBan), "Home") ?? "/Home/SoDoBan"
            };
        }
    }
}
