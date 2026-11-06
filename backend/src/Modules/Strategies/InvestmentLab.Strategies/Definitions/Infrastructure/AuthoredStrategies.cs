using InvestmentLab.Strategies.Definitions.Application.GetAuthoredVersion;
using System.Text.Json;
using InvestmentLab.Strategies.Contracts;
using InvestmentLab.Strategies.Definitions.Application.CheckParameters;
using InvestmentLab.Strategies.Definitions.Application.DeleteTrialStrategy;
using InvestmentLab.Strategies.Definitions.Application.PublishStrategy;
using InvestmentLab.Strategies.Definitions.Application.WriteTrialVersion;
using InvestmentLab.Strategies.Definitions.Domain;

namespace InvestmentLab.Strategies.Definitions.Infrastructure;

internal sealed class AuthoredStrategies(
    GetAuthoredVersionHandler getVersion,
    WriteTrialVersionHandler writeTrial,
    DeleteTrialStrategyHandler deleteTrial,
    PublishStrategyHandler publish,
    CheckParametersHandler checkParameters) : IAuthoredStrategies
{
    public Task<AuthoredVersionDto?> GetVersionAsync(Guid strategyId, int version, CancellationToken cancellationToken) =>
        getVersion.Handle(strategyId, version, cancellationToken);

    private const string ParametersRefusedCode = "strategy.parameters_refused";

    public async Task<AuthoringResult<TrialVersionDto>> WriteTrialVersionAsync(TrialVersionRequest request, CancellationToken cancellationToken) =>
        ToAuthoring(await writeTrial.Handle(request, cancellationToken));

    public async Task<AuthoringResult<bool>> DeleteTrialAsync(Guid strategyId, CancellationToken cancellationToken)
    {
        var deleted = await deleteTrial.Handle(strategyId, cancellationToken);

        return deleted.IsSuccess ? new AuthoringResult<bool>(true) : Failure<bool>(deleted.Error);
    }

    public async Task<AuthoringResult<PublishedVersionDto>> PublishAsync(PublishRequest request, CancellationToken cancellationToken) =>
        ToAuthoring(await publish.Handle(request, cancellationToken));

    public async Task<AuthoringResult<ParametersCheckDto>> CheckParametersAsync(string typeCode, string parameters, CancellationToken cancellationToken)
    {
        var check = await checkParameters.Handle(typeCode, parameters, cancellationToken);
        if (check.IsFailure)
            return Failure<ParametersCheckDto>(check.Error);

        var problems = check.Value.Problems.Select(ToDto).ToList();

        return new AuthoringResult<ParametersCheckDto>(new ParametersCheckDto(problems, check.Value.SignalIds, check.Value.HistoryBars));
    }

    private static AuthoringResult<T> ToAuthoring<T>(Result<T> result) =>
        result.IsSuccess ? new AuthoringResult<T>(result.Value) : Failure<T>(result.Error);

    private static AuthoringResult<T> Failure<T>(Error error) =>
        new(default, error.Code, error.AsMessage(), error.Code == ParametersRefusedCode ? ProblemsOf(error) : null);

    private static List<ParameterProblemDto> ProblemsOf(Error error)
    {
        var parameters = JsonSerializer.SerializeToElement(error.Parameters, JsonSerializerOptions.Web);
        var problems = parameters.GetProperty("problems").Deserialize<List<ParameterProblemDto>>(JsonSerializerOptions.Web);

        return problems ?? [];
    }

    private static ParameterProblemDto ToDto(ParameterProblem problem) => new(problem.Path, problem.Code, problem.Message);
}
