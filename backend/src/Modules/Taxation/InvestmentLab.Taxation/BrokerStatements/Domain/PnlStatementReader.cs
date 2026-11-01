using System.Globalization;
using System.Text.RegularExpressions;

namespace InvestmentLab.Taxation.BrokerStatements.Domain;

public static partial class PnlStatementReader
{
    private const decimal Cent = 0.011m;
    private const char NoMark = '\0';

    private static readonly (string Sign, string Currency)[] Signs =
        [("US$", "USD"), ("CA$", "CAD"), ("A$", "AUD"), ("HK$", "HKD"), ("$", "USD"), ("€", "EUR"), ("£", "GBP"), ("zł", "PLN"), ("CHF", "CHF")];

    public static ParsedBrokerStatement Read(string broker, IReadOnlyList<IReadOnlyList<string>> lines)
    {
        var parsed = new ParsedRows();

        foreach (var record in Records(lines))
            ReadRecord(Tokens(record), parsed);

        return new ParsedBrokerStatement(broker, parsed.Lots, parsed.Dividends, parsed.Warnings);
    }

    private static void ReadRecord(List<Token> tokens, ParsedRows parsed)
    {
        var dates = tokens.TakeWhile(token => token.Date is not null).Select(token => token.Date!.Value).ToList();
        var isin = tokens.FirstOrDefault(token => token.Isin is not null)?.Isin;
        var symbol = tokens.Skip(dates.Count).FirstOrDefault(token => token.Kind == TokenKind.Word && token.Isin is null)?.Text ?? "";
        var money = tokens.Where(token => token.Kind == TokenKind.Money).ToList();
        var label = $"{dates[0]:dd.MM.yyyy} {symbol}".Trim();

        if (isin is null)
        {
            parsed.Warnings.Add(Message.Of("tax.broker.row_without_isin", new { label }));
            return;
        }

        if (money.Select(token => token.Currency).Distinct().Count() > 1)
        {
            parsed.Warnings.Add(Message.Of("tax.broker.mixed_currencies", new { label }));
            return;
        }

        if (money.Count == 0)
        {
            parsed.Warnings.Add(UnreadableRow(dates, label));
            return;
        }

        var currency = money[0].Currency!;
        var pln = PlnAmounts(tokens, money, currency);
        var row = new RecordRow(dates, symbol, isin, currency, label, money, pln);

        if (dates.Count >= 2)
            ReadSale(row, tokens, parsed);
        else
            ReadDividend(row, parsed);
    }

    private static string UnreadableRow(List<DateOnly> dates, string label) =>
        Message.Of(dates.Count >= 2 ? "tax.broker.unreadable_sale" : "tax.broker.unreadable_dividend", new { label });

    private static List<decimal> PlnAmounts(List<Token> tokens, List<Token> money, string currency)
    {
        var pln = tokens.Where(token => token.Kind == TokenKind.Pln).Select(token => token.Value).ToList();

        return currency == "PLN" && pln.Count == 0 ? [.. money.Select(token => token.Value)] : pln;
    }

    private static void ReadSale(RecordRow row, List<Token> tokens, ParsedRows parsed)
    {
        var afterIsin = tokens.SkipWhile(token => token.Isin is null).Skip(1);
        var quantity = afterIsin.FirstOrDefault(token => token.Kind == TokenKind.Number)?.Value;
        var (money, pln) = (row.Money, row.Pln);

        if (quantity is not > 0m || money.Count < 4 || pln.Count < 4 || Math.Abs(money[1].Value - money[0].Value - money[2].Value) > 2 * Cent)
        {
            parsed.Warnings.Add(Message.Of("tax.broker.unreadable_sale", new { label = row.Label }));
            return;
        }

        parsed.Lots.Add(new BrokerSaleLot(
            row.Dates[0],
            row.Dates[1],
            row.Symbol,
            row.Isin,
            row.Currency,
            quantity.Value,
            money[0].Value,
            money[1].Value,
            money[3].Value,
            pln[0],
            pln[1],
            pln[3]));
    }

    private static void ReadDividend(RecordRow row, ParsedRows parsed)
    {
        var (money, pln) = (row.Money, row.Pln);

        if (money.Count < 3 || pln.Count < 2 || Math.Abs(money[0].Value - money[1].Value - money[2].Value) > Cent)
        {
            parsed.Warnings.Add(Message.Of("tax.broker.unreadable_dividend", new { label = row.Label }));
            return;
        }

        parsed.Dividends.Add(new BrokerDividend(
            row.Dates[0],
            row.Symbol,
            row.Isin,
            row.Currency,
            money[0].Value,
            money[1].Value,
            money[2].Value,
            pln[0],
            pln[1]));
    }

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

