namespace InvestmentLab.BuildingBlocks.Persistence;

public interface IModuleDataSeeder
{
    Task SeedAsync(CancellationToken cancellationToken);
}
