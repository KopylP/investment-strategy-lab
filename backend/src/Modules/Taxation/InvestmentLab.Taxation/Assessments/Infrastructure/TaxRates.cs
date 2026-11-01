using InvestmentLab.MarketData.Contracts.FxRates;
using InvestmentLab.Taxation.Assessments.Domain;

namespace InvestmentLab.Taxation.Assessments.Infrastructure;

internal sealed class TaxRates(IFxRates rates) : ITaxRates
{
    public async Task<ITaxRate?> OnAsync(string currency, string taxCurrency, DateOnly lookupDate, CancellationToken cancellationToken)
    {
        var found = await rates.GetFxRate(currency, taxCurrency, lookupDate, cancellationToken);

        return found is null ? null : new TaxRate(found);
    }

    private sealed class TaxRate(FxRateDto dto) : ITaxRate
    {
        private readonly FxRateDto _dto = dto;

        public decimal Rate => _dto.Rate;
        public DateOnly EffectiveDate => _dto.EffectiveDate;
    }
}
