---
title: 'Passive Springboard and generic prismatic soft constraint'
type: 'feature'
created: '2026-10-10'
status: 'in-review'
route: 'dispatch'
baseline_commit: '25d53233ab5ee724be0bd34081f175c47f938ff5'
review_loop_iteration: 0
context:
  - '{project-root}/docs/springboard-elastic-contract.md'
  - '{project-root}/docs/planning/elements/CAT-062-spring.md'
  - '{project-root}/docs/planning/elements/EL-194-springboard.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The current engine cannot admit a passive Springboard: its physical plate needs a constrained elastic degree of freedom.

**Approach:** Add a generic prismatic spring constraint and expose the preserved uncharged Springboard through ordinary Workshop placement, rotation and configuration. Contact compresses its plate and returns stored elastic energy.

## Boundaries & Constraints

**Always:** Preserve Story6.4, CAT-062, EL-194, todo461 and the elastic contract. Plate:0.25kg, box1.3×.15×1.2m, uniform inertia; fixed base1.3×.12×1.2m centred at localY−.36. Plate rest centreY.14; local-Y travel q∈[−.25,0]. Constrain two transverse translations and all three relative rotations; disable connected-body collision. Preserve plate restitution0, threshold.05m/s, friction.1 from the current EL-194 declaration. Stiffness k120–1200N/m/default400; damping coefficient c0–8N·s/m/default.2. Zero initial compression in this passive slice. Gold physical plate, navy base and silver helix follow committed motion.

**Never:** Add an element solver, callback launch, target velocity, energy injection, cosmetic collider motion or compatibility path. Preload/spring_forward remain6.5; signal integration/composite lanes and full named-element acceptance remain explicit later obligations. Preserve inherited geometry precision ownership; new constraint parameters/state/arithmetic are f32 in C#/WASM SIMD. No global qualification claim.

## I/O & Edge-Case Matrix

| Scenario | Input/state | Expected result | Rejection/control |
|---|---|---|---|
| Impact | Ball strikes uncharged plate | Compression within travel, physical rebound; lower second apex at default damping | No-spring/missed route gives no spring launch |
| Rest | Zero gravity, zero preload/velocity | Stationary assembly | Gravity-enabled plate may sag legitimately |
| Limits/tilt | Endpoint load, rotated frame, off-centre/overlapping contacts | Local-axis travel, fixed relative orientation, coherent contact reactions | No artificial reset/retrigger |
| Energy | Plate/payload translation+rotation, spring and gravitational potential | Dissipation or bounded numerical residual; no unexplained gain | Include external work when applicable |
| Parameters | Both endpoints, default, invalid values | Authored k/c retained; zero-preload mode | Nonfinite/out-of-range/obsolete strength and unsupported preload reject atomically |
| Lifecycle | Run, Pause, Reset, Save/Load, repeat | Authored construction restored; constraint impulses cleared, art/collider agree | Malformed identity/row/binding leaves current scene unchanged |

</frozen-after-approval>

## Code Map

- `engine/gpu/PhysicsDeclarations.cs`, new `PrismaticConstraint.cs`/`SoftConstraint.cs`: typed bodies/anchors/axis/limits, shared SIMD row law and admission.
- `PhysicsGpuAbi.cs`, new `PhysicsGpuAbi.Constraints.cs`, `CuriousContraptions.Simulation/Program.cs` and `wwwroot/worker.js`: bounded flat row state, versioned ABI, C# WASM batch export, generic solver scheduling.
- `WorkshopPhysicsCompiler.cs`, `WorkbenchCapacity.cs`: allocate distinct plate identity within the owner's existing64-ID reservation; fixed construction owner selects the assembly, secondary body drives plate presentation. Charge both bodies/colliders/constraint rows to capacity.
- New `WorkshopSpringboard.cs`; `WorkshopPartKind.cs`, `WorkshopConstruction.cs`, `WorkshopWire.cs`, `WorkshopSaveCodec.cs`, `engine/PartRegistry.cs`: canonical catalogue/admission/settings/serialization, reject old shapes.
- `parts/catalog/`, `parts/scenes/`, new declarative Springboard art; `ui/Workshop*.cs`, `WorkshopPresentationWire.cs`, `WorkshopPoseRing.cs`: actual tool/properties/rotation, explicit secondary-body rendering binding, helix compression from accepted poses. Preserve existing pose ownership/clock rules.
- `CuriousContraptions.tests/`, `tools/workshop-rigid-body.test.mjs`, new `tools/e2e/cat-062.test.ts`: primitive/ABI, shared solver and real UI controls.

