using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using InvestmentLab.Portfolio.Transactions.Domain;

namespace InvestmentLab.Portfolio.Transactions.Infrastructure;

internal sealed class RevolutStatementParser : IStatementParser
{
    private const char ByteOrderMark = '\uFEFF';
    private const string NonBreakingSpace = "\u00A0";
    private const int IdentityLength = 20;

    private static readonly string[] Required = ["Date", "Ticker", "Type", "Quantity", "Price per share", "Total Amount", "Currency"];

    public string Broker => "Revolut";

    public Result<ParsedStatement> Parse(string content)
    {
        var lines = NonEmptyLines(content);
        if (lines.Count == 0)
            return StatementImportErrors.UnknownFormat("the file is empty");

        var header = Fields(lines[0].TrimStart(ByteOrderMark));
        var missing = Required.Where(column => !header.Contains(column, StringComparer.OrdinalIgnoreCase)).ToList();
        if (missing.Count > 0)
            return StatementImportErrors.UnknownFormat($"columns missing: {string.Join(", ", missing)}");

        var rows = new List<StatementRow>();
        var warnings = new List<string>();
        var occurrences = new Dictionary<string, int>();

        for (var index = 1; index < lines.Count; index++)
        {
            var line = new CsvLine(header, Fields(lines[index]));
            var lineNumber = index + 1;

            if (!TryReadDate(line, out var occurredAt))
            {
                warnings.Add($"Рядок {lineNumber}: не вдалося прочитати дату «{line["Date"]}» — пропущено.");
                continue;
            }

            rows.Add(ReadRow(line, lineNumber, occurredAt, occurrences));
        }

        return new ParsedStatement(Broker, rows, warnings);
    }

    private static List<string> NonEmptyLines(string content) =>
        [.. content.Replace("\r\n", "\n").Split('\n').Where(line => !string.IsNullOrWhiteSpace(line))];

    private static bool TryReadDate(CsvLine line, out DateTimeOffset occurredAt) =>
        DateTimeOffset.TryParse(line["Date"], CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out occurredAt);

    private static StatementRow ReadRow(CsvLine line, int lineNumber, DateTimeOffset occurredAt, Dictionary<string, int> occurrences)
    {
        var type = line["Type"];
        var ticker = line["Ticker"] is { Length: > 0 } symbol ? symbol.ToUpperInvariant() : null;
        var (amountCurrency, amount) = Money(line["Total Amount"]);
        var (_, price) = Money(line["Price per share"]);
        var quantity = Number(line["Quantity"]);
        var currency = line["Currency"] is { Length: 3 } code ? code.ToUpperInvariant() : amountCurrency ?? "";
        var totalAmount = Math.Abs(amount ?? 0m);

        var kind = KindOf(type);
        if (IsTrade(kind) && (quantity is not > 0m || ticker is null))
            kind = StatementRowKind.Unsupported;

        if (IsTrade(kind) && price is not > 0m)
            price = totalAmount / quantity!.Value;

        var identity = IdentityOf($"{line["Date"]}|{type}|{ticker}", occurrences);

        return new StatementRow(lineNumber, occurredAt, kind, type, ticker, quantity, price, totalAmount, currency, identity);
    }

    private static StatementRowKind KindOf(string type) => type.ToUpperInvariant() switch
    {
        var buy when buy.StartsWith("BUY", StringComparison.Ordinal) => StatementRowKind.Buy,
        var sell when sell.StartsWith("SELL", StringComparison.Ordinal) => StatementRowKind.Sell,
        "CASH TOP-UP" => StatementRowKind.Contribution,
        "CASH WITHDRAWAL" => StatementRowKind.Withdrawal,
        "DIVIDEND" => StatementRowKind.Dividend,
        _ => StatementRowKind.Unsupported,
    };

    private static bool IsTrade(StatementRowKind kind) => kind is StatementRowKind.Buy or StatementRowKind.Sell;

    private static string IdentityOf(string key, Dictionary<string, int> occurrences)
    {
        var occurrence = occurrences[key] = occurrences.GetValueOrDefault(key) + 1;
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"{key}|{occurrence}"));

        return StatementMatcher.ReferencePrefix + Convert.ToHexString(hash)[..IdentityLength].ToLowerInvariant();
    }

    private static (string? Currency, decimal? Amount) Money(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return (null, null);

        var parts = text.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);

        return parts.Length == 2 && parts[0].Length == 3 && parts[0].All(char.IsLetter)
            ? (parts[0].ToUpperInvariant(), Number(parts[1]))
            : (null, Number(text));
    }

    private static decimal? Number(string text) =>
        decimal.TryParse(
            text.Replace(",", "").Replace(" ", ""),
            NumberStyles.Number | NumberStyles.AllowExponent,
            CultureInfo.InvariantCulture,
            out var value) ? value : null;

    private static List<string> Fields(string line)
    {
        var fields = new List<string>();
        var current = new StringBuilder();
        var quoted = false;

        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (ch == '"')
            {
                if (quoted && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                    quoted = !quoted;
            }
            else if (ch == ',' && !quoted)
            {
                fields.Add(current.ToString());
                current.Clear();
            }
            else
                current.Append(ch);
        }

        fields.Add(current.ToString());

        return fields;
    }

    private sealed record CsvLine(List<string> Header, List<string> Fields)
    {
        public string this[string column]
        {
            get
            {
                var at = Header.FindIndex(name => string.Equals(name, column, StringComparison.OrdinalIgnoreCase));

                return at >= 0 && at < Fields.Count ? Fields[at].Trim() : "";
            }
        }
    }
}
