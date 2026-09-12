namespace Forever0507App.Services;

/// <summary>Single source of truth for the contribution rules. The client-side estimator only mirrors this.</summary>
public static class FeeCalculator
{
    /// <summary>Minimum main amount a registrant contributes.</summary>
    public const decimal MinAmount = 1000;
    public const decimal MaxAmount = 1_000_000;

    /// <summary>Mobile-banking service charge added on top of the main amount (2%).</summary>
    public const decimal ServiceChargePercent = 2m;

    /// <summary>Payable = main amount + 2%, rounded up to whole taka (e.g. 1000 → 1020).</summary>
    public static decimal CalculatePayable(decimal amount)
        => Math.Ceiling(amount * (1 + ServiceChargePercent / 100m));
}
