# ADR-0016: The backtest shows the return after tax

- Status: accepted (2026-11-02)
- Complements: ADR-0013, ADR-0015

## Context
On an ordinary account, a strategy with turnover pays tax every year on the profit from sales, while "buy and hold" pays only once, when everything is sold. Without tax, the backtest showed an advantage of a strategy that the account does not have: adaptive momentum in the EU earned 12.4% a year against 13.7% for the S&P 500, and after Belka it is 10.3% against 12.1%.

## Decision
- The tax rules stay in `Taxation`. A new contract `ITaxSimulation` takes the trades (date, fund, quantity, price, commission, currency), takes the rate itself by the rule of the policy, and calculates the tax by year with the same `TaxCalculator` as the accounts: lots, carrying forward of losses, rounding. It calculates by the module's default policy. `Backtesting` knows neither the rate nor the tax currency.
- `ReplayBook` writes the journal of trades. After a run, `Backtesting` asks for the tax on the strategy's trades together with a hypothetical sale of everything on the last day, and for the benchmark and "equal weight" as one purchase at the start and one sale at the end.
- The tax for a year is paid from the portfolio on the first day of the next year; the tax of the last year is paid from the final value. A payment reduces the portfolio proportionally, so the run is not repeated: the result after tax is obtained by multiplying the final growth by the share left after each payment (`AfterTaxGrowth`). The tax from a larger portfolio scales the same way, so the error lies only in the rounding to the złoty.
- The tax in the policy's currency is converted into the backtest's currency at the `MarketData` cross rate on the day of the last trade of that year.
- The total of each series gets `AnnualReturnAfterTax`, and the screen shows it as a column "After tax". The saved backtests are deleted by a migration.

## Consequences
- The dividend tax is not counted: the backtest runs on adjusted prices, where dividends are reinvested. For distributing funds the result is overstated, so strategies are better built on accumulating funds.
- The calculation takes the module's default policy, not the policy of a specific account: the backtest belongs to the strategy. On an IKE/IKZE the return before tax remains the correct one.
- The curves on the charts and the "what would the money have been" amounts remain before tax.

## The EU set's composition in view of tax
A research simulator with tax (FIFO lots, 19% of the annual total, carrying forward of losses, 19% of the dividends of distributing funds), EU from 2015 and from 2011; the annual return after tax relative to the current set:

| Change | 2015–2026 | 2011–2026 |
|---|---|---|
| EUN2 → SXRT (the same EURO STOXX 50, accumulating) | +0.1% | +0.3% |
| XDJE (Nikkei, hedged) → EUNN (MSCI Japan, unhedged) | −2.1% | −1.5% |
| IS04 (Treasury 20+) → SXRM (Treasury 7–10, accumulating) | −0.6% | −0.8% |
| Without IQQA | −0.3% | −0.2% |
| Without IUS7 | −0.7% | −0.7% |
| A 30–70% core in the S&P 500 that is never sold (drawdown −20…−31%) | +0.2…−0.2% | −0.4…−1.4% |

The typical EU strategies take SXRT instead of EUN2, the rest of the set stays: momentum holds the distributing funds only occasionally, so the tax on their dividends is small, and replacements with another index change the result itself. The main tax burden is the profit from sales (about 2% a year), not the dividends. Refusing to trim profitable positions ("rebalance only by purchases" in the account policy) added about another 0.5% a year after tax.
