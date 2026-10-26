---
title: 'Game-grade float comparisons and resting observations'
type: 'refactor'
created: '2026-10-26'
status: 'done'
baseline_revision: 3462ad836dccffc1e1328b1ed99de490b4336786
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '/Users/aidan/dev/personal/tim/.claude/worktrees/story-game-grade-float-comparisons/AGENTS.md'
  - '/Users/aidan/dev/personal/tim/.claude/worktrees/story-game-grade-float-comparisons/docs/testing-guidance.md'
  - '/Users/aidan/dev/personal/tim/.claude/worktrees/story-game-grade-float-comparisons/docs/gpu-f32-physics.md'
  - '/Users/aidan/dev/personal/tim/.claude/worktrees/story-game-grade-float-comparisons/docs/architecture/element-package-isolation.md'
warnings: [oversized]
deferred: []
---

<intent-contract>

## Intent

**Problem:** The owner asks for intelligent float checks balancing accuracy against churn: this is an accurate-enough physics puzzle game. Existing resting tests demand nanometre displacement while the gameplay envelope allows 0.5 mm contact slop and 0.05 mm stopped displacement. Ad-hoc float checks also mix different physical quantities.

**Approach:** Establish quantity-aware finite comparison policies, apply the first bounded adoption to three resting observations and existing bore compatibility, and classify remaining comparison work. This is the first implementation of the system-wide request, not a claim that every comparison is converted.

## Boundaries & Constraints

**Always:** Named quantities/units, independently chosen limits, real production C# scenarios and positive/control/boundary checks. Preserve exact identity, ordering, canonical Reset/same-input replay, hard admission/gameplay thresholds, intentional zero semantics and independently budgeted energy. Keep all existing scenes, seeds, windows and unrelated assertions. New acceptance is explicitly the owner's game-grade direction, not a claim of f32 error certification. Preserve historical results as history.

**Never:** Universal epsilon substitution, tolerance fitted to outputs, runtime velocity snapping, solver changes, compatibility aliases, output-hash assertions, skipped game tests, mutants, browser gates, local Release worker build or web publish. Warm-up removal is a separately queued owner subtask; preserve solver warm-starting.

## I/O & Edge-Case Matrix

| Scenario | Input/state | Expected behavior | Handling |
|---|---|---|---|
| Settled physical displacement | Nominal horizontal size s in metres | allowance=min(50µm,max(5µm,1e-4*s)) | s must be finite and positive |
| Literal scales | .01,.2,1 m | 5,20,50µm, within independently bounded f32 arithmetic resolution | Inclusive representable threshold; nearest outside refuses |
| Invalid comparison | NaN/infinity or invalid scale | Never approximate-equal | Comparisons false; invalid configuration/scale explicitly rejected |
| Bore compatibility | Two admitted bore radii | Existing inclusive 1µm difference, finite operands | Non-finite operands refuse |
| Genuine slow motion | 0.1mm/s for6s, zero drag/gravity/friction | Approximately0.6mm movement; rest criterion rejects | No motion suppression |
| Excursion then return | Intermediate displacement exceeds allowance | Rest criterion fails | Observe every tick, not endpoint only |

</intent-contract>

## Code Map

Work only in /Users/aidan/dev/personal/tim/.claude/worktrees/story-game-grade-float-comparisons. This is the implementation step of the already-running Auto workflow; do not start a nested workflow or spawn reviewers. Paths below are relative to this fresh worktree at published friction main. Read-only investigation by coordinator and /root/float_policy_scope is complete; use this map rather than rescanning the repository.

