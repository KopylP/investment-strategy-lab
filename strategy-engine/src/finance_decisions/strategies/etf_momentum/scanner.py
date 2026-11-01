from __future__ import annotations

from dataclasses import dataclass, field
from datetime import date

import numpy as np
import pandas as pd

from finance_decisions.helpers import (
    completed_month_closes,
    last_valid_date,
    latest_moving_average,
    latest_value,
)
from finance_decisions.messages import msg
from .config import ScannerSettings

STATUS_SELECTED = "selected"
STATUS_BUFFER = "buffer"
STATUS_GROUP_BLOCKED = "group_blocked"
STATUS_RESERVE = "reserve"
STATUS_FAILED = "failed"

TABLE_COLUMNS = [
    "Name",
    "Group",
    "Price",
    "Momentum",
    "Return 1M",
    "Return 3M",
    "Return 12M",
    "Volatility",
    "Sharpe",
    "Drawdown",
    "Above MA200",
    "Last Date",
    "Rank",
    "Status",
    "Reason",
    "Weight",
]

MARKET_TICKERS = ("SPY", "^VIX")


@dataclass(frozen=True)
class ScanResult:
    market_ok: bool
    spy_above_ma: bool
    vix: float | None
    table: pd.DataFrame
    eligible: pd.DataFrame
    settings: ScannerSettings | None = None
    as_of: date | None = None
    spy_price: float | None = None
    spy_ma: float | None = None
    market_reasons: list[str] = field(default_factory=list)
    cash_weight: float = 0.0
    risk_free_rate: float = 0.0


def scan_etfs(
    prices: pd.DataFrame,
    settings: ScannerSettings | None = None,
    etfs: list[str] | None = None,
    current_positions: list[str] | None = None,
    *,
    groups: dict[str, str] | None = None,
    names: dict[str, str] | None = None,
    cash_etf: str | None = None,
    now: pd.Timestamp | None = None,
) -> ScanResult:
    settings = settings or ScannerSettings()
    groups = groups or {}
    names = names or {}
    prices = prices.dropna(how="all")
    if prices.empty:
        raise ValueError("No price data available.")

    as_of = prices.index[-1]
    now = now if now is not None else pd.Timestamp.now()

    spy_price, spy_ma, spy_above_ma, vix, market_reasons = _market_check(prices, as_of, settings)
    market_ok = spy_above_ma and vix is not None and vix < settings.max_vix
    risk_free = _risk_free_rate(prices, cash_etf, settings.risk_free_rate)

    excluded = set(MARKET_TICKERS) | ({cash_etf} if cash_etf else set())
    scan_tickers = etfs if etfs is not None else [c for c in prices.columns if c not in excluded]

    rows = [
        _ticker_row(prices, ticker, settings, as_of, now, risk_free, names.get(ticker, ticker), groups.get(ticker, ticker))
        for ticker in scan_tickers
    ]
    table = pd.DataFrame(rows, columns=["ETF", *TABLE_COLUMNS]).set_index("ETF")
    selected = _select(table, settings, current_positions or [])

    table = _sort_table(table)
    eligible = table.loc[[t for t in table.index if t in selected]].copy()

    return ScanResult(
        market_ok=market_ok,
        spy_above_ma=spy_above_ma,
        vix=vix,
        table=table,
        eligible=eligible,
        settings=settings,
        as_of=as_of.date(),
        spy_price=spy_price,
        spy_ma=spy_ma,
        market_reasons=market_reasons,
        cash_weight=max(0.0, 1.0 - len(selected) / settings.top_n),
        risk_free_rate=risk_free,
    )


