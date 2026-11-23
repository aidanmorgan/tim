# EL-033 · Fluid accumulator — named-identity spec

**Current delivery (24 October 2026):** [EL-033 delivery slice](../invest/named-elements.md#element-033). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes.

Brought to build-ready detail on 22 November 2026 against checkout `8d1d68aa` and repaired the same day against the independent audit `docs/planning/elements/readiness/audits/elements-batch-g.md` and the owner decisions of 22 November 2026. Liquid conventions L1 to L18 and probe rows W1 to W24 are in [EL-001](EL-001-finite-reservoir.md), which has adopted Batch G's LG1 to LG8 into L3 to L15; LG numbers are kept as short names (LG1 = L5 and L6; LG2 = L3 and L7; LG3, LG4, LG5 = L4, L11, L12; LG6 = L15; LG7 = L12; LG8 = L13). The further additions adopted by coordinator decision 21 (nesting and envelope shielding, buoyancy reaction on a carrying body, contents at the liquid centroid, overlapping shapes counted once, porous and sealed interiors, a mouth below the floor datum, the relief mouth, the crest rule with granted work head, no crest lift limit) are now written into EL-001's rows (its "Batch G's additions" paragraph maps A1 to A9 to L3, L4, L5, L7, L8, L9 and L12; the long wording stays in [readiness/batch-g.md](readiness/batch-g.md) section 2) and are cited here as **L-additions A1 to A9**. Probe rows G1 to G8 are in section 3 of that file. All are cited by number and not restated. Box sizes are full extents in metres.

## Identity

| Field | Value |
| --- | --- |
| Identity | EL-033 · Fluid accumulator · Water |
| Anchors | [element-033](../requirements.md#element-033), co-anchored `sequence-task-586`; source record [todo-340](../requirements.md#todo-340); integration [sequence-task-531](../requirements.md#sequence-task-531); ledger [element-033](../invest/named-elements.md#element-033), owner S454 |
| Disposition | To deliver as a single element. It is the only consumer in this batch of the compliant sealed store (L4, the store once called LG3) |
| Planned package | id `accumulator`, directory `elements/Accumulator/`, title "Accumulator" |
| Status | Not started. Blocked on the engine programme (owner decision 3a), then `ENGINE-GAS-NETWORK`, `ENGINE-LIQUID` and `ENGINE-THERMAL` |
| Related | [EL-022](EL-022-water-pump.md) and [EL-002](EL-002-header-tank.md) (chargers), [EL-019](EL-019-water-check-valve.md) (holds the charge), [EL-003](EL-003-tap.md) (releases it), [EL-032](EL-032-pressure-meter.md), [EL-229](EL-229-hydraulic-piston.md); the Air reservoir [EL-039](EL-039-air-reservoir.md) is the separate gas store |

## Declaration

Closest delivered shape: the Battery, a static box that holds a finite store and shows its charge (`elements/Battery/BatteryElementDeclaration.cs`).

- **Interfaces.** `ISceneGeometryElement`, `IMechanicalElement`, the liquid store declaration of `ENGINE-LIQUID` on the one fluid port record of `ENGINE-GAS-NETWORK` (W4, W6), with compliance (L4).
- **Body.** One static body, mass 0. Origin at the box centre.
- **Collider (one).** Box 0.50 × 0.60 × 0.50. Family vessel material (EL-001: restitution 0.12, friction 0.3, rolling resistance 0).
- **Store (L4, sealed and compliant).** Local id 1. Capacity C = 0.0625 m³ (one mark, 1 kg). Head at full H_e = 2.0 m: the total head at its socket is the socket elevation + 2.0 × V/C. No open aperture and no overflow: at V = C it accepts nothing more. **A sealed store declares no interior (A6): nothing is immersed in it, probed in it or streamed into it**, so a stream landing on the case is spilled by L8 like any other collider. Store 1 is also a thermal node (owner decision 20; [TH-04](TH-04-electrical-heating-plate.md) section 11, T2): heat capacity ρ·V × 1046 J/(kg·K), starting temperature declared ambient 288 K unless a level authors another; inflow mixes by mass and outflow leaves at the store's temperature. No row of this part reads temperature.
- **Socket.** `End`, pipe bore, Bidirectional, into store 1, a declared snap socket (L5) with capture distance 0.20 m and angle 20°, at (0, −0.15, 0.25), normal +Z. With the part on the bench (base at y = −0.30) the socket is **0.15 m above the bench**, level with a Water tank's socket (the kit rule, [EL-008](EL-008-straight-water-pipe.md)).

| Parameter | Type | Unit | Default | Min | Max | Editor policy |
| --- | --- | --- | --- | --- | --- | --- |
| `initial_volume` | Real f32 | m³ (unit member missing, W13) | 0 | 0 | 0.0625 | Reject; build only; player-editable unless the level locks it (D11) |

- **Presentation.** `Visual/Gauge` `ScaleY` from the committed fill fraction of store 1 (W12, missing).
- **Palette.** `ElementCategory.Power`; palette order 336 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)); colour teal (74/255, 171/255, 148/255).

