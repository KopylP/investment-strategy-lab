using System.Reflection;
using InvestmentLab.BuildingBlocks.Modules;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace InvestmentLab.BuildingBlocks.Tests.Modules;

public sealed class ModuleRegistrationTests
{
    private sealed class Marker;

    private sealed class SampleModule : IModule
    {
        public string Name => "Sample";

        public Assembly Assembly => typeof(IModule).Assembly;

        public void AddServices(IHostApplicationBuilder builder) => builder.Services.AddSingleton<Marker>();

        public void MapEndpoints(IEndpointRouteBuilder endpoints) => endpoints.MapGet("/sample", () => "ok");
    }

    [Fact]
    public void Modules_add_their_services_and_endpoints()
    {
        var builder = WebApplication.CreateSlimBuilder();

        builder.AddModules(new SampleModule());
        var app = builder.Build();
        app.MapModules();

        app.Services.GetService<Marker>().ShouldNotBeNull();
        app.Services.GetRequiredService<IReadOnlyList<IModule>>().ShouldHaveSingleItem().Name.ShouldBe("Sample");
        ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .ShouldContain(endpoint => endpoint.RoutePattern.RawText == "/sample");
    }
}
