namespace InvestmentLab.StrategyBuilder.Funds.Domain;

public sealed record FundCriteria(
    string? ExchangeId,
    string? RegionId,
    string? FundFamily,
    decimal? MinAssets,
    decimal? MaxExpenseRatio,
    IReadOnlyList<string> NameWords)
{
    private const int MaxNameWords = 5;
    private const decimal MaxYearlyFee = 0.1m;

    public static Result<FundCriteria> TryOf(
        string? exchangeId,
        string? regionId,
        string? fundFamily,
        decimal? minAssets,
        decimal? maxExpenseRatio,
        IReadOnlyList<string> nameWords)
    {
        if (string.IsNullOrWhiteSpace(exchangeId) && string.IsNullOrWhiteSpace(regionId))
            return FundErrors.PlaceRequired();
        if (nameWords.Count > MaxNameWords)
            return FundErrors.TooManyWords();
        if (minAssets < 0)
            return FundErrors.InvalidAssets();
        if (maxExpenseRatio is < 0 or > MaxYearlyFee)
            return FundErrors.InvalidExpenseRatio();

        var words = nameWords.Where(word => !string.IsNullOrWhiteSpace(word)).Select(word => word.Trim()).ToList();

        return new FundCriteria(
            ExchangeId: TextOf(exchangeId),
            RegionId: TextOf(regionId),
            FundFamily: TextOf(fundFamily),
            MinAssets: minAssets,
            MaxExpenseRatio: maxExpenseRatio,
            NameWords: words);
    }

    private static string? TextOf(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
