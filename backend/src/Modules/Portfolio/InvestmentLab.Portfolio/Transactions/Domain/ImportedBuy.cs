namespace InvestmentLab.Portfolio.Transactions.Domain;

public sealed record ImportedBuy(string Ticker, decimal Shares, decimal EntryPrice, string Currency, DateOnly Date);
