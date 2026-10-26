---
title: 'Remove synthetic startup warmup from the core'
type: 'refactor'
created: '2026-10-26'
status: 'done'
baseline_revision: b39b893d827541ce6421b744078dd76460b73214
review_loop_iteration: 0
followup_review_recommended: false
context: [AGENTS.md, docs/testing-guidance.md]
warnings: [oversized]
deferred: []
---

<intent-contract>

## Intent

**Problem:** Owner request: “add a subtask to remove the warmup out of the core code, it shouldnt be there”. Core initialization currently executes dummy collision/solver exercises, a synthetic world and a separate dummy command lifecycle.

**Approach:** Delete synthetic startup work and its entire caller/fixture/diagnostic closure. Prove normal cold starts and reused-engine behavior through production interfaces.

## Boundaries & Constraints

**Always:** Preserve actual Device.Prepare/bootstrap, CaptureClock, ordinary Reset and resource ownership, pools, caches and solver warm-starting. Use real C# simulation, typed identities and observable semantic assertions; exact same-input replay and Reset stay exact. Current owner local-test authority applies: local suites/hooks prove tests; GitHub builds/publishes/deploys only. Keep warmup removal separate from remaining tolerance adoption and element work.

**Never:** Relocate dummy work into another runtime layer, leave a no-op API/alias, introduce compatibility or an alternate initialization path, remove solver warm-starting, widen physical bounds, assert output hashes/bytes or private pool state. No browser/mutation runs or local Release worker/web publish. Performance measurement warm-up at later named gates remains a distinct contract.

## I/O & Edge-Case Matrix

| Scenario | Input/state | Observable result | Error handling |
|---|---|---|---|
| Explicit preparation | New real device, Prepare then Initialize | Empty construction, tick0; first real ball Run/Advance applies and falls at tick1 | No error |
| Implicit preparation | New real device, Initialize without prior Prepare | Same initial and first-run semantics | No error |
| Empty bench | Real empty construction | Initialize/Construct/Run/Advance/Reset apply with ordinary bench state | No error |
| Reset/replay | Real ball after first tick | Exact authored restoration; second Run matches first physical tick with correct new generation | No stale state |
| Engine reuse | Two moving/spinning bodies then original plate/ball scene | Fresh/reused 120-tick typed trajectories and samples agree; static support fixed | No residual motion |
| Borrowed ownership | Borrower disposed before next simulation | Device usable; owner eventually disposes exactly once | No premature disposal |

</intent-contract>

## Code Map

