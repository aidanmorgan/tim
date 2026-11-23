# EL-181 · Rising-edge detector — element readiness spec

**Current delivery (24 October 2026):** [EL-181 delivery slice](../invest/named-elements.md#element-181). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 Batch H named-identity spec. Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`. Values marked *proposed* have no legacy or requirement source; each carries a one-line justification and is a starting value under decision D1. Box sizes are full extents. This spec holds the **edge-detector rules X1–X7** that [EL-182 Falling-edge detector](EL-182-falling-edge-detector.md) reuses.

## 1. Identity

| Item | Value |
| --- | --- |
| Identity / name | EL-181 · Rising-edge detector |
| Type | Control |
| Anchor | [requirements.md#element-181](../requirements.md#element-181); existing record [campaign-element-coverage](../requirements.md#campaign-element-coverage) ("edge detection"); [named-elements entry](../invest/named-elements.md#element-181); owner S402 |
| Related | Refines no CAT spec. Reads a supplied condition ([EL-198 Switch](EL-198-switch.md) contact, [EL-133](EL-133-electrical-and-gate.md)–[EL-137](EL-137-electrical-nand-gate.md) gate outputs, [CAT-060](CAT-060-sound_meter.md) meter contact); feeds activation consumers ([CAT-020 Counter](CAT-020-counter.md) / [EL-183](EL-183-resettable-counter.md), [CAT-035 lamp](../requirements.md#current-cat-035), [CAT-022 Delay](CAT-022-delay.md)). Mirror element [EL-182](EL-182-falling-edge-detector.md). |
| Roadmap story | [EL-181 delivery slice](../invest/named-elements.md#element-181); canonical proof owner and prerequisite decisions are recorded there. |
| Status | not started. Blocked on the engine programme (owner decision 3a), in particular the edge row of `ENGINE-SIGNAL-NODES`; then S7 and the Family slice "Edge detectors". |
| Disposition | **Member of the Family slice "Edge detectors"** with sibling [EL-182](EL-182-falling-edge-detector.md) ([delivery-batches §1a](../invest/delivery-batches.md#1a-families-one-slice-each-if-the-probe-passes)). Not data-only today: there is no edge node (§3). After the edge row of `ENGINE-SIGNAL-NODES` the two members differ only in declared values (the polarity, the glyph, id, name, description and palette order). |
| Planned package | id `rising_edge`, directory `elements/RisingEdge/` (`RisingEdge.csproj`, `RisingEdgeElementDeclaration.cs`, `rising_edge.tscn`, `icon.svg`). Category Control; palette order 405 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)). |

## 2. Declaration

Follows the delivered System clock, the closest shape: an electrical input that a signal node reads, and a pulse output (`elements/SystemClock/SystemClockElementDeclaration.cs:40`–`:41`, `:58`, `:65`–`:67`).

| Item | Declaration (value · source) |
| --- | --- |
| Interfaces | `ISignalElement` (`CuriousContraptions.Abstractions.Signal/SignalContracts.cs:8`), `ISupplyElement` (`CuriousContraptions.Abstractions.Electrical/ElectricalContracts.cs:11`), `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`). |
| Bodies and colliders | One static scene body. Colliders from the scene: body box 0.9 × 0.7 × 0.5 m at the origin; foot box 1.0 × 0.12 × 0.6 m at (0, −0.41, 0); face plate box 0.76 × 0.56 × 0.04 m at z 0.27; two lamp spheres r 0.07 m at (−0.25, 0.2, 0.31) and (0.25, 0.2, 0.31); two socket spheres r 0.075 m at the port positions. All **proposed** (a small control module, about half the Counter's 1.65 m width; lamps and sockets get colliders as in the delivered gates). |
| Mass and material | Static (mass 0, no inertia). Material f32 as every delivered control module: bounce 1, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s (`elements/BothGate/BothGateElementDeclaration.cs:31`). |
| Signal nodes | One edge node, `SignalNodeId` `signal`, reading the electrical input port, polarity Rising. The node kind is **missing** and is delivered by the edge row of `ENGINE-SIGNAL-NODES` (§3); the shape this spec assumes is `SignalSupplyEdgeDescriptor(SignalNodeId Id, PortId SupplyInput, EdgePolarity Polarity)` with the closed enum `EdgePolarity { Rising, Falling }`, modelled on `SignalOscillatorDescriptor(Id, IntervalParameter, SupplyInput)` (`CuriousContraptions.Abstractions/SignalContractValues.cs:61`). `SignalPorts`: `ActivationOut` → (`signal`, `SignalTerminal.Output`). `SignalEdges`: none. |
| Supply graph | One `SupplyConsumerDescriptor("sense", [])` with no work target, bound `ConditionIn` → (`sense`, `SupplyTerminal.PowerIn`): the System clock's pattern. A consumer with no target requests no work, so the input is a sink (X1). `ElectricalNodes`: one `Consumer` rating row (12 V, 10 Ω, 50 W ceiling, as the clock's). |
| Events | `SimulationEventKind.TriggerActivated` from node `signal`, one per emitted occurrence (as `elements/SystemClock/SystemClockElementDeclaration.cs:52`). |
| Stores | none. The detector creates no electrical power and its pulse carries no energy. |
| Parameters | none. Polarity is fixed by the declaration, not a player choice (the falling polarity is its own identity, EL-182; decision D8). |
| Footprint | `new(Bodies: 1, MovingBodies: 0, PrismaticConstraints: 0, RevoluteConstraints: 0)`. Domains `Signal | Electrical`. |
| Catalogue | Id `rising_edge`, display name "Rising edge", `ElementCategory.Control`, colour (0.4, 0.72, 0.79) (the gates' control cyan), palette order 405, description "Sends one trigger when its input turns on. Holding it on sends nothing more." |

**Ports.**

| `PortId` | Role | Domain | Direction | Signal kind | Position (m) | Facing |
| --- | --- | --- | --- | --- | --- | --- |
| `ConditionIn` (package-local id) | `PortRole.PowerIn` | Electrical | Input | — | (−0.55, 0, 0) | −X |
| `ActivationOut` | `PortRole.ActivationOut` | Signal | Output | `PortSignalKind.Pulse` | (0.55, 0, 0) | +X |

`PortRole` is a closed enum with no condition role (`CuriousContraptions.Abstractions/PortDescriptor.cs:20`–`:35`), so the input takes `PortRole.PowerIn` bound to a consumer with no work target, the System clock's pattern (the gates' condition inputs instead bind a logic node's `FirstIn`/`SecondIn` terminals, `elements/BothGate/BothGateElementDeclaration.cs:25`–`:26`); the socket label reads "Condition". A package-local `PortId` is admitted (`PackageLocalPortsTests.UnfamiliarPortNamesAndTwoOutputsSharingOneRole_ConnectCompileSaveLoadRoundtrip`). Left-in, right-out matches the gates and the clock.

**Presentation bindings.**

| Target node | Property | Driver | Mapping |
| --- | --- | --- | --- |
| `Visual/InputLamp` | albedo R, G, B | `PresentationDriver.CommittedElectricalSocket`, `Socket: ConditionIn` | slate (85, 101, 115) → gold (247, 203, 82), SmoothStep 0.1 s (the gates' input lamp, `elements/BothGate/BothGateElementDeclaration.cs:44`–`:46`) |
| `Visual/OutputLamp` | albedo R, G, B | `PresentationDriver.Event`, `Event: TriggerActivated`, `SignalNode: signal` | gold → slate over 0.2 s, one cue per occurrence (a flash, not a held state, shows "one event"; the Impact switch's event cue is the pattern, `elements/ImpactSwitch/ImpactSwitchElementDeclaration.cs:16`–`:19`) |

**Variants.** The requirements row names no variant; the base declaration is the only required mode.

### Art and solidity

| Node | Mesh | Class | Reason / collider |
| --- | --- | --- | --- |
| `Body`, `Foot` | boxes, cyan `#66b8c9` and navy `#293954` | solid | box colliders |
| `Face` | box 0.76 × 0.56 × 0.04 m, cream `#fff8e9` | solid | box collider |
| `InputLamp`, `OutputLamp` | spheres r 0.07 m | solid | sphere colliders |
| `PortCondition`, `PortOutput` | spheres r 0.075 m, gold `#f7cb52` (electrical) and `#e8b764` (activation) | solid | sphere colliders |
| `StepGlyph` | navy quads forming a step that rises left to right | decorative | printed flat on the face (quad, 0.25 mm stand-off, as `Mesh_Mark` in `elements/BothGate/both_gate.tscn:35`–`:37`). The earlier "raised" glyph is not used: a proud static mark would need a collider. |

Icon: the rising step on a cream tile with one dot at the top of the step. Told from EL-182 by shape: the step rises to the right; EL-182's falls. Colours from [DESIGN.md](../../../DESIGN.md#colour-system) (D9).

### Edge-detector rules (shared with EL-182)

| # | Rule |
| --- | --- |
| X1 | The input is a condition: the detector samples committed availability and is a sink; no power flows from `ConditionIn` to any output (gate condition rule, `engine/BinaryCircuit.cs@a6c914e:L136-L138`). |
| X2 | `previous` starts false at admission and on Reset, matching the network's unpowered start. A condition true on the first committed tick is therefore a rising edge (EL-181) and never a falling edge (EL-182). |
| X3 | Sampling happens once per committed tick, from the committed supply availability, so every same-tick change participates and the order of part ids cannot change the result. |
| X4 | Rising: false → true emits one occurrence; Falling: true → false emits one occurrence. |
| X5 | A held value emits nothing further; re-arming needs at least one committed tick in the opposite state (no debounce beyond one tick). |
| X6 | The node samples the supply resolution of the current tick, where oscillators do (`engine/gpu/ActivationNetwork.cs:441`–`:445`), and its occurrence is routed in that same tick (`:505`–`:512`): a Counter it feeds counts, and can emit `ThresholdReached`, in that tick. A contact closed by that consumer passes supply from the next tick's electrical solve. Missed transitions are never replayed. |
| X7 | Each occurrence has a distinct identity (emitter, tick ordinal) so a counter counts it once (EL-183 contract). |

### Behaviour and numbers

- **Law.** `emit = current ∧ ¬previous` for Rising; then `previous ← current`. `current` is the committed availability of the `ConditionIn` route (boolean; no unit).
- **Tick order.** The electrical solve runs first. The edge node reads that tick's supply resolution the way an oscillator node does (`supply.IsPowered(node.PowerProvider)`, `engine/gpu/ActivationNetwork.cs:445`) and pushes its occurrence into the same tick's routing (`:505`–`:512`), so downstream signal nodes see it in that tick; only latch requests wait for the boundary resolve (`:308`–`:313`). The one-tick lag in the delivered network is from signal state to the next electrical solve (`LatchSupplyFixtureTests.SetClosesRouteOneTickAfterStrikeWithoutTopologyChange`), not from one signal node to the next. The edge row of `ENGINE-SIGNAL-NODES` fixes the sampling point once for both polarities.
- **Numbers.** Flash 0.2 s **proposed** (long enough to see at 60 FPS, shorter than the System clock's 0.25 s pulse so a clock-driven detector still shows separate flashes; presentation only). Geometry as above. No physical number affects behaviour.
- **Consistency with delivered parts.** Output Pulse → Counter `ActivationIn` (Pulse, `elements/Counter/CounterElementDeclaration.cs:60`), Delay box and Hold timer inputs (Pulse), State latch `SetIn`/`ResetIn` (Pulse), Signal lamp (a Pulse may drive an indicator, `CuriousContraptions.Abstractions.Signal/SignalContracts.cs:36`). Input: any electrical `Supply` output (Battery, contact, gate). A consumer input admits one supplier (`engine/gpu/SupplyRoute.cs:56`). The shortest clock interval is 0.1 s (12 ticks at 120 Hz), so a clock-gated condition always holds for more than the one tick X5 needs.

## 3. Engine capabilities

Families ([element map row](../general-engine-element-map.md); [binding](../../coverage/engine/element-02.json), proof owner S402): SignalPropagation. Map composition: "Reusable previous/current Boolean edge state emits once for false-to-true; ordering/Reset are explicit." The harvest-baseline lists are superseded by this probe (read on `8d1d68aa`): 6 present, 0 present-untested, 1 narrower, 1 missing.

| # | Capability | Mark | Symbol | Exercising test |
| --- | --- | --- | --- | --- |
| 1 | Electrical input bound to a consumer terminal with no work target | present | `elements/SystemClock/SystemClockElementDeclaration.cs:65`–`:67` | `ComposableSignalNodesTests.EachOscillatorUsesItsExactInputInsteadOfItsOwnersOtherSupply` (`ComposableSignalNodesTests.cs:549`) |
| 2 | A signal node that reads committed availability of a declared electrical input | **narrower**: only the oscillator node carries `SupplyInput` (`CuriousContraptions.Abstractions/SignalContractValues.cs:61`; compiled at `engine/gpu/WorkshopActivationCompiler.cs:72`; validated at `CuriousContraptions.Bundle/SignalBundleValidation.cs:165`) | — | `ComposableSignalNodesTests.BothOscillatorsUsePostPhysicsAvailabilityWhenTheirEnabledSourceDepletes` (`:778`) exercises the oscillator form only |
| 3 | A re-armable edge node: previous/current state, typed polarity, one pulse per transition, state in the committed checkpoint, cleared on Reset | **missing** | `SignalNodeKind` has ten kinds and no edge kind (`CuriousContraptions.Abstractions/SignalContractValues.cs:13`); `SignalTerminals.Kind` has no case for it (`CuriousContraptions.Abstractions.Signal/SignalContracts.cs:17`–`:27`) | owner: the edge row of **`ENGINE-SIGNAL-NODES`** (owner decision 3a; the row text proposed in `readiness/batch-h.md` section 4, under its earlier draft name, feeds that story) |
| 4 | Pulse output wired to pulse inputs and to an indicator | present | `SignalTerminals.CanConnect`, `CuriousContraptions.Abstractions.Signal/SignalContracts.cs:29`–`:37` | `StateLatchTests.Output_LevelDrivesOnlySignalLampsAndRejectsInvalidShapes` (`StateLatchTests.cs:152`); `CounterTests.Connections_AcceptsValidActivationAndElectricalLinks` (`CounterTests.cs:175`) |
| 5 | Package-local port ids | present | `CuriousContraptions.Abstractions/PortId.cs:6`–`:13` | `PackageLocalPortsTests.UnfamiliarPortNamesAndTwoOutputsSharingOneRole_ConnectCompileSaveLoadRoundtrip` (`PackageLocalPortsTests.cs:217`) |
| 6 | Committed electrical socket lamp | present | `PresentationDriver.CommittedElectricalSocket`, `CuriousContraptions.Abstractions/ElementPresentation.cs:24` | `BothGatePresentationTests.ThreeNativeLampsFollowCommittedInputsAndSupplyThroughHideAndReset` (`BothGateTests.cs:259`) |
| 7 | One-shot event cue | present | `PresentationDriver.Event`, `CuriousContraptions.Abstractions/ElementPresentation.cs:21` | `ComposableSignalNodesTests.SameKindEventCuesSelectTheirExactSiblingAndReset` (`ComposableSignalNodesTests.cs:718`): an `Event` binding on `TriggerActivated` of a named signal node, asserted active and at rest after Reset |
| 8 | Scene-extracted static colliders and port locators | present | `ISceneGeometryElement.SceneBodies`, `CuriousContraptions.Abstractions/SceneGeometry.cs:10`–`:13` | `BothGateTests.EverySuppliedGateDeclaresItsBodyFootAndProudPartsInOrder` (`BothGateTests.cs:19`) |

**What the edge row of `ENGINE-SIGNAL-NODES` must deliver** (engine tier; no element identity in engine code): a signal node with a declared electrical input and a typed polarity (Rising, Falling), sampled beside the oscillators; one Pulse occurrence per transition, routed in the same tick, repeatable within a Run, each with a distinct identity; `previous` is committed memory (a superseded candidate leaves it unchanged), false at admission and Reset; the descriptor and enum above in the abstractions, bundle schema and validation; the wire and TypeScript rows the node needs. Acceptance: a held input emits nothing; a one-tick opposite state re-arms; one fixture per polarity. It settles rows 2 and 3 together, so both detectors are then declarations, art, bindings and tests only.

**Dependencies.** Battery and a switched condition (Impact switch, gates: delivered); an activation consumer to observe (Signal lamp, Counter: delivered).

### Engine conformance (22 November 2026)

Checked against the owner's engine-conformance standard of 22 November 2026 ([engine-conformance.md](readiness/engine-conformance.md); [element-free engine design](../../../_bmad/custom/element-free-engine-design.md); AGENTS.md, Tri-Graph architecture). The same seven points hold for [EL-182](EL-182-falling-edge-detector.md), which differs in one declared enum value. Verdict: **Conforms** (one missing general capability, with one owning story: the edge row of `ENGINE-SIGNAL-NODES`).

1. **Declaration data only.** The package is a declaration, a scene and bindings. The edge behaviour is a signal node kind any package may declare, with a typed polarity; it is not a per-part loop (legacy fact L1 is carried as a rule, not as code) and no engine branch names the part.
2. **Generic laws.** Input: the supply network's availability of a consumer terminal with no work target. Edge: the signal network rule "emit one Pulse when the sampled boolean differs from committed `previous` in the declared direction". Output: ordinary same-tick occurrence routing.
3. **Three layers.** Colliders: seven on the one static body (three boxes: body, foot, face plate; four spheres: two lamps, two sockets), material as §2; nothing reads art. Visuals: those seven solids plus `StepGlyph`, printed flat; no art node is a collider. Animation: the two lamps are recoloured by presentation bindings (`CommittedElectricalSocket`, `Event`); no art node is moved by a body pose or a binding. The art table's classes stand; the layer that changes each node is: `InputLamp`, `OutputLamp` a binding (colour only); every other node nothing.
4. **One-way flow and typing.** Supply solve → signal node → presentation; nothing flows back. Polarity is the closed enum `EdgePolarity`; ports are `PortId`; there is no float in the behaviour.
5. **Flat state.** Bodies 1 (static); colliders 7 (box 3, sphere 4); joints 0; drives 0; sensors 0; domain nodes 2 (one signal edge node with one committed boolean, one supply consumer); stores 0; presentation bindings 6 rows on two lamps.
6. **Performance fitness.** Static body; seven primitives against the delivered gates' ten; none is cosmetic-only (each is a proud visible solid). One boolean sample per node per tick. The part makes no spatial query (nothing from `ENGINE-SPATIAL-QUERIES`) and has no dynamic body (nothing waits for `ENGINE-ISLANDS-SLEEP`). No per-part collider or binding limit is stated in the documents or code read here, and none is assumed.
7. **Tests are scenarios.** R1–R9 run the production simulation; none depends on a detector-specific code path.

## 4. Sources and legacy

- Requirement row [element-181](../requirements.md#element-181): "Observes a false-to-true transition and emits one typed event"; outcome "Held true input produces no repeated transitions". Campaign coverage reserves "edge detection" in 21–40.
- Named entry [element-181](../invest/named-elements.md#element-181), owner S402; component research: "Edge detector (EL-181, EL-182) | Rising/falling edge → one pulse | Counters never increment every tick; missed events are not replayed on power restoration" and "Electrical node + edge → pulse | One pulse per held-condition change | Lamp ← pulse" ([component research](../../component-research.md#logic)).

| # | Legacy fact | Source | Disposition |
| --- | --- | --- | --- |
| L1 | The Sound meter computes a rising crossing as `above ∧ ¬wasAbove` and emits one activation only on that tick and only if supplied; the pending flag is cleared afterwards, so a held level never re-emits. | `parts/SoundMeterPart.cs@a6c914e:L64-L71`, `parts/SoundMeterPart.cs@a6c914e:L72-L83` | carry forward the edge rule as a reusable node, not a per-part loop |
| L2 | A counter's threshold emits once and later deliveries saturate rather than re-emitting. | `engine/SimulationCounters.cs@a6c914e:L56-L62` | carry forward (one event per transition) |
| L3 | No standalone edge detector part, scene, level or lesson exists. | search of `parts/`, `content/puzzles.json`, `tools/Campaign` for "edge", "rising", "falling" | — |
| L4 | The legacy activation network emitted each source once per run. | `engine/gpu/ActivationNetwork.cs@a6c914e:L199-L208` | not current: delivered oscillator and passage nodes emit many times per run |

- **Files harvested:** `parts/SoundMeterPart.cs`, `engine/SimulationCounters.cs`, `engine/BinaryCircuit.cs`, `parts/LatchPart.cs`.

## 5. Acceptance outline

Acceptance authority: [element-181](../requirements.md#element-181), [electrical profile](../invest/profiles.md#electrical). Planned class `EdgeDetectorTests` (one file for the family; each identity has its own rows). Fixture: Battery → Impact switch contact → detector `ConditionIn`; detector `ActivationOut` → Counter (target 9) and Signal lamp. Conditions are driven by the Battery's `enabled` run control where a row needs a mid-run change.

| Row | Given / When / Then | Planned test | Control | Boundary |
| --- | --- | --- | --- | --- |
| R1 | Given an open contact, when it closes during Run, then exactly one pulse is emitted and the counter reads 1 | `EdgeDetectorTests.ARisingConditionEmitsExactlyOnePulse` | the same construction without the battery wire: no pulse in 240 ticks | — |
| R2 | Given the condition held true, when 240 further ticks run, then no further pulse is emitted (requirement outcome) | `EdgeDetectorTests.AHeldTrueConditionEmitsNoFurtherPulses` | R1's single pulse | 240 ticks = 2 s at the default cadence |
| R3 | Given a condition that is never true, then no pulse; given a battery wired straight to `ConditionIn`, then exactly one pulse on the first committed tick and none after (X2) | `EdgeDetectorTests.ANeverTrueConditionEmitsNothingAndAnAlwaysTrueConditionEmitsOnceAtStart` | — | first committed tick |
| R4 | Given a true condition, when it is false for exactly one committed tick and true again, then a second pulse; two rises give two occurrences with distinct identities and the counter reads 2 | `EdgeDetectorTests.OneTickOffRearmsAndEachRiseIsCountedOnce` | zero ticks off: no second pulse | one tick (X5) |
| R5 | Given both detectors on one condition, when it rises and later falls, then the rising detector fires at the rise only | `EdgeDetectorTests.RisingAndFallingDetectorsFireOnOppositeTransitions` | — | — |
| R6 | Given a run with one pulse, when Reset then Run again, then `previous` is false, the lamp is slate and the same single pulse recurs; a superseded candidate tick does not advance `previous` | `EdgeDetectorTests.ResetClearsEdgeStateAndASupersededCandidateLeavesItUnchanged` | — | — |
| R7 | Given the wired construction, when saved and loaded, then parts and both connections are equal port by port; `previous` is not persisted | `EdgeDetectorTests.SaveLoadKeepsBothConnectionsAndNoRunState` | — | — |
| R8 | Given an activation output wired to `ConditionIn`, or the detector's `ActivationOut` wired to a `PowerIn`, then the wire is refused and the construction is unchanged | `EdgeDetectorTests.WrongDomainWiresAreRefused` | the valid wire is admitted | — |
| R9 | Given the pulse, then the input lamp follows the committed condition and the output lamp flashes once per occurrence and is slate after Reset | `EdgeDetectorPresentationTests.InputLampFollowsConditionAndOutputFlashesOncePerPulse` | held condition: no second flash | — |

**Tolerances and stall guards.** Every assertion is a count, a typed state or a refusal; no float is compared. Absence windows are 240 ticks (2 s), long enough for a per-tick re-emission defect to show 240 times. No arrival tick is asserted except the defined one-tick re-arm of X5 and the first-tick edge of X2, which are the feature.

**Generic coverage today.** Admission, Run/Reset and Save/Load of every delivered package: `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad`. Wrong domain and direction: `PackageLocalPortsTests.WrongDomainAndWrongDirection_RefusesAtomically` (R8 keeps one row of its own because the input id is package-local). No parameter, so no parameter refusal. The S4 conformance suite is drafted, not delivered; once it lands R7 and R8 fold into it.

**Chrome recipe (pending, post-catalogue).** Battery → Switch contact → detector `ConditionIn`; detector `ActivationOut` → Counter `ActivationIn` (choose the socket pair); a ball presses the switch; the lamp flashes once and the counter counts 1.

## 6. Open questions

All resolved; needs the owner: none.

1. **First-tick edge — decided** (standing delegation): a condition already true on the first committed tick counts as a rising edge (X2). Reason: the network starts unpowered, so the player sees the input lamp turn on at Run; that is a turn-on, and silent arming would make "wire a battery straight in" do nothing with no visible cause.
2. **Input domain — decided** (D19): electrical condition input only. No requirement row or campaign lesson uses an activation input, and a pulse has no held state to take an edge of.
3. **f32 activation and cosmetic lanes — resolved** by Story 9A.44. EL-182 inherits this.
4. **Port role — decided** (standing delegation): `PortRole.PowerIn` with the package-local id `ConditionIn`. Reason: the closed role enum has no condition role and adding one changes the wire for no behaviour.
5. **Coordinator decisions, 22 November 2026 (owner's standing delegation).** (a) Tick order follows the delivered signal network (rule X6 as restated); reason: the earlier wording said a downstream consumer applies the pulse at its next tick boundary, which contradicts `ActivationNetwork.cs:505`–`:512`. (b) Coverage uses the binding form published on main; reason: owner decision of 21 November 2026. (c) Palette order 405 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)); reason: batches H, I and J chose overlapping numbers and the bundle refuses duplicates. (d) The edge node is the edge row of `ENGINE-SIGNAL-NODES` (owner decision 3a); the row text proposed in the batch summary feeds that story.

## 7. Integration

- **Collaborators.** Battery, Impact switch, the five gates, State latch, Hold timer and Counter contacts (delivered) as condition sources; Counter, Delay box, Hold timer, State latch, Signal lamp (delivered) as consumers; [EL-183](EL-183-resettable-counter.md) (planned after) as a consumer of its reset input; [CAT-060 Sound meter](CAT-060-sound_meter.md) contact (not delivered) is a later source and not required here.
- **Campaign.** Logic row of [campaign-element-coverage](../requirements.md#campaign-element-coverage): "edge detection", first use 21–40 (reuse 41–80, 101–150); "a new rearm/reset mode needs its own objective". No level in `content/puzzles.json` places it today (L3); the level that teaches it is authored in its own level slice and needs the lesson-fixture story ([S8](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package)), which lands after the engine programme (owner decision 22).
- **Processes.** [IX-07 signal propagation](../requirements.md#interaction-07); counter retained behaviour [todo-269](../requirements.md#todo-269); switch contact [todo-147](../requirements.md#todo-147); gates [todo-362](../requirements.md#todo-362).
- **Coverage binding (planned, not yet live).** The package binding section that main's `tools/Coverage/NamedPackageSources.cs` reads, in the form published for EL-239 (Package `rising_edge`, Named identity, Acceptance, Decision, Mode records), added to this spec. **Blocked on the coverage follow-on story S7:** the published form's preconditions are not met. The EL-181 ledger entry has no `todo-NNN` link (the reader needs exactly one); no `todo`/`sequence-task` pair for this identity is co-anchored in `requirements.md`; and owner S402 has no `<a id="s402">` anchor in `docs/planning/invest/scope-corrections.md`. The section is not written until those exist, because the reader throws on a malformed block.

## 8. Delivery

- **Slice shape.** Family slice "Edge detectors": EL-181 and EL-182 in one slice, two packages, separate acceptance rows and tests.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), in particular the edge row of `ENGINE-SIGNAL-NODES` (engine tier, six layers); then S7, because the published binding form does not yet admit this identity (§7). S3 and S4 are optional and each removes edits listed below.
- **Files the family slice adds or edits** (registration is derived, `spec-registration-derivation.md`): the two package directories; the regenerated `elements/elements.bundle.json`; `CuriousContraptions.slnx` and the trim-analysis project list at `CuriousContraptions.tests/CSharpStaticGateTests.cs:703` (until S3); `CuriousContraptions.tests/EdgeDetectorTests.cs` (compiled by the test project's one top-level rule; no project-file line); the placement switches in `PackageRuntimeParityTests.cs:596` and `:653` only if the generic placement does not fit (until S3); the capability requirement tables under `tools/Coverage/` (not derived by S1); the binding section in each of the two specs once S7 admits it; sprint status and `TODO.md`. No registry, palette, port, capacity or impact table is edited by hand.
- **Review tier.** Data-only (Verification Gap, Forward-Only Auditor, Element-Free Engine Auditor), provided the probe at planning shows rows 2 and 3 present.
- **Disqualifiers (the slice's probe re-checks).** The edge row shipping a node that cannot be bound to a package-local input port, or with a fixed polarity (then EL-182 is not a declared-value sibling); the event cue not available for the new node's occurrences; S7 unpublished.