- `CuriousContraptions.Abstractions/PhysicalTolerance.cs` (new): small pure C# comparison contract, finite scalar comparisons and named metre policies. Existing shared project is referenced by host/simulation and uses default compilation. No units framework or configurable epsilon platform.
- `engine/TubeMouthSnap.cs:26,65`: sole current BoreTolerance declaration/use. Replace with the named policy and delete duplicate authority, without an alias. CaptureDistance, CaptureAngle, JoinedDistance/JoinedDot and exact Precedes ordering stay unchanged. Real callers: PlacementPose.cs:88 and ui/WorkshopGuidance.cs:263. Anvil caller query returned empty; direct rg established these callers.
- `CuriousContraptions.tests/WorkshopPhysicsLawCylinderTests.cs:382,415,2347`: microtilt, exact-level cylinder and60 seeded boxes. Retain initial settings, admitted tilt checks, angular nongrowth and all random draws. LawWorld.Step starts from admitted bytes on each call: observe each whole settling+measurement interval with ONE call.
- `CuriousContraptions.tests/PhysicalToleranceTests.cs` (new), `TubeMouthSnapTests.cs`, explicit `CuriousContraptions.tests.csproj`: policy literals, invalid values, peak observation controls and actual Find boundary behavior. TestPrecision is a representation guard, not a comparator; leave it.
- `docs/testing-guidance.md`, `docs/gpu-f32-physics.md#game-grade-envelope`: record owner26October policy and current resting acceptance. Previous nanometre results remain historical, not continuing gates. No energy, heading, normal penetration or Weight bounds are loosened.
- `TODO.md`, `_bmad-output/planning-artifacts/epics.md`, `_bmad-output/implementation-artifacts/sprint-status.yaml`: bounded current outcome and next owner-requested warm-up-removal task. Preserve catalogue/order requirements; no new puzzle element.
- `deferred-work.md` and prior friction spec: coordinator-carried warm-up queue entry and terminal receipt are administrative evidence in this slice. Do not overwrite them.
- Remaining comparison closures: VisibleSolidColliderTests/joined-pose geometry; hollow observations (distance constants currently reused for speed/axis); normalization admission; authored anchor agreement; numerical geometry conditioning. Exact sensor/stop thresholds, integer deadlines, energy ULP budgets and worker ring identities retain their contracts. Existing handwritten JS migration is separately owned; no production TS compliance claim.

## Tasks & Acceptance

**Execution:**
1. [x] Shared policy and literal tests — one finite implementation with named physical policies, no generic default epsilon. Select the smallest public surface; only validate configuration actually exposed.
2. [x] Three rest observations — peak horizontal excursion from the original settled reference, using box min(2hx,2hz) or lying-cylinder min(2radius,2halfWidth). Scale is nominal local geometry, never world coordinate or rotated AABB. Replace obsolete31mm restriction with an observation-resolution check: each observed horizontal component's f32 spacing must be at most allowance/8 throughout the window.
3. [x] Controls — preserve all60 seeded boxes; add three held-back boxes selected before output: half-extents(.05,.03,.08),(.1,.07,.15),(.5,.2,.6), masses.5/1/2, friction.1/.3/.6, restitution0/.1/.3, all dropped.01m with yaw45° and horizontal tilts+.03/-.02rad; use the same4+6s window. Add real slow-motion and excursion/return controls.
4. [x] Bore consumer — adopt fixed floor=cap1µm, relative0, through Find. At .65f, exact real1µm difference is not representable: use nearest admitted/rejected representable radii and label them accurately. Preserve valid behavior. Non-finite bore fixtures are already refused by PartDefinition admission (BUNDLE002 / TubeMouths.BoreRadius); assert that boundary without bypassing it, and test both non-finite operands directly through the named policy.
5. [x] Guidance/current views — describe accuracy choices, bounds, units and remaining adoption inventory without global-compliance claims. Delete old nanometre acceptance wording only from current guidance, retaining original historical evidence. Link warm-up removal as next queued work.
6. [ ] Verification/review/publication — affected checks, final full native round, five Auto layers, normal hooks, exact committed-SHA approval and terminal publication.
   - [x] Affected native checks and all five Auto review layers, including independent confirmation of repaired findings.
   - [x] Final complete native round: session34321 exited0,4,409/0/0; matching solution build16108 passed0warnings/errors in6.37s.
   - [ ] Normal hooks, independent committed-SHA approval and terminal publication.

