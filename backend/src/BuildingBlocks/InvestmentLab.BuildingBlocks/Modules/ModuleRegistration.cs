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
    // Classic extension method: `params` inside an extension block trips CS8620 on the 10.0.100 compiler.
    public static IHostApplicationBuilder AddModules(this IHostApplicationBuilder builder, params IModule[] modules)
    {
        foreach (var module in modules)
            module.AddServices(builder);

        builder.Services.AddSingleton<IReadOnlyList<IModule>>(modules);
        builder.Services.TryAddSingleton(TimeProvider.System);
        builder.Services.AddHttpContextAccessor();
        builder.Services.TryAddScoped<ICurrentUser, HttpCurrentUser>();

        // Order matters: schemas exist before the outbox is polled or a job runs.
        builder.Services.AddHostedService<DatabaseMigrator>();
        builder.Services.AddHostedService<OutboxDispatcher>();
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
