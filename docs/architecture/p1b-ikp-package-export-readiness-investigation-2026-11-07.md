# P1.B — IKP package export readiness investigation

Date: 2026-11-07  
Scope: transition from the validated Object Index projection to a complete IKP 0.1 package.  
Status: historical investigation (2026-11-07); the IKP package exporter was implemented subsequently. Real-capture full-package validation remains open.

## 1. Confirmed baseline

The Object Index projection is implemented as a read-only workspace adapter plus a pure projector:

- `ObjectIndexProjectionInputReader` checks SQLite integrity and reads capture metadata, objects, issues, artifact metadata and bytes only for referenced artifacts.
- `ObjectIndexProjector` validates operation provenance, resolves identities, metadata fingerprints, definition states and parent references, then emits deterministic `object-index.json`.
- The authoritative Draft 2020-12 JSON Schema is exercised by `tools/ikp-validation/validate_object_index.py` in `.github/workflows/dotnet-scanner.yml`.

Verified CI:
- Run `42419610023`: build passed, 275 tests passed, and the C#-generated Object Index passed the official IKP 0.1 schema validator.
- The test suite contains individual projection/mapping cases for all 21 object kinds, including both `phc.user_tsql_function` source variants.
- This validates the Object Index contract and synthetic projector input. It is not a real PHC capture and does not yet constitute a complete IKP package.

## 2. Package contract already available

The package contract and reference validator exist:

- Schemas: `docs/architecture/schemas/ikp-0.1/{manifest,capabilities,coverage,object-index,checksums}.schema.json`.
- Reference validation: `tools/ikp-validation/validate_ikp_fixture.py`.
- Contract: `docs/architecture/ikp-0.1-specification.md`.

The package root requires five JSON documents:

1. `manifest.json`
2. `capabilities.json`
3. `coverage.json`
4. `object-index.json`
5. `checksums.json`

The validator additionally checks cross-file CaptureId consistency, unique artifact IDs and paths, safe relative paths, payload-to-manifest correspondence, byte lengths and SHA-256 values, logical content digest, capabilities/coverage correspondence, issue references, operation support, object parent references and definition artifact references.

A ZIP containing only a valid `object-index.json` is therefore not an IKP package.

## 3. Available source data and material gaps

### 3.1 Data available during the scan

`ScanEngine.RunAsync` returns a `ScanManifest` containing:

- CaptureId;
- InstallationId label;
- start and finish timestamps;
- ScanMode;
- ScannerVersion;
- capability catalog;
- operation coverage;
- collector results and issues.

The workspace persists:
- CaptureId, capabilities JSON, coverage JSON and scan status in `scan_meta`;
- technical objects;
- artifact metadata and artifact bytes;
- collection issues;
- declared and derived relations.

The workspace reader can read all persisted artifacts and issues without mutation.

### 3.2 What is not persisted in the current workspace schema

Current `scan_meta` columns are:

`scan_id`, `installation_label`, `status`, `created_at_utc`, `capture_id`, `capabilities_json`, `coverage_json`.

It does **not** persist:
- actual capture start and completion timestamps;
- ScanMode;
- ScannerVersion;
- a structured origin descriptor (database name, server label, PHC version/signature).

`created_at_utc` is workspace initialization time, not a reliable substitute for capture start. Current scan status is not a completion timestamp. The installation label is a display string and must not be parsed as authoritative origin metadata.

The `ScanManifest` returned by `ScanAppService.RunStructureScanAsync` has the missing capture timing, mode and ScannerVersion in memory, but is not currently persisted as a complete manifest in the workspace.

**Consequence:** a package exporter that receives only an arbitrary/legacy workspace cannot truthfully populate every required manifest field. It must not invent timestamps, mode or producer version.

## 4. Recommended exporter boundary

Implement a dedicated `IkpPackageExporter` in `PhcKnowledgeScanner.Export`, orchestrated by the application layer.

Initial export contract should require:
- the completed `IScanWorkspace`;
- the matching `ScanManifest` returned by the scan;
- an explicit origin descriptor (at minimum engine and database name; server label and PHC version optional);
- output ZIP path and cancellation token.