**Acceptance Criteria:**
- C1: Given literal scales and representable boundary values, when the named policy computes/compares, then the matrix results and non-finite rejection hold, including floor-to-relative and relative-to-cap transitions.
- C2: Given the unchanged level/microtilt/60-box scenes, when they run1+9,2+8,4+6seconds respectively, then every measured horizontal excursion is within the named allowance; microtilt angular admission/nongrowth still holds. Given the three fixed held-back scenes, the same rest behavior holds.
- C3: Given a near-origin, drag-free, gravity-free, frictionless box at.0001m/s, when6seconds elapse, then displacement is.0006m±.000005m (the independently declared absolute physical floor), velocity remains positive, and the rest criterion rejects. Given an excursion outside allowance followed by a return, window evaluation also rejects.
- C4: Given admitted compatible or just-outside bore radii and otherwise identical authored mouths, when Find runs, then the existing inclusive1µm rule holds and capture/tie/placement controls remain passing. Given a non-finite authored bore, when PartDefinition admission runs, then it refuses with BUNDLE002 and the BoreRadius non-finite reason; the named comparison policy also rejects either non-finite operand.
- C5: Given the complete changed closure, when native/build/review checks run, then all required tests pass with Skipped:0, no fallback/old alias exists, and current docs distinguish completed first adoption from remaining audit work.

## Spec Change Log

### 2026-10-26 — Non-finite bore admission surface clarified before review

The first affected run passed99 cases and failed6 fixture constructions: PartDefinition rejects non-finite bores with BUNDLE002 / TubeMouths.BoreRadius before Find can run. Correct task4/C4 to assert that actual admission refusal and retain direct policy tests for both operands. Preserve valid representable Find boundaries, all physical scenes/windows and policy limits. No admission bypass or tolerance change.


## Review Triage Log

### 2026-10-26 — Review pass 0
Five Auto layers: /root/float_blind_r0, /root/float_edge_r0, /root/float_verification_r0, /root/float_intent_r0, /root/float_forward_r0. All launched before triage; capacity required two launch waves. Edge returned no findings; Forward-Only confirmed one current path and removal of BoreTolerance.
- verdicts: 14 findings — high0, medium5, low1, false8, maybe-false0
- findings:
  - `[false]` `[reject]` B1: The current spec is deliberately omitted from the common reviewer diff because Auto sends claims only to Edge. It exists in the candidate and was read by the coordinator and Edge; it is not an absent deliverable.
  - `[medium]` `[patch]` B2: X-only negative controls leave the new horizontal metric's Z component unprotected. Add Z production motion and a diagonal helper control whose components are individually within allowance but Euclidean length is outside. Group with independently verified V1.
  - `[medium]` `[patch]` B3: Focused observation controls do not establish the reference-relative contract at a known nonzero reference. Add a resolvable translated reference with independently chosen within/outside displacements, keeping the same physical allowance.
  - `[medium]` `[patch]` B4: Resolution refusal is checked only at a positive-X initial reference. Add negative/Z and later-unresolvable-then-valid observations to verify whole-window persistence, without broadening coordinate qualification.
  - `[false]` `[reject]` B5: Non-finite comparison behavior is directly covered for both operands. In the actual observer, non-finite X/Z also makes Resolved permanently false; the peak retains NaN/infinity and the same tested finite comparator refuses it. Production committed reads already reject non-finite state. The missing Cartesian helper-input test list does not demonstrate a separate unchecked acceptance path; B4 separately covers persistence.
  - `[false]` `[reject]` B6: Floor and transition semantics are already literal public-policy tests, and the real scenes exercise the same composed observer with no size-specific runtime branch. This change does not alter small-body collision behavior. Every policy region need not be crossed with every physics fixture; the preselected three held-back scenes and existing60 scenarios remain.
  - `[medium]` `[patch]` B7: Add a nonzero slow-motion case whose six-second excursion is inside the rest allowance, asserting it still moves. This makes the distinction between tolerated displacement and suppressed velocity directly observable; retain the existing larger-motion rejection control and predeclared5µm position allowance.
  - `[low]` `[patch]` B8: The carried warm-up task still says friction is currently in CI. Correct that current sentence to the now-published friction implementation; no source change.
  - `[medium]` `[patch]` V1: Pre-verified broken-verification gap: replacing horizontal length by abs(X) would preserve current negative controls. Same patch group as B2, with production X/Z and a diagonal-magnitude control.
  - `[false]` `[reject]` I1: Comprehensive adoption remains an authorized sequence of bounded slices, not a claimed completed global audit. Current docs explicitly retain remaining comparison work; this first adoption follows the owner's one-slice delivery rule.
  - `[false]` `[reject]` I2: The actual conversation's churn was repeated microscopic acceptance failures. The requested practical policy is applied to acceptance first, while the owner-authorized friction fix is already separately published. Runtime oscillation elimination is neither implemented nor claimed by this slice.
  - `[false]` `[reject]` I3: Quantity/size-based physical budgets implement the owner's accurate-enough request; a universal operand-magnitude/ULP certificate was not required. The floor/fraction/cap choices and observation-resolution limit are explicit.
  - `[false]` `[reject]` I4: Bounded native evidence is explicitly scoped. No arbitrary-coordinate, global gameplay or repository-wide qualification is claimed; remaining work stays open.
  - `[false]` `[reject]` I5: The owner separately approved the friction correction and explicitly requested the warm-up removal subtask in this same conversation. Their receipt/queue records are authorized administrative carry-forward, not extra runtime changes.

