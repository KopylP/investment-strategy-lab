namespace InvestmentLab.Rebalancing.Planning.Domain;

public static class AccountCash
{
    private const int CashDigits = 2;

    public static decimal Of(decimal cash, IEnumerable<IForeignCash> foreign, IReadOnlyDictionary<string, decimal?> rates) =>
        cash + foreign.Sum(balance => InAccountCurrency(balance, rates));

    private static decimal InAccountCurrency(IForeignCash balance, IReadOnlyDictionary<string, decimal?> rates) =>
        rates[balance.Currency] is { } factor ? decimal.Round(balance.Amount * factor, CashDigits) : 0m;
}
