using InvestmentLab.Portfolio.Accounts.Domain;

namespace InvestmentLab.Portfolio.Transactions.Domain;

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
