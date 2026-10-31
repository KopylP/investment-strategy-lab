using System.Text.Json.Serialization;
using InvestmentLab.BuildingBlocks.Http;
using InvestmentLab.Portfolio.Transactions.Application.ImportPositions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace InvestmentLab.Portfolio.Transactions.Api;

internal static class ImportEndpoints
{
    public static RouteGroupBuilder MapImport(this RouteGroupBuilder group)
    {
        group.MapPost("/accounts/{accountId:guid}/sub-portfolios/{subPortfolioId:guid}/import-positions", ImportPositions)
            .WithTags("Import")
            .WithName("ImportPositions");

        return group;
    }

    private static async Task<IResult> ImportPositions(
        Guid accountId, Guid subPortfolioId, DateOnly? asOf, PositionsFile body, ImportPositionsHandler handler, CancellationToken cancellationToken)
    {
        var positions = (body.Positions ?? []).Select(ToLegacyPosition);
        var command = new ImportPositionsCommand(subPortfolioId, body.Cash, [.. positions], asOf);

        var imported = await handler.Handle(accountId, command, cancellationToken);

        return imported.ToHttpResult();
    }

    private static LegacyPosition ToLegacyPosition(PositionsFileEntry entry) =>
        new(entry.Ticker, entry.EntryPrice, entry.Shares, entry.EntryDate, entry.Currency);
}
