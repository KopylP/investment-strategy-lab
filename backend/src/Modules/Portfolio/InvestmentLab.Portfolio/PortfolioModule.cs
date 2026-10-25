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
using InvestmentLab.Portfolio.Contracts.Accounts;
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
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) =>
        endpoints.MapGroup("/api/portfolio")
            .MapAccounts();
}
