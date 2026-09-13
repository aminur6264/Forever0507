using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Forever0507App.Models;

/// <summary>A committee mobile-wallet account that receives registration payments.</summary>
public class PaymentMethod
{
    public int Id { get; set; }

    /// <summary>Account holder's name.</summary>
    [MaxLength(80)]
    public string AccountName { get; set; } = "";

    /// <summary>The wallet number of this account.</summary>
    [MaxLength(11)]
    public string AccountNumber { get; set; } = "";

    /// <summary>পার্সোনাল or মার্চেন্ট — see the constants.</summary>
    [MaxLength(20)]
    public string Type { get; set; } = "";

    /// <summary>The MFS brand; matches a seeded PaymentMediumOption name (বিকাশ/নগদ/রকেট).</summary>
    [MaxLength(20)]
    public string MfsName { get; set; } = "";

    /// <summary>Money sitting in the wallet. Starts at 0 and only internal flows change it — never the admin form.</summary>
    [Column(TypeName = "decimal(12,2)")]
    public decimal Balance { get; set; }

    /// <summary>Inactive accounts stay listed but are flagged off; new entries start active.</summary>
    public bool IsActive { get; set; } = true;

    public const string Personal = "পার্সোনাল";
    public const string Merchant = "মার্চেন্ট";
}

/// <summary>Insert/update surface — deliberately no Balance field: it defaults to 0 and is never editable.</summary>
public class PaymentMethodInputModel
{
    /// <summary>0 = insert, greater than 0 = update.</summary>
    public int Id { get; set; }

    [Required(ErrorMessage = "অ্যাকাউন্ট হোল্ডারের নাম লিখুন।")]
    [Display(Name = "অ্যাকাউন্ট নাম (হোল্ডার)")]
    public string? AccountName { get; set; }

    [Required(ErrorMessage = "অ্যাকাউন্ট নম্বর লিখুন।")]
    [RegularExpression(@"^01[3-9]\d{8}$", ErrorMessage = "সঠিক ১১ সংখ্যার মোবাইল নম্বর লিখুন (যেমন: 01712345678)।")]
    [Display(Name = "অ্যাকাউন্ট নম্বর")]
    public string? AccountNumber { get; set; }

    [Required(ErrorMessage = "টাইপ নির্বাচন করুন।")]
    [Display(Name = "টাইপ")]
    public string? Type { get; set; }

    [Required(ErrorMessage = "এমএফএস নাম নির্বাচন করুন।")]
    [Display(Name = "এমএফএস নাম")]
    public string? MfsName { get; set; }
}
