# EL-032 · Pressure meter — named-identity spec

**Current delivery (24 October 2026):** [EL-032 delivery slice](../invest/named-elements.md#element-032). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes.

Brought to build-ready detail on 22 November 2026 against checkout `8d1d68aa` and repaired the same day against the independent audit `docs/planning/elements/readiness/audits/elements-batch-g.md` and the owner decisions of 22 November 2026. Liquid conventions L1 to L18 and probe rows W1 to W24 are in [EL-001](EL-001-finite-reservoir.md), which has adopted Batch G's LG1 to LG8 into L3 to L15; LG numbers are kept as short names (LG1 = L5 and L6; LG2 = L3 and L7; LG3, LG4, LG5 = L4, L11, L12; LG6 = L15; LG7 = L12; LG8 = L13). The further additions adopted by coordinator decision 21 (nesting and envelope shielding, buoyancy reaction on a carrying body, contents at the liquid centroid, overlapping shapes counted once, porous and sealed interiors, a mouth below the floor datum, the relief mouth, the crest rule with granted work head, no crest lift limit) are now written into EL-001's rows (its "Batch G's additions" paragraph maps A1 to A9 to L3, L4, L5, L7, L8, L9 and L12; the long wording stays in [readiness/batch-g.md](readiness/batch-g.md) section 2) and are cited here as **L-additions A1 to A9**. Probe rows G1 to G8 are in section 3 of that file. All are cited by number and not restated. Box sizes are full extents in metres.

The meter-family body, ports, switch form and lamp are in [EL-030](EL-030-flow-meter.md) and are cited, not restated.

## Identity

| Field | Value |
| --- | --- |
| Identity | EL-032 · Pressure meter · Water |
| Anchors | [element-032](../requirements.md#element-032), co-anchored `sequence-task-585`; source record [todo-339](../requirements.md#todo-339); integration [sequence-task-390](../requirements.md#sequence-task-390); ledger [element-032](../invest/named-elements.md#element-032), owner S453 |
| Disposition | Member of the Family slice "Meters" with [EL-030](EL-030-flow-meter.md) and [EL-031](EL-031-volume-meter.md). Its deltas from the Flow meter: it reads the node pressure in pascals absolute, and it has one socket, not two (a gauge on a port, not in a line) |
| Planned package | id `pressure_meter`, directory `elements/PressureMeter/`, title "Pressure meter" |
| Status | Not started. Blocked on the engine programme (owner decision 3a), then `ENGINE-SIGNAL-NODES`, `ENGINE-GAS-NETWORK` and `ENGINE-LIQUID` |
| Related | [EL-030](EL-030-flow-meter.md), [EL-026](EL-026-communicating-tank.md), [EL-033](EL-033-fluid-accumulator.md), [EL-229](EL-229-hydraulic-piston.md), the pneumatic gauge [EL-043](EL-043-pneumatic-pressure-gauge.md) (the same pascal convention on gas) |

## Declaration

Body, colliders, family vessel material, ports, switch form and lamp: the meter family of [EL-030](EL-030-flow-meter.md). Package deltas:

- **Socket.** One: `Start` at (−0.15, 0, 0), normal −X, pipe bore, Bidirectional, a declared snap socket (L5) with capture distance 0.20 m and angle 20°. The tube's other end is closed and carries no socket, so the fitting is a dead end and passes no flow. It declares no store and so no thermal node.
- **Reading.** The committed pressure of the fitting node at the socket datum, Pa absolute (L13): 101 325 + 156.96 × (total head at the node − socket elevation). An unjoined socket, or one joined to a dry mouth, reads ambient, 101 325 Pa. A gas node's pressure never shares this reading: gas and liquid meet only through a head-capped edge (L13, L15).
- **Node levels.** On at `threshold`; off at `threshold` − 8 Pa (0.05 m of depth).

| Parameter | Type | Unit | Default | Min | Max | Editor policy |
| --- | --- | --- | --- | --- | --- | --- |
| `threshold` | Real f32 | Pa absolute (unit member missing, W13) | 101 403 | 101 341 | 101 796 | Reject; build only |

The limits are 0.10 m and 3.0 m of depth (101 325 + 156.96 × 0.10 = 101 340.7, rounded up to 101 341; 101 325 + 156.96 × 3.0 = 101 795.9); the default is 0.5 m (101 403.5, rounded down). At the minimum the off level is 101 341 − 8 = 101 333 Pa, above ambient, so the node can turn off when the reading returns to 101 325 Pa; at 101 333 the off level would be ambient itself. Near 10⁵ an f32 resolves 0.008 Pa, far finer than the 2 Pa tolerance of L17.

- **Presentation.** `Visual/Needle` `LocalRotationZ` from the committed reading, 101 325 to 101 825 Pa, clamped (W12, missing). `Visual/Lamp`: the family lamp of EL-030 (three `StateValues` bindings on the part's threshold node, slate off and gold on).
- **Palette.** `ElementCategory.Control`; palette order 419, after the flow meter (417) and the volume meter (418) (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)); colour cyan.

### Art and solidity

| Node | Mesh and size | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Body` | Tube 0.30 long, outer radius 0.14, cream `#fff8e9`, its far end closed by a flush disc | solid; the Tube collider | body pose (static) |
| `Visual/Case` | Box 0.24 × 0.24 × 0.08, cyan `#66b8c9` | solid; its box collider | body pose |
| `Visual/Dial` | Cream face with navy `#293954` ticks and a printed "P" in a ring | decorative: printed flat | nothing |
| `Visual/Needle` | Gold `#f7cb52` needle, flush | decorative: printed flat, moved by a binding | presentation binding |
| `Visual/Lamp` | Flush disc on the face | decorative: printed flat (glow) | presentation binding |
| `Loc_Port_mouth_start`, `Loc_Port_PowerIn`, `Loc_Port_Supply` | Locators | none | nothing |

Icon: a stub of pipe, closed at one end, carrying a square dial with a "P". It differs from the other meters by the glyph and the closed end.

### Behaviour and numbers

- **It reads head, not flow or amount.** Joined to a socket of a Linked tank ([EL-026](EL-026-communicating-tank.md), socket at floor height): 101 325 + 156.96 × depth. Full (1.0 m): **101 482 Pa**. Half: 101 325 + 78.48 = 101 403.5 Pa. Empty: 101 325 Pa.
- **Narrow and tall beats wide and low.** A full Linked tank (0.25 m³, 1.0 m deep) reads 101 482 Pa; a Water tank holding more (0.40 m³, 0.40 m deep over its 1.0 m² floor) reads 101 325 + 156.96 × 0.40 = **101 388 Pa** at its socket: 94 Pa less. This is the lesson Pressure, Not Plenty.
- **A raised tank.** At the foot of a riser from a Header tank whose surface is 3.0 m above the meter: 101 325 + 470.9 = 101 796 Pa, the top of the scale.
- **A sealed charge.** On a full Fluid accumulator ([EL-033](EL-033-fluid-accumulator.md), head 2.0 m at full): 101 325 + 156.96 × 2.0 = **101 639 Pa**.
- **Disconnected or depressurised.** Unjoined, or joined to an empty tank, it reads 101 325 Pa and the node is off. It cannot report a pressure nothing supplies.
- **Switching.** On a Linked tank draining through a Tap on its other socket (K = 0.0482, A = 0.25 m²): the node is on while the depth is at least (101 403 − 101 325) / 156.96 = 0.497 m, and turns off at (101 395 − 101 325) / 156.96 = 0.446 m, that is (2 × 0.25 / 0.0482) × (1 − √0.446) = 10.37 × 0.332 = **3.4 s** after a full start, plus the 0.1 s hold: 3.5 s. The tank is empty at 10.37 s.
- **While liquid moves.** The meter's node is a dead end, so its pair with the tank's socket carries no flow and reads the tank's surface. On a fitting that carries flow, a node's head lies between its neighbours' (L7); it is still the committed node pressure.
- **Tick order.** As EL-030 (L16, D7).

## Engine capabilities

| Row | Mark | Use here |
| --- | --- | --- |
| W1, W2 body and Tube; W17 switched route; W22, W23 authored state and refusals | present | as EL-030 |
| W4 fluid port contract | present, untested | fitting and socket |
| W3 snap sockets | narrower: socket count, package-local ids, capture distance 0.20 m and angle 20°, and outlets that are not joinable are `ENGINE-CAPABILITY-INSTANCES`; compiling joins to edges is `ENGINE-LIQUID`; the port record is `ENGINE-GAS-NETWORK` | 1 socket, 0 immersed ports, 0 outlets |
| W16 threshold node on a committed scalar | narrower | `ENGINE-SIGNAL-NODES` |
| Lamp from the threshold node's committed phase | narrower | delivered for contact-load nodes (EL-030's row); general threshold node: `ENGINE-SIGNAL-NODES` |
| W7 fitting; the committed node pressure (L13) as a threshold input | missing | `ENGINE-LIQUID` |
| W12 driver for a committed pressure; W13 Pa unit | missing | W12: `ENGINE-LIQUID`; W13's Pa unit: `ENGINE-GAS-NETWORK`, which lands first (EL-001 W13; engine stories index C5; owner decision 18, 22 November 2026) |

Counts, one per capability: 5 present (W1, W2, W17, W22, W23), 1 untested (W4), 3 narrower (W3, W16, lamp), 4 missing (W7, node-pressure observation, W12, W13). No thermal row: the part declares no store.

### Engine conformance (22 November 2026)

1. **Declaration data only.** The meter-family declaration with one socket, one observation kind, one parameter.
2. **Generic laws.** Pressure is the hydrostatic relation of L2 at a network node (L13). "A disconnected port reports nothing" is a fitting with no wet neighbour.
3. **Three layers.** As EL-030.
4. **One-way flow and typing.** Liquid → observation → signal → next tick's electrical solve; physics → presentation. Pascals absolute throughout, as Batch H's gas parts.
5. **Flat-state counts.** Bodies 1 (static); colliders 2 (1 Tube, 1 box); joints 0; drives 0; sensors 0; signal nodes 1 (threshold); supply routes 1; liquid nodes 1 fitting; thermal nodes 0 (no store); sockets 1; immersed ports 0; outlets 0; wire ports 2; bindings 4 (needle 1, lamp 3); parameters 1.
6. **Performance fitness.** As EL-030; a dead-end fitting adds no flow unknown.
7. **Tests are scenarios.** Rows M1 to M8.

## Sources and legacy

- Requirement: "Measures local supported fluid pressure"; outcome "Disconnected or depressurised port cannot report supplied pressure." No variants.
- Integration [sequence-task-390](../requirements.md#sequence-task-390); refinement [S708](../invest/refinements.md#s708) ("Measure actual pressure"). Component research [water](../../component-research.md#water): recipe Pressure, Not Plenty.
- **Legacy.** None at `a6c914e`. **Files harvested:** none.

## Acceptance outline

Authority: [element-032](../requirements.md#element-032). Tolerances L17 (2 Pa). Planned class `PressureMeterTests`. Unless stated, the meter is joined to the `Start` socket of a Linked tank whose `End` carries a Tap with a free end over a Catch basin.

| # | Given / When / Then | Planned test | Control | Boundary |
| --- | --- | --- | --- | --- |
| M1 | Given the tank authored at 0.25, 0.125 and 0 m³ (depths 1.0, 0.5 and 0 m) with the tap at `opening` 0, then the reading is 101 482, 101 403.5 and 101 325 Pa within 2 Pa | `PressureMeterTests.TheReadingIsTheHydrostaticPressure` | M2 | — |
| M2 | Given the meter unjoined beside a full tank, then it reads 101 325 Pa within 2 Pa and the node is off | `PressureMeterTests.ADisconnectedPortReadsAmbient` | M1 | meter 0.3 m from the socket (not joined) |
| M3 | Given a full Linked tank (other socket capped) and a Water tank authored at 0.40 m³, each with a meter on a socket, then the Linked tank's reading exceeds the Water tank's by 94 Pa within 4 Pa (two readings, 2 Pa each), although the Water tank holds more | `PressureMeterTests.DepthNotVolumeSetsThePressure` | — | — |
| M4 | Given a Battery on `PowerIn` and a Powered gate ([CAT-051](CAT-051-powered_gate.md); a Signal lamp is a signal load and cannot sit on `Supply`) on `Supply`, the tank full and the tap at `opening` 1, when the tank drains, then the gate is supplied with its blade raised at first, loses supply from 3.5 s within 10%, and its blade is below 0.05 m within 3 s after (spring return decays at 2 /s, EL-030) | `PressureMeterTests.TheRouteFollowsTheThreshold` | no Battery: the gate is never supplied | `threshold` 101 796: never supplied |
| M5 | Given M4's construction, when Run advances until the tank is under 0.004 m³ (hand time 10.4 s; stall guard 32 s; the level declares a Run length of 35 s (`ENGINE-RUN-LENGTH`)), then the reading returns to 101 325 Pa within 2 Pa and the node is off | `PressureMeterTests.ADepressurisedPortReadsAmbient` | M1 | `threshold` 101 341 (minimum): the node, on at 101 341, is off once the tank is empty |
| M6 | Given a full Fluid accumulator with the meter on its socket, then the reading is 101 639 Pa within 2 Pa | `PressureMeterTests.ItReadsASealedCharge` | an empty accumulator: 101 325 Pa within 2 Pa | — |
| M7 | Given `threshold` below 101 341, above 101 796 or non-finite, then it is refused with a typed reason | `PressureMeterTests.AThresholdOutsideItsLimitsIsRefused` | limits admitted | next f32 beyond each limit |
| M8 | Given a run, then Reset restores the node to off and the lamp to slate; save and load keep `threshold`, pose, join and wires | `PressureMeterTests.ResetAndSaveLoadKeepTheMeter` | the state before Reset differs | — |

Generic cover: W22, W23. M3 needs EL-001 and EL-012; M6 runs in the EL-033 slice.

## Open questions

All resolved; none needs the owner.

1. Absolute or gauge pascals: absolute (L1), as the pneumatic gauge. The dial's zero mark is ambient.
2. One socket or in line: one. A gauge in a line would need a second, capped socket at every use.
3. Range to 3.0 m: the Header tank's highest surface above the bench. Minimum 0.10 m so the off level stays above ambient (the Level switch's minimum moved to 0.10 m for the same reason).

## Integration

- **Works with.** Battery, Powered gate (delivered); EL-026, EL-001, EL-002, EL-003, EL-012, EL-033, EL-229 (not delivered).
- **Campaign.** Chapter 7, levels 61–70; reuse 81–100, 126–140, 146–150. Needs the lesson-fixture story (S8).

## Delivery

- **Slice shape.** Family slice "Meters".
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), in particular `ENGINE-SPATIAL-QUERIES` (stream samples, immersion and point queries, L18), `ENGINE-ISLANDS-SLEEP` (a change of contents or level wakes what it loads; not delivered, and nothing here assumes sleeping exists) and `ENGINE-CAPABILITY-INSTANCES` (declared sockets, outlets and immersed-port locators); then `ENGINE-GAS-NETWORK` (the one fluid port record); then `ENGINE-LIQUID`; then `ENGINE-THERMAL` (every liquid store is a thermal node, owner decisions 18 and 20); `ENGINE-SIGNAL-NODES`; `ENGINE-RUN-LENGTH` (row M5). Then EL-001, EL-003, EL-004, EL-012, EL-026; S7. Row M6 waits for EL-033.
- **Files.** Adds `elements/PressureMeter/PressureMeter.csproj`, `PressureMeterElementDeclaration.cs`, `pressure_meter.tscn`, `icon.svg`, `CuriousContraptions.tests/PressureMeterTests.cs`; edits `CuriousContraptions.slnx` and this spec. Other lists as EL-001.
- **Review tier.** Data-only.
- **Disqualifiers.** A part cannot declare a single socket; no committed node pressure; no Pa unit; no presentation driver for a threshold node's committed phase.

## Coverage binding (planned, not yet live)

Blocked on the coverage follow-on story S7. The named-identity binding section (the EL-239 form) cannot be written yet: umbrella `todo-339`; `sequence-task-585` co-anchored with `element-032`, not with a todo; no `s453` anchor.
