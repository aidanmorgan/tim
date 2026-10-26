---
title: 'Preserve small rotations within each simulation tick'
type: 'bugfix'
created: '2026-10-26'
status: 'blocked'
baseline_revision: 822317c99a08ec64ce2d7406090036d51c1c8fa2
review_loop_iteration: 0
followup_review_recommended: true
context: [AGENTS.md, docs/testing-guidance.md]
warnings: [oversized]
deferred: []
---

<intent-contract>

## Intent

**Problem:** The published integrator repeatedly rounds a body's absolute quaternion during a tick while independently integrating translation. The current 200 lying-drop scenes expose 73 cases whose resting movement exceeds the already adopted game-grade displacement allowance. Orientation levelling is the next independent engine slice after published central friction; the failed cap-block candidate is not a prerequisite or input.

**Approach:** Preserve small rotation increments within each committed tick through one general integration rule. Restore the named tilted-body and random-drop behavioural coverage while retaining energy, physical response and lifecycle contracts.

## Boundaries & Constraints

**Always:** Use f32, existing declared geometry/materials, 480Hz substeps and supported60/120/240Hz cadences. Keep coherent contact branches, normal/friction/rolling laws and restitution energy limits. Every tick starts solely from its committed source and declared inputs. Retain exact typed identities, Reset/same-input replay and supported save/load. Current resting acceptance is peak horizontal excursion from the original settled reference on every tick: min(50µm,max(5µm,1e-4*nominal horizontal size)), with coordinate spacing at most allowance/8. This existing owner-policy adoption supersedes historical1e-9/1e-6m/s resting pins; preserve scene seeds/windows, angular/penetration/speed and independent energy bounds. Finite residuals continue, not tick faults.

**Never:** Add a rest/velocity snap, damping, output-derived tolerance, double game arithmetic, per-element branch, alternate integration mode, fallback or compatibility path. No persistent hidden remainder or body ABI/save change. Do not import unpublished cap-block code or the rejected historical compensated-remainder implementation. No browser/mutation tests, local Release worker build or web publish.

## I/O & Edge-Case Matrix

| Scenario | Input/state | Expected behaviour | Handling |
|---|---|---|---|
| Tilted support | Lying cylinders1e-5/1e-4rad, box on end1e-4rad, original dimensions/masses/materials | Resting excursion within current allowance at4–10s; tilt within original1e-5rad and actual admitted tilt observed | Same integrator |
| Existing controls | Exactly-level cylinder1–10s;2e-6 microtilt2–10s | Existing displacement and angular nongrowth remain; no artificial motion | Preserve tests/windows |
| Random drops | Original200 seeds92000–92003,50 each; same RNG draws/scenes | Every-tick4–10s excursion within allowance; final tilt<1e-5rad, original support/penetration and<2mm/s speed | Finite advancing committed ticks |
| Genuine small/free motion | Existing slow translation controls plus free angular motion around a nontrivial starting orientation | Analytic motion remains observable; rotation composition has correct world-space order and normalized quaternion | No quantization threshold added |
| Cadence | Named tilted scenes at60/120/240Hz | Same physical bounds/windows; tick-local scratch cannot leak across ticks | No cadence-specific algorithm |
| Single-point curved side | Tilted cylinder rocking on bench; Crr0 versus.1, otherwise equal | Pitch rocking is not damped as axial rolling; existing actual rolling control still slows | One existing geometry-derived rolling-axis rule |
| Lifecycle | Tilted construction run, Reset, replay, supported Save/Load, reused engine versus fresh engine | Typed physical observations/ticks/events repeat; no stale delta or cross-body state | Reject invalid inputs atomically as before |
| Energy/interactions | Existing material energy/drop, friction ramp, cap/face restitution, Weight and identity scenes | Original budgets, seeds, heading and gameplay outcomes remain | No widened control or energy allowance |

</intent-contract>

## Code Map

