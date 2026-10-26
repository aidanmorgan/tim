---
title: 'Bound generic contact recovery by the existing energy envelope'
type: 'bugfix'
created: '2026-10-26'
status: 'done'
baseline_revision: b3905fcda3df89b38140c9610b8ff2034e4078a8
planning_gate: '2026-10-26: generic recovery derivation, cadence limits, integration and physical controls verified'
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '/Users/aidan/dev/personal/tim/.claude/worktrees/story-engine-single-point-penetration/AGENTS.md'
  - '/Users/aidan/dev/personal/tim/.claude/worktrees/story-engine-single-point-penetration/docs/testing-guidance.md'
  - '/Users/aidan/dev/personal/tim/.claude/worktrees/story-engine-single-point-penetration/docs/gpu-f32-physics.md'
  - '/Users/aidan/dev/personal/tim/.claude/worktrees/story-engine-single-point-penetration/docs/delivery-workflow.md'
warnings: ['oversized']
deferred: []
---

<intent-contract>

## Intent

**Problem:** Deep single-point rim/edge penetration gains excess committed-tick energy because biased solve velocities move the pose before unbiased relaxation removes their velocity. This blocks the existing friction candidate and the later cap-block retry. The owner explicitly requests unblocking friction in parallel with behavioural-test replacements.

**Approach:** Replace the arbitrary recovery-speed cap in the shared normal law with a physically derived bound, preserving ordinary contact recovery, resting behaviour and all existing energy/penetration limits. Prove the exact blocked trajectory with the friction candidate before claiming it unblocked.

## Boundaries & Constraints

**Always:** One generic C#/f32 normal response for all shapes/contact counts; existing 480 Hz substeps and supported 60/120/240 Hz committed cadences; independently derived physical tests, positive/control/boundary and Reset; unchanged seeds, lessons and energy limits; Anvil before writes; five Auto review layers; normal hooks and exact-SHA publication.

**Never:** Seed/element/shape-specific fix, fallback or second normal law, compatibility, rest snapping, ad hoc whole-body energy clamp, relaxed oracle, output hashes, skipped game tests, mutants, Chrome gates, local Release worker build/web publish, or unrelated cap-block/friction/narrowphase redesign. If a broader design becomes necessary, return concrete evidence before changing this scope.

## I/O & Edge-Case Matrix

| Scenario | Input/state | Expected behaviour | Handling |
|---|---|---|---|
| Deep contact | Real centered and off-centre contacts, approximately 7 mm overlap | Recovery makes progress, settles inside existing penetration envelope, no excessive committed-tick energy | Shared recovery law |
| Shallow/control | Separated, slop-band and ordinary resting contacts | No phantom support or changed shallow/resting law | Existing speculative/soft branches |
| Cadence | Same construction at 60/120/240 Hz | One physical recovery rate; energy/recovery limits hold at every committed cadence | No host-mode law |
| Friction integration | Exact Dead CylinderOntoBox case 179 with retained friction candidate | Unchanged energy gate passes; original/reversed drop and geometry controls remain | Independent prerequisite integration |
| Lifecycle | Fresh/reused engine and supported Reset/readmission | Same semantic bodies and recovery outcome, no new persisted state | Existing command/ABI |

</intent-contract>

## Code Map

Implementation handoff for this already-running Auto workflow: implement only in this dedicated worktree, originally from published `8ac611ffe81a74af7d36328bf69cc6403065b6d9`, now integrated once onto published `b3905fcda3df89b38140c9610b8ff2034e4078a8`. No nested workflow, subagents, commits or pushes before coordinator finalization. The owner explicitly authorizes concurrent hash-test and friction-unblocking work; no shared writes/build outputs. Coordinator serializes integration/publication.

- `CuriousContraptions.Simulation/WorkshopConstraintSolver.cs:28-37,356-362`: ContactSlop=.0002, SubstepSeconds=1/480, Hertz60/zeta10, current MaxPushoutSpeed=3. `NormalBranch` supplies both single-row and block solve. Replace old arbitrary cap; preserve speculative/slop-band coherence and unbiased Relax. No alternate branch.
- `CuriousContraptions.Simulation/WorkshopPhysicsEngine.cs:425,639`: cadence maps to 8/4/2 fixed substeps. Read-only ownership; no new clocks/state or lifecycle caches.
- `CuriousContraptions.tests/WorkshopPhysicsLawTests.cs`: `LawScene/LawWorld/LawBody` use real production admission/ticks. `Step` resets/re-admits each call; use one continuous call for observations and copy semantic values.
- `CuriousContraptions.tests/WorkshopPhysicsLawCylinderTests.cs`: fixed energy gates, box controls, near-origin rest, pair/penetration/rolling and lifecycle regressions. Preserve original seeds/bounds; add generic physical recovery controls here or a focused new file registered in the explicit test project.
- `CuriousContraptions.tests/WorkshopPhysicsLawRestitutionTests.cs`, `WeightTests.cs`, `WorkshopPhysicsLawHollowTests.cs`, `F32BehaviourIdentityTests.cs`, `LaunchLessonSolveTests.cs`: affected physical regression closure, read-only expectations.
- [Existing deep-contact ownership](deferred-work.md) records case 179 under cap-block at 2.447mJ. The retained sibling friction spec `../story-engine-two-point-friction/_bmad-output/implementation-artifacts/spec-engine-two-point-friction.md` relative to worktree directory records current 2.408196586mJ >2.244929245mJ. Read that exact evidence; do not edit sibling.
- `docs/gpu-f32-physics.md#game-grade-envelope`: energy bound is per committed tick, not just default120 Hz. `docs/general-engine-design.md`, TODO, sprint, epics, deferred work and this spec need affected law/status updates without claiming global qualification.
- The coordinator will ask the retained friction implementer to apply the exact prerequisite delta in its worktree for integration. Return the precise runtime diff and controls when ready; do not import the full friction patch here or modify hash-test worktree.