At pass0 triage, four medium patch groups and one low patch required repair by the same implementer; follow-up review was recommended for the shared resting-test oracle. Those repairs are complete and covered by the pass1 five-layer review below: both horizontal axes, translated references, persistent resolution failure and tolerated-but-real motion now have direct controls. Physical limits and simulation code remain unchanged.


### 2026-10-26 — Follow-up review pass 1
Five layers: /root/float_blind_r1, /root/float_edge_r1, /root/float_verification_r1, /root/float_intent_r1, /root/float_forward_r1. All launched before triage. Edge returned no findings; Verification found no gaps; Forward-Only confirmed no violation.
- verdicts: 14 findings — high0, medium3, low2, false9, maybe-false0
- findings:
  - `[medium]` `[patch]` B1: Existing resolution controls do not distinguish allowance/8 from allowance/4. Add independently chosen neighboring coordinates around the actual f32-spacing cutoff (5µm floor: just below8m resolves;8m does not), preserving the declared rule.
  - `[medium]` `[patch]` B2: A 3D-distance substitution could reject legitimate normal settling without a focused horizontal-only control. Add100µm Y-only movement, unchanged X/Z, asserting zero horizontal excursion and accepted horizontal rest.
  - `[medium]` `[patch]` B3: Add negative excursion coverage at a known reference so the new metric's signed-coordinate symmetry is explicit; retain independent literal distances and existing diagonal control.
  - `[false]` `[reject]` B4: carried from r0 B5 — actual finite gating and persistent invalid peak refuse non-finite observation; direct policy tests cover both operands, and later-resolution persistence now has explicit tests. No separate unchecked acceptance path is demonstrated.
  - `[false]` `[reject]` B5: Find already tests mover bores both above and below the target at16/17 representable steps. The production call supplies the two actual radii to the same symmetric absolute comparator, whose operand order is also checked directly. Swapping author labels adds no distinct changed branch or demonstrated gap.
  - `[false]` `[reject]` B6: Tiny positive scales underflow the relative term to zero and are clamped to the tested floor; float.MaxValue times1e-4 remains finite and clamps to the tested cap. Actual authored dimensions have much tighter admission bounds. The requested extreme-fixture expansion is not missing game-domain coverage or a demonstrated overflow/refusal defect.
  - `[false]` `[reject]` B7: The motion control's intended behavior is continued real motion and correct classification, not full integrator qualification. It directly asserts positive directional velocity every tick, independently bounded final displacement and peak-rest classification; no integration algorithm changed. Intermediate rate certification is unnecessary to this predicate adoption.
  - `[low]` `[patch]` B8: Refresh TODO at final freeze so the initial102-case result is historical and the repaired controls/latest matching verification are current.
  - `[false]` `[reject]` I1: carried from r0 I1 — system-wide adoption remains explicit authorized follow-up work; this bounded slice claims no completed global audit.
  - `[false]` `[reject]` I2: carried from r0 I2 — engineering/test churn is the conversation's actual problem. Runtime motion stays unchanged and now has direct within-allowance motion controls.
  - `[false]` `[reject]` I3: carried from r0 I3 — named quantity-specific budgets intentionally retain existing valid bore behavior; intelligent comparison need not turn every existing contract into a relative threshold.
  - `[false]` `[reject]` I4: carried from r0 I4 — native near-origin observations are explicitly bounded, with browser/device/performance and arbitrary-coordinate qualification still pending.
  - `[low]` `[patch]` I5: Clarify attribution in current guidance: the owner requested game-grade accuracy; the floor/fraction/cap values are implementation choices made before outputs under that authority. Do not imply the owner dictated the exact digits. Values and acceptance stay unchanged.
  - `[false]` `[reject]` I6: carried from r0 I5 — friction receipt and warm-up queue entry were separately and explicitly authorized in this conversation.

