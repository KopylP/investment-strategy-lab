using InvestmentLab.Rebalancing.Planning.Domain;
using InvestmentLab.Rebalancing.Reports.Domain;

namespace InvestmentLab.Rebalancing.Planning.Application.GenerateMonthlyReport;

public sealed record MonthlyPlan(
    IPlanningAccount Account,
    IReadOnlyList<IPlanningSubPortfolio> Planned,
    string Code,
    string Title,
    YearMonth Period,
    DateOnly AsOf,
    DateTimeOffset Now,
    PlanSettings PlanSettings,
    IReadOnlyList<RebalancingReport> PeriodReports,
    Currency Currency,
    IReadOnlyList<IHeldSubPortfolio> Holdings,
    IReadOnlyDictionary<Guid, string> Labels,
    Dictionary<Guid, IHeldSubPortfolio> Snapshots,
    Dictionary<Guid, IInstrumentPrice> Prices,
    List<string> Warnings,
    ContributionAllocation Allocation)
{
    public string LabelOf(Guid instrumentId) => Labels.TryGetValue(instrumentId, out var label) ? label : instrumentId.ToString();

    public StrategyAsk AskOf(IPlanningSubPortfolio sub) =>
        StrategyAsk.Of(Account, sub, Snapshots.GetValueOrDefault(sub.Id), Allocation.For(sub.Id), AsOf);
}
