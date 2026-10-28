---
title: 'bumper-cap-solid: collide with the visible cap and preserve playable lessons'
type: 'bugfix'
created: '2026-10-28'
status: 'in-review'
baseline_revision: '28a2d302e6c0e1a0e361bfe62beea9b332d6b9f9'
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '{project-root}/AGENTS.md'
  - '{project-root}/docs/testing-guidance.md'
  - '{project-root}/docs/planning/elements/CAT-015-bumper.md'
warnings: [oversized]
deferred: []
---

<intent-contract>

## Intent

**Problem:** The Bumper's visible gold cap is non-solid. A ball can pass through its exposed face/rim; making it solid changes launch normals and can invalidate the three authored Bumper lessons.

**Approach:** Add the matching cap to the existing static body's scene geometry, remove its non-colliding exemption, and re-author affected lesson solutions as needed. Preserve the existing shared contact-work, rendering and lifecycle behavior.

## Boundaries & Constraints

**Always:** Preserve CAT-015 / EL-195, requirements current-cat-015, todo-157/159/163 and IX-01. One static body owns the head sphere, upper torus and new cap cylinder; the same finite store and per-target cooldown govern every child. Cap radius0.25m, height0.12m, centre(0,0.59,0), top0.65m. Head radius0.65m and upper torus centreline0.49m/tube0.018m/centreY0.43m remain. Strength0..20m/s, preload0.5*strength²J, default32J, threshold0.05m/s and72 physical-step cooldown retain their defined feature contracts. Preserve paid/partial/exhausted behavior, real source-funded recharge, glancing spin, depth miss, stationary/separating rejection and release/recontact. Preserve overlapping positive-work Impact cosmetics and exact authored Reset/save/load fields.

**Never:** No element-specific solver, alternate collision path, compatibility, hidden warmup, hard population ceiling or art substitution. No historical trajectories/tick pins, output hashes/byte identity, solver changes to satisfy unjustified precision, mutants, browser/E2E tests, local Release build or web publish. Physical placement assistance (todo-159) remains explicitly owned by later Story15.0; this cap slice does not claim complete CAT-015/EL-195 or browser/performance qualification. Base is enclosed decoration; expanding impact ring stays cosmetic. No face-marking work.

## I/O & Edge-Case Matrix

| Scenario | Input/state | Expected behavior | Error handling |
|---|---|---|---|
| Exposed cap | Face/rim strike; include rotated Bumper | Real contact excludes visible penetration and redirects the actual target | Normal shared contact rules |
| Nearby miss | Target passes outside cap and surrounding solids | No cap contact, payout or pulse | Normal free motion |
| Energy | Paid, partially affordable, zero/exhausted store | Paid work is bounded; no free boost or unpaid pulse | Existing finite ledger |
| Child transitions | Same target touches different Bumper children within cooldown | One owner/target policy prevents duplicate payouts | Existing cooldown |
| Lessons | Sidekick, Depth, Wall+Bumper solutions and controls | Positive goals; zero-strength/wrong-depth/missing-wall remain unsolved | Finite stall guard |
| Lifecycle | Run/Reset and save/load | Authored state restored; loaded solution remains playable | Existing typed rejection |

</intent-contract>

## Code Map

