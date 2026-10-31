namespace InvestmentLab.Portfolio.Transactions.Application.ImportPositions;

public sealed record LegacyPosition(string Ticker, decimal EntryPrice, decimal Shares, DateOnly? EntryDate, string Currency);
