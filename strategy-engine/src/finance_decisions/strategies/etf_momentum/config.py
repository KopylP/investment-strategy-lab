from __future__ import annotations

from dataclasses import dataclass, field, fields
import importlib.resources
import os
from pathlib import Path
from typing import Any

try:
    import tomllib
except ModuleNotFoundError:  # pragma: no cover - Python < 3.11 compatibility.
    import tomli as tomllib


@dataclass(frozen=True)
class RiskSettings:
    ma_period: int = 200
    stop_loss_pct: float = -0.08
    drawdown_warn: float = -0.10
    drawdown_exit: float = -0.18
    vix_warn: float = 25.0
    vix_exit: float = 35.0
    portfolio_warn: float = -0.10
    portfolio_exit: float = -0.20
    # Sell only when price closes this far below MA200; between MA200 and the band is a warning.
    ma_exit_band: float = 0.02
    small_loss_warn: float = -0.04
    # Stops and drawdown limits scale with asset volatility relative to this reference.
    vol_reference: float = 0.15


@dataclass(frozen=True)
class ScannerSettings:
    top_n: int = 4
    lookback_months: int = 6
    ma_period: int = 200
    max_volatility: float = 0.35
    max_drawdown: float = -0.20
    max_vix: float = 30.0
    min_sharpe: float = 0.20
    # Absolute momentum: an ETF must beat this return over the lookback to qualify.
    min_momentum: float = 0.0
    hold_buffer_k: int = 8
    max_per_group: int = 1
    # Used only when the cash ETF has too little history to measure the real rate.
    risk_free_rate: float = 0.02
    max_stale_days: int = 5


@dataclass(frozen=True)
class PlanSettings:
    monthly_contribution: float = 0.0
    weak_market_invest_pct: float = 0.5
    trim_band: float = 0.10
    min_trade: float = 25.0
    fractional_shares: bool = True


@dataclass(frozen=True)
class BacktestSettings:
    years: int = 10
    cost_pct: float = 0.001
    fee_per_trade: float = 1.0
    benchmark: str = ""


@dataclass(frozen=True)
class StrategyConfig:
    etfs: list[str]
    risk: RiskSettings
    scanner: ScannerSettings
    plan: PlanSettings = field(default_factory=PlanSettings)
    backtest: BacktestSettings = field(default_factory=BacktestSettings)
    cash_etf: str | None = None
    currency: str = "EUR"
    names: dict[str, str] = field(default_factory=dict)
    groups: dict[str, str] = field(default_factory=dict)

    def name(self, ticker: str) -> str:
        return self.names.get(ticker, ticker)

    def group(self, ticker: str) -> str:
        return self.groups.get(ticker, ticker)


# Environment variable names that differ from the upper-cased field name.
_RISK_ENV = {"ma_period": "RISK_MA_PERIOD"}
_SCANNER_ENV = {"ma_period": "SCANNER_MA_PERIOD"}
_BACKTEST_ENV = {"years": "BACKTEST_YEARS", "cost_pct": "BACKTEST_COST_PCT", "fee_per_trade": "BACKTEST_FEE", "benchmark": "BACKTEST_BENCHMARK"}


def load_strategy_config(path: Path | None = None) -> StrategyConfig:
    path = path or _default_config_path()
    payload = _load_toml(path)

    market = _market()
    strategy = payload.get("strategy", {})
    etfs = _env_list("ETF_LIST") or strategy.get("etfs")
    if not etfs:
        raise ValueError(f"{path} names no etfs and ETF_LIST is empty")
    cash_etf = os.environ.get("CASH_ETF") or strategy.get("cash_etf")

    return StrategyConfig(
        etfs=[str(ticker).upper() for ticker in etfs],
        risk=_build(RiskSettings, payload.get("risk", {}), _RISK_ENV),
        scanner=_build(ScannerSettings, payload.get("scanner", {}), _SCANNER_ENV),
        plan=_build(PlanSettings, payload.get("plan", {}), {}),
        backtest=_build(BacktestSettings, payload.get("backtest", {}), _BACKTEST_ENV),
        cash_etf=str(cash_etf).upper() if cash_etf else None,
        currency=str(strategy.get("currency", "USD" if market == "US" else "EUR")).upper(),
        names={str(k).upper(): str(v) for k, v in payload.get("names", {}).items()},
        groups={str(k).upper(): str(v) for k, v in payload.get("groups", {}).items()},
    )


def _build(cls: type, section: dict[str, Any], env_names: dict[str, str]) -> Any:
    values = {}
    for item in fields(cls):
        default = item.default
        raw = os.environ.get(env_names.get(item.name, item.name.upper()))
        value = raw if raw not in (None, "") else section.get(item.name, default)
        values[item.name] = _cast(value, type(default))
    return cls(**values)


def _cast(value: Any, target: type) -> Any:
    if target is bool:
        if isinstance(value, str):
            return value.strip().lower() in {"1", "true", "yes", "on"}
        return bool(value)
    return target(value)


def _market() -> str:
    return os.environ.get("STRATEGY_MARKET", "EU").upper()


def _load_toml(path: Path) -> dict:
    if not path.exists():
        return {}
    with path.open("rb") as file:
        return tomllib.load(file)


def _default_config_path() -> Path:
    configured = os.environ.get("STRATEGY_CONFIG")
    if configured:
        return Path(configured)

    config_name = "strategy_us.toml" if _market() == "US" else "strategy.toml"
    return Path(importlib.resources.files(__package__) / "config" / config_name)


def _env_list(name: str) -> list[str] | None:
    value = os.environ.get(name)
    if not value:
        return None
    return [item.strip() for item in value.split(",") if item.strip()]
