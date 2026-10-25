namespace InvestmentLab.SharedKernel;

/// <summary>ISO 4217 currency code.</summary>
public sealed record Currency
{
    public static readonly Currency EUR = new("EUR");
    public static readonly Currency USD = new("USD");
    public static readonly Currency PLN = new("PLN");

    public string Code { get; }

    private Currency(string code) => Code = code;

    public static Currency Of(string code) =>
        TryOf(code) is { IsSuccess: true } result ? result.Value : throw new ArgumentException($"'{code}' is not an ISO 4217 code.", nameof(code));

    public static Result<Currency> TryOf(string? code)
    {
        var normalized = code?.Trim().ToUpperInvariant();
        if (normalized is not { Length: 3 } || !normalized.All(char.IsAsciiLetterUpper))
            return Error.Validation("currency.invalid", $"'{code}' is not an ISO 4217 currency code.");

        return new Currency(normalized);
    }

    public override string ToString() => Code;
}
