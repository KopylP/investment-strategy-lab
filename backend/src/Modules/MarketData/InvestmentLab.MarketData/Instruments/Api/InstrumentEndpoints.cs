using InvestmentLab.BuildingBlocks.Http;
using InvestmentLab.MarketData.Contracts.Instruments;
using InvestmentLab.MarketData.Instruments.Application.AssignProviderSymbol;
using InvestmentLab.MarketData.Instruments.Application.GetInstruments;
using InvestmentLab.MarketData.Instruments.Application.RegisterInstrument;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace InvestmentLab.MarketData.Instruments.Api;

public sealed record AssignProviderSymbolRequest(string ProviderSymbol);

internal static class InstrumentEndpoints
{
    public static RouteGroupBuilder MapInstruments(this RouteGroupBuilder group)
    {
        var instruments = group
            .MapGroup("/instruments")
            .WithTags("Instruments");

        instruments
            .MapGet("/", GetInstruments)
            .WithName("GetInstruments");
        instruments
            .MapPost("/", RegisterInstrument)
            .WithName("RegisterInstrument")
            .Produces<Guid>();
        instruments
            .MapPost("/{instrumentId:guid}/provider-symbols", AssignProviderSymbol)
            .WithName("AssignProviderSymbol")
            .Produces(StatusCodes.Status204NoContent);

        return group;
    }

    private static async Task<Ok<IReadOnlyList<InstrumentDto>>> GetInstruments(
        GetInstrumentsHandler handler,
        CancellationToken cancellationToken)
    {
        var instruments = await handler.Handle(cancellationToken);

        return TypedResults.Ok(instruments);
    }

    private static async Task<IResult> RegisterInstrument(
        RegisterInstrumentCommand command,
        RegisterInstrumentHandler handler,
        CancellationToken cancellationToken)
    {
        var registered = await handler.Handle(command, cancellationToken);

        return registered.ToHttpResult();
    }

    private static async Task<IResult> AssignProviderSymbol(
        Guid instrumentId,
        AssignProviderSymbolRequest body,
        AssignProviderSymbolHandler handler,
        CancellationToken cancellationToken)
    {
        var command = new AssignProviderSymbolCommand(instrumentId, body.ProviderSymbol);

        var assigned = await handler.Handle(command, cancellationToken);

        return assigned.ToHttpResult();
    }
}
