using InvestmentLab.Performance.Valuation.Domain;

namespace InvestmentLab.Performance.Tests;

public sealed class JournalMovementsTests
{
    private static readonly DateOnly Day = new(2026, 3, 2);
    private static readonly Guid Sub = Guid.NewGuid(), Other = Guid.NewGuid(), Etf = Guid.NewGuid();

    private sealed record Entry(IJournalTrade? Trade = null, IJournalCash? Cash = null, IJournalTransfer? Transfer = null) : IJournalEntry
    {
        public Guid TransactionId { get; } = Guid.NewGuid();
        public DateOnly OccurredOn => Day;
    }

    private sealed record Trade(string Side, decimal Price, decimal Fee, IReadOnlyList<IJournalQuantity> Allocations, decimal? SettledAmount = null, string? SettledCurrency = null) : IJournalTrade
    {
        public Guid InstrumentId => Etf;
    }

    private sealed record Shares(Guid SubPortfolioId, decimal Quantity) : IJournalQuantity;

    private sealed record Cash(string Kind, IReadOnlyList<IJournalAmount> Allocations, decimal? SettledAmount = null, string? SettledCurrency = null) : IJournalCash;

    private sealed record Money(Guid SubPortfolioId, decimal Amount) : IJournalAmount;

    private sealed record Transfer(Guid? InstrumentId, decimal? Quantity, decimal? Amount) : IJournalTransfer
    {
        public Guid FromSubPortfolioId => Sub;
        public Guid ToSubPortfolioId => Other;
    }

    private static IReadOnlyList<Movement> MovementsOf(params IJournalEntry[] entries) =>
        JournalMovements.Of(new AccountJournal(UserId.Of(Guid.NewGuid()), "EUR", entries, []));

    [Fact]
    public void A_buy_takes_price_and_fee_out_of_cash_and_counts_the_fee_in_the_unit_cost()
    {
        var movement = MovementsOf(new Entry(new Trade("Buy", 10m, 2m, [new Shares(Sub, 4m)]))).ShouldHaveSingleItem();

        (movement.QuantityDelta, movement.CashDelta, movement.ExternalFlow, movement.UnitCost).ShouldBe((4m, -42m, 0m, 10.5m));
    }

    [Fact]
    public void A_sell_brings_the_price_in_and_still_pays_the_fee()
    {
        var movement = MovementsOf(new Entry(new Trade("Sell", 10m, 2m, [new Shares(Sub, 4m)]))).ShouldHaveSingleItem();

        (movement.QuantityDelta, movement.CashDelta).ShouldBe((-4m, 38m));
    }

    [Theory]
    [InlineData("Buy", -279.56)]
    [InlineData("Sell", 279.56)]
    public void A_trade_moves_cash_in_whole_cents_as_the_broker_charges_it(string side, decimal cash)
    {
        var movement = MovementsOf(new Entry(new Trade(side, 95.24m, 0m, [new Shares(Sub, 2.935354m)]))).ShouldHaveSingleItem();

        movement.CashDelta.ShouldBe(cash);
    }

    [Fact]
    public void A_trade_settled_in_another_currency_moves_the_settled_amount_in_shares_of_quantity()
    {
        var trade = new Trade("Buy", 10m, 0m, [new Shares(Sub, 1m), new Shares(Other, 3m)], SettledAmount: 80m, SettledCurrency: "USD");

        var movements = MovementsOf(new Entry(trade));

        (movements[0].CashDelta, movements[0].CashCurrency).ShouldBe((-20m, "USD"));
        (movements[1].CashDelta, movements[1].CashCurrency).ShouldBe((-60m, "USD"));
    }

    [Theory]
    [InlineData("Contribution", 100, 100)]
    [InlineData("Withdrawal", -100, -100)]
    [InlineData("Dividend", 100, 0)]
    [InlineData("Fee", -100, 0)]
    public void Cash_of_each_kind_moves_cash_and_only_outside_money_counts_as_a_flow(string kind, int cash, int flow)
    {
        var movement = MovementsOf(new Entry(Cash: new Cash(kind, [new Money(Sub, 100m)]))).ShouldHaveSingleItem();

        (movement.CashDelta, movement.ExternalFlow).ShouldBe(((decimal)cash, (decimal)flow));
    }

    [Fact]
    public void A_position_transfer_moves_the_quantity_between_sub_portfolios_without_cash()
    {
        var movements = MovementsOf(new Entry(Transfer: new Transfer(Etf, 5m, null)));

        (movements[0].SubPortfolioId, movements[0].TransferQuantity).ShouldBe((Sub, -5m));
        (movements[1].SubPortfolioId, movements[1].TransferQuantity).ShouldBe((Other, 5m));
        movements.ShouldAllBe(movement => movement.CashDelta == 0m);
    }

    [Fact]
    public void A_cash_transfer_is_a_flow_out_of_one_sub_portfolio_and_into_the_other()
    {
        var movements = MovementsOf(new Entry(Transfer: new Transfer(null, null, 50m)));

        (movements[0].CashDelta, movements[0].ExternalFlow).ShouldBe((-50m, -50m));
        (movements[1].CashDelta, movements[1].ExternalFlow).ShouldBe((50m, 50m));
    }

    [Fact]
    public void A_reversed_transaction_makes_no_movement()
    {
        var entry = new Entry(Cash: new Cash("Contribution", [new Money(Sub, 100m)]));

        var journal = new AccountJournal(UserId.Of(Guid.NewGuid()), "EUR", [entry], [entry.TransactionId]);

        JournalMovements.Of(journal).ShouldBeEmpty();
    }

    [Fact]
    public void A_fee_is_shared_between_allocations_by_quantity_in_cash_and_in_unit_cost()
    {
        var movements = MovementsOf(new Entry(new Trade("Buy", 10m, 4m, [new Shares(Sub, 1m), new Shares(Other, 3m)])));

        (movements[0].CashDelta, movements[0].UnitCost).ShouldBe((-11m, 11m));
        (movements[1].CashDelta, movements[1].UnitCost).ShouldBe((-33m, 11m));
    }

    [Fact]
    public void A_sell_settled_in_another_currency_brings_the_settled_amount_in()
    {
        var movement = MovementsOf(new Entry(new Trade("Sell", 10m, 0m, [new Shares(Sub, 4m)], SettledAmount: 80m, SettledCurrency: "USD"))).ShouldHaveSingleItem();

        (movement.CashDelta, movement.CashCurrency).ShouldBe((80m, "USD"));
    }

    [Fact]
    public void A_dividend_settled_in_another_currency_is_split_by_its_allocations()
    {
        var cash = new Cash("Dividend", [new Money(Sub, 30m), new Money(Other, 10m)], SettledAmount: 80m, SettledCurrency: "USD");

        var movements = MovementsOf(new Entry(Cash: cash));

        (movements[0].CashDelta, movements[0].CashCurrency).ShouldBe((60m, "USD"));
        (movements[1].CashDelta, movements[1].CashCurrency).ShouldBe((20m, "USD"));
    }

    [Fact]
    public void A_withdrawal_settled_in_another_currency_takes_the_settled_amount_out_and_the_flow_stays_in_the_account_currency()
    {
        var cash = new Cash("Withdrawal", [new Money(Sub, 100m)], SettledAmount: 90m, SettledCurrency: "USD");

        var movement = MovementsOf(new Entry(Cash: cash)).ShouldHaveSingleItem();

        (movement.CashDelta, movement.ExternalFlow, movement.CashCurrency).ShouldBe((-90m, -100m, "USD"));
    }
}
