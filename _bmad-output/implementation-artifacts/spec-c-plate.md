---
title: "C-Plate — Declared contact-load sensor and Pressure Plate generic instance (9A.20 + 9A.40 + F.6 item F-1)"
status: review
baseline: 61ded7ad7000a296345c30833fba1dcc742fb3c3
---

# Outcome and authority

The Pressure Plate (CAT-052) becomes a `WorkshopGenericInstance` with one parameter, `minimum_mass`. Its contact-load row, its ActivationOut level node and its release hold all come from a declared contact-load capability. That capability is a shared law with no PartId.

`WorkshopPressurePlate`, `WorkshopInput.PressurePlate` and every arm that names them leave the engine.

F-1: a body placed from the palette onto a plate rests on top of it.

Roadmap position 40. Start after 9A.32 merges, and re-derive every count at that baseline.

**Authority:**
- epics C-Plate, 9A.20, 9A.40, F.6 F-1, 9R.4, 9R.5 and 9.1;
- decision (h): `minimum_mass` is f32 kg, range 0.1–16, default 0.5, inclusive ≥. It supersedes 9.1's "> 0.2 kg";
- CAT-052 Q2: ActivationOut is a level that drives lamps only;
- the Q3 override: activation is immediate, and release is held 0.1 s (12 ticks at 120 Hz) for both the contact and ActivationOut;
- CAT-052 §2 and §5;
- the C-Lamp+Detector, C-Gate, C-Switch and 9A.32 precedents.

Native real-simulation C# proof is PRIMARY. Browser proof is pending (21 Oct policy).

**Facts at 2065d3c5:**
- Arms that name the plate: the physics compiler row, the activation node, the encoder case, `MapInstance`, the whitelist, `WorkshopInput`, `CaptureInstance` and the save prevalidation.
- Already generic: the supply route, the level mirror, the presentation, the tracker and the ABI.
- Two colliders are baked; the face is `PartIdentities + 2`.
- 0 lessons use the plate.
- Known: 15 Plate rows of 207 (expect 181 → 166 after 9A.32). The 2 FinalIsolation rows stay.
- Tests: 19 factory sites in 10 files, and 8 files use the type.

# Finite design decisions

**D1 Declared contact load.**
- Add `SignalContactLoadDescriptor(string MinimumMassParameter, int FaceCollider, Vector3 LocalNormal, float MinimumAlignment, float ContactDistance, float DepthMargin, float ReleaseHoldSeconds)` and `IContactLoadSensorElement : ISignalElement`. Add `SignalPayload.ContactLoad?` with a round trip.
- The package declares `("minimum_mass", 1, +Y, 1 − 1e-6f, 1e-4f, 1e-6f, 0.1f)`.
- Footprint: x ± (face half + ContactDistance) and z ± (face half + DepthMargin), using the face box's f32 half-extents.
- The face is `PartIdentities + 1 + FaceCollider`.
- The hold is `ActivationTimerDeclaration.Duration(0.1f, cadence)`.
- Parity pins:
  - the rows are bit-identical to 9R.4's;
  - the face half is exactly .9f;
  - the face is +2;
  - the hold is 6/12/24 at Hz60/120/240.

**D2 Validator.** It returns a `BundleError`, never a throw, for each of these:
- a second node-selecting capability;
- a parameter that is not Real kg with minimum > 0;
- a face that is not a Box in range;
- a malformed normal, alignment, gap, margin or hold;
- a non-Static frame;
- no Level `ActivationOut`.

**D3 Compile.**
- Both compilers read `Signal?.ContactLoad` from a generic instance, wired or not, with no `IsAuthored`/payload gate.
- Rows go in instance order. The node keeps today's branch position (after the latch, before the clock).
- The mass comes from `owner.Parameters`.
- The sensor id keeps `+6`; F-4 stays in F.6.

**D4 Editor.** The generic editor uses `Reject`: out of range is refused, never clamped (9A.40). The value is captured at Run. The editor is disabled while Running, Paused or locked. A new plate takes 0.5 from the declaration.

