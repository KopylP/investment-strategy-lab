using InvestmentLab.BuildingBlocks.Http;
using InvestmentLab.BuildingBlocks.Security;
using InvestmentLab.Notifications.Delivery.Application;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace InvestmentLab.Notifications.Delivery.Api;

internal static class NotificationEndpoints
{
    public static RouteGroupBuilder MapNotificationSettings(this RouteGroupBuilder group)
    {
        group.WithTags("Notifications");

        group.MapGet("/settings", async (NotificationSettingsHandler handler, ICurrentUser user, CancellationToken ct) =>
                (await handler.Get(user.RequiredId, ct)).ToHttpResult())
            .WithName("GetNotificationSettings");

        group.MapPut("/settings", async (ChangeNotificationSettingsCommand body, NotificationSettingsHandler handler, ICurrentUser user, CancellationToken ct) =>
                (await handler.Change(user.RequiredId, body, ct)).ToHttpResult())
            .WithName("ChangeNotificationSettings");

        group.MapPost("/test", async (NotificationSettingsHandler handler, ICurrentUser user, CancellationToken ct) =>
                (await handler.SendTest(user.RequiredId, ct)).ToHttpResult())
            .WithName("SendTestNotification");

        return group;
    }
}
