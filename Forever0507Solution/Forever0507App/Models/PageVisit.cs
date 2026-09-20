using System.ComponentModel.DataAnnotations;

namespace Forever0507App.Models;

/// <summary>One row per page open on the site — the URL, the visitor's IP (IpAddresses.Id),
/// and the moment of the visit. Analytics/record-keeping only: never rendered to visitors.</summary>
public class PageVisit
{
    /// <summary>bigint identity — the site may log a lot of visits.</summary>
    public long Id { get; set; }

    /// <summary>Request path + query string, e.g. /Registration/Card/REG-2026-0001?print=true.</summary>
    [MaxLength(200)]
    public string Url { get; set; } = "";

    /// <summary>Visitor's IP, reusing the distinct-address rows in IpAddresses; null if unknown.</summary>
    public int? IpId { get; set; }

    /// <summary>Logged-in account (AppUsers.Id); null for anonymous visitors. Deliberately no FK
    /// so deleting a user never blocks on their past visits. The static admin has no AppUsers
    /// row, so its visits carry null here.</summary>
    public int? UserId { get; set; }

    /// <summary>Visit moment in Bangladesh time (UTC+6, fixed offset — BD has no DST). NOT UTC.</summary>
    public DateTime VisitTime { get; set; }
}
