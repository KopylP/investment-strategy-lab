using System.Net;
using System.Net.Http.Json;
using InvestmentLab.MarketData.FxRates.Application;
using InvestmentLab.MarketData.FxRates.Domain;

namespace InvestmentLab.MarketData.FxRates.Infrastructure.Nbp;

internal sealed class NbpFxRateProvider(HttpClient http) : IFxRateProvider
{
    public const string BaseAddress = "https://api.nbp.pl";

    private const int MaxDaysPerRequest = 93;

    private sealed record NbpRate(string No, DateOnly EffectiveDate, decimal Mid);

    private sealed record NbpSeries(string Code, List<NbpRate> Rates);

    public FxRateSource Source => FxRateSource.Nbp;

    public async Task<IReadOnlyList<FxRate>> FetchAsync(Currency @base, DateOnly from, DateOnly to, CancellationToken cancellationToken)
    {
        var rates = new List<FxRate>();

        for (var start = from; start <= to; start = start.AddDays(MaxDaysPerRequest))
        {
            var end = start.AddDays(MaxDaysPerRequest - 1) < to ? start.AddDays(MaxDaysPerRequest - 1) : to;
            using var response = await http.GetAsync(
                $"/api/exchangerates/rates/a/{@base.Code.ToLowerInvariant()}/{start:yyyy-MM-dd}/{end:yyyy-MM-dd}/?format=json", cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
                continue;

            response.EnsureSuccessStatusCode();

            var series = await response.Content.ReadFromJsonAsync<NbpSeries>(cancellationToken)
                ?? throw new InvalidOperationException("Empty NBP response.");
            rates.AddRange(series.Rates.Select(rate => FxRate.Of(FxRateSource.Nbp, @base, Currency.PLN, rate.EffectiveDate, rate.Mid, rate.No)));
        }

        return rates;
    }
}
