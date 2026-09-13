namespace Forever0507App.Models;

/// <summary>Auth roles, claim names, and the static admin identity (no DB row, no credentials by design).</summary>
public static class AuthConstants
{
    public const string AdminRole = "Admin";
    public const string RegularRole = "Regular";

    /// <summary>Claim set while the user still must change their password (first login / admin reset).</summary>
    public const string MustChangePasswordClaim = "MustChangePassword";

    public const string AdminLoginName = "admin";
    public const string AdminDisplayName = "সিস্টেম অ্যাডমিন";
}
