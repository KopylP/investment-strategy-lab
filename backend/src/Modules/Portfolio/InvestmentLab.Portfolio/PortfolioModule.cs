using InvestmentLab.BuildingBlocks.Modules;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;

namespace InvestmentLab.Portfolio;

public sealed class PortfolioModule : IModule
{
    public const string Schema = "portfolio";

    public string Name => "Portfolio";

    public void AddServices(IHostApplicationBuilder builder)
    {
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
    }
}
