---
title: 'Table-driven supply, run-control, port and connection routes (S6e)'
type: 'refactor'
created: '2026-11-23'
status: 'draft'
baseline_revision: 'captured at dispatch (the main that contains the planning work)'
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '{project-root}/docs/testing-guidance.md'
  - '{project-root}/_bmad/custom/element-free-engine-design.md'
  - '{project-root}/_bmad/custom/element-free-engine-review.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-s6b-response-codec-read-family-table.md'
warnings: []
deferred: []
---

<intent-contract>

## Intent

**Problem:** The supply network, run controls, ports and connections each route a closed capability enum by hand. `engine/gpu/SupplyRoute.cs` names `SupplyNodeKind` 24 times, 10 of them inside `AdmitNode` (`:172`, complexity 23) and `Resolve` (`:321`, 23), and switches on terminals, logic operations, consumer kinds and run-control lanes; consumer kinds are switched again in the wire codec (`WorkshopWireFlatBuffers.Compiled.cs:663`, `:678`, `:688`), the compiled scene, message validation and the presentation read; run-control lanes in `WorkshopRunControl.cs` (`:12`, `:23`, `:35`, `:57`, `:77`, `:98`, `:122`) and `RunControlBindings.cs`; port domains, directions and roles in `ElementPortCatalogue.Admit` (`:112`) and a role switch (`:176`) covering the 13 roles; connection domains in the engine's connection validator (`WorkshopConnections.cs:96`, an `else if` ladder with one validator per domain, and the role-pair rules `:132-170`) and in the UI (prompt, labels, status text, colour and width); parameter units in an 18-arm label switch. `ENGINE-SUPPLY-DEMAND` (drive consumers, unlimited sources) and the gas, liquid and thermal domains will add values to each, and the first domain added extends the connection validator first.

**Approach:** One static table per enum, keyed by it, in the layer that reads it: supply node kind, supply consumer kind, supply terminal, logic gate operation (its truth function and monotonicity), run-control lane (value kind, slot resolution, bounds rule, parameter binding), run-control value kind, simulation domain, connection domain (validator, direction pair, role rule, prompt, labels, status text, colour and width), port direction, port signal kind and port role (admitted domain and direction, activation terminal), the engine copies of direction and signal kind, and parameter unit (its label). The sites read rows. Behaviour, refusals and the schema's field set are unchanged.

## Boundaries & Constraints

