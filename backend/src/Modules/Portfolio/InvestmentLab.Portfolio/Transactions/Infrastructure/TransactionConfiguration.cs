using System.Text.Json;
using InvestmentLab.Portfolio.Accounts.Domain;
using InvestmentLab.Portfolio.Shared.Infrastructure;
using InvestmentLab.Portfolio.Transactions.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace InvestmentLab.Portfolio.Transactions.Infrastructure;

internal sealed class TransactionConfiguration(PortfolioDbContext context) : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> transaction)
    {
        transaction.ToTable("transactions");
        transaction.HasKey(t => t.Id);
        transaction.Property(t => t.Id).HasConversion<TransactionIdConverter>().ValueGeneratedNever();
        transaction.Property(t => t.AccountId).HasConversion<PortfolioConverters.AccountIdConverter>();
        transaction.Property(t => t.Kind).HasConversion<string>().HasMaxLength(20);
        transaction.Property(t => t.ReversalOf).HasConversion<TransactionIdConverter>();
        transaction.Property(t => t.Details).HasConversion<TransactionDetailsConverter>().HasColumnType("jsonb");
        transaction.OwnsOne(t => t.Source, source =>
        {
            source.Property(s => s.Kind).HasConversion<string>().HasMaxLength(20).HasColumnName("source_kind");
            source.Property(s => s.Reference).HasMaxLength(200).HasColumnName("source_reference");
            // Idempotency of commands from other modules (e.g. a report fill recorded twice).
            source.HasIndex(s => s.Reference).IsUnique().HasFilter("source_reference IS NOT NULL");
        });
        transaction.Navigation(t => t.Source).IsRequired();
        transaction.HasIndex(t => t.ReversalOf).IsUnique().HasFilter("reversal_of IS NOT NULL");
        transaction.HasIndex(t => new { t.AccountId, t.OccurredOn });
        transaction.Ignore(t => t.DomainEvents);
        context.HasOwnerFilter(transaction, t => t.OwnerId);
    }
}

internal sealed class TransactionIdConverter() : ValueConverter<TransactionId, Guid>(id => id.Value, value => TransactionId.Of(value));

/// <summary>Stores the body variant as JSON with primitives, so the domain records stay free of serializer concerns.</summary>
internal sealed class TransactionDetailsConverter() : ValueConverter<TransactionDetails, string>(
    details => Serialize(details), json => Deserialize(json))
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private static string Serialize(TransactionDetails details) => JsonSerializer.Serialize(details switch
    {
        TradeDetails trade => new DetailsJson("trade", Trade: new TradeJson(
            trade.InstrumentId, trade.Side.ToString(), trade.Quantity.Value, trade.Price.Amount, trade.Price.Currency.Code, trade.Fee.Amount,
            [.. trade.Allocations.Select(a => new PartJson(a.SubPortfolioId.Value, a.Quantity.Value))])),
        CashMovementDetails cash => new DetailsJson("cash", Cash: new CashJson(
            cash.Kind.ToString(), cash.Amount.Amount, cash.Amount.Currency.Code, cash.InstrumentId,
            [.. cash.Allocations.Select(a => new PartJson(a.SubPortfolioId.Value, a.Amount.Amount))])),
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
                    Money.Of(trade.Fee, currency), [.. trade.Allocations.Select(p => new Allocation(SubPortfolioId.Of(p.SubPortfolioId), Quantity.Of(p.Value)))]);
            case "cash":
                var cash = details.Cash!;
                var cashCurrency = Currency.Of(cash.Currency);
                return new CashMovementDetails(Enum.Parse<CashMovementKind>(cash.Kind), Money.Of(cash.Amount, cashCurrency),
                    [.. cash.Allocations.Select(p => new CashAllocation(SubPortfolioId.Of(p.SubPortfolioId), Money.Of(p.Value, cashCurrency)))], cash.InstrumentId);
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

    private sealed record DetailsJson(string Type, TradeJson? Trade = null, CashJson? Cash = null, TransferJson? Transfer = null, ReversalJson? Reversal = null);

    private sealed record PartJson(Guid SubPortfolioId, decimal Value);

    private sealed record TradeJson(Guid InstrumentId, string Side, decimal Quantity, decimal Price, string Currency, decimal Fee, IReadOnlyList<PartJson> Allocations);

    private sealed record CashJson(string Kind, decimal Amount, string Currency, Guid? InstrumentId, IReadOnlyList<PartJson> Allocations);

    private sealed record TransferJson(Guid From, Guid To, Guid? InstrumentId, decimal? Quantity, decimal? Amount, string? Currency);

    private sealed record ReversalJson(Guid Original, string Reason);
}
