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
        with self.assertRaisesRegex(ValueError, "is not one of"):
            validate_object_index(self.document, DEFAULT_SCHEMA)

    def test_invalid_capture_uuid_is_rejected(self):
        self.document["captureId"] = "not-a-uuid"
        with self.assertRaisesRegex(ValueError, "is not a 'uuid'"):
            validate_object_index(self.document, DEFAULT_SCHEMA)

    def test_duplicate_artifact_ids_are_rejected_by_unique_items(self):
        self.document["objects"][0]["definition"]["artifactIds"] = [
            "artifact-schema-ft",
            "artifact-schema-ft",
        ]
        with self.assertRaisesRegex(ValueError, "has non-unique elements"):
            validate_object_index(self.document, DEFAULT_SCHEMA)

    def test_objects_array_no_longer_declares_unique_items(self):
        schema = json.loads(DEFAULT_SCHEMA.read_text(encoding="utf-8"))
        self.assertNotIn("uniqueItems", schema["properties"]["objects"])
        self.assertTrue(
            schema["properties"]["objects"]["items"]["properties"]["definition"]["properties"]["artifactIds"]["uniqueItems"]
        )


if __name__ == "__main__":
    unittest.main()
