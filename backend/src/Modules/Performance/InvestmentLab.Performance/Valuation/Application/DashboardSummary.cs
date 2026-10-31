using InvestmentLab.Performance.Valuation.Domain;

namespace InvestmentLab.Performance.Valuation.Application;

public sealed record DashboardSummary(DashboardDto Dashboard, DateOnly Start, Snapshot? Before)
{
    private const decimal DaysInYear = 365.25m;

    public static DashboardDto Empty(Guid? accountId, string currency, string period, IReadOnlyList<ReportMarkerDto> reports) => new(
        AccountId: accountId,
        Currency: currency,
        AsOf: null,
        Period: period,
        Value: 0m,
        NetContributions: 0m,
        Gain: 0m,
        Twr: null,
        TwrAnnualized: null,
        Xirr: null,
        MaxDrawdown: 0m,
        CurrentDrawdown: 0m,
        PlanValue: null,
        Series: [],
        SubPortfolios: [],
        Accounts: [],
        Reports: reports,
        PositionsValue: 0m,
        CostBasis: 0m);

    public static DashboardSummary Of(IReadOnlyList<Snapshot> history, IReadOnlyDictionary<DateOnly, PlanPoint>? plan, string period)
    {
        var last = history[^1];
        var start = DashboardPeriod.StartOf(period, last.Date);
        var window = history.Where(snapshot => snapshot.Date >= start).ToList();
        var before = history.LastOrDefault(snapshot => snapshot.Date < start);
        var baseIndex = before?.TwrIndex ?? 1m;

        var twr = baseIndex > 0m ? last.TwrIndex / baseIndex - 1m : (decimal?)null;
        var drawdown = DrawdownCalculator.Of([.. window.Select(snapshot => (snapshot.Date, snapshot.TwrIndex))]);

        var dashboard = new DashboardDto(
            AccountId: null,
            Currency: "",
            AsOf: last.Date,
            Period: period,
            Value: last.Value,
            NetContributions: last.NetContributions,
            Gain: last.Value - last.NetContributions,
            Twr: twr,
            TwrAnnualized: Annualized(twr, last.Date, before?.Date ?? history[0].Date),
            Xirr: XirrOf(window, before, last),
            MaxDrawdown: drawdown.Max,
            CurrentDrawdown: drawdown.Current,
            PlanValue: plan?.GetValueOrDefault(last.Date)?.Value,
            Series: SeriesOf(window, plan, baseIndex),
            SubPortfolios: [],
            Accounts: [],
            Reports: [],
            PositionsValue: last.Value - last.Cash,
            CostBasis: last.CostBasis);

        return new DashboardSummary(dashboard, start, before);
    }

    private static decimal? Annualized(decimal? twr, DateOnly end, DateOnly start)
    {
        var years = (end.DayNumber - start.DayNumber) / DaysInYear;
        if (twr is null || years < 1m)
            return null;

        return (decimal)Math.Pow((double)(1m + twr.Value), (double)(1m / years)) - 1m;
    }

    private static decimal? XirrOf(List<Snapshot> window, Snapshot? before, Snapshot last)
    {
        var flows = window.Where(snapshot => snapshot.Flow != 0m).Select(snapshot => (snapshot.Date, -snapshot.Flow)).ToList();

        if (before is not null && before.Value != 0m)
            flows.Insert(0, (before.Date, -before.Value));

        flows.Add((last.Date, last.Value));

        return XirrCalculator.Of(flows);
    }

    private static List<SeriesPointDto> SeriesOf(List<Snapshot> window, IReadOnlyDictionary<DateOnly, PlanPoint>? plan, decimal baseIndex)
    {
        var series = new List<SeriesPointDto>();
        var peak = 0m;

        foreach (var snapshot in window)
        {
            peak = Math.Max(peak, snapshot.TwrIndex);
            var planned = plan?.GetValueOrDefault(snapshot.Date);

            series.Add(new SeriesPointDto(
                Date: snapshot.Date,
                Value: snapshot.Value,
                NetContributions: snapshot.NetContributions,
                TwrIndex: snapshot.TwrIndex / baseIndex,
                PlanValue: planned?.Value,
                PlanNetContributions: planned?.NetContributions,
                Drawdown: peak > 0m ? decimal.Round(snapshot.TwrIndex / peak - 1m, 6) : 0m,
                PositionsValue: snapshot.Value - snapshot.Cash,
                CostBasis: snapshot.CostBasis));
        }

        return series;
    }
}