## Tasks & Acceptance

- [x] Implement/adversarially review the generic row model and tests before worker wiring.
- [x] Integrate typed declaration/ABI/state/capacity and secondary-body bindings; update all affected readers/writers atomically.
- [x] Admit passive Springboard catalogue/configuration/art; preserve source palette and functional geometry.
- [x] Test matrix boundaries, malformed input, mixed contacts, energy and exact lifecycle; inspect transitive effects on existing contacts/rendering.
- [x] Produce actual serial Chrome construction: drop Basketball on board, demonstrate compression/rebound and lower second apex, tilt, missed/no-board controls, configuration endpoints, Save/Load/Reset/replay. Observe plate and coil, not only payload trajectory.
- [ ] Complete affected Production/diagnostic builds, independent SnapshotApproval, normal publication and production-origin verification before marking done.

**Acceptance:** Given the authored passive assembly, when real contact loads it, then the generic constraint produces the bounded elastic response without a prescribed launch. Given invalid construction, when decoded/compiled, then rejection preserves existing state. Given Reset, when replayed, then physical/art/configuration restoration and the intended outcome repeat.

## Implementation Notes

Implementation complete; independent final regression and publication remain pending. Final Production86119/766f11 and Playtest26410/9daae4 pass. Seven actual Chrome development cases pass: default/miss47920, rendered40-step compression3987 (4193 changed pixels, exact0-pixel Reset residual), parameter endpoints35758, tilt44320 and invalid settings/Undo/delete/recreate/Load/Run/Reset98885. The initial Save-menu scroll and wrong save-record offset failures are retained as harness corrections, not product defects.

The final active native run46193/e21710 passed756/759; three stale catalogue expectations omitted Springboard. Those test-only lists were corrected and all49 affected cases passed5962/223daf under warnings-as-errors. No runtime source changed afterward. This is an explicit union of applicable checks, not a claim that the original full invocation passed. Full affected E2E TypeScript compile37b449 passes. Shared ABI, pose ring, presentation and driver impacts require the retained reviewer’s one combined existing-plus-Springboard Chrome run.

## Spec Change Log

## Review Triage Log

- R1: generic coefficient admission allowed finite inputs whose intermediate sum/product overflowed, silently returning zero stiffness response. Added intermediate/output finite-positive rejection. Exact boundary controls reproduced two failures7405c9, then all18 passed8c1a50. Authored120–1200 stiffness was unaffected; same reviewer independently resolved R1 (24485/239d64).

- R4: relaxation reused pre-integration Jacobians with updated inertia. Refresh all reaction geometry after pose integration while retaining only the spring's pre-integration error/coefficient timing. Actual worker-stage recording control with two translating/rotating bodies reproduced4 rather than8 geometry preparations (d811d7), then passed c97620. The affected worker/ring suite passes53/53 (61900/b22c0a). This boundary control does not claim actual WASM energy qualification.

BMAD three-layer triage (all layers completed before classification; each finding retained before grouping):

