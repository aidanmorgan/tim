# Planning audit: `spec-s6e-supply-control-port-route-tables.md` (S6e), first audit (file 5)

Audited 24 November 2026 in the `spec-drafts` checkout at main `25df3cd5`, planning phase, read-only; nothing built or run. Contract: 1,719 words. Binding decisions: `owner-decisions-2026-11-22.md` (22, 24h, 37, 38). Complexity numbers were checked against the coordinator's existing threshold-12 log (`scratchpad/s6-metrics.log`, 370 members, 23 November), not re-measured. Chain scans used ast-grep 0.51.3 (switch statements and expressions, then every production member naming two or more distinct members of one enum in a `==`, `!=`, `is`/`or`/`not` pattern, `case` label or switch arm; `elements/`, tests, tools, generated code and `obj/` excluded). Constructor and `Add(...)` arguments were filtered out.

## Verdict: Fail

The approach is right and most citations are exact, but the contract cannot be implemented as written and would not close its part of S6: it never says what a row holds or where each table lives (the sites span Abstractions, Bundle, engine and ui, so layering decides the design), it leaves unowned the engine-side connection-domain validator, the one chain a new domain extends first, and its refusal and port tests cannot discriminate. Two intent-contract count errors must be corrected now because the intent becomes read-only.

## Blocking findings

### B1. Chains over S6e's own enums that no slice owns, and a manual check that cannot pass (wrong specified behaviour; closure under 24h)

Contract `:55-65` (owned scope), `:65` (UI exclusion), `:125-126` (manual check). Measured at `25df3cd5` (members naming two or more distinct members of the enum):

| Site | Enum | Distinct members | Why it is a chain |
|---|---|---|---|
| `engine/gpu/WorkshopConnections.cs:96` `Validate` [15], `:119-126` | `WorkshopConnectionDomain` | 4 | `if/else if` ladder, one validator per domain; also `:107-108` Rope direction rule. Not in any contract: S6e names only the `ui/` file of the same name |
| `engine/gpu/WorkshopConnections.cs:132` `ValidateIdentity`, `:140` `ValidateActivation`, `:153` `ValidateElectrical`, `:161` `ValidateRope`, `:170` `ValidateMechanical` | `WorkshopConnectionDomain`, `PortRole` (4, 3, 2), `SupplyTerminal` | 2 to 4 | role pairs a connection may join, per domain |
| `engine/gpu/SupplyRoute.cs:260` `AdmitEdges` | `SupplyNodeKind` (3), `SupplyTerminal` (3) | 3 | a second copy of the edge rule at `ValidateEdges :44-55`; only the first copy is cited |
| `CuriousContraptions.Abstractions/SupplyContractValues.cs:58` `SupplyTerminals.Admits` | `SupplyTerminal` | 2 | terminals each descriptor admits |
| `engine/gpu/WorkshopRunControl.cs:57` `Slot`, `:77` `RunControl(read, owner, lane)`, `:122` `Find` | `RunControlLane` | 2 each | `if (lane == ...)` ladders; only `:12 :23 :35 :98` are cited |
| `CuriousContraptions.Bundle/RunControlBindings.cs:13` `Lanes` | `RunControlLane` | 2 | per-lane binding rule |
| `CuriousContraptions.Abstractions/PhysicsPresentationRead.cs:62` `Validate`; `engine/gpu/WorkshopCompiledScene.cs:279` `InvalidActuatorBinding` | `SupplyConsumerKind` | 2 | `is not (A or B)` ladders next to cited switches |
| `ui/WorkshopConnections.cs:151-154` (status text), `:195-198` (colour and width) | `WorkshopConnectionDomain` | 3 to 4 | nested ternaries per domain; the contract excludes the whole file as "UI sequences" but takes `ConnectionPrompt` and `DisconnectLabel` (`:236`, `:244`), which are the same kind of per-domain data |

