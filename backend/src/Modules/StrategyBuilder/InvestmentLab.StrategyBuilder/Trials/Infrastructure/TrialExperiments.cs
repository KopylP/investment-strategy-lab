using InvestmentLab.Backtesting.Contracts;
using InvestmentLab.StrategyBuilder.Drafts.Domain;
using InvestmentLab.StrategyBuilder.Trials.Domain;

namespace InvestmentLab.StrategyBuilder.Trials.Infrastructure;

internal sealed class TrialExperiments(IBacktestExperiments experiments) : ITrialExperiments
{
    private const string UnknownFailure = "research.failed";

    public async Task<Result> CheckAsync(TrialExperiment experiment, string parameters, CancellationToken cancellationToken)
    {
        var settings = SettingsOf(experiment);
        var answer = await experiments.CheckAsync(RuleBasedType.Code, parameters, settings, cancellationToken);
        if (answer.ErrorCode is null)
            return Result.Success();

        return Result.Failure(Refused(answer));
    }

    public async Task<Result<Guid>> RequestAsync(TrialVersion version, TrialExperiment experiment, CancellationToken cancellationToken)
    {
        var settings = SettingsOf(experiment);
        var ticket = await experiments.RequestAsync(version.StrategyId, version.Version, settings, cancellationToken);
        if (ticket.Id is { } id)
            return id;

        return Refused(ticket);
    }

    public async Task<TrialSummary> SummaryOfAsync(Guid experimentId, CancellationToken cancellationToken)
    {
        var experiment = await experiments.GetStatusAsync(experimentId, cancellationToken);
        if (experiment is null)
            return MissingExperiment();

        var status = BacktestStatuses.TrialStatusOf(experiment.Status);
        var summary = experiment.Summary;

        return new TrialSummary(
            Status: status,
            FailureReason: experiment.FailureReason,
            AnnualReturn: summary?.AnnualReturn,
            AnnualReturnAfterTax: summary?.AnnualReturnAfterTax,
            Volatility: summary?.Volatility,
            Sharpe: summary?.Sharpe,
            MaxDrawdown: summary?.MaxDrawdown);
    }

    public async Task<Result> CancelAsync(Guid experimentId, CancellationToken cancellationToken)
    {
        var answer = await experiments.CancelAsync(experimentId, cancellationToken);
        if (answer.ErrorCode is null)
            return Result.Success();

        return Error.Validation(answer.ErrorCode, "The experiment could not be cancelled.");
    }

    public Task<IReadOnlyDictionary<Guid, int>> QueuePositionsAsync(IReadOnlyList<Guid> experimentIds, CancellationToken cancellationToken) =>
        experiments.QueuePositionsAsync(experimentIds, cancellationToken);

    private static Error Refused(ExperimentTicketDto ticket)
    {
        var code = ticket.ErrorCode ?? UnknownFailure;
        var reason = ticket.ErrorMessage ?? Message.Of(code);

        return TrialErrors.ExperimentRefused(reason);
    }

    private static ExperimentSettingsDto SettingsOf(TrialExperiment experiment)
    {
        var costs = experiment.Costs;
        var robustness = experiment.Robustness;

        return new ExperimentSettingsDto(
            Start: experiment.Period.Start,
            End: experiment.Period.End,
            InitialCapital: experiment.InitialCapital,
            CommissionRate: costs.CommissionRate,
            FeePerTrade: costs.FeePerTrade,
            SpreadBps: costs.SpreadBps,
            SlippageBps: costs.SlippageBps,
            OutOfSampleStart: robustness.OutOfSampleStart,
            TrainingMonths: robustness.WalkForward?.TrainingMonths,
            TestMonths: robustness.WalkForward?.TestMonths,
            ParameterPath: robustness.Neighborhood?.Path,
            ParameterValues: robustness.Neighborhood?.Values);
    }

    private static TrialSummary MissingExperiment() => new(
        Status: TrialStatus.Failed,
        FailureReason: Message.Of("research.not_found"),
        AnnualReturn: null,
        AnnualReturnAfterTax: null,
        Volatility: null,
        Sharpe: null,
        MaxDrawdown: null);
}
