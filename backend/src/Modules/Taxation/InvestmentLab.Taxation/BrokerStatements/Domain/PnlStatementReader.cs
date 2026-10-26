using System.Globalization;
using System.Text.RegularExpressions;

namespace InvestmentLab.Taxation.BrokerStatements.Domain;

/// <summary>
/// Reads the sold lots and dividends of a broker's Profit and Loss Statement from its text lines by their shape, not by
/// headings (which change with the app's language): a row starts with a date — two for a sold lot (bought, sold), one
/// for a dividend — and carries an ISIN; amounts in the security's currency have its sign (US$, €, £…), PLN amounts
/// stand next to "PLN". The columns are told apart by arithmetic: proceeds − cost = gain, gross − withheld = net.
/// </summary>
public static partial class PnlStatementReader
{
    private const decimal Cent = 0.011m;

    private static readonly (string Sign, string Currency)[] Signs =
        [("US$", "USD"), ("CA$", "CAD"), ("A$", "AUD"), ("HK$", "HKD"), ("$", "USD"), ("€", "EUR"), ("£", "GBP"), ("zł", "PLN"), ("CHF", "CHF")];

    /// <param name="lines">Words of each text line, left to right; an empty line is a page break.</param>
    public static ParsedBrokerStatement Read(string broker, IReadOnlyList<IReadOnlyList<string>> lines)
    {
        var lots = new List<BrokerSaleLot>();
        var dividends = new List<BrokerDividend>();
        var warnings = new List<string>();

        foreach (var record in Records(lines))
        {
            var tokens = Tokens(record);
            var dates = tokens.TakeWhile(token => token.Date is not null).Select(token => token.Date!.Value).ToList();
            var isin = tokens.FirstOrDefault(token => token.Isin is not null)?.Isin;
            var symbol = tokens.Skip(dates.Count).FirstOrDefault(token => token.Kind == TokenKind.Word && token.Isin is null)?.Text ?? "";
            var money = tokens.Where(token => token.Kind == TokenKind.Money).ToList();
            var pln = tokens.Where(token => token.Kind == TokenKind.Pln).Select(token => token.Value).ToList();
            var label = $"{dates[0]:dd.MM.yyyy} {symbol}".Trim();

            if (isin is null)
            {
                warnings.Add($"{label}: рядок без ISIN (не цінний папір) — пропущено.");
                continue;
            }
            if (money.Select(token => token.Currency).Distinct().Count() > 1)
            {
                warnings.Add($"{label}: суми в різних валютах — пропущено.");
                continue;
            }
            var currency = money.FirstOrDefault()?.Currency ?? "PLN";
            if (currency == "PLN" && pln.Count == 0)
                pln = [.. money.Select(token => token.Value)];

            if (dates.Count >= 2)
            {
                // Sold lot: cost, proceeds, gain, fees — in the currency and again in PLN.
                var afterIsin = tokens.SkipWhile(token => token.Isin is null).Skip(1);
                var quantity = afterIsin.FirstOrDefault(token => token.Kind == TokenKind.Number)?.Value;
                if (quantity is not > 0m || money.Count < 4 || pln.Count < 4 || Math.Abs(money[1].Value - money[0].Value - money[2].Value) > 2 * Cent)
                {
                    warnings.Add($"{label}: продаж не вдалося прочитати — пропущено.");
                    continue;
                }
                lots.Add(new BrokerSaleLot(dates[0], dates[1], symbol, isin, currency, quantity.Value, money[0].Value, money[1].Value, money[3].Value, pln[0], pln[1], pln[3]));
            }
            else
            {
                // Dividend: gross, withheld abroad, net.
                if (money.Count < 3 || pln.Count < 2 || Math.Abs(money[0].Value - money[1].Value - money[2].Value) > Cent)
                {
                    warnings.Add($"{label}: дивіденд не вдалося прочитати — пропущено.");
                    continue;
                }
                dividends.Add(new BrokerDividend(dates[0], symbol, isin, currency, money[0].Value, money[1].Value, money[2].Value, pln[0], pln[1]));
            }
        }

        return new ParsedBrokerStatement(broker, lots, dividends, warnings);
    }

    /// <summary>
    /// Lines of each row: from a line that starts with a date to the next one. Lines without numbers inside a row (a
    /// wrapped name, a page heading) are skipped; a line that starts with a word and has amounts (a total) ends it.
    /// </summary>
    private static IEnumerable<List<IReadOnlyList<string>>> Records(IReadOnlyList<IReadOnlyList<string>> lines)
    {
        List<IReadOnlyList<string>>? current = null;
        foreach (var line in lines)
        {
            if (line.Count > 0 && ParseDate(line[0]) is not null)
            {
                if (current is not null)
                    yield return current;
                current = [line];
                continue;
            }
            if (current is null)
                continue;

            var tokens = Tokens([line]);
            var hasAmount = tokens.Any(token => token.Kind is TokenKind.Money or TokenKind.Pln);
            if (line.Count == 0 || tokens.Count > 0 && tokens[0].Kind == TokenKind.Word && hasAmount)
            {
                yield return current;
                current = null;
            }
            else if (tokens.Any(token => token.Kind is TokenKind.Number or TokenKind.Money or TokenKind.Pln))
                current.Add(line);
        }
        if (current is not null)
            yield return current;
    }

