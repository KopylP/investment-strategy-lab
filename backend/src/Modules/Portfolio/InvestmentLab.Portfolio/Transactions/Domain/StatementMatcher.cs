namespace InvestmentLab.Portfolio.Transactions.Domain;

public static class StatementMatcher
{
    public const string ReferencePrefix = "revolut:";
    private const decimal PriceTolerance = 0.005m;
    private const decimal QuantityTolerance = 0.01m;
    private const decimal AmountTolerance = 0.01m;
    private const int MaxDaysBetweenSameEvent = 1;

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

    public static IReadOnlyList<RowOutcome> Match(
        IReadOnlyList<PricedRow> rows,
        IReadOnlyList<Transaction> journal,
        ImportMode mode,
        DateOnly? fromDate)
    {
        var live = LiveAfterUpFrontReversals(journal, mode);
        var imported = LatestImportedByIdentity(live);
        var manualOrFromReports = live.Where(entry => entry.Source.Kind is TransactionSourceKind.ReportAction or TransactionSourceKind.Manual).ToList();
        var claimed = new HashSet<TransactionId>();

        var outcomes = new List<RowOutcome>();

        foreach (var priced in rows.OrderBy(row => row.Row.OccurredAt).ThenBy(row => row.Row.Number))
        {
            var outcome = RowThatCannotBeRecorded(priced, fromDate)
                ?? RowImportedEarlier(priced, imported, mode)
                ?? RowAlreadyInJournal(priced, manualOrFromReports, claimed)
                ?? NewRow(priced.Row);

            outcomes.Add(outcome);
        }

        return outcomes;
    }

    private static List<Transaction> LiveAfterUpFrontReversals(IReadOnlyList<Transaction> journal, ImportMode mode)
    {
        var reversedUpFront = ToReverse(journal, mode).Select(entry => entry.Id).ToHashSet();

        return [.. Live(journal).Where(entry => !reversedUpFront.Contains(entry.Id))];
    }

    private static Dictionary<string, Transaction> LatestImportedByIdentity(List<Transaction> live) =>
        live.Where(IsImportedFromStatement)
            .GroupBy(entry => IdentityOf(entry.Source.Reference!))
            .ToDictionary(group => group.Key, group => group.OrderByDescending(entry => entry.RecordedAt).First());

    private static bool IsImportedFromStatement(Transaction entry) =>
        entry.Source is { Kind: TransactionSourceKind.Import, Reference: { } reference }
        && reference.StartsWith(ReferencePrefix, StringComparison.Ordinal);

    private static RowOutcome? RowThatCannotBeRecorded(PricedRow priced, DateOnly? fromDate)
    {
        var row = priced.Row;

        if (row.Kind == StatementRowKind.Unsupported)
            return NotRecorded(row, RowDecision.Unsupported, $"«{row.RawType}» не імпортується");

        if (fromDate is { } start && row.OccurredOn < start)
            return NotRecorded(row, RowDecision.OutOfScope);

        if (IsUnmapped(priced))
            return NotRecorded(row, RowDecision.NeedsMapping, WhyUnmapped(priced));

        return null;
    }

    private static bool IsUnmapped(PricedRow priced) =>
        priced.MissingRate
        || priced.SubPortfolioId is null
        || (priced.Row.NeedsInstrument && priced.InstrumentId is null);

    private static RowOutcome? RowImportedEarlier(PricedRow priced, Dictionary<string, Transaction> imported, ImportMode mode)
    {
        var row = priced.Row;

        if (!imported.TryGetValue(row.Identity, out var earlier))
            return null;

        if (SameNumbers(priced, earlier))
            return NotRecorded(row, RowDecision.AlreadyImported, existingId: earlier.Id.Value);

        var overwrite = mode != ImportMode.Merge;
        var note = overwrite ? "буде перезаписано з виписки" : "оберіть «Оновити імпортоване», щоб перезаписати";

        return Replacing(row, RowDecision.Differs, note, earlier.Id.Value, overwrite);
    }

    private static RowOutcome? RowAlreadyInJournal(PricedRow priced, List<Transaction> candidates, HashSet<TransactionId> claimed)
    {
        if (candidates.FirstOrDefault(entry => !claimed.Contains(entry.Id) && SameEvent(priced, entry)) is not { } existing)
            return null;

        claimed.Add(existing.Id);

        var differs = !SameNumbers(priced, existing);
        var note = differs
            ? "та сама угода вже в журналі з іншими цифрами — буде виправлено за випискою"
            : "та сама угода вже в журналі";

        return Replacing(priced.Row, RowDecision.MatchesExisting, note, existing.Id.Value, differs);
    }

