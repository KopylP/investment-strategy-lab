using InvestmentLab.SharedKernel;
using InvestmentLab.MarketData.FxRates.Application.Shared;
using InvestmentLab.MarketData.FxRates.Domain;

namespace InvestmentLab.MarketData.FxRates.Application.GetLatestFxRates;

public sealed record LatestFxRatesDto(DateOnly? EffectiveDate, IReadOnlyDictionary<string, decimal> ToPivot);

public sealed class GetLatestFxRatesHandler(IFxRateStore store) : IHandler
{
    public async Task<LatestFxRatesDto> Handle(CancellationToken cancellationToken)
    {
        var rates = await store.LatestToPivotAsync(cancellationToken);

        var effectiveDate = rates.Count > 0 ? rates.Min(rate => rate.EffectiveDate) : (DateOnly?)null;
        var toPivot = rates.ToDictionary(rate => rate.Base.Code, rate => rate.Rate);
        toPivot.Add(PrimaryFxSource.Pivot.Code, 1m);

        return new LatestFxRatesDto(effectiveDate, toPivot);
    }
}
