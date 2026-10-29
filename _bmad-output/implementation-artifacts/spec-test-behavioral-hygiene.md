---
title: 'Behavioral test hygiene for the game'
type: 'refactor'
created: '2026-10-29'
status: 'done'
baseline_revision: '2dae218024ab260cc1797d967b13f2a5ea70d01e'
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '{project-root}/AGENTS.md'
  - '{project-root}/docs/testing-guidance.md'
warnings: [oversized]
deferred: []
---


## Implementation evidence — 29 October 2026

The [existing inventory](test-behavioral-hygiene-inventory.md) now maps the replaced methods, independent outcomes and preserved controls/boundaries. It records the supported replacements for impossible private cadence, null-label and authority-max arrangements and distinguishes legitimate integrity/ABI/equality/structural checks.

Production changes are limited to genuine browser transport/save-store injection with the shared production lifecycle, two stable UI node names, and the approved checked AuthorityRevision transition used by every revision advancement. No physics algorithm, alternate test execution path or synthetic startup is added.

Affected checks pass:105palette/simulation/cylinder (including all60palette cases),21animation,16hint/startup,20final replay additions; three compiled Node harnesses65/0/0. First full round:4,986 passed/2 failed/0 skipped, with only stale clock adapter/classification guard entries failing (main20m56). Those entries are repaired and118 focused guard/clock/animation/reuse checks pass/0 failed/0 skipped. Original failed-round logs/TRX remain at `/tmp/tim-hygiene-final.OxsRYg`. Repaired five compiler captures passed at `/tmp/tim-hygiene-repair.iW16Av`; repaired full required suite passed4,988/0/0 (main4,496 in20m55; session26613 exit0), independently checked from all five TRXs at `/tmp/tim-hygiene-repair.iW16Av/results`. SDK10.0.403, workload10.0.401 with wasm-tools manifest10.0.112/10.0.100, Node26.12.1. Full result and matrix audit pass; Auto review, normal hooks and publication remain pending; no browser/remote qualification is claimed.
<intent-contract>

## Intent

**Problem:** Remaining game tests pin output buffers, incidental replay trajectories or private state, producing churn without reliably describing gameplay. The stabilization contract requires their replacement without losing positive, control, boundary or lifecycle coverage.

**Approach:** Replace every remaining simulation/save/event/render output identity assertion and behavioral private reach-in in the game C# tests and node runtime harnesses with independently expected behavior through defined interfaces. Complete the explicit rolling cleanup and empty solver-violation digest removal; reconcile the existing inventory with delivered work.

## Boundaries & Constraints

**Always:** Preserve real production simulation, existing input fixtures, all intended interaction/refusal coverage and authored Reset. Exact IDs, enum values, authored/canonical codec fields, published ABI fields and intentional timer/cadence contracts remain exact. Use game-relevant quantity-specific tolerances selected before observing results and generous finite stall guards. Map every removed oracle to its replacement or applicable existing proof. Retain all named elements. Follow the current local-test authority and normal hooks; five Auto review layers and exact-commit approval precede publication.

**Never:** Solver changes, new elements, a tolerance/test/evidence framework, blanket epsilon, output-hash/whole-buffer acceptance, renamed private accessors, exposed queues solely for tests, compatibility/fallback paths, weakened admission boundaries, copied production oracles, skipped game tests, mutants, browser gates, CI monitoring, local Release worker build or web publish. Do not turn this into an audit of every numeric assertion. Do not redo delivered digest/harness/TypeScript work.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|---|---|---|---|
| Save/wire/ABI | Valid authored state, supported values and records | Decode correct identities, values, ownership and lifecycle semantics | Malformed, legacy, unknown, truncated and out-of-range inputs retain typed atomic refusal |
| Run/Reset/replay | Real constructions and one-cause controls | Each run independently achieves intended interaction; Reset exactly restores authored state; causal event ownership/order preserved | Refused candidates leave committed behavior intact and later valid work succeeds |
| Rolling | Both balls, supported cadences, +X/+Z, declared and zero resistance | Net slowdown, eventual stop and supported stable motion; bounded cumulative backward distance over justified play window | No created energy or suppression of intended slow motion |
| UI/client lifecycle | Real editor/client logic with genuine external transport/clock doubles | Placement, Undo, hints, animation delivery, disposal and lease ownership remain observable through public operations | Stale/foreign/duplicate/busy cases refuse without leaked work or state mutation |
| Structural guards | Empty known violations and forbidden source controls | Direct empty-set and structural detection assertions | Forbidden source still detected; no digest stands in for the rule |

