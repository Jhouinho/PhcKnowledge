#!/usr/bin/env python3
"""Validate a standalone object-index.json against the authoritative IKP 0.1 schema."""
from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

from jsonschema import Draft202012Validator, FormatChecker


REPO_ROOT = Path(__file__).resolve().parents[2]
DEFAULT_SCHEMA = REPO_ROOT / "docs/architecture/schemas/ikp-0.1/object-index.schema.json"


def validate_object_index(document, schema_path: Path = DEFAULT_SCHEMA) -> int:
    schema = json.loads(schema_path.read_text(encoding="utf-8"))
    Draft202012Validator.check_schema(schema)
    validator = Draft202012Validator(schema, format_checker=FormatChecker())
    errors = sorted(
        validator.iter_errors(document),
        key=lambda error: list(map(str, error.absolute_path)),
    )
    if errors:
        first = errors[0]
        location = "/".join(map(str, first.absolute_path)) or "$"
        raise ValueError(f"{location}: {first.message}")
    return len(document["objects"])


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("object_index", type=Path, help="Caminho para object-index.json")
    parser.add_argument("--schema", type=Path, default=DEFAULT_SCHEMA)
    args = parser.parse_args()

    try:
        document = json.loads(args.object_index.read_text(encoding="utf-8"))
        count = validate_object_index(document, args.schema)
    except (OSError, json.JSONDecodeError, ValueError, KeyError, TypeError) as exc:
        print(f"FAIL: object-index.json: {exc}", file=sys.stderr)
        return 1

    print(json.dumps({"status": "PASS", "objects": count}, ensure_ascii=False))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
