using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CivilWeb.Data;
using CivilWeb.Models;

namespace CivilWeb.Controllers;

[AllowAnonymous]
public class AccountController : Controller
{
    private readonly ApplicationDbContext _context;

    public AccountController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: Account/Login
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        // If already logged in, redirect to Civil/Index
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Civil");
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    // POST: Account/Login
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Find user in database
        var inputUsername = model.Username?.Trim() ?? "";
        var user = await _context.UserPermissions
            .FirstOrDefaultAsync(u => u.Username.ToLower() == inputUsername.ToLower() && u.IsActive);

        if (user == null)
        {
            ModelState.AddModelError(string.Empty, GetLoginErrorMessage());
            return View(model);
        }

        // Verify password
        if (string.IsNullOrEmpty(user.PasswordHash) || !VerifyPassword(model.Password, user.PasswordHash))
        {
            ModelState.AddModelError(string.Empty, GetLoginErrorMessage());
            return View(model);
        }

        // Create claims
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("DisplayName", user.DisplayName ?? user.Username)
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        var authProperties = new AuthenticationProperties
        {
            IsPersistent = model.RememberMe,
            ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(30) : DateTimeOffset.UtcNow.AddHours(8)
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

        // Update login stats
        user.LastLoginDate = DateTime.Now;
        user.LoginCount++;
        await _context.SaveChangesAsync();

        // Redirect to return URL or default
        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return Redirect(model.ReturnUrl);
        }

        return RedirectToAction("Index", "Civil");
    }

    // POST: Account/Logout
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }

    // GET: Account/Logout (for link-based logout)
    [HttpGet]
    public async Task<IActionResult> LogoutGet()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }

    // Helper: Hash password using SHA256
    public static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }

    // Helper: Verify password
    private static bool VerifyPassword(string password, string hash)
    {
        var passwordHash = HashPassword(password);
        return passwordHash == hash;
    }

    // Helper: Get localized error message
    private string GetLoginErrorMessage()
    {
        var lang = Request.Cookies["Language"] ?? "ar";
        return lang == "ar"
            ? "اسم المستخدم أو كلمة المرور غير صحيحة"
            : "Invalid username or password";
    }
}