- `elements/Bumper/bumper.tscn`: Visual_Cap CylinderMesh and transform; existing col_0_0 sphere and col_ring2 mesh extraction. Add cap through this same scene pipeline.
- `elements/Bumper/BumperElementDeclaration.cs`: ISceneGeometryElement, empty Colliders, one static SceneBodies row; shared ContactWork and five Impact presentation bindings. Update inaccurate sphere-only comment.
- `CuriousContraptions.tests/VisibleSolidColliderTests.cs`: ArtSpec PinballBumper Uncollided exemption and pending-cap paragraph; existing independent Godot art comparison and real feature simulation helpers.
- `CuriousContraptions.tests/ContactWorkInstanceTests.cs`: production transport/store/lifecycle/recharge positives and controls. `WorkshopBumperTests.cs`, `EventRingConsumerTests.cs`, `WorkshopActivationAnimationTests.cs`, `BasketballResourceTests.cs`: direct numeric boundary, occurrence, animation and art consumers. Reuse valid existing proof; add cap-specific discrimination rather than duplicate all suites.
- `CuriousContraptions.tests/LaunchLessonSolveTests.cs`: AuthoredSolution, Bench, Solved and existing all-playable solution/Reset/save-load guard. Current bumper Bench Y1 differs from content Y1.5; reconcile affected fixtures with authored teaching, not old trajectories.
- `content/puzzles.json`: bumper_sidekick and wall_and_bumper Bumper(-3.3,1.5,0); bumper_depth(0,1.5,3.3). Wall fixture(-1,4,0), dimensions0.4x6x1.5. Inspect direct consumer wall_return and its actual playable status.
- `docs/planning/elements/CAT-015-bumper.md`: collider-target explicitly authorizes cap and three lesson re-authorings; §§2/5 preserve finite-work controls. Requirement anchors above and named-elements element-195 preserve broader obligations.
- `engine/gpu/WorkshopPhysicsCompiler.cs`, `CuriousContraptions.Bundle/BundleGeometry.cs`: existing generic compilation/extraction; no new solver capability is expected.
- `TODO.md`, `_bmad-output/implementation-artifacts/sprint-status.yaml`, `_bmad-output/planning-artifacts/epics.md`: current brief/order. Prior compound terminal receipt belongs in `spec-engine-multi-collider-dynamic.md`; historical attempts remain unchanged.

## Tasks & Acceptance

**Execution:**
- `elements/Bumper/bumper.tscn`, `BumperElementDeclaration.cs` — add actual cap collider and correct description.
- `VisibleSolidColliderTests.cs` and affected contact-work tests — delete cap exemption/pending legacy, prove matrix through production simulation, retain head/ring controls.
- `content/puzzles.json`, `LaunchLessonSolveTests.cs` and direct consumers — re-author affected solutions/fixtures based on playable goals, preserve intended teaching controls.
- `docs/planning/elements/CAT-015-bumper.md` and current brief/sprint/epics/spec — describe actual geometry, coverage, pending later qualifications and publication evidence.

**Acceptance Criteria:**
1. Given authored cap geometry, when the real bundle compiles a Bumper, then independent art comparison finds the matching cylinder on its existing static body; no Uncollided exemption or sphere-only current description remains.
2. Given face/rim and rotated cap strikes, when production C# advances, then targets contact the exposed cap without visible penetration and show plausible outgoing motion; nearby miss controls distinguish cap contact from head/ring effects. Bounds derive from functional clearance/stability, not ULP/slop fractions alone.
3. Given cap contacts with paid/partial/zero/exhausted stores and child-transition recontacts, when the real solver/ledger advances, then finite work, per-target cooldown and positive-work pulse behavior hold across all children; existing recharge/negative/glancing controls continue to pass.
4. Given each affected authored lesson at precision0,0.45,1, when run and reloaded, then its actual goal occurs in a generous finite window; matched controls remain unsolved and Reset restores authored state. Current1200ticks/10s is a stall guard, may increase only with an independently stated intended progression reason. No historical arrival comparison.
5. Given Run/Reset/save/load and Impact cosmetics, when their supported lifecycle executes, then cap geometry stays tied to authored physical pose, shared store/cooldown reset, strength persists and cosmetic scaling never changes collision.
6. Given the frozen candidate, when local verification and normal hooks run, then all required tests pass with Skipped0; independent Auto review and exact-SHA production artifact/deployment verification pass before the next slice.

## Spec Change Log

## Review Triage Log

### 2026-10-29 — Review pass
- verdicts: 3 findings — high 0, medium 2, low 0, false 1, maybe-false 0
- All five Auto layers completed. Blind Hunter, Edge Case Hunter and Forward-Only Auditor reported no findings.
- findings:
  - `[medium]` `[patch]` Verification Gap: the new cross-child test injects ABI ledger history and bypasses WorkshopSimulation — confirmed at ACompiledCapContactHonorsTheOwnersOtherChildHistory and PreviousOccurrence. It cannot catch failure to create the earlier physical contact history. Replace this new test through the existing simulation interface; the same implementation owner replaced it with ARealCapThenRingContactSharesTheCooldownEvenWithRechargedWorkAvailable through WorkshopSimulation, including an energy-available suppressed ring hit and a paid direct-ring control.
  - `[medium]` `[patch]` Intent Alignment: prior-child history is supplied, not generated by an actual earlier collision — confirmed; same root cause and fix as the preceding row. Preserve actual contact order and shared owner/target policy without historical trajectory pins.
  - `[false]` `[reject]` Intent Alignment: finite-work integration is not independently discriminated by the new cap test — no missing acceptance behavior: retained StrengthEightLaunchesTheBallPaidAtTheTargetSpeedAndTheLedgerCloses and APartiallyAffordableBoostEmptiesTheStoreAndALaterContactIsAnOrdinaryUnpaidBounce execute the compiled cap at x=0 and directly assert outgoing speed, debit, exhaustion and later passive bounce. Both passed in the full run; no solver or energy-rule change requires duplicate coverage.