**D5 Wire and save.** No schema change: `threshold` stays for the Clock.
- A pre-conversion plate record (it carries `threshold`) is refused `OutOfRange` at both boundaries, with the visible message "…generic instance and cannot carry a typed record field". There is no migration (decision h).
- A missing, 0.09 or 16.01 `minimum_mass` is refused `OutOfRange` at both boundaries; a NaN is refused `NonFinite` at the save boundary (the shared generic rule, as at base) and `OutOfRange` at the command boundary.
- 0.1, 0.5, 2.0 and 16 round-trip bit-exact.

**D6 F-1 rest placement.** One engine function, `PlacementPose.Compose(definitions, instances, candidate, at, rotation)` (pure, declared colliders, no PartId), composes grid → tube-mouth snap → rest. `ui/WorkshopGuidance.PlacementPose` only calls it, so the preview, the click and the native tests share one path.
- **Rest applies only when all of these hold:**
  - the candidate is Dynamic;
  - the tube snap did not apply;
  - its posed shapes truly overlap a Static instance collider;
  - that collider's top lies within the candidate's vertical extent;
  - the candidate's x–z centre lies over that collider's footprint.
- **The candidate is then raised** so its lowest point rests on the highest such top. x, z and the rotation are unchanged.
- **The lifted pose is validated (coordinator decision B1, 23 Oct 2026).** `Compose` returns a `ComposedPlacement` whose `PlacementRefusal` is `OutsideBench` when the lifted origin leaves the bench volume (x ∈ [−7, 7], y ∈ [0, 9], z ∈ [−4, 4]; `PlacementPose.Inside`, the same volume the pointer must lie in) and `IntoPart` when any lifted shape overlaps any Static part's collider by more than 10⁻⁴ m (for example a part lifted onto a lower top into a higher box). The preview hides and the click is refused with the typed `BenchPlacementRejection.UnplaceablePose` and its status message: no command, no undo, the construction unchanged. Only a lifted pose is checked; a grid or snapped pose is admitted as before.
- **Otherwise the pose is unchanged.** Move/drag is unchanged. A placement is still one Undo.
- **Scope (coordinator decision B2, 23 Oct 2026):** the rule applies to every flat-topped Static part, not only plates (walls, flat ramps, switches, springboards, baskets, and the vertical-axis slats of pipes, bends, detectors, gates and the funnel). This is the intended single rule: a Dynamic part overlapping a flat static top rests on it. A ball overlapping a horizontal pipe's or detector's upper slat is lifted onto its exterior top; accepted as consistent with the rule.

# Ordered tasks

- [x] **Suite-first native baseline (post-9A.32).**
  - Add a `PlacedParts.Plate(id, x, z, mass)` adapter and move the 19 sites and the `PressurePlateTests.Plate` helper onto it.
  - Run the guard classes (Code Map) on one build and keep the raw receipts.
  - Record golden per-tick traces: threshold, two plates, bounce/lamp, the palette-max bench and battery → plate → gate.
  - Record the compiled rows.
  - Pin the base-encoder old-save sha256s: plain, 2.0 kg, lamp-wired and two-plate saves.
- [x] **Capability (D1–D2):** contracts, payload, validator, round trip, package, parity pins.
- [x] **Conversion (D3–D5).**
  - Generic compile arms; `CaptureInstance`, `MapInstance` and prevalidation use the generic arms.
  - Drop the encode case and the whitelist entry.
  - `cat-052{,-two-plates}.test.ts` read the `minimum_mass` parameter (strict tsc only).
- [x] **F-1 (D6).**
  - `PlacementPose.Compose` plus the UI call.
  - Impact check: run every drawer placement in the `tools/e2e` recipes through Compose at base and candidate, and list each pose that changed. Any change other than a straddle is a Fail.
- [x] **Deletions: 0 production hits.** Record the base counts first as the control.
  - `rg -w "WorkshopPressurePlate|_plateLoaded|PlacedMinimumMass"`
  - `rg "WorkshopInput\.PressurePlate\b"` (tests too)
  - `rg '"pressure_plate"' engine/gpu engine/MachineWorld.Gpu.cs`
  - The plate `-.9f` and `+ 2` literals in the compiler.
  - The static `ReleaseHoldTicks(`.
  - Delete `WorkshopPressurePlate.cs` and its `.uid`.
