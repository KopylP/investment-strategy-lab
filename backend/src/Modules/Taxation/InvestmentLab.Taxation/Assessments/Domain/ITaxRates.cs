namespace InvestmentLab.Taxation.Assessments.Domain;

public interface ITaxRates
{
    Task<ITaxRate?> OnAsync(string currency, string taxCurrency, DateOnly lookupDate, CancellationToken cancellationToken);
}
