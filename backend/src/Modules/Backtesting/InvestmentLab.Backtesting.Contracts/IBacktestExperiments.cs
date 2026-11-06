namespace InvestmentLab.Backtesting.Contracts;

public interface IBacktestExperiments
{
    Task<ExperimentTicketDto> RequestAsync(Guid strategyId, int version, ExperimentSettingsDto settings, CancellationToken cancellationToken);

    Task<ExperimentTicketDto> CheckAsync(string typeCode, string parameters, ExperimentSettingsDto settings, CancellationToken cancellationToken);

    Task<ExperimentStatusDto?> GetStatusAsync(Guid experimentId, CancellationToken cancellationToken);
}