def _market_check(
    prices: pd.DataFrame, as_of: pd.Timestamp, settings: ScannerSettings
) -> tuple[float | None, float | None, bool, float | None, list[str]]:
    reasons: list[str] = []

    spy_price = spy_ma = None
    spy_above_ma = False
    if "SPY" in prices.columns and not _is_stale(prices["SPY"], as_of, settings.max_stale_days):
        spy_price = latest_value(prices["SPY"])
        spy_ma = latest_moving_average(prices["SPY"], settings.ma_period)
    if spy_price is not None and spy_ma is not None:
        spy_above_ma = spy_price > spy_ma
        if spy_above_ma:
            reasons.append(msg("etf_momentum.market.spy_above", price=f"{spy_price:.2f}", average=f"{spy_ma:.2f}"))
        else:
            reasons.append(msg("etf_momentum.market.spy_below", price=f"{spy_price:.2f}", average=f"{spy_ma:.2f}"))
    else:
        reasons.append(msg("etf_momentum.market.spy_missing"))

    # The last row can belong to a day when only European exchanges traded, so read the
    # last valid VIX close instead of the last row. Missing VIX fails the check.
    vix = None
    if "^VIX" in prices.columns and not _is_stale(prices["^VIX"], as_of, settings.max_stale_days):
        vix = latest_value(prices["^VIX"])
    if vix is None:
        reasons.append(msg("etf_momentum.market.vix_missing"))
    elif vix < settings.max_vix:
        reasons.append(msg("etf_momentum.market.vix_calm", vix=f"{vix:.1f}", limit=f"{settings.max_vix:.0f}"))
    else:
        reasons.append(msg("etf_momentum.market.vix_stress", vix=f"{vix:.1f}", limit=f"{settings.max_vix:.0f}"))

    return spy_price, spy_ma, spy_above_ma, vix, reasons


def _risk_free_rate(prices: pd.DataFrame, cash_etf: str | None, fallback: float) -> float:
    if cash_etf and cash_etf in prices.columns:
        clean = prices[cash_etf].dropna()
        if len(clean) >= 253 and clean.iloc[-253] > 0:
            return float(clean.iloc[-1] / clean.iloc[-253] - 1)
    return fallback


def _is_stale(series: pd.Series, as_of: pd.Timestamp, max_days: int) -> bool:
    last = last_valid_date(series)
    return last is None or (as_of - last).days > max_days


def _ticker_row(
    prices: pd.DataFrame,
    ticker: str,
    settings: ScannerSettings,
    as_of: pd.Timestamp,
    now: pd.Timestamp,
    risk_free: float,
    name: str,
    group: str,
) -> dict:
    row = {column: np.nan for column in TABLE_COLUMNS}
    row.update({"ETF": ticker, "Name": name, "Group": group, "Above MA200": False, "Last Date": None, "Status": STATUS_FAILED, "Reason": ""})

    series = prices[ticker].dropna() if ticker in prices.columns else pd.Series(dtype=float)
    if series.empty:
        row["Reason"] = msg("etf_momentum.scan.no_data")
        return row

    price = float(series.iloc[-1])
    last = series.index[-1]
    moving_average = latest_moving_average(series, settings.ma_period)
    monthly = completed_month_closes(series, now)

    def trailing(months: int) -> float:
        if len(monthly) < months + 1 or monthly.iloc[-months - 1] <= 0:
            return np.nan
        return float(monthly.iloc[-1] / monthly.iloc[-months - 1] - 1)

    daily = series.tail(253).pct_change().dropna()
    volatility = float(daily.std() * np.sqrt(252)) if len(daily) >= 20 else np.nan
    sharpe = (float(daily.mean() * 252) - risk_free) / volatility if volatility and volatility > 0 else np.nan
    drawdown = price / float(series.tail(252).max()) - 1

    row.update(
        {
            "Price": price,
            "Momentum": trailing(settings.lookback_months),
            "Return 1M": trailing(1),
            "Return 3M": trailing(3),
            "Return 12M": trailing(12),
            "Volatility": volatility,
            "Sharpe": sharpe,
            "Drawdown": drawdown,
            "Above MA200": bool(moving_average is not None and price > moving_average),
            "Last Date": last.date(),
        }
    )

    failures = []
    if (as_of - last).days > settings.max_stale_days:
        failures.append(msg("etf_momentum.scan.stale", date=f"{last:%d.%m.%Y}"))
    if moving_average is None:
        failures.append(msg("etf_momentum.scan.short_history_ma"))
    elif not row["Above MA200"]:
        failures.append(msg("etf_momentum.scan.below_ma"))
    momentum = row["Momentum"]
    if pd.isna(momentum):
        failures.append(msg("etf_momentum.scan.short_history_momentum"))
    elif momentum <= settings.min_momentum:
        failures.append(msg("etf_momentum.scan.low_momentum", momentum=f"{momentum:+.1%}", limit=f"{settings.min_momentum:+.0%}"))
    if pd.isna(volatility):
        failures.append(msg("etf_momentum.scan.short_history_volatility"))
    elif volatility >= settings.max_volatility:
        failures.append(msg("etf_momentum.scan.high_volatility", volatility=f"{volatility:.0%}", limit=f"{settings.max_volatility:.0%}"))
    if drawdown <= settings.max_drawdown:
        failures.append(msg("etf_momentum.scan.deep_drawdown", drawdown=f"{drawdown:.1%}", limit=f"{settings.max_drawdown:.0%}"))
    if pd.isna(sharpe):
        failures.append(msg("etf_momentum.scan.no_sharpe"))
    elif sharpe < settings.min_sharpe:
        failures.append(msg("etf_momentum.scan.low_sharpe", sharpe=f"{sharpe:.2f}", limit=f"{settings.min_sharpe:.2f}"))

    if failures:
        row["Reason"] = msg("etf_momentum.scan.failures", items=failures)
    else:
        row["Status"] = STATUS_RESERVE
    return row