## Tasks & Acceptance

**Execution:**
1. [x] Derive and implement one recovery-rate cap in `WorkshopConstraintSolver.cs`; delete the old 3 m/s value. Start with the selected bound below. Preserve unrelated solver paths.
2. [x] Add defined-interface deep/shallow, centered/oblique, zero/nonzero friction, static/dynamic support, cadence and recovery/energy/lifecycle tests. Derive expected progress/settle intervals and tolerances before observing output. Keep single-contact envelope distinct from multi-body qualification limits.
3. [x] Run matching affected checks; return exact runtime delta for coordinator-owned integration into the retained friction candidate and test exact case 179, drop/reversal and affected contacts. Integration evidence is required before unblocking claim.
4. [x] Update numeric/current tracking docs with derivation, limits, exact results and pending browser scope. Keep hash replacements independently owned.
5. [ ] Freeze; coordinator runs full required native round once, Auto review/fixes, hooks, committed-SHA approval and CI/artifact/deployment. Full friction review/publication remains its own resumed outcome.

**Acceptance Criteria:**
- P1: Given admitted deep contacts, when real simulation advances bounded ticks, then recovery progresses and reaches unchanged 0.5 mm resting penetration without launch/stall, while committed energy respects the existing applicable envelope. Include positive/control/boundary geometry and zero/nonzero friction.
- P2: Given all supported cadences and shallow/clear/resting controls, when they advance equal physical time, then the same physical recovery law preserves expected motion/support and each cadence's energy bound. The existing 60-box 1e-9 m/s near-origin check and cylinder rest controls remain unchanged.
- P3: Given the retained friction candidate plus this exact normal-law delta, when case 179 and the full unchanged original energy gate run, then no energy failure remains; original/exact-reversed Weight drop and geometry/lifecycle controls still pass. A passing published-base trajectory alone cannot satisfy this criterion.
- P4: Given Reset/readmission of affected scenes, when fresh/reused engines replay, then copied semantic states and physical recovery repeat. No output-buffer equality, persisted recovery state or format change is added.
- P5: Given the final closure, when full native verification and independent Auto review run, then zero failed/skipped tests or unresolved required-now regressions remain; publication uses the approved exact SHA. Browser/device/performance qualification remains at later gates.

## Spec Change Log

## Review Triage Log

### 2026-10-26 — Review pass 0

Independent Auto layers: `/root/penetration_blind_r0`, `/root/penetration_edge_r0`, `/root/penetration_verification_r0`, `/root/penetration_intent_r0`, `/root/penetration_forward_r0`. All five launched before triage. Edge Case and Verification Gap found no gaps; Forward-Only found no violations and confirmed the existing singular-block numerical recovery uses the same law.

- verdicts: 13 findings — high 0, medium 7, low 1, false 5, maybe-false 0
- findings:
  - `[false]` `[reject]` B1: The source acceptance is a ball already resting against the closed blade, not rapid recovery from a 323mm embedded start. The corrected analytically tangent fixture retains all180/300-tick release bounds and adds actual resting checks. Generic gross solid overlap was already an explicitly unresolved admission residual; the slice does not claim it rejected or corrected. Requiring its old rapid pushout would contradict the bounded recovery change rather than preserve the intended D6 behavior.
  - `[medium]` `[patch]` B2: Per-cadence broad progress bounds do not directly establish the same physical rate. Add equal-time intermediate pair separation comparisons with independently derived accumulation tolerance.
  - `[medium]` `[patch]` B3: The changed cap transition lacks just-below/at/above physical cases. Add an independently calculated single-row transition oracle whose numerical error cannot hide the capped/uncapped difference.
  - `[medium]` `[patch]` B4: The dynamic oblique pair checks linear speed but omits angular motion and total kinetic energy. Add explicit central-normal zero-torque and analytic combined-energy assertions.
  - `[false]` `[reject]` B5: The change replaces one scalar recovery cap, not the effective-mass law. Equal-mass dynamic support and static support satisfy the declared control matrix; existing cylinder/box and friction-response tests retain unequal-mass interactions. No changed mass rule or demonstrated unequal-mass failure requires a new mass sweep here.
  - `[false]` `[reject]` B6: The centered cases isolate normal recovery. P3's exact179 rim contact includes nonzero spin and full friction response, while original/reversed Weight drop and252-case response closure exercise tangential/friction competition. These are required integration evidence, not replaced by the centered rows.
  - `[medium]` `[patch]` B7: BaseSHA/session totals do not preserve the exact unpublished P3 inputs. Capture one scoped candidate runtime/native-test/project patch against the stated base, to become immutable with the prerequisite commit; do not import it as production code or claim friction publication.
  - `[medium]` `[patch]` B8: Loaded energy alone could allow compensating kinetic loss to mask excessive upward correction. Add the independently derived per-committed-tick upward displacement bound alongside existing energy and settling limits.
  - `[low]` `[patch]` B9: Current brief predates the completed gate correction/full round. Refresh current result and next acceptance check; retain parallel owners.
  - `[medium]` `[patch]` I1: Off-centre approximately7mm recovery/energy has no direct60/120/240 closure. Add the analytically positioned tilted-cylinder rim fixture at every cadence, using unchanged physical bounds and independently derived settling duration.
  - `[false]` `[reject]` I2: The stated design bounds requested correction and explicitly does not assert a universal angular/island energy theorem. Existing energy gates and added off-centre physical controls enforce the unchanged applicable envelope; no false universal guarantee is made.
  - `[false]` `[reject]` I3: Same source-backed distinction as B1: the old gross-overlap setup is not the accepted resting-ball premise, and its residual remains explicit. No intended release bound or supported resting behavior is relaxed.
  - `[medium]` `[patch]` I4: Same exact-input issue as B7. The committed scoped patch will reproduce P3 from the published base while keeping friction implementation/publication separate.

