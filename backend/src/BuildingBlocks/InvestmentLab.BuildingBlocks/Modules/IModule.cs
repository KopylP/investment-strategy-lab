using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;

namespace InvestmentLab.BuildingBlocks.Modules;

/// <summary>
/// A bounded context plugged into the host. The host knows only this interface,
/// never the module's internals.
/// </summary>
public interface IModule
{
    string Name { get; }

    void AddServices(IHostApplicationBuilder builder);

    void MapEndpoints(IEndpointRouteBuilder endpoints);
}
