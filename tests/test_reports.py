from __future__ import annotations

from datetime import date

import pandas as pd

from finance_decisions.reports import scanner_report_payload, risk_report_payload, write_html_report
from finance_decisions.risk import PositionRisk, RiskReport
from finance_decisions.scanner import ScanResult


def test_write_html_report_renders_risk_sections(tmp_path) -> None:
    report = RiskReport(
        as_of=date(2026, 5, 22),
        spy_above_ma=True,
        vix=16.7,
        market_alerts=[],
        position_risks=[
            PositionRisk(
                ticker="QQQ",
                status="green",
                current_price=717.54,
                pnl_pct=0.12,
                pnl_abs=120.0,
                drawdown_from_high=-0.03,
                above_ma=True,
            )
        ],
        portfolio_pnl=0.12,
        portfolio_value=1120.0,
        portfolio_cost=1000.0,
        actions=[],
    )

    path = tmp_path / "report.html"
    write_html_report(path, "Weekly Risk Monitor", "text report", risk_report_payload(report))

    html = path.read_text(encoding="utf-8")
    assert "Portfolio P&amp;L" in html
    assert "Position Risk" in html
    assert "How To Read This Report" in html


def test_write_html_report_renders_scanner_sections(tmp_path) -> None:
    table = pd.DataFrame(
        [
            {
                "ETF": "QQQ",
                "Price": 500.0,
                "Momentum 6M": 0.15,
                "Return 1M": 0.03,
                "Return 3M": 0.08,
                "Return 12M": 0.2,
                "Volatility": 0.18,
                "Sharpe": 1.4,
                "Drawdown": -0.04,
                "Above MA200": True,
            },
            {
                "ETF": "SPY",
                "Price": 450.0,
                "Momentum 6M": 0.08,
                "Return 1M": 0.02,
                "Return 3M": 0.05,
                "Return 12M": 0.12,
                "Volatility": 0.16,
                "Sharpe": 1.1,
                "Drawdown": -0.02,
                "Above MA200": True,
            }
        ]
    ).set_index("ETF")
    eligible = table.loc[["QQQ"]]
    result = ScanResult(
        market_ok=True,
        spy_above_ma=True,
        vix=18.0,
        table=table,
        eligible=eligible,
    )

    path = tmp_path / "scanner.html"
    write_html_report(
        path,
        "Monthly ETF Scanner",
        "text report",
        scanner_report_payload(result, current_positions=["QQQ"]),
    )

    html = path.read_text(encoding="utf-8")
    assert "Рішення на цей місяць" in html
    assert "Кандидати" in html
    assert "Рейтинг ETF" in html
    assert "Чому такий сигнал" in html
    assert "Правила Balanced hybrid" in html
    assert "Balanced hybrid" in html
    assert "25-50%" in html
    assert "Базовий горизонт" in html
    assert "Підходить" in html
    assert "eligible-row" in html
    assert "Є кращі" in html
    assert "qualified-row" in html
    assert "QQQ" in html
