using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CivilWeb.Models;

public class UserPermission
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(256)]
    [Display(Name = "Username")]
    public string Username { get; set; } = string.Empty;

    [StringLength(256)]
    [Display(Name = "Password Hash")]
    public string? PasswordHash { get; set; }

    [StringLength(256)]
    [Display(Name = "Display Name")]
    public string? DisplayName { get; set; }

    [Required]
    [StringLength(50)]
    [Display(Name = "Role")]
    public string Role { get; set; } = "User"; // "Admin" or "User"

    [Display(Name = "Is Active")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Created Date")]
    public DateTime CreatedDate { get; set; } = DateTime.Now;

    [Display(Name = "Last Login")]
    public DateTime? LastLoginDate { get; set; }

    [Display(Name = "Login Count")]
    public int LoginCount { get; set; } = 0;

    [StringLength(500)]
    [Display(Name = "Notes")]
    public string? Notes { get; set; }

    // Helper property to check if user is admin
    [NotMapped]
    public bool IsAdmin => Role == "Admin";
}

// Enum for role types
public static class UserRoles
{
    public const string Admin = "Admin";
    public const string User = "User";
}
