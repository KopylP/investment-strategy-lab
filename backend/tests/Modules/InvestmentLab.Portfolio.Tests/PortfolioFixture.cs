using InvestmentLab.Portfolio.Accounts.Domain;
using InvestmentLab.Portfolio.Transactions.Application.Shared;
using InvestmentLab.Portfolio.Transactions.Domain;

namespace InvestmentLab.Portfolio.Tests;

internal sealed class PortfolioFixture
{
    public static readonly Guid Etf = Guid.CreateVersion7();
    private static readonly DateTimeOffset Now = new(2026, 10, 25, 10, 0, 0, TimeSpan.Zero);

    private int _recorded;

    public PortfolioFixture()
    {
        Account = InvestmentAccount.Open(UserId.New(), "XTB", "XTB", Currency.EUR, "TEST-POLICY", ContributionPolicy.Default,
            [Draft("A", 70), Draft("B", 30)], Now).Value;
    }

    public InvestmentAccount Account { get; }
    public SubPortfolioId A => Account.SubPortfolios[0].Id;
    public SubPortfolioId B => Account.SubPortfolios[1].Id;
    public List<Transaction> Journal { get; } = [];

    public static Money Eur(decimal amount) => Money.Of(amount, Currency.EUR);

    public static DateOnly Day(int day) => new(2026, 6, day);

    public Transaction Contribute(SubPortfolioId sub, decimal amount, int day) =>
        Add(Transaction.CashMovement(Account, CashMovementKind.Contribution, Eur(amount), [new CashAllocation(sub, Eur(amount))], null,
            Day(day), TransactionSource.Manual, NextRecordedAt()));

    public Transaction Buy(SubPortfolioId sub, decimal quantity, decimal price, int day, decimal fee = 0m) =>
        Trade(TradeSide.Buy, [new Allocation(sub, Quantity.Of(quantity))], quantity, price, day, fee);

    public Transaction Sell(SubPortfolioId sub, decimal quantity, decimal price, int day, decimal fee = 0m) =>
        Trade(TradeSide.Sell, [new Allocation(sub, Quantity.Of(quantity))], quantity, price, day, fee);

    public Transaction Trade(TradeSide side, IReadOnlyList<Allocation> allocations, decimal quantity, decimal price, int day, decimal fee = 0m) =>
        Add(Transaction.Trade(Account, side, Etf, Quantity.Of(quantity), Price.Of(price, Currency.EUR), Eur(fee), allocations,
            Day(day), TransactionSource.Manual, NextRecordedAt()));

    public Transaction Reverse(Transaction original, int day) =>
        Add(original.Reverse(Account, "typo", Day(day), TransactionSource.Manual, NextRecordedAt()));

    public Transaction Add(Result<Transaction> transaction)
    {
        Journal.Add(transaction.Value);

        return transaction.Value;
    }

    private DateTimeOffset NextRecordedAt() => Now.AddSeconds(++_recorded);

    private static SubPortfolioDraft Draft(string name, decimal percent) =>
        new(name, TargetShare.TryFromPercent(percent).Value, null, null, Guardrails.None);
}
