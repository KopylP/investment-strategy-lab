using InvestmentLab.Strategies.Contracts;
using InvestmentLab.StrategyBuilder.Drafts.Domain;

namespace InvestmentLab.StrategyBuilder.Drafts.Infrastructure;

internal sealed class StrategyAuthoring(IAuthoredStrategies strategies) : IStrategyAuthoring
{
    public async Task<Result<RulesCheck>> CheckAsync(string rules, CancellationToken cancellationToken)
    {
        var check = await strategies.CheckParametersAsync(RuleBasedType.Code, rules, cancellationToken);
        if (!check.IsSuccess)
            return Failure(check);

        var problems = check.Value!.Problems.Select(problem => new EngineProblem(problem.Path, problem.Code, problem.Message)).ToList();

        return new RulesCheck(problems, check.Value.SignalIds);
    }

    private static Error Failure<T>(AuthoringResult<T> result) =>
        Error.Conflict(result.ErrorCode!, result.ErrorMessage ?? result.ErrorCode!);
}