Also wrong in scope: `engine/gpu/WorkshopSupplyCompiler.cs:73` (and the target switch at `:92`) switch on descriptor record types (`SupplySourceDescriptor` and so on) and produce a kind. They are not keyed by a closed enum, so no table keyed by `SupplyNodeKind` can replace them. The contract lists `:73` as "rows".

Consequence: after S6e the repository still has per-domain and per-lane `else if` ladders that the next domain (gas, liquid) must extend, S6f's AC4 ("every in-scope enum dispatched only inside its table") cannot pass at S6e's enums, and the manual check `:125-126` ("no enum ladder or switch of the eleven enums remains") is false for any implementation that follows the contract.

Fix (contract text):
- Owned scope, add: `engine/gpu/WorkshopConnections.cs` `Validate :96`, `ValidateIdentity :132`, `ValidateActivation :140`, `ValidateElectrical :153`, `ValidateRope :161`, `ValidateMechanical :170` (domain row: validator, direction pair, role rule); `engine/gpu/SupplyRoute.cs` `AdmitEdges :260` (read the same node-kind and terminal rows as `ValidateEdges`; one rule, not two); `SupplyContractValues.cs:58`; `WorkshopRunControl.cs` `Slot :57`, `RunControl :77`, `Find :122`; `RunControlBindings.cs` `Lanes :13`; `PhysicsPresentationRead.cs:62`; `WorkshopCompiledScene.cs:279`; `ui/WorkshopConnections.cs:151-154` and `:195-198` into the connection-domain row (prompt, disconnect label, connected and disconnected status text, drawn or not, colour, width).
- Owned scope, delete: `engine/gpu/WorkshopSupplyCompiler.cs:73`. Add to the Code Map one sentence: "`WorkshopSupplyCompiler.cs:73` and `:92` switch on descriptor types, not on an enum, and stay."
- Replace the manual check with: "Define a chain as a switch statement or expression, or an `==`, `!=` or `is` ladder, in one member that names two or more distinct members of one of the eleven enums. After the candidate, the run log lists every such member outside a table's own file; the list is empty. A single-member comparison (`x.Kind == E.V` guarding one branch) is not a chain; the run log lists each one kept with file:line (examples: `WorkshopSceneCompiler.cs:86`, `DeclaredPhysics.cs:128`, `SupplyRoute.cs:286`, `:431`)."
- Replace `:65` "UI sequences, not enum chains (excluded ...)" with "`ChangeConnectionAsync`, `RefreshConnectionArtwork` and `RefreshConnectionChoices` stay as sequences; their per-domain text, colour and width read the domain row."

### B2. The contract never states what a row holds or where a table lives (wrong specified behaviour: not Actionable or Sufficient)

Contract `:24` (Approach), tasks `:86-91`. The Approach names eleven enums and, in brackets, the columns of two (`LogicGateOperation`, `RunControlLane`). Tasks 2 to 4 say "supply tables", "control tables", "port and domain tables". Without columns the implementer designs the tables, AC1 (three members at least 3 below) cannot be reasoned about, and "adding a capability touches one table row" (story S6 acceptance) is unchecked. Layering is the bigger gap: the sites live in four projects, and a Bundle or Abstractions member cannot read an engine table (balanced-coupling gate, E9: `UnbalancedModel`; a Bundle to engine reference is also impossible by project references). Examples: `SupplyCycles.cs:13`, `PhysicsPresentationRead.cs:34 :55` and `RunControlValues.cs:37` are in Abstractions; `RunControlBindings.cs` and `BundleValidator.cs` are in Bundle; `SupplyRoute.cs`, `ElementPortCatalogue.cs`, `WorkshopRunControl.cs` are engine/gpu; `ui/*` is ui. Production files in `engine/gpu/` and `CuriousContraptions.Simulation/` also compile through the explicit list in `CuriousContraptions.Core/RuntimeSources.props` (`EnableDefaultCompileItems` is false), which the contract does not name.

Fix: add a Code Map subsection "Rows and layers" with this table (columns derived from the cited sites; the implementer may add a column only with a site that reads it):

