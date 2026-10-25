using InvestmentLab.MarketData.Contracts.FxRates;
using InvestmentLab.MarketData.FxRates.Application.IngestFxRates;
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

        fxRates.MapGet("/{baseCurrency}/{quoteCurrency}", async Task<Results<Ok<FxRateDto>, NotFound>> (
                string baseCurrency, string quoteCurrency, DateOnly transactionDate, IFxRates rates, CancellationToken ct) =>
                await rates.GetFxRate(baseCurrency, quoteCurrency, transactionDate, ct) is { } rate ? TypedResults.Ok(rate) : TypedResults.NotFound())
            .WithName("GetFxRate");

        return group;
    }
}
