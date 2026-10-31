namespace InvestmentLab.MarketData.Quotes.Application.IngestDailyQuotes;

public sealed record IngestReport(int Instruments, int QuotesAdded, int QuotesUpdated, IReadOnlyList<string> MissingSymbols);
