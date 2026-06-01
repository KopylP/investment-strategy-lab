from __future__ import annotations

import json
from dataclasses import asdict, is_dataclass
from datetime import date, datetime, timezone
from html import escape
from pathlib import Path
from typing import Any

import pandas as pd

from finance_decisions.helpers import is_na, money, ticker_currency
from finance_decisions.risk import RiskReport, format_risk_report
from finance_decisions.scanner import ScanResult, format_scan_result


def write_text_report(path: Path, text: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(text, encoding="utf-8")


def write_json_report(path: Path, payload: dict[str, Any]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(_json_safe(payload), indent=2), encoding="utf-8")


def write_html_report(path: Path, title: str, body_text: str, payload: dict[str, Any]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    generated_at = datetime.now(timezone.utc).replace(microsecond=0).isoformat().replace("+00:00", "Z")
    content = _render_payload_html(payload)
    console_text = _render_console_text_html(payload, body_text)
    html = f"""<!doctype html>
<html lang="uk">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>{escape(title)}</title>
  <style>
    :root {{
      --bg: #f4f7f8;
      --panel: #ffffff;
      --ink: #18232f;
      --muted: #657386;
      --line: #d9e1e7;
      --green: #18794e;
      --green-bg: #e8f5ee;
      --yellow: #946200;
      --yellow-bg: #fff4d6;
      --red: #c9352b;
      --red-bg: #fdecea;
      --blue: #235a97;
      --blue-bg: #eaf2fb;
      --soft: #f8fafb;
    }}
    * {{ box-sizing: border-box; }}
    body {{
      margin: 0;
      background: var(--bg);
      color: var(--ink);
      font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Arial, sans-serif;
      line-height: 1.45;
    }}
    main {{ max-width: 1180px; margin: 0 auto; padding: 26px 20px 42px; }}
    header {{ margin-bottom: 18px; }}
    h1 {{ font-size: 30px; margin: 0 0 6px; letter-spacing: 0; }}
    h2 {{ font-size: 18px; margin: 0 0 12px; }}
    h3 {{ font-size: 15px; margin: 0 0 8px; }}
    p {{ margin: 0; }}
    .meta {{ color: var(--muted); font-size: 14px; }}
    .grid {{ display: grid; gap: 14px; }}
    .summary-grid {{ grid-template-columns: repeat(4, minmax(0, 1fr)); margin-bottom: 14px; }}
    .two-col {{ grid-template-columns: minmax(0, 1fr) minmax(0, 1fr); }}
    .three-col {{ grid-template-columns: repeat(3, minmax(0, 1fr)); }}
    section, .card {{
      background: var(--panel);
      border: 1px solid var(--line);
      border-radius: 8px;
      padding: 16px;
      box-shadow: 0 1px 2px rgba(24, 35, 47, 0.04);
    }}
    section {{ margin-top: 16px; }}
    .decision {{
      border-left: 5px solid var(--green);
      padding: 18px;
    }}
    .decision.red {{ border-left-color: var(--red); }}
    .decision-title {{
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 12px;
      margin-bottom: 10px;
    }}
    .decision-title h2 {{ margin: 0; font-size: 20px; }}
    .decision-text {{ font-size: 18px; font-weight: 700; margin-bottom: 10px; }}
    .decision-subtext {{ color: var(--muted); max-width: 860px; }}
    .action-list {{ list-style: none; padding: 0; margin: 12px 0 0; }}
    .action-list li {{
      display: grid;
      grid-template-columns: 140px minmax(0, 1fr);
      gap: 10px;
      padding: 9px 0;
      border-top: 1px solid var(--line);
    }}
    .action-list strong {{ color: var(--ink); }}
    .metric-label {{ color: var(--muted); font-size: 12px; text-transform: uppercase; letter-spacing: 0; }}
    .metric-value {{ font-size: 24px; font-weight: 700; margin-top: 4px; }}
    .metric-note {{ color: var(--muted); font-size: 13px; margin-top: 4px; }}
    .badge {{
      display: inline-block;
      border-radius: 999px;
      padding: 3px 9px;
      font-size: 12px;
      font-weight: 700;
      text-transform: uppercase;
    }}
    .green {{ color: var(--green); }}
    .yellow {{ color: var(--yellow); }}
    .red {{ color: var(--red); }}
    .unknown {{ color: var(--muted); }}
    .badge.green {{ background: var(--green-bg); }}
    .badge.yellow {{ background: var(--yellow-bg); }}
    .badge.red {{ background: var(--red-bg); }}
    .badge.unknown {{ background: #eef1f4; }}
    .badge.blue {{ background: var(--blue-bg); color: var(--blue); }}
    .badge.large {{ font-size: 13px; padding: 5px 11px; }}
    tr.eligible-row td {{ background: #f0fbf6; }}
    tr.eligible-row td:first-child {{ border-left: 4px solid var(--green); }}
    tr.buffer-row td {{ background: #f0f4fb; }}
    tr.buffer-row td:first-child {{ border-left: 4px solid var(--blue); }}
    tr.qualified-row td {{ background: #fffaf0; }}
    tr.qualified-row td:first-child {{ border-left: 4px solid var(--yellow); }}
    table {{ width: 100%; border-collapse: collapse; font-size: 14px; }}
    th, td {{ padding: 10px 9px; border-bottom: 1px solid var(--line); text-align: left; vertical-align: top; }}
    th {{ color: var(--muted); font-size: 12px; text-transform: uppercase; letter-spacing: 0; background: #f8fafb; }}
    tr:last-child td {{ border-bottom: 0; }}
    .num {{ text-align: right; font-variant-numeric: tabular-nums; }}
    th.num {{ text-align: right; }}
    ul {{ margin: 0; padding-left: 20px; }}
    li + li {{ margin-top: 6px; }}
    .empty {{ color: var(--muted); }}
    .compact-note {{ color: var(--muted); font-size: 13px; margin-bottom: 10px; }}
    .candidate-card {{
      background: var(--soft);
      border: 1px solid var(--line);
      border-radius: 8px;
      padding: 12px;
    }}
    .candidate-name {{ font-size: 18px; font-weight: 800; margin-bottom: 8px; }}
    .candidate-stats {{
      display: flex;
      flex-direction: column;
      gap: 6px;
      font-size: 13px;
      margin-top: 10px;
    }}
    .stat-row {{
      display: flex;
      justify-content: space-between;
      align-items: center;
      border-bottom: 1px dashed var(--line);
      padding-bottom: 4px;
    }}
    .stat-row:last-child {{
      border-bottom: none;
      padding-bottom: 0;
    }}
    .stat-label {{
      color: var(--muted);
    }}
    .stat-value {{
      font-weight: 700;
      color: var(--ink);
    }}
    .tooltip {{
      position: relative;
      display: inline-block;
      cursor: pointer;
      margin-left: 4px;
      color: var(--blue);
      font-size: 14px;
      vertical-align: middle;
    }}
    .tooltip .tooltiptext {{
      visibility: hidden;
      width: 280px;
      background-color: #18232f;
      color: #fff;
      text-align: left;
      border-radius: 6px;
      padding: 10px 12px;
      position: absolute;
      z-index: 100;
      bottom: 125%;
      left: 50%;
      margin-left: -140px;
      opacity: 0;
      transition: opacity 0.2s;
      font-size: 12px;
      font-weight: normal;
      line-height: 1.45;
      box-shadow: 0 4px 12px rgba(0,0,0,0.15);
      white-space: normal;
    }}
    .tooltip:hover .tooltiptext {{
      visibility: visible;
      opacity: 1;
    }}
    .tooltip.align-left .tooltiptext {{
      left: auto;
      right: 0;
      margin-left: 0;
    }}
    .table-container {{
      width: 100%;
      overflow-x: auto;
      -webkit-overflow-scrolling: touch;
      margin-top: 10px;
      margin-bottom: 10px;
    }}
    .callout {{
      border-left: 4px solid var(--blue);
      background: var(--blue-bg);
      padding: 12px 14px;
      border-radius: 6px;
      color: #1f466f;
    }}
    details {{ margin-top: 16px; }}
    summary {{ cursor: pointer; color: var(--blue); font-weight: 700; }}
    pre {{
      background: #101820;
      color: #e8edf2;
      border-radius: 8px;
      padding: 14px;
      overflow: auto;
      font-size: 12px;
    }}
    @media (max-width: 820px) {{
      main {{ padding: 18px 12px 32px; }}
      .summary-grid, .two-col, .three-col {{ grid-template-columns: 1fr; }}
      .decision-title {{ align-items: flex-start; flex-direction: column; }}
      .action-list li {{ grid-template-columns: 1fr; gap: 2px; }}
      table {{ display: block; overflow-x: auto; white-space: nowrap; }}
    }}
  </style>
</head>
<body>
  <main>
    <header>
      <h1>{escape(title)}</h1>
      <div class="meta">Згенеровано: {escape(generated_at)}</div>
    </header>
    {content}
    <details>
      <summary>Технічні дані JSON</summary>
      <pre>{escape(json.dumps(_json_safe(payload), indent=2))}</pre>
    </details>
    <details>
      <summary>Консольний текст</summary>
      <pre>{escape(console_text)}</pre>
    </details>
  </main>
</body>
</html>
"""
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


def _render_payload_html(payload: dict[str, Any]) -> str:
    report_type = payload.get("type")
    if report_type == "weekly_risk_monitor":
        return _render_risk_html(payload)
    if report_type == "monthly_scanner":
        return _render_scanner_html(payload)
    return '<section><h2>Report</h2><p class="empty">No structured renderer is available.</p></section>'


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


def _render_risk_html(payload: dict[str, Any]) -> str:
    report = payload["report"]
    positions = report.get("position_risks", [])
    alerts = payload.get("alerts", [])
    actions = payload.get("actions", [])
    portfolio_pnl = report.get("portfolio_pnl")
    portfolio_value = report.get("portfolio_value", 0)
    portfolio_cost = report.get("portfolio_cost", 0)
    vix = report.get("vix")

    mixed_warning = ""
    if report.get("currency") == "MIXED":
        mixed_warning = """
    <div class="callout" style="border-left-color: var(--yellow); background: var(--yellow-bg); color: var(--yellow); margin-top: 14px;">
      <strong>Caution:</strong> Portfolio contains a mix of currencies. Totals (Cost and Value) are raw sums of multiple currencies without conversion.
    </div>
        """

    rows = []
    for position in positions:
        rows.append(
            "<tr>"
            f"<td><strong>{escape(position['ticker'])}</strong></td>"
            f"<td>{_status_badge(position['status'])}</td>"
            f"<td class=\"num\">{money(position['current_price'], position.get('currency', 'USD'))}</td>"
            f"<td class=\"num {_value_class(position['pnl_pct'])}\">{_pct(position['pnl_pct'])}</td>"
            f"<td class=\"num {_value_class(position['drawdown_from_high'])}\">{_pct(position['drawdown_from_high'])}</td>"
            f"<td>{'Above' if position['above_ma'] else 'Below'}</td>"
            f"<td>{_list_inline(position.get('alerts', []))}</td>"
            "</tr>"
        )

    return f"""
    <div class="grid summary-grid">
      {_metric_card("As of", report.get("as_of", "-"), "Market data date")}
      {_metric_card("Portfolio P&L", _pct(portfolio_pnl), f"{money(portfolio_value, report.get('currency', 'USD'))} value vs {money(portfolio_cost, report.get('currency', 'USD'))} cost")}
      {_metric_card("SPY vs MA200", "Above" if report.get("spy_above_ma") else "Below", "Broad market trend filter")}
      {_metric_card("VIX", _number(vix), "Volatility stress filter")}
    </div>
    {mixed_warning}
    <div class="grid two-col">
      <section>
        <h2>Actions</h2>
        {_list_block(actions, "No portfolio actions triggered.")}
      </section>
      <section>
        <h2>Alerts</h2>
        {_list_block(alerts, "No alerts. Positions are inside configured risk rules.")}
      </section>
    </div>
    <section>
      <h2>Position Risk</h2>
      <div class="table-container">
        <table>
          <thead>
            <tr>
              <th>Ticker</th>
              <th>Status</th>
              <th class="num">Price</th>
              <th class="num">P&L</th>
              <th class="num">
                Drawdown
                <span class="tooltip align-left">ⓘ
                  <span class="tooltiptext">
                    <strong>Drawdown</strong> is the peak-to-trough decline of the asset price over a period. <br/>
                    Formula: (Current Price - Peak Price) / Peak Price. <br/>
                    It is always &le; 0 (or 0% at peak). Indicates maximum paper loss from the highest peak. Affects weekly risk exits (e.g. warning at -10%, exit at -18%).
                  </span>
                </span>
              </th>
              <th>MA200</th>
              <th>Reason</th>
            </tr>
          </thead>
          <tbody>{''.join(rows) if rows else '<tr><td colspan="7" class="empty">No positions found.</td></tr>'}</tbody>
        </table>
      </div>
    </section>
    <section>
      <h2>How To Read This Report</h2>
      <div class="callout">
        Red means the configured exit rule triggered. Yellow means warning or weakness. Green means the position passed the current checks.
        Review the action list first, then confirm the market context and the raw JSON before making any trade.
      </div>
    </section>
    """


def _render_scanner_html(payload: dict[str, Any]) -> str:
    eligible = payload.get("eligible", [])
    candidates = [str(item["ETF"]) for item in eligible]
    candidate_count = len(eligible)
    weight_pct = 100.0 / candidate_count if candidate_count > 0 else 0.0

    rows = []
    eligible_tickers = {str(item["ETF"]) for item in eligible}
    settings_dict = payload.get("settings", {})
    for item in payload.get("table", []):
        ticker = str(item["ETF"])
        status = _strategy_status(item, ticker in eligible_tickers, settings_dict)
        row_class = f' class="{status["row_class"]}"' if status["row_class"] else ""
        currency = ticker_currency(ticker)
        rank_val = item.get("Rank")
        rank_str = f"{int(rank_val)}" if rank_val is not None and pd.notna(rank_val) else "-"
        weight_str = f"{weight_pct:.0f}%" if ticker in eligible_tickers else "-"
        rows.append(
            f"<tr{row_class}>"
            f"<td><strong>{escape(ticker)}</strong></td>"
            f"<td class=\"num\">{rank_str}</td>"
            f"<td class=\"num\">{weight_str}</td>"
            f"<td>{_strategy_badge(status)}</td>"
            f"<td class=\"num\">{money(item['Price'], currency)}</td>"
            f"<td class=\"num {_value_class(item['Momentum 6M'])}\">{_pct(item['Momentum 6M'])}</td>"
            f"<td class=\"num {_value_class(item['Return 1M'])}\">{_pct(item['Return 1M'])}</td>"
            f"<td class=\"num {_value_class(item['Return 3M'])}\">{_pct(item['Return 3M'])}</td>"
            f"<td class=\"num {_value_class(item['Return 12M'])}\">{_pct(item['Return 12M'])}</td>"
            f"<td class=\"num\">{_pct(item['Volatility'])}</td>"
            f"<td class=\"num\">{_number(item['Sharpe'])}</td>"
            f"<td class=\"num {_value_class(item['Drawdown'])}\">{_pct(item['Drawdown'])}</td>"
            f"<td>{'Вище' if item['Above MA200'] else 'Нижче'}</td>"
            "</tr>"
        )

    current = payload.get("current_positions", [])
    market_ok = bool(payload.get("market_ok"))
    spy_above_ma = bool(payload.get("spy_above_ma"))
    vix = payload.get("vix")
    market_reasons = _scanner_market_reasons(market_ok, spy_above_ma, vix)
    to_buy = [ticker for ticker in candidates if ticker not in current]
    to_sell = [ticker for ticker in current if ticker not in candidates]
    to_hold = [ticker for ticker in current if ticker in candidates]

    to_buy_str = ", ".join(f"{t} ({weight_pct:.0f}%)" for t in to_buy) if to_buy else "-"
    to_sell_str = ", ".join(f"{t} (0%)" for t in to_sell) if to_sell else "-"
    to_hold_str = ", ".join(f"{t} ({weight_pct:.0f}%)" for t in to_hold) if to_hold else "-"

    if market_ok:
        decision_text = (
            f"Інвестувати 100% нового внеску порівну між кандидатами (по {weight_pct:.0f}% на кожен актив)."
            if candidate_count > 0 else "Нічого купувати: немає відповідних кандидатів. Тримати 100% у кеші."
        )
        decision_subtext = (
            "Ринковий фільтр пройдено: SPY вище MA200, VIX нижче порогу 30. Режим Balanced hybrid дозволяє повний місячний внесок."
        )
    else:
        dca_low = 25.0 / candidate_count if candidate_count > 0 else 0.0
        dca_high = 50.0 / candidate_count if candidate_count > 0 else 0.0
        decision_text = (
            f"Balanced hybrid: інвестувати 25-50% нового внеску (по {dca_low:.1f}% - {dca_high:.1f}% на кандидата), решту 50-75% тримати в кеші."
            if candidate_count > 0 else "Ринкові умови несприятливі, кандидатів немає. Тримати 100% у кеші."
        )
        decision_subtext = (
            "Ринковий фільтр не пройдено. Balanced hybrid зменшує новий вхід, але не означає автоматичний продаж поточних позицій."
        )

    return f"""
    <div class="grid summary-grid">
      {_metric_card("Стан ринку", "OK" if market_ok else "Не OK", "Визначає розмір нового місячного внеску")}
      {_metric_card("SPY vs MA200", "Вище" if spy_above_ma else "Нижче", "Фільтр широкого ринкового тренду")}
      {_metric_card("VIX", _number(vix), "Фільтр стресу та волатильності")}
      {_metric_card("Кандидати", str(candidate_count), "ETF, які пройшли всі фільтри")}
    </div>
    <section class="decision {'green' if market_ok else 'red'}">
      <div class="decision-title">
        <h2>Рішення на цей місяць</h2>
        {_market_badge(market_ok)}
      </div>
      <div class="decision-text">{escape(decision_text)}</div>
      <p class="decision-subtext">{escape(decision_subtext)}</p>
      <ul class="action-list">
        <li><strong>Купити / додати</strong><span>{escape(to_buy_str)}</span></li>
        <li><strong>Скоротити / замінити</strong><span>{escape(to_sell_str)}</span></li>
        <li><strong>Залишити</strong><span>{escape(to_hold_str)}</span></li>
      </ul>
    </section>
    <div class="grid two-col">
      <section>
        <h2>Кандидати</h2>
        {_candidate_cards(eligible)}
      </section>
      <section>
        <h2>Чому такий сигнал</h2>
        {_list_block(market_reasons, "Немає додаткових пояснень.")}
      </section>
    </div>
    <section>
      <h2>Правила Balanced hybrid</h2>
      <div class="grid three-col">
        {_rule_card("Market OK", "100% внеску", "Розподілити між кандидатами після ручної перевірки.")}
        {_rule_card("Market не OK", "25-50% внеску", "Решту 50-75% залишити в кеші до наступного рапорту.")}
        {_rule_card("Поточні позиції", "Не продавати автоматично", "Вихід раніше - тільки за weekly risk monitor або свідомий ребаланс.")}
      </div>
      <p class="compact-note">Базовий горизонт утримання - один місячний цикл. Тримати довше можна, якщо ETF залишається серед кандидатів і не порушує risk monitor.</p>
    </section>
    <section>
      <h2>Рейтинг ETF</h2>
      <p class="compact-note">Зелений = top-кандидат. Синій = буферне утримання (ETF є в портфелі і утримується за буферним рангом). Жовтий = ETF виконує умови, але не входить у top 4, бо є сильніші кандидати. Сірий = порушує один із фільтрів.</p>
      <div class="table-container">
        <table>
          <thead>
            <tr>
              <th>
                ETF
                <span class="tooltip">ⓘ
                  <span class="tooltiptext">
                    <strong>ETF (Тікер)</strong> — унікальний ідентифікатор біржового фонду на біржі.
                  </span>
                </span>
              </th>
              <th class="num">
                Ранг
                <span class="tooltip">ⓘ
                  <span class="tooltiptext">
                    <strong>Ранг</strong> — місце активу в рейтингу за силою моментуму серед тих, що пройшли первинні фільтри.
                  </span>
                </span>
              </th>
              <th class="num">
                Вага
                <span class="tooltip">ⓘ
                  <span class="tooltiptext">
                    <strong>Цільова вага</strong> — рекомендована частка активу в загальній вартості портфеля (наразі 25% для кожного з топ-4).
                  </span>
                </span>
              </th>
              <th>
                Стратегія
                <span class="tooltip">ⓘ
                  <span class="tooltiptext">
                    <strong>Статус стратегії</strong>:<br/>
                    • <strong>Підходить</strong> — входить у топ-4;<br/>
                    • <strong>Утримання</strong> — утримується за буферним рангом;<br/>
                    • <strong>Є кращі</strong> — проходить фільтри, але поза топ-4;<br/>
                    • <strong>Не проходить</strong> — порушує базові фільтри стратегії.
                  </span>
                </span>
              </th>
              <th class="num">
                Ціна
                <span class="tooltip">ⓘ
                  <span class="tooltiptext">
                    <strong>Ціна</strong> — остання відома ціна закриття однієї акції ETF у валюті торгівлі.
                  </span>
                </span>
              </th>
              <th class="num">
                Моментум 6M
                <span class="tooltip">ⓘ
                  <span class="tooltiptext">
                    <strong>Моментум 6M</strong> — доходність активу за 6 місяців (з виключенням останнього неповного місяця для уникнення шумів). Основний показник для сортування рейтингу.
                  </span>
                </span>
              </th>
              <th class="num">
                1M
                <span class="tooltip">ⓘ
                  <span class="tooltiptext">
                    <strong>Доходність 1M</strong> — зміна ціни активу за останній 1 місяць.
                  </span>
                </span>
              </th>
              <th class="num">
                3M
                <span class="tooltip">ⓘ
                  <span class="tooltiptext">
                    <strong>Доходність 3M</strong> — зміна ціни активу за останні 3 місяці.
                  </span>
                </span>
              </th>
              <th class="num">
                12M
                <span class="tooltip align-left">ⓘ
                  <span class="tooltiptext">
                    <strong>Доходність 12M</strong> — зміна ціни активу за останні 12 місяців (1 рік).
                  </span>
                </span>
              </th>
              <th class="num">
                Волатильність
                <span class="tooltip align-left">ⓘ
                  <span class="tooltiptext">
                    <strong>Волатильність</strong> — річна волатильність активу, розрахована за щомісячними доходностями. Нижча волатильність означає стабільніший тренд (поріг відсікання &lt; 35%).
                  </span>
                </span>
              </th>
              <th class="num">
                Sharpe
                <span class="tooltip align-left">ⓘ
                  <span class="tooltiptext">
                    <strong>Коефіцієнт Шарпа (Sharpe Ratio)</strong> — міра ефективності інвестицій з поправкою на ризик. Розраховується як відношення річної доходності до волатильності. Поріг відсікання &ge; {settings_dict.get('min_sharpe', 0.20):.2f}.
                  </span>
                </span>
              </th>
              <th class="num">
                Просадка
                <span class="tooltip align-left">ⓘ
                  <span class="tooltiptext">
                    <strong>Просадка (Drawdown)</strong> — максимальне падіння ціни активу від його локального піку за розрахунковий період (поріг відсікання &gt; -20%).
                  </span>
                </span>
              </th>
              <th>
                MA200
                <span class="tooltip align-left">ⓘ
                  <span class="tooltiptext">
                    <strong>MA200</strong> — чи знаходиться ціна вище своєї 200-денної ковзної середньої (індикатор довгострокового висхідного тренду).
                  </span>
                </span>
              </th>
            </tr>
          </thead>
          <tbody>{''.join(rows) if rows else '<tr><td colspan="13" class="empty">Немає ETF-метрик для показу.</td></tr>'}</tbody>
        </table>
      </div>
    </section>
    <section>
      <h2>Коротка довідка</h2>
      <ul>
        <li><strong>SPY</strong> - ETF на S&amp;P 500, тут він є індикатором широкого ринку США.</li>
        <li><strong>VIX</strong> - індекс очікуваної волатильності; значення 30+ блокує нові покупки.</li>
        <li><strong>MA200</strong> - 200-денна середня. Вище MA200 = тренд прийнятний для стратегії.</li>
        <li><strong>Моментум 6M</strong> - дохідність за попередні шість місяців, без поточного неповного місяця.</li>
      </ul>
    </section>
    <section>
      <h2>Як читати цільові ваги (наприклад, 25%) та статус рекомендацій</h2>
      <div class="callout" style="border-left-color: var(--blue); background: var(--blue-bg); color: #1f466f; margin-top: 10px;">
        <strong>Цільова вага (наприклад, 25%)</strong> — це бажана частка активу в <em>усьому вашому портфелі</em> після ребалансу, а не відсоток від щомісячного поповнення.<br/><br/>
        <strong>Як зробити ребаланс:</strong>
        <ul style="margin-top: 8px; padding-left: 20px;">
          <li><strong>Не продавайте</strong> активи зі статусом <strong>«Залишити»</strong> (Hold), якщо вони не перевищують цільову вагу.</li>
          <li>Використовуйте <strong>нові кошти</strong> та кошти від продажу активів, які випали зі стратегії (статус <strong>«Скоротити»</strong>), щоб придбати активи зі статусом <strong>«Купити»</strong>.</li>
          <li>Докуповуйте активи так, щоб кожен із обраних кандидатів становив приблизно рівну частку від <em>загальної вартості портфеля</em> після поповнення (наприклад, по 25% на кожен з 4 активів).</li>
        </ul>
      </div>
    </section>
    """


def _scanner_market_reasons(market_ok: bool, spy_above_ma: bool, vix: Any) -> list[str]:
    reasons = []
    if spy_above_ma:
        reasons.append("SPY вище MA200: широкий ринок у висхідному або прийнятному тренді.")
    else:
        reasons.append("SPY нижче MA200: широкий ринок слабший за свій довгостроковий тренд, тому стратегія блокує нові покупки.")

    if vix is None or pd.isna(vix):
        reasons.append("VIX недоступний: фільтр волатильності не може підтвердити спокійний ринок.")
    elif float(vix) < 30:
        reasons.append(f"VIX {_number(vix)} нижче порогу 30: волатильність не є головною причиною блокування.")
    else:
        reasons.append(f"VIX {_number(vix)} на рівні 30 або вище: ринок показує підвищений стрес.")

    if market_ok:
        reasons.append("Обидва ринкові фільтри пройдені, тому стратегія може розглядати ETF-кандидатів.")
    else:
        reasons.append("Оскільки хоча б один ринковий фільтр не пройдено, стратегія обирає захисний режим.")
    return reasons





def _metric_card(label: str, value: str, note: str) -> str:
    return f"""
      <div class="card">
        <div class="metric-label">{escape(label)}</div>
        <div class="metric-value">{escape(str(value))}</div>
        <div class="metric-note">{escape(note)}</div>
      </div>
    """


def _rule_card(label: str, value: str, note: str) -> str:
    return f"""
      <div class="candidate-card">
        <div class="metric-label">{escape(label)}</div>
        <div class="metric-value">{escape(value)}</div>
        <div class="metric-note">{escape(note)}</div>
      </div>
    """


def _candidate_cards(items: list[dict[str, Any]]) -> str:
    if not items:
        return '<p class="empty">Жоден ETF не пройшов фільтри.</p>'

    weight_pct = 100.0 / len(items) if items else 0.0
    cards = []
    for item in items:
        cards.append(
            '<div class="candidate-card">'
            f'<div class="candidate-name">{escape(str(item["ETF"]))}</div>'
            '<div class="candidate-stats">'
            '<div class="stat-row">'
            '<span class="stat-label">Цільова вага</span>'
            f'<span class="stat-value">{weight_pct:.0f}%</span>'
            '</div>'
            '<div class="stat-row">'
            '<span class="stat-label">Моментум 6M</span>'
            f'<span class="stat-value">{escape(_pct(item["Momentum 6M"]))}</span>'
            '</div>'
            '<div class="stat-row">'
            '<span class="stat-label">Sharpe</span>'
            f'<span class="stat-value">{escape(_number(item["Sharpe"]))}</span>'
            '</div>'
            '<div class="stat-row">'
            '<span class="stat-label">Просадка</span>'
            f'<span class="stat-value">{escape(_pct(item["Drawdown"]))}</span>'
            '</div>'
            '</div>'
            '</div>'
        )
    return '<div class="grid three-col">' + "".join(cards) + "</div>"


def _market_badge(market_ok: bool) -> str:
    if market_ok:
        return '<span class="badge green large">Market OK</span>'
    return '<span class="badge red large">Market не OK</span>'


def _tickers_or_dash(tickers: list[str]) -> str:
    return ", ".join(tickers) if tickers else "-"


def _status_badge(status: str) -> str:
    status = str(status).lower()
    css = status if status in {"green", "yellow", "red", "unknown"} else "unknown"
    return f'<span class="badge {css}">{escape(status)}</span>'


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
        return f'<span class="badge yellow">{escape(label)}</span>'
    return f'<span class="badge unknown">{escape(label)}</span>'


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


def _list_block(items: list[Any], empty_text: str) -> str:
    if not items:
        return f'<p class="empty">{escape(empty_text)}</p>'
    return "<ul>" + "".join(f"<li>{escape(str(item))}</li>" for item in items) + "</ul>"


def _list_inline(items: list[Any]) -> str:
    if not items:
        return '<span class="empty">-</span>'
    return "<br>".join(escape(str(item)) for item in items)


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
