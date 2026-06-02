from __future__ import annotations

from dataclasses import dataclass

import numpy as np
import pandas as pd

from finance_decisions.helpers import latest_moving_average, latest_value, safe_division
from .config import ScannerSettings


@dataclass(frozen=True)
class ScanResult:
    market_ok: bool
    spy_above_ma: bool
    vix: float | None
    table: pd.DataFrame
    eligible: pd.DataFrame
    settings: ScannerSettings | None = None


def scan_etfs(
    prices: pd.DataFrame,
    settings: ScannerSettings | None = None,
    etfs: list[str] | None = None,
    current_positions: list[str] | None = None,
) -> ScanResult:
    settings = settings or ScannerSettings()
    if prices.empty:
        raise ValueError("No price data available.")

    spy_latest = latest_value(prices["SPY"]) if "SPY" in prices.columns else None
    spy_ma = latest_moving_average(prices["SPY"], settings.ma_period) if "SPY" in prices.columns else None
    spy_above_ma = bool(spy_latest > spy_ma) if spy_latest is not None and spy_ma is not None else False
    today = prices.iloc[-1]
    vix_value = today.get("^VIX")
    vix = float(vix_value) if pd.notna(vix_value) else None
    market_ok = spy_above_ma and (vix is None or vix < settings.max_vix)

    scan_tickers = etfs if etfs is not None else [column for column in prices.columns if column not in ("SPY", "^VIX")]

    rows = []
    for ticker in scan_tickers:
        if ticker not in prices.columns:
            continue
        latest_price = latest_value(prices[ticker])
        moving_average = latest_moving_average(prices[ticker], settings.ma_period)
        if latest_price is None:
            continue

        # Resample individually to prevent calendar holiday NaN dropping
        ticker_prices = prices[ticker].dropna()
        monthly_prices = ticker_prices.resample("ME").last()
        if len(monthly_prices) < settings.lookback_months + 2:
            continue

        monthly_returns = monthly_prices.pct_change().dropna()
        volatility = monthly_returns.std() * np.sqrt(12)
        rows.append(
            {
                "ETF": ticker,
                "Price": round(latest_price, 2),
                "Momentum 6M": safe_division(
                    monthly_prices.iloc[-2],
                    monthly_prices.iloc[-settings.lookback_months - 2],
                ) - 1,
                "Return 1M": safe_division(
                    monthly_prices.iloc[-1], monthly_prices.iloc[-2]
                ) - 1,
                "Return 3M": safe_division(
                    monthly_prices.iloc[-1], monthly_prices.iloc[-4]
                ) - 1
                if len(monthly_prices) >= 4
                else np.nan,
                "Return 12M": safe_division(
                    monthly_prices.iloc[-1], monthly_prices.iloc[-13]
                ) - 1
                if len(monthly_prices) >= 13
                else np.nan,
                "Volatility": volatility,
                "Sharpe": (monthly_returns.mean() * 12) / volatility
                if volatility > 0
                else np.nan,
                "Drawdown": ((monthly_prices - monthly_prices.cummax()) / monthly_prices.cummax()).iloc[-1],
                "Above MA200": bool(latest_price > moving_average) if moving_average is not None else False,
            }
        )

    table = pd.DataFrame(rows)
    if table.empty:
        table = pd.DataFrame(
            columns=[
                "ETF",
                "Price",
                "Momentum 6M",
                "Return 1M",
                "Return 3M",
                "Return 12M",
                "Volatility",
                "Sharpe",
                "Drawdown",
                "Above MA200",
                "Rank",
            ]
        ).set_index("ETF")
        eligible = table
    else:
        table = table.set_index("ETF").sort_values("Momentum 6M", ascending=False)
        qualified = table[
            (table["Above MA200"])
            & (table["Volatility"] < settings.max_volatility)
            & (table["Drawdown"] > settings.max_drawdown)
            & (table["Sharpe"] >= settings.min_sharpe)
        ].copy()

        qualified["Rank"] = range(1, len(qualified) + 1)
        table["Rank"] = np.nan
        table.loc[qualified.index, "Rank"] = qualified["Rank"]

        current_positions = current_positions or []
        buffer_holds = [
            t for t in current_positions
            if t in qualified.index and qualified.loc[t, "Rank"] <= settings.hold_buffer_k
        ]

        selected = []
        # Add buffer holds up to top_n
        for t in buffer_holds:
            if len(selected) < settings.top_n:
                selected.append(t)

        # Fill the remaining slots with the highest-ranked qualified assets
        for t in qualified.index:
            if len(selected) >= settings.top_n:
                break
            if t not in selected:
                selected.append(t)

        eligible = table.loc[selected].copy()

    return ScanResult(
        market_ok=market_ok,
        spy_above_ma=spy_above_ma,
        vix=vix,
        table=table,
        eligible=eligible,
        settings=settings,
    )


def format_scan_result(result: ScanResult, current_positions: list[str] | None = None) -> str:
    current_positions = current_positions or []
    lines = [
        "Monthly ETF Scanner",
        "",
        f"SPY above MA200: {'yes' if result.spy_above_ma else 'no'}",
        f"VIX: {result.vix:.1f}" if result.vix is not None else "VIX: unavailable",
        f"Market OK: {'yes' if result.market_ok else 'no'}",
        "",
    ]

    if not result.market_ok:
        lines.append("Market conditions are unfavorable. Recommendation: hold cash.")
        return "\n".join(lines)

    lines.append("ETF table:")
    if result.table.empty:
        lines.append("No ETF metrics available.")
    else:
        display = result.table.copy()
        for column in ["Momentum 6M", "Return 1M", "Return 3M", "Return 12M", "Volatility", "Drawdown"]:
            display[column] = display[column].map(lambda value: f"{value * 100:+.1f}%" if pd.notna(value) else "-")
        display["Sharpe"] = display["Sharpe"].map(lambda value: f"{value:.2f}" if pd.notna(value) else "-")
        display["Above MA200"] = display["Above MA200"].map(lambda value: "yes" if value else "no")
        if "Rank" in display.columns:
            display["Rank"] = display["Rank"].map(lambda value: f"{int(value)}" if pd.notna(value) else "-")
        lines.append(display.to_string())

    lines.extend(["", f"Top {len(result.eligible)} candidates:"])
    if result.eligible.empty:
        lines.append("- No candidates. Hold cash.")
    else:
        for ticker, row in result.eligible.iterrows():
            lines.append(
                f"- {ticker}: momentum {row['Momentum 6M'] * 100:+.1f}%, "
                f"Sharpe {row['Sharpe']:.2f}, drawdown {row['Drawdown'] * 100:+.1f}%"
            )

    if current_positions:
        to_buy = [ticker for ticker in result.eligible.index if ticker not in current_positions]
        to_sell = [ticker for ticker in current_positions if ticker not in result.eligible.index]
        to_hold = [ticker for ticker in current_positions if ticker in result.eligible.index]
        lines.extend(
            [
                "",
                "Changes vs current portfolio:",
                f"- Buy: {to_buy or '-'}",
                f"- Sell: {to_sell or '-'}",
                f"- Hold: {to_hold or '-'}",
            ]
        )

    return "\n".join(lines)
