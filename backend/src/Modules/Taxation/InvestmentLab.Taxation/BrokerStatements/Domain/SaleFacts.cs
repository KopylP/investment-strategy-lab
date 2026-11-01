namespace InvestmentLab.Taxation.BrokerStatements.Domain;

public sealed record SaleFacts(IReadOnlyList<BrokerLotFact> Lots, decimal GainInTaxCurrency, string Currency, decimal? Cost)
{
    public SaleFacts AtTaxRates(IReadOnlyList<BrokerLotFact> lots, string? entryCurrency) =>
        this with { Lots = lots, Cost = entryCurrency == Currency ? Cost : null };
}
