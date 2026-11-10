using InvestmentLab.Portfolio.Accounts.Domain;

namespace InvestmentLab.Portfolio.Transactions.Domain;

public static class StatementSuggestions
{
    public static Guid? SubPortfolioFor(Guid? instrumentId, SubPortfolioEvidence evidence)
    {
        var active = evidence.Account.ActiveSubPortfolios.ToList();

        var chosen = instrumentId is { } instrument
            ? LargestHolder(active, instrument, evidence) ?? UniverseHolder(active, instrument, evidence) ?? LargestShare(active)
            : LargestShare(active);

        return chosen?.Id.Value;
    }

    public static Guid? CashSubPortfolio(InvestmentAccount account, IReadOnlyList<StatementRow> rows, IReadOnlyList<TickerMapping> mappings)
    {
        var mostSpentOn = MostSpentOn(rows, mappings);
        var largestShare = LargestShare([.. account.ActiveSubPortfolios]);

        return mostSpentOn ?? largestShare?.Id.Value;
    }

    private static SubPortfolio? LargestHolder(IReadOnlyList<SubPortfolio> active, Guid instrument, SubPortfolioEvidence evidence)
    {
        var holders = active.Where(sub => evidence.Holdings.UnitsOf(sub.Id, instrument).Value > 0m);

        return holders.OrderByDescending(sub => evidence.Holdings.UnitsOf(sub.Id, instrument).Value).FirstOrDefault();
    }

    private static SubPortfolio? UniverseHolder(IReadOnlyList<SubPortfolio> active, Guid instrument, SubPortfolioEvidence evidence) =>
        active.FirstOrDefault(sub => HoldsInItsUniverse(sub, instrument, evidence.Universes));

    private static SubPortfolio? LargestShare(IReadOnlyList<SubPortfolio> active) =>
        active.OrderByDescending(sub => sub.TargetShare.Value.Value).ThenBy(sub => sub.Position).FirstOrDefault();

    private static bool HoldsInItsUniverse(SubPortfolio sub, Guid instrument, IReadOnlyDictionary<Guid, IReadOnlyList<Guid>> universes) =>
        sub.Strategy is { } strategy && universes.TryGetValue(strategy.StrategyId, out var universe) && universe.Contains(instrument);

    private static Guid? MostSpentOn(IReadOnlyList<StatementRow> rows, IReadOnlyList<TickerMapping> mappings)
    {
        var buys = rows.Where(row => row.Kind == StatementRowKind.Buy);
        var spent = buys.Select(row => (Sub: SubPortfolioOfTicker(row.Ticker, mappings), row.Amount)).Where(entry => entry.Sub is not null);

        return spent
            .GroupBy(entry => entry.Sub!.Value)
            .OrderByDescending(group => group.Sum(entry => entry.Amount))
            .Select(group => (Guid?)group.Key)
            .FirstOrDefault();
    }

    private static Guid? SubPortfolioOfTicker(string? ticker, IReadOnlyList<TickerMapping> mappings)
    {
        var mapping = mappings.FirstOrDefault(each => string.Equals(each.Ticker, ticker, StringComparison.OrdinalIgnoreCase));

        return mapping?.SubPortfolioId;
    }
}
