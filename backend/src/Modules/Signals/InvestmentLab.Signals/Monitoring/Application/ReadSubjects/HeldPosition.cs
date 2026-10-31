using InvestmentLab.MarketData.Contracts.Instruments;
using InvestmentLab.Portfolio.Contracts.Holdings;

namespace InvestmentLab.Signals.Monitoring.Application.ReadSubjects;

public sealed record HeldPosition(QuoteSubject Subject, decimal AverageCost, DateOnly FirstOpenedOn)
{
    public static List<HeldPosition> AllOf(AccountHoldingsDto holdings, IReadOnlyList<InstrumentDto> catalog)
    {
        var open = holdings.SubPortfolios
            .SelectMany(subPortfolio => subPortfolio.Positions)
            .Where(position => position.Quantity > 0m);

        return [.. open
            .GroupBy(position => position.InstrumentId)
            .Select(parts => Merged(parts, catalog))];
    }

    private static HeldPosition Merged(IGrouping<Guid, PositionDto> parts, IReadOnlyList<InstrumentDto> catalog)
    {
        var quantity = parts.Sum(part => part.Quantity);
        var cost = parts.Sum(part => part.CostBasis);
        var firstOpenedOn = parts.Min(part => part.FirstOpenedOn);

        return new HeldPosition(SubjectOf(parts.Key, catalog), cost / quantity, firstOpenedOn);
    }

    private static QuoteSubject SubjectOf(Guid instrumentId, IReadOnlyList<InstrumentDto> catalog)
    {
        var key = InstrumentLabels.KeyOf(instrumentId);
        var label = InstrumentLabels.LabelOf(catalog, instrumentId);

        return new QuoteSubject(instrumentId, key, label);
    }
}