</intent-contract>

## Code Map

Baseline: `2dae218024ab260cc1797d967b13f2a5ea70d01e`. Paths below are relative to this worktree. C# test basenames refer to `CuriousContraptions.tests/`. Anchors are discovery locations, not stable acceptance line numbers.

- `_bmad-output/planning-artifacts/epics.md`, stabilization row `test-behavioral-hygiene` and 28 October cleanup: scope authority. `_bmad-output/implementation-artifacts/test-behavioral-hygiene-inventory.md`: update the existing replacement map, not a parallel ledger.
- Saves/buffers: `WorkshopSaveTests.cs:384,468,544,655`; `WorkshopWireFlatBuffersTests.cs:197,226` (EncodeConstruction alias); `WorkshopWireTests.cs:284,305,446,769,903`; `CanonicalBodyTests.cs:22,65`. Decode all relevant construction/body/receipt values, allocator/settings/IDs/parameters/connections/puzzle fields; adjacent domain assertions often already supply coverage. Keep all malformed/padding controls at documented ABI boundaries.
- Element saves: `WorkshopDominoTests.cs:73`, `WorkshopBowlingTests.cs:84`, `WorkshopPipeLengthTests.cs:235`, `WorkshopPhysicsEngineTests.cs:208`, `LatchSupplyFixtureTests.cs:349`, `PalettePlacementTests.cs:661,668`, `ElectricalXorTests.cs:275`. Preserve parameter cases, canonical authored poses, Reset and actual save/load behavior.
- Other raw identity: `WorkshopSimulationTests.cs:310` Reset body encodings; `WorkshopPhysicsEngineTests.cs:72` receiver frame snapshot; `WorkshopPhysicsLawCylinderTests.cs:93,95,141,145` refused source/candidate arrays; `PrismaticConstraintTests.cs:141,198` refusal arrays and `:155` impulse bytes; `WireFaultTests.cs:169` Motion.Bytes. Decode body/joint/motion fields and assert valid-world continuation; reuse motion semantics from WorkshopReadTests.
- Bulk `HasSameBits` output oracles: `EventRingProducerTests.cs:77`, `PressurePlateTests.cs:681–689`, `LatchSupplyFixtureTests.cs:333,339,356`, `BothGateTests.cs:77,215`, `DeclaredPresentationTests.cs:32`, `ElectricalNandTests.cs:101,368`, `ElectricalNorTests.cs:106,382`, `ElectricalOrTests.cs:108,403`, `ElectricalXorTests.cs:116,400`, `DeclaredIndicatorPassageTests.cs:252,260`, `MemoryGuardTests.cs:165`, `ClockGuardTests.cs:103`, `WorkshopWireFlatBuffersTests.cs:433`, `WorkshopWireTests.cs:832`, `WorkbenchCapacityTests.cs:269,278`, `WorkshopSimulationTests.cs:73`. Observe relevant owner/node/phase/count/power/deadline/energy/binding values. Do not simply replace bits with exact simulated float arrays.
- Replay helper `NoFixedCapacityTests.cs:262`: split usage by contract, retaining exact authored/codec assertions. Motion replay callers at `:197,257`, `ContactWorkInstanceTests.cs:103–104`, `SliderInstanceTests.cs:146`, `WorkshopGpuDeviceTests.cs:33` need independent impact/work/launch or spring displacement/return/stability outcomes and controls. ContactWork and Slider decoded save comparisons are already delivered.
- Replay precision: `WorkshopPhysicsLawCylinderTests.cs:431,1950–1951` and `WorkshopPhysicsLawRestitutionTests.BouncingRunsAndAReusedEngineReplayPreserveBodyMotion`: retain descent/rotation/support and fall/rebound/static support without full historical trajectory pins. `EventRingProducerTests.cs:434–448,473`, `MemoryGuardTests.cs:196,208`, `EventRingConsumerTests.cs:463,467`: required kinds/owners/occurrences/causal order rather than incidental arrival ticks/phases. Ring cursors and deliberate timing remain exact. At `EventRingProducerTests.cs:130`, replace a separate production tracker as oracle with independently expected geometric passage.
- `WorkshopPhysicsLawTests.cs:969,997,1027,1126`: four rolling groups. Reuse RollOnBench, Settle, independently predicted rolling law, existing 0.02 m/s stopped observation, final one-second stability window and resistance-free controls. Keep both ball types/all three cadences and +X/+Z coverage. Assert cumulative backward distance from meaningful clearance, not instantaneous 1e-6 m/s gates or every-tick decreases. Preserve independent energy/contact/rolling impulse checks. At `:668–790`, replace KnownViolationsSha256/canonical/hash helper with Assert.Empty(KnownViolations), preserving forbidden-source scan and detection controls.
- Private reach-ins: `SliderInstanceTests.cs:89,122` can use existing `engine/MachineWorld.cs` Parts. `WorkshopGpuDeviceTests.cs:313–315,330–339,364–401` should observe next committed phase after candidate refusal and held/queued/disposed work completion instead of _committedMemory/_gate/GateDisposed. `HoldTimerTests.cs:207` should feed forged candidates through public advancement instead of ValidateAdvancedRead reflection. `BasketballResourceTests.cs:374` should use ordinary _ExitTree lifecycle instead of DisposeWorkshop.
- `WorkshopHintTests.cs:27,347–352` and `PalettePlacementTests.cs:802–838,938,1338,1448` plus helper callers: use existing MachineWorld.InitializeWorkshop(Func<Task<IWorkshopClient>>) and actual button/editor signals; observe construction, status, Undo effect, hints and transforms instead of private fields/handlers.
- `WorkshopActivationAnimationTests.cs:115–199,235–299,335–350`: private constructor/peer/schedule/lease/sample/queue/pump cluster. A minimal injectable genuine browser transport boundary may be necessary; use public client lifecycle, ControlUi, TryUiFrame and receive operations. Preserve foreign property/occurrence rejection, hidden-owner projection replacement, cadence retry, Reset old-world rejection, once-per-frame delivery, preparation queueing and lease release. TS/evaluator tests cover portions but cannot silently replace managed lease proof.
- `tools/runtime/workshop-clock-ring.test.ts:64,122,123`: decode probe identity/sequence/requester stamp rather than echoed buffers. `workshop-client-lifecycle.test.ts:440`: decode acknowledgement identity while retaining backpressure/completion/duplicate refusal. `workshop-animation-clock.test.ts`: replace internal turns-call-log oracles with posted delivery/epoch/no-duplicate behavior. Event-ring public memory/cursor tests are valid seams.
- Classify and preserve legitimate contracts: `RigidMassPropertiesTests.cs:118–146` tests the equality interface itself; `WorkshopReadTests.cs:40–75` tests deduplication. Published wire/ABI offsets are legitimate when tested through encoder/decoder. Reflection API-absence guards in PartRegistry/BasketballResource/BundleElementId are structural guards, not behavioral private reach-ins. `ElementIsolationTests.cs:270,340` repository source-census hash should yield to existing semantic admission/classification/shrink guard, not be misclassified as vendor integrity. Keep `VendoredFlatBuffersIdentityTests` and `CSharpStaticGateTests` external/build integrity.
- Delivered proof to reuse: digest child 12 methods/57 cases and fixtures; `F32BehaviourIdentityTests` goals/causal replay without historical endpoints; `WorkshopReadTests:159,218` decoded motion/empty admission; `PackageRuntimeParityTests:212–282` independent geometry formulas; `tools/runtime/workshop-save-storage.test.ts:54–107` decoded fixtures; core warmup removal. Do not recreate them.

