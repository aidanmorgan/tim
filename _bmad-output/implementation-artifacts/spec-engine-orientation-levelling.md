---
title: 'Preserve small rotations after the support-coverage correction'
type: 'bugfix'
created: '2026-10-27'
status: 'done'
baseline_revision: a98ef86c656040ae1f057967667f23235e41d7ce
review_loop_iteration: 0
followup_review_recommended: false
context: [AGENTS.md, docs/testing-guidance.md, docs/delivery-workflow.md]
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

## Current retry context

This is a fresh retry from published main, not dispatch of the blocked earlier spec. The immutable intent above records the original problem and policy. The prerequisite has since changed: generic support coverage was corrected and published at `a98ef86c656040ae1f057967667f23235e41d7ce`. Its strengthened sustained cap translation/rotation, cadence, thin-wall and replay checks now belong to acceptance. Neither old orientation proof nor the prerequisite's proof qualifies their combination.

The previous orientation attempt is preserved read-only at sibling worktree `../story-engine-orientation-levelling`, baseline `822317c99a08ec64ce2d7406090036d51c1c8fa2`. Its full run was 4,430 passed / 3 failed / 0 skipped: two flat-torus settling cases and a minimum-subnormal negative-Z directional residual. The final +Z assertion uses the adjacent +X test's independently derived -1e-6 m/s allowance, separate unchanged spread and resistance-free controls. This is part of the retry. Preserve old failure evidence; never label that run accepted.

## Code Map

- `CuriousContraptions.Simulation/WorkshopCollisionKernels.cs`: carry only the old candidate's transient SimBody TickStartRotation/TickRelativeRotation hunk. Preserve the published ReduceSideManifold spatial coverage algorithm, deterministic ties, keep.Clear and emission order.
- `CuriousContraptions.Simulation/WorkshopPhysicsEngine.cs`: Advance initializes scratch for every pooled body; integration left-composes world-space increments within the current tick, computes relative*start, and restores scratch with pose on rejected nonfinite substeps. Keep Q0/Cm0 substep separation, COM and all constraint laws. Analytic zero/underflow coefficient replaces finite angular-speed freeze.
- `CuriousContraptions.tests/WorkshopPhysicsLawCylinderTests.cs`: carry old tilt/cadence, original 200 drops, tiny/free-spin, two-vector Rodrigues, reused-engine and rocking test hunks. Preserve published removal of synthetic reducer feature-index fixture and real skewed depth test.
- `CuriousContraptions.tests/WeightTests.cs`: carry actual package every-tick rest/support/tilt plus Reset, supported construction Save/Load and fresh/reused typed numerical replay.
- `CuriousContraptions.tests/WorkshopPhysicsLawTests.cs`: carry only final +Z directional-residual correction described above.
- `CuriousContraptions.tests/WorkshopPhysicsLawHollowTests.cs`: published cap sustained tests at all three cadences and both orders, 17-degree controls, rotation/translation and replay. Preserve unchanged; run first with thin-wall spanning controls.
- `CuriousContraptions.Abstractions/PhysicalTolerance.cs`, `CuriousContraptions.tests/PhysicalToleranceTests.cs`: existing policy and fixed-reference observations; no universal epsilon or output-fitted budget.
- `docs/gpu-f32-physics.md`, `_bmad-output/planning-artifacts/epics.md`: update orientation behavior and qualified scope, reconcile obsolete drift-speed acceptance with current owner policy, preserve energy/angular/lifecycle and multi-collider/bumper-cap/face-marking outcomes.
- `TODO.md`, `_bmad-output/implementation-artifacts/sprint-status.yaml`, `_bmad-output/implementation-artifacts/deferred-work.md`: reconcile current retry and published prerequisite without wholesale copying stale old views.
- `_bmad-output/implementation-artifacts/spec-engine-cap-settling.md`: append short terminal receipt below. Warmup receipt is already published; do not duplicate or recopy its old changes.
- Current requirements REQ-06/07/09/14, generic f32 solver ownership, typed atomic rejection and ordinary lifecycle remain. Browser and detailed performance qualification stay at named later gates. Separate cap-block retry stays blocked; import none of its code.

## Tasks & Acceptance

