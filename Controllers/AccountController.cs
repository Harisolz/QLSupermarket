
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLSupermarket.Models;
using QLSupermarket.ViewModels;

namespace QLSupermarket.Controllers
{
    public class AccountController : Controller
    {
        private readonly QlsupermarketDbContext _context;
        private readonly PasswordHasher<User> _passwordHasher = new();

        public AccountController(QlsupermarketDbContext context)
        {
            _context = context;
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                if (User.IsInRole("Admin"))
                    return RedirectToAction("Index", "Home");

                return RedirectToAction("Index", "Customer");
            }

            return View(new LoginViewModel());
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Username == model.Username);

            if (user == null || user.Role == null)
            {
                ModelState.AddModelError("",
                    "Tên đăng nhập hoặc mật khẩu không đúng.");
                return View(model);
            }

            bool validPassword = false;

            try
            {
                var result = _passwordHasher.VerifyHashedPassword(
                    user,
                    user.PasswordHash,
                    model.Password
                );

                validPassword =
                    result != PasswordVerificationResult.Failed;
            }
            catch (FormatException)
            {
                validPassword = false;
            }

            if (!validPassword)
            {
                ModelState.AddModelError("",
                    "Tên đăng nhập hoặc mật khẩu không đúng.");
                return View(model);
            }

            // Chỉ cho phép Admin và Customer đăng nhập
            if (user.Role.RoleName != "Admin" &&
                user.Role.RoleName != "Customer")
            {
                ModelState.AddModelError("",
                    "Tài khoản không có quyền đăng nhập.");
                return View(model);
            }

            var claims = new List<Claim>
            {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    user.UserId.ToString()),

                new Claim(
                    ClaimTypes.Name,
                    user.Username),

                new Claim(
                    ClaimTypes.Role,
                    user.Role.RoleName)
            };

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme);

            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(
                        model.RememberMe ? 24 * 7 : 8)
                });

            // Chuyển hướng theo vai trò
            if (user.Role.RoleName == "Admin")
            {
                return RedirectToAction("Index", "Home");
            }

            return RedirectToAction("Index", "Customer");
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return StatusCode(403,
                "Bạn không có quyền truy cập chức năng này.");
        }
    }
}
