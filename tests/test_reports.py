from __future__ import annotations

import json
from pathlib import Path
from finance_decisions.reports import (
    write_html_report,
    write_json_report,
    write_text_report,
)


def test_write_text_report(tmp_path) -> None:
    path = tmp_path / "report.txt"
    write_text_report(path, "hello world")
    assert path.read_text(encoding="utf-8") == "hello world"


def test_write_json_report(tmp_path) -> None:
    path = tmp_path / "report.json"
    payload = {"foo": "bar", "val": 123}
    write_json_report(path, payload)
    loaded = json.loads(path.read_text(encoding="utf-8"))
    assert loaded == payload


def test_write_html_report_generic_features(tmp_path) -> None:
    # Set up template directories
    strategy_templates = tmp_path / "strategy" / "templates"
    strategy_templates.mkdir(parents=True)
    
    # Write a test template
    template_file = strategy_templates / "test_report.html"
    template_file.write_text(
        "Title: {{ title }}\n"
        "Value: {{ my_val | pct }}\n"
        "Number: {{ my_num | number }}\n"
        "Custom Filter: {{ status | format_custom }}\n"
        "Custom Test: {% if is_valid is clean %}yes{% else %}no{% endif %}\n"
        "Generated: {{ generated_at }}",
        encoding="utf-8",
    )
    
    output_path = tmp_path / "output.html"
    
    custom_filters = {
        "format_custom": lambda v: f"status is {v}",
    }
    custom_tests = {
        "clean": lambda v: v == "clean_value",
    }
    
    payload = {
        "my_val": 0.125,
        "my_num": 1.234,
        "status": "active",
        "is_valid": "clean_value",
    }
    
    write_html_report(
        path=output_path,
        title="Generic Report Test",
        body_text="plain text",
        payload=payload,
        template_name="test_report.html",
        templates_dirs=[strategy_templates],
        custom_filters=custom_filters,
        custom_tests=custom_tests,
    )
    
    assert output_path.exists()
    content = output_path.read_text(encoding="utf-8")
    
    assert "Title: Generic Report Test" in content
    assert "Value: +12.5%" in content
    assert "Number: 1.23" in content
    assert "Custom Filter: status is active" in content
    assert "Custom Test: yes" in content
    assert "Generated:" in content
