namespace InvestmentLab.Strategies.Contracts;

public sealed record EvaluateStrategyRequest(
    Guid StrategyId,
    int? Version,
    SubPortfolioStateDto SubPortfolio,
    decimal Contribution,
    DateOnly AsOf,
    Guid? CashInstrumentId = null,
    IReadOnlyList<SignalStateDto>? SignalStates = null,
    string? ValuationCurrency = null,
    string? ParametersOverride = null);
