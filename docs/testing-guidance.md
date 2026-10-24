# Unit testing guidance

Binding for every slice (owner, 24 October 2026). AGENTS.md links here. Implementers write tests to this guidance, and independent reviewers fail tests that break it.

## What a unit test is for

A unit test proves one behaviour of the game through a **defined interface**, in terms a player, an author or a consuming component would recognise. It fails when that behaviour breaks, and only then. It must not fail when a correct refactor changes internals.

## The defined interfaces

Test through these seams. They are the contracts other code relies on.

| Area | Arrange through | Act through | Observe through |
| --- | --- | --- | --- |
| Simulation | `WorkshopConstruction` built from package declarations and generic instances (`WorkshopInput.Generic`), or authored content | `WorkshopSimulation` commands: `Construct`, `Run`, `Pause`, `Resume`, `Advance`/`Step`, `Reset`, `Save`, `ConfigureElectrical`, `CancelPending` | `WorkshopCommandResult`, committed reads (`WorkshopRead`), event ring records, typed `WorkshopRejection` |
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
4. **Take tolerances from the game-grade envelope** (`docs/gpu-f32-physics.md`), or derive them from the calculation, such as f32 spacing or one tick of gravity. Write down how each tolerance was derived. Never widen a tolerance to make a test pass.
5. **Cover positive, negative and boundary cases.** Each behaviour has a positive case, a control that differs in exactly one cause, and literal boundary values (e.g. 0.25 and 8 kg admitted, values outside refused). Refusals assert the typed reason and that nothing changed: the construction, revision and Undo stack.
6. **Prove it can fail.** A new test must fail against a plausible defect. Show this with a mutation (a reverted fix, an off-by-one, a missing guard) and record it in the evidence. A test that passes under every plausible mutation is not useful.
7. **Be deterministic.** Use fixed seeds and fixed tick counts. No wall-clock sleeps, no ordering dependence and no shared mutable state between tests. Randomised sweeps use committed seeds, and a reviewer may add held-back seeds.
8. **Assert one behaviour per test**, with as few assertions as that behaviour needs. A theory may enumerate cases of the same behaviour.

## Not allowed

- **Hashes as assertions.** No hashes, digests or byte-identity of simulation, trace, event, render or save output, including golden-hash pins (AGENTS.md hard rule).
- **Internals.** No assertions on private fields, internal table or row layouts, compiled-row offsets, or reflection into non-public members, unless that layout *is* the defined interface (e.g. a published wire or ABI record, tested through its encoder and decoder).
- **Implementation-detail checks.** Method-call counts, call order or logging used as the behaviour.
- **Copied oracles.** Expectations taken from current output, or re-pinned after a behaviour change without explaining the physical reason.
- **Skipped tests.** No `Skip`, `Explicit`, conditional or excluded tests outside the listed browser deferral (AGENTS.md hard rule).
- **Duplicate test-data rows** that xUnit collapses.
- **Tests that cannot fail**, such as tautologies or assertions that a value equals itself.

## Architecture guards: a separate, small category

A few tests protect structural invariants that behaviour tests cannot see:
- the element isolation ratchet;
- the C# static gate;
- the solver-name scan;
- the rule that only the event-ring memory wrapper may view shared memory.

These tests may read source or metadata. They must:
- be named as guards;
- assert only the structural rule;
- have a mutation proving they catch a violation.

A source scan does not replace behavioural proof. Prefer encapsulation that makes the violation impossible, with a small guard that the encapsulation is respected.

## Contract tests at boundaries

A codec, validator or worker transport is tested as a contract:
- a round trip of each domain value;
- typed refusal of malformed, legacy, unknown or out-of-range input at **every** decode boundary, with nothing changed afterwards.

Legacy inputs are committed fixture files. They are not pinned by hash.

## Reviewer checklist

For each new or changed test, confirm:
- it goes through a defined interface;
- the expected values are derived independently;
- the tolerances are derived from the envelope or the calculation;
- the positive, control and boundary cases are present;
- a mutation shows it can fail;
- it is deterministic;
- it has no hash assertions;
- nothing is skipped.

Any failure is a finding against the slice.
