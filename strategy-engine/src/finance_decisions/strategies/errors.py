"""Errors every strategy type raises the same way, so the API maps them without knowing the strategy."""

from __future__ import annotations


class ContextError(ValueError):
    """The evaluation context cannot be used by this strategy (missing series, no prices before asOf)."""
