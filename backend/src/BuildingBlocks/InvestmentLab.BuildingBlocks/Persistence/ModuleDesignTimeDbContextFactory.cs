using InvestmentLab.BuildingBlocks.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace InvestmentLab.BuildingBlocks.Persistence;

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
