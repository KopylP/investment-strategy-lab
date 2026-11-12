using InvestmentLab.Portfolio.Contracts.Accounts;
using InvestmentLab.Taxation.Declarations.Application.Shared;
using InvestmentLab.Taxation.Declarations.Domain;

namespace InvestmentLab.Taxation.Declarations.Application.SetPit8C;

public sealed class SetPit8CHandler(IAccountSetupQueries accounts, IPit8CRepository pit8C) : IHandler
{
    public async Task<Result> Handle(int year, Guid accountId, Pit8CCommand command, CancellationToken cancellationToken)
    {
        if (await accounts.GetAccountSetup(accountId, cancellationToken) is not { } setup)
            return Error.NotFound("account.not_found", $"Account {accountId} does not exist.");

        var ofYear = await pit8C.ForYearAsync(year, cancellationToken);

        if (ofYear.FirstOrDefault(statement => statement.AccountId == accountId) is { } existing)
        {
            var changed = existing.Change(command.Revenue, command.Costs);
            if (changed.IsFailure)
                return changed.Error;
        }
        else
        {
            var recorded = Pit8CStatement.Record(accountId, UserId.Of(setup.OwnerId), year, command.Revenue, command.Costs);
            if (recorded.IsFailure)
                return recorded.Error;

            pit8C.Add(recorded.Value);
        }

        await pit8C.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
