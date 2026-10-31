using InvestmentLab.BuildingBlocks.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InvestmentLab.MarketData.Shared.Infrastructure;

internal sealed class MarketDataDesignTimeDbContextFactory : ModuleDesignTimeDbContextFactory<MarketDataDbContext>
{
    protected override string Schema => MarketDataDbContext.SchemaName;

    protected override MarketDataDbContext Create(DbContextOptions<MarketDataDbContext> options) => new(options);
}
