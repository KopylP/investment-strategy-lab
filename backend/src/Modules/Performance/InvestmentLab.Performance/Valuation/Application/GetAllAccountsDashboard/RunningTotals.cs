using InvestmentLab.Performance.Valuation.Application.Shared;
using InvestmentLab.Performance.Valuation.Domain;

namespace InvestmentLab.Performance.Valuation.Application.GetAllAccountsDashboard;

public sealed class RunningTotals
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
