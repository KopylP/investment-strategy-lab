# ADR-0025: Replay price sets in the strategy engine

- Status: accepted (2026-11-07)

## Context

A backtest asks the engine for a decision on every rebalance and a risk review on every review day: about 64 calls per
replayed year. Each call carried the whole price window the strategy may look at — two or more years of daily closes of
every fund, the benchmark, the cash fund and extra series, 380–580 KB of JSON. For each call the backend re-read those
quotes from the database (archive included), converted held funds into the portfolio currency, serialized the window,
and the engine parsed it again. Measured on the add-on on 2026-11-07, this made up most of the ~57 ms per call, while
the prices of consecutive calls of one replay differ only by the days that passed.

## Decision

- **A replay uploads its prices once.** `POST /v1/strategies/{code}/price-sets` stores the series of a replay — every
  series a call would carry, over the earliest call's window to the replay's end, with portfolio-currency valuations for
  every fund quoted in another currency — and returns an id.
- **A call names the set instead of carrying it.** `decide` and `risk` (and their jobs) take `priceSetId` with
  `historyFrom` and `asOf`; the engine cuts the stored series to `[historyFrom, asOf]` and evaluates exactly the
  context a full call would have carried. The window's start is part of the call because indicators that average from
  the first value (EMA, RSI) depend on it.
- **No lookahead:** the cut at `asOf` is the same one a full call goes through, so a strategy never sees later prices
  although the set holds them. A parity test compares both paths on every date of a fixture for every indicator kind.
- **Sets are kept in memory for a while:** the engine forgets a set some time after its last use, and holds a bounded
  number of them. An unknown or expired id is answered with 404 and the code `price_set.unknown`; the backend uploads
  the set again and retries the call once.
- **Only replays use sets.** Live reports keep sending the full context: they are a few calls a day, and their prices
  must be the freshest stored.
- **The set is the backend's to build:** the Strategies module builds it with the same history window and archive rules
  as a full call, so the engine's answer does not depend on which path was taken.

## Consequences

- The engine becomes stateful for the length of a replay. A restart in the middle costs one upload and a retry, not a
  failed backtest.
- A set holds valuations for funds that are not held; the engine reads valuations only for holdings, so they change
  nothing. A fund whose valuation cannot be converted makes the upload fail, and that replay falls back to full calls.
- One engine instance is assumed, as in the add-on: several instances behind a balancer would need a shared store or
  sticky routing.
