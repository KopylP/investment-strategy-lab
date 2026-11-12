using InvestmentLab.Rebalancing.Reports.Domain;

namespace InvestmentLab.Rebalancing.Reports.Application.GetReport;

public sealed record SectionAllocationDto(
    Guid? InstrumentId,
    string Role,
    decimal ValueBefore,
    decimal ValueAfter,
    decimal WeightBefore,
    decimal WeightAfter,
    decimal? TargetWeight)
{
    public static SectionAllocationDto From(SectionAllocation sectionAllocation) => new(
        InstrumentId: sectionAllocation.InstrumentId,
        Role: sectionAllocation.Role,
        ValueBefore: sectionAllocation.ValueBefore,
        ValueAfter: sectionAllocation.ValueAfter,
        WeightBefore: sectionAllocation.WeightBefore,
        WeightAfter: sectionAllocation.WeightAfter,
        TargetWeight: sectionAllocation.TargetWeight);
}
