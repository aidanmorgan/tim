---
title: 'Retry the contact block after support coverage and orientation'
type: 'bugfix'
created: '2026-10-27'
status: 'blocked'
baseline_revision: bb35e8c4dc0a9b8f609dcc7e786223a0216e11af
review_loop_iteration: 0
followup_review_recommended: false
context: [AGENTS.md, docs/testing-guidance.md, docs/delivery-workflow.md]
warnings: [oversized]
deferred: []
---

**Historical record:** superseded by [the owner-approved game-grade completion spec](spec-engine-cap-block-game-grade.md). The immutable intent and failed results below remain evidence; they are not current acceptance or additional prerequisites.

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

## Current retry context

Fresh invocation from published main `bb35e8c4dc0a9b8f609dcc7e786223a0216e11af`; do not dispatch the old blocked workflow or import its stale current documents. Preserve original intent above. Selectively carry the seven bounded candidate source/test hunks; this is not restoring or merging the old worktree. Published support coverage and tick-local orientation now resolve later prerequisites. Their combined effect on the retained cap failures is untested.

Old candidate is read-only evidence at sibling `../story-engine-cap-block-retry`, baseline `822317c99a08ec64ce2d7406090036d51c1c8fa2`. It last passed 40 support cases but failed nine of 18 rod rows and one of two Weight-stack rows (50/10/0). That result predates support reduction and orientation. First repeat exactly that bounded 60-case check, before broad checks or changing any physical assertion.

Known oracle uncertainty: the one-substep rod allowance assumes an upper bound on old-pivot support impulse/duration not established by the soft/speculative law. This is a derivation gap, not proof that the old failures were test errors. Preserve the current gate while investigating. Do not add a fitted margin, support cap, damping or revised model merely to turn the check green. The old per-impact test also excludes rebounds below one degree despite “each impact”; this remains an explicit coverage gap to resolve, not accepted completion.

## Code Map

- `CuriousContraptions.Simulation/WorkshopConstraintSolver.cs`: carry candidate principal-set solver, transformed soft equation, normal/restitution consumers and deletion of regularization/sequential row recovery. Preserve current contact geometry, soft branch, clamp, central friction and restitution energy limit. Old candidate normalized pivot cutoff is 64*2^-24, not its superseded 1e-3.
- `CuriousContraptions.Simulation/WorkshopCollisionKernels.cs`: delete only dead ContactPoint.NormalMass field from old delta; preserve current TickStartRotation/TickRelativeRotation and spatial ReduceSideManifold including cleared mask.
- `CuriousContraptions.tests/CuriousContraptions.tests.csproj`: add only WorkshopContactBlockTests registration.
- `CuriousContraptions.tests/WorkshopContactBlockTests.cs`: 40 support shape/scale/mass/drop scenes, 4-second settling plus 6-second every-tick observations. Strengthen any missing coordinate-resolution/angular direction coverage using current policy, without changing scene bounds.
- `CuriousContraptions.tests/WorkshopPhysicsLawCylinderTests.cs`: carry no-margin settling and 18 per-impact rod cases only. Preserve published orientation, tiny/free/stationary controls, full-window original 200 drops and published skew-test cleanup.
- `CuriousContraptions.tests/WorkshopPhysicsLawRestitutionTests.cs`: carry production spinning-face/clear control replacement of deleted private fallback expectations; keep analytic flat bounce and dynamic support/energy tests.
- `CuriousContraptions.tests/WorkshopPhysicsLawTests.cs`: carry scalar soft/band expectation changes justified by deleting regularization only; preserve published +Z physical residual comparison.
- `CuriousContraptions.tests/WeightTests.cs`: unchanged AStackEndDownHoldsWithoutBounceOrCreep 0.5 mm bound, both mass cases; preserve published replay/rest tests.
- `WorkshopPhysicsEngine.cs:732–801`, `WorkshopConstraintSolver.cs` warm start/application/normal solve/restitution, `WorkshopFrictionSolver.cs:134–155,204`: if needed, temporary observational instrumentation for actual signed impulses and fixed-pivot momentum. Keep runtime arithmetic/order unchanged; remove instrumentation before acceptance.
- `docs/gpu-f32-physics.md`, `_bmad-output/planning-artifacts/epics.md`, `TODO.md`, sprint-status/deferred-work: selectively reconcile the current block rule/status while preserving all source IDs, angular/energy/Weight outcomes, multi-collider/bumper-cap/face-marking order and later browser/performance gates.
- `_bmad-output/implementation-artifacts/spec-engine-orientation-levelling.md`: append the short terminal predecessor receipt below, not another receipt-only commit. Warmup/support coverage receipts already exist.
- `_bmad-output/implementation-artifacts/evidence-engine-cap-block-after-orientation/`: raw retry outputs and preserved prior-attempt spec. Historical failure records are evidence, never current instructions.
- REQ-06/07/09/14 remain; no repository-wide numeric or browser qualification claim.

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

