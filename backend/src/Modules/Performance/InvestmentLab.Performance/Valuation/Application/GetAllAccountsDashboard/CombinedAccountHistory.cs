using InvestmentLab.Performance.Valuation.Application.Shared;
using InvestmentLab.Performance.Valuation.Domain;

namespace InvestmentLab.Performance.Valuation.Application.GetAllAccountsDashboard;

public static class CombinedAccountHistory
{
    public static (IReadOnlyList<Snapshot> Series, IReadOnlyDictionary<DateOnly, PlanPoint>? Plan) Of(
        IReadOnlyList<AccountWithHistory> histories, IReadOnlyList<DateOnly> days, FxOn fx)
    {
        var anyPlan = histories.Any(entry => entry.History.Plan is not null);
        var cursors = histories.Select(entry => new AccountCursor(entry.Account.BaseCurrency, entry.History)).ToList();
        var running = new RunningTotals();

        var series = new List<Snapshot>();
        var plan = new Dictionary<DateOnly, PlanPoint>();

        foreach (var day in days)
        {
            var dayTotals = new DayTotals();

            foreach (var cursor in cursors)
                cursor.AddTo(dayTotals, day, fx);

            series.Add(running.Advance(day, dayTotals));

            if (anyPlan)
                plan[day] = running.PlanPointOf(dayTotals);
        }

        return (series, anyPlan ? plan : null);
    }
}
