from __future__ import annotations

from datetime import date

import pandas as pd

from finance_decisions import market_data


def _multi_index_download(symbols: list[str]) -> pd.DataFrame:
    index = pd.to_datetime(["2026-10-01", "2026-10-02"]).tz_localize("Europe/Berlin")
    columns = pd.MultiIndex.from_product([["Adj Close", "Close"], symbols])
    data = [[1.0 + i for i in range(len(columns))], [2.0 + i for i in range(len(columns))]]
    return pd.DataFrame(data, index=index, columns=columns)


def test_download_quotes_reads_close_and_adjusted_close_per_symbol(monkeypatch) -> None:
    calls = {}

    def fake_download(symbols, **kwargs):
        calls.update(kwargs)
        return _multi_index_download(symbols)

    monkeypatch.setattr(market_data.yf, "download", fake_download)

    result = market_data.download_quotes(["AAA", "BBB"], date(2026, 10, 1), date(2026, 10, 2))

    assert calls["auto_adjust"] is False
    assert calls["start"] == "2026-10-01"
    assert calls["end"] == "2026-10-03"  # yfinance treats end as exclusive
    assert list(result) == ["AAA", "BBB"]
    assert list(result["BBB"]["adjusted_close"]) == [2.0, 3.0]
    assert list(result["BBB"]["close"]) == [4.0, 5.0]
    assert result["AAA"].index.tz is None
    assert list(result["AAA"].index) == list(pd.to_datetime(["2026-10-01", "2026-10-02"]))


def test_download_quotes_leaves_out_symbols_without_data(monkeypatch) -> None:
    def fake_download(symbols, **kwargs):
        frame = _multi_index_download(symbols)
        frame[("Close", "NOPE")] = float("nan")
        frame[("Adj Close", "NOPE")] = float("nan")
        return frame

    monkeypatch.setattr(market_data.yf, "download", fake_download)

    result = market_data.download_quotes(["AAA", "NOPE"], date(2026, 10, 1))

    assert list(result) == ["AAA"]


def test_download_quotes_handles_flat_columns_for_one_symbol(monkeypatch) -> None:
    def fake_download(symbols, **kwargs):
        index = pd.to_datetime(["2026-10-01"])
        return pd.DataFrame({"Adj Close": [9.5], "Close": [10.0]}, index=index)

    monkeypatch.setattr(market_data.yf, "download", fake_download)

    result = market_data.download_quotes(["AAA"], date(2026, 10, 1))

    assert list(result["AAA"]["close"]) == [10.0]
    assert list(result["AAA"]["adjusted_close"]) == [9.5]


def test_symbol_info_carries_yahoos_description_when_there_is_one(monkeypatch) -> None:
    infos = {
        "AVGO": {"quoteType": "EQUITY", "currency": "USD", "longName": "Broadcom Inc.", "longBusinessSummary": " Broadcom Inc. designs semiconductors. \n"},
        "XDJE.DE": {"quoteType": "ETF", "currency": "EUR", "shortName": "Xtrackers MSCI Japan ESG"},
    }

    class FakeTicker:
        def __init__(self, symbol: str) -> None:
            self.info = infos.get(symbol, {})

    monkeypatch.setattr(market_data.yf, "Ticker", FakeTicker)

    assert market_data.symbol_info("AVGO") == {
        "name": "Broadcom Inc.", "currency": "USD", "quote_type": "EQUITY", "description": "Broadcom Inc. designs semiconductors.",
    }
    assert market_data.symbol_info("XDJE.DE")["description"] is None
    assert market_data.symbol_info("NOPE") is None
