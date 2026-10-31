using InvestmentLab.Signals.Monitoring.Domain;

namespace InvestmentLab.Signals.Monitoring.Application.Shared;

public sealed record SignalConditionDto(string Metric, string? Direction, decimal Threshold, decimal? RearmAt, int? WindowDays)
{
    public static SignalConditionDto Of(SignalCondition condition) => new(
        condition.Metric.ToString(),
        condition.Direction.ToString(),
        condition.Threshold,
        condition.RearmAt,
        condition.WindowDays);

    public Result<SignalCondition> ToCondition()
    {
        if (!Enum.TryParse<SignalMetric>(Metric, ignoreCase: true, out var metric) || !Enum.IsDefined(metric))
            return SignalErrors.UnknownMetric();

        return SignalCondition.TryOf(metric, ParsedDirection(), Threshold, RearmAt, WindowDays);
    }

    private SignalDirection ParsedDirection()
    {
        var isKnown = Enum.TryParse<SignalDirection>(Direction, ignoreCase: true, out var direction) && Enum.IsDefined(direction);

        return isKnown ? direction : SignalDirection.Above;
    }
}
