# EL-017 · Mechanical float valve — named-identity spec

**Current delivery (24 October 2026):** [EL-017 delivery slice](../invest/named-elements.md#element-017). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes.

Brought to build-ready detail on 22 November 2026 against checkout `8d1d68aa`, and reworked the same day against the independent audit `docs/planning/elements/readiness/audits/elements-batch-f.md` (finding H-1) and the owner decisions of that date. The liquid conventions L1 to L18 and the capability probe rows W1 to W24 are in [EL-001](EL-001-finite-reservoir.md); the pipe-kit values are in [EL-008](EL-008-straight-water-pipe.md). Box sizes are full extents in metres.

## Identity

| Field | Value |
| --- | --- |
| Identity | EL-017 · Mechanical float valve · Water |
| Anchors | [element-017](../requirements.md#element-017), co-anchored `sequence-task-570`; source record [todo-337](../requirements.md#todo-337); integration [sequence-task-387](../requirements.md#sequence-task-387); ledger [element-017](../invest/named-elements.md#element-017), owner S438 |
| Disposition | To deliver as a single element: one catalogue part, a ballcock with every part visible. It needs no battery. EL-166 (mechanically actuated tap) is a different identity |
| Planned package | id `float_valve`, directory `elements/FloatValve/`, title "Float valve" |
| Status | Not started. Blocked on the engine programme (owner decision 3a), then `ENGINE-GAS-NETWORK` (the one fluid port record, coordinator decision 16) and `ENGINE-LIQUID` |
| Related | [EL-016](EL-016-float.md) (immersion law), [EL-003](EL-003-tap.md) (seat law), [EL-018](EL-018-electronic-level-switch.md) (supplied alternative), EL-166 |

## Declaration

Closest delivered shape: the Impact lever, a static base with one hinged dynamic body and angular limits (`IHingeElement`, `CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:20`).

- **Interfaces.** `ISceneGeometryElement`, `IHingeElement`, the liquid fitting and seat declaration of `ENGINE-LIQUID` (W4, W7, W14).
- **Body 0 (static housing).** A vertical tube: axis local Y, origin at its centre. Colliders: Tube, bore 0.10, outer radius 0.14, length 0.40; bracket box 0.08 × 0.08 × 0.08 at (0.18, −0.10, 0), on the tube's side.
- **Body 1 (dynamic arm and float, one rigid body).** Colliders: arm box 0.60 × 0.04 × 0.04 centred 0.30 m from the hinge; float sphere radius 0.20 centred 0.70 m from the hinge. Mass 0.25 kg (arm 0.10, float 0.15); centre of mass 0.54 m from the hinge; inertia about the hinge-parallel axis through the centre of mass 0.015 kg·m². Gravity (0, −9.81, 0). Float material as EL-016 (restitution 0.2, friction 0.4).
- **Hinge.** `RevoluteHingeDescriptor` between body 0 and body 1 at (0.18, −0.10, 0), axis local Z, limits **−45° to +5°** from horizontal (the arm points along +X at 0°), friction torque 0, no drive. Initial angle −34° (in the reference rig the float starts 0.60 − 0.70 × sin 34° − 0.20 = 0.009 m above the basin floor and settles onto it; with no vessel it falls to the −45° stop). The arm swings in the plane of the tube. The outlet stream falls straight down the tube's axis at x = 0 and the float is 0.48 m or more from it, so they do not meet; if they did, a sample ending on the float inside the vessel's interior would still be credited to the vessel (L8).
- **Liquid node.** One fitting with one seat between the mouth and the outlet. Seat area = s × A₀, with s = clamp((`shut_off_angle` − θ) / 15°, 0, 1), θ the committed hinge angle (L14 b). Fully open 15° or more below the shut-off angle; closed at and above it.
- **Mouth.** `Start` at (0, +0.20, 0), normal +Y, pipe bore, Intake. The supply arrives from above through an elbow.
- **Outlet (not a mouth).** Free end at (0, −0.20, 0), normal −Y. It discharges into whatever vessel the player places the valve over.

| Parameter | Type | Unit | Default | Min | Max | Editor policy |
| --- | --- | --- | --- | --- | --- | --- |
| `shut_off_angle` | Real f32 | `ParameterUnit.Degree` | −20 | −25 | −5 | Reject; build only |

The lower limit keeps the 15° opening band (down to −40°) inside the hinge's −45° stop.

- **Presentation.** `Visual/Arm` follows body 1's pose. `Visual/Stem` `LocalY` from the committed seat opening (W12, missing).
- **Palette.** `ElementCategory.Control`; palette order 408 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)); colour coral.

