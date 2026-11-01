using InvestmentLab.Portfolio.Accounts.Domain;

namespace InvestmentLab.Portfolio.Transactions.Domain;

public static class StatementSuggestions
{
    public static Guid? SubPortfolioFor(Guid? instrumentId, InvestmentAccount account, IReadOnlyDictionary<Guid, IReadOnlyList<Guid>> universes)
    {
        if (account.ActiveSubPortfolios.ToList() is [var only])
            return only.Id.Value;

        if (instrumentId is not { } instrument)
            return null;

        var holder = account.ActiveSubPortfolios.FirstOrDefault(sub => HoldsInItsUniverse(sub, instrument, universes));

        return holder?.Id.Value;
    }

    public static Guid? CashSubPortfolio(InvestmentAccount account, IReadOnlyList<StatementRow> rows, IReadOnlyList<TickerMapping> mappings)
    {
        var buys = rows.Where(row => row.Kind == StatementRowKind.Buy);
        var spent = buys.Select(row => (Sub: SubPortfolioOfTicker(row.Ticker, mappings), row.Amount)).Where(entry => entry.Sub is not null);

        var mostSpentOn = spent
            .GroupBy(entry => entry.Sub!.Value)
            .OrderByDescending(group => group.Sum(entry => entry.Amount))
            .Select(group => (Guid?)group.Key)
            .FirstOrDefault();

        return mostSpentOn ?? account.ActiveSubPortfolios.FirstOrDefault()?.Id.Value;
    }

    private static bool HoldsInItsUniverse(SubPortfolio sub, Guid instrument, IReadOnlyDictionary<Guid, IReadOnlyList<Guid>> universes) =>
        sub.Strategy is { } strategy && universes.TryGetValue(strategy.StrategyId, out var universe) && universe.Contains(instrument);

    private static Guid? SubPortfolioOfTicker(string? ticker, IReadOnlyList<TickerMapping> mappings)
    {
        var mapping = mappings.FirstOrDefault(each => string.Equals(each.Ticker, ticker, StringComparison.OrdinalIgnoreCase));

        return mapping?.SubPortfolioId;
    }
}
