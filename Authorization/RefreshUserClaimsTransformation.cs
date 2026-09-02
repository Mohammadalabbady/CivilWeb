using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using CivilWeb.Data;

namespace CivilWeb.Authorization;

/// <summary>
/// Refreshes user claims from database on every request.
/// This ensures permission changes take effect immediately.
/// </summary>
public class RefreshUserClaimsTransformation : IClaimsTransformation
{
    private readonly ApplicationDbContext _context;

    public RefreshUserClaimsTransformation(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        // Only transform authenticated users
        if (principal.Identity?.IsAuthenticated != true)
        {
            return principal;
        }

        var username = principal.Identity.Name;
        if (string.IsNullOrEmpty(username))
        {
            return principal;
        }

        // Get current user from database
        var user = await _context.UserPermissions
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == username && u.IsActive);

        // If user not found or deactivated, return empty principal (will force re-login)
        if (user == null)
        {
            return new ClaimsPrincipal();
        }

        // Create new claims with fresh data from database
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("DisplayName", user.DisplayName ?? user.Username)
        };

        var identity = new ClaimsIdentity(claims, principal.Identity.AuthenticationType);
        return new ClaimsPrincipal(identity);
    }
}
