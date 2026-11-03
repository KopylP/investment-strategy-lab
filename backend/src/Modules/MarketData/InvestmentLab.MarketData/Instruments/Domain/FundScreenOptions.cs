namespace InvestmentLab.MarketData.Instruments.Domain;

public sealed record FundScreenOptions(IReadOnlyList<FundScreenChoice> Exchanges, IReadOnlyList<FundScreenChoice> Regions);
