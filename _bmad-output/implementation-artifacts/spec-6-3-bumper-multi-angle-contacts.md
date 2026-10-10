---
title: 'Bumper multi-angle contacts and advanced puzzles'
type: 'feature'
created: '2026-10-10'
status: 'done'
baseline_commit: '60ddbbe4f010314a64716bf8748845146b312bdc'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/docs/planning/elements/CAT-015-bumper.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-6-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Story 6.2 delivers paid radial contacts and Sidekick, but the preserved depth and combined Wall/Bumper lessons are not admitted as current playable modes.

**Approach:** Admit their exact authored fixtures through typed declarations and existing Workshop controls. Prove multi-angle contact behavior in the generic engine and solve both lessons through actual Chrome UI.

## Boundaries & Constraints

**Always:** Preserve Story 6.3, CAT-015, interaction-01, todo157/159/160/163 and sequence-task304 acceptance. Keep captured goals, locked fixtures, inventory, orientations and every difficulty-profile knot from `content/puzzles.json`. Retain approved finite work, partial affordable boost and Battery recharge semantics from completed 6.2. Use shared physics and paid-only cosmetic events; new work values remain f32.

**Never:** Add an obliqueness cutoff, element solver or free launch; inject solutions, move fixtures to fit tests, replace the Wall reference dimensions with defaults, or qualify physical nudging through manual placement. Physical nudging and full catalogue assistance remain explicit unmet obligations at their existing owner/gate; this slice preserves their data and exercises actual difficulty controls. Inherited geometry precision remains with Epic 16. No new element, electrical-network expansion or global qualification.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|---|---|---|---|
| Closing contact | Six radial directions; oblique approach above threshold | Ordinary response first, affordable radial work; post-collision tangent/spin preserved | Clamp numerical residuals |
| False trigger controls | Depth/grazing miss, resting, separating or below threshold | No paid work or powered ring; ordinary contact remains applicable | No tick fault |
| Recontact | Same body within cooldown; another body; released return after cooldown | Per-body gating; independent eligible hit; eligible return | Persistent/resting contact cannot invent approach |
| Overlap | Two eligible paid events | Rings overlap and settle; no collider scaling | Rejected/passive events add no ring |
| Depth lesson | Locked ball (0,5,3), receiver (0,.9,-2), source 90° Y orientation; one Bumper | Manual valid route captures; displaced route fails | No injected solution |
| Wall lesson | Locked ball (-3,5,0), receiver (-5,.9,0); one Wall plus Bumper | Actual Wall resize to .4×6×1.5 and valid route captures; unsuccessful route fails | Reject extra inventory atomically |
| Lifecycle | Difficulty/configuration edits, Run, Reset, Save/Load | Exact authored configuration, profiles, inventory and goals restored; runtime events cleared | Invalid serialized declarations reject |

</frozen-after-approval>

## Code Map

- `engine/gpu/WorkshopPuzzle.cs`, `WorkshopInventory.cs`: typed mode and mixed-inventory admission.
- `engine/gpu/BumperSidekick.cs`: existing declaration pattern; new lesson declarations preserve source fixtures.
- `engine/gpu/WorkshopPuzzleWire.cs`, `WorkshopSaveCodec.cs`, `WorkshopConstruction.cs`: serialized identity, validation and precision changes.
- `ui/WorkshopPuzzle.cs`, `WorkshopGuidance.cs`, `Workshop.cs`: picker, hints, properties and mode presentation.
- `CuriousContraptions.Simulation/wwwroot/worker.js`, `engine/gpu/ContactWorkImpulse.cs`: shared contact/work path; change only for a reproduced acceptance defect.
- `CuriousContraptions.tests/WorkshopBumperTests.cs`, `tools/workshop-rigid-body.test.mjs`, `tools/e2e/cat-015b.test.ts`: declaration, contact and actual-UI controls.

## Tasks & Acceptance

**Execution:**
- [x] Add typed declarations, inventory and UI selection for both preserved identities; update required project inputs and serialization consumers together.
- [x] Compare declarations against source fixtures/profiles; test unsupported identity, inventory overflow and wire/save rejection.
- [x] Test matrix boundaries in native/worker layers, including valid oblique spin/tangent behavior and per-body cooldown/recontact.
- [x] Build actual-UI recipes for both lessons: camera/depth placement, difficulty changes, Wall resize/properties, meaningful unsuccessful routes, paid rings and exact Run/Reset/Save/Load.
- [x] Retire any superseded affected path; do not restore purged Bumper physics. Refresh current docs/status and retain one scoped identity/review record.

**Acceptance Criteria:**
- Both named lessons solve with valid sampled trajectories and the existing captured sensor through real Chrome UI; `cat-015b` passes serially.
- “Glancing” forbids false boosts, not genuine oblique contact: baseline `a6c914e:CuriousContraptions.tests/BumperTests.cs:73–113` requires oblique (2,-4,0) approach to preserve tangential behavior and generate spin while launching radially; depth-separated control never triggers. Apply current game-grade tolerances.
- Bumper profile retains positional correction .25/.1/0 and zero rotation correction, distinct from Wall .25/.1/0 and 5/2/0°. Exact source assistance remains required; manual proof does not mark physical nudging complete.
- Reviewer verifies affected impact, source/bundle identities, builds and controls before SnapshotApproval; normal publication and applicable production-origin checks precede terminal Pass.

## Implementation Notes

