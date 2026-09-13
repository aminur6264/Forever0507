using System.ComponentModel.DataAnnotations;

namespace Forever0507App.Models;

/// <summary>Lookup: jersey sizes offered on the form (value stored on the registration, label shown in UI).</summary>
public class JerseySizeOption
{
    public int Id { get; set; }

    /// <summary>Stored on the registration, e.g. "XL".</summary>
    [MaxLength(5)]
    public string Value { get; set; } = "";

    /// <summary>Bengali display label, e.g. "এক্সট্রা লার্জ (XL)".</summary>
    [MaxLength(60)]
    public string Label { get; set; } = "";

    public int DisplayOrder { get; set; }

    /// <summary>Disabled sizes stay in the table (old registrations keep their label) but leave the registration form.</summary>
    public bool IsActive { get; set; } = true;
}

/// <summary>Insert/update surface — status is managed by the switch in the list, not this form.</summary>
public class JerseySizeInputModel
{
    /// <summary>0 = insert, greater than 0 = update.</summary>
    public int Id { get; set; }

    [Required(ErrorMessage = "সাইজ কোড লিখুন।")]
    [RegularExpression(@"^[A-Za-z0-9]{1,5}$", ErrorMessage = "সাইজ কোড ১-৫ অক্ষরের ইংরেজি অক্ষর/সংখ্যা (যেমন: XL)।")]
    [Display(Name = "সাইজ কোড")]
    public string? Value { get; set; }

    [Required(ErrorMessage = "লেবেল লিখুন।")]
    [Display(Name = "লেবেল")]
    public string? Label { get; set; }
}
