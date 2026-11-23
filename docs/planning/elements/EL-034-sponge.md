# EL-034 · Sponge — named-identity spec

**Current delivery (24 October 2026):** [EL-034 delivery slice](../invest/named-elements.md#element-034). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes.

Brought to build-ready detail on 22 November 2026 against checkout `8d1d68aa` and repaired the same day against the independent audit `docs/planning/elements/readiness/audits/elements-batch-g.md` and the owner decisions of 22 November 2026. Liquid conventions L1 to L18 and probe rows W1 to W24 are in [EL-001](EL-001-finite-reservoir.md), which has adopted Batch G's LG1 to LG8 into L3 to L15; LG numbers are kept as short names (LG1 = L5 and L6; LG2 = L3 and L7; LG3, LG4, LG5 = L4, L11, L12; LG6 = L15; LG7 = L12; LG8 = L13). The further additions adopted by coordinator decision 21 (nesting and envelope shielding, buoyancy reaction on a carrying body, contents at the liquid centroid, overlapping shapes counted once, porous and sealed interiors, a mouth below the floor datum, the relief mouth, the crest rule with granted work head, no crest lift limit) are now written into EL-001's rows (its "Batch G's additions" paragraph maps A1 to A9 to L3, L4, L5, L7, L8, L9 and L12; the long wording stays in [readiness/batch-g.md](readiness/batch-g.md) section 2) and are cited here as **L-additions A1 to A9**. Probe rows G1 to G8 are in section 3 of that file. All are cited by number and not restated. Box sizes are full extents in metres.

## Identity

| Field | Value |
| --- | --- |
| Identity | EL-034 · Sponge · Water |
| Anchors | [element-034](../requirements.md#element-034), co-anchored `sequence-task-587`; source record [todo-340](../requirements.md#todo-340); integration [sequence-task-531](../requirements.md#sequence-task-531); ledger [element-034](../invest/named-elements.md#element-034), owner S455 |
| Disposition | To deliver as a single element; first consumer of the porous store (L4, L11, L12, the store once called LG5; interior by A6). The Squeeze pad ([EL-171](EL-171-squeeze-pad.md)) is the identity that releases liquid by compression; the Sponge only takes it up |
| Planned package | id `sponge`, directory `elements/Sponge/`, title "Sponge" |
| Status | Not started. Blocked on the engine programme (owner decision 3a), then `ENGINE-GAS-NETWORK`, `ENGINE-LIQUID` and `ENGINE-THERMAL` |
| Related | [EL-035](EL-035-wick.md), [EL-171](EL-171-squeeze-pad.md), [EL-004](EL-004-catch-basin.md), [EL-003](EL-003-tap.md), Pressure plate and Ramp (delivered) |

## Declaration

Closest delivered shape: the Weight, a dynamic body with declared mass; closest planned shape: the Water bucket, a store on a dynamic body.

- **Interfaces.** `ISceneGeometryElement`, `IMechanicalElement`, the liquid store declaration of `ENGINE-LIQUID` (W4, W6) with the porous fields (L4, L11, L12; A6).
- **Body.** One dynamic body. `PayloadPolicy.None`. Origin at the box centre.
- **Collider (one).** Box 0.50 × 0.25 × 0.50 (0.0625 m³).
- **Mass and inertia.** Dry 0.25 kg (4 kg/m³). Box rule: Ix = Iz = 0.25 × (0.25² + 0.50²)/12 = 0.0065 kg·m²; Iy = 0.25 × (0.50² + 0.50²)/12 = 0.0104 kg·m². Contents add ρ·V at the centroid of the liquid region given by L4, with no inertia added (A5); upright and saturated that is (0, −0.0625, 0). Gravity (0, −9.81, 0).
- **Material (f32).** Restitution 0.05, friction 0.6, rolling resistance 0.05, bounce threshold 0.1 m/s: soft and grippy.
- **Store (porous).** Local id 1 on body 0. Capacity 0.03125 m³ (half a mark, 0.5 kg: half the block's volume, the rest being the sponge's own matter); plan area 0.25 m². **Interior: 0.50 × 0.25 × 0.50, the collider's own volume (A6), above the floor datum (0, −0.125, 0).** Retained at every orientation (no tilt loss, L11). Immersion uptake rate 0.01 m³/s at full immersion (L12). Open aperture 0.50 × 0.50 on the top face, y = +0.125, normal +Y, moving with the body. Streams: each sample path that ends inside the store's declared interior is credited to it, and a sample that ends on the aperture face of its carrying collider is credited (A6); a sample that meets any other collider first is spilled (L8). Where interiors nest (the sponge floating in a basin), a sample resolves to the innermost interior (A1). Overflow point (0, −0.125, 0.25), the lower front edge: liquid above capacity is pending overflow and leaves in the next substep (L9) from that point. Starts at `initial_volume`. Store 1 is also a thermal node (owner decision 20; [TH-04](TH-04-electrical-heating-plate.md) section 11, T2): heat capacity ρ·V × 1046 J/(kg·K), starting temperature declared ambient 288 K unless a level authors another; inflow mixes by mass and outflow leaves at the store's temperature. No row of this part reads temperature.
- **Ports, sockets.** None.

| Parameter | Type | Unit | Default | Min | Max | Editor policy |
| --- | --- | --- | --- | --- | --- | --- |
| `initial_volume` | Real f32 | m³ (unit member missing, W13) | 0 | 0 | 0.03125 | Reject; build only; player-editable unless the level locks it (D11) |

- **Presentation.** `Visual` follows the body's pose. `Visual/Sponge` albedo red, green and blue from the committed fill fraction of store 1, dry gold to wet ochre (W12, missing).
- **Palette.** `ElementCategory.Motion`; palette order 166 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)); colour gold (247/255, 203/255, 82/255).