Entry09b014 passed. Typed modes derive immutable Wall assistance/mixed inventory; existing wire and solver remain unchanged. Bumper physics was already purged; no new legacy deletion. Native279/279 passed; recording-boundary worker controls distinguish admission from genuine unchanged C# payment proof, including exact72-step eligibility. Both actual UI routes/lifecycle pass locally. Original Wall test invented a2m/s threshold despite capture; corrected to geometric direction reversal and reran successfully. Raw attempts remain under .anvil/story-6-3-*. Production10841 and diagnostic60309 builds passed. Independent final review/publication remain pending.

## Spec Change Log

## Review Triage Log

| Finding | Triage / evidence and resolution |
|---|---|
| B1 | Resolved: actual enums and documented tsx4.23.15 invocation; corrected Chrome4/4. |
| B2 | Evidence corrected: spy is admission only. Genuine FromStore normal-projection/finite-budget theory passed in original279 and corrected32; actual paid WASM spin>1 now asserted. Unchanged worker application closure remains for reviewer applicability. |
| B3 | Resolved: before-boundary ordinal remains1; exact eligible occurrence73 and sequence2 asserted. Targeted Node1/1, raw83f58f. |
| B4 | Low evidence correction: existing DeclaredImpulseEnvelopeCombinesOverlapInTheSharedBatchAndReturnsToNeutral ran within279; reviewer must bind unchanged inputs. No duplicate test. |
| B5 | Resolved: all live body bytes restored, capture cleared, neutral animation and repeated intended outcome; corrected Chrome4/4. |
| B6 | Resolved with G1: independent expected margin/speed/dwell/guide at0/.45/1 in native32 and actual UI4/4. |
| B7 | Resolved: both modes reject unknown identity, malformed profile/precision and inventory; original accepted bytes unchanged and decode succeeds afterward. Native32/32, raw429434. |
| B8 | Resolved: unique .anvil run directory rejects existing paths; original images untouched. Final owner images: .anvil/cat015b-correction-owner3. |
| B9 | False: snapshot28282269 already binds sources/artifacts/builds/evidence; preserve it and record only changed correction inputs. |
| E1 | Resolved: Depth move and Wall resize use validated dock anchors in corrected Chrome. Driver frame wait uses anonymous RAF callbacks to remain serializable under tsx; identical frame-count/timeout semantics. |
| G1 | Resolved with B6; distinct finding retained. |
| G2 | False: WorkshopPuzzle.Validate requires exactly one Basketball before advanced-mode validation; no duplicate production guard. |

## Verification

Correction proof: native WorkshopBumperTests32/32 (session52790, raw429434,146ms); exact cooldown Node1/1 (83f58f); actual Chrome4/4 (session44303, raw3a03b6/ea30c3,93.544s), `.anvil/story-6-3-corrected-owner-3.log`. Failed owner attempts retained: missing preview listener (`corrected-owner.log`), then tsx named-callback serialization (`corrected-owner-2.log`); neither reached gameplay. `--test-force-exit` is runner cleanup only, not lifecycle proof. Product runtime and both original build artifacts are unchanged from [snapshot28282269](snapshot-6-3-bumper-multi-angle.json). Independent seven-case Chrome and publication remain pending.

Original279 command: `dotnet test CuriousContraptions.tests/CuriousContraptions.tests.csproj --no-restore -c Release -warnaserror --filter 'FullyQualifiedName~WorkshopBumperTests|FullyQualifiedName~WorkshopSaveTests|FullyQualifiedName~WorkshopWireTests|FullyQualifiedName~WorkshopReadTests|FullyQualifiedName~WorkshopHintTests|FullyQualifiedName~WorkbenchCapacityTests|FullyQualifiedName~WorkshopActivationAnimationTests|FullyQualifiedName~DominoEffectTests|FullyQualifiedName~DelayedSignalTests|FullyQualifiedName~WorkshopWallTests' -v minimal`; raw6b636b and `.anvil/story-6-3-native.log`. This includes `DeclaredImpulseEnvelopeCombinesOverlapInTheSharedBatchAndReturnsToNeutral`; unchanged overlap implementation and genuine payment law require reviewer applicability, not duplicate tests.

- Focused `dotnet test` filters for affected puzzle, inventory, wire/save and contact controls; broaden only for demonstrated shared impact.
- `node --experimental-vm-modules --test tools/workshop-rigid-body.test.mjs` for changed contact behavior.
- Production and diagnostic publish using existing project commands; actual connector Chrome serial `cat-015b` plus affected existing cases.
- Reuse published 6.2 proof at `60ddbbe` only where unchanged inputs remain applicable; authoritative continuity: [6.2 review](review-6-2-bumper-battery-integration.md). Reviewer determines the final check set, including production-origin controls.

## Terminal scoped acceptance

Independent [review](review-6-3-bumper-multi-angle-contacts.md) records terminal Pass at published25d53233ab5ee724be0bd34081f175c47f938ff5. [Actions38037623375](https://github.com/aidanmorgan/tim/actions/runs/38037623375) passed710 tests, Production publish and Pages deployment. Four independent production routes/lifecycle controls passed with zero errors; all71 loaded assets matched CI artifact11665075314. Prior pending statements describe their original checkpoints; this terminal result supersedes their status without erasing failures or identities. Physical nudging and later qualification remain outside this bounded completion. Next: Story6.4 passive prismatic/soft-spring capability.