| Enum | Layer | Row columns |
|---|---|---|
| `LogicGateOperation` | Abstractions | `Evaluate(first, second)`, `Monotone` (read by `SupplyCycles.cs:13`, `SupplyRoute.cs:389`, `SupplyBundleValidation.cs:174`) |
| `SupplyTerminal` | Abstractions | `AdmittedBy(node descriptor kind)`, `ReadFrom(logic read)` (`SupplyContractValues.cs:58`, `PhysicsPresentationRead.cs:34`) |
| `SupplyConsumerKind` | Abstractions; engine/gpu | Abstractions: `IsActuatorDrive`, `Slot(drive)`. engine/gpu: `OwnedBy(physics, consumer, owner)`, `AdmitSlot`, wire identity field, wire decode |
| `SupplyNodeKind` | engine/gpu | `CarriesSource`, `CarriesOperation`, `MayStartEdge`, `MayEndEdge`, `AcceptsLogicTerminals`, `Admit`, `Active` (Source has no `Active`: it keeps `Unsupported electrical node.`) |
| `RunControlValueKind` | Abstractions; ui | `IsCanonical`; ui: label and toggle |
| `RunControlLane` | engine/gpu behind `IRunControlServices` (`CuriousContraptions.Abstractions/RunControlServices.cs`); Bundle: `BindsParameter` | `ValueKind`, `Resolves`, `BoundsAdmitted`, `Slot`, `ReadValue`, `Apply` |
| `PortRole` | Abstractions | `Domain`, `Direction`, activation `Terminal`, `Connects(role)` (read by `ElementPortCatalogue.cs:176`, `BundleValidator.cs:373 :520`, `WorkshopConnections.cs:140-184`, `ActivationNetwork.cs:134`) |
| `SimulationDomain` | Abstractions | `ConnectionDomain` or unsupported, `CarriesSignalKind`, `RopeAttachment` (`ElementPortCatalogue.cs:120`, `BundleValidator.cs:39 :520`) |
| `PortDirection`, `PortSignalKind` | Abstractions | engine counterpart (`WorkshopPortDirection`, `WorkshopSignalKind`) |
| `WorkshopConnectionDomain` | engine/gpu and ui | validator, direction pair, prompt, labels, status text, drawn, colour, width |

State the indexing for each (`SimulationDomain` is `[Flags]` with `None`; `RunControlLane` and `PortRole` start at 1) and that a miss throws the same exception as today's default arm, not a default row. Add `CuriousContraptions.Core/RuntimeSources.props` to the direct consumers: a new table file under `engine/gpu/` is added there in the same candidate.

### B3. E6 and the Always clause promise one refusal per enum; today each site refuses differently (test)

Contract `:47` (E6), `:29`, test row `:139`. "Today's refusal type and message" is per site. Measured at `25df3cd5`: `LogicGateOperation` undefined: `SupplyRoute.Evaluate :395` and `SupplyCycles.IsMonotone :19` throw `ArgumentOutOfRangeException(operation)`; `SupplyRoute.Validate :35-36` and `AdmitNode :187-189` throw `ArgumentException` with two different messages. `SupplyConsumerKind` undefined: `ResolveConsumerSlot :252` `ArgumentException` "Electrical consumer names an undefined drive kind."; `ActuatorDriveRead.Slot` (`PhysicsPresentationRead.cs:55`) `InvalidOperationException` "Undefined actuator drive kind."; `WorkshopCompiledScene.cs:197` and `WorkshopMessages.cs:177` fall to a refusal or `false`. `RunControlLane`: `ValueKind :16` `ArgumentOutOfRangeException`; `Resolves :30` and `BoundsAdmitted :41` return `false`; `RunControl(row) :109` `ArgumentException`; `TickControlComposition.For :416` `ArgumentOutOfRangeException(held)`. Existing tests pin some of these (`ElectricalXorTests.cs:313`). One `UndefinedValuesAreRefused` assertion per enum cannot check type and message, and a lookup helper that unifies the refusal breaks the existing pins.

