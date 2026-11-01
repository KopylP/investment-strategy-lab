using InvestmentLab.BuildingBlocks.Messaging;
using InvestmentLab.BuildingBlocks.Modules;
using InvestmentLab.BuildingBlocks.Persistence;
using InvestmentLab.Portfolio.Accounts.Api;
using InvestmentLab.Portfolio.Accounts.Application.AddSubPortfolio;
using InvestmentLab.Portfolio.Accounts.Application.AssignStrategy;
using InvestmentLab.Portfolio.Accounts.Application.ChangeContributionPolicy;
using InvestmentLab.Portfolio.Accounts.Application.ChangeTargetShares;
using InvestmentLab.Portfolio.Accounts.Application.GetAccount;
using InvestmentLab.Portfolio.Accounts.Application.Integration;
using InvestmentLab.Portfolio.Accounts.Application.ListAccounts;
using InvestmentLab.Portfolio.Accounts.Application.OpenAccount;
using InvestmentLab.Portfolio.Accounts.Application.RenameSubPortfolio;
using InvestmentLab.Portfolio.Accounts.Application.RetireSubPortfolio;
using InvestmentLab.Portfolio.Accounts.Domain;
using InvestmentLab.Portfolio.Accounts.Infrastructure;
using InvestmentLab.Portfolio.Contracts.Accounts;
using InvestmentLab.Portfolio.Contracts.Holdings;
using InvestmentLab.Portfolio.Contracts.Transactions;
using InvestmentLab.Portfolio.Holdings.Api;
using InvestmentLab.Portfolio.Holdings.Application.GetHoldings;
using InvestmentLab.Portfolio.Holdings.Infrastructure;
using InvestmentLab.Portfolio.Shared.Infrastructure;
using InvestmentLab.Portfolio.Transactions.Api;
using InvestmentLab.Portfolio.Transactions.Application.GetTransactions;
using InvestmentLab.Portfolio.Transactions.Application.ImportPositions;
using InvestmentLab.Portfolio.Transactions.Application.ConfigureStatementImport;
using InvestmentLab.Portfolio.Transactions.Application.ConfirmStatementImport;
using InvestmentLab.Portfolio.Transactions.Application.GetStatementImport;
using InvestmentLab.Portfolio.Transactions.Application.ListStatementImports;
using InvestmentLab.Portfolio.Transactions.Application.Shared;
using InvestmentLab.Portfolio.Transactions.Application.UndoStatementImport;
using InvestmentLab.Portfolio.Transactions.Application.UploadStatement;
using InvestmentLab.Portfolio.Transactions.Application.Integration;
using InvestmentLab.Portfolio.Transactions.Application.RecordCashMovement;
using InvestmentLab.Portfolio.Transactions.Application.RecordContribution;
using InvestmentLab.Portfolio.Transactions.Application.RecordInternalTransfer;
using InvestmentLab.Portfolio.Transactions.Application.RecordTrade;
using InvestmentLab.Portfolio.Transactions.Application.ReverseRecordedTransaction;
using InvestmentLab.Portfolio.Transactions.Application.ReverseTransaction;
using InvestmentLab.Portfolio.Transactions.Domain;
using InvestmentLab.Portfolio.Transactions.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace InvestmentLab.Portfolio;

public sealed class PortfolioModule : IModule
{
    public const string Schema = PortfolioDbContext.SchemaName;

    public string Name => "Portfolio";

    public void AddServices(IHostApplicationBuilder builder)
    {
        builder.AddModuleDbContext<PortfolioDbContext>(Schema);
        AddDomainEventMappers(builder.Services);

        var services = builder.Services;
        AddAccounts(services);
        AddTransactions(services);
        AddImports(services);
        AddReactionsToReports(services);
        AddHoldings(services);
    }

    private static void AddDomainEventMappers(IServiceCollection services)
    {
        services.AddDomainEventMapper<SubPortfolioAdded, SubPortfolioAddedV1, SubPortfolioAddedMapper>();
        services.AddDomainEventMapper<TargetSharesChanged, TargetSharesChangedV1, TargetSharesChangedMapper>();
        services.AddDomainEventMapper<TransactionRecorded, TransactionRecordedV1, TransactionRecordedMapper>();
        services.AddDomainEventMapper<TransactionReversed, TransactionReversedV1, TransactionReversedMapper>();
        services.AddDomainEventMapper<TransactionRejected, TransactionRejectedV1, TransactionRejectedMapper>();
    }

    private static void AddAccounts(IServiceCollection services)
    {
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<OpenAccountHandler>();
        services.AddScoped<AddSubPortfolioHandler>();
        services.AddScoped<ChangeTargetSharesHandler>();
        services.AddScoped<AssignStrategyHandler>();
        services.AddScoped<ChangeContributionPolicyHandler>();
        services.AddScoped<RetireSubPortfolioHandler>();
        services.AddScoped<RenameSubPortfolioHandler>();
        services.AddScoped<GetAccountHandler>();
        services.AddScoped<ListAccountsHandler>();
        services.AddScoped<IAccountSetupQueries, AccountSetupQueries>();


    }

    private static void AddTransactions(IServiceCollection services)
    {
        services.AddScoped<ITransactionJournal, TransactionJournal>();
        services.AddScoped<TransactionRecorder>();
        services.AddScoped<RecordTradeHandler>();
        services.AddScoped<RecordCashMovementHandler>();
        services.AddScoped<RecordContributionHandler>();
        services.AddScoped<RecordInternalTransferHandler>();
        services.AddScoped<ReverseTransactionHandler>();
        services.AddScoped<ReverseRecordedTransactionHandler>();
        services.AddScoped<GetTransactionsHandler>();
        services.AddScoped<IJournalQueries, JournalQueries>();

    }

    private static void AddImports(IServiceCollection services)
    {
        services.AddScoped<IStatementParser, RevolutStatementParser>();
        services.AddScoped<IStatementImportRepository, StatementImportRepository>();
        services.AddScoped<StatementPricing>();
        services.AddScoped<StatementPreview>();
        services.AddScoped<StatementImportLoader>();
        services.AddScoped<ListStatementImportsHandler>();
        services.AddScoped<UploadStatementHandler>();
        services.AddScoped<GetStatementImportHandler>();
        services.AddScoped<ConfigureStatementImportHandler>();
        services.AddScoped<ConfirmStatementImportHandler>();
        services.AddScoped<UndoStatementImportHandler>();
        services.AddScoped<ImportPositionsHandler>();

    }

    private static void AddReactionsToReports(IServiceCollection services)
    {
        services.AddScoped<CommandRejection>();
        services.AddIntegrationEventHandler<RecordTradeV1, RecordTradeFromReport>();
        services.AddIntegrationEventHandler<RecordContributionV1, RecordContributionFromReport>();
        services.AddIntegrationEventHandler<RecordInternalTransferV1, RecordTransferFromReport>();
        services.AddIntegrationEventHandler<ReverseTransactionV1, ReverseTransactionFromReport>();


    }

    private static void AddHoldings(IServiceCollection services)
    {
        services.AddScoped<IHoldingsReader, HoldingsReader>();
        services.AddScoped<GetHoldingsHandler>();
        services.AddScoped<IHoldingsQueries, HoldingsQueries>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) =>
        endpoints.MapGroup("/api/portfolio")
            .MapAccounts()
            .MapTransactions()
            .MapImport()
            .MapStatementImports()
            .MapHoldings();
}
