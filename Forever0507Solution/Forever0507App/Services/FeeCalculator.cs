namespace Forever0507App.Services;

/// <summary>Single source of truth for the contribution rules.</summary>
public static class FeeCalculator
{
    /// <summary>Minimum amount a registrant contributes — this is also the payable amount (no service charge).</summary>
    public const decimal MinAmount = 1020;
    public const decimal MaxAmount = 1_000_000;
}
