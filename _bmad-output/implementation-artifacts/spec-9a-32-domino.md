---
title: "9A.32 — Domino generic instance (CAT-023)"
status: review
baseline: 2065d3c5951232b3e04332101f3d31b45d5e10f1
---

# Outcome and authority

Domino (CAT-023) becomes a zero-parameter `WorkshopGenericInstance`. Its body, box and material come from its package. Its 45° orientation sensor is selected by a declared signal capability. `WorkshopDomino`, `DominoMaterial`, every arm that names them and the last Domino-owned wire fields leave the engine.

Roadmap position 38. Start after C-Lamp+Detector (36) merges, and re-derive every count at that baseline.

**Authority:**
- epics 9A.32 and the delivery-order notes: C-Balls precedes it, and `IsPayload` already excludes the domino;
- CAT-023 §2 and §5;
- the C-Balls, 9A.39 and C-Lamp+Detector precedents.

Native real-simulation C# proof is PRIMARY. Browser proof is pending (21 Oct policy).

# Finite design decisions

- **D1 Declared orientation.** Add `SignalOrientationDescriptor(float ThresholdDegrees)` and `IOrientationSensorElement : ISignalElement`. Add `SignalPayload.Orientation?` with a round trip. The package declares 45f. The validator returns a `BundleError`, never a throw, for:
  - a second node-selecting capability (this extends C-Lamp's count);
  - a threshold that is not finite or lies outside 4–179 (the `OrientationThreshold.Degrees` bounds);
  - no Pulse `ActivationOut`;
  - a body that is not Dynamic.
- **D2 Compile.**
  - The physics compiler and `WorkshopActivationCompiler` read `Signal?.Orientation` from a `WorkshopGenericInstance`.
  - They emit the sensor and the `OrientationSource` node only while the owner is wired (`HasSource`), in instance order, at today's branch position.
  - `Degrees(45f)` is bit-equal to `Degrees(45)`, a Half cos 22.5°. The parity test pins those bits.
- **D3 Body and material.** These already compile from the package: 0.4 kg and the scene-baked box of 0.25 × 1.1 × 0.65. `DominoMaterial` is a redundant Half copy. The proof is that the `compiled-geometry-9a16.json` domino row stays DeepEqual and the fixture is untouched.
- **D4 Inert metadata unchanged.** The `toppled` trigger (a full-angle 0.707) and `LatchMode.SetReset` stay as they are; C-Lamp left the lamp's `Triggers` the same way. Nothing reads them, and the threshold is never derived from them.
- **D5 No authorable parameters.** `Parameters: []`, so there is no editor panel. Every domino teaches the same physical tile.
- **D6 Wire, save and content.**
  - Delete `mass`, `bounce`, `half_extents` and `friction` from `InstanceRecord`; nothing reads them after this slice. The slots renumber: length 8→6, threshold 9→7, target_count 12→8, parameters 13→9. There are no deprecated slots and no migration.
  - `AuthoredScalar` becomes {Length, Threshold}. Delete the template `HalfExtents` member, its required key and its read. Remove 3 scalar keys and the `half_extents` block from each of the 28 templates in `content/puzzles.json`.
  - `CarriesTypedField` narrows to length, threshold and target_count.
  - Old saves:
    - Every record whose decoded meaning changes is refused with a typed reason at both boundaries.
    - A record that uses only slots 0–5 and carries no parameters decodes identically. That is compatibility, not a shim.
    - An old record that decodes to a different valid value is a **Fail**.

# Ordered tasks

- [x] **Suite-first native baseline (post-C-Lamp record path).**
  - Add a `PlacedParts.Domino` adapter and move the ~29 `WorkshopInput.Domino(` sites (18 files) onto it.
  - Run the guard classes (Domino, OrientationSensor, DominoEffect, RigidMass, WorkbenchCapacity, PayloadInstance, save/wire/lesson) on one build; keep the raw receipts.
  - Record golden per-tick traces:
    - close-gap chain;
    - broken gap;
    - 10° rock-back;
    - two-lane strike;
    - unwired end;
    - wired domino_effect.
  - Run the 12×3 lesson probe.
  - Pin the base-encoder old-save bytes by sha256: domino, pipe, plate, clock, counter, latch, every parametered generic and the zero-parameter generics.
- [x] **Capability (D1–D2):** contracts, payload, validator cases, round trip, package, parity pin.
- [x] **Conversion.**
  - `CaptureInstance`, `MapInstance` and prevalidation take the generic arms.
  - Drop the encode case, the whitelist entry and `OfType<WorkshopDomino>`.
  - Make the `ActivationNetwork` comments generic.
  - Audit tests that select a generic instance by type only.
- [x] **Wire deletion and regeneration (D6).**
  - Edit the schema. Regenerate `WorkshopWireGenerated.cs` and `tools/e2e/workshop-wire.ts` with pinned flatc 25.2.10, and diff against fresh output (the unchanged schema must reproduce the committed files first).
  - Edit the `workshop-wire.ts` mapper tail and the `run-reset-snapshot.ts:57-58` field list.
  - Retarget the `addMass` negative in the snapshot unit to `addLength`.
  - Update `content/`. Normalise `lessons-484ec38.json` on the expected side only, after asserting that the domino Material equals its compiled rows.
- [x] **Deletions: 0 production hits.** Record the base counts first as the control.
  - `rg -w "WorkshopDomino|DominoMaterial|MapDomino"`
  - `rg "WorkshopInput\.Domino\b"`
  - `rg '"domino"' engine/gpu engine/MachineWorld.Gpu.cs` (`PartId.cs` keeps its FinalIsolation row)
  - `rg 'InstanceRecord\.(Add)?(Mass|Bounce|Friction|HalfExtents)\b|AuthoredScalar\.(Mass|Bounce|Friction)'`
  - `rg '\b(r|inst)\.(Mass|Bounce|Friction|HalfExtents)\b' engine/gpu/WorkshopWireFlatBuffers.cs engine/gpu/WorkshopSavePrevalidation.cs`
  - `rg '"half_extents"' content engine`
  - Delete `engine/gpu/WorkshopDomino.cs` and its `.uid`.
- [x] **Known rows shrink only (this story's 9A.45a entries).**
  - Remove exactly the 26 `Owner: Domino` rows (207 → 181 if C-Lamp lands as specified). Add or reassign none.
  - The 2 FinalIsolation `PartId.Domino` rows stay.
  - No new production identifier contains "domino".
- [x] **Latest-design alignment.**
  - CAT-023 §2/§3 (declared capability; no record citations) and EL-193.
  - The isolation-doc A5/A6 row, and the `gpu-f32-physics.md` f32-migration row (the compiled Half and cosine lanes remain).
  - The `DeclaredPhysics.cs:63` comment.
  - No alias, bridge or fallback. No 9A.44 broadening.
- [x] **PRIMARY native proof** (production compile + worker engine, tick progression):
  - *Positive:* the cascade topples by real contact; one 45° occurrence; `ActivatedAfter` solved once; traces bit-identical.
  - *Negative:* upright never fires; unwired compiles no sensor and no node; a gap breaks the chain; the 10° rock stays silent; an input into a domino is refused.
  - *Boundary:* validator edges at 4°/179°; highest and lowest placement; two-lane strike (Bowling >60°, Basketball <20°).
  - *Controls:* drawer placement through the generic path; no panel.
  - *Lifecycle:* Run/Reset exact (upright, sensor rearmed); save/load replays identically.
  - *Refusal:*
    - A typed field or a parameter on a generic domino is refused at both boundaries.
    - Run the old-save matrix at both boundaries (save `WorkshopSaveRejection`, command `WireFaultReason`). Single out the plate/clock threshold, which now lands on the parameters slot, and the pipe length, which now lands on target_count.
- [x] **Identity.**
  - The traces above.
  - 12×3 lesson world bytes and the committed state after 480 ticks.
  - 12×3 placed solutions after 1200 ticks, plus a wired domino_effect variant (solution + EndToLamp), because `solution_connections` is empty.
  - All identical to the baseline; both fixtures untouched.
- [x] **Checks.**
  - One Debug slnx build with 0 warnings, then the full test with `--no-build --no-restore`.
  - The CI "Test C# tools" step verbatim under **bash**.
  - Release worker; Release web publish with `AppBundle` moved out.
  - Strict tsc 6.0.3 and the tsx snapshot unit.
  - `anvil_check`; `git diff --check`.
- [ ] **Independent review → SnapshotApproval → publication.**
  - Commit, merge and push the exact snapshot.
  - Verify the exact-SHA CI, artifact and deployment.
  - Mark `9a-32-generic-instance-domino` done and record the terminal scoped Pass. Browser qualification stays pending.

# Pending browser cases (post-catalogue)

- `cat-023a` and `cat-023b`: UI-built row, cascade, 45°, lamp, upright control, Run/Reset, Save/Load.
- `engine-core-2b1` variant: a stored save written by base 2065d3c5 holding a generic part is refused with the "This save cannot be loaded…" message, and the bench is kept.
- Re-run `cat-014`, `cat-052`, `cat-052-lamp`, `clock-master`, `palette-placement` and `engine-core-2b1`.

# Code Map

- **Change:**
  - Contracts: `Abstractions.Signal/SignalContracts.cs`, `Bundle/{BundleSchema,BundleValidator}.cs`, `elements/Domino/DominoElementDeclaration.cs`.
  - Engine: `engine/gpu/{WorkshopConstruction,WorkshopInstances,WorkshopPhysicsCompiler,ActivationNetwork,WorkshopWireFlatBuffers,WorkshopSavePrevalidation,AuthoredInstanceTemplate,DeclaredPhysics}.cs`, `engine/MachineWorld.Gpu.cs`.
  - Wire: `schemas/workshop_wire.fbs`, the generated C#/TS, `tools/e2e/run-reset-snapshot.ts` and its unit.
  - Data: `content/puzzles.json`, the Known fixture and `lessons-484ec38.json` (expected side only).
  - Tests, docs, `TODO.md` and `sprint-status.yaml`.
- **Delete:** `engine/gpu/WorkshopDomino.cs` and its `.uid`.
- **Unchanged:**
  - `PartId.cs` and the E2eRunner maps;
  - `compiled-geometry-9a16.json`;
  - `OrientationSensorDeclaration` and the tracker law;
  - the Half lanes;
  - the package `Triggers` and `LatchMode`;
  - presentation, already declared (pick radius 0.6; the pips follow the committed pose).

# Out of scope (coordinator, 23 October 2026)

These come from CAT-023 §6 unchanged. This slice preserves current behaviour exactly, so they stay as open items in the element doc and are not slice prerequisites.
- Item 3: by game intent, the domino sensor means "has fallen" (tilt). A tile spun about its own axis without tipping does not fire. The current 45° tilt threshold already behaves this way, and a native control should pin it.
  - **Correction (coordinator, 23 Oct 2026):** the note above was wrong. The current sensor fires on total rotation from the placed pose, measured on the shipped engine (`evidence-9a-32/baseline/capture/axial-spin.txt`): a tile spun about its long axis with no tilt fires once the spin passes 45°. This conversion preserves behaviour exactly, so the sensor rule is unchanged and no axial-spin control is added. The "has fallen" tilt rule stays the open CAT-023 §6 item 3 for a future decision.
- Items 1–2: no change in this slice.

The CAT-023 §6 items:
1. The offset centre-of-mass value (CAT-023c).
2. Whether the domino trigger-threshold assistance knots are retired.
3. Whether a tile spun about its own axis, without tipping, should fire the sensor.

# Current state

*Implementation owner, 23 Oct 2026. Uncommitted candidate in worktree `story-9a-32` on base `2065d3c5`; tracked diff (excluding `_bmad-output`) sha256 `f669a79e…` (after the coordinator's 23 Oct decisions); `DominoGuardTests.cs` `613c2956…` (unchanged since the baseline), `DeclaredOrientationTests.cs` `356ab6a9…`; probe source `0733d22e…`. Earlier freezes are under `candidate/superseded-first-freeze/` and `superseded-second-freeze/`. Awaiting independent review. Evidence: `evidence-9a-32/`.*

**Re-derived on 2065d3c5.** Counts as drafted: 26 `Owner: Domino` Known rows (207 → 181), 28 templates, slots 8→6/9→7/12→8/13→9; 29 `WorkshopInput.Domino(` sites in 12 test files. Additions found: `WorkshopSaveRejectionTests` (field enum and refusal rows), `PreConversionLaunchSaveTests` and `DeclaredIndicatorPassageTests` (old-layout saves whose reasons moved), `PalettePlacementTests` (defect test), `WorkshopPipeLengthTests`. `WorkshopFormatTests` needed no change.

**What changed**
- `SignalOrientationDescriptor(float ThresholdDegrees)` / `IOrientationSensorElement`; `SignalPayload.Orientation`; validator: fifth node-selecting capability, finite 4–179°, pulse ActivationOut, Dynamic body. The package declares 45f.
- `WorkshopPhysicsCompiler.OrientationSensorFor` (one helper) and the generic compile/node arms, gated on `WorkshopGenericInstance` + `Signal?.Orientation` + `HasSource`, at the former branch position. Deleted: `WorkshopDomino.cs` (+`.uid`), `WorkshopInput.Domino`, `CaptureInstance`/`MapInstance`/`MapDomino`/encoder arms, whitelist entry, save pre-validation arm and its now-dead material helpers, schema fields `mass`, `bounce`, `half_extents`, `friction`, `AuthoredScalar.{Mass,Bounce,Friction}`, the template `HalfExtents`, and 3 scalar keys + `half_extents` from 28 templates. `CarriesTypedField` is length/threshold/target_count. Wire regenerated with pinned flatc 25.2.10 (`candidate/wire-regeneration.txt`; C# hunks applied with Edit; both artifacts equal fresh output except the pre-existing C# header blank line).
- `MapInstance` pipe arm: an absent length (bits 0) is `WireFaultReason.MissingField` (`RequiredLength`, mirroring the Counter's `TargetCount`), so an old pipe whose length slot moved is refused typed at the command boundary; on base it was a domain `ArgumentException`.
- `DeclaredPhysics.IsPayload` doc comment returned to its member and made generic.

**Assumptions, deviations and decisions (reviewer to confirm)**
1. **Axial spin: decided (coordinator, 23 Oct 2026): behaviour preserved, no control, correction recorded under Out of scope.** The spec says the 45° threshold already ignores spin about the tile's own axis. It does not: the law is total rotation (`|q·q₀| ≤ cos 22.5°`, `WorkshopPhysicsEngine.cs:931`). Measured with the production law (`baseline/capture/axial-spin.txt`, identical on the candidate): floating at 4 rad/s fires at 45.8° yaw with zero tilt, and standing on the bench at 20 rad/s fires at tick 5 with zero tilt. Standing at 4 rad/s, friction stops it at 13.5° and it does not fire. The requested control ("spun without tipping does not fire") would fail. I did not add a weaker control, and I did not change the tracker law, which the spec marks Unchanged and whose change would move compiled sensor semantics and possibly firing ordinals. A tilt-only law would replace `alignment = |q·q₀|` with the half-angle cosine of the declared axis's tilt at that line. Recorded in CAT-023 §6 item 3.
2. **Old saves and the trailing-field rule** (`candidate/old-save-compare.txt`; base-encoder bytes sha256 in `baseline/capture/old-saves.sha256`).
   - Coordinator decision 2 (23 Oct 2026): a record must never be loaded with data ignored. The actual gap was general. A parameter in the current layout was already refused at both boundaries for every zero-parameter package (`WorkshopDominoTests`, `PayloadInstanceTests` ball/bowling, `DeclaredIndicatorPassageTests` lamp/detector). But a field in a slot beyond the 10-field `InstanceRecord` table (the 2065d3c5 parameters vector sat in slot 13) was invisible to the decoder.
   - Shared rule (`WorkshopWireFlatBuffers.HoldsFieldBeyondTheInstanceTable`): any instance record with a present vtable entry at slot ≥ 10 is refused `OutOfRange` at the save boundary (`WorkshopSaveCodec.Decode`, after pre-validation) and the command boundary (`DecodeCommand`, after the construction decode). It walks from the verified root through the vendored `Table`, reads only inside verified vtables and uses no catch. The walk's slots are pinned against the generated accessors.
   - **Consequence:** the base encoder wrote an empty parameters vector in slot 13 for every generic instance, so every base-encoder save holding any generic part is now refused (typed), including zero-parameter ones (balls, lamps, …) that previously loaded identically. Only records using slots 0–5 (the typed latch, the C-Lamp record-path lamp/detector saves) still load. Every matrix bench holds a Basketball, so the whole base-encoder matrix is now refused. 0 untyped.
   - Other reasons (unchanged by the rule, which runs last): domino and its benches OutOfRange; plate and clock VerifierRejected (threshold in the parameters slot); counter MissingField; pipe save OutOfRange / command MissingField; parametered generics OutOfRange.
   - Out of slice: the other wire tables have the same trailing-field gap, but it is forgery-only because their layouts did not change here (owner: wire).
3. **Test expectation changes** (derived, not tuned): `PreConversionRecords…` ball/bowling → VerifierRejected (drag at slot 9 = parameters), domino → OutOfRange; `PreConversionLaunchSaveTests` battery → OutOfRange (its fields lie beyond the 10-field table), domino → OutOfRange; C-Lamp's wired record-path save (counter) → refused MissingField at both boundaries, and the "passes twice" bench now saves with the current encoder; "no domino half extents" → "domino carrying a typed length", "changed ball material" → "ball carrying a typed threshold" (MissingField stays covered by "no counter target"); `PalettePlacement` defect test uses the Latch (a domino now takes the generic path); `TheCommandWireRejectsAPipeWithoutItsLength` → `WireFaultReason.MissingField`; `TypedField` → {Length, Threshold, TargetCount}, domino added. The guard delta is attributed test by test in `candidate/guards-delta.txt` (1039 → 1032).
4. **Lesson golden.** The historical Domino row's `Material` is asserted equal to the compiled body/box/material rows, then normalised on the expected side only. The fixture is untouched.
4a. **domino_effect authored solution: corrected in-slice (coordinator decision 3), a pure data fix.**
   - Cause (`candidate/domino-effect-solution/legacy-solution-cause.txt`, identical on base and candidate): the legacy rows (x = −2.4…0, y = 1) never meet the falling Basketball, whose closest horizontal gap to the first tile is 1.994 m against the 0.465 m contact distance, so tiles 4–6 never move. The last tile overlaps the end tile at placement ([−0.125, 0.125] against [−0.025, 0.225]); the push only rocks the end tile to 7.1°. `solution_connections` was empty.
   - Fix: in `content/puzzles.json`, the four rows now sit at x = −4, −3, −2, −1, y = 0.1 (identity rotation, difficulty unchanged), and `solution_connections` holds end (`fixture_2`) → lamp (`fixture_3`) activation. The production reader only requires these keys, so the lesson's fixtures, inventory and goal are untouched: `lesson-world.txt` is identical on base and candidate.
   - Proof: `TheAuthoredSolutionSolvesAtEveryPrecision` now covers `domino_effect`. It places `domino` rows, reads `solution_connections` and numbers solution parts after the fixtures. It solves at 0, 0.45 and 1, and Reset is exact. In the probe, the content-built construction has the same admission, trace and reset hashes as the hand-built corrected layout, and that layout is bit-identical on base and candidate (`candidate/domino-effect-solution/`).
4b. **E2E reads.** No suite reads a removed record field or content key (`candidate/e2e-field-reads.txt`: every hit is prose or the cat-052-lamp `'bounce'` load label). CAT-023's unpinned citations of the nonexistent `engine/gpu/DominoEffect.cs` and `WorkshopPuzzleId.DominoEffect` now point at `content/puzzles.json`.
5. **Rejected approach.** Translating domain `ArgumentException` to a wire fault in `DecodeConstructionRecord` was tried and reverted: `WireFaultTests` forbids codec catches other than `DecodeParameters`.
6. **Process disclosures.** `anvil_validate_write`/`anvil_apply_patch` ran for the first `PlacedParts` edit, the contracts, the `DeclaredOrientationTests` skeleton and the later-reverted decoder catch. All other writes ran without a prior validate: the test migrations, `DominoGuardTests`, the schema, generated code, content, the Known rows, the docs and the `RequiredLength` helper. `anvil_check` afterwards found 0 findings on all changed files. Shell file operations: `git rm` of the two deleted files; `cp`/`mv` of receipts, the probe source and `AppBundle` (moved to the scratchpad); two throwaway base worktrees under the scratchpad (in one, a scratch csproj copy without the probe include), removed afterwards. The probe compiled from the scratchpad through a temporary `Compile Include`, absent from the candidate. It was amended after the first base capture (a bench-20rad spin case; a revision-1 replay guard; the authored-solution bench). The final source (`probe/ProbeDominoCapture.cs.txt`, `0733d22e…`, which also adds the corrected-layout bench and the legacy-cause fact) re-run on base in a throwaway worktree reproduces every earlier baseline capture byte for byte. Its placed-solutions file adds the three authored-solution lines; it replaces the baseline file, with the earlier file kept in `baseline/superseded/` (`baseline/final-probe-rerun.txt`). Mutant M9's first form failed to build (IDE0051, unused helper) and ran no tests (`mutation/M9-first-form-build-failed-IDE0051.log`). The first freeze (`candidate/superseded-first-freeze/`) preceded the `PlacedParts` summary and CAT-023 citation edits. The CI tools step, Release worker and web publish ran on that first freeze; neither edit is an input of those projects, and the final full suite covers every tools test.

**Evidence** (`_bmad-output/implementation-artifacts/evidence-9a-32/`)

| Check | Receipt | Result |
|---|---|---|
| Base build + test-only diff (adapter, re-sourced material, guards) | `baseline/build.log`, `baseline/test-only.diff` | 0 warnings |
| Base full suite (probe included) | `baseline/full-test.log` | 17/164/77/189/2487; the 2 failures are the static gates flagging the out-of-tree probe |
| Base guard suites (33 classes, `baseline/guards-filter.txt`) | `baseline/guards.log` | 1039/1039 (first attempt failed on a revision bug in the new guard: `guards-first-attempt-revision-bug.log`) |
| Traces ×6, 12×3 lessons, 12×3 placed solutions + wired domino_effect (inventory layout ×3, authored solution ×3), axial spin | `baseline/capture/`, `candidate/capture/`, `candidate/capture-compare.txt` | all byte-identical (incl. compiled bodies, colliders, materials, sensors, nodes) |
| Old saves at both boundaries | `candidate/old-save-compare.txt` | as item 2; 0 untyped |
| Deletion greps with base control | `candidate/deletion-greps.txt` | 0 each (base 17/1/3/8/7/30/7; files 2→0) |
| Known list | `candidate/known-rows.txt` | 207 → 181: 26 Domino rows removed, 0 added; 2 `PartId.Domino` FinalIsolation rows kept |
| Frozen Debug build (`--no-incremental`) | `candidate/freeze-head.txt`, `freeze-build.txt` | 0 warnings, 0 errors |
| Full `dotnet test CuriousContraptions.slnx -c Debug --no-build --no-restore` | `candidate/freeze-test.txt` | WorkerAotGuard 17, BundleElements 165, Coverage 77, E2eRunner 189, CuriousContraptions.tests 2482 = 2930/2930 |
| Guards on the frozen build | `candidate/guards.txt`, `guards-delta.txt` | 1033/1033 (1032 attributed in `guards-delta.txt`, plus the `domino_effect` authored-solution case) |
| CI "Test C# tools" step under bash | `candidate/ci-tools.txt` | 77/165/17/174, rc 0 |
| Release worker | `candidate/worker-release.txt` | 0/0, rc 0 |
| Release web publish (second freeze `83e8501c`; not re-run after the decisions, per the coordinator's check list) | `candidate/web-publish.txt` | rc 0, static files present, AppBundle moved out |
| Strict tsc 6.0.3; snapshot schema unit | `candidate/ts-checks.txt` | rc 0; 24/24 |
| Wire regeneration (flatc 25.2.10) | `candidate/wire-regeneration.txt` | equal to fresh output (pre-existing C# header line aside) |
| `git diff --check`; `anvil_check` | `candidate/diff-check.txt` | clean; 0 findings |
| Mutations (`mutation/`; controls M0 165 + 91, M9-control 214 + 91; restored, `restored-check.txt`) | `M1`–`M10` logs | all killed: M1 sensor ignores wiring (14); M2 Initial = Identity (4); M3 threshold constant (1); M4 node ignores wiring (13); M5 exclusivity omits orientation (1, bundle); M6 body rule weakened (1, bundle); M7 range widened (1, bundle); M8 typed-field check omits threshold (8); M9 absent pipe length not refused (2); M10 trailing-field rule disabled (3) |

**New and changed tests:** `DominoGuardTests` (13, suite-first), `DeclaredOrientationTests` (5 methods, 7 cases: declared selection with a rotated tile and a switch control, the helper's own cosine per threshold, wired/unwired sensors, base-encoder domino/plate/clock/pipe saves refused typed at both boundaries), `BundleValidatorTests` (+1), `BundleRoundTripTests` (orientation), `PackageRuntimeParityTests` (declared ↔ compiled, `0x3b64`), `PalettePlacementTests` (+domino), `WorkshopDominoTests` (generic round trip; typed-field/parameter refusals at both boundaries), `PayloadInstanceTests` (domino typed fields).

**Review follow-up (23 Oct 2026, review-9a-32-domino.md).**
- B1: `DeclaredOrientationTests.AFieldAtExactlyTheFirstSlotBeyondTheTableIsRefusedAtBothBoundaries` forges a record whose only extra field is at slot 10, refused `OutOfRange` at `WorkshopSaveCodec.Decode` and `DecodeCommand`; the same record without it loads.
- B2: the pending `engine-core-2b1` base-save refusal case is listed above.
- F2 (`Decode` summary) and F5 (comment spacing) are fixed.
- The test states slot 10 as a schema literal: a first version read the rule's own constant and let the reviewer's mutant survive (`mutation/M11-first-run-survived-test-read-the-constant.log`). Mutant M11 (`InstanceRecordFields = 11`) is now killed by that test (`mutation/M11-table-size-eleven.log`), and the constant is restored.
- One rebuild (0 warnings), then the full slnx `--no-build --no-restore` (`candidate/review-fix-test.txt`): 17/165/77/189/2483 = 2931/2931. The first run of that suite crashed the test host with exit code 139 after 2045 tests, none of them failing (`review-fix-test-run1-crashed-139.txt`). A rerun on the same binaries passed in full. The crash is unexplained, the reviewer should note it, and an earlier freeze is in `superseded-review-fix-1/`.
- `git diff --check`: clean (`candidate/review-fix-diff-check.txt`).
- B3 (re-review): `ConstructionDecodeBoundaryTests.FloatBoundaries` dropped the `(float)int.MaxValue` row, which is `TwoTo31` bit for bit. Its explicit `0xFFC00000` NaN row (bit-identical to `float.NaN` on .NET) became the positive quiet NaN `0x7FC00000`, so the negative quiet, positive quiet and signalling encodings are all covered. One rebuild (0 warnings), then the full slnx: 17/165/77/189/2487 = 2935/2935 (4 more than before, from the formerly skipped NaN rows), with 0 duplicate-ID notices (`candidate/review-fix-test.txt`). `git diff --check` is clean. The B2 wording now says "holding a generic part". The earlier receipts are in `superseded-review-fix-2/`.

**Next:** independent re-review of the fixes; SnapshotApproval → publication.
