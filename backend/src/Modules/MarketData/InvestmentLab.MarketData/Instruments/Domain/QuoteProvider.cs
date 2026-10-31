namespace InvestmentLab.MarketData.Instruments.Domain;

public sealed record QuoteProvider
{
    public static readonly QuoteProvider YFinance = new("yfinance");

    public string Code { get; }

    private QuoteProvider(string code) => Code = code;

    public static Result<QuoteProvider> TryOf(string? code)
    {
        var normalized = code?.Trim().ToLowerInvariant();

        return string.IsNullOrEmpty(normalized) || !normalized.All(char.IsAsciiLetterOrDigit)
            ? Error.Validation("quote_provider.invalid", $"'{code}' is not a quote provider code.")
            : new QuoteProvider(normalized);
    }

    public static QuoteProvider Of(string code) =>
        TryOf(code) is { IsSuccess: true } result ? result.Value : throw new ArgumentException($"'{code}' is not a quote provider code.", nameof(code));

    public override string ToString() => Code;
}
