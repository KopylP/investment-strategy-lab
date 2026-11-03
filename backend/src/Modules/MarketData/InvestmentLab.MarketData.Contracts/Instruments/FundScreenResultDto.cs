namespace InvestmentLab.MarketData.Contracts.Instruments;

public sealed record FundScreenResultDto(IReadOnlyList<ScreenedFundDto> Funds, string? ErrorCode);