- CuriousContraptions.Simulation/WorkshopPhysicsEngine.cs: delete Warmup, retain Reset and ordinary stepping; update static pooled-body reuse comment.
- CuriousContraptions.Simulation/BrowserGpuTransport.cs: remove Warmup call from Initialize; preserve reset/readiness/candidate initialization.
- CuriousContraptions.Simulation/Program.cs: delete WarmupInstallation, WarmupSimulation and its PrepareGpu call; preserve actual preparation/bootstrap and real clock.
- CuriousContraptions.Simulation/WarmupScene.cs and CuriousContraptions.tests/CuriousContraptions.tests.csproj: delete synthetic scene and explicit linked compile entry.
- engine/gpu/WorkshopDiagnostics.cs: delete Warmup enum member and mapping, no aliases.
- .editorconfig and CuriousContraptions.tests/CSharpStaticGateTests.cs: remove only obsolete warmup catch-all exemptions/expected text.
- CuriousContraptions.tests/ElementIsolationTests.cs and CuriousContraptions.tests/Fixtures/element-isolation-known.json: remove unused Warmup owner mapping and obsolete Program.WarmupSimulation/CaptureClock fixture row; retain GateDrive.
- engine/gpu/WorkshopSimulation.cs: ownership comment becomes ordinary borrowed simulation; preserve ownership behavior.
- CuriousContraptions.tests/DeclaredPrismaticDriveTests.cs: replace warmup API and output-byte preservation with cold-start/reuse assertions below.
- CuriousContraptions.tests/WorkshopGpuDeviceTests.cs: relabel borrower test/local variable and prove usability through normal initialization, retaining disposal ownership; remove touched private GateDisposed reflection assertion.
- CuriousContraptions.tests/WorkshopPhysicsLawTests.cs: reuse existing LawScene.Sphere/LawWorld.Step without editing the harness; Step resets engine and starts from admitted input every call. Build preconditioning as one real tick, then a separate second-scene replay.
- Read-only reuse source: unpublished `/Users/aidan/dev/personal/tim/.claude/worktrees/story-test-warmup-semantic-replay` worktree DeclaredPrismaticDriveTests.cs:465–599 has typed 120-tick observer/Same helpers, independently reviewed earlier. Adapt useful observations only; do not merge/publish the warmup-preservation intent or its Warmup call.
- _bmad-output/planning-artifacts/epics.md: supersede coordinator retention statement, C-Gate clauses and Story9A.21 under newest owner removal; preserve story/source IDs and other gate behavior.
- docs/architecture/element-package-isolation.md: update A9 to actual deletion status.
- TODO.md, sprint-status.yaml, deferred-work.md, test-behavioral-hygiene-inventory.md and this spec: current result, removed/replaced test mapping, next outcome, pending browser qualification. Append one short terminal receipt to `spec-local-test-authority.md`, and link it from the float spec. Verified prior publication: b39b893d827541ce6421b744078dd76460b73214, run40318527487 success, Pages artifact12647774040 (80,835,978bytes), deployment7331335202 success2026-10-26T12:03:49Z; all six static endpoints HTTP200 and artifact-byte equal; local4411/0/0, hooks126/0/0; remote tests intentionally absent. This covers unchanged float commit133859b4dfac2066a941a9a2833f08d97834abbd. No separate receipt-only deployment.

## Source requirements

Owner removal supersedes the synthetic-workload portions of C-Gate/9A.21 and A9 only. REQ-06 forward-only deletion, REQ-07 typed values, REQ-09 exact lifecycle and REQ-14 independent delivery apply; first-use/performance/browser qualification retains its existing later owners. No new catalogue identity is introduced.

## Tasks & Acceptance

**Execution:**
- [x] Delete synthetic startup closure and update necessary compile/diagnostic/isolation guards.
- [x] Replace removed tests with cold-start and ordinary engine-reuse behavior; retain unaffected drive/contact/ownership coverage.
- [x] Update current contracts and brief, explicitly superseding prior warmup retention.
- [ ] Verify affected native checks, Auto review/repairs, one final full local round, normal hooks and exact-SHA publication.

**Acceptance Criteria:**
- C1: Given worker bootstrap and PrepareGpu, when actual resources prepare, then no synthetic scene/command/kernel workload executes; actual device preparation and first real simulation remain available. Deleted API/callers/scene/diagnostic exemptions have no active executable remnant.
- C2: Given real BrowserGpuTransport with explicit Prepare and implicit preparation through first admission, when Initialize runs, then construction is empty and tick zero (ordinary bench bodies remain). First actual Basketball construction/Run/Advance all apply, tick becomes one, the intended body falls from its declared pose and no synthetic owners/events/payments appear. Reset restores exact declared body state; next Run/Advance reproduces first-tick physical semantics while epoch/revision follow their lifecycle contract.
- C3: Given an engine used for one real tick of two separated moving/spinning spheres, no bench/gravity/drag, when reset/readmission runs the original static support/passive plate/rolling ball scene, then 120 ticks match a fresh engine's typed body state, motion samples and passive-joint state. Preconditioning asserts both bodies actually move and rotate. Each replay independently proves the plate descends, ball advances, static supports remain fixed with zero motion, finite game-envelope state, correct counts/ticks/owners and right-open motion boundaries. No pool internals or serialized byte equality.
- C4: Given ordinary empty construction and borrowed-device ownership, when lifecycle/ownership tests execute, then Initialize/Construct/Run/Advance/Reset work, a disposed borrower leaves the device usable by the next simulation, and the owner disposes it exactly once.
- C5: Given active C-Gate/9A.21/A9 contracts, when current guidance is read, then synthetic startup removal supersedes the older coordinator retention choice. Named performance warm-up measurements, solver warm-starting and post-catalogue browser gates remain intact.

