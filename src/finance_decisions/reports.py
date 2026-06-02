from __future__ import annotations

import json
from dataclasses import asdict, is_dataclass
from datetime import date, datetime, timezone
from pathlib import Path
from typing import Any

import pandas as pd
from jinja2 import Environment, FileSystemLoader

from finance_decisions.helpers import is_na, money, ticker_currency
from finance_decisions.risk import RiskReport, format_risk_report
from finance_decisions.scanner import ScanResult, format_scan_result

# Paths to strategy-specific and base templates directories
STRATEGIES_DIR = Path(__file__).resolve().parent / "strategies"
DEFAULT_TEMPLATES_DIRS = [
    STRATEGIES_DIR / "etf_momentum" / "templates",
    STRATEGIES_DIR / "templates",
]

jinja_env = Environment(
    loader=FileSystemLoader(DEFAULT_TEMPLATES_DIRS),
    autoescape=True,
)


def write_text_report(path: Path, text: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(text, encoding="utf-8")


def write_json_report(path: Path, payload: dict[str, Any]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(_json_safe(payload), indent=2), encoding="utf-8")


def write_html_report(
    path: Path,
    title: str,
    body_text: str,
    payload: dict[str, Any],
    template_name: str | None = None,
    templates_dir: Path | None = None,
) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    generated_at = datetime.now(timezone.utc).replace(microsecond=0).isoformat().replace("+00:00", "Z")

    if not template_name:
        report_type = payload.get("type")
        if report_type == "weekly_risk_monitor":
            template_name = "weekly_risk_monitor.html"
        elif report_type == "monthly_scanner":
            template_name = "monthly_scanner.html"
        else:
            raise ValueError(f"Unknown report type: {report_type}")

    env = jinja_env
    if templates_dir:
        search_dirs = [templates_dir, STRATEGIES_DIR / "templates"]
        env = Environment(loader=FileSystemLoader(search_dirs), autoescape=True)
        # Copy filters and tests to the new environment
        for name, filter_func in jinja_env.filters.items():
            env.filters[name] = filter_func
        for name, test_func in jinja_env.tests.items():
            env.tests[name] = test_func

    template = env.get_template(template_name)
    console_text = _render_console_text_html(payload, body_text)

    context = {
        "title": title,
        "generated_at": generated_at,
        "payload": payload,
        "console_text": console_text,
        **payload,
    }

    html = template.render(context)
    path.write_text(html, encoding="utf-8")


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
    write_html_report(report_dir / "report.html", "Weekly Risk Monitor", text, payload)
    return text


def write_scanner_outputs(report_dir: Path, result: ScanResult, current_positions: list[str]) -> str:
    text = format_scan_result(result, current_positions=current_positions)
    payload = scanner_report_payload(result, current_positions)
    write_text_report(report_dir / "report.txt", text)
    write_json_report(report_dir / "report.json", payload)
    write_html_report(report_dir / "report.html", "Місячний ETF-сканер", text, payload)
    return text


def _dataframe_records(frame: pd.DataFrame) -> list[dict[str, Any]]:
    if frame.empty:
        return []
    copy = frame.reset_index()
    return copy.to_dict(orient="records")


def _render_console_text_html(payload: dict[str, Any], body_text: str) -> str:
    if payload.get("type") != "monthly_scanner":
        return body_text

    lines = [
        "Місячний ETF-сканер",
        "",
        f"SPY вище MA200: {'так' if payload.get('spy_above_ma') else 'ні'}",
        f"VIX: {_number(payload.get('vix'))}",
        f"Стан ринку OK: {'так' if payload.get('market_ok') else 'ні'}",
        "",
    ]
    if payload.get("market_ok"):
        lines.append("Ринковий фільтр пройдено. Перевірте кандидатів нижче перед будь-яким рішенням.")
    else:
        lines.append("Ринкові умови несприятливі. Balanced hybrid: інвестувати 25-50% нового внеску, решту тримати в кеші.")
    return "\n".join(lines)


# Helper formatting functions
def _pct(value: Any) -> str:
    if is_na(value):
        return "-"
    return f"{float(value) * 100:+.1f}%"


def _number(value: Any) -> str:
    if is_na(value):
        return "-"
    return f"{float(value):.2f}"


def _value_class(value: Any) -> str:
    if is_na(value):
        return ""
    number = float(value)
    if number > 0:
        return "green"
    if number < 0:
        return "red"
    return ""


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


def _json_safe(value: Any) -> Any:
    if is_dataclass(value):
        return _json_safe(asdict(value))
    if isinstance(value, dict):
        return {str(key): _json_safe(item) for key, item in value.items()}
    if isinstance(value, list):
        return [_json_safe(item) for item in value]
    if isinstance(value, tuple):
        return [_json_safe(item) for item in value]
    if isinstance(value, (datetime, date)):
        return value.isoformat()
    if isinstance(value, pd.Timestamp):
        return value.isoformat()
    if is_na(value):
        return None
    return value


# Jinja2 environment filter registrations
jinja_env.filters["pct"] = _pct
jinja_env.filters["number"] = _number
jinja_env.filters["money"] = money
jinja_env.filters["ticker_currency"] = ticker_currency
jinja_env.filters["value_class"] = _value_class
jinja_env.filters["status_badge"] = _status_badge
jinja_env.filters["strategy_status"] = _strategy_status
jinja_env.filters["strategy_badge"] = _strategy_badge
jinja_env.filters["json_safe"] = _json_safe
jinja_env.filters["json_dumps"] = lambda v, indent=None: json.dumps(v, indent=indent)

# Jinja2 environment test registrations
jinja_env.tests["na"] = is_na

