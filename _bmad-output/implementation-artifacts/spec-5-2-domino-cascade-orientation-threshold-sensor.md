---
title: 'Story 5.2: Domino Cascade Mechanics & Orientation-Threshold Sensor (CAT-023b)'
type: 'feature'
created: '2026-10-09'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: 'f7a8ef00d94d96b19b12f97e9a73e2e2fa343114'
context:
  - '{project-root}/_bmad-output/implementation-artifacts/spec-5-1-dynamic-box-rigid-body-upright-stability.md'
  - '{project-root}/AGENTS.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** A Domino can stand and topple (5.1) but cannot signal anything: there is no orientation sensor in the physics declaration, no `ActivationOut` port on the Domino, and no `domino_effect` level in the forward engine. Two 5.1 residuals remain in the Box–Box fallback (edge–edge contact point is an endpoint midpoint; clipped-point feature ids can duplicate), which matter for domino-on-domino strikes.

**Approach:** Add a declarative orientation-threshold sensor table (angle from the admitted initial pose ≥ 45°, emits once per world, rearms on Reset by fresh admission) evaluated in the worker at substep endpoints and surfaced as a new activation occurrence kind through the existing network, so a Domino → Lamp connection works with the UI and wiring that already exist. Add the `domino_effect` level as declaration data (locked Basketball, locked end Domino, locked Lamp, inventory four Dominoes; goal `ActivatedAfter(end → lamp, 0)`; the player wires end → lamp as in Wait for it). Fix the two residuals. Prove in Chrome that four dominoes cascade and light the lamp once, three do not, and a 10° tilt never emits.

## Boundaries & Constraints

**Always:**
- New table, not trigger-table reuse: `OrientationSensorDeclaration(Id, Body, Initial, threshold)` for a dynamic body with a collider; header count in the free header slot; sticky fired state that never regresses within a world; `ValidateCandidate` enforces it. Sensor id = the Domino's identity block + 6.
- Compile a sensor and `OrientationSource` node only for Dominoes that are a connection source; Domino declares `ActivationOut` only (activation input explicitly rejected); connection-aware capacity check throws the typed `WorkbenchFullException(ActivationNodes)`.
- Network: new `ActivationNodeKind.OrientationSource` and `ActivationOccurrenceKind.Orientation`; edge rules as ContactSource; Latch takes first occurrence; `ValidateOccurrence` extended; activation wire layout unchanged (kind byte only).
- Threshold is declared data (45° = cos half-angle), no element identifiers in the worker; one generic evaluation loop over the table.
- Level as data: `engine/gpu/DominoEffect.cs` mirroring `DelayedSignal.cs` (fixtures, inventory, Validate, WithPrecision); `WorkshopPuzzleId.DominoEffect`; UI picker row, title/task/hint text; fixture geometry such that four dominoes at the inventory spacing bridge ball → end Domino and three cannot.
- Exact Reset (sensor rearmed, lamp dark, dominoes upright bit-for-bit); Save/Load roundtrips Domino connections and the level; enums end-to-end; worker clamps/continues.
- Fix 5.1 residuals: `supportContact` uses closest points between the two support edges; clipped-point ids unique per (plane, source id) under 256.

**Never:**
- Change existing trigger/contact semantics, pose ring layout, or existing records' persisted fields; a new puzzle id and sensor table are additive.
- Bowling ball (CAT-014), offset centre of mass, nudging/difficulty assistance, campaign JSON import.
- Per-element branches in the worker or a second sensor path.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Four-domino chain | Player places 4 dominoes between fixtures, wires end → lamp, Run | Each domino's tilt passes 45° in sequence; lamp sample reaches 1; `CCGOAL_SOLVED` once; no second emission | N/A |
| Three-domino chain | One tile omitted | Chain breaks; end Domino stays < 45°; lamp never samples; goal unsolved | N/A |
| 10° tilt | Domino placed 10° off upright via Fine rotate (2 × 5°), Run | Settles without emitting; lamp dark; sensor not fired | N/A |
| Hovering near threshold | Tilt oscillates about 45° | Fires exactly once (sticky flag) | N/A |
| Reset after cascade | Reset | Sensors unfired, lamp neutral, poses restored exactly; Run cascades again | N/A |
| Domino as target | Player tries to connect into a Domino | No ActivationIn offered; validation rejects | Typed rejection |
| Too many sources | 9 wired Dominoes in free play | `WorkbenchFullException(ActivationNodes)` status message | Typed rejection |

