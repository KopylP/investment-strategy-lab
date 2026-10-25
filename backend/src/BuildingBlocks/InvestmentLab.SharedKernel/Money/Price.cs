namespace InvestmentLab.SharedKernel;

/// <summary>Price of one unit. Keeps quote precision; money totals are rounded to cents.</summary>
public sealed record Price
{
    public decimal Amount { get; }
    public Currency Currency { get; }

    private Price(decimal amount, Currency currency) => (Amount, Currency) = (amount, currency);

    public static Price Of(decimal amount, Currency currency) =>
        amount > 0m ? new(amount, currency) : throw new ArgumentOutOfRangeException(nameof(amount), amount, "Price must be positive.");

    public static Result<Price> TryOf(decimal amount, Currency currency) =>
        amount > 0m ? new Price(amount, currency) : Error.Validation("price.not_positive", $"Price {amount} must be positive.");

    public Money TotalFor(Quantity quantity) => Money.Of(Amount * quantity.Value, Currency);

    public override string ToString() => $"{Amount.ToString(System.Globalization.CultureInfo.InvariantCulture)} {Currency}";
}
