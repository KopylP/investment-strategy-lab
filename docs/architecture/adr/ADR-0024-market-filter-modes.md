# ADR-0024: Market filter modes

- Status: accepted (2026-11-07)

## Context

A rule-based strategy has a market filter: a condition on the benchmark (or another market series) and a share to keep
in funds when the condition fails (`investWhenUnfavorable`). The engine reported that share as the decision's
`investFraction`, and the rebalancer applied it only to new contributions
(`deployable = contribution × investFraction + proceeds + parked if favourable`). Held funds were never cut and sale
proceeds were always redeployed. A backtest has no contributions, so a share of 0.2 and of 1.0 gave identical results,
while the builder's sentence promised «інакше 20%, решта в кеш-фонді». Research on 2026-11-06 found the filter inert.

## Decision

- **The rule names what an unfavourable market does:** `marketFilter.whenUnfavorable` is `reduceHoldings` or
  `newMoneyOnly`.
- **`reduceHoldings` is implemented in the engine by scaling target weights:** every target weight is multiplied by
  `investWhenUnfavorable` and `investFraction` is `1.0`. This is the path target volatility already uses, so the
  rebalancer parks the unassigned rest in the cash fund, and backtests, live reports and the rebalancer follow without
  new logic. Weights shrunk by target volatility are scaled once more by the market share, never twice by either.
- **`newMoneyOnly` keeps the former behaviour.** A rule without the field means `newMoneyOnly`, so every published
  strategy decides exactly as before.
- **New drafts from the momentum template use `reduceHoldings`**, the meaning the builder's sentence always promised.
- The builder's words and the engine's reasons name the mode chosen, each with its own message code.

## Consequences

- `reduceHoldings` sells in falling markets: in a taxable account that realizes gains, and in Poland 19% of them is
  due. The backtest's after-tax figures show this cost.
- A backtest cannot show `newMoneyOnly` until experiment conditions include contributions; until then it measures
  the same as having no filter for held money.
- The rebalancer and the decision contract are unchanged; the mode lives in the opaque rule parameters.
