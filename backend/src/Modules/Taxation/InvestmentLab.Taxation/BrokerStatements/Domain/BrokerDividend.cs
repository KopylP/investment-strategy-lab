namespace InvestmentLab.Taxation.BrokerStatements.Domain;

public sealed record BrokerDividend(
    DateOnly PaidOn,
    string Symbol,
    string Isin,
    string Currency,
    decimal Gross,
    decimal Withheld,
    decimal Net,
    decimal GrossPln,
    decimal WithheldPln);
