using InvestmentLab.BuildingBlocks.Http;
using InvestmentLab.BuildingBlocks.Security;
using InvestmentLab.Portfolio.Accounts.Application.AddSubPortfolio;
using InvestmentLab.Portfolio.Accounts.Application.AssignStrategy;
using InvestmentLab.Portfolio.Accounts.Application.ChangeContributionPolicy;
using InvestmentLab.Portfolio.Accounts.Application.ChangeTargetShares;
using InvestmentLab.Portfolio.Accounts.Application.GetAccount;
using InvestmentLab.Portfolio.Accounts.Application.ListAccounts;
using InvestmentLab.Portfolio.Accounts.Application.OpenAccount;
using InvestmentLab.Portfolio.Accounts.Application.RenameSubPortfolio;
using InvestmentLab.Portfolio.Accounts.Application.RetireSubPortfolio;
using InvestmentLab.Portfolio.Accounts.Application.Shared;
using InvestmentLab.Portfolio.Contracts.Accounts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace InvestmentLab.Portfolio.Accounts.Api;

internal static class AccountEndpoints
{
    public static RouteGroupBuilder MapAccounts(this RouteGroupBuilder group)
    {
        var accounts = group
            .MapGroup("/accounts")
            .WithTags("Accounts");

        accounts
            .MapPost(string.Empty, OpenAccount)
            .WithName("OpenAccount")
            .Produces<AccountSetupDto>();
        accounts
            .MapGet(string.Empty, ListAccounts)
            .WithName("ListAccounts");
        accounts
            .MapGet("/{accountId:guid}", GetAccount)
            .WithName("GetAccount")
            .Produces<AccountSetupDto>();
        accounts
            .MapPost("/{accountId:guid}/sub-portfolios", AddSubPortfolio)
            .WithName("AddSubPortfolio")
            .Produces<AccountSetupDto>();
        accounts
            .MapPut("/{accountId:guid}/target-shares", ChangeTargetShares)
            .WithName("ChangeTargetShares")
            .Produces<AccountSetupDto>();
        accounts
            .MapPut("/{accountId:guid}/contribution-policy", ChangeContributionPolicy)
            .WithName("ChangeContributionPolicy")
            .Produces<AccountSetupDto>();
        accounts
            .MapPut("/{accountId:guid}/sub-portfolios/{subPortfolioId:guid}/strategy", AssignStrategy)
            .WithName("AssignStrategy")
            .Produces<AccountSetupDto>();
        accounts
            .MapPut("/{accountId:guid}/sub-portfolios/{subPortfolioId:guid}/name", RenameSubPortfolio)
            .WithName("RenameSubPortfolio")
            .Produces<AccountSetupDto>();
        accounts
            .MapPost("/{accountId:guid}/sub-portfolios/{subPortfolioId:guid}/retire", RetireSubPortfolio)
            .WithName("RetireSubPortfolio")
            .Produces<AccountSetupDto>();

        return group;
    }

    private static async Task<IResult> OpenAccount(
        OpenAccountCommand body, OpenAccountHandler handler, ICurrentUser user, CancellationToken cancellationToken)
    {
        var opened = await handler.Handle(user.RequiredId, body, cancellationToken);

        return opened.ToHttpResult();
    }

    private static async Task<Ok<IReadOnlyList<AccountSetupDto>>> ListAccounts(
        ListAccountsHandler handler, CancellationToken cancellationToken)
    {
        var accounts = await handler.Handle(cancellationToken);

        return TypedResults.Ok(accounts);
    }

    private static async Task<IResult> GetAccount(Guid accountId, GetAccountHandler handler, CancellationToken cancellationToken)
    {
        var account = await handler.Handle(accountId, cancellationToken);

        return account.ToHttpResult();
    }

    private static async Task<IResult> AddSubPortfolio(
        Guid accountId, AddSubPortfolioCommand body, AddSubPortfolioHandler handler, CancellationToken cancellationToken)
    {
        var added = await handler.Handle(accountId, body, cancellationToken);

        return added.ToHttpResult();
    }

    private static async Task<IResult> ChangeTargetShares(
        Guid accountId, ChangeTargetSharesCommand body, ChangeTargetSharesHandler handler, CancellationToken cancellationToken)
    {
        var changed = await handler.Handle(accountId, body, cancellationToken);

        return changed.ToHttpResult();
    }

    private static async Task<IResult> ChangeContributionPolicy(
        Guid accountId, ContributionPolicyChoice body, ChangeContributionPolicyHandler handler, CancellationToken cancellationToken)
    {
        var changed = await handler.Handle(accountId, body, cancellationToken);

        return changed.ToHttpResult();
    }

    private static async Task<IResult> AssignStrategy(
        Guid accountId, Guid subPortfolioId, AssignStrategyCommand body, AssignStrategyHandler handler, CancellationToken cancellationToken)
    {
        var assigned = await handler.Handle(accountId, subPortfolioId, body, cancellationToken);

        return assigned.ToHttpResult();
    }

    private static async Task<IResult> RenameSubPortfolio(
        Guid accountId, Guid subPortfolioId, RenameSubPortfolioCommand body, RenameSubPortfolioHandler handler, CancellationToken cancellationToken)
    {
        var renamed = await handler.Handle(accountId, subPortfolioId, body, cancellationToken);

        return renamed.ToHttpResult();
    }

    private static async Task<IResult> RetireSubPortfolio(
        Guid accountId, Guid subPortfolioId, RetireSubPortfolioCommand body, RetireSubPortfolioHandler handler, CancellationToken cancellationToken)
    {
        var retired = await handler.Handle(accountId, subPortfolioId, body, cancellationToken);

        return retired.ToHttpResult();
    }
}
