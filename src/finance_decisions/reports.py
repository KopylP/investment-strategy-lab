from __future__ import annotations

from dataclasses import asdict, is_dataclass
from datetime import date, datetime, timezone
from functools import lru_cache
import json
from pathlib import Path
from typing import Any

import pandas as pd
from jinja2 import Environment, FileSystemLoader

from finance_decisions.helpers import is_na, money, ticker_currency


@lru_cache(maxsize=16)
def _get_jinja_env(search_paths: tuple[Path, ...]) -> Environment:
    return Environment(loader=FileSystemLoader(search_paths), autoescape=True)


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
    template_name: str,
    templates_dirs: list[Path],
    custom_filters: dict[str, Any] | None = None,
    custom_tests: dict[str, Any] | None = None,
) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)

    # 1. Resolve templates search directories: append shared fallback
    shared_dir = Path(__file__).resolve().parent / "strategies" / "templates"
    search_dirs = list(templates_dirs)
    if shared_dir not in search_dirs:
        search_dirs.append(shared_dir)

    search_paths = tuple(search_dirs)
    env = _get_jinja_env(search_paths)

    # 2. Register global formatting filters & tests
    env.filters["pct"] = _pct
    env.filters["number"] = _number
    env.filters["money"] = money
    env.filters["ticker_currency"] = ticker_currency
    env.filters["value_class"] = _value_class
    env.filters["json_safe"] = _json_safe
    env.filters["json_dumps"] = lambda v, indent=None: json.dumps(v, indent=indent)

    env.tests["na"] = is_na

    # 3. Register custom strategy-specific filters and tests
    if custom_filters:
        for name, filter_func in custom_filters.items():
            env.filters[name] = filter_func
    if custom_tests:
        for name, test_func in custom_tests.items():
            env.tests[name] = test_func

    # 4. Auto-inject standard metadata
    generated_at = datetime.now(timezone.utc).replace(microsecond=0).isoformat().replace("+00:00", "Z")
    console_text = payload.get("console_text") or body_text

    context = {
        "title": title,
        "generated_at": generated_at,
        "payload": payload,
        "console_text": console_text,
        **payload,
    }

    template = env.get_template(template_name)
    html = template.render(context)
    path.write_text(html, encoding="utf-8")


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
