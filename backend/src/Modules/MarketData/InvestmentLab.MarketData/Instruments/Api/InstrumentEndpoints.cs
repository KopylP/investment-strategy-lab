using InvestmentLab.BuildingBlocks.Http;
using InvestmentLab.MarketData.Instruments.Application.AssignProviderSymbol;
using InvestmentLab.MarketData.Instruments.Application.GetInstruments;
using InvestmentLab.MarketData.Instruments.Application.RegisterInstrument;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace InvestmentLab.MarketData.Instruments.Api;

public sealed record AssignProviderSymbolRequest(string ProviderSymbol);

internal static class InstrumentEndpoints
{
    public static RouteGroupBuilder MapInstruments(this RouteGroupBuilder group)
    {
        var instruments = group.MapGroup("/instruments").WithTags("Instruments");

        instruments.MapGet("/", async (GetInstrumentsHandler handler, CancellationToken ct) => TypedResults.Ok(await handler.Handle(ct)))
            .WithName("GetInstruments");

        instruments.MapPost("/", async (RegisterInstrumentCommand command, RegisterInstrumentHandler handler, CancellationToken ct) =>
                (await handler.Handle(command, ct)).ToHttpResult())
            .WithName("RegisterInstrument");

        instruments.MapPost("/{instrumentId:guid}/provider-symbols", async (
                Guid instrumentId, AssignProviderSymbolRequest body, AssignProviderSymbolHandler handler, CancellationToken ct) =>
                (await handler.Handle(new AssignProviderSymbolCommand(instrumentId, body.ProviderSymbol), ct)).ToHttpResult())
            .WithName("AssignProviderSymbol");

        return group;
    }
}
