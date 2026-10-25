using InvestmentLab.BuildingBlocks.Http;
using InvestmentLab.BuildingBlocks.Security;
using InvestmentLab.Portfolio.Accounts.Application;
using InvestmentLab.Portfolio.Accounts.Application.AddSubPortfolio;
using InvestmentLab.Portfolio.Accounts.Application.AssignStrategy;
using InvestmentLab.Portfolio.Accounts.Application.ChangeContributionPolicy;
using InvestmentLab.Portfolio.Accounts.Application.ChangeTargetShares;
using InvestmentLab.Portfolio.Accounts.Application.GetAccount;
using InvestmentLab.Portfolio.Accounts.Application.OpenAccount;
using InvestmentLab.Portfolio.Accounts.Application.RetireSubPortfolio;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace InvestmentLab.Portfolio.Accounts.Api;

internal static class AccountEndpoints
{
    public static RouteGroupBuilder MapAccounts(this RouteGroupBuilder group)
    {
        var accounts = group.MapGroup("/accounts").WithTags("Accounts");

        accounts.MapPost("", async (OpenAccountCommand body, OpenAccountHandler handler, ICurrentUser user, CancellationToken ct) =>
                (await handler.Handle(user.RequiredId, body, ct)).ToHttpResult())
            .WithName("OpenAccount");

        accounts.MapGet("", async (GetAccountHandler handler, CancellationToken ct) => TypedResults.Ok(await handler.List(ct)))
            .WithName("ListAccounts");

        accounts.MapGet("/{accountId:guid}", async (Guid accountId, GetAccountHandler handler, CancellationToken ct) =>
                (await handler.Handle(accountId, ct)).ToHttpResult())
            .WithName("GetAccount");

        accounts.MapPost("/{accountId:guid}/sub-portfolios", async (
                Guid accountId, AddSubPortfolioCommand body, AddSubPortfolioHandler handler, CancellationToken ct) =>
                (await handler.Handle(accountId, body, ct)).ToHttpResult())
            .WithName("AddSubPortfolio");

        accounts.MapPut("/{accountId:guid}/target-shares", async (
                Guid accountId, ChangeTargetSharesCommand body, ChangeTargetSharesHandler handler, CancellationToken ct) =>
                (await handler.Handle(accountId, body, ct)).ToHttpResult())
            .WithName("ChangeTargetShares");

        accounts.MapPut("/{accountId:guid}/contribution-policy", async (
                Guid accountId, ContributionPolicyInput body, ChangeContributionPolicyHandler handler, CancellationToken ct) =>
                (await handler.Handle(accountId, body, ct)).ToHttpResult())
            .WithName("ChangeContributionPolicy");

        accounts.MapPut("/{accountId:guid}/sub-portfolios/{subPortfolioId:guid}/strategy", async (
                Guid accountId, Guid subPortfolioId, AssignStrategyCommand body, AssignStrategyHandler handler, CancellationToken ct) =>
                (await handler.Handle(accountId, subPortfolioId, body, ct)).ToHttpResult())
            .WithName("AssignStrategy");

        accounts.MapPost("/{accountId:guid}/sub-portfolios/{subPortfolioId:guid}/retire", async (
                Guid accountId, Guid subPortfolioId, RetireSubPortfolioCommand body, RetireSubPortfolioHandler handler, CancellationToken ct) =>
                (await handler.Handle(accountId, subPortfolioId, body, ct)).ToHttpResult())
            .WithName("RetireSubPortfolio");

        return group;
    }
}
