using InvestmentLab.BuildingBlocks.Http;
using InvestmentLab.BuildingBlocks.Security;
using InvestmentLab.Signals.Monitoring.Application.ChangeSignal;
using InvestmentLab.Signals.Monitoring.Application.CheckSignal;
using InvestmentLab.Signals.Monitoring.Application.CreateSignal;
using InvestmentLab.Signals.Monitoring.Application.DeleteSignal;
using InvestmentLab.Signals.Monitoring.Application.GetSignals;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace InvestmentLab.Signals.Monitoring.Api;

internal static class SignalEndpoints
{
    public static RouteGroupBuilder MapSignals(this RouteGroupBuilder group)
    {
        group.WithTags("Signals");

        group
            .MapGet(string.Empty, GetSignals)
            .WithName("GetSignals")
            .Produces<SignalsOverviewDto>();
        group
            .MapPost(string.Empty, CreateSignal)
            .WithName("CreateSignal")
            .Produces<SignalCreatedDto>();
        group
            .MapPut("/{signalId:guid}", ChangeSignal)
            .WithName("ChangeSignal")
            .Produces(StatusCodes.Status204NoContent);
        group
            .MapDelete("/{signalId:guid}", DeleteSignal)
            .WithName("DeleteSignal")
            .Produces(StatusCodes.Status204NoContent);
        group
            .MapPost("/{signalId:guid}/check", CheckSignal)
            .WithName("CheckSignal")
            .Produces(StatusCodes.Status204NoContent);

        return group;
    }

    private static async Task<IResult> GetSignals(GetSignalsHandler handler, CancellationToken cancellationToken)
    {
        var overview = await handler.Handle(cancellationToken);

        return overview.ToHttpResult();
    }

    private static async Task<IResult> CreateSignal(
        CreateSignalCommand body, CreateSignalHandler handler, ICurrentUser user, CancellationToken cancellationToken)
    {
        var created = await handler.Handle(user.RequiredId, body, cancellationToken);

        return created.ToHttpResult();
    }

    private static async Task<IResult> ChangeSignal(
        Guid signalId, ChangeSignalCommand body, ChangeSignalHandler handler, CancellationToken cancellationToken)
    {
        var changed = await handler.Handle(signalId, body, cancellationToken);

        return changed.ToHttpResult();
    }

    private static async Task<IResult> DeleteSignal(Guid signalId, DeleteSignalHandler handler, CancellationToken cancellationToken)
    {
        var deleted = await handler.Handle(signalId, cancellationToken);

        return deleted.ToHttpResult();
    }

    private static async Task<IResult> CheckSignal(Guid signalId, CheckSignalHandler handler, CancellationToken cancellationToken)
    {
        var checkedSignal = await handler.Handle(signalId, cancellationToken);

        return checkedSignal.ToHttpResult();
    }
}
