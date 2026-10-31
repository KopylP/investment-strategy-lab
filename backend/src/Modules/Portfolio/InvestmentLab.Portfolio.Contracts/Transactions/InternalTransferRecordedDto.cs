namespace InvestmentLab.Portfolio.Contracts.Transactions;

public sealed record InternalTransferRecordedDto(
    Guid FromSubPortfolioId,
    Guid ToSubPortfolioId,
    Guid? InstrumentId,
    decimal? Quantity,
    decimal? Amount);
