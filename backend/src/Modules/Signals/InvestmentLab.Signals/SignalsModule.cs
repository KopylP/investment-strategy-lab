using System.Reflection;
using InvestmentLab.BuildingBlocks.Modules;
using InvestmentLab.BuildingBlocks.Persistence;
using InvestmentLab.Signals.Contracts;
using InvestmentLab.Signals.Monitoring.Api;
using InvestmentLab.Signals.Monitoring.Application.CheckSignal;
using InvestmentLab.Signals.Monitoring.Application.ReadSubjects;
using InvestmentLab.Signals.Monitoring.Domain;
using InvestmentLab.Signals.Monitoring.Infrastructure;
using InvestmentLab.Signals.Shared.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace InvestmentLab.Signals;

public sealed class SignalsModule : IModule
{
    public const string Schema = SignalsDbContext.SchemaName;

    public string Name => "Signals";

    public Assembly Assembly => typeof(SignalsAssemblyMarker).Assembly;

    public void AddServices(IHostApplicationBuilder builder)
    {
        builder.AddModuleDbContext<SignalsDbContext>(Schema);

        var services = builder.Services;

        services.AddScoped<ISignalRepository, SignalRepository>();
        services.AddScoped<ISignalDirectory, SignalDirectory>();
        services.AddScoped<IMarketSignalStates, MarketSignalStates>();
        services.AddScoped<ISignalReplay, SignalReplay>();
        services.AddScoped<IBuiltInSignalQueries, BuiltInSignalQueries>();
        services.AddSingleton<IBuiltInSignalReplay, BuiltInSignalReplay>();
        services.AddScoped<IInstrumentLabels, InstrumentLabels>();
        services.AddScoped<IInstrumentGroups, InstrumentGroups>();
        services.AddScoped<IOpenPositions, OpenPositions>();

        services.AddScoped<SignalChecker>();
        services.AddScoped<SignalReader>();
        services.AddScoped<QuoteReader>();
        services.AddScoped<InstrumentSubjectReader>();
        services.AddScoped<InstrumentGroupSubjectReader>();
        services.AddScoped<PositionSubjectReader>();
        services.AddScoped<PortfolioSubjectReader>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) =>
        endpoints
            .MapGroup("/api/signals")
            .MapSignals();
}
