using InvestmentLab.BuildingBlocks.Modules;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;

namespace InvestmentLab.Taxation;

public sealed class TaxationModule : IModule
{
    public const string Schema = "taxation";

    public string Name => "Taxation";

    public void AddServices(IHostApplicationBuilder builder)
    {
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
    }
}