</frozen-after-approval>

## Code Map

- `engine/gpu/ActivationNetwork.cs:8-11,72,91-104,120-193,212-284,315-334` — node kinds, latch, capacity 8, constructor rules, `Consume` (latched nodes ignore repeats `:147-153`, Latch takes first `:175-177`), `ValidateOccurrence`, compiler. `ActivationTimers.cs:6,38-58` — occurrence kinds, `ActivationCause`.
- Trigger pattern to mirror (not reuse): `ContactTriggerDeclaration.cs`; `PhysicsDeclarations.cs:161,239-250`; `WorkshopPhysicsCompiler.cs:111-112,133,141-145` (identity blocks of 64 per part; Domino uses first, first+1); `PhysicsGpuAbi.cs:116-122,232-266,336-348` (trigger record, `ReadTrigger`, candidate validation); header counts 100/104/108/112, **116..127 free**; worker offsets hard-coded at `worker.js:846,854,1068,1090,1099` (inserting a table before Motion shifts them and `tools/workshop-rigid-body.test.mjs:12` ByteLength).
- Worker evaluation points: `emitContactEvents :1064-1086` per substep at `:1269-1273`; residence sensor sticky phase `:1313-1412`; per-tick trigger clear `:852-857`.
- Host read: `CuriousContraptions.Simulation/WorkshopGpuDevice.cs:120-131` (`ReadTrigger` → `network.Consume` → `WorkshopRead.Activations`); `WorkshopActivationWire.cs` kind at byte 52.
- Ports/UI: `engine/gpu/WorkshopConnections.cs:18-25,41-48,71-88` (`For`, `LocalPosition` must gain `(Domino, ActivationOut)` or `ui/WorkshopConnections.cs:93` throws); `ui/WorkshopConnections.cs:32-70`; capacity `WorkbenchCapacity.cs:51-75` and `WorkshopConstruction.Validate :114-120`.
- Levels: `WorkshopPuzzle.cs:5,7,69-71` (**non-DelayedSignal ids fall into FirstPrinciples checks — add a branch**), `:93-137`; `DelayedSignal.cs` template; `WorkshopInventory.cs:61-65`; `WorkshopGoalEvaluator.cs:37-51`; `WorkshopPuzzleWire.cs`; picker/UI `ui/Workshop.cs:41,57-59,730-740`, `ui/WorkshopPuzzle.cs:12-95`. Legacy level `content/puzzles.json:1130`, `tools/Campaign/Program.cs:363` (inventory domino ×4; solution x = −2.4,−1.6,−0.8,0; a centred drop does not topple a box — ball must strike an upper corner, cf. cat-023a 0.4 m offset; place the end Domino beyond three-tile reach ≈ 4.05 m from the first).
- 5.1 residuals: `worker.js:456-467` (`supportContact` midpoint), `:568` (`4 + plane*16 + (p.id & 15)`), `featureBase :553`, `EDGE_FACE_ALIGNMENT :221`.
- E2E: driver `tools/e2e/workshop-driver.ts:108-118,122,223,338-344` (anchors `connections.connect.locked/delay`, `choice`; add `picker.domino_effect`, `dock.domino_effect`, `hint`, Fine rotate "Tilt" anchors (`ui/WorkshopGuidance.cs:57-64`, ±5° about Z), a `'domino'` connect panel); lamp lit = `readLastAnimationSample('5').valBits === 15360`, once via `readCaptured() === 1`; tilt helpers in `tools/e2e/cat-023a.test.ts:21-35`; `LOWER_TO_BENCH_PX`.
- Tests: `ContactActivationTests`, `ActivationTimerTests`, `WorkshopWireTests`, `WorkshopDominoTests`, `DelayedSignalTests` patterns; Node harness for the sensor (10° vs 45°, sticky once).
- Docs: `docs/engine-contracts.md` (tables, ABI byte length), `docs/simulation-presentation-bridge.md`, `docs/planning/requirements.md:592`, `vertical-delivery.md:135`, `docs/planning/delivered-slices.md`.

