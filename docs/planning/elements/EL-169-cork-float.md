# EL-169 · Cork float — named-identity spec

**Current delivery (24 October 2026):** [EL-169 delivery slice](../invest/named-elements.md#element-169). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes.

Brought to build-ready detail on 22 November 2026 against checkout `8d1d68aa` and repaired the same day against the independent audit `docs/planning/elements/readiness/audits/elements-batch-g.md` and the owner decisions of 22 November 2026. Liquid conventions L1 to L18 and probe rows W1 to W24 are in [EL-001](EL-001-finite-reservoir.md), which has adopted Batch G's LG1 to LG8 into L3 to L15; LG numbers are kept as short names (LG1 = L5 and L6; LG2 = L3 and L7; LG3, LG4, LG5 = L4, L11, L12; LG6 = L15; LG7 = L12; LG8 = L13). The further additions adopted by coordinator decision 21 (nesting and envelope shielding, buoyancy reaction on a carrying body, contents at the liquid centroid, overlapping shapes counted once, porous and sealed interiors, a mouth below the floor datum, the relief mouth, the crest rule with granted work head, no crest lift limit) are now written into EL-001's rows (its "Batch G's additions" paragraph maps A1 to A9 to L3, L4, L5, L7, L8, L9 and L12; the long wording stays in [readiness/batch-g.md](readiness/batch-g.md) section 2) and are cited here as **L-additions A1 to A9**. Probe rows G1 to G8 are in section 3 of that file. All are cited by number and not restated.

**Repair (audit H-5, L-4, section 5).** A Tennis ball on the 0.30 m disc gave a negative metacentric height and rolled off a disc with no rim. The cork is now 0.40 m in radius and 0.15 m thick, and its measured load is a flat-based Weight.

## Identity

| Field | Value |
| --- | --- |
| Identity | EL-169 · Cork float · Water |
| Anchors | [element-169](../requirements.md#element-169), co-anchored `sequence-task-722`; source record [campaign coverage](../requirements.md#campaign-element-coverage); ledger [element-169](../invest/named-elements.md#element-169), owner S462 |
| Disposition | Member of the Family slice "Floating bodies" with [EL-028](EL-028-buoyant-platform.md). Not a duplicate of the Float ([EL-016](EL-016-float.md)): a flat disc with a different shape, mass and material, made to carry a load on its top |
| Planned package | id `cork_float`, directory `elements/CorkFloat/`, title "Cork float" |
| Status | Not started. Blocked on the engine programme (owner decision 3a), then `ENGINE-LIQUID` |
| Related | [EL-016](EL-016-float.md), [EL-028](EL-028-buoyant-platform.md), [EL-170](EL-170-raft.md), [EL-029](EL-029-boat.md), [EL-004](EL-004-catch-basin.md), Weight (delivered) |

## Declaration

Closest delivered shape: the Weight, a dynamic cylinder with declared mass and inertia (`elements/Weight/WeightElementDeclaration.cs`), without its tie.

- **Interfaces.** `ISceneGeometryElement`, `IMechanicalElement`. No liquid interface: immersion acts on any dynamic body (L12 a).
- **Body.** One dynamic body. `PayloadPolicy.None`. Origin at the disc centre; underside at y = −0.075, top at +0.075.
- **Collider (one).** Solid Cylinder, radius 0.40, length 0.15, axis Y. Volume π × 0.40² × 0.15 = 0.0754 m³; top area 0.503 m².
- **Mass and inertia.** 0.15 kg (2.0 kg/m³). ½·m·r² = 0.012 kg·m² about Y; m·(3r² + h²)/12 = 0.0063 kg·m² about X and Z. Centre of mass at the origin, 0.075 m above the underside. Gravity (0, −9.81, 0); linear drag 0.
- **Material (f32).** Restitution 0.3, friction 0.6, rolling resistance 0.03, bounce threshold 0.1 m/s: cork grips what stands on it.
- **Ports, stores, parameters.** None. No store, so no thermal node.
- **Presentation.** `Visual` follows the body's pose.
- **Palette.** `ElementCategory.Motion`; order 152 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)); colour ochre (194/255, 143/255, 82/255).

### Art and solidity

| Node | Mesh and size | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Cork` | Disc radius 0.40, height 0.15, ochre `#c28f52` with darker flecks printed flat | solid; the Cylinder collider | body pose (dynamic) |
| `Visual/Line` | Navy `#293954` band 0.019 m above the underside, flush | decorative: printed flat | body pose |

Icon: a flecked round disc with a waterline band. It differs from the Float (a two-tone ball), the Floating deck (a square railed slab) and the Weight (a tall cylinder with an eye) by its low round outline and flecks.

### Behaviour and numbers

All of this is L12 a applied to one cylinder; there is no cork rule. Water is 16 kg/m³, so the disc sinks 1 / (16 × 0.503) = 0.124 m per kilogram.