## Removed/replaced test coverage

Delete TheWarmupSceneCarriesNoDeclaredSliderOrDriveValue and TheWarmupAdmitsAndStepsItsSceneWithTheDriveSuppliedThenUnsupplied because their sole synthetic subject is removed. Ordinary declared-drive/contact behavior stays in existing drive, sensor/work and gate lifecycle tests; enumerate concrete covering methods in final evidence.
Replace AWarmedEngineStepsAWorldExactlyLikeAFreshOne with C3 semantic reuse. Replace AfterTheWarmupTheFirstRunIsAppliedAndEveryCommandOfTheEmptyBenchLifecycleIsApplied with C2 cold-start variants and an ordinary empty-bench lifecycle case. Rename ABorrowedWarmupLeavesTheDeviceToItsOwnerWhichThenInitialisesAFreshSimulation to ordinary borrowed ownership, retaining meaningful behavior without inspecting its private semaphore. The unpublished warmup-preservation slice is superseded, not published as-is.

## Spec Change Log

## Review Triage Log

### 2026-10-26 — Review pass 0
- verdicts: 15 findings — high 0, medium 1, low 2, false 12, maybe-false 0.
- findings:
  - [false] [reject] B1 — Cold-state assertions do not establish code absence, but C1 is established by actual deletion/caller inspection and matching compilation. Program.PrepareGpu now performs only Device.Prepare; BrowserGpuTransport.Initialize resets/readies the engine. No synthetic API/scene/caller survives. Adding a method-invocation test would assert implementation details rather than additional required behavior.
  - [false] [reject] B2 — The production entry point is explicitly inspected in the diff: Program.PrepareGpu retains Device.Prepare only, Bootstrap retains its session/device/clock setup, and real CaptureClock remains used by Dispatch. Native transport tests are not claimed as actual browser bootstrap proof; browser qualification remains at its named later gate.
  - [medium] [patch] B3 — Empty read populations do not directly prove the event publication is empty. Added decoded public DrainEvents record assertions at cold-start/empty-bench lifecycle boundaries.
  - [false] [reject] B4 — The removed Warmup already called Reset before returning, and LawWorld.Step called Reset again. It did not carry populated contact/friction caches into the old comparison. Ordinary engine Reset still clears both caches; new two-body preconditioning targets stale motion without claiming cache-population coverage.
  - [false] [reject] B5 — MovingContact equality intentionally proves same-input replay agreement. Independent contact semantics remain in TheMovingContactLaneRecordsAContactInAnySubstepOfTheTick and the drive/contact controls run in the 445-check set. No changed contact algorithm or independent contact-oracle claim is concealed.
  - [false] [reject] B6 — Cold-start acceptance requires first real movement at tick1, directly asserted with negative vertical velocity/decreasing height. Quantitative cadence/gravity is independently covered by AClearContactDoesNotSupportABodyBeforeItsFreeFallReachesTheSurface (analytic symplectic-Euler displacement and velocity across three cadences), retained for the full run. No timestep/gravity code changes.
  - [low] [patch] B7 — Added typed authored-instance comparison after Reset so retained part, pose and configuration are directly observed alongside the existing physical replay assertions.
  - [false] [reject] B8 — The existing ownership test uses a permitted external transport double to observe resource disposal, with real WorkshopGpuDevice and WorkshopSimulation. Subsequent Initialize executes admission and proves the device remains usable. Real transport first-tick/Reset semantics are separately exercised by both cold-start variants; requiring another combined scenario is not a demonstrated missing ownership behavior.
  - [false] [reject] B9 — The sibling source is an optional reuse pointer, not acceptance evidence: the adapted code is present in this diff, newly compiled/tested and independently reviewed. No prior review is reused to waive current proof. Proposed spec-only provenance edit does not fix an executable/review dependency.
  - [low] [patch] B10 — Clarified Story9A.21's historical C-Gate inclusion versus current removal implementation/review; retain its ID and later qualification owners.
  - [false] [reject] I1 — Planning-only reading is resolved by the active owner-authorized backlog goal: add the requested subtask, then execute ready TODO-linked work. The subtask was queued before this fresh slice, which starts after predecessor terminal publication.
  - [false] [reject] I2 — The owner requested removal from core, not preservation elsewhere. Complete deletion satisfies that expectation; no relocated synthetic runtime workload is required.
  - [false] [reject] I3 — Code absence and observable state are distinct evidence: direct deletion/caller review proves absence, native tests prove the real lifecycle still works. No claim that empty reads independently prove no prior hidden work.
  - [false] [reject] I4 — Production worker/bootstrap browser execution and first-use performance are expressly at later named owner gates. Current native tests plus matching worker compilation satisfy required-now proof without claiming browser/performance qualification.
  - [false] [reject] I5 — The prior publication receipt is required carry-forward evidence in the next slice, not new float-policy behavior or an extra publication task; unchanged predecessor code is not reimplemented.
