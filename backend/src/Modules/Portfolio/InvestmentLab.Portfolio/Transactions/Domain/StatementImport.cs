using InvestmentLab.Portfolio.Accounts.Domain;

namespace InvestmentLab.Portfolio.Transactions.Domain;

public enum StatementRowKind
{
    Buy,
    Sell,
    Contribution,
    Withdrawal,
    Dividend,

    /// <summary>A row the import does not understand (splits, custody fees…): shown, never recorded.</summary>
    Unsupported,
}

/// <summary>
/// One line of a broker statement, as the broker wrote it (amounts in the row currency, always positive).
/// <see cref="Identity"/> is stable for the same line in any later statement: broker, exact time, type and ticker.
/// </summary>
public sealed record StatementRow(
    int Number,
    DateTimeOffset OccurredAt,
    StatementRowKind Kind,
    string RawType,
    string? Ticker,
    decimal? Quantity,
    decimal? Price,
    decimal Amount,
    string Currency,
    string Identity)
{
    public DateOnly OccurredOn => DateOnly.FromDateTime(OccurredAt.UtcDateTime);
    public bool IsTrade => Kind is StatementRowKind.Buy or StatementRowKind.Sell;
    public bool NeedsInstrument => Kind is StatementRowKind.Buy or StatementRowKind.Sell or StatementRowKind.Dividend;
}

public sealed record ParsedStatement(string Broker, IReadOnlyList<StatementRow> Rows, IReadOnlyList<string> Warnings);

/// <summary>Port: turns a broker's statement file into rows (an anti-corruption layer per broker).</summary>
public interface IStatementParser
{
    string Broker { get; }

    Result<ParsedStatement> Parse(string content);
}

public enum ImportMode
{
    /// <summary>Only rows not imported yet (the default; a repeated upload changes nothing).</summary>
    Merge,

    /// <summary>Like merge, and rows imported before whose numbers changed are re-recorded from the statement.</summary>
    UpdateImported,

    /// <summary>Every earlier statement and positions.json import is reversed, then the statement is imported anew.</summary>
    Replace,

    /// <summary>The whole journal of the account is reversed, then the statement is imported.</summary>
    FromScratch,
}

public enum RowDecision
{
    /// <summary>Recorded as a new entry.</summary>
    New,

    /// <summary>Already in the journal with the same numbers.</summary>
    AlreadyImported,

    /// <summary>Imported before, numbers differ: re-recorded only in <see cref="ImportMode.UpdateImported"/> and the replacing modes.</summary>
    Differs,

    /// <summary>The same real trade as an entry marked in a report or recorded by hand: the statement wins, the entry is corrected if needed.</summary>
    MatchesExisting,

    /// <summary>Before the chosen start date.</summary>
    OutOfScope,

    Unsupported,

    /// <summary>The ticker is not mapped to an instrument or sub-portfolio yet.</summary>
    NeedsMapping,
}

/// <summary>Where a ticker of the statement goes: an instrument of the catalog (or a new one) and a sub-portfolio.</summary>
/// <param name="InstrumentId">Null: not chosen yet; with <paramref name="RegisterAs"/> a new instrument is created on confirmation.</param>
/// <param name="RegisterAs">Yahoo symbol for a new instrument (e.g. <c>AAPL</c>, <c>QDVE.DE</c>).</param>
public sealed record TickerMapping(string Ticker, string Currency, Guid? InstrumentId, string? RegisterAs, Guid? SubPortfolioId);

/// <summary>What the import plans for one row, and after confirmation what happened.</summary>
public sealed record RowOutcome(int Number, RowDecision Decision, string? Note, Guid? ExistingTransactionId, bool WillRecord, bool WillReverseExisting, string? Result);

public enum StatementImportStatus
{
    Preview,
    Confirmed,
    Undone,
}