This is a follow-up pass with three medium and two low patches, no high. **Further full five-layer review recommended: false.** Keep the same implementer; independently inspect the affected controls/documentation after repair. No extra review loop follows merely from patch volume.

## Design Notes

The5µm floor is1/100 of the0.5mm resting envelope; the1e-4 size fraction is0.01%; the50µm cap is the existing stopped-displacement budget and1/10 of contact slop. These are practical acceptance choices made before new outputs, not precision theorems. Bounds stay independent of observation duration; old windows are preserved. A known22µm/s walk would move198µm over9s and still fail the cap. Measurement is near the origin; this does not certify arbitrary-world-coordinate retention.

Test expectations remain literal/analytic; do not calculate an expected result by calling the production predicate under test. A small test observation helper may track peak excursion, with its excursion/return control. There is no runtime rest state or snapping. Energy needs physical work/operation-rounding budgets, orientation needs geometric agreement (separate from normalization), and numerical conditioning needs operation-specific analysis; no percentage-of-total/per-tick free-energy allowance.

## Verification

- Implementer: one matching incremental Debug test-project build, then affected policy/bore/three-rest/held-back/slow-control checks using --no-build --no-restore; report actual counts and failures. Do not run a full suite or commit/push.
- Coordinator: incremental Debug solution build; generate the existing BrowserDeferral filter with `dotnet run --project tools/E2eRunner --no-build -c Debug -- deferred-filter`; run full solution once at the final boundary with --no-build --no-restore --nologo -v minimal -m:1 and that filter. Only the16 named browser-toolchain cases are deferred; game/node tests all execute.
- Review actual diff through Auto's five layers. No separate duplicate reviewer or mutation run. Diff/link/current-contract checks; normal hooks and exact-SHA CI production publish, Pages artifact/deployment/static verification. Browser qualification remains pending.

## Implementation evidence — 26 October 2026

Implementation owner: `/root/float_policy_dev`. Shared finite metre policies, explicit named bore consumption and peak-rest observations are implemented; old BoreTolerance authority and the three nanometre endpoint oracles are deleted. All original scenarios/seeds/windows and angular checks remain. Controls cover literal scales/transitions, invalid scales/operands, representable bore boundaries, upstream BUNDLE002 refusal, excursion-return, unresolved coordinates, three preselected held-back boxes and real slow motion. Matrix rows map to the named PhysicalToleranceTests, TubeMouthSnapTests and changed cylinder tests, all included in the affected run. PlacementPoseTests also ran.

Matching test-project command: `dotnet build CuriousContraptions.tests/CuriousContraptions.tests.csproj -c Debug --nologo -v minimal -m:1`, session19600,0warnings/errors,20.41s. Initial affected session18221:99passed,6failed,0skipped,7s; all failures were impossible non-finite PartDefinition fixtures, preserved in the Spec Change Log. Corrected build with `--no-restore --nologo -v quiet -m:1`, session50362,0warnings/errors,9.90s. Corrected affected session23565: **102 passed,0failed,0skipped**,7s.

Exact affected command: `dotnet test CuriousContraptions.tests/CuriousContraptions.tests.csproj -c Debug --no-build --no-restore --nologo -v minimal --filter 'FullyQualifiedName~PhysicalToleranceTests|FullyQualifiedName~TubeMouthSnapTests|FullyQualifiedName~PlacementPoseTests|FullyQualifiedName~ALyingCylinderSetDownWithAMicroTiltDoesNotWalkAndItsTiltDoesNotGrow|FullyQualifiedName~ACylinderSetDownExactlyLevelOnItsSideDoesNotDrift|FullyQualifiedName~BoxesDroppedOntoAFaceStayPutAtRest|FullyQualifiedName~HeldBackBoxesStayWithinThePhysicalRestingAllowance|FullyQualifiedName~GenuineSlowMotionRemainsMotionAndFailsTheRestCriterion'`.