If the same rod failure persists, use one bounded diagnostic: 12.5 degrees / 45-degree azimuth, second impact at 240 Hz, and the prior passing 12.5 / 22.5 control. Account for actual signed applications including normal/friction warm start and unloading, not only positive accumulator sums. About one fixed world-space new-rim pivot, separate normal/friction/gravity impulses, integration changes and non-rocking energy. Report whether measured old-pivot duration/impulse violates the model assumption and whether momentum/energy accounting closes. If it does not establish a lawful correction under unchanged acceptance, return the evidence and precise unresolved decision to the coordinator. Do not perform another blind numerical retry.

Baseline energy proof can be reused from the exact published predecessor's original full local TRX at `/private/tmp/tim-orientation-retry-gate.NdBkqB/results`: session 99807 passed 4,453/0/0 with unchanged current baseline source, SDK10.0.402/runtime10.0.13 and Node26.12.1. Coordinator read the original two FreshSeedDrops outputs: bounds Dead2.191 mJ, Rolling2.114 mJ, Lively1.991 mJ; all six classes zero of200 failures. Preserve printed rounding precision and compare control behavior as well as candidate pass/fail. Do not rerun an identical baseline energy sweep.

Predecessor terminal receipt: SHA `bb35e8c4dc0a9b8f609dcc7e786223a0216e11af`; successful production run40365292840, build121721224177, deploy121728177566; artifact12670871781 (80,805,094 bytes); deployment7338621740, success status20647610025. Fresh verifier confirmed six HTTP200 static paths equal artifact contents: /, /index.html, /404.html, /godot.wasm, /godot.pck, /_framework/dotnet.js. Full local4,453/0/0; normal hooks126/0/0. No GitHub tests; browser/performance pending.

## Tasks & Acceptance

**Execution:**
- Apply the seven selected hunks after Anvil validation to this worktree only. Build the affected Debug closure and run the 60-case check below. Preserve published integration/reducer and all earlier raw failures.
- If checks fail, retain red assertions and diagnose the named bounded physical cause. If no causal correction is established, report evidence without broad/full runs, no changed bounds and no publication.
- If current failures are resolved, complete every matrix row through production interfaces. Close the sub-one-degree impact coverage gap with independently derived meaningful observations; never suppress small events by an output-selected threshold.
- Run affected support/hollow/cylinder/restitution/recovery/friction/Weight/F32 identity/lesson/lifecycle checks; compare unchanged energy gates to baseline controls. Remove temporary diagnostics and rebuild changed closure only.
- Update current contract/status/evidence and predecessor terminal receipt. Do not copy old TODO/epic/deferred/A9 wholesale.
- Return to coordinator for five Auto layers/repairs, final matching compiler captures/full local round, normal hooks, exact-SHA approval and production publication. Implementer does not commit, push or run the full round.

