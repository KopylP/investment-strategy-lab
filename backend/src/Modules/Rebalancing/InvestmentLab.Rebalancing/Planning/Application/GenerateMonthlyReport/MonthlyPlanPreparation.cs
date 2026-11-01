using InvestmentLab.Rebalancing.Planning.Application.Shared;
using InvestmentLab.Rebalancing.Planning.Domain;
using InvestmentLab.Rebalancing.Reports.Domain;

namespace InvestmentLab.Rebalancing.Planning.Application.GenerateMonthlyReport;

public sealed class MonthlyPlanPreparation(
    IPlanningAccounts accounts,
    IAccountHoldings accountHoldings,
    IInstrumentPrices instrumentPrices,
    IInstrumentLabels instrumentLabels,
    IPlanSettingsRepository settings,
    IReportRepository reports,
    TimeProvider clock)
{
    public async Task<Result<IReadOnlyDictionary<Guid, StrategyAsk>>> StrategyRequestsAsync(
        GenerateMonthlyReportCommand command, CancellationToken cancellationToken)
    {
        var prepared = await PrepareAsync(command, cancellationToken);
        if (prepared.IsFailure)
            return prepared.Error;

        var plan = prepared.Value;

        return plan.Planned.ToDictionary(sub => sub.Id, plan.AskOf);
    }

    public async Task<Result<MonthlyPlan>> PrepareAsync(GenerateMonthlyReportCommand command, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var asOf = command.AsOf ?? DateOnly.FromDateTime(now.UtcDateTime);
        var period = YearMonth.Containing(asOf);

        if (await accounts.FindAsync(command.AccountId, cancellationToken) is not { } account)
            return Error.NotFound("account.not_found", $"Account {command.AccountId} does not exist.");

        var all = PlannedSubPortfolios.Of(account).ToList();
        var planned = PlannedWithin(all, command.Scope);
        if (planned.Count == 0)
            return ReportErrors.NoActiveSubPortfolios(account.Id);

        if (command.Contribution is < 0m)
            return ReportErrors.ContributionNegative();

        var code = command.Scope.Code;
        var title = command.Scope.Title;

        var planSettings = await SettingsOfAsync(account, cancellationToken);
        var periodReports = await reports.ForPeriodAsync(account.Id, period, cancellationToken);
        var contribution = command.Contribution ?? RemainingContribution(planSettings, periodReports, code, all, planned);

        var holdings = await accountHoldings.OfAsync(account.Id, account.BaseCurrency, cancellationToken);
        var labels = await instrumentLabels.AllAsync(cancellationToken);
        var snapshots = holdings.ToDictionary(sub => sub.Id);

        var warnings = new List<string>();
        var prices = await HeldAndCashPricesAsync(account, holdings, planned, asOf, warnings, cancellationToken);
        var allocation = Allocate(account, planned, contribution, snapshots, prices);
        if (planSettings.Policy.LevelByPurchasesOnly)
            allocation = allocation.WithoutTransfer();

        return new MonthlyPlan(
            Account: account,
            Planned: planned,
            Code: code,
            Title: title,
            Period: period,
            AsOf: asOf,
            Now: now,
            PlanSettings: planSettings,
            PeriodReports: periodReports,
            Currency: Currency.Of(account.BaseCurrency),
            Holdings: holdings,
            Labels: labels,
            Snapshots: snapshots,
            Prices: prices,
            Warnings: warnings,
            Allocation: allocation);
    }

    private static List<IPlanningSubPortfolio> PlannedWithin(List<IPlanningSubPortfolio> all, ReportScope scope) =>
        [.. all.Where(sub => scope.SubPortfolioIds.Contains(sub.Id))];

    private async Task<PlanSettings> SettingsOfAsync(IPlanningAccount account, CancellationToken cancellationToken) =>
        await settings.FindAsync(account.Id, cancellationToken) ?? PlanSettings.DefaultFor(account.Id, account.OwnerId);

    private static decimal RemainingContribution(
        PlanSettings planSettings,
        IReadOnlyList<RebalancingReport> periodReports,
        string code,
        List<IPlanningSubPortfolio> all,
        List<IPlanningSubPortfolio> planned)
    {
        var contributed = RebalancingReport.ContributedIn(periodReports, code);

        return planSettings.RemainingContribution(contributed, planned.Sum(sub => sub.TargetShare), all.Sum(sub => sub.TargetShare));
    }

    private async Task<Dictionary<Guid, IInstrumentPrice>> HeldAndCashPricesAsync(
        IPlanningAccount account,
        IReadOnlyList<IHeldSubPortfolio> holdings,
        List<IPlanningSubPortfolio> planned,
        DateOnly asOf,
        List<string> warnings,
        CancellationToken cancellationToken)
    {
        var heldIds = holdings.SelectMany(sub => sub.Positions.Select(position => position.InstrumentId));
        var cashIds = planned.Select(sub => sub.CashInstrumentId).OfType<Guid>();

        var found = await instrumentPrices.InCurrencyAsync(heldIds.Concat(cashIds), asOf, account.BaseCurrency, cancellationToken);
        warnings.AddRange(found.Warnings);

        return new Dictionary<Guid, IInstrumentPrice>(found.Prices);
    }

    private static ContributionAllocation Allocate(
        IPlanningAccount account,
        List<IPlanningSubPortfolio> planned,
        decimal contribution,
        Dictionary<Guid, IHeldSubPortfolio> snapshots,
        Dictionary<Guid, IInstrumentPrice> prices)
    {
        var standings = planned.Select(sub => StandingOf(sub, snapshots.GetValueOrDefault(sub.Id), prices)).ToList();
        var split = ContributionAllocator.SplitOf(account.ContributionPolicy);

        return ContributionAllocator.Allocate(contribution, standings, split, Percentage.Of(account.DriftBand));
    }

    private static SubPortfolioStanding StandingOf(
        IPlanningSubPortfolio sub, IHeldSubPortfolio? snapshot, Dictionary<Guid, IInstrumentPrice> prices)
    {
        var positions = snapshot?.Positions ?? [];
        var holdings = positions.Select(position => new HoldingValuation(
            position.Quantity,
            prices.GetValueOrDefault(position.InstrumentId)?.Close,
            position.AverageCost));

        return SubPortfolioStanding.Valued(sub.Id, sub.TargetShare, snapshot?.Cash ?? 0m, holdings);
    }
}
