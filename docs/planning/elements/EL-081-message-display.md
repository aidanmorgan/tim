# EL-081 · Message display — named-identity spec

**Current delivery (24 October 2026):** [EL-081 delivery slice](../invest/named-elements.md#element-081). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

**Build-ready revision, 22 November 2026 (checkout `8d1d68aa`), repaired the same day against the independent audit (`docs/planning/elements/readiness/audits/elements-batch-l.md`) and the owner decisions of 22 November 2026.** A `path:line` citation without a commit is in this checkout, and `path@a6c914e:L…` is the legacy harvest. Values marked **starting value** are adopted under decision D1 of [delivery batches](../invest/delivery-batches.md#3-family-level-decisions). Game values are canonical f32.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-081 · Message display · Specialist |
| Anchor | [requirements.md#element-081](../requirements.md#element-081); [named entry](../invest/named-elements.md#element-081); scope index [todo-222](../requirements.md#todo-222) (shared with EL-080); [sequence-task-634](../requirements.md#sequence-task-634) |
| Proof owner | S651 |
| Disposition | To deliver as a single element. Not a duplicate of the Counter or the Signal lamp: it has no outputs, shows a count and a state together, and its face is dark without supply. The ledger also retains [CAT-020 Counter](CAT-020-counter.md) and [CAT-035 Lamp](CAT-035-lamp.md) acceptance for the two shared node laws; the rows that repeat them cite the delivered tests. |
| Planned package | id `message_display`, directory `elements/MessageDisplay/`, class `MessageDisplayElementDeclaration` |
| Refines / extends | Declaration shapes of the Counter (`elements/Counter/CounterElementDeclaration.cs:9`) and the Signal lamp (`elements/SignalLamp/SignalLampElementDeclaration.cs:9`) |
| Status | not started; blocked on the engine programme (owner decision 3a), then ENGINE-SIGNAL-NODES (saturating count), then S7 |

## 2. Declaration

The package implements `ISignalElement` (`CuriousContraptions.Abstractions.Signal/SignalContracts.cs:8`), `ISupplyElement` (`CuriousContraptions.Abstractions.Electrical/ElectricalContracts.cs:11`) and `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`), as the Counter does. `KinematicMode.Static`, `SupportedDomains = Signal | Electrical`, footprint 1 body, nothing moving.

The display is two declared signal nodes and a set of presentation bindings that read their committed state. It observes nothing in the world, reads no goal and sends nothing on: what it shows can only have arrived on its own inputs.

### Body and colliders (one static body)

| Collider | Shape and size (m) | Centre (m) |
| --- | --- | --- |
| housing | box 1.2 × 0.8 × 0.3 | (0, 0.5, 0) |
| foot | box 1.3 × 0.1 × 0.4 | (0, 0.05, 0) |
| three sockets | spheres radius 0.075 | at the three port positions |

Material: restitution 1, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s (the Counter's housing, `CounterElementDeclaration.cs:71`). The face is the housing's +Z side at z = 0.15.

### Ports

| `PortId` | Domain | Direction | Role | Kind | Label, glyph | Local position |
| --- | --- | --- | --- | --- | --- | --- |
| `PowerIn` | Electrical | Input | `PowerIn` | — | Power | (−0.6, 0.3, 0), normal −X |
| `ActivationIn` | Signal | Input | `ActivationIn` | `PortSignalKind.Pulse` | "Count", Text | (−0.3, 0.9, 0), normal +Y |
| `StateIn` | Signal | Input | `SetIn` | `PortSignalKind.Level` | "State", Text | (0.3, 0.9, 0), normal +Y |

No output port. The state input's role is `SetIn`, decided: the validator checks a signal port's kind against the terminal of the node it binds, not against its role (`CuriousContraptions.Bundle/SignalBundleValidation.cs:52`), and an indicator node's input terminal is a Level. The slice's probe confirms the role is admitted.

### Signal nodes, supply and parameters

- `count`: `SignalPulseCountDescriptor` with target `target_count` (`CuriousContraptions.Abstractions/SignalContractValues.cs:63`), input bound to `ActivationIn`. Its output terminal is bound to nothing.
- `state`: `SignalIndicatorDescriptor` (`SignalContractValues.cs:53`), input bound to `StateIn`. It follows the level it receives in both directions, as the Signal lamp's node does (`SignalLampElementDeclaration.cs:37`, `:60`).
- Supply graph: one consumer `face` with no targets, bound to `PowerIn` (the Clock's form, `elements/SystemClock/SystemClockElementDeclaration.cs:65`). Electrical node `face`, `ElectricalRole.Consumer`, 12 V, 24 Ω, 0 F, `MaxPower` 10 W: a declared rating, read by nothing; no debit is asserted. The face has no drive, so ENGINE-SUPPLY-DEMAND debits nothing for it; it is on or off with its route.
- No stores, no events.

| Key | Type, unit | Minimum | Maximum | Default | Editor policy | Run edit | Reason |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `target_count` | WholeNumber, none | 1 | 9 | 3 | Reject | BuildOnly | the Counter's parameter (`CounterElementDeclaration.cs:53`), so a display and a Counter fed the same pulses agree |

The earlier draft's `mode` and `message` parameters are dropped (section 6).

### Metadata and bindings

Category `Control`, display name "Message display", palette order 436 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)), colour (0.91, 0.72, 0.39). All face art lies flat on the face plane.

| Target node | Property | Driver | Values | Reads |
| --- | --- | --- | --- | --- |
| `Visual/Dot_0` … `Dot_8` | albedo, three channels each | `PresentationDriver.CommittedCount`, threshold 1 … 9 | slate `#556573` to gold `#f7cb52`, smooth step 0.1 s | node `count`; the Counter's bindings (`CounterElementDeclaration.cs:13`) |
| `Visual/Dot_0` … `Dot_8` | `UniformScale` | `CommittedCount`, threshold 1 … 9 | 0.7 to 1.0 | node `count`; size, so colour is not the only cue |
| `Visual/DoneRing` | `Visible` | `CommittedActivation` | 0 to 1 | node `count` latched: the target was reached |
| `Visual/StateMark` | `Visible` | `CommittedActivation` | 0 to 1 | node `state`: the received level is on |
| `Visual/Shade` | `Visible` | `CommittedElectricalSocket`, socket `PowerIn` | 1 to 0 | hidden while the face is supplied |

### Art and solidity

| Node | Mesh | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Housing`, `Visual/Foot` | ochre `#d69c47` box, navy `#293954` foot | solid, box colliders | nothing |
| `Visual/Sockets` | gold studs | solid, sphere colliders | nothing |
| `Visual/Face` | cream `#fff8e9` panel, flush with the housing | decorative: printed flat | nothing |
| `Visual/Dot_0` … `Dot_8` | nine discs in three rows of three, on the left of the face | decorative: printed flat | colour and scale by bindings |
| `Visual/DoneRing` | gold ring around the dot block | decorative: printed flat | visibility by binding |
| `Visual/StateMark` | navy `#25344b` tick mark on the right of the face | decorative: printed flat | visibility by binding |
| `Visual/Shade` | slate pane covering the face, flush | decorative: printed flat | visibility by binding |

Icon: a small sign with three dots and a tick. It differs from the Counter (dots only, four sockets) by the tick panel and the single row of top sockets, and from the Signal lamp (a bulb) by being a flat sign.

### Behaviour

Laws: the pulse-count node (each distinct occurrence counts once; reaching the target latches the node) and the level indicator node, both in the signal network; the supply-network rule for the face; presentation bindings on committed state.

- **Count.** Each occurrence on `ActivationIn` raises the committed count; dot k is lit when the count is at least k; the ring shows when the count reaches `target_count`.
- **State.** The tick shows while the level on `StateIn` is on and hides when it goes off.
- **Supply.** The nodes are signal state and keep counting without supply; the face is the load. Unsupplied, the shade covers everything; when supply arrives the face shows the state already received.
- **Unconnected.** With nothing wired to an input its node never changes, so the dots stay dark and the ring and tick never show, whatever happens in the level, including Solved.
- **Tick order.** Signal routing is same-tick: a pulse counts in the tick it is published and the committed count of that tick drives the bindings. Bindings are evaluated off the physics thread and write nothing back.

## 3. Engine capabilities

Map row families: SignalPropagation, StateTransaction, TypedContracts, ElectricalPower (navigation only, D14; the row's mechanical families are not used).

| Capability | Status | Evidence |
| --- | --- | --- |
| Pulse-count node: counts occurrences, emits once and latches at its target | present | `SignalContractValues.cs:63`; `CounterTests.ActivationNetwork_CountingProgression_SingleEmission_AndSaturation` (`CuriousContraptions.tests/CounterTests.cs:240`); `ComposableSignalNodesTests.IndependentNodesRouteCountAndResetThroughProduction` (`CuriousContraptions.tests/ComposableSignalNodesTests.cs:67`) |
| Counting needs no electrical supply | present | `IndependentSignalCompositionTests.ElectricalFreeCountAcceptsTwelveAndCrossesOnceFromExternalPulses` (`CuriousContraptions.tests/IndependentSignalCompositionTests.cs:170`) |
| Level indicator node follows a latch on and off, with its presentation | present | `SignalContractValues.cs:53`; `DeclaredPresentationTests.CommittedLatchAndLampPreserveBothDirectionsAndDelayPhases` (`CuriousContraptions.tests/DeclaredPresentationTests.cs:415`) |
| Count-threshold dot bindings | present | `DeclaredPresentationTests.ClockTicksDrivePendulumPulseAndCounterLamps` (`DeclaredPresentationTests.cs:237`) |
| A binding that reads an electrical socket's committed state | present | `ElectricalXorTests.PictogramTwoTruthDotsAndThreeLampsFollowCommittedRowsThroughReset` (`CuriousContraptions.tests/ElectricalXorTests.cs:367`), for `FirstIn`, `SecondIn` and `Supply` sockets |
| Several signal nodes in one package, each selected by its own public input port | present | `ComposableSignalNodesTests.PublicSetResetSelectsOneMemoryAndOneOwner` (`ComposableSignalNodesTests.cs:439`): a composed package with two memory nodes and separate public set and reset ports passes the bundle validator and routes each port to its own node. The port kind is checked against the node terminal (`SignalBundleValidation.cs:52`) |
| `PresentationProperty.Visible` driven by `CommittedActivation` or `CommittedElectricalSocket` | present, untested | `Visible` exists (`CuriousContraptions.Abstractions/ElementPresentation.cs:15`) and one package uses it, with `CommittedTimerActive` (`elements/HoldTimer/HoldTimerElementDeclaration.cs:49`); no package pairs it with these two drivers, and `PowerIn` is not among the sockets a delivered binding reads |
| The committed count stops at the target | narrower | the count keeps rising past the target, so dots above the target light on later pulses; owned by ENGINE-SIGNAL-NODES (saturating count; owner decision 3a) |

Nothing is missing. No text or message rendering is used, so no text presentation capability is needed.

## 4. Sources and legacy

- **Requirement row.** [element-081](../requirements.md#element-081): a supplied display renders an explicitly received typed state or count; unconnected inputs cannot display a success event.
- **Research reference.** The manual cited by [todo-222](../requirements.md#todo-222).
- **Legacy search.** `git grep -i` at a6c914e for "message", "display" and "readout" over every Epic 7 deletion path found only the display-clock bridge, a presentation clock with no element knowledge.

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| L1 | Counter target is an integer 1–9 | `parts/CounterPart.cs@a6c914e:L39-L41` | Carry forward; it is the delivered range |
| L2 | Counter dots are laid out in rows of three | `parts/CounterPart.cs@a6c914e:L59-L63` | Carry forward the layout |
| L3 | Counter emits once at the target and stays latched until Reset | `parts/catalog/counter.tres@a6c914e:L11-L11` | Carry forward as the ring's rule |

**Files harvested:** `parts/CounterPart.cs`, `parts/catalog/counter.tres`; `engine/bridge/CommittedDisplayClock.cs` (checked, no element knowledge).

## 5. Acceptance outline

Requirement row: [element-081](../requirements.md#element-081). Planned test class `MessageDisplayTests`. Binding outputs are read from the production presentation evaluator on the placed part, as the delivered Counter and gate tests do.

| # | Given / When / Then | Planned test | Control | Boundary |
| --- | --- | --- | --- | --- |
| A1 | Given a Battery on `PowerIn` and a Ball detector wired to `ActivationIn`, when three balls pass, then the committed count reads 1, 2, 3 in order, dots 1 to 3 are lit and the ring shows after the third | `ThreeReceivedPulsesLightThreeDotsAndTheRing` | A3 | target 3; the ring is hidden after two |
| A2 | Given a State latch wired to `StateIn`, when it is set and later reset, then the tick shows and then hides | `TheStateMarkFollowsTheReceivedLevelBothWays` | A3 | checked 0.5 s after each change |
| A3 | Given both inputs unwired and a level whose goal becomes Solved while three balls pass the same detector, then the count stays 0, every dot stays dark and the ring and tick never show | `UnconnectedInputsShowNothingEvenWhenTheLevelIsSolved` | A1, A2 | every tick of the Run |
| A4 | Given A1 with no supply, then the shade is shown throughout while the committed count still reaches 3; when the Battery is then enabled, the shade hides and three dots and the ring are visible | `WithoutSupplyTheFaceIsDarkButTheCountIsKept` | A1 | — |
| A5 | Given target 3 and a fourth pulse, then the count reads 3, dots 4 to 9 stay dark and the ring is unchanged | `ExtraPulsesChangeNothing` | A1 | waits on ENGINE-SIGNAL-NODES (saturating count) |
| A6 | Targets 1 and 9 behave as A1; 0, 10 and 2.5 are refused at every seam | `TheTargetIsAWholeNumberFromOneToNine` (theory) | the bounds | exact |
| A7 | A Signal output offered to `PowerIn` and an Electrical `Supply` offered to `ActivationIn` or `StateIn` are refused | `WrongKindConnectionsAreRefused` (theory) | the correct wire is accepted | typed `InvalidConnection`; S4 covers it generically when it lands |
| A8 | Reset clears the count, the state and every binding; save and load keep the target and the three wires | `ResetAndSaveLoadRestoreTheDisplay` | — | generic row: `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad` (`CuriousContraptions.tests/CatalogueIntegrationTests.cs:273`) |

Every row fits the default 30 s Run length.

### Integration

- Count sources: Ball detector, Repeating clock, Counter, Impact switch, Delay box. State sources: State latch. Supply: Battery and any switched supply. All delivered.
- The count agrees with EL-120 Quantity goal only by being fed the same pulses; the display never reads a goal.
- Campaign: chapter 10 "Oddly Satisfying", levels 91–100, "contact display" (`docs/planning/requirements.md:2410`, chapter row `:2295`); reuse 111–120, 136–150.
- A level that fixes the display in place needs the lesson-fixture story S8.

### Delivery

- **Shape.** Single element slice.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a); then ENGINE-SIGNAL-NODES for its saturating-count row (row A5 and the dots above the target); then S7. S3 preferred.
- **Files.** `elements/MessageDisplay/` (project, declaration, `message_display.tscn`, `icon.svg`); `CuriousContraptions.tests/MessageDisplayTests.cs`; one `CuriousContraptions.slnx` line; the binding section once S7 admits it; until S3, the remaining per-package lists.
- **Form.** Port roles, node arity, target masks and mouth identities are written in the form ENGINE-CAPABILITY-INSTANCES leaves; the names here are today's. This part uses the roles `ActivationIn`, `SetIn` and `PowerIn` and the count node kind, whose names that story changes to capability names.
- **Review tier.** Engine tier because of the one *present, untested* row; data-only if the probe finds an exercising test.
- **Disqualifiers the probe re-checks.** The role `SetIn` on a Level input bound to an indicator node (decided; the probe confirms admission); `Visible` refused with either driver (then the shade and marks use colour and scale bindings, which are tested); a `PowerIn` socket that a binding cannot read (then the face consumer's committed supply is read through the supply-node form of the same driver).

## 6. Open questions

All resolved.

1. **An unsupplied display still counts.** The count is signal state; the face is the electrical load. Decided under the standing delegation: it is what the delivered nodes do, and a display that forgot pulses during a power gap would hide the cause from the player.
2. **No text: a reduced form, recorded as a decision.** The face shows dots, a ring and a tick. The requirement row asks for "an explicitly received typed state or count", which this meets; the name's "message" is met only as a mark, not as words. Text would need a localisable text binding that nothing owns, and no requirement or lesson asks for words (D19). The earlier `message` parameter is dropped. The audit lists this under criteria met in reduced form (its section 5).
3. **State and count are two inputs, not a mode** (D8, D19): a port's signal kind is fixed, and one part that shows both is simpler than a mode switch.
4. **State has an off.** It follows the received level, so a State latch's reset clears the tick.
5. **The state input's role is `SetIn`** (section 2).
6. **Sizes, colours and the 10 W rating** are starting values (D1, D9).

Needs the owner: none.

## Engine conformance (22 November 2026)

Checked against the owner addendum of 22 November 2026 (AGENTS.md element-free engine gate and Tri-Graph rule; `_bmad/custom/element-free-engine-design.md`; `docs/gpu-f32-physics.md`).

1. **Declaration data only.** One static body, two signal nodes, one supply consumer, three ports, one parameter and presentation bindings. No handler, script or engine test on this package.
2. **Generic laws.** The pulse-count and indicator node rules of the signal network; the supply-network rule; bindings on committed state. The one narrower capability is stated element-free and has one owner, ENGINE-SIGNAL-NODES.
3. **Three layers.** Colliders: housing, foot and three socket spheres. Visuals: one mesh per collider and flat printed face art. Animation: thirty-nine bindings (twenty-seven dot colour channels, nine dot scales, three visibilities), all reading committed signal or supply state; none feeds physics, signals or goals.
4. **Flow and typing.** Typed ports with roles and `PortSignalKind`; the target is a WholeNumber; nothing flows back from the face.
5. **Flat state.** Bodies 1 (static); colliders 5 (boxes 2, spheres 3); joints 0; drives 0; sensors 0; domain nodes: 2 signal, 1 supply consumer, 1 electrical; stores 0; presentation bindings 39.
6. **Performance.** Five static colliders, the Counter's order. The bindings are evaluated off the physics thread per placed display, not per scene.
7. **Tests are scenarios.** Every row runs the production simulation and the production presentation evaluator with delivered sources.

**Verdict: conforms** (one narrower general capability, with one owner).

## 7. Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of the identity's spec headed "Package source binding", read by `tools/Coverage/NamedPackageSources.cs` (pattern: EL-239 Baseball). The section is not written here: the reader parses every spec that carries that heading and fails the whole coverage run when a field does not resolve. **Blocked on the coverage follow-on story S7.**

| Field | Planned value |
| --- | --- |
| Package | `message_display` |
| Named identity | EL-081, `../invest/named-elements.md#element-081` |
| Acceptance | `todo-222`, `sequence-task-634` (`docs/planning/requirements.md:5045`) |
| Decision | S651, `../invest/scope-corrections.md#s651` |
| Mode records | `Configuration=Fixed` |

What it lacks today: `scope-corrections.md` has no `s651` block; `todo-222` is a scope index shared with EL-080, and `sequence-task-634` is anchored beside the element row, not beside that todo, which is the pairing the reader expects.
