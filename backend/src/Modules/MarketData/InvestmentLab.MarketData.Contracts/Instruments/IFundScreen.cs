namespace InvestmentLab.MarketData.Contracts.Instruments;

public interface IFundScreen
{
    FundScreenOptionsDto Options();

    Task<FundScreenResultDto> ScreenAsync(FundScreenCriteriaDto criteria, CancellationToken cancellationToken);
}
