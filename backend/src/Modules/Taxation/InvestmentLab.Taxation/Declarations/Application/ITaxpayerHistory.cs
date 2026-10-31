namespace InvestmentLab.Taxation.Declarations.Application;

public interface ITaxpayerHistory
{
    Task<TaxpayerHistoryDto> LoadAsync(CancellationToken cancellationToken);
}
