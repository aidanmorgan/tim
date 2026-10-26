---
title: 'Preserve support coverage when reducing contact manifolds'
type: 'bugfix'
created: '2026-10-26'
status: 'done'
baseline_revision: 822317c99a08ec64ce2d7406090036d51c1c8fa2
review_loop_iteration: 0
followup_review_recommended: false
context: [AGENTS.md, docs/testing-guidance.md]
warnings: [oversized]
deferred: []
---

<intent-contract>

## Intent

**Problem:** A cylinder placed on a flat torus end develops continuing sideways motion. Published main drifts 1.7409 mm during 1–10 s; the unpublished orientation candidate drifts 1.7184 mm and fails the original 1 mm/s one-second settling check. Production tracing identifies lost support coverage when sixteen symmetric cap candidates reduce to four clustered points.

**Approach:** Correct the generic manifold reduction so retained contacts represent the available support area. Preserve the existing contact/friction/integration laws and prove settling through real production state. This bounded prerequisite unblocks the retained orientation candidate; it does not import or approve the separate failed cap-block rewrite.

## Boundaries & Constraints

**Always:** One generic f32 production path, deterministic selection, deepest-contact coverage and long-line span; keep original support, speed, energy, geometry, admission and lifecycle requirements. Use existing game-grade resting allowance chosen before new outputs: min(50 µm,max(5 µm,1e-4 × nominal horizontal size)); the 0.2 m cylinder uses 20 µm. Every observed coordinate must resolve allowance/8. Keep exact typed identities/ticks, numeric state assertions and zero skipped tests. REQ-06/07/09/14 and the current delivery/test policy remain.

**Never:** Per-element response, extra damping, velocity snap, output-fitted tolerances, contact-model change, alternative solver, fallback/compatibility path, body/save ABI changes, diagnostic production code, hash assertions, mutation tests or browser gates. Do not modify sibling worktrees. The blocked cap rewrite and orientation runtime remain read-only evidence.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|---|---|---|---|
| Full cap support | Existing cylinder radius/half-height 0.1 m on flat torus end; both body orders, supported cadences | Original 1 s speed <1 mm/s and support height; every-tick 1–10 s resting excursion ≤20 µm, penetration ≤0.5 mm plus existing surface error | No tick fault |
| Shape/control | Original box-on-end and separated box/cylinder cases | Original support or free fall as applicable; no invented cap | No tick fault |
| Partial or line support | Existing clipped-cap, skew-side, thin contact and closest-depth fixtures | Actual coverage/depth preserved; no fabricated supporting point | No tick fault |
| Ordinary lifecycle | Existing supported hollow construction Run/Reset/save/load and engine reuse | Restored construction and repeated typed physical state/events | Preserve typed atomic refusals |

</intent-contract>

## Code Map

All relative paths resolve against this spec's containing worktree, `story-engine-cap-settling`; do not use the original checkout or sibling working trees as editable inputs.

