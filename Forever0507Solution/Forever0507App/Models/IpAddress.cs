using System.ComponentModel.DataAnnotations;

namespace Forever0507App.Models;

/// <summary>IP captured when a registration is submitted — referenced by Registrations.IpAddressId.
/// Record-keeping only: never rendered anywhere in the UI. The geo columns are reserved for a
/// future IP-location lookup and stay null until then.</summary>
public class IpAddress
{
    public int Id { get; set; }

    /// <summary>Dotted-quad IPv4 or full IPv6 text — 45 chars covers the longest IPv6 form.</summary>
    [MaxLength(45)]
    public string Ip { get; set; } = "";

    [MaxLength(80)]
    public string? City { get; set; }

    [MaxLength(80)]
    public string? State { get; set; }

    /// <summary>ISO short code, e.g. BD.</summary>
    [MaxLength(10)]
    public string? CountryShortName { get; set; }

    [MaxLength(80)]
    public string? CountryFullName { get; set; }

    [MaxLength(80)]
    public string? TimeZone { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
