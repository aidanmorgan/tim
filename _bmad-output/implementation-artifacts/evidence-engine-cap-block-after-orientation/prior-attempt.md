---
title: 'Retry the contact block solve after friction and recovery prerequisites'
type: 'bugfix'
created: '2026-10-26'
status: 'blocked'
baseline_revision: 822317c99a08ec64ce2d7406090036d51c1c8fa2
review_loop_iteration: 0
followup_review_recommended: false
context: [AGENTS.md, docs/testing-guidance.md]
warnings: [oversized]
deferred: []
---

<intent-contract>

## Intent

**Problem:** The next authorized roadmap slice is engine-cap-block. Its current normal and restitution blocks regularize rank-deficient coplanar contacts, then switch to sequential per-row recovery when inversion fails or an accumulated impulse would become negative. This causes poorly determined contact response and retains two solving paths. Published central friction and bounded penetration now satisfy the historical retry conditions.

**Approach:** Replace the normal and restitution recovery paths with one deterministic, bounded active-set rule for every nonempty contact block. Keep real physical response, soft-contact branch semantics and current energy gates; prove supported cap/face/line/point contacts and rod rocking through production simulation.

## Boundaries & Constraints

**Always:** Use f32, one current algorithm, accumulated nonnegative impulses, the existing coherent NormalBranch/slop/push-out policy, central friction and rolling, restitution eligibility and per-substep energy limiter. Preserve current seeds, physical energy/heading/penetration/Weight/F32 behavior bounds, exact replay and typed identities. Current game-grade resting displacement acceptance supersedes old 1e-9 m/s gates. Numerical correction failure discards that correction and continues; never fault a tick for residuals. Use native behavioral tests and independent Auto review; local tests/hooks authoritative, GitHub builds/publishes/deploys only.

**Never:** Per-row fallback, a separate single-contact solver rule, mode/compatibility alias, diagonal regularization to hide structural rank, double game arithmetic, velocity snapping/sleep thresholds, changed ABI/save format, relaxed energy gates or output-derived tolerances. No private solver-state certificate as the acceptance substitute for scene behavior. No browser/mutants or local Release worker/web publish. Do not restore or merge the old failed worktree.

## I/O & Edge-Case Matrix

| Scenario | Input/state | Observable behavior | Numerical handling |
|---|---|---|---|
| Flat support | Standing cylinders r0.1/r0.4 and box face on plane, rest and small drop | Finite advancing ticks, support within0.5mm resting penetration, no introduced spin; current size-based excursion allowance | Same block algorithm |
| Partial unloading | Declared off-center/spinning impact with approaching and separating regions, e0 and nonzero | No tensile attraction; appropriate rebound/support and bounded energy | Negative deltas allowed, total impulse nonnegative |
| Flat restitution | Cap/box on bench and box on dynamic box, e0/intermediate/1 | Flat response without artificial spin; retained analytic rebound and energy bounds | Singular candidate skipped |
| Rod rocking | r0.1,h0.4,m1 Dead rod at10.5°/12.5°, azimuth0/22.5/45; above-rim control | Each rim impact within one-substep inelastic model without+.01 margin; no over-rim crossing, tilt<1° by model settle time; above-rim control topples | No restored empirical margin |
| Line/point/slop | Lying-cylinder line and single contacts, approach/separation and band boundaries | Retained coherent support/speculation/restitution and friction behavior | Same enumerator including one point |
| Scales/near dependence | Supported small/large declared bodies and near-dependent face/cap contacts | Finite state and real support/response with advancing ticks; no persistent lost support | Reject unusable correction atomically, continue |
| Lifecycle | Same construction on fresh/reused engine and Reset/save/load where supported | Exact typed physical replay/restoration and correct epoch/ticks | No stale impulses/state |

</intent-contract>

## Code Map

