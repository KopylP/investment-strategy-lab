# ETF Momentum Strategy

Each month, buy the strongest ETFs that are in a confirmed uptrend, at most one per
group of look-alike funds. Each week, check every position against its exit rules.
The same code produces the Telegram message, the text report, the HTML report and
the backtest, so the instructions never disagree.

## Package Layout

```text
src/finance_decisions/strategies/etf_momentum/
├── config/
│   ├── strategy.toml        # EU universe (EUR, Xetra)
│   └── strategy_us.toml     # US universe (USD)
├── templates/
│   ├── backtest.html
│   ├── monthly_scanner.html
│   └── weekly_risk_monitor.html
├── backtest.py              # historical replay + CLI
├── config.py                # settings dataclasses, TOML + env loading
├── monthly_scanner.py       # CLI
├── plan.py                  # scan + portfolio -> concrete orders
├── positions.json           # your portfolio
├── reports.py               # text, Telegram, JSON and HTML outputs
├── risk.py                  # weekly exit rules
├── runner.py                # download data, run, write reports, notify
├── scanner.py               # market check, filters, ranking, selection
└── weekly_risk_monitor.py   # CLI
```

## Rules

### Market check (monthly)

The market is **OK** when SPY closes above its 200-day average **and** VIX is below
`max_vix` (30). Missing or stale SPY/VIX data counts as **not OK**.

### ETF filters

An ETF qualifies only if all of these hold:

| Filter | Default |
|---|---|
| Price above its 200-day average | `ma_period = 200` |
| Momentum over the last 6 **completed** months above `min_momentum` | `> 0%` |
| Annual volatility (daily, last 12 months) below `max_volatility` | `< 35%` |
| Drop from the 12-month high not deeper than `max_drawdown` | `> -20%` |
| Sharpe over 12 months, net of the cash ETF's return, at least `min_sharpe` | `>= 0.20` |
| Last price not older than `max_stale_days` | `5 days` |

### Selection and weights

1. Rank qualified ETFs by momentum.
2. A held ETF keeps its place while it ranks within `hold_buffer_k` (8), so the
   portfolio does not churn. If two holdings share a group, the larger one is kept.
3. Take at most `max_per_group` (1) ETF from each `[groups]` entry. Nasdaq-100,
   Nasdaq-100 hedged and S&P 500 IT are one group: holding all three is one bet.
4. Up to `top_n` (4) picks. Each slot is `1/top_n` of the portfolio (25%).
   **Unfilled slots stay in the cash ETF**, so fewer candidates never means a
   bigger bet on the remaining ones.

### Monthly plan

1. Sell every held ETF that is no longer selected.
2. Market OK: invest the whole contribution plus any parked money.
   Market weak: invest `weak_market_invest_pct` (50%) of the contribution and park
   the rest. Existing positions are not sold because of the market check.
3. Sale proceeds are always reinvested into the current picks.
4. Top up picks that are below target. A pick is trimmed only if it is **in profit**
   and more than `trim_band` (10 pp) above target. Losing overweight positions
   shrink naturally as new money goes elsewhere.
5. Orders below `min_trade` (EUR 25) are skipped. Anything not invested is parked
   in the cash ETF (`XEON.DE` for EU, `BIL` for US).

### Weekly exits

Each position is sold when its close falls below the **highest** of three levels:

- **Stop-loss:** entry price × (1 + `stop_loss_pct`), widened or tightened by the
  fund's volatility (between -4% and -25%).
- **Trend:** 200-day average × (1 − `ma_exit_band`). Between the average and this
  band is only a warning, which avoids selling on noise.
- **Trailing drawdown:** highest close since entry × (1 + `drawdown_exit`),
  volatility-adjusted.

The report shows that exit price and the distance to it for every position.

Portfolio-level: if the time-weighted portfolio falls `portfolio_exit` (20%) from
its **peak** (not from cost), sell everything into the cash ETF and re-enter with
the next monthly plan. If VIX is above `vix_exit` (35), positions that already
have warnings are sold immediately.

## How to Act on the Reports

**Monthly report** (first Sunday of the month, act on Monday):

1. Read the verdict at the top: it says how much of the contribution to invest.
2. Execute the numbered orders in order: sells first, then buys.
3. Use limit orders during liquid hours (Xetra 9:30–17:00 CET).
4. Update `positions.json` with the actual fills. The report includes a
   ready-made JSON snippet for the new purchases.

**Weekly report** (every Sunday, act on Monday):

- Green verdict: nothing to do.
- Amber verdict: nothing to sell; note the exit prices.
- Red verdict: sell what the checklist says, move the money to the cash ETF, and
  remove the sold lots from `positions.json`.

## Positions File

Add **one entry per purchase**. Lots of the same ticker are combined
automatically (total shares, average cost, earliest entry date). Record money
sitting uninvested on the broker account in `cash`. Parked money in the cash ETF
is recorded as a normal position.

```json
{
  "cash": 12.40,
  "positions": [
    {"ticker": "NQSE.DE", "entry_price": 17.756, "shares": 22.5276, "entry_date": "2026-06-01", "currency": "EUR"},
    {"ticker": "QDVI.DE", "entry_price": 16.63, "shares": 8.0186, "entry_date": "2026-11-02", "currency": "EUR"},
    {"ticker": "XEON.DE", "entry_price": 150.59, "shares": 1.3, "entry_date": "2026-11-02", "currency": "EUR"}
  ]
}
```

When you sell a ticker completely, delete all of its entries.

## Run Locally

From the repository root:

```bash
PYTHONPATH=src python -m finance_decisions.strategies.etf_momentum.monthly_scanner
PYTHONPATH=src python -m finance_decisions.strategies.etf_momentum.monthly_scanner --contribution 600
PYTHONPATH=src python -m finance_decisions.strategies.etf_momentum.weekly_risk_monitor
PYTHONPATH=src python -m finance_decisions.strategies.etf_momentum.backtest --years 8
```

Reports are written to `reports/latest/etf-momentum/{monthly-scanner,weekly-risk-monitor,backtest}/`.

## Backtest

The backtest replays the live code: each month it scans on data up to the
previous close and executes the plan at the next close with costs
(`cost_pct` + `fee_per_trade`); each week in between it runs the risk monitor.
The benchmark (`SXR8.DE` for EU) receives the same contributions.

Use it to check rule changes, not to forecast returns. The ETF list is today's
list (survivorship bias), taxes are ignored, and funds with short Yahoo history
join only after 12+ months of data.

## Configuration

Edit `config/strategy.toml` for normal changes; set `plan.monthly_contribution` to
your real monthly amount. GitHub Actions can override most values through
repository variables (see the root README). Keep the repository private if
`positions.json` contains real holdings.
