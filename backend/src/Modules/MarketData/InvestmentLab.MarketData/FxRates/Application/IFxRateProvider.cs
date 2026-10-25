using InvestmentLab.MarketData.FxRates.Domain;

namespace InvestmentLab.MarketData.FxRates.Application;

public interface IFxRateProvider
{
    FxRateSource Source { get; }

    /// <summary>Published rates of <paramref name="base"/> in PLN from <paramref name="from"/> to <paramref name="to"/> inclusive.</summary>
    Task<IReadOnlyList<FxRate>> FetchAsync(Currency @base, DateOnly from, DateOnly to, CancellationToken cancellationToken);
}

public interface IFxRateStore
{
    Task<DateOnly?> LastEffectiveDateAsync(FxRateSource source, Currency @base, Currency quote, CancellationToken cancellationToken);

    Task<IReadOnlyList<FxRate>> RatesBetweenAsync(Currency @base, Currency quote, DateOnly from, DateOnly to, CancellationToken cancellationToken);

    /// <summary>The newest NBP rate of every currency against PLN (display conversions, not taxes).</summary>
    Task<IReadOnlyList<FxRate>> LatestToPlnAsync(CancellationToken cancellationToken);

    void Add(FxRate rate);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