- `CuriousContraptions.Simulation/WorkshopPhysicsEngine.cs`: Advance~209 copies committed source; body read~285–328 initializes pooled bodies each tick. Integration~740–790 currently multiplies dq by the previously rounded absolute quaternion each substep, integrates COM, then restores pose/zeros motion if nonfinite. Dynamic frames update before relax. This is the single affected integration path.
- `CuriousContraptions.Simulation/WorkshopCollisionKernels.cs`: SimBody~19–85 already owns transient Cm0/Q0 for substep contact separation. Keep those semantics. Add only clearly named tick-local scratch if needed; initialize it at each Advance, not just pool allocation.
- `CuriousContraptions.Simulation/WorkshopConstraintSolver.cs`: PrepareRolling/RollingAxis~205–264 projects cylinder-axis support into the tangent plane even for a singleton. Sphere/crossed-cylinder contact rolls about both tangents. CurrentSeparation uses existing substep Q0; do not repurpose it.
- `CuriousContraptions.tests/WorkshopPhysicsLawCylinderTests.cs`: existing small-tilt/control~379–428, rocking~432, seeded lying drops~2290–2340. Restore the larger tilted cases and add RestingObservation to the original200-case loop without duplicating scenes or RNG draws. Retain original angular/support/speed checks.
- `CuriousContraptions.Abstractions/PhysicalTolerance.cs` and RestingObservation in `CuriousContraptions.tests/PhysicalToleranceTests.cs`: reuse current rest policy, independently stated literals/derivations, coordinate-resolution check and fixed original reference.
- Temporary investigation `CuriousContraptions.tests/OrientationInvestigation.cs` and its Compile registration are measurement-only: fold the required assertions into existing behavioural tests and remove the temporary diagnostic. Retain original raw evidence at `_bmad-output/implementation-artifacts/evidence-engine-orientation-levelling/investigation-random200.log`; do not turn its success into acceptance.
- Existing LawScene/LawWorld production builders, Workshop lifecycle/save tests, friction/restitution/Weight/F32BehaviourIdentity/LaunchLesson classes: affected consumer proof. New files require explicit Compile entries in `CuriousContraptions.tests/CuriousContraptions.tests.csproj`.
- `docs/gpu-f32-physics.md`: orientation and Resting Drift rows; describe actual tick-local integration and qualified bounds only after passing proof. `docs/testing-guidance.md`: existing float/rest policy remains authoritative.
- `_bmad-output/planning-artifacts/epics.md`: engine-orientation-levelling~186; explicitly depends on published engine-two-point-friction. Reconcile obsolete speed wording with already adopted displacement policy; preserve angular/energy/lifecycle criteria. No mutation counterfactual is required under later owner policy: use production material controls and analytic torque comparison.
- `TODO.md`, sprint-status.yaml, deferred-work.md and isolation A9: update bounded status and carry the published warmup receipt. REQ-06/07/09/14 remain.
- Historical `spec-engine-orientation-levelling-draft.md` is diagnosis only. Its persistent compensated remainder failed Dead energy4.109mJ versus2.043mJ and required a body-record decision. Do not restore its implementation or obsolete acceptance.
- Prior cap retry is retained uncommitted in sibling worktree story-engine-cap-block-retry, HALT implementation verification failed: support40 pass after rank correction; rod9 failures and Weight4/4 movement0.5049187mm>0.5mm remain. Import no runtime/test changes. Record this bounded status without claiming it is solved.
- Warmup predecessor: published822317c99a08ec64ce2d7406090036d51c1c8fa2; production run40324744448, artifact12650828689(80,805,681bytes), deployment7332347579/status20623823471; six static endpoints200/artifact-equal; full local4411/0/0, hooks126/0/0. Carry short receipt in its existing spec; browser/first-use performance pending.

## Tasks & Acceptance

**Execution:**
- In `CuriousContraptions.Simulation/WorkshopPhysicsEngine.cs` and transient `SimBody` in `WorkshopCollisionKernels.cs`, implement tick-relative rotation and delete repeated absolute-quaternion accumulation. Preserve source/candidate rejection and transient ownership.
- In `CuriousContraptions.tests/WorkshopPhysicsLawCylinderTests.cs` and the existing Workshop command/lifecycle test files, cover every matrix row through production interfaces; no private scratch-field assertions or committed output identity/hashes.
- Refresh `docs/gpu-f32-physics.md`, `_bmad-output/planning-artifacts/epics.md`, `TODO.md`, sprint-status/deferred-work and the predecessor warmup spec; preserve all requirements and pending later qualifications.
- Verify affected native checks, five Auto review layers and repairs; only then one final full local round, normal hooks, exact committed-SHA approval and publication.

