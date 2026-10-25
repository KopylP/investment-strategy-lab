using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace InvestmentLab.BuildingBlocks.Persistence;

/// <summary>A module DbContext known to the migrator and the outbox dispatcher.</summary>
public sealed record ModuleDbContextType(Type ContextType);

/// <summary>One Postgres database for the monolith, one schema (and migrations history) per module.</summary>
public static class ModuleDatabase
{
    /// <summary>Connection name shared with the Aspire AppHost.</summary>
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
            // Read the connection string lazily: test hosts override configuration after modules register.
            builder.Services.AddDbContext<TContext>((services, options) =>
                Configure(options, services.GetRequiredService<IConfiguration>().GetConnectionString(ConnectionName), schema));
            builder.EnrichNpgsqlDbContext<TContext>();
            builder.Services.AddSingleton(new ModuleDbContextType(typeof(TContext)));
            return builder;
        }
    }
}

/// <summary>Lets <c>dotnet ef</c> build a module context without starting the host.</summary>
public abstract class ModuleDesignTimeDbContextFactory<TContext> : IDesignTimeDbContextFactory<TContext>
    where TContext : ModuleDbContext
{
    protected abstract string Schema { get; }

    protected abstract TContext Create(DbContextOptions<TContext> options);

    public TContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<TContext>();
        ModuleDatabase.Configure(options, "Host=localhost;Database=investmentlab", Schema);
        return Create(options.Options);
    }
}
