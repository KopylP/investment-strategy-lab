namespace InvestmentLab.Strategies.Contracts;

public sealed record StrategyDecisionDto(
    Guid StrategyId,
    int StrategyVersion,
    string StrategyCode,
    string EngineVersion,
    DateOnly AsOf,
    decimal InvestFraction,
    IReadOnlyList<TargetWeightDto> Targets,
    MarketRegimeDto Market,
    RationaleDto Rationale,
    IReadOnlyList<string> Warnings,
    Guid? CashInstrumentId = null);
