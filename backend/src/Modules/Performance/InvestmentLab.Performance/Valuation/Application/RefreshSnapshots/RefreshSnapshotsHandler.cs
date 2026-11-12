using InvestmentLab.SharedKernel;
using InvestmentLab.Performance.Valuation.Application.Shared;
using InvestmentLab.Portfolio.Contracts.Accounts;

namespace InvestmentLab.Performance.Valuation.Application.RefreshSnapshots;

public sealed record RefreshSnapshotsCommand(Guid? AccountId);

public sealed class RefreshSnapshotsHandler(SnapshotBuilder builder, IAccountSetupQueries accounts) : IHandler
{
    public async Task Handle(RefreshSnapshotsCommand command, CancellationToken cancellationToken)
    {
        var accountIds = await AccountsToRefreshAsync(command.AccountId, cancellationToken);

        foreach (var accountId in accountIds)
            await builder.EnsureFreshAsync(accountId, cancellationToken);
    }

    private async Task<IReadOnlyList<Guid>> AccountsToRefreshAsync(Guid? accountId, CancellationToken cancellationToken)
    {
        if (accountId is null)
            return [.. (await accounts.GetAccountSetups(cancellationToken)).Select(account => account.Id)];

        var account = await accounts.GetAccountSetup(accountId.Value, cancellationToken);

        return account is null ? [] : [account.Id];
    }
}