Six medium patch groups (B2, B3, B4, B7/I4, B8, I1) and one low brief correction (B9) returned to the original implementer and are now applied. The repair evidence below records 61 passing affected cases, unchanged runtime and exact P3 input capture. No intent gap, spec loopback or deferred finding. Follow-up review is required to independently check the newly derived cap-boundary and off-centre assertions; final full native session 11843 is running. Publication remains pending.



### 2026-10-26 — Follow-up review pass 1

Independent layers: `/root/penetration_blind_r1`, `/root/penetration_edge_r1`, `/root/penetration_verification_r1`, `/root/penetration_intent_r1`, `/root/penetration_forward_r1`. All five launched before triage. Edge Case, Verification Gap and Forward-Only found no violations/gaps. The intent auditor confirmed direct coverage of the previously missing off-centre/cadence surface.

- verdicts: 14 findings — high 0, medium 4, low 2, false 8, maybe-false 0
- findings:
  - `[medium]` `[patch]` B1: A final-only loaded rest assertion can miss an oscillation during the independently predicted settled interval. Apply the existing penetration/speed/spin limits continuously over a final interval after the derived settling time, without widening them.
  - `[medium]` `[patch]` B2: The off-centre test describes a one-second resting hold but checks speed/spin only at its last sample. Apply the existing rest limits throughout the stated hold, retaining the original derived time budget.
  - `[medium]` `[patch]` B3: The D6 Resting helper asserts translation but omits spin. Add the applicable existing angular-rest envelope through the committed read; retain all timing/release bounds.
  - `[false]` `[reject]` B4: The initial placement independently proves both tangencies; the unchanged held axial guard, rest predicate and closed-supply state then prove the ball remains held upstream before controlled opening. The existing release/no-throwback outcome is the source acceptance. No alternate unsupported resting configuration was shown reachable here.
  - `[low]` `[patch]` B5: Add a stationary positive-gap control inside the existing speculative margin, using a literal small separation. This extends the declared quiet boundary without changing any production branch.
  - `[false]` `[reject]` B6: The named clear-contact control deliberately uses one interval whose analytic fall is strictly shorter than the5mm gap at every cadence. This is a bounded negative control, not a claim to trace every preimpact instant; requiring free fall through a crossing substep could incorrectly reject legitimate speculative contact.
  - `[false]` `[reject]` B7: The zero-motion pair's explicit analytic energy check is correctly implied by stricter velocity/spin bounds; it is not a tautology and a defect producing energy cannot pass all those guards. Loaded and off-centre tests provide independent nonzero-energy sensitivity. No additional energy guarantee is claimed for the quiet pair.
  - `[false]` `[reject]` B8: The new cadence test explicitly compares separation. Each cadence's existing pair cases also bound center of mass, transverse displacement/velocity and angular motion against the same authored reference. Common translation/rotation cannot escape that combined closure; no broader bit-identical trajectory claim is made.
  - `[medium]` `[patch]` B9: Lifecycle replay repeats physical states but its independent final outcome omits rest and lateral position. Add the existing rest bounds and independently rounded authored lateral coordinates for every replay, preserving semantic equality.
  - `[low]` `[reject]` B10: The requested fix edits this build's spec and is rejected under Auto. The chronological sections already state that prior full results predate review repairs; finalization will name one current matching build/round without erasing historical receipts.
  - `[false]` `[reject]` I1: Carried prior I2: the design expressly bounds requested correction rather than claiming a universal island theorem. Physical energy gates enforce the resulting body-state envelope.
  - `[false]` `[reject]` I2: P3 is explicitly integration evidence from the retained friction candidate, now reproducible from its scoped patch and baseline; the generic direct off-centre test complements rather than substitutes for exact case179.
  - `[false]` `[reject]` I3: Carried prior B1/I3: source acceptance is the resting-ball release interaction, and the admitted gross-overlap residual remains explicit; no intended release bound was relaxed.
  - `[false]` `[reject]` I4: Candidate evidence is not terminal delivery, and the spec explicitly leaves exact-SHA publication pending. That distinction is correct at review time.

Four medium and one low patch entry returned to `/root/penetration_dev`; all five are applied, with 66 affected checks passing and no changed runtime, limits or duration. Follow-up patch counts: high 0, medium 4, low 1. No additional full review loop is recommended under Auto's follow-up rule; the retained Verification layer checks these affected fixes independently. Final verification will use the declared BrowserDeferral filter, after one integration of published main; CI/artifact/deployment remain pending. No deferred finding.

## Design Notes

Selected initial design after independent read-only diagnosis: use one physical cap from ContactSlop divided by the longest supported committed interval: .0002/(1/60)=.012 m/s. Engine commits 8/4/2 substeps at 60/120/240 Hz, giving requested correction budgets .2/.1/.05mm. A 120 Hz-only derivation (.024m/s) misses the source's per-committed-tick obligation and is rejected before implementation.

At h=1/480,Hertz60,zeta10, BiasRate≈18.14/s; .012 cap engages near .862 mm total penetration. Ordinary estimated static sag .2mm+g/(2π60)^2≈.269mm remains below cap/envelope. This bounds an isolated translational row's requested positional correction, not all angular/friction/island energy. Existing physical gates decide sufficiency. Do not present it as a universal energy certificate or tune after observing failing results.

The friction failure is single rim/edge feature 0x4042 at 6.4–6.9mm around ticks 97–98. Biased integration retains upward positional work while relaxation returns committed velocity nearly to freefall. Initial case 179 q=(.36008772,.65099156,.010655787,.66815656), spin=(−.4708124,−2.6997364,−1.1430255), height=.31139722, offset=(.18465194,0,.047059108); original gate controls seed 91000, trajectory seed 91002 and 179×12 draws. Full scene/test code is retained in the friction worktree. At the planning boundary no normal-law change had been applied there; the completed exact-delta integration is recorded below.

## Verification

