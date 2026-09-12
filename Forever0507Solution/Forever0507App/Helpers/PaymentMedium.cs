namespace Forever0507App.Helpers;

/// <summary>Accepted mobile-banking mediums (single choice on the form).</summary>
public static class PaymentMedium
{
    public const string Bkash = "বিকাশ";
    public const string Nagad = "নগদ";
    public const string Rocket = "রকেট";

    public static readonly string[] All = [Bkash, Nagad, Rocket];
}
