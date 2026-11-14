using InvestmentLab.BuildingBlocks.Http;
using InvestmentLab.Portfolio.Transactions.Application.GetTransactions;
using InvestmentLab.Portfolio.Transactions.Application.RecordCashMovement;
using InvestmentLab.Portfolio.Transactions.Application.RecordContribution;
using InvestmentLab.Portfolio.Transactions.Application.RecordInternalTransfer;
using InvestmentLab.Portfolio.Transactions.Application.RecordTrade;
using InvestmentLab.Portfolio.Transactions.Application.ReverseTransaction;
using InvestmentLab.Portfolio.Transactions.Application.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace InvestmentLab.Portfolio.Transactions.Api;

internal static class TransactionEndpoints
{
    public static RouteGroupBuilder MapTransactions(this RouteGroupBuilder group)
    {
        var account = group
            .MapGroup("/accounts/{accountId:guid}")
            .WithTags("Transactions");

        account
            .MapPost("/trades", RecordTrade)
            .WithName("RecordTrade")
            .Produces<RecordedTransaction>();
        account
            .MapPost("/contributions", RecordContribution)
            .WithName("RecordContribution")
            .Produces<RecordedTransaction>();
        account
            .MapPost("/cash-movements", RecordCashMovement)
            .WithName("RecordCashMovement")
            .Produces<RecordedTransaction>();
        account
            .MapPost("/internal-transfers", RecordInternalTransfer)
            .WithName("RecordInternalTransfer")
            .Produces<RecordedTransaction>();
        account
            .MapPost("/transactions/{transactionId:guid}/reverse", ReverseTransaction)
            .WithName("ReverseTransaction")
            .Produces<RecordedTransaction>();
        account
            .MapGet("/transactions", GetTransactions)
            .WithName("GetTransactions")
            .Produces<IReadOnlyList<TransactionDto>>();

        return group;
    }

    private static async Task<IResult> RecordTrade(
        Guid accountId,
        RecordTradeCommand body,
        RecordTradeHandler handler,
        CancellationToken cancellationToken)
    {
        var recorded = await handler.RecordManually(accountId, body, cancellationToken);

        return recorded.ToHttpResult();
    }

    private static async Task<IResult> RecordContribution(
        Guid accountId,
        RecordContributionCommand body,
        RecordContributionHandler handler,
        CancellationToken cancellationToken)
    {
        var recorded = await handler.RecordManually(accountId, body, cancellationToken);

        return recorded.ToHttpResult();
    }

    private static async Task<IResult> RecordCashMovement(
        Guid accountId,
        RecordCashMovementCommand body,
        RecordCashMovementHandler handler,
        CancellationToken cancellationToken)
    {
        var recorded = await handler.RecordManually(accountId, body, cancellationToken);

        return recorded.ToHttpResult();
    }

    private static async Task<IResult> RecordInternalTransfer(
        Guid accountId,
        RecordInternalTransferCommand body,
        RecordInternalTransferHandler handler,
        CancellationToken cancellationToken)
    {
        var recorded = await handler.RecordManually(accountId, body, cancellationToken);

        return recorded.ToHttpResult();
    }

    private static async Task<IResult> ReverseTransaction(
        Guid accountId,
        Guid transactionId,
        ReverseTransactionCommand body,
        ReverseTransactionHandler handler,
        CancellationToken cancellationToken)
    {
        var reversed = await handler.Handle(accountId, transactionId, body, cancellationToken);

        return reversed.ToHttpResult();
    }

    private static async Task<IResult> GetTransactions(
        Guid accountId,
        GetTransactionsHandler handler,
        CancellationToken cancellationToken)
    {
        var transactions = await handler.Handle(accountId, cancellationToken);

        return transactions.ToHttpResult();
    }
}
