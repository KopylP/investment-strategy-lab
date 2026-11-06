# ADR-0017: The backtest is compared with what the strategy type declares

- Status: accepted (2026-11-03)
- Complements: ADR-0012, ADR-0016

## Context
The backtest of each strategy showed three lines: the strategy, the benchmark and "equal weight in all funds". These three lines were written into an enum, into the tax totals, into the API response and onto the screen. But not every comparison says something about every strategy:

- "Equal weight in the set" checks whether the selection pays off, so it makes sense for strategies that choose from a set (momentum). Buy-and-hold is itself "equal weight", so this line almost duplicates the strategy itself. For a strategy with fixed weights (60/40), the honest comparison is the same weights without rules, not equal weights.
- The benchmark answers the question "what if I had simply bought the market?" and suits all equity strategies.
- The goal of our strategies is more return per unit of risk (ADR-0012). An ordinary benchmark does not show this: a benchmark diluted with cash to the strategy's volatility is needed.

## Decision
- The strategy type in the engine declares `backtestComparisons`: the comparison codes in display order. Momentum strategies declare `benchmark`, `benchmark-at-equal-risk`, `universe-equal-weight`; buy-and-hold only the first two.
- In `Backtesting`, each comparison is a separate `IReplayComparison` block with a code. The block builds its line from the `ReplayedHistory` (prices, settings, the growth of the strategy), and states how the tax sees the holding of it. The tax after sale, the Sharpe, the drawdown and "what would the money have been" are calculated the same way for any line.
- A new comparison is a new class and its registration, without changes in the run, the API or the screen. An unknown code stops the backtest with the error `backtest.unknown_comparison` instead of silently dropping the line.
- A line in the result has a code and a message-name (`@backtest.series.…`). The screen draws as many lines as it receives and knows nothing about the specific comparisons.
- `benchmark-at-equal-risk`: each day, a share `w = min(1, σ of the strategy / σ of the benchmark)` is held in the benchmark, and the rest in the strategy's cash fund. `w` is taken over the whole period, so this is a yardstick, not a strategy that could be repeated. A line above the whole benchmark, that is, on borrowed money, is not taken.
- The saved backtests are deleted by a migration.

## Consequences
- The tax for `universe-equal-weight` and `benchmark-at-equal-risk` is counted as one purchase at the start and one sale at the end, although both lines are rebalanced daily. They are slightly overstated.
- A strategy with fixed weights will have its own comparison (the same weights without rules) when such a strategy appears.
- The benchmark remains mandatory, because momentum reads the market regime from it. A strategy on other instruments will choose its own benchmark, for example a bond index, and will declare only the comparisons that fit it.