## Tasks & Acceptance

**Execution:**
- `CuriousContraptions.tests/` and `tools/runtime/` mapped sites -- replace oracles and migrate private arrangements/observations; inspect helper aliases/callers so the named backlog closes rather than only a grep pattern.
- `engine/gpu/BrowserWorkshopClient.cs`, `engine/gpu/IWorkshopClient.cs` and `engine/MachineWorld.cs` -- only if required, inject the genuine external boundary using one production path; never expose implementation internals or mock the solver.
- `WorkshopPhysicsLawTests.cs` -- complete four rolling replacements and direct empty-list guard; delete obsolete hash-only code.
- `_bmad-output/implementation-artifacts/test-behavioral-hygiene-inventory.md` -- record compact per-method old oracle → preserved behavior/control/boundary mapping, reuse delivered proof and classify remaining legitimate integrity/ABI/interface checks.
- `TODO.md`, `_bmad-output/implementation-artifacts/sprint-status.yaml`, `_bmad-output/planning-artifacts/epics.md` and this spec -- keep current status/evidence accurate; record TypeScript publication and hygiene results without remote qualification claims.

**Acceptance Criteria:**
- Given every replaced assertion in the inventory, when its real interface is exercised, then a direct assertion detects its named plausible defect, retaining positive, one-cause control and applicable boundary/lifecycle proof.
- Given the remaining game tests and helper aliases, when inspected against the inventory, then no simulation/save/event/render hash or whole-buffer oracle and no behavioral private reach-in remains; legitimate integrity, structural guards, defined equality and published ABI tests are explicitly distinguished.
- Given equivalent runs, when numerical/contact timing varies harmlessly, then tests still accept the intended outcome; loss of interaction, wrong ownership/order, created energy, broken restoration or meaningful drift still fails.
- Given the final candidate, when required local verification and five Auto layers complete, then all required tests pass with zero failures/skips, no unresolved required-now findings remain, and normal hooks plus exact-SHA approval support publication. Browser/deployment qualification remains explicitly unverified.