Before writing:
1. check workspace integrity;
2. compare workspace CaptureId with manifest CaptureId;
3. compare persisted capabilities and coverage with the manifest;
4. require a completed/valid capture state;
5. resolve object index using the existing workspace projector;
6. validate every payload path as canonical safe relative POSIX path;
7. read exact bytes and verify artifact metadata SHA-256 and byte length;
8. fail closed on missing or conflicting data.

This keeps the current workspace schema unchanged for the first exporter implementation and avoids treating a UI label as origin identity.

For a future workflow that reopens an old workspace and exports without the in-memory manifest, a separately reviewed persistence decision is required. Options include a versioned capture-manifest sidecar or a backward-compatible metadata extension. Do not silently use `created_at_utc` or infer mode from collector coverage.

## 5. Initial package scope

First implementation should produce a valid minimal IKP package with the five mandatory root documents and the payload files already represented by workspace artifact rows.

- Map each workspace artifact to a safe path under `payload/` while preserving a stable, non-colliding relationship to its artifact ID.
- Preserve exact bytes; do not normalize line endings or reserialize textual PHC/SQL artifacts.
- Include an issues document when issues exist or are referenced by coverage; preserve IssueId, severity, code, message, object name, detail and OperationId according to the payload contract.
- Generate `manifest.artifacts` from the actual payload files, not from assumptions about collector results.
- Generate `checksums.files` over every package file except `checksums.json`; calculate the logical digest using the exact canonical ordering/line format implemented by the reference validator.
- Keep optional schema snapshot, REL/DTL and derived knowledge payloads out of the first minimal package unless their source files are actually available and their semantics are mapped. Their absence is represented by the relevant capabilities/coverage and omitted optional payload, not fabricated empty files.

## 6. Validation and test sequence

1. Pure serialization tests for capabilities, coverage, manifest and checksums.
2. Package writer tests for safe paths, deterministic ordering, exact artifact bytes, duplicate IDs/paths and cancellation.
3. End-to-end synthetic workspace test: create workspace, save capabilities/coverage, objects, artifacts and issues; pass a matching ScanManifest and origin descriptor; export ZIP.
4. Extract ZIP and run the existing Python reference validator against the package directory.
5. Negative tests: mismatched CaptureId, stale/missing coverage, missing artifact bytes, SHA/length mismatch, unsafe relative path, duplicate artifact ID, missing issue reference, failed operation with observed object, and tampered ZIP entry.
6. Only after the synthetic package passes, validate with a real controlled PHC demo capture. That real capture validates collectors and data fidelity; synthetic tests validate package logic only.

## 7. Explicit non-goals for this step

- No new SQLite tables or columns.
- No changes to PHC/SQL collectors or their queries/filters.
- No inference of installation identity, object removals or PHC standard metadata.
- No forced inclusion of Schema Snapshot, REL/DTL or derived knowledge before their payload mapping is validated.
- No claim that a synthetic package is a real installation capture.

## 8. Next implementation increment

Implement the minimal five-document IKP package exporter using the existing `ScanManifest` plus workspace as separate, cross-validated inputs. Keep the package writer independent from collector execution and preserve exact source artifact bytes. Add the synthetic end-to-end round-trip test and run the reference validator in CI.


## Status update — 2026-11-10

The dedicated `IkpPackageExporter` is now implemented in `PhcKnowledgeScanner.Export` and integrated through CLI commands `scan-ikp` and `export-ikp`. It emits the five mandatory contract files, checksums and logical digest, includes referenced artifacts and the supported REL/DTL payloads, and has dedicated automated tests. The Object Index projection was also exercised with the real `CSDefault202603` capture (8/21 kinds present; 16,618 projected objects; 49 artifacts with size/hash confirmed).

This does **not** yet confirm a complete ZIP generated by the exporter from that real capture and accepted by the independent validator. That is the remaining Scanner validation before/alongside implementation of Receive / Validate / Archive in the central Knowledge platform. No SQLite schema or collector changes are implied.