## Verification

Implementer: one incremental Debug build of affected closure with `-noAutoResponse`, then affected C# tests using `--no-build --no-restore -c Debug -f net10.0` and TreatNoTestsAsError=true; select only genuinely affected classes for scoped checks. Preserve failed receipts, fix assertions only against independently stated game behavior. No new test framework or diagnostics platform.

Coordinator: five matching compiler captures and one final full solution run per docs/delivery-workflow.md#local-test-authority, then normal hooks. Auto review layers supply independent review; no separate redundant review system. GitHub only builds/publishes; verify exact committed SHA, Pages artifact/deployment and deployed files. Browser/performance stays pending.


## Implementer record — 28 October 2026

- Added the cap through the existing `col_` scene-mesh extraction; no compiler, solver or presentation branch changed. Removed its Uncollided exemption and current sphere-only descriptions.
- Re-authored Bumper centres to (-3.35,1.5,0) for Sidekick/Wall+Bumper and (0,1.5,3.35) for Depth. Wall+Bumper's wall is now (-1.75,4,0), retaining0.4×6×1.5 dimensions. The earlier reflection clears the Bumper on the return route and reaches the strict Receiver. `wall_return` remains Planned and unchanged.
- Native proof adds exposed face/rim/rotated-cap clearance, near-miss absence of payout/pulse, and paid/partial/empty/exhausted cap-store/pulse cases. Cap-return occurrences obey the72-step cooldown. Existing tests retain head/ring, recharge, spin, rejection, overlap and lifecycle coverage.
- The 28 October policy resolves older save/timing oracles: removed three whole-save byte comparisons in affected tests (decoded authored-field equality remains), and replaced the lesson final-trajectory replay comparison with successful reloaded goal occurrence. No thresholds or solver algorithms were relaxed.
- Final combined affected run before the last two redundant byte assertions were removed: **259 passed,0 failed,0 skipped**,110.61s runner duration. Command: `dotnet test CuriousContraptions.tests/CuriousContraptions.tests.csproj --no-build --no-restore -c Debug -f net10.0 --filter 'FullyQualifiedName~LaunchLessonSolveTests|FullyQualifiedName~ContactWorkInstanceTests|FullyQualifiedName~VisibleSolidColliderTests|FullyQualifiedName~WorkshopBumperTests|FullyQualifiedName~EventRingConsumerTests|FullyQualifiedName~WorkshopActivationAnimationTests|FullyQualifiedName~BasketballResourceTests|FullyQualifiedName~WorkshopPhysicsLawSensorWorkTests' --logger 'console;verbosity=normal' -- RunConfiguration.TreatNoTestsAsError=true`. Original tool receipt: session53039, completion chunk d66ada. Matching incremental Debug build:0 warnings/errors,12.38s, receipt345f48.
- Earlier expected failures are retained in execution receipts: session20116 (old lessons and two-shape expectation);84783 (first wall layout);41485/44830 (authored/strict wall misses);28539 (temporary authored-layout diagnostic, subsequently deleted because final lesson tests cover the selected layout). The first no-restore build lacked worktree assets; a normal restore/build resolved it.
- Superseded round-one proof (rejected by independent review below): `ACompiledCapContactHonorsTheOwnersOtherChildHistory`: the production compiler supplies the actual Bumper and Bowling ball; canonical committed ledger input records the same owner's earlier sphere/torus hit. A real cap collision before expiry preserves the occurrence and store while giving ordinary bounce; expiry or another target admits a paid occurrence explicitly owned by the cap collider. Six cases cover both prior child kinds and all three admission outcomes. This proves the committed-state transition contract, not an engineered trajectory between the two hits; the separate face/rim/rotated-cap tests retain geometric discrimination.
- Coordinator still owns independent Auto review, the final full local round/compiler captures, normal hooks and exact-SHA production artifact/deployment receipts. Story15.0 retains placement assistance; browser/performance retain their named later gates.
- After removing the remaining redundant byte assertions: incremental Debug build0 warnings/errors (8.40s, receipt b57c1e); affected WorkshopBumperTests **55/0/0** (263ms, receipt551557). `git diff --check` passed. No runtime/content inputs changed after the259-test run.


