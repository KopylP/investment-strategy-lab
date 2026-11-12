using System.Reflection;
using System.Net.Http.Headers;
using InvestmentLab.BuildingBlocks.Messaging;
using InvestmentLab.BuildingBlocks.Modules;
using InvestmentLab.BuildingBlocks.Persistence;
using InvestmentLab.Notifications.Delivery.Api;
using InvestmentLab.Notifications.Delivery.Application.ChangeNotificationSettings;
using InvestmentLab.Notifications.Delivery.Application.GetMutedSignals;
using InvestmentLab.Notifications.Delivery.Application.GetNotificationSettings;
using InvestmentLab.Notifications.Delivery.Application.HearSignal;
using InvestmentLab.Notifications.Delivery.Application.Integration;
using InvestmentLab.Notifications.Delivery.Application.SendTestNotification;
using InvestmentLab.Notifications.Delivery.Application.Shared;
using InvestmentLab.Notifications.Delivery.Domain;
using InvestmentLab.Notifications.Delivery.Infrastructure;
using InvestmentLab.Notifications.Shared.Infrastructure;
using InvestmentLab.Rebalancing.Contracts;
using InvestmentLab.Signals.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace InvestmentLab.Notifications;

public sealed class NotificationsModule : IModule
{
    public const string Schema = NotificationsDbContext.SchemaName;

    public string Name => "Notifications";

    public Assembly Assembly => typeof(NotificationsAssemblyMarker).Assembly;

    public void AddServices(IHostApplicationBuilder builder)
    {
        builder.AddModuleDbContext<NotificationsDbContext>(Schema);

        var services = builder.Services;
        services.AddScoped<INotificationPreferencesRepository, NotificationPreferencesRepository>();
        services.AddScoped<Notifier>();
        services.AddScoped<NextReportLookup>();

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
