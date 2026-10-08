---
title: 'Story 5.1: Dynamic Box Rigid Body & Upright Stability (CAT-023a Domino)'
type: 'feature'
created: '2026-10-09'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: 'f7a8ef00d94d96b19b12f97e9a73e2e2fa343114'
context:
  - '{project-root}/_bmad-output/implementation-artifacts/spec-4-3-legacy-presentation-code-retirement.md'
  - '{project-root}/AGENTS.md'
  - '{project-root}/docs/gpu-f16-physics.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The physics worker cannot simulate a dynamic box: it never reads the compiled 3x3 inertia (body record bytes 80–119), applies only linear impulses to boxes, has no box–workbench contact branch (a dynamic box falls through the bench), uses hard-coded solid-sphere inertia in Box-Sphere friction, and its Box-Box path returns one wrong point with an inverted normal. The Domino (CAT-023a) cannot exist until boxes stand and topple.

**Approach:** Give the generic solver rotational rigid-body dynamics driven by the declared inertia, a 4-point area-maximizing manifold for Box–Plane and Box–Box with per-point feature ids, and one generic point-constraint row (lever arms, two-axis Coulomb friction, warm start, speculative margin) used by every pair kind. Then add the Domino purely as declaration data and art, delete the ad-hoc inertia approximations and the legacy `parts/DominoPart.cs` physics, and prove in Chrome that an upright Domino stands still and topples flat when struck by a Basketball.

## Boundaries & Constraints

**Always:**
- One generic solver: no Domino/box/element identifiers in the worker; the body record's mass, COM, principal frame and inertia are the only source of dynamics; sphere inertia comes from the record too.
- Box2D v3 TGS Soft contacts (compliance, softness, effective mass with angular terms), speculative contacts, dissipative restitution; residuals clamp or continue (velocity clamps 64 m/s, 128 rad/s per the game-grade envelope), never fault a tick.
- Delete superseded code in this slice: hard-coded `3.5*invMass`/`2.5/radius` sphere terms, the Sphere–Plane resting velocity-overwrite hack, the single-point Box-Box result and its inverted normal, the silent material fallbacks (0 → 0.75/0.1/0.3) in favour of declared values, `parts/DominoPart.cs` legacy physics/tilt sensor.
- Domino is declaration data: `WorkshopDomino` record (half extents (.125,.55,.325), mass 0.4 kg, material restitution .05 / friction .6), `Cosmetic = None`, no ports (5.2 adds ActivationOut and the orientation sensor); body origin at the box centre (offset COM is 5.2/CAT-014).
- Exact Reset restores the placed pose bit-for-bit; Save/Load roundtrips the Domino (append enum value and wire arm; bump `CanonicalConstruction` only if the slot layout changes). Enums end-to-end.
- Preserve all existing suites: Sphere–Plane, Sphere–Sphere, Box–Sphere behaviour for Ramp/Wall/Switch/Bumper must still pass the cumulative e2e.

**Never:**
- Change table capacities, the pose ring layout (orientation already published), or the persisted fields of existing instance records.
- Add orientation sensors, ActivationOut, the cascade level, campaign content or nudging (Story 5.2 / Epic 15).
- Introduce per-element solver branches, a second integrator, or a CPU fallback.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Upright rest | Domino placed upright on the bench, Run 3 s | Tilt < 2°, drift < 5 mm, |v| < 0.05 m/s, py at half-height above the bench within 0.5 mm | N/A |
| Struck by ball | Basketball dropped onto the Domino's upper third | Tilt passes 60° within 2 s; settles near 90° flat on the bench; frame-to-frame Δq below jitter bound for 1 s | N/A |
| Box on Ramp/Wall | Domino resting on a static box face | Box–Box 4-point manifold holds it without sinking or launching | N/A |
| Fast box | Angular or linear speed beyond clamps | Clamped to 64 m/s / 128 rad/s, tick continues | No fault |
| Material zero | Declared restitution 0 | Used as 0 (no silent fallback) | Compiler rejects undefined/non-finite |
| Capacity | 16 dynamic bodies already placed | Domino placement rejected "Workbench is full: dynamic bodies" | Typed WorkbenchFullException |
| Reset / Save-Load | After toppling | Reset restores upright placed pose exactly; Load restores Domino kind, pose, parameters | N/A |

</frozen-after-approval>

## Code Map