- Edge Case Hunter returned []; Verification Gap Reviewer found no gaps. Forward-Only Auditor found no violations, independently checking runtime deletion, shared explicit/implicit preparation, ordinary owned/borrowed lifetime and static pooled-body clearing.
- Review identities: /root/warmup_removal_blind_r0, /root/warmup_removal_edge_r0, /root/warmup_removal_verification_r0, /root/warmup_removal_intent_r0 and /root/warmup_removal_forward_r0. The four-slot platform required two launch waves; all five launched before triage. No duplicate review system, mutants or browser checks.


## Design Notes

Preconditioning two dynamic bodies before any static body exercises state clearing when the subsequent scene reuses storage for its bench/frame, without asserting a private slot assignment. Sphere positions must be separated and motion nonzero; one real tick suffices. Keep the existing plate/ball scene and observation window. Canonical equality is a same-input replay contract, not a nanometre drift oracle. Body identity comparisons remain exact; Reset's epoch/revision progression is asserted separately rather than requiring stale identity across generations.

## Verification

Read current local-test-authority commands. Capture required matching compiler logs for this fresh worktree, using Debug compiler proof only; refresh complete affected logs when relevant inputs change, reuse unchanged logs. Implementer does not commit, push, or run the full suite. Implementer runs affected real native classes (DeclaredPrismaticDriveTests, WorkshopPhysicsEngineTests, WorkshopGpuDeviceTests, WorkshopSimulationTests, CSharpStaticGateTests, ElementIsolationTests, relevant prismatic/friction/sensor-work and event consumers) with no-build/no-restore and required CSHARP_STATIC_GATE_BINLOGS. Coordinator completes Auto five layers and accepted repairs/retained checks before one final full solution round with generated browser deferral filter. All required local tests execute, zero skips. Normal hooks remain mandatory; no remote game/tool tests. Exact committed-SHA approval then production build/artifact/deployment/static-origin verification. Browser and first-use performance qualification stay at their named later gates.


## Implementation handoff — 26 October 2026

Runtime deletion is complete: `Warmup`, its transport call, `WarmupScene`, `WarmupSimulation` / `WarmupInstallation`, the diagnostic member/mapping and their compile/guard/fixture closure are removed. Actual Prepare/bootstrap, CaptureClock, ordinary Reset, caches/pools and solver warm-starting remain. The LawWorld harness is unchanged. All writes received Anvil `allow`. Its graph first reported not-ready and later returned pre-edit symbols despite a clean attestation; deletion evidence therefore uses the actual filesystem and matching compilation, not that stale graph result.

