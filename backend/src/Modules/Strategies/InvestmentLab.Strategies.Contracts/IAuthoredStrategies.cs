namespace InvestmentLab.Strategies.Contracts;

public interface IAuthoredStrategies
{
    Task<AuthoringResult<TrialVersionDto>> WriteTrialVersionAsync(TrialVersionRequest request, CancellationToken cancellationToken);

    Task<AuthoringResult<bool>> DeleteTrialAsync(Guid strategyId, CancellationToken cancellationToken);

    Task<AuthoringResult<PublishedVersionDto>> PublishAsync(PublishRequest request, CancellationToken cancellationToken);

    Task<AuthoringResult<ParametersCheckDto>> CheckParametersAsync(string typeCode, string parameters, CancellationToken cancellationToken);
}