def _select(table: pd.DataFrame, settings: ScannerSettings, current_positions: list[str]) -> list[str]:
    """Rank qualified ETFs, apply the hold buffer and the per-group cap, and mark every row."""
    qualified = table[table["Status"] == STATUS_RESERVE].sort_values("Momentum", ascending=False)
    ranks = {ticker: index + 1 for index, ticker in enumerate(qualified.index)}
    for ticker, rank in ranks.items():
        table.loc[ticker, "Rank"] = rank

    # Held ETFs keep priority in the order given (largest holding first), so when two
    # holdings share a group the bigger one stays and no large same-bet swap is made.
    buffer = [t for t in dict.fromkeys(current_positions) if t in ranks and ranks[t] <= settings.hold_buffer_k]
    order = buffer + [t for t in qualified.index if t not in buffer]

    selected: list[str] = []
    group_members: dict[str, list[str]] = {}
    for ticker in order:
        rank = ranks[ticker]
        group = table.loc[ticker, "Group"]
        members = group_members.setdefault(group, [])
        if len(members) >= settings.max_per_group:
            table.loc[ticker, "Status"] = STATUS_GROUP_BLOCKED
            table.loc[ticker, "Reason"] = msg("etf_momentum.scan.group_taken", group=group, members=", ".join(members))
            continue
        if len(selected) >= settings.top_n:
            table.loc[ticker, "Reason"] = msg("etf_momentum.scan.stronger_exist", rank=rank)
            continue

        selected.append(ticker)
        members.append(ticker)
        table.loc[ticker, "Weight"] = 1.0 / settings.top_n
        if ticker in buffer and rank > settings.top_n:
            table.loc[ticker, "Status"] = STATUS_BUFFER
            table.loc[ticker, "Reason"] = msg("etf_momentum.scan.held_in_buffer", rank=rank, buffer=settings.hold_buffer_k)
        else:
            table.loc[ticker, "Status"] = STATUS_SELECTED
            table.loc[ticker, "Reason"] = msg("etf_momentum.scan.ranked", rank=rank)
    return selected


def _sort_table(table: pd.DataFrame) -> pd.DataFrame:
    ranked = table[table["Rank"].notna()].sort_values("Rank")
    unranked = table[table["Rank"].isna()].sort_values("Momentum", ascending=False, na_position="last")
    return pd.concat([ranked, unranked])
