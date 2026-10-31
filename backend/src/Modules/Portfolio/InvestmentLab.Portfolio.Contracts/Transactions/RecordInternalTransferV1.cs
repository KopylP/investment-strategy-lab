namespace InvestmentLab.Portfolio.Contracts.Transactions;

public sealed record RecordInternalTransferV1(
    Guid AccountId,
    string SourceReference,
    Guid FromSubPortfolioId,
    Guid ToSubPortfolioId,
    DateOnly OccurredOn,
    Guid? InstrumentId,
    decimal? Quantity,
    decimal? Amount);
