using System.ComponentModel.DataAnnotations;

namespace Forever0507App.Models;

/// <summary>A "why join" benefit card shown on the home page; only active ones are visible.</summary>
public class WhyJoinItem
{
    public int Id { get; set; }

    [MaxLength(120)]
    public string Title { get; set; } = "";

    [MaxLength(500)]
    public string Description { get; set; } = "";

    /// <summary>One of <see cref="AvailableIcons"/> — rendered as the card's icon.</summary>
    [MaxLength(20)]
    public string Icon { get; set; } = "";

    /// <summary>Inactive items stay listed but are hidden from the home page; new items start active.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Curated icon choices for the admin select.</summary>
    public static readonly string[] AvailableIcons =
        ["🎉", "🤝", "🏆", "📸", "🍽️", "🎽", "❤️", "🌟", "🎯", "📚", "🚌", "🏏", "🎤", "🙏", "🌐", "📖", "🏫", "💪"];
}

/// <summary>Insert/update surface — status is managed by the switch in the list, not this form.</summary>
public class WhyJoinItemInputModel
{
    /// <summary>0 = insert, greater than 0 = update.</summary>
    public int Id { get; set; }

    [Required(ErrorMessage = "শিরোনাম লিখুন।")]
    [Display(Name = "শিরোনাম")]
    public string? Title { get; set; }

    [Required(ErrorMessage = "বর্ণনা লিখুন।")]
    [Display(Name = "বর্ণনা")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "আইকন নির্বাচন করুন।")]
    [Display(Name = "আইকন")]
    public string? Icon { get; set; }
}