## Spec Change Log

- Implementation discovery, 29 October: WorkshopSimulationTests.AuthorityExhaustionFaultsBeforeGpuMutation seeds private Committed state at ulong.MaxValue; no existing public path or equivalent exhaustion test exists. Add a small checked advancement operation to AuthorityRevision in engine/gpu/WorkshopConstruction.cs and use that single rule in engine/gpu/WorkshopSimulation.cs revision admission/advancement. Test zero, ordinary, penultimate and maximum revision values directly, preserving no wrap and explicit refusal; retain public simulation revision progression, rejection atomicity and lifecycle proof. Do not add a session-state injection hook. This makes the genuine value boundary testable without private mutation; the old owner-state arrangement is recorded as unreachable through supported commands.

## Review Triage Log

### 2026-10-29 — Review pass 1
- verdicts: 15 findings — high 0, medium 5, low 3, false 7, maybe-false 0
- findings:
  - `[medium]` `[patch]` Blind Hunter: rejected parameter edits no longer prove Undo preservation — construction/transport checks and Undo of the later successful edit miss spurious entries underneath. Restore direct Undo-to-previous-real-edit proof.
  - `[low]` `[reject]` Blind Hunter: live coordinator exhaustion coverage was replaced by value-boundary proof — true coverage distinction, already disclosed. Authority starts at zero and supported inputs cannot set it; reaching ulong.MaxValue needs approximately 4.9 billion years at120advances/second. A new initialization/state-injection surface is more than a direct correction for a condition users do not encounter; keep exact checked value refusal/no-wrap and existing public progression/atomicity.
  - `[low]` `[reject]` Verification Gap: exhaustion no longer observes Faulted phase/no device mutation — pre-verified regression gap accepted as filed, not refuted by value tests. Same rare unreachable fixture condition as Blind Hunter; a new session-state injection surface is disproportionate under the low-finding rule. Production CanAdvanceRevision still sets Faulted/IdentityExhausted before mutation, and every advancement uses the checked operation.
  - `[medium]` `[patch]` Verification Gap other finding: recovery retains a23micrometre roundoff-only bound — code explicitly derives960×.4×2^-24 with no gameplay consequence. Replace with a functional drift/stability bound over the observation window, preserving deep recovery and settlement.
  - `[medium]` `[patch]` Edge Case Hunter: invalid parameter submissions may consume Undo steps — verified same missing observation as Blind Hunter; include this outcome in the shared Undo repair.
  - `[medium]` `[patch]` Edge Case Hunter: malformed-save preservation assertion compares a shallow alias to itself — original=saved followed by Equal(original,saved) cannot expose shared mutation or decoder poisoning. Replace with independent decoded valid-state/refusal continuation proof.
  - `[medium]` `[patch]` Edge Case Hunter: harmless lateral recovery variation fails the23micrometre bound — same roundoff-only oracle as Verification Gap; shared functional drift repair.
  - `[false]` `[reject]` Intent Alignment: bounded cleanup versus universal epsilon — the29October owner clarification explicitly requires quantity-specific tolerances and forbids a universal epsilon or new precision audit. The diff implements that resolved interpretation; all runtime float operations are not this slice's acceptance.
  - `[false]` `[reject]` Intent Alignment: component proof versus browser experience — current owner policy explicitly defers browser qualification until catalogue completion. Native/defined-interface proof and pending browser status are the required current surface, with no browser completion claim.
  - `[low]` `[reject]` Intent Alignment: authority exhaustion moves to the value boundary — accurate, disclosed and the same rare-state coverage tradeoff as the two other reports; no new state injection is justified.
  - `[false]` `[reject]` Intent Alignment: node delivery-before-service uses an external-host observation — the changed test targets the worker transport contract, and posted output makes consumed delivery observable. Actual managed animation/evaluator behavior is separately exercised by the real native integration/client tests; this is an allowed genuine external boundary, not an uncovered claim of evaluator proof.
  - `[false]` `[reject]` Intent Alignment: candidate versus completed backlog — spec/TODO explicitly mark review/hooks/publication pending and keep the entire backlog goal active. No claim of finished publication or backlog exists.
  - `[false]` `[reject]` Forward-Only borderline: browser transport injection — both entry points converge on one lifecycle, and direct imports move into the sole browser adapter. No old caller, fallback or second simulation path is retained.
  - `[false]` `[reject]` Forward-Only borderline: save-store interface — one production browser store and one codec/admission path; in-memory store exists only at the external test boundary.
  - `[false]` `[reject]` Forward-Only borderline: candidate-transforming test device — malformed outward input wraps the real device solely to test refusal; it is not a supported alternate simulation path.
