from __future__ import annotations

from typing import Any

from fastapi.encoders import jsonable_encoder
from pydantic import ValidationError

from finance_decisions.messages import msg
from finance_decisions.strategies.rule_based.checks import RuleProblem
from .contracts import ParameterProblem

KNOWN_ERROR_TYPES = {
    "missing",
    "extra_forbidden",
    "greater_than",
    "greater_than_equal",
    "less_than",
    "less_than_equal",
    "literal_error",
    "union_tag_invalid",
    "int_type",
    "float_type",
    "string_pattern_mismatch",
    "too_short",
}
SAME_AS = {"float_parsing": "float_type", "int_parsing": "int_type", "int_from_float": "int_type"}
INVALID_CODE = "engine.parameters.invalid"
DICT_KEY_SEGMENT = "[key]"


def shape_problems(raw: dict[str, Any], error: ValidationError) -> list[ParameterProblem]:
    return [_shape_problem(raw, detail) for detail in error.errors(include_url=False)]


def meaning_problem(problem: RuleProblem) -> ParameterProblem:
    return ParameterProblem(path=list(problem.path), code=problem.code, message=msg(problem.code, **problem.parameters))


def json_path_of(raw: Any, loc: tuple[str | int, ...]) -> list[str | int]:
    path: list[str | int] = []
    current = raw

    for segment in loc:
        if segment == DICT_KEY_SEGMENT:
            continue
        if isinstance(current, list) and isinstance(segment, int) and 0 <= segment < len(current):
            path.append(segment)
            current = current[segment]
        elif isinstance(current, dict) and segment in current:
            path.append(segment)
            current = current[segment]
        elif isinstance(current, dict) and segment != current.get("kind"):
            path.append(segment)
            current = None

    return path


def _shape_problem(raw: dict[str, Any], detail: dict[str, Any]) -> ParameterProblem:
    path = json_path_of(raw, tuple(detail["loc"]))
    error_type = SAME_AS.get(detail["type"], detail["type"])
    if error_type not in KNOWN_ERROR_TYPES:
        return ParameterProblem(path=path, code=INVALID_CODE, message=msg(INVALID_CODE, detail=detail["msg"]))

    code = f"engine.parameters.{error_type}"
    context = jsonable_encoder(detail.get("ctx", {}))

    return ParameterProblem(path=path, code=code, message=msg(code, **context))
