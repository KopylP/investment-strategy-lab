namespace InvestmentLab.SharedKernel;

public static class MoneyExtensions
{
    extension(IEnumerable<Money> amounts)
    {
        public Money Total(Currency currency) => amounts.Aggregate(Money.Zero(currency), (total, amount) => total + amount);
    }
}