- [x] **Known rows shrink only.**
  - Remove exactly the 15 Plate rows. Add or reassign none.
  - No new production identifier contains "plate".
- [x] **Latest-design alignment.**
  - CAT-052: the §2 sensor row becomes the declared normal-and-gap capability; update §3; mark §6 Q1 decided.
  - The isolation doc: close the A3/A5/A6 row and the A7 plate cell.
  - Make the `ActivationNetwork` and `WorkshopConnections` comments generic.
  - No alias, bridge or fallback.
- [x] **PRIMARY native proof** (production compile + worker engine, tick progression):
  - *Threshold:*
    - At 0.5, a Basketball is Loaded on its first qualifying tick: the lamp is lit and the gate opens per 9R.4. A Domino is Underweight.
    - At 2.0, a Basketball stays inactive and a Bowling ball activates immediately.
    - At 1.0, a Basketball is Loaded (inclusive).
    - At 16, four Bowling balls are Loaded, while three plus a Basketball is Underweight.
    - At 0.1, a Domino loads the plate.
  - *Hold:* bounce gaps of 11 ticks or fewer stay steady. After the last contact the lamp is lit at 11, dark at 12 and dark at 13. Lifting behaves the same way.
  - *Lamp and refusals:* the level lights the lamp. Counter, Delay and latch inputs are refused with "A level output can only light a Signal lamp."
  - *Negative:* a static box with the plate's exact geometry and no capability never activates. Two plates are independent.
  - *Editor (native seam):* run `InstanceParameterCollection.Validate` → `WithInstance` → `ReplaceConstruction` and Undo in the `PalettePlacementTests` harness.
    - 0.1 and 16 are accepted.
    - 0.09, 16.01 and NaN are refused with no change and no clamp.
    - Applying is one Undo, which restores 0.5.
    - Running, Paused and locked are refused.
  - *F-1:* two Dominoes from `Compose` rest on the plate, reach 0.8 kg Loaded and open the gate. One Domino is the Underweight control. Unchanged-pose controls: a ball in or above a funnel, a domino beside a wall, a ball on a rotated ramp and a pipe mouth (the tube snap wins).
  - *Lifecycle:* Run/Reset is exact (Empty, hold cleared, lamp Slate, gate shut, a tick-identical replay). Save/load replays identically.
  - *Old saves:* the D5 refusals, with their text, at both boundaries, recorded at base and candidate.
- [x] **Identity.**
  - The traces and rows are bit-identical.
  - The `compiled-geometry-9a16` plate row (material 0.1/0.1/0.3) stays DeepEqual.
  - 12×3 lesson bytes and the state after 480 ticks, and the 12×3 placed solutions after 1200 ticks, are identical (a control). Both fixtures are untouched.
- [x] **Checks.**
  - One Debug slnx build with 0 warnings, then the full test with `--no-build --no-restore`.
  - The CI "Test C# tools" step verbatim under **bash**.
  - The Release worker; the Release web publish with `AppBundle` moved out.
  - Strict tsc 6.0.3 and the snapshot unit.
  - `anvil_check`; `git diff --check`.
  - Mutations, each of which must be killed: footprint, hold 0, parameter ignored, exclusivity, rest removed, and rest without the footprint guard.
- [ ] **Independent review → SnapshotApproval → publication.**
  - Commit, merge and push the exact snapshot. Verify the exact-SHA CI, artifact and deployment.
  - Mark `9a-c-plate-contact-load-conversion`, `9a-20` and `9a-40` done.
  - `9-1` stays in-progress, with a native-only addendum.
  - F.6 keeps its other items.
  - Record the terminal scoped Pass. Browser qualification stays pending.

# Pending browser cases (post-catalogue)

