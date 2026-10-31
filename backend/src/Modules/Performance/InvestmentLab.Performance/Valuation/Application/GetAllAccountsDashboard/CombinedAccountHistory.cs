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

    private sealed class DayTotals
    {
        public decimal Value { get; set; }
        public decimal Cash { get; set; }
        public decimal CostBasis { get; set; }
        public decimal Flow { get; set; }
        public decimal Planned { get; set; }
        public decimal PlanFlow { get; set; }
    }

    private sealed class AccountCursor(string currency, AccountHistory history)
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
            var isToday = snapshot.Date == day;

            totals.Value += snapshot.Value * rate;
            totals.Cash += snapshot.Cash * rate;
            totals.CostBasis += snapshot.CostBasis * rate;

            if (isToday)
                totals.Flow += snapshot.Flow * rate;

            AddPlanTo(totals, snapshot, isToday, rate);
        }

        private void MoveTo(DateOnly day)
        {
            while (_position + 1 < history.Series.Count && history.Series[_position + 1].Date <= day)
                _position++;
        }

        private void AddPlanTo(DayTotals totals, Snapshot snapshot, bool isToday, decimal rate)
        {
            if (history.Plan?.TryGetValue(snapshot.Date, out var shadow) == true)
            {
                _lastPlanValue = shadow.Value;

                if (isToday)
                    totals.PlanFlow += shadow.Flow * rate;
            }
            else if (history.Plan is null && isToday)
                totals.PlanFlow += snapshot.Flow * rate;

            var plannedValue = history.Plan is null ? snapshot.Value : _lastPlanValue;
            totals.Planned += plannedValue * rate;
        }
    }

    private sealed class RunningTotals
    {
        private decimal _previousValue;
        private decimal _twrIndex = 1m;
        private decimal _contributions;
        private decimal _planContributions;

        public Snapshot Advance(DateOnly day, DayTotals totals)
        {
            var startValue = _previousValue + totals.Flow;
            if (startValue > 0m)
                _twrIndex *= totals.Value / startValue;

            _contributions += totals.Flow;
            _planContributions += totals.PlanFlow;
            _previousValue = totals.Value;

            return new Snapshot(
                Date: day,
                SubPortfolioId: null,
                Value: decimal.Round(totals.Value, 2),
                Cash: decimal.Round(totals.Cash, 2),
                Flow: decimal.Round(totals.Flow, 2),
                NetContributions: decimal.Round(_contributions, 2),
                TwrIndex: decimal.Round(_twrIndex, 8),
                CostBasis: decimal.Round(totals.CostBasis, 2));
        }

        public PlanPoint PlanPointOf(DayTotals totals) => new(
            decimal.Round(totals.Planned, 2),
            decimal.Round(totals.PlanFlow, 2),
            decimal.Round(_planContributions, 2));
    }
}
