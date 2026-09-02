using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using CivilWeb.Data;
using CivilWeb.Models;

namespace CivilWeb.Controllers;

[Authorize] // Require authentication for all actions
public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;

    public HomeController(ApplicationDbContext context)
    {
        _context = context;
    }

    // Dashboard - accessible to all authenticated users
    public async Task<IActionResult> Index()
    {
        // Get dashboard statistics
        var totalRecords = await _context.CivilData.CountAsync();
        var maleCount = await _context.CivilData.CountAsync(c => c.Gender == "Male" || c.Gender == "ذكر" || c.Gender == "ذكر " || (c.Gender != null && c.Gender.Trim() == "ذكر"));
        var femaleCount = await _context.CivilData.CountAsync(c => c.Gender == "Female" || c.Gender == "أنثى" || c.Gender == "انثى" || c.Gender == "انثي" || c.Gender == "أنثي" || c.Gender == "اثنى" || (c.Gender != null && (c.Gender.Trim() == "انثى" || c.Gender.Trim() == "أنثى" || c.Gender.Trim() == "انثي" || c.Gender.Contains("نث"))));
        var aliveCount = await _context.CivilData.CountAsync(c => c.Alive == true);
        var deceasedCount = await _context.CivilData.CountAsync(c => c.Alive == false);

        // Get recent records
        var recentRecords = await _context.CivilData
            .AsNoTracking()
            .OrderByDescending(c => c.Id)
            .Take(5)
            .ToListAsync();

        ViewData["TotalRecords"] = totalRecords;
        ViewData["MaleCount"] = maleCount;
        ViewData["FemaleCount"] = femaleCount;
        ViewData["AliveCount"] = aliveCount;
        ViewData["DeceasedCount"] = deceasedCount;
        ViewData["RecentRecords"] = recentRecords;

        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [AllowAnonymous] // Error page should be accessible to everyone
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    // Access Denied page for unauthorized users
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }

    // Sign Out - API endpoint that returns 401 to clear credentials
    [AllowAnonymous]
    public new IActionResult SignOut()
    {
        Response.Headers["Clear-Site-Data"] = "\"cookies\", \"storage\"";
        return StatusCode(401);
    }

    // Sign Out page with JavaScript to handle credential clearing
    [AllowAnonymous]
    public IActionResult SignOutPage()
    {
        return View();
    }

    // Welcome/Landing page - accessible to all authenticated users
    public IActionResult Welcome()
    {
        return View();
    }

    // Dashboard - accessible to all authenticated users
    public async Task<IActionResult> Dashboard()
    {
        // Get dashboard statistics
        var totalRecords = await _context.CivilData.CountAsync();
        var maleCount = await _context.CivilData.CountAsync(c => c.Gender == "Male" || c.Gender == "ذكر" || c.Gender == "ذكر " || (c.Gender != null && c.Gender.Trim() == "ذكر"));
        var femaleCount = await _context.CivilData.CountAsync(c => c.Gender == "Female" || c.Gender == "أنثى" || c.Gender == "انثى" || c.Gender == "انثي" || c.Gender == "أنثي" || c.Gender == "اثنى" || (c.Gender != null && (c.Gender.Trim() == "انثى" || c.Gender.Trim() == "أنثى" || c.Gender.Trim() == "انثي" || c.Gender.Contains("نث"))));
        var aliveCount = await _context.CivilData.CountAsync(c => c.Alive == true);
        var deceasedCount = await _context.CivilData.CountAsync(c => c.Alive == false);

        // Get total users count
        var totalUsers = await _context.UserPermissions.CountAsync();
        var activeUsers = await _context.UserPermissions.CountAsync(u => u.IsActive);

        ViewData["TotalRecords"] = totalRecords;
        ViewData["MaleCount"] = maleCount;
        ViewData["FemaleCount"] = femaleCount;
        ViewData["AliveCount"] = aliveCount;
        ViewData["DeceasedCount"] = deceasedCount;
        ViewData["TotalUsers"] = totalUsers;
        ViewData["ActiveUsers"] = activeUsers;

        return View();
    }
}
