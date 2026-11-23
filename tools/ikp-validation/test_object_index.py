from __future__ import annotations

import json
import unittest
from pathlib import Path

from validate_object_index import DEFAULT_SCHEMA, validate_object_index


REPO_ROOT = Path(__file__).resolve().parents[2]
FIXTURE = REPO_ROOT / "docs/architecture/fixtures/ikp-0.1/minimal-valid/object-index.json"


class ObjectIndexSchemaTests(unittest.TestCase):
    def setUp(self):
        self.document = json.loads(FIXTURE.read_text(encoding="utf-8"))

    def test_reference_object_index_is_valid(self):
        self.assertEqual(3, validate_object_index(self.document, DEFAULT_SCHEMA))

    def test_invalid_definition_status_is_rejected(self):
        self.document["objects"][0]["definition"]["status"] = "Unknown"
        with self.assertRaises(ValueError):
            validate_object_index(self.document, DEFAULT_SCHEMA)

    def test_object_index_v1_is_rejected(self):
        self.document["schemaVersion"] = "1"
        with self.assertRaises(ValueError):
            validate_object_index(self.document, DEFAULT_SCHEMA)

    def test_unknown_reason_code_is_preserved_and_accepted(self):
        fingerprint = self.document["objects"][1]["definition"]["definitionFingerprint"]
        fingerprint["reasonCode"] = "FutureReasonCode"
        self.assertEqual(3, validate_object_index(self.document, DEFAULT_SCHEMA))

    def test_unavailable_fingerprint_cannot_contain_digest(self):
        fingerprint = self.document["objects"][1]["definition"]["definitionFingerprint"]
        fingerprint["value"] = "a" * 64
        with self.assertRaises(ValueError):
            validate_object_index(self.document, DEFAULT_SCHEMA)

    def test_invalid_capture_uuid_is_rejected(self):
        self.document["captureId"] = "not-a-uuid"
        with self.assertRaisesRegex(ValueError, "is not a 'uuid'"):
            validate_object_index(self.document, DEFAULT_SCHEMA)


if __name__ == "__main__":
    unittest.main()
