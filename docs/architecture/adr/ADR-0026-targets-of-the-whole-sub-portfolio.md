# ADR-0026: Targets are shares of the whole sub-portfolio

- Status: accepted (2026-11-08)

## Context

A review of the rebalancer on 2026-11-08 found three faults that made a plan hard to predict:

- **Holdings melted away in a weak market.** Target values were computed from the money being invested this time
  (`kept + contribution × investFraction + proceeds`), which leaves out money already parked while the market is
  unfavourable. With `reduceHoldings` (ADR-0024) a fund targeted at 50% was cut to 50%, then to 25%, 12.5% and so
  on, one report or backtest rebalance after another. `etf_momentum` with empty slots did the same. Backtests of such
  strategies showed more cash and less risk than the rule promised.
- **"In profit" compared two currencies.** A trim needs the fund to be in profit, but the price was in the account
  currency and the average cost in the currency of the trade, so a dollar fund on a złoty account always looked in
  profit, and the backtest (one currency) behaved differently from the live report.
- **Skipped trades were silent.** An overweight fund at a loss, a buy smaller than one whole share or a pick without a
  price left no trace, and a weak-market note claimed "positions are not sold" next to a trim. A note about "empty
  slots" guessed the slot count from equal weights and was wrong whenever weights were scaled.

## Decision

- **A target weight is a share of the whole sub-portfolio after the contribution**: held funds, parked money, cash and
  the contribution. It is the same base the report's "before → after" table uses, so a reason and the table agree.
- **What may be spent is a separate question:** the invested part of the contribution, sale proceeds, trims, and
  parked money only when the market is favourable. Parked money keeps waiting in a weak market.
- **"In profit" is judged in the fund's own currency:** the report converts the average cost with the same rate as the
  price; a position without a known cost is not trimmed.
- **Every trade the rules skip is named in a note:** below the minimum trade, below one whole share, without a price,
  above target but at a loss, above target but kept because the account levels by purchases only. A trim reason states
  the fund's current weight. The weak-market note describes only what the plan does, parked money gets its own note,
  and partly covered targets are reported as their total share instead of a guessed number of slots.
- `etf_momentum/plan.py` changes the same way, and the golden fixtures prove both implementations still agree.

## Consequences

- `reduceHoldings` now holds the reduced share for as long as the market stays weak. Backtests of strategies using it,
  and research numbers taken from them before this change, must be run again.
- In a weak market with `newMoneyOnly`, new money is split by each fund's gap to its share of the whole sub-portfolio,
  and held funds are trimmed only when they are above that share; before, the gaps were measured against the
  invested part only.
- Reports carry more notes, each one naming a concrete fund and reason.