- Cooldown completion verification: incremental Debug build **0 warnings/errors**,6.02s (receipt f389b1); `dotnet test CuriousContraptions.tests/CuriousContraptions.tests.csproj --no-build --no-restore -c Debug -f net10.0 --filter 'FullyQualifiedName~WorkshopPhysicsLawSensorWorkTests|FullyQualifiedName~ContactWorkInstanceTests' --logger 'console;verbosity=normal' -- RunConfiguration.TreatNoTestsAsError=true`: **38 passed,0 failed,0 skipped**,3.253s (session37713, receipt cf63dc). The cap-return comment now claims only the observed repeated-cap behavior. No runtime changes.


## Coordinator verification

The frozen implementation diff is `implementation.diff` in this slice's rendered workflow directory; root inspected runtime/test/content changes and the accompanying current-record edits. No solver algorithm changes were added. The matrix below includes the review correction; its final execution membership must be checked against the refreshed full TRX.

| Matrix row | Discriminating tests |
|---|---|
| Exposed cap | EverySolidArtPartHasItsColliderAndNoColliderReachesBeyondTheArt; TheExposedCapFaceAndRimStopAndRedirectTheBall |
| Nearby miss | ABallOutsideTheBumperSolidsFallsPastWithoutWorkOrPulse |
| Energy | ExposedCapContactsUseTheFiniteStoreAndOnlyPaidWorkEmitsAPulse; APartiallyAffordableBoostEmptiesTheStoreAndALaterContactIsAnOrdinaryUnpaidBounce; ADirectlyWiredBatteryRechargesTheStoreAndAnUnwiredOneDoesNot |
| Child transitions | ARealCapThenRingContactSharesTheCooldownEvenWithRechargedWorkAvailable; CooldownEligibilityIsPerBodyAndOpensAtTheExactSeventyTwoStepBoundary |
| Lessons | TheAuthoredSolutionSolvesAtEveryPrecision; EachLaunchRouteSolvesOrStaysUnsolvedAndResetRestoresTheBench |
| Lifecycle | RunResetAndSaveLoadAreExact; authored lesson reload/Reset assertions; existing EventRingConsumerTests and WorkshopActivationAnimationTests |

The first compiler-capture attempt (session50183, exit1; logs `tim-bumper-final-gate.2TR5Kl`) stopped before tests because the fresh worktree lacked full-solution restore assets. A normal full solution restore resolved this provisioning issue; no source changes. Retry session35375 uses `/private/var/folders/bg/f_3jxb251pnfck7qpwb9xmxc0000gn/T/tim-bumper-final-gate.QfPVEX` and launches the full five-project run with `bumper-full-round1.trx` receipts. Full result: session35375 exited0 on29 October; main4,345/0/0 in20m plus tools491/0/0, total4,836/0/0. Root independently parsed all five TRXs and confirmed all11 named matrix methods plus EventRingConsumerTests/WorkshopActivationAnimationTests executed and passed (including all6 cross-child cases). All five compiler captures passed. Independent Auto review, hooks and publication remain pending.

## Review correction — 29 October 2026

The synthetic six-case history test is removed; WorkshopPhysicsLawSensorWorkTests.cs returns to baseline. Its replacement, ARealCapThenRingContactSharesTheCooldownEvenWithRechargedWorkAvailable, uses authored construction and the existing WorkshopSimulation Ring harness. A paid cap contact is followed by a geometrically isolated ring collision inside the defined72-step cooldown. A directly wired battery restores the2J store before that collision; unchanged occurrence/store proves suppression is not caused by exhaustion. A matched drop directly above the authored ring centreline pays on its first ring contact. The5mm observation band distinguishes the18mm ring tube from the head and cap; no historical arrival tick or trajectory is asserted.

