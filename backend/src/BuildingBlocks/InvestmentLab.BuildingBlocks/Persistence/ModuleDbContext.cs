using InvestmentLab.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace InvestmentLab.BuildingBlocks.Persistence;

/// <summary>
/// Base of every module's DbContext: own schema, no cross-module mappings,
/// SharedKernel value objects stored as primitives.
/// </summary>
public abstract class ModuleDbContext(DbContextOptions options) : DbContext(options)
{
    protected abstract string Schema { get; }

    protected abstract void OnModuleModelCreating(ModelBuilder modelBuilder);

    protected sealed override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        OnModuleModelCreating(modelBuilder);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Prices with sub-cent precision and fractional shares both fit.
        configurationBuilder.Properties<decimal>().HavePrecision(20, 8);

        configurationBuilder.Properties<Currency>().HaveConversion<SharedKernelConverters.CurrencyConverter>().HaveMaxLength(3);
        configurationBuilder.Properties<Isin>().HaveConversion<SharedKernelConverters.IsinConverter>().HaveMaxLength(12);
        configurationBuilder.Properties<UserId>().HaveConversion<SharedKernelConverters.UserIdConverter>();
        configurationBuilder.Properties<YearMonth>().HaveConversion<SharedKernelConverters.YearMonthConverter>().HaveMaxLength(7);
        configurationBuilder.Properties<Quantity>().HaveConversion<SharedKernelConverters.QuantityConverter>();
        configurationBuilder.Properties<Percentage>().HaveConversion<SharedKernelConverters.PercentageConverter>();
    }
}
