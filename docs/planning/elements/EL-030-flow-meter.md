# EL-030 · Flow meter — named-identity spec

**Current delivery (24 October 2026):** [EL-030 delivery slice](../invest/named-elements.md#element-030). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes.

Brought to build-ready detail on 22 November 2026 against checkout `8d1d68aa` and repaired the same day against the independent audit `docs/planning/elements/readiness/audits/elements-batch-g.md` and the owner decisions of 22 November 2026. Liquid conventions L1 to L18 and probe rows W1 to W24 are in [EL-001](EL-001-finite-reservoir.md), which has adopted Batch G's LG1 to LG8 into L3 to L15; LG numbers are kept as short names (LG1 = L5 and L6; LG2 = L3 and L7; LG3, LG4, LG5 = L4, L11, L12; LG6 = L15; LG7 = L12; LG8 = L13). The further additions adopted by coordinator decision 21 (nesting and envelope shielding, buoyancy reaction on a carrying body, contents at the liquid centroid, overlapping shapes counted once, porous and sealed interiors, a mouth below the floor datum, the relief mouth, the crest rule with granted work head, no crest lift limit) are now written into EL-001's rows (its "Batch G's additions" paragraph maps A1 to A9 to L3, L4, L5, L7, L8, L9 and L12; the long wording stays in [readiness/batch-g.md](readiness/batch-g.md) section 2) and are cited here as **L-additions A1 to A9**. Probe rows G1 to G8 are in section 3 of that file. All are cited by number and not restated. Box sizes are full extents in metres.

This spec owns the **meter-family body, sockets, ports, switch form and lamp**; [EL-031](EL-031-volume-meter.md) and [EL-032](EL-032-pressure-meter.md) cite them.

## Identity

| Field | Value |
| --- | --- |
| Identity | EL-030 · Flow meter · Water |
| Anchors | [element-030](../requirements.md#element-030), co-anchored `sequence-task-583`; source record [todo-339](../requirements.md#todo-339); integration [sequence-task-390](../requirements.md#sequence-task-390); ledger [element-030](../invest/named-elements.md#element-030), owner S451 |
| Disposition | Member of the Family slice "Meters" with [EL-031](EL-031-volume-meter.md) and [EL-032](EL-032-pressure-meter.md) ([delivery batches 1c](../invest/delivery-batches.md#1-family-slices-and-runs)). The three differ in the committed observation they read, its unit and limits, the socket count and the dial art. Each keeps its own entry (D8) |
| Planned package | id `flow_meter`, directory `elements/FlowMeter/`, title "Flow meter" |
| Status | Not started. Blocked on the engine programme (owner decision 3a), then `ENGINE-SIGNAL-NODES`, `ENGINE-GAS-NETWORK` and `ENGINE-LIQUID` |
| Related | [EL-018](EL-018-electronic-level-switch.md) (the same switch form on a depth), [EL-003](EL-003-tap.md), [EL-008](EL-008-straight-water-pipe.md) |

## Declaration

Closest delivered shape: the Pressure plate, whose signal node closes a route from `PowerIn` to `Supply` (`elements/PressurePlate/PressurePlateElementDeclaration.cs:45`) and lights its studs from that node's committed phase (`:14`–`:25`); closest planned shape: the Level switch.

### Meter-family body (shared by EL-030, EL-031, EL-032)

- **Interfaces.** `ISceneGeometryElement`, `ISupplyElement`, `ISignalElement`, the liquid fitting declaration of `ENGINE-LIQUID` on the one fluid port record of `ENGINE-GAS-NETWORK` (W4, W7).
- **Body.** One static body, mass 0. Origin at the tube centre; the tube axis is local X.
- **Colliders (two).** Tube: bore 0.10, outer radius 0.14, length 0.30, axis X. Dial case: box 0.24 × 0.24 × 0.08 at (0, 0.26, 0), standing on the tube with its face toward +Z. Family vessel material for every pipe-bore tube (EL-001: restitution 0.12, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s), the Tap's material.
- **Liquid node.** One fitting, zero volume, no seat: it is lossless (L7), so inserting a meter does not change a route's flow. It declares no store and so no thermal node.
- **Sockets (pipe bore, Bidirectional).** Declared snap sockets (L5), capture distance 0.20 m and angle 20°: `Start` at (−0.15, 0, 0), normal −X; `End` at (+0.15, 0, 0), normal +X. An unjoined `End` is a free end (L8). (EL-032 declares `Start` only.)
- **Switch form.** One threshold signal node on the part's committed reading, with an on level, an off level and a 0.1 s hold (D10); one supply route `PowerIn` → `Supply`, closed while the node is on (W17). The meter grants no energy of its own. The threshold node is `ENGINE-SIGNAL-NODES` (coordinator decision 16).
- **Lamp.** `Visual/Lamp` albedo red, green and blue, three bindings in the Pressure plate's form: `PresentationValueMapping.StateValues` naming the part's signal node, slate (85, 101, 115)/255 when off and gold (247, 203, 82)/255 when on (`elements/PressurePlate/PressurePlateElementDeclaration.cs:14`–`:25`, driver `PresentationDriver.CommittedContactLoadPhase`). The delivered driver reads a contact-load node only; reading the committed phase of the general threshold node is part of `ENGINE-SIGNAL-NODES`, which delivers that node.

| `PortId` | Role | Domain | Direction | Local position, normal |
| --- | --- | --- | --- | --- |
| `PowerIn` | `PortRole.PowerIn` | `SimulationDomain.Electrical` | Input | (−0.12, 0.26, 0), −X |
| `Supply` | `PortRole.Supply` | `SimulationDomain.Electrical` | Output | (+0.12, 0.26, 0), +X |

### This part

- **Reading.** The magnitude of the committed flow through the fitting, m³/s (L13).
- **Node levels.** On at `threshold`; off at `threshold` − 0.005 m³/s.

| Parameter | Type | Unit | Default | Min | Max | Editor policy |
| --- | --- | --- | --- | --- | --- | --- |
| `threshold` | Real f32 | m³/s (unit member missing, W13) | 0.02 | 0.01 | 0.08 | Reject; build only |

- **Presentation.** `Visual/Needle` `LocalRotationZ` from the committed reading, 0 to 0.10 m³/s, clamped (W12, missing). `Visual/Lamp` as the family lamp above.
- **Palette.** `ElementCategory.Control`; palette order 417 (EL-031 418, EL-032 419; coordinator decision 36, [palette allocation](readiness/palette-allocation.md)); colour cyan (102/255, 184/255, 201/255).

### Art and solidity

| Node | Mesh and size | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Body` | Tube 0.30 long, outer radius 0.14, cream `#fff8e9` | solid; the Tube collider (true hollow) | body pose (static) |
| `Visual/Case` | Box 0.24 × 0.24 × 0.08, cyan `#66b8c9` | solid; its box collider | body pose |
| `Visual/Dial` | Cream face with navy `#293954` ticks and a printed arrow glyph | decorative: printed flat | nothing |
| `Visual/Needle` | Gold `#f7cb52` needle, flush on the face | decorative: printed flat, moved by a binding | presentation binding |
| `Visual/Lamp` | Flush disc on the face | decorative: printed flat (glow) | presentation binding |
| `Loc_Port_mouth_start`, `Loc_Port_mouth_end`, `Loc_Port_PowerIn`, `Loc_Port_Supply` | Locators | none | nothing |

Icon: a short pipe carrying a square dial with an arrow. The three meters differ by the dial glyph, not colour: an arrow (flow), a stack of bars (volume), a "P" in a ring (pressure). It differs from the Tap by the square dial in place of the bonnet and lever.

### Behaviour and numbers

- **It reads what moves.** For a full Water tank → meter → open Tap with a free end there are three apertures (the meter adds none): K = K₀/√3 = 0.08349/1.732 = 0.0482, so the reading is 0.0482·√d: **0.0482 m³/s** at depth 1.0 m, 0.0482 × √0.25 = 0.0241 at 0.25 m. The tank empties in 2 × 1.0 × 1.0 / 0.0482 = 41.5 s and holds (1 − t/41.5)² after t.
- **Stopped flow reads zero.** With the tap at `opening` 0 the reading is under 0.0005 m³/s (L17) while the tank still holds 1.0 m³. The reading is flow, never stored volume.
- **Switching.** With the default `threshold` the node is on from the start, and turns off when the reading falls to 0.015 m³/s, at depth (0.015 / 0.0482)² = 0.097 m, that is 41.5 × (1 − √0.097) = 41.5 × 0.689 = **28.6 s** into the drain, plus the 0.1 s hold: **28.7 s**. The route then opens: a Powered gate ([CAT-051](CAT-051-powered_gate.md)) on `Supply` loses supply and its spring closes the blade while water still trickles (at 35 s the reading is 0.0482 × (1 − 35/41.5) = 0.0075 m³/s). The blade's return (spring 14 N/m, damping 4 N·s/m, 1 kg) decays at 4 ÷ (2 × 1) = 2 /s, so from 1.55 m it is below 0.05 m after ln(1.55 ÷ 0.05) ÷ 2 = 1.7 s. A Signal lamp cannot be the load: it is a signal load with `ActivationIn` only (`elements/SignalLamp/SignalLampElementDeclaration.cs:35`).
- **The band.** A tank authored at 0.13 m³ gives a first flow of 0.0482 × √0.13 = **0.0174 m³/s**, between the off level (0.015) and the on level (0.02): a node that starts off stays off. Authored at 0.18 m³ the first flow is 0.0482 × √0.18 = 0.0204, above 0.02: on at once, off after 41.5 × (√0.18 − √0.097) = 4.7 s.
- **Direction.** The reading is a magnitude; flow in either direction reads the same.
- **Tick order.** The reading commits with the tick; the node is evaluated with the other sensors after the last substep; the electrical solve of the next tick reads it (L16, D7).
- **Load.** The route passes whatever the source grants: 120 W and 1 J per tick from a default Battery.

## Engine capabilities

Map families (navigation only, D14): EnvironmentState, FiniteLedger, FluidAdvection, GeometryQuery, JointConstraint, PressureWork, RigidBodyDynamics, SignalPropagation, StateTransaction, TopologyTransaction.

| Row | Mark | Use here |
| --- | --- | --- |
| W1 static body; W2 Tube collider | present | body |
| W17 switched supply route closed by a signal state | present | `PowerIn` to `Supply` |
| W22, W23 authored state, wire refusal, parameter refusal | present | ports, `threshold` |
| W4 fluid port contract | present, untested | fitting and sockets |
| W3 snap sockets | narrower: socket count, package-local ids, capture distance 0.20 m and angle 20°, and outlets that are not joinable are `ENGINE-CAPABILITY-INSTANCES`; compiling joins to edges is `ENGINE-LIQUID`; the port record is `ENGINE-GAS-NETWORK` | 2 sockets, 0 immersed ports, 0 outlets |
| W16 threshold node on a committed scalar | narrower | delivered for contact load only; the general node is `ENGINE-SIGNAL-NODES` (owner decision 3a and coordinator decision 16) |
| Lamp from the threshold node's committed phase | narrower | delivered for contact-load nodes (`PresentationDriver.CommittedContactLoadPhase` with `StateValues`, `elements/PressurePlate/PressurePlateElementDeclaration.cs:14`–`:25`; `ComposedPhysicalSignalTests.TwoLoadFacesOnOneStaticFrameKeepTheirOwnState`, `CuriousContraptions.tests/ComposedPhysicalSignalTests.cs:121`); general threshold node: `ENGINE-SIGNAL-NODES` |
| W7 fitting and flow; the committed edge-flow observation (L13) as a threshold input | missing | `ENGINE-LIQUID` |
| W12 driver for a committed flow; W13 m³/s unit | missing | `ENGINE-LIQUID` |

Counts, one per capability: 5 present (W1, W2, W17, W22, W23), 1 untested (W4), 3 narrower (W3, W16, lamp), 4 missing (W7, edge-flow observation, W12, W13). No thermal row: the part declares no store. `CommittedSupply` is not used for the lamp: validation refuses it unless the node is a `SupplySourceDescriptor` (`CuriousContraptions.Bundle/SupplyBundleValidation.cs:183`–`:185`).

### Engine conformance (22 November 2026)

1. **Declaration data only.** A static body, two colliders, one fitting, two sockets, one threshold node, one supply route, two ports, one parameter, four bindings.
2. **Generic laws.** The reading is a committed observation of the network (L13). The switch is the general threshold node and the delivered switched route. The lamp reads the node's committed phase. No row says "when water passes, do X".
3. **Three layers.** *Colliders:* a Tube and a box. *Visuals:* the table above. *Animation:* needle and lamp read committed state.
4. **One-way flow and typing.** Liquid → observation → signal → next tick's electrical solve; physics → presentation. Port ids and roles are the delivered enums; socket ids are package-local typed ids.
5. **Flat-state counts.** Bodies 1 (static); colliders 2 (1 Tube, 1 box); joints 0; drives 0; sensors 0; signal nodes 1 (threshold); supply routes 1; liquid nodes 1 fitting; thermal nodes 0 (no store); sockets 2; immersed ports 0; outlets 0; wire ports 2; bindings 4 (needle 1, lamp 3); parameters 1.
6. **Performance fitness.** Two colliders against the Pressure plate's ten. The fitting merges with its neighbours and adds no unknown (L7); reading it is constant work.
7. **Tests are scenarios.** Rows F1 to F8.

## Sources and legacy

- Requirement: "Measures actual volume per simulation time through its ports"; outcome "Stopped flow reads zero despite upstream stored volume." No variants.
- Integration [sequence-task-390](../requirements.md#sequence-task-390) ("each device/measurement remains a distinct item"); refinement [S708](../invest/refinements.md#s708) ("Measure actual flow"). Component research [water](../../component-research.md#water): "threshold controls do not power actuators".
- **Legacy.** None at `a6c914e` (searched for "flow meter", "meter", "water", "liquid"). **Files harvested:** none.

## Acceptance outline

Authority: [element-030](../requirements.md#element-030). Tolerances L17. Planned class `FlowMeterTests`. The route is a Water tank → meter → Tap (`opening` 1 unless stated) with a free end over a Catch basin; the tank is full unless stated.

| # | Given / When / Then | Planned test | Control | Boundary |
| --- | --- | --- | --- | --- |
| F1 | Given the tap open, when sampled at tank depths 1.0 m and 0.25 m, then the reading is 0.0482 and 0.0241 m³/s within 10% | `FlowMeterTests.TheReadingIsTheCommittedFlow` | F2 | — |
| F2 | Given the tap at `opening` 0 and the tank full, when Run advances 10 s, then the reading is under 0.0005 m³/s at every sampled tick and the tank keeps 1.0 m³ within 0.004 m³ | `FlowMeterTests.StoppedFlowReadsZero` | F1 | — |
| F3 | Given the same route with and without the meter, then the tank volume after 12 s agrees within 0.004 m³ (hand 0.505 m³) | `FlowMeterTests.TheMeterAddsNoLoss` | — | — |
| F4 | Given a Battery on `PowerIn` and a Powered gate ([CAT-051](CAT-051-powered_gate.md)) on `Supply`, when Run advances 35 s (the level declares a Run length of 35 s (`ENGINE-RUN-LENGTH`)), then the gate is supplied and its blade raised at first, loses supply from 28.7 s within 10% (by 31.6 s), and its blade is below 0.05 m within 3 s after, while the reading is still at least 0.0005 m³/s at 35 s | `FlowMeterTests.TheRouteFollowsTheThreshold` | F5 | `threshold` 0.08: the gate is never supplied |
| F5 | Given no Battery, then the gate is never supplied (blade closed) and the needle still shows the flow | `FlowMeterTests.ReadingNeedsNoPower` | F4 | — |
| F6 | Given the tank authored at 0.13 m³ (first flow 0.0174 m³/s, between the off and on levels), when Run advances 10 s, then the node stays off and the gate on `Supply` stays unsupplied with its blade closed | `FlowMeterTests.TheBandPreventsChatter` | the tank authored at 0.18 m³: on at once, off after 4.7 s within 10% | — |
| F7 | Given `threshold` below 0.01, above 0.08 or non-finite, then it is refused with a typed reason | `FlowMeterTests.AThresholdOutsideItsLimitsIsRefused` | limits admitted | next f32 beyond each limit |
| F8 | Given a run, then Reset restores the node to off and the lamp to slate, and save and load keep `threshold`, the pose, the joins and both wires | `FlowMeterTests.ResetAndSaveLoadKeepTheMeter` | the state before Reset differs | — |

Generic cover: W22 (a signal wire on `PowerIn` is refused), W23. Chrome recipe (pending): snap the meter between a tank and a tap, wire a Battery and a Powered gate, open the tap.

## Open questions

All resolved; none needs the owner.

1. A switched contact as well as a dial: yes. The integration row asks that measurements "react to the actual fluid state", and a dial alone gives a machine nothing to act on. It is the Level switch's form, so no new capability.
2. Signed reading: no (D19).
3. A second, analog output: none; no delivered port carries a scalar.
4. Lamp driver: the node's committed phase in the Pressure plate's `StateValues` form (coordinator decision for the batch); the reading of a general threshold node's phase belongs to `ENGINE-SIGNAL-NODES`.

## Integration

- **Works with.** Battery, Powered gate (delivered); EL-001, EL-003, EL-004, EL-008, EL-022, EL-167 (not delivered).
- **Campaign.** Chapter 7, levels 61–70; reuse 81–100, 126–140, 146–150. Needs the lesson-fixture story (S8).

## Delivery

- **Slice shape.** Family slice "Meters" (EL-030, EL-031, EL-032).
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), in particular `ENGINE-SPATIAL-QUERIES` (stream samples, immersion and point queries, L18), `ENGINE-ISLANDS-SLEEP` (a change of contents or level wakes what it loads; not delivered, and nothing here assumes sleeping exists) and `ENGINE-CAPABILITY-INSTANCES` (declared sockets, outlets and immersed-port locators); then `ENGINE-GAS-NETWORK` (the one fluid port record); then `ENGINE-LIQUID`; then `ENGINE-THERMAL` (every liquid store is a thermal node, owner decisions 18 and 20); `ENGINE-SIGNAL-NODES` (threshold node and its lamp phase); `ENGINE-RUN-LENGTH` (row F4). Then EL-001, EL-003, EL-004; S7.
- **Files.** Adds `elements/FlowMeter/FlowMeter.csproj`, `FlowMeterElementDeclaration.cs`, `flow_meter.tscn`, `icon.svg`, `CuriousContraptions.tests/FlowMeterTests.cs`; edits `CuriousContraptions.slnx` and this spec. Other lists as EL-001.
- **Review tier.** Data-only.
- **Disqualifiers.** A threshold node that cannot take a liquid observation; no presentation driver for a threshold node's committed phase; a fitting that adds loss; no m³/s unit.

## Coverage binding (planned, not yet live)

Blocked on the coverage follow-on story S7. The named-identity binding section (the EL-239 form) cannot be written yet: umbrella `todo-339`; `sequence-task-583` co-anchored with `element-030`, not with a todo; no `s451` anchor.