- `CuriousContraptions.Simulation/WorkshopConstraintSolver.cs`: NormalBranch~319, CouplingMass~347, SolveLinear~377, SolveNormalBlock~420, SweepManifolds~486, ApplyRestitution~614. Delete BlockRegularization and SolveNormalRow/ApplyRestitutionRow and their single-point/recovery branches. Replace/rework SolveLinear and its only two production consumers within this closure; preserve contact geometry, chord-identity coupling, branch coefficients, sweep order, Bounces, MaxImpulse and restitution energy limiter.
- `CuriousContraptions.Simulation/WorkshopCollisionKernels.cs`: ContactPoint.NormalMass declaration if it becomes unused; remove its assignment and all truly dead consumers, not a deprecated slot. Keep EffectiveMass where rolling/friction still needs it. Trace before deleting SoftCoefficients members; coefficient identity is not assumed bit-exact.
- `CuriousContraptions.tests/WorkshopPhysicsLawCylinderTests.cs`: InelasticRockingSettleSeconds~1072 currently adds+.01; rod test~1122 lacks per-impact observations; face rest sweep~2398 already uses PhysicalTolerance. Retain all seeds/windows and current acceptance; add direct impact observations and flat-support/scale controls.
- `CuriousContraptions.tests/WorkshopPhysicsLawRestitutionTests.cs`: The pulling/rank-deficient fallback test around line392 directly asserts old internal/equal-share behavior. Replace meaningful coverage with admitted-scene behavior, not another private distribution assertion. Retain AFlatDropReboundsAtItsRestitutionWithoutSpinOrEnergyGain, dynamic-box and stack energy controls, seeded energy sweep and replay cases.
- `CuriousContraptions.tests/WorkshopPhysicsLawTests.cs` plus relevant law classes: reuse LawScene/LawWorld production admission/step and typed observations; adapt only assertions invalidated by the deleted rule. No new harness/platform or broad cleanup.
- `CuriousContraptions.tests/WorkshopPhysicsLawRecoveryTests.cs`, `WorkshopPhysicsLawFrictionTests.cs`, `WeightTests.cs`, `F32BehaviourIdentityTests.cs`, `LaunchLessonSolveTests.cs` and applicable Workshop lifecycle/save tests: regression consumers, preserve their physical outcomes.
- `docs/gpu-f32-physics.md` One Constraint Row, normal branch~234, rocking model~238–242, restitution~249 and game-grade envelope: replace obsolete fallback descriptions with the complete current rule/units/continuation policy; remove+.01 model margin only with passing direct proof; retain all unrelated gates.
- `_bmad-output/planning-artifacts/epics.md` engine-cap-block story~120 and current execution plan, TODO.md, sprint-status.yaml/deferred-work: record prerequisites published and current retry; supersede only nanometre rest wording with existing owner policy. Preserve story/requirement IDs.
- Historical `spec-engine-cap-block.md` and `engine-cap-block-attempt.patch` are read-only diagnosis, not instructions/current input. Original fixed-order candidate failed through moving endpoint friction and deep rim push-out; both prerequisites now changed. Minimum-norm experiments also regressed control energy. Do not import their algorithms or stale worktree paths.
- Carry one short warmup terminal receipt in `spec-engine-remove-core-warmup.md`, update its TODO/sprint/deferred/A9 status as part this slice. Verified predecessor SHA822317c99a08ec64ce2d7406090036d51c1c8fa2; run40324744448 success; Pages artifact12650828689/80,805,681bytes; deployment7332347579/status20623823471; all six static endpoints200/artifact-equal; full local4411/0/0, hooks126/0/0; browser/performance pending. No receipt-only publication.

## Design Notes

This fresh retry uses the source story's first admissible active subset, not the historical minimum-norm amendment: central friction now consumes aggregate load, removing the historical reason to spread loads merely to stabilize endpoint friction. Sets enumerate by decreasing cardinality, lexicographic contact-index combinations, empty last. Candidate computation never mutates bodies/accumulators. All nonempty sizes, including1, use this mechanism; no per-row recovery.

Preserve the current unconstrained soft relation exactly at the algebra level:
`lambdaNew_i = (1-impulseScale_i)*lambdaOld_i + massScale_i*(K^-1*(-vn-bias))_i`.
With positive `massScale_i`, use `z_i=lambdaNew_i/massScale_i` and `b=K*(((1-impulseScale)/massScale)*lambdaOld)-vn-bias`. Enumerate nonnegative accumulated z, solve selected principal systems, validate inactive transformed residual `K*z-b >= -tolerance`, then apply `lambdaNew-lambdaOld`. The residual is the soft/speculative constraint residual, not an unconditional zero raw normal velocity. Do not silently substitute a rigid solve or assume separately rounded mass/impulse coefficients sum bit-exactly to1. For restitution use massScale1/impulseScale0 and its own target `-e*RelVel`, retaining old support in b. Document this derivation.

