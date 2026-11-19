var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<UshiMania.DAL.DbHelper>();
builder.Services.AddScoped<UshiMania.DAL.BanAnDAL>();
builder.Services.AddScoped<UshiMania.BLL.BanAnBLL>();
builder.Services.AddScoped<UshiMania.DAL.OrderDAL>();
builder.Services.AddScoped<UshiMania.BLL.OrderBLL>();
builder.Services.AddScoped<UshiMania.DAL.HoaDonKhoDAL>();
builder.Services.AddScoped<UshiMania.BLL.HoaDonKhoBLL>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
