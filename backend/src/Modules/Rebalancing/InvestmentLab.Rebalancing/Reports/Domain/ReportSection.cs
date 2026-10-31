namespace InvestmentLab.Rebalancing.Reports.Domain;

public sealed record ReportSection(
    Guid SubPortfolioId,
    string Name,
    decimal TargetShare,
    Guid? StrategyId,
    int? StrategyVersion,
    string? StrategyCode,
    string? EngineVersion,
    decimal Contribution,
    SectionDecision? Decision,
    IReadOnlyList<SectionOrder> Orders,
    IReadOnlyList<SectionAllocation> Allocation,
    decimal TotalBefore,
    decimal TotalAfter,
    decimal ParkedBefore,
    decimal ParkedAfter,
    IReadOnlyList<string> Notes,
    SectionRisk? Risk,
    string? Error);
