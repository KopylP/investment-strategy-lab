namespace InvestmentLab.Backtesting.Replays.Domain;

public sealed class ReplayOrders
{
    private List<PendingReplayOrder> _pending = [];

    public void Rebalance(IReadOnlyList<ReplayTrade> trades, DateOnly day, ReplayExplanation? explanation)
    {
        var exits = _pending.Where(order => order.IsRiskExit).ToList();
        var blocked = exits.Select(order => order.Trade.InstrumentId).ToHashSet();
        var allowed = trades.Where(trade => !blocked.Contains(trade.InstrumentId));
        var orders = allowed.Select(trade => OrderOf(trade, day, explanation));

        _pending = [.. exits, .. orders];
    }

    public void ExitPositions(IReadOnlyList<BookPosition> positions, ReplayRiskDecision assessment, DateOnly day)
    {
        var exiting = positions.Where(position => assessment.Exits.Contains(position.InstrumentId)).ToList();
        var identifiers = exiting.Select(position => position.InstrumentId).ToHashSet();
        var others = _pending.Where(order => !identifiers.Contains(order.Trade.InstrumentId));
        var exits = exiting.Select(position => ExitOf(position, day, assessment.Explanation));

        _pending = [.. others, .. exits];
    }

    public IReadOnlyList<PendingReplayOrder> TakeForExecution()
    {
        var orders = _pending.OrderByDescending(order => order.Trade.IsSale).ToList();
        _pending = [];

        return orders;
    }

    public void Keep(PendingReplayOrder order) => _pending.Add(order);

    private static PendingReplayOrder OrderOf(ReplayTrade trade, DateOnly day, ReplayExplanation? explanation)
    {
        var reason = ReasonOf(trade.InstrumentId, explanation);

        return new PendingReplayOrder(trade, day, reason);
    }

    private static PendingReplayOrder ExitOf(BookPosition position, DateOnly day, ReplayExplanation? explanation)
    {
        var trade = new ReplayTrade(
            InstrumentId: position.InstrumentId,
            IsSale: true,
            Quantity: position.Quantity,
            Amount: 0m);
        var reason = ReasonOf(position.InstrumentId, explanation);

        return new PendingReplayOrder(trade, day, reason, IsRiskExit: true);
    }

    private static string ReasonOf(Guid instrumentId, ReplayExplanation? explanation)
    {
        var candidate = explanation?.Candidates?.FirstOrDefault(item => item.InstrumentId == instrumentId);

        return candidate?.Reason ?? explanation?.Summary ?? Message.Of("research.journal.target");
    }
}