- `cat-052`: place dominoes from the drawer only (F-1, no gizmo lift); threshold; bounce.
- 9A.40 editor at 2.0 kg: Bowling vs Basketball; the text/NaN parse; Cancel; out-of-range refusal; Run/Reset; Save/Load.
- Re-run `cat-052-two-plates`, `cat-052-lamp`, `palette-placement`, `cat-051*`, `cat-023b`, `cat-014` and `cat-037`, and (F-1 near misses, B2) `cat-002`, `cat-020`, `engine-core-2a3` and `capacity-9r5b`.
- F-1 rest on every flat-topped Static part (B2), each from the real drawer: a ball dropped overlapping a wall top and a flat ramp rests on it; the same over a switch and a springboard; a ball overlapping a pipe's or a detector's upper slat is lifted onto its exterior top (playability check).
- F-1 refusals (B1): a ball dropped into a plate at the top of the bench (≈ 9 m) shows no preview and is refused with the status message and no undo; a ball lifted onto a plate's base slab into its face is refused the same way.
- `cat-052-lamp` bounce: the 112 px lift now starts from the rested pose; the lift press point may miss the handle if the gizmo follows the rested part (`evidence-c-plate/candidate/f1-impact.txt`).

# Open questions

None are owner-only. F-1's "proven in Chrome" is deferred under the 21 Oct policy, not waived.

# Code Map

- **Change:**
  - Contracts and package: `Abstractions.Signal/SignalContracts.cs`, `Bundle/{BundleSchema,BundleValidator}.cs`, `elements/PressurePlate/PressurePlateElementDeclaration.cs`.
  - Engine: `engine/gpu/{WorkshopPhysicsCompiler,ActivationNetwork,WorkshopConstruction,WorkshopInstances,WorkshopWireFlatBuffers,WorkshopSavePrevalidation,WorkshopConnections}.cs`, `engine/MachineWorld.Gpu.cs`, and the new `engine/PlacementPose.cs`.
  - UI: `ui/WorkshopGuidance.cs`.
  - E2E sources: `tools/e2e/cat-052{,-two-plates}.test.ts`.
  - Fixtures and docs: the Known fixture, tests, CAT-052, the isolation doc, `TODO.md` and `sprint-status.yaml`.
- **Guard classes:**
  - Plate and gate: `PressurePlate`, `PoweredGate`, `NoFixedCapacity`, `LampDetectorGuard`.
  - Presentation and package: `DeclaredPresentation`, `BundlePorts`, `PackageRuntimeParity`, `PartRegistry`.
  - Capacity and resources: `WorkbenchCapacity`, `BasketballResource`.
  - Decode, wire and save: `ConstructionDecodeBoundary`, `WorkshopWireFlatBuffers`, `WorkshopSaveRejection`.
  - Placement: `TubeMouthSnap`, `PalettePlacement`.
- **New tests:** `PlateGuardTests`, `DeclaredContactLoadTests` and `PlacementPoseTests`, plus validator, round-trip and parity cases.
- **Delete:** `engine/gpu/WorkshopPressurePlate.cs` and its `.uid`.
- **Unchanged:**
  - `PartId.cs` and the E2eRunner maps;
  - `workshop_wire.fbs` and the generated files;
  - the tracker and the ABI;
  - the `+6` id;
  - presentation and the supply route;
  - `content/puzzles.json`, `lessons-484ec38.json` and `compiled-geometry-9a16.json`.

# Current state

*Implementation owner, 23 Oct 2026. Uncommitted native candidate in worktree `story-c-plate` on base `61ded7ad`. Tracked diff (excluding `_bmad-output`, untracked new files aside) sha256 `346c9922…` after the re-review fixes (first freeze `20a5d41b…`); new-file hashes in `evidence-c-plate/rereview/new-file-hashes.txt`; probe `evidence-c-plate/probe/ProbePlateCapture.cs.txt` (`ea880125…`). Awaiting independent review. Browser qualification pending.*

**Re-derived on 61ded7ad.** Known: 15 `Owner: Plate` rows of 181 (→ 166), 2 `PartId.PressurePlate` FinalIsolation rows kept. Factory sites: 19 in 10 files (+ 9 `new WorkshopPressurePlate(` record tests). After 9A.32 the plate's record threshold sits in slot 7; the trailing-field rule is unchanged and runs after the typed-field refusal.

