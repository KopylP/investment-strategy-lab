using InvestmentLab.Backtesting.Replays.Domain;

namespace InvestmentLab.Backtesting.Replays.Application.GetMoneyOutcomes;

public sealed record MoneyOutcomesDto(decimal Contributed, int Years, MoneyOutcomes Strategy, MoneyOutcomes Benchmark);
