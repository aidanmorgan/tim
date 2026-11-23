# TH-32 · Tinder pad — named-identity readiness spec

**Current delivery (24 October 2026):** [TH-32 delivery slice](../invest/named-elements.md#thermal-32). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Rewritten on 22 November 2026 as one current text against checkout `spec-drafts` (published main `8d1d68aa`); repaired the same day against the Batch N audit (`docs/planning/elements/readiness/audits/elements-batch-n.md`, finding A2 and the optical tick order), and on 23 November 2026 against `docs/planning/elements/readiness/audits/reaudit-group-3.md` (P5's Given). Code citations are `path:line` in that checkout; `path@a6c914e` citations appear only in section 4. Nothing was built or run: every number is a hand calculation shown here (steady values from the steady-state balance, transients from lumped estimates) and a starting value under decision D1 of [delivery batches](../invest/delivery-batches.md#3-family-level-decisions). Thermal rules are the [thermal conventions](TH-04-electrical-heating-plate.md#thermal-conventions) (T1 to T14); burning follows the [combustion conventions](TH-01-candle.md#combustion-conventions) (F1 to F6); light follows the [optical conventions](EL-176-red-laser-emitter.md#optical-conventions) (1 to 12). None is restated.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | TH-32 · Tinder pad · Thermal |
| Anchors | [requirements.md#thermal-32](../requirements.md#thermal-32) (task `sequence-task-466`); [named-elements.md#thermal-32](../invest/named-elements.md#thermal-32) |
| Owner key / research | S594 · [TH-S03 combustion and suppression](../../thermal-component-research.md#th-s03) |
| Related identities | Lit by [TH-31 Flint striker](TH-31-flint-striker.md), [TH-33 Spring-mounted match](TH-33-spring-mounted-match.md), focused light through [TH-06 Converging lens](TH-06-converging-lens.md) (TX-03) or a hot surface ([TH-04](TH-04-electrical-heating-plate.md)); lights [TH-02](TH-02-fire-bowl.md) and [TH-03](TH-03-combustible-block.md). No CAT refines it. |
| Campaign | Intro 58; practice 59; reuse 85, 149 ([thermal allocation](../requirements.md#thermal-campaign-allocation)) |
| Status | Not started. Blocked on the engine programme (owner decision 3a, 22 November 2026), and on ENGINE-THERMAL, ENGINE-IGNITION-BLAST and ENGINE-RUN-LENGTH (ENGINE-OPTICAL for row P6 only). |

### Disposition

**To deliver as a single element**, the lowest-threshold fuel. It can be the first burning part delivered, because the delivered-first Heating plate can light it (rows P1, P2). Planned package id `tinder_pad`, directory `elements/TinderPad/`, display name "Tinder pad".

## 2. Declaration

Follows the delivered Domino shape for a dynamic box part with no ports (`elements/Domino/DominoElementDeclaration.cs:36`).

- **Interfaces.** `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`) plus the thermal, fuel and optical-receiver contracts of the three engine stories. `KinematicMode.Dynamic`; `PayloadPolicy.None`.
- **Body.** One dynamic body, 0.15 kg; centre of mass at the tray centre; principal moments of a 0.36 × 0.06 × 0.36 m box: about Y 0.15 × (0.36² + 0.36²) ÷ 12 = 0.00324 kg·m², about X and Z 0.15 × (0.36² + 0.06²) ÷ 12 = 0.00167 kg·m².
- **Colliders (metres, boxes).** `col_tray`: centre (0, 0, 0), half extents (0.18, 0.02, 0.18). `col_wafer`: centre (0, 0.03, 0), half extents (0.15, 0.01, 0.15), 0.02 m proud of the tray; the face of node `fibre`.
- **Material (f32).** Friction 0.6, restitution 0.05, rolling resistance 0.
- **Thermal node.** `fibre`: 0.00425 J/K (10 mg of fibre × 425), ambient 0.02 W/K, face `col_wafer` 16 W/K, ignite row at 450 K, extinction 420 K. The tray declares no node: it neither stores nor passes heat.
- **Region link** (T3). A box 0.36 × 0.35 × 0.36 m centred at (0, 0.125, 0) in the pad's frame (0.05 m below the tray to 0.30 m above it), axis +Y in the pad's frame, fed by `fibre`, G_r 0.06 W/K (audit A2; was 0.2). A face collider's share is the area of overlap of its bounds with the box projected on the pad's XZ plane ÷ 0.1296 m², granted nearest-first along the axis, the sum capped at 1. The pad's own `col_wafer` lies inside the box; a link from `fibre` to its own face moves nothing, and the rows here rely on it taking no share of the cap (a clause requested for T3 in the Batch N summary: "a region link grants no share to the faces of the node that feeds it"). Without that clause the wafer's 0.09 ÷ 0.1296 = 0.69 would be granted before a body covering the pad from above.
- **Fuel store.** `wafer_fuel` on `fibre`: 0.05 kg × 15 625 = 781 J; 16 W while lit; credited to `fibre` itself (F1, a thin fuel); oxidiser 1.2/1024 kg of O₂ per kg (1.0 g/s of fuel, 1.2 mg/s of O₂).
- **Optical receiver** (optical convention 8; T3 "Radiation"). A disc aperture of radius 0.03 m (0.00283 m²) at the centre of `col_wafer`'s top face, one sample point at its centre with weight 1, sensed channels Red, Green and Blue, strength WeightedSum with weights 1/3. Its reading is an observation and credits nothing by itself. `col_wafer`, `fibre`'s face, declares absorptance 0.9 and keeps the default Absorb response: 0.9 × the light the optical solve booked on `col_wafer`'s absorbed record in a tick (collimated power ending anywhere on the wafer, plus the receiver's conserved diverging shares) moves to `fibre` in that tick's thermal step, and the rest stays on the record (T3 "Radiation"; booked once, coordinator decision 26). The tray is no face: light ending on it is booked on its own record and heats nothing.
- **Parameters.** None (D13).
- **Ports.** None. The requirement forbids a special lens interaction: light reaches the pad only as the reading of the receiver above.
- **Presentation bindings.** `Visual/Ember` emission ← committed burn power (0 to 16 W); `Visual/Ring_0` to `Ring_3` albedo warm wood → navy-grey, from the outer ring inward, as the committed fuel fraction falls past 0.75, 0.5, 0.25 and 0.
- **Palette.** Category `ElementCategory.Motion` (TH-04 section 6, item 5: burning cargo). Order: 145 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)).

### Art and solidity

| Art node | Mesh | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Tray` cream `#fff8e9` | box 0.36 × 0.04 × 0.36 m | solid, `col_tray` | body pose |
| `Visual/Wafer` warm wood `#c28f52` | box 0.30 × 0.02 × 0.30 m | solid, `col_wafer` | body pose |
| `Visual/Ring_0` to `Ring_3` | four concentric square rings of woven pattern printed flush on the wafer | decorative: printed flat, recoloured by a binding | body pose; presentation binding |
| `Visual/Ember` gold `#f7cb52` | a flat glow quad on the wafer centre | decorative: glow | body pose; presentation binding |

The "visibly shrinking fuel area" of the requirement is the rings greying from the outside in; the wafer's collider does not change. Icon: a square tray with a woven square. At a glance it differs from the Pressure plate by the woven concentric pattern and from the Combustible block by being a thin tray.

### Compiled counts

Bodies 1 (dynamic); colliders 2 (boxes); joints 0; sensors 0; thermal nodes 1; internal links 0; region links 1; fuel stores 1; ignite rows 1; optical receivers 1; ports 0; presentation bindings 5.

## 3. Engine capabilities

### Capability probe

| # | Capability | State | Owner or test |
| --- | --- | --- | --- |
| TH-04 R1 | Dynamic box colliders from art, visible = solid | present | test named in [TH-04](TH-04-electrical-heating-plate.md#3-engine-capabilities) |
| TH-04 R9 to R13 | Thermal nodes, contact conduction, ambient exchange, drivers, units | **missing** | ENGINE-THERMAL |
| TH-01 C1 | Store fraction as a presentation driver | narrower | ENGINE-IGNITION-BLAST |
| TH-01 C2 to C4 | Ignition and burn, oxidiser, spent-solid outcome | **missing** | ENGINE-IGNITION-BLAST |
| TH-01 C5 | Region link, here on a dynamic frame body | **missing** | ENGINE-THERMAL |
| K1 | A face's declared absorptance share of the light booked on its collider's absorbed record, moved to the face's node | **missing** | ENGINE-THERMAL (T3 "Radiation", delivered as an interface whose end-to-end proof lands in ENGINE-OPTICAL); the absorbed record, the receiver and its reading are ENGINE-OPTICAL's (optical conventions 7 and 8), and only row P6 needs them |

Counts: present 1, untested 0, narrower 1, missing 10 (R9 to R13, C2 to C5, K1). Each missing row has one owner. Nothing here needs code that names this part.

## 4. Sources and legacy

- **Requirement row** [thermal-32](../requirements.md#thermal-32): a finite porous combustible configures surface area, thermal mass and reaction properties; no special lens interaction; no variants. Visual style: "Warm-wood patterned wafer on a cream tray with a visibly shrinking fuel area".
- **Named entry** [thermal-32](../invest/named-elements.md#thermal-32): owner S594; "Defocused/insufficient heating fails; reacted material cannot be reused as fresh fuel".
- **Research** per-element table: "low-threshold combustible"; "catches from a striker or lens, lights the next fuel"; ember ← state.
- **Element map** row TH-32: ChemicalReaction, Extinction, FiniteLedger, GeometryQuery, Ignition, SensibleHeat, TopologyTransaction (D14).
- **Processes and scenarios.** [IX-28](../requirements.md#interaction-28), [IX-29](../requirements.md#interaction-29), [IX-30](../requirements.md#interaction-30), [IX-14](../requirements.md#interaction-14); [TX-03](../requirements.md#thermal-scenario-03).
- **Legacy.** None found. Searched `parts/`, `engine/physics/`, `engine/*.cs`, `engine/bridge/`, `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/Campaign` and `reference/` at a6c914e for tinder, kindling, ember and ignition terms. Consulted, no element knowledge: `reference/P0-022-before/docs/coverage/engine/task-002.json@a6c914e:L2783-L2787`.

## 5. Acceptance

Point of truth: [thermal-32](../requirements.md#thermal-32), [TX-03](../requirements.md#thermal-scenario-03) and [ELEMENT acceptance](../requirements.md#accept-element). Chrome proof is pending the post-catalogue phase.

### Behaviour and numbers

- **To ignite.** The fibre needs 0.00425 × 162 = 0.69 J to go from 288 K to 450 K. With nothing in its box it loses 0.02 W/K, so a steady input P settles at 288 + P ÷ 0.02 and ignites only above 0.02 × 162 = 3.24 W. Lying on a cold thermal face (a bowl's bed) it also loses 0.06 W/K through its region link and needs more than 0.08 × 162 = 13.0 W steady; a struck match beside it gives that for the first few tenths of a second ([TH-02](TH-02-fire-bowl.md) row B5).
- **Burn.** 781 ÷ 16 = 48.8 s. Free ember 288 + 16 ÷ 0.02 = 1088 K.
- **Covered ember** (audit A2). A 288 K thermal body whose bounds cover the box takes the full 0.06 W/K: the ember settles at 288 + 16 ÷ (0.02 + 0.06) = 488 K, 68 K above its 420 K extinction, and passes 0.06 × 200 = 12 W into the body. With the earlier 0.2 W/K it settled at 288 + 16 ÷ 0.22 = 361 K and went out; with the audit's 0.08 W/K it settles at 448 K, whose committed value (below) would sit about 13 K above extinction. The fibre is a 0.00425 J/K node coupled at 0.08 W/K, so its committed temperature can sit about 8% of its rise (15 K) below the continuous balance, depending on where T8 applies a tick's credit: about 473 K, still 53 K above 420 K. Rows assert that it stays lit and burns out its store, not the point value.
- **Smothered.** A thermal body resting on the wafer touches it: the contact link is 16 × 16 ÷ 32 = 8 W/K, so the fibre takes the body's temperature within a few milliseconds and goes out. A 0.2125 J/K wood surface pulls a burning fibre to (0.00425 × 1088 + 0.2125 × 288) ÷ 0.21675 = 304 K. Things are lit by holding them in the box, not by laying them on the ember.
- **On a hot surface.** Laid wafer-down on the Heating plate's disc, the pair conducts 16 × 64 ÷ 80 = 12.8 W/K, so the fibre follows the disc. With the pad on it, the disc's loss is 0.5 + 0.02 = 0.52 W/K: it settles at 288 + 120 ÷ 0.52 = 519 K with a time constant of 15.25 ÷ 0.52 = 29.3 s, and passes 450 K at −29.3 × ln(1 − 162 ÷ 231) = 35.5 s. A default Battery (3600 J, 30 s) leaves the disc at 288 + 231 × (1 − e^(−30 ÷ 29.3)) = 436 K: no ignition. A Battery of 7200 J empties at 60 s, after which the disc cools towards 288 + 16 ÷ 0.52 = 319 K and the fibre falls below 420 K at about 78 s with about 100 J unburnt; a Battery of 14 400 J (its maximum) runs to 120 s and the pad burns out its whole store at 35.5 + 48.8 = 84.3 s. Laid tray-down, the disc is inside the pad's region box (its 0.40 m bounds cover the whole 0.1296 m²), so the fibre is heated only through the region link: it settles at 288 + 0.06 ÷ 0.08 × (T_disc − 288) and needs the disc at 504 K; with the disc's loss 0.5 + 0.015 = 0.515 W/K (time constant 29.6 s, final 521 K) that is −29.6 × ln(1 − 216 ÷ 233) = 77.5 s.
- **Focused light** (TX-03; the lens model and its figures are [TH-06 section 5](TH-06-converging-lens.md#5-acceptance), which owns them). The Converging lens is a Relay whose inlet is a receiver aperture on its slab and whose one outlet is a collimated ray from the slab centre along the lens axis, τ = 0.95, range 3.0 m. A Flashlight (90 W, 15°) 1.5 m in front of it puts its whole 90 W share into the slab, so the ray carries 0.95 × 90 = 85.5 W. A collimated ray delivers its whole power to the aperture it ends on (optical conventions 3 and 8): with the pad stood on edge, its wafer facing the lens and its receiver centre on the axis within 3.0 m of the slab, the receiver reads 85.5 W, the wafer's record books it once, and the fibre is credited 0.9 × 85.5 = 77 W. With a time constant of 0.00425 ÷ 0.02 = 0.21 s it reaches 450 K after −0.21 × ln(1 − 162 ÷ 3850) = 0.01 s. A ray that ends anywhere on the wafer heats the fibre the same, because heat comes from the wafer's record and not from the reading; off the 0.03 m aperture the reading is 0 but the fibre still ignites. "Defocused" is the ray missing the wafer: the pad moved more than 0.18 m off the axis (past the tray's half-width, so the ray ends on the Wall behind it) or beyond the 3.0 m reach. The lens then gives the pad nothing. The 0.5° direction tolerance of coordinator decision 21 moves the ray by 2.0 × tan 0.5° = 0.017 m at 2.0 m, inside the aperture.
- **Without a lens.** At P6's place, 1.5 + 2.0 = 3.5 m from the Flashlight, the bare cone gives 420.4 ÷ 3.5² = 34.3 W/m²: a diverging segment books only through receivers, so the wafer's record gets the receiver's share, 34.3 × 0.00283 = 0.097 W, and 0.087 W moves to the fibre, a rise of 0.087 ÷ 0.02 = 4.4 K. The pad stays near 292 K and never ignites.
- **Lighting the next fuel.** A burning pad lying on a Fire bowl's bed lights the bed in about 11 s ([TH-02](TH-02-fire-bowl.md)); a Wood block held 0.06 m above a burning pad lights in about 14 s ([TH-03](TH-03-combustible-block.md)); both inside the 48.8 s burn.
- **The flame follows the pad (P5).** The region box is declared on the pad's body, so it moves with the pose. The delivered Conveyor has no drive of its own (`elements/Conveyor/ConveyorElementDeclaration.cs:35-37`, `:44`), so a Motor turns it: `speed` 0.075 rad/s (admitted 0 to 20, `elements/Motor/MotorElementDeclaration.cs:41`) × the default `surface_per_radian` 0.667 m/rad gives a belt of 0.05 m/s; the default 6 rad/s would give 4 m/s and carry the pad out of the match's box before it lights. A struck match lights a pad within 1 s ([TH-02](TH-02-fire-bowl.md) row B5), while the pad moves under 0.05 m. A TH-26 block is 0.6 × 0.4 m in plan ([TH-26](TH-26-thermal-storage-block.md) section 2), laid with 0.4 m along the belt on a Wall of the same plan; the pad's box is 0.36 m along the belt. The box leaves the first block's bounds when the pad has moved 0.2 + 0.18 = 0.38 m (7.6 s) and reaches the second, centred 1.0 m on, after 1.0 − 0.38 = 0.62 m (12.4 s), both well inside the 48.8 s burn. Each block's underside is 0.02 + 0.12 = 0.14 m above the wafer, inside the box (0.26 m above the wafer at its top). The match burns for 13.7 s ([TH-33](TH-33-spring-mounted-match.md) section 5), longer than the 7.6 s, so its own box (0.03 to 0.23 m above its guide's base) must stay below the first block: with the guide's base 0.11 m below the wafer's top the box tops out 0.12 m above the wafer, 0.02 m under the block, and grants the block nothing; the match's shaft travels 0.35 m from the pad's centre, outside the 0.6 m Walls (0.30 m each side). The Walls are static parts placed off the floor with nothing under them ([TH-01](TH-01-candle.md) section 5 records that this is admitted); they declare no node, so they take no share. While the pad is under a block that block gains about 0.06 × (488 − 288) = 12 W (covered ember, above), so a block that stops gaining or starts is unmistakable within 1 s.
- **Tick order.** Light absorbed in tick N comes from that tick's optical solve, which runs after the last substep and books it on the wafer's record ([combined tick order](EL-176-red-laser-emitter.md#combined-tick-order)); 0.9 × that record moves to `fibre` in the thermal step of tick N (T3, T8). The burn starts in the tick after the threshold is committed.
- Run length these rows need: 100 s (ENGINE-RUN-LENGTH, owner decisions 13 and 17; default 30 s, maximum 300 s). Row P1 burns out at 84.3 s, at most 97 s with the 15% time tolerance; every other row fits in 60 s.
- **Tolerances** (T12): energy 2% or 5 J; temperature 2 K or 5% of the rise; times 15%; stall guard the smaller of 3 × the hand time and the hand time + 60 s, at least 10 s, never past the Run length.

### Rows

Planned class `TinderPadTests`.

| # | Given / When / Then | Planned test | Control | Boundary | Tolerance |
| --- | --- | --- | --- | --- | --- |
| P1 | Given a pad laid wafer-down on a Heating plate wired to a Battery of 14 400 J, then it ignites between 30 and 41 s, its store pays 781 J and it goes out between 72 and 97 s | `ATinderPadOnAHotEnoughSurfaceIgnitesAndBurnsOut` | the same pad on the bench beside the plate never ignites and its store is whole | laid tray-down it lights later, between 66 and 89 s (77.5 s ± 15%), heated only through its region link | times 15%; 16 J; stall guard 100 s (the Run length) |
| P2 | Given P1 with a default Battery (the disc peaks near 436 K), then the pad never ignites | `InsufficientHeatingFails` | P1 | the two Batteries bracket the 450 K threshold | exact zero burn power |
| P3 | Given P1 after burn-out and the plate still hot, then the burn power stays 0 and the store stays 0 | `ReactedMaterialCannotBeReusedAsFreshFuel` | a fresh pad in the same place ignites | none | exact |
| P4 | Given P1, then fuel debit = heat credited to nodes and the ambient account | `FuelDebitEqualsHeatReleased` | P2: every term 0 | none | 16 J (2% of 781 J) |
| P5 | Given a pad lying centred on a delivered Conveyor (`width` 0.6 m, `length` 3 m) driven through `DriveIn` by a delivered Motor at `speed` 0.075 rad/s on a default Battery (belt about 0.05 m/s), lit at Run start by a Spring match beside the belt whose region box covers half the wafer (0.03 W/K, as in P9), its guide base 0.11 m below the wafer's top so that its 0.20 m tall box spans from 0.08 m below to 0.12 m above the wafer's top, under the first block, and two TH-26 blocks (420 J/K at 288 K), each resting on its own static Wall laid flat across the belt (`width` 0.6 m, `height` 0.4 m, `thickness` 0.12 m; its underside 0.02 m above the wafer), the first centred over the pad at Run start and the second centred 1.0 m further along the belt, then the first block stops gaining within 1 s of the pad's box leaving its bounds (hand 7.6 s) and the second starts gaining within 1 s of the box reaching its bounds (hand 12.4 s) | `TheFlameFollowsThePad` (a cross-part row owned by the TH-33 slice: the match is taught at 85, after TH-26 at 81 and the pad at 58) | the Motor left unwired, so the belt stays still: only the first block gains; the second gains under 5 J in 20 s | none | 1 s on each time; 5 J |
| P6 | Given a Flashlight, a Converging lens 1.5 m in front of it and the pad stood on edge against a Wall 2.0 m behind the lens, its wafer facing the lens and its receiver centre on the lens axis, then the receiver reads 85.5 W and the pad ignites within 1 s; with the pad moved 0.20 m off the axis (the ray passes the tray and ends on the Wall) it does not ignite and stays below 300 K | `OnlyFocusedLightLightsThePad` (a cross-part row owned by the TH-06 slice: the lens is taught at 59, after the pad at 58 and the Flashlight at 51) | no lens: it does not ignite and stays below 300 K (hand rise 4.4 K) | the pad on the axis 2.9 m behind the lens ignites; 3.1 m behind it (beyond the 3.0 m reach) it does not ignite and stays below 300 K | ± 2% power on the reading, 0.5° direction (coordinator decision 21's optical set); stall guard 10 s |
| P7 | Given P1 then Reset, then the store is full, the fibre is at 288 K and the pose is as authored | `ResetRestoresFuelTemperatureAndPose` | none | none | exact store; pose 0.001 m |
| P8 | Given a placed pad, when saved and loaded, then its pose is kept and a fresh Run starts unlit and full | `SaveAndLoadKeepThePose` | none | none | exact |
| P9 | Given a pad on the bench lit at Run start by a Spring match whose rail runs 0.35 m from the pad's centre (its region box covers half the wafer, 0.03 W/K), and a Thermal storage block (TH-26, 420 J/K) released 1.0 m above the pad that lands after about 0.45 s on two Walls laid flat either side of the pad, its underside 0.06 m above the wafer and covering the box, then the pad stays lit and pays its whole 781 J, including the 35 s after the match has gone out | `ACoveredEmberStaysLit` (a cross-part row owned by the TH-33 slice: the match is taught at 85, after TH-26 at 81 and the pad at 58) | without the Walls the block lands on the wafer and the pad goes out within 0.1 s with fuel left (smothered) | none | the fibre's committed temperature never below 420 K while fuel remains; 16 J |

No ports and no parameters, so no wrong-kind or parameter rows. The requirement's outcome is P2 and P6 ("defocused/insufficient heating fails") and P3 ("cannot be reused as fresh fuel"). A noncombustible body at the same focus warms without burning: that control is run with TH-26 in its slice.

## 6. Open questions

Resolved:

1. **Game scale.** D5; stated in TH-04.
2. **Burn law.** A fixed 16 W while lit (F1; decided now: one burn law for every fuel).
3. **Oxygen model.** D6; F3.
4. **Light-sensitive area.** The whole wafer (redecided 23 November 2026 under coordinator decision 26: absorbed light is booked once, on the absorbing collider's record, and heat is taken from that record, never from a reading, so a ray that ends anywhere on the wafer heats the fibre). The 0.03 m disc receiver stays as the observation row P6 asserts (85.5 W on the axis). The requirement's defocus control is the unfocused cone, P6's no-lens control (0.087 W into the fibre, a 4.4 K rise), with the beyond-reach boundary; the off-axis control moves the pad clear of the ray (0.20 m, past the tray's 0.18 m half-width). The earlier aperture-only credit was a second credit beside the record and is withdrawn.
5. **Wafer bulk.** No separate node (decided now: only the fibre's state matters to any row; one node is cheaper).
6. **Region conductance** (audit A2). 0.06 W/K, not the audit's 0.08 (decided now: at 0.08 a covered ember's committed temperature sits only about 13 K above extinction, and one more small loss path, such as a spent match's tip in the box, removes it; at 0.06 the margin is 53 K committed and it still lights a bed or a block in about 11 to 14 s).
7. **The 7200 J plate row.** P1 uses the 14 400 J Battery (decided now: on 7200 J the disc cools after 60 s and the pad goes out with about 100 J unburnt, so "its store pays 781 J" could not hold).

Needs the owner: none.

## 7. Integration

- **Works with.** TH-04 (delivered before it); TH-31, TH-33, TH-06 with the Flashlight (igniters); TH-02, TH-03 (next fuels); TH-26 and Wall for P9; Conveyor and Motor (delivered), TH-33, TH-26 and Wall for P5.
- **Campaign.** Introduction 58, practice 59; reuse 85, 149. Lesson fixtures need story [S8](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package), which lands after the engine programme (owner decision 22).
- **Coverage.** Section 10.

## 8. Delivery

- **Slice shape.** Single element.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which comes first: the programme (ENGINE-SPATIAL-QUERIES for the region query, ENGINE-CONTACT-OBSERVATIONS for the plate contact); ENGINE-GAS-NETWORK, ENGINE-LIQUID and ENGINE-THERMAL in the order of owner decision 18; ENGINE-IGNITION-BLAST as amended; ENGINE-RUN-LENGTH for P1; TH-04; S7. Row P6 runs in the TH-06 slice and rows P5 and P9 in the TH-33 slice (cross-part rows owned by those later slices).
- **Files.** `elements/TinderPad/TinderPad.csproj`, `TinderPadElementDeclaration.cs`, `tinder_pad.tscn`, `icon.svg`; one guarded line in `CuriousContraptions.slnx`; `CuriousContraptions.tests/TinderPadTests.cs`; a row in `VisibleSolidColliderTests`; this spec's binding section once S7 allows it. Registration is derived.
- **Review tier.** Data-only if every probe row is present and tested when the slice starts; otherwise the engine tier.
- **Disqualifiers.** A region link that cannot ride a dynamic body; a region link in which the feeding node's own face takes a share of the cap (then the requested T3 clause is settled first); a burn that cannot credit its own ignite node; absorbed light credited per collider and not per declared receiver aperture (then the wafer collider is split into a small centre collider and a surround).

## 9. Engine conformance (22 November 2026)

1. **Declaration data only.** Two colliders on one dynamic body, one node, one region link, one fuel store, one optical receiver, five bindings.
2. **Generic laws.** Threshold ignition and store-paid burn; Q̇ = G·ΔT by T9's exchange; a face's absorptance share of its collider's absorbed record as a heat credit (T3). "Lit by a lens" is absorbed watts exceeding the node's loss; nothing names a lens. "Smothered" is the contact law.
3. **Three layers.** Colliders: tray and wafer. Visuals: the same two solids; printed rings; an ember glow. Animation: ring colour and ember from committed fuel fraction and burn power.
4. **Flow and typing.** No ports; f32 values; channels are enums.
5. **Flat state.** The counts in section 2.
6. **Performance.** One small dynamic body that rests and may sleep once ENGINE-ISLANDS-SLEEP exists (no island sleeping exists today; the thermal step still runs for a burning node). One bounded region query per tick (T10).
7. **Tests are scenarios.** P1 to P9 run the production simulation.

**Verdict: Conforms; blocked on ENGINE-THERMAL and ENGINE-IGNITION-BLAST (general capabilities) and on the engine programme (owner decision 3a).**

## 10. Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of its spec that `tools/Coverage/NamedPackageSources.cs` reads (pattern: EL-239 Baseball). That section is not written here. **Blocked on the coverage follow-on story S7:** main's reader admits only an `EL-NNN` identity.

| Field | Planned value |
| --- | --- |
| Package | `tinder_pad` |
| Named identity | TH-32, `../invest/named-elements.md#thermal-32` |
| Acceptance | none resolvable today; the row's own task is `sequence-task-466` and the ledger entry has no todo link |
| Decision | S594; `scope-corrections.md` on published main has no `s594` block |
| Mode records | `Configuration=Fixed` |
