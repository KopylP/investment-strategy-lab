# ADR-0015: Adaptive momentum as a separate strategy type

- Status: accepted (2026-11-02)
- Complements: ADR-0012

## Context
In the backtest of the EU set, ETF momentum gave 8.6% a year with a Sharpe of 0.76: less than "equal weight in all funds" (10.5%, 0.82). Its return was eaten by the protection layers: selling individual positions on stops, exit on VIX and on an account fall, volatility, Sharpe and drawdown filters, and 6-month momentum. The goal of the new version: more return at comparable risk, and only with rules that help in both the EU and the US sets (ADR-0012).

The rules were iterated with a research simulator on daily Yahoo prices (EU from 2015 and from 2011, US from 2003, 2008 and 2015), and the outcome was verified with the service's backtest, using the service's rebalancer.

## Decision
A new type `adaptive-momentum` next to the old one; the old one does not change.

- Strength of a fund: the average return over 3, 6 and 12 months (63, 126 and 252 trading days), without a volatility adjustment.
- A fund qualifies only when its strength is positive and its price is above its 200-day average. There are no other filters.
- Up to three of the strongest, one per group, and not similar to one already chosen (the correlation of daily changes over 126 days above 0.7). Each takes a third; an empty place waits in the cash fund.
- A target volatility of 14%: when the chosen portfolio has moved more strongly over the last 21 trading days, the share of all funds decreases proportionally, and the rest goes to the cash fund (volatility-managed portfolios, Moreira & Muir 2017).
- Decisions only monthly. There is no risk report and no sales between rebalances, so the strategy does not set stops.
- The strategy always declares the market favourable: the share in funds is set by the weights, not by a flag, otherwise the rebalancer would not return money from the cash fund.
- The default strategies "EU adaptive momentum" and "US adaptive momentum" have the same funds, cash and benchmark as the ETF momentum.
- The saved backtests are deleted by a migration, so that all strategies are recalculated under the same rules.

## Results of the service backtest
100,000 at the start, no contributions, a commission of 0.1% + 1 per trade.

| Strategy | EU 2015–2026: return, volatility, Sharpe, drawdown | US 2002–2026 |
|---|---|---|
| ETF momentum (correlation 0.7, no sale of positions) | 10.0%, 11.3%, 0.83, −15.8% | 8.3%, 12.1%, 0.57, −25.2% |
| Adaptive momentum, target 14% | 12.5%, 13.0%, 0.91, −15.0% | 9.5%, 13.8%, 0.59, −29.4% |
| Adaptive momentum without a volatility target | 13.2%, 14.1%, 0.88, −15.0% | 9.8%, 16.5%, 0.51, −37.7% |
| Equal weight in all funds | 10.5%, 11.9%, 0.82, −28.2% | 9.8%, 13.8%, 0.61, −40.8% |
| S&P 500 | 13.7%, 18.6%, 0.70, −33.0% | 11.3%, 18.5%, 0.54, −55.2% |

## What was checked and not taken
Each of these did not improve the result in both sets at once:

- stops on individual positions (in the EU they cost about 4% a year), an exit of the portfolio after a fall of 6–10% from the rebalance, and an exit on a volatility spike between rebalances: they sell at the bottom and miss the rebound;
- a rank by strength adjusted for volatility, weights inversely proportional to volatility, 6-month or 12-1 momentum, Keller's 13612W weighting, Clenow's trend quality (slope of an exponential regression × R²);
- 2 or 4 funds instead of 3, a holding buffer, a fund volatility limit of 30–35%;
- a share in cash by the number of funds with negative momentum (Keller, DAA), protective assets instead of cash, a market filter by the S&P 500 or its momentum;
- a 30–60% core in the broad market: it helped in the US set and worsened the EU one.

## Consequences
- In the EU the new strategy is 2.5% a year more profitable than ETF momentum and has a smaller drawdown. In the US it is 1.2% more profitable, but the drawdown is deeper (−29% against −25%), and against "equal weight in all funds" at equal risk it only matches it.
- A weak spot: years of a sideways market, when there are no strong trends (US 2011–2016, 2023). The strategy then picks the protective sectors and lags the index. Its strong side: long falls (in the research simulator, 2008: +0.1% against −37% for the S&P 500; 2022 in the EU: +4.9% against −14%).
- The rebalancer trims a position only beyond a 10% band and only a profitable one, so small reductions of the share by volatility are not always executed. The service backtest accounts for this; the research simulator showed 1–2% more.
- The limitations of ADR-0012 remain: the funds are taken from today's list, taxes are not counted, the rules were chosen on the same history, so the future result will be worse than the backtest.