- Worker `CuriousContraptions.Simulation/wwwroot/worker.js`: body record read `:396-458` (no COM/inertia; `radius||0.34`, material fallbacks `:430-432,485-487`); gravity `:632-636`; position/quaternion integration `:1069-1100` (about origin, no ω drag/clamp); contact branches `:639-1066` — Sphere–Plane `:641-713` (resting hack `:686-693`), Sphere–Sphere `:715-865`, Box–Sphere `:867-1030` (`solveBoxSphere :206-264`, sphere-only friction/ω `:983,995-1000`), Box–Box `:1032-1065` (`solveBoxBox :266-327`, wrong point, normal sign inverted at `:1052-1061`); no Box–Plane branch; pairs `:575-582`; AABB fattening linear only `:549-551`; pose ring write `:1261-1326`.
- Body record ABI `engine/gpu/PhysicsGpuAbi.cs:59-79` (COM 80, principal quat 88, inertia 96/104/112 as Half mantissa + int32 exponent); `RigidMassProperties.cs:39-48` box inertia; `PhysicsDeclarations.cs:156-157,213-218` capacities and one-collider rule; `docs/gpu-f16-physics.md:148-151,207-213,248-262`.
- Add-a-part (mirror Bumper): `WorkshopPartKind.cs:3` (append `Domino`); new `engine/gpu/WorkshopDomino.cs` (Wall/Bumper pattern); `WorkshopConstruction.cs:181-188` `WorkshopInput.Domino`; `WorkshopInstances.cs:75`; `WorkshopPhysicsCompiler.cs:30-40` dynamic block + box collider; `WorkbenchCapacity.cs:41` footprint (1 body, 1 dynamic, 1 collider); `WorkshopConnections.cs:23` no ports; `WorkshopWire.cs:329-331,385-388` arm; `WorkshopSaveCodec.cs:6`; `WorkshopInventory.cs:49-58` append row; `PartDefinition.cs:12,15` `"domino"`; `PartRegistry.cs:39-56`; `MachinePart.cs:37-49`; `MachineWorld.Gpu.cs:104-106` builder; `parts/DominoPart.cs` rewrite (keep art: pips at .2/.7, colours), `parts/catalog/domino.tres`, `parts/scenes/domino.tscn`; `CuriousContraptions.csproj:40,52`; `export_presets.cfg:9,37,78,114`; `ui/WorkshopIcons.cs:53` exists.
- Placement: `ui/Workshop.cs:493-501` snaps 0.1; bench surface y −0.46 (`WorkshopConstruction.cs:118`); upright centre = −0.46 + .55.
- Legacy to retire: `parts/DominoPart.cs` legacy API and inertia `:18-21`; uncompiled `CuriousContraptions.tests/DominoPhysicsTests.cs`, `DominoOwnershipTests.cs` (rewrite forward or delete), `BodyDiagnosticTests.cs:12-34`, `TrampolineTests.cs:307` references.
- E2E: driver `tools/e2e/workshop-driver.ts:71,88-93,210-218` (add `'domino'`, free_workshop row ≈(130,572), dock moves down one row ≈ y 666 — re-anchor and re-run cumulative); pose fields `:9-22`; pattern `tools/e2e/anim-1c.test.ts`.
- Tests compiled list `CuriousContraptions.tests.csproj:53`; add `WorkshopDominoTests.cs`, extend `WorkbenchCapacityTests`, `WorkshopSaveTests`, `RigidMassPropertiesTests`; worker is JS — add a Node harness fact under `tools/` if one exists for worker math, else rely on Chrome.

## Tasks & Acceptance

**Execution:**
- [x] `worker.js` body record + integrator -- read COM/principal frame/inertia, world I⁻¹ per substep, integrate about COM, angular drag/clamps -- rotational dynamics from declared data
- [x] `worker.js` manifolds -- Box–Plane (up to 4 deepest vertices), Box–Box face clipping reduced to 4 area-maximizing points with feature ids, fix normal sign; Box–Sphere point reused -- 4-point manifolds
- [x] `worker.js` constraint row -- one generic TGS Soft point constraint with lever arms, two-axis Coulomb friction, warm start keyed by pair+feature, speculative margin, restitution; delete sphere-only hacks and material fallbacks -- generic contact
- [x] `engine/gpu/*` + `parts/DominoPart.cs` + catalog/csproj/export -- Domino declaration, compiler block, footprint, wire arm, inventory row, registry, art -- new element as data
- [x] `CuriousContraptions.tests/*` -- Domino record/compile/save facts, capacity footprint, inertia read-back; retire legacy Domino tests -- coverage
- [x] `tools/e2e/cat-023a.test.ts` (new), driver anchors -- upright rest, struck topple + settle, Reset exactness, Save/Load, negative control (no ball → no tilt) -- story AC
- [x] `docs/gpu-f16-physics.md`, `docs/general-engine-design.md`, `vertical-delivery.md` CAT-023a row, `TODO.md`, sprint status -- contract and status

