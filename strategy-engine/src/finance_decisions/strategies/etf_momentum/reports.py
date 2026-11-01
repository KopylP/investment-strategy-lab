from __future__ import annotations

from dataclasses import asdict
import json
from pathlib import Path
from typing import Any

import pandas as pd

from finance_decisions.helpers import money, next_weekday, ua_day, ua_month
from finance_decisions.messages import render, render_deep, t
from finance_decisions.reports import write_html_report, write_json_report, write_text_report
from .plan import KIND_BUY, KIND_PARK, MonthlyPlan
from .risk import RiskReport, format_risk_report, status_label
from .scanner import ScanResult

TEMPLATES_DIR = Path(__file__).resolve().parent / "templates"


def _action_label(action: str) -> str:
    return t(f"etf_momentum.cli.action.{action}")


def _scan_status_label(status: str) -> str:
    return t(f"etf_momentum.cli.scan_status.{status}")


# ---------------------------------------------------------------- monthly scanner


def scanner_report_payload(result: ScanResult, plan: MonthlyPlan, current_positions: list[str]) -> dict[str, Any]:
    execute_on = next_weekday(result.as_of)
    settings = asdict(result.settings) if result.settings else {}
    return render_deep({
        "type": "monthly_scanner",
        "as_of": result.as_of,
        "plan_month": ua_month(execute_on),
        "execute_on": ua_day(execute_on, weekday=True),
        "execute_date": execute_on,
        "market_ok": result.market_ok,
        "spy_above_ma": result.spy_above_ma,
        "spy_price": result.spy_price,
        "spy_ma": result.spy_ma,
        "vix": result.vix,
        "market_reasons": result.market_reasons,
        "risk_free_rate": result.risk_free_rate,
        "cash_weight": result.cash_weight,
        "table": _dataframe_records(result.table),
        "eligible": _dataframe_records(result.eligible),
        "current_positions": current_positions,
        "settings": settings,
        "plan": asdict(plan),
        "currency": plan.currency,
        "positions_snippet": positions_snippet(plan, execute_on),
    })


def format_scan_report(result: ScanResult, plan: MonthlyPlan) -> str:
    """Full plain-text plan: the same content as the HTML report, without the ranking details."""
    execute_on = next_weekday(result.as_of)
    lines = [
        t("etf_momentum.cli.plan_title", month=ua_month(execute_on)),
        t("etf_momentum.cli.data_as_of", date=f"{result.as_of:%d.%m.%Y}", day=ua_day(execute_on, weekday=True)),
        "",
        _market_headline(plan),
    ]
    lines.extend(f"  - {render(reason)}" for reason in result.market_reasons)

    lines.extend(["", t("etf_momentum.cli.what_to_do")])
    lines.extend(_order_lines(plan, detailed=True) or [t("etf_momentum.cli.no_trades_indented")])

    lines.extend(["", t("etf_momentum.cli.portfolio_after")])
    for row in plan.allocation:
        if row.value_after <= 0 and row.value_before <= 0:
            continue
        target = t("etf_momentum.cli.target", weight=f"{row.target_weight:.0%}") if row.target_weight else ""
        lines.append(f"  {row.ticker:<9} {row.weight_before:>4.0%} -> {row.weight_after:>4.0%}{target}  {money(row.value_after, plan.currency)}")

    if plan.notes:
        lines.extend(["", t("etf_momentum.cli.notes")])
        lines.extend(f"  - {render(note)}" for note in plan.notes)

    lines.extend(["", t("etf_momentum.cli.ranking")])
    ranked = result.table[result.table["Rank"].notna()]
    if ranked.empty:
        lines.append(t("etf_momentum.cli.nothing_passed"))
    for ticker, row in ranked.iterrows():
        mark = "*" if ticker in result.eligible.index else " "
        lines.append(
            t(
                "etf_momentum.cli.rank_row",
                mark=mark,
                rank=f"{int(row['Rank']):>2}",
                ticker=f"{ticker:<9}",
                momentum=f"{row['Momentum']:+.1%}",
                reason=row["Reason"],
            )
        )
    lines.extend(["", t("etf_momentum.cli.update_positions")])
    return "\n".join(lines)