| Finding | Verdict | Evidence and route |
| --- | --- | --- |
| B1 secondary identity | medium | `IsSecondaryBodyIdentity` accepts all64 reserved entities and `InstallPhysicalIdentity` trusts it. The current world caller supplies the correct plate, but the binding admission invariant admits same-owner colliders/materials; patch exact declared body membership and reject those entities. |
| B2 coil visual assertion | medium | The screenshot assertion counts the whole plate/payload region, so their motion can conceal a frozen coil. Native coil tests do not close this actual-rendering gap; patch a coil-specific assertion. |
| B3 SIMD population coverage | medium | Worker tests exercise a single joint, bypassing multi-lane scatter and shared-body scheduling. Patch distinguishable disjoint/partial batches and a shared-body sequential reaction control. |
| B4 connected collision control | medium | The generic declaration admits Enabled and Disabled, but numerical controls do not compare both or retain an unrelated collision. Patch that runtime pair-filter control. |
| B5 general angular frames | medium | Unit-quaternion local frames are admitted, while angular controls cover only individual axes with identity frames. No solver defect is established; patch compound/nonidentity-frame and sign-equivalence controls, including bounded integrated restoration without claiming an exact large-angle log Jacobian. |
| B6 repeatable WASM verification | low | Current retained evidence explicitly executed the real-WASM tests, so this is not missing candidate proof. The standard e2e instructions omit the required environment/build recipe; patch a matching-bundle command that requires execution. |
| B7 PNG dependency portability | low | The new PNG import repeats the absolute MCP installation dependency already required by `workshop-driver.ts:8`; Linux/different-install launch already fails there. Defer the pre-existing harness dependency-resolution root cause; this candidate is verified in the declared Chrome environment. |
| B8 distinct observations | medium | `observe` counts repeated pose publications; configuration checks can satisfy the count without adequate simulation progress. Patch deduplication using existing publication sequence/time and assert actual progress. |
| B9 row role typing | medium | Worker indices5 and>=6 determine spring/stop bias and bounds independently of C# row order. A changed order can silently alter constitutive behavior; patch a canonical typed row domain, explicit JS boundary mapping and mapping checks. |
| B10 current brief | low | TODO repeats historical attempts and counts already retained in review evidence, obscuring the next acceptance check. Patch directly to outcome, blocker/owner/retry, next check and links. |
| E1 source palette | low | EL-194 requires base#273446 and coil#ccd9df; art currently uses#293954/#bdc7cc. Correct the two literals and verify the resulting render. |
| G1 SIMD scatter coverage | medium | Pre-verified gap: all numerical fixtures have one joint, so scattering only lane0 would pass. Patch five distinguishable disjoint joints against individual responses/impulses, including the partial batch. |

Grouping after individual verdicts: B3/G1 share the untested worker batch scheduling/scatter root cause. All other retained fixes remain separate; B7 is deferred as the inherited harness root cause. Corrections preserve the approved behavior and existing evidence; no new gameplay API or owner decision is needed.

## Design Notes