    private static List<Token> Tokens(IReadOnlyList<IReadOnlyList<string>> lines)
    {
        var tokens = new List<Token>();
        foreach (var line in lines)
        {
            var words = line.Where(word => !string.IsNullOrWhiteSpace(word)).Select(word => word.Trim()).ToList();
            for (var i = 0; i < words.Count; i++)
            {
                var word = words[i];
                var next = i + 1 < words.Count ? words[i + 1] : null;
                if (ParseDate(word) is { } date)
                    tokens.Add(new Token(TokenKind.Word, word, Date: date));
                else if (IsIsin(word))
                    tokens.Add(new Token(TokenKind.Word, word, Isin: word));
                else if (Money(word) is { } money)
                    tokens.Add(money.Currency == "PLN" ? new Token(TokenKind.Pln, word, money.Value) : new Token(TokenKind.Money, word, money.Value, money.Currency));
                else if (Number(word) is { } number && next is not null && IsPlnCode(next))
                {
                    tokens.Add(new Token(TokenKind.Pln, word, number));
                    i++;
                }
                else if (Number(word) is { } amount && next is not null && CurrencyCode().IsMatch(next))
                {
                    tokens.Add(new Token(TokenKind.Money, word, amount, next));
                    i++;
                }
                else if (Number(word) is { } plain)
                    tokens.Add(new Token(TokenKind.Number, word, plain));
                else
                    tokens.Add(new Token(TokenKind.Word, word));
            }
        }
        return tokens;
    }

    private static DateOnly? ParseDate(string text) =>
        DateOnly.TryParseExact(text, ["yyyy-MM-dd", "dd.MM.yyyy"], CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ? date : null;

    private static bool IsPlnCode(string text) => text is "PLN" or "zł";

    /// <summary>"US$1,234.56", "-€5.27", "12,50€", "PLN12.00" → currency and value; a bare number is not money.</summary>
    private static (string Currency, decimal Value)? Money(string text)
    {
        var negative = text.StartsWith('-') || text.StartsWith('−');
        var body = negative ? text[1..] : text;
        foreach (var (sign, currency) in Signs)
        {
            if (body.StartsWith(sign, StringComparison.Ordinal) && Number(body[sign.Length..]) is { } prefixed)
                return (currency, negative ? -prefixed : prefixed);
            if (body.EndsWith(sign, StringComparison.Ordinal) && Number(body[..^sign.Length]) is { } suffixed)
                return (currency, negative ? -suffixed : suffixed);
        }
        if (body.Length > 3 && CurrencyCode().IsMatch(body[..3]) && Number(body[3..]) is { } coded)
            return (body[..3], negative ? -coded : coded);
        return null;
    }

    /// <summary>
    /// "1,486.56", "1.486,56", "216", "-29.70", "0,5" → a number. With both marks the last one is decimal; a lone mark
    /// is decimal unless it groups thousands ("1,486" — a lone "." stays decimal: "3.784" is a rate).
    /// </summary>
    private static decimal? Number(string text)
    {
        var body = text.Replace(" ", "").Replace(" ", "").Replace('−', '-');
        if (body.Length == 0 || !NumberShape().IsMatch(body))
            return null;
        var comma = body.LastIndexOf(',');
        var dot = body.LastIndexOf('.');
        var decimalMark = (comma >= 0, dot >= 0) switch
        {
            (true, true) => comma > dot ? ',' : '.',
            (true, false) => CommaThousands().IsMatch(body) ? '\0' : ',',
            (false, true) => DotThousands().IsMatch(body) ? '\0' : '.',
            _ => '\0',
        };
        var groupMark = decimalMark == ',' ? '.' : decimalMark == '.' ? ',' : comma >= 0 ? ',' : '.';
        var normalized = body.Replace(groupMark.ToString(), "").Replace(',', '.');
        return decimal.TryParse(normalized, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var value) ? value : null;
    }

    /// <summary>Two letters, nine letters or digits and a check digit that agrees with them (ISO 6166).</summary>
    private static bool IsIsin(string text)
    {
        if (!IsinShape().IsMatch(text))
            return false;
        var digits = string.Concat(text[..^1].Select(ch => char.IsDigit(ch) ? ch.ToString() : (ch - 'A' + 10).ToString(CultureInfo.InvariantCulture)));
        var sum = 0;
        for (var i = 0; i < digits.Length; i++)
        {
            var digit = digits[digits.Length - 1 - i] - '0';
            if (i % 2 == 0)
                digit = digit * 2 is var doubled && doubled > 9 ? doubled - 9 : doubled;
            sum += digit;
        }
        return (10 - sum % 10) % 10 == text[^1] - '0';
    }

    [GeneratedRegex(@"^[A-Z]{2}[A-Z0-9]{9}[0-9]$")]
    private static partial Regex IsinShape();

    [GeneratedRegex(@"^[A-Z]{3}$")]
    private static partial Regex CurrencyCode();

    [GeneratedRegex(@"^-?[0-9][0-9.,]*$")]
    private static partial Regex NumberShape();

    [GeneratedRegex(@"^-?[0-9]{1,3}(,[0-9]{3})+$")]
    private static partial Regex CommaThousands();

    [GeneratedRegex(@"^-?[0-9]{1,3}(\.[0-9]{3}){2,}$")]
    private static partial Regex DotThousands();

    private enum TokenKind
    {
        Word,
        Number,
        Money,
        Pln,
    }

    private sealed record Token(TokenKind Kind, string Text, decimal Value = 0m, string? Currency = null, DateOnly? Date = null, string? Isin = null);
}
