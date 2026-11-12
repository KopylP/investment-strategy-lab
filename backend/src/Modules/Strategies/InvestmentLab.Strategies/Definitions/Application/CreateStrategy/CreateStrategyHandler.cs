using InvestmentLab.Strategies.Definitions.Application.Shared;
using InvestmentLab.Strategies.Definitions.Domain;

namespace InvestmentLab.Strategies.Definitions.Application.CreateStrategy;

public sealed record CreateStrategyCommand(string Name, string TypeCode, StrategySetupDraft Setup);

public sealed class CreateStrategyHandler(IStrategyRepository strategies, IStrategyTypes types, StrategySetupFactory setups, TimeProvider clock) : IHandler
{
    public async Task<Result<StrategyDto>> Handle(UserId ownerId, CreateStrategyCommand command, CancellationToken cancellationToken)
    {
        if (await strategies.NameTakenAsync(ownerId, command.Name?.Trim() ?? "", cancellationToken))
            return StrategyErrors.NameTaken(command.Name!.Trim());

        var setup = await setups.CreateAsync(command.TypeCode ?? "", command.Setup, cancellationToken);
        if (setup.IsFailure)
            return setup.Error;

        var created = Strategy.Create(ownerId, command.Name ?? "", command.TypeCode!, setup.Value, clock.GetUtcNow());
        if (created.IsFailure)
            return created.Error;

        strategies.Add(created.Value);
        await strategies.SaveChangesAsync(cancellationToken);

        var type = await types.FindAsync(created.Value.TypeCode, cancellationToken);

        return created.Value.ToDto(type);
    }
}
