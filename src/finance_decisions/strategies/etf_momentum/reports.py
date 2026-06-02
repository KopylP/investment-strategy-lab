from __future__ import annotations

import json
from dataclasses import asdict
from datetime import date, datetime, timezone
from pathlib import Path
from typing import Any

import pandas as pd

from finance_decisions.helpers import is_na, money, ticker_currency
from finance_decisions.reports import write_html_report, write_json_report, write_text_report
from .risk import RiskReport, format_risk_report
from .scanner import ScanResult, format_scan_result

# Local template search folder for this strategy
TEMPLATES_DIR = Path(__file__).resolve().parent / "templates"


def risk_report_payload(report: RiskReport) -> dict[str, Any]:
    return {
        "type": "weekly_risk_monitor",
        "report": asdict(report),
        "alerts": report.alerts,
        "actions": report.actions,
    }


def scanner_report_payload(result: ScanResult, current_positions: list[str]) -> dict[str, Any]:
    max_vol = result.settings.max_volatility if (result.settings is not None) else 0.35
    max_dd = result.settings.max_drawdown if (result.settings is not None) else -0.20
    min_sr = result.settings.min_sharpe if (result.settings is not None) else 0.20
    hold_k = result.settings.hold_buffer_k if (result.settings is not None) else 8
    top_n = result.settings.top_n if (result.settings is not None) else 3
    return {
        "type": "monthly_scanner",
        "market_ok": result.market_ok,
        "spy_above_ma": result.spy_above_ma,
        "vix": result.vix,
        "table": _dataframe_records(result.table),
        "eligible": _dataframe_records(result.eligible),
        "current_positions": current_positions,
        "settings": {
            "max_volatility": max_vol,
            "max_drawdown": max_dd,
            "min_sharpe": min_sr,
            "hold_buffer_k": hold_k,
            "top_n": top_n,
        }
    }


def write_risk_outputs(report_dir: Path, report: RiskReport) -> str:
    text = format_risk_report(report)
    payload = risk_report_payload(report)
    write_text_report(report_dir / "report.txt", text)
    write_json_report(report_dir / "report.json", payload)
    
    # We pass TEMPLATES_DIR to search for local etf_momentum templates, 
    # plus strategy custom filters and tests
    custom_filters = {
        "status_badge": _status_badge,
    }
    
    write_html_report(
        path=report_dir / "report.html",
        title="Тижневий моніторинг ризиків",
        body_text=text,
        payload=payload,
        template_name="weekly_risk_monitor.html",
        templates_dirs=[TEMPLATES_DIR],
        custom_filters=custom_filters,
    )
    return text


def write_scanner_outputs(report_dir: Path, result: ScanResult, current_positions: list[str]) -> str:
    text = format_scan_result(result, current_positions=current_positions)
    payload = scanner_report_payload(result, current_positions)
    write_text_report(report_dir / "report.txt", text)
    write_json_report(report_dir / "report.json", payload)
    
    # Register strategy-specific filters and tests for the Jinja template
    custom_filters = {
        "strategy_status": _strategy_status,
        "strategy_badge": _strategy_badge,
    }
    
    write_html_report(
        path=report_dir / "report.html",
        title="Місячний ETF-сканер",
        body_text=text,
        payload=payload,
        template_name="monthly_scanner.html",
        templates_dirs=[TEMPLATES_DIR],
        custom_filters=custom_filters,
    )
    return text


def _dataframe_records(frame: pd.DataFrame) -> list[dict[str, Any]]:
    if frame.empty:
        return []
    copy = frame.reset_index()
    return copy.to_dict(orient="records")


def _render_console_text_html(payload: dict[str, Any], body_text: str) -> str:
    if payload.get("type") != "monthly_scanner":
        return body_text

    vix_val = payload.get("vix")
    vix_str = f"{float(vix_val):.2f}" if vix_val is not None and not is_na(vix_val) else "-"
    lines = [
        "Місячний ETF-сканер",
        "",
        f"SPY вище MA200: {'так' if payload.get('spy_above_ma') else 'ні'}",
        f"VIX: {vix_str}",
        f"Стан ринку OK: {'так' if payload.get('market_ok') else 'ні'}",
        "",
    ]
    if payload.get("market_ok"):
        lines.append("Ринковий фільтр пройдено. Перевірте кандидатів нижче перед будь-яким рішенням.")
    else:
        lines.append("Ринкові умови несприятливі. Balanced hybrid: інвестувати 25-50% нового внеску, решту тримати в кеші.")
    return "\n".join(lines)


# Local formatting and presentation logic specific to this strategy
def _status_badge(status: str) -> str:
    status_lower = str(status).lower()
    css = status_lower if status_lower in {"green", "yellow", "red", "unknown"} else "unknown"
    return f'<span class="badge {css}">{status_lower}</span>'


def _strategy_status(item: dict[str, Any], is_eligible: bool, settings: dict[str, Any]) -> dict[str, str]:
    top_n = settings.get("top_n", 3)
    rank = item.get("Rank")
    if is_eligible:
        if rank is not None and pd.notna(rank) and int(rank) > top_n:
            return {
                "label": "Буферне утримання",
                "css": "blue",
                "row_class": "buffer-row",
            }
        return {
            "label": "Top кандидат",
            "css": "green",
            "row_class": "eligible-row",
        }
    if _passes_strategy_filters(item, settings):
        return {
            "label": "Є кращі",
            "css": "yellow",
            "row_class": "qualified-row",
        }
    return {
        "label": "Не проходить",
        "css": "unknown",
        "row_class": "",
    }


def _strategy_badge(status: dict[str, str]) -> str:
    label = status["label"]
    css = status["css"]
    if css == "green":
        return '<span class="badge green">Підходить</span>'
    if css == "blue":
        return '<span class="badge blue">Утримання</span>'
    if css == "yellow":
        return f'<span class="badge yellow">{label}</span>'
    return f'<span class="badge unknown">{label}</span>'


def _passes_strategy_filters(item: dict[str, Any], settings: dict[str, Any]) -> bool:
    max_vol = settings.get("max_volatility", 0.35)
    max_dd = settings.get("max_drawdown", -0.20)
    min_sr = settings.get("min_sharpe", 0.20)

    vol = item.get("Volatility")
    dd = item.get("Drawdown")
    sr = item.get("Sharpe")

    vol_ok = float(vol) < max_vol if vol is not None and pd.notna(vol) else False
    dd_ok = float(dd) > max_dd if dd is not None and pd.notna(dd) else False
    sr_ok = float(sr) >= min_sr if sr is not None and pd.notna(sr) else False
    ma_ok = bool(item.get("Above MA200"))

    return ma_ok and vol_ok and dd_ok and sr_ok