Fix: replace the E6 row text with "For each enum and each site that refuses an undefined value, the refusal type and message listed in the run log from the baseline are unchanged; the miss branch stays at the site." Replace the E6 test with `UndefinedValuesAreRefusedAtEverySite` (theory over a site list written in the test from the baseline: enum, site entry point, undefined value, exception type, message prefix; undefined values: 0 or one past the last defined, and for `SimulationDomain` also `Mechanical | Electrical` and `1 << 6`). Reuse `BundlePortsTests.cs:170-190` (existing undefined direction, audio domain and combined domain refusals) as the port cases instead of new ones.

### B4. E5 has no independent oracle and omits two of the five refusals (test)

Contract `:46`, `:138`. "Admitted to its connection domain, or refused" is the table's own content unless the expectation comes from elsewhere. The refusal column lists `UnsupportedDomain`, `UnsupportedDirection`, `UndefinedSocket` and leaves out `SocketDisagrees` (role against domain and direction, `ElementPortCatalogue.cs:173-189`) and `SignalKindDisagrees` (`:136-146`), the two refusals the role and signal-kind tables decide.

Fix: E5 error column: add `SocketDisagrees`, `SignalKindDisagrees`. Test text: "expected admitted combinations are the 13 roles' declared domain and direction written out in the test from the `PortRole` definitions (`CuriousContraptions.Abstractions/PortDescriptor.cs:20-33`), crossed with every `SimulationDomain`, `PortDirection` and `PortSignalKind` value taken from `Enum.GetValues`; a combination with no written expectation fails the case (never skipped). Control: each delivered package's ports admitted (`PackageLocalPortsTests`). Boundary: the `BundlePortsTests.cs:179-181` combinations."

### B5. Intent-contract count and location errors (wrong specified behaviour; the intent becomes read-only)

Replacement text for the intent and Code Map, consolidated:
- `:22` "compares `SupplyNodeKind` 16 times inside `AdmitNode` (`:172`, complexity 23) and `Resolve` (`:321`, 23)": `rg -o 'SupplyNodeKind\.\w+' engine/gpu/SupplyRoute.cs | wc -l` is 24; `AdmitNode` holds 4 and `Resolve` 6. Write "names `SupplyNodeKind` 24 times in `engine/gpu/SupplyRoute.cs`, 10 of them inside `AdmitNode` (`:172`, complexity 23) and `Resolve` (`:321`, 23)".
- `:22` "a 13-arm role switch (`:176`)": 7 arms covering the 13 roles. Write "a role switch (`:176`) covering the 13 roles" (a wording nit).
- `:61` "`BundleValidator.cs:14` (domain switch) and `:377` (roles, inside `Port` `:520` [25])": `:14` is the `AllDomains` constant used by the check at `:39`; `:377` is in `MechanicalPort` (`:373`, [18]); the role rule inside `Port` is the rope block at `:543-551`. Write "`BundleValidator.cs:39` (the domain mask over `AllDomains` `:14`), `MechanicalPort :373` [18] (role and direction `:377-381`) and `Port :520` [25] (signal-kind rule, rope roles)".
- `:61` "`SupplyRoute.cs :371` node and terminal switch": `:371` is the node-kind switch; terminals appear inside its arms. Write "the `:371` node-kind switch".
- `:65` "`workshop-observation.ts:12`, `:41` mirror domains": `:12` is `AnimationOutputKind`. Mirrors are `:30` (`SimulationDomain`, missing `Rope`), `:41` (`WorkshopConnectionDomain`, missing `Mechanical`), `:48` (`PortRole`), `:105` (`SupplyConsumerKind`, missing `MechanicalTransmission`). Drift stays with `ENGINE-CAPABILITY-INSTANCES`.

### B6. One switch owned by two slices (closure, coordinator decision)

