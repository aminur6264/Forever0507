using System.ComponentModel.DataAnnotations;

namespace Forever0507App.Models;

/// <summary>Form-bound validation surface for <see cref="Registration"/>.</summary>
public class RegistrationInputModel
{
    [Required(ErrorMessage = "অনুগ্রহ করে আপনার পূর্ণ নাম লিখুন।")]
    [StringLength(120, MinimumLength = 3, ErrorMessage = "নাম কমপক্ষে ৩ অক্ষরের হতে হবে।")]
    [Display(Name = "পূর্ণ নাম")]
    public string FullName { get; set; } = "";

    [Required(ErrorMessage = "মোবাইল নম্বর দেওয়া আবশ্যক।")]
    [RegularExpression(@"^01[3-9]\d{8}$", ErrorMessage = "সঠিক ১১ সংখ্যার মোবাইল নম্বর লিখুন (যেমন: 01712345678)।")]
    [Display(Name = "মোবাইল নম্বর")]
    public string Phone { get; set; } = "";

    [EmailAddress(ErrorMessage = "সঠিক ইমেইল ঠিকানা লিখুন।")]
    [Display(Name = "ইমেইল (ঐচ্ছিক)")]
    public string? Email { get; set; }

    [StringLength(120, ErrorMessage = "পেশা সর্বোচ্চ ১২০ অক্ষরের হতে হবে।")]
    [Display(Name = "পেশা (ঐচ্ছিক)")]
    public string? Occupation { get; set; }

    [StringLength(200, ErrorMessage = "ঠিকানা সর্বোচ্চ ২০০ অক্ষরের হতে হবে।")]
    [Display(Name = "বর্তমান ঠিকানা (ঐচ্ছিক)")]
    public string? PresentAddress { get; set; }

    [Required(ErrorMessage = "আপনার পাসের সন লিখুন।")]
    [Range(1960, 2026, ErrorMessage = "পাসের সন ১৯৬০ থেকে ২০২৬ এর মধ্যে হতে হবে।")]
    [Display(Name = "এসএসসি/পাসের সন")]
    public int BatchYear { get; set; }

    [Required(ErrorMessage = "সঙ্গী/পরিবারের সদস্য সংখ্যা লিখুন (না থাকলে ০)।")]
    [Range(0, 5, ErrorMessage = "সর্বোচ্চ ৫ জন পূর্ণবয়স্ক সহগামী যোগ করা যাবে।")]
    [Display(Name = "সঙ্গী ও পূর্ণবয়স্ক পরিবারের সদস্য")]
    public int ExtraMembers { get; set; }

    [Required(ErrorMessage = "শিশুর সংখ্যা লিখুন (না থাকলে ০)।")]
    [Range(0, 2, ErrorMessage = "৫ বছরের কম বয়সী সর্বোচ্চ ২ জন শিশু যোগ করা যাবে।")]
    [Display(Name = "৫ বছরের কম বয়সী শিশু")]
    public int ChildrenUnder5 { get; set; }

    [StringLength(400, ErrorMessage = "বার্তা সর্বোচ্চ ৪০০ অক্ষরের হতে হবে।")]
    [Display(Name = "কোনো বার্তা বা স্মৃতিচারণ (ঐচ্ছিক)")]
    public string? Note { get; set; }
}
