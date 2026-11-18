---
title: 'Faster validation without weaker tests'
type: refactor
created: '2026-11-18'
status: done
baseline_revision: c34f63903722295ae0e130aa3d279876d20420bb
review_loop_iteration: 1
followup_review_recommended: false
context:
  - /Users/aidan/dev/personal/tim/_bmad/custom/project-engineering-contract.md
  - /Users/aidan/dev/personal/tim/_bmad/custom/element-free-engine-design.md
  - /Users/aidan/dev/personal/tim/_bmad/custom/element-free-engine-review.md
  - /Users/aidan/dev/personal/tim/_bmad/custom/bmad-loop-protocol.md
  - /Users/aidan/.codex/worktrees/tim-test-loop/tim/docs/testing-guidance.md
warnings: [multiple-goals, oversized]
deferred: []
---
<intent-contract>

## Intent

User: "make the changes you identified", following investigation of unacceptably slow E2E validation without relaxing tests. Additional user direction: "add a task to investigate why there are sleeps in there, we shouldnt be sleeping as it will slow things down".

**Problem:** Each browser invocation publishes again, Preview can build implicitly, and expensive independent native physics cases execute serially. Fixed UI waits and contention waits need their causal purpose established. Historical wall time also includes host sleep.

**Approach:** Reuse a named CI-produced artifact through one verified consumer path, launch a prebuilt Preview, and bound parallel execution of independent native seeded cases. Investigate sleeps explicitly and replace only waits whose equivalent observable completion can be established. Keep browser tuning requiring actual Chrome proof pending at the existing gate.

## Boundaries & Constraints

**Always:** Preserve every case, seed, assertion, negative observation window, timer, failure and lifecycle check. Existing native fixture/collection remains exclusive. Artifact identity is not simulation acceptance. Stale/malformed artifacts refuse before Preview/Chrome. All required native tests run, zero skips; compiler evidence and normal hooks remain. Use C#, typed enums and current boundaries. Keep exact baseline and unrelated dirty work.
**Never:** No blanket timeout reduction, case removal, browser worker increase without calibration, unsafe shared browser state, hidden publish fallback, stale binaries, simulation-output hashes or mutants. No local Release/web publish or browser gate. No hook bypass or compiler-proof weakening. No OS power-setting changes. No unrelated backlog execution.

## I/O & Edge-Case Matrix

| Scenario | Input/state | Expected behavior | Refusal |
|---|---|---|---|
| Repeated artifact run | Same valid CI artifact and applicable source | Reuse artifact; no publish/build during execution; fresh run outputs | None |
| Stale/malformed artifact | Missing/invalid receipt, changed bundle, missing required file, unknown source or changed production input | No Preview/Chrome launch | Typed runner error |
| Harness-only change | App inputs identical; only explicitly classified harness/docs inputs change | Same artifact remains applicable; harness identity separately recorded | Unknown changes conservatively refuse |
| Native parallel cases | Existing streams/cases at maximum two workers | Same seeds, complete observations and assertions, deterministic diagnostic order | Any job failure fails test |
| Test sleeps | Fixed delay, polling backoff, observation window, UI hold or contention wait | Name purpose and observable replacement where equivalent | Preserve required duration/isolation when equivalence unproved |

</intent-contract>

## Code Map

- Checkout: /Users/aidan/.codex/worktrees/tim-test-loop/tim, branch perf-test-loop.
- Original baseline_revision: c34f63903722295ae0e130aa3d279876d20420bb (published origin/main at entry). No carried product changes.
- Policy: primary checkout HEAD 30eba561c09164a4bc8c0c8080b7b7fcbdc16760 plus explicitly loaded, read-only uncommitted _bmad/custom guides/Auto overrides and supplied owner AGENTS. Do not copy/commit unrelated primary-workspace changes.
- Effective Auto render: project-customized bmad-build-auto, rendered in the primary checkout on 18 November. Structural preflight passed with zero findings; six active review layers inspected: blind-hunter, edge-case-hunter, verification-gap, intent-alignment, forward-only, element-free-engine.
- tools/E2eRunner/Program.cs: CLI, implicit publish per invocation, selection and calibration. SourceIdentity.cs existing harness identity; BundleIdentity.cs existing bundle identity/watch/served verification; PreviewHost.cs implicit dotnet run build; RunEngine.cs source/end checks, quiet waits and retry.
- .github/workflows/pages.yml: existing Release producer and main Pages upload. Stamp final bundle after .nojekyll; upload reusable artifact for successful branch builds.
- CuriousContraptions.tests/WorkshopPhysicsLawCylinderTests.cs: Gate (~2537) six independent class streams per material; controls and seeds fixed. NativeSceneFixture.cs/BundlePortsTests.cs/xunit.runner.json enforce one cold-start fixture and serialization; KEEP them. LawMaterial/LawBalls depend on installed DeclaredPhysics; global collection parallelism unsafe.
- tools/e2e/workshop-driver.ts: launch/reload, UI_SETTLE_FRAMES, SCROLL_SETTLE_FRAMES, input holds and holdUntil. Suite files have observation waits. Keep actual UI construction.
- Historical raw evidence: /private/tmp/tim-reverse-final-gate.JRBtqA/round3/results and full-tests.log; 5661 game tests, 1639.131s summed active durations vs 4080.080s wall. pmset logs corroborate Software Sleep 19:52–19:57 and Clamshell/Maintenance 20:01–20:37 on 17 November. Do not claim avoiding host sleep as code speedup.

## Tasks & Acceptance

