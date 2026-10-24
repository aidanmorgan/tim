---
title: "9A.44 — f32 everywhere (Half purge and double game values to f32)"
status: done
baseline: f1ad3bd19e27c32cb05ba34f83ccc1d6cf59fc76
review_loop_iteration: 0
followup_review_recommended: true
deferred:
  - summary: >-
      The run-reset-snapshot.ts mirror parity check (its mirrored assistance and save fields) has not been run against the f32 layout.
    evidence: |-
      The mirror lives in the browser E2E toolchain, which owner policy (24 Oct) defers until every named puzzle element passes unit and integration tests; it compiles under tsc but is not executed.
    location: >-
      tools/e2e/run-reset-snapshot.ts
    severity: low
context:
  - AGENTS.md
  - docs/testing-guidance.md
  - docs/gpu-f32-physics.md
  - docs/architecture/element-package-isolation.md
---

## Working rules (coordinator)

- Edit only inside this worktree (/Users/aidan/dev/personal/tim/.claude/worktrees/story-9a-44). Use Write/Edit for file changes, run Anvil validation before each write, and use the pinned flatc 25.2.10 for generated wire code.
- Run one `dotnet build CuriousContraptions.slnx -c Debug` per changed input set, then `dotnet test CuriousContraptions.slnx -c Debug --no-build --no-restore`. NEVER use `--blame-crash` on the full solution. Run the node harnesses once, the CI tools step from .github/workflows/pages.yml under bash, the Release worker builds, and `git diff --check`.
- Hard rules apply: no skipped tests, no output-hash assertions, docs/testing-guidance.md is binding, and new or changed non-C# code is TypeScript only where the conversion slice already exists; otherwise keep the edit minimal in existing JS and record it for the conversion slice. No repeat-N stress loops as evidence.
- Do not commit; the coordinator runs review and publication. Keep the evidence logs in _bmad-output/implementation-artifacts/evidence-9a-44/. Keep a terse "Current state" section at the end of this spec.


# Outcome and authority
Every game value is IEEE-754 f32 end to end, from authoring through construction, save/wire, compile, ABI, WASM worker and read model to presentation. No `Half`, binary16 codec or Half-pinned test remains. This resolves CAT-037 Q2.

Authority:
- AGENTS.md: f32, no legacy/compat/shim, refactor forward.
- docs/gpu-f32-physics.md: f32 Migration Status and #game-grade-envelope.
- epics Story 9A.44 and delivery note 5.
- Owner, 18 Oct: purge Half; refuse old saves visibly; never migrate.
- Owner, 21 Oct:
  - No format versions.
  - Native first, with Chrome after the catalogue. This supersedes the AC's serial Chrome run: those suites are queued, not claimed.

# Finite design decisions
- **D1 One atomic slice, no split.** Shared value types couple the lanes:
  - `AssistanceKnot` ⊃ `Metres`/`LinearSpeed`/`DurationSeconds`.
  - `MetreVector` spans collider, sensor, guide, COM and pose.
  - `DurationSeconds` spans sensor dwell, goal and knot blend.

  Any lane-family split needs a narrowing bridge or touches the same types twice, and both are forbidden. The only independent seam is the JS animation mirror (D7), which is folded in.
- **D2 Types.** These become `float`:
  - `Metres, Kilograms, Acceleration, InverseSeconds, Restitution, FrictionCoefficient, RollingResistance, LinearSpeed, DurationSeconds, MetreVector, AccelerationVector, PuzzlePrecision, OrientationThreshold`
  - every `EventPhase`/`StartPhase`

  Other changes:
  - `HalfBits` is deleted. Bit identity uses `SingleToUInt32Bits`, so −0 stays distinct.
  - `PhysicsDeclarationBounds` keeps its limits and checks them at exact f32 bounds.
  - `PrincipalInertia` loses the Half mantissa and int exponent, which were a Half range workaround, and becomes one f32 moment.
  - `PhysicalFrequency` is an integer and stays U32.