**Acceptance Criteria:**
- Given an upright Domino on the bench, when Run for 3 s, then it stands within the rest tolerances above.
- Given a Basketball dropped onto it, when struck, then it topples past 60° and rests flat without jitter.
- Given existing levels, when the cumulative suites run, then all prior suites still pass (no regression of sphere behaviour).
- Grep of `worker.js` finds no hard-coded inertia constants, no material fallback literals, no element identifiers.
- `anim-*`, `engine-core-*`, `cat-023a` suites pass 100% serially in Chrome.

## Implementation Notes

- 2026-10-09 planning: Checkpoint 1 self-approved under the owner's standing autonomous instruction; no Open Questions. Scope decision: offset centre of mass deferred to 5.2/CAT-014 (body origin = box centre). The preview bundle on :8060 is free for publish/e2e.
- 2026-10-09 implementation (Amelia). Decisions: (1) Domino declaration `engine/gpu/WorkshopDomino.cs`: `DominoMaterial` (half extents .125/.55/.325, mass .4, restitution .05, friction .6, bounce threshold .1) admitted only at its default bits (Basketball pattern), `WorkshopDomino` with `Cosmetic = None`; wire slot 104..116 carries the six Half fields, `CanonicalConstruction` unchanged (no existing kind layout moved); compiler emits one Dynamic body + one Box collider + one material; footprint (1,1,1,0,0,0); no ports; Free inventory appends the Domino row after Ramp; registry/`PartDefinition`/`MachinePart` admit `"domino"`; `parts/DominoPart.cs` is artwork only (centred box, pips at ±.25 which are the source .2/.7 heights relative to the new centre origin). (2) Worker: body records now supply mass, COM, principal frame and the three principal moments (Half mantissa + int32 exponent at 96/104/112) for every dynamic body; world inverse inertia is rebuilt each substep; orientation integrates from ω and the origin follows the COM (zero offset for every current body). Every pair kind (Sphere–Plane, Sphere–Sphere, Box–Sphere, Box–Plane, Box–Box) produces the same point records (normal from B toward A, world point, gap, feature id) and the same TGS Soft row: lever arms, effective masses with angular terms, two-axis Coulomb friction (geometric-mean μ, circular cone), warm start keyed by collider pair + feature, speculative margin `SPECULATIVE_SLOP + |v_rel| h`, Box2D v3 soft parameters (60 Hz, ζ=10, 3 m/s pushout), relax at the integrated positions (anchor displacement), restitution (product e, max threshold) only once the surfaces have met. Box–Plane takes the vertices within the margin reduced to four (deepest / farthest / largest triangle / opposite-side quad); Box–Box is the 15-axis SAT with the normal oriented B→A (the inverted sign is gone), reference-face selection, Sutherland–Hodgman clipping of the incident face and the same reduction, feature id = reference/incident face code × 16 + clip index. The four normal rows of one manifold are solved jointly (Delassus matrix, Gaussian elimination, 1e-3 relative diagonal regularization because four coplanar points span three rigid DOF) with sequential projected rows as the fallback; friction rows stay sequential; 8 biased + 4 relax sweeps. Contact triggers and contact-work stores on a static owner respond to any dynamic body from the same event hook (previously triggers were Box–Sphere-only and work Sphere–Sphere-only). Cell remainders carry on the Half-rounded value so a committed remainder can never round to 0.5. Deleted: `3.5*invMass` / `2.5/radius` sphere terms, the Sphere–Plane resting velocity overwrite and 0.95 damping, the single-point Box–Box result, `planeY`, the `|| 0.34 / 0.75 / 0.1 / 0.3 / 1.0 / 8.0` fallbacks, `v_in` caching. (3) Angular envelope: `RigidBodyDeclaration`, `PhysicsBodyRead` and the motion-piece decode now admit |ω| ≤ 128 rad/s (were 64) to match the frozen 128 rad/s clamp; the test pinning 64 moved to 128. (4) Node harness `tools/workshop-rigid-body.test.mjs` steps the shipped worker through hand-built admission records (same layout as `PhysicsGpuAbi.Admission`): upright rest, topple + settle, sphere bounce/rest, sliding→rolling, box on a static box face, ball strikes standing box (+ per-tick budget), clamps, source grep; every committed remainder is checked canonical. (5) E2E: driver gains the Domino palette row (130,562) and the free-workshop dock moves to y 656; `cat-023a.test.ts` places the Domino at the 3 m placement plane and lowers it onto the bench with the real move gizmo (−162 px ≈ 2.85 m at ≈56 px/m), drops the Basketball at x = 0.4 m onto the tile's upper corner, and proves rest / topple+settle / Reset exactness (re-Run shows identity orientation and placed x/z bit-for-bit) / Save–reload–Load.
- Surprises: (a) The host (not the physics) rejected the first Chrome strike at tick 119 with `Motion COM remainder is not canonical`: a double remainder of 0.49998 survived the `=== 0.5` carry and rounded to Half 0.5 — a latent bug the rotating COM-driven integration happened to hit; the UI only showed the generic "GPU simulation stopped" text, so it was found by dumping the worker's byte pairs from the published bundle and running them through `PhysicsGpuAbi.ValidateCandidate` in a throwaway xunit probe (removed). (b) A flat landing spun the box: sequential Gauss-Seidel over a thin box's four coplanar rows converges too slowly (λ 0.027/0.025/0.010/0.012 after 8 sweeps), the relax pass originally used the stale pre-integration gap, and restitution fired on a speculative row before touching — all three are fixed as described; the 4×4 block is singular without regularization. (c) Balls now roll without the old artificial 0.95 damping and keep rolling on the bench (declared drag is still not applied by the worker — pre-existing). (d) The harness test file was written before its anvil gate call (validated immediately afterwards, allow). (e) Rest penetration settles at 0.40 mm (slop 0.2 mm + soft offset), inside the 0.5 mm envelope. Host tick cost in Chrome is ≈6 ms of the 8.33 ms budget regardless of this change (JS stage ≈0.15 ms median).
- Files: new `engine/gpu/WorkshopDomino.cs`, `CuriousContraptions.tests/WorkshopDominoTests.cs`, `tools/workshop-rigid-body.test.mjs`, `tools/e2e/cat-023a.test.ts`; edited `CuriousContraptions.Simulation/wwwroot/worker.js`, `engine/gpu/{WorkshopPartKind,WorkshopInstances,WorkshopConstruction,WorkshopPhysicsCompiler,WorkbenchCapacity,WorkshopConnections,WorkshopWire,WorkshopInventory,PhysicsDeclarations,PhysicsBodyReadSet,PhysicsMotionRead}.cs`, `engine/{PartDefinition,PartRegistry,MachinePart,MachineWorld.Gpu}.cs`, `parts/DominoPart.cs`, `CuriousContraptions.csproj`, `export_presets.cfg`, `CuriousContraptions.tests/{CuriousContraptions.tests.csproj,WorkbenchCapacityTests,RigidMassPropertiesTests,BasketballResourceTests,BodyDiagnosticTests,TrampolineTests}.cs`, `tools/e2e/workshop-driver.ts`, `docs/gpu-f16-physics.md`, `docs/general-engine-design.md`, `docs/planning/invest/vertical-delivery.md`, `TODO.md`, sprint status; deleted (git rm) `CuriousContraptions.tests/DominoPhysicsTests.cs`, `DominoOwnershipTests.cs`.
- 2026-10-09 cumulative-run fixes (Amelia, same owner). The first cumulative Chrome run was 44/46 with two sphere regressions: ENGINE-CORE-2a2 test 2 recorded one bounce apex and ENGINE-CORE-2c test 2 never saw the ball in the Receiver window. Cause: restitution read the approach speed from the touching substep only, after the preceding speculative substep had already trimmed it to −gap/h, so each landing bounced with an arbitrary fraction of its impact speed. Fix: each contact carries the deepest pre-solve approach in its warm-start cache (`approach`) until a restitution impulse consumes it (`bounced`); Node apexes now decay 0.815 → 0.158 m (e² scaling). The 2c trajectory's lateral (−z) deflection off the lifted second ramp is the scenario's own geometry: the HEAD worker swapped into the published bundle reproduces it exactly (pz −3.07 m inside the window), so it is not a solver change.
- 2026-10-09 review fixes (Amelia, same owner). Worker: contact events require `currentSeparation ≤ 0` (no firing from a speculative row); the carried approach is zeroed once the contact touches, not only after a bounce; Box–Box feature ids derive from geometry (axis type/index, reference/incident face, incident corner or clip plane × starting corner) and pairs are ordered by collider index before manifold generation; a genuine edge–edge axis (cross axis more than ~8° from every face normal, `EDGE_FACE_ALIGNMENT`) and an empty clip result emit one support contact along the SAT normal with the SAT separation — a cross axis that coincides with a face normal still clips as a face (the first attempt to route every type-2 axis to the support contact collapsed the box-on-box rest manifold to one point and tipped the tile; caught by the harness); clamps are `MAX·(1−1/512)`; a zero/non-finite principal moment reads as infinite inertia; a second non-finite guard after the relax/restitution sweeps drops a non-finite velocity. Harness: `halfBits` rounds to nearest even; clamp inputs now start outside both clamps; new facts for zero restitution/friction, solid-vs-hollow rolling speed from record moments (2.14 vs 1.80 m/s), the Half-0.5 remainder carry, and zero-moment finiteness. `WorkshopWireTests.MotionPiecesAdmitTheWorkerAngularClampAndRejectBeyondIt` pins the 128 rad/s piece bound. `cat-023a` test 3 is self-contained (reload → load → Run/Reset/Run) and the strike message names the 4 s window. Docs: catalog description drops the output claim; general-engine-design states that LinearDrag is compiled but unapplied (deferred-work entry added); roadmap row and requirements CAT-023 entry record the deferred offset COM; bridge doc lists the suites and Node controls; Verification lists the Node harness command. Re-run: Node harnesses 14/14; `anvil check --changed` 0 (4 files); `dotnet test CuriousContraptions.slnx` 600/600; `dotnet publish` exit 0 (bundle sha = source sha); `cat-023a.test.ts` 4/4. Cumulative suites deliberately not re-run (reviewer schedules them).
- Verification: `dotnet test CuriousContraptions.slnx` → 599/599 (591 + 8 new facts); `node --experimental-vm-modules --test tools/workshop-rigid-body.test.mjs tools/workshop-observation.test.mjs tools/workshop-pose-ring.test.mjs` → 10/10; `anvil check` (worker.js, driver, cat-023a, harness) → 0 warnings; `dotnet publish CuriousContraptions.web` → exit 0; `node --test --test-concurrency=1 --test-timeout=150000 tools/e2e/cat-023a.test.ts` → 4/4 in Chrome; cumulative `node --test --test-concurrency=1 --test-timeout=150000 tools/e2e/*.test.ts` → 13 suites / 46 tests pass, 0 fail (586 s, one serial Chrome run, final bundle byte-identical to `worker.js`); grep of `worker.js` for element names / inertia constants / material fallback literals → 0 matches. Not done / risk: no independent review yet (sprint status `review`); not committed or pushed. Balls now roll freely on the bench (no artificial damping; declared linear drag still unapplied by the worker, pre-existing). Resting-contact creep over long runs is unmeasured beyond the 3–6 s windows. The Chrome host tick sits at ≈6 ms of the 8.33 ms budget independent of this slice (noted for the performance gate). The 16-dynamic-body capacity rejection is proven at unit level (`WorkbenchFullException`, "moving body table"), not in Chrome.

