using InvestmentLab.MarketData.Contracts.Quotes;
using InvestmentLab.Portfolio.Contracts.Accounts;
using InvestmentLab.Portfolio.Contracts.Holdings;
using InvestmentLab.Rebalancing.Planning.Domain;
using InvestmentLab.Rebalancing.Reports.Domain;
using InvestmentLab.Strategies.Contracts;

namespace InvestmentLab.Rebalancing.Planning.Application.GenerateMonthlyReport;

public sealed record MonthlyPlan(
    AccountSetupDto Account,
    IReadOnlyList<SubPortfolioSetupDto> Planned,
    string Code,
    string Title,
    YearMonth Period,
    DateOnly AsOf,
    DateTimeOffset Now,
    PlanSettings PlanSettings,
    IReadOnlyList<RebalancingReport> PeriodReports,
    Currency Currency,
    AccountHoldingsDto Holdings,
    IReadOnlyDictionary<Guid, string> Labels,
    Dictionary<Guid, SubPortfolioHoldingsDto> Snapshots,
    Dictionary<Guid, LatestPriceDto> Prices,
    List<string> Warnings,
    ContributionAllocation Allocation)
{
    public string LabelOf(Guid instrumentId) => Labels.TryGetValue(instrumentId, out var label) ? label : instrumentId.ToString();

    public EvaluateStrategyRequest RequestOf(SubPortfolioSetupDto sub) => new(
        sub.StrategyId!.Value,
        sub.PinnedStrategyVersion,
        PlanningInputs.StateOf(Account, Snapshots.GetValueOrDefault(sub.Id)),
        Allocation.For(sub.Id),
        AsOf,
        sub.CashInstrumentId);
}
