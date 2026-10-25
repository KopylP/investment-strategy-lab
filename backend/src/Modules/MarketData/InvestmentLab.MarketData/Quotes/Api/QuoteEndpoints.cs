using InvestmentLab.BuildingBlocks.Http;
using InvestmentLab.BuildingBlocks.Security;
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
    public static RouteGroupBuilder MapQuotes(this RouteGroupBuilder group)
    {
        var quotes = group.MapGroup("/quotes").WithTags("Quotes");

        quotes.MapPost("/ingest", async (IngestDailyQuotesHandler handler, CancellationToken ct) => TypedResults.Ok(await handler.Handle(ct)))
            .WithName("IngestDailyQuotes");

        quotes.MapGet("/latest", async ([FromQuery] Guid[] instrumentIds, DateOnly? asOf, IQuoteHistory history, TimeProvider clock, CancellationToken ct) =>
                TypedResults.Ok(await history.GetLatestPrices(instrumentIds, asOf ?? DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime), ct)))
            .WithName("GetLatestPrices");

        // Several series at once (sparklines and changes in the instrument list).
        quotes.MapGet("/history", async ([FromQuery] Guid[] instrumentIds, DateOnly from, DateOnly to, IQuoteHistory history, CancellationToken ct) =>
                TypedResults.Ok(await history.GetQuoteHistory(instrumentIds, from, to, ct)))
            .WithName("GetQuoteHistories");

        quotes.MapGet("/{instrumentId:guid}", async (Guid instrumentId, DateOnly from, DateOnly to, IQuoteHistory history, CancellationToken ct) =>
                TypedResults.Ok((await history.GetQuoteHistory([instrumentId], from, to, ct)).Single()))
            .WithName("GetQuoteHistory");

        quotes.MapPost("/{instrumentId:guid}/corrections", async (
                Guid instrumentId, CorrectQuoteRequest body, CorrectQuoteHandler handler, ICurrentUser user, CancellationToken ct) =>
                (await handler.Handle(
                    new CorrectQuoteCommand(instrumentId, body.TradingDate, body.Close, body.AdjustedClose, body.Reason), user.RequiredId.ToString(), ct))
                .ToHttpResult())
            .WithName("CorrectQuote");

        return group;
    }
}
