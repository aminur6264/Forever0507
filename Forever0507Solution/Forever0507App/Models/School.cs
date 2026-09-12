using System.ComponentModel.DataAnnotations;

namespace Forever0507App.Models;

/// <summary>Lookup: schools offered in the dropdown. Curated entries are seeded; registrants' own additions land here too.</summary>
public class School
{
    public int Id { get; set; }

    [MaxLength(160)]
    public string Name { get; set; } = "";

    /// <summary>True when the school was added by a registrant via the "অন্যান্য" option rather than seeded.</summary>
    public bool IsUserAdded { get; set; }
}
