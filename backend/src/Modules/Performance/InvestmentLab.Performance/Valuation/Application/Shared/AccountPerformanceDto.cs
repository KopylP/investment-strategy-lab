namespace InvestmentLab.Performance.Valuation.Application.Shared;

public sealed record AccountPerformanceDto(Guid Id, string Name, string Broker, string BaseCurrency, decimal Value, decimal Share, decimal NetContributions, decimal? Twr);
