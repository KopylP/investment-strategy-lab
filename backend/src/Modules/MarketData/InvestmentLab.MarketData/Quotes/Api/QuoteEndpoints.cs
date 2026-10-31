using InvestmentLab.BuildingBlocks.Http;
using InvestmentLab.BuildingBlocks.Security;
using InvestmentLab.MarketData.Contracts.Quotes;
using InvestmentLab.MarketData.Quotes.Application.CorrectQuote;
using InvestmentLab.MarketData.Quotes.Application.IngestDailyQuotes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace InvestmentLab.MarketData.Quotes.Api;

public sealed record CorrectQuoteRequest(DateOnly TradingDate, decimal Close, decimal? AdjustedClose, string Reason);

internal static class QuoteEndpoints
{
    public static RouteGroupBuilder MapQuotes(this RouteGroupBuilder group)
    {
        var quotes = group.MapGroup("/quotes").WithTags("Quotes");

        quotes.MapPost("/ingest", IngestDailyQuotes).WithName("IngestDailyQuotes");
        quotes.MapGet("/latest", GetLatestPrices).WithName("GetLatestPrices");
        quotes.MapGet("/history", GetQuoteHistories).WithName("GetQuoteHistories");
        quotes.MapGet("/{instrumentId:guid}", GetQuoteHistory).WithName("GetQuoteHistory");
        quotes.MapPost("/{instrumentId:guid}/corrections", CorrectQuote).WithName("CorrectQuote");

        return group;
    }

    private static async Task<Ok<IngestReport>> IngestDailyQuotes(IngestDailyQuotesHandler handler, CancellationToken cancellationToken)
    {
        var report = await handler.Handle(cancellationToken);

        return TypedResults.Ok(report);
    }

    private static async Task<Ok<IReadOnlyList<LatestPriceDto>>> GetLatestPrices(
        [FromQuery] Guid[] instrumentIds, DateOnly? asOf, IQuoteHistory history, TimeProvider clock, CancellationToken cancellationToken)
    {
        var date = asOf ?? DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        var prices = await history.GetLatestPrices(instrumentIds, date, cancellationToken);

        return TypedResults.Ok(prices);
    }

    private static async Task<Ok<IReadOnlyList<QuoteSeriesDto>>> GetQuoteHistories(
        [FromQuery] Guid[] instrumentIds, DateOnly from, DateOnly to, IQuoteHistory history, CancellationToken cancellationToken)
    {
        var series = await history.GetQuoteHistory(instrumentIds, from, to, cancellationToken);

        return TypedResults.Ok(series);
    }

    private static async Task<Ok<QuoteSeriesDto>> GetQuoteHistory(
        Guid instrumentId, DateOnly from, DateOnly to, IQuoteHistory history, CancellationToken cancellationToken)
    {
        var series = await history.GetQuoteHistory([instrumentId], from, to, cancellationToken);

        return TypedResults.Ok(series.Single());
    }

    private static async Task<IResult> CorrectQuote(
        Guid instrumentId, CorrectQuoteRequest body, CorrectQuoteHandler handler, ICurrentUser user, CancellationToken cancellationToken)
    {
        var command = new CorrectQuoteCommand(instrumentId, body.TradingDate, body.Close, body.AdjustedClose, body.Reason);

        var corrected = await handler.Handle(command, user.RequiredId.ToString(), cancellationToken);

        return corrected.ToHttpResult();
    }
}