### Art and solidity

| Node | Mesh and size | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Sponge` | Box 0.50 × 0.25 × 0.50 with rounded edges inside the box, gold `#f7cb52`, darkening to ochre `#c28f52` when wet | solid; the box collider | body pose (dynamic); colour by a presentation binding |
| `Visual/Pores` | Navy `#293954` dots on every face | decorative: printed flat | body pose |

Icon: a gold block covered in dots. It differs from the Squeeze pad (a tray with a plate on top) by being a loose block, and from the Wall by its dots and rounded outline.

### Behaviour and numbers

- **Soaking in a vessel (L12 a with porous uptake).** A dry sponge floats with 4/16 = 25% of its volume under water. It draws liquid from the store it floats in at 0.01 × (submerged fraction) m³/s. Its mass is 0.25 + 16·V, so the submerged fraction is (0.25 + 16·V)/(16 × 0.0625) = 0.25 + 16·V and dV/dt = 0.01 × (0.25 + 16·V): V(t) = 0.015625 × (e^(0.16·t) − 1). It is saturated (0.03125 m³) when e^(0.16·t) = 3: t = ln 3 / 0.16 = **6.9 s**.
- **Saturated, it takes no more.** At capacity the uptake is zero. It weighs 0.25 + 0.5 = 0.75 kg (12 kg/m³) and floats with 0.75 / 16 / 0.25 = 0.1875 m of its 0.25 m height under water: **75%**.
- **What it removes.** The vessel's store loses 0.03125 m³, but while the sponge floats its displaced volume grows by the same amount (it displaces its own mass, 16·V more), so the vessel's level does not change: in a Catch basin authored at 0.375 m³ the depth is (0.375 + 0.015625)/1.25 = 0.3125 m before and (0.34375 + 0.046875)/1.25 = 0.3125 m after (L4).
- **Soaking under a stream.** From a tap at `opening` 0.125 (0.0103 m³/s from a full Water tank, [EL-003](EL-003-tap.md) row T2) whose stream lands on the top face, it saturates in 0.03125 / 0.0103 = **3.0 s**; the excess is pending overflow and leaves at the overflow point in the next substep (L9), conserved.
- **It gets heavier.** Dry 0.25 kg; saturated 0.75 kg. On a default Pressure plate (0.5 kg threshold) a dry sponge does not press it; under the tap it presses it once V passes (0.5 − 0.25)/16 = 0.0156 m³, after 0.0156 / 0.0103 = 1.5 s. This is the "observed before it produces an action" row.
- **It keeps its water.** Tumbled or carried, it loses nothing (porous store, L11); only the Squeeze pad's compression rule (capacity bound to a coordinate, L4) releases liquid, and the Sponge does not declare it.
- **Dry bench.** Away from liquid it stays dry: uptake needs immersion or a credited stream.

## Engine capabilities

