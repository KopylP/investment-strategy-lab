using InvestmentLab.Strategies.Contracts;
using InvestmentLab.Strategies.Definitions.Application.Shared;
using InvestmentLab.Strategies.Definitions.Domain;

namespace InvestmentLab.Strategies.Definitions.Application.PublishStrategy;

public sealed class PublishStrategyHandler(IStrategyRepository strategies, StrategySetupFactory setups, TimeProvider clock)
{
    public async Task<Result<PublishedVersionDto>> Handle(PublishRequest request, CancellationToken cancellationToken)
    {
        var draft = AuthoredSetupMapping.ToDraft(request.Setup);
        if (await SetupPrecheck.ProblemsOfAsync(setups, request.Setup.TypeCode, draft, cancellationToken) is { } refused)
            return refused;

        var setup = await setups.CreateAsync(request.Setup.TypeCode, draft, cancellationToken);
        if (setup.IsFailure)
            return setup.Error;

        var published = request.StrategyId is { } strategyId
            ? await NewVersionAsync(strategyId, request.Name, setup.Value, cancellationToken)
            : await NewStrategyAsync(request, setup.Value, cancellationToken);
        if (published.IsFailure)
            return published.Error;

        await strategies.SaveChangesAsync(cancellationToken);

        return new PublishedVersionDto(published.Value.Id.Value, published.Value.Latest.Number);
    }

    private async Task<Result<Strategy>> NewStrategyAsync(PublishRequest request, StrategySetup setup, CancellationToken cancellationToken)
    {
        var owner = UserId.Of(request.OwnerId);
        var name = request.Name?.Trim() ?? "";
        if (await strategies.NameTakenAsync(owner, name, cancellationToken))
            return StrategyErrors.NameTaken(name);

        var created = Strategy.Create(owner, name, request.Setup.TypeCode, setup, clock.GetUtcNow());
        if (created.IsSuccess)
            strategies.Add(created.Value);

        return created;
    }

    private async Task<Result<Strategy>> NewVersionAsync(Guid strategyId, string name, StrategySetup setup, CancellationToken cancellationToken)
    {
        if (strategyId == Guid.Empty || await strategies.FindAsync(StrategyId.Of(strategyId), cancellationToken) is not { } strategy)
            return StrategyErrors.NotFound(strategyId);

        var version = strategy.Reconfigure(setup, clock.GetUtcNow());
        if (version.IsFailure)
            return version.Error;

        strategy.Rename(name);

        return strategy;
    }
}
