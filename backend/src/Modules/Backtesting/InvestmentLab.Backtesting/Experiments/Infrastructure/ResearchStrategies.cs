using InvestmentLab.Backtesting.Experiments.Domain;
using InvestmentLab.Backtesting.Replays.Domain;
using InvestmentLab.Strategies.Contracts;

namespace InvestmentLab.Backtesting.Experiments.Infrastructure;

internal sealed class ResearchStrategies(IReplayedStrategies setups, IAuthoredStrategies authored) : IResearchStrategies
{
    public async Task<ResearchSource?> FindAsync(Guid strategyId, int version, CancellationToken cancellationToken)
    {
        var setup = await setups.FindAsync(strategyId, version, cancellationToken);
        if (setup is null)
            return null;

        var definition = await authored.GetVersionAsync(strategyId, version, cancellationToken);
        if (definition is null)
            return null;

        return new ResearchSource(setup, definition.Setup.Parameters, definition.TypeCode);
    }

    public async Task<Result<int>> CheckAsync(string typeCode, string parameters, CancellationToken cancellationToken)
    {
        var check = await authored.CheckParametersAsync(typeCode, parameters, cancellationToken);
        if (!check.IsSuccess)
            return ExperimentErrors.ParameterCheckFailed(check.ErrorMessage);

        var problems = check.Value!.Problems.Select(problem => problem.Message).ToList();
        if (problems.Count > 0)
            return ExperimentErrors.ParametersRefused(problems);

        return check.Value.HistoryBars;
    }
}
