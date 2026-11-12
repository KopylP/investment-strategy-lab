using InvestmentLab.Strategies.Contracts;
using InvestmentLab.Strategies.Definitions.Application.Shared;
using InvestmentLab.Strategies.Definitions.Domain;

namespace InvestmentLab.Strategies.Definitions.Application.WriteTrialVersion;

public sealed class WriteTrialVersionHandler(IStrategyRepository strategies, StrategySetupFactory setups, TimeProvider clock) : IHandler
{
    public async Task<Result<TrialVersionDto>> Handle(TrialVersionRequest request, CancellationToken cancellationToken)
    {
        var draft = AuthoredSetupMapping.ToDraft(request.Setup);
        if (await SetupPrecheck.ProblemsOfAsync(setups, request.Setup.TypeCode, draft, cancellationToken) is { } refused)
            return refused;

        var setup = await setups.CreateAsync(request.Setup.TypeCode, draft, cancellationToken);
        if (setup.IsFailure)
            return setup.Error;

        var written = request.TrialStrategyId is { } trialId
            ? await NextVersionAsync(trialId, setup.Value, cancellationToken)
            : FirstVersion(request, setup.Value);
        if (written.IsFailure)
            return written.Error;

        await strategies.SaveChangesAsync(cancellationToken);

        return new TrialVersionDto(written.Value.Id.Value, written.Value.Latest.Number);
    }

    private Result<Strategy> FirstVersion(TrialVersionRequest request, StrategySetup setup)
    {
        var created = Strategy.CreateTrial(UserId.Of(request.OwnerId), request.Setup.TypeCode, setup, clock.GetUtcNow());
        if (created.IsSuccess)
            strategies.Add(created.Value);

        return created;
    }

    private async Task<Result<Strategy>> NextVersionAsync(Guid trialId, StrategySetup setup, CancellationToken cancellationToken)
    {
        if (trialId == Guid.Empty || await strategies.FindAsync(StrategyId.Of(trialId), cancellationToken) is not { } trial)
            return StrategyErrors.NotFound(trialId);

        var version = trial.ReconfigureTrial(setup, clock.GetUtcNow());

        return version.IsFailure ? version.Error : trial;
    }
}