- Groups: three medium patch groups (Undo preservation; independent save refusal continuation; functional recovery drift). No deferred findings or intent/spec gaps. Original implementer owns repairs; affected independent re-review follows.


## Verification

- Read `docs/delivery-workflow.md#local-test-authority` for the exact five matching compiler-log commands and generated browser deferral filter. Provision the pinned SDK/WASM workload and Node 26.12.1. No local Release worker build or web publish.
- Run affected native and compiled node tests after a matching build; reuse binaries with `--no-build --no-restore` until relevant inputs change. Review plausible defects by reading assertions, never mutants.
- At the end of the round, run `dotnet test CuriousContraptions.slnx -c Debug -f net10.0 --no-build --no-restore --nologo -noAutoResponse` with matching `CSHARP_STATIC_GATE_BINLOGS`, only the generated browser deferral filter, TRX evidence and TreatNoTestsAsError. All required game/tool tests must pass, Skipped: 0.
- Run `git diff --check`; reconcile removed-oracle map and active links. Normal commit hooks are supplemental and required. Do not poll CI.

## Review repair evidence — 29 October 2026

The original implementer repaired all three medium groups: sixteen generic-editor UI cases prove Undo reaches the previous real edit and original placement; eight malformed-save cases verify independent valid decoding before and after refusal; recovery checks finite motion and 1 mm horizontal drift throughout two seconds and stable support through the final second at all three cadences. Inventory mappings were updated. Matching incremental build passed with zero warnings/errors; affected checks passed 27/0/0 at `/tmp/tim-hygiene-review-fixes/_aidan-runlevelfive_2026-10-29_12_22_43_net10.0.trx`.

