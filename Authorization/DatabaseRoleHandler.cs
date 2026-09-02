using Microsoft.AspNetCore.Authorization;
using CivilWeb.Data;
using Microsoft.EntityFrameworkCore;

namespace CivilWeb.Authorization;

public class DatabaseRoleRequirement : IAuthorizationRequirement
{
    public string RequiredRole { get; }

    public DatabaseRoleRequirement(string role)
    {
        RequiredRole = role;
    }
}

public class DatabaseRoleHandler : AuthorizationHandler<DatabaseRoleRequirement>
{
    private readonly IServiceProvider _serviceProvider;

    public DatabaseRoleHandler(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        DatabaseRoleRequirement requirement)
    {
        var username = context.User.Identity?.Name;

        if (string.IsNullOrEmpty(username))
        {
            return; // Not authenticated
        }

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Check if user exists in database and has required role (case-insensitive)
        var userPermission = await dbContext.UserPermissions
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower() && u.IsActive);

        if (userPermission != null)
        {
            // User is in database - check their role
            if (requirement.RequiredRole == "Admin" && userPermission.Role == "Admin")
            {
                context.Succeed(requirement);

                // Update last login (fire and forget)
                _ = UpdateLastLoginAsync(dbContext, userPermission.Id);
            }
            else if (requirement.RequiredRole == "User")
            {
                // Any active user in database has User access
                context.Succeed(requirement);
            }
        }
        else
        {
            // User not in database - check AD groups as fallback
            // BUILTIN\Administrators or Domain Admins get full access
            if (context.User.IsInRole("BUILTIN\\Administrators") ||
                context.User.IsInRole("Domain Admins") ||
                context.User.IsInRole("Administrators"))
            {
                if (requirement.RequiredRole == "Admin" || requirement.RequiredRole == "User")
                {
                    context.Succeed(requirement);

                    // Auto-add to database as Admin
                    _ = AutoAddUserAsync(dbContext, username, "Admin");
                }
            }
        }
    }

    private async Task UpdateLastLoginAsync(ApplicationDbContext dbContext, int userId)
    {
        try
        {
            var user = await dbContext.UserPermissions.FindAsync(userId);
            if (user != null)
            {
                user.LastLoginDate = DateTime.Now;
                user.LoginCount++;
                await dbContext.SaveChangesAsync();
            }
        }
        catch
        {
            // Ignore errors for login tracking
        }
    }

    private async Task AutoAddUserAsync(ApplicationDbContext dbContext, string username, string role)
    {
        try
        {
            // Check if user doesn't already exist (case-insensitive)
            if (!await dbContext.UserPermissions.AnyAsync(u => u.Username.ToLower() == username.ToLower()))
            {
                dbContext.UserPermissions.Add(new Models.UserPermission
                {
                    Username = username,
                    DisplayName = username.Contains("\\") ? username.Split('\\').Last() : username,
                    Role = role,
                    IsActive = true,
                    CreatedDate = DateTime.Now,
                    LastLoginDate = DateTime.Now,
                    LoginCount = 1,
                    Notes = "Auto-added from AD group membership"
                });
                await dbContext.SaveChangesAsync();
            }
        }
        catch
        {
            // Ignore errors for auto-add
        }
    }
}
