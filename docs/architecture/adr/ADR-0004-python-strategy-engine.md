# ADR-0004: A Python sidecar as the strategy engine

- Status: accepted (2026-10-25)

## Decision
The existing `finance_decisions` code moves into `strategy-engine/` without changes to its logic and gets a FastAPI wrapper.
.NET calls it through the `IStrategyEngine` port. The engine produces targets (weights, invest fraction, risk signals), not orders;
.NET computes the orders (`SubPortfolioRebalancer`). .NET passes the prices; the engine does not load them itself, except `/prices` for MarketData.

## Consequences
- The legacy GitHub Actions flow works with `strategy-engine/` until the end of the parallel run (stage 9).
- The `/v1/...` contract is versioned, and its JSON Schema is checked on both sides.