Conditioning and feasibility have separate units, selected before new output:
- Equilibrate each principal matrix by its positive diagonal square roots; reject nonfinite/nonpositive scale and a pivot below64*2^-24 of the normalized matrix scale. This dimensionless f32 cutoff rejects numerical dependence without removing the resolvable rotational support of admitted thin faces. The64-u factor is a practical small-system arithmetic margin, not a global conditioning certificate; residual validation remains separate. Fixed partial-pivot tie order.
- Impulse nonnegativity is exact; a negative candidate is rejected, not snapped. Negative deltas remain valid unloading.
- Residual allowance is `64*2^-24*max(ContactSlop/h, maxAbs(b), maxRowSumAbs(K_ij*z_j))` in m/s, applied to active equality and inactive inequality. The64-u factor is a practical rounding allowance for the bounded four-row computation, not a conditioning-error certificate; the velocity floor is one substep's slop speed, not a stop threshold. This numerical choice does not widen any scene energy/pose criterion. Document units and scaling.
- If no finite admissible candidate exists, discard the correction with no partial mutation and continue from existing finite state. This is numerical continuation inside this algorithm, not another solver or a claim that residuals were solved. No exception, per-row recovery or least-residual alternate law. Behavioral scale/near-dependence controls must prove supported scenes retain support and respond, not merely remain finite.

The exact mathematical coefficients and role of transformed residuals must be preserved by code and checked against single-contact scalar behavior and mixed slop scenes. Any reproducible physical failure is a finding to resolve within this slice, not permission to restore fallback or loosen a gate. Current first-use/performance qualification stays later.

## Tasks & Acceptance

- [ ] Add/replace production-interface tests for every matrix row, recording removed internal assertions and their replacement behavior. Observe current-baseline energy controls before solver changes so a moved control cannot disguise a regression; bounded affected baseline checks only, no full baseline suite.
- [ ] Implement the one block algorithm and delete its legacy closure; document the formula/order/tolerances and numerical continuation.
- [ ] Update current contracts/tracking and predecessor terminal receipt, preserving affected REQ-06 forward-only, REQ-07 typed values, REQ-09 lifecycle and REQ-14 independent delivery.
- [ ] Run affected checks, independent Auto review/repairs, final full local round, normal hooks and exact-SHA publication.

**Acceptance Criteria:**
- Given admitted flat/partial/line/point scenes, when production simulation advances, then every matrix outcome holds with no dual solver path, ABI change, fault or persistent missing support.
- Given the rod matrix, when each rim impact is observed, then independently derived one-substep retention and no-margin settling hold; the original physical control remains discriminating.
- Given current game-grade rest and energy contracts, when existing seeded/regression scenes run, then bounds/seeds/windows remain unchanged and the control's own energy behavior has not regressed to conceal a cylinder excess.
- Given local verification and independent review, when the candidate is published, then the exact SHA's production build/artifact/deployment are verified; browser/performance remain explicitly pending.

## Spec Change Log

### 2026-10-26 — Implementation investigation: preserve the thin-face physical mode

The initial1e-3 conditioning choice incorrectly rejects a real support direction for the admitted0.2m by0.2m by0.002m box. Independent analytic coupling uses Kij=1/m+zi*zj/Ixx+xi*xj/Izz, Ixx=m*(hy²+hz²)/3, Izz=m*(hx²+hy²)/3. For half extents(.1,.1,.001), diagonal-equilibrated four-corner eigenvalues are1.599808,2.399712,0.000479894,0; a three-corner principal solve has pivots1,0.960058,0.000479751. Mass cancels, so both tested masses have the same distinction. The previous cutoff rejects the legitimate thin-axis rotational mode, not just the coplanar nullspace.