def format_monthly_telegram(result: ScanResult, plan: MonthlyPlan) -> str:
    execute_on = next_weekday(result.as_of)
    lines = [
        t("etf_momentum.cli.short_title", month=ua_month(execute_on), date=f"{result.as_of:%d.%m.%Y}"),
        _market_headline(plan),
        "",
        t("etf_momentum.cli.what_to_do_on", day=ua_day(execute_on, weekday=True)),
    ]
    lines.extend(_order_lines(plan, detailed=False) or [t("etf_momentum.cli.no_trades")])
    if plan.notes:
        lines.append("")
        lines.extend(f"• {render(note)}" for note in plan.notes)
    lines.extend(["", t("etf_momentum.cli.details_link")])
    return "\n".join(lines)


def _market_headline(plan: MonthlyPlan) -> str:
    invest = money(plan.contribution * plan.invest_pct, plan.currency)
    if plan.market_ok:
        return t("etf_momentum.cli.market_ok", invested=invest)
    return t("etf_momentum.cli.market_weak", share=f"{plan.invest_pct:.0%}", invested=invest)


def _order_lines(plan: MonthlyPlan, detailed: bool) -> list[str]:
    lines = []
    for index, order in enumerate(plan.orders, 1):
        shares = f"{order.shares:.4f}".rstrip("0").rstrip(".")
        line = t(
            "etf_momentum.cli.order",
            index=index,
            action=_action_label(order.action),
            ticker=order.ticker,
            amount=money(order.amount, plan.currency),
            shares=shares,
        )
        if detailed:
            reason = render(order.reason)
            line += t("etf_momentum.cli.order_detail", price=money(order.price, plan.currency), name=order.name, reason=f"{reason[:1].upper()}{reason[1:]}")
        lines.append(line)
    return lines


def positions_snippet(plan: MonthlyPlan, execute_on) -> str:
    """JSON lots to paste into positions.json after the buys are filled."""
    lots = [
        {
            "ticker": order.ticker,
            "entry_price": round(order.price, 4),
            "shares": round(order.shares, 6),
            "entry_date": execute_on.isoformat(),
            "currency": plan.currency,
        }
        for order in plan.orders
        if order.kind in (KIND_BUY, KIND_PARK)
    ]
    return ",\n".join(json.dumps(lot, ensure_ascii=False) for lot in lots)


def write_scanner_outputs(report_dir: Path, result: ScanResult, plan: MonthlyPlan, current_positions: list[str]) -> str:
    text = format_scan_report(result, plan)
    payload = scanner_report_payload(result, plan, current_positions)
    write_text_report(report_dir / "report.txt", text)
    write_json_report(report_dir / "report.json", payload)
    write_html_report(
        path=report_dir / "report.html",
        title=t("etf_momentum.cli.plan_html_title", month=payload["plan_month"]),
        body_text=text,
        payload=payload,
        template_name="monthly_scanner.html",
        templates_dirs=[TEMPLATES_DIR],
        custom_filters=_filters(),
    )
    return text


# ---------------------------------------------------------------- weekly risk monitor


def risk_report_payload(report: RiskReport) -> dict[str, Any]:
    return render_deep({
        "type": "weekly_risk_monitor",
        "report": asdict(report),
        "status": report.status,
        "alerts": report.alerts,
        "actions": report.actions,
        "week": report.as_of.isocalendar()[1],
    })


def format_weekly_telegram(report: RiskReport) -> str:
    return format_risk_report(report)


def write_risk_outputs(report_dir: Path, report: RiskReport) -> str:
    text = format_risk_report(report)
    payload = risk_report_payload(report)
    write_text_report(report_dir / "report.txt", text)
    write_json_report(report_dir / "report.json", payload)
    write_html_report(
        path=report_dir / "report.html",
        title=t("etf_momentum.cli.risk_html_title", date=f"{report.as_of:%d.%m.%Y}"),
        body_text=text,
        payload=payload,
        template_name="weekly_risk_monitor.html",
        templates_dirs=[TEMPLATES_DIR],
        custom_filters=_filters(),
    )
    return text


# ---------------------------------------------------------------- shared helpers


