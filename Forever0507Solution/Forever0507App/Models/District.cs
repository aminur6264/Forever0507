using System.ComponentModel.DataAnnotations;

namespace Forever0507App.Models;

/// <summary>Lookup: one of Bangladesh's 64 districts, seeded on first run.</summary>
public class District
{
    public int Id { get; set; }

    [MaxLength(40)]
    public string Name { get; set; } = "";

    /// <summary>Seed order (division-grouped) used for the dropdown.</summary>
    public int DisplayOrder { get; set; }
}
