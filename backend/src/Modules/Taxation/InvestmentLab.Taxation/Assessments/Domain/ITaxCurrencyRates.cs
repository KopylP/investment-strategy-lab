namespace InvestmentLab.Taxation.Assessments.Domain;

public interface ITaxCurrencyRates
{
    Task<ITaxCurrencyRate?> OnAsync(string currency, string taxCurrency, DateOnly lookupDate, CancellationToken cancellationToken);
}
