using InvestmentLab.MarketData.Contracts.FxRates;
using InvestmentLab.MarketData.FxRates.Application.IngestFxRates;
using InvestmentLab.MarketData.FxRates.Application.GetLatestFxRates;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace InvestmentLab.MarketData.FxRates.Api;

internal static class FxRateEndpoints
{
    public static RouteGroupBuilder MapFxRates(this RouteGroupBuilder group)
    {
        var fxRates = group
            .MapGroup("/fx-rates")
            .WithTags("FX rates");

        fxRates
            .MapPost("/ingest", IngestFxRates)
            .WithName("IngestFxRates");
        fxRates
            .MapGet("/latest", GetLatestFxRates)
            .WithName("GetLatestFxRates");
        fxRates
            .MapGet("/{baseCurrency}/{quoteCurrency}", GetFxRate)
            .WithName("GetFxRate");

        return group;
    }

    private static async Task<Ok<FxIngestReport>> IngestFxRates(IngestFxRatesHandler handler, CancellationToken cancellationToken)
    {
        var report = await handler.Handle(cancellationToken);

        return TypedResults.Ok(report);
    }

    private static async Task<Ok<LatestFxRatesDto>> GetLatestFxRates(GetLatestFxRatesHandler handler, CancellationToken cancellationToken)
    {
        var rates = await handler.Handle(cancellationToken);

        return TypedResults.Ok(rates);
    }

    private static async Task<Results<Ok<FxRateDto>, NotFound>> GetFxRate(
        string baseCurrency, string quoteCurrency, DateOnly transactionDate, IFxRates rates, CancellationToken cancellationToken)
    {
        var rate = await rates.GetFxRate(baseCurrency, quoteCurrency, transactionDate, cancellationToken);

        return rate is null ? TypedResults.NotFound() : TypedResults.Ok(rate);
    }
}
