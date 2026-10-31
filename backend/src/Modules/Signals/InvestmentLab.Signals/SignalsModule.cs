using InvestmentLab.BuildingBlocks.Messaging;
using InvestmentLab.BuildingBlocks.Modules;
using InvestmentLab.BuildingBlocks.Persistence;
using InvestmentLab.BuildingBlocks.Scheduling;
using InvestmentLab.Signals.Contracts;
using InvestmentLab.Signals.Monitoring.Api;
using InvestmentLab.Signals.Monitoring.Application.ChangeSignal;
using InvestmentLab.Signals.Monitoring.Application.CheckDueSignals;
using InvestmentLab.Signals.Monitoring.Application.CheckSignal;
using InvestmentLab.Signals.Monitoring.Application.CreateSignal;
using InvestmentLab.Signals.Monitoring.Application.DeleteSignal;
using InvestmentLab.Signals.Monitoring.Application.EnsureBuiltInSignals;
using InvestmentLab.Signals.Monitoring.Application.GetSignals;
using InvestmentLab.Signals.Monitoring.Application.Integration;
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

    public void AddServices(IHostApplicationBuilder builder)
    {
        builder.AddModuleDbContext<SignalsDbContext>(Schema);

        var services = builder.Services;

        services.AddScoped<ISignalRepository, SignalRepository>();

        services.AddScoped<GetSignalsHandler>();
        services.AddScoped<CreateSignalHandler>();
        services.AddScoped<ChangeSignalHandler>();
        services.AddScoped<DeleteSignalHandler>();
        services.AddScoped<CheckSignalHandler>();
        services.AddScoped<EnsureBuiltInSignalsHandler>();

        services.AddScoped<SignalChecker>();
        services.AddScoped<SignalReader>();
        services.AddScoped<QuoteReader>();
        services.AddScoped<CrossRates>();
        services.AddScoped<InstrumentSubjectReader>();
        services.AddScoped<PositionSubjectReader>();
        services.AddScoped<PortfolioSubjectReader>();

        services.AddDomainEventMapper<SignalFired, SignalTriggeredV1, SignalFiredMapper>();
        services.AddDomainEventMapper<SignalCleared, SignalClearedV1, SignalClearedMapper>();
        services.AddDomainEventMapper<SignalDeleted, SignalDeletedV1, SignalDeletedMapper>();

        services.AddScheduledJob<SignalsJob>(SignalsJob.Schedule);
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) =>
        endpoints.MapGroup("/api/signals").MapSignals();
}
