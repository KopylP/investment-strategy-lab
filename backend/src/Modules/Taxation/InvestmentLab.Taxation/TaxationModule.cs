using InvestmentLab.BuildingBlocks.Messaging;
using InvestmentLab.BuildingBlocks.Modules;
using InvestmentLab.BuildingBlocks.Persistence;
using InvestmentLab.Portfolio.Contracts.Transactions;
using InvestmentLab.Taxation.Assessments.Api;
using InvestmentLab.Taxation.Assessments.Application;
using InvestmentLab.Taxation.Assessments.Application.Integration;
using InvestmentLab.Taxation.Assessments.Infrastructure;
using InvestmentLab.Taxation.BrokerStatements.Domain;
using InvestmentLab.Taxation.BrokerStatements.Infrastructure;
using InvestmentLab.Taxation.Contracts;
using InvestmentLab.Taxation.Ledger.Domain;
using InvestmentLab.Taxation.Ledger.Infrastructure;
using InvestmentLab.Taxation.Shared.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace InvestmentLab.Taxation;

public sealed class TaxationModule : IModule
{
    public const string Schema = TaxationDbContext.SchemaName;

    public string Name => "Taxation";

    public void AddServices(IHostApplicationBuilder builder)
    {
        builder.AddModuleDbContext<TaxationDbContext>(Schema);

        var services = builder.Services;
        services.AddScoped<ITaxLedgerRepository, TaxLedgerRepository>();
        services.AddScoped<IAssessmentRepository, AssessmentRepository>();
        services.AddScoped<TaxInputs>();
        services.AddScoped<TaxRecalculator>();
        services.AddScoped<TaxQueries>();
        services.AddScoped<ITaxEstimator, TaxEstimator>();
        services.AddScoped<IBrokerStatementRepository, BrokerStatementRepository>();
        services.AddSingleton<IBrokerStatementParser, RevolutPnlPdfParser>();
        services.AddScoped<BrokerStatementService>();
        services.AddIntegrationEventHandler<TransactionRecordedV1, WhenJournalChanges>();
        services.AddIntegrationEventHandler<TransactionReversedV1, WhenJournalChanges>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) =>
        endpoints.MapGroup("/api/taxation").MapAssessments().MapBrokerStatements();
}
