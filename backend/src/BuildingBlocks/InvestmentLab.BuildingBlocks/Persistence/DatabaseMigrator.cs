using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace InvestmentLab.BuildingBlocks.Persistence;

/// <summary>Applies every module's migrations before the host starts serving.</summary>
internal sealed class DatabaseMigrator(
    IEnumerable<ModuleDbContextType> contexts,
    IServiceScopeFactory scopes,
    ILogger<DatabaseMigrator> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var context in contexts)
        {
            await using var scope = scopes.CreateAsyncScope();
            var database = ((DbContext)scope.ServiceProvider.GetRequiredService(context.ContextType)).Database;

            logger.LogInformation("Migrating {Context}", context.ContextType.Name);
            await database.MigrateAsync(cancellationToken);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