### Art and solidity

| Node | Mesh and size | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Case` | Box 0.50 × 0.60 × 0.50, teal `#4aab94`, with a domed top printed as a navy `#293954` arc on each face | solid; the box collider | body pose (static) |
| `Visual/Window` | Cream `#fff8e9` strip on the front face | decorative: printed flat | nothing |
| `Visual/Gauge` | Gold `#f7cb52` bar in the strip, flush | decorative: printed flat, moved by a binding | presentation binding |
| `Visual/PortRing` | Navy ring, radius 0.14, low on the front face | decorative: printed flat | nothing |
| `Loc_Port_mouth_end` | Socket locator, bore 0.10 | none | nothing |

Icon: a closed teal box with a spring glyph and a port ring. It differs from the Water tank (open, cream) by being closed, and from the Battery by the port ring and spring glyph.

### Behaviour and numbers

The accumulator is a store whose head rises with what it holds (L4, sealed compliant). Flow in and out is L7; nothing else acts.

- **Head and energy.** Head = 2.0 × V/C: 1.0 m when half full, 2.0 m when full. Stored energy E = ½·ρ·g·H_e·V²/C: full, ½ × 156.96 × 2.0 × 0.0625 = **9.81 J**; half full, 9.81 / 4 = 2.45 J. Pressure at the socket when full: 101 325 + 156.96 × 2.0 = 101 639 Pa.
- **Charging takes head.** Joined to a full Water tank on the same bench (the two sockets level, one joined pair, K₀), liquid enters until the heads match: the tank's depth V_tank / 1.0 equals 32·V_acc, with V_tank + V_acc = 1.0, so V_acc = 1/33 = **0.0303 m³** (48% full) and it stops. The difference Δ closes as dΔ/dt = −K₀·√Δ·(1/1.0 + 1/0.03125) = −33·K₀·√Δ: 2 × √1.0 / (33 × 0.08349) = 0.73 s. It never charges above the head that feeds it.
- **Charging fully** needs 2.0 m of head: a Header tank whose surface is 2.0 m or more above the socket, or a Water pump (head cap 3.0 m, [EL-022](EL-022-water-pump.md)), which spends at least 9.81 / 0.5 = 19.6 J of its grant to store 9.81 J. **An unpowered pump is open (L15)**, so a pump-charged accumulator needs a Check valve ([EL-019](EL-019-water-check-valve.md)) between them to hold its charge when the pump stops.
- **Holding.** A Tap at `opening` 0 (fixed for the Run, `EL-003-tap.md:35`) or a Check valve on the socket holds the charge: a seat of zero area or a one-way seat (L14).
- **Discharge through an open Tap with a free end** (K = 0.0482): the head h obeys dh/dt = −K·√h / A_e with A_e = C/H_e = 0.03125 m². First flow 0.0482 × √2.0 = 0.0682 m³/s, leaving the tap at 0.0682 / 0.01885 = 3.6 m/s; empty after 2·A_e·√2.0 / K = 2 × 0.03125 × 1.414 / 0.0482 = **1.83 s**. At 0.0156 m³ (a quarter, head 0.5 m) the flow is 0.0482 × √0.5 = 0.0341, half the first.
- **Discharge through a Water nozzle** (K = 0.01319): 6.19 m/s at first, as from a 2.0 m tank; empty after 2 × 0.03125 × 1.414 / 0.01319 = 6.7 s. A bench-level part throws as far as a tank on a 2 m stand, for 0.0625 m³.
- **No free recharge.** Discharging lowers V, head and energy together. An empty accumulator has no head; it refills only from a higher head.
- **Liquid lifted.** Joined to an empty Linked tank on the same bench, a full accumulator pushes liquid out until 2.0 × V_acc/C equals the tank's depth V_tank/0.25: 32·V_acc = 4·V_tank with V_acc + V_tank = 0.0625, so 0.0069 m³ stays and **0.0556 m³ stands 0.22 m deep** in the tank. A charge raised water 0.22 m with no pump running; it cannot do so twice.

