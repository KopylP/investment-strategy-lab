using InvestmentLab.BuildingBlocks.Modules;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;

namespace InvestmentLab.Performance;

public sealed class PerformanceModule : IModule
{
    public const string Schema = "performance";

    public string Name => "Performance";

    public void AddServices(IHostApplicationBuilder builder)
    {
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
    }
}
