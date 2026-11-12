using InvestmentLab.Backtesting.Contracts;
using InvestmentLab.Backtesting.Experiments.Application.CancelExperiment;
using InvestmentLab.Backtesting.Experiments.Application.CheckExperiment;
using InvestmentLab.Backtesting.Experiments.Application.GetQueuePositions;
using InvestmentLab.Backtesting.Experiments.Application.GetExperimentOutcome;
using InvestmentLab.Backtesting.Experiments.Application.GetExperimentStatus;
using InvestmentLab.Backtesting.Experiments.Application.RequestExperiment;
using InvestmentLab.Backtesting.Experiments.Application.Shared;

namespace InvestmentLab.Backtesting.Experiments.Infrastructure;

internal sealed class BacktestExperiments(
    RequestExperimentHandler requestExperiment,
    CheckExperimentHandler checkExperiment,
    GetExperimentStatusHandler getStatus,
    GetExperimentOutcomeHandler getOutcome,
    CancelExperimentHandler cancelExperiment,
    GetQueuePositionsHandler getQueuePositions) : IBacktestExperiments
{
    public async Task<ExperimentTicketDto> RequestAsync(
        Guid strategyId,
        int version,
        ExperimentSettingsDto settings,
        CancellationToken cancellationToken)
    {
        var conditions = ConditionsOf(settings);
        var command = new RequestExperimentCommand(strategyId, version, conditions);
        var result = await requestExperiment.Handle(command, cancellationToken);

        return TicketOf(result);
    }

    public async Task<ExperimentTicketDto> CheckAsync(
        string typeCode,
        string parameters,
        ExperimentSettingsDto settings,
        CancellationToken cancellationToken)
    {
        var command = new CheckExperimentCommand(
            Conditions: ConditionsOf(settings),
            TypeCode: typeCode,
            Parameters: parameters);
        var result = await checkExperiment.Handle(command, cancellationToken);

        return result.IsSuccess ? new ExperimentTicketDto(null) : Refused(result.Error);
    }

    public Task<ExperimentStatusDto?> GetStatusAsync(Guid experimentId, CancellationToken cancellationToken) =>
        getStatus.Handle(experimentId, cancellationToken);

    public Task<ExperimentOutcomeDto?> GetOutcomeAsync(Guid experimentId, CancellationToken cancellationToken) =>
        getOutcome.Handle(experimentId, cancellationToken);

    public async Task<ExperimentTicketDto> CancelAsync(Guid experimentId, CancellationToken cancellationToken)
    {
        var cancelled = await cancelExperiment.Handle(experimentId, cancellationToken);

        return cancelled.IsSuccess ? new ExperimentTicketDto(experimentId) : Refused(cancelled.Error);
    }

    public Task<IReadOnlyDictionary<Guid, int>> QueuePositionsAsync(IReadOnlyList<Guid> experimentIds, CancellationToken cancellationToken) =>
        getQueuePositions.Handle(experimentIds, cancellationToken);

    private static ExperimentConditionsDto ConditionsOf(ExperimentSettingsDto settings)
    {
        var period = new ExperimentPeriodDto(settings.Start, settings.End);
        var costs = CostsOf(settings);
        var robustness = RobustnessOf(settings);

        return new ExperimentConditionsDto(period, settings.InitialCapital, costs, robustness);
    }

    private static ResearchTradingCostsDto CostsOf(ExperimentSettingsDto settings) => new(
        CommissionRate: settings.CommissionRate,
        FeePerTrade: settings.FeePerTrade,
        SpreadBps: settings.SpreadBps,
        SlippageBps: settings.SlippageBps);

    private static RobustnessPlanDto RobustnessOf(ExperimentSettingsDto settings)
    {
        var windows = WindowsOf(settings);
        var neighborhood = NeighborhoodOf(settings);

        return new RobustnessPlanDto(settings.OutOfSampleStart, windows, neighborhood);
    }

    private static WalkForwardWindowsDto? WindowsOf(ExperimentSettingsDto settings)
    {
        if (settings is not { TrainingMonths: { } training, TestMonths: { } test })
            return null;

        return new WalkForwardWindowsDto(training, test);
    }

    private static ParameterNeighborhoodDto? NeighborhoodOf(ExperimentSettingsDto settings)
    {
        if (settings.ParameterPath is null && settings.ParameterValues is not { Count: > 0 })
            return null;

        return new ParameterNeighborhoodDto(settings.ParameterPath ?? "", settings.ParameterValues ?? []);
    }

    private static ExperimentTicketDto TicketOf(Result<Guid> result) =>
        result.IsSuccess ? new ExperimentTicketDto(result.Value) : Refused(result.Error);

    private static ExperimentTicketDto Refused(Error error) => new(
        Id: null,
        ErrorCode: error.Code,
        ErrorMessage: error.AsMessage());
}
