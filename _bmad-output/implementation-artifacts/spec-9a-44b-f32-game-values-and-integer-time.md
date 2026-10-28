---
title: '9A.44B: f32 game values and integer game time'
type: 'refactor'
created: '2026-10-29'
status: 'in-review'
baseline_revision: '6125040efa3becf31c6a5957f35571735f04972c'
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '{project-root}/AGENTS.md'
  - '{project-root}/docs/testing-guidance.md'
  - '{project-root}/docs/shared-clock-cadence.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-9A-context.md'
warnings: [oversized]
deferred: []
---

<intent-contract>

## Intent

**Problem:** Canonical lanes are f32, but authoring helpers, committed-read validation, presentation/animation and clock derivation still calculate or retain game values/time in double. This leaves the required numeric migration incomplete and makes local animation progression depend on absolute floating timestamps.

**Approach:** Complete Story9A.44B with f32 game calculations and integer time plus bounded f32 phase, through current producers and consumers. Retain the existing clock authority, gameplay, timing features and lifecycle. Convert the affected glue to strict TypeScript and add a small architecture guard against reintroducing double game calculations.

## Boundaries & Constraints

**Always:** Preserve all existing elements, authored content, typed identities, canonical f32 save/wire fields, finite/range/enum validation and atomic refusal. Preserve Run/Pause/Step/Resume/Reset, supported Save/Load, world-versus-autonomous animation, deliberate timer/delay/cadence boundaries and causal event order. Keep current defaults120Hz physics/480Hz substeps and60Hz animation; retain admitted cadence profiles. Only named genuine external adapters may consume double, validating/converting at the boundary; integer identities and external performance observations keep their correct representations. Include implicit wider arithmetic, not just stored fields. Retain geometric demand growth and no arbitrary population caps. Keep source requirement identities PERF-40–45, REQ-09/14 and the shared-clock lifecycle contracts.

**Never:** No double shadow game model, floating absolute game-seconds counter, half precision, fallback, old-format decoder, compatibility overload, alias, migration or version branch. No new clock authority or per-element path. Do not extend the30-second Workshop Run policy to test long-lived clock consumers. No historical trajectory/tick goldens, output hashes/whole-byte identity, arbitrary new scene precision, mutants or browser gate. Do not make the later complete integration harness or remaining repository-wide TypeScript conversion prerequisites. No local Release worker build or web publish; local tests are authoritative.

## I/O & Edge-Case Matrix

| Scenario | Input/state | Expected behavior | Error handling |
|---|---|---|---|
| Canonical values | Authored f32 configuration, construction, command/save round trips and committed/presentation reads | Intended values remain f32 field by field; same puzzle outcomes and meaningful physical bounds | Existing typed refusals at external boundaries |
| Time conversion | Integral master time, pulse/commit ordinals and valid event phase, including normalization boundaries | Correct ordinal/local phase without absolute floating-seconds arithmetic; adjacent local samples remain distinct | Nonfinite/out-of-range phase and arithmetic overflow refuse before mutation |
| Long-lived consumers | Direct timestamps through one hour and nearby pulse/fractional boundaries | Real looping and short animation samples progress/complete like their independently known local phase near origin; exact pulse eligibility retained | No sleeps or hour-long physics stepping |
| World lifecycle | Running, Pause with advancing master, Step, Resume, Reset and stale generations | World animation freezes on Pause while autonomous UI progresses; Step advances one configured tick; Reset restores authored state and retires old world effects | Stale/foreign/overflowing updates do not change admitted state |
| Clock derivation and ring | Narrow/wide/late exchanges, startup outlier control, slew/rederive and signed correction | One integral nanosecond representation carries correction and reply time; existing outcome/epoch semantics preserved | Malformed/nonfinite external input and overflow reject before counters/pending state/ring publication |
| Motion and cues | Production motion pieces and committed event/timer observations | Supported/dragged/rotating motion remains plausible; correct cue ownership/order and declared before/due/after behavior | Existing physical/admission boundaries remain explicit |
| Numeric guard | Explicit or inferred double game arithmetic; genuine external adapter controls | Guard rejects prohibited calculation at its source and admits float/integer logic and narrowly named adapters | Diagnostic identifies rule/location; no mutated checkout |
| Shipped glue | Fresh compile and node harnesses exercising converted modules | Strict pinned TypeScript emits the runtime JS used by worker/client and harnesses | Unknown external values reject; no any escape or hand-written changed JS |

