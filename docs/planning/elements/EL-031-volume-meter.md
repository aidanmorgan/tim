# EL-031 · Volume meter — named-identity spec

**Current delivery (24 October 2026):** [EL-031 delivery slice](../invest/named-elements.md#element-031). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes.

Brought to build-ready detail on 22 November 2026 against checkout `8d1d68aa` and repaired the same day against the independent audit `docs/planning/elements/readiness/audits/elements-batch-g.md` and the owner decisions of 22 November 2026. Liquid conventions L1 to L18 and probe rows W1 to W24 are in [EL-001](EL-001-finite-reservoir.md), which has adopted Batch G's LG1 to LG8 into L3 to L15; LG numbers are kept as short names (LG1 = L5 and L6; LG2 = L3 and L7; LG3, LG4, LG5 = L4, L11, L12; LG6 = L15; LG7 = L12; LG8 = L13). The further additions adopted by coordinator decision 21 (nesting and envelope shielding, buoyancy reaction on a carrying body, contents at the liquid centroid, overlapping shapes counted once, porous and sealed interiors, a mouth below the floor datum, the relief mouth, the crest rule with granted work head, no crest lift limit) are now written into EL-001's rows (its "Batch G's additions" paragraph maps A1 to A9 to L3, L4, L5, L7, L8, L9 and L12; the long wording stays in [readiness/batch-g.md](readiness/batch-g.md) section 2) and are cited here as **L-additions A1 to A9**. Probe rows G1 to G8 are in section 3 of that file. All are cited by number and not restated. Box sizes are full extents in metres.

The meter-family body, sockets, ports, switch form and lamp are in [EL-030](EL-030-flow-meter.md) and are cited, not restated.

## Identity

| Field | Value |
| --- | --- |
| Identity | EL-031 · Volume meter · Water |
| Anchors | [element-031](../requirements.md#element-031), co-anchored `sequence-task-584`; source record [todo-339](../requirements.md#todo-339); integration [sequence-task-390](../requirements.md#sequence-task-390); ledger [element-031](../invest/named-elements.md#element-031), owner S452 |
| Disposition | Member of the Family slice "Meters" with [EL-030](EL-030-flow-meter.md) and [EL-032](EL-032-pressure-meter.md). Its delta from the Flow meter: it reads the cumulative volume passed, in m³, and its contact stays closed once reached |
| Planned package | id `volume_meter`, directory `elements/VolumeMeter/`, title "Volume meter" |
| Status | Not started. Blocked on the engine programme (owner decision 3a), then `ENGINE-SIGNAL-NODES`, `ENGINE-GAS-NETWORK` and `ENGINE-LIQUID` |
| Related | [EL-030](EL-030-flow-meter.md), [EL-014](EL-014-water-carrying-bucket.md) (its 0.125 m³ is the default target), [EL-167](EL-167-solenoid-tap.md) (a tap this contact can shut; row U4) |

## Declaration

Body, colliders, family vessel material, fitting, the two sockets `Start` and `End` (capture 0.20 m, 20°), ports, switch form and lamp: the meter family of [EL-030](EL-030-flow-meter.md), unchanged. The fitting declares no store and so no thermal node. Package deltas:

- **Reading.** The cumulative volume that has passed the fitting from `Start` to `End` since Run began, in m³ (L13, the cumulative edge volume once called LG8). Reverse flow adds nothing and subtracts nothing. Reset zeroes it.
- **Node levels.** On at `target`; off at `target` − 0.004 m³. The reading never falls, so once on the node stays on until Reset.

| Parameter | Type | Unit | Default | Min | Max | Editor policy |
| --- | --- | --- | --- | --- | --- | --- |
| `target` | Real f32 | m³ (unit member missing, W13) | 0.125 | 0.0625 | 2.0 | Reject; build only |

- **Presentation.** `Visual/Bar` `ScaleY` from the committed reading ÷ `target`, clamped to 1 (W12, missing). `Visual/Lamp`: the family lamp of EL-030 (three `StateValues` bindings on the part's threshold node, slate off and gold on).
- **Palette.** `ElementCategory.Control`; palette order 418, between the flow meter (417) and the pressure meter (419) (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)); colour cyan.

### Art and solidity

| Node | Mesh and size | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Body` | Tube 0.30 long, outer radius 0.14, cream `#fff8e9` | solid; the Tube collider | body pose (static) |
| `Visual/Case` | Box 0.24 × 0.24 × 0.08, cyan `#66b8c9` | solid; its box collider | body pose |
| `Visual/Dial` | Cream face with a printed column of four navy `#293954` bars | decorative: printed flat | nothing |
| `Visual/Bar` | Gold `#f7cb52` column, flush on the face | decorative: printed flat, moved by a binding | presentation binding |
| `Visual/Lamp` | Flush disc on the face | decorative: printed flat (glow) | presentation binding |
| Socket and port locators | as EL-030 | none | nothing |

Icon: a short pipe carrying a square dial with a stack of bars. It differs from the Flow meter (arrow) and the Pressure meter ("P" in a ring) by the glyph.

### Behaviour and numbers

- **It adds up what passed.** For a full Water tank → meter → open Tap with a free end (K = 0.0482, [EL-003](EL-003-tap.md)) the tank holds (1 − t/41.5)² m³ after t seconds, so the reading is 1 − (1 − t/41.5)²: 0.125 m³ at t = 41.5 × (1 − √0.875) = 41.5 × 0.0646 = **2.68 s**; 1 − (1 − 12/41.5)² = 1 − 0.505 = **0.495 m³** at 12 s; 1.0 m³ when the tank is empty at 41.5 s.
- **It equals what the receiver got.** With the stream caught by a Catch basin, the reading and the basin's gain agree until the basin overflows (the reading keeps counting; the basin does not).
- **One count per volume.** The reading is committed state advanced only by flow (L13). Reading it, by the bar, the node, a goal, or a test sampling every tick instead of every second, changes nothing. Two meters in series show the same number.
- **Stopping the flow freezes it.** A shut seat leaves the reading where it is. A Tap's opening is fixed during Run (`EL-003-tap.md:35`), so a flow is stopped during Run by a Solenoid tap ([EL-167](EL-167-solenoid-tap.md)) whose command is withdrawn: with `PowerIn` on one Battery and `FirstIn` on a second, the second is disabled through its Run control (`enabled` is `ParameterRunEdit.RunControl`, `elements/Battery/BatteryElementDeclaration.cs:48`–`:49`, lane `RunControlLane.SourceEnable`; switched during Run by `ClutchTests.EqualInertiaDeclaredClosure_SplitsSpinHalfAndNeverCreatesKineticEnergy`, `CuriousContraptions.tests/ClutchTests.cs:317`, command at `:392`). The pin opens in 0.15 s and shuts in under 0.1 s after the electrical solve of the next tick (EL-167). Opened at 0 and shut at 5 s, it passes about 1 − (1 − 4.85/41.5)² = **0.220 m³**.
- **Reverse flow.** Between two Linked tanks that level back and forth, only `Start` → `End` flow counts. Through the meter the pair has two apertures (K₀/√2 = 0.05904) and levels in 0.25 / 0.05904 = 4.2 s ([EL-026](EL-026-communicating-tank.md)).
- **Switching.** The route closes 0.1 s after the reading reaches `target` (2.68 + 0.1 = **2.78 s** at the default) and stays closed. A Solenoid tap wired through a delivered inverting gate then shuts: "pour exactly two marks".
- **Precision.** The reading uses the engine's bulk and remainder pair (L10), so many small steps are not lost.
- **Tick order.** As EL-030 (L16, D7).

## Engine capabilities

| Row | Mark | Use here |
| --- | --- | --- |
| W1, W2 body and Tube; W17 switched route; W22, W23 authored state and refusals | present | as EL-030 |
| W4 fluid port contract | present, untested | fitting and sockets |
| W3 snap sockets | narrower: socket count, package-local ids, capture distance 0.20 m and angle 20°, and outlets that are not joinable are `ENGINE-CAPABILITY-INSTANCES`; compiling joins to edges is `ENGINE-LIQUID`; the port record is `ENGINE-GAS-NETWORK` | 2 sockets, 0 immersed ports, 0 outlets |
| W16 threshold node on a committed scalar | narrower | `ENGINE-SIGNAL-NODES` |
| Lamp from the threshold node's committed phase | narrower | delivered for contact-load nodes (EL-030's row); general threshold node: `ENGINE-SIGNAL-NODES` |
| W7 fitting and flow | missing | `ENGINE-LIQUID` |
| Cumulative edge volume as a committed observation (L13) | missing | `ENGINE-LIQUID` |
| W12 driver for it; W13 m³ unit | missing | `ENGINE-LIQUID` |

Counts, one per capability: 5 present (W1, W2, W17, W22, W23), 1 untested (W4), 3 narrower (W3, W16, lamp), 4 missing (W7, cumulative edge volume, W12, W13). No thermal row: the part declares no store.

### Engine conformance (22 November 2026)

1. **Declaration data only.** The meter-family declaration with one observation kind, one parameter and one binding changed. No counter in package code.
2. **Generic laws.** Integration is the network's own accounting of an edge (L13), kept the way store volumes are. The contact is the general threshold node; the lamp reads its committed phase.
3. **Three layers.** As EL-030; the bar is printed art moved by a binding.
4. **One-way flow and typing.** Liquid → observation → signal → next tick's electrical solve; physics → presentation.
5. **Flat-state counts.** Bodies 1 (static); colliders 2 (1 Tube, 1 box); joints 0; drives 0; sensors 0; signal nodes 1 (threshold); supply routes 1; liquid nodes 1 fitting with 1 cumulative row; thermal nodes 0 (no store); sockets 2; immersed ports 0; outlets 0; wire ports 2; bindings 4 (bar 1, lamp 3); parameters 1.
6. **Performance fitness.** As EL-030; one more scalar per fitting.
7. **Tests are scenarios.** Rows U1 to U8.

## Sources and legacy

- Requirement: "Integrates actual transported liquid volume"; outcome "Repeated inspection does not count the same volume twice." No variants.
- Integration [sequence-task-390](../requirements.md#sequence-task-390); refinement [S708](../invest/refinements.md#s708) ("Measure accumulated volume").
- **Legacy.** None at `a6c914e`. **Files harvested:** none.

## Acceptance outline

Authority: [element-031](../requirements.md#element-031). Tolerances L17. Planned class `VolumeMeterTests`. The route is a full Water tank → meter → Tap (`opening` 1 unless stated), the stream caught by a Catch basin.

| # | Given / When / Then | Planned test | Control | Boundary |
| --- | --- | --- | --- | --- |
| U1 | Given the tap open, when Run advances 12 s, then the reading is 0.495 m³ within 10% (the flow tolerance carried through) and equals the tank's loss within 0.004 m³ | `VolumeMeterTests.TheReadingIsTheVolumePassed` | U2 | — |
| U2 | Given the tap at `opening` 0, when Run advances 10 s, then the reading stays under 0.004 m³ | `VolumeMeterTests.NoFlowNoCount` | U1 | — |
| U3 | Given two meters in series, one read every tick and one read once at the end, then their readings agree within 0.004 m³ | `VolumeMeterTests.InspectionDoesNotCountTwice` | — | — |
| U4 | Given the Tap replaced by a Solenoid tap whose `PowerIn` is fed by one Battery and `FirstIn` by a second, when the second Battery is disabled through its Run control at 5 s, then the reading at 5.5 s is 0.220 m³ within 10% and changes by under 0.004 m³ from 5.5 s to 15.5 s | `VolumeMeterTests.AStoppedFlowFreezesTheCount` | the second Battery left enabled: the reading grows by 0.36 m³ within 10% over the same 10 s | — |
| U5 | Given the meter between two Linked tanks (outer sockets capped) with 0.25 m³ on its `End` side, when they level (hand 4.2 s; stall guard 13 s), then the reading stays under 0.004 m³; with the 0.25 m³ on its `Start` side it reads 0.125 m³ within 0.004 m³ | `VolumeMeterTests.OnlyForwardFlowCounts` | — | — |
| U6 | Given a Battery on `PowerIn` and a Powered gate ([CAT-051](CAT-051-powered_gate.md); a Signal lamp is a signal load and cannot sit on `Supply`) on `Supply`, when Run advances 50 s (the tank empties at 41.5 s, 45.7 s with 10%; the level declares a Run length of 50 s (`ENGINE-RUN-LENGTH`)), then the gate becomes supplied at 2.78 s within 10%, its blade raised to 1.55 m within 1.5 s after, and is still supplied with its blade raised at 50 s | `VolumeMeterTests.TheRouteClosesAtTheTargetAndStays` | no Battery: the gate is never supplied | `target` 2.0: never supplied from a 1.0 m³ tank |
| U7 | Given `target` below 0.0625, above 2.0 or non-finite, then it is refused with a typed reason | `VolumeMeterTests.ATargetOutsideItsLimitsIsRefused` | limits admitted | next f32 beyond each limit |
| U8 | Given a run, then Reset zeroes the reading, sets the node off and opens the route; save and load keep `target`, pose, joins and wires; the runtime reading is not stored | `VolumeMeterTests.ResetZeroesTheCount` | the state before Reset differs | — |

U4's control: from 5.5 s to 15.5 s an open seat passes (1 − 5.5/41.5)² − (1 − 15.5/41.5)² = 0.752 − 0.393 = 0.36 m³. Generic cover: W22, W23. U4 needs EL-167; U5 needs EL-026.

## Open questions

All resolved; none needs the owner.

1. Net or forward count: forward only. A dose is what was delivered; a net count would let backflow reopen the route.
2. A reset input: none (D19); Reset restores it.
3. Shown as a fraction of `target`: yes, so the bar is full exactly when the route closes.
4. Stopping a flow during Run: a Solenoid tap whose command Battery is disabled through its delivered Run control; a Tap is fixed during Run.

## Integration

- **Works with.** Battery, Powered gate, the electrical gates (delivered); EL-001, EL-003, EL-004, EL-026, EL-167 (not delivered).
- **Campaign.** Chapter 7, levels 61–70; reuse 81–100, 126–140, 146–150. Needs the lesson-fixture story (S8).

## Delivery

- **Slice shape.** Family slice "Meters".
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), in particular `ENGINE-SPATIAL-QUERIES` (stream samples, immersion and point queries, L18), `ENGINE-ISLANDS-SLEEP` (a change of contents or level wakes what it loads; not delivered, and nothing here assumes sleeping exists) and `ENGINE-CAPABILITY-INSTANCES` (declared sockets, outlets and immersed-port locators); then `ENGINE-GAS-NETWORK` (the one fluid port record); then `ENGINE-LIQUID` (with the cumulative edge volume of L13); then `ENGINE-THERMAL` (every liquid store is a thermal node, owner decisions 18 and 20); `ENGINE-SIGNAL-NODES`; `ENGINE-RUN-LENGTH` (row U6). Then EL-001, EL-003, EL-004; EL-026 (row U5); EL-167 (row U4); S7.
- **Files.** Adds `elements/VolumeMeter/VolumeMeter.csproj`, `VolumeMeterElementDeclaration.cs`, `volume_meter.tscn`, `icon.svg`, `CuriousContraptions.tests/VolumeMeterTests.cs`; edits `CuriousContraptions.slnx` and this spec. Other lists as EL-001.
- **Review tier.** Data-only.
- **Disqualifiers.** No cumulative edge observation; a count kept in one f32 that stops growing for small steps; no presentation driver for a threshold node's committed phase.

## Coverage binding (planned, not yet live)

Blocked on the coverage follow-on story S7. The named-identity binding section (the EL-239 form) cannot be written yet: umbrella `todo-339`; `sequence-task-584` co-anchored with `element-031`, not with a todo; no `s452` anchor.
