from __future__ import annotations

import json
import shutil
import tempfile
import unittest
from pathlib import Path

from validate_ikp_fixture import validate_package


REPO_ROOT = Path(__file__).resolve().parents[2]
FIXTURE = REPO_ROOT / "docs/architecture/fixtures/ikp-0.1/minimal-valid"
SCHEMAS = REPO_ROOT / "docs/architecture/schemas/ikp-0.1"


class IkpFixtureValidationTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.package = Path(self.temp.name) / "package"
        shutil.copytree(FIXTURE, self.package)

    def tearDown(self):
        self.temp.cleanup()

    def read_json(self, relative):
        path = self.package / relative
        return json.loads(path.read_text(encoding="utf-8"))

    def write_json(self, relative, value):
        (self.package / relative).write_text(
            json.dumps(value, ensure_ascii=False, indent=2) + "\n",
            encoding="utf-8",
        )

    def assert_invalid(self, expected_fragment):
        with self.assertRaisesRegex(ValueError, expected_fragment):
            validate_package(self.package, SCHEMAS)

    def test_reference_fixture_is_valid(self):
        result = validate_package(self.package, SCHEMAS)
        self.assertEqual("PASS", result["status"])
        self.assertEqual(4, result["objects"])
        self.assertEqual(8, result["checksummedFiles"])

    def test_tampered_payload_is_rejected(self):
        path = self.package / "payload/customizations/brule/7F3A.prg"
        path.write_text(path.read_text(encoding="utf-8") + "* tampered\n", encoding="utf-8")
        self.assert_invalid("SHA-256 divergente")

    def test_missing_required_contract_file_is_rejected(self):
        (self.package / "capabilities.json").unlink()
        self.assert_invalid("capabilities.json")

    def test_invalid_coverage_enum_is_rejected_by_schema(self):
        doc = self.read_json("coverage.json")
        doc["operations"][0]["result"] = "Unknown"
        self.write_json("coverage.json", doc)
        self.assert_invalid("coverage.json")

    def test_unknown_operation_in_coverage_is_rejected(self):
        doc = self.read_json("coverage.json")
        doc["operations"][0]["operationId"] = "operation.unknown"
        self.write_json("coverage.json", doc)
        self.assert_invalid("operação não suportada")

    def test_reference_to_missing_artifact_is_rejected(self):
        doc = self.read_json("object-index.json")
        doc["objects"][0]["definition"]["artifactIds"] = ["artifact-does-not-exist"]
        self.write_json("object-index.json", doc)
        self.assert_invalid("artifactId não resolvido")

    def test_duplicate_source_object_id_is_rejected(self):
        doc = self.read_json("object-index.json")
        doc["objects"][1]["sourceObjectId"] = doc["objects"][0]["sourceObjectId"]
        self.write_json("object-index.json", doc)
        self.assert_invalid("sourceObjectId duplicado")

    def test_unresolved_issue_reference_is_rejected(self):
        doc = self.read_json("coverage.json")
        doc["operations"][0]["issueIds"] = ["issue-does-not-exist"]
        self.write_json("coverage.json", doc)
        self.assert_invalid("issueId não resolvido")

    def test_unsupported_operation_cannot_have_coverage(self):
        doc = self.read_json("capabilities.json")
        doc["operations"][0]["supported"] = False
        self.write_json("capabilities.json", doc)
        self.assert_invalid("operação não suportada")

    def test_tampered_checksum_manifest_is_rejected(self):
        doc = self.read_json("checksums.json")
        doc["files"][0]["sha256"] = "0" * 64
        self.write_json("checksums.json", doc)
        self.assert_invalid("SHA-256 divergente")


if __name__ == "__main__":
    unittest.main()
