using System.Security.Cryptography;
using System.Text;
using InvestmentLab.MarketData.Contracts.FxRates;
using InvestmentLab.MarketData.Contracts.Instruments;
using InvestmentLab.Portfolio.Accounts.Domain;
using InvestmentLab.Portfolio.Transactions.Application.ReverseTransaction;
using InvestmentLab.Portfolio.Transactions.Domain;
using InvestmentLab.Strategies.Contracts;

namespace InvestmentLab.Portfolio.Transactions.Application.ImportStatement;

public interface IStatementImportRepository
{
    Task<StatementImport?> FindAsync(Guid importId, CancellationToken cancellationToken);

    Task<IReadOnlyList<StatementImport>> ForAccountAsync(AccountId accountId, CancellationToken cancellationToken);

    void Add(StatementImport statementImport);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public sealed record TickerMappingInput(string Ticker, Guid? InstrumentId, string? RegisterAs, Guid? SubPortfolioId);

/// <param name="Mode">Merge, UpdateImported, Replace or FromScratch.</param>
public sealed record ConfigureImportCommand(string Mode, DateOnly? FromDate, Guid? CashSubPortfolioId, IReadOnlyList<TickerMappingInput> Mappings);

public sealed record TickerMappingDto(string Ticker, string Currency, Guid? InstrumentId, string? RegisterAs, Guid? SubPortfolioId, int Rows, string? InstrumentName);

public sealed record StatementRowDto(
    int Number, DateOnly OccurredOn, string Kind, string RawType, string? Ticker, decimal? Quantity, decimal? Price, decimal Amount, string Currency,
    decimal AmountInAccountCurrency, string Decision, string? Note, bool WillRecord, bool WillReverseExisting, string? Result);

public sealed record ImportCountsDto(int New, int AlreadyImported, int Differs, int MatchesExisting, int OutOfScope, int Unsupported, int NeedsMapping, int ToRecord, int ToReverse);

public sealed record StatementImportDto(
    Guid Id,
    string Broker,
    string FileName,
    DateTimeOffset UploadedAt,
    string Status,
    string Mode,
    DateOnly? FromDate,
    Guid? CashSubPortfolioId,
    string AccountCurrency,
    IReadOnlyList<TickerMappingDto> Mappings,
    IReadOnlyList<StatementRowDto> Rows,
    ImportCountsDto Counts,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<SubPortfolioOptionDto> SubPortfolios,
    DateTimeOffset? ConfirmedAt,
    DateTimeOffset? UndoneAt);

public sealed record SubPortfolioOptionDto(Guid Id, string Name);

public sealed record StatementImportSummaryDto(Guid Id, string Broker, string FileName, DateTimeOffset UploadedAt, string Status, string Mode, int Recorded);

/// <summary>
/// Broker statement import (stage 10): upload → suggested mapping → preview of what each row does → confirm →
/// (optionally) undo the whole batch. Rows become ordinary journal entries with <c>Source = Import</c>.
/// </summary>
public sealed class StatementImportService(
    ITransactionJournal journal,
    IStatementImportRepository imports,
    IStatementParser parser,
    TransactionRecorder recorder,
    ReverseTransactionHandler reversals,
    IInstrumentCatalog catalog,
    IInstrumentRegistry registry,
    IStrategyDirectory strategies,
    IFxRates fxRates,
    TimeProvider clock)
{
    private const string YahooPrefix = "yfinance:";

    public async Task<IReadOnlyList<StatementImportSummaryDto>> List(Guid accountId, CancellationToken cancellationToken) =>
        accountId == Guid.Empty
            ? []
            : [.. (await imports.ForAccountAsync(AccountId.Of(accountId), cancellationToken)).OrderByDescending(i => i.UploadedAt)
                .Select(i => new StatementImportSummaryDto(i.Id, i.Broker, i.FileName, i.UploadedAt, i.Status.ToString(), i.Mode.ToString(), i.RecordedReferences.Count))];

    public async Task<Result<StatementImportDto>> Upload(Guid accountId, string fileName, string content, CancellationToken cancellationToken)
    {
        if (accountId == Guid.Empty || await journal.FindAccountAsync(AccountId.Of(accountId), cancellationToken) is not { } account)
            return AccountErrors.NotFound(accountId);

        var parsed = parser.Parse(content);
        if (parsed.IsFailure)
            return parsed.Error;

        var statement = StatementImport.Upload(account.Id, account.OwnerId, fileName, parsed.Value, clock.GetUtcNow());
        var mappings = await SuggestMappingsAsync(account, statement.Rows, cancellationToken);
        statement.Configure(ImportMode.Merge, null, SuggestCashSubPortfolio(account, statement.Rows, mappings), mappings);
        imports.Add(statement);
        return await PreviewAsync(account, statement, cancellationToken);
    }

    public async Task<Result<StatementImportDto>> Get(Guid accountId, Guid importId, CancellationToken cancellationToken)
    {
        var loaded = await LoadAsync(accountId, importId, cancellationToken);
        return loaded.IsFailure ? loaded.Error : await PreviewAsync(loaded.Value.Account, loaded.Value.Import, cancellationToken);
    }

    public async Task<Result<StatementImportDto>> Configure(Guid accountId, Guid importId, ConfigureImportCommand command, CancellationToken cancellationToken)
    {
        var loaded = await LoadAsync(accountId, importId, cancellationToken);
        if (loaded.IsFailure)
            return loaded.Error;
        var (account, statement) = loaded.Value;
        if (!Enum.TryParse<ImportMode>(command.Mode, ignoreCase: true, out var mode))
            return Error.Validation("statement_import.mode_invalid", $"'{command.Mode}' is not Merge, UpdateImported, Replace or FromScratch.");

        var mappings = statement.Mappings.Select(mapping => command.Mappings.FirstOrDefault(input => string.Equals(input.Ticker, mapping.Ticker, StringComparison.OrdinalIgnoreCase)) is { } input
            ? mapping with { InstrumentId = input.InstrumentId, RegisterAs = input.InstrumentId is null ? input.RegisterAs?.Trim().ToUpperInvariant() : null, SubPortfolioId = input.SubPortfolioId }
            : mapping).ToList();
        var configured = statement.Configure(mode, command.FromDate, command.CashSubPortfolioId, mappings);
        if (configured.IsFailure)
            return configured.Error;
        return await PreviewAsync(account, statement, cancellationToken);
    }

    public async Task<Result<StatementImportDto>> Confirm(Guid accountId, Guid importId, CancellationToken cancellationToken)
    {
        var loaded = await LoadAsync(accountId, importId, cancellationToken);
        if (loaded.IsFailure)
            return loaded.Error;
        var (account, statement) = loaded.Value;
        if (!statement.IsPreview)
            return StatementImportErrors.NotInPreview(statement.Id);

        // 1. New instruments are registered first, so every row has a real instrument.
        var mappings = new List<TickerMapping>();
        foreach (var mapping in statement.Mappings)
        {
            if (mapping is { InstrumentId: null, RegisterAs: { Length: > 0 } symbol })
            {
                var registered = await registry.RegisterAsync(new NewInstrumentDto(symbol, mapping.Ticker, mapping.Currency, "Stock"), cancellationToken);
                if (registered.InstrumentId is null)
                    return Error.Validation("statement_import.instrument_not_registered", $"{mapping.Ticker}: {registered.Error}");
                mappings.Add(mapping with { InstrumentId = registered.InstrumentId, RegisterAs = null });
            }
            else
                mappings.Add(mapping);
        }
        statement.Configure(statement.Mode, statement.FromDate, statement.CashSubPortfolioId, mappings);

        var entries = await journal.GetEntriesAsync(account.Id, cancellationToken);
        var priced = await PriceAsync(account, statement, cancellationToken);
        var plan = StatementMatcher.Match(priced, entries, statement.Mode, statement.FromDate);
        var unmapped = plan.Where(outcome => outcome.Decision == RowDecision.NeedsMapping)
            .Select(outcome => statement.Rows.First(row => row.Number == outcome.Number).Ticker ?? "кеш").Distinct().ToList();
        if (unmapped.Count > 0)
            return StatementImportErrors.Unmapped(unmapped);

        // 2. What the mode replaces goes first, newest first, so holdings stay valid at every step.
        var reversed = new List<Guid>();
        var warnings = new List<string>();
        foreach (var entry in StatementMatcher.ToReverse(entries, statement.Mode).OrderByDescending(entry => entry.OccurredOn).ThenByDescending(entry => entry.RecordedAt))
        {
            var result = await reversals.Handle(account.Id.Value, entry.Id.Value, new ReverseTransactionCommand($"імпорт виписки {statement.FileName}"), cancellationToken);
            if (result.IsSuccess)
                reversed.Add(entry.Id.Value);
            else
                warnings.Add($"Не вдалося сторнувати операцію {entry.OccurredOn:yyyy-MM-dd}: {result.Error.Message}");
        }

        // 3. Rows in time order: corrections reverse the old entry, then the statement's version is recorded.
        var byNumber = priced.ToDictionary(row => row.Row.Number);
        var references = new List<string>();
        var outcomes = new List<RowOutcome>();
        foreach (var outcome in plan)
        {
            string? result = null;
            if (outcome is { WillReverseExisting: true, ExistingTransactionId: { } existing })
            {
                var reversal = await reversals.Handle(account.Id.Value, existing, new ReverseTransactionCommand("виправлено за випискою"), cancellationToken);
                if (reversal.IsSuccess)
                    reversed.Add(existing);
                else
                    result = $"старий запис не сторновано: {reversal.Error.Message}";
            }
            if (outcome.WillRecord && result is null)
            {
                var row = byNumber[outcome.Number];
                var reference = await NextReferenceAsync(row.Row.Identity, cancellationToken);
                var asIncome = false;
                var recorded = await recorder.RecordAsync(account.Id.Value, context =>
                {
                    var created = Create(context, row, statement, reference);
                    asIncome = row.Row.Kind == StatementRowKind.Sell && created is { IsSuccess: true, Value.Kind: TransactionKind.Dividend };
                    return created;
                }, cancellationToken);
                if (recorded.IsSuccess)
                {
                    references.Add(reference);
                    result = asIncome ? "записано як дохід: продаж акцій без купівлі (спін-оф)" : "записано";
                }
                else
                    result = $"не записано: {recorded.Error.Message}";
            }
            outcomes.Add(outcome with { Result = result ?? Describe(outcome.Decision) });
        }

        statement.Confirm(outcomes, references, reversed, clock.GetUtcNow());
        statement.Warnings.AddRange(warnings);
        await imports.SaveChangesAsync(cancellationToken);
        return await PreviewAsync(account, statement, cancellationToken);
    }

    /// <summary>Takes the whole batch back: every entry this import recorded is reversed (newest first).</summary>
    public async Task<Result<StatementImportDto>> Undo(Guid accountId, Guid importId, CancellationToken cancellationToken)
    {
        var loaded = await LoadAsync(accountId, importId, cancellationToken);
        if (loaded.IsFailure)
            return loaded.Error;
        var (account, statement) = loaded.Value;
        if (statement.Status != StatementImportStatus.Confirmed)
            return StatementImportErrors.NotConfirmed(statement.Id);

        foreach (var reference in Enumerable.Reverse(statement.RecordedReferences))
        {
            var result = await reversals.HandleBySource(account.Id.Value, reference, $"скасовано імпорт {statement.FileName}", cancellationToken);
            if (result.IsFailure && result.Error.Code != "transaction.already_reversed")
                statement.Warnings.Add($"{reference}: {result.Error.Message}");
        }
        if (statement.ReversedTransactionIds.Count > 0)
            statement.Warnings.Add($"{statement.ReversedTransactionIds.Count} операцій, сторнованих під час імпорту, не відновлено: за потреби запишіть їх заново.");

        statement.Undo(clock.GetUtcNow());
        await imports.SaveChangesAsync(cancellationToken);
        return await PreviewAsync(account, statement, cancellationToken);
    }

    private async Task<Result<(InvestmentAccount Account, StatementImport Import)>> LoadAsync(Guid accountId, Guid importId, CancellationToken cancellationToken)
    {
        if (accountId == Guid.Empty || await journal.FindAccountAsync(AccountId.Of(accountId), cancellationToken) is not { } account)
            return AccountErrors.NotFound(accountId);
        if (await imports.FindAsync(importId, cancellationToken) is not { } statement || statement.AccountId != account.Id)
            return StatementImportErrors.NotFound(importId);
        return (account, statement);
    }

    private async Task<StatementImportDto> PreviewAsync(InvestmentAccount account, StatementImport statement, CancellationToken cancellationToken)
    {
        var priced = await PriceAsync(account, statement, cancellationToken);
        var outcomes = statement.IsPreview
            ? StatementMatcher.Match(priced, await journal.GetEntriesAsync(account.Id, cancellationToken), statement.Mode, statement.FromDate)
            : statement.Outcomes;
        if (statement.IsPreview)
        {
            statement.Plan(outcomes);
            await imports.SaveChangesAsync(cancellationToken);
        }

        var names = (await catalog.GetInstruments(cancellationToken)).ToDictionary(instrument => instrument.Id, instrument => instrument.Name);
        var byNumber = outcomes.ToDictionary(outcome => outcome.Number);
        var pricedByNumber = priced.ToDictionary(row => row.Row.Number);
        var toReverse = statement.IsPreview ? StatementMatcher.ToReverse(await journal.GetEntriesAsync(account.Id, cancellationToken), statement.Mode).Count : statement.ReversedTransactionIds.Count;

        return new StatementImportDto(
            statement.Id,
            statement.Broker,
            statement.FileName,
            statement.UploadedAt,
            statement.Status.ToString(),
            statement.Mode.ToString(),
            statement.FromDate,
            statement.CashSubPortfolioId,
            account.BaseCurrency.Code,
            [.. statement.Mappings.Select(mapping => new TickerMappingDto(mapping.Ticker, mapping.Currency, mapping.InstrumentId, mapping.RegisterAs, mapping.SubPortfolioId,
                statement.Rows.Count(row => string.Equals(row.Ticker, mapping.Ticker, StringComparison.OrdinalIgnoreCase)),
                mapping.InstrumentId is { } id && names.TryGetValue(id, out var name) ? name : null))],
            [.. statement.Rows.Select(row =>
            {
                var outcome = byNumber.GetValueOrDefault(row.Number);
                return new StatementRowDto(row.Number, row.OccurredOn, row.Kind.ToString(), row.RawType, row.Ticker, row.Quantity, row.Price, row.Amount, row.Currency,
                    pricedByNumber.TryGetValue(row.Number, out var p) ? decimal.Round(p.Amount, 2) : row.Amount,
                    (outcome?.Decision ?? RowDecision.NeedsMapping).ToString(), outcome?.Note, outcome?.WillRecord ?? false, outcome?.WillReverseExisting ?? false, outcome?.Result);
            })],
            new ImportCountsDto(
                outcomes.Count(o => o.Decision == RowDecision.New), outcomes.Count(o => o.Decision == RowDecision.AlreadyImported),
                outcomes.Count(o => o.Decision == RowDecision.Differs), outcomes.Count(o => o.Decision == RowDecision.MatchesExisting),
                outcomes.Count(o => o.Decision == RowDecision.OutOfScope), outcomes.Count(o => o.Decision == RowDecision.Unsupported),
                outcomes.Count(o => o.Decision == RowDecision.NeedsMapping), outcomes.Count(o => o.WillRecord),
                toReverse + outcomes.Count(o => o.WillReverseExisting)),
            statement.Warnings,
            [.. account.ActiveSubPortfolios.Select(sub => new SubPortfolioOptionDto(sub.Id.Value, sub.Name))],
            statement.ConfirmedAt,
            statement.UndoneAt);
    }

    /// <summary>Rows in the account currency (NBP cross rate via PLN of the trade day) with their mapping applied.</summary>
    private async Task<IReadOnlyList<PricedRow>> PriceAsync(InvestmentAccount account, StatementImport statement, CancellationToken cancellationToken)
    {
        var accountCurrency = account.BaseCurrency.Code;
        var rates = new Dictionary<(string, DateOnly), decimal?>();
        async Task<decimal?> InPln(string currency, DateOnly day)
        {
            if (currency == "PLN")
                return 1m;
            if (!rates.TryGetValue((currency, day), out var rate))
                rate = rates[(currency, day)] = (await fxRates.GetFxRate(currency, "PLN", day.AddDays(1), cancellationToken))?.Rate;
            return rate;
        }

        var priced = new List<PricedRow>();
        foreach (var row in statement.Rows)
        {
            var factor = 1m;
            if (!string.Equals(row.Currency, accountCurrency, StringComparison.OrdinalIgnoreCase))
                factor = await InPln(row.Currency, row.OccurredOn) is { } from && await InPln(accountCurrency, row.OccurredOn) is { } to && to > 0m ? from / to : 0m;

            var mapping = statement.MappingOf(row.Ticker);
            var instrument = mapping?.InstrumentId ?? (mapping?.RegisterAs is { Length: > 0 } symbol ? Placeholder(symbol) : null);
            var sub = row.NeedsInstrument ? mapping?.SubPortfolioId ?? (row.Kind == StatementRowKind.Dividend ? statement.CashSubPortfolioId : null) : statement.CashSubPortfolioId;
            // A row in another currency settles from (or into) the broker's balance in that currency, as Revolut keeps it.
            var settlement = !string.Equals(row.Currency, accountCurrency, StringComparison.OrdinalIgnoreCase) && factor != 0m && row.Amount > 0m && row.Kind != StatementRowKind.Unsupported && Currency.TryOf(row.Currency) is { IsSuccess: true } own
                ? Money.Of(row.Amount, own.Value)
                : null;
            priced.Add(new PricedRow(row, instrument, factor == 0m ? null : sub, row.Quantity ?? 0m, (row.Price ?? 0m) * factor, row.Amount * factor, settlement,
                PricedRow.FeeOf(row) * factor));
        }
        return priced;
    }

    private Result<Transaction> Create(RecordingContext context, PricedRow row, StatementImport statement, string reference)
    {
        var account = context.Account;
        var currency = account.BaseCurrency;
        var source = new TransactionSource(TransactionSourceKind.Import, reference);
        var sub = SubPortfolioId.Of(row.SubPortfolioId!.Value);
        var day = row.Row.OccurredOn;
        switch (row.Row.Kind)
        {
            case StatementRowKind.Sell when IsSpinOffSale(context, row, sub):
                // Shares that came without a buy (a spin-off, e.g. SFGYY from Sony) and were sold: the proceeds are income.
                var proceeds = Money.Of(decimal.Round(row.Amount, 2), currency);
                return Transaction.CashMovement(account, CashMovementKind.Dividend, proceeds, [new CashAllocation(sub, proceeds)], row.InstrumentId,
                    day, source, context.Now, row.Settlement);
            case StatementRowKind.Buy or StatementRowKind.Sell:
                var quantity = Quantity.TryOf(row.Quantity);
                var price = Price.TryOf(decimal.Round(row.Price, 6), currency);
                if (quantity.IsFailure || price.IsFailure)
                    return quantity.IsFailure ? quantity.Error : price.Error;
                return Transaction.Trade(account, row.Row.Kind == StatementRowKind.Buy ? TradeSide.Buy : TradeSide.Sell, row.InstrumentId!.Value,
                    quantity.Value, price.Value, Money.Of(decimal.Round(row.Fee, 2), currency), [new Allocation(sub, quantity.Value)], day, source, context.Now, row.Settlement);
            default:
                var kind = row.Row.Kind switch
                {
                    StatementRowKind.Contribution => CashMovementKind.Contribution,
                    StatementRowKind.Withdrawal => CashMovementKind.Withdrawal,
                    _ => CashMovementKind.Dividend,
                };
                var amount = Money.Of(row.Amount, currency);
                return Transaction.CashMovement(account, kind, amount, [new CashAllocation(sub, amount)],
                    kind == CashMovementKind.Dividend ? row.InstrumentId : null, day, source, context.Now, row.Settlement);
        }
    }

    /// <summary>A sale of units the account never bought (none held now and no buy of the instrument in the journal).</summary>
    private static bool IsSpinOffSale(RecordingContext context, PricedRow row, SubPortfolioId sub) =>
        row.InstrumentId is { } instrument
        && context.Holdings.UnitsOf(sub, instrument).Value < row.Quantity
        && !context.Journal.Any(entry => entry.Details is TradeDetails { Side: TradeSide.Buy } trade && trade.InstrumentId == instrument);

    /// <summary>"revolut:abc", or "revolut:abc#2" when the row was recorded before and corrected.</summary>
    private async Task<string> NextReferenceAsync(string identity, CancellationToken cancellationToken)
    {
        if (!await journal.SourceExistsAsync(identity, cancellationToken))
            return identity;
        for (var n = 2; ; n++)
            if (!await journal.SourceExistsAsync($"{identity}#{n}", cancellationToken))
                return $"{identity}#{n}";
    }

    private async Task<List<TickerMapping>> SuggestMappingsAsync(InvestmentAccount account, IReadOnlyList<StatementRow> rows, CancellationToken cancellationToken)
    {
        var instruments = await catalog.GetInstruments(cancellationToken);
        var symbols = instruments.SelectMany(instrument => instrument.ProviderSymbols
                .Where(symbol => symbol.StartsWith(YahooPrefix, StringComparison.OrdinalIgnoreCase))
                .Select(symbol => (Symbol: symbol[YahooPrefix.Length..].ToUpperInvariant(), Instrument: instrument)))
            .ToList();
        var universes = (await strategies.GetStrategies(cancellationToken)).ToDictionary(strategy => strategy.Id, strategy => strategy.Universe);

        return [.. rows.Where(row => row.NeedsInstrument && row.Ticker is not null)
            .GroupBy(row => row.Ticker!, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var currency = group.First().Currency;
                var ticker = group.Key.ToUpperInvariant();
                var match = symbols.FirstOrDefault(s => s.Symbol == ticker && s.Instrument.TradingCurrency == currency).Instrument
                    ?? symbols.FirstOrDefault(s => s.Symbol.StartsWith(ticker + ".", StringComparison.Ordinal) && s.Instrument.TradingCurrency == currency).Instrument
                    ?? symbols.FirstOrDefault(s => s.Symbol == ticker || s.Symbol.StartsWith(ticker + ".", StringComparison.Ordinal)).Instrument;
                var sub = match is null
                    ? null
                    : account.ActiveSubPortfolios.FirstOrDefault(s => s.Strategy is { } strategy && universes.TryGetValue(strategy.StrategyId, out var universe) && universe.Contains(match.Id))?.Id.Value;
                return new TickerMapping(ticker, currency, match?.Id, match is null ? (currency == "EUR" ? $"{ticker}.DE" : ticker) : null, sub);
            })];
    }

