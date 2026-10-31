namespace InvestmentLab.Taxation.Declarations.Domain;

public interface ITaxpayerHistory
{
    Task<TaxpayerHistoryDto> LoadAsync(CancellationToken cancellationToken);
}
