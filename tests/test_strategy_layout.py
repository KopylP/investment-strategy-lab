from __future__ import annotations

from pathlib import Path

from finance_decisions.config import load_strategy_config
from finance_decisions.strategies.etf_momentum import monthly_scanner, weekly_risk_monitor
from finance_decisions.strategies.etf_momentum.runner import DEFAULT_CONFIG, DEFAULT_POSITIONS


def test_etf_momentum_defaults_live_in_strategy_package(monkeypatch) -> None:
    monkeypatch.delenv("STRATEGY_CONFIG", raising=False)

    config = load_strategy_config()

    assert Path(DEFAULT_CONFIG).exists()
    assert Path(DEFAULT_POSITIONS).exists()
    assert "src/finance_decisions/strategies/etf_momentum" in str(DEFAULT_CONFIG)
    assert "SXRV.DE" in config.etfs or "NQSE.DE" in config.etfs


def test_load_strategy_config_selects_market_defaults(monkeypatch) -> None:
    monkeypatch.delenv("STRATEGY_CONFIG", raising=False)
    
    # Verify EU defaults
    monkeypatch.setenv("STRATEGY_MARKET", "EU")
    config_eu = load_strategy_config()
    assert any(".DE" in ticker for ticker in config_eu.etfs)
    
    # Verify US defaults
    monkeypatch.setenv("STRATEGY_MARKET", "US")
    config_us = load_strategy_config()
    assert "QQQ" in config_us.etfs


def test_ticker_currency() -> None:
    from finance_decisions.helpers import ticker_currency
    assert ticker_currency("SXR8.DE") == "EUR"
    assert ticker_currency("NQSE.DE") == "EUR"
    assert ticker_currency("QDVE.DE") == "EUR"
    assert ticker_currency("SPY") == "USD"
    assert ticker_currency("QQQ") == "USD"
    assert ticker_currency("ISF.L") == "GBP"


def test_etf_momentum_module_entrypoints_are_available() -> None:
    assert monthly_scanner.main.__name__ == "main"
    assert weekly_risk_monitor.main.__name__ == "main"
