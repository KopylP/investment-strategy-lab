namespace InvestmentLab.Rebalancing.Planning.Domain;

public sealed class PlanNotes(SubPortfolioSituation situation, string parking, Func<Guid, string> label)
{
    private readonly decimal _contribution = situation.Contribution;
    private readonly string _currency = situation.Currency.Code;
    private readonly decimal _minTrade = situation.Policy.MinTrade;
    private readonly StrategyTargets _strategy = situation.Strategy;

    public IEnumerable<string> OfContribution()
    {
        if (_contribution <= 0m)
            yield return Message.Of("report.note.zero_contribution");
    }

    public IEnumerable<string> OfMarket(decimal parkedBefore)
    {
        var parkedEnough = parkedBefore >= _minTrade;

        if (_strategy.MarketIsFavorable)
        {
            if (parkedEnough)
                yield return Message.Of("report.note.market_recovered", new { parked = parkedBefore, currency = _currency });

            yield break;
        }

        if (InvestsPartOfContribution())
            yield return WeakMarket();

        if (parkedEnough)
            yield return Message.Of("report.note.parked_waits", new { parked = parkedBefore, currency = _currency, parking });
    }

    public IEnumerable<string> OfTargets(int picked)
    {
        if (picked == 0)
            yield return Message.Of("report.note.nothing_picked", new { parking });
        else if (_strategy.CoversLessThanWhole)
            yield return Message.Of("report.note.partial_targets", new { total = _strategy.TotalWeight, parking });
    }

    public IEnumerable<string> OfSkipped(SkippedTrades skipped)
    {
        if (skipped.SmallBuys.Count > 0)
            yield return Message.Of("report.note.small_buys_skipped", new { minimum = _minTrade, currency = _currency, labels = Labels(skipped.SmallBuys) });

        if (skipped.BuysBelowOneShare.Count > 0)
            yield return Message.Of("report.note.buys_below_one_share", new { labels = Labels(skipped.BuysBelowOneShare), parking });

        foreach (var id in skipped.UnpricedBuys)
            yield return Message.Of("report.note.no_price", new { label = label(id) });

        if (skipped.TrimsAtLoss.Count > 0)
            yield return Message.Of("report.note.trims_skipped_at_loss", new { labels = Labels(skipped.TrimsAtLoss) });

        if (skipped.TrimsBySetting.Count > 0)
            yield return Message.Of("report.note.trims_skipped_by_setting", new { labels = Labels(skipped.TrimsBySetting) });
    }

    private bool InvestsPartOfContribution() => _strategy.InvestFraction.Value < 1m && _contribution > 0m;

    private string WeakMarket()
    {
        var fraction = _strategy.InvestFraction.Value;

        return Message.Of("report.note.weak_market", new
        {
            fraction,
            invested = _contribution * fraction,
            rest = _contribution * (1m - fraction),
            currency = _currency,
            parking,
        });
    }

    private string[] Labels(List<Guid> ids) => [.. ids.Select(label)];
}
