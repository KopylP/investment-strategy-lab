from __future__ import annotations

import json
from dataclasses import asdict, is_dataclass
from datetime import date, datetime
from html import escape
from pathlib import Path
from typing import Any

import pandas as pd

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
    generated_at = datetime.utcnow().replace(microsecond=0).isoformat() + "Z"
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
    tr.qualified-row td {{ background: #fffaf0; }}
    tr.qualified-row td:first-child {{ border-left: 4px solid var(--yellow); }}
    table {{ width: 100%; border-collapse: collapse; font-size: 14px; }}
    th, td {{ padding: 10px 9px; border-bottom: 1px solid var(--line); text-align: left; vertical-align: top; }}
    th {{ color: var(--muted); font-size: 12px; text-transform: uppercase; letter-spacing: 0; background: #f8fafb; }}
    tr:last-child td {{ border-bottom: 0; }}
    .num {{ text-align: right; font-variant-numeric: tabular-nums; }}
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
      display: grid;
      grid-template-columns: repeat(3, minmax(0, 1fr));
      gap: 8px;
      font-size: 13px;
      color: var(--muted);
    }}
    .candidate-stats strong {{ display: block; color: var(--ink); font-size: 15px; }}
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
      .candidate-stats {{ grid-template-columns: 1fr; }}
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
    return {
        "type": "monthly_scanner",
        "market_ok": result.market_ok,
        "spy_above_ma": result.spy_above_ma,
        "vix": result.vix,
        "table": _dataframe_records(result.table),
        "eligible": _dataframe_records(result.eligible),
        "current_positions": current_positions,
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

    rows = []
    for position in positions:
        rows.append(
            "<tr>"
            f"<td><strong>{escape(position['ticker'])}</strong></td>"
            f"<td>{_status_badge(position['status'])}</td>"
            f"<td class=\"num\">{_money(position['current_price'])}</td>"
            f"<td class=\"num {_value_class(position['pnl_pct'])}\">{_pct(position['pnl_pct'])}</td>"
            f"<td class=\"num {_value_class(position['drawdown_from_high'])}\">{_pct(position['drawdown_from_high'])}</td>"
            f"<td>{'Above' if position['above_ma'] else 'Below'}</td>"
            f"<td>{_list_inline(position.get('alerts', []))}</td>"
            "</tr>"
        )

    return f"""
    <div class="grid summary-grid">
      {_metric_card("As of", report.get("as_of", "-"), "Market data date")}
      {_metric_card("Portfolio P&L", _pct(portfolio_pnl), f"{_money(portfolio_value)} value vs {_money(portfolio_cost)} cost")}
      {_metric_card("SPY vs MA200", "Above" if report.get("spy_above_ma") else "Below", "Broad market trend filter")}
      {_metric_card("VIX", _number(vix), "Volatility stress filter")}
    </div>
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
      <table>
        <thead>
          <tr>
            <th>Ticker</th>
            <th>Status</th>
            <th class="num">Price</th>
            <th class="num">P&L</th>
            <th class="num">Drawdown</th>
            <th>MA200</th>
            <th>Reason</th>
          </tr>
        </thead>
        <tbody>{''.join(rows) if rows else '<tr><td colspan="7" class="empty">No positions found.</td></tr>'}</tbody>
      </table>
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
    rows = []
    eligible_tickers = {str(item["ETF"]) for item in payload.get("eligible", [])}
    for item in payload.get("table", []):
        ticker = str(item["ETF"])
        status = _strategy_status(item, ticker in eligible_tickers)
        row_class = f' class="{status["row_class"]}"' if status["row_class"] else ""
        rows.append(
            f"<tr{row_class}>"
            f"<td><strong>{escape(ticker)}</strong></td>"
            f"<td>{_strategy_badge(status)}</td>"
            f"<td class=\"num\">{_money(item['Price'])}</td>"
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

    eligible = payload.get("eligible", [])
    candidates = [str(item["ETF"]) for item in eligible]
    current = payload.get("current_positions", [])
    market_ok = bool(payload.get("market_ok"))
    spy_above_ma = bool(payload.get("spy_above_ma"))
    vix = payload.get("vix")
    market_reasons = _scanner_market_reasons(market_ok, spy_above_ma, vix)
    candidate_count = len(eligible)
    to_buy = [ticker for ticker in candidates if ticker not in current]
    to_sell = [ticker for ticker in current if ticker not in candidates]
    to_hold = [ticker for ticker in current if ticker in candidates]
    decision_text = (
        "Інвестувати 100% нового місячного внеску в кандидатів після ручної перевірки."
        if market_ok
        else "Інвестувати 25-50% нового внеску як DCA, решту 50-75% залишити в кеші."
    )
    decision_subtext = (
        "Ринковий фільтр пройдено: SPY вище MA200, VIX нижче порогу 30. Режим Balanced hybrid дозволяє повний місячний внесок."
        if market_ok
        else "Ринковий фільтр не пройдено. Balanced hybrid зменшує новий вхід, але не означає автоматичний продаж поточних позицій."
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
        <li><strong>Купити / додати</strong><span>{escape(_tickers_or_dash(to_buy))}</span></li>
        <li><strong>Скоротити / замінити</strong><span>{escape(_tickers_or_dash(to_sell))}</span></li>
        <li><strong>Залишити</strong><span>{escape(_tickers_or_dash(to_hold))}</span></li>
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
      <p class="compact-note">Зелений = top-кандидат. Жовтий = ETF виконує умови, але не входить у top 3, бо є сильніші кандидати. Сірий = порушує один із фільтрів.</p>
      <table>
        <thead>
          <tr>
            <th>ETF</th>
            <th>Стратегія</th>
            <th class="num">Ціна</th>
            <th class="num">Моментум 6M</th>
            <th class="num">1M</th>
            <th class="num">3M</th>
            <th class="num">12M</th>
            <th class="num">Волатильність</th>
            <th class="num">Sharpe</th>
            <th class="num">Просадка</th>
            <th>MA200</th>
          </tr>
        </thead>
        <tbody>{''.join(rows) if rows else '<tr><td colspan="11" class="empty">Немає ETF-метрик для показу.</td></tr>'}</tbody>
      </table>
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


def _scanner_playbook(market_ok: bool, candidate_count: int, current_positions: list[str]) -> list[str]:
    if not market_ok:
        items = [
            "Цього місяця ринковий фільтр не пройдено, тому стратегія зменшує розмір нового входу.",
            "Balanced-рішення: інвестувати 25-50% нового місячного внеску як DCA, а решту 50-75% залишити в кеші.",
            "Поточні позиції не варто автоматично докуповувати тільки тому, що вони вже є в портфелі.",
            "Для вже відкритих позицій використовуй тижневий risk monitor: саме він має підказувати, чи є ризиковий сигнал на вихід.",
        ]
        if current_positions:
            items.append(
                "Оскільки в портфелі є відкриті позиції, головне завдання зараз - контроль ризику, а не збільшення експозиції."
            )
        return items

    if candidate_count == 0:
        return [
            "Ринок пройшов базовий фільтр, але жоден ETF не пройшов усі внутрішні фільтри стратегії.",
            "У такому циклі краще не змушувати стратегію купувати: пропусти місяць або тримай кеш до появи якісних кандидатів.",
        ]

    return [
        "Раз на місяць, після генерації рапорту, переглянь список найкращих кандидатів.",
        "Як базове правило, розподіляй новий капітал рівними частинами між ETF-кандидатами, які ти після ручної перевірки готовий тримати.",
        "Якщо ETF вже є в портфелі і він досі серед кандидатів, його можна залишити без зайвої операції.",
        "Якщо поточна позиція більше не входить до кандидатів, розглянь поступове скорочення або заміну під час місячного ребалансу.",
        "Не докуповуй усередині місяця без нового сигналу: стратегія задумана як місячна, а не щоденна.",
    ]


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

    cards = []
    for item in items:
        cards.append(
            '<div class="candidate-card">'
            f'<div class="candidate-name">{escape(str(item["ETF"]))}</div>'
            '<div class="candidate-stats">'
            f'<span><strong>{escape(_pct(item["Momentum 6M"]))}</strong>Моментум 6M</span>'
            f'<span><strong>{escape(_number(item["Sharpe"]))}</strong>Sharpe</span>'
            f'<span><strong>{escape(_pct(item["Drawdown"]))}</strong>Просадка</span>'
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


def _strategy_status(item: dict[str, Any], is_eligible: bool) -> dict[str, str]:
    if is_eligible:
        return {
            "label": "Top кандидат",
            "css": "green",
            "row_class": "eligible-row",
        }
    if _passes_strategy_filters(item):
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
    if css == "yellow":
        return f'<span class="badge yellow">{escape(label)}</span>'
    return f'<span class="badge unknown">{escape(label)}</span>'


def _passes_strategy_filters(item: dict[str, Any]) -> bool:
    return (
        bool(item.get("Above MA200"))
        and float(item.get("Volatility", 0)) < 0.35
        and float(item.get("Drawdown", 0)) > -0.20
    )


def _list_block(items: list[Any], empty_text: str) -> str:
    if not items:
        return f'<p class="empty">{escape(empty_text)}</p>'
    return "<ul>" + "".join(f"<li>{escape(str(item))}</li>" for item in items) + "</ul>"


def _list_inline(items: list[Any]) -> str:
    if not items:
        return '<span class="empty">-</span>'
    return "<br>".join(escape(str(item)) for item in items)


def _pct(value: Any) -> str:
    if value is None or pd.isna(value):
        return "-"
    return f"{float(value) * 100:+.1f}%"


def _money(value: Any) -> str:
    if value is None or pd.isna(value):
        return "-"
    return f"${float(value):,.2f}"


def _number(value: Any) -> str:
    if value is None or pd.isna(value):
        return "-"
    return f"{float(value):.2f}"


def _value_class(value: Any) -> str:
    if value is None or pd.isna(value):
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
    if pd.isna(value):
        return None
    return value