- **D3 ABI layout.**
  - Every offset is a named constant in `PhysicsGpuAbi`, `PhysicsMotionRead` or `PhysicsBodyWire`.
  - `WorkshopAbiLiteralScanTests` covers every record.
  - Bump `CurrentLayoutTag` 0x43544633 → 0x43544634, following the C-Gate relayout precedent (b951eef5: 2→3). This is an in-memory layout identity, not a save version.
  - The AC's "cache": the ABI has no cache record. The solver warm-start cache is managed f32 and contains no Half, and the scan covers it.
  - Layouts are proposed; the reviewer verifies there is no overlap, padding is zero and fields are 4-aligned.

  | Record | Before | After |
  |---|---|---|
  | Body | 160 | **160**: mass 88, drag 92, gravity 96, COM 108, principal frame 120, moments 136/140/144, collider 148, zero to 160 |
  | Collider | 96 | 96: pose translation 24 and rotation 36, radius 52, half-extents 56–68 |
  | Material | 32 | 32: restitution 8, bounce 12, friction 16, rolling 20 |
  | Sensor | 128 | 128: pose 24–52, min 52, max 64, speed 76, dwell 80, runtime ordinal plus f32 phases after 84 |
  | Guide | 64 | **80**: pose 16–44, min 44, max 56, maxAcc 68, supportH 72, margin 76 |
  | Trigger / orientation | 64 / 64 | unchanged size: phase f32 44 / cosine 32, phase 48 |
  | Work occurrence | 40 | 40: phase 24, approach 28, debit 32, effect 36 |
  | Motion piece | 144 | **160**: 3 f32 phases plus U32 frequency at 16–32, later lanes shifted, all vectors f32 |
  | PhysicsBodyWire | 80 | **96**: COM 48, velocity 60, angular 72. C# consumers only (`PhysicsPresentationRead`, `WorkshopTraceRecord`). worker.js copies the C#-composed f32 pose slot, so `WorkerAbiTests` doesn't change. |
  | Trace capture | 24 | 24: phase f32 at 16 |
