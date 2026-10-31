using InvestmentLab.Rebalancing.Planning.Domain;

namespace InvestmentLab.Rebalancing.Planning.Application;

public sealed class ChangePlanSettingsHandler(PlanningInputs inputs, IPlanSettingsRepository settings, TimeProvider clock)
{
    public async Task<Result<PlanSettingsDto>> Handle(Guid accountId, ChangePlanSettingsCommand command, CancellationToken cancellationToken)
    {
        if (await inputs.AccountAsync(accountId, cancellationToken) is not { } account)
            return Error.NotFound("account.not_found", $"Account {accountId} does not exist.");

        var policy = RebalancingPolicy.TryOf(command.MinTrade, command.TrimBand, command.AllowFractionalShares);
        if (policy.IsFailure)
            return policy.Error;

        var current = await FindOrAddAsync(accountId, UserId.Of(account.OwnerId), cancellationToken);

        var changed = current.Change(command.MonthlyContribution, policy.Value, command.GenerateOnSchedule, clock.GetUtcNow());
        if (changed.IsFailure)
            return changed.Error;

        await settings.SaveChangesAsync(cancellationToken);

        return PlanSettingsDto.Of(current);
    }

    private async Task<PlanSettings> FindOrAddAsync(Guid accountId, UserId ownerId, CancellationToken cancellationToken)
    {
        var existing = await settings.FindAsync(accountId, cancellationToken);
        if (existing is not null)
            return existing;

        var created = PlanSettings.DefaultFor(accountId, ownerId);
        settings.Add(created);

        return created;
    }
}
