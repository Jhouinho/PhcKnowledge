#!/usr/bin/env python3
"""Validate an extracted IKP 0.1 package and its cross-file references."""
from __future__ import annotations

import argparse
import hashlib
import json
import sys
import unicodedata
from pathlib import Path, PurePosixPath

from jsonschema import Draft202012Validator, FormatChecker


CONTRACT_FILES = (
    "manifest.json",
    "capabilities.json",
    "coverage.json",
    "object-index.json",
    "checksums.json",
)


def load_json(path: Path):
    try:
        return json.loads(path.read_text(encoding="utf-8"))
    except Exception as exc:
        raise ValueError(f"JSON inválido ou ilegível: {path}: {exc}") from exc


def sha256(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def safe_relative_path(value: str) -> str:
    if not value or "\\" in value:
        raise ValueError(f"Caminho inválido: {value!r}")
    normalized = unicodedata.normalize("NFC", value)
    path = PurePosixPath(normalized)
    if path.is_absolute() or any(part in ("", ".", "..") for part in path.parts):
        raise ValueError(f"Caminho absoluto ou traversal: {value!r}")
    if path.as_posix() != normalized:
        raise ValueError(f"Caminho não canónico: {value!r}")
    return normalized


def validate_schema(document, schema_path: Path, schema_dir: Path, label: str):
    schema = load_json(schema_path)
    Draft202012Validator.check_schema(schema)
    # The object-index schema uses a local $defs reference. No remote schema
    # resolution or network access is required.
    validator = Draft202012Validator(schema, format_checker=FormatChecker())
    errors = sorted(validator.iter_errors(document), key=lambda e: list(map(str, e.absolute_path)))
    if errors:
        first = errors[0]
        location = "/".join(map(str, first.absolute_path)) or "$"
        raise ValueError(f"{label}: {location}: {first.message}")


def validate_package(package_dir: Path, schema_dir: Path) -> dict:
    package_dir = package_dir.resolve()
    schema_dir = schema_dir.resolve()
    docs = {name: load_json(package_dir / name) for name in CONTRACT_FILES}

    schema_map = {
        "manifest.json": "manifest.schema.json",
        "capabilities.json": "capabilities.schema.json",
        "coverage.json": "coverage.schema.json",
        "object-index.json": "object-index.schema.json",
        "checksums.json": "checksums.schema.json",
    }
    for name, schema_name in schema_map.items():
        validate_schema(docs[name], schema_dir / schema_name, schema_dir, name)

    manifest = docs["manifest.json"]
    capabilities = docs["capabilities.json"]
    coverage = docs["coverage.json"]
    object_index = docs["object-index.json"]
    checksums = docs["checksums.json"]

    capture_id = manifest["capture"]["captureId"]
    for label, doc in (("coverage", coverage), ("object-index", object_index)):
        if doc["captureId"] != capture_id:
            raise ValueError(f"{label}: CaptureId divergente do manifesto")

    checksum_entries = {}
    for entry in checksums["files"]:
        path = safe_relative_path(entry["path"])
        if path in checksum_entries:
            raise ValueError(f"checksums: caminho duplicado: {path}")
        checksum_entries[path] = entry

    actual_files = set()
    for path in package_dir.glob("*.json"):
        if path.name != "checksums.json":
            actual_files.add(path.name)
    payload_dir = package_dir / "payload"
    if payload_dir.exists():
        for path in payload_dir.rglob("*"):
            if path.is_file():
                actual_files.add(path.relative_to(package_dir).as_posix())

    expected_files = set(checksum_entries)
    if actual_files != expected_files:
        missing = sorted(expected_files - actual_files)
        unlisted = sorted(actual_files - expected_files)
        raise ValueError(f"checksums: ficheiros em falta={missing}; ficheiros não declarados={unlisted}")

    for relative, entry in checksum_entries.items():
        safe_relative_path(relative)
        data = (package_dir / relative).read_bytes()
        if len(data) != entry["byteLength"]:
            raise ValueError(f"checksums: byteLength divergente: {relative}")
        if sha256(data) != entry["sha256"]:
            raise ValueError(f"checksums: SHA-256 divergente: {relative}")

    canonical_lines = []
    for relative in sorted(checksum_entries):
        entry = checksum_entries[relative]
        canonical_lines.append(f"{relative}\n{entry['byteLength']}\n{entry['sha256']}\n")
    logical_digest = sha256("".join(canonical_lines).encode("utf-8"))
    if logical_digest != checksums["logicalContentDigest"]["value"]:
        raise ValueError("checksums: logicalContentDigest divergente")

    artifacts = manifest["artifacts"]
    artifact_ids = [item["artifactId"] for item in artifacts]
    artifact_paths = [safe_relative_path(item["path"]) for item in artifacts]
    if len(set(artifact_ids)) != len(artifact_ids):
        raise ValueError("manifest: artifactId duplicado")
    if len(set(artifact_paths)) != len(artifact_paths):
        raise ValueError("manifest: path de artefacto duplicado")
    artifact_by_id = {item["artifactId"]: item for item in artifacts}
    for item in artifacts:
        relative = safe_relative_path(item["path"])
        if relative not in checksum_entries:
            raise ValueError(f"manifest: artefacto não consta em checksums: {relative}")
        data = (package_dir / relative).read_bytes()
        if len(data) != item["byteLength"] or sha256(data) != item["sha256"]:
            raise ValueError(f"manifest: integridade do artefacto divergente: {relative}")

    operations = capabilities["operations"]
    operation_ids = [item["operationId"] for item in operations]
    if len(set(operation_ids)) != len(operation_ids):
        raise ValueError("capabilities: operationId duplicado")
    supported = {item["operationId"] for item in operations if item["supported"]}

    issues_path = package_dir / "payload" / "diagnostics" / "issues.json"
    issues = load_json(issues_path).get("issues", []) if issues_path.exists() else []
    issue_ids = [item["issueId"] for item in issues]
    if len(set(issue_ids)) != len(issue_ids):
        raise ValueError("diagnostics: issueId duplicado")
    issue_id_set = set(issue_ids)

    coverage_ids = [item["operationId"] for item in coverage["operations"]]
    if len(set(coverage_ids)) != len(coverage_ids):
        raise ValueError("coverage: operationId duplicado")
    for operation in coverage["operations"]:
        if operation["operationId"] not in supported:
            raise ValueError(f"coverage: operação não suportada ou não declarada: {operation['operationId']}")
        for issue_id in operation["issueIds"]:
            if issue_id not in issue_id_set:
                raise ValueError(f"coverage: issueId não resolvido: {issue_id}")

    objects = object_index["objects"]
    object_ids = [item["sourceObjectId"] for item in objects]
    if len(set(object_ids)) != len(object_ids):
        raise ValueError("object-index: sourceObjectId duplicado")
    object_id_set = set(object_ids)
    for obj in objects:
        if obj["operationId"] not in supported:
            raise ValueError(f"object-index: operationId não suportado: {obj['operationId']}")
        definition = obj["definition"]
        for artifact_id in definition["artifactIds"]:
            if artifact_id not in artifact_by_id:
                raise ValueError(f"object-index: artifactId não resolvido: {artifact_id}")
        if definition["status"] == "Available" and not definition["artifactIds"]:
            raise ValueError(f"object-index: definição Available sem artefacto: {obj['sourceObjectId']}")

    relations_path = package_dir / "payload" / "relations" / "declared-relations.json"
    if relations_path.exists():
        relations = load_json(relations_path)
        if relations.get("captureId") != capture_id:
            raise ValueError("relations: CaptureId divergente")
        for relation in relations.get("declaredRelations", []):
            if relation["sourceObjectId"] not in object_id_set:
                raise ValueError(f"relations: sourceObjectId não resolvido: {relation['sourceObjectId']}")
        for interpretation in relations.get("interpretations", []):
            if interpretation["sourceObjectId"] not in object_id_set:
                raise ValueError(f"relations: interpretação sem objeto: {interpretation['sourceObjectId']}")
            for issue_id in interpretation.get("issueIds", []):
                if issue_id not in issue_id_set:
                    raise ValueError(f"relations: issueId não resolvido: {issue_id}")

    return {
        "status": "PASS",
        "contractFiles": len(CONTRACT_FILES),
        "checksummedFiles": len(checksum_entries),
        "artifacts": len(artifacts),
        "objects": len(objects),
        "operations": len(coverage["operations"]),
        "issues": len(issues),
        "logicalContentDigest": logical_digest,
    }


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("package_dir", type=Path, help="Diretório de um IKP extraído")
    parser.add_argument("--schemas", type=Path, default=Path(__file__).resolve().parents[2] / "docs/architecture/schemas/ikp-0.1")
    args = parser.parse_args()
    try:
        result = validate_package(args.package_dir, args.schemas)
    except (ValueError, OSError, KeyError, TypeError) as exc:
        print(f"FAIL: {exc}", file=sys.stderr)
        return 1
    print(json.dumps(result, ensure_ascii=False, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
