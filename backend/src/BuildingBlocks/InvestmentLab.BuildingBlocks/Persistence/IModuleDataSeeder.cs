namespace InvestmentLab.BuildingBlocks.Persistence;

/// <summary>Reference data a module needs from day one; runs after all migrations, must be idempotent.</summary>
public interface IModuleDataSeeder
{
    Task SeedAsync(CancellationToken cancellationToken);
}
