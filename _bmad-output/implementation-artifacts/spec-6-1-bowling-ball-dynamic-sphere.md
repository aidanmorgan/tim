---
title: 'Story 6.1: Bowling Ball Dynamic Sphere by Material Declaration (CAT-014)'
type: 'feature'
created: '2026-10-09'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: 'b0a0944374d3e109f52f24cdfe29df9d172ce1bf'
context:
  - '{project-root}/_bmad-output/implementation-artifacts/spec-5-2-domino-cascade-orientation-threshold-sensor.md'
  - '{project-root}/AGENTS.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Only one dynamic sphere exists. `WorkshopBall` hard-codes `Kind = Basketball`, `BasketballMaterial` admits only its default bits, ball friction/threshold are hard-coded in the compiler, and the legacy `parts/catalog/bowling.tres` uses an untyped parameter dictionary the registry rejects. A heavier, low-bounce sphere is required for mass-driven puzzles (CAT-014).

**Approach:** Generalise the ball to one record with a typed material per kind (`BallMaterial.For(kind)`): append `WorkshopPartKind.BowlingBall`, rewrite the catalog resource as typed bits, add the free-play row, and prove in Chrome that a Bowling Ball and a Basketball striking identical stock Domino targets in one Run produce different outcomes (Bowling topples, Basketball does not). No solver change; the worker already reads mass, inertia, radius and material from the record.

## Boundaries & Constraints

