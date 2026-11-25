---
title: 'Closure review: electrical gates (EL-133 to EL-137 close as duplicates of the five delivered gate parts)'
type: 'chore'
created: '2026-11-21'
status: 'blocked'
baseline_revision: 'captured at dispatch (the main that contains the planning work)'
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '{project-root}/docs/testing-guidance.md'
  - '{project-root}/docs/delivery-workflow.md'
  - '{project-root}/docs/planning/invest/delivery-batches.md'
  - '{project-root}/docs/planning/elements/EL-133-electrical-and-gate.md'
warnings: ['blocked-on-prerequisite', 'multiple-identities']
deferred: []
---

<intent-contract>

## Intent

**Problem:** Five named identities, EL-133 to EL-137 (electrical AND, OR, XOR, NOR and NAND gates), are still `backlog`, although each names the same part, id, ports, operation and art as a delivered gate package. Leaving them open would schedule five slices that could only ship a second copy of an existing part.

**Approach:** One closure review. For each identity, record that it has no behaviour, parameter, port, mode, material or acceptance of its own, cite the executed tests that prove its required behaviour through the delivered part, add the few tests its spec requires that no test executes today, and close its ledger row and sprint key. No package, catalogue entry or palette entry is added.

## Boundaries & Constraints

**Always:**
- Each identity keeps its own ledger row, acceptance rows and cited tests; closing one does not close another.
- An identity is closed only when every acceptance row has a named test that ran and passed in this slice's full local round.
- New tests run the real production C# simulation through the delivered gate packages.
- The closure records what it does not claim: browser proof and campaign first use.

**Never:**
- No new or changed package, catalogue entry, palette entry, icon, scene, part id or alias.
- No production change in any engine, abstraction, bundle, element or tool project. If a planned test fails, the slice stops and returns to planning (see Design Notes).
- No edit to `docs/planning/requirements.md`, to any CAT spec or to `docs/planning/invest/current-consumers.md`.
- No deleted, weakened or skipped test; no hash or byte-identity assertion; no mutants; no browser run.

## I/O & Edge-Case Matrix

Each row is observed on a supplied consumer (a Powered gate) wired to the gate's `Supply`, and on the gate's committed logic read.

