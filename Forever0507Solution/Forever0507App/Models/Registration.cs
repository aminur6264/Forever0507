using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Forever0507App.Models;

/// <summary>A persisted reunion registration. Validation lives on <see cref="RegistrationInputModel"/>.</summary>
public class Registration
{
    public int Id { get; set; }

    /// <summary>REG-2026-#### — derived from the identity Id after the first save.</summary>
    [MaxLength(20)]
    public string RegistrationNo { get; set; } = "";

    [MaxLength(120)]
    public string FullName { get; set; } = "";

    /// <summary>One of Bangladesh's 64 districts (see Helpers.BdDistricts).</summary>
    [MaxLength(40)]
    public string District { get; set; } = "";

    /// <summary>School as chosen from the dropdown or typed in the "other" box.</summary>
    [MaxLength(160)]
    public string SchoolName { get; set; } = "";

    /// <summary>Fixed for this cohort (2005).</summary>
    public int SscYear { get; set; }

    /// <summary>বিকাশ / নগদ / রকেট (see Helpers.PaymentMedium).</summary>
    [MaxLength(20)]
    public string PaymentMedium { get; set; } = "";

    [Column(TypeName = "decimal(12,2)")]
    public decimal Amount { get; set; }

    /// <summary>Amount + service charge (2%), rounded up to whole taka.</summary>
    [Column(TypeName = "decimal(12,2)")]
    public decimal PayableAmount { get; set; }

    /// <summary>Mobile-banking transaction id supplied by the registrant.</summary>
    [MaxLength(30)]
    public string TransactionId { get; set; } = "";

    /// <summary>S / M / L / XL / XXL (see Helpers.JerseySize).</summary>
    [MaxLength(5)]
    public string JerseySize { get; set; } = "";

    /// <summary>11-digit BD mobile number; lookup key for the card page.</summary>
    [MaxLength(11)]
    public string Phone { get; set; } = "";

    /// <summary>Committee verifies the transaction after registration.</summary>
    public string PaymentStatus { get; set; } = "যাচাই অপেক্ষমান";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