**Always:**
- Every value of `SupplyNodeKind` (4), `SupplyConsumerKind` (3), `SupplyTerminal` (4), `LogicGateOperation` (5), `RunControlLane` (2), `RunControlValueKind` (2), `SimulationDomain` (7 including `None`, a `[Flags]` enum), `WorkshopConnectionDomain` (4), `PortDirection` (3), `PortSignalKind` (2), `PortRole` (13), `WorkshopPortDirection` (3), `WorkshopSignalKind` (2) and `ParameterUnit` (18) has exactly one row in its table. `RunControlLane` and `PortRole` start at 1, and `SimulationDomain` admits a combination only as today (`Mechanical | Electrical` and bit `1 << 6` are refused as `BundlePortsTests.cs:170-190` pins).
- A value with no row answers each site as that site answers it today; the miss branch stays at the site and no lookup helper unifies the refusals. The run log lists, from the baseline, each site that refuses an undefined value with its exception type and message prefix: `LogicGateOperation`: `SupplyRoute.Evaluate` (`:395`) and `SupplyCycles.IsMonotone` (`:19`) throw `ArgumentOutOfRangeException(operation)`, `SupplyRoute.Validate` (`:35-36`) and `AdmitNode` (`:187-189`) throw `ArgumentException` with two different messages; `SupplyConsumerKind`: `ResolveConsumerSlot` (`:252`) `ArgumentException` "Electrical consumer names an undefined drive kind.", `ActuatorDriveRead.Slot` (`PhysicsPresentationRead.cs:55`) `InvalidOperationException` "Undefined actuator drive kind.", `WorkshopCompiledScene.cs:197` and `WorkshopMessages.cs:177` refusal or `false`; `RunControlLane`: `ValueKind` (`:16`) `ArgumentOutOfRangeException`, `Resolves` (`:30`) and `BoundsAdmitted` (`:41`) `false`, `RunControl(row)` (`:109`) `ArgumentException`, `TickControlComposition.For` (`:416`) `ArgumentOutOfRangeException(held)`; `WorkshopConnectionDomain`: `ValidateIdentity` "Unsupported or duplicate connection." as a `WorkshopEditRejectedException`, `ConnectionPrompt` and `DisconnectLabel` `ArgumentOutOfRangeException`; `ParameterUnit`: `UnreachableException` "The admitted schema contains an undefined parameter unit."; ports: today's `ElementPortRejection` reason and text. Existing tests pin some of these (`ElectricalXorTests.cs:313`).
- Supply resolution (powered nodes, contacts, logic outputs, consumer enables), run-control admission and application, port admission, connection validation and choices, and unit labels are unchanged for every delivered package and lesson.
- `engine/gpu/WorkshopActivationCompiler.cs:36-42` (`SignalTerminal` to `PortRole`) and `engine/gpu/ActivationNetwork.cs:131-137` (`PortRole` to `SignalTerminal`) are the two directions of one mapping; S6e owns both as columns of one `PortRole` row, so the two cannot drift.
- Tables are built once from static data and functions; the per-tick supply path allocates nothing new (no measurement of allocation: a per-node row lookup replaces a switch, and the existing `Resolve` allocates per call), and nothing uses reflection or runtime enum enumeration.

**Never:**
- The N-input boolean node, deleting `FirstIn` and `SecondIn` (audit E2), socket glyphs (`SocketGlyph`, E3): owned by `ENGINE-CAPABILITY-INSTANCES`. Drive debit and new consumer or source kinds: `ENGINE-SUPPLY-DEMAND`.
- A change to the wire schema, the schema's field set, element packages or their declared ports; to `WorkshopSupplyCompiler.cs:73` and `:92`, which switch on descriptor record types (not an enum) and stay.
- A second dispatch path beside a table; a default row; a test-only accessor on a production type; suppressions; hashes; mutants; browser runs.

## I/O & Edge-Case Matrix

