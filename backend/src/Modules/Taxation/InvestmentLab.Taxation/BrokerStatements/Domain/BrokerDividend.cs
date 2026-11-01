using System.Text.Json.Serialization;

namespace InvestmentLab.Taxation.BrokerStatements.Domain;

public sealed record BrokerDividend(
    DateOnly PaidOn,
    string Symbol,
    string Isin,
    string Currency,
    decimal Gross,
    decimal Withheld,
    decimal Net,
    [property: JsonPropertyName("grossPln")] decimal GrossInTaxCurrency,
    [property: JsonPropertyName("withheldPln")] decimal WithheldInTaxCurrency);