Authored damping is c, not dimensionless ζ. With m_eff=1/(J M⁻¹ Jᵀ), ω=√(k/m_eff), ζ=c/(2√(k m_eff)); defaults give ζ=.01. Use γ=1/[h(c+h k)], β=h k/(c+h k), Δλ=−(Jv+βC/h+γλ)/(J M⁻¹ Jᵀ+γ). This matches existing worker.makeSoft and the [official Box2D soft formulation](https://box2d.org/posts/2024/02/solver2d/). The prior documentation γ had an extra h; correct its two occurrences, not the contact behavior.

Interleave generic bilateral alignment, unilateral stops and elastic rows with contact sweeps at480Hz; warm-start keyed rows, use accumulated limit clamps. A real spring remains active during relaxation ([official prismatic implementation](https://github.com/erincatto/box2d/blob/main/src/prismatic_joint.c)); do not relax away its physical restoring response. Three-dimensional Jacobians use both bodies' current inertia/lever arms. No general motor/hinge API is implied.

EL-194 already declares the material, so CAT-062's material question is stale, not a new owner choice. Story7.4 already purged SpringPart; retire no imaginary legacy file. Existing epic6context remains valid;6.3 is published/done.

## Verification

Native model tests include an independent oscillator time-scale/damping oracle at h=1/480s: m=.25kg, k=400N/m, c=0 has analytical period2π√(m/k)≈.15708s; c=.2 gives ζ=.01 and amplitude envelope exp(−c*t/(2m)). Measure convergence over physical time with different iteration/relaxation counts, so repeated full-step force or removal of the real spring cannot pass. Also test effective-mass/damping units, travel/tilt, energy including rotational terms and f32/SIMD parity; wire/save/capacity tests cover two-body ownership and atomic rejection. Worker and actual WASM tests exercise the same row law. Chrome uses actual controls and run-specific evidence inside tim. Shared solver/ABI/rendering impact broadens the retained reviewer's regression selection; detailed performance remains at its named gate.

- R2: corrected review diagnosis: Admit already enforces byte-exact initial state; the actual defect was electrical-route comparison spanning the appended prismatic state. A genuine advance with nonzero impulses failed with “Electrical routes changed” (67821/c86331). Limited that comparison to its table and validated finite signed alignment/spring versus nonnegative unilateral stop state. Full candidate controls pass15/15 (52746/bcc386), including declaration/count/padding/NaN rejection; no redundant initial-admission gate was added.
- Secondary identity closure: pose-ring records move forward to64bytes with full UInt64 IDs at48. Actual JavaScript reader controls pass3/3 (66d414), including first secondary/maximal ID, sequence selection and old-layout rejection. Native full-width controls pass20/20 with ABI cases (82804/3b900e); affected driver/consumer TypeScript checking passes35774a. Actual preview/plate/coil native Reset and declaration/Save/capacity pass6/6 (1250/057549). Whole-world lifecycle and actual Chrome remain incomplete.

- R3: actual worker→reader test reproduced per-slot sequence ties selecting timestamp100 instead of200 (d8e881). One monotonic publication sequence now spans all slots;4/4 passc8fa5f and independent0c8778. Covers wraps, empty/shrinking body population and fresh-ring rebind; not a claim of worker disposal/restart proof.
- Generic3D Jacobians: independent22/22 passed20244/5fc0e1. Moving/rotating off-centre finite differences and rigid-transform covariance cover linear rows; angular controls prove signed error and quaternion-sign equivalence, not integrated restoration. Original covariance failure880089 measured2.38e−6 against2e−6;4e−6 permits accumulated f32 transforms/dot/cross residual at O(1–4) coordinates (~8–32 local ULPs), not an energy allowance.
- Integration draft: C# f32 Jacobians and SIMD row export interleave with contact sweeps; connected collision disabled. Simulation build70784/4901bc passed. Native UI compile corrections889023 (reserved tuple Rest) ande1570c (Godot Basis constructor), plus unused-fixture warning6d6fec, are retained; clean6/6 native pass057549 follows. Preview declares a visual slot with no fake identity; authored admission installs the secondary ID. Integrated WASM behavior is the next decisive check.

- Initial actual Chrome draft observation after Playtest98943/b0596f: palette/preview and manual lifted Basketball/board construction work at8074. Plate secondary ID4294967490 compressed from rest3.14m to sampled2.984m and reversed with payload at2.889m/s; sampled payload apex4.050m then3.808m, below initial4.55m. Zero captured errors. This pre-R4 debug run is retained, not final energy/lifecycle proof. Screenshots: `.anvil/springboard-first-ready.png`, `.anvil/springboard-first-ball.png`; real UI dock and drag recipe retained in tool output. Corrected bundle and plate/coil visual/lifecycle controls remain owed.

## Candidate acceptance matrix

| Criterion / affected closure | Actual evidence and applicability | Remaining gate |
|---|---|---|
| f32 SIMD coefficients and physical time | Independent18/18 model239d64;22/22 Jacobian5fc0e1; unchanged model inputs | Final snapshot binding |
| ABI admission / finite state | Independent16/16 R2 8a2d3f; full candidate malformed controls | Current native result above |
| UInt64 secondary identity / newest ring | Independent actual writer-reader4/4 0c8778; full-width native controls | Combined Chrome lifecycle |
| Generic integration / energy | Real worker plus actual WASM4/4 independently3869aa and4118b1: zero-g rest/stops, tilted pair, anisotropic reactions, gravity/contact full energy; fixtures bypass admission | Bind unchanged worker/export law to final bundle |
| Authoring / malformed bindings | Independent6/6 authoring4ec68a and2/2 whole-world atomicity8604ef; current native756+49 scoped reconciliation | Combined Chrome |
| Actual passive gameplay / art | Default/miss, lower second apex, tilt, k/c endpoints, compression4193 pixels and exact rendered Reset, Save/Load/repeat | Independent seven new UI cases |
| Invalid settings / ownership | Final98885/a2c4b8 pass1/1,16.833s; rejects nonfinite/out-of-range, preserves Undo, retires old plate on delete/recreate, reloads new identity | Independent UI replay |
| Existing callers / clocks / animation | Changed shared engine, presentation, ABI and driver invalidate affected previous browser receipts | All19 existing E2E files plus new Springboard once, reviewer |
| Publication | Production and Playtest logs in .anvil; no current product failure | SnapshotApproval, normal commit/push, CI/deployed identity and actual production controls |

Historical draft assertions below remain original evidence, not final status. Final source/artifact membership is recorded in snapshot-6-4-passive-springboard.json; reviewer record is excluded from self-hashing. Preload remains6.5; no authorable compressed fixture was introduced.

## Post-freeze verification corrections

Original snapshot `d4f7a1a5dfd04dfa379b7ede14ba8f35d5fadf05067a239a3dbf05f458abb3fd` and both bundles remain immutable. Reviewer combined Chrome57003 ran68 cases:64 passed,4 failed; all7 Springboard cases passed. Retained failure log: `.anvil/reviewer-story-6-4-chrome.log`. Actual screenshots established stale test targets after the new palette row: Battery supply button is750 (old703 hit heading), Domino source connection790 (old746 hit heading); target choice746 remains valid. Only test anchors changed. Targeted Battery3/3 pass60085/db9dac (79.030s), Domino1/1 pass82401/1e7982 (11.567s). Original PNGs preserved in `.anvil/springboard-regression-original-images`.

- Source fact4 now has an actual worker/WASM parameterized test for0/0/0,30/50/70,90/0/0, Godot YXZ convention, zero gravity/c0 and2m/s normal ball approach. Original1e−5J bound is retained without relaxation; excess0/3.12e−7/4.216e−6J, compression~.0864m, return~1.499m/s. Pass2e7ab1/f25dd8; `.anvil/springboard-three-orientations.log`.
- New resource/wire controls reject obsolete strength/initial_compression resource Parameters before mutation, permit valid retry, reject unsupported Springboard tail bytes112/156 and preserve accepted save. Actual `WorkshopSimulation.Reset`→`WorkshopGpuDevice` compilation/admission/commit clears all8 warm-start lanes and restores the exact prismatic table over2cycles. The held transport stages nonzero valid impulses plus a complete motion envelope; this proves lifecycle transaction behavior, not numerical dynamics. Affected23/23 pass27532/892ec1/3316a5,296ms,-warnaserror. Initial readonly-property compilation and incomplete motion-fixture failures remain in `.anvil/story-6-4-proof-gaps-native*.log`.
- Process deviation: one Anvil patch failed input-shape validation, but the batched call applied its test-only motion-span edit before the decision was inspected. Full current content was immediately revalidated Allow; this was not pre-write compliance. Coordinator requires strictly separate gate/decision/write calls thereafter. No policy exception or product change is inferred.
- Delta paths: `tools/workshop-rigid-body.test.mjs`, `tools/e2e/cat-005.test.ts`, `tools/e2e/workshop-driver.ts`, `CuriousContraptions.tests/WorkshopSpringboardTests.cs`, plus newly affected `CuriousContraptions.tests/WorkshopGpuDeviceTests.cs`. Exact hashes delivered in tool4df40d and retained reviewer message. Independent correction review and BMAD layers remain pending; no publication approval yet.

## BMAD correction candidate

All12 triage rows above remain preserved. B1 now admits only the exact secondary plate ID; same-owner material/collider/constraint IDs reject without changing the installed binding. E1 restores base#273446/coil#ccd9df. B9 uses canonical C# row kinds, explicit semantic export order and validated JS mapping; equations/order are unchanged. B2/B8 now observe distinct advancing sequence/time samples and a lower-coil-only silver mask that excludes payload/plate pixels. B3/G1, B4 and B5 have real-worker/WASM batching/shared-body, Enabled/Disabled plus unrelated-collision, and compound/nonidentity-frame ±quaternion/near180° controls. B6's matching build/SPRING_WASM recipe requires nonzero execution and zero skips. B7 remains explicitly deferred in current deferred work; B10's concise brief is coordinator-owned.

Affected native36/36 owner64936/032edb and independent87925/c52da9 pass. Affected worker/ring/observation Node55 pass with8 deliberately gated skips; matching actual-WASM selection8/8,zero skipped passes45640/e778ff and independent1417/e81a1d. Near180° restoration ends below.00037rad; the128rad/s assertion is the existing committed angular envelope, not a prediction of instantaneous stabilization bias. These numerical fixtures bypass C# admission; no exact arbitrary-angle log-Jacobian claim is made.

Matching Playtest15054/09cc50 and Production37088/e7cf6d pass; retained separately at `.anvil/story-6-4-bmad-playtest` (immutable8076) and `.anvil/story-6-4-bmad-production`. Owner actual Chrome7/7 passes14164/cda87e in87.003s. Final narrowed coil-only replay33050/c4d3f3 passes1/1 in18.180s:41UIsteps,226rest/120compressed silver pixels,47new strand positions and exact0-pixel Reset residual. Images/log: `.anvil/springboard-bmad-coil-final/`, `.anvil/story-6-4-bmad-coil-final.log`. The prior dead8074 listener attempt and private test-helper TypeScript error remain retained; corrected UI targets live8076 and affected TypeScript compiles e39e08.

No unresolved product finding is known; independent final seven-case UI/empty-joint control, exact correction identity binding, SnapshotApproval and publication/deployed proof remain pending. Original snapshot is unchanged; scoped replacement identities/current membership live in `snapshot-6-4-review-corrections.json`.
