using InvestmentLab.MarketData.Contracts.FxRates;
using InvestmentLab.MarketData.FxRates.Application.IngestFxRates;
using InvestmentLab.MarketData.FxRates.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace InvestmentLab.MarketData.FxRates.Api;

internal static class FxRateEndpoints
{
    public static RouteGroupBuilder MapFxRates(this RouteGroupBuilder group)
    {
        var fxRates = group.MapGroup("/fx-rates").WithTags("FX rates");

        fxRates.MapPost("/ingest", async (IngestFxRatesHandler handler, CancellationToken ct) => TypedResults.Ok(await handler.Handle(ct)))
            .WithName("IngestFxRates");

        // Latest NBP mid rates to PLN for every currency: the UI converts any amount to the display currency through PLN.
        fxRates.MapGet("/latest", async (FxRateStore store, CancellationToken ct) =>
            {
                var rates = await store.LatestToPlnAsync(ct);
                return TypedResults.Ok(new LatestFxRatesDto(
                    rates.Count > 0 ? rates.Min(rate => rate.EffectiveDate) : null,
                    rates.ToDictionary(rate => rate.Base.Code, rate => rate.Rate).Append(KeyValuePair.Create("PLN", 1m)).ToDictionary()));
            })
            .WithName("GetLatestFxRates");

        fxRates.MapGet("/{baseCurrency}/{quoteCurrency}", async Task<Results<Ok<FxRateDto>, NotFound>> (
                string baseCurrency, string quoteCurrency, DateOnly transactionDate, IFxRates rates, CancellationToken ct) =>
                await rates.GetFxRate(baseCurrency, quoteCurrency, transactionDate, ct) is { } rate ? TypedResults.Ok(rate) : TypedResults.NotFound())
            .WithName("GetFxRate");

        return group;
    }
}

public sealed record LatestFxRatesDto(DateOnly? EffectiveDate, IReadOnlyDictionary<string, decimal> ToPln);
