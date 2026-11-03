using InvestmentLab.BuildingBlocks.Modules;
using InvestmentLab.BuildingBlocks.Persistence;
using InvestmentLab.StrategyBuilder.Shared.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;

namespace InvestmentLab.StrategyBuilder;

public sealed class StrategyBuilderModule : IModule
{
    public const string Schema = StrategyBuilderDbContext.SchemaName;

    public string Name => "StrategyBuilder";

    public void AddServices(IHostApplicationBuilder builder) =>
        builder.AddModuleDbContext<StrategyBuilderDbContext>(Schema);

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
    }
}
