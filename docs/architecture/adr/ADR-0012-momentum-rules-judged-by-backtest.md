# ADR-0012: ETF momentum rules are judged by a backtest against simple portfolios

- Status: accepted (2026-11-01)

## Context
The backtest compared the strategy only with the S&P 500, so it did not show whether its rules paid off. A run on Yahoo history (EU from 2016, US from November 2007, 100,000 at the start, no contributions) gave these results:

| Variant | EU: return, Sharpe, drawdown | US: return, Sharpe, drawdown |
|---|---|---|
| Rules before the change | 10.3%, 0.67, −17.6% | 4.5%, 0.09, −25.7% |
| Correlation limit 0.7 and no sale of individual positions | 12.8%, 0.95, −16.1% | 7.1%, 0.32, −21.3% |
| Equal weight in all funds | 11.1%, 0.80, −28.2% | 8.2%, 0.33, −40.8% |
| 60% equities, 40% bonds | 8.5%, 0.60, −19.0% | 8.4%, 0.44, −31.4% |
| S&P 500 | 14.9%, 0.81, −33.8% | 11.2%, 0.39, −53.9% |

## Decision
- The backtest report also shows equal shares and 60/40 (`bond_benchmark`). The US run starts at the end of 2007 to cover the crisis.
- `scanner.maxCorrelation` does not allow taking a fund that, over the last 130 days, moves too similarly to one already chosen. A value of 1 turns the check off; for new strategies 0.7 applies.
- `risk.sellOnPositionExit` decides whether to sell an individual position on its exit rule. For new strategies it is off: a position only warns, and the exit of the whole portfolio applies.
- The default parameter values stay as they were, so saved strategies do not change without the user's decision.
- Limits by asset class and by region are not added to the strategy: they did not give a stable improvement in both sets.
- The weak-market rule (`weakMarketInvestPct`) stays: it has almost no effect on the result, but removing it would break saved parameters, because the model rejects unknown fields.

## Consequences
- In the 2007–2009 crisis the US set's drawdown is deeper: −18% against −12%. The largest drawdown over the whole period is smaller, though, because other periods turned out worse.
- The US strategy stays the standard one: it loses to 60/40 in return and Sharpe, but has a smaller drawdown and earned in the crisis (+8% against −21%).
- The comparison overstates the result: the funds are taken from today's list, taxes are not counted, and the variants were tried on the same history. The 0.7 threshold is sensitive: in the EU at 0.8 the Sharpe is 0.79, at 0.6 it is 0.62.

## Evaluation at equal risk
The goal of the strategy is more return per unit of risk. So we compare it with the S&P 500 scaled with cash to the same volatility. The difference in annual return:

| Variant | EU | US |
|---|---|---|
| Rules before the change | −1.5% | −3.2% |
| Current rules | +1.9% | −0.3% |

For the current rules, the 90% interval covers zero (EU from −3.1% to +7.1%, US from −4.2% to +3.3%), so the advantage over the index is not proven. A market limit, a momentum rank adjusted for volatility, a different number of funds and a different correlation threshold did not improve the result in both sets at once, so they are not added.
