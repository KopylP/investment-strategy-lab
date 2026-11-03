namespace InvestmentLab.StrategyBuilder.Funds.Domain;

public sealed record FundSearchOptions(IReadOnlyList<FundSearchChoice> Exchanges, IReadOnlyList<FundSearchChoice> Regions);
