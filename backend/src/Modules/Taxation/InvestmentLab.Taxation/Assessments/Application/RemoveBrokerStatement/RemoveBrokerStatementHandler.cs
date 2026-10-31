using InvestmentLab.Taxation.Assessments.Application.Shared;
using InvestmentLab.Taxation.BrokerStatements.Domain;

namespace InvestmentLab.Taxation.Assessments.Application.RemoveBrokerStatement;

public sealed class RemoveBrokerStatementHandler(IBrokerStatementRepository statements, TaxRecalculator recalculator)
{
    public async Task<Result> Handle(Guid accountId, Guid statementId, CancellationToken cancellationToken)
    {
        var uploaded = await statements.ForAccountAsync(accountId, cancellationToken);
        if (uploaded.FirstOrDefault(statement => statement.Id == statementId) is not { } statement)
            return BrokerStatementErrors.NotFound(statementId);

        statements.Remove(statement);
        await statements.SaveChangesAsync(cancellationToken);
        await recalculator.RecomputeAsync(accountId, cancellationToken);

        return Result.Success();
    }
}