**Execution:**
- Apply only the five source/test file hunks above from the retained orientation worktree to this worktree, after per-file Anvil validation. Do not copy whole shared files. Delete the old repeated absolute-rotation accumulation and finite-speed freeze without adding a second integration rule.
- Preserve the earlier blocked spec and its original raw logs as historical evidence under `_bmad-output/implementation-artifacts/evidence-engine-orientation-levelling/`; use `prior-attempt.md` for the spec and preserve relative evidence links. Clearly distinguish historical outputs from this retry. Do not rerun old baseline diagnostics.
- Build the changed closure once and test the previously failing cap plus strengthened sustained/rotated/thin-wall/replay cases first, together with named orientation and Weight/+Z checks. If they fail, diagnose one causal hypothesis without relaxing acceptance or importing cap-block.
- Run the remaining affected energy/friction/restitution, typed identity, lesson and lifecycle controls using matching binaries. Record exact commands, totals and raw outputs in the existing evidence directory.
- Refresh the bounded current documents listed above; append prerequisite receipt; retain every pending qualification and current execution order.
- Return implementation and evidence to coordinator without committing, pushing or running the full final round. Coordinator owns five Auto layers, repairs, final compiler captures/full local round, normal hooks, exact-SHA approval and publication.

**Acceptance Criteria:**
- Given the named tilted, random, tiny/free-motion and lifecycle cases in the immutable matrix, when the combined production engine advances, then every original physical/control bound holds, typed observations replay and pooled scratch cannot leak.
- Given the published cap support coverage cases including rotated both-order cases at 60/120/240 Hz, when they settle and replay under the new integrator, then their unchanged every-tick translation, rotational displacement, speed, support and lifecycle assertions pass.
- Given thin-wall spanning, skew contacts, original energy seeds, friction/rolling and restitution controls, when this integration change runs, then their original budgets and outcomes pass with no widened oracle.
- Given final independent approval, when the candidate is published, then all required local tests and normal hooks pass with zero skipped tests; GitHub only compiles/publishes/deploys and terminal verification binds the exact SHA, artifact and deployment.

## Spec Change Log

## Review Triage Log

### 2026-10-27 — Review pass 0

- Verdicts: 17 findings — high 0, medium 3, low 5, false 9, maybe-false 0. Five Auto layers launched before triage; three-agent capacity required two waves. Edge returned [], Verification Gap found no gaps, and Forward-Only found no violations. Its analytic zero-length limit and nonfinite rollback are the same-law numerical handling, not alternative paths.
- Findings:
  - B1 [false] [reject] Missing narrative in blind patch — deliberate separation required by Auto: current claims go only to Edge after tracing; coordinator reads the complete candidate and preserved historical spec. All actual runtime/tests/docs and raw evidence are reviewed; no acceptance change is hidden.
  - B2 [false] [reject] Save payload discarded — WorkshopSimulation.Save returns only WorkshopCommandResult(Outcome, Reason), increments revision and validates Building; it has no payload. Construction encode/decode and subsequent production Construct are the supported persistence surface.
  - B3 [medium] [patch] Fixed tilted cases omit support/speed observations — add original geometric support/penetration and speed bounds throughout the existing window, without modifying limits.
  - B4 [medium] [patch] Random resting support/tilt observed only at endpoint — strengthen the unchanged 200 scenes with whole-window extrema, retaining seeds and limits.
  - B5 [medium] [patch] Zero-spin boundary only uses identity — add nontrivial admitted orientation and offset-centre stationary control using existing declaration interfaces and independently derived allowances.
  - B6 [low] [reject] Constant increments cannot isolate relative-increment order — code left-composes correctly and existing contact/rocking/energy cases vary angular response. A new controlled nonparallel increment oracle requires additional scenario machinery for a speculative error; not a direct correction.
  - B7 [false] [reject] Missing multibody pooled reuse — DeclaredPrismaticDriveTests.AReusedEnginePreservesEveryBodyTrajectoryAndStaticSupportInTheNextScene primes two moving/spinning bodies, then reuses slots for static supports and multiple dynamics; 120 per-ID trajectory/presentation comparisons ran in the 585-test run.
  - B8 [low] [reject] Singleton interval inferred from geometry — the declared low-end separation exceeds the speculative margin and the production Crr control directly observes first committed pitch plus the unchanged one-second peak. There is no demonstrated loss of the initial singleton interval; adding private contact certificates or a new alternate solver would exceed a direct correction. This is bounded behavioral evidence, not an all-substep contact-count claim.
  - B9 [low] [reject] Replay helper lacks unrelated package metadata — it asserts body count and each canonical ID before numeric state, so index ordering cannot silently exchange bodies. This passive Weight construction already has declaration/admission/save parameter coverage; new ownership-map machinery is unnecessary for transient integration.
  - B10 [low] [reject] Tiny permitted reverse residual could accumulate — within the six-second scenario 1e-6 m/s permits at most 6 micrometres of velocity-derived travel, well below the retained 1 mm stopped-spread limit. No infinite-time no-drift claim exists; additional accumulated-distance instrumentation is not needed for this residual correction.
  - I1 [false] [reject] Historical speed pins differ from displacement acceptance — the published owner-policy adoption already supersedes those pins; seeds/windows/angular/support/energy are preserved.
  - I2 [false] [reject] Alternative-law comparison absent — later no-mutation and forward-only rules apply; production Crr controls and independent geometry/early response establish the intended current-law behavior.
  - I3 [false] [reject] Save continuation is construction replay — Save explicitly refuses every phase except Building. In-flight checkpoint serialization is not a supported surface and is not claimed.
  - I4 [false] [reject] No arbitrary cross-tick sub-ulp guarantee — immutable intent is game-grade behavior with tick-local state; code/docs expressly retain that bound.
  - I5 [false] [reject] +Z numerical control changed — the change is the pre-existing +X directional roundoff contract applied to an observed minimum-subnormal residual; independent fixed 1e-6 allowance and original displacement/control remain. It does not loosen energy or allow physical-scale reversal.
  - I6 [false] [reject] Full local/publication proof still pending — current records explicitly say pending; coordinator enforces these before terminal approval.
  - C1 [low] [patch] Deferred singleton entry still requests obsolete two-law experiment — record current production-control evidence and disposition, retaining historical uncertainty and later qualification.

