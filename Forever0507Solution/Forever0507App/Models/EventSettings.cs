using System.ComponentModel.DataAnnotations;

namespace Forever0507App.Models;

/// <summary>
/// The single row of event/site text that used to live in appsettings.json.
/// Seeded from config the first time the app runs; afterwards it is update-only — the seeder never touches it again.
/// </summary>
public class EventSettings
{
    /// <summary>Always 1 — this table holds exactly one row.</summary>
    public const int SingleRowId = 1;

    public int Id { get; set; }

    [MaxLength(120)]
    public string CommunityName { get; set; } = "";

    [MaxLength(200)]
    public string EventName { get; set; } = "";

    [MaxLength(200)]
    public string Tagline { get; set; } = "";

    [MaxLength(120)]
    public string CohortLine { get; set; } = "";

    public int SscYear { get; set; }

    [MaxLength(400)]
    public string EstablishedNote { get; set; } = "";

    /// <summary>Asia/Dhaka wall-clock start time (offset kept separately in TimeZoneOffset).</summary>
    public DateTime StartTime { get; set; }

    [MaxLength(10)]
    public string TimeZoneOffset { get; set; } = "+06:00";

    [MaxLength(160)]
    public string Venue { get; set; } = "";

    [MaxLength(160)]
    public string Address { get; set; } = "";

    [MaxLength(30)]
    public string ContactPhone { get; set; } = "";

    [MaxLength(120)]
    public string ContactEmail { get; set; } = "";

    [MaxLength(400)]
    public string PayNote { get; set; } = "";

    /// <summary>First-run seed from the appsettings "Event" section.</summary>
    public static EventSettings FromOptions(EventOptions o) => new()
    {
        Id = SingleRowId,
        CommunityName = o.CommunityName,
        EventName = o.EventName,
        Tagline = o.Tagline,
        CohortLine = o.CohortLine,
        SscYear = o.SscYear,
        EstablishedNote = o.EstablishedNote,
        StartTime = o.StartTime,
        TimeZoneOffset = o.TimeZoneOffset,
        Venue = o.Venue,
        Address = o.Address,
        ContactPhone = o.ContactPhone,
        ContactEmail = o.ContactEmail,
        PayNote = o.PayNote,
    };

    /// <summary>Back to the options shape the whole app reads (computed display props come along).</summary>
    public EventOptions ToOptions() => new()
    {
        CommunityName = CommunityName,
        EventName = EventName,
        Tagline = Tagline,
        CohortLine = CohortLine,
        SscYear = SscYear,
        EstablishedNote = EstablishedNote,
        StartTime = StartTime,
        TimeZoneOffset = TimeZoneOffset,
        Venue = Venue,
        Address = Address,
        ContactPhone = ContactPhone,
        ContactEmail = ContactEmail,
        PayNote = PayNote,
    };

    /// <summary>Copies editable fields from the form model onto the stored row.</summary>
    public void UpdateFrom(EventSettingsInputModel m)
    {
        CommunityName = m.CommunityName.Trim();
        EventName = m.EventName.Trim();
        Tagline = m.Tagline.Trim();
        CohortLine = m.CohortLine.Trim();
        SscYear = m.SscYear;
        EstablishedNote = m.EstablishedNote.Trim();
        StartTime = m.StartTime;
        TimeZoneOffset = m.TimeZoneOffset.Trim();
        Venue = m.Venue.Trim();
        Address = m.Address.Trim();
        ContactPhone = m.ContactPhone.Trim();
        ContactEmail = m.ContactEmail.Trim();
        PayNote = m.PayNote.Trim();
    }
}

/// <summary>Form surface for creating/updating the single EventSettings row.</summary>
public class EventSettingsInputModel
{
    [Required(ErrorMessage = "কমিউনিটির নাম লিখুন।")]
    [Display(Name = "কমিউনিটি নাম")]
    public string CommunityName { get; set; } = "";

    [Required(ErrorMessage = "ইভেন্টের নাম লিখুন।")]
    [Display(Name = "ইভেন্টের নাম")]
    public string EventName { get; set; } = "";

    [Required(ErrorMessage = "ট্যাগলাইন লিখুন।")]
    [Display(Name = "ট্যাগলাইন")]
    public string Tagline { get; set; } = "";

    [Required(ErrorMessage = "ব্যাচ লাইন লিখুন।")]
    [Display(Name = "ব্যাচ লাইন")]
    public string CohortLine { get; set; } = "";

    [Range(1990, 2100, ErrorMessage = "সঠিক এসএসসি সাল দিন।")]
    [Display(Name = "এসএসসি সাল")]
    public int SscYear { get; set; }

    [Required(ErrorMessage = "প্রতিষ্ঠা নোট লিখুন।")]
    [Display(Name = "প্রতিষ্ঠা নোট")]
    public string EstablishedNote { get; set; } = "";

    [Required(ErrorMessage = "শুরুর সময় দিন।")]
    [Display(Name = "শুরুর সময়")]
    public DateTime StartTime { get; set; }

    [Required(ErrorMessage = "টাইমজোন অফসেট দিন।")]
    [Display(Name = "টাইমজোন অফসেট")]
    public string TimeZoneOffset { get; set; } = "+06:00";

    [Required(ErrorMessage = "ভেন্যু লিখুন।")]
    [Display(Name = "ভেন্যু")]
    public string Venue { get; set; } = "";

    [Required(ErrorMessage = "ঠিকানা লিখুন।")]
    [Display(Name = "ঠিকানা")]
    public string Address { get; set; } = "";

    [Required(ErrorMessage = "যোগাযোগ ফোন দিন।")]
    [Display(Name = "যোগাযোগ ফোন")]
    public string ContactPhone { get; set; } = "";

    [EmailAddress(ErrorMessage = "সঠিক ইমেইল দিন।")]
    [Display(Name = "যোগাযোগ ইমেইল")]
    public string ContactEmail { get; set; } = "";

    [Required(ErrorMessage = "পেমেন্ট নোট লিখুন।")]
    [Display(Name = "পেমেন্ট নোট")]
    public string PayNote { get; set; } = "";
}

/// <summary>Program seeds the DB and then swaps <see cref="Current"/> to the DB-backed values; everything resolves through this.</summary>
public class EventOptionsHolder(EventOptions initial)
{
    public EventOptions Current { get; set; } = initial;
}
