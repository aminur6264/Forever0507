using System.ComponentModel.DataAnnotations;

namespace Forever0507App.Models;

/// <summary>Form-bound validation surface for <see cref="Registration"/>.</summary>
public class RegistrationInputModel : IValidatableObject
{
    public const string OtherSchoolValue = "__OTHER__";

    [Required(ErrorMessage = "অনুগ্রহ করে আপনার পূর্ণ নাম লিখুন।")]
    [StringLength(120, MinimumLength = 3, ErrorMessage = "নাম কমপক্ষে ৩ অক্ষরের হতে হবে।")]
    [Display(Name = "১. নাম")]
    public string FullName { get; set; } = "";

    [Required(ErrorMessage = "অনুগ্রহ করে আপনার জেলা নির্বাচন করুন।")]
    [Display(Name = "২. জেলা")]
    public string? District { get; set; }

    /// <summary>Chosen dropdown value; <see cref="OtherSchoolValue"/> means the registrant types their own.</summary>
    [Required(ErrorMessage = "অনুগ্রহ করে আপনার স্কুলের নাম নির্বাচন করুন।")]
    [Display(Name = "৩. স্কুলের নাম")]
    public string? SchoolChoice { get; set; }

    [StringLength(160, MinimumLength = 3, ErrorMessage = "স্কুলের নাম কমপক্ষে ৩ অক্ষরের হতে হবে।")]
    [Display(Name = "আপনার স্কুলের নাম লিখুন")]
    public string? OtherSchoolName { get; set; }

    /// <summary>Free text, digits only; minimum enforced by FeeCalculator.MinAmount.</summary>
    [Required(ErrorMessage = "টাকার পরিমাণ লিখুন।")]
    [RegularExpression(@"^\d+(\.\d{1,2})?$", ErrorMessage = "শুধুমাত্র সংখ্যা লিখুন (যেমন: 1500)।")]
    [Display(Name = "৬. টাকার পরিমাণ (মূল)")]
    public string? AmountText { get; set; }

    [Required(ErrorMessage = "ট্রানজেকশন মাধ্যম নির্বাচন করুন।")]
    [Display(Name = "৫. ট্রানজেকশন মাধ্যম")]
    public string? PaymentMedium { get; set; }

    [Required(ErrorMessage = "ট্রানজেকশন আইডি লিখুন।")]
    [RegularExpression(@"^[A-Za-z0-9]{4,30}$", ErrorMessage = "ট্রানজেকশন আইডি ৪-৩০ অক্ষরের, শুধু ইংরেজি অক্ষর ও সংখ্যা (যেমন: 9HT7K2XPLM)।")]
    [Display(Name = "৭. ট্রানজেকশন আইডি")]
    public string? TransactionId { get; set; }

    [Required(ErrorMessage = "জার্সি সাইজ নির্বাচন করুন।")]
    [Display(Name = "৮. জার্সি সাইজ")]
    public string? JerseySize { get; set; }

    [Required(ErrorMessage = "মোবাইল নম্বর দেওয়া আবশ্যক।")]
    [RegularExpression(@"^01[3-9]\d{8}$", ErrorMessage = "সঠিক ১১ সংখ্যার মোবাইল নম্বর লিখুন (যেমন: 01712345678)।")]
    [Display(Name = "৯. মোবাইল নম্বর")]
    public string? Phone { get; set; }

    /// <summary>Final school name — dropdown value or the typed "other" name. Null when invalid.</summary>
    public string? ResolvedSchoolName
    {
        get
        {
            if (SchoolChoice == OtherSchoolValue)
                return string.IsNullOrWhiteSpace(OtherSchoolName) ? null : OtherSchoolName.Trim();
            return string.IsNullOrWhiteSpace(SchoolChoice) ? null : SchoolChoice;
        }
    }

    /// <summary>Parsed main amount. Null when the text is missing or below minimum.</summary>
    public decimal? ResolvedAmount
        => decimal.TryParse(AmountText, System.Globalization.CultureInfo.InvariantCulture, out var amount)
            && amount >= Services.FeeCalculator.MinAmount
            ? amount
            : null;

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (SchoolChoice == OtherSchoolValue && string.IsNullOrWhiteSpace(OtherSchoolName))
            yield return new ValidationResult(
                "তালিকায় আপনার স্কুল না থাকলে নিজের স্কুলের নাম লিখুন।",
                [nameof(OtherSchoolName)]);

        if (!string.IsNullOrWhiteSpace(AmountText))
        {
            if (decimal.TryParse(AmountText, System.Globalization.CultureInfo.InvariantCulture, out var amount)
                && amount < Services.FeeCalculator.MinAmount)
                yield return new ValidationResult(
                    $"টাকার পরিমাণ সর্বনিম্ন {Helpers.BengaliText.Taka(Services.FeeCalculator.MinAmount)} হতে হবে।",
                    [nameof(AmountText)]);
        }

        if (!string.IsNullOrEmpty(PaymentMedium) && !Helpers.PaymentMedium.All.Contains(PaymentMedium))
            yield return new ValidationResult("সঠিক ট্রানজেকশন মাধ্যম নির্বাচন করুন।", [nameof(PaymentMedium)]);

        if (!string.IsNullOrEmpty(JerseySize) && !Helpers.JerseySize.All.Contains(JerseySize))
            yield return new ValidationResult("সঠিক জার্সি সাইজ নির্বাচন করুন।", [nameof(JerseySize)]);
    }
}