- `CuriousContraptions.Simulation/WorkshopCollisionKernels.cs`: `ReduceSideManifold` (~1090) retains deepest + farthest pair, then fills by depth. Reused by `KeepClosest` (~1084), skew cylinder sides (~1712), and hollow collision. This is the bounded production correction point.
- `CuriousContraptions.Simulation/WorkshopHollowKernels.cs`: `CollideHollow` (~72–94) forwards public hollow query candidates and applies that reducer for non-spheres. Preserve candidate normals/features/support data.
- `engine/gpu/HollowContactQuery.cs`: generates true candidate geometry. Prior observation found all 16 supporting normals vertical; do not reapply the disproven gradient-conditioning hypothesis.
- `CuriousContraptions.Simulation/WorkshopFrictionSolver.cs` (~80–95): force anchor is the reduced points' arithmetic centroid; selection also determines friction radius/key.
- `CuriousContraptions.Simulation/WorkshopConstraintSolver.cs` and `WorkshopPhysicsEngine.cs`: normal rows, warm start and central friction consume the selected manifold. Read for impact; no solver/integrator redesign.
- `CuriousContraptions.tests/WorkshopPhysicsLawHollowTests.cs`: `ASolidPartnerMeetsTheFlatTorusEndWithoutInventedCap` retains original speed/support and separated controls; add sustained behavior/cadence proof here.
- `CuriousContraptions.tests/WorkshopPhysicsLawTests.cs`: existing `LawScene`/~61 and `LawWorld`/~243 admit and advance real production simulation.
- `CuriousContraptions.tests/PhysicalToleranceTests.cs`: existing `RestingObservation`/~182 measures every-tick fixed-reference excursion/coordinate resolution. Production policy is `CuriousContraptions.Abstractions/PhysicalTolerance.cs`.
- `CuriousContraptions.tests/WorkshopPhysicsLawCylinderTests.cs`, `WorkshopPhysicsLawFrictionTests.cs`, `WorkshopPhysicsLawRestitutionTests.cs`, `HollowElementTests.cs`, `F32BehaviourIdentityTests.cs`, `WeightTests.cs`: affected geometry, energy, friction and lifecycle controls. Existing output-bit helpers are not new test APIs; use explicit typed numeric observations for additions.
- `docs/hollow-geometry-design.md` Pair rules and `docs/gpu-f32-physics.md` reduction rule: update the one documented current selection algorithm. TODO, epics and sprint retain the blocked orientation/cap work and next retry.
- Read-only predecessor evidence: sibling `story-engine-orientation-levelling/_bmad-output/implementation-artifacts/spec-engine-orientation-levelling.md` Auto Run Result and its three `failed-closure-*.log` files. Baseline is published `822317c99a08ec64ce2d7406090036d51c1c8fa2`.

## Tasks & Acceptance

**Execution:**
- `WorkshopCollisionKernels.cs` — replace depth-only fill in the shared reducer with deterministic spatial coverage selection; retain deepest coverage, line extent, valid original points and emitted order. Delete superseded fill logic; no old/new selection flag.
- `WorkshopPhysicsLawHollowTests.cs` — retain original eight cases and add sustained cylinder rest at 60/120/240 Hz and both orders through committed production state. Tests must expose the original motion, not assert internal selected indices.
- Existing affected native tests — retain clipped/line/closest-depth, geometry, friction, energy, lessons and lifecycle coverage; add only a missing boundary directly required by the chosen selection rule.
- Design/status documents above — record the algorithm and actual proof without qualifying blocked siblings or later browser/performance work.

**Acceptance Criteria:**
- Given full symmetric support, when the real cylinder lands and advances through 10 s, then the I/O matrix's unchanged speed/support and preselected resting allowance hold at each cadence and body order.
- Given incomplete or line-like candidate support, when reduction runs through real collision, then existing penetration/energy/negative-control tests still pass without inventing surface points.
- Given supported Reset/save/load and reused engine scenarios, when they repeat, then construction and typed state/events remain correct.
- Given the frozen candidate, when independent Auto review and the complete local suite run, then all required checks pass with zero skips; normal hooks and exact-SHA build/artifact/deployment precede publication. Browser/performance remain pending.

## Spec Change Log

## Review Triage Log

### 26 October 2026 — Review pass

