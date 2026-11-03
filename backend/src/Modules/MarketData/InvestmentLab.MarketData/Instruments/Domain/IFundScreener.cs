namespace InvestmentLab.MarketData.Instruments.Domain;

public interface IFundScreener
{
    FundScreenOptions Options();

    Task<IReadOnlyList<FundScreenMatch>?> ScreenAsync(FundScreenQuery query, CancellationToken cancellationToken);
}
