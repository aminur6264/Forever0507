using System.ComponentModel.DataAnnotations;

namespace Forever0507App.Models;

public class LoginInputModel
{
    [Required(ErrorMessage = "ফোন নম্বর লিখুন।")]
    [RegularExpression(@"^01[3-9]\d{8}$", ErrorMessage = "সঠিক ১১ সংখ্যার মোবাইল নম্বর লিখুন (যেমন: 01712345678)।")]
    [Display(Name = "ফোন নম্বর (ইউজারনেম)")]
    public string? Phone { get; set; }

    [Required(ErrorMessage = "পাসওয়ার্ড লিখুন।")]
    [DataType(DataType.Password)]
    [Display(Name = "পাসওয়ার্ড")]
    public string? Password { get; set; }
}

public class ChangePasswordInputModel
{
    [Required(ErrorMessage = "বর্তমান পাসওয়ার্ড লিখুন।")]
    [DataType(DataType.Password)]
    [Display(Name = "বর্তমান পাসওয়ার্ড")]
    public string? CurrentPassword { get; set; }

    [Required(ErrorMessage = "নতুন পাসওয়ার্ড লিখুন।")]
    [DataType(DataType.Password)]
    [Display(Name = "নতুন পাসওয়ার্ড")]
    public string? NewPassword { get; set; }

    [Required(ErrorMessage = "নতুন পাসওয়ার্ড আবার লিখুন।")]
    [DataType(DataType.Password)]
    [Display(Name = "নতুন পাসওয়ার্ড (আবার)")]
    public string? ConfirmPassword { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (!string.IsNullOrEmpty(NewPassword) && NewPassword != ConfirmPassword)
            yield return new ValidationResult(
                "দুটি পাসওয়ার্ড এক নয়।",
                [nameof(ConfirmPassword)]);
    }
}

public class CreateUserInputModel
{
    [Required(ErrorMessage = "ফোন নম্বর লিখুন।")]
    [RegularExpression(@"^01[3-9]\d{8}$", ErrorMessage = "সঠিক ১১ সংখ্যার মোবাইল নম্বর লিখুন (যেমন: 01712345678)।")]
    [Display(Name = "ফোন নম্বর (ইউজারনেম)")]
    public string? Phone { get; set; }

    [Required(ErrorMessage = "প্রাথমিক পাসওয়ার্ড দিন।")]
    [Display(Name = "প্রাথমিক পাসওয়ার্ড")]
    public string? Password { get; set; }
}

public class ResetPasswordInputModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "নতুন পাসওয়ার্ড দিন।")]
    [Display(Name = "নতুন পাসওয়ার্ড")]
    public string? NewPassword { get; set; }
}
