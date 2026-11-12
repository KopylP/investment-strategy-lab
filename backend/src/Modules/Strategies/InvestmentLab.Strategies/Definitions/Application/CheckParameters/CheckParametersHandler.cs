using InvestmentLab.SharedKernel;
using System.Text.Json;
using InvestmentLab.Strategies.Contracts;
using InvestmentLab.Strategies.Definitions.Application.Shared;
using InvestmentLab.Strategies.Definitions.Domain;

namespace InvestmentLab.Strategies.Definitions.Application.CheckParameters;

public sealed class CheckParametersHandler(StrategySetupFactory setups) : IHandler
{
    public async Task<Result<ParametersCheckDto>> Handle(string typeCode, string parameters, CancellationToken cancellationToken)
    {
        var given = JsonDocument.Parse(parameters).RootElement;

        var check = await setups.CheckAsync(typeCode, given, cancellationToken);
        if (check.IsFailure)
            return check.Error;

        return ToDto(check.Value);
    }

    private static ParametersCheckDto ToDto(ParametersCheck check)
    {
        var problems = check.Problems.Select(ToDto).ToList();

        return new ParametersCheckDto(problems, check.SignalIds, check.HistoryBars);
    }

    private static ParameterProblemDto ToDto(ParameterProblem problem) => new(problem.Path, problem.Code, problem.Message);
}