## Spec Change Log

## Review Triage Log

Pass 1 (2026-10-09). Layers: blind-hunter (BH), edge-case (EC), verification-gap (VG, mutation-verified), Murdoch (M, probe-verified). Murdoch verdict: Pass scoped (599/599; harness 10/10; anvil 0; cat-023a 4/4; cumulative 13 suites / 46 tests; worker.js sha 39462cfd…). Verified findings from other layers route to one patch round.

| # | Finding (layer) | Verdict | Route |
|---|---|---|---|
| 1 | Contact events fire from speculative rows before surfaces meet (BH, EC) | medium | patch (gate on separation ≤ 0) |
| 2 | Carried approach never decays for non-bouncing contacts (BH, EC; M: pre-existing shape) | medium | patch (zero once touching) |
| 3 | Box–Box warm-start feature id from clip index; axis type absent; pair order can flip (BH, EC) | medium | patch |
| 4 | Edge–edge SAT axis gap/normal mismatch; empty polygon → no contact (EC) | medium | patch (fallback point) |
| 5 | Clamp at exactly 64/128 can round above host bound in Half (EC) | low | patch (scale 1−1/512) |
| 6 | Non-finite guard before final sweeps; zero inertia → NaN committed (M-F2, unreachable from host) | low | patch |
| 7 | Harness clamp fact vacuous (VG, M-F1) | medium | patch |
| 8 | Half-carry fix unpinned (VG) | medium | patch (harness fact) |
| 9 | Zero-material only grep-tested (VG, BH) | medium | patch (executing fact) |
| 10 | Motion-piece 128 rad/s bound untested (VG, BH) | medium | patch (C# fact) |
| 11 | Harness inertia fact indistinct; halfBits truncates (M-F3, BH) | low | patch |
| 12 | cat-023a test 3 depends on test 2; "2 s" message vs 4 s window (EC, VG) | low | patch |
| 13 | domino.tres stale output description (BH, EC, M-F8) | low | patch |
| 14 | general-engine-design claims damping; LinearDrag unread; balls roll without decay (BH, EC, M-F5) | medium | patch doc + defer drag slice |
| 15 | Offset-COM deferral not recorded at requirement/roadmap (BH) | low | patch (doc line) |
| 16 | Node harness not on verification path / suite inventory (VG, BH) | low | patch (spec + bridge doc) |
| 17 | supportExtent stale during tick for rotating box (BH, EC, VG) | low | defer (guides only target balls) |
| 18 | Tilted plane AABB culling (BH, EC) | low | defer (only +Y bench plane exists) |
| 19 | Concentric spheres never separate (EC) | low | rejected (unreachable) |
| 20 | Block solve rarely applies mid-topple; mixed row scales; per-substep allocation (BH, EC) | low | defer (performance gate; stability probed) |
| 21 | Edits to uncompiled BodyDiagnosticTests/TrampolineTests (BH) | low | defer (Epic 7 purge) |
| 22 | Spec frozen row text "dynamic bodies" vs typed message "moving body table" (M-F7) | low | recorded; frozen |
| 23 | Box on Ramp/Wall, capacity 16 proven Node/unit only (M-F6, BH) | low | defer (Chrome proof with 5.2 cascade on Ramp) |
| 24 | Worker is JS doubles not wasm-simd128/f32 (M-F10) | — | pre-existing architecture gap; already roadmap (Epic 16) — recorded |

Pass 2 (2026-10-09, Murdoch re-review after the patch round). Verdict: Pass scoped to worker.js sha 1d0d8a1c…9532 / diff 0e49bb9b…1159, publish 20:58:47Z. Node harness 14/14; anvil 0; 600/600; cat-023a 4/4; cumulative 13 suites / 46 tests in one serial Chrome run. All 16 patched items verified; pass-1 probes bit-identical except the intended clamp-margin and zero-inertia deltas; warm start confirmed hitting (0 key-set changes over 1.5 s box-on-box); 20 000 random Box–Box poses: 0 non-finite, 0 >4 points.

| # | Finding (layer) | Verdict | Route |
|---|---|---|---|
| 25 | `supportContact` edge–edge point is an endpoint midpoint, not closest points of the two edges (M-F1) | low | carried into Story 5.2 (domino-on-domino edge strikes) |
| 26 | Clipped-point feature ids can duplicate within a manifold (22/14 291 random poses) (M-F2) | low | carried into Story 5.2 (encode full parent id) |
| 27 | cat-023a allows 4 s to 60° vs frozen 2 s; 2 s pinned only in the Node harness (M-F3) | low | recorded with #22 |
| 28 | TODO.md counts stale (M-F4) | low | fixed at close |

## Verification

**Commands:**
- `anvil check --changed` -- expected: 0 warnings
- `node --experimental-vm-modules --test tools/workshop-rigid-body.test.mjs tools/workshop-observation.test.mjs tools/workshop-pose-ring.test.mjs` -- expected: 100%
- `dotnet test CuriousContraptions.slnx` -- expected: 100%
- `dotnet publish CuriousContraptions.web` -- expected: exit 0
- `node --test --test-concurrency=1 --test-timeout=150000 tools/e2e/cat-023a.test.ts` -- expected: 100% in Chrome
- `node --test --test-concurrency=1 --test-timeout=150000 tools/e2e/*.test.ts` -- expected: all suites 100%