- verdicts: 15 findings — high 0, medium 7, low 3, false 5, maybe-false 0. Edge Case Hunter and Verification Gap Reviewer found no gaps; Forward-Only Auditor found no violations. Blind Hunter supplied B1–B10; Intent Alignment supplied I1–I4.
- findings:
  - `B1` `medium` `patch` Selection reads the stack-allocated keep mask before every entry is initialized. The C# stackalloc contract leaves contents undefined; explicitly clear this algorithm's mask. This is a source-level initialization defect, not a claim that native runs observed dirty memory.
  - `B2` `low` `reject` An exact synthetic six-point collinear selection fixture was removed. Supported line-span behavior remains covered by the real thin-wall beam spanning two separated support regions, with support/speed checks, and deepest recovery by core-boundary cases. Restoring an internal-layout fixture would violate testing guidance; a new artificial collinear scenario adds complexity without an evidenced ordinary-use gap.
  - `B3` `medium` `patch` General hollow/reuse checks do not directly establish repeatability of this tied cap contact set. Add one actual cap trajectory through reset/reused and fresh engines with typed numeric state comparison.
  - `B4` `medium` `patch` Sustained position alone permits body rotation. Add independent geometric surface displacement and angular surface-speed observations using the existing radius and preselected budgets; grouped with B5/I3.
  - `B5` `medium` `patch` Only one-second speed was checked. Check the same 1 mm/s bound throughout the settled window; grouped with B4/I3.
  - `B6` `low` `patch` The reference chosen at one second can hide lateral displacement during this symmetric vertical-only drop. Add initial-placement horizontal displacement using the same preselected geometric allowance, separately from settled-reference excursion.
  - `B7` `medium` `patch` The previously failing rotated boundary had only 120 Hz coverage. Add 60/240 Hz in both orders without changing acceptance.
  - `B8` `false` `reject` The docs promise collinear extent, not exact endpoints for every nearly-collinear cloud. Nonzero area legitimately represents available width; the real thin-wall fixture is already rotated 37 degrees and checks narrow supported patches. No counterexample within the claimed contract was supplied.
  - `B9` `false` `reject` This is the uncommitted review boundary and documents explicitly leave full verification/publication pending. Exact committed-SHA approval and final proof association remain mandatory later steps; no immutable delivered identity is falsely claimed.
  - `B10` `low` `reject` Scanning to locate the at-most-three retained points is an optimization opportunity, not an observed correctness or playable-performance defect. An explicit retained-index collection adds state; detailed performance is at its named later gate.
  - `I1` `false` `reject` Intent permits impulse/manifold causal diagnosis. Observing actual reduced support geometry and corrected production behavior establishes a manifold mechanism; it does not claim a solved-impulse certificate. Further impulse instrumentation is not a required-now criterion.
  - `I2` `false` `reject` Final energy/full-suite proof is explicitly pending and the obsolete metric's 595/1/0 is explicitly excluded as proof. The coordinator will enforce full current-candidate local verification before acceptance.
  - `I3` `medium` `patch` A position-only reading proves less than physical rest; the sustained rotation/speed repair addresses this observable gap, grouped with B4/B5.
  - `I4` `false` `reject` The prerequisite's review, hooks and publication are explicitly pending; the orientation candidate remains blocked. Workflow order is preserved and completion is not claimed.

  - `C1` `medium` `patch` Coordinator found the governing execution order still contradicted the current TODO, and the deferred destination called cap-block in progress. The same implementer reconciled the execution plan/table, sprint comment and current destination while preserving all remaining work and historical evidence. Scoped YAML/link/order checks and retained independent review passed.

