using System.Reflection;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;

namespace InvestmentLab.BuildingBlocks.Modules;

public interface IModule
{
    string Name { get; }

    Assembly Assembly { get; }

    void AddServices(IHostApplicationBuilder builder);

    void MapEndpoints(IEndpointRouteBuilder endpoints);
}
