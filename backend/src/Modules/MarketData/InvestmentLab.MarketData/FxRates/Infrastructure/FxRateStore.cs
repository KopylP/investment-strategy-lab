using InvestmentLab.MarketData.Contracts.FxRates;
using InvestmentLab.MarketData.FxRates.Application.Shared;
using InvestmentLab.MarketData.FxRates.Domain;
using InvestmentLab.MarketData.Shared.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace InvestmentLab.MarketData.FxRates.Infrastructure;

internal sealed class FxRateStore(MarketDataDbContext db) : IFxRateStore, IFxRates
{
    public async Task<DateOnly?> LastEffectiveDateAsync(FxRateSource source, Currency @base, Currency quote, CancellationToken cancellationToken) =>
        await db.FxRates
            .Where(rate => rate.Source == source && rate.Base == @base && rate.Quote == quote)
            .MaxAsync(rate => (DateOnly?)rate.EffectiveDate, cancellationToken);

    public async Task<DateOnly?> FirstEffectiveDateAsync(FxRateSource source, Currency @base, Currency quote, CancellationToken cancellationToken) =>
        await db.FxRates
            .Where(rate => rate.Source == source && rate.Base == @base && rate.Quote == quote)
            .MinAsync(rate => (DateOnly?)rate.EffectiveDate, cancellationToken);

    public async Task<IReadOnlyList<FxRate>> RatesBetweenAsync(
        Currency @base, Currency quote, DateOnly from, DateOnly to, CancellationToken cancellationToken) =>
        await db.FxRates.AsNoTracking()
            .Where(rate => rate.Source == PrimaryFxSource.Source && rate.Base == @base && rate.Quote == quote
                && rate.EffectiveDate >= from && rate.EffectiveDate <= to)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<FxRate>> LatestToPivotAsync(CancellationToken cancellationToken)
    {
        var rates = await db.FxRates.AsNoTracking()
            .Where(rate => rate.Source == PrimaryFxSource.Source && rate.Quote == PrimaryFxSource.Pivot)
            .GroupBy(rate => rate.Base)
            .Select(group => group.OrderByDescending(rate => rate.EffectiveDate).First())
            .ToListAsync(cancellationToken);
        return rates;
    }

    public void Add(FxRate rate) => db.FxRates.Add(rate);

    public void Record(IDomainEvent domainEvent) => db.Record(domainEvent);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);

    public async Task<FxRateDto?> GetFxRate(string baseCurrency, string quoteCurrency, DateOnly transactionDate, CancellationToken cancellationToken)
    {
        if (Currency.TryOf(baseCurrency) is not { IsSuccess: true } @base || Currency.TryOf(quoteCurrency) is not { IsSuccess: true } quote)
            return null;

        var window = await RatesBetweenAsync(
            @base.Value, quote.Value, transactionDate.AddDays(-FxRateResolver.MaxAgeDays), transactionDate, cancellationToken);
        var resolved = FxRateResolver.Resolve(window, @base.Value, quote.Value, transactionDate);

        return resolved.IsSuccess
            ? new FxRateDto(resolved.Value.Source.ToString(), resolved.Value.Base.Code, resolved.Value.Quote.Code,
                resolved.Value.EffectiveDate, resolved.Value.Rate, resolved.Value.TableNumber)
            : null;
    }

    public async Task<IReadOnlyList<FxRateDto>> GetFxRateHistory(
        string baseCurrency,
        string quoteCurrency,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken)
    {
        if (Currency.TryOf(baseCurrency) is not { IsSuccess: true } @base || Currency.TryOf(quoteCurrency) is not { IsSuccess: true } quote)
            return [];

        return [.. (await RatesBetweenAsync(@base.Value, quote.Value, from, to, cancellationToken))
            .OrderBy(rate => rate.EffectiveDate)
            .Select(rate => new FxRateDto(rate.Source.ToString(), rate.Base.Code, rate.Quote.Code, rate.EffectiveDate, rate.Rate, rate.TableNumber))];
    }

    public async Task<decimal?> GetCrossRate(string fromCurrency, string toCurrency, DateOnly asOf, CancellationToken cancellationToken)
    {
        if (IsSameCurrency(fromCurrency, toCurrency))
            return 1m;

        var fromInPivot = await InPivotAsync(fromCurrency, asOf, cancellationToken);
        var toInPivot = await InPivotAsync(toCurrency, asOf, cancellationToken);

        return CrossRate.Of(fromInPivot, toInPivot);
    }

    public async Task<IReadOnlyList<CrossRateDto>> GetCrossRateHistory(
        string fromCurrency,
        string toCurrency,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken)
    {
        var fromLeg = await PivotLegAsync(fromCurrency, from, to, cancellationToken);
        var toLeg = await PivotLegAsync(toCurrency, from, to, cancellationToken);

        return [.. CrossRate.SeriesOf(fromLeg, toLeg).Select(point => new CrossRateDto(point.Date, point.Rate))];
    }

    private async Task<decimal?> InPivotAsync(string currency, DateOnly asOf, CancellationToken cancellationToken)
    {
        if (IsSameCurrency(currency, PrimaryFxSource.Pivot.Code))
            return 1m;

        var rate = await GetFxRate(currency, PrimaryFxSource.Pivot.Code, asOf.AddDays(1), cancellationToken);

        return rate?.Rate;
    }

    private async Task<IReadOnlyList<(DateOnly Date, decimal Rate)>?> PivotLegAsync(
        string currency, DateOnly from, DateOnly to, CancellationToken cancellationToken)
    {
        if (IsSameCurrency(currency, PrimaryFxSource.Pivot.Code))
            return null;

        var history = await GetFxRateHistory(currency, PrimaryFxSource.Pivot.Code, from, to, cancellationToken);

        return [.. history.Select(rate => (rate.EffectiveDate, rate.Rate))];
    }

    private static bool IsSameCurrency(string left, string right) => string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
}