- **D4 Save/wire.**
  - **Free saves:** the FlatBuffers instance, puzzle and goal fields are already `float`. Free saves are byte-identical and load as exact f32, with no migration or version.
  - **The one structural change:** `PuzzleRecord.raw_assistance:[uint8]` (198 B, 9 knots × 11 Half) becomes `assistance:[AssistanceKnotRecord]`, a struct of 11 floats with exactly 9 entries.
    - Regenerate `WorkshopWireGenerated.cs` and `tools/e2e/workshop-wire.ts` with flatc 25.2.10, per schemas/README.md.
    - Delete the `WorkshopPuzzleWire` byte codec.
  - **Refusal breadth:** every pre-9A.44 save with a puzzle record is refused atomically, with a visible typed refusal (`MissingField` when the count ≠ 9, or the verifier's `WireFault`). The bench is left unchanged. Free saves are never refused.
  - **Range:** values that Half rounding used to admit (precision `nextUp(1f)`, goal `nextUp(120f)`) become `OutOfRange`. Exact maxima are still admitted.
- **D5 Goldens.** Half-era baselines are never reinterpreted: JSON "0.04" read as f32 silently changes meaning.
  - **Outcome baseline:** at T0, before any edit, capture native Half-era outcomes as `Fixtures/outcomes-<baselineSHA>.json` with provenance (SHA, command, date). It records goal met, event order, goal tick and final COM, for every element fixture and every lesson at 3 precisions.
  - **`compiled-geometry-9a16.json`:** delete it. An analytic oracle replaces it: each compiled lane is bit-exactly its package f32 value, and each derived lane equals its f32 closed form.
  - **`lessons-484ec38.json`:** delete it. A regenerated `lessons-<baselineSHA>-f32.json` replaces it, produced by a recorded command.
  - **Delta table:** keep a one-time table as review evidence (old value, new value, |Δ| ≤ 2⁻¹¹·|new|), with no Half type in test code.
- **D6 Envelope consequences.** Restate these in gpu-f32-physics. The exact old values come from the T0 delta table.
  - Constants:
    - g: 9.81 (was 9.8125).
    - Ball radius: 0.34 (was 0.340088).
    - Drag: 0.04 (was 0.040009).
    - Precision: 0.45 (was 0.449951).
  - Inertia error: ≤ 6·2⁻²⁴, down from 6·2⁻¹¹ (10.1).
  - Phase resolution: 2⁻²⁴ relative, with the range [−2048, 2048) kept.
  - Upper capture bound: Y+margin with a single f32 rounding (c-ramp).
  - Orientation: the 4°–179° range that came from rounding becomes explicit degree bounds (Domino 45°).
  - Bundle gate: `(Half)package == runtime` becomes exact equality (9A.4).
  - Delete "stated against binary16" and the table rows.
- **D7 Animation mirror (stale defect).**
  - The Animation worker has written the f32 channel at 56 and the property at 136 since 83de446d (21 Oct).
  - `workshop-client.js` still reads `getUint16(56)` through `decodeFloat16`, with the property at 58.
  - Fix: read `getFloat32(56)` and `getUint16(136)`, and delete `decodeFloat16`.
  - e2e: change `HALF_ONE` 15360 to value === 1 in anim-1a/1b/1c and cat-014.
  - Fix the presentation-bindings table.
- **D8 Gate scope.**
  - In scope: code, schemas, tests, tools, content, elements and docs.
  - Excluded by path in the test:
    - `archives/`, `node_modules`, `_bmad-output/**`
    - historical docs: `docs/work-orders/**`, `docs/coverage/**`, `docs/committed-tick-presentation-*.json`, `docs/planning/delivered-slices.md`
    - campaign titles "Half the Battle" and "Half-life…" (OQ1)
  - Every other docs hit is edited in T13, so the gate and T13 agree by construction. At f5ffdb4e that means:
    - gpu-f32-physics, including "Half-precision (`f16`)" at :3/:349
    - presentation-bindings
    - element-package-isolation B6
    - about 60 `docs/planning/elements/*` "Binary16 debt" notes
    - vertical-delivery :140/:156
    - requirements CAT-038
    - radiation-research :32, if not a title
  - Rename `Workbench.RenderBox.Half` and `PlacementPose.Posed.Half` to `HalfExtent`, and reword comments ("Half the…", "Half-angle", "Half-full").

# Ordered tasks
- [x] T0 Freeze the baseline after 9A.23+25b merges, re-run both inventory scans, and capture the D5 outcome baseline.
- [x] T1 Write the PRIMARY tests and confirm they fail.
- [x] T2 Change the D2 types and bounds, plus `CaptureLatch`, the ContactWork/ContactTrigger/PassageSensor/OrientationSensor declarations, `WorkshopPuzzle` (`Mix` in f32), `PuzzleCatalogue`, `AuthoredInstanceTemplate`, `WorkshopGoalEvaluator` and `PuzzleDefinition`.
- [x] T3 Delete the narrowing in `WorkshopPhysicsCompiler`, `DeclaredPhysics`, `DeclaredCollider`, `ActivationNetwork.PassageSensorFor`, `WarmupScene`, `BrowserWorkshopClient.ContactWork` and `PhysicsBodyReadSet`.
- [x] T4 Make `RigidMassProperties` use f32 closed forms.
- [x] T5 Relayout the ABI (D3) with named offsets and a new tag, and extend the literal scan to every record.
- [x] T6 Worker: delete `ReadF16`/`WriteF16`/`ReadPrincipalInertia` and `SimBody.DragBits`, and read f32 through named offsets.
- [x] T7 Relayout `PhysicsMotionRead`, `PhysicsBodyWire` and `WorkshopTraceRecord`, together with their C# consumers.
- [x] T8 Save/wire (D4): schema, codegen, cast-free decode, exact-f32 `WorkshopSavePrevalidation` with its doc comment fixed, and the refusal text.
- [x] T9 UI: `ui/Workshop.cs`, `WorkshopGuidance.cs`, `ui/WorkshopPuzzle.cs`, `engine/Workbench.cs`, and the `PlacementPose` renames.
- [x] T10 D7 JS and e2e changes, type-checked only.
- [x] T11 Rewrite the 58 Half-pinned test files to f32.
  - Delete `PhysicalFrequencyIsExactlyRepresentableInTheHalfWireField` and the `PackageRuntimeParityTests` `Lane.Half` arm.
  - Update `OldLayoutSaves`.
- [x] T12 Deferred items:
  - C-Lamp F3: kill mutant R4 (`PassageSensorFor` drops `owner.Local`).
  - 10.1: f32 inertia bound.
  - Orientation degree bounds.
  - 9A.4: exact bundle equality.
- [x] T13 Edit the D8 docs, then README :5/:11 and the TODO brief.

# PRIMARY native tests (production C# simulation)
1. **Bit-exact f32 round trip per lane:** authoring → compile → ABI → worker → read model, for every D3 record lane and every knot lane. Include non-Half-representable inputs (0.34, 0.1, 9.81, 0.45f) and −0f.
2. **Behaviour identity against the D5 outcome baseline:**
   - Goal-met and event order are identical.
   - Goal tick, final COM and timings stay within the envelope, and every delta is explained.
3. **Every lesson solves** at precisions 0, 0.45f and 1, through tick progression and Reset/save/load.
4. **Old-save refusal at both boundaries:**
   - Refused: the legacy 198 B blob, and knot counts of 8 and 10.
   - Accepted with a bit-exact round trip: a 9-knot f32 save.
   - Unchanged: a Half-era Free save.
   - Precision 1 and goal 120 are admitted; `nextUp` of each is refused.
5. **Injected NaN/±∞** at the save, ABI decode and worker candidate is rejected, and the tick continues.
6. **Inventory scans (one test, with positive and negative controls):**
   - `rg -w "Half|HalfBits|Float16|float16|getF16|setF16|binary16"` returns 0 hits under D8.
   - A substring scan for `ReadF16|WriteF16|ReadHalf|WriteHalf|HalfToUInt16Bits|UInt16BitsToHalf|decodeFloat16|DragBits` returns 0 hits.
7. **Literal-offset scan** of every record, with positive and negative controls.

# Verification and publication
- **Build:** one matching build of the affected closure, reused with `--no-build --no-restore`.
- **CI tools:**
  - `dotnet build CuriousContraptions.slnx` reports 0 warnings, across both gate builds and the web build.
  - `dotnet test CuriousContraptions.tests`.
  - Coverage.Tests, BundleElements.Tests, WorkerAotGuard.Tests, and E2eRunner.Tests with the CI filter.
  - e2e `tsc`.
  - `dotnet publish CuriousContraptions.web -c Release`.
- **Process:**
  - Anvil runs before each write.
  - An independent implementer/reviewer pair works from an impact matrix.
- **Publication:** SnapshotApproval → commit, push, deploy → the reviewer verifies the deployed identity.
- **Browser:** marked pending post-catalogue; queued are anim-1a/1b/1c, cat-014, cat-005 and the save/load suites.

# Open questions (owner-only)
- OQ1: The AC's literal 0-hit `rg -w Half` would hit the campaign titles "Half the Battle" and "Half-life of the party" (requirements.md, campaign.md, campaign-reservations.md). **Default:** keep the titles and exclude them by path and line pattern (D8).

# Code Map — Half inventory at f5ffdb4e
Count: 746 lines matching `\bHalf\b|System.Half|binary16|float16` in 100 code, test and tool files (production C# is 209 lines in 37 files; tests are 518 in 58). Add 55 `ReadF16`/`WriteF16` calls in the worker. Over the whole tree the epics regex finds 1268 hits in 303 files, of which `_bmad-output` has 453 and docs 79 files.

| Family | Files | Lanes |
|---|---|---|
| Value types / bounds | engine/gpu/PhysicsDeclarations.cs, WorkshopConstruction.cs (`HalfBits`) | every declared scalar and vector |
| Mass | RigidMassProperties.cs, PhysicsBodyReadSet.cs | COM, inertia mantissa |
| ABI | PhysicsGpuAbi.cs, .Header.cs, .ContactWork.cs | body, collider, material, sensor, guide, trigger, occurrence, orientation |
| Worker | CuriousContraptions.Simulation/WorkshopPhysicsEngine.cs, WorkshopCollisionKernels.cs, WarmupScene.cs | F16 decode, motion pieces |
| Motion / read / trace | PhysicsMotionRead.cs, PhysicsBodyWire.cs, WorkshopTraceRecord.cs | piece phases, frequency, drag and forces; COM; capture phase |
| Compile | WorkshopPhysicsCompiler.cs, DeclaredPhysics.cs, DeclaredCollider.cs, ActivationNetwork.cs | narrowing casts |
| Events / sensors | CaptureLatch.cs, ContactWorkDeclaration.cs, ContactTriggerDeclaration.cs, PassageSensorDeclaration.cs, OrientationSensorDeclaration.cs, BrowserWorkshopClient.ContactWork.cs | phases, aperture, cosine |
| Puzzle / save | WorkshopPuzzle.cs, WorkshopPuzzleWire.cs, WorkshopWireFlatBuffers.cs, WorkshopSavePrevalidation.cs, AuthoredInstanceTemplate.cs, PuzzleCatalogue.cs, PuzzleDefinition.cs, WorkshopGoalEvaluator.cs, WorkshopCadence.cs (comment); schemas/workshop_wire.fbs `raw_assistance` | knots, precision, goal duration |
| UI | ui/Workshop.cs, WorkshopGuidance.cs, WorkshopPuzzle.cs; engine/Workbench.cs, PlacementPose.cs (identifier) | placement height and depth, bench box |
| JS / TS | wwwroot/workshop-client.js; tools/e2e anim-1a/1b/1c, cat-014, cat-005, workshop-wire.ts (generated) | stale animation mirror |
| Fixtures | CuriousContraptions.tests/Fixtures/compiled-geometry-9a16.json, lessons-484ec38.json | Half-era goldens |
| Docs | D8 list. The gpu-f32 table is stale: `engine/BallMaterialResource.cs` is gone and `engine/presentation` has no Half | current-state text |

# Coordinator decisions (24 October 2026, under owner delegation)

1. **Single slice:** accepted. A split would need a temporary bridge, which AGENTS.md forbids.
2. **Zero-hit scan targets binary16 identifiers, not English words:**
   - covered: `\bHalf\b` used as a type or member in code (`System.Half`, `(Half)`, `Half.`, `ReadF16`/`WriteF16`, `decodeFloat16`/`encodeFloat16`, `float16` in schemas, `HalfBits`);
   - out of scope: the campaign titles "Half the Battle" and "Half-life of the party", which are content.
   The scan is a code-aware regex test over `*.cs`, `*.ts`, `*.js`, `*.mjs` and `*.fbs`, plus the worker sources, and excludes `content/` strings and `_bmad-output`. A positive control proves a planted `Half` field is caught.
3. **The stale `decodeFloat16` in workshop-client.js** is a live defect: the Animation worker has written f32 since 83de446d. Fix it in this slice with a native/JS harness test showing correct pose decode.

# Owner directive and scope expansion (24 October 2026)

The owner says: "we should be using f32 everywhere, one of the key upgrade requirements is to transition to f32 as per the design documentation" (docs/gpu-f32-physics.md; AGENTS.md: f32 across authoring, solver and presentation; integers and declared external adapters keep their correct types).

The scope now covers BOTH:
- **binary16:** every `Half` lane. At main e344b19c this is 262 production lines in 37 files: WorkshopPhysicsEngine, PhysicsDeclarations, WorkshopPhysicsCompiler, RigidMassProperties, PhysicsGpuAbi, WorkshopConstruction, PhysicsMotionRead, WorkshopPuzzle, CaptureLatch, OrientationSensorDeclaration and others.
- **`double` game values:** 230 production lines in 32 files. The largest are AnimationBatch, PhysicsMotionRead, WorkshopClockDerivation, AnimationImpulseDefinition, WorkshopConstruction, AnimationOscillationDefinition, AnimationValues and WorkshopPoseHistory. The JS side has Float64Array in clock-ring.js and workshop-client.js.

Decisions:
1. Every game value in authoring, solver, worker, compiled rows, wire/save, committed reads, the event ring, presentation/animation and the client is f32.
2. `double` is allowed only at **named external adapters**: browser/host clock timestamps (`performance.now()`, the Godot frame clock) up to the point of conversion into the f32 or integer-tick game clock; and the vendored FlatBuffers library. These adapters are listed in ONE place, an allowlist with a reason per entry, and converted to f32 or integer ticks at the boundary.
3. **Game time:** use integer ticks plus f32 fractions, not double seconds. The presentation clock carries an integer frame/tick count plus an f32 phase. Long runs must not lose precision, and an integration test at 1 hour of simulated time proves no drift.
4. **Architecture guard (named Guard…):**
   - zero `Half`/binary16 anywhere in production code, tests or schemas (except the campaign titles, per the earlier decision);
   - `double`/`Float64Array` only in the allowlisted adapters.

   A mutation proves the guard catches a new `double` field in a game type.
5. If one slice is too big, split it by layer — **A: solver/compiled/wire/save Half purge; B: presentation/animation/client double→f32 + clock** — with the guard landing in A for Half and in B for double. The rule against a temporary bridge still applies within each split.
6. **Tests:**
   - behavioural tests per docs/testing-guidance.md;
   - all lessons solve at 3 precisions;
   - Reset and Save/Load per the integration guidance;
   - old saves refused with typed reasons at both boundaries;
   - no hash pins.

# Current state (24 October 2026, implementer)

- **Layer A (D1–D8): implemented, not committed.** T0–T13 done; awaiting independent review, SnapshotApproval and publication.
- **Layer B (decision 5: `double` game values, integer-tick clock, `double` guard, 1-hour drift test): not started.**
- PRIMARY 1–7: `F32LaneRoundTripTests`, `F32BehaviourIdentityTests`, `LaunchLessonSolveTests` (save/load replay), `F32SaveBoundaryTests`, NaN/±∞ theories, `HalfPrecisionGuardTests`, `WorkshopAbiLiteralScanTests.RecordFields` (every world record).
- Review fixes P1–P18 applied (24 Oct, implementer); affected classes pass on a Debug solution build (0 warnings). Full verification is the coordinator's.
- Evidence (evidence-9a-44/): t1 deltas and notes (regenerated after the content restore: outcomes unchanged); the t3–t5 logs predate the review fixes.
- Browser: pending post-catalogue.

## Review Triage Log

### 2026-10-24 — Review pass (layers: intent-alignment, edge-case 10, blind 18, verification-gap 4+2)
- verdicts: grouped into 21 entries — medium 12, low 6, reject 1, defer 1, accept 1. No false or maybe-false.
- **patch (medium):**
  - P1: the cat-062b binary16 decoder survives, and the guard misses getFloat16/setFloat16/Float16Array/Int16BitsToHalf/HalfToInt16Bits/encodeFloat16/WGSL f16.
  - P2: the guard over-reaches into .json/.md prose and its exemptions are too broad. Revert the prose edits (CAT-038, radiation research, CAT-008) and scan code and schemas only.
  - P3: the NaN injection test is vacuous on immutable lanes. Add a finite control, decode-path NaN, a check that the tick continues, and knot-lane NaN.
  - P4: the prismatic rest bound was loosened. Restore |v|<1e-2 and fix the limit-cycle root cause.
  - P5: the authored content literals changed. Restore 2.324/0.12/0.08 and fix the difficulty blocks.
  - P6: legacy lesson saves get VerifierRejected. Use a new assistance field id (deprecate raw_assistance) so the typed explanatory refusal fires; test that the bench is unchanged after the refusal.
  - P7: the JS animation decode offsets are unpinned. Pin them to the C# constants.
  - P8: the behaviour-identity test hard-asserts GoalMet and strips event timings, and the docs and test bounds disagree.
  - P9: a new test enshrines the GoalDuration prevalidation defect.
  - P10: the exact ===1 e2e expectations are unproven. Add a native test that curve endpoints clamp to 1f.
  - P11: knot interpolation at .2/.7 is untested.
  - P12: guide support height/margin decode is untested.
- **patch (low):**
  - P13: literal offsets remain; extend the scan.
  - P14: padding offsets are unchecked.
  - P15: the lane round-trip coverage claims more than it covers.
  - P16: golden fixture provenance and naming.
  - P17: stale README and vertical-delivery offsets, dead code, redundant casts, a misplaced summary, the TraceRecord brace.
  - P18: the C-Lamp F3 fix is checked at declaration level only; add a simulation case.
- **reject:** the legacy deletions (ShaderPreamble, Initialize()) are not listed in the spec. The fix would be a spec edit, so name them in the commit message instead.
- **defer:** the run-reset-snapshot.ts mirror parity test is browser-deferred by owner policy. Queue it for the browser phase.
- **accept:** this candidate delivers Part A only, which the spec permits. Part B is the next iteration, 9A.44B.

### 2026-10-24 — Review pass (follow-up on e06ef6e5)
- verdicts: 31 findings — high 1, medium 8, low 11, false 0, maybe-false 0, plus 11 carried, accepted or rejected rows.
- findings:
  - Blind Hunter:
    - `[high]` `patch` The new `Assert.Equal(bytes, WorkshopSaveCodec.Encode(reloaded))` in `LaunchLessonSolveTests.cs:152` asserts save byte-identity, which the AGENTS hard rule forbids. Fix: delete it; the `saved == reloaded` check and the replay cover the behaviour.
    - `[medium]` `patch` The P5 restore went the wrong way. Baseline `content/puzzles.json` has `capture_speed` 2.325 in all 460 blocks, as do CAT-004 and EL-192. 2.324 is the half-precision artefact 2.32421875, and the `"Value": 2.324` entries come from 2f5650b6 generated content. Fix: set every capture speed to 2.325, keep 0.08/0.12, and set the doc line to "2.325 (was 2.32421875)".
    - `[medium]` `patch` The `SuppliedDriveForce` |d|/dt cap has no direct test, and `docs/gpu-f32-physics.md:292` still states the old drive law. Grouped with Verification Gap 1. Fix: add a hold-window assertion and update the law in the doc.
    - `[low]` `patch` The Animation Output and Control record offsets are still literals in `Program.cs` and `WorkshopHint.cs`, and the scan skips both files. Fix: name the offsets and add both files to `RecordFiles`.
    - `[low]` `patch` The literal scan accepts any `NamedOffset + digit`. Fix: allow only `+ 4`, `+ 8` or `sizeof(float)` steps.
    - `[low]` `patch` The [−2048, 2048) phase rule is hand-written in five places, and `PhysicalPhase` lives in `CaptureLatch.cs`. Fix: use `PhysicalPhase.IsAdmitted` everywhere and move it to its own file.
    - `[medium]` `patch` The guard misses `ToHalf`, `Math.f16round`, `float16_t`, `min16float`, `vec3h`/`vec4h`, the mask-first decoder `(bits & 0x7C00) >> 10`, and the `.html`/`.mts`/`.cts`/`.cjs`/`.tsx`/`.tscn`/`.tres` extensions. Grouped with Edge Case 1 and 2. Fix: add each pattern and extension with a positive control.
    - `[low]` `patch` `BodyPaddingOffset` 152–160 and the collider/sensor 20–24 gaps are unchecked. Fix: check the zero padding of the admitted world at admission.
    - `[low]` `patch` In the NaN test, `Assert.Equal(committed, admitted)` is trivially true. Fix: delete it. The refusal-then-advance check stays as is, because `ValidateCandidate` is the production gate.
    - `[medium]` `patch` The `lessons-f32.json` DeepEquals golden pins the slice's own compile output. That is an output pin the hard rule forbids, and this slice regenerated it. First routed `defer` because the pattern pre-existed; re-routed to `patch` after the owner asked on 24 Oct why byte-identity checks exist. Fix: delete the golden, and assert any behaviour it uniquely covered explicitly.
    - `[medium]` `patch` The analytic oracle dropped the pipe-length (1/3.6/8 m) and prismatic rows that the deleted golden covered, and `.Single(collider)` throws on compound bodies. Grouped with Edge Case 3. Fix: extend the closed-form checks to those rows.
    - `[low]` `patch` The knot-interpolation theory never asserts `NotEqual(y)` and covers only four lanes. Fix: assert strictly between both ends and add the remaining Receiver lanes.
    - `[medium]` `patch` Prevalidation admits finite knots that domain construction refuses, which breaks the "admits exactly what construction admits" contract. The `MissingField` text also blames "saved before f32" for a current save with the wrong count. Fix: prevalidation applies the domain's knot rule, and the message is chosen by cause.
    - `[low]` `patch` The behaviour-identity tolerances (12 ticks, 2 mm) were fitted to observed results. Grouped with Intent Alignment (g). Fix: derive the bound from the D6 input deltas in the test and doc, and tighten if the derivation allows.
    - `[low]` `patch` About 40 redundant casts remain, e.g. `(float)0.7f`, `(float)(-.5)`, `var h = (.085f)`. Fix: remove them.
    - `[low]` `patch` Hand-built test fixtures still use literal motion and body offsets (`OrientationSensorTests`, `PrismaticConstraintTests`, `WorkshopWireTests`). Fix: use the named offsets.
  - Verification Gap:
    - `[medium]` `patch` The drive-law fix is only proven when the final tick lands on a peak: the mutant passes in 144 of 240 phases. Fix: assert max substep |v| < 1e-3 over the last 240 ticks.
    - `[medium]` `patch` `HalfPrecisionGuardTests` walks the file system, so from the main checkout it scans `.claude/worktrees/*` (excluded only through `.git/info/exclude`) and fails. Fix: list sources with `git ls-files --cached --others --exclude-standard`, like the other scans.
  - Edge Case Hunter:
    - `[medium]` `patch` Guard misses `ToHalf`, `float16_t` and `half`. Grouped with Blind 7.
    - `[medium]` `patch` Guard misses the `.mts`/`.cjs`/`.tsx`/`.html` extensions. Grouped with Blind 7.
    - `[medium]` `patch` Deleted golden coverage for pipe lengths and prismatic rows. Grouped with Blind 11.
    - `[low]` `patch` `PhysicsPresentationRead.cs:97-98` reads `R32(record, 8/12)` and the scan does not see that file. Fix: name the offsets and add the file to `RecordFiles`.
    - `[low]` `reject` `PhysicsMotionRead.Sample` still computes in double. The intent text assigns `double` game values to Part B (9A.44B).
  - Intent Alignment:
    - `accept` (carried) Part A only.
    - `carried` CLR stand-in instead of WASM; the Release worker build is CI's.
    - `carried` (P6) Refusal proved at the codec and UI failure report, not the Godot Load UI. The browser path is deferred.
    - `carried` (P7) Synthetic Animation buffer plus pinned offsets.
    - `carried` (P2, coordinator decision 2) Guard scope covers identifiers in code, not prose (R4 over R3).
    - `reject` R6(a): the layout tag and the deprecated `raw_assistance` slot as "format versions". D3 names the tag as an in-memory layout identity. The deprecated slot is the refusal mechanism: nothing reads it and nothing migrates.
    - `reject` R7(a): Half-era baselines as test inputs. D6 requires comparing against the f1ad3bd1 baseline, which is the reference, not a Half-pinned expectation.
    - `low` (g) Tolerance is not the envelope table. Grouped with Blind 14.
    - `accept` (carried) No authoring-package change; elements were already f32.
    - `carried` The AC's serial Chrome run is superseded by the owner.
    - `reject` (h) `double` presentation and clock are Part B.

## Auto Run Result

- **Summary:** Layer A of 9A.44. Every Half/binary16 game value, wire lane, decoder and save field is now canonical f32.
  - ABI is CTF4 with named offsets: guide 80 B, motion piece 160 B, body wire 96 B.
  - Save assistance moves to field 11 as nine typed `AssistanceKnotRecord`s. `raw_assistance` (slot 10) stays deprecated, so pre-9A.44 saves get the typed `MissingField` refusal with explanatory text.
  - A Half-precision guard covers code and schema files, with positive controls.
  - Layer B (double game values, integer-tick clock, double guard, drift test) is iteration 9A.44B.
- **Legacy deleted:** Half lanes and `decodeFloat16`, `WorkshopPuzzleWire.cs` (the binary16 puzzle wire), `ShaderPreamble`, `Initialize(string)`, and the Half-era fixtures `lessons-484ec38.json` and `compiled-geometry-9a16.json`.
- **Files changed** (232 files):
  - `engine/gpu/*`: ABI, records, wire, prevalidation, compiler and simulation moved to f32, with named offsets and padding checks.
  - `CuriousContraptions.Simulation/*`: solver and transport f32. `SuppliedDriveForce` caps the commanded speed at |d|/dt, which removes the hold chatter.
  - `CuriousContraptions.Animation.Worker/Program.cs`: named output offsets.
  - `schemas/workshop_wire.fbs` and the generated wire code (flatc 25.2.10).
  - `CuriousContraptions.web/wwwroot/workshop-client.js`: f32 reads with named `ANIMATION_OUTPUT_*` constants.
  - `ui/*`: f32 guidance and puzzle values.
  - `content/puzzles.json`: authored literals restored; difficulty blocks use the same literals.
  - Tests: new `F32BehaviourIdentityTests`, `F32LaneRoundTripTests`, `F32SaveBoundaryTests`, `HalfPrecisionGuardTests` and `TestPrecision`. About 60 existing test classes were updated to f32 expectations.
  - Fixtures: `lessons-f32.json`, `outcomes-f1ad3bd1.json`, and the two `save-f1ad3bd1-*.bin` inputs.
  - `tools/e2e/*.ts`: f32 expectations (browser-deferred, compiled with tsc only).
  - Docs: `gpu-f32-physics.md`, README, `vertical-delivery.md`, `general-engine-design.md`.
- **Review findings:**
  - Patched: 18 entries (P1–P18), 12 medium and 6 low, 0 high.
  - Deferred: one entry, the `run-reset-snapshot.ts` mirror parity check (browser-deferred until post-catalogue).
  - Rejected: one entry. The legacy deletions are not listed in the spec; this was rejected because it is spec bookkeeping, and the deletions are recorded above and in the commit message.
  - Accepted: Part A only (spec-sanctioned split).
- **followup_review_recommended: true.** This first pass patched 12 medium entries. Two named risks are unverified:
  - the new production drive-law change in `SuppliedDriveForce` (P4), which affects every prismatic drive near its target;
  - the save-schema field move to slot 11 (P6).
  - The 483 ast-grep removals of redundant `(float)` casts (P17) also went unvalidated by Anvil.
- **Verification (coordinator, 24 Oct):**
  - `dotnet build CuriousContraptions.slnx`: 0 warnings, 0 errors.
  - `dotnet test CuriousContraptions.slnx --no-build --no-restore`:
    - CuriousContraptions.tests: 3122 total, 1 failure, Skipped 0. The failure was `ElementIsolationTests.ProductionKnowledgeIsExactlyTheOwnedShrinkingList`, which scanned the ignored local `CuriousContraptions.web/AppBundle` publish output (the known deferred ratchet hazard). After moving that output aside, `ElementIsolationTests` passed 21/21, Skipped 0.
    - E2eRunner.Tests 208/208, BundleElements.Tests 191/191, Coverage.Tests 77/77, WorkerAotGuard.Tests 17/17, all Skipped 0.
  - `anvil_check` on 131 changed code files: 0 errors and 1 warning (GS-001 at `tools/e2e/workshop-driver.ts:985`), on a line this slice did not change.
- **Residual risks:**
  - The t3–t5 evidence logs predate the review fixes. This candidate's evidence is the test totals above, plus CI on the published SHA.
  - The isolation ratchet's AppBundle scan hazard remains deferred.
  - Browser qualification is pending post-catalogue.

### Follow-up pass (24 Oct, on e06ef6e5)
- **Patched:** 1 high, 8 medium, 11 low, all fixed by the same implementer:
  - The save byte-identity assert is deleted.
  - The `lessons-f32.json` golden is deleted and replaced by `EveryLessonAdmitsItsAuthoredPlacementsInventoryGoalAndKnotsAsF32`, which asserts placements, inventory, goal and knots against `content/puzzles.json`.
  - `capture_speed` is back to the authored 2.325 (532 values). The P5 instruction to use 2.324 was a coordinator error: that value was the half-precision artefact.
  - The drive hold-window assertion is added (every substep |v| < 1e-3 over 240 ticks) and the documented drive law updated.
  - The guard lists sources through git, with the new patterns and extensions.
  - The closed-form oracle now covers pipe lengths, slider rows and compound bodies.
  - `AssistanceProfile.HasAuthoredShape` is shared by the content reader and prevalidation; the refusal message is chosen by cause.
  - Animation, presentation and prismatic offsets are named and scanned, and the scan step rule is tightened.
  - `PhysicalPhase` has its own file and is the single phase rule.
  - Body, collider, material and sensor padding is checked in `ValidateCandidate`.
  - Interpolation is strictly between both knots, over five lanes.
  - About 330 redundant casts are removed, and test fixtures use named offsets.
- **Tolerances:**
  - The element pose bound is now derived (½·|Δg|·t² over 2 s plus the radius change = 5.1 mm; observed 1.12 mm).
  - The 12-tick event bound is a declared tolerance with its reason stated: contact sequences amplify the sub-tick first-order shift beyond what any input delta bounds.
- **Rejected:** `PhysicsMotionRead.Sample` doubles (Part B), the layout tag and deprecated slot as "format versions" (D3), and the Half-era baseline as a test input (D6).
- **Carried:** the remaining Intent rows.
- **Out of scope:** the save byte-identity asserts in `ContactWorkInstanceTests:102` and `SliderInstanceTests:145` existed at baseline. They are owned by the test-hygiene slice.
- **followup_review_recommended: true.** This follow-up pass patched a high. These production changes from this round are not yet reviewed by any layer:
  - per-tick padding validation in `ValidateCandidate`, which could refuse a valid worker candidate;
  - the `HasAuthoredShape` prevalidation rule, which could refuse a valid save;
  - the `PhysicalPhase` consolidation across timers, the network, the ABI and hints.
- **Verification (coordinator):**
  - `dotnet build CuriousContraptions.slnx`: 0 warnings, 0 errors.
  - `dotnet test CuriousContraptions.slnx --no-build --no-restore`: CuriousContraptions.tests 3142/3142, E2eRunner.Tests 208/208, BundleElements.Tests 191/191, Coverage.Tests 77/77, WorkerAotGuard.Tests 17/17. Skipped 0 in every project.
  - `anvil_check` on 54 changed code files: 0 findings.
  - `git diff --check`: clean.