`engine/gpu/WorkshopActivationCompiler.cs:36-41` is one switch keyed by `SignalTerminal` (`SignalTerminal.Input => PortRole.ActivationIn` and so on). S6e lists it as `:38` (`:61-62`, task 4); S6d lists it as `:36` (`spec-s6d` owned scope) and S6d's Concurrency also says S6e edits `:38`. Its inverse `ActivationNetwork.cs:134-139` is keyed by `PortRole` and is S6e's by both contracts. Two owners for one switch violates "exactly one slice".

Fix: S6e owns both mappings as one pair of columns (`PortRole.Terminal` here, and the inverse read from the same row), so the terminal-to-role and role-to-terminal rules cannot drift. S6e's text: "`WorkshopActivationCompiler.cs:36-41` and `ActivationNetwork.cs:134-139` are the two directions of one activation role and signal terminal mapping; S6e owns both." Proposed S6d edit (for the coordinator): delete `:36 (SignalTerminal)` from S6d's `WorkshopActivationCompiler.cs` entry (keep `:51-70`) and the `:38` mention in S6d's Concurrency.

## Non-blocking findings

- **N1. `LogicGateOperation` row also carries `Monotone`.** `SupplyCycles.cs:13` is `IsMonotone`, not a truth function; contract `:24` and E2 mention only the truth table. Existing tests already assert monotone for all five operations (`ElectricalNandTests.cs:291-292` And and Nand, `ElectricalOrTests.cs:312`, `ElectricalNorTests.cs:293`, `ElectricalXorTests.cs:308`), so no new test is needed; cite them in the probe and name the second column (B2 table).
- **N2. E4 row isolation.** `WorkshopCompiledScene.cs:100-105` refuses a row with one message for duplicate slot, unresolved slot, undefined lane and bad bounds. The out-of-bounds control must differ from the admitted row only in `Minimum` or `Maximum` (same `Owner`, `LaneIndex`, `Source`), otherwise `Resolves` can be what refuses it.
- **N3. Test filter** (`:120`) omits `BundleValidatorTests` (in `tools/BundleElements.Tests`, which the solution test run includes; it covers port validation in the changed `BundleValidator.cs`). Add `FullyQualifiedName~BundleValidatorTests`.
- **N4. Test delta.** AC3 gives no N. State it from enum counts at baseline: E1 4, E2 5, E3 3, E4 2 lanes (plus the bounds controls), E6 one case per site in the B3 list.
- **N5. Concurrency** (`:79-81`): "No other pre-programme slice touches these files" is false inside the chain: S6c edits `BundleValidator.cs`, `WorkshopCompiledScene.cs` (`:341-356`) and `SupplyBundleValidation.cs`; S6d edits `WorkshopMessages.cs:32`, `ActivationNetwork.cs`, `WorkshopActivationCompiler.cs`. Write: "S6c and S6d publish first and S6e rebases on them (one publisher at a time); repair parts A to C edit package scenes only."
- **N6. Documents that cite line numbers** of changed files (they re-read after publication): 35 files cite `SupplyRoute.cs:` and 14 cite `ElementPortCatalogue.cs:` (for example `EL-137-electrical-nand-gate.md:39-41`, `CAT-013-both_gate.md:92`, `CAT-005-battery.md:93`). The contract names two documents only (`:65`).
- **N7. Not growth chains.** `PortDirection`, `PortSignalKind` and `RunControlValueKind` do not grow when a capability is added; their tables are two or three row conversions. Harmless; kept because S6b's inventory lists them.
- **N8. Performance.** The supply path runs per tick on the worker; the contract's "allocates nothing new" has no test. Acceptable for an unmeasured structural change if the contract says "no measurement: per-node row lookup replaces a switch; the existing `Resolve` allocates per call". Add that sentence to Design Notes 4.

## Decisions that need the owner