C2 runs explicit and implicit Prepare variants through the real BrowserGpuTransport/device/simulation, checks empty tick-zero initialization, the declared ball's first fall, no synthetic activity/payment populations, exact physical Reset and first-tick replay with new epoch/revision. C3 preconditions the engine with two separated spheres translating and rotating for one real tick, then observes the original scene for 120 ticks on both fresh and reused engines. Typed trajectories, right-open motion samples, passive joints, finite envelopes and static supports are asserted. C4 retains ordinary empty-bench and borrowed-device ownership tests. The [replacement inventory](test-behavioral-hygiene-inventory.md#synthetic-startup-removal--26-october-2026) names every removed test and retained ordinary drive/contact/sensor/work/event coverage. No legacy runtime replacement or compatibility path was added.

**Compiler proof:** physical directory `/private/tmp/tim-remove-warmup.wMdgoa`, .NET `10.0.402`, workload `wasm-tools 10.0.112/10.0.100`, Node `v26.12.1`. Commands follow `docs/delivery-workflow.md#local-test-authority`. Logs: `playtest.binlog`, `solution.binlog`, `web.binlog`, `check-staged-tests.binlog`, `verify-policy.binlog`, each with adjacent `.log` output. Final PLAYTEST/solution captures passed with zero warnings/errors in 22.95s/13.31s. Debug web capture passed with zero warnings/errors in 8.60s; file-app compiles passed and CSharpStaticGateTests validates their actual Csc records. No local Release worker build or publish ran.

**Affected native evidence:** session55495 / `affected.log`: **445 passed, 0 failed, 0 skipped**, 20s. Executed command:

```sh
CSHARP_STATIC_GATE_BINLOGS=/private/tmp/tim-remove-warmup.wMdgoa dotnet test CuriousContraptions.tests/CuriousContraptions.tests.csproj -c Debug -f net10.0 --no-build --no-restore --nologo -noAutoResponse --filter 'FullyQualifiedName~DeclaredPrismaticDriveTests|FullyQualifiedName~WorkshopPhysicsEngineTests|FullyQualifiedName~WorkshopGpuDeviceTests|FullyQualifiedName~WorkshopSimulationTests|FullyQualifiedName~CSharpStaticGateTests|FullyQualifiedName~ElementIsolationTests|FullyQualifiedName~WorkshopPhysicsLawPrismaticTests|FullyQualifiedName~WorkshopPhysicsLawFrictionTests|FullyQualifiedName~WorkshopPhysicsLawSensorWorkTests|FullyQualifiedName~EventRingConsumerTests|FullyQualifiedName~EventRingProducerTests|FullyQualifiedName~PrismaticConstraintTests' -- RunConfiguration.TreatNoTestsAsError=true
```

Final inspection corrected only the new cold-start test's height conversion to `(Cell.Y + Local.Y) / 16f`. The prior assertion passed but its local component had the wrong unit. Refreshed complete solution/PLAYTEST logs and repeated `DeclaredPrismaticDriveTests|CSharpStaticGateTests` with the same command options: session46522 / `affected-recheck.log`, **108 passed, 0 failed, 0 skipped**, 6s. Other 445-check inputs are unchanged. Initial compilation found two test assertions comparing the epoch wrapper to the body's ulong; these were corrected before the passing captures. No failed native test occurred. `git diff --check` passes; executable/source/project search for `WarmupScene|WarmupSimulation|WarmupInstallation|\.Warmup\(` returns zero hits. Performance benchmark settling loops and the historical GateDrive story identity remain intentionally distinct.

**Pending coordinator work:** independent Auto five-layer review/repairs, one final full local solution round with generated browser deferral filter, normal hooks, committed-SHA SnapshotApproval and exact-SHA production publication. No full suite, commit, push, browser or mutation run was performed by the implementer. Browser and first-use/performance qualification remain pending at their named later gates; no terminal Pass is claimed for this slice. The preceding policy/float publication receipt is recorded separately in `spec-local-test-authority.md` as authorized by the spec.

## Coordinator verification and matrix audit

Reviewed the complete actual staged diff, including deletions and all current contract/fixture changes. Temporary diff evidence is kept in the ignored rendered-workflow directory because Anvil forbids writes outside the admitted workspace. The system-temp write was refused before any file was created; no gate was bypassed.

