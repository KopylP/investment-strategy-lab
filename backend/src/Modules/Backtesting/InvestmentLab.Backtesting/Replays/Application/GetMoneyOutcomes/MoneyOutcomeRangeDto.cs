using InvestmentLab.Backtesting.Replays.Domain;

namespace InvestmentLab.Backtesting.Replays.Application.GetMoneyOutcomes;

public sealed record MoneyOutcomeRangeDto(
    int Periods,
    MoneyOutcomeDto? Worst,
    MoneyOutcomeDto? Typical,
    MoneyOutcomeDto? Best)
{
    public static MoneyOutcomeRangeDto From(MoneyOutcomes moneyOutcomes) => new(
        Periods: moneyOutcomes.Periods,
        Worst: moneyOutcomes.Worst is { } worst ? MoneyOutcomeDto.Of(worst) : null,
        Typical: moneyOutcomes.Typical is { } typical ? MoneyOutcomeDto.Of(typical) : null,
        Best: moneyOutcomes.Best is { } best ? MoneyOutcomeDto.Of(best) : null);
}
