using InvestmentLab.BuildingBlocks.Http;
using InvestmentLab.Portfolio.Transactions.Application.GetTransactions;
using InvestmentLab.Portfolio.Transactions.Application.RecordCashMovement;
using InvestmentLab.Portfolio.Transactions.Application.RecordInternalTransfer;
using InvestmentLab.Portfolio.Transactions.Application.RecordTrade;
using InvestmentLab.Portfolio.Transactions.Application.ReverseTransaction;
using InvestmentLab.Portfolio.Transactions.Domain;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace InvestmentLab.Portfolio.Transactions.Api;

internal static class TransactionEndpoints
{
    public static RouteGroupBuilder MapTransactions(this RouteGroupBuilder group)
    {
        var account = group.MapGroup("/accounts/{accountId:guid}").WithTags("Transactions");

        account.MapPost("/trades", async (Guid accountId, RecordTradeCommand body, RecordTradeHandler handler, CancellationToken ct) =>
                (await handler.Handle(accountId, body, TransactionSource.Manual, ct)).ToHttpResult())
            .WithName("RecordTrade");

        account.MapPost("/contributions", async (Guid accountId, RecordContributionCommand body, RecordCashMovementHandler handler, CancellationToken ct) =>
                (await handler.Handle(accountId, body, TransactionSource.Manual, ct)).ToHttpResult())
            .WithName("RecordContribution");

        account.MapPost("/cash-movements", async (Guid accountId, RecordCashMovementCommand body, RecordCashMovementHandler handler, CancellationToken ct) =>
                (await handler.Handle(accountId, body, TransactionSource.Manual, ct)).ToHttpResult())
            .WithName("RecordCashMovement");

        account.MapPost("/internal-transfers", async (Guid accountId, RecordInternalTransferCommand body, RecordInternalTransferHandler handler, CancellationToken ct) =>
                (await handler.Handle(accountId, body, ct)).ToHttpResult())
            .WithName("RecordInternalTransfer");

        account.MapPost("/transactions/{transactionId:guid}/reverse", async (
                Guid accountId, Guid transactionId, ReverseTransactionCommand body, ReverseTransactionHandler handler, CancellationToken ct) =>
                (await handler.Handle(accountId, transactionId, body, ct)).ToHttpResult())
            .WithName("ReverseTransaction");

        account.MapGet("/transactions", async (Guid accountId, GetTransactionsHandler handler, CancellationToken ct) =>
                (await handler.Handle(accountId, ct)).ToHttpResult())
            .WithName("GetTransactions");

        return group;
    }
}
