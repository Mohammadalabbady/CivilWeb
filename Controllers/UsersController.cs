using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using CivilWeb.Data;
using CivilWeb.Models;

namespace CivilWeb.Controllers;

[Authorize(Policy = "Administrator")]
public class UsersController : Controller
{
    private readonly ApplicationDbContext _context;

    public UsersController(ApplicationDbContext context)
    {
        _context = context;
    }

    // Helper: Hash password using SHA256
    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }

    // GET: Users
    public async Task<IActionResult> Index()
    {
        var users = await _context.UserPermissions
            .OrderByDescending(u => u.Role == "Admin")
            .ThenBy(u => u.Username)
            .ToListAsync();

        // Get statistics
        ViewData["TotalUsers"] = users.Count;
        ViewData["AdminCount"] = users.Count(u => u.Role == "Admin");
        ViewData["UserCount"] = users.Count(u => u.Role == "User");
        ViewData["ActiveCount"] = users.Count(u => u.IsActive);
        ViewData["InactiveCount"] = users.Count(u => !u.IsActive);

        return View(users);
    }

    // GET: Users/Create
    public IActionResult Create()
    {
        return View(new UserPermission());
    }

    // POST: Users/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserPermission userPermission, string? NewPassword)
    {
        // Normalize username
        userPermission.Username = userPermission.Username?.Trim() ?? "";

        // Check if username already exists (case-insensitive)
        if (await _context.UserPermissions.AnyAsync(u => u.Username.ToLower() == userPermission.Username.ToLower()))
        {
            ModelState.AddModelError("Username", "This username already exists.");
        }

        // Validate password
        if (string.IsNullOrEmpty(NewPassword))
        {
            ModelState.AddModelError("NewPassword", "Password is required for new users.");
        }
        else if (NewPassword.Length < 4)
        {
            ModelState.AddModelError("NewPassword", "Password must be at least 4 characters.");
        }

        if (ModelState.IsValid)
        {
            userPermission.CreatedDate = DateTime.Now;
            userPermission.PasswordHash = HashPassword(NewPassword!);
            _context.Add(userPermission);
            await _context.SaveChangesAsync();
            TempData["Success"] = "User added successfully.";
            return RedirectToAction(nameof(Index));
        }
        return View(userPermission);
    }

    // GET: Users/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var userPermission = await _context.UserPermissions.FindAsync(id);
        if (userPermission == null)
        {
            return NotFound();
        }
        return View(userPermission);
    }

    // POST: Users/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, UserPermission userPermission, string? NewPassword)
    {
        if (id != userPermission.Id)
        {
            return NotFound();
        }

        // Get original record to preserve some fields
        var original = await _context.UserPermissions.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id);

        if (original == null)
        {
            return NotFound();
        }

        // Normalize username
        userPermission.Username = userPermission.Username?.Trim() ?? "";

        // Check if username is being changed and if new username already exists (case-insensitive)
        if (!original.Username.Equals(userPermission.Username, StringComparison.OrdinalIgnoreCase))
        {
            if (await _context.UserPermissions.AnyAsync(u => u.Username.ToLower() == userPermission.Username.ToLower() && u.Id != id))
            {
                ModelState.AddModelError("Username", "This username already exists.");
            }
        }

        // Validate password if provided
        if (!string.IsNullOrEmpty(NewPassword) && NewPassword.Length < 4)
        {
            ModelState.AddModelError("NewPassword", "Password must be at least 4 characters.");
        }

        if (ModelState.IsValid)
        {
            try
            {
                // Preserve original created date and login info
                userPermission.CreatedDate = original.CreatedDate;
                userPermission.LastLoginDate = original.LastLoginDate;
                userPermission.LoginCount = original.LoginCount;

                // Update password if provided, otherwise keep original
                if (!string.IsNullOrEmpty(NewPassword))
                {
                    userPermission.PasswordHash = HashPassword(NewPassword);
                }
                else
                {
                    userPermission.PasswordHash = original.PasswordHash;
                }

                _context.Update(userPermission);
                await _context.SaveChangesAsync();
                TempData["Success"] = !string.IsNullOrEmpty(NewPassword)
                    ? "User updated and password changed successfully."
                    : "User updated successfully.";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserPermissionExists(userPermission.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }
        return View(userPermission);
    }

    // GET: Users/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var userPermission = await _context.UserPermissions
            .FirstOrDefaultAsync(m => m.Id == id);
        if (userPermission == null)
        {
            return NotFound();
        }

        return View(userPermission);
    }

    // POST: Users/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var userPermission = await _context.UserPermissions.FindAsync(id);

        // Prevent deleting the current user
        var currentUsername = User.Identity?.Name;
        if (userPermission != null && userPermission.Username.Equals(currentUsername, StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "You cannot delete your own account.";
            return RedirectToAction(nameof(Index));
        }

        if (userPermission != null)
        {
            _context.UserPermissions.Remove(userPermission);
            await _context.SaveChangesAsync();
            TempData["Success"] = "User deleted successfully.";
        }

        return RedirectToAction(nameof(Index));
    }

    // Toggle user active status
    [HttpPost]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var userPermission = await _context.UserPermissions.FindAsync(id);

        // Prevent deactivating the current user
        var currentUsername = User.Identity?.Name;
        if (userPermission != null && userPermission.Username.Equals(currentUsername, StringComparison.OrdinalIgnoreCase))
        {
            return Json(new { success = false, message = "You cannot deactivate your own account." });
        }

        if (userPermission != null)
        {
            userPermission.IsActive = !userPermission.IsActive;
            await _context.SaveChangesAsync();
            return Json(new { success = true, isActive = userPermission.IsActive });
        }

        return Json(new { success = false, message = "User not found." });
    }

    private bool UserPermissionExists(int id)
    {
        return _context.UserPermissions.Any(e => e.Id == id);
    }
}
