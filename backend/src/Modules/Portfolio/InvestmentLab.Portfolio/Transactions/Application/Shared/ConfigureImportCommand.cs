namespace InvestmentLab.Portfolio.Transactions.Application.Shared;

public sealed record ConfigureImportCommand(string Mode, DateOnly? FromDate, Guid? CashSubPortfolioId, IReadOnlyList<TickerMappingChoice> Mappings);
