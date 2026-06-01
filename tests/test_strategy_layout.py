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
    assert "QQQ" in config.etfs


def test_etf_momentum_module_entrypoints_are_available() -> None:
    assert monthly_scanner.main.__name__ == "main"
    assert weekly_risk_monitor.main.__name__ == "main"