def _filters() -> dict[str, Any]:
    return {
        "status_label": lambda status: status_label(str(status)),
        "action_label": lambda action: _action_label(str(action)),
        "scan_status": lambda status: _scan_status_label(str(status)),
        "shares": lambda value: f"{float(value):.4f}".rstrip("0").rstrip("."),
        "pct0": _pct0,
        "sentence": lambda text: f"{str(text)[:1].upper()}{str(text)[1:]}" if text else "",
        "ua_date": lambda value: _as_date(value).strftime("%d.%m.%Y") if value else "-",
        "cushion_width": _cushion_width,
    }


def _pct0(value: Any) -> str:
    if value is None or pd.isna(value):
        return "-"
    return f"{float(value) * 100:.0f}%"


def _cushion_width(distance: Any, scale: float = 0.20) -> int:
    """Bar width (0-100) for the gap between price and its exit level; 20% gap fills the bar."""
    if distance is None or pd.isna(distance):
        return 0
    return int(max(0.0, min(1.0, -float(distance) / scale)) * 100)


def _as_date(value: Any):
    if isinstance(value, str):
        return pd.Timestamp(value).date()
    return value


def _dataframe_records(frame: pd.DataFrame) -> list[dict[str, Any]]:
    if frame.empty:
        return []
    return frame.reset_index().to_dict(orient="records")


# ---------------------------------------------------------------- backtest


def backtest_payload(result) -> dict[str, Any]:
    weekly = lambda series: series.resample("W-FRI").last().dropna()  # noqa: E731
    growth = {"strategy": weekly(result.growth) * 100, "benchmark": weekly(result.benchmark_growth) * 100}
    drawdown = {key: series / series.cummax() - 1 for key, series in growth.items()}
    return {
        "type": "backtest",
        "start": result.start,
        "end": result.end,
        "years": round((result.end - result.start).days / 365.25, 1),
        "benchmark": result.benchmark,
        "contribution": result.contribution,
        "initial": result.initial,
        "trades": result.trades,
        "costs": result.costs,
        "risk_exits": result.risk_exits,
        "months": result.months,
        "months_market_ok": result.months_market_ok,
        "metrics": result.metrics,
        "comparisons": result.comparisons,
        "yearly": result.yearly,
        "growth_chart": line_chart(growth, money_axis=True),
        "drawdown_chart": line_chart(drawdown, pct_axis=True, floor_zero=True),
    }


def format_backtest_report(result) -> str:
    s, b = result.metrics["strategy"], result.metrics["benchmark"]
    lines = [
        t("etf_momentum.cli.backtest_title", start=f"{result.start:%d.%m.%Y}", end=f"{result.end:%d.%m.%Y}"),
        t("etf_momentum.cli.backtest_contribution", amount=money(result.contribution, "EUR"), benchmark=result.benchmark),
        "",
        f"{'':<22}{t('etf_momentum.cli.backtest_strategy'):>12}{result.benchmark:>12}",
        f"{t('etf_momentum.cli.backtest_cagr'):<22}{s['cagr']:>12.1%}{b['cagr']:>12.1%}",
        f"{t('etf_momentum.cli.backtest_volatility'):<22}{s['volatility']:>12.1%}{b['volatility']:>12.1%}",
        f"{'Sharpe':<22}{s['sharpe']:>12.2f}{b['sharpe']:>12.2f}",
        f"{t('etf_momentum.cli.backtest_drawdown'):<22}{s['max_drawdown']:>12.1%}{b['max_drawdown']:>12.1%}",
        f"{t('etf_momentum.cli.backtest_final'):<22}{s['final_value']:>12,.0f}{b['final_value']:>12,.0f}",
        f"{t('etf_momentum.cli.backtest_contributed'):<22}{s['contributed']:>12,.0f}{b['contributed']:>12,.0f}",
        "",
        t("etf_momentum.cli.backtest_trades", trades=result.trades, costs=money(result.costs, "EUR"), exits=result.risk_exits),
        t("etf_momentum.cli.backtest_months", ok=result.months_market_ok, months=result.months),
        "",
        t("etf_momentum.cli.backtest_by_year"),
    ]
    lines.extend(
        f"  {row['year']}{'*' if row['partial'] else ' '} {row['strategy']:+7.1%} / {row['benchmark']:+7.1%}" for row in result.yearly
    )
    lines.extend(["", t("etf_momentum.cli.backtest_comparisons")])
    lines.extend(_comparison_line(name, metrics) for name, metrics in result.comparisons.items())
    lines.extend(["", t("etf_momentum.cli.backtest_footnote")])
    return "\n".join(lines)


