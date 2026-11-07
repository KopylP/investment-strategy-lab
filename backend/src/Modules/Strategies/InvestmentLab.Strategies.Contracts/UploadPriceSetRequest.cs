namespace InvestmentLab.Strategies.Contracts;

public sealed record UploadPriceSetRequest(
    Guid StrategyId,
    int? Version,
    DateOnly FirstAsOf,
    DateOnly LastAsOf,
    string ValuationCurrency,
    string? ParametersOverride = null);