**Acceptance Criteria:**
- Given each named/resting/random scene, when real production simulation advances, then current displacement and original tilt/support/speed bounds hold without suppressing genuine slow motion.
- Given unchanged energy and interaction scenes, when the integration changes, then no physical budget or control is relaxed and no regression remains.
- Given fresh/reused engines and supported lifecycle commands, when the same inputs replay, then typed physical state, events and ticks match exactly without hidden persistent state.
- Given the independently reviewed candidate, when published, then local tests/hooks pass with zero skips and exact-SHA production build/artifact/deployment are verified; browser/performance qualification stays pending.

## Spec Change Log

## Review Triage Log

### 2026-10-26 — Review pass 0

- Verdicts: 16 findings — high 1, medium 5, low 4, false 6, maybe-false 0.
- Layers: orientation_blind_r0 (10), orientation_edge_r0 (0), orientation_verification_r0 (0), orientation_intent_r0 (5 descriptive divergences), orientation_forward_r0 (1). All five launched before triage; capacity required two waves. The spec narrative was supplied separately to Edge after tracing and omitted from the blind review diff; the full candidate snapshot remains retained.
- Findings:
  - `B1` [medium] [patch] New Weight replay helper reaches F32Bits/HasSameBits output assertions. Replace new calls with typed numeric state comparisons; preserve exact replay without output-bit identity.
  - `B2` [low] [patch] One transformed vector leaves rotation about that vector unconstrained. Add a second independent basis-vector oracle.
  - `B3` [low] [reject] Constant-axis free rotation cannot isolate noncommuting incremental order. Current code left-composes world-space increments correctly; existing varying-contact integration/energy tests cover ordinary interactions. A dedicated controlled nonparallel-angular-input scenario would require substantial new setup for this speculative regression, not a direct correction.
  - `B4` [medium] [patch] Smallest new free-spin row does not exercise the retained finite-speed cutoff. Group with F1 and add near-identity tiny-spin/zero controls.
  - `B5` [false] [reject] DeclaredPrismaticDriveTests.AReusedEnginePreservesEveryBodyTrajectoryAndStaticSupportInTheNextScene explicitly reuses moving/spinning pooled slots as static supports and multiple dynamic bodies, comparing120 ticks. It ran in the390-test affected run.
  - `B6` [low] [patch] Replay agreement alone can preserve a wrong Weight outcome. Add direct current-envelope resting/support/tilt observations to the new package lifecycle scene.
  - `B7` [medium] [patch] Initial singleton geometry plus a later peak does not directly observe early singleton angular response. Add bounded early angular-pitch control, grouped with B8.
  - `B8` [medium] [patch] Maximum torque ratio is not by itself a derivation of a peak-angle difference. Retain torque geometry as context, derive early f32 comparison separately, and remove overstated peak-effect wording.
  - `B9` [medium] [patch] Both edited current plan views must retain the already named multi-collider/bumper-cap/face-marking outcomes before9A.44B.
  - `B10` [low] [patch] New status/contract prose concatenates words, identities and units. Direct spacing correction improves reviewability without changing behavior.
  - `I1` [false] [reject] Arbitrary cross-tick sub-ulp accumulation is not required by the owner's accurate-enough game-grade direction; current scope explicitly preserves finite-window displacement and angular bounds and makes no universal precision claim.
  - `I2` [false] [reject] Historical nanometre/speed resting criteria are superseded by the published26October physical-comparison policy; original angular/support/speed/energy requirements remain.
  - `I3` [false] [reject] WorkshopSimulation.Save rejects every phase exceptBuilding (WorkshopSimulation.cs223–229). Supported save/load is construction replay, not arbitrary running-state serialization; test observes the supported surface.
  - `I4` [false] [reject] Later no-mutation policy and forward-only law apply. Production Crr controls plus independent geometry/torque expectations qualify the intended singleton rocking response without a deliberately altered solver; no experimental two-law comparison is claimed. Early response evidence is strengthened underB7/B8.
  - `I5` [false] [reject] Review-stage candidate explicitly retains full local/hook/publication gates. Its current incompleteness is not a false done claim; coordinator will enforce remaining exact-SHA gates.
  - `F1` [high] [patch] Finite nonzero angular velocity at/below1e-9 takes a frozen-orientation rule. Replace threshold with the analytic zero-length coefficient inside one integration law; preserve rejection semantics. Confirmed forward-only finding cannot be deferred.