**Acceptance Criteria:**
- Given current declared cap/face/line/point/near-dependent cases, when the one enumerator advances production simulation, then all physical support, finite motion and positive/control/boundary observations pass with no legacy recovery path.
- Given the complete rod release/cadence matrix, when every impact and settling window is observed, then the source no-margin inelastic model and no-toppling/righting criteria pass; no unresolved excluded-small-impact gap is accepted.
- Given original energy/heading/Weight and published orientation/hollow cases, when the combined candidate runs, then bounds, seeds and windows hold and shifted control energy does not conceal a regression.
- Given current supported Reset/construction save/load and fresh/reused engines, when inputs replay, then typed physical state/ticks/events restore without stale solver state.
- Given independently accepted local verification, when published, then full local tests/hooks have zero failures/skips and exact-SHA production artifact/deployment match; GitHub repeats no tests.

## Spec Change Log

### Bounded Weight correction — 27 October 2026

The two-row motion check retained all assertions and reproduced 1 pass / 1 failure / 0 skipped. Equal-mass top-origin path is 0.540668368 mm (horizontal variation 0.493501751 mm; vertical 0.141322613 mm); COM path is 0.671059239 mm, with only 0.0222692/0.0048280/-0.0027112 mm net displacement. Maximum interbody spin difference is 4.123e-8 rad/s despite peak spin 0.001845 rad/s: coherent whole-stack rocking is real, not merely an offset-origin observation artifact. Raw reads: `/private/tmp/tim-cap-after-orientation-weight/weight-motion.trx`.

The fixed-window trace (committed ticks 580–588, substeps 2317–2352) also reproduced 1/1/0. At substep 2332 the top manifold changes feature membership from [8224,8226,8228,28710] to [8226,8228,28704,28710]. New +X feature 28704 has zero warm load while surviving -X feature 8228 retains 0.040865447 N·s. Normal warm start produces opposite z angular increments +0.06312688/-0.06312706 rad/s. Bench membership is stable. Subsequent coupled sweeps leave coherent z spin approximately -0.00097339 rad/s. About fixed pivot (0,-0.46,0), combined H_z changes -0.004950008 N·m·s: bench normal -0.004941673, gravity -0.000009603, bench friction +0.000001270; internal top-pair moments cancel; closure 7.8e-10. The 8/1 control keeps stable membership and momentum changes near 1e-8. Raw original trace: `/private/tmp/tim-cap-after-orientation-weight/weight-contact-trace.trx`. These observations establish the specific partial-cache excitation; they do not qualify an untested fix.

Coordinator authorized one causal correction: normal warm-start reuse requires exact complete manifold feature membership, independent of enumeration order, using the same sorted collider-pair/feature-set identity already used by friction. Rename that key and pair type generically, with no old aliases. A changed set initializes every normal accumulator to zero before any application; unchanged sets retain their existing per-point loads. The engine rebuilds membership alongside its caches each substep and clears it on Reset. This is one uniform cache-validity rule and the same active-set solver, not another contact law, solver fallback, fitted threshold or changed physical gate. No broader moving-point invalidation is inferred from this trace.

Counterfactual scope: matching incremental build, only the existing two Weight rows with the original trace window and unchanged 0.5 mm assertions. After they passed, the coordinator replaced the planned clean two-row repetition with the original 60-case selection, because normal-cache membership also affects rod/support contacts. No broad/full run or publication. Rod acceptance and the pending owner decision remain unchanged.

## Review Triage Log

## Verification

First incremental Debug build of `CuriousContraptions.tests/CuriousContraptions.tests.csproj`, with fresh restore if needed; -noAutoResponse, no Release worker/web publish. Then:

```sh
dotnet test CuriousContraptions.tests/CuriousContraptions.tests.csproj -c Debug -f net10.0 --no-build --no-restore -noAutoResponse --filter 'FullyQualifiedName~WorkshopContactBlockTests|FullyQualifiedName~EveryRodRimImpact|FullyQualifiedName~AStackEndDownHoldsWithoutBounceOrCreep' --logger 'console;verbosity=normal' -- RunConfiguration.TreatNoTestsAsError=true
```