B3/B4/B5 are distinct medium test repairs. C1 is a direct documentation correction. No deferred findings. The retained implementer owns all repairs; final independent confirmation and final local/publication checks remain.


## Design Notes

Carry the previously reviewed tick-local design, including its repairs, as a candidate to reverify. Relative accumulation only spans a tick's 2/4/8 substeps; it makes no arbitrary cross-tick sub-ulp precision claim. No persistent remainder, ABI/save change, damping, snapping or per-element law is allowed. Genuine small rotation remains observed directly, not through squared distances that underflow.

Predecessor terminal receipt: SHA `a98ef86c656040ae1f057967667f23235e41d7ce`; production run `40356741495` success; build job `121691125567`, deploy job `121696004928`; Pages artifact `12666996791`, 80,798,440 bytes; deployment `7337266683`, success status `20642484253`. Fresh terminal verifier confirmed six HTTP 200 paths equal artifact contents: /, /index.html, /404.html, /godot.wasm, /godot.pck, /_framework/dotnet.js. Full local 4,428/0/0, normal hooks 126/0/0. No GitHub tests or browser qualification claimed.

## Verification

- Provision declared SDK/workload and Node 26.12.1; record actual versions. Incremental Debug build of `CuriousContraptions.tests/CuriousContraptions.tests.csproj` with `-noAutoResponse`; fresh worktree restore if required.
- Scoped `dotnet test CuriousContraptions.tests/CuriousContraptions.tests.csproj -c Debug -f net10.0 --no-build --no-restore --nologo -noAutoResponse --filter '<affected names>' --logger 'console;verbosity=normal' -- RunConfiguration.TreatNoTestsAsError=true`. Cover every matrix row and strengthened cap checks. All tests run and pass; zero skipped. Reuse matching build; retain commands and raw results.
- Coordinator checks actual complete diff, preserved original intent, changed-doc links/status and test matrix, reusing recorded matching executions.
- After review repairs, coordinator captures the five matching compiler logs and runs the full solution once at the end using `docs/delivery-workflow.md#local-test-authority` and only generated browser deferral. Normal hooks then run separately. No local Release worker/web publish, browser, mutation or GitHub test runs.

## Retry implementation and affected verification — 27 October 2026

Dedicated implementation owner: `orientation_retry_dev`. Baseline is published `a98ef86c656040ae1f057967667f23235e41d7ce`. Only the five named source/test hunks from the retained candidate were applied, after per-file Anvil allow. The published spatial support reducer, deterministic ties, cleared selection mask, emission order, strengthened hollow tests and physical-tolerance implementation/tests remain unchanged.

