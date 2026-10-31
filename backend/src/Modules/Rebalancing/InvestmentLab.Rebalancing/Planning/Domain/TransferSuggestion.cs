namespace InvestmentLab.Rebalancing.Planning.Domain;

public sealed record TransferSuggestion(Guid FromSubPortfolioId, Guid ToSubPortfolioId, decimal Amount)
{
    public string WarningFor(string fromName, string toName, Percentage driftBand, Currency currency) =>
        $"Під-портфелі відхилилися від цільових часток більше ніж на {driftBand}: " +
        $"варто перенести {Amount:0.00} {currency} з «{fromName}» до «{toName}».";
}