Use64 f32 unit roundoffs (3.8146973e-6) at normalized matrix scale: a small-system numerical-rank allowance, below this physically required mode by over100×, while leaving active/inactive residual validation unchanged. This corrects the algorithm's resolution, not scene acceptance. KEEP deterministic ordering, transformed soft equation, no regularization/fallback, all scene/energy/rod/Weight bounds and seeds/windows, and exact nonnegative accumulated impulses. No assertion tolerance is widened. Recheck the existing40 support cases and discriminating failing rod/Weight cases before any broad run. The independent calculation is coordinator command chunk bc925c; it is not measured simulation output.


## Review Triage Log

## Verification

Implementer: no commit/push/full suite. Use incremental affected Debug builds and actual production tests; reuse matching outputs with --no-build --no-restore. Capture current baseline physical energy/control observations before runtime edits, then affected positive/control/boundary checks after implementation, including cylinder/restitution/recovery/friction/Weight/F32 identity/lessons and lifecycle closure. Refresh complete matching compiler logs under docs/delivery-workflow.md#local-test-authority when relevant inputs change; run static/isolation guards against those logs. No unsupported test exclusions, Skipped0.

Coordinator: audit every matrix row's executed passing test, run all five Auto layers, return accepted fixes to same implementer and retain independent confirmation. Only after review fixes, run one full local solution round with mandatory five compiler logs and the generated browser deferral filter. Normal hooks, exact committed-SHA SnapshotApproval, non-force publication from this baseline, then small terminal verifier. No remote tests, browser, mutants, local Release worker or web publish.

## Implementer result — 26 October 2026 (incomplete)

The specified accumulated active-set algorithm is implemented, but the unchanged physical acceptance is **Fail**. No commit, push, full solution round, hook run, browser, mutant, Release worker build or web publish was performed. Independent Auto review and final compiler-log/static qualification remain coordinator work after physical failures are resolved.

### Changes

- One equilibrated principal-set enumerator handles every normal/restitution block, including one point; ordering, conditioning and transformed residual match this spec. Candidate evaluation is private and applies only a fully finite correction. Removed diagonal regularization, both sequential row solvers, their recovery branches and the dead NormalMass slot/assignment.
- Replaced the old private pulling/rank-deficient equal-share assertion with admitted spinning-face contact versus lifted control at e0/e0.5; existing flat rebound tests cover rank-deficient admitted faces/caps. Existing mixed-band scalar expectations now use the unregularized relation, retaining their original bounds.
- Added forty admitted support cases (cap/face/narrow face/line/point, r0.1/r0.4, m0.25/m8, rest/2mm drop) and eighteen rod impact observations (six release/azimuth combinations at all three cadences). New tests are explicitly registered in the test project.
- Rod impacts are bracketed by observed sign crossings and successive turning points; energy is actual kinetic plus potential above upright rest, retaining the initial0.5mm lift and finite-sampling kinetic remainder. No empirical margin is added. Settling oracle removes+.01 in this unapproved candidate, but the per-impact requirement remains failing.
- Current physics contract documents the complete candidate rule and unresolved failures. TODO/sprint/epics/deferred/A9 are refreshed; warmup predecessor terminal receipt is carried in its own spec. REQ-06/07/09/14 identities and requirements remain unchanged.

### Commands and results

All commands ran from this worktree. Ordinary test options: `-c Debug -f net10.0 --no-build --no-restore -noAutoResponse -- RunConfiguration.TreatNoTestsAsError=true`. Build command: `dotnet build CuriousContraptions.tests/CuriousContraptions.tests.csproj -c Debug --no-restore -noAutoResponse -p:TwoDogAutoImport=false --nologo`. Incremental build session69701: success,0 warnings/errors,4.37s. Subsequent added-test compile initially found double observations passed to float.IsFinite; corrected to double.IsFinite before execution. New files require explicit Compile registration, now present.

| Run/session | Scope | Result |
|---|---|---|
|83620 baseline|Cylinder and restitution law classes, pre-change binaries|196 passed,0 failed,0 skipped;7.8759m|
|98244 baseline detail|The two FreshSeedDrops energy tests; existing output helper reports material control bounds|2 passed,0 failed,0 skipped;2.5318m|
|51905 candidate|Restitution class, rod settling, mixed-band expectations, dropped-cap/Weight controls|77 passed,0 failed,0 skipped;1.8875m|
|47207 candidate|New40 support cases plus18 per-impact rod cases|45 passed,13 failed,0 skipped;4.63s|
|55903 diagnostic repeat|Support cases alone, minimal output to retain failure details|36 passed,4 failed,0 skipped|
|25739 candidate|Two seeded energy gates, Recovery/Friction/Weight/F32 identity/LaunchLesson classes|222 passed,1 failed,0 skipped;3.3326m|
|10995 failure isolation|Same regression classes without already-passing seeded energy gates|220 passed,1 failed,0 skipped;25s|
|3583 temporary causal probe|Eighteen rod rows with active-set outcome counting only; no changed solving rule|9 passed,9 failed,0 skipped|