Original Edge Case Hunter and Verification Gap reviewers independently re-reviewed their affected repairs and found the original findings resolved with no new concrete defect. Edge Case Hunter also inspected the 27/0/0 TRX. Coordinator confirmed `WorkshopSimulation.CandidateRead` rejects every nonzero device candidate revision before initialization; the disclosed maximum-authority coverage distinction remains correctly triaged. No new state-injection hook is introduced.

Final full local round passed 5,004/0/0 at `/tmp/tim-hygiene-review-final` (main 4,512 in 20m58; session 48176 exit 0). Coordinator independently parsed all five TRX counters. Solution and playtest compiler captures passed with zero warnings/errors for changed tests. Unchanged web and file-tool compiler logs are reused from `/tmp/tim-hygiene-repair.iW16Av` under the local evidence-reuse rule. Hooks, exact-SHA approval and publication remain pending.

## Auto Run Result

Implemented the behavioral-test replacement inventory through defined interfaces: decoded saves/wire records, independent gameplay outcomes, actual editor/Undo behavior and real client lifecycle over genuine external transport/storage boundaries. Removed output identity oracles, private behavioral reach-ins, rolling precision pins and the empty-violation digest. Production uses one checked authority-revision transition; no solver, compatibility or fallback path was added.

Review: five Auto layers reported 15 findings. Five medium reports formed three patch groups, all repaired and independently re-reviewed; patched entry counts: high 0, medium 3, low 0. Deferred: none. Three low authority-exhaustion reports were rejected because the disclosed value-versus-owner coverage difference would require a new unreachable-state injection surface; CandidateRead refuses nonzero device revisions. Seven false findings were rejected for the reasons individually recorded above: resolved quantity-specific epsilon intent, explicit browser deferral, external worker transport observation backed by managed proof, no backlog-completion claim, and three permitted external-boundary abstractions. No confirmed forward-only finding remains.

Follow-up review recommended: false. The first pass had three medium patch groups, but the original affected reviewers have already checked their repairs and identified no remaining unverified repair risk. This is scoped native implementation proof; browser, deployment and release qualification remain pending. Maximum authority-owner exhaustion is not claimed by the value-boundary tests.

Verification: matching builds and scoped repair checks passed 27/0/0; prior complete round passed 4,988/0/0. Final full round passed 5,004/0/0 at `/tmp/tim-hygiene-review-final/results`, main 4,512 in 20m58 and other projects 492; process exit 0, all five TRX counters independently parsed. Refreshed solution/playtest compiler logs and unchanged web/file-tool logs form the matching five-log set. `git diff --check` passes. Normal hooks run on the final commit; exact-SHA approval and publication remain pending and will be recorded with their actual result. Original failed-round and passing-round evidence remain linked above.

### Files changed