- One matching incremental `dotnet build CuriousContraptions.slnx -c Debug --nologo -v quiet`; 0 warnings/errors.
- Affected native tests with `dotnet test CuriousContraptions.tests/CuriousContraptions.tests.csproj -c Debug --no-build --no-restore`, selecting the new recovery tests and relevant resting/cylinder/restitution/Weight/hollow controls. Record exact commands/totals.
- Coordinator-arranged exact-delta friction integration: isolated case 179, unchanged full energy gates, original/reversed drop and affected geometry/lifecycle. Reuse build until relevant inputs change; no mutants or probes copied to modified trees.
- Coordinator final required round at the frozen integrated boundary: generate the filter with `dotnet run --project tools/E2eRunner --no-build -c Debug -- deferred-filter`, then pass that exact value to `dotnet test CuriousContraptions.slnx -c Debug --no-build --no-restore --nologo -v minimal -m:1 --filter "$pen_native_filter"`. This excludes only the browser-toolchain methods on the single existing BrowserDeferral list; every game test and non-browser runtime node harness runs, Skipped: 0. Earlier unfiltered local rounds are historical results and included 16 extra deferred toolchain cases.
- `git diff --check`, scoped deletion/type checks, independent behavioural matrix and Auto layers; normal hooks. No local Release/web publish.

## Implementation evidence — 26 October 2026

Implementation owner: `/root/penetration_dev`; dedicated worktree based on `8ac611ffe81a74af7d36328bf69cc6403065b6d9`. No commit, push, browser run, mutant, local Release worker build or web publish. Coordinator owns final native round, five Auto layers, fixes, exact-SHA approval and publication.

### Delivered closure and physical oracle

- Runtime delta: replace `MaxPushoutSpeed = 3f` with `ContactSlop / (1f / 60f)`, plus the derivation comment. `NormalBranch`, speculative/slop-band behavior, relaxation, clocks, caches and all other solver paths remain unchanged. Deleted legacy: the arbitrary 3 m/s recovery cap.
- New explicitly registered `WorkshopPhysicsLawRecoveryTests`: 48 cases. Loaded 7 mm sphere/box/upright-cylinder contacts at 60/120/240 Hz and friction 0/.5; oblique sphere pairs with static/dynamic support and both frictions; literal clear/contact/slop-band controls; analytic free-fall/no-support controls; copied semantic fresh/reused/Reset replay.
- Loaded recovery expectations use the isolated loaded-row estimate, corrected after the coordinator caught an erroneous formula in the first comment: with omega = 2π60, h = 1/480, zeta = 10 and a2 = h omega (2 zeta + h omega), massScale = a2/(1+a2) = .9422793 and impulseScale = 1/(1+a2) = .05772068. The loaded advance is massScale × cap − impulseScale × g h = massScale × (cap − g/[omega (2 zeta + h omega)]). The load bias is .00125193 m/s and capped advance .01012769 m/s. Recovering from 7 mm to the .86162 mm cap transition takes .6061 s. The soft tail rate massScale × biasRate = 17.0904/s tends toward .269025 mm static sag and reaches .5 mm in .05514 s. The earlier expression g/(omega² h) actually evaluates to .033132 m/s, not .00133, and was incorrect; no passing assertion was retuned. Assertions require >1 mm progress at .25 s and rest inside unchanged .5 mm penetration by 2 s; no upward launch beyond .5 mm. Every loaded committed tick obeys the unchanged unit-mass g × .2 mm energy-rise limit.
- Energy is independently calculated from copied committed states, analytic sphere/cube/cylinder moments, and host double summation. Production remains f32. Host precision prevents rounding a roughly 2 J total from hiding a small breach of the 1.962 mJ limit.
- Oblique sphere normals pass through the centers; these cases prove direction and equal/opposite recovery, not angular leverage. The unchanged tilted-cap/rod/rim tests and the retained friction candidate's exact rim/edge case 179 supply angular-leverage coverage. No multi-body global energy qualification is claimed.
- Initial oblique assertions incorrectly used a 1 um measurement tolerance for accumulated positions/center of mass across 960 additions. That oracle was incomplete, not evidence of a production defect. The corrected independent accumulation bound is 960 × .4 m × 2^-24 = 22.9 um; a separate direct lateral-velocity assertion remains 1 um/s, and the per-tick geometric increment tolerance remains 1 um. No acceptance energy, resting, seed or lesson bound was widened.
- Numeric authority, general-design law, TODO, epics, sprint and deferred-work ownership now identify this prerequisite and preserve the pending friction review/publication and post-catalogue browser gates. One old row-test comment now names the .012 m/s cap; its assertions are unchanged.

### Local commands and results

All commands ran in this worktree. Matching outputs were reused until test source changed.

| Command/check | Result |
| --- | --- |
| `dotnet build CuriousContraptions.slnx -c Debug --nologo -v quiet` | Failed MSB4166: all 15 child nodes exited prematurely, 0 warnings/15 errors, 5.35 s. The reported diagnostic directory was absent when inspected. No tests ran on this failed build. |
| `dotnet build CuriousContraptions.slnx -c Debug --nologo -v quiet -m:1` | Passed, 0 warnings/errors, 24.76 s. Serial worker scheduling avoided the failed parallel-node setup. |
| `dotnet test CuriousContraptions.tests/CuriousContraptions.tests.csproj -c Debug --no-build --no-restore --filter 'FullyQualifiedName~WorkshopPhysicsLawRecoveryTests' --nologo -v minimal` | Initial oracle: 33 passed, 12 failed, 0 skipped, 852 ms. All failures were the unsupported accumulated-position/COM tolerance described above. |
| `dotnet build CuriousContraptions.tests/CuriousContraptions.tests.csproj -c Debug --no-restore --nologo -v quiet -m:1` | Corrected position oracle: 0 warnings/errors, 10.69 s. |
| Affected closure command below (session 20992) | 364 passed, 0 failed, 0 skipped, 1m55s. |
| Same incremental test-project build after host-double energy oracle and three no-support controls | 0 warnings/errors, 8.99 s. |
| Same recovery-only test command (session 13504) | Final 48 passed, 0 failed, 0 skipped, 921 ms. |
| Final `dotnet build CuriousContraptions.slnx -c Debug --nologo -v quiet -m:1` after all source/comment edits (session 85680) | 0 warnings/errors, 8.83 s; exact frozen outputs ready for coordinator full round. |
| `git diff --check`; scoped search for old `MaxPushoutSpeed = 3f` and obsolete normal-law cap documentation | Pass; no old-cap matches. |

