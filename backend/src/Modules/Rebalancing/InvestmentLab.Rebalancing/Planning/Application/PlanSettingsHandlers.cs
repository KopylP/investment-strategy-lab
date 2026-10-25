using InvestmentLab.Rebalancing.Planning.Domain;

namespace InvestmentLab.Rebalancing.Planning.Application;

public sealed record PlanSettingsDto(
    Guid AccountId, decimal MonthlyContribution, decimal MinTrade, decimal TrimBand, bool AllowFractionalShares, bool GenerateOnSchedule);

/// <param name="TrimBand">Fraction 0..1.</param>
public sealed record ChangePlanSettingsCommand(
    decimal MonthlyContribution, decimal MinTrade, decimal TrimBand, bool AllowFractionalShares, bool GenerateOnSchedule = true);

/// <summary>Read and change how an account's plan is made; an account without settings gets the defaults.</summary>
public sealed class PlanSettingsHandler(PlanningInputs inputs, IPlanSettingsRepository settings, TimeProvider clock)
{
    public async Task<Result<PlanSettingsDto>> Get(Guid accountId, CancellationToken cancellationToken)
    {
        if (await inputs.AccountAsync(accountId, cancellationToken) is not { } account)
            return Error.NotFound("account.not_found", $"Account {accountId} does not exist.");

        var current = await settings.FindAsync(accountId, cancellationToken) ?? PlanSettings.DefaultFor(accountId, UserId.Of(account.OwnerId));
        return ToDto(current);
    }

    public async Task<Result<PlanSettingsDto>> Change(Guid accountId, ChangePlanSettingsCommand command, CancellationToken cancellationToken)
    {
        if (await inputs.AccountAsync(accountId, cancellationToken) is not { } account)
            return Error.NotFound("account.not_found", $"Account {accountId} does not exist.");

        var policy = RebalancingPolicy.TryOf(command.MinTrade, command.TrimBand, command.AllowFractionalShares);
        if (policy.IsFailure)
            return policy.Error;

        var current = await settings.FindAsync(accountId, cancellationToken);
        if (current is null)
        {
            current = PlanSettings.DefaultFor(accountId, UserId.Of(account.OwnerId));
            settings.Add(current);
        }

        var changed = current.Change(command.MonthlyContribution, policy.Value, command.GenerateOnSchedule, clock.GetUtcNow());
        if (changed.IsFailure)
            return changed.Error;

        await settings.SaveChangesAsync(cancellationToken);
        return ToDto(current);
    }

    private static PlanSettingsDto ToDto(PlanSettings settings) => new(
        settings.AccountId, settings.MonthlyContribution, settings.MinTrade, settings.TrimBand.Value, settings.AllowFractionalShares, settings.GenerateOnSchedule);
}