Repairs applied by the retained orientation_dev: always-compose analytic zero/underflow coefficient, seven zero/tiny-spin cases, two Rodrigues basis vectors, numeric typed replay plus actual Weight outcomes, early rocking response, complete plan order and readable prose. B4/F1 share one repair; B7/B8 share one repair. Accepted patch entries: high 1, medium 3, low 3. B3 was rejected as a low-priority additional test scenario requiring disproportionate new setup; six false findings were rejected for the specific reasons above. No deferred findings.

Retained independent confirmation: orientation_forward_r0 explicitly resolved F1 with no remaining concrete concern. orientation_edge_r0 confirmed all repairs and independently ran 14 affected tests in session 70751: 14 passed, 0 failed, 0 skipped, exit 0, 2.21 s, reusing the Debug build with --no-build --no-restore -noAutoResponse and TreatNoTestsAsError=true. A final comment documents the unchanged Rodrigues allowance as 5 µm surface displacement at radius 0.25 m; no behaviour changed after this confirmation. Coordinator restored two formatting-only changes inside the immutable intent to its exact captured text. Full local/compiler/hook/publication gates still remain.


## Design Notes

Current-baseline investigation (SDK10.0.402/runtime10.0.13, Darwin arm64): fixed scenes pass current envelope; random200 has73 outside allowance (batches15/18/19/21), zero coordinate-resolution or existing tilt/slop/speed failures. Maximum excursion87.6128615µm; worst ratio2.395603 at batch3/drop48 (51.56281µm versus21.523938µm).182 quaternions are frozen throughout4–10s. These are measurements, not new tolerances. Diagnostic four theories passed only because they collected measurements; no acceptance claim. Earlier fixed-case measurements and original raw sweep are retained by the investigation agent. Runtime source is unchanged.

One bounded candidate: at each Advance initialize startRotation from committed body rotation and relativeRotation=identity. At each substep compose its world-space dq on the LEFT of relativeRotation; derive the physical orientation by normalizing relativeRotation*startRotation. RelativeRotation retains tiny increments around identity within the tick, while geometry receives the actual composed pose every substep. Do not change COM integration or angular velocity, store across ticks, or reuse Q0. If the existing nonfinite candidate recovery rejects a substep, restore the previous relativeRotation alongside its previous pose. Pooled/static/dynamic reuse must initialize scratch deterministically.

This improves resolution across the tick's2/4/8 substeps; it does not promise arbitrary cross-tick sub-ulp accumulation or exact zero tilt. The acceptance is the stated game envelope. If the candidate fails, report a causal finding and preserve the failing test; do not add a persistent remainder or alternate algorithm silently. Body ABI changes remain a separately explicit owner decision.

Single-point rocking control: r.25,h.4,m1, initial pitch.01rad about X from lying along Z, lowest support h*sin(theta)+r*cos(theta), angular speed4rad/s about X; compare Crr0/.1 for1s. The low/high endpoint separation is about8mm, distinguishable from the speculative contact margin. Axial rolling torque is orthogonal to pitch; a two-tangent law would resist pitch by up to Crr*r*m*g=.245Nm versus gravity arm m*g*h=3.924Nm(6.25%). Derive the behavioural tolerance before observing peaks; retain existing rolling positive control. Geometry/production observations support the singleton claim, not a private-state certificate.

## Verification

Use the declared SDK/Node and incremental Debug affected builds, then reuse matching binaries with --no-build --no-restore -noAutoResponse and TreatNoTestsAsError=true. Run named matrix and affected consumer checks; baseline seeded energy/control observations must be captured before runtime changes if relative controls could otherwise hide energy regressions. No full baseline round.

