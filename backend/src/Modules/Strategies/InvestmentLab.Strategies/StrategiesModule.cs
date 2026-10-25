using InvestmentLab.BuildingBlocks.Modules;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;

namespace InvestmentLab.Strategies;

public sealed class StrategiesModule : IModule
{
    public const string Schema = "strategies";

    public string Name => "Strategies";

    public void AddServices(IHostApplicationBuilder builder)
    {
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
    }
}
