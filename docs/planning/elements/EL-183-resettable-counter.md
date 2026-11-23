# EL-183 · Resettable counter — element readiness spec

**Current delivery (24 October 2026):** [EL-183 delivery slice](../invest/named-elements.md#element-183). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 Batch H named-identity spec. Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`. Values marked *proposed* have no legacy or requirement source; each carries a one-line justification and is a starting value under decision D1. Box sizes are full extents (legacy `AddBox` takes the full size and stores half-extents, `reference/cpu/MachinePart.cs@a6c914e:L352-L356`). The base counter harvest is [CAT-020](CAT-020-counter.md); this spec adds the separately addressed reset input.

## 1. Identity

| Item | Value |
| --- | --- |
| Identity / name | EL-183 · Resettable counter |
| Type | Control |
| Anchor | [requirements.md#element-183](../requirements.md#element-183); existing record [campaign-element-coverage](../requirements.md#campaign-element-coverage) ("counter/reset input"); [named-elements entry](../invest/named-elements.md#element-183); owner S404 |
| Related | Extends [CAT-020 Counter](CAT-020-counter.md), delivered as package `elements/Counter`, id `counter` (three count/contact ports and no reset socket). Reset socket modelled on the delivered [State latch](../requirements.md#current-cat-037) `ResetIn`. Inputs from [EL-181](EL-181-rising-edge-detector.md)/[EL-182](EL-182-falling-edge-detector.md), [CAT-002 Ball detector](../requirements.md#current-cat-002), [CAT-063 Switch](CAT-063-switch.md). |
| Roadmap story | [EL-183 delivery slice](../invest/named-elements.md#element-183); canonical proof owner and prerequisite decisions are recorded there. |
| Status | not started (the delivered Counter itself is implemented). Blocked on the engine programme (owner decision 3a), in particular the saturating-count and count-reset rows of `ENGINE-SIGNAL-NODES` and the node-kind rename of `ENGINE-CAPABILITY-INSTANCES`; then S7. |
| Disposition | **To deliver as a single element.** Not a duplicate of CAT-020: it adds a port (`ResetIn`), a behaviour (clear and re-arm during Run) and acceptance of its own, so under the Duplicate closure rule it is an element ([delivery-batches §1b, "Supply and signal" run](../invest/delivery-batches.md#1b-runs-each-its-own-slice-planning-reference-only)). Not data-only today (§3). |
| Planned package | id `reset_counter`, directory `elements/ResetCounter/` (`ResetCounter.csproj`, `ResetCounterElementDeclaration.cs`, `reset_counter.tscn`, `icon.svg`). Category Control; palette order 403 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)). The delivered `counter` (order 22) is unchanged. |

## 2. Declaration

The delivered Counter's declaration (`elements/Counter/CounterElementDeclaration.cs`) plus one port and one terminal binding. The first table reuses the delivered Counter's values except where a row says otherwise: the scene's proud solids and sockets are seated on their faces (Bodies and colliders, Ports).

| Item | Declaration (value · source) |
| --- | --- |
| Interfaces | `ISignalElement`, `ISupplyElement`, `ISceneGeometryElement` (`elements/Counter/CounterElementDeclaration.cs:9`). |
| Bodies and colliders | One static scene body with **sixteen** scene colliders. Fifteen have the delivered Counter's shapes and sizes (`elements/Counter/counter.tscn:135`–`:219`; asserted by `CounterTests.CounterScene_ExtractsItsSolidPartsAsCollidersAndPortLocators`, `CuriousContraptions.tests/CounterTests.cs:80`, count at `:92`): housing box 1.65 × 1.25 × 0.65 m at the origin (x ±0.825, y ±0.625, z ±0.325); foot box 1.8 × 0.16 × 0.85 m (legacy sizes, `parts/CounterPart.cs@a6c914e:L56-L57`); the face panel 1.4 × 1.02 × 0.04 m; nine dot lamps r 0.1 m at z 0.4 in rows from the top left; and the three sockets r 0.075 m that stand outside the foot (`ActivationIn`, `PowerIn`, `Supply`). The `ActivationOut` socket lies wholly inside the foot and has no collider (`CounterTests.cs:89`–`:91`). The sixteenth is new: a socket sphere r 0.075 m at the `ResetIn` position on the top face. The slice copies the Counter scene and seats each proud solid on the face it stands from, because the delivered scene floats them: its foot at y −0.72 has its top at −0.72 + 0.08 = −0.64, 0.015 m below the housing's −0.625; its face panel at z 0.35 spans 0.33–0.37, 0.005 m in front of the housing's 0.325; its top sockets at y 0.72 leave a 0.72 − 0.075 − 0.625 = 0.02 m gap; its side sockets at x ±0.93 leave 0.93 − 0.075 − 0.825 = 0.03 m. Seated here: foot at (0, −0.705, 0), top −0.625; face panel at (0, 0, 0.345), back face 0.325; socket centres on their faces (Ports). The same gaps in the delivered Counter are recorded against CAT-020, outside this spec. |
| Mass and material | Static. Bounce 1, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s (`CounterElementDeclaration.cs:70`–`:73`, `SceneBodies`). |
| Signal nodes | One `SignalPulseCountDescriptor("signal", target_count)` (`:80`). Bindings: `ActivationIn` → (`signal`, `Input`); `ActivationOut` → (`signal`, `Output`); **new** `ResetIn` → (`signal`, `SignalTerminal.Reset`). The Reset terminal on a pulse-count node is **missing** today (§3). |
| Supply graph | One `SupplyContactDescriptor("contact", SignalStateContactControl("signal", SignalContactState.Latched))` with `PowerIn` and `Supply` (`:88`–`:93`): closed while the count has reached its target. `ElectricalNodes`: one `Switch` rating row (12 V, 0.05 Ω, 100 W). |
| Events | `SimulationEventKind.ThresholdReached` from `signal` (`CounterElementDeclaration.cs:74`). |
| Stores | none. The contact passes supply and never generates it. |
| Footprint | one static body; domains `Signal | Electrical`. |
| Catalogue | Id `reset_counter`, display name "Reset counter", `ElementCategory.Control`, colour (0.91, 0.72, 0.39) (the Counter's), palette order 403, description "Counts trigger events like the Counter. A trigger on the ring socket clears the count without counting as an event." |

**Parameters.**

| Name | Type | Unit | Default | Min | Max | Editor policy |
| --- | --- | --- | --- | --- | --- | --- |
| `target_count` | `ParameterKind.WholeNumber` | none | 3 | 1 | 9 | default policy (refuse out of range), build-only; as `CounterElementDeclaration.cs:53`. Player-editable unless the level locks it (D11). |

**Ports** (ids, roles and kinds of the first four as `CounterElementDeclaration.cs:60`–`:63`; every socket centre is seated on its face):

| `PortId` | Role | Domain | Direction | Signal kind | Position (m) | Facing | Glyph |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `ActivationIn` | `PortRole.ActivationIn` | Signal | Input | Pulse | (0, 0.625, 0.1), on the housing top | +Y | Text |
| `ActivationOut` | `PortRole.ActivationOut` | Signal | Output | Pulse | (0, −0.705, 0.1), inside the seated foot (sphere y −0.78 to −0.63 within −0.785 to −0.625; z 0.025 to 0.175 within ±0.425) | −Y | Text |
| `PowerIn` | `PortRole.PowerIn` | Electrical | Input | — | (−0.825, 0, 0), on the housing's −X face | −X | Text |
| `Supply` | `PortRole.Supply` | Electrical | Output | — | (0.825, 0, 0), on the housing's +X face | +X | Text |
| `ResetIn` (new) | `PortRole.ResetIn` | Signal | Input | Pulse | (0.62, 0.625, 0.1) **proposed**, on the housing top | +Y | `SocketGlyph.Reset` |

`PortId.ResetIn`, `PortRole.ResetIn` and `SocketGlyph.Reset` exist (`CuriousContraptions.Abstractions/PortId.cs:23`, `PortDescriptor.cs:24`, `:38`) and are used by the State latch (`elements/StateLatch/StateLatchElementDeclaration.cs:36`). The position is on the top face beside the count input, 0.62 m to the right, so the two top sockets are 0.47 m apart edge to edge and cannot be confused.

**Presentation bindings.** The Counter's 27 bindings unchanged (`CounterElementDeclaration.cs:13`–`:39`): `Visual/DotLamp_i` albedo R, G, B ← `PresentationDriver.CommittedCount`, slate (85, 101, 115) → gold (247, 203, 82), SmoothStep 0.1 s, threshold i + 1, node `signal`. After a reset the committed count is 0, so every dot returns to slate through the same binding; no new driver.

**Variants.** The requirements row names no variant; the base declaration is the only required mode.

### Art and solidity

| Node | Mesh | Class | Reason / collider |
| --- | --- | --- | --- |
| `Housing`, `Foot` | boxes, gold `#e8b764` and navy `#293954` | solid | box colliders |
| `Face` | cream `#fff8e9` panel standing proud of the housing | solid | box collider (one of the delivered fifteen) |
| `DotLamp_0`–`8` | lamp solids | solid | nine colliders, as the Counter |
| Sockets `ActivationIn`, `PowerIn`, `Supply`, and the new `ResetIn` | spheres r 0.075 m | solid | four sphere colliders (three delivered, one new) |
| Socket `ActivationOut` | sphere r 0.075 m | enclosed | lies wholly inside the foot box, whose collider covers it; no collider of its own (as delivered) |
| `ResetRing` | navy ring mark beside `ResetIn` | decorative | printed flat on the top face (the earlier "hollow ring" is drawn as a flat quad; a proud ring would need a torus collider for no gameplay gain) |

The delivered Counter is not listed as a finding in `spec-engine-art-solidity.md`. Icon: the Counter's dots with a ring at the top right. Told from the Counter by shape: a second top socket with a ring mark; the Counter has one top socket and no ring.

### Behaviour and numbers

- **Count law** (delivered, with one inherited repair): each distinct accepted occurrence on `ActivationIn` increments once; the one that reaches `target_count` pushes one `ThresholdReached` occurrence in the same tick and closes the contact (`engine/gpu/ActivationNetwork.cs:531`–`:541`). Emission and the contact are correct, but the delivered count does not hold at the target: later arrivals keep raising it (`engine/gpu/ActivationMemory.cs:13`), so a default counter lights a fourth to ninth dot. The saturating-count row of `ENGINE-SIGNAL-NODES` repairs this in the generic pulse-count law ([CAT-020](CAT-020-counter.md) repair 1); after it, later arrivals leave the count at the target. EL-183 inherits the repair and needs it before its own slice.
- **Reset law** (new): an occurrence on `ResetIn` sets count = 0, returns the node to phase Clear, opens the contact and emits nothing. It is never counted as an arrival (requirement outcome). After a reset the node can reach its target and emit again.
- **Same-tick order.** The delivered counter counts inline as each occurrence is routed (`:531`–`:541`); only State latch requests are collected (`:525`–`:530`) and resolved once per node after all routing of the tick (`:307`–`:318`). An inline reset would make the result depend on which source is routed first, and a resolve after all routing, as the latch's, would come too late to route a crossing's `ThresholdReached` in its own tick. So the rule, stated once for every pulse-count node (the count-reset row of `ENGINE-SIGNAL-NODES`): count and reset requests routed to a pulse-count node in a tick are resolved once for that node, after the last occurrence routed to it in that tick and before its own output is routed. Same-tick routing is acyclic (`ComposableSignalNodesTests.InvalidLocalReferencesAndInstantaneousCyclesAreRefused`, `CuriousContraptions.tests/ComposableSignalNodesTests.cs:140`), so this order exists. Any reset gives count 0, phase Clear and no emission, and the tick's arrivals are not counted, even one that would have crossed the target. Otherwise arrivals count in occurrence order, and a crossing emits one `ThresholdReached`, routed onward in that same tick as today. This moves the delivered Counter's inline count to the per-node resolve; it is one rule for both packages, not a second path, and a Counter with no reset wire behaves as under the saturating-count row; the count-reset rule itself changes no existing Counter outcome. (The saturating-count row does change one pinned count: `MemoryGuardTests.cs:147` pins 12 past the target and is corrected by that row.)
- **Tick order.** Routing is same-tick (`:505`–`:512`): an arrival that crosses the target emits in the tick it arrives, and a Signal lamp or a second counter on `ActivationOut` sees it in that tick. The contact follows the node's signal state, and a supplied load sees a closed or opened contact from the next tick's electrical solve; that signal-state-to-electrical lag is the only one-tick lag (`LatchSupplyFixtureTests.SetClosesRouteOneTickAfterStrikeWithoutTopologyChange` measures it for the latch).
- **Numbers.** `target_count` 1–9, default 3 (requirement [current-cat-020](../requirements.md#current-cat-020)). Nine dots is what the face shows: after the saturation repair, dots 1 to `target_count` light and the dots past the target never light ([CAT-020](CAT-020-counter.md) repair 2, inherited). No physical number affects behaviour.
- **Consistency.** Inputs accept the Pulse outputs of the Impact switch, Ball detector, System clock, Delay box and the edge detectors. A Level output (State latch, Pressure plate) cannot drive either input (`WorkshopEditRejection.LevelDrivesLevelInputOnly`, `engine/gpu/WorkshopConnections.cs:147`–`:150`).

## 3. Engine capabilities

Families ([element map row](../general-engine-element-map.md); [binding](../../coverage/engine/element-02.json), proof owner S404): SignalPropagation. Map composition: "Discrete integer counter state and separately addressed reset; distinct occurrence IDs avoid duplicate counting." The harvest-baseline "Missing" list (Stories 8.1, 9.2, 9.4) is delivered. Probe on `8d1d68aa`, corrected 22 November 2026: 5 present, 0 present-untested, 2 narrower, 0 missing.

| # | Capability | Mark | Symbol | Exercising test |
| --- | --- | --- | --- | --- |
| 1 | Pulse-count node: counts each occurrence once, emits once at target, saturates | **narrower**: counts once and emits once, but the count keeps rising past the target (`engine/gpu/ActivationMemory.cs:13`; `MemoryGuardTests.cs:147` pins a count of 12) | `SignalPulseCountDescriptor`, `CuriousContraptions.Abstractions/SignalContractValues.cs:63`; `engine/gpu/ActivationNetwork.cs:531` | `CounterTests.ActivationNetwork_CountingProgression_SingleEmission_AndSaturation` (`CounterTests.cs:240`) asserts emission and contact, not the count. Owner: the saturating-count row of `ENGINE-SIGNAL-NODES` |
| 2 | Count in the committed checkpoint; superseded candidate leaves it unchanged; Workshop Reset clears it | present | `engine/gpu/ActivationNetwork.cs:252`–`:253` | `CounterTests.CounterCount_LivesInTheCommittedCheckpoint_SupersededCandidateLeavesItUnchanged_AndResetClearsIt` (`:326`) |
| 3 | A `Reset` terminal on the pulse-count node (clear, re-arm, no emission), with count and reset requests of a tick resolved once per node | **narrower**: `SignalTerminal.Reset` is a pulse terminal only on a memory node (`CuriousContraptions.Abstractions.Signal/SignalContracts.cs:19`); a `ResetIn` edge is routed only to a latch (`engine/gpu/ActivationNetwork.cs:527`); the counter counts inline (`:531`–`:541`) | — | none. Owner: the count-reset row of **`ENGINE-SIGNAL-NODES`** (owner decision 3a; row text proposed in `readiness/batch-h.md` section 4 under its earlier draft name) |
| 4 | Contact closed while the node is Latched | present | `SignalStateContactControl`, `CuriousContraptions.Abstractions/SignalContactControl.cs:12`; `engine/gpu/ActivationNetwork.cs:242` | `CatalogueIntegrationTests.AClockCountsTwoPulsesThenTheSuppliedCounterOpensItsGate` (`CatalogueIntegrationTests.cs:69`) |
| 5 | Request resolution at the tick boundary with Reset dominant, independent of source order | **present for the State latch only** (counted as present; the pulse-count form is row 3) | `engine/gpu/ActivationNetwork.cs:308`–`:313` | `StateLatchTests.Network_SameTickConflictResolvesOffWhicheverSourceIsOrderedFirst` (`StateLatchTests.cs:320`) |
| 6 | `CommittedCount` presentation driver | present | `CuriousContraptions.Abstractions/ElementPresentation.cs:22` | `ComposableAnimationMappingTests.DriverRules_Supports_LevelDriversSupportLinearAndTweenClipsOnly` (`ComposableAnimationMappingTests.cs:95`) admits the driver's mapping; `ComposableSignalNodesTests.InvalidLocalReferencesAndInstantaneousCyclesAreRefused` (`:140`) refuses a binding to a missing or non-count node. No located test asserts a dot's colour against a committed count, so row C9 is this slice's own proof |
| 7 | Whole-number parameter with typed refusal | present | `elements/Counter/CounterElementDeclaration.cs:53` | `CounterTests.TheDeclaredTargetIsAWholeNumberFromOneToNine` (`:163`); `PalettePlacementTests.TheCounterTargetEditorAdmitsOneToNineRefusesTheRestAndAppliesAsOneUndo` (`:268`) |

**What the count-reset row of `ENGINE-SIGNAL-NODES` must deliver** (engine tier; one generic law, no element identity): `Reset` is a Pulse terminal on a pulse-count node. Count and reset requests routed to a pulse-count node in a tick are resolved once for that node, after the last occurrence routed to it in that tick and before its own output is routed; same-tick routing is acyclic, so this order exists. Any reset gives count 0, phase Clear and no emission. Otherwise arrivals count in occurrence order, and a crossing emits one `ThresholdReached`, routed onward in that same tick. Bundle validation, wire and TypeScript rows as needed; Workshop Reset and the checkpoint rules unchanged; existing Counter tests stay green unchanged. Two qualification fixtures with different targets.

**Dependencies.** Activation sources for counting and resetting (Impact switch, Ball detector, System clock: delivered); Battery and a supplied load for the contact (delivered).

### Engine conformance (22 November 2026)

Checked against the owner's engine-conformance standard of 22 November 2026 ([engine-conformance.md](readiness/engine-conformance.md); [element-free engine design](../../../_bmad/custom/element-free-engine-design.md); AGENTS.md, Tri-Graph architecture). Verdict: **Conforms**: each gap has one owning story (rows 1 and 3: the saturating-count and count-reset rows of `ENGINE-SIGNAL-NODES`; the node-kind name: `ENGINE-CAPABILITY-INSTANCES`, finding E1).

1. **Declaration data only.** The package is a declaration, a scene and bindings. `engine/`, `CuriousContraptions.Simulation`, `.Core`, `.Bundle` and the abstractions projects contain no reference to the package id `counter` or its class (searched 22 November 2026). **Finding E1 (owned):** the engine's internal node kind for a pulse-count node is named after the product, `ActivationNodeKind.Counter` (`engine/gpu/ActivationNetwork.cs:165` maps it to the declared `SignalNodeKind.PulseCount`; used at `:82`, `:156`, `:229`, `:531` and `engine/gpu/ActivationMemory.cs:11`). It branches on a node kind any package may declare, not on an element, so it is a naming residue and not element logic; `ENGINE-CAPABILITY-INSTANCES` renames it to `PulseCount` through compiler, wire, read and the TypeScript mirror and deletes the old name (engine design audit finding E1).
2. **Generic laws.** Counting and reset: the signal network's pulse-count rule with a declared target, resolved once per node per tick. Contact: the supply network rule "output available = input available ∧ node state Latched".
3. **Three layers.** Colliders: sixteen on the one static body (three boxes: housing, foot, face panel; thirteen spheres: nine dot lamps, four sockets); nothing reads art. Visuals: those solids, the enclosed `ActivationOut` socket and the flat printed `ResetRing`; no art node is a collider. Animation: the nine dot lamps are recoloured by presentation bindings (`CommittedCount`). What moves each art node: the nine dot lamps, a binding (colour only); housing, foot, face, sockets and `ResetRing`, nothing (one static body, no pose change).
4. **One-way flow and typing.** Signal network → committed count → presentation; nothing flows back. `target_count` is a whole number compiled to an integer; ports are `PortId`.
5. **Flat state.** Bodies 1 (static); colliders 16 (box 3, sphere 13); joints 0; drives 0; sensors 0; domain nodes 2 (one pulse-count signal node, one supply contact); stores 0; presentation bindings 27 rows on nine lamps.
6. **Performance fitness.** Static body. Sixteen colliders is one more than the delivered Counter's fifteen; the reason is the one new proud socket. Nine of the sixteen exist only because the dot lamps stand proud (visible = solid); redrawing the dots flush would remove nine colliders from this package and from `counter`, a visible-solid choice for the delivered part that is not made here. The part makes no spatial query (nothing from `ENGINE-SPATIAL-QUERIES`) and has no dynamic body (nothing waits for `ENGINE-ISLANDS-SLEEP`). No per-part collider or binding limit is stated in the documents or code read here, and none is assumed.
7. **Tests are scenarios.** C1–C10 run the production simulation; none depends on a counter-specific code path.

## 4. Sources and legacy

- Requirement row [element-183](../requirements.md#element-183): "Counts distinct supported arrivals and accepts a separately addressed reset input"; outcome "Reset clears the count without manufacturing a new arrival". [current-cat-020](../requirements.md#current-cat-020): target 1–9/default 3, emits threshold once, latches a separately supplied contact until Reset; held/busy/extra triggers cannot count every tick.
- Named entry [element-183](../invest/named-elements.md#element-183), owner S404.

| # | Legacy fact | Source | Disposition |
| --- | --- | --- | --- |
| R1 | Counter phase is Counting until count = target, then Reached; results Accumulated, Reached, Saturated. | `engine/SimulationCounters.cs@a6c914e:L16-L22` | carry forward as enums |
| R2 | Each target 1, 3, 9 with/without supply: each delivery counts once (held input not re-sampled), Reached emits once, the contact powers a load only with supply, 20 extra deliveries change nothing, Reset restores count 0. | `CuriousContraptions.tests/CounterTests.cs@a6c914e:L39-L88` | carry forward |
| R3 | Three physical detector crossings are needed; replay is identical regardless of entity order. | `CuriousContraptions.tests/CounterTests.cs@a6c914e:L110-L152` | carry forward |
| R4 | The authored target round-trips; a partial run count is not persisted; invalid targets (−1, 10, 1.5, NaN, ∞) reject. | `CuriousContraptions.tests/CounterTests.cs@a6c914e:L153-L189` | carry forward |
| R5 | The legacy counter is cleared only by Workshop Reset; it has no reset socket. | `parts/CounterPart.cs@a6c914e:L8-L9`, `parts/CounterPart.cs@a6c914e:L28-L34` | the reset input is new in EL-183 |
| R6 | Latch `ResetIn` is a typed Activation input carrying the Reset command; commands settle at the next tick boundary; reset dominates. | `parts/LatchPart.cs@a6c914e:L8-L9`, `parts/LatchPart.cs@a6c914e:L31-L31` | carry forward |

- Legacy art and port sources: `parts/CounterPart.cs@a6c914e:L21-L71`; catalogue `parts/catalog/counter.tres@a6c914e:L14-L14`; latch reset mark `parts/LatchPart.cs@a6c914e:L62-L62`.
- **Files harvested:** `parts/CounterPart.cs`, `parts/catalog/counter.tres`, `engine/SimulationCounters.cs`, `CuriousContraptions.tests/CounterTests.cs`, `parts/LatchPart.cs`, `reference/cpu/MachinePart.cs` (`AddBox` extents).

## 5. Acceptance outline

Acceptance authority: [element-183](../requirements.md#element-183), [current-cat-020](../requirements.md#current-cat-020). Planned class `ResetCounterTests`. Fixture: a System clock (or scripted occurrences through the network harness, as `CounterTests` does) → `ActivationIn`; a second source → `ResetIn`; `ActivationOut` → Signal lamp; Battery → `PowerIn`; `Supply` → Powered gate.

| Row | Given / When / Then | Planned test | Control | Boundary |
| --- | --- | --- | --- | --- |
| C1 | Given target 3, when three distinct occurrences arrive, then the count is 3, one `ActivationOut` is emitted, the contact closes and the gate is powered | `ResetCounterTests.ThreeArrivalsReachTheTargetEmitOnceAndCloseTheContact` | two arrivals: no emission, contact open; with `PowerIn` unwired, the target is reached and the contact closes, but the gate is never powered (no supply creates no electricity, [current-cat-020](../requirements.md#current-cat-020)) | targets 1 and 9 (theory) |
| C2 | Given count 2 of 3, when a reset occurrence arrives, then the count is 0, no occurrence is emitted, and the count stays 0 for 2 s of game time with no arrival (requirement outcome) | `ResetCounterTests.ResetClearsTheCountWithoutManufacturingAnArrival` | no reset: count stays 2 | reset at count 0 changes nothing |
| C3 | Given a reached counter with its contact closed, when a reset arrives, then the node is Clear in that tick, the contact is open and the supplied gate is unpowered from the next tick's electrical solve; three more arrivals reach the target and emit a second time | `ResetCounterTests.ResetAfterReachedOpensTheContactAndRearmsTheEmission` | without the reset, 20 extra arrivals emit nothing | — |
| C4 | Given a count input and a reset in the same tick, then the count is 0 after the tick, whichever source is ordered first. Given count target−1, an arrival and a reset in one tick: count 0, contact open, no `ThresholdReached`, in both source orders | `ResetCounterTests.SameTickResetDominatesWhicheverSourceIsOrderedFirst` (theory over both source orders and over counts 0 and target−1) | reset one tick before the arrival: count 1; at target−1 with no reset the arrival emits in that tick | same tick against adjacent ticks |
| C5 | Given a held source, then it counts once; given one source wired to both `ActivationIn` and `ResetIn`, then the count never leaves 0 | `ResetCounterTests.AHeldInputCountsOnceAndOneSourceOnBothInputsNeverCounts` | — | — |
| C6 | Given a partly counted run, when Workshop Reset then Run, then count 0, dots slate, contact open; a superseded candidate leaves the committed count unchanged | `ResetCounterTests.WorkshopResetRestoresZeroAndASupersededCandidateLeavesTheCountUnchanged` | — | — |
| C7 | Given the wired construction, when saved and loaded, then `target_count` and all five connections are equal port by port; the run count is not persisted | `ResetCounterTests.SaveLoadKeepsTheTargetAndAllFiveConnections` | — | — |
| C8 | Given a Level output or an electrical output wired to `ResetIn`, then the wire is refused with its typed reason and the construction is unchanged; `target_count` 0, 10, 1.5, NaN and ∞ are refused atomically | `ResetCounterTests.WrongKindWiresAndInvalidTargetsAreRefused` | a Pulse output is admitted; target 1 and 9 are admitted | 1 and 9 inclusive |
| C9 | Given counts and a reset, then dot i is gold while committed count ≥ i + 1 and all dots are slate after the reset | `ResetCounterPresentationTests.DotsFollowTheCommittedCountAndClearOnReset` | — | target 3 and 20 further arrivals: count 3, dots 1–3 gold and dots 4–9 slate (the inherited saturation repair) |
| C10 | Given a Counter and a Reset counter in one construction, then the delivered `counter` still has four ports and is unaffected by reset occurrences sent to the other part | `ResetCounterTests.TheDeliveredCounterIsUnchangedBesideIt` | — | — |

**Tolerances and stall guards.** Counts, typed states and refusals only; no float comparison. Absence windows are 2 s of game time. The only timing asserted is the defined same-tick rule of C4 and, in C3, the defined lag from signal state to the next electrical solve.

**Generic coverage today.** Admission, Run/Reset and Save/Load: `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad`. Wrong domain and direction: `PackageLocalPortsTests.WrongDomainAndWrongDirection_RefusesAtomically`. C7 and C8 keep their own rows until the S4 conformance suite is delivered.

**Chrome recipe (pending, post-catalogue).** Ball detector → Reset counter `ActivationIn`; a Switch → `ResetIn` (choose `ActivationOut → ResetIn`); counter `ActivationOut` → Signal lamp; Battery → `PowerIn`, `Supply` → supplied load. Three balls cross: dots light 1, 2, 3; the lamp lights once; the load runs. A ball presses the reset switch: count 0, dots slate, contact opens, no lamp event.

## 6. Open questions

All resolved; needs the owner: none.

1. **Separate entry or extended counter — decided: a separate package `reset_counter`** (D8: an identity that is not a duplicate gets its own catalogue entry; D19: the spec's proposal). Reason: the Counter's early lessons keep a three-socket part with nothing to mis-wire, and the reset lesson introduces a visibly different part. The two packages share one node law; there is no second rule. This also answers CAT-020 open question 2.
2. **Same-tick order — decided: any reset in a tick wins, resolved once per node** (coordinator decision, 22 November 2026, under the owner's standing delegation). Reason: the delivered counter counts inline and emits in the same tick, so "reset dominates" has to be a per-node resolve or the result would depend on source order; the State latch already resolves its requests that way, so the player learns one rule for every reset socket. The earlier text, which rested on latch semantics the counter does not have, is withdrawn.
3. **f32 activation and cosmetic lanes — resolved** by Story 9A.44.
4. **Re-emission after a reset — decided: yes** (standing delegation). Reason: a counter that could be cleared but never fire again would make the reset socket useless for repeating machines.
5. **Coordinator decisions, 22 November 2026 (owner's standing delegation).** (a) A separate `reset_counter` package is confirmed (item 1); the delivered `counter` keeps its ports. (b) Coverage uses the binding form published on main; reason: owner decision of 21 November 2026. (c) Palette order 403 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)); reason: batches H, I and J chose overlapping numbers and the bundle refuses duplicates. (d) The reset terminal is the count-reset row of `ENGINE-SIGNAL-NODES` (owner decision 3a); the row text proposed in the batch summary feeds that story. (e) EL-183 inherits CAT-020's saturation repair and nine-dot display (Batch B hand-back, `readiness/batch-b.md` decision 1).

## 7. Integration

- **Collaborators.** Sources: Impact switch, Ball detector, System clock, Delay box (delivered); [EL-181](EL-181-rising-edge-detector.md)/[EL-182](EL-182-falling-edge-detector.md) (planned before or after; not required for this slice's acceptance). Consumers: Signal lamp, Powered gate, Motor (delivered).
- **Campaign.** Logic row of [campaign-element-coverage](../requirements.md#campaign-element-coverage): "counter/reset input", first use 21–40 (reuse 41–80, 101–150); "a new rearm/reset mode needs its own objective", so the reset input needs a lesson of its own, authored in a level slice; if that level pre-places the part it needs the lesson-fixture story ([S8](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package)), which lands after the engine programme (owner decision 22), so such a level waits for it. No level in `content/puzzles.json` places it today.
- **Processes.** [IX-07 signal propagation](../requirements.md#interaction-07) (count and reset inputs), [IX-06 electrical power transfer](../requirements.md#interaction-06) (supplied contact); Counter retained behaviour [todo-269](../requirements.md#todo-269).
- **Coverage binding (planned, not yet live).** The package binding section that main's `tools/Coverage/NamedPackageSources.cs` reads, in the form published for EL-239 (Package `reset_counter`, Named identity, Acceptance, Decision, Mode records), added to this spec. **Blocked on the coverage follow-on story S7:** the published form's preconditions are not met. The EL-183 ledger entry has no `todo-NNN` link (the reader needs exactly one; its source is the campaign coverage row); no `todo`/`sequence-task` pair for this identity is co-anchored in `requirements.md`; and owner S404 has no `<a id="s404">` anchor in `docs/planning/invest/scope-corrections.md`. The section is not written until those exist, because the reader throws on a malformed block.

## 8. Delivery

- **Slice shape.** Single element.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), in particular `ENGINE-CAPABILITY-INSTANCES` (the node-kind rename) and the saturating-count and count-reset rows of `ENGINE-SIGNAL-NODES` (engine tier, six layers; they change how the delivered Counter resolves a tick, so they re-run `CounterTests` and `CatalogueIntegrationTests`); then S7. S3 and S4 are optional.
- **Files the slice adds or edits.** `elements/ResetCounter/` (project, declaration, scene, icon); regenerated `elements/elements.bundle.json`; `CuriousContraptions.slnx` and `CSharpStaticGateTests.cs:703` (until S3); `CuriousContraptions.tests/ResetCounterTests.cs` (compiled by the test project's one top-level rule; no project-file line); `tools/Coverage/` capability requirement tables; this spec's binding section once S7 admits it; sprint status and `TODO.md`. Nothing in `elements/Counter/` changes.
- **Review tier.** Data-only, provided the planning probe shows rows 1 and 3 present.
- **Disqualifiers (the slice's probe re-checks).** The count-reset row delivering a reset that emits, or that cannot re-arm the emission (C2, C3 fail); a reset whose result depends on source order, or a crossing that survives a same-tick reset (C4 fails); `SignalStateContactControl.Latched` not following the cleared state (C3); S7 unpublished; an owner decision to add `ResetIn` to `counter` instead, which would turn this into an edit of the delivered package and remove the planned package id.