## Engine capabilities

Map families (navigation only, D14): ElasticStorage, EnvironmentState, FiniteLedger, FluidAdvection, GeometryQuery, JointConstraint, PressureWork, RigidBodyDynamics, StateTransaction, TopologyTransaction.

| Row | Mark | Use here |
| --- | --- | --- |
| W1 static body and collider | present | case |
| W22, W23 authored state, refusals | present | `initial_volume` |
| W4 fluid port contract | present, untested | store and socket |
| W3 snap sockets | narrower: socket count, package-local ids, capture distance 0.20 m and angle 20°, and outlets that are not joinable are `ENGINE-CAPABILITY-INSTANCES`; compiling joins to edges is `ENGINE-LIQUID`; the port record is `ENGINE-GAS-NETWORK` | 1 socket, 0 immersed ports, 0 outlets |
| W6 store; W7 joins, apertures, flow | missing | `ENGINE-LIQUID` |
| Compliant sealed store: head proportional to volume, no aperture, no overflow, no interior (L4, A6) | missing | `ENGINE-LIQUID` |
| W12 fill-fraction driver; W13 m³ unit | missing | W12: `ENGINE-LIQUID`; W13's m³ unit: `ENGINE-GAS-NETWORK`, which lands first (EL-001 W13; owner decision 18, 22 November 2026) |
| Liquid store as thermal node | missing | `ENGINE-THERMAL` |

Counts, one per capability: 3 present (W1, W22, W23), 1 untested (W4), 1 narrower (W3), 6 missing (W6, W7, sealed compliant store, W12, W13, thermal).

### Engine conformance (22 November 2026)

1. **Declaration data only.** A static body, one collider, one compliant sealed store, one socket, one parameter, one binding. No spring body, piston or bladder routine.
2. **Generic laws.** Charge and discharge are L7 with the store's declared head (L4). "No free recharge" is that flow runs only toward lower head.
3. **Three layers.** *Collider:* one box. *Visuals:* the table above. *Animation:* the gauge reads the committed fill.
4. **One-way flow and typing.** Socket id is a package-local typed id, its role an enum; volumes f32.
5. **Flat-state counts.** Bodies 1 (static); colliders 1 box; joints 0; drives 0; sensors 0; signal nodes 0; liquid nodes 1 store (no interior); thermal nodes 1, one per store; sockets 1; immersed ports 0; outlets 0; wire ports 0; bindings 1; parameters 1.
6. **Performance fitness.** One box, as the Battery's body. Constant work per store; with no interior it takes no immersion or stream query.
7. **Tests are scenarios.** Rows Q1 to Q8.

## Sources and legacy

