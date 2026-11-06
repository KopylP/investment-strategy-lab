namespace InvestmentLab.Strategies.Evaluation.Application.Shared;

public sealed record MarketPoint(DateOnly Date, decimal Close, decimal AdjustedClose, decimal? ValuationClose = null);