Expected60/0/0; a failure triggers bounded diagnosis, not wider verification. Reuse matching builds for subsequent affected checks. Every matrix row must have executed passing coverage before review. No hashes/byte identity assertions, skipped game tests, mutants or browser.

Coordinator final: actual complete diff and matrix inspection, five Auto layers with same implementer repairs/independent confirmation; five matching compiler captures and one full local suite at the end under local-test-authority, generated browser deferral only; normal hooks then exact-SHA approval/push/terminal verification. Record exact commands/totals and original evidence paths.

## Retry result — 27 October 2026

Status: blocked, unapproved candidate. The seven selected source/test deltas were applied to published baseline `bb35e8c4dc0a9b8f609dcc7e786223a0216e11af`, retaining its orientation integration, side-manifold reduction, original seeds/windows and Weight assertions. [Prior attempt](evidence-engine-cap-block-after-orientation/prior-attempt.md) remains historical.

Initial incremental Debug build (session 8005) succeeded with 0 warnings/errors in 18.84 s. The exact 60-case command above (session 4003) returned exit 1: **50 passed, 10 failed, 0 skipped**, 8.2422 s. [Original output](evidence-engine-cap-block-after-orientation/first-60.log).
- All 40 support cases passed; no new coordinate-resolution/angular-direction strengthening was performed after this red gate.
- Nine rod rows failed at the second impact: 12.5° at azimuths 0° and 45°, and 10.5° at 22.5°, each at 60/120/240 Hz. At 12.5°/45°/240 Hz, retention is 0.8570483314325438 against 0.8483291037505862.
- Equal-mass Weight stack (4 kg/4 kg) fails its last-second accumulated path-length bound: body 2 travels 0.54066837 mm against 0.5 mm. The 8 kg/1 kg control passes. Weight source and bounds remain unchanged.
- The sub-one-degree impact exclusion remains an unresolved coverage gap. No physical assertion was relaxed.

### Bounded signed-impulse diagnosis

Temporary observational callbacks captured production state before/after each actual normal and friction impulse, including warm starts and negative unloading, friction torque, gravity, integration and substep end. No arithmetic/order or acceptance assertion changed. A temporary two-row diagnostic drove only 12.5° at azimuths 22.5° and 45°, 240 Hz, for 430 ticks. Diagnostic build session 53518 passed 0 warnings/errors (6.59 s); diagnostic session 37584 passed 2/0/0. This is observation execution, not physical acceptance.

Exact diagnostic invocation: `dotnet test CuriousContraptions.tests/CuriousContraptions.tests.csproj -c Debug -f net10.0 --no-build --no-restore -noAutoResponse --filter 'FullyQualifiedName~TemporarySignedRodDiagnostic' --logger 'trx;LogFileName=signed-rod.trx' --results-directory /private/tmp/tim-cap-after-orientation-diagnostic -- RunConfiguration.TreatNoTestsAsError=true`.

The original full trace remains at `/private/tmp/tim-cap-after-orientation-diagnostic/signed-rod.trx` (both rows, substeps 500–850). The removed callback/test replacements and numeric stage legend are preserved in [diagnostic instrumentation](evidence-engine-cap-block-after-orientation/diagnostic-instrumentation.md). The exact selected raw rows are preserved in [signed-trace.csv](evidence-engine-cap-block-after-orientation/signed-trace.csv); [accounting output](evidence-engine-cap-block-after-orientation/signed-momentum.txt) records all quantities below.

Windows were selected from first approaching-side impulse through the last transition impulse before ordinary support resumes: substeps 691–694 for 22.5°, 693–696 for 45°. Both span four 1/480 s substeps (8.333 ms), not the model's one substep. Observed sign crossings occur at substeps 692 and 695 respectively. The 45° transition already loads transverse cap points at 693/694; the radial-direction threshold only engages at 695 (initial tilts -0.004128, -0.002169, -0.000229, +0.001683 rad at 693–696). A threshold-only correction is therefore not established.