            if (line.Count == 0 || StartsNonSecurityRow(tokens))
            {
                yield return current;
                current = null;
            }
            else if (tokens.Any(IsAmountOrNumber))
                current.Add(line);
        }

        if (current is not null)
            yield return current;
    }

    private static bool StartsNonSecurityRow(List<Token> tokens)
    {
        var hasAmount = tokens.Any(token => token.Kind is TokenKind.Money or TokenKind.Pln);

        return tokens.Count > 0 && tokens[0].Kind == TokenKind.Word && hasAmount;
    }

    private static bool IsAmountOrNumber(Token token) => token.Kind is TokenKind.Number or TokenKind.Money or TokenKind.Pln;

    private static List<Token> Tokens(IReadOnlyList<IReadOnlyList<string>> lines)
    {
        var tokens = new List<Token>();

        foreach (var line in lines)
        {
            var words = line.Where(word => !string.IsNullOrWhiteSpace(word)).Select(word => word.Trim()).ToList();

            for (var i = 0; i < words.Count; i++)
            {
                var next = i + 1 < words.Count ? words[i + 1] : null;
                var (token, usesNextWord) = TokenOf(words[i], next);

                tokens.Add(token);

                if (usesNextWord)
                    i++;
            }
        }

        return tokens;
    }

    private static (Token Token, bool UsesNextWord) TokenOf(string word, string? next)
    {
        if (ParseDate(word) is { } date)
            return (new Token(TokenKind.Word, word, Date: date), false);

        if (IsIsin(word))
            return (new Token(TokenKind.Word, word, Isin: word), false);

        if (Money(word) is { } money)
            return (MoneyToken(word, money), false);

        if (Number(word) is not { } number)
            return (new Token(TokenKind.Word, word), false);

        if (next is not null && IsPlnCode(next))
            return (new Token(TokenKind.Pln, word, number), true);

        if (next is not null && CurrencyCode().IsMatch(next))
            return (new Token(TokenKind.Money, word, number, next), true);

        return (new Token(TokenKind.Number, word, number), false);
    }

    private static Token MoneyToken(string word, (string Currency, decimal Value) money) =>
        money.Currency == "PLN"
            ? new Token(TokenKind.Pln, word, money.Value)
            : new Token(TokenKind.Money, word, money.Value, money.Currency);

    private static DateOnly? ParseDate(string text) =>
        DateOnly.TryParseExact(text, ["yyyy-MM-dd", "dd.MM.yyyy"], CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ? date : null;

    private static bool IsPlnCode(string text) => text is "PLN" or "zł";

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

    private static decimal? Number(string text)
    {
        var body = WithoutSpaces(text).Replace('−', '-');
        if (body.Length == 0 || !NumberShape().IsMatch(body))
            return null;

        var groupMark = GroupMarkOf(body);
        var normalized = body.Replace(groupMark.ToString(), "").Replace(',', '.');

        return decimal.TryParse(
            normalized,
            NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture,
            out var value) ? value : null;
    }

    private static string WithoutSpaces(string text) => string.Concat(text.Where(character => !char.IsWhiteSpace(character)));

    private static char GroupMarkOf(string body)
    {
        var comma = body.LastIndexOf(',');
        var dot = body.LastIndexOf('.');

        var decimalMark = (comma >= 0, dot >= 0) switch
        {
            (true, true) => comma > dot ? ',' : '.',
            (true, false) => CommaThousands().IsMatch(body) ? NoMark : ',',
            (false, true) => DotThousands().IsMatch(body) ? NoMark : '.',
            _ => NoMark,
        };

        return decimalMark switch
        {
            ',' => '.',
            '.' => ',',
            _ => comma >= 0 ? ',' : '.',
        };
    }

    private static bool IsIsin(string text)
    {
        if (!IsinShape().IsMatch(text))
            return false;

        var checkDigit = text[^1] - '0';
        var digits = string.Concat(text[..^1].Select(IsinDigits));

        return (10 - LuhnSum(digits) % 10) % 10 == checkDigit;
    }

    private static string IsinDigits(char ch) => char.IsDigit(ch) ? ch.ToString() : (ch - 'A' + 10).ToString(CultureInfo.InvariantCulture);

    private static int LuhnSum(string digits)
    {
        var sum = 0;

        for (var i = 0; i < digits.Length; i++)
        {
            var digit = digits[digits.Length - 1 - i] - '0';

            if (i % 2 == 0)
                digit = digit * 2 is var doubled && doubled > 9 ? doubled - 9 : doubled;

            sum += digit;
        }

        return sum;
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

    private sealed record RecordRow(
        List<DateOnly> Dates, string Symbol, string Isin, string Currency, string Label, List<Token> Money, List<decimal> Pln);

    private sealed class ParsedRows
    {
        public List<BrokerSaleLot> Lots { get; } = [];
        public List<BrokerDividend> Dividends { get; } = [];
        public List<string> Warnings { get; } = [];
    }
}
