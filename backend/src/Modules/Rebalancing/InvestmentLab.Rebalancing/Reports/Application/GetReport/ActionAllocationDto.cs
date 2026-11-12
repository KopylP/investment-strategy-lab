using InvestmentLab.Rebalancing.Reports.Domain;

namespace InvestmentLab.Rebalancing.Reports.Application.GetReport;

public sealed record ActionAllocationDto(
    Guid SubPortfolioId,
    decimal Quantity,
    string? Reason)
{
    public static ActionAllocationDto From(ActionAllocation actionAllocation) => new(
        SubPortfolioId: actionAllocation.SubPortfolioId,
        Quantity: actionAllocation.Quantity,
        Reason: actionAllocation.Reason);
}