**What changed**
- `SignalContactLoadDescriptor(MinimumMassParameter, FaceCollider, LocalNormal, MinimumAlignment, ContactDistance, DepthMargin, ReleaseHoldSeconds)` / `IContactLoadSensorElement`; `SignalPayload.ContactLoad`; validator (D2): sixth node-selecting capability; Real kg parameter with minimum > 0; face = a baked unrotated Box in range; unit normal; alignment (0, 1]; gap and z margin [0, 0.01] m; hold (0, 1] s; Static frame without sliders (so the face id is `PartIdentities + 1 + FaceCollider`); Level `ActivationOut`. The package declares `("minimum_mass", 1, +Y, 1 − 1e-6f, 1e-4f, 1e-6f, 0.1f)` and drops its dead `"pressure"` trigger (threshold 0.5, a second 0.5 kg literal with no consumer).
- `WorkshopPhysicsCompiler.ContactLoadSensorFor` (one helper; f32 face half-extents from `BundleGeometry.Colliders`, not the binary16 admitted colliders) and the generic compile and node arms (node at the former branch position; hold `ActivationTimerDeclaration.Duration(0.1f, cadence)` = 6/12/24).
- Deleted: `WorkshopPressurePlate.cs` (+ `.uid`), `WorkshopInput.PressurePlate`, the `CaptureInstance`, `MapInstance`, encoder and save-prevalidation arms, the whitelist entry, the `-.9f`/`+ 2` literals and the static `ReleaseHoldTicks(`.
- `MapInstance`: a generic record's typed-field refusal now precedes its parameter decode (the save pre-validation's order), so a record-path plate is refused as "Generic instance cannot carry a typed record field." at the command boundary too, not as a missing parameter.
- F-1: `engine/PlacementPose.Compose` (grid → tube snap → rest → lifted-pose validation, returning a `ComposedPlacement` with its `PlacementRefusal`; Box/Sphere only, others refused explicitly; f32 bundle colliders with `DeclaredPhysics.Parameters`; undeclared part → unchanged so the capture's typed refusal stands). `ui/WorkshopGuidance.PlacementPose` only calls it while editable (grid otherwise, as before). `TubeMouthSnap.Pose` became internal for reuse; `ImpactRules` maps the new file to BenchPlacement.
- E2E sources: `cat-052{,-two-plates}` read `minimum_mass`; cat-052, cat-052-two-plates and cat-052-lamp drop the straddle-correcting gizmo lifts (drawer only); the tilted `rolloff` lifts stay. Docs: CAT-052 §2/§3/§6 Q1, isolation A3/A5/A6 and A7, generic `ActivationNetwork`/`WorkshopConnections` comments.

**Decisions and deviations (reviewer to confirm)**
1. **F-1 "top" (accepted).** Rest only on a Static Box with a vertical axis (|axis·Y| ≥ 1 − 1e-6): a tilted box has no defined top, so a ball is never lifted onto a tilted surface (a tilted ramp, a tilted gate). Any box whose axis is vertical does lift, including funnel, pipe, bend, detector and gate slats in that orientation (corrected rationale, review F2; scope accepted by coordinator decision B2). Touching is not overlapping. Spheres are never rest surfaces.
2. **D5 NaN (accepted).** At the save boundary a NaN `minimum_mass` is refused `NonFinite` by the shared generic rule (as at base), at the command boundary `OutOfRange`; D5 now says so. No plate arm.
3. **Negative control.** No static part can carry the plate's exact geometry without the declaration (one bundle per process). The guard uses a Wall sized to the face's exact 1.8 × 1.8 footprint and top height (0.4 m thick; the Wall's minimum); the record path's PipeBend45 impostor test stays.
4. **"Lifting" hold proof.** Native has no lift input, so a Basketball dropped 8 m (first rebound gap 15 ticks) proves lit at n = 11, dark at 12 and 13; the 2 m drop proves the steady bounce; the existing tilted roll-off proves leaving.
5. **Editor seam.** Exercised through the real Configure/Apply buttons in the `PalettePlacementTests` harness (literal "0.09", "16.01", "NaN", "Infinity", "two"; "16" and "0.1"; two Undos restore 0.5; Running, Paused, locked refused).
6. **Lifted-pose validation (B1, fixed in-slice per coordinator decision, 23 Oct 2026).** The earlier claim that an out-of-bounds lifted pose took an existing refusal was false (review B1). `Compose` now refuses a lifted pose outside the bench volume (`OutsideBench`) or overlapping a Static part by more than 10⁻⁴ m (`IntoPart`); the preview (`ShowPreviewAt`) hides and the click refuses with `BenchPlacementRejection.UnplaceablePose` (no command, no undo). Tests: `PlacementPoseTests.ARestedPoseAboveTheBenchIsRefused` (Basketball, Bowling ball, Domino into a plate at 8.9 m; control at 8 m), `ALiftIntoAHigherBoxIsRefused` (ball at 0.7 m lifted onto the base slab into the face; control rests on the face), every admitted case asserts `Refusal == None`; `PalettePlacementTests.ThePreviewAndTheClickShareTheComposedPoseAndItsRefusal` (8.9 m: preview hidden, click refused with the message, no command, no undo, unchanged; 8 m: the preview's pose is the placed pose). Mutants M9 (bench check disabled) and M10 (into-part refusal removed) are killed. Review F1 (rest not iterated to a fixed point) is now a refusal, not an interpenetrating placement.
6a. **Typed-field order (review F3, observed).** For other generic kinds the reorder changes only which message a record with both a typed field and bad parameters gets at the command boundary; the reason (`OutOfRange`) is unchanged.
7. **Process disclosures.** `anvil_validate_write` ran before the first write to each production, test and doc file (the first `PlacedParts` edit was validated straight after). Later edits to an already-validated file were not individually validated: the follow-up test and comment edits, the cat-052-two-plates and cat-052-lamp recipe edits, the 5° and recipe-pattern controls, and every mutation edit and restore. Never validated: the `WorkshopConnections` comment edits, the two csproj edits, the Known-fixture row deletions, `PlacementPose.cs.uid`, and the spec/TODO/sprint-status/evidence writes. `anvil_check`: 0 findings on every changed source file. Shell operations: `git rm` of the two deleted files; `cp` of the guard file from the scratchpad and of the probe into evidence; `perl` on the scratchpad probe (CA1859 return types); `mv` of the published `AppBundle` to the scratchpad; `git worktree add`/`git apply`/`cp`/`git worktree remove` for the throwaway base re-run under the scratchpad. The probe compiled through a temporary absolute `Compile Include`, removed before the frozen build. An earlier candidate capture (`candidate/capture/`, before F-1) is superseded by `candidate/capture-final/`. The F-1 impact screen is a delegated read-only static analysis (no browser).

**Evidence** (`_bmad-output/implementation-artifacts/evidence-c-plate/`)

| Check | Receipt | Result |
|---|---|---|
| Base build + full suite | `baseline/build.log`, `baseline/full-test.log` | 0 warnings; 17/165/77/189/2487 |
| Base guards (16 classes + PlateGuard, test-only diff `baseline/test-only.diff`) | `baseline/guards.log` | 585/585 |
| Base identity re-run: throwaway worktree at 61ded7ad + `test-only.diff` (`8e998317…`) + the shipped `PlateGuardTests.cs` (`483cf6cb…`) + the archived probe (`ea880125…`), removed afterwards | `baseline/rerun/` | 0 warnings; guards 585/585 (PlateGuard included); probe 12/12; every capture byte-identical to `baseline/capture/` |
| Traces ×9 (rows, nodes, bodies, colliders, admission), 12×3 lessons, 12×3 placed solutions | `baseline/capture/`, `candidate/capture-final/`, `candidate/capture-compare.txt` | all byte-identical |
| Old saves (plain, 2.0 kg, locked/rotated 16 kg, lamp-wired, two-plate; `baseline/old-saves.sha256`) | `candidate/capture-final/old-save-outcomes.txt` | base LOADED → candidate refused `OutOfRange` at both boundaries with the D5 text; 0 untyped. Pinned in `DeclaredContactLoadTests` (4 saves × 2 boundaries, sha256-checked). The 2065d3c5-layout plate save stays refused `VerifierRejected` at both boundaries (`DeclaredOrientationTests.ABaseEncoderSaveWhoseSlotsMovedIsRefusedTypedAtBothBoundaries`, plate row) |
| Deletion greps with base control | `candidate/deletion-greps.txt` | production 0 each (base 13/2/22/3/2/1/4) |
| Known list | `candidate/known-rows.diff` | 181 → 166: 15 Plate rows removed, 0 added |
| Frozen Debug slnx build (`--no-incremental`) | `candidate/freeze-build.log` | 0 warnings, 0 errors |
| Full `dotnet test CuriousContraptions.slnx -c Debug --no-build --no-restore --blame-crash` | `candidate/freeze-test.txt`, `freeze-test-full.log` | 17/77/166/189/2527 = 2976/2976, 0 skipped, 0 duplicate-ID notices, rc 0 |
| Final rebuild after the last test-source edits (recipe-pattern controls, typed `PartId` theory data, a corrected comment; no production input changed) and full suite with `--blame-crash` (covers the final TS through E2eRunner's type check) | `candidate/final-build.log`, `final-test.txt`, `final-test-full.log` | 0 warnings; 17/77/166/189/2527 = 2976/2976, 0 skipped, 0 duplicate-ID notices, rc 0 |
| E2E semantic reads of the plate's former threshold | `candidate/e2e-threshold-reads.txt` | none remain (cat-017's `threshold()` is the clock) |
| Guards on the frozen build | `candidate/guards.txt` | 587/587 (+1 editor test, +1 wire setting row) and PlateGuard 13/13 |
| CI "Test C# tools" under bash (on the freeze; no tools input changed afterwards) | `candidate/ci-tools.log` | 77/166/17/174, rc 0 |
| Release worker (Simulation, Animation.Worker); Release web publish (AppBundle moved out) | `candidate/worker-release.txt`, `web-publish-full.log` | 0/0; rc 0 |
| Strict tsc 6.0.3; snapshot unit | `candidate/ts-checks.txt` | rc 0; 24/24 |
| `git diff --check`; `anvil_check` | `candidate/diff-check.txt` | clean; 0 findings |
| F-1 impact screen | `candidate/f1-impact.txt` | a reasoned (not executed) recipe screen: the recipes' changed placements are plate straddles (cat-052, -two-plates, -lamp). The rule itself changes drops on every flat-topped Static part (review `probes/rest-impact.txt`); accepted as the single rule (B2) and queued as pending browser cases with the near-miss recipes |
| Re-review fixes (B1 lifted-pose validation, B2 scope and pending cases, F2/F4 wording, F5 validator cases, F6 worker rc): one `--no-incremental` Debug build, full slnx `--no-build --no-restore --blame-crash`, Release workers, `git diff --check`, `anvil_check` | `rereview/` | 0 warnings; 17/166/77/189/2533 = 2982/2982, 0 skipped, 0 duplicate-ID notices, rc 0; Simulation and Animation.Worker Release rc 0 / 0 warnings; diff check clean; 0 findings; tracked diff `346c9922…` |
| Mutations | `mutation/summary.txt` | M1–M8 all killed (M8 survived the first controls; a 5° ramp control added) |

**New and changed tests:** `PlateGuardTests` (13, suite-first, unchanged since base), `DeclaredContactLoadTests` (18), `PlacementPoseTests` (7), `BundleValidatorTests` (+1), `BundleRoundTripTests` (contact load), `PalettePlacementTests` (+1 editor), `PressurePlateTests` (record validation/wire → generic; compiled hold parity), `ConstructionDecodeBoundaryTests` (plate boundary on `minimum_mass`), `WorkshopWireFlatBuffersTests` (record-path plate threshold refused).

**Pending browser cases (post-catalogue):** as listed above, plus the cat-052-lamp bounce lift from the rested pose (the lift handle press point may miss if the gizmo follows the rested part; `candidate/f1-impact.txt`).
