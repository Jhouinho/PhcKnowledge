#!/usr/bin/env python3
"""Validate an IKP 0.1 ZIP container and its extracted package contract."""
from __future__ import annotations

import argparse
import json
import stat
import sys
import tempfile
import unicodedata
import zipfile
from pathlib import Path, PurePosixPath

from validate_ikp_fixture import validate_package


def safe_entry_name(name: str) -> str:
    if not name or "\\\\" in name or "\\x00" in name:
        raise ValueError(f"ZIP entry path invalid: {name!r}")
    normalized = unicodedata.normalize("NFC", name)
    path = PurePosixPath(normalized)
    if path.is_absolute() or any(part in ("", ".", "..") for part in path.parts):
        raise ValueError(f"ZIP entry path traversal/absolute path: {name!r}")
    if path.as_posix() != normalized or (len(normalized) >= 2 and normalized[1] == ":"):
        raise ValueError(f"ZIP entry path is not canonical: {name!r}")
    return normalized


def validate_zip(zip_path: Path, schema_dir: Path) -> dict:
    if not zip_path.is_file():
        raise ValueError(f"ZIP not found: {zip_path}")
    with zipfile.ZipFile(zip_path, "r") as archive:
        names: set[str] = set()
        folded: set[str] = set()
        total_uncompressed = 0
        for info in archive.infolist():
            name = safe_entry_name(info.filename)
            if info.is_dir():
                continue
            if info.flag_bits & 0x1:
                raise ValueError(f"Encrypted ZIP entry is not supported: {name}")
            mode = (info.external_attr >> 16) & 0xFFFF
            if stat.S_ISLNK(mode):
                raise ValueError(f"Symbolic link ZIP entry is not allowed: {name}")
            if name in names or name.casefold() in folded:
                raise ValueError(f"Duplicate or case-colliding ZIP entry: {name}")
            names.add(name)
            folded.add(name.casefold())
            total_uncompressed += info.file_size
            if info.file_size < 0 or info.compress_size < 0:
                raise ValueError(f"Invalid ZIP entry size: {name}")

        bad_entry = archive.testzip()
        if bad_entry is not None:
            raise ValueError(f"ZIP CRC failure: {bad_entry}")

        with tempfile.TemporaryDirectory(prefix="ikp-zip-validation-") as temp:
            root = Path(temp).resolve()
            for info in archive.infolist():
                if info.is_dir():
                    continue
                name = safe_entry_name(info.filename)
                target = (root / Path(*PurePosixPath(name).parts)).resolve()
                if root not in target.parents:
                    raise ValueError(f"ZIP entry escaped extraction root: {name}")
                target.parent.mkdir(parents=True, exist_ok=True)
                with archive.open(info, "r") as source, target.open("xb") as destination:
                    while True:
                        block = source.read(1024 * 1024)
                        if not block:
                            break
                        destination.write(block)

            result = validate_package(root, schema_dir)
            result["zipEntries"] = len(names)
            result["zipByteLength"] = zip_path.stat().st_size
            result["zipUncompressedByteLength"] = total_uncompressed
            return result


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("zip_path", type=Path, help="IKP ZIP gerado pelo Scanner")
    parser.add_argument(
        "--schemas",
        type=Path,
        default=Path(__file__).resolve().parents[2] / "docs/architecture/schemas/ikp-0.1",
    )
    args = parser.parse_args()
    try:
        result = validate_zip(args.zip_path, args.schemas)
    except (ValueError, OSError, KeyError, TypeError, zipfile.BadZipFile, RuntimeError) as exc:
        print(f"FAIL: {exc}", file=sys.stderr)
        return 1
    print(json.dumps(result, ensure_ascii=False, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
