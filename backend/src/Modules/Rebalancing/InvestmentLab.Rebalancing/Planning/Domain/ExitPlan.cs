namespace InvestmentLab.Rebalancing.Planning.Domain;

public sealed record ExitPlan(IReadOnlyList<PlannedOrder> Orders, IReadOnlyList<string> Notes)
{
    private const string ExitKind = "exit";

    public static ExitPlan Of(
        IEnumerable<RiskSignal> signals,
        IReadOnlyDictionary<Guid, decimal> heldQuantities,
        IReadOnlyDictionary<Guid, decimal> closes,
        Func<Guid, string> labelOf)
    {
        var orders = new List<PlannedOrder>();
        var notes = new List<string>();

        foreach (var signal in signals)
        {
            if (signal.Kind == ExitKind
                && heldQuantities.TryGetValue(signal.InstrumentId, out var quantity)
                && closes.TryGetValue(signal.InstrumentId, out var close))
                orders.Add(ExitOrder(signal, quantity, close));
            else
                notes.Add($"{labelOf(signal.InstrumentId)}: {signal.Reason}");
        }

        return new ExitPlan(orders, notes);
    }

    private static PlannedOrder ExitOrder(RiskSignal signal, decimal quantity, decimal close) => new(
        OrderKind.Exit,
        signal.InstrumentId,
        quantity,
        close,
        quantity * close,
        Message.Of("report.reason.exit_signal", new { reason = signal.Reason }));
}
