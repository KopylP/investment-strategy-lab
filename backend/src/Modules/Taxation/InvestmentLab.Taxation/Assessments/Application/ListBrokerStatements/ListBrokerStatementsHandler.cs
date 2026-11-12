using InvestmentLab.Taxation.Assessments.Application.Shared;
using InvestmentLab.Taxation.BrokerStatements.Domain;

namespace InvestmentLab.Taxation.Assessments.Application.ListBrokerStatements;

public sealed class ListBrokerStatementsHandler(IBrokerStatementRepository statements, TaxAccountSync sync, TaxEventReader taxEvents) : IHandler
{
    public async Task<Result<BrokerStatementsDto>> Handle(Guid accountId, CancellationToken cancellationToken)
    {
        if (await sync.EnsureAccountAsync(accountId, cancellationToken) is not { } account)
            return Error.NotFound("account.not_found", $"Account {accountId} does not exist.");

        var uploaded = await statements.ForAccountAsync(accountId, cancellationToken);
        var (lots, dividends) = BrokerStatement.Combine(uploaded);
        var events = uploaded.Count > 0 ? (await taxEvents.EventsAsync(account, cancellationToken)).Events : [];

        return new BrokerStatementsDto(
            [.. uploaded.OrderByDescending(statement => statement.UploadedAt).Select(ToDto)],
            lots.Select(lot => (lot.SoldOn, lot.Isin)).Distinct().Count(),
            events.Count(e => e.Broker is not null),
            dividends.Count,
            events.Count(e => e.Withholding is not null));
    }

    private static BrokerStatementDto ToDto(BrokerStatement statement)
    {
        var salesCount = statement.Lots.Select(lot => (lot.SoldOn, lot.Isin)).Distinct().Count();

        return new BrokerStatementDto(
            statement.Id,
            statement.Broker,
            statement.FileName,
            statement.UploadedAt,
            statement.Span.From,
            statement.Span.To,
            salesCount,
            statement.Dividends.Count,
            statement.Warnings);
    }
}
