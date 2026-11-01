using System.Globalization;
using InvestmentLab.BuildingBlocks.Messaging;
using InvestmentLab.BuildingBlocks.Modules;
using InvestmentLab.BuildingBlocks.Persistence;
using InvestmentLab.Portfolio.Contracts.Transactions;
using InvestmentLab.Taxation.Assessments.Api;
using InvestmentLab.Taxation.Assessments.Application.GetTaxProgress;
using InvestmentLab.Taxation.Assessments.Application.GetTaxYear;
using InvestmentLab.Taxation.Assessments.Application.GetTaxRegime;
using InvestmentLab.Taxation.Assessments.Application.GetTaxYears;
using InvestmentLab.Taxation.Assessments.Application.ListTaxPolicies;
using InvestmentLab.Taxation.Assessments.Application.Integration;
using InvestmentLab.Taxation.Assessments.Application.ListBrokerStatements;
using InvestmentLab.Taxation.Assessments.Application.ListLossCarryforwards;
using InvestmentLab.Taxation.Assessments.Application.RecordLossCarryforward;
using InvestmentLab.Taxation.Assessments.Application.RefreshTaxAssessments;
using InvestmentLab.Taxation.Assessments.Application.RemoveBrokerStatement;
using InvestmentLab.Taxation.Assessments.Application.RemoveLossCarryforward;
using InvestmentLab.Taxation.Assessments.Application.Shared;
using InvestmentLab.Taxation.Assessments.Application.UploadBrokerStatement;
using InvestmentLab.Taxation.Assessments.Domain;
using InvestmentLab.Taxation.Assessments.Infrastructure;
using InvestmentLab.Taxation.BrokerStatements.Domain;
using InvestmentLab.Taxation.BrokerStatements.Infrastructure;
using InvestmentLab.Taxation.Contracts;
using InvestmentLab.Taxation.Declarations.Api;
using InvestmentLab.Taxation.Declarations.Application.GetDeclaration;
using InvestmentLab.Taxation.Declarations.Application.GetDeclarationPdf;
using InvestmentLab.Taxation.Declarations.Application.ListDeclarationForms;
using InvestmentLab.Taxation.Declarations.Application.RemovePit8C;
using InvestmentLab.Taxation.Declarations.Application.SetPit8C;
using InvestmentLab.Taxation.Declarations.Application.Shared;
using InvestmentLab.Taxation.Declarations.Domain;
using InvestmentLab.Taxation.Declarations.Infrastructure;
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
        builder.Services.AddDomainEventMapper<TaxAssessmentRecomputed, TaxAssessmentUpdatedV2, TaxAssessmentRecomputedMapper>();

        var services = builder.Services;
        AddAssessments(builder);
        AddDeclarations(services);
        AddBrokerStatements(services);
    }

    private static decimal? AlertShareOf(IHostApplicationBuilder builder)
    {
        var configured = decimal.TryParse(builder.Configuration["Taxation:AlertShare"], CultureInfo.InvariantCulture, out var share);

        return configured && share is > 0m and <= 1m ? share : null;
    }

    private static void AddAssessments(IHostApplicationBuilder builder)
    {
        var services = builder.Services;
        services.AddScoped<ITaxLedgerRepository, TaxLedgerRepository>();
        services.AddScoped<IAssessmentRepository, AssessmentRepository>();
        services.AddScoped<TaxAccountSync>();
        services.AddScoped<TaxpayerReader>();
        services.AddScoped<TaxEventReader>();
        services.AddScoped<ITaxCurrencyRates, TaxCurrencyRates>();
        services.AddScoped<ITaxpayerAccounts, TaxpayerAccounts>();
        services.AddScoped<ITaxJournals, TaxJournals>();
        services.AddScoped<IInstrumentKeys, InstrumentKeys>();
        services.AddScoped<TaxRecalculator>();
        services.AddScoped<RefreshTaxAssessmentsHandler>();
        services.AddScoped<GetTaxYearsHandler>();
        services.AddScoped<GetTaxRegimeHandler>();
        services.AddSingleton<ListTaxPoliciesHandler>();
        services.AddScoped<GetTaxYearHandler>();
        services.AddScoped<GetTaxProgressHandler>();
        services.AddScoped<ListLossCarryforwardsHandler>();
        services.AddScoped<RecordLossCarryforwardHandler>();
        services.AddScoped<RemoveLossCarryforwardHandler>();
        services.AddSingleton(new TaxAlertOptions(AlertShareOf(builder)));

    }

    private static void AddDeclarations(IServiceCollection services)
    {
        services.AddScoped<DeclarationAccounts>();
        services.AddScoped<ListDeclarationFormsHandler>();
        services.AddScoped<GetDeclarationHandler>();
        services.AddScoped<GetDeclarationPdfHandler>();
        services.AddScoped<SetPit8CHandler>();
        services.AddScoped<RemovePit8CHandler>();

        services.AddSingleton<ITaxDeclarationForm, Pit38Form>();
        services.AddScoped<ITaxpayerHistory, TaxpayerHistory>();
        services.AddSingleton<IDeclarationPrinter, DeclarationSummaryPrinter>();
        services.AddSingleton<IDeclarationPrinter, Pit38PdfPrinter>();
        services.AddScoped<IPit8CRepository, Pit8CRepository>();
        services.AddScoped<ITaxEstimator, TaxEstimator>();
        services.AddSingleton<ITaxPolicyDirectory, TaxPolicyDirectory>();

    }

    private static void AddBrokerStatements(IServiceCollection services)
    {
        services.AddScoped<IBrokerStatementRepository, BrokerStatementRepository>();
        services.AddSingleton<IBrokerStatementParser, RevolutPnlPdfParser>();
        services.AddScoped<ListBrokerStatementsHandler>();
        services.AddScoped<UploadBrokerStatementHandler>();
        services.AddScoped<RemoveBrokerStatementHandler>();
        services.AddIntegrationEventHandler<TransactionRecordedV1, RecomputeOnTransactionRecorded>();
        services.AddIntegrationEventHandler<TransactionReversedV1, RecomputeOnTransactionReversed>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) =>
        endpoints.MapGroup("/api/taxation").MapAssessments().MapBrokerStatements().MapDeclarations();
}