After all review repairs and retained confirmation, capture five matching compiler logs and run the full local solution under docs/delivery-workflow.md#local-test-authority with only the explicit generated browser deferral. No game/tool tests run on GitHub. Implementer does not commit/push or launch the full round; coordinator owns final review/publication sequence.

## Implementation and affected verification — 26 October 2026

Implemented by the dedicated orientation implementer, without importing the sibling cap retry. Deleted repeated absolute-quaternion accumulation: each Advance resets TickStartRotation from the committed quaternion and TickRelativeRotation to identity for every pooled body; each world-space dq left-composes the relative rotation, then the actual pose is normalized relative*start. Nonfinite substep rejection restores previous relative scratch alongside the previous pose. Existing substep Q0/Cm0, COM integration, velocities, contacts, friction/rolling and restitution budgets remain unchanged. No ABI/save slot or persistent remainder was introduced.

Restored behavioural coverage in the existing cylinder tests: nine fixed tilt/cadence cases, original 200 drops with unchanged RNG plus every-tick displacement, four analytic free-rotation/reused-engine cases, and the existing rocking control extended to .01 rad single-end support. The box uses the policy's nominal local min(2hx,2hz)=.3 m, independent of its orientation. Initial 17-case proof used a stricter 20 µm box allowance; corrected to the actual 30 µm policy before the broader run, not selected from outputs. Existing level/microtilt, genuine slow translation, rolling-positive and angular/support/speed limits remain.

The singleton calculation is independent of solver-private counts: endpoint separation 2h sin(.01)=.00799987 m exceeds the initial speculative margin .004+4sqrt(.25²+.4²)/480+9.81/480²=.0079734 m. Its 26 µm clearance is resolved in f32. Crr 0/.1 rocking peaks satisfy the original 0.5% comparison, with direct first-commit angular-pitch agreement within 512 ULPs at 4 rad/s; the 6.25% ratio bounds torque rather than predicting a peak-angle effect; the geometry-derived axial rolling law is retained. No mutation was made.

The Weight command test runs a genuinely tilted package construction through Construct/Run/Advance/Reset/Save, then compares all 1200 committed ticks against Reset replay and a fresh decoded-construction run through existing typed state/event observations. Free rotation uses an isotropic sphere, Rodrigues' independent vector formula, a nontrivial starting orientation, quaternion normalization and exact fresh/reused typed trajectories.

Removed OrientationInvestigation.cs and its temporary Compile registration; the original investigation-random200.log remains unchanged. The temporary control-only diagnostic was also removed after its measurement. Warmup's supplied published receipt is carried in its spec, isolation A9, TODO, sprint and deferred views. The separate cap retry remains HALT with its stated rod/Weight failures.

### Evidence and commands

All file writes received Anvil allow after correcting any malformed patch input. SDK 10.0.402/runtime 10.0.13, Darwin arm64. Incremental Debug test-closure builds used:
`dotnet build CuriousContraptions.tests/CuriousContraptions.tests.csproj -c Debug --no-restore --nologo -noAutoResponse -v:q`.
Initial candidate build 43250 passed 0 warnings/errors; complete test candidate 39505 passed 0/0. The final post-diagnostic-removal build 33043 passed 0 warnings/errors in 6.10 s. No local Release worker build, web publish, full solution round, hook, commit, push, browser or mutation run was performed.

- Before candidate rebuilding, baseline energy session 5871 passed 3/0/0 in 2.5119 min; current original energy gates pass within the broader run. [Baseline output excerpts/current control-only measurement](energy-controls.log) preserve original report precision. Dead/Rolling control budgets match at captured 0.001 mJ precision; Lively falls from 2.151 mJ to 1.990610432 mJ. No control or energy expectation changed.
- Initial resting/tilt/rocking matrix session 86833 passed 17/0/0 in 15.77 s, including all original 200 random drops (baseline 73 outside allowance).
- [Affected native raw output](affected-native.log), session 90001, passed 390/0/0 in 8.122 min. Classes: cylinder laws, friction, restitution, Weight, F32BehaviourIdentity, LaunchLessonSolve and DeclaredPrismaticDrive; the log records the exact command. This includes energy/material seeds, original ramp/heading laws, existing reused-engine/static-body control, lessons and supported rejection/lifecycle behaviour.
- After removing the diagnostic and completing the singleton comment, session 70215 passed 16/0/0 in 3.3387 s against build 33043. Filter: `FullyQualifiedName~ATiltedBodyLevels|FullyQualifiedName~FreeAngularMotionComposes|FullyQualifiedName~TiltedWeightReset|FullyQualifiedName~ARockingLyingCylinder`; same test command options as affected-native.log. The broad runtime/tests remain unchanged except the explanatory comment; no repeated energy sweep was needed.