- **The load.** A delivered Weight sinks between about 0.5 and 4.9 kg and floats outside that range (linear size law, `elements/Weight/WeightElementDeclaration.cs:59-62`); a row that immerses one states its mass. Its base is flat: a 0.5 kg Weight is a cylinder of radius 0.04808 × 0.5 + 0.15933 = 0.183 m and height 2 × (0.03818 × 0.5 + 0.12653) = 0.291 m, centre of mass 0.146 m above its base. Standing on the cork it does not roll.
- **Draught.** Unloaded 0.15 / 8.04 = **0.019 m**. With the 0.5 kg Weight: 0.65 / 8.04 = **0.081 m**, leaving 0.069 m of freeboard.
- **Measured load.** The draught is the scale: each 0.1 kg of load sinks it 0.0124 m further.
- **Stability with the load.** KB = 0.040 m; BM = (π × 0.40⁴ / 4) / (0.65 / 16) = 0.0201 / 0.0406 = 0.495 m; KG = (0.15 × 0.075 + 0.5 × (0.15 + 0.146)) / 0.65 = 0.245 m; GM = 0.040 + 0.495 − 0.245 = **0.29 m**. With the submerged volume computed at each heel (the rim goes under at atan(0.069 / 0.40) = 9.8°): righting moment 0.07 N·m at 2°, 0.34 at 10°, 0.45 at 15° (the greatest), 0.26 at 30°, zero near 41°. The Weight slides at atan(√(0.6 × 0.3)) = 23° (geometric-mean friction of cork's 0.6 and the Weight's 0.3, `CuriousContraptions.Simulation/WorkshopConstraintSolver.cs:119`) and would tip at atan(0.183 / 0.146) = 51°, so it stays on while the heel is under 23°; the cork rights itself from any heel under about 41°. Unloaded GM = 0.0094 + 0.0201 / 0.0094 − 0.075 = 2.07 m: it floats flat.
- **Lift is bounded.** Fully under water it lifts 16 × 0.0754 = 1.21 kg: it carries at most **1.06 kg**. Before that limit it loses its stability: with a 1.0 kg Weight the freeboard is 0.007 m and the rim is under at 1°, so the righting moment is gone by 5° and it capsizes, shedding the Weight. A 1.5 kg Weight (1.65 kg in all) takes it under: the cork's 1.21 kg is short, and the Weight's own displacement (0.0619 m³, 0.99 kg) can only hold the pair with the cork submerged, so the cork's top goes below the surface whether or not the Weight then slides off. A Basketball (1.0 kg) on its flat top rolls off at the first tilt; the cork carries flat loads.
- **A dry cork gives no lift.** With no liquid there is no submerged volume: on the vessel floor it is a 0.15 kg disc.
- **Displacement (L4).** Unloaded it displaces 0.0094 m³: 0.0075 m of level in a Catch basin.
- **Bobbing.** Stiffness 156.96 × 0.503 = 78.9 N/m; unloaded frequency √(78.9 / 0.15) = 22.9 rad/s; immersion damping 1.0 × 0.0094 / 0.0754 = 0.12 s⁻¹ (L12 a). c = 1.0 s⁻¹ is a starting value that `ENGINE-LIQUID` sets for a damping ratio near 0.5 at a floating draught (L12 a).
- **Release from depth.** Held under and let go, the law gives (1.21 − 0.15) × 9.81 / 0.15 = 69 m/s² upward; the force-region acceleration clamp of 64 m/s² (`docs/gpu-f32-physics.md:153`, which lists fluid buoyancy) caps it. No row measures this rise.

## Engine capabilities

Map families (navigation only, D14): Buoyancy, ContactImpulse, EnvironmentState, FiniteLedger, FluidAdvection, GeometryQuery, JointConstraint, RigidBodyDynamics, StateTransaction, TopologyTransaction.

| Row | Mark | Use here |
| --- | --- | --- |
| W21 dynamic body with declared mass and inertia | present | the disc |
| W22 authored state | present | pose |
| W10 immersion: buoyancy and damping from displaced volume; displaced volume raises the level | missing | `ENGINE-LIQUID` |

Counts: 2 present, 0 untested, 0 narrower, 1 missing; one owner. The package could be declared today; its acceptance cannot run before W10 and a store to float in.

### Engine conformance (22 November 2026)

1. **Declaration data only.** One dynamic body, one Cylinder collider, one material.
2. **Generic laws.** Floating, the load-dependent draught, the lift limit and "dry gives no lift" are L12 a with the declared mass and shape; the load rides by contact and friction.
3. **Three layers.** *Collider:* one Cylinder. *Visuals:* follow the body. *Animation:* none.
4. **One-way flow and typing.** Values are f32.
5. **Flat-state counts.** Bodies 1 (dynamic); colliders 1 Cylinder; joints 0; drives 0; sensors 0; liquid nodes 0; thermal nodes 0; ports 0; bindings 0 (the art follows the body pose); parameters 0.
6. **Performance fitness.** One primitive collider, as the Weight. Immersion only for candidates `ENGINE-SPATIAL-QUERIES` returns for a store interior (L18). Whether a floating body may sleep is `ENGINE-ISLANDS-SLEEP`'s; nothing here assumes it.
7. **Tests are scenarios.** Rows O1 to O6.

## Sources and legacy