The initial baseline broad class selection included unrelated cylinder geometry sweeps and took longer than necessary. Detailed energy capture overlapped the first run because normal console output omits passing-test output; this was not another algorithm candidate. No additional baseline run followed.

Baseline and candidate seeded material bounds, in mJ (existing helper rounds to0.001mJ; actual control is bound minus m*g*ContactSlop within that rounding interval):

| Material | Baseline bound | Candidate bound | All six classes |
|---|---:|---:|---|
|Dead|2.191|1.994|0/200 failures each|
|Rolling|2.114|2.115|0/200 failures each|
|Lively|2.151|1.966|0/200 failures each|

Dead and Lively controls improved. Rolling changes by about0.001mJ; all cylinder maxima also remain below the original rounded baseline bound, so the shifted control does not hide a cylinder excess. No seeds, windows or energy allowances changed.

### Unresolved findings and bounded causal diagnosis

1. **Rod retention:** three release/azimuth combinations fail at all cadences, nine rows total. At240Hz,10.5°/22.5° second impact keeps0.8514348196 vs0.8488746499;12.5°/0° keeps0.8523219411 vs0.8483997198;12.5°/45° keeps0.8571401276 vs0.8483290195. The120/60Hz samples show the same excess (small peak sampling differences). No-margin settling and above-rim toppling controls pass; they do not replace impact acceptance.
2. **Narrow face (new stricter window; required-contract failure not established):** full dimensions0.2m ×0.2m ×0.002m (half extents0.1,0.1,0.001), mass0.25kg or8kg, rest-height or2mm-drop start. This new test uses2s settling plus1s every-tick observation, NOT the existing4s settling plus6s observation contract. It moves beyond the independently selected5µm rest allowance in this earlier window. No follow-up standard-window run was performed; these four failures require review of the test window before being called a required-contract regression. First failures include5.260101µm at tick277 (m0.25/drop2mm) and5.6317404µm at tick244 (m8/drop2mm). The other36 support cases pass; no missing-support/finite-only claim substitutes for rest.
3. **Existing Weight stack:** AStackEndDownHoldsWithoutBounceOrCreep(4,4) moves body2 by0.5049187mm in its last second against the unchanged0.5mm limit.

Two preliminary rod observations were rejected as invalid oracles, not used to change the bound: consecutive sign-change frames cut through the distributed impact; tilt-only potential omitted the release lift and sample kinetic remainder. The retained test measures completed peak-to-peak actual energy and keeps the same one-substep retention limit.

A temporary observer counted selected active sets between peaks without changing solver arithmetic. Every rod solve found a candidate: **zero discarded corrections**. The failing12.5°/45° second interval at240Hz had (block size, selected size) counts (1,1)=2096,(3,1)=272,(4,1)=240,(4,2)=12,(4,0)=4. Thus the continuation branch is not causing the excess; the preserved soft/speculative contact law predominantly selects one loaded rim through the transition. This is diagnostic evidence, not a private-state acceptance certificate. Observer and counters were removed. No alternative algorithm, tolerance tuning, damping, geometry or branch change was attempted.

Remaining: determine a lawful causal correction within this slice, preserve all failing tests, then complete affected lifecycle/save/face-sweep/static/compiler evidence and independent Auto review. Only thereafter run the final full local round, hooks, committed-SHA approval and publication. Browser/performance remain explicitly pending.

Final clean incremental rebuild after deleting temporary diagnostics: session93580, success,0 warnings/errors,6.21s. Source search confirms no BlockObservation, BlockRegularization, SolveNormalRow, ApplyRestitutionRow or NormalMass remains in the affected production/test closure. git diff --check passes. No final full suite or qualification claim.

