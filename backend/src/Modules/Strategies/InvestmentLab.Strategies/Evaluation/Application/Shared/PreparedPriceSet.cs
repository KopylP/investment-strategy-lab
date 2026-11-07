namespace InvestmentLab.Strategies.Evaluation.Application.Shared;

public sealed record PreparedPriceSet(string TypeCode, IReadOnlyList<MarketSeries> Series);
