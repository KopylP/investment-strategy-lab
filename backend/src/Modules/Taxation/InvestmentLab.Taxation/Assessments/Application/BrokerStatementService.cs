using InvestmentLab.Taxation.BrokerStatements.Domain;

namespace InvestmentLab.Taxation.Assessments.Application;

/// <param name="From">First day with a sale or a dividend in the report.</param>
public sealed record BrokerStatementDto(Guid Id, string Broker, string FileName, DateTimeOffset UploadedAt, DateOnly From, DateOnly To, int Sales, int Dividends,
    IReadOnlyList<string> Warnings);

/// <summary>The uploads, and how many of their sales and dividends found their entry in the journal (all uploads together).</summary>
public sealed record BrokerStatementsDto(IReadOnlyList<BrokerStatementDto> Statements, int Sales, int MatchedSales, int Dividends, int MatchedDividends);

public sealed record UploadBrokerStatementCommand(string FileName, string ContentBase64);

/// <summary>Upload, list and remove broker tax reports; every change recomputes the account's tax.</summary>
public sealed class BrokerStatementService(
    IBrokerStatementRepository statements, IBrokerStatementParser parser, TaxInputs inputs, TaxRecalculator recalculator, TimeProvider clock)
{
    public async Task<Result<BrokerStatementsDto>> List(Guid accountId, CancellationToken cancellationToken)
    {
        if (await inputs.EnsureAccountAsync(accountId, cancellationToken) is not { } account)
            return Error.NotFound("account.not_found", $"Account {accountId} does not exist.");

        var uploaded = await statements.ForAccountAsync(accountId, cancellationToken);
        var (lots, dividends) = BrokerStatement.Combine(uploaded);
        var events = uploaded.Count > 0 ? (await inputs.EventsAsync(account, cancellationToken)).Events : [];
        return new BrokerStatementsDto(
            [.. uploaded.OrderByDescending(statement => statement.UploadedAt).Select(ToDto)],
            lots.Select(lot => (lot.SoldOn, lot.Isin)).Distinct().Count(),
            events.Count(e => e.Broker is not null),
            dividends.Count,
            events.Count(e => e.Withholding is not null));
    }

    public async Task<Result<BrokerStatementsDto>> Upload(Guid accountId, UploadBrokerStatementCommand command, CancellationToken cancellationToken)
    {
        if (await inputs.EnsureAccountAsync(accountId, cancellationToken) is not { } account)
            return Error.NotFound("account.not_found", $"Account {accountId} does not exist.");

        byte[] content;
        try
        {
            content = Convert.FromBase64String(command.ContentBase64);
        }
        catch (FormatException)
        {
            return BrokerStatementErrors.Unreadable("the file content is not base64");
        }

        var parsed = parser.Parse(content);
        if (parsed.IsFailure)
            return parsed.Error;
        var statement = BrokerStatement.Upload(accountId, account.OwnerId, command.FileName, parsed.Value, clock.GetUtcNow());
        if (statement.IsFailure)
            return statement.Error;

        statements.Add(statement.Value);
        await statements.SaveChangesAsync(cancellationToken);
        await recalculator.RecomputeAsync(accountId, cancellationToken);
        return await List(accountId, cancellationToken);
    }

    public async Task<Result> Remove(Guid accountId, Guid statementId, CancellationToken cancellationToken)
    {
        if ((await statements.ForAccountAsync(accountId, cancellationToken)).FirstOrDefault(statement => statement.Id == statementId) is not { } statement)
            return BrokerStatementErrors.NotFound(statementId);
        statements.Remove(statement);
        await statements.SaveChangesAsync(cancellationToken);
        await recalculator.RecomputeAsync(accountId, cancellationToken);
        return Result.Success();
    }

    private static BrokerStatementDto ToDto(BrokerStatement statement) =>
        new(statement.Id, statement.Broker, statement.FileName, statement.UploadedAt, statement.Span.From, statement.Span.To,
            statement.Lots.Select(lot => (lot.SoldOn, lot.Isin)).Distinct().Count(), statement.Dividends.Count, statement.Warnings);
}
