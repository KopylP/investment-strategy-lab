namespace InvestmentLab.StrategyBuilder.Drafts.Domain;

public sealed record DirectoryInstrument(Guid Id, string Label, string Name, string? Group, string? AssetClass, string? Region, bool IsVolatilityIndex);
