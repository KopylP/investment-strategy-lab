namespace InvestmentLab.SharedKernel;

public sealed record SignedPercentage
{
    public decimal Value { get; }

    private SignedPercentage(decimal value) => Value = value;

    public static SignedPercentage Of(decimal fraction) => new(fraction);
    public static SignedPercentage FromPercent(decimal percent) => new(percent / 100m);

    public bool IsNegative => Value < 0m;

    public override string ToString() => $"{Value * 100m:+0.##;-0.##;0}%";
}