### Art and solidity

| Node | Mesh and size | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Housing` | Vertical tube 0.40 long, cream `#fff8e9`, navy flush top band | solid, hollow; the Tube collider | body 0 pose (static) |
| `Visual/Bracket` | Box 0.08 × 0.08 × 0.08, cream | solid; its box collider | body 0 pose |
| `Visual/Arm/Rod` | Box 0.60 × 0.04 × 0.04, gold `#f7cb52` | solid; the arm collider | body 1 pose |
| `Visual/Arm/Float` | Sphere radius 0.20, coral `#f06e54` and cream | solid; the sphere collider | body 1 pose |
| `Visual/Stem` | Gold mark in a slot on the housing's front, flush | decorative: printed flat, moved by a binding | presentation binding |
| mouth socket `Start`, outlet locator | Locators | none | body 0 pose |

Icon: an upright pipe stub with an arm and a ball. It differs from the Tap (a lever on a bonnet) by the long arm and float.

### Behaviour and numbers

- **What moves the arm.** Gravity, buoyancy on the float (L12 a) and contacts. The liquid in the seat exerts no torque (L12 c).
- **Floating draught.** Gravity torque about the hinge: 0.25 × 9.81 × 0.54 = 1.32 N·m. Buoyancy acts at 0.70 m, so it must be 1.89 N: V_sub = 1.89 / 156.96 = 0.0121 m³, 36% of the float's 0.0335 m³. That is a cap 0.162 m deep on the 0.20 m sphere, so the surface sits 0.038 m below the float's centre.
- **Level held.** The float's centre is 0.70·sin θ above the hinge, so the held surface is at hinge height + 0.70·sin(`shut_off_angle`) − 0.038 m.

