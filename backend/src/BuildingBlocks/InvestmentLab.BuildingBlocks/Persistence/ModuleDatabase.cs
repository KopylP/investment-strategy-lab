using InvestmentLab.BuildingBlocks.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace InvestmentLab.BuildingBlocks.Persistence;

public static class ModuleDatabase
{
    public const string ConnectionName = "investmentlab";

    public const string MigrationsHistoryTable = "__ef_migrations_history";

    public static DbContextOptionsBuilder Configure(DbContextOptionsBuilder options, string? connectionString, string schema) =>
        options
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable(MigrationsHistoryTable, schema))
            .UseSnakeCaseNamingConvention();

    extension(IHostApplicationBuilder builder)
    {
        public IHostApplicationBuilder AddModuleDbContext<TContext>(string schema)
            where TContext : ModuleDbContext
        {
            builder.Services.AddDomainEventsOutbox();
            builder.Services.AddDbContext<TContext>((services, options) =>
                Configure(options, services.GetRequiredService<IConfiguration>().GetConnectionString(ConnectionName), schema)
                    .AddInterceptors(services.GetRequiredService<DomainEventsInterceptor>()));
            builder.EnrichNpgsqlDbContext<TContext>();
            builder.Services.AddSingleton(new ModuleDbContextType(typeof(TContext)));

            return builder;
        }
    }
}