- Requirement: "Light material body displaces actual liquid under its measured load"; outcome "Dry or overloaded cork cannot provide unlimited lift." No variants.
- Campaign coverage row: "cork/raft/boat/floating platform … distinct hull/load modes retain separate rows". Component research [water](../../component-research.md#water): "Flotation from displaced volume and load".
- **Legacy.** None at `a6c914e` (searched for "cork", "float", "buoyan", "water"). **Files harvested:** none.

## Acceptance outline

Authority: [element-169](../requirements.md#element-169). Tolerances L17. Planned class `CorkFloatTests`. The vessel is a Catch basin with `initial_volume` 0.375 m³ (depth 0.30 m) unless stated.

| # | Given / When / Then | Planned test | Control | Boundary |
| --- | --- | --- | --- | --- |
| O1 | Given the cork placed on the water, when Run advances 20 s, then its underside's depth below the surface, averaged over the last 2 s (more than seven bobbing periods of 0.27 s), is 0.019 m within 0.01 m | `CorkFloatTests.TheCorkFloatsAtItsDraught` | O4 | — |
| O2 | Given a 0.5 kg Weight set upright on its centre, when Run advances 20 s, then the draught averaged over the last 2 s is 0.081 m within 0.01 m, the heel is under 5° and the Weight is still on the cork. Hand: the immersion damping acts on the cork only, 1.0 × 0.15 × 0.0406 / 0.0754 = 0.081 N·s/m; ω = √(78.9 / 0.65) = 11.0 rad/s; ζ = 0.081 / (2 × √(78.9 × 0.65)) = 0.0057. From the 0.062 m offset (0.081 − 0.019) the bob is 0.062 × e^(−0.0057 × 11.0 × 20) = 0.018 m at 20 s (0.033 m at 10 s, outside the old ±0.015 m); a 2 s mean of it is off by at most 2 × 0.018 / (11.0 × 2) = 0.002 m | `CorkFloatTests.TheDraughtMeasuresTheLoad` | O1 | a 0.25 kg Weight: averaged over the last 2 s of 20 s, 0.050 m within 0.01 m (ζ = 0.0044, ω = 14.0 rad/s; bob 0.031 × e^(−1.24) = 0.009 m at 20 s, mean off by under 0.001 m) |
| O3 | Given a 1.5 kg Weight set upright on its centre, when Run advances 10 s, then the cork's top has been below the surface | `CorkFloatTests.AnOverloadedCorkGoesUnder` | O2 | — |
| O4 | Given an empty basin, then the cork rests on the floor, loaded or not | `CorkFloatTests.ADryCorkGivesNoLift` | O1 | depth 0.01 m: still aground |
| O5 | Given O1, then the liquid total is unchanged and the committed level is not below what the basin's volume alone gives | `CorkFloatTests.DisplacementRaisesTheLevel` | — | — |
| O6 | Given a run, then Reset restores the authored pose and save and load keep it | covered by W22 | — | — |

Generic cover: W22. No port, no parameter. O5's expected rise (0.0075 m) is smaller than the depth tolerance; the row asserts conservation and that the rise is not negative, and the displacement law itself is proven by EL-028 row B6. O2's boundary: 0.40 / 8.04 = 0.050 m.

## Open questions

All resolved; none needs the owner.

1. A tie for hanging loads: none (D19); loads stand on the top.
2. Size or density parameters: none (D13).
3. Family with EL-016: see EL-028 question 3.
4. Cargo: a flat-based Weight, not a ball (decision under the standing delegation, audit H-5): a ball on a rimless disc rolls off at the first tilt.

## Integration

- **Works with.** Weight and delivered balls; EL-004, EL-006, EL-017 (not delivered).
- **Campaign.** Chapter 7, levels 61–70; reuse 81–100, 126–140, 146–150. Needs the lesson-fixture story (S8).

## Delivery

- **Slice shape.** Family slice "Floating bodies" with EL-028, after EL-016.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), in particular `ENGINE-SPATIAL-QUERIES` (immersion candidates, L18), `ENGINE-ISLANDS-SLEEP` (a change of level wakes what it loads; not delivered, and nothing here assumes sleeping exists) and `ENGINE-CAPABILITY-INSTANCES`; then `ENGINE-GAS-NETWORK`; then `ENGINE-LIQUID`; EL-004, EL-016; S7.
- **Files.** Adds `elements/CorkFloat/CorkFloat.csproj`, `CorkFloatElementDeclaration.cs`, `cork_float.tscn`, `icon.svg`, `CuriousContraptions.tests/CorkFloatTests.cs`; edits `CuriousContraptions.slnx` and this spec. Registration, bundle, palette and coverage membership are derived (`spec-registration-derivation.md`); lists S3 owns need an edit until S3 lands.
- **Review tier.** Data-only.
- **Disqualifiers the slice's probe must re-check.** Submerged volume not computed for a tilted Cylinder; buoyancy applied at the body centre instead of the centroid of the submerged part.

## Coverage binding (planned, not yet live)

Blocked on the coverage follow-on story S7. The named-identity binding section (the EL-239 form) cannot be written yet: the ledger entry links only the campaign coverage row; `sequence-task-722` is co-anchored with `element-169`, not with a todo; no `s462` anchor.