| Row | Input / State | Expected Output / Behavior | Error Handling |
|-----|--------------|---------------------------|----------------|
| E1 | For each `SupplyNodeKind`, a real supply graph of a delivered part that declares it: source `Battery`, contact `Switch`, consumer `Motor`, logic gate `BothGate`, compiled with `CompiledSceneFixture.Compile` and run through `WorkshopIntegrationHarness` | Admitted; resolution powers and enables as today | No error expected |
| E2 | For each `LogicGateOperation`, the four input combinations, through the delivered gate of that operation (`ElectricalNandTests`, `ElectricalNorTests`, `ElectricalOrTests`, `ElectricalXorTests`, `BothGateTests`) and the public `SupplyCycles.IsMonotone` | The operation's truth table; `IsMonotone` answers for each operation as the existing assertions state (`ElectricalNandTests.cs:291-292`, `ElectricalOrTests.cs:312`, `ElectricalNorTests.cs:293`, `ElectricalXorTests.cs:308`), the expected values being those tests' own | No error expected |
| E3 | For each `SupplyConsumerKind`, a compiled scene of a delivered part with that consumer (`Motor` revolute drive, the Powered gate prismatic drive, `Clutch` transmission), encoded and decoded through the response and command codecs | Round-trips; the consumer is enabled by its supply | No error expected |
| E4 | For each `RunControlLane`, a run-control row of a delivered part in range and one out of bounds, the bounds row differing from the admitted row only in `Minimum` or `Maximum` (same `Owner`, `LaneIndex`, `Source`), so `Resolves` is not what refuses it (`WorkshopCompiledScene.cs:100-105` refuses with one message) | In range admitted and applied to its slot; out of bounds refused | `ArgumentException` of the compiled scene; control: the in-range row |
| E5 | Every `PortRole` (13) crossed with every `SimulationDomain`, `PortDirection` and `PortSignalKind` value taken from `Enum.GetValues`; the expected admitted combinations are the 13 roles' declared domain and direction written out in the test from the `PortRole` definitions (`CuriousContraptions.Abstractions/PortDescriptor.cs:20-33`); a combination with no written expectation fails the case (never skipped) | Admitted to its connection domain, or refused | Today's `ElementPortRejection` (`UnsupportedDomain`, `UnsupportedDirection`, `UndefinedSocket`, `SocketDisagrees`, `SignalKindDisagrees`); control: each delivered package's ports admitted (`PackageLocalPortsTests`); boundary: the `BundlePortsTests.cs:179-181` combinations |
| E6 | For each undefined-value site listed in the second Always bullet, written from the baseline in the test (enum, entry point, undefined value, exception type, message prefix): 0 or one past the last defined value, and for `SimulationDomain` also `Mechanical | Electrical` and `1 << 6` | Refused at the site as before | The type and message the list names; reuse `BundlePortsTests.cs:170-190` for the port cases |
| E7 | For each `WorkshopConnectionDomain`, through `WorkshopConnections.Validate`: a real connection between two delivered parts of that domain, and the same connection with the role pair swapped or wrong; the prompt, disconnect label, status text, colour and width the UI reads from the row (`Workshop.ConnectionPrompt`, `Workshop.DisconnectLabel`, existing statics the tests already reach) | The real connection admitted; the wrong pair refused with the domain's message ("Unsupported activation output.", "Unsupported electrical endpoint.", "Unsupported rope socket.", "Unsupported mechanical socket."); the texts equal the independently written strings | `WorkshopEditRejectedException` `InvalidConnection` (activation level to non-level input: `LevelDrivesLevelInputOnly`) |
| E8 | For each `ParameterUnit`, its label | The independently written label (`""`, `m`, `kg`, `s` and the rest as `WorkshopConfiguration.cs:255-` writes them) | The undefined unit: `UnreachableException` as today |

</intent-contract>

## Code Map

Baseline: captured at dispatch (coordinator decision 38, 23 November 2026): the published main that contains the planning work and the slices before this one; scope guards compare against it. Drafted on `25df3cd5`, the drafting revision only, not a baseline; every line cited here was read there on 24 November 2026. **Status: draft; runs before the engine programme** (owner decision 24h, 23 November 2026). Prerequisites: S6d published (both edit `ActivationNetwork.cs` and `WorkshopActivationCompiler.cs`). Review tier: **engine**. Delivery shape: one structural slice, the supply, control, port and connection rows of the S6 follow-on inventory (`spec-s6b-response-codec-read-family-table.md`); the fifth audit judged it large (about 30 production files across Abstractions, Bundle, engine and ui) and the coordinator keeps it one slice; if the first review pass finds it too large, the split is supply and run control (E1 to E4) versus ports, connections and units (E5 to E8), with the same acceptance.

Planning verdict: replanned at dispatch from the main that contains the previous S6 slice (coordinator decision 43); sixth-audit findings in `docs/planning/elements/readiness/audits/audit-s6e-6.md` carried into that replanning.