**Execution:**
- tools/E2eRunner/Program.cs, PublishedArtifact.cs (new), RunnerError.cs, SourceIdentity.cs, RunEngine.cs: sole artifact consumer with mandatory --artifact for full/run/calibrate; CI record-artifact producer verb; remove implicit publish and old output path. One receipt beside AppBundle records source revision, CI run/attempt and existing bundle identity, not a duplicate manifest. Treat every changed path as production-affecting unless on a narrow explicit non-production allowlist: tools/e2e/**, tools/E2eRunner/**, tools/E2eRunner.Tests/**, docs/**, _bmad/**, _bmad-output/** and Markdown files. Verify these exemptions against publish inputs and narrow where any is consumed. Compare artifact revision against committed/index/worktree contents and untracked inputs; unknown paths, revisions and unsafe symlinks refuse. Check production source again at run end. Test worker/build-target edits, new production files, allowed harness edits and mid-run source edits. Trusted CI provenance is not cryptographic authentication.
- .github/workflows/pages.yml: produce receipt after existing publish/static checks and upload exact artifact, including hidden files, on branches; preserve Pages. No tests added to CI.
- tools/E2eRunner/PreviewHost.cs and project dependency/build wiring: make Preview a normal runner build dependency and consume its executable from that runner build's output directory, not an independently stale checkout-relative bin path. Normal incremental runner build rebuilds/copies Preview on its relevant source/config changes; documented no-build reuse requires those matching outputs, same as native tests. Native integration check changes a temporary Preview project input, rebuilds the dependency and proves updated served response or matching-output behavior; no execution-time build fallback. Missing copied outputs refuse typed.
- tools/E2eRunner.Tests: native receipt/CLI/freshness tests with temporary Git repos/artifacts, positive/control/boundary cases without browser or publish.
- CuriousContraptions.tests/WorkshopPhysicsLawCylinderTests.cs: independent Gate class streams degree two, each owns RNG and engine/world, deterministic indexed results and post-join diagnostics. Preserve serial controls and all case counts. Independently assess similarly isolated rim/overlap pose batches; implement only if input stream/state ownership is fully preserved.
- tools/e2e/README.md and existing verification documentation: current commands, artifact freshness/trust limits and build reuse; full-round/hook proofs stay unchanged.
- Explicit sleep-investigation task: inspect all E2E/native runner sleeps, fixed frame waits and polling delays; record call site, reason, cumulative cost where measured, required observation semantics and replacement condition in this spec's Implementation Notes. Implement safe observable waits only when they can be verified under current native tools; retain unproved browser timing changes as pending browser qualification, with owner/next check. Distinguish OS sleep; document awake benchmark procedure without system-setting mutation.
- Profile affected native test durations against existing active timings; no invented speedup claims. Record investigation and pending browser calibration/reuse measurements here, not another ledger.

**Acceptance Criteria:**
- Given a valid artifact and matching production source, when full/run/calibrate executes repeatedly, then no application publish or Preview build occurs and all existing suite selection/isolation/verdict checks remain.
- Given stale, malformed, changed, unsafe-path or missing artifact inputs, when admitted, then typed refusal precedes server/browser startup; source/bundle changes during a run cannot yield Pass.
- Given unchanged seeded physics cases, when bounded jobs execute, then every original case and per-tick assertion remains and failures propagate with reproducible case identification.
- Given a fixed sleep/wait, when investigated, then its purpose and safe removal/replacement or retained contract are documented; no required negative-observation/timer window is shortened.
- Given the final candidate, when local verification and independent Auto review complete, then required tests pass with zero skips and no unresolved confirmed defects; browser/remote qualification remains explicitly unverified.

## Design Notes

Eight answers: outcome faster equivalent validation; reusable capabilities existing artifact manifest/source identity, Preview and independent LawWorld; declared input is explicit artifact/CI receipt and unchanged test streams; path CI publish→receipt→source/bundle admission→existing runner and native jobs→ordered assertions; ownership CI artifact producer/runner admission, each job owns mutable state, native fixture unchanged; generality temporary repository/artifact variations and unchanged positive/control/boundary physics; deletes implicit PublishAsync/build-on-preview path; uncertain browser launch/frame cost and concurrency calibration stay at browser gate, not silently optimized.

E2/E4/E5 apply to typed tool boundaries and artifact/process lifecycle; E7/E8/E9 apply to evidence, scope and maintainability. D1–D7 engine behavior unchanged; no element capability/production physics change. Compiler/hook redesign is not justified by duplicate build counts alone and remains unchanged unless equivalent exact-input evidence is independently established.

## Verification

Implementer owns incremental affected build/tests and pre-review E9; coordinator owns final compiler captures/full solution round and final E9. Normal hooks separately required before authorized publication; no CI wait. Raw logs outside checkout. Relevant source/dependency/config changes invalidate affected outputs.
- Native runner tests cover admission and real file/Git boundaries; existing deferred methods remain solely in BrowserDeferral.
- Affected cylinder tests use matching Debug/net10.0 output, no rebuild between cases; capture active times and host sleep if any.
- Final full local round uses docs/delivery-workflow.md local-test-authority five normal compiler logs, all five projects and existing browser-only filter.
- E9 uses /Users/aidan/dev/personal/tim/tools/anvil/VerifyPolicy.cs -- --maintainability --project-root /Users/aidan/.codex/worktrees/tim-test-loop/tim; both canaries and coupling summary required. Rebuild normally after injected gate before no-build verification.

## Spec Change Log

### 2026-11-18 — Owner prioritizes native end-to-end throughput
Subsequent user direction: "focus on the end to end non-browser tests, we need to make these exponentially faster". The remaining work targets an order-of-magnitude native latency reduction, subject to measurement; no such result is claimed. Previously implemented browser artifact work remains preserved, with no further browser optimization or qualification in this task. Publication pauses while native scope evolves.

The current full round at /private/tmp/tim-test-loop-final.dZtaoS is now the before-change native baseline. Its five compiler captures completed successfully and show /optimize- for Core and the native test assembly. No edits/builds to that closure occur while the round is running. The single fixture already performs cold catalogue startup once; global test parallelism remains unsafe because native scene and scoped catalogue state are shared.

Retained implementation owner /root/test_loop_implementer proposed, and /root/test_loop_planning_auditor independently granted planning Scoped Pass:
- Set unconditional Optimize=true in CuriousContraptions.Core/CuriousContraptions.Core.csproj and CuriousContraptions.tests/CuriousContraptions.tests.csproj only. Core owns production simulation and codecs; native tests own expensive independent geometric oracle loops. This is one production algorithm, without an alternate test execution path.
- Preserve Debug configuration, DEBUG/PLAYTEST definitions, portable symbols, every case/assertion/observation window, collection isolation, analyzers and warnings-as-errors. Optimized locals may be less visible to a debugger; diagnostics and test coverage remain.
- After the baseline finishes, rebuild the affected dependency closure once and inspect actual compiler invocations for optimization and retained guard settings. Prior affected binaries/compiler captures then become historical.
- Compare identical named energy3, rim/overlap4, producer2 and representative native lesson/Reset cases against the baseline TRX. No pre-change rerun and no exploratory full round; a final required full round follows the completed reviewed implementation.
- If numerical behavior differs, investigate under existing gameplay policy, without fitting bounds, dropping cases or weakening properties.
A separate measured follow-up candidate is repeated electrical-source array copying/scanning inside every presentation binding in WorkshopSceneCompiler: approximately1.15 billion copied rows for8739 producers. Consider an invocation-local typed lookup only after optimized measurements establish the remaining cost; no persistent cache, shared state or speculative framework.

Prior six-layer review/repair and final scope check covered the earlier21-file candidate only. Its affected repair re-reviews all resolved and E9 passed both canaries with0metric/0coupling findings. The new compiler-setting amendment requires affected independent review, matching final evidence and renewed final scope assessment before completion.


### 2026-11-18 — Controlled review repair
The energy oracle and ignored-source admission need bounded design repair before implementation. Preserve immutable intent, baseline and retained owner. KEEP artifact-only execution, prebuilt Preview, all seeds/cases/observations, bounded job ownership, deterministic diagnostics, missing-contact assertion and patched toolchain. No blanket reversion. The fixed-seed corpus supplies reproducibility; explicitly defined domains/boundaries and independent properties supply test validity. No new property framework is required solely for determinism.
The retained owner's proposal received independent planning Scoped Pass from /root/test_loop_planning_auditor before code repair. Implement these exact bounded decisions: Provenance refusal cases are a direct patch.
- Gate and authored case179 use the documented local per-committed-tick energy-injection ceiling of **1.962 mJ/kg**, fixed in the test independently of production ContactSlop and measured control output. The Thermodynamic Energy row in docs/gpu-f32-physics.md names this interface limit for the sampled single-body/static-support cylinder classes. This is not a new tolerance derived from slop and does not claim cumulative conservation. Preserve every generated case, seed, material and three-second window; assert the same fixed limit independently on all serial box controls, finite energies and initial-to-first-tick as well as subsequent transitions. Replace the dependent comparison with this stricter independent contract and retain reproduction diagnostics. Report any failure without fitting the bound or changing the solver.
- Artifact source enumeration includes every untracked input regardless of Git ignores. Tracked HEAD/index/worktree comparisons remain distinct and never receive generated-output exemptions. Unknown paths refuse; only audited root build/cache locations, bin/obj immediately under tracked project directories and exact outputs of RuntimeTypeScript/BundleElements receive generated exemptions. No broad *.tmp or arbitrary AppBundle exemptions. Check each audited generated-directory root and its ancestors for links, then treat its untracked internals as opaque generated state; required npm .bin links are legitimate. Source, unknown paths, exact generated-file outputs and the artifact tree still reject links. Project bin/obj opacity requires identified tracked project roots whose build inputs exclude them. Test legitimate npm internal links, redirected generated roots, unknown source links and tracked changes beneath generated roots. Planning auditor corrected the earlier blanket link ordering after actual npm-ci output established the conflict. Test local/global ignored production sources/resources/build inputs, legitimate generated output reuse, fake generated-looking source paths and tracked changes.
- Record and Admit both reject zero, negative and nonnumeric run/attempt identifiers with valid source/bundle/other identifier, alongside existing positive cases.
Planning acceptance is source/design only; affected execution and independent repair review still required. Necessary affected test/documentation changes belong to this repair closure; unrelated numerical tests are not a new audit assignment.


## Review Triage Log

### 2026-11-18 — Review pass
Six independent layers completed with first launches `fork_turns=none`, in bounded waves: /root/test_loop_blind, /root/test_loop_edge, /root/test_loop_verification, /root/test_loop_intent, /root/test_loop_forward and /root/test_loop_engineering. No earlier findings were supplied to later layers.
- verdicts: 4 findings — high 1, medium 3, low 0, false 0, maybe-false 0.
- [medium] [bad_spec] Blind: Gate and case179 derive acceptance from the observed box control plus production ContactSlop. Confirmed in Gate/GateCase/regression: common energy regressions or solver-setting changes can enlarge acceptance. Replace the dependent oracle with independently justified physical acceptance; preserve generated cases and meaningful controls.
- [high] [bad_spec] Edge: ArtifactSource enumerates untracked files with --exclude-standard, so local/global ignores can hide a new production source from freshness checks while compiler inputs include it. Enumerate ignored inputs too, exempt only proven generated/non-production paths independently of user Git ignores, and prove local/global ignore refusal plus normal generated-output reuse.
- [medium] [patch] Verification: invalid CI run/attempt identifiers have no refusal test despite positive-integer validation in Record and Admit. Add zero, negative and nonnumeric cases through both boundaries, with valid bundle/source controls.
- [medium] [bad_spec] Engineering: same energy-oracle defect, independently confirmed under E7. This shares the blind finding's repair; no solver change or observation-fitted tolerance is authorized.
- Forward-only: no findings. Intent alignment: mechanisms match artifact reuse, native-loop speed and reproducible samples; a property framework/shrinking is not implemented or claimed. Sleep task is recorded above. Toolchain repair is justified by the recorded audit failure. Remote/browser performance remains unqualified.
- Terminal acceptance remains Incomplete pending repairs, affected independent re-review and final full/compiler/E9/hook proof.


## Implementation Notes

Planning audit first pass /root/test_loop_planning_auditor: Incomplete for unspecified default-deny source admission and Preview build ownership. Both amended above; independent affected re-review returned planning Scoped Pass. No implementation acceptance implied.

Implementation owner: /root/test_loop_implementer, first launch fork_turns=none; retained for repairs. Coordinator owns full round and final compiler captures. No engine behavior changes; D1–D7 remain unchanged. E2/E4/E5 apply to artifact/tool typed boundaries and process ownership, E7–E9 to verification and delivery.

### Initial verification evidence (historical 10.0.13 toolchain, superseded for final qualification)

- Implementer /root/test_loop_implementer (`fork_turns=none`) owns affected Debug/net10.0 builds, targeted native checks and pre-handoff E9. Coordinator /root owns final five normal compiler logs, full solution round, final E9, hooks and independent approval. These initial commands used `PATH=/private/tmp/tim-dotnet-10.0.402:/private/tmp/node-v26.12.1-darwin-arm64/bin:$PATH DOTNET_ROOT=/private/tmp/tim-dotnet-10.0.402` (SDK 10.0.402, runtime/ref pack 10.0.13, workload 10.0.402.1, Node 26.12.1).
- Matching runner build: `dotnet build tools/E2eRunner.Tests --nologo -noAutoResponse`, latest raw `/private/tmp/tim-test-loop-runner-build5.log`. Native build: `dotnet build CuriousContraptions.tests --nologo -noAutoResponse`, raw `/private/tmp/tim-test-loop-native-build3.log`. Each passed with zero warnings/errors. Relevant source, project/dependency/configuration or generated inputs invalidate matching output reuse.
- Affected runner command: `dotnet test tools/E2eRunner.Tests --no-build --no-restore --filter 'FullyQualifiedName~ArtifactTests|FullyQualifiedName~CommandLineTests|FullyQualifiedName~PreviewBuildTests|FullyQualifiedName~SchedulerTests|FullyQualifiedName~IdentityTests.PreviewServes|FullyQualifiedName~IdentityTests.PortHeld|FullyQualifiedName~IdentityTests.LostPort' --logger 'console;verbosity=normal'`. Raw latest `/private/tmp/tim-test-loop-affected-runner3.log`; previous checkpoints 41/0/0 and 46/0/0 in `affected-runner.log` and `affected-runner2.log` with the same prefix.
- Affected physics command: `dotnet test CuriousContraptions.tests --no-build --no-restore --filter 'FullyQualifiedName~FreshSeedDropsGainNoMore|FullyQualifiedName~FreshSeedDropsWithRestitution|FullyQualifiedName~ARimAimedWalkThroughTouch|FullyQualifiedName~OverlapDepthNeverExceeds' --logger 'console;verbosity=normal'`, raw `/private/tmp/tim-test-loop-affected-native.log`. This is a scoped check, never the required full round.
- Failed build attempts retained: first runner build `/private/tmp/tim-test-loop-runner-build.log` reported CA1859 on source-path enumeration; fixed the return type to the actual array. Native build2 reported CA1861 after moving the overlap loop into jobs; allocated the unchanged depth values once outside jobs. No assertion/seed/window changes.
- Source admission checks artifact commit against HEAD, index, worktree and untracked files, then rejects source/artifact links before reads. Exemptions were traced against explicit C# compile includes, Web export `exclude_filter="docs/*,..."`, and tooling `.gdignore` guards. Changes to `.gdignore` are not exempt. Other build targets, workers and new unknown production files refuse. Artifact metadata authenticates neither CI nor source: trusted workflow provenance is an explicit assumption.
- Preview's normal project reference copies its executable/runtimeconfig/deps alongside the runner and test host. Native build integration uses purpose-built temporary programs and the actual project wiring; changing a temporary assembly-version input must update copied assembly metadata. No mutated production source or mutation testing is used. Existing native serving and port-lifecycle tests exercise the real Preview.
- Gate jobs retain serial box controls and original per-material/class RNG seeds. Each job owns the RNG, scenes, engines/world buffers and diagnostics. Solver shared scratch is thread-local; fixture/collection exclusion remains. Rim/overlap draws are generated serially in the identical order before bounded jobs; collider/contact/oracle state is per pose, and output is flattened by pose index. All original observations and assertions remain.
- Numerical-policy review: no new numerical oracle, tolerance, seed or trajectory requirement was introduced. Existing Gate energy checks exclude created energy relative to their control; existing oracle arithmetic and per-tick checks are preserved for this scheduling change. Historical microradian/rest precision elsewhere in the class is not promoted into new acceptance. If any numerical-only failure occurs, the 28 October policy governs before further repair.

### Sleep investigation completed

Raw call-site inventory: `/private/tmp/tim-test-loop-sleep-inventory.txt` (`rg -n 'Task.Delay|Thread.Sleep|waitForTimeout|setTimeout|_SETTLE_FRAMES|frames\\(' tools/E2eRunner tools/E2eRunner.Tests tools/e2e`). Actual aggregate wall cost is unmeasured; frame counts are not fixed milliseconds.

| Call sites / category | Purpose and retained semantics | Action / next observable replacement |
| --- | --- | --- |
| Native game tests | No executable Task.Delay/Thread.Sleep; TickLoopTests contains a comment only | Physics concurrency changes shorten independent work, not simulation duration |
| SchedulerTests.Execute | Previously arbitrary 10–32 ms delays created overlap | Replaced with first-three-worker completion barrier plus yielding; assert peak exactly three and preserve serial isolation/order checks |
| RunEngine.QuietLoadAsync / WaitForNoForeignChromeAsync | Five-second polling backoff, up to ten-minute contention/isolation guard | Retained; load-average/foreign-process observations have no completion notification; no success-window reduction |
| PreviewHost.WaitUntilAnsweringAsync | Ready notification then HTTP readiness; 200 ms backoff only on unsuccessful response/connection | Retained observable readiness and failure backoff; normal launch now excludes builds |
| ProcessesTests.CommandPastItsLimit / Gone | Child sleeps 60 s to prove one-second timeout kills process tree; 100 ms bounded reaping poll | Retained controlled timeout stimulus and process-exit observation, not a 60-second normal wait |
| IdentityTests.RunWaitingAsync and wait/hang fixtures | 100 ms fixture-start polling, fixed 1500 ms grace; release-file backoff and intentional hanging child | Browser-toolchain tests already deferred; replace grace with fixture-ready signal at post-catalogue gate before claiming improvement |
| Driver readiness, UI and input frames | Readiness five frames, UI settle three, mouse hold four, 70 ms key hold (shorter documented ignored), picker 250 ms guard | Retained until actual Chrome proves command/readiness/UI acknowledgement equivalence |
| Driver scroll tool/parameter paths (739/743, 1098/1101 at baseline) | Two 30-frame waits per scroll action; highest obvious fixed-frame candidate | Pending actual UI scroll/layout completion signal and Chrome proof; no timing calibration guessed |
| Driver pose reads / holdUntil / console/command polling | Up to five 10 ms pose-read retries; finite observable-state polling with real input held | Retain backoff and input duration semantics; replace only with equivalent committed-state notification |
| Driver negative windows and suite-specific waits | Twenty-frame no-command observation, Run/Reset windows, intended holds, rate/retention windows and fault-injection pauses | Retain full observation/timer/cadence; a signal cannot replace proving absence over a required interval |
| setTimeout fail-safe races | Finite timeout guards cleared on completion | Retained; they do not delay successful execution |

Browser follow-up owner: project owner/coordinator at post-catalogue Chrome qualification, after every named element's native completion. Next check: instrument scroll/layout and fixture-ready boundaries, preserve negative windows, then compare repeated artifact reuse/calibration on actual Chrome. Browser/remote artifact/build/deployment qualification is unverified.

Awake benchmark procedure: keep the host connected/open and awake under the operator's normal session, record start/end and raw active test durations, inspect `pmset -g log` for sleep during that interval, and report elapsed vs active time separately. No system power settings are changed. Historical 4080.080 s wall versus 1639.131 s active included OS sleep; none of that gap is credited to code changes.

### Pre-handoff scope inspection

Inspected the full owned diff from baseline c34f63903722295ae0e130aa3d279876d20420bb, including new source/test/spec files. Kept one artifact admission path, deleted implicit PublishAsync and checkout-relative app-bundle selection, and made artifact verification mandatory in RunEngine. Reused the existing bundle identity and MSBuild project-reference copying rather than adding another manifest or output cache. Native fixture exclusions and browser worker calibration remain unchanged. All owned changes serve artifact reuse, independent jobs, sleep investigation or their verification/docs; no unrelated gameplay/backlog changes. Final independent review and coordinator post-review scope analysis remain required.

### Bounded repair — patched verification toolchain (planning Scoped Pass)

The first E9 invocation could not build its file-based tool: NuGet audit raised NU1903 for Microsoft.DotNet.ILCompiler 10.0.13; raw `/private/tmp/tim-test-loop-maintainability.log`. Microsoft's [GHSA-p3jp-9v83-76w9 advisory](https://github.com/advisories/GHSA-p3jp-9v83-76w9) identifies .NET 10 ILCompiler and Mono browser runtimes below 10.0.14 as affected, with 10.0.14 patched. This is a toolchain dependency defect blocking required verification, not permission to suppress audit, downgrade warnings, skip restore or relax compiler provenance.

Necessary repair closure, independently rechecked by /root/test_loop_planning_auditor and approved as planning Scoped Pass before implementation:
- `.github/workflows/pages.yml`: select SDK **10.0.403**, install workload set **10.0.403** (manifest wasm-tools **10.0.114/10.0.100**); preserve all static/production compilation, audit, upload and deployment checks.
- `global.json`: replace the obsolete minimum/version commentary with SDK **10.0.403**; retain the existing latestFeature policy so only supported newer installed SDKs can roll forward, while CI and documented local proof use the same exact SDK.
- `docs/delivery-workflow.md`: update the supported local/CI provisioning command to the patched SDK/workload; Node remains **26.12.1**, five compiler captures, warnings-as-errors, full-round and hook requirements unchanged.
- `CuriousContraptions.tests/CSharpStaticGateTests.cs`: replace the obsolete 10.0.12/10.0.13 external ref-pack generator identities with the six exact **10.0.14** package-relative paths and independently measured SHA-256 identities from the restored official package. Preserve positive/tampering/unknown-version/unrelated-project controls; explicitly refuse the retired versions. These third-party integrity pins are permitted, unlike simulation-output hashes.
- Native spec records the new provisioning/evidence. No primary-checkout files change. No new compatibility version branch or old-version allowance remains. Historic Anvil README results stay historical.
- Implementer runs E9 through the primary read-only tool source with the isolated target root using `PATH=/private/tmp/tim-dotnet-10.0.403:/private/tmp/node-v26.12.1-darwin-arm64/bin:$PATH DOTNET_ROOT=/private/tmp/tim-dotnet-10.0.403`. Existing installation was inspected: SDK10.0.403, workload10.0.403, wasm-tools10.0.114/10.0.100, reference pack10.0.14. Fresh restore must validate official package provenance and audit without bypass.
- Coordinator then owns fresh five normal compiler captures and the sole final full round on this repaired toolchain; earlier 10.0.13 checks are diagnostic historical evidence, not final patched-runtime qualification. Normal hooks and independent review remain mandatory; CI/browser qualification stays unverified.

### Deterministic property coverage and repair

These tests already express properties over a committed deterministic generated corpus, not unseeded fuzzing: normalized quaternions from the existing finite RNG draws, fixed collider dimensions, explicit box/cylinder partner choices and committed seeds. Parallelism never shares the generator or changes draw order. No property framework or corpus replacement is needed to obtain reproducibility; shrinking is not claimed.

Rim property: each of60 poses per partner walks53 offsets from **+1 mm separation through exact touch to -1.6 mm overlap**, at unchanged50 µm steps, inside the existing **5 mm** contact-generation margin. It checks continuous gap and no excessive positive gap while overlapping at the unchanged0.5 mm tolerance. Inspection found that an empty contact list silently continued and could make the entire property vacuous. Repair: require a contact in this existing valid domain and report missing contacts; the margin/offset geometry independently justifies that expectation. This adds no tighter numerical tolerance. Existing analytic face/cap/rim controls and core-boundary recovery cases retain their exact role.

Overlap property:200 poses per partner, seeds402/403, independently bounded touching placement, depths **0.3,2 and5 mm**, existing missing-contact refusal and unchanged0.5 mm excess-depth limit. Gate property:200 cases in each of six classes for each material set, original seed-derived per-class streams, serial box control and complete three-second per-tick observations. All counts, seeds, boundaries, tolerances and controls remain. Failures now carry seed, case/pose index and geometry; standard runtime/assertion exceptions inside pose jobs become ordered failure records instead of context-free aggregates. Unexpected exceptions still fail the test. Gate exception context records geometry and seeded case identity.


### Current verification state

Current executor environment: `PATH=/private/tmp/tim-dotnet-10.0.403:/private/tmp/node-v26.12.1-darwin-arm64/bin:$PATH DOTNET_ROOT=/private/tmp/tim-dotnet-10.0.403`. SDK10.0.403/workload10.0.403/runtime10.0.14 supersede the historical .402/.13 commands above. Production Web restore passed without audit suppression (`/private/tmp/tim-test-loop-patched-restore.log`). Six external generator pins were measured from the official NuGet10.0.14 package and independently matched the installed SDK pack. The three-project allowlist is unchanged; retired10.0.12/.13, unknown versions, altered bytes and unrelated projects remain refused.

Pre-handoff E9 command (implementer): `dotnet run --file /Users/aidan/dev/personal/tim/tools/anvil/VerifyPolicy.cs -- --maintainability --project-root /Users/aidan/.codex/worktrees/tim-test-loop/tim`; current raw output `/private/tmp/tim-test-loop-maintainability2.log`. Gate **PASS**: both canaries passed, metrics0, coupling0; contract3270/model4/intrusive0. Normal affected rebuilds follow the injected gate configuration. Coordinator's final evidence root is `/private/tmp/tim-test-loop-final.dZtaoS`; no full round has been duplicated.

Latest pre-upgrade affected results: runner **50 passed,0 failed,0 skipped** in6.8908s (`/private/tmp/tim-test-loop-affected-runner3.log`); physics **6 passed,0 failed,0 skipped** in3.8075min (`/private/tmp/tim-test-loop-affected-native.log`). Approximate active case times: dead+rolling91s versus historical144.33s; lively45s versus71.33s; rim box47s versus119.15s; overlap box27s versus53.16s. These are uncontrolled observations, not a calibrated speedup claim. Latest diagnostic/vacuity additions and patched-runtime acceptance require the final matching build/full round. Native build4 additionally exposed CA1031 in newly added diagnostic catches; narrowed them to standard runtime/assertion exceptions without suppression. Unexpected failures still propagate.

Artifact admission, CLI and Preview negative boundaries were tested without Chrome. The final RunEngine source-edit check uses real Preview with an empty native schedule, changes a production source at the end boundary, and asserts IdentityChanged. It does not provision deferred browser tooling or qualify real browser suite execution.

### Implementation handoff — frozen candidate

Candidate: baseline/HEAD `c34f63903722295ae0e130aa3d279876d20420bb` plus the20 owned tracked/new paths reported by `git status --short` in the isolated checkout. No primary source writes or unrelated dirty work were adopted. Retained owner /root/test_loop_implementer, first launch `fork_turns=none`, remains responsible for repairs.

Current patched-runtime proof (all after E9, normal Debug/net10.0 configuration):
- `dotnet build CuriousContraptions.tests --nologo -noAutoResponse`: **0 warnings,0 errors**, raw `/private/tmp/tim-test-loop-native-build5.log`.
- `dotnet build tools/E2eRunner.Tests --nologo -noAutoResponse`: **0 warnings,0 errors**, raw `/private/tmp/tim-test-loop-runner-build6.log`.
- Runner affected command recorded above, same filter: **50 passed,0 failed,0 skipped**,7.5464s, raw `/private/tmp/tim-test-loop-affected-runner4.log`.
- Physics affected command recorded above with `|FullyQualifiedName~ReferencePackPinsReject` appended to its filter: **7 passed,0 failed,0 skipped**,3.8052min, raw `/private/tmp/tim-test-loop-affected-native2.log`. This includes both strengthened rim cases, both overlap cases, both full Gate sweeps and patched provenance positive/negative controls.
- E9 remains **PASS**, metrics0/coupling0, both canaries passed, raw `/private/tmp/tim-test-loop-maintainability2.log`; no production source/configuration changed after that run. Subsequent edits only record evidence and clarify output-directory documentation.
- `git diff --check` passed. Anvil changed-code scan reported no anti-pattern warnings. Every authored write used pre-write validation.

Full owned diff and dependency closure inspected again after property diagnostics and patched toolchain repair. The additional closure fixes the discovered vacuous contact property and vulnerable toolchain; no assertion removals or tolerance changes, no new physics paths, no cache/manifest platform, and no justified further simplification identified. Keeping receipt admission, source applicability and symlink validation as separate small types separates actual boundary obligations. This self-check is not independent approval.

**Remaining:** independent six-layer Auto review and repair convergence; coordinator-owned final compiler logs/full solution round/final E9 and normal hooks; authorized publication. Remote CI artifact/build/deployment and actual Chrome reuse/calibration remain unverified. Required-now implementation evidence is supplied for review; no terminal acceptance or release qualification is claimed.

### Controlled review repair handoff — 2026-11-18

Retained owner/executor: /root/test_loop_implementer, first launch `fork_turns=none`. Baseline remains `c34f63903722295ae0e130aa3d279876d20420bb`; candidate is that baseline plus the21 owned tracked/new paths in the isolated checkout. Source frozen for affected re-review. The following supersedes the initial energy-oracle and ignored-input descriptions above, which remain historical evidence.

- Gate and authored case179 now assert the fixed documented **1.962 mJ/kg per committed tick** local interface ceiling, independently on cylinder classes and serial box controls. All seeds, cases, materials and three-second windows remain; finite initial/per-tick energy and initial-to-first-tick rise are checked. This is neither an observed-control allowance nor a cumulative-conservation claim. All three checks passed without changing numerical expectations or the solver.
- Untracked source enumeration includes ignored paths. Tracked changes are checked before generated exemptions. The small ArtifactGeneratedPaths classifier identifies audited roots, immediate bin/obj under tracked project directories and exact declared generated files. Generated directory roots/ancestors reject links; their untracked internals are opaque, allowing ordinary npm .bin links. Source/unknown paths, exact generated files and artifact trees still reject links. Local dependency-cache integrity is not claimed.
- Producer and consumer refusal tests independently exercise run/attempt 0, -1 and nonnumeric values with otherwise valid source, receipt and bundle. Source tests cover local/global ignore rules, .cs/.ts/resource/build inputs, generated output reuse, fake generated-looking locations, root/source links and tracked changes beneath generated roots.
- Real built-checkout inventory: `git ls-files --others --directory`, raw `/private/tmp/tim-test-loop-repair-generated-inventory.log`. Existing generated directories match tracked-project bin/obj and audited root caches; all emitted JS and elements bundle paths match RuntimeTypeScript.targets/BundleElements outputs. The root game project uses explicit compile inputs; SDK project immediate bin/obj exclusions apply. CI's exact web AppBundle output is admitted; arbitrary AppBundle paths are refused. No browser or local publish was executed.

#### Verification

Executor /root/test_loop_implementer used SDK10.0.403/runtime10.0.14 and pinned Node26.12.1 with the current environment above, normal Debug/net10.0 outputs:
- `dotnet build tools/E2eRunner.Tests/E2eRunner.Tests.csproj --no-restore`: zero warnings/errors,3.82s; raw `/private/tmp/tim-test-loop-repair-runner-build.log`.
- `dotnet build CuriousContraptions.tests/CuriousContraptions.tests.csproj --no-restore`: zero warnings/errors,21.45s; raw `/private/tmp/tim-test-loop-repair-native-build.log`.
- `dotnet test tools/E2eRunner.Tests/E2eRunner.Tests.csproj --no-build --no-restore --filter 'FullyQualifiedName~ArtifactTests|FullyQualifiedName~PreviewBuildTests|FullyQualifiedName~CommandLineTests|FullyQualifiedName~SchedulerTests' --logger 'console;verbosity=normal'`: **70 passed,0 failed,0 skipped**,12.5688s; raw `/private/tmp/tim-test-loop-repair-runner-tests.log`.
- `dotnet test CuriousContraptions.tests/CuriousContraptions.tests.csproj --no-build --no-restore --filter 'FullyQualifiedName~FreshSeedDropsObeyThePassiveContactEnergyCeiling|FullyQualifiedName~FreshSeedDropsWithRestitutionObeyThePassiveContactEnergyCeiling|FullyQualifiedName~DeadCylinderBoxRimRegressionObeysThePassiveContactEnergyCeiling' --logger 'console;verbosity=normal'`: **3 passed,0 failed,0 skipped**,2.5195min total. Reported test active durations: dead/rolling1m32s,lively45s,authored case17910s. Raw `/private/tmp/tim-test-loop-repair-energy-tests.log`. Historical144.33s/71.33s comparison remains observational, not a controlled benchmark.

Relevant code, generated inputs, dependencies, configuration or runtime/toolchain changes invalidate affected build/test reuse. These tests preceded E9 and ran against matching normal builds. E9 then overwrote outputs with injected analyzer configuration; no later --no-build test reused those outputs. Coordinator /root owns final E9 followed by five normal compiler captures and the sole full solution round, plus normal hooks. No duplicate full round was run.

Full owned baseline diff, new files and affected dependency closure were inspected for scope/simplicity. The additional classifier isolates the one generated-path rule from source comparison; no fallback, duplicate admission path, solver change or unrelated cleanup was added. No further materially justified simplification was identified. This inspection is not independent approval. Every authored repair write passed Anvil validation.

Remaining: affected independent re-review and coordinator final E9/compiler/full/hook proof before acceptance/publication. Remote build/artifact/deployment, browser reuse and release qualification remain unverified. Ownership remains with the same implementer for repairs.

Pre-handoff E9: `dotnet run --file /Users/aidan/dev/personal/tim/tools/anvil/VerifyPolicy.cs -- --maintainability --project-root /Users/aidan/.codex/worktrees/tim-test-loop/tim` passed on the frozen repair candidate: metrics0,coupling0; contract3270,model4,intrusive0; both metric and coupling canaries PASS. Raw `/private/tmp/tim-test-loop-repair-maintainability.log`. No gate suppression, exclusion or threshold change.



### Native before-change baseline and guard repair
Coordinator's full round completed on the pre-optimization candidate: /private/tmp/tim-test-loop-final.dZtaoS/full-tests.log and results/*.trx. Native **5660 passed,1 failed,0 skipped**,5661total; reported23m59s, TRX wall1443.56s. Other projects: runner242, bundle244, coverage77, worker guard17, all passed/0skipped. Aggregate6240passed/1failed/0skipped. This is failed verification and useful timing evidence, never an acceptance Pass.
The failure is CatchAllsExistOnlyAtThePinnedBoundariesWithTheirExactCounts: three diagnostic lambda filters include SystemException, which the existing guard correctly classifies as general. The retained owner repairs only those catches using specific exceptions/context; no new catch pins, allowlists, analyzer changes or guard weakening. Unexpected exceptions continue to fail. Add the catch policy and classifier controls to affected proof; independently re-review.
Baseline compiler captures all completed; prior final E9 passed both canaries,0metrics/0coupling. Source/project changes now invalidate affected captures. No OS sleep/wake events appeared in pmset for09:11–09:35.
Same-case baseline seconds: producer8738=54.197,producer8739=59.268; energydead/rolling96.980,lively45.720,authored10.449; rimbox47.202,overlapbox27.552. The complete TRX is authoritative for remaining comparisons. The approved two-project optimization and handler repair start only after this run ended; no source/build mutation contaminated it.

### Optimized native checkpoint and approved source-lookup repair
The two-project optimization plus specific diagnostic catches passed60affected tests,0failed/0skipped in2.4070min. Matching build /private/tmp/tim-test-loop-opt-native-build2.log and binlog /private/tmp/tim-test-loop-opt-native2.binlog; comparison log /private/tmp/tim-test-loop-opt-tests.log, TRX /private/tmp/tim-test-loop-opt-results. Both Core/tests actual compiler invocations use /optimize+ while definition, portable-debug, warnings-as-errors, analyzer and analyzer-config lists match the baseline. The first XML placement attempt refused before compilation (MSB4067), corrected without changing the plan; its raw log is retained.
Same-case baseline→optimized seconds: dead/rolling96.980→12.999;lively45.720→6.117;authored10.449→1.584;rimbox47.202→4.895;rimcylinder6.305→0.650;overlapbox27.552→2.124;overlapcylinder6.032→0.578;36lesson/cadence cases72.546→13.257;saved-counter/reset16.055→5.166. Producer8738=54.197→44.724 and8739=59.268→46.324: their91.048s residual dominates. This checkpoint is scoped evidence, not a final full-suite claim.
Planning auditor /root/test_loop_planning_auditor granted Scoped Pass for this next bounded repair before implementation:
- Only replace presentation capacity's repeated Sources.ToArray().Single scan in engine/gpu/WorkshopSceneCompiler.cs with the existing typed physics.Layout.GetElectricalSourceSlot(source) indexing the same physics.Electrical.Sources table.
- Preserve default-source→zero, every track identity/order, source ownership and capacity. No new dictionary/cache/API, no SourceEnable change, no element-specific path.
- Physical admission rejects duplicate/invalid identities before layout construction. Layout maps the same admitted source array; presentation validation rejects unknown source-node declarations; source and track compilation resolve the same owner-scoped identity.
- Add semantic compiled-interface proof for unequal capacities and reversed owner ordering, asserting every relevant track's owner/source/capacity, plus a non-source zero control. Preserve/reuse applicable missing-reference and duplicate-source-ID refusals. Never assert private slot ordinals.
- Rebuild the affected closure and compare the same8738/8739cases. Then final E9, matching compiler captures, full round, independent affected reviews and renewed scope assessment. No exploratory full-round repeat.

### Residual validation and managed dependency optimization — planning Scoped Pass
Capacity-only checkpoint:50passed/0failed/0skipped,1.3442min; /private/tmp/tim-test-loop-capacity-native-build.log, capacity-tests.log, capacity-native.binlog and capacity-results TRX (same prefix). Producer8738=36.689s,8739=40.385s; the remaining77.074s justifies tracing the next repeated scan. WorkshopCompiledScene.InvalidSupplyBinding repeats Sources.ToArray().Any for every supply/charge track during scene creation and decoded reconstruction.
The saved-counter/reset path also crosses still-unoptimized FlatBuffers builders/accessors and Abstractions read/value collections for every one of four600-tick intervals. Core-only optimization leaves this managed dependency closure inconsistent.
Independent planner /root/test_loop_planning_auditor granted Scoped Pass for:
- One unconditional Optimize=true property in root Directory.Build.props, removing the superseded Core/test project overrides. All importing managed projects/file-based tools are now affected; inspect their compiler invocations and refresh all five final normal captures. Preserve configuration, DEBUG/PLAYTEST, portable symbols, analyzers/audit/warnings and vendor bytes/exemptions. Reduced optimized-local debugger visibility applies across this managed closure; there is no alternate runtime algorithm.
- Replace only the electrical-source copy/scan in InvalidSupplyBinding with existing ElectricalSourceSlots.TryGetValue. Missing IDs remain invalid and reach the existing ArgumentException/refusal boundary; admitted rows must match exact owner and source capacity. Keep source-driver consistency, non-source zero capacity and socket validation unchanged. No new map/API, SourceEnable change or fallback.
- Add unknown-source, foreign-owner source and wrong-capacity controls, exercising compiled construction and applicable wire refusal coverage, with semantic preservation of installed scene/epoch/tick/revision/body state and normal continuation where the existing harness supports it. Retain positive unequal-capacity/reordered-owner/zero controls and unknown-declaration/duplicate-ID refusals.
- Rebuild once for this closure, compare the same native energy/geometry/lesson/Reset/producer cases and boundary checks. No pre-change rerun or exploratory full round. Final E9/full compiler/full suite and affected independent review follow the completed candidate.

### Native throughput implementation handoff — 2026-11-18

Current retained implementation owner and command executor: /root/test_loop_implementer, initial `fork_turns=none`. Original baseline `c34f63903722295ae0e130aa3d279876d20420bb` remains unchanged. Frozen candidate includes26 owned tracked/new paths in the isolated checkout; the temporary Core/test project Optimize overrides were removed entirely. The final single setting lives in Directory.Build.props. Earlier21-path handoff and two-project checkpoint are historical; this section is the current handoff.

**What changed and why:** all managed builds now optimize the existing code while retaining Debug/PLAYTEST symbols, diagnostics, assertions and compiler/static gates. Source-table capacity compilation and compiled presentation validation reuse the existing immutable typed source-ID index instead of repeatedly copying/scanning the source table per binding. Unknown sources use TryGetValue and retain the existing ArgumentException → WireFaultReason.OutOfRange refusal. Source ownership, exact declared capacity, source-driver/non-source checks, track identities/order and admission validation remain. No SourceEnable/API/cache change, no vendor source changes, no alternate simulation path or global test parallelism. The three diagnostic lambda catches now name specific exception types; the catch-all guard and its pins were not changed.

**Semantic coverage:** new decoded compiled-interface tests cover unequal900/2700J capacities, both authored owner orderings, all15 source tracks per battery and a non-source zero control. Missing source-node declarations refuse. Existing ElectricalSupplyTests retain duplicate-source-ID and unknown-route refusal. Three new malformed compiled track cases independently reject unknown source, another owner's source with equal capacity, and wrong capacity. Constructor ArgumentException and command/response wire OutOfRange are checked. Installed scene, epoch/tick/revision and decoded state remain unchanged; subsequent Run/two ticks/Reset succeed. No private slot ordinals, output hashes, fitted tolerance or reduced corpus/window is used.

#### Verification ownership and matching evidence

Environment for all commands remains SDK10.0.403/runtime10.0.14, Node26.12.1 via the recorded PATH/DOTNET_ROOT. Normal configuration is Debug/net10.0. Implementer owns the following scoped builds/checks and pre-handoff E9; /root owns final E9, five normal compiler captures, sole final full round, normal hooks and affected independent approval.
- Capacity-only build: `dotnet build CuriousContraptions.tests/CuriousContraptions.tests.csproj --no-restore -bl:/private/tmp/tim-test-loop-capacity-native.binlog`, zero warnings/errors26.50s, raw `/private/tmp/tim-test-loop-capacity-native-build.log`.
- Capacity-only tests: `dotnet test CuriousContraptions.tests/CuriousContraptions.tests.csproj --no-build --no-restore --filter 'FullyQualifiedName~ComposableAnimationMappingTests|FullyQualifiedName~ElectricalSupplyTests|FullyQualifiedName~FormerProducerLimitAdmitsWithoutAllocatingHypotheticalEvents' --logger 'console;verbosity=normal' --logger trx --results-directory /private/tmp/tim-test-loop-capacity-results`. **50passed/0failed/0skipped**,1.3442min; raw `/private/tmp/tim-test-loop-capacity-tests.log`, TRX `/private/tmp/tim-test-loop-capacity-results/_aidan-runlevelfive_2026-11-18_09_43_05_net10.0.trx`.
- Combined final closure build: same normal native project build with `-bl:/private/tmp/tim-test-loop-residual-native.binlog`, zero warnings/errors27.24s; raw `/private/tmp/tim-test-loop-residual-native-build.log`. A final explicit epoch/revision assertion was added before test execution because the reused semantic-state helper omits those fields; its necessary incremental rebuild passed zero warnings/errors12.67s, raw `/private/tmp/tim-test-loop-residual-native-build2.log`, binlog `/private/tmp/tim-test-loop-residual-native2.binlog`. No affected test was repeated for that addition.
- Actual closure compiler commands show /optimize+ for Core, root host, Abstractions, Animation, FlatBuffers and tests. Read-only comparison of51 matching baseline/current commands found identical define, debug, warnings-as-errors, analyzer and analyzer-config argument lists, including DEBUG/PLAYTEST where previously present and /debug:portable. The vendor's existing analyzer exemption and byte-identical source were unchanged.
- Final affected command: `dotnet test CuriousContraptions.tests/CuriousContraptions.tests.csproj --no-build --no-restore --filter 'FullyQualifiedName~FreshSeedDropsObeyThePassiveContactEnergyCeiling|FullyQualifiedName~FreshSeedDropsWithRestitutionObeyThePassiveContactEnergyCeiling|FullyQualifiedName~DeadCylinderBoxRimRegressionObeysThePassiveContactEnergyCeiling|FullyQualifiedName~ARimAimedWalkThroughTouchKeepsTheGapContinuousAndNeverPositiveWhileOverlapping|FullyQualifiedName~OverlapDepthNeverExceedsTheCertifiedBound|FullyQualifiedName~FormerProducerLimitAdmitsWithoutAllocatingHypotheticalEvents|FullyQualifiedName~CatchAllsExistOnlyAtThePinnedBoundariesWithTheirExactCounts|FullyQualifiedName~TypedRethrowingAndHandledCatchesPass|FullyQualifiedName~TheCatchRulesFlagSwallowsTheAnalyzersSkip|FullyQualifiedName~AuthoredLessonSolvesAndOneMissingCausePreventsTheGoal|FullyQualifiedName~ResetIsExactAndASavedCounterReplaysIdentically|FullyQualifiedName~ComposableAnimationMappingTests|FullyQualifiedName~ElectricalSupplyTests|FullyQualifiedName~CompiledSceneBoundaryTests' --logger 'console;verbosity=normal' --logger trx --results-directory /private/tmp/tim-test-loop-residual-results`. **151passed/0failed/0skipped**,55.2684s, raw `/private/tmp/tim-test-loop-residual-tests.log`, TRX `/private/tmp/tim-test-loop-residual-results/_aidan-runlevelfive_2026-11-18_09_47_31_net10.0.trx`. CSHARP_STATIC_GATE_BINLOGS pointed to the baseline final-proof root only to enumerate unchanged source membership for the catch guard; current closure binlogs establish the changed build. Final compiler-input qualification remains coordinator-owned.

Same-case active durations (seconds), from original TRX:
| Case | Unoptimized full baseline | Two-project optimization | Capacity-only checkpoint | Final shared optimization/indexes |
| --- | ---: | ---: | ---: | ---: |
|8738 producers|54.197|44.724|36.689|1.257|
|8739 producers|59.268|46.324|40.385|2.305|
|Dead/rolling energy|96.980|12.999|—|12.265|
|Restitution energy|45.720|6.117|—|5.970|
|Authored case179|10.449|1.584|—|1.492|
|Box rim walk|47.202|4.895|—|4.832|
|Cylinder rim walk|6.305|0.650|—|0.655|
|Box overlap|27.552|2.124|—|2.142|
|Cylinder overlap|6.032|0.578|—|0.584|
|36 lesson/cadence cases|72.546|13.257|—|11.518|
|Saved counter/Reset|16.055|5.166|—|5.900|

The same60-case checkpoint totals442.308s→51.683s (~8.56×); its baseline contains the known catch-guard failure and is not an acceptance Pass. Producer improvement is25.72–43.11×. These are observed scoped comparisons with preserved cases, not a claim of tenfold full-suite speedup. Root optimization did not improve the saved-counter residual in this observation; no speculative further change was made. Correction to initial read-only investigation: fixture cold startup is once, but WorkshopIntegrationHarness constructor rereads/validates/reinstalls catalogue data per harness. That remains a possible later profiling target, not an added cache or a current acceptance obligation.

**Applicability and risks:** code, dependencies, generated inputs, compiler configuration/runtime or observation contracts invalidate affected proof. The above tests used matching normal outputs before E9. E9 replaces outputs with injected analyzer builds; coordinator must rebuild normally before the final round, and the implementer does not redundantly rebuild them. Optimization can reduce debugger local-variable visibility while preserving symbols and diagnostics. No final full-suite speedup, remote artifact/deployment, browser or release qualification is claimed.

Full owned baseline diff including untracked deliverables and the justified repair closure was inspected for scope/simplicity. One shared compiler setting replaces per-project duplication; both lookups reuse the admitted typed map with no extra cache/state/interface. The necessary behavioral tests discriminate owner/capacity/unknown-source errors and atomic refusal. The root compiler change broadens managed-build impact, which is why the coordinator full round and matching compiler captures remain required. No further materially justified simplification was identified. This is not independent approval. All authored writes used Anvil. Same owner retains repair responsibility.

Pre-handoff E9: `dotnet run --file /Users/aidan/dev/personal/tim/tools/anvil/VerifyPolicy.cs -- --maintainability --project-root /Users/aidan/.codex/worktrees/tim-test-loop/tim` PASS on the frozen native candidate, both metric/coupling canaries PASS; metrics0,coupling0; contract3271,model4,intrusive0. Raw `/private/tmp/tim-test-loop-native-final-maintainability.log`. No exclusions, threshold changes, suppressions or project reclassification. Remaining: coordinator final verification, affected independent re-review and acceptance/publication decision.


## Auto Run Result

Owner's clarified usability target: **under five minutes for the full native suite**. Achieved on this host: runner-reported native duration **23m59s → 4m8s**, approximately5.8× faster. The final native suite has5667tests (six additional semantic cases), all passed/0failed/0skipped. The complete five-project round is **6247passed/0failed/0skipped**: native5667,runner242,bundle244,coverage77,worker guard17. This is a local measurement, not a cross-device/browser/release claim. No further optimization expansion is needed for the owner's target.

Final coordinator proof: /private/tmp/tim-native-fast-final.dB2jCc. Normal solution/playtest/web/file-tool builds and their five named binary logs completed after final E9, using SDK10.0.403/workload10.0.403/Node26.12.1. Full-tests.log and results/*.trx contain the passing round. Required compiler-input tests replayed the binary logs and passed; no warning/audit bypass. Coordinator also independently inspected embedded compiler command strings across the five logs:64solution+64playtest+49web+1check-staged+1verify-policy outputs all show /optimize+, with no observed differences in the compared define/debug/warnaserror/analyzer/analyzerconfig tokens versus the baseline and no missing/extra output keys. That extra string inspection is corroborating, not a substitute for the structured compiler-input tests.
Final maintainability.log: both canaries PASS, metrics0/coupling0, contract3271/model4/intrusive0. Relevant code/config inputs remained frozen through proof.

Independent affected native re-reviews by /root/test_loop_blind, /root/test_loop_verification, /root/test_loop_forward and /root/test_loop_engineering found no remaining defects; prior unchanged edge/artifact and other initial six-layer findings remain resolved. All confirmed defects were repaired: independent energy acceptance, missing-contact refusal, ignore-independent artifact freshness, provenance coverage and specific diagnostic catches. No finding deferred or rejected.

**Final diff scope — Scoped Pass:** informed reviewer /root/test_loop_engineering sequentially inspected all26owned files, six new, from original baseline c34f63903722295ae0e130aa3d279876d20420bb after affected reviews completed. Root optimization and both source lookups address measured costs. Existing typed indexes replace repeated copies/scans; no new cache/platform/API or alternate algorithm. Prior authorized artifact reuse/Preview/sleep work and patched toolchain retain their necessity. No unrelated cleanup, duplicate ledger or materially beneficial further simplification identified. Required E9 receipt inspected. This scope verdict does not itself replace full/compiler/hook/exact-SHA evidence.

Implementation commit **fc3891e39a46266e70c3f253650f5753338f0b6b** passed mandatory normal hooks: five fresh staged compiler builds, zero policy errors/warnings, **134passed/0failed/0skipped**; raw /private/tmp/tim-native-fast-final.dB2jCc/commit.log. Independent /root/test_loop_engineering granted **SnapshotApproval: Pass** on that exact SHA after inspecting the committed candidate and actual receipts. The worktree was clean.
This final status/receipt update changes only this spec and the scoped TODO entry; implementation and verification inputs remain unchanged. Its recording commit still runs normal hooks and receives exact-identity confirmation before publication. No new runtime test round or E9 run is warranted for accurate receipt-only additions. Publish the approved branch without waiting for CI. Remote build/artifact/deployment and actual Chrome qualification remain unverified. The owner's under-five-minute native target is met; this scoped task does not advance the gameplay backlog.
