namespace InvestmentLab.Taxation.Contracts;

public sealed record ProposedSaleDto(Guid Key, Guid InstrumentId, decimal Quantity, decimal Price, IReadOnlyList<Guid> SubPortfolioIds);
