---
title: '9A.27 Generic Instance and Parameter Editor — Wall'
type: refactor
created: '2026-10-21'
status: in-review
route: dispatch
baseline_commit: 83de446db17f08d8caa179468dec088a60f4252d
review_loop_iteration: 0
context:
  - AGENTS.md
  - docs/architecture/element-package-isolation.md
  - docs/planning/elements/CAT-066-wall.md
---

<frozen-after-approval reason="coordinator-authorised bounded Wall conversion">
## Intent
Wall (CAT-066) becomes a generic declared instance with typed f32 parameter values and an editor generated from the package schema. Preserve collision, pose, presentation, resizing, lifecycle and saved properties while deleting the Wall record path. Exact Story9A.27 and merged9R.14 acceptance in epics remains authoritative; latest owner policy defers routine browser proof until the whole catalogue is implemented.

## Boundaries & Constraints
One element only. Preserve existing declaration ranges: width0.4–8m/default3; height0.4–6m/default2; thickness0.12–2m/default0.25. Package owns geometry/material, parameter schema and presentation. No compatibility reader, alias, format version or new solver. Other records/panels retain their named conversion owners; change only necessary BumperAdvanced caller.

9A.25a is deployed and independently accepted on the baseline above. MachinePart already supports generic dimensions/presentation. Coordinator numeric decision: f32 is exact through generic authored parameters, save/wire and evaluated bundle geometry. Existing common collider admission/ABI still narrows geometry to Half and remains9A.44 ownership unchanged. Delete the Wall-specific Half adapter; do not claim whole-runtime f32 migration. Compare compiled rows for identical canonical inputs; distinguish intentional restored f32 parameter bounds from material/geometry regressions.

## I/O & Edge-Case Matrix
| Given / when | Then |
|---|---|
| Valid declared parameters submitted | Commit all values together, retain f32 bits, update geometry/presentation |
| Unknown, missing, duplicate, nonfinite or out-of-range submitted parameters | Reject atomically; old instance/save remains unchanged |
| Finite pointer resize exceeds range | Clamp at declared limit; nonfinite rejects |
| Running, paused or locked part edited | Reject; construction remains unchanged |
| Rotated/resized wall across ball path | Ball stops/rebounds; no-wall control continues |
| Cancel/one-gesture Undo, Run/Reset or Save/Load | Restore exact authored dimensions and pose |

</frozen-after-approval>

## Code Map
- `elements/Wall/WallElementDeclaration.cs`: sole ranges, material, box bindings and sizing; preserve.
- `engine/gpu/WorkshopInstances.cs`, `DeclaredPhysics.cs`: generic instance parameter ownership and validation; remove Wall whitelist/adapter.
- `engine/gpu/WorkshopConstruction.cs`, `WorkshopWall.cs`: remove Wall factory/record/dimensions.
- `schemas/workshop_wire.fbs`, `WorkshopWireFlatBuffers.cs`, `WorkshopSavePrevalidation.cs`: generic parameter vector and structural admission; remove Wall encode/decode/check arms, no migration.
- `engine/MachineWorld.Gpu.cs`, `ui/WorkshopConfiguration.cs`: generic capture/edit using declared schema; retain unrelated record panels.
- `engine/MachinePart.cs`, `ui/RotationGizmo.cs`: reuse existing generic presentation/gesture behavior.
- `engine/gpu/BumperAdvanced.cs`: recognize generic Wall for existing lesson inventory only.

## Tasks & Acceptance
- [x] Capture record-path native Wall/Bumper/golden baseline using one matching build: 70/70 passed344ms (tool80ec60), build0warnings15.11s (tool62719f).
- [x] Add immutable generic parameter values with strongly typed extensible keys, declared validation and value equality; wire/save owns exact f32 values.
- [x] Implement declaration-driven generic editor (units/ranges) and atomic application through existing construction command; capture generic resize parameters without element-id Wall arm.
- [x] Delete WorkshopWall, WallDimensions, WorkshopInput.Wall, Wall adapter and all obsolete callers/fixtures/whitelist/codec arms. Existing common collider admission stays unchanged.
- [x] Align with current package-isolation and shared f32 authority; retain only the explicitly owned common collider ABI boundary under9A.44, no new shim.
- [x] Prove production C# simulation ball blocking versus no-wall, rotated contact, controls/limits, ticks, Run/Reset/save/load, generic editor/resize Cancel/Undo and affected Bumper lesson integration. Compare existing compiled golden, plus exact non-Half f32 authored/evaluated geometry.
- [ ] Carry9A25a terminal review receipt, refresh current TODO/sprint and append6.5d/9R.14 review addendum; retained independent review, exact SnapshotApproval and publication.

Given a Wall placed/configured, the editor exposes declared values with units/ranges and the instance stores them as f32. Given invalid submission, no durable or visible state changes. Given supported lifecycle or persistence, dimensions/pose restore exactly. Given source scan, deleted names have zero production hits. All original behavior clauses remain; emitter-dependent light/air/sound occlusion remains with named later capability owners.

## Verification
C# UNIT tests executing the real production simulation are PRIMARY proof; integration is supplemental. Build affected native closure once per relevant-input/config change. Run matching tests with --no-build --no-restore. Baseline: WorkshopWallTests, WorkshopWallResourceTests, WorkshopBumperTests and PackageRuntimeParityTests. Add focused real simulation and generic editing/serialization negative controls where existing tests leave gaps; broaden only to affected consumers. Worker build and normal hook/CI protections remain. No new harness, Full/timing/calibration or browser campaign.

