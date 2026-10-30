using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Forever0507App.Models;

/// <summary>
/// Money a donor gave outside the registration fee (ডোনেশন). One admin records it; a
/// different admin approves — that approval is the moment the chosen wallet is credited,
/// the same way registration approval credits a payable amount. Approved entries are
/// permanent; pending/rejected ones can be deleted.
/// </summary>
public class Donation
{
    public int Id { get; set; }

    [MaxLength(120)]
    public string DonorName { get; set; } = "";

    /// <summary>11-digit BD mobile number.</summary>
    [MaxLength(11)]
    public string Phone { get; set; } = "";

    [Column(TypeName = "decimal(12,2)")]
    public decimal Amount { get; set; }

    /// <summary>Wallet the money landed in — loose reference by Id only (payment methods are
    /// never deleted). Required on new entries; null only on rows from before it was mandatory.</summary>
    public int? PaymentMethodId { get; set; }

    /// <summary>Recorder's username (phone / static admin name).</summary>
    [MaxLength(50)]
    public string CreatedBy { get; set; } = "";

    /// <summary>Recorder's display name at creation time — the list shows this without extra joins.</summary>
    [MaxLength(120)]
    public string CreatedByName { get; set; } = "";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Null until decided — pending. Approval credits the wallet and makes the entry
    /// permanent; rejected entries never touch any balance.</summary>
    [MaxLength(20)]
    public string? Status { get; set; }

    /// <summary>Who approved/rejected — username; the deciding admin can never be the creator.</summary>
    [MaxLength(50)]
    public string? ActionBy { get; set; }

    public DateTime? ActionAt { get; set; }

    public const string Approved = "অনুমোদিত";
    public const string Rejected = "প্রত্যাখ্যাত";
}

/// <summary>Insert surface for the donation form.</summary>
public class DonationInputModel
{
    [Required(ErrorMessage = "ডোনারের নাম লিখুন।")]
    [Display(Name = "ডোনারের নাম")]
    public string? DonorName { get; set; }

    [Required(ErrorMessage = "মোবাইল নম্বর লিখুন।")]
    [RegularExpression(@"^01[3-9]\d{8}$", ErrorMessage = "সঠিক ১১ সংখ্যার মোবাইল নম্বর লিখুন (যেমন: 01712345678)।")]
    [Display(Name = "মোবাইল নম্বর")]
    public string? Phone { get; set; }

    [Required(ErrorMessage = "টাকার পরিমাণ লিখুন।")]
    [Range(1, 9_999_999_999, ErrorMessage = "টাকার পরিমাণ ০-র বেশি হতে হবে।")]
    [Display(Name = "টাকার পরিমাণ (৳)")]
    public decimal? Amount { get; set; }

    /// <summary>Receiving wallet — required; its balance is credited on save.</summary>
    public int? PaymentMethodId { get; set; }
}
