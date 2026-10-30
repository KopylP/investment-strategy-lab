using InvestmentLab.Portfolio.Holdings.Domain;
using InvestmentLab.Portfolio.Transactions.Domain;

namespace InvestmentLab.Portfolio.Tests.Holdings;

public sealed class HoldingsProjectorTests
{
    private readonly PortfolioFixture _p = new();

    private AccountHoldings Project() => HoldingsProjector.Project(_p.Account, _p.Journal).Value.Holdings;

    [Fact]
    public void A_buy_turns_cash_into_a_lot_whose_cost_includes_the_fee()
    {
        _p.Contribute(_p.A, 1000m, 1);
        _p.Buy(_p.A, 10m, 50m, 2, fee: 2m);

        var a = Project().For(_p.A)!;
        a.Cash.Amount.ShouldBe(498m);
        var line = a.Lines.ShouldHaveSingleItem();
        line.Quantity.Value.ShouldBe(10m);
        line.CostBasis.ShouldBe(502m);
        line.AverageCost.ShouldBe(50.2m);
    }

    [Fact]
    public void A_sale_takes_the_oldest_lots_first()
    {
        _p.Contribute(_p.A, 1000m, 1);
        _p.Buy(_p.A, 5m, 10m, 2);
        _p.Buy(_p.A, 5m, 20m, 3);
        _p.Sell(_p.A, 7m, 30m, 4, fee: 1m);

        var a = Project().For(_p.A)!;
        a.Cash.Amount.ShouldBe(1000m - 50m - 100m + 210m - 1m);
        var lot = a.Lines.ShouldHaveSingleItem().Lots.ShouldHaveSingleItem();
        lot.Quantity.Value.ShouldBe(3m);
        lot.UnitCost.ShouldBe(20m);
        lot.OpenedOn.ShouldBe(PortfolioFixture.Day(3));
    }

    [Fact]
    public void A_sub_portfolio_cannot_sell_units_another_one_holds()
    {
        _p.Buy(_p.A, 5m, 10m, 2);
        _p.Sell(_p.B, 1m, 10m, 3);

        HoldingsProjector.Project(_p.Account, _p.Journal).Error.Code.ShouldBe("transaction.not_enough_units");
    }

    [Fact]
    public void A_backdated_sale_before_the_buy_is_refused()
    {
        _p.Buy(_p.A, 5m, 10m, 10);
        _p.Sell(_p.A, 5m, 12m, 5);

        HoldingsProjector.Project(_p.Account, _p.Journal).Error.Code.ShouldBe("transaction.not_enough_units");
    }

    [Fact]
    public void A_reversal_restores_the_holdings_before_the_entry()
    {
        _p.Contribute(_p.A, 1000m, 1);
        _p.Buy(_p.A, 10m, 50m, 2);
        var before = Project();

        var mistake = _p.Buy(_p.A, 3m, 40m, 3);
        _p.Reverse(mistake, 4);

        var after = Project();
        after.For(_p.A)!.Cash.ShouldBe(before.For(_p.A)!.Cash);
        after.For(_p.A)!.Lines.ShouldHaveSingleItem().Quantity.Value.ShouldBe(10m);
    }

    [Fact]
    public void A_split_trade_shares_the_fee_like_the_quantity()
    {
        _p.Trade(TradeSide.Buy, [new Allocation(_p.A, Quantity.Of(7m)), new Allocation(_p.B, Quantity.Of(3m))], 10m, 10m, 2, fee: 1m);

        var holdings = Project();
        holdings.For(_p.A)!.Cash.Amount.ShouldBe(-70.7m);
        holdings.For(_p.B)!.Cash.Amount.ShouldBe(-30.3m);
    }

    [Fact]
    public void Buying_without_the_contribution_warns_instead_of_failing()
    {
        _p.Buy(_p.A, 1m, 10m, 2);

        var projected = HoldingsProjector.Project(_p.Account, _p.Journal).Value;
        projected.Warnings.ShouldHaveSingleItem().ShouldContain("'A'");
    }

