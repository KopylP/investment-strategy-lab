using InvestmentLab.Rebalancing.Reports.Domain;

namespace InvestmentLab.Rebalancing.Reports.Application.GetReport;

public sealed record ReportSectionDto(
    Guid SubPortfolioId,
    string Name,
    decimal TargetShare,
    Guid? StrategyId,
    int? StrategyVersion,
    string? StrategyCode,
    string? EngineVersion,
    decimal Contribution,
    SectionDecisionDto? Decision,
    IReadOnlyList<SectionOrderDto> Orders,
    IReadOnlyList<SectionAllocationDto> Allocation,
    decimal TotalBefore,
    decimal TotalAfter,
    decimal ParkedBefore,
    decimal ParkedAfter,
    IReadOnlyList<string> Notes,
    SectionRiskDto? Risk,
    string? Error)
{
    public static ReportSectionDto From(ReportSection reportSection) => new(
        SubPortfolioId: reportSection.SubPortfolioId,
        Name: reportSection.Name,
        TargetShare: reportSection.TargetShare,
        StrategyId: reportSection.StrategyId,
        StrategyVersion: reportSection.StrategyVersion,
        StrategyCode: reportSection.StrategyCode,
        EngineVersion: reportSection.EngineVersion,
        Contribution: reportSection.Contribution,
        Decision: reportSection.Decision is { } decision ? SectionDecisionDto.From(decision) : null,
        Orders: [.. reportSection.Orders.Select(SectionOrderDto.From)],
        Allocation: [.. reportSection.Allocation.Select(SectionAllocationDto.From)],
        TotalBefore: reportSection.TotalBefore,
        TotalAfter: reportSection.TotalAfter,
        ParkedBefore: reportSection.ParkedBefore,
        ParkedAfter: reportSection.ParkedAfter,
        Notes: reportSection.Notes,
        Risk: reportSection.Risk is { } risk ? SectionRiskDto.From(risk) : null,
        Error: reportSection.Error);
}
