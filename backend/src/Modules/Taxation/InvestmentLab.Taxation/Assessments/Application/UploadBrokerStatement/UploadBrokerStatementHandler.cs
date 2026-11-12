using InvestmentLab.SharedKernel;
using InvestmentLab.Taxation.Assessments.Application.ListBrokerStatements;
using InvestmentLab.Taxation.Assessments.Application.Shared;
using InvestmentLab.Taxation.BrokerStatements.Domain;

namespace InvestmentLab.Taxation.Assessments.Application.UploadBrokerStatement;

public sealed class UploadBrokerStatementHandler(
    IBrokerStatementRepository statements,
    IBrokerStatementParser parser,
    TaxAccountSync sync,
    TaxRecalculator recalculator,
    ListBrokerStatementsHandler list,
    TimeProvider clock) : IHandler
{
    public async Task<Result<BrokerStatementsDto>> Handle(Guid accountId, UploadBrokerStatementCommand command, CancellationToken cancellationToken)
    {
        if (await sync.EnsureAccountAsync(accountId, cancellationToken) is not { } account)
            return Error.NotFound("account.not_found", $"Account {accountId} does not exist.");

        var content = Decode(command.ContentBase64);
        if (content.IsFailure)
            return content.Error;

        var parsed = parser.Parse(content.Value);
        if (parsed.IsFailure)
            return parsed.Error;

        var statement = BrokerStatement.Upload(accountId, account.OwnerId, command.FileName, parsed.Value, clock.GetUtcNow());
        if (statement.IsFailure)
            return statement.Error;

        statements.Add(statement.Value);
        await statements.SaveChangesAsync(cancellationToken);
        await recalculator.RecomputeAsync(accountId, cancellationToken);

        return await list.Handle(accountId, cancellationToken);
    }

    private static Result<byte[]> Decode(string contentBase64)
    {
        try
        {
            return Convert.FromBase64String(contentBase64);
        }
        catch (FormatException)
        {
            return BrokerStatementErrors.Unreadable("the file content is not base64");
        }
    }
}