</intent-contract>

## Code Map

- Authority: [Story9A.44B](../planning-artifacts/epics.md#story-9a-44b), [numeric scope](../../docs/gpu-f32-physics.md#precision-status), [game fidelity](../../docs/testing-guidance.md), [shared clock](../../docs/shared-clock-cadence.md). Read affected PERF-40–45 rows in requirements.md:393–418, not the full requirements/history. PartA removed half precision; its old golden/mutant/Release-build instructions are superseded. Preserve its current f32 lanes and unsupported-save refusal.
- `engine/gpu/WorkshopCadence.cs`: reuse PulseOrdinal, WorkshopPulse.Due/Deadline and WorkshopWorldProjection.DueTick. Master nanoseconds and rational pulse arithmetic already own scheduling. `engine/gpu/PhysicalPhase.cs` defines the sole4096-unit phase rule; move that rule with shared time into Abstractions, delete its old implementation.
- New `CuriousContraptions.Abstractions/PhysicalTime.cs`, moved `CuriousContraptions.Abstractions/PhysicalPhase.cs` and `CuriousContraptions.Animation/CuriousContraptions.Animation.csproj`: shared timestamp belongs in Abstractions; Animation currently has no project references and explicitly links its source files. Add the Abstractions dependency, never a dependency on Simulation/engine.
- `engine/gpu/WorkshopConstruction.cs:71`: WorkshopInput helpers and Position use double; change callers/test builders together. `WorkshopPhysicsCompiler.cs:44`, `engine/TubeMouthSnap.cs:124`, `OrientationSensorDeclaration.cs:12`, `CanonicalBody.cs`, `PhysicsDeclarations.cs`: remove widening, double trigonometry and speed constants. Preserve valid authoring ranges.
- `engine/gpu/PhysicsElectricalRead.cs`, `PhysicsGpuAbi.Electrical.cs`, `PhysicsContactWorkRead.cs`: validation residuals/credit/debit are game calculations. Convert the single algorithm with justified local f32 arithmetic bounds; do not retain a wider verification path.
- `engine/gpu/PhysicsMotionRead.cs:148`: fractional double ordinal, elapsed/drag polynomial, rotation, center/origin carries and normalization. Subtract integer anchors before bounded f32 evaluation. `WorkshopPoseHistory.cs`: PresentationSimulationTime(double Seconds), retained time and sampling arithmetic. Update its actual consumers in `engine/MachineWorld.Gpu.cs:238,253`, `MachineWorld.cs:17`, `WorkshopPresentationWire.cs`.
- `engine/presentation/AnimationBatch.cs`, `AnimationValues.cs`, `AnimationFollowDefinition.cs`, `AnimationImpulseDefinition.cs`, `AnimationOscillationDefinition.cs`, `AnimationTimerSegment.cs`: replace absolute double Started/Admitted/time arguments and double formulas. Remove Number widening and double Narrow, update current callers rather than keeping overloads. Preserve AnimationClock.Presentation/Simulation domains.
- `engine/gpu/WorkshopDeclaredPresentation.cs`: Cues, EventSeconds, Advance and tick-seconds fields. `WorkshopAnimationTracks.cs`: master/presentation seconds. `CuriousContraptions.Animation.Worker/Program.cs`: direct integer schedule/capture/event projection. Keep event ownership, exact declared boundaries and animation property semantics.
- `ui/Workshop.cs:767`: displayed game time. `ui/WorkshopPuzzle.cs:54–83`: validate/convert Godot slider input at ChangePrecision, pass float inward through ChangePrecisionAsync, calculate before external output widening. `ui/WorkshopGuidance.cs:301` and `ui/WorkshopAnimation.cs:69`: ApplyUiBindings(delta) only validates an unused value; keep boundary validation and remove the unnecessary internal parameter. Inspect callbacks in `BrowserWorkshopClient.cs` and `BrowserWorkshopClient.Ui.cs` for the same boundary distinction.
- `engine/gpu/WorkshopClockDerivation.cs`: signed correction, exchange stamps, brackets, pending correction and midpoint calculations. Replace infinity sentinels with explicit interval state. `CuriousContraptions.Simulation/Program.Schedule.cs`, `Program.cs`, `wwwroot/worker.ts`: update actual interop/callers atomically.
- Convert four runtime sources before functional glue changes: `CuriousContraptions.Animation.Worker/wwwroot/worker.js`, `CuriousContraptions.Simulation/wwwroot/clock-master.js`, `CuriousContraptions.web/wwwroot/clock-ring.js`, `native-clock.js`. Current clock ring has TWO f64 lanes: header correction at24–31 and Simulation reply timestamp at slotBase+ReplyOffset. Replace both and remove toBits/fromBits; browser/Animation reply stamps are already integral.
- Move affected `tools/workshop-clock-ring.test.mjs`, `workshop-animation-clock.test.mjs`, `workshop-animation-output.test.mjs`, `workshop-client-lifecycle.test.mjs` into the existing strict `tools/runtime` flow. Update `tools/E2eRunner.Tests/NodeHarnessTests.cs` registrations, current TS callers `workshop-observation.test.ts`, `event-worker-harness.ts`, `pose-client-harness.ts`, `worker-contracts.ts`, `external-runtime.d.ts`; remove ambient stand-ins for converted modules.
- Build closure: `tsconfig.runtime.json`, `tools/runtime/RuntimeTypeScript.targets`, `.gitignore`, affected worker/web project deployment entries. Keep the single Abstractions-triggered pinned tsc path; enumerate outputs/copies/FileWrites and WasmExtraFilesToDeploy for fresh builds. WasmMainJSPath still points to generated worker.js. Update source-path assertions in ClockRingScriptTests, EventRingConsumerTests, EventRingProducerTests, WorkshopFormatTests and affected isolation fixtures.
- New `CuriousContraptions.tests/GameFloatGuardTests.cs` owns the named guard and single adapter inventory; register it in the native test project. Guard seam: `CuriousContraptions.tests/CSharpStaticGateTests.cs` already owns RepositoryFiles and matching Csc binlog inputs/references/options; `HalfPrecisionGuardTests.cs` supplies source membership controls. Reuse these rather than a new scanner platform. Do not call Ownership.SourceInventory.Capture: it executes Rebuild including Release contexts.
- Named genuine boundaries: `WorkshopNativeClock.FromMilliseconds`; external Godot clock/slider signatures; JS-import native clock samples; validated JS-export identity transport; `WorkshopTrace` tick-duration observations and `Program.ObservationMilliseconds` performance telemetry; vendored FlatBuffers under its existing integrity protection. A mixed file/class is not an exception. `tools/BundleElements/SceneGeometryExtractor.cs:264`, `PackageValidator.cs:128` and `ui/WorkshopConfiguration.cs:61` already parse/validate float: preserve this control. `tools/WireContract/ContractFixture.cs` and `RevisionTwoOracles.cs` contain synthetic oracle values, not a production authoring exemption; update affected fixtures/callers with changed interfaces.
- Existing test seams: F32LaneRoundTripTests, F32SaveBoundaryTests, F32BehaviourIdentityTests, ConstructionDecodeBoundaryTests, WorkshopReadTests, WorkshopWireTests, PortableAnimationTests, DeclaredPresentationTests, WorkshopAnimationTracksTests, WorkshopActivationAnimationTests, EventRingConsumerTests, ClockTests, ClockGuardTests, WorkshopClockDerivationTests, WorkshopTickLoopTests, ClockRingScriptTests and emitted-output node harnesses. EventRingConsumerTests:337's nine-decimal event-seconds expectation must become meaningful timestamp/cue behavior.
- Coordinator owns TODO, sprint status, current epics order and the prior face-marking terminal receipt. The current worktree starts at published6125040efa3becf31c6a5957f35571735f04972c.

## Tasks & Acceptance

**Execution:**
1. Listed runtime/harness TypeScript sources and build declarations — convert the affected dependency closure to strict TS, preserving behavior and removing superseded hand-written sources/ambient stand-ins.
2. Abstractions/time, cadence callers, motion/history and Animation files — implement one shared ordinal/phase representation and f32 evaluation; update all current APIs/callers/wire readers together.
3. Clock derivation, ring, worker C#/TS and interop — use one signed integral-nanosecond correction/exchange path with checked candidate validation and current lifecycle.
4. Authoring/read validation/UI files — remove remaining double game calculations, preserving ranges, outcomes and refusals.
5. Native tests/project registration and shipped-output harnesses — cover every matrix row using production interfaces; replace obsolete seconds/precision assertions by behavior and record the mapping. Add the small numeric guard and named adapter inventory.
6. `docs/gpu-f32-physics.md`, `docs/shared-clock-cadence.md`, `docs/engine-contracts.md`, `docs/architecture/element-package-isolation.md` B6, affected presentation/clock wire descriptions and current callers — state the sole new representation and actual completed scope. Preserve requirement IDs and deferred browser/performance qualification.

**Acceptance Criteria:**
1. Given every current production path in the inventory and its callers, when authoring, validation, simulation, transport and presentation execute, then game arithmetic/state is f32 and time uses integer identity plus f32 phase; double exists only at exact named external operations with a documented conversion/validation boundary.
2. Given real clock/animation interfaces sampled directly at one hour and nearby local times, when they advance, then expected local progression and intentional pulse/feature boundaries remain observable without cumulative floating-seconds drift. The Workshop Run limit remains unchanged.
3. Given the production node modules after a fresh ordinary build, when node harnesses load emitted output, then changed clock/animation/lifecycle routes and malformed-input controls pass with strict TS and no hand-written changed runtime JS.
4. Given prohibited explicit or implicit double game calculations and permitted boundary/float/integer controls, when the named guard runs, then it identifies the prohibited source locations and admits the controls; a neighboring unapproved member in an adapter file is still rejected. Existing half-precision and literal-ABI guards remain active.
5. Given the frozen candidate, when affected production native/harness checks, the full local round, five Auto layers and normal hooks run, then all required tests pass with zero skips and no unresolved regressions before exact-SHA production publication. Browser/performance qualification remains explicitly pending.

## Spec Change Log

## Review Triage Log

### 2026-10-29 — Review pass1
- Reviewers: f32_blind, f32_edge, f32_gap, f32_intent and f32_forward; all five layers completed. Three ran initially and the remaining two launched as concurrency slots became available. Blind and Forward-Only reported no confirmed defects; Forward-Only's listed borderline cases were explicitly permitted.
- Candidate: `review-auto1.diff` in the rendered workflow directory; baseline `6125040efa3becf31c6a5957f35571735f04972c`. Full local round2: **4,871 passed,0 failed,0 skipped**, main20m19s. All eight matrix rows have executed native/runtime-harness coverage, but review identified the composed lifecycle gap below.
- verdicts: 6 findings — high1, medium3, low0, false2, maybe-false0
- findings:
  - `medium` `patch` Verification Gap: worker schedule-to-world projection is not executed by existing tests — filed evidence establishes that native tests supply PhysicalTime directly and node tests stub managed Service. Same implementer moved projection into internal WorkshopDeclaredPresentation.Advance(schedule,master), used by Service and exercised through real committed reads across12cadence combinations.
  - `high` `patch` Edge Case: Pause/Completed can reverse retained declared animation time — Service extrapolates past committed world time; CommitInstallation anchors to endpoint.Read.Tick; Rebind retains _time and Advance rejects the earlier anchor. The worker-used internal evaluation path now caps running time at the latest admitted committed tick; ordinary transition and uncapped/capped progression tests pass.
  - `medium` `patch` Intent Alignment: composed lifecycle intent and tested surface differ — same missing projection integration coverage as Verification Gap; grouped with it; the new production-seam test covers the composed expectation.
  - `medium` `patch` Intent Alignment: real hour-aged loop is not sampled — PhysicalTimeTests tests helper aging and fresh real loops at a large origin separately. Added a direct real AnimationBatch loop begun at zero and sampled at an hour plus known quarter phases; no long simulation.
  - `false` `reject` Intent Alignment: C# guard versus language-independent numeric enforcement — the intended guard targets double C# game calculations; TypeScript number itself is not prohibited, and integer clock transport uses BigInt. Strict TS is not claimed as f32 enforcement. The described distinction establishes no missing requirement or affected wider game calculation.
  - `false` `reject` Intent Alignment: retained gameplay coverage versus new comprehensive integration proof — intent explicitly excludes making the later complete integration harness a prerequisite. Real existing simulation/save/load/motion/cue cases ran in round2; scoped reuse is the required proof, not a new exhaustive harness.
- Same-owner repairs complete. Independent affected re-review: f32_edge Pass,52 passed/0 failed/0 skipped in9s using the unchanged repair build; f32_gap confirms production-projection and aged-loop gaps closed by reading actual assertions. No new findings. The high lifecycle risk identified by the first pass has been independently re-reviewed; final full-round and publication gates remain pending.

## Design Notes

Use a shared absolute480Hz ordinal plus f32 phase in[-2048,2048),4096 units/ordinal, with no negative time at ordinal zero. Autonomous/session ordinals use a wide integer; conversion to existing physical-event ordinal widths is checked. Move the single PhysicalPhase rule into Abstractions; do not duplicate it. Presentation and simulation domains remain separately supplied to AnimationBatch.Advance; a timestamp is a view of existing authority, not a new clock.

Convert integral master/pulse time using widened integer quotient/remainder. Convert only the bounded remainder into f32 phase; normalize rounded phase boundaries with checked carry. Subtract integer anchors before forming local f32 elapsed values. Reduce whole periods before evaluating long-lived periodic phase; converting a large absolute seconds value to float first is not sufficient. Preserve existing clip/follow/impulse/oscillation semantics, including negative/zero rates where already supported.

The ring's correction and Simulation reply timestamp become signed i64 nanoseconds (BigInt64Array/DataView big-int operations in TS), with one forward representation and no number round-trip for integral interop. Use an explicit byte envelope if the existing JS export binding cannot carry the integer type. Convert raw external clocks before internal subtraction. Current derivation constants retain their meaning:15800ns bound,31600ns maximum bracket,5000ns slew,100000ns rederive/wide-offset threshold. Use widened signed midpoint arithmetic, truncating toward zero, validate representability before any state/publication mutation. This is clock-boundary conversion, not a new scene tolerance.

The one-hour test jumps through real pulse/timestamp and animation APIs: known loop phase at3600s and nearby fractions, plus fresh short follow/impulse/oscillation progress at that origin compared with the same authored local behavior near zero. Use independent expected phases and gameplay-relevant value bounds. Also cover a sufficiently large integer ordinal where conversion to a single float would lose adjacent local progression. No long solver sweep. Pause tests pass an advancing presentation timestamp and frozen world timestamp to the real evaluator, then exercise Resume/Reset ownership.

The guard inspects semantic types/operations so inferred locals, implicit promotion, double Math overloads and widen-calculate-narrow expressions cannot evade a token scan. Inline source controls are fixtures, not mutated trees. Do not ban valid integer/float Math overloads or TS number itself. Keep a single narrow adapter inventory; reject broad file allowlists and guard coverage omissions.

## Verification

Implementer: restore the affected fresh-worktree dependencies, use incremental Debug builds with -noAutoResponse, then affected native tests with --no-build --no-restore -c Debug -f net10.0 and TreatNoTestsAsError=true, plus the existing emitted-output node harness runner. Reuse matching binaries/logs until relevant inputs change. Record actual commands/results and failed attempts. Do not run browser tests or mutants.

Coordinator: one final full local solution round with the five matching compiler captures per docs/delivery-workflow.md#local-test-authority; audit actual matrix test membership. All five Auto review layers, same-owner fixes and affected independent re-review, normal hooks, exact committed-SHA approval and production artifact/deployment checks follow. No local Release worker build or web publish.

## Auto Run Result

Implemented f32 game calculations and one shared integer-ordinal/f32-phase time representation across authoring, reads, motion, animation and presentation. Clock exchanges/corrections use signed integral nanoseconds; affected runtime/harness glue is strict TypeScript. Declared world animation follows admitted committed world time across lifecycle transitions while autonomous UI follows master time.

| Changed files / closure | Result |
|---|---|
| Abstractions/PhysicalTime.cs, PhysicalPhase.cs; Animation.csproj | Shared normalized time, bounded local evaluation, allocation-free periodic reduction and sole phase authority |
| engine/presentation/Animation*.cs | f32 channel arithmetic and separate presentation/simulation timestamp domains |
| engine/gpu/WorkshopDeclaredPresentation.cs, WorkshopAnimationTracks.cs, PhysicsMotionRead.cs, WorkshopPoseHistory.cs, WorkshopPresentationWire.cs; MachineWorld*.cs | Current typed timestamp consumers, cue ownership and committed-time-bounded declared lifecycle |
| WorkshopClockDerivation.cs, ClockDerivationWire.cs; Simulation/Program*.cs; Animation.Worker/Program.cs | Integral derivation/interop and current worker schedule projection |
| WorkshopConstruction.cs, WorkshopPhysicsCompiler.cs, TubeMouthSnap.cs, canonical/read/declaration files; UI and BrowserWorkshopClient boundaries | f32 game calculations with narrow validated external adapters |
| Four converted worker/web runtime files, four converted harnesses, current TypeScript callers, tsconfig.runtime.json and RuntimeTypeScript.targets | One pinned strict build, emitted-output tests and deployment entries; removed handwritten predecessors/stand-ins |
| CuriousContraptions.tests changed fixtures, GameFloatGuardTests.cs, PhysicalTimeTests.cs, AnimationTestTime.cs and project registration | Semantic numeric guard, behavioral migration, real aged-loop/projection lifecycle and invalid-input controls |
| Numeric/clock/engine/isolation docs, current epic context/spec/status/TODO and previous face receipt | Sole current contracts, execution order and original evidence references |

Review: all five Auto layers completed. Six recorded observations were triaged: high1, medium3, false2. Three repair groups addressed the lifecycle defect, projection coverage (two observations sharing that gap) and real aged-loop coverage. No deferred items. The two rejected descriptive distinctions are refuted individually in Review Triage Log. Same-owner repairs received independent affected Pass from f32_edge (52/0/0,9s) and verification-gap closure from f32_gap. No unresolved findings. Patched entries: high1, medium2; the first-pass high risk required follow-up, now completed, so no remaining follow-up recommendation.

Final verification: five `TestResults/f32-full-round3.trx` files report **4,884 passed,0 failed,0 skipped**: main4,393 (20m20s), Bundle206, E2eRunner191, Coverage77 and WorkerAotGuard17. All12 production projection/lifecycle cases, real zero-to-hour loop, four rational pulse cases,11 numeric-guard cases and nine emitted-output harness registrations passed. Matrix evidence covers canonical values/refusals; time conversion; long-lived consumers; world lifecycle; derivation/ring; motion/cues; numeric guard; shipped glue. Earlier failures and affected commands remain in [implementation notes](story-9a44b-implementation-notes.md).

Final full command: `CSHARP_STATIC_GATE_BINLOGS=/private/tmp/tim-f32-round3.5GOnjZ dotnet test CuriousContraptions.slnx -c Debug -f net10.0 --no-build --no-restore --nologo -noAutoResponse --filter "$local_deferred_filter" --logger 'trx;LogFileName=f32-full-round3.trx' -- RunConfiguration.TreatNoTestsAsError=true`, with the filter from `dotnet run --project tools/E2eRunner --no-build -c Debug -- deferred-filter`. Only the single explicit browser deferral applies. Refreshed playtest/solution/web captures passed0 warnings/errors in22.46s/13.18s/8.55s; unchanged file-tool captures reused. Original round1/round2 results and captures remain preserved. Diff whitespace checks passed.

Publication is pending normal hooks, exact committed-SHA SnapshotApproval and production artifact/deployment checks. Per the repository publication gate, status remains in-review until its terminal receipt; no browser/device/performance qualification is claimed. Remaining risk is deferred browser integration qualification, not an unresolved required-now native finding.
