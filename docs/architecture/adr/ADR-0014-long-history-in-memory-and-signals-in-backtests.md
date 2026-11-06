# ADR-0014: Long quote history lives only in memory, and the backtest replays reports by signals

- Status: accepted (2026-11-02)
- Complements: ADR-0013

## Context
A backtest must reach back more than 10 years to cover the 2008 crisis. The first option loaded 20 years of daily quotes of each instrument into `marketdata.daily_quotes` once. That is hundreds of thousands of rows, which grow with every instrument and end up in the Home Assistant backups, although they are needed only for the duration of a run. Old prices cannot be stored monthly either: the strategy computes moving averages, volatility and correlations by day, and monthly prices would produce different decisions.

A strategy's risk report comes not only on the schedule but also when a built-in signal fires (VIX, a fall of the account, a fall of a position, ADR-0010). Without signals, a run made fewer risk checks than the service would have made.

## Decision
- The database keeps a daily window, which the daily load collects (3 years and forward). The loading of the long history is removed, and the rows it had already loaded are deleted by a migration.
- `IQuoteHistory.GetQuoteHistoryWithArchive` returns the stored quotes, and when a request reaches further back than this window, `MarketData` takes up to 25 years of daily prices from the provider and keeps them in memory. The limit there is about 500 thousand prices (≈20 MB); an entry is forgotten if it has not been read for 30 minutes, and in any case after 6 hours. The adjusted prices of the archive are scaled so that no invented return appears at the joint with the stored ones.
- Only the strategy evaluation and the backtest call this method. Signals, account assessment and reports read only what is stored, so their behaviour did not change, and a report for the current date does not depend on the availability of the provider. A provider error fails the run, and a failed backtest is retried after a day.
- The backtest replays the reports by signals. A strategy type declares which signals its report comes on. `Signals` gives, through `IBuiltInSignalReplay`, an "observer" with the default thresholds of the built-in signals: the peak window, a re-firing only after recovery, and a position is counted from its first purchase. Each day the backtest passes it the VIX level, the growth of the portfolio and the prices of the positions. The day a signal fires brings a report, as in the service. Thresholds changed by the user for a specific account are not taken into account, because the backtest belongs to the strategy, not to the account.
- A run does not depend on a specific strategy: only the `supportsBacktest` flag of the type switches it on. Decisions come through `/decide`, the frequency is given by the rebalancing report (without a schedule, monthly), and the risk checks give the risk report and its signals. The engine itself handles funds that did not yet exist, so a run simply skips the days without a price.
- A run is in the currency that most of the strategy's funds trade in. The prices of funds, the benchmark and the cash in other currencies are converted at the cross rate from `IFxRates` for each day, as in the live report. The volatility index is not converted, because it is a level, not a price. Missing rates are loaded into the database through `IFxRateBackfill`: this is a few thousand rows per currency, which NBP publishes since 2002. The result's currency is shown by the screen; the frontend does not guess it.
- When the window or the rules of a run change, a migration deletes the stored backtests, and they are recalculated when the strategy is opened.

## Consequences
- The database does not grow from backtests. Only the monthly points of the result are stored.
- Each run loads the long history from the provider once, if it is not in memory.
- Funds that did not exist back then have no history: a set of European UCITS funds starts at about 2012–2013, and the 2008 crisis is not visible in it.
