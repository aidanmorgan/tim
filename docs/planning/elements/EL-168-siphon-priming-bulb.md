# EL-168 · Siphon priming bulb — named-identity spec

**Current delivery (24 October 2026):** [EL-168 delivery slice](../invest/named-elements.md#element-168). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes.

Brought to build-ready detail on 22 November 2026 against checkout `8d1d68aa` and repaired the same day against the independent audit `docs/planning/elements/readiness/audits/elements-batch-g.md` and the owner decisions of 22 November 2026. Liquid conventions L1 to L18 and probe rows W1 to W24 are in [EL-001](EL-001-finite-reservoir.md), which has adopted Batch G's LG1 to LG8 into L3 to L15; LG numbers are kept as short names (LG1 = L5 and L6; LG2 = L3 and L7; LG3, LG4, LG5 = L4, L11, L12; LG6 = L15; LG7 = L12; LG8 = L13). The further additions adopted by coordinator decision 21 (nesting and envelope shielding, buoyancy reaction on a carrying body, contents at the liquid centroid, overlapping shapes counted once, porous and sealed interiors, a mouth below the floor datum, the relief mouth, the crest rule with granted work head, no crest lift limit) are now written into EL-001's rows (its "Batch G's additions" paragraph maps A1 to A9 to L3, L4, L5, L7, L8, L9 and L12; the long wording stays in [readiness/batch-g.md](readiness/batch-g.md) section 2) and are cited here as **L-additions A1 to A9**. Probe rows G1 to G8 are in section 3 of that file. All are cited by number and not restated. Box sizes are full extents in metres.

## Identity

| Field | Value |
| --- | --- |
| Identity | EL-168 · Siphon priming bulb · Water |
| Anchors | [element-168](../requirements.md#element-168), co-anchored `sequence-task-721`; source record [campaign coverage](../requirements.md#campaign-element-coverage); integration [sequence-task-389](../requirements.md#sequence-task-389); ledger [element-168](../invest/named-elements.md#element-168), owner S461 |
| Disposition | To deliver as a single element, directly after the Siphon ([EL-024](EL-024-primed-siphon.md)). First consumer of a store whose capacity follows a committed coordinate (L4) with a relief mouth (A8) |
| Planned package | id `priming_bulb`, directory `elements/PrimingBulb/`, title "Priming bulb" |
| Status | Not started. Blocked on the engine programme (owner decision 3a), then `ENGINE-JOINT-KINDS` (spring rest coordinate), `ENGINE-GAS-NETWORK`, `ENGINE-LIQUID` and `ENGINE-THERMAL` (Delivery) |
| Related | [EL-024](EL-024-primed-siphon.md) (its consumer), [EL-171](EL-171-squeeze-pad.md) (the same rule on a pad), [EL-022](EL-022-water-pump.md) (the other primer), Weight (delivered) |

## Declaration

Closest delivered shape: the Powered gate's static frame with a body on a spring-returned prismatic slider, without its drive.

- **Interfaces.** `ISceneGeometryElement`, `ISliderElement`, the liquid store declaration of `ENGINE-LIQUID` (W4, W6) with a coordinate-bound capacity (L4) and a relief mouth (A8).
- **Body 0 (static cup), one collider.** Tube: inner radius 0.16, outer radius 0.20, length 0.25, axis Y. Origin at its centre. Family vessel material (EL-001: restitution 0.12, friction 0.3, rolling resistance 0).
- **Body 1 (dynamic plunger), two Cylinders, axis Y.** Piston radius 0.15, length 0.30, centred at (0, 0.25, 0) at rest: its foot is 0.025 m inside the cup. Cap radius 0.25, length 0.04, at (0, 0.42, 0). 0.2 kg in all; inertia ½·m·r² = 0.0023 kg·m² about Y and 0.0026 about X and Z (piston values). Material restitution 0.1, friction 0.5, rolling resistance 0, bounce threshold 0.1 m/s. Gravity (0, −9.81, 0). Plunger and cup do not collide with each other.
- **Slider.** `PrismaticSliderDescriptor` along −Y (pressing is positive), travel 0 to 0.20 m, starting offset 0. Return spring 100 N/m, **damping 40 N·s/m**, spring rest coordinate −0.03 m, outside the travel: (0 + 0.03) × 100 = 3 N holds the plunger up against its own 1.96 N. No drive. A prismatic slider declares its spring rest coordinate, which may lie outside its travel; `SliderTravel` admits it; the starting offset stays a separate value (owner `ENGINE-JOINT-KINDS`, owner decision 6 and coordinator decision 21). Today this is refused, not merely absent: `CuriousContraptions.Bundle/MechanicalValueDomain.cs:81` returns false when `lower > 0f || upper < 0f`; `Compression` (`CuriousContraptions.Abstractions/MechanicalContractValues.cs:111`) is the starting offset (`MechanicalValueDomain.cs:80`, `engine/gpu/WorkshopPhysicsCompiler.cs:159`), not a rest position.
- **Store (L4, sealed, A6).** Local id 1 on body 0. Rest capacity C₀ = 0.02 m³; plan area π × 0.16² = 0.0804 m². Capacity bound to the slider coordinate x: C(x) = 0.02 × (1 − x / 0.25), so 0.004 m³ at full travel. **Sealed:** no open aperture, no overflow point and no interior (A6): nothing is immersed in it, probed in it or streamed into it. With no rim, L11's tilt loss does not apply: it keeps its charge at every pose. Liquid above C(x) is pending overflow and leaves in the next substep (L9) through the declared relief (A8).
- **Store 1 is also a thermal node** (owner decision 20; [TH-04](TH-04-electrical-heating-plate.md) section 11, T2): heat capacity ρ·V × 1046 J/(kg·K), starting temperature declared ambient 288 K unless a level authors another; inflow mixes by mass and outflow leaves at the store's temperature. No row of this part reads temperature.
- **Relief mouth (one socket).** `End`, pipe bore, Discharge (outward only, L5), capture 0.20 m and 20°, at (0, −0.125, 0), normal −Y, declared as store 1's relief mouth (A8) and as nothing else: it carries the store's pending overflow into the joined node in the next substep, or as a free end if unjoined, and passes no head-driven flow (L7), so an unpressed bulb never drains by gravity into what it joins. It joins a Siphon's `Branch` mouth, on which the cup stands.

| Parameter | Type | Unit | Default | Min | Max | Editor policy |
| --- | --- | --- | --- | --- | --- | --- |
| `initial_volume` | Real f32 | m³ (unit member missing, W13) | 0.02 | 0 | 0.02 | Reject; build only |

- **Presentation.** `Visual/Plunger` follows body 1's pose. `Visual/Charge` `ScaleY` from the committed fill fraction of store 1 (W12, missing).
- **Palette.** `ElementCategory.Control`; order 415 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)); colour coral (240/255, 110/255, 84/255).

