namespace InvestmentLab.Strategies.Shared.Infrastructure.Engine;

public sealed record EngineHolding(string Key, IReadOnlyList<EngineLot> Lots);
