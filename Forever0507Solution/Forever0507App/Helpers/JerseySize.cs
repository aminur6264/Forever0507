namespace Forever0507App.Helpers;

/// <summary>Jersey sizes offered on the form (stored value → Bengali display label).</summary>
public static class JerseySize
{
    public static readonly (string Value, string Label)[] Options =
    [
        ("S", "স্মল (S)"),
        ("M", "মিডিয়াম (M)"),
        ("L", "লার্জ (L)"),
        ("XL", "এক্সট্রা লার্জ (XL)"),
        ("XXL", "ডাবল এক্সট্রা লার্জ (XXL)"),
    ];

    public static readonly string[] All = Options.Select(o => o.Value).ToArray();

    public static string LabelOf(string value) =>
        Options.FirstOrDefault(o => o.Value == value).Label ?? value;
}
