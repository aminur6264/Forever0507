using System.ComponentModel.DataAnnotations;

namespace Forever0507App.Models;

/// <summary>A welcome/greeting note shown on the home page; only active ones are visible.</summary>
public class WelcomeNote
{
    public int Id { get; set; }

    [MaxLength(1000)]
    public string Message { get; set; } = "";

    [MaxLength(80)]
    public string Name { get; set; } = "";

    [MaxLength(120)]
    public string Designation { get; set; } = "";

    /// <summary>Optional profile photo. Null → the letter avatar is shown instead.</summary>
    [MaxLength(200)]
    public string? PhotoUrl { get; set; }

    /// <summary>Photo bytes — DB storage (shared-hosting safe), served via /Image/Welcome/{id}.</summary>
    public byte[]? PhotoData { get; set; }

    [MaxLength(50)]
    public string? PhotoContentType { get; set; }

    /// <summary>Inactive notes stay listed but are hidden from the home page; new notes start active.</summary>
    public bool IsActive { get; set; } = true;
}

/// <summary>Insert/update surface — status is managed by the switch in the list, not this form.</summary>
public class WelcomeNoteInputModel
{
    /// <summary>0 = insert, greater than 0 = update.</summary>
    public int Id { get; set; }

    [Required(ErrorMessage = "বার্তা লিখুন।")]
    [Display(Name = "বার্তা")]
    public string? Message { get; set; }

    [Required(ErrorMessage = "নাম লিখুন।")]
    [Display(Name = "নাম")]
    public string? Name { get; set; }

    [Required(ErrorMessage = "পদবি লিখুন।")]
    [Display(Name = "পদবি")]
    public string? Designation { get; set; }
}
