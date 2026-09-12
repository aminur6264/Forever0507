using System.Globalization;

namespace Forever0507App.Models;

public class EventOptions
{
    public const string SectionName = "Event";

    /// <summary>Short community/brand name shown in the navbar and titles, e.g. "ব্যাচ-২০০৫ পরিবার".</summary>
    public string CommunityName { get; set; } = "";
    public string EventName { get; set; } = "";
    public string Tagline { get; set; } = "";

    /// <summary>e.g. "এসএসসি ২০০৫ · এইচএসসি ২০০৭".</summary>
    public string CohortLine { get; set; } = "";

    /// <summary>Fixed SSC year for this cohort's registrations (2005).</summary>
    public int SscYear { get; set; }

    public string EstablishedNote { get; set; } = "";

    /// <summary>Asia/Dhaka wall-clock start time (kept offset-free so config binding never shifts it to the server's time zone).</summary>
    public DateTime StartTime { get; set; }
    public string TimeZoneOffset { get; set; } = "+06:00";

    public string Venue { get; set; } = "";
    public string Address { get; set; } = "";
    public string ContactPhone { get; set; } = "";
    public string ContactEmail { get; set; } = "";

    /// <summary>Shown on the landing page, form and registration card.</summary>
    public string PayNote { get; set; } = "";

    /// <summary>Mobile-banking accounts money can be sent to, e.g. "বিকাশ (পার্সোনাল): 01700-000000".</summary>
    public string[] PaymentAccounts { get; set; } = [];

    /// <summary>Curated dropdown list; registrants can add their own school name (persisted for future dropdowns).</summary>
    public string[] Schools { get; set; } = [];

    private static readonly CultureInfo BengaliCulture = new("bn-BD");

    /// <summary>Deterministic ISO-8601 string handed to the JS countdown, e.g. "2026-11-06T10:00:00+06:00".</summary>
    public string StartTimeIso => $"{StartTime:yyyy-MM-ddTHH:mm:ss}{TimeZoneOffset}";

    /// <summary>Human-readable Bengali start time, e.g. "০৬ নভেম্বর ২০২৬, শুক্রবার".</summary>
    public string StartTimeBn =>
        Helpers.BengaliText.ToBengaliDigits(StartTime.ToString("dd MMMM yyyy", BengaliCulture)) +
        ", " + StartTime.ToString("dddd", BengaliCulture);

    /// <summary>e.g. "সকাল ১০:০০".</summary>
    public string StartTimeOnlyBn =>
        Helpers.BengaliText.ToBengaliDigits(StartTime.ToString("hh:mm", BengaliCulture)) +
        (StartTime.Hour < 12 ? " সকাল" : " বিকাল");

    /// <summary>Year used in registration numbers (REG-2026-####).</summary>
    public int RegistrationYear => StartTime.Year;
}
