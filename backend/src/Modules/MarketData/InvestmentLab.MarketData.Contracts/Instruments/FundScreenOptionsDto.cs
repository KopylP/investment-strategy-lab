namespace InvestmentLab.MarketData.Contracts.Instruments;

public sealed record FundScreenOptionsDto(IReadOnlyList<ScreenChoiceDto> Exchanges, IReadOnlyList<ScreenChoiceDto> Regions);
