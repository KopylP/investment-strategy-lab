using System.Globalization;

namespace InvestmentLab.SharedKernel;

public sealed record Money
{
    public decimal Amount { get; }
    public Currency Currency { get; }

    private Money(decimal amount, Currency currency) => (Amount, Currency) = (amount, currency);

    public static Money Of(decimal amount, Currency currency) =>
        new(decimal.Round(amount, 2, MidpointRounding.ToEven), currency);

    public static Money Zero(Currency currency) => new(0m, currency);

    public bool IsZero => Amount == 0m;
    public bool IsNegative => Amount < 0m;

    public Money Add(Money other) => Of(Amount + EnsureSameCurrency(other).Amount, Currency);
    public Money Subtract(Money other) => Of(Amount - EnsureSameCurrency(other).Amount, Currency);
    public Money Multiply(decimal factor) => Of(Amount * factor, Currency);
    public Money Negate() => new(-Amount, Currency);

    public static Money operator +(Money left, Money right) => left.Add(right);
    public static Money operator -(Money left, Money right) => left.Subtract(right);

    public override string ToString() => $"{Amount.ToString("0.00", CultureInfo.InvariantCulture)} {Currency}";

    private Money EnsureSameCurrency(Money other) =>
        other.Currency == Currency ? other : throw new CurrencyMismatchException(Currency, other.Currency);
}
