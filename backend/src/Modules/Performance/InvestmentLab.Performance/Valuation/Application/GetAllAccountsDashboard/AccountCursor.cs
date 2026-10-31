using InvestmentLab.Performance.Valuation.Application.Shared;
using InvestmentLab.Performance.Valuation.Domain;

namespace InvestmentLab.Performance.Valuation.Application.GetAllAccountsDashboard;

public sealed class AccountCursor(string currency, AccountHistory history)
{
    private int _position = -1;
    private decimal _lastPlanValue;

    public void AddTo(DayTotals totals, DateOnly day, FxOn fx)
    {
        MoveTo(day);

        if (_position < 0)
            return;

        var snapshot = history.Series[_position];
        var rate = fx(currency, day) ?? 0m;
        var isOwnDay = snapshot.Date == day;

        AddBalances(totals, snapshot, rate);

        if (isOwnDay)
            totals.Flow += snapshot.Flow * rate;

        if (history.Plan is { } plan)
            AddPlanned(totals, plan, snapshot, isOwnDay, rate);
        else
            AddOwnValueAsPlanned(totals, snapshot, isOwnDay, rate);
    }

    private void MoveTo(DateOnly day)
    {
        while (_position + 1 < history.Series.Count && history.Series[_position + 1].Date <= day)
            _position++;
    }

    private static void AddBalances(DayTotals totals, Snapshot snapshot, decimal rate)
    {
        totals.Value += snapshot.Value * rate;
        totals.Cash += snapshot.Cash * rate;
        totals.CostBasis += snapshot.CostBasis * rate;
    }

    private void AddPlanned(DayTotals totals, IReadOnlyDictionary<DateOnly, PlanPoint> plan, Snapshot snapshot, bool isOwnDay, decimal rate)
    {
        if (plan.TryGetValue(snapshot.Date, out var shadow))
        {
            _lastPlanValue = shadow.Value;

            if (isOwnDay)
                totals.PlanFlow += shadow.Flow * rate;
        }

        totals.Planned += _lastPlanValue * rate;
    }

    private static void AddOwnValueAsPlanned(DayTotals totals, Snapshot snapshot, bool isOwnDay, decimal rate)
    {
        if (isOwnDay)
            totals.PlanFlow += snapshot.Flow * rate;

        totals.Planned += snapshot.Value * rate;
    }
}
