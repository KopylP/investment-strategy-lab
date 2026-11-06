"""The engine owns indicator lookbacks; callers only consume the observation count."""
from .checks import placed_conditions
from .indicators import history_needed
from .parameters import CrossedCondition, RuleParameters


def required_history(parameters: RuleParameters) -> int:
    needs = [260, *(history_needed(indicator) for indicator in parameters.indicators.values())]
    for placed in placed_conditions(parameters):
        condition = placed.condition
        if isinstance(condition, CrossedCondition):
            for name in (condition.indicator, condition.other):
                if name in parameters.indicators:
                    needs.append(history_needed(parameters.indicators[name]) + condition.within_days)
    if parameters.selection.max_correlation is not None:
        needs.append(parameters.selection.correlation_days + 1)
    if parameters.weights.method == "inverseVolatility":
        needs.append(parameters.weights.volatility_days + 1)
    if parameters.weights.target_volatility is not None:
        needs.append(parameters.weights.target_volatility_days + 1)
    return max(needs)
