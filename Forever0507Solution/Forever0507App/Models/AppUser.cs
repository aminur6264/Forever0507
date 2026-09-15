using System.ComponentModel.DataAnnotations;

namespace Forever0507App.Models;

/// <summary>A regular system user. The username is their phone number; the admin is static and has no row here.</summary>
public class AppUser
{
    public int Id { get; set; }

    /// <summary>Display name. Required for new accounts; legacy rows may be blank.</summary>
    [MaxLength(120)]
    public string FullName { get; set; } = "";

    /// <summary>Login username — an 11-digit BD mobile number.</summary>
    [MaxLength(11)]
    public string Phone { get; set; } = "";

    [MaxLength(500)]
    public string PasswordHash { get; set; } = "";

    /// <summary>True right after account creation or an admin password reset — the user must set a new password at next login.</summary>
    public bool MustChangePassword { get; set; }

    /// <summary>Admins manage users and registrations; everyone else is a regular user. Default is regular.</summary>
    public bool IsAdmin { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
