namespace InvestmentLab.Portfolio.Accounts.Domain;

public static class TargetShares
{
    public static Result<IReadOnlyDictionary<SubPortfolioId, TargetShare>> TryFromPercents(IReadOnlyDictionary<Guid, decimal>? percents)
    {
        var shares = new Dictionary<SubPortfolioId, TargetShare>();

        foreach (var (id, percent) in percents ?? new Dictionary<Guid, decimal>())
        {
            if (id == Guid.Empty)
                return AccountErrors.TargetSharesIncomplete();

            var share = TargetShare.TryFromPercent(percent);
            if (share.IsFailure)
                return share.Error;

            shares[SubPortfolioId.Of(id)] = share.Value;
        }

        return shares;
    }
}
