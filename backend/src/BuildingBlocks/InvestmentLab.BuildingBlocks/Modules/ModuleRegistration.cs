using InvestmentLab.BuildingBlocks.Messaging;
using InvestmentLab.BuildingBlocks.Persistence;
using InvestmentLab.BuildingBlocks.Scheduling;
using InvestmentLab.BuildingBlocks.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace InvestmentLab.BuildingBlocks.Modules;

public static class ModuleRegistration
{
    public static IHostApplicationBuilder AddModules(this IHostApplicationBuilder builder, params IModule[] modules)
    {
        foreach (var module in modules)
        {
            builder.Services.AddServicesOf(module.Assembly);
            module.AddServices(builder);
        }

        builder.Services.AddSingleton<IReadOnlyList<IModule>>(modules);
        builder.Services.TryAddSingleton(TimeProvider.System);
        builder.Services.AddHttpContextAccessor();
        builder.Services.TryAddScoped<ICurrentUser, HttpCurrentUser>();

        builder.Services.AddHostedService<DatabaseMigrator>();
        builder.Services.AddHostedService<OutboxDispatcher>();
        builder.Services.TryAddSingleton<JobWakeUps>();
        builder.Services.AddHostedService<JobScheduler>();

        return builder;
    }

    extension(WebApplication app)
    {
        public WebApplication MapModules()
        {
            foreach (var module in app.Services.GetRequiredService<IReadOnlyList<IModule>>())
                module.MapEndpoints(app);

            return app;
        }
    }
}