    [Fact]
    public void An_internal_transfer_moves_lots_with_their_cost_and_date()
    {
        _p.Buy(_p.A, 4m, 10m, 2);
        _p.Add(Transaction.InternalTransfer(_p.Account, _p.A, _p.B, PortfolioFixture.Etf, Quantity.Of(3m), null,
            PortfolioFixture.Day(5), TransactionSource.Manual, DateTimeOffset.UtcNow));

        var holdings = Project();
        holdings.UnitsOf(_p.A, PortfolioFixture.Etf).Value.ShouldBe(1m);
        var moved = holdings.For(_p.B)!.Lines.ShouldHaveSingleItem().Lots.ShouldHaveSingleItem();
        (moved.Quantity.Value, moved.UnitCost, moved.OpenedOn).ShouldBe((3m, 10m, PortfolioFixture.Day(2)));
    }

    [Fact]
    public void Holdings_as_of_a_date_ignore_later_entries()
    {
        _p.Contribute(_p.A, 100m, 1);
        _p.Contribute(_p.A, 50m, 20);

        var holdings = HoldingsProjector.Project(_p.Account, _p.Journal, PortfolioFixture.Day(10)).Value.Holdings;
        holdings.For(_p.A)!.Cash.Amount.ShouldBe(100m);
    }

    [Fact]
    public void Entries_settled_in_dollars_move_the_dollar_balance_and_leave_euro_cash_alone()
    {
        // The Revolut shape: USD top-up → buy from it → sell into it → withdraw part. Amounts in EUR are that day's rate.
        static Money Usd(decimal amount) => Money.Of(amount, Currency.USD);
        var account = _p.Account;
        _p.Add(Transaction.CashMovement(account, CashMovementKind.Contribution, PortfolioFixture.Eur(69.57m), [new CashAllocation(_p.A, PortfolioFixture.Eur(69.57m))],
            null, PortfolioFixture.Day(1), TransactionSource.Manual, DateTimeOffset.UnixEpoch.AddDays(1), settlement: Usd(80m)));
        _p.Add(Transaction.Trade(account, TradeSide.Buy, PortfolioFixture.Etf, Quantity.Of(0.15m), Price.Of(463.8m, Currency.EUR), PortfolioFixture.Eur(0m),
            [new Allocation(_p.A, Quantity.Of(0.15m))], PortfolioFixture.Day(1), TransactionSource.Manual, DateTimeOffset.UnixEpoch.AddDays(2), settlement: Usd(80m)));
        _p.Add(Transaction.Trade(account, TradeSide.Sell, PortfolioFixture.Etf, Quantity.Of(0.15m), Price.Of(458m, Currency.EUR), PortfolioFixture.Eur(0m),
            [new Allocation(_p.A, Quantity.Of(0.15m))], PortfolioFixture.Day(20), TransactionSource.Manual, DateTimeOffset.UnixEpoch.AddDays(3), settlement: Usd(80.04m)));
        _p.Add(Transaction.CashMovement(account, CashMovementKind.Withdrawal, PortfolioFixture.Eur(68.1m), [new CashAllocation(_p.A, PortfolioFixture.Eur(68.1m))],
            null, PortfolioFixture.Day(25), TransactionSource.Manual, DateTimeOffset.UnixEpoch.AddDays(4), settlement: Usd(79.74m)));

        var a = Project().For(_p.A)!;
        a.Cash.Amount.ShouldBe(0m);                                   // not the 1.47 € a single-currency replay would leave
        a.ForeignCash.ShouldHaveSingleItem().ShouldBe(Money.Of(0.30m, Currency.USD));
        a.Lines.ShouldBeEmpty();
    }

    [Fact]
    public void A_settlement_in_the_account_currency_is_refused()
    {
        Transaction.CashMovement(_p.Account, CashMovementKind.Contribution, PortfolioFixture.Eur(10m), [new CashAllocation(_p.A, PortfolioFixture.Eur(10m))],
            null, PortfolioFixture.Day(1), TransactionSource.Manual, DateTimeOffset.UnixEpoch, settlement: PortfolioFixture.Eur(10m))
            .Error.Code.ShouldBe("transaction.settlement_in_account_currency");
    }
}
