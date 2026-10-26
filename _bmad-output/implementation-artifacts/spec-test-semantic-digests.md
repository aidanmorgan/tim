---
title: 'Behavioural verification: replace replay and fixture digests'
type: 'refactor'
created: '2026-10-26'
status: 'done'
baseline_revision: 8ac611ffe81a74af7d36328bf69cc6403065b6d9
planning_gate: '2026-10-26: source assertions, fixture independence, case preservation and behavioural oracles verified'
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '/Users/aidan/dev/personal/tim/.claude/worktrees/story-test-semantic-digests/AGENTS.md'
  - '/Users/aidan/dev/personal/tim/.claude/worktrees/story-test-semantic-digests/docs/testing-guidance.md'
  - '/Users/aidan/dev/personal/tim/.claude/worktrees/story-test-semantic-digests/docs/delivery-workflow.md'
warnings: ['oversized']
deferred: []
---

<intent-contract>

## Intent

**Problem:** The owner requested a subagent to find hash-based verification and replace it with behavioural tests. The first bounded child of `test-behavioral-hygiene` replaces simulation replay fingerprints and legacy-fixture digest pins: 12 existing methods, 57 literal cases (static inventory, not a runner total).

**Approach:** Assert identified body state and independently expected activity through real simulation reads; preserve fixed input fixtures and their existing typed refusal or accepted-domain assertions. Keep all cases and controls. Subsequent children own other raw output comparisons and repository-owned source guards.

## Boundaries & Constraints

**Always:** Preserve existing 600-tick, three-run replay at 60/120/240 Hz; all 50 refused and four accepted fixture cases; real production codecs/engine; enum-typed domains; Anvil before writes; one matching incremental build, then no-build/no-restore checks and one full native round; independent Auto review and exact-SHA publication. Every required game test runs with Skipped: 0.

**Never:** Weaken an oracle, drop/merge cases, replace hashes with encoded-byte equality, modify game runtime/physics/content, add compatibility/migration or a fixture generation platform, run mutants/Chrome/local Release worker build/web publish, or change unrelated dirty worktrees. Third-party integrity checks remain. No global test-compliance claim from this slice.

## I/O & Edge-Case Matrix

| Scenario | Input/state | Expected observable behaviour | Handling |
|---|---|---|---|
| Replay | Existing ball/box/cylinder scene, three cadences, fresh and reused engine | 600 corresponding ticks and identified body positions/rotations/velocities/angular velocities match; real falling/rebound/rotation occurs | Finite states and envelope limits |
| Unsupported fixtures | 50 existing literal cases at their existing boundaries | Same typed refusals and existing messages, no accepted domain value | Production save/command rejection |
| Valid fixed fixtures | Two latches, lamp and detector | Existing expected identities, pose, lock, parameters and construction fields | Ordinary current decoder only |
| Current-input controls | Existing parameter limits, malformed fields and valid cases | Existing acceptance/refusal distinctions and lifecycle outcomes remain | No special fixture admission |
| Fixture independence | 22 historically generated save/command pairs | Fixed committed inputs preserve existing cases without runtime old-encoder dependence | Missing/invalid fixture fails visibly |

</intent-contract>

## Code Map

This is the implementation step of an already-running Auto iteration. Implementer owns changes, affected checks and a frozen return; coordinator owns full-round verification, Auto reviews and publication. Do not invoke a nested build workflow, spawn another implementer/reviewer, commit or push before finalization handoff.

Start from published main `8ac611ffe81a74af7d36328bf69cc6403065b6d9`. The friction candidate is separately blocked/unpublished in sibling `story-engine-two-point-friction`; do not import its runtime edits. Its full run exposed the pre-existing deep single-point push-out trajectory (case 179). Preserve this blocker in current tracking; the owner-requested test work now comes first.

