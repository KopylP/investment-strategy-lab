from __future__ import annotations

import json
from pathlib import Path

import pytest

from finance_decisions.helpers import money, ticker_currency
from finance_decisions.models import load_portfolio
from finance_decisions.strategies.etf_momentum import backtest, monthly_scanner, weekly_risk_monitor
from finance_decisions.strategies.etf_momentum.config import load_strategy_config
from finance_decisions.strategies.etf_momentum.runner import DEFAULT_CONFIG, DEFAULT_POSITIONS


@pytest.fixture(autouse=True)
def _clean_env(monkeypatch):
    for name in ("STRATEGY_CONFIG", "STRATEGY_MARKET", "ETF_LIST", "TOP_N", "MONTHLY_CONTRIBUTION", "CASH_ETF"):
        monkeypatch.delenv(name, raising=False)


def test_etf_momentum_defaults_live_in_strategy_package() -> None:
    config = load_strategy_config()

    assert Path(DEFAULT_CONFIG).exists()
    assert Path(DEFAULT_POSITIONS).exists()
    assert "src/finance_decisions/strategies/etf_momentum" in str(DEFAULT_CONFIG)
    assert "NQSE.DE" in config.etfs


def test_eu_universe_is_complete_and_has_no_single_stocks() -> None:
    config = load_strategy_config()

    assert "ZIL2.DE" not in config.etfs
    assert "SXRS.DE" in config.etfs
    assert config.cash_etf == "XEON.DE"
    assert config.cash_etf not in config.etfs
    assert set(config.etfs) <= set(config.groups)
    assert set(config.etfs) <= set(config.names)
    assert config.group("SXRV.DE") == config.group("NQSE.DE") == config.group("QDVE.DE")


def test_load_strategy_config_selects_market_defaults(monkeypatch) -> None:
    monkeypatch.setenv("STRATEGY_MARKET", "EU")
    config_eu = load_strategy_config()
    assert all(ticker.endswith(".DE") for ticker in config_eu.etfs)
    assert config_eu.currency == "EUR"

    monkeypatch.setenv("STRATEGY_MARKET", "US")
    config_us = load_strategy_config()
    assert "QQQ" in config_us.etfs
    assert config_us.cash_etf == "BIL"
    assert config_us.currency == "USD"
    assert set(config_us.etfs) <= set(config_us.groups)


def test_environment_overrides_config(monkeypatch) -> None:
    monkeypatch.setenv("TOP_N", "3")
    monkeypatch.setenv("MONTHLY_CONTRIBUTION", "250")
    monkeypatch.setenv("FRACTIONAL_SHARES", "false")

    config = load_strategy_config()

    assert config.scanner.top_n == 3
    assert config.plan.monthly_contribution == 250.0
    assert config.plan.fractional_shares is False


def test_positions_file_supports_lots_and_cash(tmp_path) -> None:
    path = tmp_path / "positions.json"
    path.write_text(
        json.dumps(
            {
                "cash": 12.5,
                "positions": [
                    {"ticker": "aaa.de", "entry_price": 10, "shares": 2, "entry_date": "2026-01-05"},
                    {"ticker": "AAA.DE", "entry_price": 20, "shares": 2, "entry_date": "2026-02-02"},
                ],
            }
        ),
        encoding="utf-8",
    )

    portfolio = load_portfolio(path)

    assert portfolio.cash == 12.5
    assert len(portfolio.lots) == 2
    assert portfolio.tickers == ["AAA.DE"]
    position = portfolio.positions[0]
    assert position.shares == 4
    assert position.entry_price == 15
    assert position.entry_date == "2026-01-05"
    assert position.currency == "EUR"


def test_ticker_currency_and_money() -> None:
    assert ticker_currency("SXR8.DE") == "EUR"
    assert ticker_currency("SPY") == "USD"
    assert ticker_currency("ISF.L") == "GBP"
    assert money(-20.95, "EUR") == "-€20.95"
    assert money(1234.5, "USD", 0) == "$1,234"


def test_etf_momentum_module_entrypoints_are_available() -> None:
    assert monthly_scanner.main.__name__ == "main"
    assert weekly_risk_monitor.main.__name__ == "main"
    assert backtest.main.__name__ == "main"