    private static RowOutcome NewRow(StatementRow row) => new(
        Number: row.Number,
        Decision: RowDecision.New,
        Note: null,
        ExistingTransactionId: null,
        WillRecord: true,
        WillReverseExisting: false,
        Result: null);

    private static RowOutcome NotRecorded(StatementRow row, RowDecision decision, string? note = null, Guid? existingId = null) =>
        Replacing(row, decision, note, existingId, replace: false);

    private static RowOutcome Replacing(StatementRow row, RowDecision decision, string? note, Guid? existingId, bool replace) => new(
        Number: row.Number,
        Decision: decision,
        Note: note,
        ExistingTransactionId: existingId,
        WillRecord: replace,
        WillReverseExisting: replace,
        Result: null);

    public static string IdentityOf(string reference) => reference.Split('#')[0];

    private static List<Transaction> Live(IReadOnlyList<Transaction> journal)
    {
        var reversed = journal.Where(entry => entry.ReversalOf is not null).Select(entry => entry.ReversalOf!.Value).ToHashSet();

        return [.. journal.Where(entry => entry.ReversalOf is null && !reversed.Contains(entry.Id))];
    }

    private static bool SameEvent(PricedRow priced, Transaction entry)
    {
        var row = priced.Row;
        if (Math.Abs(entry.OccurredOn.DayNumber - row.OccurredOn.DayNumber) > MaxDaysBetweenSameEvent)
            return false;

        return (row.Kind, entry.Details) switch
        {
            (StatementRowKind.Buy or StatementRowKind.Sell, TradeDetails trade) => SameTrade(priced, trade),
            (StatementRowKind.Contribution, CashMovementDetails { Kind: CashMovementKind.Contribution } cash) => SameAmount(priced, cash),
            (StatementRowKind.Withdrawal, CashMovementDetails { Kind: CashMovementKind.Withdrawal } cash) => SameAmount(priced, cash),
            (StatementRowKind.Dividend, CashMovementDetails { Kind: CashMovementKind.Dividend } cash) =>
                cash.InstrumentId == priced.InstrumentId && SameAmount(priced, cash),
            _ => false,
        };
    }

    private static bool SameTrade(PricedRow priced, TradeDetails trade)
    {
        var side = priced.Row.Kind == StatementRowKind.Buy ? TradeSide.Buy : TradeSide.Sell;

        return trade.InstrumentId == priced.InstrumentId
            && trade.Side == side
            && Close(trade.Quantity.Value, priced.Quantity, QuantityTolerance);
    }

    private static bool SameAmount(PricedRow priced, CashMovementDetails cash) =>
        Math.Abs(cash.Amount.Amount - priced.Amount) <= AmountTolerance;

    private static bool SameNumbers(PricedRow priced, Transaction entry) => entry.Details switch
    {
        TradeDetails trade => entry.OccurredOn == priced.Row.OccurredOn && SameTradeNumbers(priced, trade),
        CashMovementDetails cash => SameAmount(priced, cash)
                                    && entry.OccurredOn == priced.Row.OccurredOn
                                    && cash.Settlement == priced.Settlement,
        _ => false,
    };

    private static bool SameTradeNumbers(PricedRow priced, TradeDetails trade) =>
        trade.Quantity.Value == priced.Quantity
        && Close(trade.Price.Amount, priced.Price, PriceTolerance)
        && trade.Settlement == priced.Settlement
        && Math.Abs(trade.Fee.Amount - decimal.Round(priced.Fee, 2)) <= AmountTolerance;

    private static bool Close(decimal left, decimal right, decimal tolerance) =>
        right == 0m ? left == 0m : Math.Abs(left - right) / Math.Abs(right) <= tolerance;

    private static string WhyUnmapped(PricedRow priced) => priced switch
    {
        { MissingRate: true } => $"немає курсу NBP {priced.Row.Currency} на {priced.Row.OccurredOn:dd.MM.yyyy}",
        { Row.NeedsInstrument: true, InstrumentId: null } => $"{priced.Row.Ticker}: не вибрано інструмент",
        { Row.NeedsInstrument: true } => $"{priced.Row.Ticker}: не вибрано під-портфель",
        _ => "не вибрано під-портфель для коштів",
    };
}
