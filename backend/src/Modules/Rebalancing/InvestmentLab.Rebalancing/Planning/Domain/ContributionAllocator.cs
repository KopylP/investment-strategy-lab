namespace InvestmentLab.Rebalancing.Planning.Domain;

public static class ContributionAllocator
{
    public static ContributionSplit SplitOf(string policy) =>
        Enum.TryParse<ContributionSplit>(policy, out var split) ? split : ContributionSplit.FillUnderweight;

    public static ContributionAllocation Allocate(
        decimal contribution, IReadOnlyList<SubPortfolioStanding> subPortfolios, ContributionSplit split, Percentage driftBand)
    {
        var amount = decimal.Round(Math.Max(0m, contribution), 2);
        var totalAfter = subPortfolios.Sum(sub => sub.Value) + amount;

        var raw = split switch
        {
            ContributionSplit.FixedSplit => subPortfolios.ToDictionary(sub => sub.SubPortfolioId, sub => amount * sub.TargetShare.Value),
            _ => FillUnderweight(amount, totalAfter, subPortfolios),
        };
        var shares = RoundToCents(amount, subPortfolios, raw);

        return new ContributionAllocation(amount, split, shares, SuggestTransfer(subPortfolios, shares, totalAfter, driftBand));
    }

    private static Dictionary<Guid, decimal> FillUnderweight(decimal amount, decimal totalAfter, IReadOnlyList<SubPortfolioStanding> subPortfolios)
    {
        var gaps = subPortfolios.ToDictionary(sub => sub.SubPortfolioId, sub => Math.Max(0m, sub.TargetShare.Value * totalAfter - sub.Value));
        var totalGap = gaps.Values.Sum();

        return totalGap > 0m
            ? gaps.ToDictionary(gap => gap.Key, gap => amount * gap.Value / totalGap)
            : subPortfolios.ToDictionary(sub => sub.SubPortfolioId, sub => amount * sub.TargetShare.Value);
    }

    private static List<ContributionShare> RoundToCents(
        decimal amount,
        IReadOnlyList<SubPortfolioStanding> subPortfolios,
        Dictionary<Guid, decimal> raw)
    {
        var shares = subPortfolios.Select(sub => new ContributionShare(sub.SubPortfolioId, decimal.Round(raw[sub.SubPortfolioId], 2))).ToList();
        var rest = amount - shares.Sum(share => share.Amount);
        if (rest != 0m && shares.Count > 0)
        {
            var largest = shares.Index().MaxBy(entry => entry.Item.Amount).Index;
            shares[largest] = shares[largest] with { Amount = shares[largest].Amount + rest };
        }

        return shares;
    }

    private static TransferSuggestion? SuggestTransfer(
        IReadOnlyList<SubPortfolioStanding> subPortfolios, List<ContributionShare> shares, decimal totalAfter, Percentage driftBand)
    {
        if (totalAfter <= 0m || subPortfolios.Count < 2)
            return null;

        var drift = subPortfolios
            .Select(sub => (Sub: sub, Excess: sub.Value + shares.First(share => share.SubPortfolioId == sub.SubPortfolioId).Amount - sub.TargetShare.Value * totalAfter))
            .ToList();
        var (over, overExcess) = drift.MaxBy(entry => entry.Excess);
        var (under, underExcess) = drift.MinBy(entry => entry.Excess);

        if (overExcess / totalAfter <= driftBand.Value && -underExcess / totalAfter <= driftBand.Value)
            return null;

        var amount = decimal.Round(Math.Min(overExcess, -underExcess), 2);

        return amount > 0m ? new TransferSuggestion(over.SubPortfolioId, under.SubPortfolioId, amount) : null;
    }
}
