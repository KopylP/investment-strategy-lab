using InvestmentLab.Portfolio.Accounts.Domain;
using InvestmentLab.Portfolio.Contracts.Accounts;

namespace InvestmentLab.Portfolio.Accounts.Application.AssignStrategy;

public sealed record AssignStrategyCommand(Guid? StrategyId, int? PinnedVersion);

public sealed class AssignStrategyHandler(IAccountRepository accounts)
{
    public async Task<Result<AccountSetupDto>> Handle(Guid accountId, Guid subPortfolioId, AssignStrategyCommand command, CancellationToken cancellationToken)
    {
        if (accountId == Guid.Empty || await accounts.FindAsync(AccountId.Of(accountId), cancellationToken) is not { } account)
            return AccountErrors.NotFound(accountId);

        if (subPortfolioId == Guid.Empty)
            return AccountErrors.SubPortfolioNotFound(subPortfolioId);

        StrategyRef? strategy = null;

        if (command.StrategyId is { } strategyId)
        {
            var reference = StrategyRef.TryOf(strategyId, command.PinnedVersion);
            if (reference.IsFailure)
                return reference.Error;

            strategy = reference.Value;
        }

        var assigned = account.AssignStrategy(SubPortfolioId.Of(subPortfolioId), strategy);
        if (assigned.IsFailure)
            return assigned.Error;

        await accounts.SaveChangesAsync(cancellationToken);

        return account.ToSetupDto();
    }
}
