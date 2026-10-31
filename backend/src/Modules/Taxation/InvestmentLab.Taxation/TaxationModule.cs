using System.Globalization;
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
using InvestmentLab.Taxation.Declarations.Api;
using InvestmentLab.Taxation.Declarations.Application;
using InvestmentLab.Taxation.Declarations.Domain;
using InvestmentLab.Taxation.Declarations.Infrastructure;
using InvestmentLab.Taxation.Ledger.Domain;
using InvestmentLab.Taxation.Ledger.Infrastructure;
using InvestmentLab.Taxation.Shared.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using InvestmentLab.Taxation.Assessments.Domain;

namespace InvestmentLab.Taxation;

public sealed class TaxationModule : IModule
{
    public const string Schema = TaxationDbContext.SchemaName;

    public string Name => "Taxation";

    public void AddServices(IHostApplicationBuilder builder)
    {
        builder.AddModuleDbContext<TaxationDbContext>(Schema);

        builder.Services.AddDomainEventMapper<TaxAssessmentRecomputed, TaxAssessmentUpdatedV2, TaxAssessmentRecomputedMapper>();

        var services = builder.Services;
        services.AddScoped<ITaxLedgerRepository, TaxLedgerRepository>();
        services.AddScoped<IAssessmentRepository, AssessmentRepository>();
        services.AddScoped<TaxInputs>();
        services.AddScoped<TaxRecalculator>();
        services.AddScoped<TaxQueries>();
        services.AddSingleton(new TaxAlertOptions(
            decimal.TryParse(builder.Configuration["Taxation:AlertShare"], CultureInfo.InvariantCulture, out var share) && share is > 0m and <= 1m ? share : null));
        services.AddScoped<DeclarationQueries>();

        services.AddSingleton<ITaxDeclarationForm, Pit38Form>();
        services.AddScoped<ITaxpayerHistory, TaxpayerHistory>();
        services.AddSingleton<IDeclarationPrinter, DeclarationSummaryPrinter>();
        services.AddSingleton<IDeclarationPrinter, Pit38PdfPrinter>();
        services.AddScoped<IPit8CRepository, Pit8CRepository>();
        services.AddScoped<ITaxEstimator, TaxEstimator>();
        services.AddScoped<IBrokerStatementRepository, BrokerStatementRepository>();
        services.AddSingleton<IBrokerStatementParser, RevolutPnlPdfParser>();
        services.AddScoped<BrokerStatementService>();
        services.AddIntegrationEventHandler<TransactionRecordedV1, RecomputeOnTransactionRecorded>();
        services.AddIntegrationEventHandler<TransactionReversedV1, RecomputeOnTransactionReversed>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) =>
        endpoints.MapGroup("/api/taxation").MapAssessments().MapBrokerStatements().MapDeclarations();
}