Each budget uses one fixed world new-rim pivot throughout: first true new-rim application with projected arm >0.09 m and normal impulse >0.025 N·s in the selected window, projected vertically to the bench y=-0.46 m. These selectors identify the trace event, not an acceptance tolerance. The resulting pivots are (0.010300212,-0.46,0.006259330) for 22.5° and (0.007880373,-0.46,0.007880750) for 45°. All preceding transverse applications are included about that same fixed point.

Diagnostic derivation, using double analysis only: for axis a, velocity v, angular velocity w, centre x, fixed pivot P and rocking direction t=Y×(cos azimuth,0,sin azimuth), Iw=Ix*w+(Iy-Ix)*a*(a·w), Ix=r²/4+h²/3, Iy=r²/2, m=1. Angular momentum H=Iw+(x-P)×v. Every signed force impulse contributes ((x+arm)-P)×impulse; torque impulses contribute directly; gravity acts at x. Integration and intervening state changes are accounted separately from impulses. Reported scalar moments are dot products with t. The nominal decimal inertia calculation differs slightly from production f32 rounding; these small diagnostic closure residuals are not solver certificates.

For 45°, expected signed moments (N·m·s) are transverse normal +0.01121146581, new-side normal +0.00198104415, old-side normal +0.00001457780, gravity -0.00821458614 and friction +0.00000151971. Net observed H increases by 0.00499402215; accounting closes to 1.24e-9, with maximum individual application discrepancy below 1e-8. Control H decreases by 0.00821321888; accounting closes to 1.28e-8.

Old-side signed normal impulse is approximately zero after unloading in both windows. Counting positive old-side applications alone would incorrectly report 0.047022667 N·s for 45° and 0.018870320 N·s for the control. The trace does not demonstrate an excess net old-pivot support impulse; it demonstrates additional transverse/support-site moments and a multi-substep transition absent from the one-substep idealization. The model's support/duration assumption is not established for this soft/speculative law.

Mechanical energy K+g*(y-bench-h) decreases from 0.099330877 to 0.086966508 J for 45°, and 0.098965058 to 0.076918338 J for the control. Decomposing K into H_t²/(2*I_pivot) and the residual, where I_pivot=t·I*t+|x-P|²-((x-P)·t)², gives residual non-rocking K of 0.016438495→0.000000209 J and 0.016651042→0.000009413 J respectively. The raw accounting separately records normal/friction/gravity energy changes and potential change during integration. Non-increasing energy does not satisfy or relax the stricter unchanged rod retention gate.

All temporary callbacks and the temporary diagnostic test were removed. Matching clean-candidate incremental Debug rebuild session 59789 passed 0 warnings/errors in 5.82 s. Its exact restored source is the candidate tested by the 60-case check; no unchanged test was repeated. No broad/full run, compiler-proof round, mutation, browser, Release worker/web publish, commit or publication occurred.

### Unresolved decision and next check

Coordinator owns the remaining rod contact-model/geometry decision under unchanged no-margin acceptance. The initial Weight regression is now corrected in scoped native proof below, still unreviewed. The sub-one-degree impact coverage gap remains open. Any rod acceptance change requires the pending owner decision; no such change has been applied. Remaining scale/coordinate/angular coverage, lifecycle/energy affected checks, independent Auto review and publication are incomplete. REQ-06/07/09/14 remain obligations; no global/browser qualification is claimed.

## Auto Run Result

**HALT: blocked / implementation verification failed.** The causal normal-cache correction fixes the Weight defect in scoped native checks, but nine rod assertions still fail. The immutable intent, baseline and all physical bounds remain unchanged; historical 50/10/0 evidence is preserved.

