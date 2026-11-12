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


def _quoted_in(currencies: dict[str, str], asked: list[str] | None = None):
    def quote_currency(symbol: str) -> str:
        if asked is not None:
            asked.append(symbol)
        if symbol not in currencies:
            raise market_data.QuoteCurrencyUnknown(symbol)
        return currencies[symbol]

    return quote_currency


def test_london_quotes_in_pence_come_back_in_pounds(monkeypatch) -> None:
    asked: list[str] = []
    monkeypatch.setattr(market_data.yf, "download", lambda symbols, **kwargs: _multi_index_download(symbols))
    monkeypatch.setattr(market_data, "quote_currency", _quoted_in({"VUSA.L": "GBp"}, asked))

    result = market_data.download_quotes(["AAA", "VUSA.L"], date(2026, 10, 1))

    assert list(result["VUSA.L"]["adjusted_close"]) == [0.02, 0.03]
    assert list(result["VUSA.L"]["close"]) == [0.04, 0.05]
    assert list(result["AAA"]["close"]) == [3.0, 4.0]
    assert asked == ["VUSA.L"]


def test_a_london_symbol_quoted_in_dollars_keeps_its_prices(monkeypatch) -> None:
    monkeypatch.setattr(market_data.yf, "download", lambda symbols, **kwargs: _multi_index_download(symbols))
    monkeypatch.setattr(market_data, "quote_currency", _quoted_in({"CSPX.L": "USD"}))

    result = market_data.download_quotes(["CSPX.L"], date(2026, 10, 1))

    assert list(result["CSPX.L"]["close"]) == [2.0, 3.0]


def test_cents_and_agorot_come_back_in_rand_and_shekels(monkeypatch) -> None:
    monkeypatch.setattr(market_data.yf, "download", lambda symbols, **kwargs: _multi_index_download(symbols))
    monkeypatch.setattr(market_data, "quote_currency", _quoted_in({"NPN.JO": "ZAc", "TEVA.TA": "ILA"}))

    result = market_data.download_quotes(["NPN.JO", "TEVA.TA"], date(2026, 10, 1))

    assert list(result["NPN.JO"]["close"]) == [0.03, 0.04]
    assert list(result["TEVA.TA"]["close"]) == [0.04, 0.05]


def test_quotes_of_a_london_symbol_whose_quote_currency_is_unknown_are_left_out_rather_than_guessed(monkeypatch) -> None:
    monkeypatch.setattr(market_data.yf, "download", lambda symbols, **kwargs: _multi_index_download(symbols))
    monkeypatch.setattr(market_data, "quote_currency", _quoted_in({}))

    result = market_data.download_quotes(["AAA", "VUSA.L"], date(2026, 10, 1))

    assert list(result) == ["AAA"]


def test_close_prices_for_the_command_line_are_in_pounds_too(monkeypatch) -> None:
    monkeypatch.setattr(market_data.yf, "download", lambda tickers, **kwargs: _multi_index_download(tickers))
    monkeypatch.setattr(market_data, "quote_currency", _quoted_in({"VUSA.L": "GBp"}))

    prices = market_data.download_close_prices(["AAA", "VUSA.L"], period="1y")

    assert list(prices["VUSA.L"]) == [0.04, 0.05]
    assert list(prices["AAA"]) == [3.0, 4.0]


def test_the_quote_currency_comes_from_yahoos_chart_metadata_and_is_asked_once(monkeypatch) -> None:
    created: list[str] = []

    class FakeTicker:
        def __init__(self, symbol: str) -> None:
            created.append(symbol)
            self.fast_info = {"currency": "GBp"}

    monkeypatch.setattr(market_data.yf, "Ticker", FakeTicker)
    market_data.quote_currency.cache_clear()

    assert market_data.quote_currency("ISF.L") == "GBp"
    assert market_data.quote_currency("ISF.L") == "GBp"
    assert created == ["ISF.L"]
    market_data.quote_currency.cache_clear()


def test_an_unanswered_quote_currency_is_unknown_and_asked_again_next_time(monkeypatch) -> None:
    class SilentTicker:
        def __init__(self, symbol: str) -> None:
            self.fast_info = {"currency": None}

    monkeypatch.setattr(market_data.yf, "Ticker", SilentTicker)
    market_data.quote_currency.cache_clear()

    for _ in range(2):
        try:
            market_data.quote_currency("ISF.L")
            raise AssertionError("an unknown quote currency must not pass")
        except market_data.QuoteCurrencyUnknown:
            pass
    assert market_data.quote_currency.cache_info().currsize == 0


def test_a_symbol_quoted_in_pence_is_described_in_pounds(monkeypatch) -> None:
    class FakeTicker:
        def __init__(self, symbol: str) -> None:
            self.info = {"quoteType": "EQUITY", "currency": "GBp", "longName": "Shell plc"}

    monkeypatch.setattr(market_data.yf, "Ticker", FakeTicker)

    assert market_data.symbol_info("SHEL.L")["currency"] == "GBP"
