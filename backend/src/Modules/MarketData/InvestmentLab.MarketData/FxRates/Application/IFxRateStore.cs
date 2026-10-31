using InvestmentLab.MarketData.FxRates.Domain;

namespace InvestmentLab.MarketData.FxRates.Application;

public interface IFxRateStore
{
    Task<DateOnly?> LastEffectiveDateAsync(FxRateSource source, Currency @base, Currency quote, CancellationToken cancellationToken);

    Task<DateOnly?> FirstEffectiveDateAsync(FxRateSource source, Currency @base, Currency quote, CancellationToken cancellationToken);

    Task<IReadOnlyList<FxRate>> RatesBetweenAsync(Currency @base, Currency quote, DateOnly from, DateOnly to, CancellationToken cancellationToken);

    Task<IReadOnlyList<FxRate>> LatestToPlnAsync(CancellationToken cancellationToken);

    void Add(FxRate rate);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
