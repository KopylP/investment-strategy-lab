using System.Reflection;
using InvestmentLab.BuildingBlocks.Persistence;
using InvestmentLab.MarketData.FxRates.Domain;
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
    public DbSet<FxRate> FxRates => Set<FxRate>();

    protected override string Schema => SchemaName;

    protected override Assembly MappingsAssembly => typeof(MarketDataAssemblyMarker).Assembly;
}