**Always:**
- Material declaration only: Bowling Ball = mass 4 kg, radius 0.38 m (owner decision 9 Oct 2026, delivered in Story 6.1b: now 0.28 m, smaller and heavier than the Basketball), bounce 0.14, drag 0.04, buoyancy 0 (game-scale values from the legacy catalog; the PRD's real-world 7.2 kg / 0.11 m is rejected as inconsistent with the 0.34 m Basketball scale — owner may override). Friction and bounce threshold become declared per-kind material data, not compiler constants.
- One `WorkshopBall` record with a `Kind` field validated against `BallMaterial.For(kind)`; `WorkshopInput.Basketball` kept as a thin wrapper; save wire reuses the existing ball slot layout (kind at 0, material at 104–113) with no version bump unless layout changes.
- Receiver sensors/guides, capture, WorkbenchCapacity (count `is WorkshopBall`), inventory (append row), export presets, icon by id; authored puzzles that need "the Basketball" require the Basketball kind explicitly.
- Visible art size equals collision radius for both balls (`parts/BallPart.cs` reads the declared material).
- Delete the legacy: untyped `bowling.tres` parameters, any Basketball-specific naming that blocks a second kind (`BasketballMaterial` → `BallMaterial`, resource and definition export renamed), hard-coded ball friction/threshold in the compiler.
- Exact Reset and Save/Load for both balls; enums end-to-end; no element identifiers in the worker.
- The e2e setup must be found by a Node harness sweep first (identical stock Dominoes; candidate setups: low-clearance corner drop with equal overlap, slow roll from a short Ramp, or a flush block of Dominoes) and must show a clear margin: Basketball target tilt < 20°, Bowling target tilt > 60°, both balls in one Run in separate lanes.

**Never:**
- Change Domino material/validation, table capacities, pose ring, or worker code (other than nothing); no bespoke solver branches.
- Implement the offset centre of mass (re-defer explicitly as a Domino item in docs), drag application, or campaign content.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Two lanes | Basketball and Bowling Ball released identically at matched Domino targets, Run | Bowling lane Domino tilt > 60°; Basketball lane < 20°; both balls rest without jitter | N/A |
| Rest/bounce | Bowling Ball dropped on bench | Low bounce (declared 0.14), rests at radius height | N/A |
| Receiver | Bowling Ball rolls into Receiver ≤ 1.5 m/s | Captured; halo animates (same declared path) | N/A |
| Save/Load | Construction with both kinds | Roundtrips kind and material bits; forged material bits rejected | Typed rejection |
| Capacity | 16 dynamic bodies | 17th ball of either kind rejected "Workbench is full: moving body table" | Typed |
| Authored puzzles | First principles / Wait for it / Domino effect | Still require their Basketball fixture; Bowling Ball not offered | N/A |

</frozen-after-approval>

## Code Map

- `engine/gpu/WorkshopConstruction.cs:39-65,127-135` — `BasketballMaterial` (Default 0.34/1/0.55/0.04/0, default-bits-only Validate), `WorkshopBall` (Kind hard-coded `:58`), `WorkshopInput.Basketball`. `engine/BasketballMaterialResource.cs:9-25`; `parts/catalog/ball.tres:8-24` (bits); `parts/BallPart.cs:10`; `parts/scenes/ball.tscn` shared; legacy `parts/catalog/bowling.tres` (untyped Parameters; mass 4, bounce .14, radius .38, drag .04).
- Compiler `engine/gpu/WorkshopPhysicsCompiler.cs:31-41` (body/material/sphere; friction .3 and threshold .1 hard-coded `:38`), `:66-78` Receiver sensor/guide per `OfType<WorkshopBall>()` — generic already.
- Add-a-part: `WorkshopPartKind.cs:3` (append `BowlingBall`); `PartDefinition.cs:12,19`; `PartRegistry.cs:14-18,48,60`; `MachinePart.cs:37-45`; `MachineWorld.Gpu.cs:43,73-79,93-108` (generic `CaptureBall(kind)`), `:237` presentation uses Basketball part only (harmless); `WorkshopInstances.cs:75`; `WorkshopInventory.cs:50-59` (append row; order is UI contract); `WorkbenchCapacity.cs:36,60` (count balls by `is WorkshopBall`), `:68-72` sensor rows per ball; `WorkshopWire.cs:307-310,373-374` (decode `Basketball or BowlingBall when Zero(slot[114..])`); `WorkshopConnections.cs:24`; `CuriousContraptions.csproj:36,45`; `export_presets.cfg:9,37,78,114` (add bowling.tres); `ui/Workshop.cs:289,344`; `ui/WorkshopIcons.cs:41` ("bowling" pictogram exists); `WorkshopPuzzle.cs:66-67` (tighten to Basketball kind); `DominoEffect.cs:50`, `DelayedSignal.cs:53` skip every ball (fine).
- Physics is data: worker reads mass `+64`, COM `+80`, frame `+88`, inertia `+96..112`, radius from collider (`worker.js:922-976,1003,1038`); `RigidMassProperties.cs:36-39` solid sphere 0.4·m·r²; bounds `PhysicsDeclarations.cs:56,66,91`. Element-name guard `tools/workshop-rigid-body.test.mjs:353`. Drag unread (deferred).
- Tests to update: `BasketballResourceTests.cs:14-28`, `WorkbenchCapacityTests.cs:13,37,69-73,89,144`, `WorkshopWireTests.cs:332`; harness builders `tools/workshop-rigid-body.test.mjs:193-196` (`domino()`, `basketball()`).
- E2E: `tools/e2e/cat-023a.test.ts:12-75,107-140` placement/tilt pattern; driver `tools/e2e/workshop-driver.ts:64-73,121-130,237` — a tenth free-play row moves the Domino connect anchor (y 644) and free_workshop choice anchor down one row (~47–51 px): update and re-run cat-023b; palette scroll area `ui/Workshop.cs:314-325` may clip.
- Docs: `requirements.md:538,592` (re-defer offset COM as a Domino item), `vertical-delivery.md:134,136`, `current-consumers.md:131-139`, `delivered-slices.md`, `TODO.md`, `README.md:5`, `DESIGN.md:166` colour #45639c.

## Tasks & Acceptance

**Execution:**
- [x] `engine/gpu/WorkshopConstruction.cs`, `WorkshopPartKind.cs`, `WorkshopWire.cs`, `WorkbenchCapacity.cs`, `WorkshopInventory.cs`, `WorkshopPuzzle.cs`, `WorkshopPhysicsCompiler.cs` -- `BallMaterial.For(kind)` with declared friction/threshold, `WorkshopBall.Kind`, decode both kinds, footprint/count, row, Basketball-kind puzzle checks -- ball generalised as data
- [x] `engine/BallMaterialResource.cs` (rename), `PartDefinition.cs`, `PartRegistry.cs`, `MachinePart.cs`, `MachineWorld.Gpu.cs`, `parts/BallPart.cs`, `parts/catalog/{ball,bowling}.tres`, csproj, export presets -- typed bowling catalog, art size from material -- new element wired
- [x] `tools/workshop-rigid-body.test.mjs` -- Bowling vs Basketball momentum transfer; sweep to pick the e2e setup with the stated margins -- physics proof as data
- [x] `CuriousContraptions.tests/WorkshopBowlingTests.cs` (new) + updated tests -- material validation, save roundtrip, capacity, Receiver capture, puzzle fixture kind -- coverage
- [x] `tools/e2e/cat-014.test.ts` (new), driver anchors (+ cat-023b anchor update) -- two-lane proof, rest/bounce, Reset, Save/Load -- story AC
- [x] Docs + `TODO.md` + sprint status -- contract, re-deferral, status

**Acceptance Criteria:**
- Given both balls released identically at identical stock Domino targets in one Run, when struck, then the Bowling lane tilts > 60° and the Basketball lane stays < 20°.
- Given a Bowling Ball dropped on the bench, then it bounces low and rests; given it rolls into a Receiver slowly, then it is captured.
- Given Save/reload/Load, then both kinds restore with exact material bits; Reset restores poses exactly.
- All prior suites plus `cat-014` pass 100% serially in Chrome; harness green; grep of worker.js finds no element identifiers or ball constants.

## Implementation Notes

- 2026-10-09 planning: Checkpoint 1 self-approved under the owner's standing autonomous instruction. Decisions: game-scale Bowling values (4 kg / 0.38 m / 0.14) over PRD real-world values; offset centre of mass re-deferred explicitly as a Domino (CAT-023) item, not CAT-014; e2e setup chosen by harness sweep. Preview bundle on :8060 is free.
- 2026-10-09 implementation (Amelia). **Decisions.** (1) One `BallMaterial(Radius, Mass, Bounce, Drag, Buoyancy, Friction, BounceThreshold)` with `For(kind)` and `Validate(kind)`; friction 0.3 and threshold 0.1 m/s are now declared per kind and the compiler reads them (`WorkshopPhysicsCompiler` constants deleted). `WorkshopBall(Id, Kind, Cell, Local, Rotation, Material, Locked)`; `WorkshopInput.Ball(kind, …)` is the general entry and `Basketball(…)` the thin wrapper. (2) Save/command wire keeps the ball slot layout (kind at 0, five material Halfs at 104–113, zero padding from 114; no version bump): decode admits `Basketball or BowlingBall`, reconstructs `BallMaterial.For(kind) with { five persisted fields }` and validation rejects forged bits or a swapped kind tag; friction/threshold are kind data, not persisted fields. (3) `BallMaterialResource` (seven bit fields, `Capture(kind)`) replaces `BasketballMaterialResource`; `PartDefinition.Ball` replaces `.Basketball`; `"bowling"` maps to `WorkshopPartKind.BowlingBall` (enum appended last); `PartRegistry.LoadBall(path, kind)` loads both catalogs; `MachineWorld.CaptureBall(kind, …)` replaces `CaptureBasketball`/`BasketballDefinition`; `BallPart` sizes art from the declared radius of its own kind. Bits: Basketball 13681/15360/14438/10527/0, Bowling 13844/17408/12411/10527/0, friction 13517, threshold 11878. (4) `WorkbenchFootprint.Of(BowlingBall)` = one dynamic body; capacity counts `is WorkshopBall`; free-play row appended last (`Free.Keys.Last() == BowlingBall`); `WorkshopPuzzle.Validate` rejects any non-Basketball ball in an authored puzzle with a typed message; `WorkshopPorts.For(BowlingBall)` is empty. (5) Node harness sweep before the e2e (scratch `sweep.mjs` over corner drops at equal overlap, equal UI offsets, slow rolls, two-lane worlds and bench drops). Chosen geometry: identical stock Dominoes 1 cm above the bench at x = 0, lanes z = −1.5 (Basketball) and +1.5 (Bowling); both balls placed at x = 0.3 (placement snaps to 0.1 m, so the UI offset is equal; corner overlap 0.165 m vs 0.205 m) and lowered by the same move-gizmo drag (−113 px ≈ 1.95 m) to centre ≈ 1.05 m. Margins from the sweep: Basketball lane max tilt 4.5–4.9° (< 20° by ≥ 15°) and Bowling lane 90° for centres 0.95–1.15 m (isolated non-monotonic exceptions at 1.2 m where the Bowling ball only rocks the tile 9° and at 1.4 m where the Basketball topples it, both outside the chosen band); at equal overlap 0.2 m the separation holds at every clearance 0.05–1.0 m; lanes never interact (z stays ±1.50). Rolling strikes separate too (0.7 m/s: Bowling 90°, Basketball 4.2°; 0.5 m/s: 56.9° vs 1.7°) but need a Ramp in the UI, so the drop was chosen. A corner drop from the placement plane is not usable (both balls topple at x = 0.4; at x = 0.5 the Basketball misses entirely). (6) Screen mapping derived from the free-workshop camera (azimuth 0.6, elevation 0.48, orthographic 65.2 px/m): (x, 3, z) → (720 + 53.8x − 36.8z, 485 + 17.0x + 24.8z), vertical 57.9 px/m; confirmed by a calibration placement reading (0.300, ·, 1.500) from the pose ring. (7) Offset centre of mass re-deferred explicitly as a Domino (CAT-023) item (requirements CAT-014/CAT-023 notes, vertical-delivery, current-consumers, deferred-work); declared drag still unapplied, so the two-lane e2e asserts vertical rest at radius height and lane containment, not a stop.
- **Files touched.** New: `engine/BallMaterialResource.cs`, `CuriousContraptions.tests/WorkshopBowlingTests.cs`, `tools/e2e/cat-014.test.ts`. Deleted: `engine/BasketballMaterialResource.cs` (`git rm`). Changed: `engine/gpu/{WorkshopConstruction,WorkshopPartKind,WorkshopWire,WorkbenchCapacity,WorkshopInventory,WorkshopPuzzle,WorkshopPhysicsCompiler,WorkshopConnections}.cs`, `engine/{PartDefinition,PartRegistry,MachinePart,MachineWorld.Gpu}.cs`, `parts/BallPart.cs`, `parts/catalog/{ball,bowling}.tres`, `CuriousContraptions.csproj`, `export_presets.cfg` (bowling.tres in all four presets), `ui/Workshop.cs` (friction toggle status text), tests (`BasketballResourceTests`, `WorkbenchCapacityTests`, test csproj), `tools/workshop-rigid-body.test.mjs` (bowling builder, three facts, `bowling` added to the element-name guard), `tools/e2e/workshop-driver.ts` (palette `bowling` row y 612; free-workshop dock y 656 → 707; Domino connect row and free-workshop choice y 644 → 695; `selectTool('bowling')`), docs (`requirements.md` CAT-014/CAT-023 notes, `vertical-delivery.md` CAT-023a/CAT-014 rows, `current-consumers.md` CAT-014-I/CAT-023 capability, `delivered-slices.md`, `README.md`), `deferred-work.md`, `TODO.md`, `sprint-status.yaml` (6-1 → review). Worker untouched (sha256 4ecc1747c52e58ce…).
- **Surprises.** With ≈ 3 cm of clearance the Bowling ball strikes its tile ≈ 80 ms after Run, before the first pose-ring read, so a bit-for-bit "admitted pose" check at the first read is impossible in the two-lane construction (first attempt failed on tile B at 0.39°/0.52°); the e2e now admits the started strike at first reads (tilt < 3°, lane position, release height ≤ band top via py + vy²/2g, lower bound only for an unstruck lane) and proves exact Reset (identity rotation, exact x/z for both kinds) in the 3 m drop test instead. The ten-row palette fits without clipping; the Bowling row sits at y ≈ 612 and the dock moved 51 px. anvil's patch applier rejects hand-written multi-hunk diffs on context mismatch; full-content validation was used for the larger rewrites. `WorkshopWireTests.OnlyBasketball` helper kept its name (it selects the construction's one Basketball; not a blocker for a second kind). `vertical-delivery` named "Bowling-keyed branches in ui/WorkshopIcons.cs" as legacy; there is none — the `bowling` pictogram is content keyed by resource id, which the row now says.
- **Evidence (implementer, not a review verdict).** `anvil_check` 0 warnings over all changed source, test, catalog, preset and e2e files; `dotnet test CuriousContraptions.slnx` 619/619 (first run 618/619: my test's save allocator `new(3)` had to exceed the four ids → `new(5)`); `node --experimental-vm-modules --test tools/workshop-rigid-body.test.mjs tools/workshop-observation.test.mjs tools/workshop-pose-ring.test.mjs` 25/25; `dotnet publish CuriousContraptions.web` exit 0 (served on :8060); `node --test --test-concurrency=1 --test-timeout=150000 tools/e2e/cat-014.test.ts` 4/4 (second run; run 1 failed 2/4 on the first-read pose assumption above); cumulative `node --test --test-concurrency=1 --test-timeout=150000 tools/e2e/*.test.ts` 15 suites / 54 tests 100% in one serial Chrome run (711 s); `grep -ci bowling worker.js` = 0 and no `0.38`/`0.14` constants; screenshots `palette.png`, `dock-domino.png`, `dock-ball.png` in the session scratchpad. Not committed or pushed.
- 2026-10-09 review round 1 fixes (Amelia; material values unchanged pending the owner decision). Harness: mass-isolation controls on the 0.7 m/s rolling strike (4 kg at the Basketball radius 90° > 60°, 1 kg at the Bowling radius 4.4° < 20°), drop rebound apex within ±15% of e²·h for both kinds (0.059 vs 0.060 m, 0.936 vs 0.944 m), `0\.38|0\.14` added to the worker constant guard. C#: capacity rows (Sensors, Receiver 2, BowlingBall 9) and (Guides, Receiver 2, BowlingBall 1); `OnlyBasketball` → `OnlyBall`; presentation telemetry follows the lowest-id `WorkshopBall` of either kind (the `Part(kind)` helper deleted); `CaptureBall` throws a typed `ArgumentException` for a non-ball kind before the registry lookup; `bowling.tres` description made factual. E2E: test 2 proves exact Reset by replaying the first Run's free-fall states (py, velocity, position, rotation equal bit-for-bit at ≥ 3 coinciding committed states per ball; the pose ring carries no tick, so py identifies the tick); test 4 builds and saves its own construction; header documents that the two-lane Chrome outcome uses mass and radius together and that the mass-only proof is the harness; driver `selectTool('bowling')` throws outside free play. Docs: requirements CAT-014 note and README marked pending independent review, README "WGSL f16" wording replaced with the canonical f32 / WASM worker wording, TODO "Current result" trimmed to CAT-014 plus links, three deferred-work entries with destinations (ENGINE-DRAG, CAT-034 Impact lever slice, CAT-023c) and a roadmap note in vertical-delivery for ENGINE-DRAG and CAT-023c. Evidence: Node harness 25/25; `anvil check --changed` 0 (3 script files) plus `anvil_check` 0 over the edited C#/catalog files; `dotnet test CuriousContraptions.slnx` 621/621; `dotnet publish CuriousContraptions.web` exit 0; `tools/e2e/cat-014.test.ts` 4/4. Cumulative suites not re-run (reviewer's step).

## Spec Change Log

## Review Triage Log

Pass 1 (2026-10-09; first launch lost to a usage-credit stop, relaunched on the unchanged candidate). Layers: blind-hunter (BH), edge-case (EC), verification-gap (VG), Murdoch (M). Murdoch: scoped Pass (anvil 0; 619/619; harness 25/25; publish exit 0; cat-014 4/4; cumulative 15 suites / 54 tests; worker sha 4ecc1747… untouched; 448/448 single-bit forgeries of the Bowling slot rejected).

| # | Finding (layer) | Verdict | Route |
|---|---|---|---|
| 1 | Chrome two-lane outcome is mass + radius, not mass alone; no mass-isolation control (M-F1) | medium | patch (harness controls) |
| 2 | Receiver guide/sensor capacity never tested with Bowling-only balls (VG) | medium | patch |
| 3 | Exact Reset compares x/z/rotation only; py/velocity unchecked; test 4 depends on test 1 save (BH, EC) | medium | patch |
| 4 | Rebound assertions much looser than e² (M-F2) | low | patch |
| 5 | Presentation telemetry keyed to Basketball part (BH, EC, M-F4) | low | patch (any ball) |
| 6 | `CaptureBall` unsupported kind throws KeyNotFound (EC) | low | patch |
| 7 | bowling.tres description false outside the tuned geometry (BH) | low | patch |
| 8 | Worker guard regex lacks ball constants (BH) | low | patch |
| 9 | Driver `selectTool('bowling')` silently clicks a free-play anchor on authored levels (EC) | low | patch |
| 10 | Status docs claim "proven in Chrome" before review; README "WGSL f16"; TODO brief grew (BH, M-F5) | low | patch |
| 11 | `WorkshopWireTests.OnlyBasketball` / `BasketballResourceTests` naming (BH, M-F5) | low | patch (rename helper only) |
| 12 | Lever-loading obligation, offset COM and drag application have no roadmap destination (BH) | low | patch (deferred-work entries with destinations) |
| 13 | Frozen row "both balls rest without jitter": struck balls keep rolling because declared drag is unapplied (BH) | medium | intent gap — not reverted; recorded and raised to owner; resolves with the deferred drag slice |
| 14 | Bowling radius 0.38 m > Basketball 0.34 m inverts real-world order; values chosen without owner (BH) | medium | intent gap — owner decision raised; kept pending answer |
| 15 | Receiver test drops in rather than rolls in (EC, M-F3) | low | defer (rolling entry with the Ramp-fed levels) |
| 16 | Ball-kind test hard-coded at six sites; material in three hand-kept copies; pixel anchors (BH) | low | defer (capability mapping when a third ball arrives) |
| 17 | Friction/threshold not persisted; default-only so round-trip holds by construction (EC, BH) | low | defer (persist when non-default friction exists) |
| 18 | "More surface friction" toggle is a no-op (BH) | low | defer |
| 19 | Publication order: Epic 5 unpushed while CAT-014 built (BH) | — | owner instruction: commit per epic, no push |

Pass 2 (2026-10-09, Murdoch re-review after the fix round). Verdict: scoped Pass; SnapshotApproval for a local commit granted (no push). Node harness 25/25; anvil 0; 621/621; publish exit 0; cat-014 4/4; cumulative 15 suites / 54 tests, no reruns; worker sha 4ecc1747… unchanged; candidate diff sha bfb32a46…. Items #1–#12 verified (mass-isolation controls fail under a radius-decides mutant; exact-Reset height matching proven sound: 41 states bit-identical per ball, moved-ball control rejected). #13–#14 remain open owner decisions. New: F1 TODO next-slice conflict with the roadmap (fixed at close: ENGINE-DRAG next); F2 stale delivered-slices counts (fixed at close); `CaptureBall` rejection branch untested (informational).

## Verification

**Commands:**
- `node --experimental-vm-modules --test tools/workshop-rigid-body.test.mjs tools/workshop-observation.test.mjs tools/workshop-pose-ring.test.mjs` -- expected: all pass
- `anvil check --changed` -- expected: 0 warnings
- `dotnet test CuriousContraptions.slnx` -- expected: 100%
- `dotnet publish CuriousContraptions.web` -- expected: exit 0
- `node --test --test-concurrency=1 --test-timeout=150000 tools/e2e/cat-014.test.ts` -- expected: 100% in Chrome
- `node --test --test-concurrency=1 --test-timeout=150000 tools/e2e/*.test.ts` -- expected: all suites 100%
