# EL-027 · Canal lock chamber — named-identity spec

**Current delivery (24 October 2026):** [EL-027 delivery slice](../invest/named-elements.md#element-027). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes.

Brought to build-ready detail on 22 November 2026 against checkout `8d1d68aa` and repaired the same day against the independent audit `docs/planning/elements/readiness/audits/elements-batch-g.md` and the owner decisions of 22 November 2026. Liquid conventions L1 to L18 and probe rows W1 to W24 are in [EL-001](EL-001-finite-reservoir.md), which has adopted Batch G's LG1 to LG8 into L3 to L15; LG numbers are kept as short names (LG1 = L5 and L6; LG2 = L3 and L7; LG3, LG4, LG5 = L4, L11, L12; LG6 = L15; LG7 = L12; LG8 = L13). The further additions adopted by coordinator decision 21 (nesting and envelope shielding, buoyancy reaction on a carrying body, contents at the liquid centroid, overlapping shapes counted once, porous and sealed interiors, a mouth below the floor datum, the relief mouth, the crest rule with granted work head, no crest lift limit) are now written into EL-001's rows (its "Batch G's additions" paragraph maps A1 to A9 to L3, L4, L5, L7, L8, L9 and L12; the long wording stays in [readiness/batch-g.md](readiness/batch-g.md) section 2) and are cited here as **L-additions A1 to A9**. Probe rows G1 to G8 are in section 3 of that file. All are cited by number and not restated. Box sizes are full extents in metres.

## Identity

| Field | Value |
| --- | --- |
| Identity | EL-027 · Canal lock chamber · Water |
| Anchors | [element-027](../requirements.md#element-027), co-anchored `sequence-task-580`; source record [todo-339](../requirements.md#todo-339); integration [sequence-task-390](../requirements.md#sequence-task-390); ledger [element-027](../invest/named-elements.md#element-027), owner S448 |
| Disposition | To deliver as a single element. Its "separately controlled openings" are two Sluice gates ([EL-021](EL-021-sluice-gate.md)) joined to its two sockets; the chamber declares no gate of its own |
| Planned package | id `lock_chamber`, directory `elements/LockChamber/`, title "Lock chamber" |
| Status | Not started. Blocked on the engine programme (owner decision 3a), then `ENGINE-GAS-NETWORK`, `ENGINE-LIQUID` and `ENGINE-THERMAL` |
| Related | [EL-021](EL-021-sluice-gate.md), [EL-007](EL-007-open-gutter.md) (upper and lower pounds), [EL-028](EL-028-buoyant-platform.md) and [EL-029](EL-029-boat.md) (what it lifts; both, and [EL-170](EL-170-raft.md), author its depth through `initial_volume`), [EL-026](EL-026-communicating-tank.md) |

## Declaration

Closest delivered shape: the Receiver's open five-box vessel; closest planned shape: the Water tank.

- **Interfaces.** `ISceneGeometryElement`, `IMechanicalElement`, the liquid store declaration of `ENGINE-LIQUID` on the one fluid port record of `ENGINE-GAS-NETWORK` (W4, W6).
- **Body.** One static body, mass 0. Origin at the centre of the outer box.
- **Colliders (five boxes).** Outer 1.24 × 1.35 × 1.24; interior 1.0 × 1.2 × 1.0. Floor 1.24 × 0.15 × 1.24 at (0, −0.60, 0); two walls 0.12 × 1.2 × 1.24 at (±0.56, 0.075, 0); two walls 1.0 × 1.2 × 0.12 at (0, 0.075, ±0.56). Interior floor at y = −0.525, rim at y = +0.675. Family vessel material (EL-001: restitution 0.12, friction 0.3, rolling resistance 0).
- **Store.** Local id 1. Capacity 1.2 m³; plan area 1.0 m²; interior 1.0 × 1.2 × 1.0 (H = 1.2 m) above the floor datum (0, −0.525, 0). Open aperture 1.0 × 1.0 at y = +0.675, normal +Y, the top of the interior. Overflow point (0, 0.675, 0.62). Starts at `initial_volume`. Streams: each sample path that ends inside the store's declared interior is credited to it; a sample that meets any other collider first is spilled (L8). Store 1 is also a thermal node (owner decision 20; [TH-04](TH-04-electrical-heating-plate.md) section 11, T2): heat capacity ρ·V × 1046 J/(kg·K), starting temperature declared ambient 288 K unless a level authors another; inflow mixes by mass and outflow leaves at the store's temperature. No row of this part reads temperature.
- **Sockets (channel bore 0.20, Bidirectional, into store 1).** Declared snap sockets (L5), capture distance 0.20 m and angle 20°. `Start` (upper) at (−0.62, +0.225, 0): its datum is 0.75 m above the chamber floor. `End` (lower) at (+0.62, −0.525, 0): at the chamber floor. Each faces 5° from the horizontal so that a gate and gutter sloped 5° can join it: `Start` normal (−0.9962, +0.0872, 0) (a pound sloping down toward the chamber), `End` normal (+0.9962, −0.0872, 0) (a pound sloping down away from it). Each is an aperture of 0.08 m² (K_c = 0.21261). They join channel sockets only (L6): a Sluice gate or a gutter.

| Parameter | Type | Unit | Default | Min | Max | Editor policy |
| --- | --- | --- | --- | --- | --- | --- |
| `initial_volume` | Real f32 | m³ (unit member missing, W13) | 0 | 0 | 1.2 | Reject; build only; player-editable unless the level locks it (D11) |

- **Presentation.** `Visual/Water` `ScaleY` and `LocalY` from the committed fill fraction (W12, missing).
- **Palette.** `ElementCategory.Structure`; palette order 530 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)); colour ochre (194/255, 143/255, 82/255).

