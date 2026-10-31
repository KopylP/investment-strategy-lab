using InvestmentLab.MarketData.FxRates.Application.Shared;

namespace InvestmentLab.MarketData.FxRates.Application.GetLatestFxRates;

public sealed record LatestFxRatesDto(DateOnly? EffectiveDate, IReadOnlyDictionary<string, decimal> ToPln);

public sealed class GetLatestFxRatesHandler(IFxRateStore store)
{
    public async Task<LatestFxRatesDto> Handle(CancellationToken cancellationToken)
    {
        var rates = await store.LatestToPlnAsync(cancellationToken);

        var effectiveDate = rates.Count > 0 ? rates.Min(rate => rate.EffectiveDate) : (DateOnly?)null;
        var toPln = rates.ToDictionary(rate => rate.Base.Code, rate => rate.Rate);
        toPln.Add("PLN", 1m);

        return new LatestFxRatesDto(effectiveDate, toPln);
    }
}
