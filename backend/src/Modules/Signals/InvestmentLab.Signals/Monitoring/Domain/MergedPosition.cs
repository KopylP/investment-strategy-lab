namespace InvestmentLab.Signals.Monitoring.Domain;

public sealed record MergedPosition(Guid InstrumentId, decimal AverageCost, DateOnly FirstOpenedOn)
{
    public static IReadOnlyList<MergedPosition> AllOf(IEnumerable<IOpenPosition> positions)
    {
        var held = positions.Where(position => position.Quantity > 0m);

        return [.. held.GroupBy(position => position.InstrumentId).Select(Merged)];
    }

    private static MergedPosition Merged(IGrouping<Guid, IOpenPosition> parts)
    {
        var quantity = parts.Sum(part => part.Quantity);
        var cost = parts.Sum(part => part.CostBasis);
        var firstOpenedOn = parts.Min(part => part.FirstOpenedOn);

        return new MergedPosition(parts.Key, cost / quantity, firstOpenedOn);
    }
}
