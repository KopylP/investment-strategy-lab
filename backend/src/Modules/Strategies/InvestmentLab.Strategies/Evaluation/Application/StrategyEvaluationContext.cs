using InvestmentLab.Strategies.Contracts;

namespace InvestmentLab.Strategies.Evaluation.Application;

public sealed record StrategyEvaluationContext(
    DateOnly AsOf,
    string Parameters,
    IReadOnlyList<UniverseMember> Universe,
    Guid? CashInstrumentId,
    IReadOnlyList<MarketSeries> MarketSeries,
    SubPortfolioStateDto SubPortfolio,
    decimal Contribution);
