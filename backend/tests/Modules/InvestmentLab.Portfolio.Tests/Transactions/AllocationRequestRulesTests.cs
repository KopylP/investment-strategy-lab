using InvestmentLab.Portfolio.Transactions.Domain;

namespace InvestmentLab.Portfolio.Tests.Transactions;

public sealed class AllocationRequestRulesTests
{
    private static readonly Guid Sub = Guid.NewGuid();
    private static readonly Currency Eur = Currency.Of("EUR");

    [Fact]
    public void Quantities_given_by_hand_become_allocations()
    {
        var allocation = Allocation.TryAll([(Sub, 2.5m)]).Value.ShouldHaveSingleItem();

        allocation.Quantity.Value.ShouldBe(2.5m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void A_quantity_that_is_not_positive_is_refused(int quantity)
    {
        Allocation.TryAll([(Sub, quantity)]).Error.ShouldBe(TransactionErrors.QuantityNotPositive());
    }

    [Fact]
    public void An_allocation_without_a_sub_portfolio_is_refused()
    {
        Allocation.TryAll([(Guid.Empty, 1m)]).Error.ShouldBe(TransactionErrors.UnknownSubPortfolio(Guid.Empty));
        CashAllocation.TryAll([(Guid.Empty, 1m)], Eur).Error.ShouldBe(TransactionErrors.UnknownSubPortfolio(Guid.Empty));
    }

    [Fact]
    public void Amounts_given_by_hand_are_in_the_currency_of_the_account()
    {
        var allocation = CashAllocation.TryAll([(Sub, 10m)], Eur).Value.ShouldHaveSingleItem();

        allocation.Amount.ShouldBe(Money.Of(10m, Eur));
    }

    [Theory]
    [InlineData("buy", true)]
    [InlineData("Sell", true)]
    [InlineData("1", false)]
    [InlineData("Hold", false)]
    [InlineData(null, false)]
    public void A_kind_is_read_by_its_name_only(string? text, bool isKnown)
    {
        EnumText.TryParse<TradeSide>(text, "code").IsSuccess.ShouldBe(isKnown);
    }
}
