from __future__ import annotations

from pathlib import Path
import re

from finance_decisions.messages import _all_messages, _plural, msg, render, render_deep, t

SOURCE = Path(__file__).resolve().parents[1] / "src" / "finance_decisions"
CYRILLIC = re.compile(r"[Ѐ-ӿ]")
LITERAL_CODE = re.compile(r"""\b(?:msg|t)\(\s*"([a-z_0-9.]+)"[,)]""")


def _python_sources() -> list[Path]:
    return [path for path in SOURCE.rglob("*.py")]


def test_engine_code_holds_no_text_in_the_users_language() -> None:
    offenders = [str(path) for path in _python_sources() if CYRILLIC.search(path.read_text(encoding="utf-8"))]

    assert offenders == []


def test_every_code_in_the_engine_has_a_text() -> None:
    known = _all_messages()
    used = {code for path in _python_sources() for code in LITERAL_CODE.findall(path.read_text(encoding="utf-8"))}
    shared_with_the_service = {"rationale.default_title", "rationale.default_instrument"}

    assert sorted(used - known.keys() - shared_with_the_service) == []


def test_a_text_that_is_no_message_stays_as_it_is() -> None:
    assert render("просто текст") == "просто текст"
    assert render(5) == 5


def test_a_message_inside_a_message_is_rendered_with_its_parameters() -> None:
    reason = msg("etf_momentum.scan.ranked", rank=2)

    assert t("etf_momentum.plan.exit_dropped_because", reason=reason) == "випав із вибірки: ранг 2 за моментумом"


def test_a_list_of_failures_is_joined_by_the_template() -> None:
    items = [msg("etf_momentum.scan.below_ma"), msg("etf_momentum.scan.no_sharpe")]

    assert t("etf_momentum.scan.failures", items=items) == "ціна нижче MA200; Sharpe недоступний"


def test_counted_words_follow_the_language() -> None:
    forms = "слот|слоти|слотів"

    assert [_plural(count, forms) for count in (1, 2, 5, 11, 21, 22)] == ["слот", "слоти", "слотів", "слотів", "слот", "слоти"]


def test_rendering_a_payload_reaches_every_nested_message() -> None:
    assert render_deep({"notes": [msg("etf_momentum.scan.below_ma")], "count": 1}) == {"notes": ["ціна нижче MA200"], "count": 1}


def test_no_message_is_glued_into_a_joined_text() -> None:
    glued = re.compile(r"""["'][;,] ["']\.join\((?:report|risk|plan|result)\.\w*(?:alerts|notes|reasons|actions)""")

    offenders = [str(path) for path in _python_sources() if glued.search(path.read_text(encoding="utf-8"))]

    assert offenders == []
