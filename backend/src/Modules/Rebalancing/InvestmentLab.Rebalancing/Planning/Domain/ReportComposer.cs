using InvestmentLab.Rebalancing.Reports.Domain;

namespace InvestmentLab.Rebalancing.Planning.Domain;

public static class ReportComposer
{
    private const int QuantityDecimals = 8;
    private static readonly string ContributionReason = Message.Of("report.reason.contribution");

    public static ReportDraft Compose(
        ReportHeader header, ContributionAllocation? contribution, IReadOnlyList<SubPortfolioOutcome> outcomes, IReadOnlyList<string> warnings)
    {
        var netted = Netted(outcomes);

        return new ReportDraft(
            header.AccountId,
            header.OwnerId,
            header.AccountName,
            header.Currency,
            header.Period,
            header.Kind,
            header.Trigger,
            header.MarketAsOf,
            ContributionPlanOf(contribution),
            [.. outcomes.Select(Section)],
            ActionsOf(contribution, netted),
            warnings,
            null,
            header.Code,
            header.Title);
    }

    private static NettedOrders Netted(IReadOnlyList<SubPortfolioOutcome> outcomes)
    {
        var orders = outcomes.SelectMany(outcome => outcome.Orders.Select(order => new SubPortfolioOrder(outcome.SubPortfolioId, order)));

        return OrderNetting.Net([.. orders]);
    }

    private static List<ActionDraft> ActionsOf(ContributionAllocation? contribution, NettedOrders netted)
    {
        var actions = new List<ActionDraft>();

        if (contribution is { Total: > 0m })
            actions.Add(ContributionAction(contribution));

        var trades = BrokerActions(netted).Concat(TransferActions(netted));
        actions.AddRange(trades.OrderBy(action => action.Kind));

        return actions;
    }

    private static ActionDraft ContributionAction(ContributionAllocation contribution)
    {
        var paying = contribution.Shares.Where(share => share.Amount > 0m);
        var allocations = paying.Select(share => new ActionAllocation(share.SubPortfolioId, share.Amount, null));

        return new ActionDraft(ActionKind.Contribute, null, contribution.Total, 1m, [.. allocations], ContributionReason);
    }

    private static IEnumerable<ActionDraft> BrokerActions(NettedOrders netted) =>
        netted.BrokerOrders.Where(order => Cut(order.Quantity) > 0m).Select(BrokerAction);

    private static ActionDraft BrokerAction(BrokerOrder order)
    {
        var allocations =
            order.Allocations.Select(allocation => new ActionAllocation(allocation.SubPortfolioId, Cut(allocation.Quantity), allocation.Reason));

        return new ActionDraft(KindOf(order.Kind), order.InstrumentId, Cut(order.Quantity), order.Price, [.. allocations], order.Reason);
    }

    private static IEnumerable<ActionDraft> TransferActions(NettedOrders netted) =>
        netted.Transfers.Where(transfer => Cut(transfer.Quantity) > 0m).Select(TransferAction);

    private static ActionDraft TransferAction(InternalTransfer transfer)
    {
        var quantity = Cut(transfer.Quantity);
        var allocation = new ActionAllocation(transfer.ToSubPortfolioId, quantity, transfer.Reason);

        return new ActionDraft(
            ActionKind.Transfer,
            transfer.InstrumentId,
            quantity,
            transfer.Price,
            [allocation],
            transfer.Reason,
            transfer.FromSubPortfolioId,
            transfer.ToSubPortfolioId);
    }

    private static ContributionPlan ContributionPlanOf(ContributionAllocation? contribution)
    {
        if (contribution is null)
            return ContributionPlan.None;

        var parts = contribution.Shares.Select(share => new ContributionPart(share.SubPortfolioId, share.Amount));
        var hint = contribution.Transfer is { } transfer
            ? new TransferHint(transfer.FromSubPortfolioId, transfer.ToSubPortfolioId, transfer.Amount)
            : null;

        return new ContributionPlan(contribution.Total, contribution.Split.ToString(), [.. parts], hint);
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
        [.. outcome.Orders.Select(SectionOrderOf)],
        [.. (outcome.Plan?.Allocation ?? []).Select(SectionAllocationOf)],
        decimal.Round(outcome.Plan?.TotalBefore ?? 0m, 2),
        decimal.Round(outcome.Plan?.TotalAfter ?? 0m, 2),
        decimal.Round(outcome.Plan?.ParkedBefore ?? 0m, 2),
        decimal.Round(outcome.Plan?.ParkedAfter ?? 0m, 2),
        [.. outcome.Notes, .. outcome.Plan?.Notes ?? []],
        outcome.Risk,
        outcome.Error);

    private static SectionOrder SectionOrderOf(PlannedOrder order) =>
        new(order.Kind.ToString(), order.InstrumentId, Cut(order.Quantity), order.Price, decimal.Round(order.Amount, 2), order.Reason);

    private static SectionAllocation SectionAllocationOf(AllocationRow row) => new(
        row.InstrumentId,
        row.Role.ToString(),
        decimal.Round(row.ValueBefore, 2),
        decimal.Round(row.ValueAfter, 2),
        decimal.Round(row.WeightBefore, 6),
        decimal.Round(row.WeightAfter, 6),
        row.TargetWeight is { } target ? decimal.Round(target, 6) : null);

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
