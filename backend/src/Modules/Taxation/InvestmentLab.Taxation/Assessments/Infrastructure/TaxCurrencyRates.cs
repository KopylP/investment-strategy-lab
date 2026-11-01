using InvestmentLab.MarketData.Contracts.FxRates;
using InvestmentLab.Taxation.Assessments.Domain;

namespace InvestmentLab.Taxation.Assessments.Infrastructure;

internal sealed class TaxCurrencyRates(IFxRates rates) : ITaxCurrencyRates
{
    public async Task<ITaxCurrencyRate?> OnAsync(string currency, string taxCurrency, DateOnly lookupDate, CancellationToken cancellationToken)
    {
        var found = await rates.GetFxRate(currency, taxCurrency, lookupDate, cancellationToken);

        return found is null ? null : new CurrencyRate(found);
    }

    private sealed class CurrencyRate(FxRateDto dto) : ITaxCurrencyRate
    {
        private readonly FxRateDto _dto = dto;

        public decimal Rate => _dto.Rate;
        public DateOnly EffectiveDate => _dto.EffectiveDate;
    }
}
