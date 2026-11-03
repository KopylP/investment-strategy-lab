using InvestmentLab.Strategies.Evaluation.Domain;
using InvestmentLab.Strategies.Contracts;

namespace InvestmentLab.Strategies.Evaluation.Application.Shared;

public sealed record StrategyEvaluationContext(
    DateOnly AsOf,
    string Parameters,
    IReadOnlyList<UniverseMember> Universe,
    Guid? CashInstrumentId,
    IReadOnlyList<MarketSeries> MarketSeries,
    SubPortfolioStateDto SubPortfolio,
    decimal Contribution,
    IReadOnlyList<SignalState>? Signals = null);
