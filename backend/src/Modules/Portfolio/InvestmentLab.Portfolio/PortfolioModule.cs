using InvestmentLab.BuildingBlocks.Modules;
using InvestmentLab.BuildingBlocks.Persistence;
using InvestmentLab.Portfolio.Accounts.Api;
using InvestmentLab.Portfolio.Accounts.Application;
using InvestmentLab.Portfolio.Accounts.Application.AddSubPortfolio;
using InvestmentLab.Portfolio.Accounts.Application.AssignStrategy;
using InvestmentLab.Portfolio.Accounts.Application.ChangeContributionPolicy;
using InvestmentLab.Portfolio.Accounts.Application.ChangeTargetShares;
using InvestmentLab.Portfolio.Accounts.Application.GetAccount;
using InvestmentLab.Portfolio.Accounts.Application.OpenAccount;
using InvestmentLab.Portfolio.Accounts.Application.RetireSubPortfolio;
using InvestmentLab.Portfolio.Accounts.Infrastructure;
using InvestmentLab.BuildingBlocks.Messaging;
using InvestmentLab.Portfolio.Contracts.Accounts;
using InvestmentLab.Portfolio.Contracts.Holdings;
using InvestmentLab.Portfolio.Contracts.Transactions;
using InvestmentLab.Portfolio.Holdings.Api;
using InvestmentLab.Portfolio.Holdings.Application.GetHoldings;
using InvestmentLab.Portfolio.Holdings.Infrastructure;
using InvestmentLab.Portfolio.Transactions.Api;
using InvestmentLab.Portfolio.Transactions.Application;
using InvestmentLab.Portfolio.Transactions.Application.GetTransactions;
using InvestmentLab.Portfolio.Transactions.Application.ImportPositions;
using InvestmentLab.Portfolio.Transactions.Application.Integration;
using InvestmentLab.Portfolio.Transactions.Application.RecordCashMovement;
using InvestmentLab.Portfolio.Transactions.Application.RecordInternalTransfer;
using InvestmentLab.Portfolio.Transactions.Application.RecordTrade;
using InvestmentLab.Portfolio.Transactions.Application.ReverseTransaction;
using InvestmentLab.Portfolio.Transactions.Infrastructure;
using InvestmentLab.Portfolio.Shared.Infrastructure;
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

        var services = builder.Services;
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<OpenAccountHandler>();
        services.AddScoped<AddSubPortfolioHandler>();
        services.AddScoped<ChangeTargetSharesHandler>();
        services.AddScoped<AssignStrategyHandler>();
        services.AddScoped<ChangeContributionPolicyHandler>();
        services.AddScoped<RetireSubPortfolioHandler>();
        services.AddScoped<GetAccountHandler>();
        services.AddScoped<IAccountSetupQueries, AccountSetupQueries>();

        services.AddScoped<ITransactionJournal, TransactionJournal>();
        services.AddScoped<TransactionRecorder>();
        services.AddScoped<RecordTradeHandler>();
        services.AddScoped<RecordCashMovementHandler>();
        services.AddScoped<RecordInternalTransferHandler>();
        services.AddScoped<ReverseTransactionHandler>();
        services.AddScoped<GetTransactionsHandler>();
        services.AddScoped<ImportPositionsHandler>();
        services.AddIntegrationEventHandler<RecordTradeV1, PortfolioCommandHandlers>();
        services.AddIntegrationEventHandler<RecordContributionV1, PortfolioCommandHandlers>();
        services.AddIntegrationEventHandler<RecordInternalTransferV1, PortfolioCommandHandlers>();
        services.AddIntegrationEventHandler<ReverseTransactionV1, PortfolioCommandHandlers>();

        services.AddScoped<IHoldingsReader, HoldingsReader>();
        services.AddScoped<GetHoldingsHandler>();
        services.AddScoped<IHoldingsQueries, HoldingsQueries>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) =>
        endpoints.MapGroup("/api/portfolio")
            .MapAccounts()
            .MapTransactions()
            .MapImport()
            .MapHoldings();
}
