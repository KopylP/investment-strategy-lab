using System.Text.Json;
using InvestmentLab.Portfolio.Accounts.Domain;
using InvestmentLab.Portfolio.Transactions.Application.Shared;
using InvestmentLab.Portfolio.Transactions.Domain;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace InvestmentLab.Portfolio.Transactions.Infrastructure;

internal sealed class TransactionDetailsConverter() : ValueConverter<TransactionDetails, string>(
    details => Serialize(details), json => Deserialize(json))
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private static string Serialize(TransactionDetails details) => JsonSerializer.Serialize(details switch
    {
        TradeDetails trade => new DetailsJson("trade", Trade: new TradeJson(
            trade.InstrumentId, trade.Side.ToString(), trade.Quantity.Value, trade.Price.Amount, trade.Price.Currency.Code, trade.Fee.Amount,
            [.. trade.Allocations.Select(a => new PartJson(a.SubPortfolioId.Value, a.Quantity.Value))], trade.Settlement?.Amount, trade.Settlement?.Currency.Code)),
        CashMovementDetails cash => new DetailsJson("cash", Cash: new CashJson(
            cash.Kind.ToString(), cash.Amount.Amount, cash.Amount.Currency.Code, cash.InstrumentId,
            [.. cash.Allocations.Select(a => new PartJson(a.SubPortfolioId.Value, a.Amount.Amount))], cash.Settlement?.Amount, cash.Settlement?.Currency.Code)),
        InternalTransferDetails transfer => new DetailsJson("transfer", Transfer: new TransferJson(
            transfer.From.Value, transfer.To.Value, transfer.InstrumentId, transfer.Quantity?.Value, transfer.Amount?.Amount, transfer.Amount?.Currency.Code)),
        ReversalDetails reversal => new DetailsJson("reversal", Reversal: new ReversalJson(reversal.Original.Value, reversal.Reason)),
        _ => throw new ArgumentOutOfRangeException(nameof(details), details.GetType().Name, null),
    }, Json);

    private static TransactionDetails Deserialize(string json)
    {
        var details = JsonSerializer.Deserialize<DetailsJson>(json, Json)!;

        switch (details.Type)
        {
            case "trade":
                var trade = details.Trade!;
                var currency = Currency.Of(trade.Currency);

                return new TradeDetails(trade.InstrumentId, Enum.Parse<TradeSide>(trade.Side), Quantity.Of(trade.Quantity), Price.Of(trade.Price, currency),
                    Money.Of(trade.Fee, currency), [.. trade.Allocations.Select(p => new Allocation(SubPortfolioId.Of(p.SubPortfolioId), Quantity.Of(p.Value)))],
                    Settlement(trade.SettledAmount, trade.SettledCurrency));
            case "cash":
                var cash = details.Cash!;
                var cashCurrency = Currency.Of(cash.Currency);

                return new CashMovementDetails(Enum.Parse<CashMovementKind>(cash.Kind), Money.Of(cash.Amount, cashCurrency),
                    [.. cash.Allocations.Select(p => new CashAllocation(SubPortfolioId.Of(p.SubPortfolioId), Money.Of(p.Value, cashCurrency)))], cash.InstrumentId,
                    Settlement(cash.SettledAmount, cash.SettledCurrency));
            case "transfer":
                var transfer = details.Transfer!;

                return new InternalTransferDetails(SubPortfolioId.Of(transfer.From), SubPortfolioId.Of(transfer.To), transfer.InstrumentId,
                    transfer.Quantity is { } quantity ? Quantity.Of(quantity) : null,
                    transfer.Amount is { } amount ? Money.Of(amount, Currency.Of(transfer.Currency!)) : null);
            case "reversal":
                return new ReversalDetails(TransactionId.Of(details.Reversal!.Original), details.Reversal.Reason);
            default:
                throw new InvalidOperationException($"Unknown transaction body '{details.Type}'.");
        }
    }

    private static Money? Settlement(decimal? amount, string? currency) =>
        amount is { } value && currency is { Length: > 0 } code ? Money.Of(value, Currency.Of(code)) : null;

    private sealed record DetailsJson(string Type, TradeJson? Trade = null, CashJson? Cash = null, TransferJson? Transfer = null, ReversalJson? Reversal = null);

    private sealed record PartJson(Guid SubPortfolioId, decimal Value);

    private sealed record TradeJson(
        Guid InstrumentId, string Side, decimal Quantity, decimal Price, string Currency, decimal Fee, IReadOnlyList<PartJson> Allocations,
        decimal? SettledAmount = null, string? SettledCurrency = null);

    private sealed record CashJson(
        string Kind, decimal Amount, string Currency, Guid? InstrumentId, IReadOnlyList<PartJson> Allocations,
        decimal? SettledAmount = null, string? SettledCurrency = null);

    private sealed record TransferJson(Guid From, Guid To, Guid? InstrumentId, decimal? Quantity, decimal? Amount, string? Currency);

    private sealed record ReversalJson(Guid Original, string Reason);
}
