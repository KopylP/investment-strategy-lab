using System.Text;

namespace InvestmentLab.SharedKernel;

public sealed record Isin
{
    public string Value { get; }

    private Isin(string value) => Value = value;

    public static Isin Of(string value) =>
        TryOf(value) is { IsSuccess: true } result ? result.Value : throw new ArgumentException($"'{value}' is not a valid ISIN.", nameof(value));

    public static Result<Isin> TryOf(string? value)
    {
        var normalized = value?.Trim().ToUpperInvariant() ?? "";
        if (normalized.Length != 12
            || !char.IsAsciiLetterUpper(normalized[0]) || !char.IsAsciiLetterUpper(normalized[1])
            || !normalized[2..11].All(char.IsAsciiLetterOrDigit)
            || !char.IsAsciiDigit(normalized[11])
            || !HasValidCheckDigit(normalized))
        {
            return Error.Validation("isin.invalid", $"'{value}' is not a valid ISIN.");
        }

        return new Isin(normalized);
    }

    public override string ToString() => Value;

    private static bool HasValidCheckDigit(string isin)
    {
        var digits = new StringBuilder();

        foreach (var c in isin)
            digits.Append(char.IsAsciiDigit(c) ? (c - '0').ToString() : (c - 'A' + 10).ToString());

        var sum = 0;
        var doubleIt = false;

        for (var i = digits.Length - 1; i >= 0; i--)
        {
            var digit = digits[i] - '0';

            if (doubleIt)
            {
                digit *= 2;

                if (digit > 9)
                    digit -= 9;
            }

            sum += digit;
            doubleIt = !doubleIt;
        }

        return sum % 10 == 0;
    }
}