- Read-only singleton-branch check: a singleton sets SoftPoint only when s<=0; NormalBranch consults that flag only in its s>0 band branch. The common enumerator therefore preserves the old singleton branch semantics.
- Motion-only instrumentation build 35274: 0 warnings/errors, 8.13 s. Existing two-row test session 62434: **1 passed / 1 failed / 0 skipped**, original equal-stack 0.54066837 mm failure. Raw `/private/tmp/tim-cap-after-orientation-weight/weight-motion.trx`.
- Fixed-window runtime-observer build 98169: 0 warnings/errors, 5.73 s. Session 36609: **1/1/0**, same assertion failure. Raw `/private/tmp/tim-cap-after-orientation-weight/weight-contact-trace.trx`.
- Uniform whole-manifold membership correction build 36488: 0 warnings/errors, 5.78 s. Same two-row/window session 65630: **2/0/0**. Raw `/private/tmp/tim-cap-after-orientation-weight/weight-membership-correction.trx`. Equal-stack maximum substep-ending spin over the fixed window falls from 0.00192032115 to 3.24633311e-8 rad/s; maximum speed from 0.00220594146 to 4.37048602e-5 m/s. The 8/1 control maxima remain unchanged at 2.69620939e-8 rad/s and 9.82057920e-9 m/s. These causal trace measurements are not substitute acceptance.
- All temporary output/callbacks removed; WeightTests.cs restored exactly. Matching clean incremental Debug build **62292**: 0 warnings/errors, 5.88 s. Clean original 60-case session **64262**: **51 passed / 9 failed / 0 skipped**, exit 1. All 40 support rows and both Weight rows pass unchanged assertions; same nine rod matrix rows fail at impact 2. At 12.5°/45°/240 Hz, retention is 0.8571061259480253 against 0.8483290616305793. Raw `/private/tmp/tim-cap-after-orientation-weight/clean-membership-60.trx`. No separate clean two-row run was repeated.

Exact final failing command:

```sh
dotnet test CuriousContraptions.tests/CuriousContraptions.tests.csproj -c Debug -f net10.0 --no-build --no-restore -noAutoResponse --filter 'FullyQualifiedName~WorkshopContactBlockTests|FullyQualifiedName~EveryRodRimImpact|FullyQualifiedName~AStackEndDownHoldsWithoutBounceOrCreep' --logger 'trx;LogFileName=clean-membership-60.trx' --logger 'console;verbosity=quiet' --results-directory /private/tmp/tim-cap-after-orientation-weight -- RunConfiguration.TreatNoTestsAsError=true
```

The three preceding bounded Weight runs use the same project/configuration/no-build flags, filter `FullyQualifiedName~AStackEndDownHoldsWithoutBounceOrCreep`, quiet console logger and the respective TRX filename listed above. Their raw original outputs remain outside the checkout; no duplicate log package is created.

Weight trace schema: MARK identifies warm/solve/relax iteration; C contains substep, body pair, feature, world point xyz, normal xyz, gap and pre-application accumulated normal load; P contains substep, body pair, member count, centre-of-mass-relative patch anchor xyz, radius, normal xyz, followed by its member C records. S contains substep, numeric stage, body id, centre-of-mass xyz, quaternion xyzw, linear velocity xyz, angular velocity xyz, applied impulse/torque xyz and arm xyz. Stage mapping is 0 Begin, 1 Gravity, 2 IntegrateBefore, 3 IntegrateAfter, 4 End, 5/6 NormalBefore/After, 7/8 FrictionBefore/After, 9/10 TorqueBefore/After. Before/after pairs measure actual applied changes; count signed impulses only once. Use the fixed-pivot H formula documented above, with each Weight's declared mass/radius/half-height, summing both bodies; internal pair moments cancel. The reported observation decomposition parses shortest round-trip text back to f32 before double analysis.

Current correction ownership: engine normal membership state is rebuilt each substep and cleared on Reset. ContactFeatureSetKey/ContactColliderPair replace the former friction-only type names completely, sharing exact sorted feature-set semantics without aliases. No solver, tolerance, branch, contact geometry, ABI or save-format change accompanies this fix. Current checks find no diagnostic code, old type names or whitespace error in the working diff. Broader affected/lifecycle/energy proof, full local/compiler round, independent Auto review, hooks and publication remain incomplete. No files were committed or staged by the implementer.
