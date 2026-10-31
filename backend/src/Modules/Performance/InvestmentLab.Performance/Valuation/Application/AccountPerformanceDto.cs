namespace InvestmentLab.Performance.Valuation.Application;

public sealed record AccountPerformanceDto(Guid Id, string Name, string Broker, string BaseCurrency, decimal Value, decimal Share, decimal NetContributions, decimal? Twr);
