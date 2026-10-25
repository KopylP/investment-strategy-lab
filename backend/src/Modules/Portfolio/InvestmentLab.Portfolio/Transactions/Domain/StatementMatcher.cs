namespace InvestmentLab.Portfolio.Transactions.Domain;

/// <summary>A statement row in the account currency, with its instrument and sub-portfolio resolved (when mapped).</summary>
public sealed record PricedRow(StatementRow Row, Guid? InstrumentId, Guid? SubPortfolioId, decimal Quantity, decimal Price, decimal Amount);

/// <summary>
/// Decides what each row of a statement does to the journal (stage 10): new entry, already there, changed, the same
/// trade as one marked in a report, out of scope. The statement is the truth for trades it shares with the journal.
/// </summary>
public static class StatementMatcher
{
    public const string ReferencePrefix = "revolut:";
    private const decimal PriceTolerance = 0.005m;
    private const decimal QuantityTolerance = 0.01m;

    /// <summary>The journal entries a mode reverses before importing.</summary>
    public static IReadOnlyList<Transaction> ToReverse(IReadOnlyList<Transaction> journal, ImportMode mode)
    {
        var live = Live(journal);
        return mode switch
        {
            ImportMode.Replace => [.. live.Where(entry => entry.Source.Kind == TransactionSourceKind.Import)],
            ImportMode.FromScratch => live,
            _ => [],
        };
    }

    public static IReadOnlyList<RowOutcome> Match(IReadOnlyList<PricedRow> rows, IReadOnlyList<Transaction> journal, ImportMode mode, DateOnly? fromDate)
    {
        var reversedUpFront = ToReverse(journal, mode).Select(entry => entry.Id).ToHashSet();
        var live = Live(journal).Where(entry => !reversedUpFront.Contains(entry.Id)).ToList();
        var imported = live.Where(entry => entry.Source is { Kind: TransactionSourceKind.Import, Reference: { } reference } && reference.StartsWith(ReferencePrefix, StringComparison.Ordinal))
            .GroupBy(entry => IdentityOf(entry.Source.Reference!))
            .ToDictionary(group => group.Key, group => group.OrderByDescending(entry => entry.RecordedAt).First());
        var others = live.Where(entry => entry.Source.Kind is TransactionSourceKind.ReportAction or TransactionSourceKind.Manual).ToList();
        var claimed = new HashSet<TransactionId>();

        var outcomes = new List<RowOutcome>();
        foreach (var priced in rows.OrderBy(row => row.Row.OccurredAt).ThenBy(row => row.Row.Number))
        {
            var row = priced.Row;
            if (row.Kind == StatementRowKind.Unsupported)
            {
                outcomes.Add(new RowOutcome(row.Number, RowDecision.Unsupported, $"«{row.RawType}» не імпортується", null, false, false, null));
                continue;
            }
            if (fromDate is { } start && row.OccurredOn < start)
            {
                outcomes.Add(new RowOutcome(row.Number, RowDecision.OutOfScope, null, null, false, false, null));
                continue;
            }
            if (priced.SubPortfolioId is null || row.NeedsInstrument && priced.InstrumentId is null)
            {
                outcomes.Add(new RowOutcome(row.Number, RowDecision.NeedsMapping, null, null, false, false, null));
                continue;
            }

            if (imported.TryGetValue(row.Identity, out var earlier))
            {
                if (SameNumbers(priced, earlier))
                    outcomes.Add(new RowOutcome(row.Number, RowDecision.AlreadyImported, null, earlier.Id.Value, false, false, null));
                else
                {
                    var update = mode != ImportMode.Merge;
                    outcomes.Add(new RowOutcome(row.Number, RowDecision.Differs, update ? "буде перезаписано з виписки" : "оберіть «Оновити імпортоване», щоб перезаписати",
                        earlier.Id.Value, update, update, null));
                }
                continue;
            }

            if (others.FirstOrDefault(entry => !claimed.Contains(entry.Id) && SameEvent(priced, entry)) is { } existing)
            {
                claimed.Add(existing.Id);
                var differs = !SameNumbers(priced, existing);
                outcomes.Add(new RowOutcome(row.Number, RowDecision.MatchesExisting,
                    differs ? "та сама угода вже в журналі з іншими цифрами — буде виправлено за випискою" : "та сама угода вже в журналі",
                    existing.Id.Value, differs, differs, null));
                continue;
            }

            outcomes.Add(new RowOutcome(row.Number, RowDecision.New, null, null, true, false, null));
        }
        return outcomes;
    }

    /// <summary>"revolut:abc#2" → "revolut:abc": a row re-recorded after a correction keeps its identity.</summary>
    public static string IdentityOf(string reference) => reference.Split('#')[0];

    private static List<Transaction> Live(IReadOnlyList<Transaction> journal)
    {
        var reversed = journal.Where(entry => entry.ReversalOf is not null).Select(entry => entry.ReversalOf!.Value).ToHashSet();
        return [.. journal.Where(entry => entry.ReversalOf is null && !reversed.Contains(entry.Id))];
    }

    private static bool SameEvent(PricedRow priced, Transaction entry)
    {
        var row = priced.Row;
        if (Math.Abs(entry.OccurredOn.DayNumber - row.OccurredOn.DayNumber) > 1)
            return false;
        return (row.Kind, entry.Details) switch
        {
            (StatementRowKind.Buy or StatementRowKind.Sell, TradeDetails trade) =>
                trade.InstrumentId == priced.InstrumentId
                && trade.Side == (row.Kind == StatementRowKind.Buy ? TradeSide.Buy : TradeSide.Sell)
                && Close(trade.Quantity.Value, priced.Quantity, QuantityTolerance),
            (StatementRowKind.Contribution, CashMovementDetails { Kind: CashMovementKind.Contribution } cash) => Math.Abs(cash.Amount.Amount - priced.Amount) <= 0.01m,
            (StatementRowKind.Withdrawal, CashMovementDetails { Kind: CashMovementKind.Withdrawal } cash) => Math.Abs(cash.Amount.Amount - priced.Amount) <= 0.01m,
            (StatementRowKind.Dividend, CashMovementDetails { Kind: CashMovementKind.Dividend } cash) =>
                cash.InstrumentId == priced.InstrumentId && Math.Abs(cash.Amount.Amount - priced.Amount) <= 0.01m,
            _ => false,
        };
    }

    private static bool SameNumbers(PricedRow priced, Transaction entry) => entry.Details switch
    {
        TradeDetails trade => trade.Quantity.Value == priced.Quantity && Close(trade.Price.Amount, priced.Price, PriceTolerance) && entry.OccurredOn == priced.Row.OccurredOn,
        CashMovementDetails cash => Math.Abs(cash.Amount.Amount - priced.Amount) <= 0.01m && entry.OccurredOn == priced.Row.OccurredOn,
        _ => false,
    };

    private static bool Close(decimal left, decimal right, decimal tolerance) =>
        right == 0m ? left == 0m : Math.Abs(left - right) / Math.Abs(right) <= tolerance;
}
