"""FastAPI application of the strategy engine. Run: uvicorn finance_decisions.api.main:app"""

from __future__ import annotations

from fastapi import FastAPI

from . import catalog, decide, funds, jobs, parameters_check, price_sets, prices, risk, symbols


def create_app() -> FastAPI:
    app = FastAPI(
        title="Investment Lab Strategy Engine",
        version="1",
        description="Strategy decisions, risk checks (also as background jobs), market quotes and symbol lookup for the Investment Lab backend.",
    )
    app.include_router(catalog.router)
    app.include_router(decide.router)
    app.include_router(risk.router)
    app.include_router(price_sets.router)
    app.include_router(parameters_check.router)
    app.include_router(jobs.router)
    app.include_router(prices.router)
    app.include_router(symbols.router)
    app.include_router(funds.router)

    @app.get("/health", tags=["health"])
    def health() -> dict[str, str]:
        return {"status": "ok"}

    return app


app = create_app()