Grouped patch entries: medium 5 (initialization, replay, sustained rest, rotated cadence, current order), low 1 (landing reference). No deferred findings. All requested patches were applied by the same implementation owner. Build 22714 passed without warnings/errors; focused repair session 70761 passed 34/0/0 in 6.6985 s. Retained Edge reviewer independently confirmed all code/test repairs and the subsequent documentation-only correction. No remaining repair gap was identified; final full local verification and publication remain pending. B1 language authority: [Microsoft C# stackalloc reference](https://learn.microsoft.com/dotnet/csharp/language-reference/operators/stackalloc).

## Design Notes

Observation on unchanged production: the initial sixteen equally spaced cap-rim candidates at identity orientation reduce to indices 0,1,2,8. Relative X/Z positions are approximately (0.10000014,0), (0.092388,0.038268), (0.070711,0.070711), (-0.0999999,0). There is no negative-Z coverage; COM is on the selected hull boundary and the average anchor shifts about (0.040775,0.027245) m. The retained hull area is 0.00736237 m² versus the full rim's 0.03061467 m², with anchor distance 49.039 mm. Across 500 measured manifolds, 178 place COM within 1 µm of or outside a selected hull edge. [Original production observation](evidence-engine-cap-settling/manifold-reduction.log): diagnostic build 92508 passed 0 warnings/errors; session 45784 collected one measurement test successfully (1/0/0, 124 ms), not acceptance. All diagnostic source was removed, so rebuild before acceptance. This establishes a coverage defect, not proof that every downstream settling failure is solved by a proposed algorithm. Implementation must verify actual behavior before acceptance. Selection geometry may change; the contact laws may not.

## Verification

- Incremental Debug build of `CuriousContraptions.tests/CuriousContraptions.tests.csproj`, then matching `dotnet test ... -c Debug -f net10.0 --no-build --no-restore -noAutoResponse` for changed hollow behavior and affected cylinder/friction/restitution/lifecycle classes; expected zero failures/skips.
- Coordinator captures only affected compiler logs under `docs/delivery-workflow.md#local-test-authority`, then one full solution round with the generated browser deferral and `CSHARP_STATIC_GATE_BINLOGS`. No local Release worker or web publish.
- Five Auto review layers, no mutants or duplicated reviewer system. Normal hooks, committed-SHA SnapshotApproval, push and fresh small build/artifact/deployment verifier. No GitHub tests.

## Implementer result — 26 October 2026

The single generic reducer now retains the deepest point, then the farthest point from it, and fills to four by maximizing summed triangle areas to retained pairs. Area ties maximize summed squared distances to retained points, then prefer depth and emitted order. Selected original contact objects keep their geometry/features/support and original emitted order; no solver, damping, integration, ABI or element-specific changes.

For two retained points the area score is twice triangle area; for three coplanar points its increase measures convex-hull growth. For a collinear set, the area is zero for every candidate. Starting with deepest coordinate d and the farther endpoint b, the squared-distance tie score (x-d)²+(x-b)² is convex and attains its largest remaining value at the opposite endpoint a. Thus the same comparator retains the full line span without a separate line-mode or fallback.

Two bounded failed variants are retained in [verification evidence](evidence-engine-cap-settling/implementer-verification.log). Max-min distance fill with the independent diameter passed initial cap tests but failed thin-wall speed in the eight-class run (595/1/0). Area fill with that same diameter still failed thin-wall speed and both 17° cap rest cases (33/3/0); the cap reached 20.331598 µm at tick 729 against the unchanged 20 µm allowance. No threshold was changed.

The [exact-fixture production observation](evidence-engine-cap-settling/area-failure-geometry.log) reads actual retained contacts and public candidate geometry from a temporary test-only observer. At the rotated cap, forced diameter plus deepest consumed three slots and left a displaced support centroid; q.X/q.Z changed over ticks 726–729, ruling out frozen levelling in this observation. This justified anchoring the diameter at the deepest point. Thin-wall selection had two support regions and mixed actual normals; its detailed causal dynamics remain unproven, but its unchanged production behavior now passes. The initial observer used different angle/start f32 expressions and was corrected before the retained exact-fixture evidence. All observer/reflection code is removed; no production diagnostic or mutant was used.

Tests preserve the original eight flat-end cases and add four 17° positive/free-fall controls, plus eight sustained cases (all three cadences/both orders at identity yaw, and both orders at 17°/120 Hz). Every tick validates the public candidate identity and typed SimulationTick. From 1–10 s, every horizontal coordinate must resolve allowance/8, excursion stays within 20 µm, and support height keeps the existing penetration/surface bounds; one-second speed stays below 1 mm/s.

Removed only the synthetic reducer-index section of SideManifoldsKeepTheDeepestPointAndTheExtremeEndsWithinFour. Its replacement behavioral coverage is ThinWallSupportsAdvancingTiltedPartnersBetweenTheirVertices (two separated narrow support regions, supported/free-fall cases) and ABoxOverlappingAcrossTheCylinderCoreBoundaryReceivesARecoveryReaction (steps 22–28, actual depth recovery and energy). The original real skew-contact loop remains as CrossedSkewedSidesKeepTheirClosestDepthWithinFourContacts. No feature-index assertion was added to acceptance tests.

Final runtime/test build 86392 passed with 0 warnings/errors in 5.74 s. Focused session 77979 passed 36/0/0 in 4.8579 s. On those same binaries, session 47089 passed all HollowElementTests plus AReusedEnginePreservesEveryBodyTrajectoryAndStaticSupportInTheNextScene: 28/0/0, 1 s. A trailing blank-line cleanup afterward changes no test or runtime semantics. Commands/raw outputs are linked above. Final energy, friction and full-suite proof belong to the coordinator's one complete local round; the earlier obsolete-metric run is not reused as final-algorithm proof.

Current algorithm docs, brief, epic dependency and sprint status now agree. The verified warmup-removal terminal receipt is copied from the read-only predecessor record so current status does not regress published main. The orientation candidate and separate cap-block rewrite remain unpublished and unapproved; neither was edited or imported. Independent Auto review, complete local compiler/test closure, normal hooks, committed-SHA approval and production publication remain pending. Browser/performance qualification remains at its named later gates.

## Implementer review repairs — 26 October 2026

The reducer explicitly clears its stack-allocated selection span before reading it. Selection geometry and tie rules are unchanged.

The cap rest theory now covers identity and 17° yaw at 60/120/240 Hz, both body orders: twelve cases. A separate initial-placement observation bounds horizontal motion throughout the vertical-only drop and rest to the preselected 20 µm allowance; the original one-second settled reference independently bounds later excursion. Each observation retains the allowance/8 coordinate-resolution check.

Every observed tick from 1–10 s also requires linear speed below the existing 1 mm/s budget. Eight body-relative cap-rim directions, each at the declared 0.1 m radius and ±0.1 m half-height, are transformed by current and one-second orientations; their displacement is bounded by the same independently selected 20 µm geometric allowance. This measures rotational surface motion without adding body-centre translation or comparing quaternion component magnitudes. Rotational surface speed is bounded independently by |ω| sqrt(r²+h²) < 1 mm/s, using r=h=0.1 m. No allowance was derived from output.

ARotatedCapRepeatsItsTypedTrajectoryAfterEngineResetAndOnAFreshEngine observes a representative 17°/120 Hz cap for 1,200 ticks, repeats through the same engine's ordinary Step/Reset lifecycle, then through a fresh engine. Public candidate validation and decoded body reads check actual identity and tick; every replay compares numeric canonical position, orientation, velocity and angular velocity plus epoch/identity/tick. No hashes, output-byte assertions or reducer internals.

Repair build 22714 passed with 0 warnings/errors in 8.39 s. Scoped session 70761 passed **34/0/0** in 6.6985 s (12 sustained, 12 cap controls, 8 thin-wall, semantic cap replay and retained skew geometry). The first repair build caught a test-only SimulationTick/ulong comparison mismatch, corrected by constructing the typed tick at the decoded boundary before the successful build. [Raw repair verification](evidence-engine-cap-settling/review-repairs.log). This supersedes the earlier pre-review proof for changed inputs; full verification and independent repair confirmation remain coordinator-owned. Prior failed algorithm attempts and the immutable intent are preserved.

## Auto Run Result

Status: done for the local implementation and independent Auto review. The shared reducer now preserves useful cap support coverage through one deepest-seeded area/span selection rule; the previous depth-only fill is deleted. Original point geometry/features and contact laws are preserved, with explicit mask initialization.

Files changed: the shared collision reducer; hollow production-law settling/control/replay tests; removal of the obsolete synthetic reducer-index fixture while retaining real skew geometry; current hollow/physics design and delivery status documents; this spec and original diagnostic/verification evidence. No solver, integrator, ABI, element package or sibling candidate was changed.

Review: all five layers ran. The 15 individual findings, evidence and rejected reasons are in Review Triage Log: high 0, medium 7, low 3, false 5. Six grouped patches (medium 5, low 1) were applied by the retained implementer and independently confirmed by the retained Edge reviewer, including the later documentation-only order correction. No deferred findings. Follow-up review recommended: false; although patch volume triggers consideration, no specific unverified repair risk remains after retained confirmation and the final full local run. No duplicate five-layer review was added.

Verification completed on the final runtime/test source:
- SDK 10.0.402; wasm-tools manifest 10.0.112/10.0.100, workload installation SDK 10.0.400; Node v26.12.1.
- Matching compiler logs at `/private/tmp/tim-cap-settling-gate.Y745du`: PLAYTEST build 35506, 0 warnings/errors, 22.38 s; normal solution 6316, 0 warnings/errors, 22.34 s; Debug web host 63311, 0 warnings/errors, 13.97 s; file-app captures 88769 and 98540 exited 0. No local Release worker build or web publish.
- Final full local session 83621 exited 0: **4,428 passed, 0 failed, 0 skipped**. Game 3,936 (9 m 6 s), Coverage 77 (653 ms), E2eRunner non-browser 192 (12 s), WorkerAotGuard 17 (40 ms), BundleElements 206 (330 ms). This includes game runtime Node harnesses and all final geometry/friction/energy/lifecycle checks. Original TRX files are in that log directory's `results` subdirectory.
- Command: `CSHARP_STATIC_GATE_BINLOGS=/private/tmp/tim-cap-settling-gate.Y745du dotnet test CuriousContraptions.slnx -c Debug -f net10.0 --no-build --no-restore --nologo -noAutoResponse -m:1 --filter "$local_deferred_filter" --logger trx --results-directory /private/tmp/tim-cap-settling-gate.Y745du/results -- RunConfiguration.TreatNoTestsAsError=true`, where the filter comes only from `dotnet run --project tools/E2eRunner --no-build -c Debug -- deferred-filter`.
- The unchanged seeded energy gates passed: no class exceeded its Dead 2.191 mJ, Rolling 2.114 mJ or Lively 2.151 mJ control-derived budget. No repeat sweep was needed outside the full run.
- Scoped YAML, sprint membership/order, links and `git diff --check` passed. Exact source-diff comparison confirmed runtime/tests did not change during the final captures/run.

This closes the current local settling defect, not global numerical, device, performance or browser qualification. The orientation candidate must still be retried against published cap-settling and reverified; its previous proof does not qualify the combined integrator. The separate cap-block rewrite remains blocked. Both failed reducer designs and original baseline observations remain evidence, not supported runtime paths.

Remaining publication sequence: normal-hook commit, retained reviewer approval on the exact committed SHA, non-force push, then fresh small terminal verification of production build, Pages artifact, deployment and static-origin contents. GitHub executes no game/tool tests. The terminal receipt will bind these local results to the committed/deployed identity; no publication Pass is claimed before that check.

## Terminal publication receipt — 27 October 2026

Published SHA `a98ef86c656040ae1f057967667f23235e41d7ce`; production run `40356741495` succeeded (build job `121691125567`, deploy job `121696004928`). Pages artifact `12666996791`, 80,798,440 bytes; deployment `7337266683`, success status `20642484253`. Fresh terminal verification confirmed HTTP 200 and artifact equality for /, /index.html, /404.html, /godot.wasm, /godot.pck and /_framework/dotnet.js. Full local **4,428/0/0** and normal hooks **126/0/0** bind to this publication; GitHub runs no tests. Browser/performance qualification remains pending. The orientation retry starts from this published prerequisite and must verify their combination independently.
