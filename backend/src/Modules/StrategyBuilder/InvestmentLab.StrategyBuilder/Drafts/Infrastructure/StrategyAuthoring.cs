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

    public async Task<Result> DeleteTrialAsync(Guid trialStrategyId, CancellationToken cancellationToken)
    {
        var deleted = await strategies.DeleteTrialAsync(trialStrategyId, cancellationToken);

        return deleted.IsSuccess ? Result.Success() : Result.Failure(Failure(deleted));
    }

    public async Task<Result<PublishedVersion>> PublishAsync(UserId owner, PublishedOrigin? origin, string name, AuthoredStrategy strategy, CancellationToken cancellationToken)
    {
        var setup = SetupOf(strategy);
        var request = new PublishRequest(
            OwnerId: owner.Value,
            StrategyId: origin?.StrategyId,
            Name: name,
            Setup: setup);
        var published = await strategies.PublishAsync(request, cancellationToken);
        if (!published.IsSuccess)
            return Failure(published);

        return new PublishedVersion(published.Value!.StrategyId, published.Value.Version);
    }

    public async Task<AuthoredVersion?> GetVersionAsync(Guid strategyId, int version, CancellationToken cancellationToken)
    {
        var authored = await strategies.GetVersionAsync(strategyId, version, cancellationToken);
        if (authored is null)
            return null;

        var source = authored.Setup;
        var setup = new DraftSetup(
            Funds: source.Universe,
            BenchmarkId: source.BenchmarkInstrumentId,
            CashId: source.CashInstrumentId,
            VolatilityIndexId: source.VolatilityIndexInstrumentId,
            ExtraSeries: source.ExtraSeries);
        var reports = source.Reports.Select(DraftReportOf).ToList();

        return new AuthoredVersion(
            Name: authored.Name,
            Rules: source.Parameters,
            Setup: setup,
            Reports: reports,
            TypeCode: authored.TypeCode,
            Version: version);
    }

    private static DraftReport DraftReportOf(AuthoredReportDto report)
    {
        var schedule = report.Schedule;
        var timing = new ReportTiming(
            Frequency: schedule.Frequency,
            Day: schedule.Day,
            Weekday: schedule.Weekday,
            Time: schedule.Time,
            Minute: schedule.Minute);

        return new DraftReport(report.Code, timing, report.SignalKinds, report.SignalIds);
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
