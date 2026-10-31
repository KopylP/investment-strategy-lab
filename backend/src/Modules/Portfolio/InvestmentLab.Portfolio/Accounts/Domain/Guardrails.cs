namespace InvestmentLab.Portfolio.Accounts.Domain;

public sealed record Guardrails(Percentage? MaxPositionWeight, Percentage? MaxMonthlyTurnover)
{
    public static Guardrails None => new(null, null);

    public static bool AllowLeverage => false;
}