The one integration rule initializes every pooled body's tick start from committed input and its relative rotation to identity. Each substep left-composes the world-space increment and derives normalized relative × start. The analytic zero/underflow coefficient removes the finite-speed freeze. Nonfinite recovery restores the relative scratch alongside the pose; Q0/Cm0 remain substep constraint state. Deleted repeated absolute-quaternion accumulation and its 1e-9 angular-speed cutoff. No ABI/save change, persistent remainder, damping, snapping, alternate law or cap-block code was introduced.

Coverage carries the original named tilt/cadence, 200 seeded lying drops, tiny/free rotation, two-direction Rodrigues oracle, engine reuse, rocking and actual Weight Reset/construction Save/Load replay tests. The +Z directional-residual assertion now matches the adjacent +X independently derived -1e-6 m/s allowance; its separate spread and resistance-free controls are unchanged.

| Criterion / impact | Matching native evidence |
| --- | --- |
| Published support coverage and integrator interaction | First run: all hollow laws, sustained cap translation/rotation at three cadences, both orders and 17° controls, thin-wall spanning and typed replay. |
| Small rotation, resting and genuine motion | First run: named fixed tilt/cadence, tiny/free spin and two-vector Rodrigues, level/microtilt, rocking and +Z. Broad run: original four 50-drop batches with every-tick reference displacement, slow translation and all cylinder controls. |
| Energy, friction, restitution and direct consumers | Broad run: full cylinder, friction, restitution and general law classes, F32BehaviourIdentity, LaunchLessonSolve and DeclaredPrismaticDrive. Original seeds and energy budgets retained. |
| Typed lifecycle and scratch isolation (REQ-07/09) | Weight, WorkshopPhysicsEngineTests, free-rotation fresh/reused trajectories, hollow semantic replay and physical-tolerance boundaries. |
| Forward-only scope and current views (REQ-06/14) | Runtime diff confined to transient fields and integration; no supported legacy path added. Current TODO, sprint, deferred work, epics and numeric contract refreshed. Independent Auto review remains coordinator-owned. |

Environment: SDK 10.0.402, runtime 10.0.13, wasm-tools manifest 10.0.112/10.0.100 (installation SDK 10.0.400), Node v26.12.1. Incremental Debug closure build session 23603 passed with 0 warnings and 0 errors in 19.60 s; [raw build](evidence-engine-orientation-levelling/retry-build.log). No relevant source changed before these two recorded runs; later review test repairs are recorded separately below. Test executions use that build with --no-build --no-restore, -noAutoResponse and TreatNoTestsAsError=true; exact commands and raw outputs are in the logs.

- First combined native session 44345: **181 passed, 0 failed, 0 skipped**, exit 0, 9.8475 s. [Raw output](evidence-engine-orientation-levelling/retry-first-native.log).
- Broad affected session 50282: **585 passed, 0 failed, 0 skipped**, exit 0, 8.1537 min. [Raw output](evidence-engine-orientation-levelling/retry-affected-native.log). This includes all original 200 lying drops and the unchanged energy/material gates.

The six original historical raw logs were copied byte-for-byte and checked against the retained worktree. [prior-attempt.md](evidence-engine-orientation-levelling/prior-attempt.md) preserves the original blocked spec; only its relative log link prefixes changed to resolve after relocation. Its 4,430/3/0 full run is still a failure and is not reused as acceptance. No old baseline diagnostics were rerun.

Scoped checks passed: unchanged hollow/tolerance source/tests; retained cylinder test differences consist only of the published synthetic-fixture removal/skewed-depth test; Ruby YAML parsing; affected sprint key uniqueness/status; current brief/spec and relocated archive links; and git diff --check. No runtime schema changes require new membership.

Handoff: five independent Auto layers, review repairs, final matching compiler captures, one full local solution round, normal hooks, exact committed-SHA approval and publication remain coordinator-owned. No commit/push, full final round, local Release worker build/web publish, browser or mutation run was performed. Browser/performance qualification remains pending at named later gates. Tick-local relative accumulation makes no arbitrary cross-tick sub-ulp guarantee.

## Review repairs — 27 October 2026