### Art and solidity

| Node | Mesh and size | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Cup` | Tube, outer radius 0.20, length 0.25, cyan `#66b8c9` at alpha 0.16, with a cream `#fff8e9` foot ring printed flush | solid; the Tube collider (true hollow) | body 0 pose (static) |
| `Visual/Plunger` | Piston radius 0.15 and cap radius 0.25, coral `#f06e54` | solid; the two Cylinder colliders | body 1 pose |
| `Visual/Charge` | Cyan column inside the cup | decorative: enclosed, moved by a binding | presentation binding |
| `Loc_Port_mouth_end` | Socket locator, bore 0.10 | none | nothing |

Icon: a coral mushroom-shaped plunger standing in a clear cup. It differs from the Squeeze pad (a wide square tray and plate) by its round, upright outline. It is drawn as a plunger, not a rubber bulb, so that everything that moves is a body with a collider.

### Behaviour and numbers

The bulb is a small charged store that a press empties into whatever its relief mouth joins. Loads are **set on the cap at rest**. Its travel follows force by the spring: (x + 0.03) × 100 N/m = the load's weight + 1.96 N. The damping ratio with a load of mass m is 40 / (2·√(100 × (0.2 + m))); every load below is at or above 0.98, so the press does not overshoot its static travel, and a one-way release that follows the largest travel reached delivers what the table says.

| Load on the cap | Force | Damping ratio | Travel x | C(x) | Delivered from a full bulb | Primes a Siphon (0.0125 m³)? |
| --- | --- | --- | --- | --- | --- | --- |
| nothing | 1.96 N | 40 / (2·√20) = 4.47 | 0 (held by the 3 N preload) | 0.0200 | 0 | no |
| Tennis ball, 0.35 kg | 5.39 N | 40 / (2·√55) = 2.70 | 0.024 m | 0.0181 | 0.0019 m³ | no |
| Weight, 1.5 kg | 16.68 N | 40 / (2·√170) = 1.53 | 0.137 m | 0.0091 | **0.0109 m³** | **no** (0.0016 m³ short) |
| Weight, 2.0 kg | 21.58 N | 40 / (2·√220) = 1.35 | 0.186 m | 0.0051 | **0.0149 m³** | yes |
| Bowling ball, 4 kg | 41.2 N | 40 / (2·√420) = 0.98 | 0.20 m (stop) | 0.0040 | 0.0160 m³ | yes |