Map families (navigation only, D14): CapillaryTransport, EnvironmentState, FiniteLedger, FluidAdvection, GeometryQuery, JointConstraint, PressureWork, RigidBodyDynamics, StateTransaction, TopologyTransaction.

| Row | Mark | Use here |
| --- | --- | --- |
| W21 dynamic body with declared mass and inertia | present | the block |
| W22, W23 authored state, refusals | present | pose, `initial_volume` |
| W4 fluid port contract | present, untested | store |
| W11 contents add to the carrying body's mass | narrower | gas stores only today, at the body's centre; contents at the liquid centroid (A5) are a new mass-source kind, `ENGINE-LIQUID` |
| W6 store; W8 stream credit at an aperture on a body; W9 overflow | missing | `ENGINE-LIQUID` |
| W10 immersion | missing | `ENGINE-LIQUID` |
| Porous store: interior is its carrying collider (A6), immersion uptake at a declared rate, retention at every orientation (L4, L11, L12) | missing | `ENGINE-LIQUID` (its map families already name CapillaryTransport) |
| W12 fill-fraction driver; W13 m³ unit | missing | W12: `ENGINE-LIQUID`; W13's m³ unit: `ENGINE-GAS-NETWORK`, which lands first (EL-001 W13; owner decision 18, 22 November 2026) |
| Liquid store as thermal node | missing | `ENGINE-THERMAL` |

Counts, one per capability: 3 present (W21, W22, W23), 1 untested (W4), 1 narrower (W11), 8 missing (W6, W8, W9, W10, porous store, W12, W13, thermal).

### Engine conformance (22 November 2026)

1. **Declaration data only.** One dynamic body, one collider, one porous store with an interior, an aperture and an overflow point, one parameter, four bindings.
2. **Generic laws.** Uptake is the porous-store rule (L12) on the immersion of L12 a; stream credit is L8 with A6; the weight is A5; "saturated takes no more" is V = C.
3. **Three layers.** *Collider:* one box. *Visuals:* follow the body. *Animation:* the colour reads the committed fill.
4. **One-way flow and typing.** Values f32.
5. **Flat-state counts.** Bodies 1 (dynamic); colliders 1 box; joints 0; drives 0; sensors 0; signal nodes 0; liquid nodes 1 store; thermal nodes 1, one per store; sockets 0; immersed ports 0; outlets 0; open apertures 1; overflow points 1; wire ports 0; bindings 1 body + 3 drivers; parameters 1.
6. **Performance fitness.** One box. Uptake reuses the immersion result already computed for buoyancy; the interior is the collider, so no extra query (L18). Islands that hold it wake when its contents change (`ENGINE-ISLANDS-SLEEP`, not delivered).
7. **Tests are scenarios.** Rows J1 to J9.

## Sources and legacy

