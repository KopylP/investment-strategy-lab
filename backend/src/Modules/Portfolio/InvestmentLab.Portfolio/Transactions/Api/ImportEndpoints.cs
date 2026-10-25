using System.Text.Json.Serialization;
using InvestmentLab.BuildingBlocks.Http;
using InvestmentLab.Portfolio.Transactions.Application.ImportPositions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace InvestmentLab.Portfolio.Transactions.Api;

/// <summary>The legacy <c>positions.json</c> exactly as the Python flow writes it.</summary>
public sealed record PositionsFile(
    [property: JsonPropertyName("cash")] decimal Cash,
    [property: JsonPropertyName("positions")] IReadOnlyList<PositionsFileEntry> Positions);

public sealed record PositionsFileEntry(
    [property: JsonPropertyName("ticker")] string Ticker,
    [property: JsonPropertyName("entry_price")] decimal EntryPrice,
    [property: JsonPropertyName("shares")] decimal Shares,
    [property: JsonPropertyName("entry_date")] DateOnly? EntryDate,
    [property: JsonPropertyName("currency")] string Currency);

internal static class ImportEndpoints
{
    public static RouteGroupBuilder MapImport(this RouteGroupBuilder group)
    {
        group.MapPost("/accounts/{accountId:guid}/sub-portfolios/{subPortfolioId:guid}/import-positions", async (
                Guid accountId, Guid subPortfolioId, DateOnly? asOf, PositionsFile body, ImportPositionsHandler handler, CancellationToken ct) =>
                (await handler.Handle(accountId, new ImportPositionsCommand(
                    subPortfolioId,
                    body.Cash,
                    [.. (body.Positions ?? []).Select(p => new LegacyPosition(p.Ticker, p.EntryPrice, p.Shares, p.EntryDate, p.Currency))],
                    asOf), ct)).ToHttpResult())
            .WithTags("Import")
            .WithName("ImportPositions");

        return group;
    }
}
