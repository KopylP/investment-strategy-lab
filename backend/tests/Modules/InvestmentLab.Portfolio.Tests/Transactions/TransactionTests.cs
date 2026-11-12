using InvestmentLab.Portfolio.Accounts.Domain;
using InvestmentLab.Portfolio.Transactions.Domain;

namespace InvestmentLab.Portfolio.Tests.Transactions;

public sealed class TransactionTests
{
    private readonly PortfolioFixture _p = new();

    private Result<Transaction> Trade(IReadOnlyList<Allocation> allocations, decimal quantity, Currency? currency = null) =>
        Transaction.Trade(_p.Account, TradeSide.Buy, PortfolioFixture.Etf, Quantity.Of(quantity), Price.Of(10m, currency ?? Currency.EUR),
            Money.Zero(currency ?? Currency.EUR), allocations, PortfolioFixture.Day(1), TransactionSource.Manual, DateTimeOffset.UtcNow);

    [Fact]
    public void Allocations_must_add_up_to_the_traded_quantity() =>
        Trade([new Allocation(_p.A, Quantity.Of(6m)), new Allocation(_p.B, Quantity.Of(3m))], 10m)
            .Error.Code.ShouldBe("transaction.allocations_do_not_add_up");

    [Fact]
    public void A_trade_is_kept_in_the_account_currency() =>
        Trade([new Allocation(_p.A, Quantity.Of(1m))], 1m, Currency.USD).Error.Code.ShouldBe("transaction.currency_mismatch");

    [Fact]
    public void A_sub_portfolio_of_another_account_is_refused() =>
        Trade([new Allocation(SubPortfolioId.New(), Quantity.Of(1m))], 1m).Error.Code.ShouldBe("transaction.sub_portfolio_unknown");

    [Fact]
    public void A_reversal_cannot_be_reversed()
    {
        var reversal = _p.Reverse(_p.Contribute(_p.A, 10m, 1), 2);

        reversal.ReversalOf.ShouldNotBeNull();
        reversal.Reverse(_p.Account, "again", PortfolioFixture.Day(3), TransactionSource.Manual, DateTimeOffset.UtcNow)
            .Error.Code.ShouldBe("transaction.reverse_reversal");
    }

    [Fact]
    public void Allocator_splits_by_weight_and_gives_the_rounding_rest_to_the_largest()
    {
        var parts = TradeAllocator.Split(Money.Of(100m, Currency.EUR), [(_p.A, 1m), (_p.B, 2m)]);

        parts.Select(part => part.Amount.Amount).ShouldBe([33.33m, 66.67m]);
    }

    [Fact]
    public void Allocator_fee_split_follows_the_quantity_and_keeps_the_order()
    {
        var fees = TradeAllocator.SplitFee(Money.Of(1m, Currency.EUR), [new Allocation(_p.A, Quantity.Of(1m)), new Allocation(_p.B, Quantity.Of(2m))]);

        fees.Select(fee => fee.Amount).ShouldBe([0.33m, 0.67m]);
    }
}
