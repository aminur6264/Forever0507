using Forever0507App.Models;

namespace Forever0507App.Services;

/// <summary>Single source of truth for participation fees. The client-side estimator only mirrors this.</summary>
public static class FeeCalculator
{
    public const int SeniorBatchCutoffYear = 2019; // ≤ 2019 → senior fee
    public const int SeniorFee = 1000;
    public const int JuniorFee = 500;              // 2020–2026
    public const int PerExtraMemberFee = 500;
    public const int MaxChildrenUnder5Free = 2;    // enforced by validation; children are free

    public static int Calculate(int batchYear, int extraMembers, int childrenUnder5 = 0)
    {
        var baseFee = batchYear <= SeniorBatchCutoffYear ? SeniorFee : JuniorFee;
        return baseFee + Math.Max(0, extraMembers) * PerExtraMemberFee;
    }

    public static int Calculate(RegistrationInputModel model)
        => Calculate(model.BatchYear, model.ExtraMembers, model.ChildrenUnder5);
}
