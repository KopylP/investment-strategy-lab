namespace InvestmentLab.Taxation.Assessments.Domain;

public sealed record LotMatch(
    Guid BuyTransactionId,
    DateOnly AcquiredOn,
    decimal Quantity,
    decimal CostPln,
    bool FromBroker = false,
    decimal Gross = 0m,
    decimal FxRate = 0m);