    private static Guid? SuggestCashSubPortfolio(InvestmentAccount account, IReadOnlyList<StatementRow> rows, IReadOnlyList<TickerMapping> mappings)
    {
        var spentBySub = rows.Where(row => row.Kind == StatementRowKind.Buy)
            .Select(row => (Sub: mappings.FirstOrDefault(m => string.Equals(m.Ticker, row.Ticker, StringComparison.OrdinalIgnoreCase))?.SubPortfolioId, row.Amount))
            .Where(entry => entry.Sub is not null)
            .GroupBy(entry => entry.Sub!.Value)
            .OrderByDescending(group => group.Sum(entry => entry.Amount))
            .Select(group => (Guid?)group.Key)
            .FirstOrDefault();
        return spentBySub ?? account.ActiveSubPortfolios.FirstOrDefault()?.Id.Value;
    }

    private static Guid Placeholder(string symbol) => new(MD5.HashData(Encoding.UTF8.GetBytes($"new:{symbol}")));

    private static string Describe(RowDecision decision) => decision switch
    {
        RowDecision.AlreadyImported => "уже в журналі",
        RowDecision.MatchesExisting => "збігається з журналом",
        RowDecision.Differs => "відрізняється — не змінено",
        RowDecision.OutOfScope => "до дати початку",
        RowDecision.Unsupported => "не імпортується",
        _ => "—",
    };
}