Retained implementer added fixed-case support/speed assertions, whole-window random-drop tilt/support extrema and three stationary rotated offset-centre controls. Runtime code and physical limits are unchanged. Build 41716 passed with zero warnings/errors in 8.82 s; session 20434 passed 18/0/0 in 15.3768 s. [Raw repair evidence](evidence-engine-orientation-levelling/retry-review-repairs.log). Retained Edge independently confirmed B3–B5 from actual assertions and matching evidence; no additional run was needed. C1 required a second prose correction: preserve the original insufficient-pitch-constraint concern and distinguish Crr invariance/named resting behavior from any unproved general singleton levelling claim. Final compiler captures/full local and publication gates remain.

## Auto Run Result

Status: done locally; required publication pending. Tick-local f32 relative rotation replaces repeated absolute-quaternion accumulation and the finite angular-speed cutoff. Every pooled body initializes from committed input; rejected nonfinite substeps restore scratch with pose. No persistent remainder, ABI/save change, damping, alternate law or cap-block import.

Files: WorkshopCollisionKernels.cs adds transient scratch; WorkshopPhysicsEngine.cs implements the one tick-local integration rule; CylinderTests covers fixed/seeded rest, genuine tiny/free rotation, stationary rotated offset-centre motion and rocking; WeightTests covers actual package rest and typed Reset/construction-save replay; LawTests gives +Z the existing +X directional roundoff allowance with separate original spread/control. Current numeric/epic/TODO/sprint/deferred views and predecessor receipt are reconciled. Original blocked-attempt evidence remains historical, not acceptance.

All five Auto layers completed. Review triage retains every finding: 17 rows, high 0, medium 3, low 5, false 9. Patched entries: medium 3 and low 1; four low and nine false findings were rejected for the individual reasons above. No deferred review findings. Retained Edge independently confirmed B3–B5 and the corrected C1; no unresolved repair finding. Follow-up review recommendation: false. Although three medium test gaps were repaired, their exact assertions and passing 18-case run have independent confirmation, leaving no named unverified repair risk.

Final verification: compiler logs at `/private/tmp/tim-orientation-retry-gate.NdBkqB`; SDK 10.0.402/runtime 10.0.13, wasm-tools manifest 10.0.112/10.0.100 (installation SDK 10.0.400), Node v26.12.1. PLAYTEST build 8121 (22.01 s), normal solution 78167 (23.09 s), Debug web-host compile 41718 (12.83 s), CheckStagedTests 33302 and VerifyPolicy 7617 all passed with no warnings/errors. No Release worker build or web publish ran locally.

Full local session 99807 exited 0: **4,453 passed, 0 failed, 0 skipped**. Game 3,961 (9 m 11 s), Coverage 77, E2eRunner non-browser 192, WorkerAotGuard 17, BundleElements 206. Original TRX files remain in that log directory's `results`. Command used the complete solution, Debug/net10.0, --no-build --no-restore -noAutoResponse -m:1, the single generated browser-deferral filter and TreatNoTestsAsError=true, with CSHARP_STATIC_GATE_BINLOGS pointing at the five matching captures. Final runtime/test diff remained unchanged through capture and execution. Earlier affected 181/0/0 and 585/0/0 plus repair 18/0/0 supplement this full result. Scoped YAML, current links, preserved original intent and six raw historical logs also passed inspection.

Remaining publication steps: normal-hook commit, retained reviewer SnapshotApproval on exact SHA, non-force push, then fresh terminal production build/artifact/deployment/static-origin verification. GitHub runs no game/tool tests. Browser/device/performance qualification remains pending at named gates. Tick-local accumulation has no arbitrary cross-tick sub-ulp guarantee; broader singleton levelling remains explicitly unverified in its existing deferred-work destination, separate from the named passing scenes.

## Terminal publication receipt — 27 October 2026

Published SHA `bb35e8c4dc0a9b8f609dcc7e786223a0216e11af`; successful production run 40365292840, build 121721224177, deploy 121728177566; Pages artifact 12670871781 (80,805,094 bytes); deployment 7338621740, success status 20647610025. Fresh terminal verifier confirmed HTTP 200 and artifact-content equality for /, /index.html, /404.html, /godot.wasm, /godot.pck and /_framework/dotnet.js. Full local 4,453/0/0; normal hooks 126/0/0. GitHub ran no tests. Browser/performance qualification remains pending.
