namespace InvestmentLab.Backtesting.Replays.Application.GetMoneyOutcomes;

public sealed record MoneyOutcomesDto(decimal Contributed, int Years, IReadOnlyList<SeriesMoneyOutcomesDto> Series);
