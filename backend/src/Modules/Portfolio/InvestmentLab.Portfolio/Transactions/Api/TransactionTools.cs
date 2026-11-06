using System.ComponentModel;
using InvestmentLab.BuildingBlocks.Mcp;
using InvestmentLab.Portfolio.Transactions.Application.GetTransactions;
using InvestmentLab.Portfolio.Transactions.Application.RecordCashMovement;
using InvestmentLab.Portfolio.Transactions.Application.RecordContribution;
using InvestmentLab.Portfolio.Transactions.Application.RecordInternalTransfer;
using InvestmentLab.Portfolio.Transactions.Application.RecordTrade;
using InvestmentLab.Portfolio.Transactions.Application.Shared;
using ModelContextProtocol.Server;

namespace InvestmentLab.Portfolio.Transactions.Api;

[McpServerToolType]
internal static class TransactionTools
{
    [McpServerTool(Name = "get_transactions", ReadOnly = true)]
    [Description("An account's recorded transactions.")]
    public static async Task<IReadOnlyList<TransactionDto>> GetTransactions(Guid accountId, GetTransactionsHandler handler, CancellationToken cancellationToken)
    {
        var transactions = await handler.Handle(accountId, cancellationToken);

        return transactions.ValueOrThrow();
    }

    [McpServerTool(Name = "record_trade", Destructive = true)]
    [Description("Records a buy or sell the user made at the broker.")]
    public static async Task<RecordedTransaction> RecordTrade(
        Guid accountId,
        RecordTradeCommand trade,
        RecordTradeHandler handler,
        CancellationToken cancellationToken)
    {
        var recorded = await handler.RecordManually(accountId, trade, cancellationToken);

        return recorded.ValueOrThrow();
    }

    [McpServerTool(Name = "record_contribution", Destructive = true)]
    [Description("Records money the user paid into the account, optionally split between sub-portfolios.")]
    public static async Task<RecordedTransaction> RecordContribution(
        Guid accountId,
        RecordContributionCommand contribution,
        RecordContributionHandler handler,
        CancellationToken cancellationToken)
    {
        var recorded = await handler.RecordManually(accountId, contribution, cancellationToken);

        return recorded.ValueOrThrow();
    }

    [McpServerTool(Name = "record_cash_movement", Destructive = true)]
    [Description("Records a dividend, interest, fee, tax or withdrawal.")]
    public static async Task<RecordedTransaction> RecordCashMovement(
        Guid accountId,
        RecordCashMovementCommand movement,
        RecordCashMovementHandler handler,
        CancellationToken cancellationToken)
    {
        var recorded = await handler.RecordManually(accountId, movement, cancellationToken);

        return recorded.ValueOrThrow();
    }

    [McpServerTool(Name = "record_internal_transfer", Destructive = true)]
    [Description("Records moving cash or holdings between sub-portfolios of one account.")]
    public static async Task<RecordedTransaction> RecordInternalTransfer(
        Guid accountId,
        RecordInternalTransferCommand transfer,
        RecordInternalTransferHandler handler,
        CancellationToken cancellationToken)
    {
        var recorded = await handler.RecordManually(accountId, transfer, cancellationToken);

        return recorded.ValueOrThrow();
    }
}
