"""Messages as codes: the API sends tokens, the words live in the catalogs next to the code that uses them."""

from __future__ import annotations

from functools import lru_cache
import json
from pathlib import Path
import re
from typing import Any

_PACKAGE_DIR = Path(__file__).resolve().parent
LANGUAGES = ("uk", "en")
_COMMAND_LINE_LANGUAGE = "uk"
_TOKEN = re.compile(r"^@(?P<code>[A-Za-z0-9_.\-]+)(?: (?P<parameters>\{.*\}))?$", re.DOTALL)
_PLACEHOLDER = re.compile(r"\{(?P<name>[A-Za-z0-9_]+)(?::(?P<format>[A-Za-z]+)(?::(?P<argument>[^}]*))?)?\}")


def msg(code: str, **parameters: Any) -> str:
    if not parameters:
        return f"@{code}"
    return f"@{code} {json.dumps(parameters, ensure_ascii=False)}"


def catalog_of(directory: Path, language: str) -> dict[str, str]:
    """The texts one strategy (or the engine itself) keeps in its own locales folder, in one language."""
    return json.loads((directory / "locales" / f"{language}.json").read_text(encoding="utf-8"))


def catalogs_of(directory: Path) -> dict[str, dict[str, str]]:
    return {language: catalog_of(directory, language) for language in LANGUAGES}


@lru_cache(maxsize=len(LANGUAGES))
def _all_messages(language: str = _COMMAND_LINE_LANGUAGE) -> dict[str, str]:
    merged: dict[str, str] = {}
    for path in _PACKAGE_DIR.glob(f"**/locales/{language}.json"):
        merged.update(json.loads(path.read_text(encoding="utf-8")))
    return merged


def render(text: Any) -> Any:
    """Words for what the engine prints itself: its command-line reports, in Ukrainian. The service never calls this."""
    if not isinstance(text, str):
        return text
    token = _TOKEN.match(text)
    if token is None:
        return text
    template = _all_messages().get(token["code"])
    if template is None:
        return text
    parameters = json.loads(token["parameters"]) if token["parameters"] else {}
    return _PLACEHOLDER.sub(lambda placeholder: _fill(placeholder, parameters), template)


def render_deep(value: Any) -> Any:
    if isinstance(value, dict):
        return {key: render_deep(item) for key, item in value.items()}
    if isinstance(value, (list, tuple)):
        return [render_deep(item) for item in value]
    return render(value)


def t(code: str, **parameters: Any) -> str:
    return render(msg(code, **parameters))


def _fill(placeholder: re.Match, parameters: dict[str, Any]) -> str:
    value = parameters.get(placeholder["name"])
    if value is None:
        return ""
    if placeholder["format"] == "join":
        return (placeholder["argument"] or " ").join(str(render(item)) for item in value)
    if placeholder["format"] == "plural":
        return _plural(int(value), placeholder["argument"])
    return str(render(value))


def _plural(count: int, forms: str) -> str:
    one, few, many = forms.split("|")
    last_two, last = abs(count) % 100, abs(count) % 10
    if 11 <= last_two <= 14:
        return many
    if last == 1:
        return one
    return few if 2 <= last <= 4 else many
