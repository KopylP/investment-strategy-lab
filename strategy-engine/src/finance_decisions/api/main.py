"""FastAPI application of the strategy engine. Run: uvicorn finance_decisions.api.main:app"""

from __future__ import annotations

from fastapi import FastAPI

from . import catalog, decide, prices, risk


def create_app() -> FastAPI:
    app = FastAPI(
        title="Investment Lab Strategy Engine",
        version="1",
        description="Strategy decisions, risk checks and market quotes for the Investment Lab backend.",
    )
    app.include_router(catalog.router)
    app.include_router(decide.router)
    app.include_router(risk.router)
    app.include_router(prices.router)

    @app.get("/health", tags=["health"])
    def health() -> dict[str, str]:
        return {"status": "ok"}

    return app


app = create_app()
