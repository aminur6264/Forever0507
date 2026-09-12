using System.Globalization;

namespace Forever0507App.Models;

public class EventOptions
{
    public const string SectionName = "Event";

    public string SchoolName { get; set; } = "";
    public string EventName { get; set; } = "";
    public string Tagline { get; set; } = "";
    public int EstablishedYear { get; set; }

    /// <summary>Asia/Dhaka wall-clock start time (kept offset-free so config binding never shifts it to the server's time zone).</summary>
    public DateTime StartTime { get; set; }
    public string TimeZoneOffset { get; set; } = "+06:00";

    public string Venue { get; set; } = "";
    public string Address { get; set; } = "";
    public string ContactPhone { get; set; } = "";
    public string ContactEmail { get; set; } = "";
    public string PayNote { get; set; } = "";

    private static readonly CultureInfo BengaliCulture = new("bn-BD");

    /// <summary>Deterministic ISO-8601 string handed to the JS countdown, e.g. "2027-05-07T10:00:00+06:00".</summary>
    public string StartTimeIso => $"{StartTime:yyyy-MM-ddTHH:mm:ss}{TimeZoneOffset}";

    /// <summary>Human-readable Bengali start time, e.g. "০৭ মে ২০২৭, শুক্রবার".</summary>
    public string StartTimeBn =>
        Helpers.BengaliText.ToBengaliDigits(StartTime.ToString("dd MMMM yyyy", BengaliCulture)) +
        ", " + StartTime.ToString("dddd", BengaliCulture);

    /// <summary>e.g. "সকাল ১০:০০".</summary>
    public string StartTimeOnlyBn =>
        Helpers.BengaliText.ToBengaliDigits(StartTime.ToString("hh:mm", BengaliCulture)) +
        (StartTime.Hour < 12 ? " সকাল" : " বিকাল");
}
