using InvestmentLab.MarketData.FxRates.Domain;

namespace InvestmentLab.MarketData.FxRates.Application;

public interface IFxRateProvider
{
    FxRateSource Source { get; }

    Task<IReadOnlyList<FxRate>> FetchAsync(Currency @base, DateOnly from, DateOnly to, CancellationToken cancellationToken);
}
