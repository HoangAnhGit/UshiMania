using Microsoft.AspNetCore.Authentication.Cookies;
using UshiMania.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();
builder.Services.AddScoped<UshiMania.DAL.DbHelper>();
builder.Services.AddScoped<UshiMania.DAL.BanAnDAL>();
builder.Services.AddScoped<UshiMania.BLL.BanAnBLL>();
builder.Services.AddScoped<UshiMania.DAL.OrderDAL>();
builder.Services.AddScoped<UshiMania.BLL.OrderBLL>();
builder.Services.AddScoped<UshiMania.DAL.HoaDonKhoDAL>();
builder.Services.AddScoped<UshiMania.BLL.HoaDonKhoBLL>();

// Đăng ký dịch vụ xác thực tài khoản Ushi Mania
builder.Services.AddSingleton<IAuthService, AuthService>();

// Cấu hình xác thực Cookie Authentication & Phân quyền Roles
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Home/DangNhap";
        options.LogoutPath = "/Home/Logout";
        options.AccessDeniedPath = "/Home/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

// Bật Middleware Session & Xác thực phân quyền
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();
app.MapControllers();
app.MapHub<UshiMania.Hubs.RestaurantHub>("/restaurantHub");

app.Run();