- Requirement: "Stores a finite fluid charge against a compliant boundary"; outcome "Discharging lowers stored energy; no free recharge." No variants.
- Integration [sequence-task-531](../requirements.md#sequence-task-531) ("Finite absorption/storage … can be observed before it produces an action"). Component research [water](../../component-research.md#water): "Stores and returns bounded hydraulic energy".
- **Legacy.** None at `a6c914e` (searched for "accumulator", "bladder", "water", "liquid"). **Files harvested:** none.

## Acceptance outline

Authority: [element-033](../requirements.md#element-033). Tolerances L17. Planned class `AccumulatorTests`. A Tap's opening is fixed during Run (`EL-003-tap.md:35`), so rows that compare a shut and an open tap use two Runs.

| # | Given / When / Then | Planned test | Control | Boundary |
| --- | --- | --- | --- | --- |
| Q1 | Given an empty accumulator joined to a full Water tank on the same bench, when Run advances until every committed flow is under 0.0005 m³/s (hand time 0.73 s; stall guard 10 s), then it holds 0.0303 m³ within 0.004 m³ and the total is unchanged within 0.004 m³ | `AccumulatorTests.ItChargesOnlyToTheSupplyHead` | Q2 | — |
| Q2 | Given the accumulator with its socket capped (EL-012), when Run advances 10 s, then its volume is unchanged within 0.004 m³ | `AccumulatorTests.NothingChargesItRemotely` | Q1 | a stream from a tap landing on the case: the volume is unchanged and `Spilled` gains the stream (no interior, A6) |
| Q3 | Given a pipe run from a Header tank whose surface starts 3.0 m above the socket, then the accumulator fills to 0.0625 m³ within 0.004 m³ and takes no more | `AccumulatorTests.TwoMetresOfHeadFillIt` | Q1 | supply surface 1.5 m above the socket: it stops at or below 0.75 × 0.0625 = 0.047 m³ |
| Q4 | Two Runs from a full accumulator behind a Tap with a free end over a Catch basin. With `opening` 0, when Run advances 20 s, its volume is unchanged within 0.004 m³. With `opening` 1, when Run advances until it holds under 0.004 m³ (hand time 1.83 s; stall guard 10 s), the basin and `Spilled` together gained 0.0625 m³ within 0.004 m³ | `AccumulatorTests.ItHoldsAndThenReleasesItsCharge` | the two Runs are each other's control | — |
| Q5 | Given Q4's second Run sampled when the store holds 0.0625 and 0.0156 m³, then the committed outflow at the second is half the first within 10% | `AccumulatorTests.DischargeLowersHeadAndFlow` | — | — |
| Q6 | Given an accumulator authored empty behind a Tap at `opening` 1 with no supply joined, when Run advances 10 s, then every committed flow is under 0.0005 m³/s (L17) and the volume stays under 0.004 m³ | `AccumulatorTests.NoFreeRecharge` | Q4's second Run | — |
| Q7 | Given `initial_volume` below 0, above 0.0625 or non-finite, then it is refused with a typed reason | `AccumulatorTests.AVolumeOutsideItsLimitsIsRefused` | limits admitted | next f32 beyond each limit |
| Q8 | Given a run, then Reset restores the authored volume; save and load keep `initial_volume`, pose and join; the gauge art shows 0, half and full | `AccumulatorTests.ResetSaveLoadAndGauge` | the state before Reset differs | 0 and 0.0625 |

Generic cover: W22, W23. No wire port. Later-partner rows: the pump charging it through a Check valve (energy debit at least 19.6 J for a full charge; the valve holds the charge when the pump's supply stops) runs here as `AccumulatorTests.APumpPaysAtLeastTwiceTheStoredEnergy` once EL-022, EL-019 and `ENGINE-SUPPLY-DEMAND` are delivered; the Pressure meter row is EL-032 M6.

## Open questions

All resolved; none needs the owner.

1. A piston on a spring, or a store with a declared head: the store (L4). A piston would need liquid force on a body, which the family does not have outside EL-229.
2. Gas pre-charge: none. One linear law is enough for "stored and returned".
3. Capacity and head parameters: none (D13).
4. Interior: none (A6). The store is sealed; a stream or a body cannot enter it.

## Integration

- **Works with.** EL-001, EL-002, EL-003, EL-004, EL-012, EL-013, EL-019, EL-022, EL-026, EL-032, EL-229 (none delivered).
- **Campaign.** Chapter 9 "Stored Potential", levels 81–90; reuse 91–100, 131–140, 146–150. Needs the lesson-fixture story (S8).

## Delivery

- **Slice shape.** Single element.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), in particular `ENGINE-SPATIAL-QUERIES` (stream samples, immersion and point queries, L18), `ENGINE-ISLANDS-SLEEP` (a change of contents or level wakes what it loads; not delivered, and nothing here assumes sleeping exists) and `ENGINE-CAPABILITY-INSTANCES` (declared sockets, outlets and immersed-port locators); then `ENGINE-GAS-NETWORK` (the one fluid port record); then `ENGINE-LIQUID`; then `ENGINE-THERMAL` (every liquid store is a thermal node, owner decisions 18 and 20). Then EL-001, EL-002, EL-003, EL-004, EL-008, EL-012; S7. The later-partner row waits for EL-019, EL-022 and `ENGINE-SUPPLY-DEMAND`.
- **Files.** Adds `elements/Accumulator/Accumulator.csproj`, `AccumulatorElementDeclaration.cs`, `accumulator.tscn`, `icon.svg`, `CuriousContraptions.tests/AccumulatorTests.cs`; edits `CuriousContraptions.slnx` and this spec. Other lists as EL-001.
- **Review tier.** Data-only if the probe finds the sealed compliant store delivered.
- **Disqualifiers.** A store whose head cannot be declared apart from its drawn shape; a sealed store that overflows or credits a stream.

## Coverage binding (planned, not yet live)

Blocked on the coverage follow-on story S7. The named-identity binding section (the EL-239 form) cannot be written yet: umbrella `todo-340`; `sequence-task-586` co-anchored with `element-033`, not with a todo; no `s454` anchor.
