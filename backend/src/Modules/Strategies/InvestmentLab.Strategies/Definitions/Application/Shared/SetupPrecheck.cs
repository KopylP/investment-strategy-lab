using InvestmentLab.Strategies.Definitions.Domain;

namespace InvestmentLab.Strategies.Definitions.Application.Shared;

public static class SetupPrecheck
{
    public static async Task<Error?> ProblemsOfAsync(StrategySetupFactory setups, string typeCode, StrategySetupDraft draft, CancellationToken cancellationToken)
    {
        var check = await setups.CheckAsync(typeCode, draft.Parameters, cancellationToken);
        if (check.IsFailure)
            return check.Error;

        return check.Value.IsClean ? null : StrategyErrors.ParametersRefused(check.Value.Problems);
    }
}
