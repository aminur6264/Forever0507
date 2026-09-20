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

    /// <summary>Wallet number the money was sent from (registrant's account).</summary>
    [MaxLength(11)]
    public string FromAccount { get; set; } = "";

    /// <summary>Wallet number the money was sent to (committee's account).</summary>
    [MaxLength(11)]
    public string ToAccount { get; set; } = "";

    /// <summary>S / M / L / XL / XXL (see Helpers.JerseySize).</summary>
    [MaxLength(5)]
    public string JerseySize { get; set; } = "";

    /// <summary>The name printed on the jersey — English letters, as the registrant wants it shown.</summary>
    [MaxLength(20)]
    public string NameOnJersey { get; set; } = "";

    /// <summary>11-digit BD mobile number; lookup key for the card page.</summary>
    [MaxLength(11)]
    public string Phone { get; set; } = "";

    /// <summary>Optional contact email; empty when the registrant skipped it.</summary>
    [MaxLength(120)]
    public string Email { get; set; } = "";

    /// <summary>Committee verifies the transaction after registration.</summary>
    public string PaymentStatus { get; set; } = "যাচাই অপেক্ষমান";

    /// <summary>Null until the admin decides. Once অনুমোদিত/প্রত্যাখ্যাত the decision is final; only approved cards are viewable.</summary>
    [MaxLength(20)]
    public string? ApprovalStatus { get; set; }

    /// <summary>Who decided — admin's username (phone) or the static admin's display name; null while undecided.</summary>
    [MaxLength(50)]
    public string? ApprovalBy { get; set; }

    /// <summary>UTC instant of the approve/reject decision; null while undecided.</summary>
    public DateTime? ApprovalAt { get; set; }

    public const string ApprovalApproved = "অনুমোদিত";
    public const string ApprovalRejected = "প্রত্যাখ্যাত";

    /// <summary>Submitter's IP, stored as IpAddresses.Id at registration time — record-keeping
    /// only, never displayed. Null for registrations made before this was captured.</summary>
    public int? IpAddressId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
