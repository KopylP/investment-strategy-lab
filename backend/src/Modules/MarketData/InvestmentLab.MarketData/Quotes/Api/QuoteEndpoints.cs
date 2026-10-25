using InvestmentLab.BuildingBlocks.Http;
using InvestmentLab.MarketData.Contracts.Quotes;
using InvestmentLab.MarketData.Quotes.Application.CorrectQuote;
using InvestmentLab.MarketData.Quotes.Application.IngestDailyQuotes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace InvestmentLab.MarketData.Quotes.Api;

public sealed record CorrectQuoteRequest(DateOnly TradingDate, decimal Close, decimal? AdjustedClose, string Reason);

internal static class QuoteEndpoints
{
    // Until Identity (stage 3) there is no signed-in user to blame.
    private const string UnknownAuthor = "unknown";

    public static RouteGroupBuilder MapQuotes(this RouteGroupBuilder group)
    {
        var quotes = group.MapGroup("/quotes").WithTags("Quotes");

        quotes.MapPost("/ingest", async (IngestDailyQuotesHandler handler, CancellationToken ct) => TypedResults.Ok(await handler.Handle(ct)))
            .WithName("IngestDailyQuotes");

        quotes.MapGet("/latest", async ([FromQuery] Guid[] instrumentIds, DateOnly? asOf, IQuoteHistory history, TimeProvider clock, CancellationToken ct) =>
                TypedResults.Ok(await history.GetLatestPrices(instrumentIds, asOf ?? DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime), ct)))
            .WithName("GetLatestPrices");

        quotes.MapGet("/{instrumentId:guid}", async (Guid instrumentId, DateOnly from, DateOnly to, IQuoteHistory history, CancellationToken ct) =>
                TypedResults.Ok((await history.GetQuoteHistory([instrumentId], from, to, ct)).Single()))
            .WithName("GetQuoteHistory");

        quotes.MapPost("/{instrumentId:guid}/corrections", async (
                Guid instrumentId, CorrectQuoteRequest body, CorrectQuoteHandler handler, CancellationToken ct) =>
                (await handler.Handle(
                    new CorrectQuoteCommand(instrumentId, body.TradingDate, body.Close, body.AdjustedClose, body.Reason), UnknownAuthor, ct))
                .ToHttpResult())
            .WithName("CorrectQuote");

        return group;
    }
}
