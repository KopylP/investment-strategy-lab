using InvestmentLab.Backtesting.Replays.Domain;

namespace InvestmentLab.Backtesting.Replays.Application.GetMoneyOutcomes;

public sealed record MoneyOutcomeDto(
    decimal Final,
    DateOnly From,
    DateOnly To)
{
    public static MoneyOutcomeDto Of(MoneyOutcome moneyOutcome) => new(
        Final: moneyOutcome.Final,
        From: moneyOutcome.From,
        To: moneyOutcome.To);
}
