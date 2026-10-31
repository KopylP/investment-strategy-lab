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
    decimal CostPln,
    decimal ProceedsPln,
    decimal FeesPln);