### Final support-window correction (coordinator instruction)

The new flat-support test now uses the established **4s settling plus6s every-tick observation**; the earlier2s+1s checks remain diagnostic history only. No body, mass, drop, physical tolerance or solver changed. Matching incremental compilation plus only the40-case support class ran in session16592: **36 passed,4 failed,0 skipped** (test duration4s).

The same four narrow faces fail the support-height assertion at the start of observation. Expected centre Y is [-0.360500008,-0.359500021]m (bench plus0.1m half-height, ±0.5mm):
- mass0.25kg/drop2mm: Y=-0.437348694m;
- mass8kg/drop2mm: Y=-0.714289606m;
- mass0.25kg/rest: Y=-0.452970117m;
- mass8kg/rest: Y=-0.560559452m.

Thus the corrected standard-window check also fails; the initial early-window drift is not the sole evidence. Other36 shape/scale/mass/drop combinations pass. Rod and existing Weight failures remain independently unresolved. Final source search confirms temporary BlockObservation is absent; git diff --check passes. Spec remains in-progress for coordinator verification/HALT, not done.

## Auto Run Result

**HALT — step03: implementation verification failed.** This closes the current failed implementation attempt; it does not declare the overall backlog goal blocked. Intent, acceptance and earlier observations remain intact. The uncommitted candidate and red tests remain available for inspection.

The analytically justified normalized-rank correction from1e-3 to64*2^-24 is implemented, with matching current-rule documentation. It restores the real thin-face support mode; no scene threshold, residual allowance, accumulated-impulse rule, ordering, geometry or damping changed.

Final matching incremental build, session **17524**, succeeded with0 warnings/errors in6.34s:
```sh
dotnet build CuriousContraptions.tests/CuriousContraptions.tests.csproj -c Debug --no-restore -noAutoResponse -p:TwoDogAutoImport=false --nologo
```

Retained implementation verification, session **62738**, returned **50 passed,10 failed,0 skipped** in7.8112s:
```sh
dotnet test CuriousContraptions.tests/CuriousContraptions.tests.csproj -c Debug -f net10.0 --no-build --no-restore -noAutoResponse --filter 'FullyQualifiedName~WorkshopContactBlockTests|FullyQualifiedName~EveryRodRimImpact|FullyQualifiedName~AStackEndDownHoldsWithoutBounceOrCreep' --logger 'console;verbosity=normal' -- RunConfiguration.TreatNoTestsAsError=true
```

- **Fixed:** all40 support cases pass under the established4s settling plus6s observation contract, including all formerly failing thin faces.
- **Still failing:** nine of18 rod rows, the same release/azimuth combinations at every cadence. At240Hz the second-impact retentions remain0.8514348195923744 versus0.848874649862072 (10.5°/22.5°),0.8523219411276318 versus0.8483997198065828 (12.5°/0°), and0.8571401276083823 versus0.8483290195063815 (12.5°/45°). The earlier zero-discard probe ruled out numerical continuation for that observed trajectory; it did not establish a complete physical cause or justify changing the soft/speculative law.
- **Still failing:** existing Weight stack4kg/4kg, body2 movement0.5049187mm versus0.5mm allowed. The8kg/1kg stack control passes. Its physical cause remains unresolved.
- **Coverage gap:** EveryRodRimImpactObeysTheInelasticOneSubstepRetention asserts only completed rebounds at or above1 degree and requires at least three. Immutable intent requires each rim impact. Smaller impacts are unqualified; passing rows do not establish the full per-impact requirement. Tests remain unchanged in this failed frozen attempt.
- Earlier seeded-energy/control observations predate the rank-cutoff correction. They remain historical scoped proof, **not final qualification** of the corrected candidate.
- Coordinator confirmation session28993 has a missing handle and unavailable output. It is **not evidence** and supplies no pass/fail claim.

The failed implementation did not proceed to Auto review, a full local round, hooks, commit, push or publication. No further tests ran while closing this record. Matching compiler-log/static qualification, independent review and publication remain incomplete. Browser/performance remain at their named later gates. A future retry needs a bounded causal correction for rod retention and Weight-stack motion under unchanged acceptance, followed by required verification; do not publish this candidate.
