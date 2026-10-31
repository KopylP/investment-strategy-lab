using InvestmentLab.MarketData.Contracts.Instruments;
using InvestmentLab.Portfolio.Accounts.Domain;
using InvestmentLab.Portfolio.Transactions.Application.ReverseTransaction;
using InvestmentLab.Portfolio.Transactions.Application.Shared;
using InvestmentLab.Portfolio.Transactions.Domain;

namespace InvestmentLab.Portfolio.Transactions.Application.ConfirmStatementImport;

public sealed class ConfirmStatementImportHandler(
    StatementImportLoader loader,
    StatementPreview preview,
    StatementPricing pricing,
    ITransactionJournal journal,
    IStatementImportRepository imports,
    TransactionRecorder recorder,
    ReverseTransactionHandler reversals,
    IInstrumentRegistry registry,
    TimeProvider clock)
{
    private const string NewInstrumentKind = "Stock";
    private const string CashTicker = "кеш";

    public async Task<Result<StatementImportDto>> Handle(Guid accountId, Guid importId, CancellationToken cancellationToken)
    {
        var loaded = await loader.LoadAsync(accountId, importId, cancellationToken);
        if (loaded.IsFailure)
            return loaded.Error;

        var (account, statement) = loaded.Value;

        if (!statement.IsPreview)
            return StatementImportErrors.NotInPreview(statement.Id);

        var mappings = await RegisterNewInstrumentsAsync(statement, cancellationToken);
        if (mappings.IsFailure)
            return mappings.Error;

        statement.Configure(statement.Mode, statement.FromDate, statement.CashSubPortfolioId, mappings.Value);

        var entries = await journal.GetEntriesAsync(account.Id, cancellationToken);
        var priced = await pricing.PriceAsync(account, statement, cancellationToken);
        var plan = StatementMatcher.Match(priced, entries, statement.Mode, statement.FromDate);

        var unmapped = UnmappedTickers(plan, statement);
        if (unmapped.Count > 0)
            return StatementImportErrors.Unmapped(unmapped);

        var progress = new ConfirmationProgress();
        await ReverseReplacedEntriesAsync(account, statement, entries, progress, cancellationToken);
        await CarryOutPlanAsync(account, plan, priced, progress, cancellationToken);

        statement.Confirm(progress.Outcomes, progress.References, progress.Reversed, clock.GetUtcNow());
        statement.Warnings.AddRange(progress.Warnings);
        await imports.SaveChangesAsync(cancellationToken);

        return await preview.PlanAndShowAsync(account, statement, cancellationToken);
    }

    private async Task<Result<List<TickerMapping>>> RegisterNewInstrumentsAsync(StatementImport statement, CancellationToken cancellationToken)
    {
        var mappings = new List<TickerMapping>();

        foreach (var mapping in statement.Mappings)
        {
            if (mapping is not { InstrumentId: null, RegisterAs: { Length: > 0 } symbol })
            {
                mappings.Add(mapping);
                continue;
            }

            var instrument = new NewInstrumentDto(symbol, mapping.RegisterName ?? mapping.Ticker, mapping.Currency, NewInstrumentKind);
            var registered = await registry.RegisterAsync(instrument, cancellationToken);
            if (registered.InstrumentId is null)
                return Error.Validation("statement_import.instrument_not_registered", $"{mapping.Ticker}: {registered.Error}");

            mappings.Add(mapping with { InstrumentId = registered.InstrumentId, RegisterAs = null });
        }

        return mappings;
    }

    private static List<string> UnmappedTickers(IReadOnlyList<RowOutcome> plan, StatementImport statement)
    {
        var unmapped = plan.Where(outcome => outcome.Decision == RowDecision.NeedsMapping);
        var tickers = unmapped.Select(outcome => statement.Rows.First(row => row.Number == outcome.Number).Ticker ?? CashTicker);

        return [.. tickers.Distinct()];
    }

    private async Task ReverseReplacedEntriesAsync(
        InvestmentAccount account,
        StatementImport statement,
        IReadOnlyList<Transaction> entries,
        ConfirmationProgress progress,
        CancellationToken cancellationToken)
    {
        var replaced = StatementMatcher.ToReverse(entries, statement.Mode)
            .OrderByDescending(entry => entry.OccurredOn)
            .ThenByDescending(entry => entry.RecordedAt);
        var reversal = new ReverseTransactionCommand($"імпорт виписки {statement.FileName}");

        foreach (var entry in replaced)
        {
            var result = await reversals.Handle(account.Id.Value, entry.Id.Value, reversal, cancellationToken);

            if (result.IsSuccess)
                progress.Reversed.Add(entry.Id.Value);
            else
                progress.Warnings.Add($"Не вдалося сторнувати операцію {entry.OccurredOn:yyyy-MM-dd}: {result.Error.Message}");
        }
    }

    private async Task CarryOutPlanAsync(
        InvestmentAccount account,
        IReadOnlyList<RowOutcome> plan,
        IReadOnlyList<PricedRow> priced,
        ConfirmationProgress progress,
        CancellationToken cancellationToken)
    {
        var pricedOf = priced.ToDictionary(row => row.Row.Number);

        foreach (var outcome in plan)
        {
            var result = await ReverseDifferingEntryAsync(account, outcome, progress, cancellationToken);

            if (outcome.WillRecord && result is null)
                result = await RecordRowAsync(account, pricedOf[outcome.Number], progress, cancellationToken);

            progress.Outcomes.Add(outcome with { Result = result ?? Describe(outcome.Decision) });
        }
    }

    private async Task<string?> ReverseDifferingEntryAsync(
        InvestmentAccount account, RowOutcome outcome, ConfirmationProgress progress, CancellationToken cancellationToken)
    {
        if (outcome is not { WillReverseExisting: true, ExistingTransactionId: { } existing })
            return null;

        var reversal = await reversals.Handle(account.Id.Value, existing, new ReverseTransactionCommand("виправлено за випискою"), cancellationToken);

        if (reversal.IsFailure)
            return $"старий запис не сторновано: {reversal.Error.Message}";

        progress.Reversed.Add(existing);

        return null;
    }

    private async Task<string> RecordRowAsync(
        InvestmentAccount account, PricedRow row, ConfirmationProgress progress, CancellationToken cancellationToken)
    {
        var reference = await NextReferenceAsync(row.Row.Identity, cancellationToken);
        var asIncome = false;

        var recorded = await recorder.RecordAsync(account.Id.Value, context =>
        {
            var created = StatementRowTransaction.Of(context, row, reference);
            asIncome = row.Row.Kind == StatementRowKind.Sell && created is { IsSuccess: true, Value.Kind: TransactionKind.Dividend };

            return created;
        }, cancellationToken);

        if (recorded.IsFailure)
            return $"не записано: {recorded.Error.Message}";

        progress.References.Add(reference);

        return asIncome ? "записано як дохід: продаж акцій без купівлі (спін-оф)" : "записано";
    }

    private async Task<string> NextReferenceAsync(string identity, CancellationToken cancellationToken)
    {
        if (!await journal.SourceExistsAsync(identity, cancellationToken))
            return identity;

        for (var n = 2; ; n++)
        {
            if (!await journal.SourceExistsAsync($"{identity}#{n}", cancellationToken))
                return $"{identity}#{n}";
        }
    }

    private static string Describe(RowDecision decision) => decision switch
    {
        RowDecision.AlreadyImported => "уже в журналі",
        RowDecision.MatchesExisting => "збігається з журналом",
        RowDecision.Differs => "відрізняється — не змінено",
        RowDecision.OutOfScope => "до дати початку",
        RowDecision.Unsupported => "не імпортується",
        _ => "—",
    };

    private sealed class ConfirmationProgress
    {
        public List<Guid> Reversed { get; } = [];
        public List<string> Warnings { get; } = [];
        public List<string> References { get; } = [];
        public List<RowOutcome> Outcomes { get; } = [];
    }
}
