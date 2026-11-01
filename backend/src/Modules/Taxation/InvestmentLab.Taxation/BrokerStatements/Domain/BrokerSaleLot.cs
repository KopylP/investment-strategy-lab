using System.Text.Json.Serialization;

namespace InvestmentLab.Taxation.BrokerStatements.Domain;

public sealed record BrokerSaleLot(
    DateOnly AcquiredOn,
    DateOnly SoldOn,
    string Symbol,
    string Isin,
    string Currency,
    decimal Quantity,
    decimal Cost,
    decimal Proceeds,
    decimal Fees,
    [property: JsonPropertyName("costPln")] decimal CostInTaxCurrency,
    [property: JsonPropertyName("proceedsPln")] decimal ProceedsInTaxCurrency,
    [property: JsonPropertyName("feesPln")] decimal FeesInTaxCurrency);