### Review handoff

Implementation is ready for the five independent Auto review layers. Coordinator retains the final matching five compiler captures, one full local solution round, normal hooks, committed-SHA approval and production build/artifact/deployment. REQ-06/07/09/14 remain; no terminal implementation Pass, global numeric qualification, browser or release qualification is claimed. Browser/performance proof remains pending at its named gates. The method only accumulates small increments within one tick; it deliberately makes no arbitrary cross-tick sub-ulp guarantee.

## Scoped review repairs — 26 October 2026

Removed the angular-speed cutoff. Every body composes the same tick-relative increment; at zero length (including length underflow), sin(w dt/2)/w uses its analytic dt/2 limit. Tick initialization and rejection rollback remain. New zero, 1e-10 rad/s and 1e-30 rad/s controls directly assert signed quaternion X against the analytic solution with a relative rounding bound, avoiding distance underflow. The nontrivial free-rotation oracle now observes two independent basis vectors.

The new Weight replay uses explicit typed numeric fields and checks empty passive-state collections without calling output bit-comparison helpers. Before replay acceptance, every tick from 4 to 10 s must satisfy the fixed-reference resting displacement, 1e-5 rad tilt, original support/penetration and 2 mm/s speed limits. Existing helper users remain unchanged.

The rocking test keeps its original one-second peak control and now compares committed angular pitch at the first 120 Hz tick. The initial singleton geometry is unchanged; a separate allowance of 512 ULPs at the declared 4 rad/s scale budgets four substeps, eight sweeps and 16 ULPs per sweep. The 6.25% maximum torque ratio is not asserted to predict the peak-angle response. The actual axial rolling positive control is retained.

Governing order now includes the independent blocked cap retry, multi-collider dynamics, solid bumper cap and flush face markings before 9A.44B. Newly added status prose has readable identities and units; historical evidence files were not altered.

Incremental Debug build session 8885 passed with 0 warnings and 0 errors in 8.92 s. Scoped repair test session 98720 passed 15 tests, 0 failed, 0 skipped in 2.8184 s. Command:
`dotnet test CuriousContraptions.tests/CuriousContraptions.tests.csproj -c Debug -f net10.0 --no-build --no-restore --nologo -noAutoResponse --filter 'FullyQualifiedName~RepresentableTinyFreeSpin|FullyQualifiedName~FreeAngularMotionComposes|FullyQualifiedName~TiltedWeightReset|FullyQualifiedName~ARockingLyingCylinder|FullyQualifiedName~ALogRollingOverAParallelLog' --logger 'console;verbosity=normal' -- RunConfiguration.TreatNoTestsAsError=true`.

The earlier 390-test run belongs to the pre-repair candidate. Full repaired-candidate verification and independent review confirmation remain coordinator-owned; no broader tests were run for these repairs.

### Final local round — failures retained

Session `7426`, using the five matching compiler logs at `/private/tmp/tim-orientation-gate.cvRSVS`, ran all required local tests with only the generated browser deferral: **4,430 passed, 3 failed, 0 skipped**. Game tests: 3,938 passed / 3 failed; Coverage: 77; E2eRunner non-browser: 192; WorkerAotGuard: 17; BundleElements: 206, all other projects passing. Game duration was 9 m 2 s. Original TRX files remain in that directory's `results` subdirectory.

