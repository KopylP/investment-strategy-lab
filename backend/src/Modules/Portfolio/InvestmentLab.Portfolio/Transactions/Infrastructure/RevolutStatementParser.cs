using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using InvestmentLab.Portfolio.Transactions.Domain;

namespace InvestmentLab.Portfolio.Transactions.Infrastructure;

/// <summary>
/// Anti-corruption layer for the Revolut trading statement (CSV): <c>Date,Ticker,Type,Quantity,Price per share,Total Amount,Currency,FX Rate</c>,
/// amounts written as "USD 525.83". Unknown row types are kept as unsupported rather than dropped.
/// </summary>
internal sealed class RevolutStatementParser : IStatementParser
{
    private static readonly string[] Required = ["Date", "Ticker", "Type", "Quantity", "Price per share", "Total Amount", "Currency"];

    public string Broker => "Revolut";

    public Result<ParsedStatement> Parse(string content)
    {
        var lines = content.Replace("\r\n", "\n").Split('\n').Where(line => !string.IsNullOrWhiteSpace(line)).ToList();
        if (lines.Count == 0)
            return StatementImportErrors.UnknownFormat("the file is empty");

        var header = Fields(lines[0].TrimStart('﻿'));
        var missing = Required.Where(column => !header.Contains(column, StringComparer.OrdinalIgnoreCase)).ToList();
        if (missing.Count > 0)
            return StatementImportErrors.UnknownFormat($"columns missing: {string.Join(", ", missing)}");
        int Column(string name) => header.FindIndex(column => string.Equals(column, name, StringComparison.OrdinalIgnoreCase));

        var rows = new List<StatementRow>();
        var warnings = new List<string>();
        var seen = new Dictionary<string, int>();
        for (var index = 1; index < lines.Count; index++)
        {
            var fields = Fields(lines[index]);
            string Field(string name) => Column(name) is var at and >= 0 && at < fields.Count ? fields[at].Trim() : "";

            if (!DateTimeOffset.TryParse(Field("Date"), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var occurredAt))
            {
                warnings.Add($"Рядок {index + 1}: не вдалося прочитати дату «{Field("Date")}» — пропущено.");
                continue;
            }

            var type = Field("Type");
            var ticker = Field("Ticker") is { Length: > 0 } t ? t.ToUpperInvariant() : null;
            var (amountCurrency, amount) = Money(Field("Total Amount"));
            var (_, price) = Money(Field("Price per share"));
            var quantity = Number(Field("Quantity"));
            var currency = Field("Currency") is { Length: 3 } c ? c.ToUpperInvariant() : amountCurrency ?? "";

            var kind = type.ToUpperInvariant() switch
            {
                var buy when buy.StartsWith("BUY", StringComparison.Ordinal) => StatementRowKind.Buy,
                var sell when sell.StartsWith("SELL", StringComparison.Ordinal) => StatementRowKind.Sell,
                "CASH TOP-UP" => StatementRowKind.Contribution,
                "CASH WITHDRAWAL" => StatementRowKind.Withdrawal,
                "DIVIDEND" => StatementRowKind.Dividend,
                _ => StatementRowKind.Unsupported,
            };
            if (kind is StatementRowKind.Buy or StatementRowKind.Sell && (quantity is not > 0m || ticker is null))
                kind = StatementRowKind.Unsupported;
            if (kind is StatementRowKind.Buy or StatementRowKind.Sell && price is not > 0m)
                price = Math.Abs(amount ?? 0m) / quantity!.Value;

            // The exact time is unique per line; a repeat within the file still gets its own identity.
            var key = $"{Field("Date")}|{type}|{ticker}";
            var occurrence = seen[key] = seen.GetValueOrDefault(key) + 1;
            var identity = StatementMatcher.ReferencePrefix + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{key}|{occurrence}")))[..20].ToLowerInvariant();

            rows.Add(new StatementRow(index + 1, occurredAt, kind, type, ticker, quantity, price, Math.Abs(amount ?? 0m), currency, identity));
        }

        return new ParsedStatement(Broker, rows, warnings);
    }

    /// <summary>"USD 1,234.56" → (USD, 1234.56); "EUR -15.77" → (EUR, -15.77).</summary>
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
        decimal.TryParse(text.Replace(",", "").Replace(" ", ""), NumberStyles.Number | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out var value) ? value : null;

    /// <summary>CSV fields with quoted values ("1,234.56").</summary>
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
}
