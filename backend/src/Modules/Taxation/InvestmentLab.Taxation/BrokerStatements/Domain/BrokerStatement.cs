namespace InvestmentLab.Taxation.BrokerStatements.Domain;

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

    private BrokerStatement() => (Broker, FileName, Lots, Dividends, Warnings) = (null!, null!, null!, null!, null!);

    public static Result<BrokerStatement> Upload(
        Guid accountId,
        UserId ownerId,
        string fileName,
        ParsedBrokerStatement statement,
        DateTimeOffset uploadedAt)
    {
        if (statement.Lots.Count == 0 && statement.Dividends.Count == 0)
            return BrokerStatementErrors.Empty;

        return new BrokerStatement(accountId, ownerId, string.IsNullOrWhiteSpace(fileName) ? "statement.pdf" : fileName.Trim(), statement, uploadedAt);
    }

    public (DateOnly From, DateOnly To) Span
    {
        get
        {
            var days = Lots.Select(lot => lot.SoldOn).Concat(Dividends.Select(dividend => dividend.PaidOn)).ToList();

            return (days.Min(), days.Max());
        }
    }

    public static (IReadOnlyList<BrokerSaleLot> Lots, IReadOnlyList<BrokerDividend> Dividends) Combine(IEnumerable<BrokerStatement> statements)
    {
        var ordered = statements.OrderByDescending(statement => statement.UploadedAt).ToList();
        var lots = ordered.SelectMany(statement => statement.Lots).DistinctBy(lot => (lot.SoldOn, lot.AcquiredOn, lot.Isin, lot.Quantity)).ToList();
        var dividends = ordered.SelectMany(statement => statement.Dividends).DistinctBy(dividend => (dividend.PaidOn, dividend.Isin, dividend.Net)).ToList();

        return (lots, dividends);
    }
}
