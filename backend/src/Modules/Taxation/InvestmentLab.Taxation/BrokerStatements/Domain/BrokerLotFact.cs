namespace InvestmentLab.Taxation.BrokerStatements.Domain;

public sealed record BrokerLotFact(DateOnly AcquiredOn, decimal Quantity, decimal Cost, decimal CostInTaxCurrency)
{
    private const int TaxCostDigits = 2;

    public BrokerLotFact AtTaxCurrencyRate(decimal rate) =>
        this with { CostInTaxCurrency = decimal.Round(Cost * rate, TaxCostDigits, MidpointRounding.AwayFromZero) };
}
