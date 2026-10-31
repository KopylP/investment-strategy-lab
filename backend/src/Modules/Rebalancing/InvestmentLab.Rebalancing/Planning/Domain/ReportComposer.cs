using InvestmentLab.Rebalancing.Reports.Domain;

namespace InvestmentLab.Rebalancing.Planning.Domain;

public static class ReportComposer
{
    private const int QuantityDecimals = 8;

    public static ReportDraft Compose(
        ReportHeader header, ContributionAllocation? contribution, IReadOnlyList<SubPortfolioOutcome> outcomes, IReadOnlyList<string> warnings)
    {
        var netted = OrderNetting.Net([.. outcomes.SelectMany(outcome => outcome.Orders.Select(order => new SubPortfolioOrder(outcome.SubPortfolioId, order)))]);

        var actions = new List<ActionDraft>();

        if (contribution is { Total: > 0m })
            actions.Add(new ActionDraft(
                ActionKind.Contribute, null, contribution.Total, 1m,
                [.. contribution.Shares.Where(share => share.Amount > 0m).Select(share => new ActionAllocation(share.SubPortfolioId, share.Amount, null))],
                "щомісячний внесок на рахунок"));

        var brokerActions = netted.BrokerOrders
            .Select(order => (Order: order, Quantity: Cut(order.Quantity)))
            .Where(entry => entry.Quantity > 0m)
            .Select(entry => new ActionDraft(
                KindOf(entry.Order.Kind), entry.Order.InstrumentId, entry.Quantity, entry.Order.Price,
                [.. entry.Order.Allocations.Select(allocation => new ActionAllocation(allocation.SubPortfolioId, Cut(allocation.Quantity), allocation.Reason))],
                entry.Order.Reason))
            .ToList();
        var transfers = netted.Transfers
            .Where(transfer => Cut(transfer.Quantity) > 0m)
            .Select(transfer => new ActionDraft(
                ActionKind.Transfer, transfer.InstrumentId, Cut(transfer.Quantity), transfer.Price,
                [new ActionAllocation(transfer.ToSubPortfolioId, Cut(transfer.Quantity), transfer.Reason)],
                transfer.Reason, transfer.FromSubPortfolioId, transfer.ToSubPortfolioId));

        actions.AddRange(brokerActions.Concat(transfers).OrderBy(action => action.Kind));

        var contributionPlan = contribution is null
            ? ContributionPlan.None
            : new ContributionPlan(
                contribution.Total,
                contribution.Split.ToString(),
                [.. contribution.Shares.Select(share => new ContributionPart(share.SubPortfolioId, share.Amount))],
                contribution.Transfer is { } hint ? new TransferHint(hint.FromSubPortfolioId, hint.ToSubPortfolioId, hint.Amount) : null);

        return new ReportDraft(
            header.AccountId,
            header.OwnerId,
            header.AccountName,
            header.Currency,
            header.Period,
            header.Kind,
            header.Trigger,
            header.MarketAsOf,
            contributionPlan,
            [.. outcomes.Select(Section)],
            actions,
            warnings,
            null,
            header.Code,
            header.Title);
    }

    private static ReportSection Section(SubPortfolioOutcome outcome) => new(
        outcome.SubPortfolioId,
        outcome.Name,
        outcome.TargetShare,
        outcome.Strategy?.StrategyId,
        outcome.Strategy?.Version,
        outcome.Strategy?.Code,
        outcome.Strategy?.EngineVersion,
        outcome.Contribution,
        outcome.Decision,
        [.. outcome.Orders.Select(order => new SectionOrder(order.Kind.ToString(), order.InstrumentId, Cut(order.Quantity), order.Price, decimal.Round(order.Amount, 2), order.Reason))],
        [.. (outcome.Plan?.Allocation ?? []).Select(row => new SectionAllocation(
            row.InstrumentId, row.Role.ToString(), decimal.Round(row.ValueBefore, 2), decimal.Round(row.ValueAfter, 2),
            decimal.Round(row.WeightBefore, 6), decimal.Round(row.WeightAfter, 6), row.TargetWeight is { } target ? decimal.Round(target, 6) : null))],
        decimal.Round(outcome.Plan?.TotalBefore ?? 0m, 2),
        decimal.Round(outcome.Plan?.TotalAfter ?? 0m, 2),
        decimal.Round(outcome.Plan?.ParkedBefore ?? 0m, 2),
        decimal.Round(outcome.Plan?.ParkedAfter ?? 0m, 2),
        [.. outcome.Notes, .. outcome.Plan?.Notes ?? []],
        outcome.Risk,
        outcome.Error);

    private static ActionKind KindOf(OrderKind kind) => kind switch
    {
        OrderKind.Exit or OrderKind.Trim => ActionKind.Sell,
        OrderKind.Unpark => ActionKind.Unpark,
        OrderKind.Buy => ActionKind.Buy,
        OrderKind.Park => ActionKind.Park,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };

    private static decimal Cut(decimal quantity) => decimal.Round(quantity, QuantityDecimals, MidpointRounding.ToZero);
}
