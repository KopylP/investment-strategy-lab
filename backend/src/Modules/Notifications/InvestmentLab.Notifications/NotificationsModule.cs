using System.Net.Http.Headers;
using InvestmentLab.BuildingBlocks.Messaging;
using InvestmentLab.BuildingBlocks.Modules;
using InvestmentLab.BuildingBlocks.Persistence;
using InvestmentLab.Notifications.Delivery.Api;
using InvestmentLab.Notifications.Delivery.Application;
using InvestmentLab.Notifications.Delivery.Application.Integration;
using InvestmentLab.Notifications.Delivery.Domain;
using InvestmentLab.Notifications.Delivery.Infrastructure;
using InvestmentLab.Notifications.Shared.Infrastructure;
using InvestmentLab.Rebalancing.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace InvestmentLab.Notifications;

/// <summary>
/// Tells the user what happened while they were away (a scheduled report is ready) through every configured channel,
/// to the devices they picked or, until they pick, the ones the channel finds for them. A channel is registered only
/// when it is configured: Home Assistant when the Supervisor gave the add-on its token. Without any, the events are
/// handled and nothing is sent.
/// </summary>
public sealed class NotificationsModule : IModule
{
    public const string Schema = NotificationsDbContext.SchemaName;

    public string Name => "Notifications";

    public void AddServices(IHostApplicationBuilder builder)
    {
        builder.AddModuleDbContext<NotificationsDbContext>(Schema);

        var services = builder.Services;
        services.AddScoped<INotificationPreferencesRepository, NotificationPreferencesRepository>();
        services.AddScoped<Notifier>();
        services.AddScoped<NotificationSettingsHandler>();
        services.AddIntegrationEventHandler<ReportGeneratedV1, ReportNotifications>();
        services.AddIntegrationEventHandler<ReportClosedV1, ReportNotifications>();
        services.AddIntegrationEventHandler<ReportSupersededV1, ReportNotifications>();

        AddHomeAssistant(builder);
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) =>
        endpoints.MapGroup("/api/notifications").MapNotificationSettings();

    private static void AddHomeAssistant(IHostApplicationBuilder builder)
    {
        if (builder.Configuration["SUPERVISOR_TOKEN"] is not { Length: > 0 } token)
            return;

        var section = builder.Configuration.GetSection(HomeAssistantChannelOptions.Section);
        builder.Services.AddSingleton(new HomeAssistantChannelOptions(section["PanelPath"]));
        builder.Services.AddHttpClient<INotificationChannel, HomeAssistantChannel>(http =>
        {
            http.BaseAddress = new Uri(section["SupervisorAddress"] ?? HomeAssistantChannel.SupervisorAddress);
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        });
    }
}
