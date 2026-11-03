using InvestmentLab.Strategies.Contracts;
using InvestmentLab.StrategyBuilder.Drafts.Domain;

namespace InvestmentLab.StrategyBuilder.Drafts.Infrastructure;

internal sealed class StrategyAuthoring(IAuthoredStrategies strategies) : IStrategyAuthoring
{
    public async Task<Result<TrialVersion>> WriteTrialVersionAsync(UserId owner, Guid? trialStrategyId, AuthoredStrategy strategy, CancellationToken cancellationToken)
    {
        var setup = SetupOf(strategy);
        var request = new TrialVersionRequest(owner.Value, trialStrategyId, setup);
        var written = await strategies.WriteTrialVersionAsync(request, cancellationToken);
        if (!written.IsSuccess)
            return Failure(written);

        return new TrialVersion(written.Value!.StrategyId, written.Value.Version);
    }

    private static AuthoredSetupDto SetupOf(AuthoredStrategy strategy)
    {
        var reports = strategy.Reports.Select(ReportOf).ToList();

        return new AuthoredSetupDto(
            TypeCode: RuleBasedType.Code,
            Parameters: strategy.Rules,
            Universe: strategy.Setup.Funds,
            BenchmarkInstrumentId: strategy.Setup.BenchmarkId!.Value,
            CashInstrumentId: strategy.Setup.CashId,
            VolatilityIndexInstrumentId: strategy.Setup.VolatilityIndexId,
            ExtraSeries: strategy.Setup.ExtraSeries,
            Reports: reports);
    }

    private static AuthoredReportDto ReportOf(DraftReport report)
    {
        var timing = report.Schedule;
        var schedule = new StrategyReportScheduleDto(timing.Frequency, timing.Day, timing.Weekday, timing.Time, timing.Minute);

        return new AuthoredReportDto(report.Code, schedule, report.SignalKinds, report.SignalIds);
    }

    public async Task<Result<RulesCheck>> CheckAsync(string rules, CancellationToken cancellationToken)
    {
        var check = await strategies.CheckParametersAsync(RuleBasedType.Code, rules, cancellationToken);
        if (!check.IsSuccess)
            return Failure(check);

        var problems = check.Value!.Problems.Select(problem => new EngineProblem(problem.Path, problem.Code, problem.Message)).ToList();

        return new RulesCheck(problems, check.Value.SignalIds);
    }

    private static Error Failure<T>(AuthoringResult<T> result)
    {
        if (result.Problems is null)
            return Error.Conflict(result.ErrorCode!, result.ErrorMessage ?? result.ErrorCode!);

        var problems = result.Problems.Select(problem => new EngineProblem(problem.Path, problem.Code, problem.Message)).ToList();
        var refused = new RulesRefused(problems);

        return Error.Conflict(result.ErrorCode!, result.ErrorMessage ?? result.ErrorCode!, refused);
    }
}