### Art and solidity

| Node | Mesh and size | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Shell` | Open box, the five slabs above, ochre `#c28f52` | solid; the five box colliders | body pose (static) |
| `Visual/Water` | Box 1.0 × 1.2 × 1.0, cyan `#66b8c9` at alpha 0.16 | decorative: enclosed, moved by a binding | presentation binding |
| `Visual/Marks` | Gold `#f7cb52` lines on the inner back wall every 0.25 m; a navy line at 0.75 m | decorative: printed flat | nothing |
| `Visual/PortFrames` | Navy `#293954` rectangles 0.40 × 0.20 on the two end faces, one high, one low | decorative: printed flat | nothing |
| `Loc_Port_mouth_start`, `Loc_Port_mouth_end` | Socket locators, channel bore | none | nothing |

Icon: a deep square chamber with a high slot on one side and a low slot on the other. It differs from the Water tank by the two rectangular slots at different heights.

### Behaviour and numbers

The chamber is a store with a high socket and a low socket. Level changes are L7 through whatever is joined to them. **Worked construction** (the chamber on the bench, floor 0.15 m up, `Start` datum 0.90 m up): a Sluice gate on each socket, each sloped 5° (0.30 m long, so 0.30 × sin 5° = 0.026 m of fall across it) and lifted by a 2.5 kg Weight on a rope over a Pulley ([EL-021](EL-021-sluice-gate.md): 2.5 kg lifts the blade fully; no Weight leaves it closed); an upper gutter (3 m, sloped 5° toward the upper gate, its low end's floor 0.776 m above the chamber floor, carried on Wall blocks); a full Water tank on Wall blocks 1.40 m high with a Tap at `opening` 1 whose free end is over the gutter's high end, so the stream lands along the gutter (2.56 m/s from about 0.36 m above its floor lands 0.69 m out at first and nearer as the tank drains: inside the 3 m channel). The lower gate's downstream socket is free unless stated (its stream falls to the bench and is spilled); a lower gutter sloped 5° along the flow may be joined there.

- **The supply drains.** The tap's flow falls as the tank drains, by the same head law as [EL-021](EL-021-sluice-gate.md) SG1: Q = K·√h with K = 0.04820 ([EL-003](EL-003-tap.md), `opening` 1) and √h = 1 − 0.04820·t / 2 on the tank's 1.0 m² plan, so Q(t) = 0.0482 × (1 − t/41.5) m³/s: 0.0482 at first, 0.0241 at 20.8 s, and zero when the 1.0 m³ tank is empty at 41.5 s. The volume delivered by time t is 1 − (1 − t/41.5)².
- **Filling (upper gate open, lower closed).** While the chamber is below 0.75 m its `Start` socket is dry and its head is the datum, so inflow is whatever the gutter passes, which follows the tap's falling flow. The gutter holds only the wedge that drives Q through the open gate's three apertures: its depth at the gate is (Q / 0.1228)² − 0.026 and its volume 0.40 × depth² / 0.175, so 0.40 × 0.128² / 0.175 = 0.037 m³ at the first flow and 0.40 × 0.0125² / 0.175 = 0.0004 m³ at 20.8 s. Delivering 0.75 m³ takes 41.5 × (1 − √(1 − 0.75)) = **20.8 s**, and the chamber passes 0.75 m about 0.0004 / 0.0241 = 0.02 s later; if the gutter instead held its full retained 0.091 m³ (EL-007, C(5°)), 41.5 × (1 − √0.159) = **24.9 s** at most. Above 0.75 m the `Start` socket is wet and the chamber and upper gutter share a surface s (above the chamber floor). When the tank is empty: s + 0.40 × (s − 0.776)² / 0.175 = 1.0 gives **s = 0.94 m** (0.94 + 2.286 × 0.164² = 1.00).
- **Holding.** With both gates closed nothing moves: a seat of zero area (L14).
- **Emptying (lower gate fully open, free downstream).** Three apertures in series, K = K_c/√3 = 0.1228 ([EL-021](EL-021-sluice-gate.md)); head d + 0.026. From 0.80 m: first outflow 0.1228 × √0.826 = **0.112 m³/s**; to 0.01 m it takes 2 × (√0.826 − √0.036) / 0.1228 = 2 × 0.719 / 0.1228 = **11.7 s**, empty at 2 × (√0.826 − √0.026) / 0.1228 = 12.2 s. Joined instead to a lower gutter sloped 5° away, the first outflow is more than the gutter carries (0.095 m³/s, EL-007), so the gutter overtops at its low rim at first, conserved (L9).
- **Emptying gently (lower gate at lift 0.05 m, s = 0.25, a 2.14 kg Weight).** Seat K = 0.6 × 0.02 × 4.42945 = 0.05315; with the two channel apertures 1/K² = 1/0.05315² + 2/0.21261² = 398.2, K = 0.0501. First outflow 0.0501 × √0.826 = 0.046 m³/s, inside the gutter; time to empty 2 × 0.748 / 0.0501 = 29.8 s.
- **Open ends cannot hold a level.** With both gates fully open and the draining tank supplying, the depth follows where outflow equals inflow: d + 0.026 = (Q / 0.1228)², at most (0.0482 / 0.1228)² − 0.026 = **0.128 m** at the first flow (0.160 m if the flow were 10% higher: (1.1 × 0.0482 / 0.1228)² − 0.026), far under the 0.75 m lift, and it falls as the tank drains: with Q(t) = 0.0482 × (1 − t/41.5), d(t) = (0.3925 × (1 − t/41.5))² − 0.026, so 0.063 m at 10 s and 0 once Q is at most 0.1228 × √0.026 = 0.0198 m³/s, from 41.5 × (1 − 0.0198 / 0.0482) = 24.4 s. Once the tank is empty, the gutter drains into the chamber (its low end is 0.026 m above the `Start` datum, so it empties entirely) and the chamber, which by then holds only the gutter's last wedge, empties within the 3.3 s it would take from the first-flow depth, 2 × (√0.154 − √0.036) / 0.1228.
- **Lifting a craft.** A Buoyant platform ([EL-028](EL-028-buoyant-platform.md): deck 0.90 × 0.20 × 0.90, 1.0 kg) with a Tennis ball (0.35 kg) aboard displaces 1.35 / 16 = 0.084 m³ and floats at a draught of 1.35 / (16 × 0.81) = 0.104 m. Its deck top goes from 0.20 m above the floor (aground) to 0.75 − 0.104 + 0.20 = 0.846 m when the level passes 0.75 m. The chamber's 1.0 × 1.0 interior clears the 0.90 × 0.90 deck by 0.05 m a side.
- **Displacement raises the level (L4, L12 a).** With 0.5 m³ held, the level is 0.50 m alone and 0.50 + 0.084 / 1.0 = **0.584 m** with the loaded platform afloat; both are below the 0.75 m `Start` datum, so nothing leaves.
- **Bodies do not pass the sockets.** A socket is a liquid aperture in a solid wall.
- **Why these values.** A 0.75 m lift is three Wall courses and needs 0.75 m³, inside one Water tank; a 1.0 m² plan makes rise in metres equal volume in cubic metres. The 5° socket normals give a joined pound the slope it needs: a gutter that must hold liquid slopes 5° toward a joined end ([EL-007](EL-007-open-gutter.md)); the snap turns the moving part by the shortest arc until its mouth faces the target's exactly (`engine/TubeMouthSnap.cs:76`–`:77`), and two mouths count as joined only within a dot product of −0.9999 (`:28`). On a level socket the snap would turn a joined gutter level, and a level gutter drains out of its free far end.

## Engine capabilities

Map families (navigation only, D14): EnvironmentState, FiniteLedger, FluidAdvection, GeometryQuery, JointConstraint, PressureWork, RigidBodyDynamics, StateTransaction, TopologyTransaction.

| Row | Mark | Use here |
| --- | --- | --- |
| W1 static body and colliders | present | shell |
| W22, W23 authored state, refusals | present | pose, `initial_volume` |
| W4 fluid port contract | present, untested | store and sockets |
| W3 snap sockets | narrower: socket count, package-local ids, capture distance 0.20 m and angle 20°, and outlets that are not joinable are `ENGINE-CAPABILITY-INSTANCES`; compiling joins to edges is `ENGINE-LIQUID`; the port record is `ENGINE-GAS-NETWORK` | 2 sockets, 0 immersed ports, 0 outlets |
| W6 store; W7 joins, apertures, flow; W9 overflow and ledger | missing | `ENGINE-LIQUID` |
| W10 immersion and displaced volume | missing | `ENGINE-LIQUID` |
| W12 fill-fraction driver; W13 m³ unit | missing | W12: `ENGINE-LIQUID`; W13's m³ unit: `ENGINE-GAS-NETWORK`, which lands first (EL-001 W13; owner decision 18, 22 November 2026) |
| Liquid store as thermal node | missing | `ENGINE-THERMAL` |

Counts: 3 present, 1 untested, 1 narrower, 7 missing (W6, W7, W9, W10, W12, W13, thermal).

### Engine conformance (22 November 2026)

1. **Declaration data only.** A static body, five colliders, one store with an interior, an aperture, an overflow point and two sockets at different heights, one parameter, two bindings. No lock sequence exists in code: the order of gates is the player's machine.
2. **Generic laws.** Filling, holding, emptying and the failure to hold with open ends are all L7 with the seats of the joined gates. Lifting a craft is L12 a; the raised level is L4.
3. **Three layers.** *Colliders:* five boxes. *Visuals:* the table above. *Animation:* the water box reads the committed fill.
4. **One-way flow and typing.** Socket ids are package-local typed ids, roles are enums; `initial_volume` is f32.
5. **Flat-state counts.** Bodies 1 (static); colliders 5 boxes; joints 0; drives 0; sensors 0; signal nodes 0; liquid nodes 1 store; thermal nodes 1, one per store; sockets 2; immersed ports 0; outlets 0; open apertures 1; overflow points 1; wire ports 0; bindings 2; parameters 1.
6. **Performance fitness.** Five boxes, as the Receiver. Static. One bounded query of the interior for immersion, from `ENGINE-SPATIAL-QUERIES` (L18).
7. **Tests are scenarios.** Rows K1 to K9.

## Sources and legacy

- Requirement: "Bounded chamber changes water level through separately controlled openings"; outcome "Open ends cannot hold a raised level without a balancing supply." No variants.
- Integration [sequence-task-390](../requirements.md#sequence-task-390) ("sluice-controlled floating transport"); refinement [S708](../invest/refinements.md#s708) ("Control floating transport with actual discharge"). Component research [water](../../component-research.md#water): recipe Canal Lift.
- **Legacy.** None at `a6c914e` (searched for "lock", "canal", "sluice", "water"). **Files harvested:** none.

## Acceptance outline

Authority: [element-027](../requirements.md#element-027). Tolerances L17. Planned class `LockChamberTests`. Rows use the worked construction. "Gate open" means a 2.5 kg Weight hangs on its rope; "closed" means none.

| # | Given / When / Then | Planned test | Control | Boundary |
| --- | --- | --- | --- | --- |
| K1 | Given the upper gate open and the lower closed, when Run advances until the chamber depth passes 0.75 m (hand time 20.8 to 24.9 s; stall guard 75 s; the level declares a Run length of 75 s (`ENGINE-RUN-LENGTH`)), then the chamber holds at least 0.75 m³ and the ledger identity holds at every sampled tick | `LockChamberTests.TheUpperOpeningFillsTheChamber` | K2 | — |
| K2 | Given both gates closed and the chamber authored empty, when Run advances 20 s, then the chamber holds under 0.004 m³, the upper gutter holds 0.091 m³ within 0.004 m³ and the tank's further loss is in `Spilled` (the gutter overflows at its low rim, outside the chamber) | `LockChamberTests.ClosedOpeningsPassNothing` | K1 | — |
| K3 | Given `initial_volume` 0.80, no supply, the upper gate closed (0.80 m is above the `Start` datum) and the lower gate open, when Run advances until the chamber is under 0.01 m deep (hand time 11.7 s plus the gate's lift; stall guard 36 s; the level declares a Run length of 40 s (`ENGINE-RUN-LENGTH`)), then stores, `Spilled` and `Drained` account for the 0.80 m³ within 0.004 m³ | `LockChamberTests.TheLowerOpeningEmptiesTheChamber` | lower gate closed: the volume stays 0.80 m³ within 0.004 m³ for 20 s | — |
| K4 | Given both gates open and the full supply tank, when Run advances 65 s (the tank empties by 41.5 s, 45.7 s with 10%; the level declares a Run length of 65 s (`ENGINE-RUN-LENGTH`)), then the chamber depth never passes 0.19 m while the tank holds liquid (hand: 0.128 m at the first flow, falling with the tap's flow to 0.063 m at 10 s and to the gate's drop alone from 24.4 s), and is under 0.01 m within 20 s of the tank emptying | `LockChamberTests.OpenEndsDoNotHoldARaisedLevel` | K1 (lower gate closed: the depth passes 0.75 m) | — |
| K5 | Given a Buoyant platform aground in the empty chamber with a Tennis ball on it, when K1 runs (Run length 75 s), then the platform's deck top rises at least 0.60 m (hand 0.646 m) and the ball is still on it | `LockChamberTests.ARisingLevelLiftsAPlatformAndItsCargo` | chamber not filled (upper gate closed): the deck does not rise | — |
| K6 | Given `initial_volume` 0.5 with both gates closed and the loaded platform of K5 afloat, when Run advances 10 s, then the committed level is 0.584 m within 0.01 m | `LockChamberTests.DisplacementRaisesTheLevel` | without the platform: 0.50 m within 0.01 m | — |
| K7 | Given a gutter sloped 5° offered to the `Start` socket and a water pipe offered to it, then the gutter joins and the pipe does not | `LockChamberTests.OnlyChannelSocketsJoin` | — | a level gutter offered within 20° snaps to the 5° slope and joins; one offered more than 20° off does not snap |
| K8 | Given a run that filled and emptied, then Reset restores `initial_volume` and zeroes the accounts; save and load keep `initial_volume`, the pose and both joins | `LockChamberTests.ResetAndSaveLoadRestore` | the state before Reset differs | `initial_volume` 0 and 1.2 |
| K9 | Given `initial_volume` below 0, above 1.2 or non-finite, then it is refused with a typed reason and the construction is unchanged | `LockChamberTests.AVolumeOutsideItsLimitsIsRefused` | the limits are admitted | next f32 beyond each limit |

Generic cover: W22 and W23 (refusal mechanism, authored state); no wire port, so any wire naming it is refused. K5 and K6 need EL-028; the Weights hang outside the liquid. Chrome recipe (pending): place the chamber, snap a sloped Sluice gate on each socket and a gutter beyond the upper one, feed the upper gutter from a tank and tap, float a platform inside, raise and lower the gates with ropes and Weights.

## Open questions

All resolved; none needs the owner.

1. Built-in gates: none. Two Sluice gates joined to the sockets are the separately controlled openings, so the gate law exists once (EL-021).
2. A larger sluice for the chamber: not needed; emptying through one fully open gate takes 12.2 s.
3. Craft passing through a gate: not modelled (D19). The lesson is the lift.
4. Size and lift parameters: none (D13). An authored starting volume is needed (other parts' rows and this part's K3 and K6 author a depth): `initial_volume`, as EL-004 has.
5. Socket normals 5° off the horizontal: decided under the standing delegation, because the kit's gutters hold liquid only when sloped toward a joined end and a join needs opposite normals; a pound joined here takes the 5° slope through the snap's alignment (K7 boundary).

## Integration

- **Works with.** EL-021, EL-007, EL-003, EL-001, EL-028, EL-029, EL-170 (none delivered); Rope anchor, Pulley, Weight and Wall (delivered) through the gates and supports.
- **Campaign.** Chapter 7, levels 61–70 (Canal Lift); reuse 81–100, 126–140, 146–150. First taught at **67a**, after the Buoyant platform (66a) its rows K5 and K6 lift, and before the Boat in the same level, which needs the lock (coordinator decision 36; [per-level allocation](readiness/campaign-coverage.md#6-per-level-allocation-and-progression-check-owner-decisions-24e-and-24f)). Needs the lesson-fixture story (S8).

## Delivery

- **Slice shape.** Single element, after EL-021 and EL-028.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), in particular `ENGINE-SPATIAL-QUERIES` (stream samples, immersion and point queries, L18), `ENGINE-ISLANDS-SLEEP` (a change of contents or level wakes what it loads; not delivered, and nothing here assumes sleeping exists) and `ENGINE-CAPABILITY-INSTANCES` (declared sockets, outlets and immersed-port locators); then `ENGINE-GAS-NETWORK` (the one fluid port record); then `ENGINE-LIQUID`; then `ENGINE-THERMAL` (every liquid store is a thermal node, owner decisions 18 and 20); `ENGINE-RUN-LENGTH` (rows K1, K3, K4, K5). `ENGINE-JOINT-KINDS` reaches it through EL-021's spring rest position. Then EL-001, EL-003, EL-007, EL-021, EL-028; S7.
- **Files.** Adds `elements/LockChamber/LockChamber.csproj`, `LockChamberElementDeclaration.cs`, `lock_chamber.tscn`, `icon.svg`, `CuriousContraptions.tests/LockChamberTests.cs`; edits `CuriousContraptions.slnx` and this spec. Other lists as EL-001.
- **Review tier.** Data-only.
- **Disqualifiers.** A store socket above the surface that cannot receive (L5 says it can); a channel socket that cannot sit in a wall above floor height or face 5° off its body axis; displaced volume not added to the level; no `initial_volume` on a store.

## Coverage binding (planned, not yet live)

Blocked on the coverage follow-on story S7. The named-identity binding section (the EL-239 form) cannot be written yet: umbrella `todo-339`; `sequence-task-580` co-anchored with `element-027`, not with a todo; no `s448` anchor.
