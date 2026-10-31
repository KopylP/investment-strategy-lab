using InvestmentLab.Rebalancing.Planning.Domain;

namespace InvestmentLab.Rebalancing.Planning.Application;

public sealed class GetPlanSettingsHandler(PlanningInputs inputs, IPlanSettingsRepository settings)
{
    public async Task<Result<PlanSettingsDto>> Handle(Guid accountId, CancellationToken cancellationToken)
    {
        if (await inputs.AccountAsync(accountId, cancellationToken) is not { } account)
            return Error.NotFound("account.not_found", $"Account {accountId} does not exist.");

        var current = await settings.FindAsync(accountId, cancellationToken);

        return PlanSettingsDto.Of(current ?? PlanSettings.DefaultFor(accountId, UserId.Of(account.OwnerId)));
    }
}