/// <summary>
/// One upload of a broker statement (Revolut first): preview → confirmed → undone. Keeps the rows, the user's decisions
/// and what was written, so the whole batch can be taken back with one click.
/// </summary>
public sealed class StatementImport
{
    public Guid Id { get; private set; }
    public AccountId AccountId { get; private set; }
    public UserId OwnerId { get; private set; }
    public string Broker { get; private set; }
    public string FileName { get; private set; }
    public DateTimeOffset UploadedAt { get; private set; }
    public StatementImportStatus Status { get; private set; }
    public ImportMode Mode { get; private set; }
    public DateOnly? FromDate { get; private set; }
    public Guid? CashSubPortfolioId { get; private set; }
    public List<StatementRow> Rows { get; private set; }
    public List<TickerMapping> Mappings { get; private set; }
    public List<string> Warnings { get; private set; }
    public List<RowOutcome> Outcomes { get; private set; }
    public List<string> RecordedReferences { get; private set; }
    public List<Guid> ReversedTransactionIds { get; private set; }
    public DateTimeOffset? ConfirmedAt { get; private set; }
    public DateTimeOffset? UndoneAt { get; private set; }

    private StatementImport(AccountId accountId, UserId ownerId, string fileName, ParsedStatement statement, DateTimeOffset uploadedAt)
    {
        Id = Guid.CreateVersion7();
        AccountId = accountId;
        OwnerId = ownerId;
        Broker = statement.Broker;
        FileName = fileName;
        UploadedAt = uploadedAt;
        Status = StatementImportStatus.Preview;
        Mode = ImportMode.Merge;
        Rows = [.. statement.Rows];
        Mappings = [];
        Warnings = [.. statement.Warnings];
        Outcomes = [];
        RecordedReferences = [];
        ReversedTransactionIds = [];
    }

    // EF Core.
    private StatementImport() => (Broker, FileName, Rows, Mappings, Warnings, Outcomes, RecordedReferences, ReversedTransactionIds) = (null!, null!, null!, null!, null!, null!, null!, null!);

    public static StatementImport Upload(AccountId accountId, UserId ownerId, string fileName, ParsedStatement statement, DateTimeOffset uploadedAt) =>
        new(accountId, ownerId, string.IsNullOrWhiteSpace(fileName) ? "statement.csv" : fileName.Trim(), statement, uploadedAt);

    public bool IsPreview => Status == StatementImportStatus.Preview;

    public Result Configure(ImportMode mode, DateOnly? fromDate, Guid? cashSubPortfolioId, IReadOnlyList<TickerMapping> mappings)
    {
        if (!IsPreview)
            return StatementImportErrors.NotInPreview(Id);
        Mode = mode;
        FromDate = fromDate;
        CashSubPortfolioId = cashSubPortfolioId;
        Mappings = [.. mappings];
        return Result.Success();
    }

    public void Plan(IReadOnlyList<RowOutcome> outcomes) => Outcomes = [.. outcomes];

    public Result Confirm(IReadOnlyList<RowOutcome> outcomes, IReadOnlyList<string> recordedReferences, IReadOnlyList<Guid> reversed, DateTimeOffset now)
    {
        if (!IsPreview)
            return StatementImportErrors.NotInPreview(Id);
        Outcomes = [.. outcomes];
        RecordedReferences = [.. recordedReferences];
        ReversedTransactionIds = [.. reversed];
        Status = StatementImportStatus.Confirmed;
        ConfirmedAt = now;
        return Result.Success();
    }

    public Result Undo(DateTimeOffset now)
    {
        if (Status != StatementImportStatus.Confirmed)
            return StatementImportErrors.NotConfirmed(Id);
        Status = StatementImportStatus.Undone;
        UndoneAt = now;
        return Result.Success();
    }

    public TickerMapping? MappingOf(string? ticker) =>
        ticker is null ? null : Mappings.FirstOrDefault(mapping => string.Equals(mapping.Ticker, ticker, StringComparison.OrdinalIgnoreCase));
}

public static class StatementImportErrors
{
    public static Error NotFound(Guid importId) => Error.NotFound("statement_import.not_found", $"Import {importId} does not exist.");

    public static Error NotInPreview(Guid importId) => Error.Conflict("statement_import.not_in_preview", $"Import {importId} is already confirmed or undone.");

    public static Error NotConfirmed(Guid importId) => Error.Conflict("statement_import.not_confirmed", $"Import {importId} was not confirmed.");

    public static Error UnknownFormat(string details) => Error.Validation("statement_import.unknown_format", $"The file is not a Revolut statement: {details}");

    public static Error Unmapped(IEnumerable<string> tickers) =>
        Error.Validation("statement_import.unmapped", $"Choose an instrument and a sub-portfolio for: {string.Join(", ", tickers)}.");
}