Rows G4 and G5 state the behaviour after the prerequisite, the self-edge row slice of `ENGINE-SIGNAL-NODES` ([epics.md, "Slice: self-edge row"](../planning-artifacts/epics.md#slice-self-edge-row-a-supply-self-edge-is-a-one-node-cycle)), is published. On `25df3cd5` both wires are refused as `InvalidConnection`. In both rows the self-wire is carried by a construction passed to the native edit interface and by a stored save, not made with the Workbench connect tool, which refuses a same-part target.

| Row | Input / State | Expected Output / Behavior | Error Handling |
|-----|--------------|---------------------------|----------------|
| G1 | For each of the five gates: every input row (00, 01, 10, 11) with and without supply | The consumer is powered exactly when the supply is present and the gate's operation is true: And 11; Or 01, 10, 11; Xor 01, 10; Nor 00; Nand 00, 01, 10 | A true row with no supply wire never powers the consumer |
| G2 | For each gate: a condition changes during Run | The output follows on the committed tick the change is read; reordered placement and wiring give the same result | No error expected |
| G3 | For each gate: Run, Reset, Save and Load | Reset restores the unpowered authored state and wiring; a loaded save equals the saved construction, socket by socket | No error expected |
| G4 | Xor, Nor or Nand with its own `Supply` wired to its own `FirstIn` or `SecondIn` | Refused before any solve with `NonmonotoneFeedback` and the loop message "Break the wire loop through parts N. This gate's output cannot feed its own inputs."; the construction is unchanged | A save holding that wire is refused at load with `InvalidConnection`, inner reason `NonmonotoneFeedback` |
| G5 | Both gate or Either gate with its own `Supply` wired to its own `FirstIn` | Admitted; it never powers itself. A Both gate stays off whatever its other input does. An Either gate turns on with its other input and off again on the next committed tick after that input is removed | No error expected |
| G6 | A battery on a gate's condition inputs and another on `PowerIn`, with a consumer drawing work | Only the `PowerIn` battery is debited | No error expected |
| G7 | The five delivered packages | Each is its own catalogue entry with its own name, icon and operation, four electrical ports (`FirstIn`, `SecondIn`, `PowerIn`, `Supply`), no parameter, no energy store and no activation port | No error expected |
| G8 | For each gate: its three lamps | Input lamps follow committed inputs; the output lamp follows truth and supply; Reset returns them to slate | No error expected |

</intent-contract>

## Code Map

**Status: blocked on the engine programme (owner decision 22) and on the self-edge row of `ENGINE-SIGNAL-NODES`.** Baseline: captured at dispatch (coordinator decision 38, 23 November 2026): `baseline_revision` is the published main that contains the planning work, and every scope guard and `git diff` here compares against it; `25df3cd5` is the drafting revision only, not a baseline. Closure reviews wait for the programme (orders 1–10, owner decisions 3a and 22); the self-edge row's slice text, formerly the separate contract `spec-engine-supply-self-edge-cycle-rule.md` (retired 23 November 2026), is in [epics.md, ENGINE-SIGNAL-NODES, "Slice: self-edge row"](../planning-artifacts/epics.md#slice-self-edge-row-a-supply-self-edge-is-a-one-node-cycle). Owner: coordinator. Retry condition: that slice is published on main. Shape: **closure review** (Duplicates of delivered parts; one record, one candidate commit, one approval). All five identities stay in this one closure. `baseline_revision` is the drafting revision, published main `25df3cd5` (the cited code is unchanged from `8d1d68aa`); the contract is replanned from the published main that holds the self-edge slice, and the coordinator re-captures `baseline_revision` then, before first implementation. The Coverage admission gap does not affect it, because it adds no package.

Why it is blocked: each gate has one supply node (`elements/BothGate/BothGateElementDeclaration.cs:33`–`:40`), so `Supply` to its own `FirstIn` resolves to a self-edge (`engine/gpu/WorkshopSupplyCompiler.cs:37`–`:38`) that `SupplyTopology.ValidateEdges` refuses as `InvalidConnection` (`engine/gpu/SupplyRoute.cs:50`–`:54`) before the cycle check at `:60`. EL-135, EL-136 and EL-137 require the loop refusal for that wire, and the requirement admits And/Or cycles (`requirements.md:6689`), so rows G4 and G5 are unmet through the delivered parts today.

**Why the self-edge slice stays the prerequisite** (coordinator's decision, 21 November 2026): EL-135's own acceptance requires an own-input wire to be refused with the loop message; a save or a command can carry such a wire even though the Workbench connect tool refuses a same-part target (`ui/WorkshopConnections.cs:106`, "Choose another part with a matching input socket."); and one rule for one-node and many-node cycles is the forward-only state. The connect tool is left unchanged.

**Planning verdict:** Scoped Pass of the `8d1d68aa` draft (planning auditor `aa4a6be6eda1b6b51`, second pass, 21 November 2026). It does not carry over: the replanned contract passes the planning gate again.

**At replanning** (from the published main that holds the self-edge slice):
1. The self-edge slice publishes with its rows S1, S2, S3 and S5 as drafted, or rows G4 and G5 here are corrected before this intent block freezes.
2. Probe row 6 is re-marked present with the repaired symbol in `engine/gpu/SupplyRoute.cs` and a named test of that slice (the class name `SupplySelfEdgeTests` is checked then), and the counts line is updated.
3. Line numbers the programme and the self-edge slice shift are refreshed, including the `SupplyRoute.cs` lines in "Why it is blocked" and the node names `ENGINE-CAPABILITY-INSTANCES` renames.
4. Rows G1–G8 and the G7 identity are taken from the CAT-013 and CAT-024 to CAT-027 acceptance rows, which are the single acceptance authority for a closure identity (coordinator decisions 16 and 27), not from the EL specs.
5. G6 is executed once, by the cited `ComposableSupplyRoutingTests` case, not repeated per gate.

### Duplicate decision, per identity

Each EL spec was read in full against its CAT spec and package (21 November 2026).

| Identity | Satisfying part (package, id) | Operation | Delta found | Decision |
|---|---|---|---|---|
| EL-133 Electrical AND gate | CAT-013, `elements/BothGate`, `both_gate` | And (`BothGateElementDeclaration.cs:34`) | None in bodies, material, ports, parameters, lamps, art or acceptance. The spec's legacy description text is longer than the package's; wording only. | True duplicate |
| EL-134 Electrical OR gate | CAT-026, `elements/ElectricalOr`, `electrical_or` | Or | None | True duplicate |
| EL-135 Electrical XOR gate | CAT-027, `elements/ElectricalXor`, `electrical_xor` | Xor | None | True duplicate |
| EL-136 Electrical NOR gate | CAT-025, `elements/ElectricalNor`, `electrical_nor` | Nor | None | True duplicate |
| EL-137 Electrical NAND gate | CAT-024, `elements/ElectricalNand`, `electrical_nand` | Nand | None | True duplicate |

The EL specs themselves give the delivered id, title, colour and port positions as their catalogue entry (each spec's Declaration table, "Catalogue / inventory" row), and `EL-133-electrical-and-gate.md:5` says the other four "add only their operation". No identity leaves the group.

Three acceptance items in the EL specs have no executed test today and are added here as four tests: a single gate wired to its own input (EL-135, EL-136 and EL-137 negative controls; EL-133 and EL-134 loop boundaries), a Both gate condition removed and restored during Run (EL-133), and one check that each identity's declared entry matches its spec (G7).

### Capability probe

Counts on `25df3cd5` (every cited symbol unchanged from `8d1d68aa`): 7 present, 0 present-untested, 1 narrower, 0 missing.

| # | Capability | Mark | Symbol | Exercising test |
|---|---|---|---|---|
| 1 | Supplied logic node with a declared operation | present | `elements/BothGate/BothGateElementDeclaration.cs:33` | `BothGateTests.SeparateConditionsAndEnergyDriveActualConsumer` |
| 2 | Energy provenance from `PowerIn` only | present | `engine/gpu/SupplyRoute.cs:77` | `BothGateTests.EnergyProvenanceIgnoresBothConditionSourcesAndUndefinedRulesReject` |
| 3 | Finite source debit through a gate | present | `ComposableSupplyRoutingTests.cs:286` construction over `elements/Battery/BatteryElementDeclaration.cs:70` | `ComposableSupplyRoutingTests.LogicProvenanceFundsNoWorkWhilePowerSourceFundsWork` |
| 4 | Monotone cycle settles from unpowered each tick | present | `engine/gpu/SupplyRoute.cs:60` | `ElectricalOrTests.OrLoopIsAdmittedAndSettlesAtItsLeastFixedPointEveryTick` |
| 5 | Nonmonotone cycle refused atomically, in edit and save | present | `engine/gpu/SupplyRoute.cs:69` | `ElectricalXorTests.ZeroDelayFeedbackThroughTheGateIsRefusedAtomicallyBeforeAnySolve` |
| 6 | A gate wired to its own input is judged by the cycle rule | **narrower**: refused first as `InvalidConnection` | `engine/gpu/SupplyRoute.cs:50`; the one-node branch at `CuriousContraptions.Abstractions/SupplyCycles.cs:67` is unreachable for a single-node part | none. Delivered and tested by the self-edge row slice of `ENGINE-SIGNAL-NODES` (planned class `SupplySelfEdgeTests`); this slice adds the per-identity cases (G4, G5) |
| 7 | Committed socket lamps | present | `BothGateElementDeclaration.cs:43` | `BothGatePresentationTests.ThreeNativeLampsFollowCommittedInputsAndSupplyThroughHideAndReset` (`BothGateTests.cs:252`) |
| 8 | Save and Load of electrical wiring; Reset | present | `engine/gpu/WorkshopSaveCodec.cs:19`, `:48` | `ElectricalNorTests.ClosingOneSwitchRetractsTheOutputAndSaveLoadAndResetAreExact` |

### Review tier

**Engine tier (all six layers).** Capability 6 is narrower on this revision. The tier is determined again, from the probe, when the contract is replanned from the revision that publishes the prerequisite.

### What "closed" changes on disk (owned scope list)

- `CuriousContraptions.tests/ElectricalGateClosureTests.cs` -- new; the four tests below. It may call the existing `internal` helpers of `BothGateTests` and `ElectricalXorTests`; it does not change them.
- No test-project edit: `CuriousContraptions.tests.csproj:77` compiles every top-level `*.cs` (S2, `25df3cd5`).
- `docs/planning/invest/named-elements.md:1227`, `:1236`, `:1245`, `:1254`, `:1263` -- in each entry replace the sentence "**Backlog:** implementation and native proof must be established; declaration readiness alone is not completion." with the closure sentence in task 2.
- `_bmad-output/implementation-artifacts/sprint-status.yaml:687`–`:691` -- `named-el-133` to `named-el-137` change from `backlog` to `done`, each with the comment `# closed as duplicate of <id>; spec-closure-review-electrical-gates; no package`.
- `docs/planning/elements/EL-133-electrical-and-gate.md:16` and line 16 of the EL-134 to EL-137 specs -- `Status` changes from "not started" to "closed as a duplicate of `<id>` (CAT-0nn); no package of its own".
- `docs/planning/invest/delivery-batches.md`, section 2a (Candidate duplicates of delivered parts) -- the Electrical gates row gains "closed by `spec-closure-review-electrical-gates.md`".
- `TODO.md`, `_bmad-output/implementation-artifacts/deferred-work.md` (only if a finding is deferred under the third-pass re-review exception only; never for a failing planned row), this contract and `run-logs/spec-closure-review-electrical-gates.md`.

**Coverage tables: no change.** `tools/Coverage` keeps `element-133` to `element-137` as source obligations (for example `tools/Coverage/CapabilityRequirements.cs:291`); it has no "closed" state and this slice adds none. `docs/coverage/**` is not regenerated. `docs/planning/general-engine-element-map.md:238`–`:242` are navigation rows and stay. `docs/planning/requirements.md` is not edited: its checkboxes are not the status authority, and each requirement block is a hashed coverage source (`tools/Coverage/RequirementDiscovery.cs:72`), as are the CAT specs and consumer sections (`tools/Coverage/CurrentCatalogue.cs:93`).

**Not added:** no directory under `elements/`, no line in `CuriousContraptions.slnx`, no palette, catalogue, registry or coverage-catalogue entry, no icon, no id.

**Deleted by this slice:** nothing. No list, guard or test is removed, so no protection is lost. The `backlog` status that would have scheduled five slices is replaced by a status that names the proof; a reviewer can re-run every cited test.

### Owner decisions made under the standing delegation

1. **A gate wired to its own input** (made by the coordinator under the standing delegation, 21 November 2026): a monotone gate (Both, Either) is admitted and settles unpowered, the same rule as any And/Or cycle; a nonmonotone gate (Xor, Nor, Nand) is refused with the loop message; the repair is the self-edge row slice of `ENGINE-SIGNAL-NODES`, published first. Reason: `requirements.md:6689` states one cycle rule with no one-part exception.
2. **Finite supply through a gate** (EL-133 open question 4): a gate carries no power limit of its own; the source's limit and balance govern, as delivered and as capability 3 proves.
3. **Observable consumer and condition sources** (EL-133 open questions 1 and 2): the Powered gate is the consumer and batteries, Switch, Latch and Hold contacts are the condition sources, as CAT-013's delivery decisions of 22 October 2026 record. The EL rows ask for "a supplied load (Motor or Powered gate)". A Motor through an Either gate is already executed (`MotorTests.AMotorSuppliedThroughAnEitherGateRunsOnlyWhileTheGateIsOn`, `MotorTests.cs:497`); a Motor through a Both gate stays CAT-013's own residual (its decision 1) and is not required to close EL-133.
4. **Description wording for the Both gate**, decided here: the delivered package text stands. Reason: the longer legacy text only adds wiring advice the socket marks already give; it is not behaviour.
5. **Socket names in a save** (EL-133 spec line 82 lists `first_in`, `second_in`, `power_in`, `supply`): G3 is met by the typed round-trip. A saved wire carries `PortId` values (`FirstIn`, `SecondIn`, `PowerIn`, `Supply`; `CuriousContraptions.Abstractions/PortId.cs:25`–`:28`), and the cited tests assert the loaded construction equals the saved one record by record, which fails if any socket changes. The snake-case spellings are the harvest baseline's and are not current input.

Needs the owner: none.

### What the closure does not claim

Browser qualification: the five parts' own browser cases cover these identities in the post-catalogue E2E phase. Campaign first use (levels 21–40, `requirements.md#campaign-element-coverage`) stays with the level slices. The "short loop message on the bottom status line" is a browser case for the two-part loop only: the connect tool cannot create a self-wire (`ui/WorkshopConnections.cs:106`), so no browser case is queued for G4 or G5, whose refusal and message text are asserted natively through the construction and save paths.

### Carry-forward and concurrency

No `deferred-work.md` entry touches these paths. This slice shares the ledger, the sprint status, `delivery-batches.md` and `TODO.md` with any element slice, so it runs alone or publishes serially, after the engine programme and the self-edge slice.

## Tasks & Acceptance

**Execution:**
1. `CuriousContraptions.tests/ElectricalGateClosureTests.cs` -- create with four tests -- the unexecuted acceptance items.
   - `ABothGateOutputRetractsWhenOneConditionIsRemovedDuringRunAndReturns` (`both_gate`): `BothGateTests.Machine()` observed in three phases of 30 ticks: both conditions and supply on, the consumer is powered on every read; the second condition battery disabled, the gate reads `FirstOnly`, is inactive and the consumer unpowered on every read of the phase; the battery enabled again, powered on every read.
   - `ANonmonotoneGateWiredToItsOwnInputIsRefusedAtomically` (theory: `electrical_xor`, `electrical_nor`, `electrical_nand` × `FirstIn`, `SecondIn`): a battery on `PowerIn`, a Powered gate on `Supply`. Control: without the self-wire the construction validates. With it, `Validate` refuses `WorkshopEditRejection.NonmonotoneFeedback`, the message starts "Break the wire loop" and names the gate's id; decoding a save that holds the wire is refused `WorkshopSaveRejection.InvalidConnection` with that inner reason; the construction without the wire still validates and equals the original.
   - `AMonotoneGateWiredToItsOwnInputIsAdmittedAndNeverPowersItself` (theory: `both_gate`, `electrical_or`): `Supply` to its own `FirstIn`, battery on `PowerIn`, a second battery on `SecondIn` starting disabled. For 60 ticks the gate is inactive and the consumer unpowered. With the second battery enabled for 60 ticks: the Either gate is active and the consumer powered on every read; the Both gate stays inactive, because its first input is its own unpowered output. With it disabled again: both gates are inactive on the next committed read and for 60 ticks.
   - `EachGateIdentityIsItsOwnCatalogueEntryWithItsDeclaredOperation` (theory rows from the EL specs, not from the packages): `both_gate`/"Both gate"/And, `electrical_or`/"Either gate"/Or, `electrical_xor`/"Exactly one gate"/Xor, `electrical_nor`/"Neither gate"/Nor, `electrical_nand`/"Not both gate"/Nand. Asserts the bundle entry's display name, category Control, the one logic node's operation, exactly the four electrical ports with their directions, no parameters, no energy store, no activation port, and an icon path that exists and is unique among the five.
2. `docs/planning/invest/named-elements.md` -- in each of the five entries write: "**Closed as a duplicate** (Duplicate closure, closure review `spec-closure-review-electrical-gates.md`) of [CAT-0nn](../elements/<CAT spec file>) package `elements/<Dir>` (id `<id>`): no package, catalogue or palette entry of its own. Proof through that part: `<the tests of its row in the acceptance-to-test table>`. Browser cases are that part's; campaign first use stays with the level slices." -- the ledger row cites the part and the tests.
3. `_bmad-output/implementation-artifacts/sprint-status.yaml`, the five EL specs, `docs/planning/invest/delivery-batches.md`, `TODO.md` -- make the status edits listed above in the candidate commit -- status.

**Acceptance Criteria:**
- AC-133: Given the delivered Both gate, when the tests cited for EL-133 run, then G1–G3 and G5–G8 hold for operation And, and the EL-133 ledger row cites the part and those tests.
- AC-134: Given the delivered Either gate, when the tests cited for EL-134 run, then G1–G3, G5, G7 and G8 hold for operation Or, and the ledger row cites them.
- AC-135: Given the delivered Exactly one gate, when the tests cited for EL-135 run, then G1–G4, G7 and G8 hold for operation Xor, and the ledger row cites them.
- AC-136: Given the delivered Neither gate, when the tests cited for EL-136 run, then G1–G4, G7 and G8 hold for operation Nor, and the ledger row cites them.
- AC-137: Given the delivered Not both gate, when the tests cited for EL-137 run, then G1–G4, G7 and G8 hold for operation Nand, and the ledger row cites them.
- AC-N: Given the candidate, when its changed paths are listed, then nothing under `elements/`, `engine/`, `tools/`, `schemas/` or any production project changed, and the palette and bundle hold the same parts as the baseline (45 at the drafting revision `25df3cd5`).

## Design Notes

1. **Behaviour and scope.** No behaviour changes. The slice proves five identities through five delivered parts and records it (loop protocol, Slice shapes, Duplicate closure).
2. **Reusable primitive and composition.** None new; the supplied logic node and cycle rule are delivered (probe rows 1, 4, 5).
3. **Generality.** The new tests take their expected names, operations and outcomes from the EL specs and the requirement rule (`requirements.md:6689`), not from the package source.
4. **Forward-only.** No alias, second id or second entry is created; that is the point of the closure.
5. **If a planned test fails.** The owner stops and returns the slice to planning. The defect is repaired by a named engine slice published first, and this closure is replanned from that revision. No identity is closed by a candidate in which any planned row is absent, failing or removed.

## Verification

Executor: the implementation owner runs every command. Environment and the five compiler-log builds are those in `docs/delivery-workflow.md#reproducible-local-verification`; a change under `CuriousContraptions.tests/` invalidates `solution.binlog` and `playtest.binlog`.

**Commands:**
- The five `dotnet build … -bl:` captures from the workflow -- expected: zero warnings and errors.
- `dotnet run --file tools/anvil/VerifyPolicy.cs -- --maintainability --project-root "$PWD"` -- expected: both canary `PASS` lines, the `Coupling dependencies:` summary, zero violations.
- The registration-check `dotnet test … --filter` command from `docs/delivery-workflow.md#new-package-registration-check` (a test file is added) -- expected: zero failed, `Skipped: 0`.
- `dotnet test CuriousContraptions.slnx -c Debug -f net10.0 --no-build --no-restore --nologo -noAutoResponse --filter "FullyQualifiedName~ElectricalGateClosureTests|FullyQualifiedName~BothGateTests|FullyQualifiedName~BothGatePresentationTests|FullyQualifiedName~ElectricalOrTests|FullyQualifiedName~ElectricalOrPresentationTests|FullyQualifiedName~ElectricalXorTests|FullyQualifiedName~ElectricalXorPresentationTests|FullyQualifiedName~ElectricalNorTests|FullyQualifiedName~ElectricalNorPresentationTests|FullyQualifiedName~ElectricalNandTests|FullyQualifiedName~ElectricalNandPresentationTests|FullyQualifiedName~ComposableSupplyRoutingTests|FullyQualifiedName~SupplySelfEdgeTests"` -- expected: zero failed, `Skipped: 0`; this is the run the table cites for existing tests.
- `git add -N CuriousContraptions.tests/ElectricalGateClosureTests.cs`, then `git diff --check <baseline_revision>` -- expected: no output.
- `git diff --stat <baseline_revision> -- elements engine tools schemas CuriousContraptions.slnx` -- expected: no output (AC-N).
- `caffeinate -i` the full-round `dotnet test` command from the workflow -- expected: zero failed, `Skipped: 0`; the total is the current full local round plus the new cases (the owner records the count) and nothing is removed.
- The candidate commit runs the unmodified `.githooks/pre-commit` on the staged snapshot; the new test file is staged.

### Acceptance-to-test table

`Closure` stands for `ElectricalGateClosureTests`. Existing tests are named with their class. Same-tick assertions in the existing tests are the feature (a zero-delay electrical solve), not incidental timing. "Run" is filled with the affected run and the full round.

| Row | Named test | Control | Boundary | Run |
|---|---|---|---|---|
| EL-133 G1, G3 | `BothGateTests.SeparateConditionsAndEnergyDriveActualConsumer` (8 rows; save, load and Reset inside) | `BothGateTests.MissingBottomWireNeverConvertsConditionsIntoEnergy` | rows 00, 01, 10 with supply; 11 without | planned |
| EL-133 G2 | `Closure.ABothGateOutputRetractsWhenOneConditionIsRemovedDuringRunAndReturns` | first phase powered | `BothGateTests.ChainedReconvergenceAndMutualCyclesRetractWithoutOrderDelay` (reversed ids and wiring, source-free loop); `BothGateTests.OpeningPoweredContactRetractsMutualLoopWhileSourcesRemainEnabled` | planned |
| EL-133 G5 | `Closure.AMonotoneGateWiredToItsOwnInputIsAdmittedAndNeverPowersItself(both_gate)` | first 60 ticks unpowered | still off with its second input powered | planned |
| EL-133 G6 | `ComposableSupplyRoutingTests.LogicProvenanceFundsNoWorkWhilePowerSourceFundsWork` | condition battery balance unchanged | `BothGateTests.EnergyProvenanceIgnoresBothConditionSourcesAndUndefinedRulesReject` | planned |
| EL-133 G7, G8 | `Closure.EachGateIdentityIsItsOwnCatalogueEntryWithItsDeclaredOperation(both_gate)`; `BothGatePresentationTests.ThreeNativeLampsFollowCommittedInputsAndSupplyThroughHideAndReset` | undelivered control part keeps a slate lamp | `BothGateTests.CommittedLatchSetResetAndSupplyRestorationDriveBothGate` (three cadences) | planned |
| EL-134 G1, G3 | `ElectricalOrTests.SwitchedConditionsAndSeparateSupplyDriveTheActualConsumer`; `ClosingOneSwitchOpensTheOutputAndSaveLoadAndResetAreExact` | `ElectricalOrTests.TrueRowWithoutSupplyWireNeverCreatesPower` | row 00 with supply | planned |
| EL-134 G2, G5 | `ElectricalOrTests.CommittedReadsFollowEveryRowAndAdmissionIsNeutral`; `Closure.AMonotoneGateWiredToItsOwnInputIsAdmittedAndNeverPowersItself(electrical_or)` | source-free loop unpowered | `ElectricalOrTests.PlacementAndWiringOrderPreserveTruthTableAndConsumerResponse` (order); `ElectricalOrTests.OrLoopIsAdmittedAndSettlesAtItsLeastFixedPointEveryTick` (forgets a removed source) | planned |
| EL-134 G7, G8 | `Closure.EachGateIdentity…(electrical_or)`; `ElectricalOrPresentationTests.PictogramThreeTruthDotsAndThreeLampsFollowCommittedRowsThroughReset` | `ElectricalOrTests.CommittedReadsFollowEveryRowAndAdmissionIsNeutral` | Reset to slate | planned |
| EL-135 G1, G3 | `ElectricalXorTests.SwitchedConditionsAndSeparateSupplyDriveTheActualConsumer`; `ClosingOneSwitchOpensTheOutputAndSaveLoadAndResetAreExact` | `ElectricalXorTests.RemovingTheSupplyOrConditionLinksUnpowersATrueRow` | rows 00 and 11 with supply | planned |
| EL-135 G2 | `ElectricalXorTests.CommittedReadsFollowEveryRowAndAdmissionIsNeutral` (`:343`; five phases changed during Run) | its 00 and 11 phases are off | `ElectricalXorTests.ReconvergenceThroughAnEitherGateKeepsTheOutputOffFromTheFirstTick`; `PlacementAndWiringOrderPreserveTruthTableAndConsumerResponse` | planned |
| EL-135 G4 | `Closure.ANonmonotoneGateWiredToItsOwnInputIsRefusedAtomically(electrical_xor, …)` | same parts without the wire validate | `ElectricalXorTests.ZeroDelayFeedbackThroughTheGateIsRefusedAtomicallyBeforeAnySolve` (two-part loops, open switch, `PowerIn` entry) | planned |
| EL-135 G7, G8 | `Closure.EachGateIdentity…(electrical_xor)`; `ElectricalXorPresentationTests.PictogramTwoTruthDotsAndThreeLampsFollowCommittedRowsThroughReset` | `ElectricalXorTests.CommittedReadsFollowEveryRowAndAdmissionIsNeutral` | Reset to slate | planned |
| EL-136 G1, G3 | `ElectricalNorTests.SwitchedConditionsAndSeparateSupplyDriveTheActualConsumer`; `ClosingOneSwitchRetractsTheOutputAndSaveLoadAndResetAreExact` | `ElectricalNorTests.TrueRowWithoutSupplyWireNeverCreatesPower` | rows 01, 10, 11 with supply | planned |
| EL-136 G2 | `ElectricalNorTests.ReconvergentConditionKeepsTheOutputOffFromTheFirstTick` | `ElectricalNorTests.MonotoneLoopIsAdmittedStaysUnpoweredAndSettlesBeforeTheGateItFeeds` | `ElectricalNorTests.PlacementAndWiringOrderPreserveTruthTableAndConsumerResponse` | planned |
| EL-136 G4 | `Closure.ANonmonotoneGateWiredToItsOwnInputIsRefusedAtomically(electrical_nor, …)` | same parts without the wire validate | `ElectricalNorTests.ZeroDelayFeedbackThroughTheGateIsRefusedAtomicallyBeforeAnySolve` | planned |
| EL-136 G7, G8 | `Closure.EachGateIdentity…(electrical_nor)`; `ElectricalNorPresentationTests.PictogramSingleTruthDotAndThreeLampsFollowCommittedRowsThroughReset` | `ElectricalNorTests.CommittedReadsFollowEveryRowAndAdmissionIsNeutral` | Reset to slate | planned |
| EL-137 G1, G3 | `ElectricalNandTests.SwitchedConditionsAndSeparateSupplyDriveTheActualConsumer`; `ClosingBothSwitchesRetractsTheOutputAndSaveLoadAndResetAreExact` | `ElectricalNandTests.TrueRowWithoutSupplyWireNeverCreatesPower` | row 11 with supply | planned |
| EL-137 G2 | `ElectricalNandTests.ReconvergentConditionRetractsOutputOnTheSameTick` | `ElectricalNandTests.MonotoneLoopIsAdmittedStaysUnpoweredAndSettlesBeforeTheGateItFeeds` | `ElectricalNandTests.PlacementAndWiringOrderPreserveTruthTableAndConsumerResponse` covers row 11 only, in four orders; the other rows' order independence rests on `CommittedReadsFollowEveryRowAndAdmissionIsNeutral` | planned |
| EL-137 G4 | `Closure.ANonmonotoneGateWiredToItsOwnInputIsRefusedAtomically(electrical_nand, …)` | same parts without the wire validate | `ElectricalNandTests.ZeroDelayFeedbackThroughTheGateIsRefusedAtomicallyBeforeAnySolve`; `LongLoopRefusalNamesTheFirstPartsAndCountsTheRest` | planned |
| EL-137 G7, G8 | `Closure.EachGateIdentity…(electrical_nand)`; `ElectricalNandPresentationTests.PictogramTruthDotsAndThreeLampsFollowCommittedRowsThroughReset` | `ElectricalNandTests.CommittedReadsFollowEveryRowAndAdmissionIsNeutral` | Reset to slate | planned |
| AC-N | `git diff --stat` command above; `PartRegistryTests.Load_RegistersEveryBundleElementFromItsPackageInPaletteOrder` | none needed: an added package would change the diff | the baseline's part count before and after (45 at drafting) | planned |

No tolerance is introduced: every assertion in the new tests is a typed state, a typed refusal, a count or a declared identity. The 60-tick windows are observation windows for an absence, chosen as half a second at the default cadence, long enough for a self-latching defect to appear on its first tick.

**Manual checks (if no CLI):**
- Read each edited ledger row against the table above: it names the part, the package path and id, and the tests of its own rows only.

## Spec Change Log

## Review Triage Log

## Auto Run Result
