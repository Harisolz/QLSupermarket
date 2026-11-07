
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using QLSupermarket.Models;

var builder = WebApplication.CreateBuilder(args);

// 1. Kết nối SQL Server
var connectionString = builder.Configuration
    .GetConnectionString("QlsupermarketDbContext")
    ?? throw new InvalidOperationException(
        "Connection string 'QlsupermarketDbContext' not found.");

builder.Services.AddDbContext<QlsupermarketDbContext>(options =>
    options.UseSqlServer(connectionString));

// 2. Đăng ký MVC
builder.Services.AddControllersWithViews();

// 3. Cấu hình Cookie Authentication
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";

        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;

        options.Cookie.Name = "QLSupermarket.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

// 4. Phân quyền
builder.Services.AddAuthorization();

// 5. Session
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// 6. Cấu hình môi trường
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// 7. Middleware
app.UseHttpsRedirection();

app.UseRouting();

app.UseSession();

// Authentication phải nằm trước Authorization
app.UseAuthentication();
app.UseAuthorization();

// 8. Static files (.NET 10)
app.MapStaticAssets();

// 9. Route mặc định
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}")
    .WithStaticAssets();

app.Run();