## Tasks & Acceptance

**Execution:**
- [x] `engine/gpu/{OrientationSensorDeclaration,PhysicsDeclarations,PhysicsGpuAbi,WorkshopPhysicsCompiler}.cs`, `WorkshopGpuDevice.cs`, `worker.js` -- sensor table, ABI, worker evaluation with sticky once, read path -- declarative sensor
- [x] `engine/gpu/{ActivationNetwork,ActivationTimers,WorkshopConnections,WorkbenchCapacity,WorkshopConstruction}.cs` -- OrientationSource node/occurrence, Domino ActivationOut + port position, source-only compilation, typed node capacity -- Domino signals
- [x] `engine/gpu/{DominoEffect,WorkshopPuzzle,WorkshopInventory}.cs`, `ui/Workshop.cs`, `ui/WorkshopPuzzle.cs` -- level data, puzzle id/validation branch, picker/title/task/hint -- domino_effect playable (`WorkshopInventory.cs` needed no change: `Authored(puzzle)` is generic)
- [x] `worker.js` -- edge–edge closest points; unique clip ids -- 5.1 residuals
- [x] Tests: C# (network kinds, sensor validation/ABI round-trip/regression, level validation, capacity), Node harness (10°/45°/sticky), fakes -- coverage
- [x] `tools/e2e/cat-023b.test.ts` (new) + driver anchors -- four-chain solve once, three-chain control, 10° control, Reset re-cascade, Save/Load -- story AC
- [x] Docs + `TODO.md` + sprint status -- contract and status

**Acceptance Criteria:**
- Given domino_effect with four placed dominoes wired end → lamp, when Run, then all topple in sequence, the lamp lights, and `CCGOAL_SOLVED` prints exactly once.
- Given three dominoes, when Run, then the lamp never lights.
- Given a 10° tilted Domino, when Run, then no orientation occurrence is emitted.
- Given Reset after a solve, then sensors are unfired and a second Run solves again; Save/reload/Load restores the wired construction and solves.
- All prior suites plus `cat-023b` pass 100% serially in Chrome; Node harness green; no element identifiers in the worker.

## Implementation Notes

