namespace InvestmentLab.Portfolio.Transactions.Application.Shared;

public sealed record StatementImportDto(
    Guid Id,
    string Broker,
    string FileName,
    DateTimeOffset UploadedAt,
    string Status,
    string Mode,
    DateOnly? FromDate,
    Guid? CashSubPortfolioId,
    string AccountCurrency,
    IReadOnlyList<TickerMappingDto> Mappings,
    IReadOnlyList<StatementRowDto> Rows,
    ImportCountsDto Counts,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<SubPortfolioOptionDto> SubPortfolios,
    DateTimeOffset? ConfirmedAt,
    DateTimeOffset? UndoneAt);