None. B6's choice of owner for the shared switch is a coordinator decision (default above). Whether to add `ParameterUnit` (`ui/WorkshopConfiguration.cs:255`, 18 arms, excluded in S6b on a weak reason: electrical added Joule and Watt, gas and thermal will add units) is a coordinator decision; default: add one row column "unit label" to S6e since the file is already owned, with one theory over `ParameterUnit` and independently written labels.

## What was checked and holds

- Cited lines and complexities match the threshold-12 log: `SupplyRoute.cs` `Validate :22` 16, `ValidateEdges :44` 18, `.ctor :107` 16, `AdmitNode :172` 23, `ResolveConsumerSlot :233` 14, `Resolve :321` 23; `BundleValidator.Port :520` 25; `ElementPortCatalogue.Admit :112` 16; `ChangeConnectionAsync :125` 25, `RefreshConnectionArtwork :170` 25, `RefreshConnectionChoices :60` 22. Switch lines `:389` (5 arms), `:412`, `WorkshopRunControl.cs:12 :23 :35 :98`, `SupplyCycles.cs:13`, `PhysicsPresentationRead.cs:34 :55`, `WorkshopCompiledScene.cs:117 :197 :334`, `WorkshopMessages.cs:177`, `WorkshopWireFlatBuffers.Compiled.cs:663 :678 :688`, `ElementPortCatalogue.cs:120 :128 :136`, `ui/WorkshopConnections.cs:236 :244`, `RunControlValues.cs:37`, `WorkshopConfiguration.cs:110 :149` are as cited.
- Enum counts: `SupplyNodeKind` 4, `SupplyConsumerKind` 3, `SupplyTerminal` 4, `LogicGateOperation` 5, `RunControlLane` 2, `RunControlValueKind` 2, `WorkshopConnectionDomain` 4. `SimulationDomain` has 7 values with `None`, `PortDirection` 3, `PortSignalKind` 2, `PortRole` 13 (the contract states no counts for the last four).
- AC1 is attainable for `AdmitNode`, `Resolve` and `Port`: the node-kind branches, the `Active` switch and the rope and signal rules inside `Port` are the decision points a row removes.
- Probe tests exist: `ElectricalSupplyTests`, `ComposableSupplyRoutingTests`, `SwitchElectricalTests`, `LatchSupplyFixtureTests`, `ElectricalNand/Nor/Or/XorTests`, `BothGateTests`, `MotorTests`, `ClutchTests`, `CompiledSceneBoundaryTests`, `RunControlTableTests`, `BundlePortsTests`, `PackageLocalPortsTests`.
- Route-test reach: E1, E3 and the in-range half of E4 reach their tables through the production admission and codec; a missing row fails them (the in-range case is refused or throws). The E4 refusal half is not discriminating on its own (N2).
- Removed protections: none beyond the runtime default arm; there is no exhaustiveness analyzer configured (the first S6 audit's search for `IDE0010|IDE0072|CS8509` found none); the every-value theories replace it and fail for an enum value with no written expectation (B4 fix).
- Forward-only, element-free, enums end to end, f32: tables key generic enums, nothing deleted is kept, no float changes; tables are built from static data with no `Enum.GetValues` in production.
- Engine tier, one structural slice, 1,719 words; registration check applies (new test file) and is listed; `tools/anvil/` unchanged so the policy harness is not needed; the new test file is top-level and compiled by the glob rule published at `25df3cd5`.
- Order: S6d then S6e then S6f, each from the published main of the previous; shared files with S6f (`WorkshopWireFlatBuffers.Compiled.cs`, `BundleValidator.cs`) are stated.

## Story closure (owner decision 24h), from S6e's side

Not closed by S6e as drafted. Unassigned chains over S6e's enums are in B1; the double-owned switch is B6. The full closure finding for all six slices is in `audit-s6f-5.md`.

## Evidence limits

Complexity values were read from the coordinator's log, not re-measured; the chain scan is text and AST based, not type-resolved, so a chain written through a `using static` or a local alias is not seen. `IsMonotone` test coverage was read, not run. No test, build or measurement was run.