- 2026-10-09 planning: Checkpoint 1 self-approved under the owner's standing autonomous instruction; no Open Questions. Decisions: 45° threshold from requirements CAT-023; new sensor table inserted before Motion (shift worker offsets + harness ByteLength); sensors/nodes only for connection-source Dominoes; level fixtures use a locked Basketball (bowling ball is CAT-014). Preview bundle on :8060 is free.
- 2026-10-09 implementation (Amelia). **Decisions.** (1) `OrientationSensorDeclaration(Id, Body, Initial, OrientationThreshold)`; threshold is the Half cosine of half the angle (`OrientationThreshold.Degrees(45)` = `WorkshopDomino.ActivationThreshold`), declared data read by the worker from the record. ABI: 16 × 64-byte records at `OrientationSensorsOffset` = 19744 (old MotionOffset), count in header slot 116 (`Header` now zero-checks 120..128), `MotionOffset` 20768, `ByteLength` 150832; record = id, body slot, initial quaternion, threshold (32 declaration bytes compared byte-exact) + fired/ordinal/phase + zero padding; `ReadOrientationSensor` resolves the body's own collider from body record byte 120. (2) The physical cause now carries one typed `GpuEventSourceId Source` (trigger id or sensor id from the one document identity space) instead of `GpuContactTriggerId Trigger`, and `ActivationLatch.ContactBody` is `Body`; wire layout unchanged (kind value 3 = `Orientation`). `ValidateOccurrence` resolves the source against `scene.Triggers` then `scene.OrientationSensors`. (3) `Consume(..., orientations)` is an optional trailing span; an Armed read against a latched node rejects ("regressed"), a changed ordinal rejects. (4) Node capacity: `WorkbenchCapacity.ValidateConnected(instances, connections)` from `WorkshopConstruction.Validate`, so an unwired Domino costs no node. (5) Level geometry from a Node harness experiment: a ball 0.4 m **left** of a tile tips it toward +x; four tiles at 1.0 m spacing reach an end tile 4.1 m from the first (end passes 45° at tick 321), omitting one breaks it. Fixtures: ball (−4.4, 3), end Domino (0.1, 0.09), lamp (3, 1); inventory 4; goal `ActivatedAfter(end → lamp, 0)`. (6) Residuals: `supportContact` builds each box's support edge (axis most perpendicular to n through its deepest vertex) and takes the midpoint of the segment–segment closest points; clipped vertex ids are `(4 << plane) + source id` (< 64, 255 stays the fallback). A/B in the harness: the old midpoint spun a box balanced on an edge crossing to 2.5 rad/s and sank it 0.19 m; the fix holds it (spin 0.006 rad/s, drift 0.2 mm). (7) Host hard-coded range: `PhysicsGpuAbi.ContactWork.cs` zero-checked work occurrences up to `MotionOffset`; now up to `OrientationSensorsOffset` (found by the new byte-flip sweep; without it every fired sensor would have rejected the candidate).
- **Files touched.** New: `engine/gpu/OrientationSensorDeclaration.cs`, `engine/gpu/DominoEffect.cs`, `CuriousContraptions.tests/OrientationSensorTests.cs`, `CuriousContraptions.tests/DominoEffectTests.cs`, `tools/e2e/cat-023b.test.ts`. Changed: `ActivationNetwork.cs`, `ActivationTimers.cs`, `PhysicsDeclarations.cs`, `PhysicsGpuAbi.cs`, `PhysicsGpuAbi.ContactWork.cs`, `WorkshopPhysicsCompiler.cs`, `WorkshopConnections.cs`, `WorkbenchCapacity.cs`, `WorkshopConstruction.cs`, `WorkshopDomino.cs`, `WorkshopPuzzle.cs`, `WorkshopActivationWire.cs`, `WorkshopTimerWire.cs`, `WorkshopWire.cs`, `CuriousContraptions.Simulation/WorkshopGpuDevice.cs`, `CuriousContraptions.Simulation/wwwroot/worker.js`, `ui/Workshop.cs`, `ui/WorkshopPuzzle.cs`, tests (`ContactActivationTests`, `ActivationTimerTests`, `WorkshopWireTests`, `WorkshopDominoTests`, test csproj), `tools/workshop-rigid-body.test.mjs`, `tools/e2e/workshop-driver.ts`, `tools/e2e/cat-023a.test.ts`, docs (`gpu-f16-physics.md` capability row, `general-engine-design.md`, `simulation-presentation-bridge.md`, `requirements.md` CAT-023 note), `deferred-work.md`, `sprint-status.yaml`, `delivered-slices.md`, `TODO.md`.
- **Surprises.** The picker popup rows sit at y ≈ 89/116/143/170 (the driver's older anchors land inside their rows); "The domino effect" is (600, 172). In the free workshop the Domino's "Connect ActivationOut" row pushes the part dock from y 656 to 697 (`setPartMode(mode, signalRows)`; cat-023a updated) and the "ActivationOut → ActivationIn" choice sits at (133, 644), not the authored (133, 232). Placement snaps to 0.1 m, so player tiles start 1 cm above the bench and their first published pose can already carry landing micro-rotation (~2e-5); bit-for-bit Reset/Load restoration is asserted on the locked end Domino (committed x = 0.100006, the Half remainder of 0.1) and tiles get 0.05°/2 mm tolerances. `docs/engine-contracts.md` has no ABI table/byte-length passage to update (code map stale); the capability table lives in `docs/gpu-f16-physics.md`. Fine rotate is reached by scrolling the Workshop menu to its end (Tilt −/+ at (1115, 413)/(1159, 413)); Escape closes it and deselects. domino_effect has no Ramp, so the deferred Box-on-Ramp Chrome proof stays deferred. Node harness rigid-body facts: 18 (5.1: 14) plus observation/pose-ring = 20.
- 2026-10-09 review fixes (Amelia, round 1): sensor `Initial` must equal the sensed body's admitted `Rotation` (scene validation, plus a byte check in `ValidateCandidate` at tick 0 against body record 40..48 — a read-time check against the live rotation was tried first and faulted the world on the first rotating tick, so the admission check plus the per-tick declaration-byte equality carries the invariant; tests: 90°-about-Y Domino compiles `sensor.Initial == domino.Rotation`, harness 90°-Y tile stands 2 s and never fires); late-fire case (tick-2 candidate firing at ordinal 3/4 over a tick-1 source rejects, 5 admits); Domino → Delay → Lamp fact (timer carries the Orientation-kind input, lamp latches `TimerElapsed` with the sensor as `Source`, `ValidateRead` and `WorkshopTimerWire` round-trip); `Consume(... orientations)` is now required and throws "Orientation sensor read is missing." when an `OrientationSource` node has no read (all 17 test callers pass `[]`); off-centre edge-crossing harness fact (box 0.7 m along the beam); `OrientationThreshold.Degrees` admits 4°–179° (cos 1.25° rounds to Half 1.0, so 2.5° was not enough); cat-023b orders the cascade by pose-ring sequence and states its test-1 save dependency; `tiltSelectedByFineRotate` tracks the toggle (reset on reload/level change); `domino.tres` description names the 45° output; vertical-delivery CAT-023a row, current-consumers CAT-023 entry and TODO "Next" aligned (offset COM → CAT-014; `parts/DominoPart.cs` is artwork only).
- **Evidence (implementer, not a review verdict).** `anvil_check` 0 warnings over all changed files; `dotnet test CuriousContraptions.slnx` 610/610 (612/612 after the review patch round); `node --experimental-vm-modules --test tools/workshop-rigid-body.test.mjs tools/workshop-observation.test.mjs tools/workshop-pose-ring.test.mjs` 20/20 (22/22 after the patch round); `dotnet publish CuriousContraptions.web` exit 0 (served on :8060); `node --test --test-concurrency=1 --test-timeout=150000 tools/e2e/cat-023b.test.ts` 4/4 (third run; runs 1–2 failed only on my assertion precision: Half-quantised x = 0.100006 and the lamp glow ramp needing a bounded wait); cumulative `tools/e2e/*.test.ts` 14 suites / 50 tests 100% serially in Chrome; `worker.js` sha256 4ecc1747c52e58ce…; harness worker-source grep for element names clean. Not committed or pushed.

## Spec Change Log

## Review Triage Log

Pass 1 (2026-10-09). Layers: blind-hunter (BH), edge-case (EC), verification-gap (VG, mutation-verified), Murdoch (M, probe-verified). Murdoch verdict: Pass scoped (anvil 0; 610/610; harness 20/20; publish exit 0; cat-023b 4/4; cumulative 14 suites / 50 tests; worker.js sha 4ecc1747…; "three cannot bridge" proven geometric: reach 1.159 m vs minimum 1.35 m gaps; 5.1 residuals closed P15 error 0 / P17 duplicates 0). Verified findings from other layers route to one patch round.

| # | Finding (layer) | Verdict | Route |
|---|---|---|---|
| 1 | Sensor Initial not tied to admitted body rotation; only compiler ties them; tested only at identity (VG, BH, EC) | medium | patch (validation + tests) |
| 2 | ValidateCandidate late-fire clause never the rejecting check (VG) | medium | patch (test) |
| 3 | Domino → Delay → Lamp chain untested; ValidateOccurrence guard unprotected (VG) | medium | patch (test) |
| 4 | Consume `orientations` optional; missing read silently never latches (BH, EC) | medium | patch |
| 5 | Edge–edge harness only at symmetric midpoint (VG) | medium | patch (off-centre case) |
| 6 | Degrees() validates after Half rounding (EC) | low | patch |
| 7 | cat-023b test 4 not self-contained; cascade order by Date.now (BH) | low | patch |
| 8 | Fine rotate toggle non-idempotent in driver (BH) | low | patch |
| 9 | domino.tres description lacks the restored output (BH) | low | patch |
| 10 | Doc drift: vertical-delivery CAT-023a target, current-consumers CAT-023, TODO Next (BH, M-F1) | low | patch |
| 11 | Host does not cross-check fired state against committed pose (BH) | low | defer (worker-trusted like contact triggers; recorded design decision) |
| 12 | Effective threshold 45.015° after Half quantisation (EC, M-Q3) | low | rejected (within game-grade envelope) |
| 13 | Precision slider is a no-op for DominoEffect (BH) | low | defer (UI polish; difficulty out of scope) |
| 14 | CAT-023 "striker must not hit second tile" unasserted (BH) | low | defer (campaign level tuning, Epic 15) |
| 15 | supportContact parallel edges resolve to an endpoint (M-F2) | low | defer (quality gate; finite, non-launching) |
| 16 | Node-cost rule duplicated in compiler and ValidateConnected (BH) | low | defer |
| 17 | Worker linear body search per sensor per substep; silent skip of non-dynamic sensed body (BH, EC) | low | defer (performance gate; host rejects at admission) |
| 18 | Clip-id uniqueness has no observing test (VG) | low | defer (needs exported manifold generator) |
| 19 | Three-tile even spacing not in e2e (BH) | false | Murdoch Q2 swept seven three-tile layouts incl. uniform 1.367 m: never fires |

Pass 2 (2026-10-09, Murdoch re-review after the patch round). Verdict: Pass scoped; SnapshotApproval for the Epic 5 local commit granted (no push). Node harness 22/22; anvil 0; 612/612; publish exit 0; cat-023b 4/4; cumulative 14 suites / 50 tests in one serial Chrome run; worker.js sha 4ecc1747… unchanged; probes P1–P18 and Q1–Q8 identical. Items #1–#10 verified. New: F1 stale counts (fixed at close); F2 informational (body-slot slice bound unreachable). Publication-dependent checks for a later push: remote commit identity and deployed `/simulation/worker.js` sha 4ecc1747….

## Verification

**Commands:**
- `node --experimental-vm-modules --test tools/workshop-rigid-body.test.mjs tools/workshop-observation.test.mjs tools/workshop-pose-ring.test.mjs` -- expected: all pass
- `anvil check --changed` -- expected: 0 warnings
- `dotnet test CuriousContraptions.slnx` -- expected: 100%
- `dotnet publish CuriousContraptions.web` -- expected: exit 0
- `node --test --test-concurrency=1 --test-timeout=150000 tools/e2e/cat-023b.test.ts` -- expected: 100% in Chrome
- `node --test --test-concurrency=1 --test-timeout=150000 tools/e2e/*.test.ts` -- expected: all suites 100%