Failures: `ASolidPartnerMeetsTheFlatTorusEndWithoutInventedCap` with cylinder/on-end in both body orders fails its 1 mm/s one-second settling speed; `ABallRollingAlongPlusZAboutXAlsoComesToRest` fails the combined nonnegative-Z-speed/rest-spread assertion. The retained implementation owner is diagnosing these before repair; no acceptance has been relaxed and no publication approval is claimed. The final seeded energy tests pass unchanged: Dead 2.191 mJ, Rolling 2.114 mJ and Lively 1.991 mJ control-derived budgets, with no class exceeding its budget. This failed full run does not establish slice acceptance. GitHub tests are not required or run; local tests and normal hooks remain authoritative.

## Auto Run Result

Status: blocked. Blocking condition: patch verification failed — the full local round exposed two cylinder-on-flat-torus settling failures, and no causal runtime repair is established. The candidate is preserved in this isolated worktree, unpublished; the overall backlog goal remains active.

Implemented tick-relative f32 orientation accumulation, transient per-tick initialization/rollback, analytic zero-length angular increment, and behavioral levelling/free-spin/Reset/save/load tests. Runtime changes are confined to `WorkshopCollisionKernels.cs` and `WorkshopPhysicsEngine.cs`; tests are in `WorkshopPhysicsLawCylinderTests.cs`, `WeightTests.cs`, and the final directional-residual correction in `WorkshopPhysicsLawTests.cs`. Current planning/status documents and original evidence accompany them. No cap rewrite, damping, tolerance-fitting, ABI change or compatibility path was imported.

Review: the 16 findings and every patch/reject reason are retained in Review Triage Log (high 1, medium 5, low 4, false 6). Grouped patched entries: high 1, medium 3, low 3; no deferred findings. Retained Edge and Forward review confirmed those repairs; Edge session 70751 passed 14/0/0. Follow-up review recommended: true, specifically the unresolved cap-end settling impact and the final directional comparison must be independently checked after a causal correction. No SnapshotApproval or implementation Pass is claimed.

Final verification: five compiler captures passed; full session 7426 returned 4,430 passed / 3 failed / 0 skipped. Existing energy gates passed unchanged. The ball failure was solely minimum subnormal velocity -1e-45 m/s with zero resting spread. Its test now uses the same independently bounded -1e-6 m/s directional rounding allowance as the adjacent +X test (about 16 f32 ULPs at declared 0.7 m/s); unchanged 1 mm displacement and resistance-free control assertions are separate. Build 93797 passed with 0 warnings/errors; scoped session 50594 passed 3/0/0 in 2.2808 s. No full rerun is justified while the two hollow failures are known unresolved. The final test edit invalidates affected compiler captures; refresh them before any subsequent full acceptance run.

Diagnostic evidence: [candidate](failed-closure-candidate.log), [published baseline](failed-closure-baseline.log), [contact normals](failed-closure-normals.log). Baseline 822317c99a08ec64ce2d7406090036d51c1c8fa2 passes the original one-second checks, but its cylinder travels 1.740900 mm from 1–10 s; candidate travel is 1.718384 mm. The endpoint speed check samples different jitter phases. All 16 supporting contacts at the failing pose have pure vertical normals, ruling out the proposed partner-normal cancellation repair. This implicates existing cap-contact behavior but does not yet establish its exact causal defect. Diagnostic source changes were removed; the published worktree is clean (its diagnostic binaries are not matching restored-source proof). No test was skipped, removed or loosened to conceal this motion.

Retry owner: coordinator, with a fresh bounded Auto cap-contact investigation from published main. Preserve this orientation candidate and the separate failed cap-block attempt. First establish net contact impulse/settling behavior under unchanged acceptance. Read-only next-spec analysis also found the rod oracle's one-substep angular budget assumes an upper duration/support-impulse bound not established by the soft/speculative law; observe the 12.5-degree/45-degree second impact at 240 Hz plus a passing control, including warm start and signed corrections, before selecting a model or implementation correction. This is a derivation gap, not proof of the observed failure's cause. No output-fitted duration, support cap or extra damping is authorized by this finding.

Normal hooks, commit, push, production publication and terminal checks were not run for this blocked candidate. GitHub tests remain disabled by the owner policy. Browser/performance qualification remains pending at named later gates.