def _comparison_line(name: str, metrics: dict[str, float]) -> str:
    label = t(f"etf_momentum.cli.backtest_comparison.{name}")
    return f"  {label:<20}{metrics['cagr']:>8.1%}{metrics['sharpe']:>8.2f}{metrics['max_drawdown']:>9.1%}"


def write_backtest_outputs(report_dir: Path, result) -> str:
    text = format_backtest_report(result)
    payload = backtest_payload(result)
    write_text_report(report_dir / "report.txt", text)
    write_json_report(report_dir / "report.json", {k: v for k, v in payload.items() if not k.endswith("_chart")})
    write_html_report(
        path=report_dir / "report.html",
        title=t("etf_momentum.cli.backtest_html_title"),
        body_text=text,
        payload=payload,
        template_name="backtest.html",
        templates_dirs=[TEMPLATES_DIR],
        custom_filters=_filters(),
    )
    return text


def line_chart(
    series: dict[str, pd.Series],
    *,
    width: int = 760,
    height: int = 260,
    money_axis: bool = False,
    pct_axis: bool = False,
    floor_zero: bool = False,
) -> dict[str, Any]:
    """SVG geometry for a small multi-line chart sharing one x and one y axis."""
    left, right, top, bottom = 52, 92, 12, 28
    frame = pd.DataFrame(series).dropna()
    if frame.empty:
        return {"width": width, "height": height, "lines": [], "y_ticks": [], "x_ticks": [], "data": {}}
    low, high = float(frame.min().min()), float(frame.max().max())
    if floor_zero:
        high = 0.0
    ticks = _nice_ticks(low, high)
    low, high = ticks[0], ticks[-1]
    plot_w, plot_h = width - left - right, height - top - bottom

    def x(i: int) -> float:
        return left + plot_w * i / max(len(frame) - 1, 1)

    def y(v: float) -> float:
        return top + plot_h * (1 - (v - low) / (high - low or 1))

    def label(v: float) -> str:
        if pct_axis:
            return f"{v * 100 + 0.0:.0f}%" if round(v * 100) != 0 else "0%"
        return f"€{v:,.0f}" if money_axis else f"{v:,.2f}"

    lines = []
    for key in frame.columns:
        values = frame[key].tolist()
        points = " ".join(f"{x(i):.1f},{y(v):.1f}" for i, v in enumerate(values))
        lines.append({"key": key, "points": points, "end_x": x(len(values) - 1), "end_y": y(values[-1]), "end_label": label(values[-1])})
    # Keep end labels from overlapping.
    if len(lines) == 2 and abs(lines[0]["end_y"] - lines[1]["end_y"]) < 16:
        upper, lower = sorted(lines, key=lambda line: line["end_y"])
        upper["label_y"], lower["label_y"] = upper["end_y"] - 8, lower["end_y"] + 8
    for line in lines:
        line.setdefault("label_y", line["end_y"])

    years = sorted({d.year for d in frame.index})
    x_ticks = []
    for year in years:
        first = next(i for i, d in enumerate(frame.index) if d.year == year)
        if first > 0:
            x_ticks.append({"x": x(first), "label": str(year)})

    return {
        "width": width,
        "height": height,
        "left": left,
        "right": width - right,
        "top": top,
        "bottom": height - bottom,
        "lines": lines,
        "y_ticks": [{"y": y(t), "label": label(t)} for t in ticks],
        "x_ticks": x_ticks,
        "data": {
            "x": [round(x(i), 1) for i in range(len(frame))],
            "dates": [d.strftime("%d.%m.%Y") for d in frame.index],
            **{key: [label(v) for v in frame[key]] for key in frame.columns},
        },
    }


def _nice_ticks(low: float, high: float, count: int = 4) -> list[float]:
    import math

    span = high - low or abs(high) or 1.0
    raw = span / count
    magnitude = 10 ** math.floor(math.log10(raw))
    step = next(m * magnitude for m in (1, 2, 2.5, 5, 10) if m * magnitude >= raw)
    start = math.floor(low / step) * step
    ticks = []
    value = start
    while value < high + step * 0.999:
        ticks.append(round(value, 10))
        value += step
    return ticks
