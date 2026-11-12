using InvestmentLab.Portfolio.Contracts.Accounts;
using InvestmentLab.Rebalancing.Planning.Application.Shared;
using InvestmentLab.Rebalancing.Planning.Domain;

namespace InvestmentLab.Rebalancing.Planning.Application.GetPlanSettings;

public sealed class GetPlanSettingsHandler(IAccountSetupQueries accounts, IPlanSettingsRepository settings) : IHandler
{
    public async Task<Result<PlanSettingsDto>> Handle(Guid accountId, CancellationToken cancellationToken)
    {
        if (await accounts.GetAccountSetup(accountId, cancellationToken) is not { } account)
            return Error.NotFound("account.not_found", $"Account {accountId} does not exist.");

        var current = await settings.FindAsync(accountId, cancellationToken);

        return PlanSettingsDto.Of(current ?? PlanSettings.DefaultFor(accountId, UserId.Of(account.OwnerId)));
    }
}