Same-owner affected proof:33 passed,0 failed,0 skipped in3.179s (session1392, receipt73e424); matching incremental Debug build0 warnings/errors,6.29s (cb4ef0). Only comment/whitespace cleanup followed that run. All temporary diagnostic tests are deleted. The original verification reviewer returned Pass after inspecting the actual fix and helper interfaces: no remaining finding. Final verification session15947 uses compiler logs `/private/var/folders/bg/f_3jxb251pnfck7qpwb9xmxc0000gn/T/tim-bumper-round2-gate.ZrWB88`: solution/playtest captures are refreshed; unchanged web/file-tool captures are reused from round one. Full local proof passed with `bumper-full-round2.trx` receipts; see the final local receipt below. Round-one4,836/0/0 remains historical candidate evidence, not proof of this changed test.

## Auto Run Result

Implemented the Bumper's visible cap as a cylinder on its existing static compound body, using the shared geometry/contact-work pipeline. Re-authored the three affected lessons and retained positive/control, finite-work, lifecycle and cosmetic behavior.

Changed files:
- `elements/Bumper/bumper.tscn`, `BumperElementDeclaration.cs`: cap collision and accurate compound-body description.
- `VisibleSolidColliderTests.cs`: matching cap art plus face/rim/rotation contact and nearby miss.
- `ContactWorkInstanceTests.cs`: cap finite-store/pulse coverage and real cap-to-ring cooldown transition with paying control; decoded save coverage replaces a byte pin.
- `LaunchLessonSolveTests.cs`, `content/puzzles.json`: playable authored placements across supported precision settings, meaningful controls and loaded-goal proof.
- `WorkshopBumperTests.cs`: expected three-child geometry; decoded save checks replace redundant byte comparisons.
- `DESIGN.md`: correct the stale sphere-only collider description; the original reviewer independently confirmed the head/ring/cap wording and cosmetic-only scaling.
- `docs/planning/elements/CAT-015-bumper.md`: actual cap geometry and current lesson behavior; later placement/browser qualification remains explicit.
- `TODO.md`, sprint status, epics, this spec: current outcome and evidence.
- `spec-engine-multi-collider-dynamic.md`: prior slice's already-verified terminal publication receipt.

Review: all five Auto layers completed. Three findings were recorded: two medium observations share one synthetic-history coverage defect, fixed by the original implementer; one finite-work coverage concern was rejected because retained real-cap speed/debit/exhaustion tests directly cover it. No deferred findings. The original verification reviewer independently passed the actual correction. Patched entries: high0, medium1, low0. Follow-up review recommended:false; affected re-review is already complete, with no named unverified risk.

Final local verification and publication receipts follow below. Browser and detailed performance qualification remain pending at their named later gates; this slice does not claim full CAT-015/EL-195 or repository-wide test-hygiene completion.

## Final local receipt — 29 October 2026

Session15947 exited0 (completion e46f72). Full solution: **4,831 passed,0 failed,0 skipped** — main4,340, BundleElements206, Coverage77, E2eRunner191, WorkerAotGuard17. Main TRX start00:26:26+08, finish00:46:30+08 (20m04s). All five projects' original receipts are `TestResults/bumper-full-round2.trx` under their project directories. Root independently parsed their counters and confirmed execution/Pass of every matrix method, including the new real cap-to-ring case, all12 authored precision cases, all12 lesson positive/control cases, and event/presentation lifecycle suites.

Compiler captures: `/private/var/folders/bg/f_3jxb251pnfck7qpwb9xmxc0000gn/T/tim-bumper-round2-gate.ZrWB88`; refreshed playtest22.49s and solution13.36s both0 warnings/errors; unchanged web/check-staged-tests/verify-policy logs reused from round one. Command: `dotnet test CuriousContraptions.slnx -c Debug -f net10.0 --no-build --no-restore --nologo -noAutoResponse --filter "$local_deferred_filter" --logger 'trx;LogFileName=bumper-full-round2.trx' -- RunConfiguration.TreatNoTestsAsError=true`, with `CSHARP_STATIC_GATE_BINLOGS` set to that directory and the filter generated by E2eRunner's single explicit browser-deferral list. No skipped game tests, mutants, browser runs, local Release worker build or web publish.

Implementation owner: `/root/bumper_cap_dev`. Independent Auto layers: `/root/bumper_blind`, `/root/bumper_edge`, `/root/bumper_verification`, `/root/bumper_intent`, `/root/bumper_forward`. The verification reviewer passed the corrected real-contact test and the final DESIGN.md wording. No unresolved required-now findings remain. Normal commit hooks, exact committed-SHA approval and production artifact/deployment verification remain pending; status stays in-review until terminal publication is verified.
