namespace InvestmentLab.Portfolio.Transactions.Application.ImportStatement;

public sealed record ConfigureImportCommand(string Mode, DateOnly? FromDate, Guid? CashSubPortfolioId, IReadOnlyList<TickerMappingInput> Mappings);
