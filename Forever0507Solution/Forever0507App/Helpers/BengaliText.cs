using System.Text;

namespace Forever0507App.Helpers;

/// <summary>Display-only helpers for rendering numbers in Bengali numerals. Numeric form fields always post ASCII digits.</summary>
public static class BengaliText
{
    private const string Digits = "০১২৩৪৫৬৭৮৯";

    public static string ToBengaliDigits(int value) => ToBengaliDigits(value.ToString());

    public static string ToBengaliDigits(string text)
    {
        var sb = new StringBuilder(text.Length);
        foreach (var c in text)
            sb.Append(char.IsAsciiDigit(c) ? Digits[c - '0'] : c);
        return sb.ToString();
    }

    /// <summary>e.g. 2000 → "৳২,০০০".</summary>
    public static string Taka(decimal amount) => "৳" + ToBengaliDigits(((int)amount).ToString("N0"));
}
