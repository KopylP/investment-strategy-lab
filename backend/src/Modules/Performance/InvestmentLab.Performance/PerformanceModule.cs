using System.Reflection;
using InvestmentLab.BuildingBlocks.Modules;
using InvestmentLab.BuildingBlocks.Persistence;
using InvestmentLab.Signals.Contracts;
using InvestmentLab.Performance.Shared.Infrastructure;
using InvestmentLab.Performance.Valuation.Api;
using InvestmentLab.Performance.Valuation.Application.Integration;
using InvestmentLab.Performance.Valuation.Application.Shared;
using InvestmentLab.Performance.Valuation.Domain;
using InvestmentLab.Performance.Valuation.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace InvestmentLab.Performance;

public sealed class PerformanceModule : IModule
{
    public const string Schema = PerformanceDbContext.SchemaName;

    public string Name => "Performance";

    public Assembly Assembly => typeof(PerformanceAssemblyMarker).Assembly;

    public void AddServices(IHostApplicationBuilder builder)
    {
        builder.AddModuleDbContext<PerformanceDbContext>(Schema);

        var services = builder.Services;
        services.AddScoped<IPerformanceRepository, PerformanceRepository>();
        services.AddScoped<PriceBook>();
        services.AddScoped<SnapshotBuilder>();
        services.AddScoped<AccountHistoryLoader>();
        services.AddScoped<IAccountJournals, AccountJournals>();
        services.AddScoped<IPortfolioReturnHistory, PortfolioReturnHistory>();
        services.AddScoped<SnapshotInvalidation>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) =>
        endpoints.MapGroup("/api/performance").MapDashboard();
}