Rows by hand: 1.5 kg: x = 16.68 / 100 − 0.03 = 0.1368 m; C = 0.02 × (1 − 0.1368 / 0.25) = 0.00906 m³; delivered 0.0200 − 0.0091 = 0.0109 m³, under the Siphon's 0.0125 m³ prime. 2.0 kg: x = 0.2158 − 0.03 = 0.1858 m; C = 0.02 × 0.2568 = 0.00514 m³; delivered 0.0149 m³. The lightest load that primes from a full bulb needs C ≤ 0.0075 m³, x ≥ 0.156 m, force ≥ 18.6 N: 1.70 kg. The Bowling ball's ratio 0.98 overshoots by e^(−π·0.98/√(1 − 0.98²)) ≈ 10⁻⁶ of its travel, and it rests on the stop anyway (static travel 0.40 m).

- **Finite.** One charge, one priming. Released, the spring lifts the plunger and the capacity returns, but nothing flows back: the relief mouth is Discharge only and carries no head flow. A bulb holding no more than its pressed capacity delivers nothing.
- **Insufficient priming.** A press lighter than 1.70 kg, or a bulb authored below 0.0165 m³ (which cannot deliver 0.0125 m³ even at full travel: 0.0165 − 0.004 = 0.0125), leaves the Siphon's prime inventory short and the Siphon closed.
- **Pressing consumes work.** The 2.0 kg Weight and the 0.2 kg plunger descend 0.186 m and give up 2.2 × 9.81 × 0.186 = 4.01 J; the spring holds ½ × 100 × (0.216² − 0.03²) = 2.29 J and the damper takes the rest, 1.72 J. The liquid returns none of it (L12 c).
- **No remote fill.** Liquid reaches the Siphon only through the joined pair `End`–`Branch` (L6). An unjoined bulb's pending overflow leaves `End` as a free end, an L8 stream with no discharge speed of its own (L9): its start velocity is the velocity of body 0 at `End`, which is static, so the stream starts at rest and goes wherever it lands: each sample path that ends inside a store's declared interior is credited to it; a sample that meets any other collider first is spilled (L8).
- **Every orientation.** Upright the preload holds the plunger with 3 − 1.96 = 1.04 N to spare (the worst case); on its side with 3 N; inverted, weight and spring both push it onto the stop, 4.96 N. Sealed, it keeps its charge in each.
- **Conserved.** The charge is authored inventory in the ledger (L10); after priming it is in the Siphon, then in the receiver.
- **Tick order.** The store reads the travel committed by the previous substep (L16).

## Engine capabilities

Map families (navigation only, D14): EnvironmentState, FiniteLedger, FiniteWorkActuation, FluidAdvection, GasState, GeometryQuery, JointConstraint, PressureWork, RigidBodyDynamics, StateTransaction, TopologyTransaction. GasState is not used: priming is by transferred liquid.

| Row | Mark | Use here |
| --- | --- | --- |
| W1 static body; W2 Tube; W21 plunger body with Cylinders | present | bodies |
| W18 prismatic slider with limits, spring stiffness and damping | present | plunger |
| W22, W23 authored state, refusals | present | `initial_volume` |
| W4 fluid port contract | present, untested | store and relief mouth |
| W3 sockets and joins | narrower: socket count, package-local ids, capture distance 0.20 m and angle 20°, and outlets that are not joinable are `ENGINE-CAPABILITY-INSTANCES`; compiling joins to edges is `ENGINE-LIQUID`; the port record is `ENGINE-GAS-NETWORK` | 1 socket (`End`), 0 immersed ports, 0 outlets |
| G8 spring rest coordinate outside the travel | narrower: refused today (`MechanicalValueDomain.cs:81`) | `ENGINE-JOINT-KINDS` |
| W6 store; W7 joins; W8 free end | missing | `ENGINE-LIQUID` |
| Capacity bound to a committed coordinate (L4), its excess pending overflow through a relief mouth (L9, A8) | missing | `ENGINE-LIQUID` |
| Sealed store with no interior (A6) | missing | `ENGINE-LIQUID` |
| W12 fill-fraction driver; W13 m³ unit | missing | W12: `ENGINE-LIQUID`; W13's m³ unit: `ENGINE-GAS-NETWORK`, which lands first (EL-001 W13; owner decision 18, 22 November 2026) |
| Liquid store as thermal node | missing | `ENGINE-THERMAL` |

