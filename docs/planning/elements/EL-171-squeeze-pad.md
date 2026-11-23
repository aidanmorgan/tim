# EL-171 · Squeeze pad — named-identity spec

**Current delivery (24 October 2026):** [EL-171 delivery slice](../invest/named-elements.md#element-171). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes.

Brought to build-ready detail on 22 November 2026 against checkout `8d1d68aa` and repaired the same day against the independent audit `docs/planning/elements/readiness/audits/elements-batch-g.md` and the owner decisions of 22 November 2026. Liquid conventions L1 to L18 and probe rows W1 to W24 are in [EL-001](EL-001-finite-reservoir.md), which has adopted Batch G's LG1 to LG8 into L3 to L15; LG numbers are kept as short names (LG1 = L5 and L6; LG2 = L3 and L7; LG3, LG4, LG5 = L4, L11, L12; LG6 = L15; LG7 = L12; LG8 = L13). The further additions adopted by coordinator decision 21 (nesting and envelope shielding, buoyancy reaction on a carrying body, contents at the liquid centroid, overlapping shapes counted once, porous and sealed interiors, a mouth below the floor datum, the relief mouth, the crest rule with granted work head, no crest lift limit) are now written into EL-001's rows (its "Batch G's additions" paragraph maps A1 to A9 to L3, L4, L5, L7, L8, L9 and L12; the long wording stays in [readiness/batch-g.md](readiness/batch-g.md) section 2) and are cited here as **L-additions A1 to A9**. Probe rows G1 to G8 are in section 3 of that file. All are cited by number and not restated. Box sizes are full extents in metres.

## Identity

| Field | Value |
| --- | --- |
| Identity | EL-171 · Squeeze pad · Water |
| Anchors | [element-171](../requirements.md#element-171), co-anchored `sequence-task-724`; source record [campaign coverage](../requirements.md#campaign-element-coverage) and scope index [todo-340](../requirements.md#todo-340); integration [sequence-task-531](../requirements.md#sequence-task-531); ledger [element-171](../invest/named-elements.md#element-171), owner S464 |
| Disposition | To deliver as a single element, second of the porous run. Not a duplicate of the Sponge ([EL-034](EL-034-sponge.md)): it is fixed, it has a pressed plate, and compression releases its liquid |
| Planned package | id `squeeze_pad`, directory `elements/SqueezePad/`, title "Squeeze pad" |
| Status | Not started. Blocked on the engine programme (owner decision 3a), then `ENGINE-JOINT-KINDS` (spring rest coordinate), `ENGINE-GAS-NETWORK`, `ENGINE-LIQUID` and `ENGINE-THERMAL` (Delivery) |
| Related | [EL-034](EL-034-sponge.md), [EL-035](EL-035-wick.md), [EL-168](EL-168-siphon-priming-bulb.md) (the same capacity rule), [EL-036](EL-036-sprinkler.md) and [EL-003](EL-003-tap.md) (wetting sources), [EL-004](EL-004-catch-basin.md) |

## Declaration

Closest delivered shape: the Pressure plate, a static tray with a plate on a spring-returned prismatic slider.

- **Interfaces.** `ISceneGeometryElement`, `ISliderElement`, the liquid store declaration of `ENGINE-LIQUID` (W4, W6), porous (L4, L11, A6) with a coordinate-bound capacity (L4).
- **Body 0 (static tray), five boxes.** Base 0.62 × 0.06 × 0.62 at (0, −0.16, 0) (underside y = −0.19); two walls 0.04 × 0.26 × 0.62 at (±0.29, 0, 0); two walls 0.54 × 0.26 × 0.04 at (0, 0, ±0.29). Inside the walls 0.54 × 0.54; floor at y = −0.13; wall top at y = +0.13. Family vessel material (EL-001: restitution 0.12, friction 0.3, rolling resistance 0).
- **Body 1 (dynamic press), three colliders.** Plate: box 0.50 × 0.04 × 0.50 at (0, 0.14, 0) at rest, lying on the pad inside the walls. Post: Cylinder radius 0.08, length 0.30, axis Y, at (0, 0.31, 0). Cap: Cylinder radius 0.15, length 0.04, at (0, 0.48, 0), top at y = +0.50. 0.2 kg in all; inertia by the plate's box rule, 0.0042 kg·m² about X and Z and 0.0083 about Y. Material restitution 0.1, friction 0.5, rolling resistance 0, bounce threshold 0.1 m/s. Gravity (0, −9.81, 0). Press and tray do not collide with each other.
- **Slider.** `PrismaticSliderDescriptor` along −Y (pressing is positive), travel 0 to 0.20 m, starting offset 0. Return spring 100 N/m, **damping 40 N·s/m**, spring rest coordinate −0.03 m, outside the travel: 3 N holds the press up against its own 1.96 N. No drive. At full travel the plate's underside is 0.05 m above the tray floor. A prismatic slider declares its spring rest coordinate, which may lie outside its travel; `SliderTravel` admits it; the starting offset stays a separate value (owner `ENGINE-JOINT-KINDS`, owner decision 6 and coordinator decision 21). Today this is refused, not merely absent: `CuriousContraptions.Bundle/MechanicalValueDomain.cs:81` returns false when `lower > 0f || upper < 0f`; `Compression` (`CuriousContraptions.Abstractions/MechanicalContractValues.cs:111`) is the starting offset (`MechanicalValueDomain.cs:80`, `engine/gpu/WorkshopPhysicsCompiler.cs:159`), not a rest position.
- **Store (porous, bound capacity).** Local id 1 on body 0. Rest capacity C₀ = 0.03125 m³ (half a mark; the 0.50 × 0.25 × 0.50 pad is half pore). Plan area 0.54 × 0.54 = 0.2916 m²; floor datum (0, −0.13, 0). **Open aperture 0.54 × 0.54 at y = +0.50**, normal +Y; **interior 0.54 × 0.63 × 0.54** (the aperture extruded to the floor datum, L4), which contains the plate, the post and the cap. Porous: it keeps its contents at every orientation (L11 does not apply). Its interior is the declared box above, not a carrying collider (A6's porous form), because the pad itself is enclosed art with no collider. Capacity bound to the slider coordinate x: C(x) = 0.03125 × (1 − 4·x), so 0.00625 m³ at full travel. Liquid above C(x) is pending overflow and leaves in the next substep (L9) through the declared relief (A8), here the spout overflow point. The level by L4 is V/0.2916, at most 0.107 m above the floor, and the plate's underside is 0.25 − x above it, more than 0.107 × (1 − 4x) at every travel: the press is never below the surface and is not immersed (L12 a).
- **Store 1 is also a thermal node** (owner decision 20; [TH-04](TH-04-electrical-heating-plate.md) section 11, T2): heat capacity ρ·V × 1046 J/(kg·K), starting temperature declared ambient 288 K unless a level authors another; inflow mixes by mass and outflow leaves at the store's temperature. No row of this part reads temperature.
- **Spout (an overflow point, not a mouth).** At (0.31, −0.10, 0) on the tray's +X face, **0.09 m above the tray's underside**: the liquid leaves it as an L8 stream with no discharge speed of its own (L9): its start velocity is the velocity of body 0 at the spout, which is static, so the stream starts at rest. No socket, immersed port or outlet.

| Parameter | Type | Unit | Default | Min | Max | Editor policy |
| --- | --- | --- | --- | --- | --- | --- |
| `initial_volume` | Real f32 | m³ (unit member missing, W13) | 0 | 0 | 0.03125 | Reject; build only |

- **Presentation.** `Visual/Press` follows body 1's pose. `Visual/Pad` `ScaleY` from the committed slider travel (`PresentationDriver.CommittedPrismaticPhase`) and albedo from the committed fill fraction of store 1 (W12, missing).
- **Palette.** `ElementCategory.Structure`; order 543 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)); colour gold (247/255, 203/255, 82/255).

### Art and solidity

| Node | Mesh and size | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Tray` | Base and four walls, cream `#fff8e9` | solid; the five box colliders | body 0 pose (static) |
| `Visual/Press` | Plate 0.50 × 0.04 × 0.50 with navy `#293954` holes printed flat, post and cap, coral `#f06e54` | solid; the plate box and two Cylinders | body 1 pose |
| `Visual/Pad` | Block 0.50 × 0.25 × 0.50 between floor and plate, gold `#f7cb52`, ochre when wet | decorative: enclosed by the tray walls and the plate, scaled by a binding | presentation binding |
| `Visual/Spout` | Navy lip printed on the +X face, flush | decorative: printed flat | nothing |

Icon: a square tray holding a dotted gold block under a plate with a knob. It differs from the Sponge (a loose block) by the tray and knob, and from the Priming bulb (round, upright) by its square outline.

### Behaviour and numbers

The pad is a porous store whose capacity follows the press. Loads are **set on the cap at rest**. Travel follows force by the spring: (x + 0.03) × 100 N/m = the load's weight + 1.96 N. The damping ratio with a load of mass m is 40 / (2·√(100 × (0.2 + m))); every load below is at or above 0.98, so the press does not overshoot its static travel and the one-way release follows the table.

| Load on the cap | Force | Damping ratio | Travel x | C(x) | Released from a saturated pad |
| --- | --- | --- | --- | --- | --- |
| nothing | 1.96 N | 4.47 | 0 (held by the 3 N preload) | 0.03125 | 0 |
| Tennis ball authored at 0.1 kg | 2.94 N | 40 / (2·√30) = 3.65 | 0 (under the 3 N preload) | 0.03125 | 0 |
| Tennis ball, 0.35 kg | 5.39 N | 2.70 | 0.024 m | 0.0283 | 0.0030 m³ |
| Weight, 1.5 kg | 16.68 N | 1.53 | 0.137 m | 0.0141 | **0.0171 m³** |
| Weight, 2.0 kg | 21.58 N | 1.35 | 0.186 m | 0.0080 | 0.0232 m³ |
| Bowling ball, 4 kg | 41.2 N | 0.98 | 0.20 m (stop) | 0.00625 | 0.0250 m³ |

Rows by hand: 1.5 kg: x = 0.1668 − 0.03 = 0.1368 m; C = 0.03125 × (1 − 0.547) = 0.01415 m³; released 0.03125 − 0.01415 = 0.0171 m³. 2.0 kg: x = 0.1858 m; C = 0.03125 × 0.2568 = 0.00803 m³; released 0.0232 m³. 0.1 kg: 0.981 + 1.96 = 2.94 N, under the 3 N preload by 0.06 N, so the plate stays on its stop.

- **A dry pad produces nothing.** With V = 0 no press takes V above C(x).
- **A part-wet pad** releases only what exceeds C(x): holding 0.025 m³ under the 1.5 kg Weight it gives 0.025 − 0.0141 = 0.0109 m³; under a 0.35 kg Tennis ball (C = 0.0283), nothing.
- **Squeezing consumes work.** The 1.5 kg Weight and the 0.2 kg press descend 0.137 m and give up 1.7 × 9.81 × 0.137 = 2.28 J; the spring holds ½ × 100 × (0.167² − 0.03²) = 1.35 J and the damper takes the rest, 0.93 J. The liquid returns none of it (L12 c). A load that cannot beat the 3 N preload does no work and releases nothing.
- **Wetting.** Each sample path that ends inside the store's declared interior is credited to it; a sample that meets any other collider first is spilled (L8). The interior contains the plate, post and cap, so a stream that lands on them is credited; a load on the cap that overhangs the tray shadows it. From a tap at `opening` 0.125 (0.0103 m³/s) a dry pad saturates in 0.03125 / 0.0103 = 3.0 s; more is pending overflow and leaves at the spout. Pressed, it takes only up to C(x).
- **It keeps its water** until pressed, in any mounting: upright the preload holds the press with 3 − 1.96 = 1.04 N to spare (the worst case), on its side with 3 N, inverted with 4.96 N.
- **It can be used again.** Released and re-wetted, it gives again; this is the difference from the Priming bulb, which cannot refill.
- **Where the water goes.** From the spout, 0.09 m above the tray's underside, the stream starts with the static tray's velocity, zero (L9), and falls straight down beside the tray. A basin can go under it only if the pad stands above the basin's 0.55 m rim; the reference rig below places it so.
- **Tick order.** The store reads the travel committed by the previous substep (L16).

**Reference rig (rows Z1 to Z8).** The pad is static and is placed with its tray underside 0.65 m above the bench on a Wall 0.12 × 0.65 × 0.62 (turned so its 0.12 m is along X; faces at x = ±0.06) under its centre. A Catch basin ([EL-004](EL-004-catch-basin.md): interior 1.25 × 0.40 × 1.0, floor datum (0, −0.125, 0)) stands on the bench with its long axis along X, its near outer wall face at x = 0.09 (0.03 m clear of the Wall) and its near inner face at x = 0.21, so the spout (x = 0.31, 0.74 m above the bench) is 0.10 m inside the basin's interior in plan and 0.19 m above its rim; the tray's underside clears the basin's rim by 0.10 m.

## Engine capabilities

Map families (navigation only, D14): CapillaryTransport, EnvironmentState, FiniteLedger, FluidAdvection, GeometryQuery, JointConstraint, PressureWork, RigidBodyDynamics, StateTransaction, TopologyTransaction.

| Row | Mark | Use here |
| --- | --- | --- |
| W1 static tray; W21 press body with a box and Cylinders | present | bodies |
| W18 prismatic slider with limits, spring stiffness and damping | present | press |
| W22, W23 authored state, refusals | present | `initial_volume` |
| W4 fluid port contract | present, untested | store |
| `CommittedPrismaticPhase` driver (`CuriousContraptions.Abstractions/ElementPresentation.cs:31`) | present, untested | pad height (confirmed as an enum member only; no test of this driver was opened) |
| G8 spring rest coordinate outside the travel | narrower: refused today (`MechanicalValueDomain.cs:81`) | `ENGINE-JOINT-KINDS` |
| W6 store; W8 sample-path credit inside a declared interior; W9 pending overflow at an overflow point | missing | `ENGINE-LIQUID` |
| Capacity bound to a committed coordinate (L4), its excess pending overflow through the declared relief (L9, A8) | missing | `ENGINE-LIQUID` |
| Porous store: retention at every orientation (L11) with a declared interior (L4, A6) | missing | `ENGINE-LIQUID` |
| W12 fill-fraction driver; W13 m³ unit | missing | W12: `ENGINE-LIQUID`; W13's m³ unit: `ENGINE-GAS-NETWORK`, which lands first (EL-001 W13; owner decision 18, 22 November 2026) |
| Liquid store as thermal node | missing | `ENGINE-THERMAL` |

Counts (by row): 3 present, 2 untested, 1 narrower, 5 missing. One owner per capability: liquid (`ENGINE-LIQUID`), m³ unit (`ENGINE-GAS-NETWORK`), spring rest coordinate (`ENGINE-JOINT-KINDS`), thermal node (`ENGINE-THERMAL`). No socket, so no W3 row.

### Engine conformance (22 November 2026)

1. **Declaration data only.** Two bodies, eight colliders, one slider, one porous store with a bound capacity, an aperture, an interior and an overflow point, one parameter, three bindings.
2. **Generic laws.** The press moves by contact against a declared spring and damper. Release is "a store never holds more than its capacity" with the capacity bound to a coordinate (L4), the excess pending overflow (L9). "A dry pad cannot produce water" is V = 0. Wetting is the stream credit of L8 inside a declared interior.
3. **Three layers.** *Colliders:* five tray boxes; a plate box and two Cylinders on the press. *Visuals:* the press art follows its body. *Animation:* the pad block is enclosed art scaled by the committed travel; it is not a collider and nothing can reach it.
4. **One-way flow and typing.** Mechanics → coordinate → store capacity → liquid; nothing flows back.
5. **Flat-state counts.** Bodies 2 (1 static, 1 dynamic); colliders 8 (6 boxes, 2 Cylinders); joints 1 prismatic; drives 0; sensors 0; signal nodes 0; liquid nodes 1 store; thermal nodes 1, one per store; sockets 0; immersed ports 0; outlets 0; open apertures 1; overflow points 1; bindings 1 body + 2 drivers; parameters 1.
6. **Performance fitness.** Eight colliders against the Pressure plate's ten. The walls enclose the pad art, so no visible solid is without a collider; the post and cap let a load wider than the tray press the plate. One bounded interior per store and one stream's fixed sample paths while the spout runs, candidates from `ENGINE-SPATIAL-QUERIES` (L18).
7. **Tests are scenarios.** Rows Z1 to Z8.

## Sources and legacy

- Requirement: "Mechanical compression expels retained liquid from a porous store"; outcome "A dry pad cannot produce water and squeezing consumes work." No variants.
- Integration [sequence-task-531](../requirements.md#sequence-task-531). Component research [water](../../component-research.md#water): "compression releases it".
- **Legacy.** None at `a6c914e` (searched for "squeeze", "pad", "sponge", "water"). **Files harvested:** none.

## Acceptance outline

Authority: [element-171](../requirements.md#element-171). Tolerances L17. Planned class `SqueezePadTests`. Every row uses the reference rig; every load is set on the cap at rest at Run start; "saturated" is `initial_volume` 0.03125.

| # | Given / When / Then | Planned test | Control | Boundary |
| --- | --- | --- | --- | --- |
| Z1 | Given a saturated pad and a 1.5 kg Weight set on the cap, when Run advances 10 s, then the basin has gained 0.0171 m³ within 0.004 m³ and the pad holds 0.0141 m³ within 0.004 m³ | `SqueezePadTests.APressReleasesWhatNoLongerFits` | Z2, Z3 | Bowling ball: 0.0250 m³ |
| Z2 | Given a dry pad under the same Weight, when Run advances 10 s, then every committed flow is under 0.0005 m³/s (L17) and the basin stays empty | `SqueezePadTests.ADryPadProducesNoWater` | Z1 | — |
| Z3 | Given a saturated pad with nothing on the cap, when Run advances 20 s, then its volume is unchanged within 0.004 m³ and the travel stays under 0.002 m | `SqueezePadTests.AnUnpressedPadKeepsItsWater` | Z1 | mounted on its side and inverted: the same |
| Z4 | Given Z1, then the Weight has descended 0.137 m within 0.015 m and the potential energy of Weight and press has fallen by at least the spring's 1.35 J | `SqueezePadTests.SqueezingConsumesWork` | Z3 | a Tennis ball authored at 0.1 kg (2.94 N with the press against the 3 N preload) does not move the plate (travel under 0.002 m) and releases nothing (pad unchanged within 0.004 m³) |
| Z5 | Given a pad authored at 0.025 m³, when Run advances 10 s under a 0.35 kg Tennis ball, then nothing leaves (C = 0.0283 m³); under the 1.5 kg Weight it gives 0.0109 m³ within 0.004 m³ | `SqueezePadTests.OnlyTheExcessLeaves` | Z1 (saturated: 0.0171 m³) | — |
| Z6 | Given a dry pad under a tap at `opening` 0.125 on a full Water tank standing on a Wall 1.24 × 1.69 × 1.24 (so the tap's free `End` is 1.84 m above the bench, 0.5 m above the aperture at 1.34 m) and its stream landing inside it, when Run advances 10 s, then the pad holds 0.03125 m³ within 0.004 m³ and the remainder has left at the spout into the basin | `SqueezePadTests.AStreamWetsThePad` | stream beside the tray: the pad stays dry | the stream landing on the cap: credited the same |
| Z7 | Given `initial_volume` below 0, above 0.03125 or non-finite, then it is refused with a typed reason | `SqueezePadTests.AVolumeOutsideItsLimitsIsRefused` | limits admitted | next f32 beyond each limit |
| Z8 | Given a run, then Reset restores the press and the authored volume; save and load keep `initial_volume` and the pose; the ledger identity holds at every sampled tick of Z1 | `SqueezePadTests.ResetSaveLoadAndLedger` | — | — |

Hand calculations. Z1, Z4 and Z5: the table and Behaviour. With 1.7 kg on the spring ω_n = √(100/1.7) = 7.67 s⁻¹ and the slower root is 7.67 × (1.53 − √(1.53² − 1)) = 7.67 × 0.372 = 2.9 s⁻¹, a 0.35 s time constant: settled well inside 10 s. Z6: the tap's stream leaves at 0.0103 / 0.01885 = 0.55 m/s and lands 0.55 × √(2 × 0.5 / 9.81) = 0.18 m out; by 10 s the tank has given about 0.10 m³, so about 0.07 m³ has left at the spout.

Generic cover: W22, W23. No wire port. Chrome recipe (pending): wet the pad from a tap, put a basin under its spout, set a Weight on the knob.

## Open questions

All resolved; none needs the owner.

1. Shared declaration with the Sponge: no. The Sponge is a loose body; this is a fixture with a press. They share the porous-store rule (L4, L11).
2. Force from the liquid on the plate: none (L12 c). The declared spring is what makes squeezing cost work.
3. Capacity parameter: none (D13); `initial_volume` is authored so a lesson can start wet.
4. Pad art exposed above the walls: avoided by walls as high as the pad, so the art is enclosed.
5. Interior: the aperture is raised to the cap's top so the store's own press lies inside its interior; a stream landing on the press is credited, not spilled.
6. Damping: 40 N·s/m, so a load set at rest presses to its static travel and no further. A load dropped from a height can press further; rows set loads at rest.

## Integration

- **Works with.** Weight, balls, Wall, Linear pusher (delivered) as presses and stand; EL-003, EL-004, EL-014, EL-036 (not delivered).
- **Campaign.** Chapter 9 "Stored Potential", levels 81–90; reuse 91–100, 131–140, 146–150. Needs the lesson-fixture story (S8).

## Delivery

- **Slice shape.** Single element, after EL-034 and EL-168 (which prove the porous store and the bound capacity).
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), in particular `ENGINE-SPATIAL-QUERIES` (stream samples, immersion and point queries, L18), `ENGINE-ISLANDS-SLEEP` (a change of contents or level wakes what it loads; not delivered, and nothing here assumes sleeping exists) and `ENGINE-CAPABILITY-INSTANCES` (declared sockets, outlets and immersed-port locators); then `ENGINE-GAS-NETWORK` (the one fluid port record); then `ENGINE-LIQUID`; then `ENGINE-THERMAL` (every liquid store is a thermal node, owner decisions 18 and 20), plus `ENGINE-JOINT-KINDS` (the spring rest coordinate); then EL-001, EL-003, EL-004, EL-034, EL-168; then S7.
- **Files.** Adds `elements/SqueezePad/SqueezePad.csproj`, `SqueezePadElementDeclaration.cs`, `squeeze_pad.tscn`, `icon.svg`, `CuriousContraptions.tests/SqueezePadTests.cs`; edits `CuriousContraptions.slnx` and this spec. Other lists as EL-001.
- **Review tier.** Data-only if the probe finds the bound capacity and the porous store delivered.
- **Disqualifiers.** A store capacity that cannot follow a prismatic coordinate; an interior that cannot contain the store's own press; a scale binding that cannot read slider travel; a slider spring rest coordinate outside the travel still refused.

## Coverage binding (planned, not yet live)

Blocked on the coverage follow-on story S7. The named-identity binding section (the EL-239 form) cannot be written yet: the ledger entry links only the campaign coverage row; `sequence-task-724` is co-anchored with `element-171`, not with a todo; no `s464` anchor.
