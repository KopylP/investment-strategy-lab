using InvestmentLab.Backtesting.Replays.Domain;

namespace InvestmentLab.Backtesting.Replays.Application.GetMoneyOutcomes;

public sealed record SeriesMoneyOutcomesDto(string Code, string Label, MoneyOutcomes Outcomes);