Counts (by row): 3 present, 1 untested, 2 narrower, 5 missing. One owner per capability: liquid (`ENGINE-LIQUID`), m³ unit (`ENGINE-GAS-NETWORK`), sockets (`ENGINE-CAPABILITY-INSTANCES`), spring rest coordinate (`ENGINE-JOINT-KINDS`), thermal node (`ENGINE-THERMAL`).

### Engine conformance (22 November 2026)

1. **Declaration data only.** Two bodies, three colliders, one slider, one sealed store with a bound capacity and one relief mouth, one parameter, two bindings.
2. **Generic laws.** The plunger moves by contact against a declared spring and damper. The transfer is "a store never holds more than its capacity" with the capacity a clamped linear map of a coordinate (L4), the store-side twin of the seat map L14 b; the excess is pending overflow through the relief (L9, A8). Nothing names a siphon.
3. **Three layers.** *Colliders:* Tube and two Cylinders. *Visuals:* the plunger art follows its body. *Animation:* the charge column reads the committed fill.
4. **One-way flow and typing.** Mechanics → coordinate → store capacity → liquid; nothing flows back. Mouth id and role are enums.
5. **Flat-state counts.** Bodies 2 (1 static, 1 dynamic); colliders 3 (1 Tube, 2 Cylinders); joints 1 prismatic; drives 0; sensors 0; signal nodes 0; liquid nodes 1 store (sealed, no interior); thermal nodes 1, one per store; sockets 1 (the relief mouth); immersed ports 0; outlets 0; bindings 1 body + 1 driver; parameters 1.
6. **Performance fitness.** Three colliders; the cap is there so a Weight or ball can rest on the plunger. Constant work per store; a sealed store takes no immersion or stream query.
7. **Tests are scenarios.** Rows Y1 to Y8.

## Sources and legacy

