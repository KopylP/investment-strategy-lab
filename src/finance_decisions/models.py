from __future__ import annotations

import json
from dataclasses import dataclass
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

        return cls(
            ticker=str(data["ticker"]).upper(),
            entry_price=entry_price,
            shares=shares,
            entry_date=data.get("entry_date"),
            currency=str(data.get("currency", "USD")).upper(),
        )


def load_positions(path: Path) -> list[Position]:
    if not path.exists():
        raise FileNotFoundError(f"Positions file not found: {path}")

    with path.open(encoding="utf-8") as file:
        payload = json.load(file)

    return [Position.from_dict(item) for item in payload.get("positions", [])]
