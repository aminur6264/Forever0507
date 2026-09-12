using System.ComponentModel.DataAnnotations;

namespace Forever0507App.Models;

/// <summary>Lookup: accepted mobile-banking mediums (বিকাশ / নগদ / রকেট), seeded on first run.</summary>
public class PaymentMediumOption
{
    public int Id { get; set; }

    [MaxLength(20)]
    public string Name { get; set; } = "";

    public int DisplayOrder { get; set; }
}
