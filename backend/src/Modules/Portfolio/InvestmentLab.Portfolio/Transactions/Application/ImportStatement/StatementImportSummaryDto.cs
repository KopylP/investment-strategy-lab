namespace InvestmentLab.Portfolio.Transactions.Application.ImportStatement;

public sealed record StatementImportSummaryDto(Guid Id, string Broker, string FileName, DateTimeOffset UploadedAt, string Status, string Mode, int Recorded);