- Requirement: "Finite mechanical compression expels gas or transfers liquid to establish a supported liquid column"; outcome "Insufficient priming leaves the siphon broken; no remote automatic fill." The row offers two means; this spec takes "transfers liquid".
- Integration [sequence-task-389](../requirements.md#sequence-task-389). Component research [water](../../component-research.md#water): "never … bypasses priming".
- **Legacy.** None at `a6c914e`. **Files harvested:** none.

## Acceptance outline

Authority: [element-168](../requirements.md#element-168). Tolerances L17. Planned class `PrimingBulbTests`. The bulb stands on the `Branch` mouth of a Siphon in the worked placement of [EL-024](EL-024-primed-siphon.md). Every load is set on the cap at rest at Run start.

| # | Given / When / Then | Planned test | Control | Boundary |
| --- | --- | --- | --- | --- |
| Y1 | Given a full bulb and a 2.0 kg Weight set on its cap, when Run advances until the tank falls below 0.9 m³ (stall guard 15 s), then the Siphon is flowing and the bulb holds 0.0051 m³ within 0.004 m³ | `PrimingBulbTests.AFullPressPrimesTheSiphon` | Y2, Y3 | — |
| Y2 | Given a full bulb and a 1.5 kg Weight set on its cap, when Run advances 15 s, then the Siphon's prime inventory holds 0.0109 m³ within 0.004 m³, the Siphon is closed and the tank keeps 1.0 m³ within 0.004 m³ | `PrimingBulbTests.AShortPressLeavesTheSiphonClosed` | Y1 | — |
| Y3 | Given a bulb authored at 0.012 m³ under a Bowling ball, then at most 0.008 m³ is delivered and the Siphon stays closed | `PrimingBulbTests.AnUnderfilledBulbCannotPrime` | Y1 | — |
| Y4 | Given the bulb 0.3 m beside the `Branch` mouth (not joined) and pressed as Y1, then the Siphon's prime inventory stays 0 and the delivered volume is in `Spilled` or a receiver below | `PrimingBulbTests.ThereIsNoRemoteFill` | Y1 | — |
| Y5 | Given a bulb authored at 0.004 m³ (the charge a full-travel press leaves) under a Bowling ball set on its cap, when Run advances 10 s, then the plunger is on its 0.20 m stop, every committed flow through `End` stays under 0.0005 m³/s (L17) and the Siphon stays closed | `PrimingBulbTests.OneChargeOnePriming` | Y1 (a full bulb under the same ball delivers 0.016 m³) | — |
| Y6 | Given nothing on the cap and `End` free, when Run advances 10 s, then the travel stays under 0.002 m and every committed flow through `End` is under 0.0005 m³/s | `PrimingBulbTests.ItDoesNotLeakUnpressed` | Y1 | mounted on its side and inverted: the same |
| Y7 | Given `initial_volume` below 0, above 0.02 or non-finite, then it is refused with a typed reason | `PrimingBulbTests.AVolumeOutsideItsLimitsIsRefused` | limits admitted | next f32 beyond each limit |
| Y8 | Given a run, then Reset restores the plunger, the charge and the Siphon; save and load keep `initial_volume`, pose and join; the ledger identity holds at every sampled tick of Y1 | `PrimingBulbTests.ResetSaveLoadAndLedger` | — | — |

Hand calculations. Y1 and Y2: the table. The slower root of the overdamped press is ω_n·(ζ − √(ζ² − 1)): with 2.0 kg, ω_n = √(100/2.2) = 6.74 s⁻¹ and the root is 6.74 × (1.35 − 0.91) = 3.0 s⁻¹, a time constant of 0.33 s, so the press settles well inside 15 s. Y3: 0.012 − 0.004 = 0.008 m³ < 0.0125 m³. Y5: C(0.20) = 0.004 m³ equals the charge, so nothing is pending.

Generic cover: W22, W23. No wire port. Chrome recipe (pending): snap the bulb onto the Siphon's gold ring, set a Weight on it.

## Open questions

All resolved; none needs the owner.

1. Expel gas or transfer liquid: transfer liquid. The requirement allows either; a gas path would need `ENGINE-GAS-NETWORK` joined to the liquid network for one part.
2. Refill: none. The outcome asks for finite compression.
3. A drawn rubber bulb: no. A squashing bulb would be art that looks solid without a collider; a plunger in a cup keeps visible = solid.
4. Capacity parameter: none (D13); `initial_volume` is the authored charge.
5. Damping: 40 N·s/m, so no load on the cap overshoots its static travel and the delivered volume follows the load's weight, not how it was dropped. A load dropped from a height can still press further than its weight alone; rows set loads at rest.

## Integration

- **Works with.** Weight, balls, Linear pusher (delivered) as presses; EL-024 (not delivered).
- **Campaign.** Chapter 7, levels 61–70; reuse 71–90, 114, 126–130, 136–150. Needs the lesson-fixture story (S8).

## Delivery

- **Slice shape.** Single element, directly after EL-024 (both taught in level 65a, the siphon first). It also owns the Siphon's primed rows, EL-024 P1 and P3 to P7, as cross-part rows.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), in particular `ENGINE-SPATIAL-QUERIES` (stream samples, immersion and point queries, L18), `ENGINE-ISLANDS-SLEEP` (a change of contents or level wakes what it loads; not delivered, and nothing here assumes sleeping exists) and `ENGINE-CAPABILITY-INSTANCES` (declared sockets, outlets and immersed-port locators); then `ENGINE-GAS-NETWORK` (the one fluid port record); then `ENGINE-LIQUID`; then `ENGINE-THERMAL` (every liquid store is a thermal node, owner decisions 18 and 20), plus `ENGINE-JOINT-KINDS` (the spring rest coordinate); then EL-001, EL-024; then S7.
- **Files.** Adds `elements/PrimingBulb/PrimingBulb.csproj`, `PrimingBulbElementDeclaration.cs`, `priming_bulb.tscn`, `icon.svg`, `CuriousContraptions.tests/PrimingBulbTests.cs`; edits `CuriousContraptions.slnx` and this spec. Other lists as EL-001.
- **Review tier.** Data-only if the probe finds the bound capacity and the relief mouth delivered.
- **Disqualifiers.** A store capacity that cannot follow a prismatic coordinate; a relief mouth that admits return flow or passes head-driven flow; a sealed store that is given an interior; a Tube collider with a bore radius of 0.16 m not admitted; a slider spring rest coordinate outside the travel still refused.

## Coverage binding (planned, not yet live)

Blocked on the coverage follow-on story S7. The named-identity binding section (the EL-239 form) cannot be written yet: the ledger entry links only the campaign coverage row; `sequence-task-721` is co-anchored with `element-168`, not with a todo; no `s461` anchor.
