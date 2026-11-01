namespace InvestmentLab.Rebalancing.Planning.Domain;

public sealed record TransferSuggestion(Guid FromSubPortfolioId, Guid ToSubPortfolioId, decimal Amount)
{
    public string WarningFor(string fromName, string toName, Percentage driftBand, Currency currency) =>
        Message.Of("report.warning.transfer_suggestion", new
        {
            band = driftBand.Value,
            amount = Amount,
            currency = currency.Code,
            from = fromName,
            to = toName,
        });
}
