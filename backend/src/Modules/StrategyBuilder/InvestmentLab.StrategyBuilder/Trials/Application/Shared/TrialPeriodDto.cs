using InvestmentLab.StrategyBuilder.Trials.Domain;

namespace InvestmentLab.StrategyBuilder.Trials.Application.Shared;

public sealed record TrialPeriodDto(DateOnly Start, DateOnly End)
{
    public static TrialPeriodDto From(TrialPeriod period) => new(period.Start, period.End);

    public TrialPeriod ToDomain() => new(Start, End);
}