- Requirement: "Absorbs available liquid up to material capacity"; outcome "Saturated sponge cannot remove additional volume." No variants.
- Integration [sequence-task-531](../requirements.md#sequence-task-531). Component research [water](../../component-research.md#water): "Absorbs finite water (adds mass)"; "Damp fraction ← volume".
- **Legacy.** None at `a6c914e` (searched for "sponge", "absorb", "porous", "water"). **Files harvested:** none.

## Acceptance outline

Authority: [element-034](../requirements.md#element-034). Tolerances L17. Planned class `SpongeTests`.

| # | Given / When / Then | Planned test | Control | Boundary |
| --- | --- | --- | --- | --- |
| J1 | Given a dry sponge set on a Catch basin authored at 0.375 m³, when Run advances 21 s (three times 6.9 s), then the sponge holds 0.03125 m³ within 0.004 m³, the basin lost the same within 0.004 m³ and the basin's depth is 0.3125 m within 0.01 m | `SpongeTests.ItSoaksUpToItsCapacity` | J3 | — |
| J2 | Given J1 continued 20 s more (41 s in all; the level declares a Run length of 45 s (`ENGINE-RUN-LENGTH`)), then neither volume changes by more than 0.004 m³ | `SpongeTests.ASaturatedSpongeRemovesNoMore` | J1 | — |
| J3 | Given a dry sponge on an empty basin, when Run advances 10 s, then it holds under 0.004 m³ | `SpongeTests.NoLiquidNoUptake` | J1 | — |
| J4 | Given a dry sponge on the bench under a tap at `opening` 0.125 whose stream lands on its top face, when Run advances 10 s, then it holds 0.03125 m³ within 0.004 m³ and the rest of the tank's loss is in `Spilled` or a receiver below | `SpongeTests.ItCapturesAStreamAndOverflows` | sponge beside the stream: stays under 0.004 m³ | — |
| J5 | Two Runs with the sponge on a default Pressure plate under that tap. With `opening` 0.125, the plate's contact is open at Run start and closes within the stall guard 10 s (hand 1.6 s). With `opening` 0, it never closes in 10 s | `SpongeTests.AWetSpongePressesAPlateADryOneDoesNot` | the two Runs are each other's control | — |
| J6 | Given `initial_volume` 0.03125 (saturated) and the sponge released at the top of a Ramp, when it has come to rest, then it still holds 0.03125 m³ within 0.004 m³ | `SpongeTests.ItKeepsItsWaterWhenTumbled` | `initial_volume` 0: it stays under 0.004 m³ | — |
| J7 | Given J1, then the sponge's waterline settles with 75% of its height (0.1875 m) under water within 0.01 m | `SpongeTests.ASoakedSpongeRidesLower` | dry: 25% (0.0625 m) within 0.01 m | — |
| J8 | Given a run, then Reset restores `initial_volume` at the authored pose; save and load keep `initial_volume` and the pose; the art is gold when dry and ochre when saturated | `SpongeTests.ResetSaveLoadAndColour` | the state before Reset differs | `initial_volume` 0 and 0.03125 |
| J9 | Given `initial_volume` below 0, above 0.03125 or non-finite, then it is refused with a typed reason and the construction is unchanged | `SpongeTests.AVolumeOutsideItsLimitsIsRefused` | the limits are admitted | next f32 beyond each limit |

Generic cover: W22 and W23 (refusal mechanism, authored state). No port. Chrome recipe (pending): drop the sponge in a filled basin, watch the level and the colour.

## Open questions

All resolved; none needs the owner.

1. Release by compression: not in this part. The Squeeze pad is the identity for it (D8, D19).
2. Capacity parameter: none (D13). An authored starting volume is: `initial_volume`, as the Squeeze pad has, so a level can place a saturated sponge (row J6).
3. Evaporation or drying: none (D19). It dries on Reset.
4. Where its contents act: at the liquid centroid (A5), which for a porous store follows L4 at the committed pose.

## Integration

- **Works with.** Pressure plate, Ramp (delivered); EL-003, EL-004, EL-036 (not delivered).
- **Campaign.** Chapter 9 "Stored Potential", levels 81–90; reuse 91–100, 131–140, 146–150. Needs the lesson-fixture story (S8).

## Delivery

- **Slice shape.** Single element, first of the porous run (EL-034, EL-171, EL-035).
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), in particular `ENGINE-SPATIAL-QUERIES` (stream samples, immersion and point queries, L18), `ENGINE-ISLANDS-SLEEP` (a change of contents or level wakes what it loads; not delivered, and nothing here assumes sleeping exists) and `ENGINE-CAPABILITY-INSTANCES` (declared sockets, outlets and immersed-port locators); then `ENGINE-GAS-NETWORK` (the one fluid port record); then `ENGINE-LIQUID`; then `ENGINE-THERMAL` (every liquid store is a thermal node, owner decisions 18 and 20); `ENGINE-RUN-LENGTH` (row J2). Then EL-001, EL-003, EL-004, EL-016; S7.
- **Files.** Adds `elements/Sponge/Sponge.csproj`, `SpongeElementDeclaration.cs`, `sponge.tscn`, `icon.svg`, `CuriousContraptions.tests/SpongeTests.cs`; edits `CuriousContraptions.slnx` and this spec. Other lists as EL-001.
- **Review tier.** Data-only if the probe finds the porous store delivered.
- **Disqualifiers.** No uptake from immersion; a store on a body that always applies tilt loss; a porous store whose interior cannot be its carrying collider; contents placed at the body's centre rather than the liquid centroid; an albedo binding that cannot read a liquid observation.

## Coverage binding (planned, not yet live)

Blocked on the coverage follow-on story S7. The named-identity binding section (the EL-239 form) cannot be written yet: umbrella `todo-340`; `sequence-task-587` co-anchored with `element-034`, not with a todo; no `s455` anchor.
