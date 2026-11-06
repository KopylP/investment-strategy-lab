# ADR-0013: A backtest asks the strategy through the same contract as the reports

- Status: accepted (2026-11-02)

## Context
The user must see how a strategy with their parameters behaved on history. Strategies may run on different engines, so the backtest code cannot depend on the language or the internal structure of a strategy. The first option, a simulator in Python, called the strategies as engine functions and would not work for an engine in another language.

## Decision
- A separate `Backtesting` module with its own schema. It queues a backtest, runs it in the background and stores the result.
- The simulation runs in C# and asks the strategy through `IStrategyEvaluation`; the trades are counted by `SubPortfolioRebalancer` through `IRebalancingRules`. A new strategy type works without any backtest code.
- The frequency of decisions is set by the reports the type declares. The risk check is called only for a type that declares it.
- A type that cannot answer a thousand times in a row, for example an AI agent, declares `supportsBacktest: false`, and the screen shows that the check is unavailable.
- A decision is made at the close of the day, and the trade is executed at the price of the next day, so as not to look into the future.
- The simulation runs day by day, but monthly points are stored, with the worst drawdown within the month and metrics computed by day. For the user, the amounts multiply the monthly returns by their start and contributions, without a new run.
- A backtest runs for a new version of a strategy, when a strategy is opened without a fresh result (older than 32 days), and after the quote history is extended.

## Consequences
- One run makes hundreds or thousands of requests to the engine, so it takes longer than a calculation inside the engine. Runs go one at a time.
- The rebalancing rules exist in one place, but the amounts are approximate: the minimum trade size is not recalculated for the user's contributions.
- The result does not account for taxes and is built on today's list of funds, so the screen says plainly that this is the past, not a forecast.