Affected closure command:

```sh
dotnet test CuriousContraptions.tests/CuriousContraptions.tests.csproj -c Debug --no-build --no-restore --filter 'FullyQualifiedName~WorkshopPhysicsLawRecoveryTests|FullyQualifiedName~WorkshopPhysicsLawRestitutionTests|FullyQualifiedName~WorkshopPhysicsLawHollowTests|FullyQualifiedName~WeightTests|FullyQualifiedName~F32BehaviourIdentityTests|FullyQualifiedName~LaunchLessonSolveTests|FullyQualifiedName~BoxesDroppedOntoAFaceStayPutAtRest|FullyQualifiedName~LyingDropsComeToRest|FullyQualifiedName~ATiltedCapLands|FullyQualifiedName~ARodRightsItself|FullyQualifiedName~DroppedOnItsSide|FullyQualifiedName~UprightBoxSetAbove|FullyQualifiedName~InRelaxABandPoint|FullyQualifiedName~InTheSolvePassABandPoint|FullyQualifiedName~AMixedBandAndSoft' --nologo -v minimal
```


The final comment-only edit to `WorkshopPhysicsLawTests` followed the test runs; the final 8.83 s matching solution build covers that trivia change. The final 48-case run strengthens only new-test oracles; the 364-case run's other tests and runtime inputs remain unchanged. Anvil allowed every write; one proposed comment patch was refused for a wrong line number, corrected and then allowed before writing.

### Retained friction integration

The coordinator instructed retained owner `/root/friction_dev` to apply exactly the replacement of `MaxPushoutSpeed = 3f` by `ContactSlop / (1f / 60f)` and its two derivation comments in `story-engine-two-point-friction`; no full friction patch entered this worktree. Matching native project build: 0 warnings/errors, 8.07 s. Exact case179 + original/positive/reversed Weight drop + flush + saved reversal: 6 passed, 0 failed, 0 skipped, 9.95 s. The unchanged full Dead/Rolling energy gate passed (1m42s), as did the 60-box and 200-lying-drop controls. The final response closure (session 94131) passed **252 tests, 0 failed, 0 skipped, 5.2708 minutes**; the original Lively gate passed in 50 s. Together with the nonoverlapping early six, P3 has **258 passing cases** at unchanged seeds and bounds. The original local evidence is `story-engine-two-point-friction/_bmad-output/implementation-artifacts/spec-engine-two-point-friction.md`, section `P3 prerequisite integration — 26 October 2026`; it is an unpublished sibling worktree provenance location, not a repository link. The commands and identities needed to assess this receipt are recorded here. It covers all friction/hollow/restitution fixtures, static/dynamic supports, rest/rocking/rim energy and semantic lifecycle. Prior geometry-only proof is reused because this delta does not change geometry; physical response proof was rerun. This establishes candidate P3 integration, not friction publication.

### Remaining enforcement

P1/P2/P4 have scoped native evidence above; P3 has the retained-candidate energy/physical closure receipt. P5 requires coordinator's full solution run, all non-browser node harnesses, independent Auto review and exact-SHA CI/artifact/deployment. No implementation Pass, browser, device, performance or global qualification is claimed. Unchanged failed historical results remain above and in their original records.


## Correction round — resting gate fixture and derivation

Coordinator's full run (session 32431), `dotnet test CuriousContraptions.slnx -c Debug --no-build --no-restore --nologo -v minimal`, exited 1: main 3,766 passed / 2 failed / 0 skipped (3,768 total, 8m48s); Coverage 77 passed (639ms), E2eRunner 208 passed (22s), WorkerAot 17 passed (41ms), Bundle 206 passed (336ms). Overall **4,274 passed / 2 failed / 0 skipped**, 4,276 total. Only the two authored-enable variants of `ABallRestingAgainstTheClosedTiltedBladePassesDownstreamWhenEnabledDuringRun` failed: held axial position −.0917186663 versus unchanged [−.5, −.39].