Owned scope (complexity at threshold 12, 23 November 2026, in brackets):
- `engine/gpu/SupplyRoute.cs` -- `Validate` `:22` [16], `ValidateEdges` `:44` [18] (`SupplyTerminal` at `:52`), `.ctor` `:107` [16], `AdmitNode` `:172` [23], `ResolveConsumerSlot` `:233` [14] (`SupplyConsumerKind` at `:240`), `AdmitEdges` `:260` (a second copy of the edge rule at `ValidateEdges :44-55`: read the same node-kind and terminal rows, one rule, not two), `Resolve` `:321` [23], the `:371` node-kind switch, the `:389` 5-arm `LogicGateOperation` switch and the `:412` `RunControlLane` switch -- read rows. The `:381` `PrismaticCommand` switch stays: `PrismaticCommand` is a drive's per-tick decided state, excluded in the inventory.
- `CuriousContraptions.Abstractions/SupplyCycles.cs:13` (5-arm operation switch), `CuriousContraptions.Abstractions/SupplyContractValues.cs:58` (`SupplyTerminals.Admits`), `CuriousContraptions.Abstractions/PhysicsPresentationRead.cs:34`, `:55`, `:62` (`Validate`, an `is not (A or B)` ladder on `SupplyConsumerKind`) -- rows.
- `engine/gpu/WorkshopSupplyCompiler.cs` -- not owned: `:73` and `:92` switch on descriptor record types and stay (the `CoordinateKind` property patterns at `:94-97` are S6g's); `CuriousContraptions.Bundle/SupplyBundleValidation.cs:58`, `:87` (descriptor-type dispatch to supply capabilities) -- read the node-kind and terminal rows; `:183-190` (driver payload checks) is S6c's.
- `engine/gpu/WorkshopCompiledScene.cs:117`, `:197`, `:279` (`InvalidActuatorBinding`, a `SupplyConsumerKind` ladder; the member's driver rules are S6c's), `:334` (the `SupplyTerminal` test in `InvalidBindingEvaluation`), `engine/gpu/WorkshopMessages.cs:177` -- rows.
- `engine/gpu/WorkshopWireFlatBuffers.Compiled.cs:663-693` -- the three consumer-kind switches read the consumer row (identity field and constructor); the schema's field set is unchanged.
- `engine/gpu/WorkshopRunControl.cs:12`, `:23`, `:35`, `:57` (`Slot`), `:77` (`RunControl(read, owner, lane)`), `:98`, `:122` (`Find`) -- the lane table; `CuriousContraptions.Bundle/RunControlBindings.cs:13` (`Lanes`: the lane's parameter binding) -- the lane row; `CuriousContraptions.Abstractions/RunControlValues.cs:37`; `ui/WorkshopConfiguration.cs:110`, `:120`, `:149` -- the value-kind table.
- `ui/WorkshopConfiguration.cs:255` `ParameterUnits` -- the unit label table (`ParameterUnit` row: label), kept in the same file: the label is UI text, and the test reaches the `internal static` member as `ReverseTransmissionArtTests` reaches `Workshop.ConnectionPrompt`.
- `engine/gpu/ElementPortCatalogue.cs` -- `Admit` `:112` [16] (domain `:120`, direction `:128`, signal kind `:136`), the role switch `:176` and `:173-191`; `engine/gpu/ActivationNetwork.cs:131-137` and `engine/gpu/WorkshopActivationCompiler.cs:36-42` (the one terminal mapping, both directions); `CuriousContraptions.Bundle/BundleValidator.cs:39` (the domain mask over `AllDomains` `:14`), `MechanicalPort :373` [18] (role and direction `:377-381`) and `Port :520` [25] (signal-kind rule, rope roles `:543-551`); `CuriousContraptions.Abstractions.Signal/SignalContracts.cs:17` is S6d's. `CuriousContraptions.Abstractions/EventDescriptor.cs:51` (event kind to domain) is keyed by `SimulationEventKind` and owned by S6d; read only here.
- `engine/gpu/WorkshopConnections.cs` -- `Validate` `:96` [15] (the `else if` ladder, and the Rope direction rule at `:107-108`), `ValidateIdentity :132`, `ValidateActivation :140`, `ValidateElectrical :153`, `ValidateRope :161`, `ValidateMechanical :170` -- the connection-domain row: validator, direction pair, role rule. `ui/WorkshopConnections.cs:151-154` (status text), `:195-198` (colour and width), `:236` `ConnectionPrompt`, `:244` `DisconnectLabel` -- the same row's UI columns (prompt, disconnect label, connected and disconnected status text, drawn or not, colour, width); `ChangeConnectionAsync :125`, `RefreshConnectionArtwork :170` and `RefreshConnectionChoices :60` stay as sequences and read the row. The glyph switch `:34` is excluded (`SocketGlyph`, E3).
- `CuriousContraptions.tests/SupplyControlPortRouteTests.cs` -- new: E1 to E8.
- `docs/planning/invest/delivery-batches.md` (story S6), `deferred-work.md`, `TODO.md`, `sprint-status.yaml` (key `s6e-supply-control-port-route-tables`), this contract and its run log. Documents that cite line numbers of the changed files are re-read after publication: 35 files cite `SupplyRoute.cs:` and 14 cite `ElementPortCatalogue.cs:` (for example `EL-137-electrical-nand-gate.md:39-41`, `CAT-013-both_gate.md:92`, `CAT-005-battery.md:93`); the coordinator re-reads them, not this slice.

**Rows and layers (decided).** The sites live in four projects, so layering decides the design: a Bundle or Abstractions member cannot read an engine table (E9 `UnbalancedModel`; a Bundle-to-engine reference is also impossible by project references). An implementer adds a column only with a site that reads it.

| Enum | Layer | Row columns |
|---|---|---|
| `LogicGateOperation` | Abstractions | `Evaluate(first, second)`, `Monotone` (read by `SupplyCycles.cs:13`, `SupplyRoute.cs:389`, `SupplyBundleValidation.cs:174`) |
| `SupplyTerminal` | Abstractions | `AdmittedBy(node descriptor kind)`, `ReadFrom(logic read)` (`SupplyContractValues.cs:58`, `PhysicsPresentationRead.cs:34`) |
| `SupplyConsumerKind` | Abstractions; engine/gpu | Abstractions: `IsActuatorDrive`, `Slot(drive)`. engine/gpu: `OwnedBy(physics, consumer, owner)`, `AdmitSlot`, wire identity field, wire decode |
| `SupplyNodeKind` | engine/gpu | `CarriesSource`, `CarriesOperation`, `MayStartEdge`, `MayEndEdge`, `AcceptsLogicTerminals`, `Admit`, `Active` (`Source` has no `Active`: it keeps "Unsupported electrical node.") |
| `RunControlValueKind` | Abstractions; ui | `IsCanonical`; ui: label and toggle |
| `RunControlLane` | engine/gpu behind `IRunControlServices` (`CuriousContraptions.Abstractions/RunControlServices.cs`); Bundle: `BindsParameter` | `ValueKind`, `Resolves`, `BoundsAdmitted`, `Slot`, `ReadValue`, `Apply` |
| `PortRole` | Abstractions | `Domain`, `Direction`, activation `Terminal` and its inverse (the one terminal mapping above), `Connects(role)` (read by `ElementPortCatalogue.cs:176`, `BundleValidator.cs:373`, `:520`, `WorkshopConnections.cs:140-184`, `ActivationNetwork.cs:134`) |
| `SimulationDomain` | Abstractions | `ConnectionDomain` or unsupported, `CarriesSignalKind`, `RopeAttachment` (`ElementPortCatalogue.cs:120`, `BundleValidator.cs:39`, `:520`) |
| `PortDirection`, `PortSignalKind` | Abstractions | the engine counterpart (`WorkshopPortDirection`, `WorkshopSignalKind`), a two- or three-row conversion |
| `WorkshopConnectionDomain` | engine/gpu and ui | validator, direction pair, prompt, labels, status text, drawn, colour, width |
| `ParameterUnit` | ui | label |

Every table is built once and read with `TryGetValue`; a miss answers as the second Always bullet lists. `CuriousContraptions.Core/RuntimeSources.props` (its compile list is explicit; `EnableDefaultCompileItems` is false) lists a new table file under `engine/gpu/` in the same candidate.

Direct consumers: every member keeps its signature. `CuriousContraptions.web/wwwroot/workshop-observation.ts` mirrors domains and roles by value: `:30` (`SimulationDomain`, missing `Rope`), `:41` (`WorkshopConnectionDomain`, missing `Mechanical`), `:48` (`PortRole`), `:105` (`SupplyConsumerKind`, missing `MechanicalTransmission`); the drift is audit E13, owned by `ENGINE-CAPABILITY-INSTANCES`, and the file is unchanged here. `spec-engine-goal-evaluation.md` and the `ENGINE-SUPPLY-DEMAND` story text cite `SupplyRoute.cs` lines; both are after the programme and re-read them.

### Capability probe

All routes exist; nothing in a solver law, the worker ABI, the wire schema or a shared presentation contract changes.

- **Present:** sources, contacts, consumers -- `ElectricalSupplyTests`, `ComposableSupplyRoutingTests`, `SwitchElectricalTests`, `LatchSupplyFixtureTests`; logic gates and their monotonicity -- `ElectricalNandTests`, `ElectricalNorTests`, `ElectricalOrTests`, `ElectricalXorTests`, `BothGateTests`; consumer kinds -- `MotorTests` (revolute drive), `ClutchTests` (transmission), the Powered gate tests in `BothGateTests` and `LatchSupplyFixtureTests` (prismatic drive); compiled-scene codec -- `CompiledSceneBoundaryTests`; run controls -- `RunControlTableTests`, `MotorTests` (run-time speed); ports -- `BundlePortsTests`, `PackageLocalPortsTests`; connections -- `ReverseTransmissionArtTests` (prompt and label), the connection tests of the Workshop edit path.
- **Present, untested:** that every value of the fourteen enums has exactly one row, the five-operation truth table as one theory, the connection validator per domain with its own messages, the unit labels and the independent port oracle. This slice adds E1 to E8.

### Decisions

- **Ports, domains and connections belong here (decided).** Port admission and connection validation route the same supply and signal domains and have no other S6 home; the inventory assigns them to this slice.
- **Rows keep today's terminals (decided).** Two-input logic stays until `ENGINE-CAPABILITY-INSTANCES` replaces it; the operation row holds today's truth function and monotonicity.
- **One owner of the terminal mapping (decided; coordinator decision 42 and the fifth audit of S6d and S6e).** S6e owns both directions; S6d keeps `WorkshopActivationCompiler.cs:51-70` and drops `:36` and `:134`.
- **Parameter units are in scope (decided).** Electrical added `Joule` and `Watt`; gas, liquid and thermal add units; the label row is the one place.
- **Single-member comparisons are not chains (decided).** `x.Kind == E.V` guarding one branch stays; the run log lists each kept with file:line (examples: `WorkshopSceneCompiler.cs:86`, `DeclaredPhysics.cs:128`, `SupplyRoute.cs:286`, `:431`).

### Concurrency

After S6d; before S6f and S6g. S6c and S6d publish first and S6e rebases on them (one publisher at a time): S6c edits `BundleValidator.cs`, `WorkshopCompiledScene.cs` (`:231-356`) and `SupplyBundleValidation.cs:183-190`; S6d edits `WorkshopMessages.cs:32`, `ActivationNetwork.cs` and `WorkshopActivationCompiler.cs:51-70`. S6g edits `WorkshopWireFlatBuffers.Compiled.cs:65-77` in the same file as `:663-693`; this slice publishes first and S6g re-reads. Repair parts A to C edit package scenes only. The new test file is staged with `git add` before the candidate commit.

## Tasks & Acceptance

**Execution:**
1. `.tools/s6e-evidence/` -- the S6 measurement on the baseline (configuration `CA1502: 12`, judged against 14) -- before; the hot-path measurement of the S6e reference scene on the baseline; the undefined-value site list of the second Always bullet written from the baseline.
2. `engine/gpu/SupplyRoute.cs`, `CuriousContraptions.Abstractions/SupplyCycles.cs`, `SupplyContractValues.cs`, `PhysicsPresentationRead.cs`, `CuriousContraptions.Bundle/SupplyBundleValidation.cs`, `engine/gpu/WorkshopCompiledScene.cs`, `WorkshopMessages.cs`, `WorkshopWireFlatBuffers.Compiled.cs` -- supply tables -- E1 to E3.
3. `engine/gpu/WorkshopRunControl.cs`, `CuriousContraptions.Bundle/RunControlBindings.cs`, `CuriousContraptions.Abstractions/RunControlValues.cs`, `ui/WorkshopConfiguration.cs` -- control tables and the unit label -- E4, E8.
4. `engine/gpu/ElementPortCatalogue.cs`, `ActivationNetwork.cs`, `WorkshopActivationCompiler.cs`, `WorkshopConnections.cs`, `CuriousContraptions.Bundle/BundleValidator.cs`, `ui/WorkshopConnections.cs` -- port, connection and domain tables -- E5, E7.
5. `CuriousContraptions.tests/SupplyControlPortRouteTests.cs` -- the tests in the table; E6.
6. `.tools/s6e-evidence/`, planning records -- after-measurements; result.

**Acceptance Criteria:**
- AC1: Given the measurement, when it runs on the candidate, then no changed or added member reports CA1502 above its baseline value, and `SupplyRoute.AdmitNode`, `SupplyRoute.Resolve` and `BundleValidator.Port` each report at least 3 below their baseline value, or no line.
- AC2: Given the maintainability gate, when it runs, then both canaries pass and violations are 0.
- AC3: Given the full local round, when it completes, then failures are 0 and skipped are 0 in every project, and the total is the baseline round plus 72 (E1 4, E2 5, E3 3, E4 4: two lanes, each in range and out of bounds, E5 13: one per `PortRole`, each over the full cross product, E6 17: the 4 `LogicGateOperation`, 4 `SupplyConsumerKind`, 5 `RunControlLane`, 3 `WorkshopConnectionDomain` and 1 `ParameterUnit` sites of the second Always bullet, the port sites reusing `BundlePortsTests`, E7 8: 4 domains times admitted and refused, E8 18), each case named in the run log.
- AC4: Given the hot-path rule of the inventory (S6b), when the S6e reference scene is measured on the baseline and the candidate, then the candidate's median is not above the slowest baseline run. Reference scene: one scene of a Battery, a Switch, a BothGate, a Motor and a Clutch (the E1 and E3 parts) wired as the delivered lessons wire them, 10,000 ticks with the switch toggling every 100 ticks, through the supply route's `Resolve`.

## Spec Change Log

## Review Triage Log

## Design Notes

1. Behaviour and scope: no behaviour change; supply, control, port and connection routing becomes data (story S6, owner decision 24h).
2. Reusable primitive: one row per enum value; `ENGINE-SUPPLY-DEMAND` and the fluid domains add rows, and a new domain adds one connection-domain row instead of an `else if`.
3. Declared composition: packages declare nodes, ports and controls; tables key generic enums only.
4. Execution and ownership: same supply resolution order; immutable static tables. No measurement of allocation beyond AC4: a per-node row lookup replaces a switch, and the existing `Resolve` allocates per call.
5. Generality: theories over every value of each enum on real delivered parts; an added value without a row, or an expectation the test does not write, fails the case.
6. Forward-only, deleted: the ladders and switches listed in owned scope; nothing kept beside the tables.
7. Prerequisites: S6d. E1, E2, E3, E4, E5, E7, E8, E9 and D2, D4, D5 apply; E6 and D6, D7 unaffected.

## Verification

Executor: implementation owner. Environment: `PATH=~/.local/share/tim-toolchain/dotnet-10.0.403:~/.local/share/tim-toolchain/node-v26.12.1-darwin-arm64/bin:$PATH`, `DOTNET_ROOT=~/.local/share/tim-toolchain/dotnet-10.0.403`. The measurement runs before the compiler-log captures.

**Commands:**
- The S6 measurement with `CA1502: 12` in its configuration, judged against 14 -- expected before: the bracketed values above; after: AC1.
- The five compiler-log capture commands -- expected: every build succeeds.
- `dotnet run --file tools/anvil/VerifyPolicy.cs -- --maintainability --project-root .` -- expected: AC2.
- The registration check filter (a test file is added) -- expected: 0 failed, 0 skipped.
- `dotnet test CuriousContraptions.slnx -c Debug -f net10.0 --no-build --no-restore --nologo -noAutoResponse --filter "FullyQualifiedName~SupplyControlPortRouteTests|FullyQualifiedName~ElectricalSupplyTests|FullyQualifiedName~ComposableSupplyRoutingTests|FullyQualifiedName~SwitchElectricalTests|FullyQualifiedName~LatchSupplyFixtureTests|FullyQualifiedName~ElectricalNandTests|FullyQualifiedName~ElectricalNorTests|FullyQualifiedName~ElectricalOrTests|FullyQualifiedName~ElectricalXorTests|FullyQualifiedName~BothGateTests|FullyQualifiedName~MotorTests|FullyQualifiedName~ClutchTests|FullyQualifiedName~CompiledSceneBoundaryTests|FullyQualifiedName~RunControlTableTests|FullyQualifiedName~BundlePortsTests|FullyQualifiedName~BundleValidatorTests|FullyQualifiedName~PackageLocalPortsTests|FullyQualifiedName~ReverseTransmissionArtTests"` -- expected: 0 failed, 0 skipped.
- `git add -N` on new files, then `git diff --check <baseline_revision>` -- expected: clean.
- One full local round -- expected: AC3.

**Manual checks (if no CLI):**
- A chain is a switch statement or expression, or an `==`, `!=` or `is` ladder, in one member that names two or more distinct members of one of the fourteen enums. After the candidate, the run log lists every such member outside a table's own file; the list is empty. A single-member comparison is not a chain; the run log lists each one kept with file:line.

### Acceptance-to-test table

No tolerance is chosen: assertions are enables, truth values, refusal reasons, strings and decoded fields.

| Rows | Named discriminating test | Control and boundary | Execution |
|---|---|---|---|
| E1 | `SupplyControlPortRouteTests.EverySupplyNodeKindAdmitsAndResolves` (theory over `SupplyNodeKind`, the part per kind named in the matrix) | Control: an open contact leaves its consumer unpowered. Boundary: a cyclic component settles at its least fixed point | planned |
| E2 | `SupplyControlPortRouteTests.EveryLogicOperationGivesItsTruthTable` (theory over `LogicGateOperation` times four inputs) | Control: an unpowered gate outputs nothing whatever its inputs | planned |
| E3 | `SupplyControlPortRouteTests.EveryConsumerKindRoundTripsAndIsEnabled` | Boundary: the transmission consumer, whose identity is a transmission id, not a joint | planned |
| E4 | `SupplyControlPortRouteTests.EveryRunControlLaneAdmitsItsBoundsAndDrivesItsSlot` | Boundary: a revolute target at plus and minus `AngularVelocity.MaximumSpeed` admitted, beyond refused (differing only in `Minimum` or `Maximum`); an Enable lane with non-zero bounds refused | planned |
| E5 | `SupplyControlPortRouteTests.EveryPortRoleDomainDirectionAndSignalKindIsAdmittedOrRefusedAsDeclared` | Control: a delivered package's ports admitted. Boundary: a domain the engine does not connect refused `UnsupportedDomain` | planned |
| E6 | `SupplyControlPortRouteTests.UndefinedValuesAreRefusedAtEverySite` (theory over the site list written from the baseline) | Boundary: one past each last defined value; `SimulationDomain` combinations | planned |
| E7 | `SupplyControlPortRouteTests.EveryConnectionDomainValidatesAndTalksAsToday` | Control: the real connection admitted. Boundary: the level-to-non-level activation connection refused with `LevelDrivesLevelInputOnly` | planned |
| E8 | `SupplyControlPortRouteTests.EveryParameterUnitHasItsLabel` | Boundary: `RadianPerSecondPerNewton`, the last unit | planned |
| AC1 to AC4 | Measurement, gate, full round, hot-path comparison | Baseline values and the five runs in the run log | planned |