Coordinator inspected the actual source/test/doc diff. Final matching incremental solution command: `dotnet build CuriousContraptions.slnx -c Debug --nologo -v quiet -m:1`, session96609: **0warnings/errors,20.79s**. It includes the implementer's last indentation-only adjustment. The complete native run using the generated BrowserDeferral filter, session67040, subsequently passed4,394/0/0 as recorded below. This build/run is historical pre-review-repair evidence. Normal hooks and exact-SHA publication remain pending. No browser, mutant, local Release worker build or web publish ran.

## Review repairs and verification — 26 October 2026

The first full native round, session67040, exited0: **4,394 passed,0failed,0skipped** (main3,902/9m4s; Coverage77/646ms,E2eRunner192/12s,WorkerAotGuard17/40ms,Bundle206/333ms). Matching solution build96609 passed0warnings/errors,20.79s. This is pre-review-repair evidence, retained without claiming it covers later test additions.

Same implementation owner fixed B2/V1 with production X/Z controls and an independent diagonal-length check; B3 with a known nonzero reference and cumulative displacement; B4 with positive/negative X/Z and explicitly sticky later resolution refusal; B7 with1µm/s physical motion over6s inside allowance plus the existing100µm/s rejection controls; B8 with the published-friction wording. No physical limit or production solver/policy changed. Motion direction remains an enum; all expected displacement intervals are literals from v*t and the declared5µm floor.

Matching test-project build70353:0warnings/errors,9.70s. Affected session70108: **31passed,0failed,0skipped**,105ms, using `dotnet test CuriousContraptions.tests/CuriousContraptions.tests.csproj -c Debug --no-build --no-restore --nologo -v minimal --filter 'FullyQualifiedName~PhysicalToleranceTests|FullyQualifiedName~GenuineSlowMotionRemainsMotionAcrossTheRestCriterion'`. Diff check passes. Source is frozen for final matching solution build, full native round and five-layer follow-up; earlier tests not affected by these controls remain in the final full run.

### Follow-up repair evidence and current Auto result

The intermediate complete round20436 exited0: **4,406 passed,0failed,0skipped** (main3,914/9m4s; tools77+192+17+206). Its matching solution build90730 passed0warnings/errors in3.61s. This is pre-final-addition evidence, alongside the preserved4,394/0/0 earlier round; neither substitutes for the final run.

Pass1 repairs add the neighboring f32-resolution boundary (BitDecrement(8f) resolves,8f does not at the5µm floor),100µm Y-only movement with zero horizontal peak, and negative15/30µm reference-relative excursions. They clarify policy attribution and refresh TODO. Exact digits and allowance/8 remain unchanged. Matching test-project build86291 passed0warnings/errors in10.06s; affected session16714 passed **30/0/0 in40ms**, command: `dotnet test CuriousContraptions.tests/CuriousContraptions.tests.csproj -c Debug --no-build --no-restore --nologo -v minimal --filter 'FullyQualifiedName~PhysicalToleranceTests'`. Build command: `dotnet build CuriousContraptions.tests/CuriousContraptions.tests.csproj -c Debug --no-restore --nologo -v quiet -m:1`.

Retained independent reviewer `/root/float_verification_r1` confirmed pass1 B1/B2/B3/I5/B8 resolved and found no introduced defect. All accepted patches are resolved: pass0 **four medium groups plus one low** (B2/V1 grouped, B3, B4, B7; B8), pass1 **three medium plus two low** (B1/B2/B3; B8/I5). The triage log above retains the reasons for every rejected finding: pass0 B1/B5/B6/I1/I2/I3/I4/I5; pass1 B4/B5/B6/B7/I1/I2/I3/I4/I6. There are no unresolved or deferred findings; `deferred: []`, `followup_review_recommended: false`. No further five-layer loop is requested.

