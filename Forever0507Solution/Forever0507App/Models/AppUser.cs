using System.ComponentModel.DataAnnotations;

namespace Forever0507App.Models;

/// <summary>A regular system user. The username is their phone number; the admin is static and has no row here.</summary>
public class AppUser
{
    public int Id { get; set; }

    /// <summary>Login username — an 11-digit BD mobile number.</summary>
    [MaxLength(11)]
    public string Phone { get; set; } = "";

    [MaxLength(500)]
    public string PasswordHash { get; set; } = "";

    /// <summary>True right after account creation or an admin password reset — the user must set a new password at next login.</summary>
    public bool MustChangePassword { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
