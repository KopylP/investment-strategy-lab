using InvestmentLab.BuildingBlocks.Persistence;
using InvestmentLab.MarketData.Instruments.Domain;
using InvestmentLab.MarketData.Quotes.Domain;
using Microsoft.EntityFrameworkCore;

namespace InvestmentLab.MarketData.Shared.Infrastructure;

public sealed class MarketDataDbContext(DbContextOptions<MarketDataDbContext> options) : ModuleDbContext(options)
{
    public const string SchemaName = "marketdata";

    public DbSet<Instrument> Instruments => Set<Instrument>();
    public DbSet<DailyQuote> DailyQuotes => Set<DailyQuote>();
    public DbSet<QuoteCorrection> QuoteCorrections => Set<QuoteCorrection>();

    protected override string Schema => SchemaName;

    protected override void OnModuleModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MarketDataDbContext).Assembly);
}

internal sealed class MarketDataDesignTimeDbContextFactory : ModuleDesignTimeDbContextFactory<MarketDataDbContext>
{
    protected override string Schema => MarketDataDbContext.SchemaName;

    protected override MarketDataDbContext Create(DbContextOptions<MarketDataDbContext> options) => new(options);
}
