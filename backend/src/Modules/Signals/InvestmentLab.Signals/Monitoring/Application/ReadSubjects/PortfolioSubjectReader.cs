using InvestmentLab.Performance.Contracts;
using InvestmentLab.Portfolio.Contracts.Accounts;
using InvestmentLab.Signals.Monitoring.Domain;

namespace InvestmentLab.Signals.Monitoring.Application.ReadSubjects;

public sealed class PortfolioSubjectReader(IAccountSetupQueries accounts, IValuationHistory valuation, TimeProvider clock)
{
    private static readonly string UnknownSubPortfolioLabel = Message.Of("signal.subject.unknown_sub_portfolio");

    public async Task<SubjectReadings> ReadAsync(Signal signal, CancellationToken cancellationToken)
    {
        var accountId = signal.Target.AccountId!.Value;
        var account = await accounts.GetAccountSetup(accountId, cancellationToken);
        if (account is null)
            return SubjectReadings.Nothing;

        var (key, label) = SubjectOf(signal.Target, account);
        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        var from = MeasurementWindow.StartOf(signal.Condition, today);

        var points = await valuation.GetTwrHistory(accountId, signal.Target.SubPortfolioId, from, cancellationToken);
        var value = Measure(signal.Condition.Metric, points);
        if (value is not { } measured)
            return SubjectReadings.Unmeasured(key);

        return SubjectReadings.One(new SubjectReading(key, label, measured, points[^1].Date));
    }

    private static (string Key, string Label) SubjectOf(SignalTarget target, AccountSetupDto account)
    {
        if (target.SubPortfolioId is not { } subPortfolioId)
            return ($"account:{account.Id:N}", account.Name);

        var subPortfolio = account.SubPortfolios.FirstOrDefault(each => each.Id == subPortfolioId);

        return ($"sub-portfolio:{subPortfolioId:N}", subPortfolio?.Name ?? UnknownSubPortfolioLabel);
    }

    private static decimal? Measure(SignalMetric metric, IReadOnlyList<TwrPointDto> points)
    {
        var indexes = points.Select(point => point.TwrIndex).ToList();

        return metric == SignalMetric.DayFall
            ? SignalMetrics.DayFall(indexes)
            : SignalMetrics.DrawdownFromPeak(indexes);
    }
}
