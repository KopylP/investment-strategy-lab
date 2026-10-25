"""Write the OpenAPI document of the engine to contracts/v1/openapi.json.

The file is the published contract with the .NET backend: both sides test against it.
Run after any change to finance_decisions/api/contracts.py:

    uv run python tools/export_contract.py
"""

from __future__ import annotations

import json
from pathlib import Path

from finance_decisions.api.main import create_app

CONTRACT_PATH = Path(__file__).resolve().parents[1] / "contracts" / "v1" / "openapi.json"


def main() -> None:
    document = create_app().openapi()
    CONTRACT_PATH.parent.mkdir(parents=True, exist_ok=True)
    CONTRACT_PATH.write_text(json.dumps(document, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    print(f"Contract written to {CONTRACT_PATH}")


if __name__ == "__main__":
    main()