**Source-backed fixture correction, authorized by the coordinator after the full run:** [Story 9A.19](../planning-artifacts/epics.md#story-9a19-declared-prismatic-drive-for-the-powered-gate) requires a ball resting against the closed tilted blade to pass downstream without entry-mouth ejection. The [original C-Gate decision](spec-c-gate.md#current-state), item 5, explicitly distinguishes an initially interpenetrating ball from that resting-ball scenario and records gross initial overlap as a separate residual.

The old world center (−.08, 3, 0) at −15° gives local axial center −.077274 m. With radius .34 m and blade half-thickness .06 m it starts .322726 m inside the blade. Its held assertion demanded >.3127 m correction in 1.5 s, while the new normal law requests at most .018 m. That was a hidden gross-overlap recovery precondition, not the source's resting-ball premise.

The corrected fixture uses independently declared dimensions: Basketball radius .34 m (`elements/Ball/ball.tscn`), blade thickness .12 m, sleeve bore .70−.05=.65 m and collar bore .78−.13=.65 m (`elements/PoweredGate/powered_gate.tscn`). Its initial gate-local center is (−(.06+.34), −(.65−.34), 0) = **(−.4, −.31, 0)**, transformed by the existing mount pose. It is tangent to both blade and bore floor. Initial signed-clearance assertions allow only 1 um f32 authoring/transform roundoff at the 3 m mount (below .1 mm placement slop); pre-enable speed must be below the existing .01 m/s rest envelope. Gravity locally points downstream and down (+2.539, −9.476 m/s²), loading both surfaces before opening. The original 180/300 ticks, held [−.5,−.39], no-throwback .02 m and downstream >.49 m bounds stay unchanged. Reset's expected coordinate now derives from the new initial geometry; saved/fresh/reused semantic replay remains required.

**Residual not fixed or rejected:** generic solid-blade overlap is currently admitted. `WorkshopPhysicsCompiler` rejects hollow-wall overlap, and `PlacementPose` checks other solid overlaps after a horizontal rest lift; neither generally rejects the old tilted-blade placement. This slice does not claim global placement rejection or fast recovery from gross authored overlap.

### Self-contained P3 integration commands

Provenance: owner `/root/friction_dev`, retained uncommitted candidate on baseline `8ac611ffe81a74af7d36328bf69cc6403065b6d9`, with only this prerequisite's exact normal-cap substitution/comments added for integration. Build `dotnet build CuriousContraptions.tests/CuriousContraptions.tests.csproj --no-restore -v quiet`: 0 warnings/errors, 8.07 s.

Early command (session 52824; 6 passed, 0 failed/skipped, 9.9493 s):

```sh
dotnet test CuriousContraptions.tests/CuriousContraptions.tests.csproj --no-build --no-restore --filter 'FullyQualifiedName~DeadCylinderBoxRimRegressionStaysWithinTheUnchangedControlBudget|FullyQualifiedName~OnItsSideItRollsDownARamp|FullyQualifiedName~ReversingTheGeneratorAxisAndSavingTheDrop' --logger 'console;verbosity=normal'
```

Response command (session 94131; 252 passed, 0 failed/skipped, 5.2708 minutes):

```sh
dotnet test CuriousContraptions.tests/CuriousContraptions.tests.csproj --no-build --no-restore --filter 'FullyQualifiedName~FreshSeedDropsGainNoMoreEnergyThanTheBoxControlAndTheSlop|FullyQualifiedName~FreshSeedDropsWithRestitutionGainNoMoreEnergyThanTheBoxControlAndTheSlop|FullyQualifiedName~WorkshopPhysicsLawFriction|FullyQualifiedName~WorkshopPhysicsLawHollowTests|FullyQualifiedName~WorkshopPhysicsLawRestitutionTests|FullyQualifiedName~TwoRunsAndAResetReadmission|FullyQualifiedName~LyingDropsComeToRest|FullyQualifiedName~BoxesDroppedOntoAFace|FullyQualifiedName~ACylinderTumblingOverACapRim|FullyQualifiedName~ATiltedCapLands|FullyQualifiedName~ARodRightsItself|FullyQualifiedName~ABoxTumblingOverACapRim|FullyQualifiedName~DropsOntoALyingCylinder|FullyQualifiedName~InRelaxABandPoint|FullyQualifiedName~InTheSolvePassABandPoint|FullyQualifiedName~AMixedBandAndSoft|FullyQualifiedName~ResetRestoresTheBuiltPosesAndAReplay|FullyQualifiedName~DroppedEndDownItRests|FullyQualifiedName~AStackEndDownHolds' --logger 'console;verbosity=normal'
```

### Correction verification and frozen return

- Matching solution build: `dotnet build CuriousContraptions.slnx -c Debug --nologo -v quiet -m:1` (session 58842), **0 warnings/errors, 10.97 s**.
- Affected command: `dotnet test CuriousContraptions.tests/CuriousContraptions.tests.csproj -c Debug --no-build --no-restore --filter 'FullyQualifiedName~DeclaredPrismaticDriveTests|FullyQualifiedName~WorkshopPhysicsLawRecoveryTests' --nologo -v minimal` (session 27464), **64 passed, 0 failed, 0 skipped, 1 s**. Both D6 rows satisfy initial tangency, resting speed, all unchanged release bounds, Reset and saved/fresh replay.
- `git diff --check` passes. No runtime change was made in this correction round; only the source-backed gate test, recovery derivation comments and this evidence changed. Anvil rejected one formula-comment patch for an incorrect line location; full proposed content was then validated and allowed before applying.
- Code and tests are frozen on the matching build above. The coordinator owns the next full native round and five Auto review layers. The failed full-round receipt remains evidence; no terminal implementation/publication Pass is claimed.


## Auto review repairs — 26 October 2026

The coordinator's pre-repair full run (session 85155) passed **4,276 tests, 0 failed, 0 skipped**: main 3,768 (8m58s), Coverage 77 (671ms), E2eRunner 208 (23s), WorkerAot 17 (52ms), Bundle 206 (353ms). Five Auto layers then identified coverage/evidence gaps; this round adds only tests/evidence, with no runtime change or weakened existing bound.

- **Cap transition:** three gravity-free sphere controls at independently derived d* = .2 mm + .012/b = .8616198 mm, offset −20/0/+20 um. The oracle solves two compliant substeps at 240 Hz from rest using physical h=1/480, omega=2π60 and zeta=10. The 1e−7 m allowance covers six f32 ulps at .2 m (8.94e−8 m). The above-cap case's uncapped prediction differs by more than four allowances, explicitly proving sensitivity; no observed result sets a bound.
- **Cadence:** four equal-scene static/dynamic, zero/nonzero-friction pair cases compare eight intermediate separations every .25 s through 2 s across all three cadences. The difference allowance covers two trajectories × two bodies × 960 coordinate additions below .4 m × 2^-24 plus 1 um distance observation rounding.
- **Oblique angular/energy:** each existing pair case now checks spin against a conservative zero-torque arithmetic bound N × 20 × u × cap/r. It comes from 8u r cap impulse-moment error per substep times spherical inverse inertia 5/(2r²). Both bodies' translational and analytic rotational kinetic energies are summed and constrained by the existing total contact-mass slop envelope.
- **Loaded correction rate:** every existing loaded case now limits upward height change per tick to .012/ticksPerSecond plus 1 um near-origin coordinate/substep rounding. The energy and final rest bounds remain unchanged.
- **Off-centre deep contact:** six cylinder/plane cases start 7 mm deep with r=h=.25 m and 5° tilt. The initial lowest-rim arm is .22726 m, and the other rim is 43.58 mm higher, outside the speculative margin. Each cadence/friction case asserts ≥1 mm recovery by .25 s, unchanged 5% transient and .5 mm resting penetration, total analytic kinetic/potential energy rise ≤mg×.2 mm each tick, <1° settled tilt and rest speed/spin. The time budget is full-depth recovery at the independently derived .01012769 m/s plus the existing inelastic rocking model, followed by one second holding the resting envelope. The first execution passed; no threshold or geometry was fitted to observations.

### Exact P3 input artifact

[Retained friction candidate inputs](engine-single-point-penetration-friction-inputs.patch) is one actual scoped patch against published baseline `8ac611ffe81a74af7d36328bf69cc6403065b6d9` (also the sibling's unchanged HEAD). It captures the runtime/native-test/project inputs present for P3 sessions 52824 and 94131, including the exact normal recovery prerequisite. Its scope is:

- Production: `WorkshopCollisionKernels.cs`, `WorkshopConstraintSolver.cs`, `WorkshopPhysicsEngine.cs`, and added `WorkshopFrictionSolver.cs`.
- Native tests/project: `CuriousContraptions.tests.csproj`, `WeightTests.cs`, `WorkshopPhysicsLawCylinderTests.cs`, `WorkshopPhysicsLawHollowTests.cs`, `WorkshopPhysicsLawTests.cs`, and added `WorkshopPhysicsLawFrictionTests.cs`.

Paths retain their actual `CuriousContraptions.Simulation/` or `CuriousContraptions.tests/` prefixes in the patch. Tracking/evidence documents are excluded; all other dependencies are the named baseline. Extraction used `git diff --binary --unified=0 <baseline> -- CuriousContraptions.Simulation CuriousContraptions.tests` plus real new-file diffs for the two untracked source files. The zero-context form avoids whitespace-only context lines in the committed patch artifact. Apply it with `--unidiff-zero`. Read-only `git apply --unidiff-zero --check --cached <artifact>` against the sibling's baseline index and `git apply --unidiff-zero --reverse --check <artifact>` against its actual candidate both passed. The sibling was not changed. Committing this artifact binds the P3 inputs; it does not approve or publish the friction outcome.

### Affected verification and return

- `dotnet build CuriousContraptions.tests/CuriousContraptions.tests.csproj -c Debug --no-restore --nologo -v quiet -m:1` (session 3260): **0 warnings/errors, 7.25 s**.
- `dotnet test CuriousContraptions.tests/CuriousContraptions.tests.csproj -c Debug --no-build --no-restore --filter 'FullyQualifiedName~WorkshopPhysicsLawRecoveryTests|FullyQualifiedName~ADeepOffCentreRimRecoversWithinTheEnergyEnvelopeAtEveryCadence' --nologo -v minimal` (session 48471): **61 passed, 0 failed, 0 skipped, 1 s**.
- No further full suite was run by the implementation owner. The coordinator owns final matching full verification, re-review and exact-SHA approval/publication. The previous full result predates these review repairs.

## Focused follow-up verification — 26 October 2026

Coordinator full session 11843, before this follow-up, passed **4,289 tests, 0 failed, 0 skipped**: main 3,781 (9m15s), Coverage 77 (674ms), E2eRunner 208 (23s), WorkerAot 17 (44ms), Bundle 206 (356ms). The unfiltered local E2eRunner total included 16 explicitly deferred browser-toolchain cases; it is not browser qualification. The coordinator's final required run will use the existing `BrowserDeferral.CiFilter`, generated by `dotnet run --project tools/E2eRunner --no-build -c Debug -- deferred-filter`, preserving that named browser deferral without excluding game-system tests.

The focused follow-up keeps every existing limit and duration: loaded contacts now hold penetration/speed/spin rest throughout the final second (after the independent .662 s estimate); off-centre contacts hold speed/spin throughout their existing settleTick-to-end interval; D6 checks committed angular speed <.02 rad/s as well as linear rest; quiet controls add literal +.0001 m inside the speculative margin; each lifecycle replay independently satisfies final rest and authored x=.4,z=0 within 960×.4×2^-24 positional rounding, alongside the retained semantic comparisons.

- Matching native project build: `dotnet build CuriousContraptions.tests/CuriousContraptions.tests.csproj -c Debug --no-restore --nologo -v quiet -m:1` (session 7928), **0 warnings/errors, 9.69 s**.
- Affected check: `dotnet test CuriousContraptions.tests/CuriousContraptions.tests.csproj -c Debug --no-build --no-restore --filter 'FullyQualifiedName~WorkshopPhysicsLawRecoveryTests|FullyQualifiedName~ADeepOffCentreRimRecoversWithinTheEnergyEnvelopeAtEveryCadence|FullyQualifiedName~ABallRestingAgainstTheClosedTiltedBladePassesDownstreamWhenEnabledDuringRun' --nologo -v minimal` (session 39558), **66 passed, 0 failed, 0 skipped, 1 s**.
- `git diff --check` passes. No runtime edit, full run, merge, commit or push by this implementation owner. The retained Verification layer reviews these focused fixes; coordinator merges published hash-test main once after its terminal receipt, then runs final verification and handles committed-SHA approval/publication.

## Final published-main integration and required verification — 26 October 2026

Integrated published `origin/main` **once**, by `git merge --ff-only --autostash origin/main`, from `8ac611ffe81a74af7d36328bf69cc6403065b6d9` to `b3905fcda3df89b38140c9610b8ff2034e4078a8`. The sole conflict was TODO; its resolution preserves the penetration outcome and hash-test inventory/publication ownership. Sprint and epics merged cleanly and retain both slices. Current views now record the hash slice's terminal Pass; its coordinator-authored terminal receipt is included in the inherited hash spec, which this implementation owner did not edit.

**Source drift and review applicability:** the tracked solver/test/project closure is unchanged against the pre-integration autostash `8a13f46d`. The coordinator independently compared all six final reviewed runtime/test/project diff entries, including untracked `WorkshopPhysicsLawRecoveryTests.cs`, with the final reviewed revision and found every entry exactly equal. The P3 artifact remains intact and anchored to its original 8ac baseline; frontmatter now names b390 as the current integration baseline. The retained independent reviewer `/root/penetration_verification_r1` gave explicit **Pass** on the final affected fixes with no gaps or weakened oracles; this integration introduced no source changes that invalidate that review.

Matching incremental build:
`dotnet build CuriousContraptions.slnx -c Debug --nologo -v quiet -m:1` (session 74852): **0 warnings/errors, 31.64 s**.

Final full required command (session 24325):

```sh
penetration_browser_deferral_filter="$(dotnet run --project tools/E2eRunner --no-build -c Debug -- deferred-filter)"
dotnet test CuriousContraptions.slnx -c Debug --no-build --no-restore --nologo -v minimal --filter "$penetration_browser_deferral_filter"
```

**Exit 0; 4,276 passed, 0 failed, 0 skipped.**

| Project | Passed | Duration |
| --- | ---: | --- |
| CuriousContraptions.tests | 3,784 | 9m6s |
| Coverage.Tests | 77 | 678ms |
| WorkerAotGuard.Tests | 17 | 45ms |
| BundleElements.Tests | 206 | 414ms |
| E2eRunner.Tests, including non-browser NodeHarnessTests | 192 | 14s |

Every game-system test runs. The generated existing filter excludes only the 15 listed browser-toolchain methods/16 cases in `BrowserDeferral`, with their named post-catalogue re-enable gate. Compared with the preceding unfiltered 4,289-case round, this candidate adds the three positive speculative-gap cases and explicitly defers those 16 browser-toolchain cases: 4,289 + 3 − 16 = 4,276. No skip or hidden game-test exclusion is introduced.

Final source is frozen; no extra tests, local Release worker build or web publish ran. Implementation verification/review is complete, while Task 5's normal-hook commit, committed-SHA approval, CI/artifact/deployment and terminal publication remain coordinator-owned and pending. Browser/device/performance qualification stays at its named later gates. No commit or push was made by this implementation owner.

## Auto Run Result

**Implementation review Pass; publication pending.** Replaced the shared 3 m/s recovery cap with the independently derived 0.012 m/s bound. Existing contact branches, clocks, seeds and physical acceptance limits remain. The D6 test now arranges the source-required resting ball analytically, preserving its release timing and bounds; gross solid-overlap admission remains a separately recorded residual.

Files changed: `WorkshopConstraintSolver.cs` changes only the cap and its explanation; the new registered `WorkshopPhysicsLawRecoveryTests.cs` proves recovery, energy, cadence, transition boundaries, quiet/free-fall controls and semantic replay; cylinder tests add deep off-centre recovery at all cadences; prismatic-drive tests establish the accepted tangent/resting premise and rest checks; the existing law-test comment and numeric/design documents describe the same rule. TODO, sprint, epics and deferred work preserve current ownership. The scoped P3 patch records exact historical friction integration inputs, without importing its implementation. The inherited hash-test spec adds only its successful terminal publication receipt.

P1/P2 are covered by real production loaded/oblique/off-centre recovery, per-tick energy/displacement and continuous rest, equal-time cadence comparison, cap-transition and quiet/free-fall controls. P3 passed 258 exact retained-friction integration cases at unchanged bounds, with original inputs retained against `8ac611ffe81a74af7d36328bf69cc6403065b6d9`. P4 uses copied typed states plus independently expected rest/position on each replay. P5's required native verification and review are complete; publication-only checks remain below. Friction's subsequent enum changes retain their own affected verification and full review obligation.

All five Auto layers ran in each review round; every finding and rejection has individual evidence in the triage log. Initial pass:13 findings (7 medium,1 low,5 false), six medium repair groups and one low repair. Follow-up:14 findings (4 medium,2 low,8 false), four medium and one low repairs applied; the spec-only request was rejected under Auto. No high finding or deferred entry. Retained Verification reviewer `/root/penetration_verification_r1` independently returned **Pass** for the final affected fixes, finding no gaps or weakened oracles. **Follow-up review recommended: false.**

The once-only integration of published main `b3905fcda3df89b38140c9610b8ff2034e4078a8` preserved all six reviewed runtime/test/project diff entries exactly, independently compared by the coordinator. Only TODO conflicted; both ownership records were retained. Original review/P3 baseline and historical failures remain recorded.

Final matching incremental Debug solution build (session74852): **0 warnings/errors**,31.64s. Final required suite (session24325), using the exact existing `E2eRunner deferred-filter` and `--no-build --no-restore -m:1`: **4,276 passed,0 failed,0 skipped**. Main3,784/9m6s; Coverage77/678ms; WorkerAotGuard17/45ms; BundleElements206/414ms; E2eRunner192/14s, including required non-browser node harnesses. The difference from historical4,289 is three added quiet boundary cases minus16 explicitly deferred browser-toolchain cases. Every game test ran. `git diff --check` passes; all prior failures and repairs remain recorded.

No local Release worker build/web publish, browser qualification or mutation testing was performed. Ordinary native correctness is scoped to the verified closure; this does not claim universal island energy, gross-overlap admission, performance/device or browser qualification. Friction's full review/publication and cap-block retry remain distinct outcomes.

Remaining finalization: normal commit protections and review Pass bound to the committed SHA. Publication-only checks: exact-SHA CI including production build/publish, Pages artifact/deployment identities and deployed static artifact comparison. A short terminal receipt follows those checks; native Pass alone does not establish publication.