- `CuriousContraptions.tests/BasketballResourceTests.cs` — Exercise disposal through normal scene exit.
- `CuriousContraptions.tests/BothGateTests.cs` — Assert gate inputs, owned rows and consumer response.
- `CuriousContraptions.tests/CanonicalBodyTests.cs` — Decode canonical body fields and verify refusal continuation.
- `CuriousContraptions.tests/ClockGuardTests.cs` — Assert deliberate pulse schedules and paused/reset clock state.
- `CuriousContraptions.tests/CompoundMassAndRollingTests.cs` — Assert independent receiver ownership and capture outcomes.
- `CuriousContraptions.tests/ContactWorkInstanceTests.cs` — Assert work occurrence, energy and launch after reload.
- `CuriousContraptions.tests/DeclaredIndicatorPassageTests.cs` — Assert causal passage counts and lamp outcomes.
- `CuriousContraptions.tests/DeclaredPresentationTests.cs` — Assert admitted presentation rows and native cue.
- `CuriousContraptions.tests/DeclaredPrismaticDriveTests.cs` — Assert real prior-scene reuse, passive travel and support.
- `CuriousContraptions.tests/DominoGuardTests.cs` — Assert each cascade reaches its sensor after Reset/reload.
- `CuriousContraptions.tests/ElectricalNandTests.cs` — Assert NAND truth table and consumer response independently.
- `CuriousContraptions.tests/ElectricalNorTests.cs` — Assert NOR truth table and consumer response independently.
- `CuriousContraptions.tests/ElectricalOrTests.cs` — Assert OR truth table and consumer response independently.
- `CuriousContraptions.tests/ElectricalXorTests.cs` — Assert XOR truth table and atomic decoded cyclic refusal.
- `CuriousContraptions.tests/ElementIsolationTests.cs` — Replace source digest with semantic shrinking-list guard.
- `CuriousContraptions.tests/EventRingConsumerTests.cs` — Assert cue response to the current event and Reset.
- `CuriousContraptions.tests/EventRingProducerTests.cs` — Assert geometric passage, event owners/counts and causal cycles.
- `CuriousContraptions.tests/Fixtures/element-isolation-known.json` — Move generic clock protocol classifications with transport extraction.
- `CuriousContraptions.tests/GameFloatGuardTests.cs` — Update exact external clock adapter inventory.
- `CuriousContraptions.tests/HoldTimerTests.cs` — Reject seven forged candidates through real coordinator advancement.
- `CuriousContraptions.tests/LampDetectorGuardTests.cs` — Assert forward passage, reverse control and rearm.
- `CuriousContraptions.tests/LatchSupplyFixtureTests.cs` — Assert latch, blade, lamp and supply after reload.
- `CuriousContraptions.tests/MemoryGuardTests.cs` — Assert owned checkpoint fields and independent counter threshold.
- `CuriousContraptions.tests/NoFixedCapacityTests.cs` — Assert large machines run after reload; preserve exact authored Reset.
- `CuriousContraptions.tests/PalettePlacementTests.cs` — Replace private arrangements with actual editor/input/Undo behavior.
- `CuriousContraptions.tests/PayloadInstanceTests.cs` — Assert both payload kinds fall after Reset/reload.
- `CuriousContraptions.tests/PressurePlateTests.cs` — Assert saved plate activation and clear controls.
- `CuriousContraptions.tests/PrismaticConstraintTests.cs` — Decode joint/state fields and preserve typed refusal controls.
- `CuriousContraptions.tests/SliderInstanceTests.cs` — Assert launch/return and observe public scene parts.
- `CuriousContraptions.tests/SourceStoreInstanceTests.cs` — Assert supply, consumer response and paused control semantics.
- `CuriousContraptions.tests/SwitchElectricalTests.cs` — Assert contact drives both domains after reload.
- `CuriousContraptions.tests/WeightTests.cs` — Assert saved compound rolls, settles and remains supported.
- `CuriousContraptions.tests/WireFaultTests.cs` — Decode motion intervals, owners and samples.
- `CuriousContraptions.tests/WorkbenchCapacityTests.cs` — Assert owned wide-occurrence state rather than bulk bits.
- `CuriousContraptions.tests/WorkshopActivationAnimationTests.cs` — Exercise real client lifecycle through browser transport boundary.
- `CuriousContraptions.tests/WorkshopBowlingTests.cs` — Retain decoded authored state without re-encoded byte pin.
- `CuriousContraptions.tests/WorkshopClockDerivationTests.cs` — Decode probe/reply fields and derivation identity.
- `CuriousContraptions.tests/WorkshopDelayTests.cs` — Assert contact-relative authored delay after reload.
- `CuriousContraptions.tests/WorkshopDominoTests.cs` — Retain decoded authored state without re-encoded byte pin.
- `CuriousContraptions.tests/WorkshopGpuDeviceTests.cs` — Assert committed progression and held/queued disposal outcomes.
- `CuriousContraptions.tests/WorkshopHintTests.cs` — Use startup, controls and frame reconciliation for hint behavior.
- `CuriousContraptions.tests/WorkshopPhysicsEngineTests.cs` — Assert static frames and independent receiver capture after reload.
- `CuriousContraptions.tests/WorkshopPhysicsLawCylinderTests.cs` — Assert descent, rotation, support and malformed refusal continuation.
- `CuriousContraptions.tests/WorkshopPhysicsLawRecoveryTests.cs` — Assert deep recovery and full-window game-grade drift/stability.
- `CuriousContraptions.tests/WorkshopPhysicsLawRestitutionTests.cs` — Assert each fresh/reused run falls, rebounds and retains support.
- `CuriousContraptions.tests/WorkshopPhysicsLawTests.cs` — Assert functional rolling/stopping and direct empty violation set.
- `CuriousContraptions.tests/WorkshopPipeLengthTests.cs` — Assert decoded length and capture through public authoring lifecycle.
- `CuriousContraptions.tests/WorkshopSaveTests.cs` — Assert decoded saves and independent valid continuation after refusal.
- `CuriousContraptions.tests/WorkshopSimulationTests.cs` — Assert semantic Reset and checked revision value boundaries.
- `CuriousContraptions.tests/WorkshopWallTests.cs` — Assert saved wall supports/deflects the falling body.
- `CuriousContraptions.tests/WorkshopWireFlatBuffersTests.cs` — Decode every construction, goal and physical-read field.
- `CuriousContraptions.tests/WorkshopWireTests.cs` — Decode settings, schedule, identities and retained ACK fields.
- `TODO.md` — Track current outcome, local evidence and next publication check.
- `_bmad-output/implementation-artifacts/spec-runtime-typescript-conversion.md` — Record the preceding TypeScript publication receipt.
- `_bmad-output/implementation-artifacts/spec-test-behavioral-hygiene.md` — Record scope, review triage, repairs and local verification.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` — Record TypeScript completion and hygiene slice status.
- `_bmad-output/implementation-artifacts/test-behavioral-hygiene-inventory.md` — Map removed oracles to behavior and retained controls.
- `_bmad-output/planning-artifacts/epics.md` — Record published prerequisites without changing backlog order.
- `engine/gpu/BrowserWorkshopClient.Schedule.cs` — Use the sole injected browser transport for schedule operations.
- `engine/gpu/BrowserWorkshopClient.Ui.cs` — Use the sole injected browser transport for UI delivery.
- `engine/gpu/BrowserWorkshopClient.cs` — Extract browser imports behind a genuine transport boundary.
- `engine/gpu/BrowserWorkshopSaveStore.cs` — Inject external storage while retaining one codec/admission path.
- `engine/gpu/IWorkshopClient.cs` — Declare the typed external browser transport contract.
- `engine/gpu/WorkshopConstruction.cs` — Add checked AuthorityRevision advancement with no wrap.
- `engine/gpu/WorkshopSimulation.cs` — Use the checked revision operation for every advancement.
- `tools/runtime/workshop-animation-clock.test.ts` — Assert posted delivery/epoch behavior instead of internal call logs.
- `tools/runtime/workshop-client-lifecycle.test.ts` — Decode acknowledgement fields instead of comparing raw output.
- `tools/runtime/workshop-clock-ring.test.ts` — Decode probe identity, sequence and clock fields.
- `ui/Workshop.cs` — Name existing controls and inject genuine save storage for native UI testing.

## Publication receipt — 29 October 2026

Published commit: `7e12c9452a165f42ab2ef9c8b80964f237a05b00`, parent `2dae218024ab260cc1797d967b13f2a5ea70d01e`. Final local suite **5,004/0/0** (main4,512 in20m58); normal commit hooks **126/0/0**, exit0. Original logs/TRXs: `/tmp/tim-hygiene-review-final`; hook output: `commit.log`.

Five Auto layers and affected repair re-review completed. Independent reviewer `/root/hygiene_edge` returned SnapshotApproval Pass for this exact committed SHA, then independently verified remote main equals it after push and returned terminal implementation Pass. Worktree was clean. No extra tests or hashing were used for terminal verification. CI/artifact/deployment, browser and release qualification remain unverified under current owner policy. Next slice is9A.24, started from this published main.
