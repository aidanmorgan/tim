# Unit testing guidance

Binding for every slice (owner, 24 October 2026). AGENTS.md links here. Implementers write tests to this guidance, and independent reviewers fail tests that break it.

## Required local verification

Use the [complete local verification contract](delivery-workflow.md#local-test-authority): provision the declared .NET SDK/WASM workload and exact Node 26.12.1, capture the five matching compiler logs, then run the complete solution with `CSHARP_STATIC_GATE_BINLOGS` and only the generated browser deferral filter. All required game/tool tests pass with zero failures and zero skipped game tests. The full local round and supplemental normal hook checks are both required; scoped hooks do not replace the full suite. Reuse unchanged matching logs and test binaries; relevant source, generated-input, dependency or configuration changes invalidate affected evidence. Missing compiler logs are a failure, never an alternate test mode.

## What a unit test is for

A unit test proves one behaviour of the game through a **defined interface**, in terms a player, an author or a consuming component would recognise. It fails when that behaviour breaks, and only then. It must not fail when a correct refactor changes internals.

## The defined interfaces

Test through these seams. They are the contracts other code relies on.

| Area | Arrange through | Act through | Observe through |
| --- | --- | --- | --- |
| Simulation | `WorkshopConstruction` built from package declarations and generic instances (`WorkshopInput.Generic`), or authored content, compiled on main through `WorkshopSceneCompiler` and the runtime codec (normally via the shared native harness) | `WorkshopSimulation` commands over the compiled scene: `Construct`, `Run`, `Pause`, `Resume`, `Advance`/`Step`, `Reset`, `Save`, `ConfigureElectrical`, `CancelPending` | `WorkshopCommandResult`, committed reads (`WorkshopRead`), event ring records, typed `WorkshopRejection` |
| Saves and wire | Domain values (`WorkshopSavedConstruction`, `WorkshopCommand`) or a committed fixture file of legacy bytes | `WorkshopSaveCodec.Encode`/`Decode`, `WorkshopWireFlatBuffers.EncodeCommand`/`DecodeCommand` | The decoded domain value, field by field, or the typed refusal reason |
| Packages and bundle | Package declaration data | `BundleValidator.Validate`, `PartRegistry.Load` | The returned `BundleError` set, or the admitted catalogue entry |
| Authoring and UI logic | The Workshop scene or the generic editor, as a player would use them | Placement (`PlacementPose`), the editor's parameter operations, wiring, Undo and Cancel | The construction after the operation, the typed refusal and the status text |
| Presentation | Committed reads and ring records | The declared presentation evaluator `Advance` | The evaluated binding values over time |
| JS runtime (workers, client) | The module's exported API, such as `EventRingMemory` | Its public methods, through the node harnesses | Values returned or posted, and observable errors |

Use the existing test builders (`PlacedParts`, `LawScene`, `SwitchMachine` and similar) when they compose these interfaces. Builders must not reach past them.

## A useful, meaningful test

1. **Name the behaviour.** The test name is a sentence describing what happens, e.g. `AFieldAtExactlyTheFirstSlotBeyondTheTableIsRefusedAtBothBoundaries`.
2. **Run the real production code.** Use the real simulation, compiler, solver and codecs. The only test doubles allowed are for genuine external boundaries (clock, browser or worker transport, file system), using the existing harness doubles.
3. **Derive the expected value independently**, from the spec, the CAT documents, physics or the declared data. For example:
   - analytic inertia;
   - (2/3) g sin θ for a rolling solid cylinder;
   - e² for a bounce ratio;
   - a declared range limit written as a literal.

   Never copy the expectation from observed output.
4. **Choose fidelity from the gameplay requirement before observing outputs.** Name the consequence a defect would cause: missed clearance, wrong contact/load, visible instability, cumulative drift over a play window, incorrect routing or created energy. Use the game-grade envelope (`docs/gpu-f32-physics.md`) where it covers that consequence. ULP counts, a fraction of contact slop, historical outputs or analytic digits alone do not justify a scene-level tolerance. Tight numeric allowances remain appropriate for bounded arithmetic/codec helpers. Never fit a margin to a failing observation; if an old oracle protects no intended behavior, replace it with a direct behavioral assertion and document the coverage mapping.
5. **Cover positive, negative and boundary cases.** Each behaviour has a positive case, a control that differs in exactly one cause, and literal boundary values (e.g. 0.25 and 8 kg admitted, values outside refused). Refusals assert the typed reason and that nothing changed: the construction, revision and Undo stack.
6. **Make it able to fail.** A new test asserts the changed result directly, so that a plausible defect (a reverted fix, an off-by-one, a missing guard) would make an assertion fail. Reviewers check this by reading the assertions. Building and running mutants is deferred to the post-catalogue E2E phase (owner, 25 October 2026).
7. **Be deterministic.** Use fixed seeds and bounded simulation windows; assert exact tick counts only where timing defines the feature. No wall-clock sleeps, no ordering dependence and no shared mutable state between tests. Randomised sweeps use committed seeds, and a reviewer may add held-back seeds.
8. **Assert one behaviour per test**, with as few assertions as that behaviour needs. A theory may enumerate cases of the same behaviour.

## Physical comparisons (owner, 26 October 2026)

The owner’s 28 October direction takes precedence: choose scene-level acceptance from functional clearance, intended contact/load and routing, visible resting stability, cumulative drift over a play window and independently justified energy limits. First identify the gameplay failure a bound would catch. A fraction of slop, a count of ULPs or proximity to archived output is not enough. A tight numerical allowance can still be appropriate for a bounded arithmetic helper, a canonical codec field or an explicit admission boundary.

`PhysicalTolerance` currently uses a 1 µm bore-radius allowance and resting excursion `min(50 µm, max(5 µm, 1e-4 × s))`, with nominal horizontal size `s`. Existing tests use box `min(2hx, 2hz)` and lying-cylinder `min(2radius, 2halfWidth)`, near-origin observations and f32 spacing no larger than allowance/8. These are existing implementation/test values, not mandatory default gameplay acceptance or proof of player relevance. Existing microradian levelling bounds have the same limitation. When one blocks a slice, identify its functional consequence before more solver work. Replace an unjustified oracle with direct behavioral coverage and record the mapping; do not fit a margin to a failing result or apply a blanket multiplier. Assess cumulative motion throughout the relevant play window: a final endpoint alone misses an excursion that returns. Invalid scales and NaN/infinity still reject, and no rest tolerance may snap velocity or suppress intended slow motion.

Preserve exact identities, ordering, authored Reset, intentional zero semantics and hard admission/gameplay thresholds. Same-input replay compares meaningful decoded fields; never hashes or whole output byte arrays. Historical lesson tick/ordinal matching and migration-only endpoint proximity are not gameplay acceptance, including on primitive controls. Keep goals, typed event ownership/kinds/causal order and semantic replay. Lesson deadlines are finite stall guards chosen for intended progression; the current ten-second guard is a default, not a ceiling, and may be increased when intended progression needs more time. Timing is strict where elapsed time defines the feature: timers, delays, cadence and declared holds.

No repository-wide comparison-compliance claim is made. Remaining physical comparisons must be judged by quantity, units and the behavior they protect. Original nanometre and historical migration observations remain evidence of past implementations, not new task prerequisites.

## Float comparison and effort rules

Owner clarification, 29 October 2026; applies immediately to active work and older specifications.

- Choose the least strict assertion that reliably detects the named gameplay defect. Prefer goal completion, required events in causal order, supported contact, plausible movement and stable lifecycle over complete trajectories or event sequences.
- Reuse named tolerances for each physical quantity and scale. For approximate finite scalar equality, an absolute-plus-relative rule such as `abs(actual - expected) <= max(absoluteAllowance, relativeAllowance * max(abs(actual), abs(expected)))` is suitable when justified by that quantity. The absolute allowance has the quantity's units; the relative allowance is dimensionless. Select them from functional clearances and the relevant play duration before observing results. There is no single epsilon for positions, speeds, angles, energy and time. Do not substitute machine epsilon for gameplay accuracy or add a new tolerance framework merely to satisfy this guidance.
- Exact IDs, counts required by a feature, enum values, authored state, intentional zero and explicit admission thresholds stay exact. Approximate equality must not silently widen authoring limits or suppress slow intended movement. Reject non-finite values at their defined boundaries.
- Run until the observable condition with a generous finite stall guard expressed in game seconds and converted using the selected cadence. Increase that guard when justified by intended progression; neither twelve ticks nor ten seconds is a universal limit. Assert elapsed timing precisely only for a deliberate timer, delay, hold or clock contract. Deterministic execution does not require pinning its historical trajectory.
- Before a second numeric repair, explain the observable defect in one sentence. If no gameplay or interface defect is established, replace the over-specified assertion and retain its meaningful positive/control coverage. A short rationale in the test or slice record is sufficient; do not create a precision audit, certificate or new evidence platform.
- Reviewers must identify a concrete uncovered failure before requesting tighter bounds, more seeds or repeated runs. Reuse applicable passing evidence; repeat only after relevant changes, a failure or an unresolved concern. Correctness remains required, but harmless float churn is not a defect.

## Not allowed

- **Hashes as assertions.** No hashes, digests or byte-identity of simulation, trace, event, render or save output, including golden-hash pins (AGENTS.md hard rule).
- **Internals.** No assertions on private fields, internal table or row layouts, compiled-row offsets, or reflection into non-public members, unless that layout *is* the defined interface (e.g. a published wire or ABI record, tested through its encoder and decoder).
- **Implementation-detail checks.** Method-call counts, call order or logging used as the behaviour.
- **Copied oracles.** Expectations taken from current output, or re-pinned after a behaviour change without explaining the physical reason.
- **Skipped tests.** No `Skip`, `Explicit`, conditional or excluded tests outside the listed browser deferral (AGENTS.md hard rule).
- **Duplicate test-data rows** that xUnit collapses.
- **Tests that cannot fail**, such as tautologies or assertions that a value equals itself.

## Integration tests: the system working as a whole, without a browser

Integration tests prove that the real subsystems work together as players experience them:
- content and packages;
- authoring;
- compilation;
- the worker device and solver;
- committed reads;
- the event ring;
- presentation;
- saves.

They never need Chrome, Playwright or a running browser. They run the core systems through one well-defined, deterministic mechanism.

**The mechanism.** All integration tests use the single shared native harness, the *Workshop integration harness* in `CuriousContraptions.tests`. It composes the production pieces:
- **Bundle:** the actual built `elements/elements.bundle.json`, read through `PartRegistry.Read`; actual scene startup exercises `PartRegistry.LoadFile`. Do not regenerate a separate integration bundle. Content comes from `content/` through its production reader.
- **Simulation:** the real Core-owned `WorkshopSimulation`, over the real `WorkshopGpuDevice` and `BrowserGpuTransport`. Main-owned `WorkshopSceneCompiler` compiles authored constructions into typed capability scenes; workers do not load packages or authored records. Commands pass through `WorkshopCommandRouter`, the shared production `WorkshopCommandExecutor` and the wire codec. Explicit native advancement uses the same response encoding; it does not add an Advance wire command or copy the worker command switch.
- **Clock:** an explicit integer physical-step clock and declared cadence settings (60, 120 or 240 Hz). Advance world and master time according to their production contracts; a manual Step does not invent elapsed master time. No wall-clock sleeps.
- **Authoring:** construction goes through the same operations the UI performs (placement through `PlacementPose`, generic-editor parameter operations, wiring, Undo and Cancel), or through authored lesson content. Hand-built internal tables are not allowed.
- **Observation:** committed reads, actual event memory through `EventRingConsumer` and `EventRingDelivery`, presentation samples from the retained/rebound declared evaluator, goal outcomes and typed refusals. Harness A establishes this mechanism and migrates duplicate orchestration; harness B supplies the complete current-catalogue matrix.

**A scenario** is a short, deterministic script:
1. **Build.** Place parts, set parameters and wire them through authoring operations.
2. **Run.** Run for N ticks, or until a named committed condition, with a bounded tick limit.
3. **Interact.** Pause, Step, Reset, Save, then Load into a fresh harness, where the behaviour applies.
4. **Assert.** Check observable outcomes: which bodies moved where, within envelope tolerances; which events fired, with what ownership and causal order (and feature-defined timing where applicable); which lamps or gates changed state; whether the goal was met; and what the player would see (presentation values) and read (status or refusal text).

**What integration tests must cover:**
- **Elements:** every catalogue and named element/variant in its source-required cross-element scenarios with the parts it is designed to interact with (e.g. switch → latch → supply → powered gate; ball → bumper → receiver).
- **Lessons:** every Playable lesson's authored solution solves at all three supported cadence profiles; all game values remain f32.
- **Reset:** exact after a run.
- **Save/Load:** reproduces the run.
- **Unsupported inputs:** refused atomically, end to end.

The JS runtime layer (simulation and animation workers, client, rings) is covered through the node harnesses. They drive the shipped modules under Node with SharedArrayBuffer and workers, deterministically, with no browser.

**Rules:**
- Integration tests follow every rule above: derived expectations, envelope tolerances, controls and boundaries, assertions that a plausible defect would fail, deterministic, no hashes, nothing skipped.
- Each test states the behaviour of the *system*, not of one unit. If a scenario fails, the assertion message names the observable that diverged.
- They run in the required local native suite; GitHub builds and deploys without repeating tests (owner, 26 October 2026). Keep them fast: bounded tick counts, the smallest construction that shows the interaction, and fixed seeds for sweeps.
- New elements add their integration scenarios in the same slice. A slice that adds or changes an interaction adds or updates the scenario that proves it.
- Browser E2E tests are a separate, later layer (see AGENTS.md). They are not a substitute for, or a prerequisite of, integration proof.

## Stress and randomised tests (coordinator, 24 October 2026)

- **Concurrency correctness comes from design.** Shared memory is reached only through one encapsulated atomic API per side, and a named guard proves nothing bypasses it. A stress test cannot prove the absence of races.
- **One bounded concurrency stress run is allowed in the routine suite**, as a smoke test of a few seconds.
- **Repeat-N loops are not acceptance evidence.** "Run it 40 times" is not required in reviews or evidence. Use them only as a one-off diagnosis tool when a flaky test is being investigated.
- **Randomised physics gates stay as routine tests:** fixed committed seeds, a bounded case count (e.g. 200 per class), and bounds derived from the envelope. Reviewers may add held-back seeds. Larger sweeps are one-off acceptance evidence, not routine tests.

## Architecture guards: a separate, small category

A few tests protect structural invariants that behaviour tests cannot see:
- the build-time element isolation guard;
- the C# static gate;
- the solver-name scan;
- the rule that only the event-ring memory wrapper may view shared memory.

These tests may read source or metadata. They must:
- be named as guards;
- assert only the structural rule;
- assert the rule so that a violation would fail them (a positive control, not a mutation run).

A source scan does not replace behavioural proof. Prefer encapsulation that makes the violation impossible, with a small guard that the encapsulation is respected.

## Contract tests at boundaries

A codec, validator or worker transport is tested as a contract:
- a round trip of each domain value;
- typed refusal of malformed, legacy, unknown or out-of-range input at **every** decode boundary, with nothing changed afterwards.

Legacy inputs are committed fixture files. They are not pinned by hash.

## Reviewer checklist

For each new or changed unit or integration test, confirm:
- it goes through a defined interface;
- the expected values are derived independently;
- scene tolerances have a gameplay justification, units and observation window; local mathematical tolerances follow their defined calculation;
- the positive, control and boundary cases are present;
- its assertions would fail under a plausible gameplay/contract defect (judged by reading, no mutation runs), and scene-level precision has a functional justification rather than only ULP/slop/history arithmetic;
- it is deterministic;
- it has no hash assertions;
- nothing is skipped.

Any failure is a finding against the slice.
