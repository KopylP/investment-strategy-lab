using InvestmentLab.BuildingBlocks.Http;
using InvestmentLab.BuildingBlocks.Security;
using InvestmentLab.Notifications.Delivery.Application.ChangeNotificationSettings;
using InvestmentLab.Notifications.Delivery.Application.GetMutedSignals;
using InvestmentLab.Notifications.Delivery.Application.GetNotificationSettings;
using InvestmentLab.Notifications.Delivery.Application.HearSignal;
using InvestmentLab.Notifications.Delivery.Application.SendTestNotification;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace InvestmentLab.Notifications.Delivery.Api;

internal static class NotificationEndpoints
{
    public static RouteGroupBuilder MapNotificationSettings(this RouteGroupBuilder group)
    {
        group.WithTags("Notifications");

        group
            .MapGet("/settings", GetNotificationSettings)
            .WithName("GetNotificationSettings")
            .Produces<NotificationSettingsDto>();
        group
            .MapPut("/settings", ChangeNotificationSettings)
            .WithName("ChangeNotificationSettings")
            .Produces<NotificationSettingsDto>();
        group
            .MapGet("/muted-signals", GetMutedSignals)
            .WithName("GetMutedSignals")
            .Produces<IReadOnlyList<Guid>>();
        group
            .MapPut("/signals/{signalId:guid}", HearSignal)
            .WithName("HearSignal");
        group
            .MapPost("/test", SendTestNotification)
            .WithName("SendTestNotification");

        return group;
    }

    private static async Task<IResult> GetNotificationSettings(
        GetNotificationSettingsHandler handler, ICurrentUser user, CancellationToken cancellationToken)
    {
        var settings = await handler.Handle(user.RequiredId, cancellationToken);

        return settings.ToHttpResult();
    }

    private static async Task<IResult> ChangeNotificationSettings(
        ChangeNotificationSettingsCommand body, ChangeNotificationSettingsHandler handler, ICurrentUser user, CancellationToken cancellationToken)
    {
        var changed = await handler.Handle(user.RequiredId, body, cancellationToken);

        return changed.ToHttpResult();
    }

    private static async Task<IResult> GetMutedSignals(
        GetMutedSignalsHandler handler, ICurrentUser user, CancellationToken cancellationToken)
    {
        var muted = await handler.Handle(user.RequiredId, cancellationToken);

        return muted.ToHttpResult();
    }

    private static async Task<IResult> HearSignal(
        Guid signalId, HearSignalCommand body, HearSignalHandler handler, ICurrentUser user, CancellationToken cancellationToken)
    {
        var heard = await handler.Handle(user.RequiredId, signalId, body, cancellationToken);

        return heard.ToHttpResult();
    }

    private static async Task<IResult> SendTestNotification(
        SendTestNotificationHandler handler, ICurrentUser user, CancellationToken cancellationToken)
    {
        var sent = await handler.Handle(user.RequiredId, cancellationToken);

        return sent.ToHttpResult();
    }
}
