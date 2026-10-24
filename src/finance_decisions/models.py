from __future__ import annotations

import json
from dataclasses import dataclass, field
from pathlib import Path
from typing import Any


@dataclass(frozen=True)
class Position:
    ticker: str
    entry_price: float
    shares: float
    entry_date: str | None = None
    currency: str = "USD"

    @classmethod
    def from_dict(cls, data: dict[str, Any]) -> "Position":
        for key in ("ticker", "entry_price", "shares"):
            if key not in data:
                raise ValueError(f"Position is missing required field '{key}': {data}")

        entry_price = float(data["entry_price"])
        shares = float(data["shares"])
        if entry_price <= 0:
            raise ValueError(f"entry_price must be positive, got {entry_price}: {data}")
        if shares <= 0:
            raise ValueError(f"shares must be positive, got {shares}: {data}")

        ticker = str(data["ticker"]).upper()
        currency_val = data.get("currency")
        if currency_val is None or str(currency_val).strip() == "":
            from finance_decisions.helpers import ticker_currency
            currency = ticker_currency(ticker)
        else:
            currency = str(currency_val).upper()

        return cls(
            ticker=ticker,
            entry_price=entry_price,
            shares=shares,
            entry_date=data.get("entry_date"),
            currency=currency,
        )


@dataclass(frozen=True)
class Portfolio:
    """Positions file contents: one entry per purchase (lot) plus uninvested broker cash."""

    lots: list[Position] = field(default_factory=list)
    cash: float = 0.0

    @property
    def positions(self) -> list[Position]:
        return aggregate_positions(self.lots)

    @property
    def tickers(self) -> list[str]:
        return [position.ticker for position in self.positions]


def aggregate_positions(lots: list[Position]) -> list[Position]:
    """Combine lots of the same ticker: total shares, average cost, earliest entry date."""
    grouped: dict[str, list[Position]] = {}
    for lot in lots:
        grouped.setdefault(lot.ticker, []).append(lot)

    positions = []
    for ticker, items in grouped.items():
        shares = sum(item.shares for item in items)
        cost = sum(item.shares * item.entry_price for item in items)
        dates = [item.entry_date for item in items if item.entry_date]
        positions.append(
            Position(
                ticker=ticker,
                entry_price=cost / shares,
                shares=shares,
                entry_date=min(dates) if dates else None,
                currency=items[0].currency,
            )
        )
    return positions


def load_portfolio(path: Path) -> Portfolio:
    if not path.exists():
        raise FileNotFoundError(f"Positions file not found: {path}")

    with path.open(encoding="utf-8") as file:
        payload = json.load(file)

    cash = float(payload.get("cash", 0.0) or 0.0)
    if cash < 0:
        raise ValueError(f"cash must not be negative, got {cash}")
    return Portfolio(
        lots=[Position.from_dict(item) for item in payload.get("positions", [])],
        cash=cash,
    )


def load_positions(path: Path) -> list[Position]:
    return load_portfolio(path).positions