Pending post-catalogue browser cases: cat-066, cat-015b and other affected Wall construction/integration suites; actual UI drawer/editor/local-axis handles, projections, Cancel/Undo, controls and Reset/save/load. Do not claim browser qualification from native results.

## Implementation Notes
Anvil graph for this new worktree returned not_ready; scoped source searches established closure. No extra graph platform work.

## Original Evidence and Scope
All native test commands used `dotnet test CuriousContraptions.tests/CuriousContraptions.tests.csproj --no-build --no-restore` with the named class/method filters. Original tool results are retained; no copied raw manifest. C# builds used that project and `-v:q`; worker build used `CuriousContraptions.Simulation/CuriousContraptions.Simulation.csproj -c Release -v:q`.

| Criterion / affected closure | Original evidence |
|---|---|
| Baseline Wall/Bumper/compiled rows | 70/70,344ms tool80ec60; build tool62719f |
| Wall real collision/control at0/45/90degrees and64m/s; generic values, exact f32/save, editor guards/Cancel/Undo/clamping, malformed saves/vectors | Final focused113/113,7.8892s session88267 (result store run27_88267; summary tool55bb6e), filter WorkshopWallTests, WorkshopWallResourceTests, PalettePlacementTests, WorkshopSaveRejectionTests. Includes120tick Run/Reset/save replay. |
| Bumper, immutable compiled golden, wire and capacity consumers | Initial focused244 passing of251, tool91a9d3 session32481. Seven failures corrected below; reviewer accepts unchanged passing input applicability. Filter additionally WorkshopBumperTests, PackageRuntimeParityTests, WorkshopWireFlatBuffersTests, WorkbenchCapacityTests. |
| Format/wire/no fixed capacity and static boundary pins | 119 passing of120, tool1dac64 session80809; filter WorkshopFormatTests, WorkshopWireTests, NoFixedCapacityTests and three pinned static methods. One static classification corrected below. |
| Final UI classification and editor/gesture regression | 3/3,921ms toola3e00a session72730: TheEditValidationPathsRefuseOnlyWithTheTypedRejection, GenericWallEditor, GenericWallResize. Final native build0warnings/errors8.28s tool8987d1 session27067. |
| Worker affected build | Release0warnings/errors12.38s toolbba3b8 session41645; subsequent changes UI exception classification and schema test parser only. |
| Generated schema consumers | flatc25.2.10 generated C#/TS from workshop_wire.fbs; strict TS6.0.3 compilation exit0 toold18c85. Browserless schema parser tests24/24,114.8ms tool0e2fba. |
| Deletion and source closure | WorkshopWall/WallDimensions/WorkshopInput.Wall/AuthoredHalfDimension zero hits under engine/ui/elements/schemas/tools; removed record and uid; no Wall whitelist/capture/codec fallback. Common Half admission unchanged, owned9A.44. |

Failed attempts retained: builds68221/88480 exposed analyzer/test-callsite adaptation issues, corrected before clean build9339. Initial251-test run had five missing typed UnsupportedPart guards in generic capture, one malformed-key exception escaping typed save refusal, and one new lifecycle fixture using revision1 instead of required2. Corrected causal113-test run passes. The120-test run exposed ArgumentException for an impossible admitted units enum; use UnreachableException and rerun affected static/editor methods. Initial schema23/24 exposed the test inventory parser omitting `(required)` fields; corrected parser now checks the complete generated layout. No valid expected behavior weakened.

## Spec Change Log

## Review Triage Log
- Retained reviewer design/dependency precheck Pass: exact f32 authoring/persistence/evaluation, unchanged common ABI admission, bounded Wall conversion.
- Mixed-format wire finding: remaining typed record variants silently ignored generic parameters. Reject nonempty carriage before admission; forged vectors cover every remaining typed variant plus missing/duplicate/unknown/blank keys. Reviewer source recheck accepted.
- Final scoped checks have no known unresolved failure. SnapshotApproval and remote/CI/deployment checks remain pending; browser proof explicitly deferred until catalogue completion.

Publication hook forward fix: session78006 refused commit with123/124 hook tests passing; `EveryUiCatchReportsThroughTheClassificationOrIsAFaultOrRethrows` identified the inner validation catch translating instead of directly reporting. It now reports the same typed rejection and returns before mutation. Necessary native rebuild22374 passed0warnings/errors8.56s (toola00f1c); matching no-build/no-restore causal filter `WorkshopUiFailureTests|TheEditValidationPathsRefuseOnlyWithTheTypedRejection|GenericWallEditor|GenericWallResize` passed37/37 in872ms (tool993441 session16586). No assertion weakened; updated SnapshotApproval required.

CI39633245604 on beacc787f6b73572dddb5eae66c23dfd5b63667a passed2013/2014; deployment skipped. Sole failure: old WireFaultTests catch pin lacked the reviewed declared-parameter normalization boundary. Test-only forward fix preserves global catch allowlist and admits exactly one DecodeParameters ArgumentException catch whose sole statement throws WireFaultException.OutOfRange carrying the original error. No swallow/retry is allowed. Initial test build63277 required the standard cancellation token; corrected build26062 clean7.22s (tool3bace5). Matching no-build/no-restore WireFaultTests|WorkshopSaveRejectionTests|WorkshopWallTests passed132/132,5s (toolaceac1 session32100). Runtime unchanged; earlier2013 CI passes remain applicable.
