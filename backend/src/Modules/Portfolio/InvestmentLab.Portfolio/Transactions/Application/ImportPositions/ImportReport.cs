namespace InvestmentLab.Portfolio.Transactions.Application.ImportPositions;

public sealed record ImportReport(int Recorded, int Skipped, IReadOnlyList<string> Warnings);
