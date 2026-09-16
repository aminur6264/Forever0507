using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Forever0507App.Models;

/// <summary>
/// A committee expense invoice (খরচ). Created by an admin, decided (অনুমোদিত/প্রত্যাখ্যাত) by
/// a different admin; editable by its creator until decided. Amount is always the server-side
/// sum of the line items.
/// </summary>
public class Khoroch
{
    public int Id { get; set; }

    /// <summary>KH-yyyyMMdd-INIT-#### — date + creator initials + the identity Id (auto increment).</summary>
    [MaxLength(30)]
    public string InvoiceCode { get; set; } = "";

    /// <summary>Expense date (wall clock as entered on the form).</summary>
    public DateTime Date { get; set; }

    [MaxLength(200)]
    public string Description { get; set; } = "";

    /// <summary>Σ quantity × unit price across items — recomputed on every save, never trusted from the client.</summary>
    [Column(TypeName = "decimal(12,2)")]
    public decimal Amount { get; set; }

    /// <summary>Creator's username (phone / static admin name).</summary>
    [MaxLength(50)]
    public string CreatedBy { get; set; } = "";

    /// <summary>Creator's display name at creation time — the list shows this without extra joins.</summary>
    [MaxLength(120)]
    public string CreatedByName { get; set; } = "";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Null until decided; the creator can still edit while null.</summary>
    [MaxLength(20)]
    public string? Status { get; set; }

    /// <summary>Who approved/rejected — username; the deciding admin can never be the creator.</summary>
    [MaxLength(50)]
    public string? ActionBy { get; set; }

    public DateTime? ActionAt { get; set; }

    /// <summary>Optional receipt/photo under /uploads/khoroch, shown as a modal on click. Null → no image icon.</summary>
    [MaxLength(200)]
    public string? ImageUrl { get; set; }

    public List<KhorochItem> Items { get; set; } = [];

    public const string Approved = "অনুমোদিত";
    public const string Rejected = "প্রত্যাখ্যাত";
}

/// <summary>One line of an expense invoice.</summary>
public class KhorochItem
{
    public int Id { get; set; }

    public int KhorochId { get; set; }
    public Khoroch? Khoroch { get; set; }

    [MaxLength(160)]
    public string ProductName { get; set; } = "";

    [Column(TypeName = "decimal(12,2)")]
    public decimal Quantity { get; set; }

    [Column(TypeName = "decimal(12,2)")]
    public decimal UnitPrice { get; set; }

    /// <summary>Line total — derived, never stored.</summary>
    public decimal LineTotal => Quantity * UnitPrice;
}

/// <summary>Form surface for creating/updating a Khoroch invoice with its item rows.</summary>
public class KhorochInputModel
{
    /// <summary>0 = create, greater than 0 = update (creator, pending only).</summary>
    public int Id { get; set; }

    [Required(ErrorMessage = "খরচের তারিখ দিন।")]
    [Display(Name = "তারিখ")]
    public DateTime Date { get; set; } = DateTime.Today;

    [StringLength(200, ErrorMessage = "বিবরণ সর্বোচ্চ ২০০ অক্ষরের হতে হবে।")]
    [Display(Name = "বিবরণ")]
    public string? Description { get; set; }

    public List<KhorochItemInput> Items { get; set; } = [];
}

public class KhorochItemInput
{
    public string? ProductName { get; set; }

    /// <summary>Text on the wire — parsed with InvariantCulture so 2.5 always means ২.৫.</summary>
    public string? Quantity { get; set; }
    public string? UnitPrice { get; set; }
}