**Current Auto result: local Auto done.** Frontmatter `done` records step04 completion, not terminal publication. Implementation, required local review and final native verification are complete; terminal completion remains pending publication. Final matching solution build16108 passed **0warnings/errors in6.37s**. Complete native session34321 exited0: **4,409passed,0failed,0skipped** (game3,917/9m5s; Coverage77; E2eRunner192/12s; WorkerAotGuard17; Bundle206). Source/test inputs are frozen. Normal hooks, exact committed-SHA approval and CI/Pages/artifact/deployment verification remain pending. Browser qualification remains explicitly pending at post-catalogue E2E.

**Changed closure:** `CuriousContraptions.Abstractions/PhysicalTolerance.cs` and generated `.cs.uid`; `engine/TubeMouthSnap.cs`; `CuriousContraptions.tests/PhysicalToleranceTests.cs`, `WorkshopPhysicsLawCylinderTests.cs`, `TubeMouthSnapTests.cs` and `CuriousContraptions.tests.csproj`; `docs/testing-guidance.md`, `docs/gpu-f32-physics.md`, `TODO.md`; `_bmad-output/planning-artifacts/epics.md`; `_bmad-output/implementation-artifacts/sprint-status.yaml`, this spec, `deferred-work.md` and the carried `spec-engine-two-point-friction.md` terminal receipt. The change removes duplicate BoreTolerance authority and obsolete endpoint nanometre resting gates, adds finite named metre policies and complete-window observation, and records bounded adoption plus canonical next task `engine-remove-core-warmup`. No solver, runtime rest state, compatibility path or velocity snapping is introduced.

| Criterion | Changed impact and independent expected behavior | Evidence |
|---|---|---|
| C1 | Pure shared policy: literal5/20/50µm scales, floor/relative/cap transitions, inclusive representable threshold, explicit invalid-scale and nonfinite refusal | PhysicalToleranceTests; final affected30/0/0 |
| C2 | Test-only resting acceptance: unchanged1+9,2+8,4+6s windows,60 seeded boxes,3 fixed held-back boxes; original tilt checks preserved; nominal local geometry drives size | Cylinder resting/held-back cases in original affected102/0/0 and full4,394/0/0, then full4,406/0/0; helper reference, sign, diagonal, horizontal-only and resolution controls in final affected30/0/0 |
| C3 | Actual production simulation retains X/Z motion at100µm/s and1µm/s;6s displacement uses literal v*t±5µm; larger motion rejects and smaller motion remains real while accepted | Production motion theory plus helper excursion-return controls; repaired affected31/0/0 and full4,406/0/0; later helper additions30/0/0 |
| C4 | Sole production consumer is bore compatibility; capture, ordering and placement remain unchanged; ±16/17 f32-step radii bracket1µm, while admission refuses nonfinite bores with BUNDLE002 | TubeMouthSnapTests and PlacementPoseTests in original affected102/0/0 and subsequent full rounds; direct symmetric finite-policy controls |
| C5 | Compile/test closure, forward-only deletion and accurate current guidance; only named browser-toolchain cases deferred, zero skipped game tests | Matching builds and historical full rounds above; both five-layer reviews and retained repair confirmation; final34321 passed4,409/0/0; publication checks pending |

### Publication-policy transition — 26 October 2026

Float commit `133859b4dfac2066a941a9a2833f08d97834abbd` completed full local **4,409 passed, 0 failed, 0 skipped**, normal hooks **124 passed, 0 failed, 0 skipped**, and independent SnapshotApproval. [Remote run 40311722182](https://github.com/aidanmorgan/tim/actions/runs/40311722182) was cancelled by the owner-directed local-test-authority policy change before publication; no remote test or deployment Pass is claimed. Earlier receipts and the immutable float intent above remain historical evidence unchanged.

The [local-test-authority slice](spec-local-test-authority.md) changes test execution location and policy only. It leaves the float runtime, numeric policy, simulation inputs and float tests unchanged, so their original local behavioral proof remains applicable subject to independent review of this closure. Its terminal receipt will name the later workflow-policy SHA containing the float commit and verify the exact production build, Pages artifact and deployment. Browser qualification remains pending; synthetic warm-up removal remains the next owner-requested task.
