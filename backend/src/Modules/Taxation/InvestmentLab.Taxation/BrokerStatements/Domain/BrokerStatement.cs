namespace InvestmentLab.Taxation.BrokerStatements.Domain;

/// <summary>
/// One lot closed by a sale, as the broker reports it: amounts in the security's currency and in PLN at the broker's
/// rate. <see cref="Fees"/> are the buy and sell commissions of the lot together.
/// </summary>
public sealed record BrokerSaleLot(
    DateOnly AcquiredOn,
    DateOnly SoldOn,
    string Symbol,
    string Isin,
    string Currency,
    decimal Quantity,
    decimal Cost,
    decimal Proceeds,
    decimal Fees,
    decimal CostPln,
    decimal ProceedsPln,
    decimal FeesPln);

/// <summary>A dividend as the broker reports it: before and after the tax withheld abroad.</summary>
public sealed record BrokerDividend(
    DateOnly PaidOn,
    string Symbol,
    string Isin,
    string Currency,
    decimal Gross,
    decimal Withheld,
    decimal Net,
    decimal GrossPln,
    decimal WithheldPln);

public sealed record ParsedBrokerStatement(string Broker, IReadOnlyList<BrokerSaleLot> Lots, IReadOnlyList<BrokerDividend> Dividends, IReadOnlyList<string> Warnings);

/// <summary>Port: turns a broker's tax report file (Revolut's P&amp;L PDF first) into lots and dividends.</summary>
public interface IBrokerStatementParser
{
    string Broker { get; }

    Result<ParsedBrokerStatement> Parse(byte[] content);
}

/// <summary>
/// A broker's tax report the user uploaded (e.g. Revolut's Profit and Loss Statement): a second source next to the
/// journal for what the journal cannot know — tax withheld from dividends abroad, the cost of shares that came from a
/// corporate action, and the broker's own gain to check ours against. Only the parsed rows are kept, not the file.
/// </summary>
public sealed class BrokerStatement
{
    public Guid Id { get; private set; }
    public Guid AccountId { get; private set; }
    public UserId OwnerId { get; private set; }
    public string Broker { get; private set; }
    public string FileName { get; private set; }
    public DateTimeOffset UploadedAt { get; private set; }
    public List<BrokerSaleLot> Lots { get; private set; }
    public List<BrokerDividend> Dividends { get; private set; }
    public List<string> Warnings { get; private set; }

    private BrokerStatement(Guid accountId, UserId ownerId, string fileName, ParsedBrokerStatement statement, DateTimeOffset uploadedAt)
    {
        Id = Guid.CreateVersion7();
        AccountId = accountId;
        OwnerId = ownerId;
        Broker = statement.Broker;
        FileName = fileName;
        UploadedAt = uploadedAt;
        Lots = [.. statement.Lots];
        Dividends = [.. statement.Dividends];
        Warnings = [.. statement.Warnings];
    }

    // EF Core.
    private BrokerStatement() => (Broker, FileName, Lots, Dividends, Warnings) = (null!, null!, null!, null!, null!);

    public static Result<BrokerStatement> Upload(Guid accountId, UserId ownerId, string fileName, ParsedBrokerStatement statement, DateTimeOffset uploadedAt)
    {
        if (statement.Lots.Count == 0 && statement.Dividends.Count == 0)
            return BrokerStatementErrors.Empty;
        return new BrokerStatement(accountId, ownerId, string.IsNullOrWhiteSpace(fileName) ? "statement.pdf" : fileName.Trim(), statement, uploadedAt);
    }

    /// <summary>The first and last day with a sale or a dividend in the report.</summary>
    public (DateOnly From, DateOnly To) Span
    {
        get
        {
            var days = Lots.Select(lot => lot.SoldOn).Concat(Dividends.Select(dividend => dividend.PaidOn)).ToList();
            return (days.Min(), days.Max());
        }
    }

    /// <summary>
    /// The rows of all uploads of an account, one per real lot or dividend: a later upload (e.g. a longer period) wins
    /// over an earlier one for the rows they share.
    /// </summary>
    public static (IReadOnlyList<BrokerSaleLot> Lots, IReadOnlyList<BrokerDividend> Dividends) Combine(IEnumerable<BrokerStatement> statements)
    {
        var ordered = statements.OrderByDescending(statement => statement.UploadedAt).ToList();
        var lots = ordered.SelectMany(statement => statement.Lots).DistinctBy(lot => (lot.SoldOn, lot.AcquiredOn, lot.Isin, lot.Quantity)).ToList();
        var dividends = ordered.SelectMany(statement => statement.Dividends).DistinctBy(dividend => (dividend.PaidOn, dividend.Isin, dividend.Net)).ToList();
        return (lots, dividends);
    }
}

public interface IBrokerStatementRepository
{
    Task<IReadOnlyList<BrokerStatement>> ForAccountAsync(Guid accountId, CancellationToken cancellationToken);

    void Add(BrokerStatement statement);

    void Remove(BrokerStatement statement);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public static class BrokerStatementErrors
{
    public static readonly Error Empty =
        Error.Validation("broker_statement.empty", "No sold lots or dividends were found: the file does not look like a Revolut Profit and Loss Statement (PDF).");

    public static Error Unreadable(string details) => Error.Validation("broker_statement.unreadable", $"The PDF could not be read: {details}");

    public static Error NotFound(Guid statementId) => Error.NotFound("broker_statement.not_found", $"Broker statement {statementId} does not exist.");
}