All test paths below are under `CuriousContraptions.tests/`:
- `WorkshopPhysicsLawRestitutionTests.cs:531`: `BouncingRunsAndAResetReplayAreBitIdentical` hashes `state.World`. Retain returned body IDs. `LawState` wraps reused bytes: copy typed `LawBody` values inside the callback, not raw world references. `LawWorld.Step` resets/re-admits each invocation; this proves engine lifecycle, not UI qualification.
- `DeclaredContactLoadTests.cs:139`: four plate fixture rows and both-boundary messages.
- `DeclaredMemoryTests.cs:127`: `Bytes` hashes committed base64 inputs; ten counters, generic lamp and two latches. Counter4/8 differ in typed refusal; keep distinctions.
- `DeclaredOrientationTests.cs:83`: domino/plate/clock theory and pipe fact; pipe save/command reasons differ.
- `DeclaredOscillatorTests.cs:200`: eight fixed inputs and existing parameter-boundary controls.
- `DeclaredIndicatorPassageTests.cs:167`: wired refusal plus lamp/detector accepted fields and passage lifecycle.
- `PreConversionLaunchSaveTests.cs:12`: nine `Records` and thirteen `PreSourceStoreRecords` cases currently call old encoders and assert pins. Freeze 44 buffers once before deleting digest assertions. Existing current battery/bumper/spring controls stay.
- `OldLayoutSaves.cs`: the two record enumerators above have only those consumers. Delete newly unused enumerators/layout constants/defaults/helpers after fixture capture. Preserve actual `PreDominoRecords`, raw malformed-table controls in orientation/oscillator and historical material constants used by `PayloadInstanceTests`. No unsupported decoder is added.
- `Fixtures/`, `F32SaveBoundaryTests.cs:10`: existing project-relative fixed resource loading convention; use simple named fixed inputs, no new platform. Project explicitly lists C# compilation in `CuriousContraptions.tests.csproj`.
- `docs/gpu-f32-physics.md#game-grade-envelope`: 64 m/s, 128 rad/s and quaternion norm error 1e-4; independent physics expectations, not replay-equality tolerances.
- [Source hygiene acceptance](../planning-artifacts/epics.md#current-execution-plan) and [testing guidance](../../docs/testing-guidance.md) govern coverage preservation.

## Tasks & Acceptance

**Execution:**
1. [x] Capture the existing 22 save/command pairs as committed fixture input(s), with concise provenance; review preservation once without adding permanent hash/byte assertions. Replace affected calls and delete only newly dead historical encoder code.
2. [x] Replace replay SHA/list with copied identified body state per tick; assert activity and finite/envelope conditions. Rename byte-identical wording accurately.
3. [x] Remove fixture digest columns/arguments/assertions/imports in six classes; retain every typed reason, message and domain assertion. Avoid duplicate theory rows after removing digest columns.
4. [x] Update `TODO.md`, sprint status, affected hygiene row in epics, this spec and a concise `test-behavioral-hygiene-inventory.md` with removed assertions→replacement tests and remaining groups. Preserve blocked friction/penetration ownership; no unrelated planning import.
5. [ ] Matching build, scoped checks, full round, five Auto layers, fixes to same implementer, normal hooks, committed-SHA approval and terminal publication.

**Acceptance Criteria:**
- H1: Given all three cadences, when the original scene executes 600 ticks on two fresh engines and the first engine again, then exactly 600 identified samples per run preserve tick/body membership and semantic position, quaternion, linear/angular velocity exactly. No hash or raw-world retention/equality remains.
- H2: Given the elevated scene, when the baseline advances, then the basketball falls and rebounds, box orientation changes, and every sampled body is finite within 64 m/s, 128 rad/s and 1e-4 quaternion normalization error. Expectations come from declared physics, not measured output. Existing flat-drop restitution and strict-threshold controls remain.
- H3: Given each unchanged unsupported fixture, when each existing production boundary decodes it, then its exact typed refusal and existing message hold. Given the four current-valid fixtures, when decoded, then their independently authored domain fields remain correct. Historical provenance does not authorize an old-format path.
- H4: Given the fixture cutover, when tests run, then all 57 existing cases remain distinct and all affected class controls/lifecycle checks execute. Fixed input loading cannot silently regenerate or substitute data.
- H5: Given the completed diff, when full native checks, review and publication run, then zero failed/skipped tests and no lost behavioural coverage remain; report actual totals and exact commit/CI identities. Broader hygiene/browser/global qualification stays explicitly pending.

## Spec Change Log

## Review Triage Log

### 2026-10-26 — Review pass 0

Five independent Auto layers: `/root/hash_blind_r0`, `/root/hash_edge_r0`, `/root/hash_verification_r0`, `/root/hash_intent_r0`, `/root/hash_forward_r0`. All layers launched before triage; capacity required waves. Verification Gap found no gaps; Forward-Only found no violations, including the current-schema historical fixtures and retained refusal-input builders. The intent auditor's four surface distinctions are recorded individually below.

- verdicts: 15 findings — high 0, medium 7, low 3, false 5, maybe-false 0
- findings:
  - `[medium]` `[patch]` B1: Rounded world Position hides changes in canonical Cell/Local. LawState.Read sums cell+local in f32; compare the already-copied typed Cell and Local too.
  - `[medium]` `[patch]` B2: The callback tick is the harness loop counter while production increments HeaderTickOffset. ReadDynamicBodies exposes CanonicalBody.Tick; assert that committed tick independently.
  - `[medium]` `[patch]` B3: The dictionary is built from expected IDs and LawState.Read uses declaration offsets rather than committed record IDs. Assert actual ReadDynamicBodies count and IDs.
  - `[medium]` `[patch]` B4: The old fingerprint included the static bench while the new snapshot omits it. Include the declared bench's semantic pose/velocities and assert it stays authored. Internal solver scratch is deliberately not a behavioural oracle; other control/lifecycle tests cover their public effects.
  - `[medium]` `[patch]` B5: The new independent activity checks omit the cylinder. Add descent from its authored elevated pose; preserve existing cylinder collision/rest law tests.
  - `[medium]` `[patch]` B6: Quaternion inequality can pass on a sign or normalization change without rotation. Compare a transformed body axis using a threshold derived from prescribed initial angular motion and f32 accuracy.
  - `[low]` `[patch]` B7: A later positive vertical velocity alone does not locate the rebound at the bench. Add a contact-height bound derived from ball radius, initial drop, gravity and one committed interval; retain the existing sign and rise checks.
  - `[low]` `[patch]` B8: Bare equality failures omit the replay/run/tick/body identity. Add local assertion context for the existing comparisons; no shared framework.
  - `[low]` `[reject]` B9: Fixture names identify the distinguishing cases and source comments enumerate refused fields/reasons; fixed inputs are intentional. A new 22-case descriptive manifest duplicates archived encoder definitions without changing decode coverage. No additional fixture-definition platform is warranted.
  - `[false]` `[reject]` B10: Local commands and actual result totals are retained in the spec and original tool outputs. The owner light-evidence policy uses commit/CI identities and test totals; an additional duplicated raw-evidence bundle is not a precondition. Exact-SHA CI and terminal receipt remain mandatory before publication Pass.
  - `[medium]` `[patch]` E1: Same canonical-coordinate loss as B1; preserve Cell/Local directly, not rounded reconstructed Position alone.
  - `[false]` `[reject]` I1: The explicit intent matrix replaces byte identity with body behavior. Patches above retain canonical bodies, committed tick/membership and static bench; internal world-byte identity is expressly forbidden and is not an additional intended observable.
  - `[false]` `[reject]` I2: Recurring fixture-byte identity is expressly removed by the owner's rule. Fixed inputs and one-off preservation plus existing typed decode outcomes implement the intended contract.
  - `[false]` `[reject]` I3: The intent requires independent activity, with detailed physical accuracy retained in existing law/control tests. Activity assertions are strengthened under B5-B7; a full analytic trajectory oracle was not required.
  - `[false]` `[reject]` I4: This is an intermediate review candidate, explicitly not terminal Pass. Full verification, independent fixes and exact-SHA publication remain required and will be completed before finalization.

Grouped repairs: B1/E1 canonical coordinates; B2/B3 committed read metadata; B4 bench semantics; B5 cylinder activity; B6 physical orientation; B7 contact-height activity; B8 diagnostics. Five medium and two low patch entries go to the original implementer. No intent gap, spec loopback or deferred finding. Follow-up review is required for canonical-state and committed-read coverage after these repairs; no terminal approval yet.

Pre-repair full native round (session 9021): `dotnet test CuriousContraptions.slnx -c Debug --no-build --no-restore --nologo -v minimal -m:1`, exit 0, **4,228 passed / 0 failed / 0 skipped**. Main 3,720 (8m47s); Coverage 77 (664ms); E2eRunner non-browser tooling/harnesses 208 (23s); WorkerAotGuard 17 (44ms); Bundle 206 (344ms). The repeat followed removal of the redundant assertion and corresponding rebuild. Review repairs change relevant test inputs and require matching affected verification before the next full round.



### 2026-10-26 — Follow-up review pass 1

Independent layers: `/root/hash_blind_r1`, `/root/hash_edge_r1`, `/root/hash_verification_r1`, `/root/hash_intent_r1`, `/root/hash_forward_r1`. All five launched before triage. Edge Case and Verification Gap reported no gaps; Forward-Only reported no violation. Initial repairs are verified by source inspection and the three passing cadence cases; this follow-up identifies no remaining implementation repair.

- verdicts: 14 findings — high 0, medium 0, low 3, false 11, maybe-false 0
- findings:
  - `[false]` `[reject]` B1: The required engine-state replay still reads the production ABI through the established LawState seam. ReadDynamicBodies is additionally used for metadata, not claimed as a replacement decoder-validation test. Its unchanged ReadCell/ReadLocal/rotation/velocity paths read the same lanes; existing F32LaneRoundTripTests and WorkshopPhysicsEngineTests exercise committed read values. No incorrect decoder or lost original reader assertion was identified.
  - `[false]` `[reject]` B2: ReadDynamicBodies checks motion enums and population, so a static-to-dynamic change fails the new count/ID assertion. Static identity is an immutable declaration copied by Advance and enforced by ValidateCandidate's static-body guard; deterministic static identity corruption also passed the old replay hash equality. The new static-state check deliberately adds pose/velocity preservation rather than claiming all declaration validation.
  - `[false]` `[reject]` B3: The stated independent oracle is physical rotation/activity, now proved by a transformed axis beyond its derived gyro/roundoff lower bound. A full signed analytic trajectory is a broader requirement; detailed physical-law tests remain. No incorrect rotation in this code was demonstrated.
  - `[false]` `[reject]` B4: The intent calls for independently expected activity, not re-proving gravity magnitude in every replay scene. Descent, contact-height rebound, physical rotation and existing gravity/law controls remain. This is the same broader-physical-accuracy distinction rejected under I3 in pass 0.
  - `[false]` `[reject]` B5: Cylinder descent is the promised independent activity. CylinderPlaneManifoldsHaveFourCapPointsTwoSidePointsOrOneRimPoint, DroppedEndDownACylinderSettlesOnItsCapWithoutJitterOrDrift, DroppedOnItsSideACylinderRestsAlongALineWithoutCreepingAlongItsAxis and seeded cylinder-plane energy gates retain contact/rest proof. The replay need not duplicate every physical law with this exact pose.
  - `[false]` `[reject]` B6: LawWorld.Step resets engine caches then clones the unchanged admitted buffer before Advance. No engine-visible state exists between Reset and the first Advance to observe through this seam; checking the cloned initial buffer would be tautological. The test explicitly names reused-engine replay, not WorkshopSimulation Reset command qualification; existing simulation/device lifecycle tests retain that responsibility.
  - `[false]` `[reject]` B7: The three InlineData theory rows identify SimulationCadence in the xUnit failure display; run/tick/body/field are now in assertion messages. Cadence is therefore available without new instrumentation or a rerun.
  - `[low]` `[patch]` B8: TODO describes launching initial review although the first review and repairs completed. Refresh the current brief to final native verification/follow-up review and publication.
  - `[low]` `[patch]` B9: Inventory verification paragraph predates the seven review repairs. Refresh its current summary while retaining prior exact results and broader-hygiene ownership.
  - `[low]` `[reject]` B10: The requested fix edits this build's spec, which Auto expressly rejects as a finding route. Finalize computes followup_review_recommended for the completed pass; this follow-up has no high patch or unverified risk, so its terminal value is false. In-review status and the prose already distinguish pending verification/publication from approval.
  - `[false]` `[reject]` I1: Carried pass-0 I1: internal serialized identity is expressly replaced by the behavioral matrix; canonical bodies, metadata and static state now strengthen that surface.
  - `[false]` `[reject]` I2: Carried pass-0 I2: fixed input files plus typed decode outcomes intentionally replace recurring byte-identity pins.
  - `[false]` `[reject]` I3: Carried pass-0 I3: independent activity is required here; complete independent physical trajectory prediction remains outside this replay contract.
  - `[false]` `[reject]` I4: Carried pass-0 I4: candidate review is intermediate. Full native verification and exact-SHA publication are still required before terminal Pass.

Two low documentation patches returned to the same implementer. Follow-up patched counts: high 0, medium 0, low 2. No unverified implementation risk remains; `followup_review_recommended: false` for this completed follow-up. Final native session 59612 and exact-SHA hooks/CI/artifact/deployment remain required; no terminal publication Pass yet.

## Design Notes

### Review repairs — 26 October 2026

Original implementer `/root/hash_dev` applied all seven grouped repairs in `WorkshopPhysicsLawRestitutionTests.cs`: canonical Cell/Local equality, production `ReadDynamicBodies` membership/clock, authored static-bench preservation, cylinder descent, physical axis motion, independently bounded rebound contact height, and local mismatch context. No fixture/runtime/content changes. Initial repair build failed because `Assert.Single` required the declared bodies span converted to an array; corrected before testing. Matching incremental Debug solution build with `-m:1`: 0 warnings/errors, 10.07 s. Affected replay command with `--filter FullyQualifiedName~BouncingRunsAndAReusedEngineReplayPreserveBodyMotion --no-build --no-restore`: **3 passed / 0 failed / 0 skipped**, 1 s (session 91593). `git diff --check` passed. All patch rows above are applied; no deferred entries. Coordinator final full round session 59612 and follow-up Auto review are pending. The repeat is justified by changed semantic assertions, not unchanged-source requalification.


The four accepted historical-provenance fixtures use the ordinary current generic decoder without migration. Retain their semantic expectations. Unsupported fixture builders are test inputs, not production compatibility; removing now-unused builders is necessary deletion, not a redesign.

Exact equality of copied typed deterministic states is acceptable; equality of encoded output buffers or digests is not. Do not replace response bytes with a domain equality helper that secretly compares motion bytes. This slice needs only body snapshots and fixed fixture decode.

Remaining audit groups: raw physics replay/Reset (including prismatic, cylinder refusal, receiver frame, sensor, device and simulation Reset); event-ring/motion semantics; codec saves; element lifecycle saves; repository-owned source guard pins and compiled-geometry golden review. Preserve vendored integrity. Node save-storage ownership tests require strict TypeScript when changed; they are not part of this C# digest slice. The sibling friction candidate already replaces its 600-tick cylinder replay; coordinate eventual merge without duplication.

## Verification

- `dotnet build CuriousContraptions.slnx -c Debug --nologo -v quiet`: matching incremental output, zero warnings/errors; no local Release/publish.
- Scoped `dotnet test CuriousContraptions.tests/CuriousContraptions.tests.csproj -c Debug --no-build --no-restore --nologo -v minimal --filter 'FullyQualifiedName~WorkshopPhysicsLawRestitutionTests|FullyQualifiedName~DeclaredContactLoadTests|FullyQualifiedName~DeclaredMemoryTests|FullyQualifiedName~DeclaredOrientationTests|FullyQualifiedName~DeclaredOscillatorTests|FullyQualifiedName~DeclaredIndicatorPassageTests|FullyQualifiedName~PreConversionLaunchSaveTests'`: every affected class and existing control passes; record actual totals, not 57 as a suite total.
- Coordinator once at final frozen round: `dotnet test CuriousContraptions.slnx -c Debug --no-build --no-restore --nologo -v minimal`, required non-browser node harnesses through normal suite; zero failed/skipped.
- `git diff --check`, scoped digest/dead-caller search, explicit case-preservation/replacement matrix, normal hooks and Auto's Blind Hunter, Edge Case Hunter, Verification Gap, Intent Alignment, Forward-Only Auditor.
- Reuse matching proof until relevant source/generated inputs/dependencies change. No mutation testing. Record failures and fixes without duplicate manifests/hashes.

## Implementation verification — 26 October 2026

- 26 October: replaced the bounded assertions and captured fixed inputs; no runtime/content changes. Current TODO records the separately authorized penetration investigation without importing its implementation. Build and 187 scoped tests passed; coordinator full native results are recorded below; review/publication remain pending.

- `dotnet build CuriousContraptions.slnx -c Debug --nologo -v quiet`: exit 0, 0 warnings/errors. Initial 20.25 s; helper cleanup incremental builds 12.49 s and 6.43 s. Final build with `-m:1` after typing fixture family/boundary enums: exit 0, 0 warnings/errors, 10.94 s. These rebuilds followed changed test helper source; no full suite was repeated.
- Scoped seven-class command above: exit 0, Passed 187, Failed 0, Skipped 0, Duration 1 m 14 s. Final helper-only cleanup consumer check (`--filter 'FullyQualifiedName~DeclaredOrientationTests|FullyQualifiedName~DeclaredOscillatorTests'` with the same no-build/no-restore command): exit 0, Passed 46, Failed 0, Skipped 0, 174 ms. Final typed-fixture path check (`--filter 'FullyQualifiedName~PreConversionLaunchSaveTests'` with the same no-build/no-restore command): exit 0, Passed 26, Failed 0, Skipped 0, 88 ms; the other classes and replay source did not change after their passing run.
- One-off input preservation: all 22 fixed save buffers matched existing pins; all 44 files were also independently re-read and compared once against original builder output (22 saves and 22 commands, exact equality, exit 0). The temporary capture/comparison program was deleted; no permanent byte assertions were introduced. All seven classes retain their InlineData counts (15/14/3/34/6/24/23), and lamp/detector MemberData retain two distinct rows. The 57-case acceptance inventory is not the test-run total.
- `git diff --check`: exit 0. Scoped searches find no digest assertions or removed encoder callers; no runtime/content edits, skipped tests, browser execution or mutation runs.
- Coordinator full native session 95423: exit 0, 4,228 passed, 0 failed/skipped. Main: 3,720 (8 m 48 s); Coverage: 77 (638 ms); E2eRunner tooling: 208 (23 s, solution tooling execution, not browser qualification); WorkerAotGuard: 17 (42 ms); Bundle: 206 (327 ms). Pre-review cleanup removes the redundant keys assertion built from the same ID list, repairs the epics table and corrects/synchronizes evidence. Post-fix matching build (same Debug command with -m:1): exit 0, 0 warnings/errors, 11.27 s. Affected replay command (same no-build/no-restore test command with --filter FullyQualifiedName~BouncingRunsAndAReusedEngineReplayPreserveBodyMotion): exit 0, Passed 3, Failed 0, Skipped 0, 1 s. The coordinator final full round follows these edits. Five Auto review layers, commit/hooks and exact-SHA publication remain coordinator-owned and pending. Browser/global qualification and the separately owned friction/penetration blocker remain open.

## Auto Run Result

**Implementation review Pass; publication pending.** Replaced the bounded replay fingerprints and fixture pins with semantic/physical assertions while retaining all 57 literal cases. The first review's seven grouped repairs are applied and independently re-reviewed. Follow-up found only two documentation corrections, now applied; no required-now implementation finding remains. This is the first child of broader test-behavioral-hygiene, not a global compliance or browser qualification claim.

Changed files: `WorkshopPhysicsLawRestitutionTests.cs` now checks 600 tick/three-engine-run semantic replay, canonical coordinates, committed identities/ticks, static bench and independent activity; `DeclaredContactLoadTests.cs`, `DeclaredMemoryTests.cs`, `DeclaredOrientationTests.cs`, `DeclaredOscillatorTests.cs`, `DeclaredIndicatorPassageTests.cs` remove fixture digest pins while preserving typed/domain assertions; `PreConversionLaunchSaveTests.cs` loads 44 fixed inputs; `OldLayoutSaves.cs` deletes the now-unused encoder branches; the 44 fixture files and provenance README preserve those input cases. TODO, sprint, epics, the replacement inventory and this spec carry scoped ownership/evidence.

Matrix audit: replay is covered by all 3 cadences of `BouncingRunsAndAReusedEngineReplayPreserveBodyMotion`; unsupported and valid fixed fixtures by the unchanged 50 refusal/4 acceptance cases enumerated in the inventory; current-input controls by the affected classes' existing parameter/malformed/lifecycle tests; fixed-file independence by actual file loading and typed decoding, with missing/invalid files failing directly. All ran in the final full native round. No case was skipped, merged or removed. No runtime/content change, output identity assertion, compatibility path, browser test or mutant was introduced.

Independent review identities and every finding/disposition are in the two triage entries above. Pass 0:15 findings, 7 medium/3 low/5 false, grouped into 5 medium+2 low repairs. Pass 1:14 findings, 3 low/11 false;2 low documentation corrections applied, 1 spec-only request rejected under Auto; no high repair or remaining unverified risk. No deferred finding. All rejected claims retain their individual evidence above. **Follow-up review recommended: false.**

Final frozen-source verification (session 59612): `dotnet test CuriousContraptions.slnx -c Debug --no-build --no-restore --nologo -v minimal -m:1`, exit 0: **4, 228 passed, 0 failed, 0 skipped**. Main 3720/8 m 59 s; Coverage 77/657 ms; E 2 eRunner tooling and non-browser node harnesses 208/23 s; WorkerAotGuard 17/45 ms; Bundle 206/347 ms. Matching repaired build:0 warnings/errors/10.07 s; affected replay 3/0/0. `git diff --check` passes. Previous failed build and all prior round results remain recorded. No local Release worker build or web publish ran.

Commit with normal hooks next, then bind independent review to that exact SHA. Required publication-only checks: exact-SHA CI (including production Release publish), Pages artifact and deployment identity, deployed static artifact response. Browser/device/performance qualification and the separate friction/penetration candidate remain pending at their named gates. Terminal publication receipt will be appended after those checks, without claiming this local Pass publishes anything.


## Terminal publication receipt — 26 October 2026

**Terminal scoped Pass** for commit `b3905fcda3df89b38140c9610b8ff2034e4078a8`. Independent Auto review covered the unchanged committed source/fixture candidate; all 52 source/fixture diff entries matched the reviewed candidate. Normal pre-commit protections passed: normal, playtest and web builds with zero warnings/errors, 124 guard/UI tests with zero failures/skips, and Anvil checks.

Independent terminal verifier `/root/hash_terminal_verifier` verified [CI 40249169834](https://github.com/aidanmorgan/tim/actions/runs/40249169834): **4,212 passed, 0 failed, 0 skipped** (game 3,720; Coverage 77; BundleElements 206; WorkerAotGuard 17; E2eRunner 192). Game duration 18m12s. CI owns the production Release/publish proof. Earlier local totals of 4,228 included 16 deferred browser-toolchain cases; those are historical results, not browser qualification. Subsequent required local full runs use the existing `E2eRunner deferred-filter`, matching CI's explicit BrowserDeferral list while retaining every game test.

Pages artifact **12620546696** (`github-pages`, 80,779,740 bytes) belongs to that exact run and SHA. Deployment **7321697410** succeeded at **2026-10-26T00:56:00Z** for the same SHA. The verifier fetched production `/`, `index.html`, `404.html`, `godot.wasm`, `godot.pck` and `_framework/dotnet.js`; all returned HTTP 200 and matched artifact bytes in memory. No browser qualification is claimed.

This receipt is carried by the following penetration publication to avoid a separate receipt-only deployment. It does not alter the reviewed hash-test implementation. Broader behavioral-test hygiene, friction/penetration implementation and post-catalogue browser qualification retain their separate owners.
