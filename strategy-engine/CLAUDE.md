# Strategy engine (Python `finance_decisions`)

FastAPI service the backend calls for strategy decisions, plus the ETF momentum strategy's own CLI tools.
See `README.md` for the endpoint table and CLI examples.

## Commands (from `strategy-engine/`, uv + Python 3.12)

```bash
uv sync
uv run pytest -q                                          # all tests (no network needed)
uv run pytest -q tests/test_api_contracts.py              # the HTTP contract
uv run uvicorn finance_decisions.api.main:app --reload    # the API alone (the Aspire AppHost normally starts it)
uv run python tools/export_contract.py                    # after changing api/contracts.py
uv run python tools/export_golden_fixtures.py             # after changing etf_momentum/plan.py
```

## Layout

- `src/finance_decisions/api/` — FastAPI app (`main.py`), Pydantic contracts (`contracts.py`), strategy catalog.
- `src/finance_decisions/strategies/<name>/` — everything of one strategy: logic, `decision.py` (what it declares to
  the backend: parameters, reports and their default schedules), config TOMLs, templates. Shared infrastructure stays
  in `src/finance_decisions/` (market data, reports rendering, helpers).
- `contracts/v1/openapi.json` — the published contract; a test fails when it is stale.

## Gotchas

- Changing `api/contracts.py` changes what the .NET backend reads (`Strategies` module): regenerate
  `contracts/v1/openapi.json` and keep the backend in step.
- `etf_momentum/plan.py` is ported to .NET (`SubPortfolioRebalancer`); its golden fixtures live in
  `../backend/tests/InvestmentLab.GoldenTests/Fixtures/` — regenerate them and run the golden tests after a change.
- Strategies must ignore data after the request's `asOf`; prices come in the request, not from the network.
- No Ukrainian in Python: `msg(code, **parameters)` makes a token and the words live in `locales/uk.json` next to the strategy, sent to the backend with the type's descriptor. The command-line reports render tokens themselves with `render`; a test fails on Cyrillic in `.py` files or a code missing from a catalog.
- `plan.py` fixtures hold the rendered words, so the golden tests also prove the wording of the .NET rebalancer.
