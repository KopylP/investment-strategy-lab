from __future__ import annotations

from dataclasses import dataclass
import importlib.resources
import os
from pathlib import Path

try:
    import tomllib
except ModuleNotFoundError:  # pragma: no cover - Python < 3.11 compatibility.
    import tomli as tomllib


DEFAULT_ETFS_US = [
    "SPY",
    "QQQ",
    "IWM",
    "EFA",
    "EEM",
    "GLD",
    "SLV",
    "TLT",
    "IEF",
    "LQD",
    "VNQ",
    "USO",
    "DBA",
    "XLE",
    "XLK",
    "XLV",
    "XLF",
    "XLU",
]

DEFAULT_ETFS_EU = [
    "SXR8.DE",
    "SXRV.DE",
    "IUSN.DE",
    "EUN2.DE",
    "IS3N.DE",
    "4GLD.DE",
    "ZIL2.DE",
    "IS04.DE",
    "IUS7.DE",
    "IBCI.DE",
    "IQQA.DE",
    "XDJE.DE",
    "QDVE.DE",
    "QDVF.DE",
    "QDVG.DE",
    "QDVI.DE",
]

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


@dataclass(frozen=True)
class ScannerSettings:
    top_n: int = 4
    lookback_months: int = 6
    ma_period: int = 200
    max_volatility: float = 0.35
    max_drawdown: float = -0.20
    max_vix: float = 30.0
    min_sharpe: float = 0.20
    hold_buffer_k: int = 8


@dataclass(frozen=True)
class StrategyConfig:
    etfs: list[str]
    risk: RiskSettings
    scanner: ScannerSettings


def load_strategy_config(path: Path | None = None) -> StrategyConfig:
    path = path or _default_config_path()
    payload = _load_toml(path)

    market = os.environ.get("STRATEGY_MARKET", "EU").upper()
    default_etfs = DEFAULT_ETFS_US if market == "US" else DEFAULT_ETFS_EU
    etfs = _env_list("ETF_LIST") or payload.get("strategy", {}).get("etfs", default_etfs)
    risk_payload = payload.get("risk", {})
    scanner_payload = payload.get("scanner", {})

    return StrategyConfig(
        etfs=[str(ticker).upper() for ticker in etfs],
        risk=RiskSettings(
            ma_period=_env_int("RISK_MA_PERIOD", risk_payload.get("ma_period", RiskSettings.ma_period)),
            stop_loss_pct=_env_float("STOP_LOSS_PCT", risk_payload.get("stop_loss_pct", RiskSettings.stop_loss_pct)),
            drawdown_warn=_env_float("DRAWDOWN_WARN", risk_payload.get("drawdown_warn", RiskSettings.drawdown_warn)),
            drawdown_exit=_env_float("DRAWDOWN_EXIT", risk_payload.get("drawdown_exit", RiskSettings.drawdown_exit)),
            vix_warn=_env_float("VIX_WARN", risk_payload.get("vix_warn", RiskSettings.vix_warn)),
            vix_exit=_env_float("VIX_EXIT", risk_payload.get("vix_exit", RiskSettings.vix_exit)),
            portfolio_warn=_env_float("PORTFOLIO_WARN", risk_payload.get("portfolio_warn", RiskSettings.portfolio_warn)),
            portfolio_exit=_env_float("PORTFOLIO_EXIT", risk_payload.get("portfolio_exit", RiskSettings.portfolio_exit)),
        ),
        scanner=ScannerSettings(
            top_n=_env_int("TOP_N", scanner_payload.get("top_n", ScannerSettings.top_n)),
            lookback_months=_env_int(
                "LOOKBACK_MONTHS",
                scanner_payload.get("lookback_months", ScannerSettings.lookback_months),
            ),
            ma_period=_env_int("SCANNER_MA_PERIOD", scanner_payload.get("ma_period", ScannerSettings.ma_period)),
            max_volatility=_env_float(
                "MAX_VOLATILITY",
                scanner_payload.get("max_volatility", ScannerSettings.max_volatility),
            ),
            max_drawdown=_env_float("MAX_DRAWDOWN", scanner_payload.get("max_drawdown", ScannerSettings.max_drawdown)),
            max_vix=_env_float("MAX_VIX", scanner_payload.get("max_vix", ScannerSettings.max_vix)),
            min_sharpe=_env_float("MIN_SHARPE", scanner_payload.get("min_sharpe", ScannerSettings.min_sharpe)),
            hold_buffer_k=_env_int("HOLD_BUFFER_K", scanner_payload.get("hold_buffer_k", ScannerSettings.hold_buffer_k)),
        ),
    )


def _load_toml(path: Path) -> dict:
    if not path.exists():
        return {}
    with path.open("rb") as file:
        return tomllib.load(file)


def _default_config_path() -> Path:
    configured = os.environ.get("STRATEGY_CONFIG")
    if configured:
        return Path(configured)
    
    market = os.environ.get("STRATEGY_MARKET", "EU").upper()
    config_name = "strategy_us.toml" if market == "US" else "strategy.toml"
    
    return Path(
        importlib.resources.files("finance_decisions.strategies.etf_momentum")
        / "config"
        / config_name
    )


def _env_float(name: str, default: float) -> float:
    value = os.environ.get(name)
    return float(value) if value is not None and value != "" else float(default)


def _env_int(name: str, default: int) -> int:
    value = os.environ.get(name)
    return int(value) if value is not None and value != "" else int(default)


def _env_list(name: str) -> list[str] | None:
    value = os.environ.get(name)
    if not value:
        return None
    return [item.strip() for item in value.split(",") if item.strip()]