| `shut_off_angle` | Held surface relative to the hinge | With the hinge 0.60 m above a vessel floor |
| --- | --- | --- |
| −25° | −0.334 m | depth 0.27 m |
| −20° (default) | −0.277 m | depth **0.32 m** |
| −15° | −0.219 m | depth 0.38 m |
| −5° | −0.099 m | depth 0.50 m (above a Catch basin's 0.40 m rim: it overflows first) |

- **Throttle.** The seat starts to close when the float's centre is 0.70·(sin(−20°) − sin(−35°)) = 0.16 m below its shut-off height, so inflow tapers and the level approaches the shut-off without overshoot.
- **At rest in the reference rig.** With the hinge 0.60 m above a Catch basin's floor and no liquid, the float (radius 0.20) rests on the floor with its centre 0.40 m below the hinge: θ = −arcsin(0.40 / 0.70) = −34.8°, and s = (−20 + 34.8)/15 = 0.99: open. The −45° stop is reached only when nothing is under the float.
- **Clearances in the reference rig.** The outlet is 0.10 m below the hinge, so it is 0.50 m above the floor, 0.10 m above the 0.40 m rim. The float's far edge is 0.18 + 0.70 + 0.20 = 1.08 m from the tube's axis at most; a Catch basin's 1.25 m interior, long axis along the arm, takes the tube 0.10 m inside one end wall.
- **Flow when open.** As a fully open Tap: three apertures from a tank, K₀/√3.
- **Immobilised linkage.** If anything holds the float down (a Wall block above it), θ cannot rise, s stays above zero and the vessel overflows however high the water goes.
- **No latch.** The opening is a function of the committed angle only, so a falling level reopens the seat.
- **Settling.** The arm follows the float; with the starting damping value it bobs for some seconds (L12 a). Rows assert the settled level, not a time.

## Engine capabilities

| Row (EL-001 probe) | Mark | Use here |
| --- | --- | --- |
| W1 static body; W2 Tube collider; W21 dynamic body | present | housing, arm and float |
| W19 revolute hinge with angular limits | present | the arm |
| W22, W23 authored state, refusals | present | `shut_off_angle` |
| W4 fluid port contract | present, untested | fitting; the one fluid port record is `ENGINE-GAS-NETWORK`'s (coordinator decision 16) |
| W3 mouth socket and a declared outlet | narrower | `ENGINE-CAPABILITY-INSTANCES` for the socket list; `ENGINE-LIQUID` for the compiled join |
| W7 fitting, seat and flow; W8 outlet stream | missing | `ENGINE-LIQUID` |
| W10 immersion on the float collider of a hinged body | missing | `ENGINE-LIQUID` |
| W14 seat opening from a committed revolute coordinate, with an offset from a parameter | missing | `ENGINE-LIQUID` (L14 b: a coordinate map with parameter terms) |
| W12 driver for a committed seat opening | missing | `ENGINE-LIQUID` |

Counts: 6 present, 1 untested, 1 narrower, 5 missing.

### Engine conformance (22 November 2026)

1. **Declaration data only.** Two bodies, four colliders, one hinge, one fitting with one seat, one mouth, one outlet, one parameter, two bindings. There is no valve state and no event: the opening is a declared function of a committed coordinate.
2. **Generic laws.** The float rises by L12 a; the hinge is the delivered revolute constraint; the seat is L7 with L14 b. "Immobilised linkage prevents closure" needs no rule: a held coordinate gives a held opening.
3. **Three layers.** *Colliders:* Tube and box on body 0; box and sphere on body 1. *Visuals:* arm and float follow body 1's pose. *Animation:* the stem mark follows the committed opening. No art was moved to suit an engine rule: the arm lies in the tube's plane.
4. **One-way flow and typing.** The liquid step reads the hinge angle committed by the previous substep (L16) and never writes to it.
5. **Flat-state counts.** Bodies 2 (1 static, 1 dynamic); colliders 4 (1 Tube, 2 boxes, 1 sphere); joints 1 revolute; drives 0; sensors 0; liquid nodes 1 fitting; seats 1; mouths 1; outlets 1; bindings 1 body + 1 liquid; parameters 1.
6. **Performance fitness.** Four colliders against the Impact lever's five (`elements/ImpactLever/impact_lever.tscn`). No island sleeping exists today; once `ENGINE-ISLANDS-SLEEP` lands the arm sleeps when the level is still and a level change wakes it.
7. **Tests are scenarios.** Rows V1 to V7.

## Sources and legacy

- Requirement: "Float linkage mechanically changes a real flow aperture"; outcome "Immobilised linkage prevents closure even at high water." No variants.
- Refinement [S706](../invest/refinements.md#s706) float-valve; component research [water](../../component-research.md#water) line 33 ("the mechanical valve needs no battery") and line 56 (Quietly Rising).
- **Legacy search.** No float valve, float or liquid valve at a6c914e. Hinge limits are CAT-034's harvest ([CAT-034](CAT-034-impact_lever.md)). **Files harvested:** none.

## Acceptance outline

Authority: [element-017](../requirements.md#element-017). Tolerances L17. Planned class `FloatValveTests`. **Reference rig:** a Water tank on a Wall shelf feeds a pipe and a 90° elbow down into the valve's `Start`; the valve stands over a Catch basin with its hinge 0.60 m above the basin's floor, the arm along the basin's long axis.

| # | Given / When / Then | Planned test | Control | Boundary |
| --- | --- | --- | --- | --- |
| V1 | Given the rig with the default `shut_off_angle`, when Run advances until inflow has been below 0.0005 m³/s for 5 s (hand estimate about 30 s: fill about 8 s, 1.25 m² × 0.32 m = 0.40 m³ at about 0.048 m³/s; throttle τ ≈ 3 s; arm settling τ ≈ 4 s; stall guard 90 s, three times that (L17); the level declares a Run length of 90 s, `ENGINE-RUN-LENGTH`, owner decisions 13 and 17), then the basin's depth is 0.32 m within 0.03 m, under its 0.40 m rim, the tank still holds liquid and `Spilled` is 0 | `FloatValveTests.TheFloatClosesTheSeatBelowTheRim` | V2 | — |
| V2 | Given a Wall block fixed 0.02 m above the float at its resting position (float on the basin floor), when Run advances until the tank is empty or the basin overflows (hand estimate about 10 s: the basin's 0.50 m³ less the held float's 0.034 m³ displacement at about 0.048 m³/s; stall guard 30 s, three times that, inside the default 30 s Run), then the seat never closes and the basin overflows | `FloatValveTests.AnImmobilisedArmNeverCloses` | V1 | — |
| V3 | Given `shut_off_angle` −25 and −15 in two lanes, when Run advances as V1 (stall guard 90 s; the level declares a Run length of 90 s, `ENGINE-RUN-LENGTH`), then the held depths are 0.27 m and 0.38 m, each within 0.03 m | `FloatValveTests.TheShutOffAngleSetsTheHeldLevel` | — | −25 and −5 admitted; outside or non-finite refused whole |
| V4 | Given the rig with the basin authored at `initial_volume` 0.5 (full, depth 0.40 m), when Run advances 20 s, then over the last 10 s the seat opening is 0 at every sampled tick and the tank loses less than 0.004 m³ in those 10 s; given the basin authored at 0.2 m³ (depth 0.16 m), the seat is open at Run start and, as V1 (stall guard 90 s; the level declares a Run length of 90 s, `ENGINE-RUN-LENGTH`), the basin fills to the V1 depth. Hand: at 0.40 m depth the float's centre floats 0.038 m above the surface, 0.162 m below the hinge, so the arm settles at −asin(0.162 / 0.70) = −13.4°, 0.70 × (sin 20° − sin 13.4°) = 0.077 m of float travel above the −20° shut-off. Released at −34° nearly submerged (0.229 m below that equilibrium), it rises past it and, at the starting damping (stiffness 156.96 × π × (0.20² − 0.038²) = 19 N/m; effective mass 0.0879 / 0.68² = 0.19 kg; ω ≈ 10 rad/s; damping 1.0 × 0.25 × 0.35 = 0.088 N·s/m; ζ ≈ 0.023), its first return swings fall below −20° and reopen the seat briefly; the swing falls under the 0.077 m margin after ln(0.229 / 0.077) / (0.023 × 10) ≈ 5 s, so the 10–20 s window is closed. At 0.16 m depth the float on the floor has a 0.151 m cap under water, 0.0107 m³ giving 1.68 N < the 1.89 N it needs, so it stays on the floor and s = 0.99 | `FloatValveTests.TheOpeningFollowsTheLevelBothWays` | a seat that ignored the angle, or a float without buoyancy (resting at −34.8°, s = 0.99), keeps the seat open in the window | — |
| V5 | Given an empty tank, then the float rests on the basin floor (θ near −35°), the seat is open and nothing flows | `FloatValveTests.NoSupplyNoFlow` | V1 | with no vessel under it the arm rests on its −45° stop |
| V6 | Given the valve with no battery or wire anywhere in the construction, then V1 holds (it declares no electrical or signal port, and a wire offered to it is refused) | `FloatValveTests.ItNeedsNoSupply` | — | — |
| V7 | Given a run, then Reset restores arm pose and volumes; save and load keep `shut_off_angle`, the pose and the join | `FloatValveTests.ResetAndSaveLoadKeepTheValve` | — | — |

Generic cover: W22, W23. Chrome recipe (pending): Header tank → pipe → elbow → Float valve placed over a basin; set the shut-off control.

## Open questions

All resolved; none needs the owner.

1. Water pressure pushing back on the arm: ignored (L12 c).
2. One catalogue part or player-assembled: one part (D8).
3. Arm, limits and float size: arm and float kept (D1); limits −45° to +5°, shut-off −25° to −5° with a 15° band, so that the valve shuts below a Catch basin's rim with its outlet above that rim (audit finding H-1).
4. Rim bracket: removed; a clamp would be a joint between two parts.
5. Housing orientation: vertical with a downward outlet. The stream then has no horizontal throw, the float and the stream fit one basin, and the arm needs no sideways offset.

## Integration

- **Works with.** Planned: EL-001, EL-002, EL-008, EL-010, EL-004. Delivered: Wall (shelf and the block in V2).
- **Campaign.** Chapter 7, levels 61–70 (Quietly Rising); reuse 71–90, 114, 126–130, 136–150. First taught at **63**, after the Float in the same level and before the actuated taps (64) (coordinator decision 36; [per-level allocation](readiness/campaign-coverage.md#6-per-level-allocation-and-progression-check-owner-decisions-24e-and-24f)). Needs the lesson-fixture story (S8).
- **Coverage binding (planned, not yet live).** A binding section in the form main's coverage reader reads is written by the slice only once S7 allows, as EL-001 describes; S7 also widens the reader to admit companion packages and non-EL identities (owner decision 15). **Blocked on the coverage follow-on story S7** (umbrella `todo-337`; `sequence-task-570` not co-anchored with a todo; no `s438` anchor).

## Delivery

- **Slice shape.** Single element.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), then: `ENGINE-GAS-NETWORK` (the one fluid port record); `ENGINE-LIQUID` including immersion and a coordinate-driven seat; EL-001; EL-008; the elbow family; EL-004; EL-016 (first proof of immersion); S7.
- **Files.** Adds `elements/FloatValve/FloatValve.csproj`, `FloatValveElementDeclaration.cs`, `float_valve.tscn`, `icon.svg`, `CuriousContraptions.tests/FloatValveTests.cs`; edits `CuriousContraptions.slnx` and this spec.
- **Review tier.** Data-only.
- **Disqualifiers.** A seat that cannot read a revolute coordinate; immersion that ignores a collider of a hinged body; an opening map that cannot take a parameter offset; a float that does not settle (then `ENGINE-LIQUID` retunes the damping value, not this part).
