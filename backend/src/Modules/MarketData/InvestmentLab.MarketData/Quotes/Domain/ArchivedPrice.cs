namespace InvestmentLab.MarketData.Quotes.Domain;

public readonly record struct ArchivedPrice(DateOnly Date, decimal Close, decimal AdjustedClose);