All six matrix rows have registered production-interface tests included in passing affected runs: explicit/implicit initialization and Reset/replay in AColdDeviceStartsTheFirstRealBallAndResetReplaysItsFirstTick (both theory rows); empty bench in AnEmptyBenchAppliesItsOrdinaryLifecycleWithoutProducingDynamicBodiesOrEvents; engine reuse in AReusedEnginePreservesEveryBodyTrajectoryAndStaticSupportInTheNextScene; ownership in ABorrowedSimulationLeavesTheDeviceUsableUntilItsOwnerDisposesIt. Initial affected445/0/0 covered all named classes; final pre-review108/0/0 covered drive/static guards. Review repairs B3/B7 passed all16 drive-class tests with0 failures/skips; B10 is documentation-only. Matching compiler captures and the final full suite follow the retained repair confirmation.

Follow-up five-layer review is not recommended: accepted patch entries are medium1 and low2, high0; no unresolved high or second medium entry. The retained Verification Gap reviewer independently checks repaired assertions. No earlier speculative full suite ran in this slice.

## Auto Run Result

Local implementation and independent Auto review are complete. Deleted the synthetic kernel/solver exercises, startup scene and dummy command lifecycle; preserved real device preparation, worker bootstrap/clock, ordinary Reset/resource ownership, pools/caches and solver warm-starting. Replaced obsolete warmup/byte-equality tests with actual cold-start, first-tick, exact Reset/replay, decoded empty-event, authored-instance, empty-bench and 120-tick engine-reuse observations.

Files changed: engine/transport/Program and WarmupScene deletion remove runtime work; diagnostic/ownership files remove obsolete terminology/member; test project, static/isolation guards and fixture remove dead references; DeclaredPrismaticDriveTests and WorkshopGpuDeviceTests prove retained behavior; epics/isolation/TODO/sprint/deferred/inventory record the superseding owner contract; predecessor float/policy specs carry the verified terminal receipt; this spec records review and verification.

Review: 15 individually triaged findings, high0/medium1/low2/false12. Three small patches B3/B7/B10 are applied; retained independent /root/warmup_removal_verification_r0 confirmed all resolved with no new required-now regression. No deferred findings. Every rejection and its reason is retained above. Follow-up review recommendation false (patched high0, medium1, low2); no new five-layer loop is required.

Final matching compiler captures: PLAYTEST session68909 passed0 warnings/errors in23.53s; solution69979 passed0 warnings/errors in21.65s. Unchanged web and both file-app captures remain applicable. Final full native session39307 exited0, using required CSHARP_STATIC_GATE_BINLOGS=/private/tmp/tim-remove-warmup.wMdgoa, generated browser deferral filter, Debug/net10.0, --no-build --no-restore -noAutoResponse -m:1 and TreatNoTestsAsError=true: **4,411 passed,0 failed,0 skipped**. Game3919/9m9s; Coverage77/654ms; E2eRunner192/12s; WorkerAotGuard17/41ms; Bundle206/340ms. This includes game runtime Node harnesses. One full local round ran after all review repairs. Spec YAML/affected sprint membership and git diff --check passed.

Local Auto status done does not claim terminal publication. Remaining: normal-hook commit, exact committed-SHA SnapshotApproval, non-force publication from the reviewed baseline, production build, Pages artifact/deployment and static-origin verification. GitHub executes no game/tool tests. Browser and first-use/performance qualification stay pending at their named later gates; no global numeric/test-hygiene or release qualification is claimed.

## Terminal publication receipt — 26 October 2026

Published `822317c99a08ec64ce2d7406090036d51c1c8fa2`. Production build run `40324744448`, Pages artifact `12650828689` (80,805,681 bytes), deployment `7332347579`, deployment status `20623823471`. Six static production endpoints returned 200 and matched the artifact. Full local 4,411/0/0; normal hooks 126/0/0. This receipt supersedes pending-publication wording above; browser and first-use/performance qualification remain pending.
