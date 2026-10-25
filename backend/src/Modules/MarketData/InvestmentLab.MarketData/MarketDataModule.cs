using InvestmentLab.BuildingBlocks.Modules;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;

namespace InvestmentLab.MarketData;

public sealed class MarketDataModule : IModule
{
    public const string Schema = "marketdata";

    public string Name => "MarketData";

    public void AddServices(IHostApplicationBuilder builder)
    {
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
    }
}
