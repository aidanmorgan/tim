# EL-016 · Float — named-identity spec

**Repair, 22 November 2026.** Repaired against the independent audit `docs/planning/elements/readiness/audits/elements-batch-f.md`, the conventions as restated in EL-001 and the owner decisions of that date. This part is blocked on the engine programme (owner decision 3a) before `ENGINE-LIQUID`.

**Current delivery (24 October 2026):** [EL-016 delivery slice](../invest/named-elements.md#element-016). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes.

Brought to build-ready detail on 22 November 2026 against checkout `8d1d68aa`. The liquid conventions L1 to L18 and the capability probe rows W1 to W24 are in [EL-001](EL-001-finite-reservoir.md) and are cited by number. Sizes are in metres.

## Identity

| Field | Value |
| --- | --- |
| Identity | EL-016 · Float · Water |
| Anchors | [element-016](../requirements.md#element-016), co-anchored `sequence-task-569`; source record [todo-337](../requirements.md#todo-337); integration [sequence-task-387](../requirements.md#sequence-task-387); ledger [element-016](../invest/named-elements.md#element-016), owner S437 |
| Disposition | To deliver as a single element. Its declaration is an ordinary light sphere and contains nothing liquid; its required behaviour is the general immersion law (L12 a), so it is the first acceptance of that law. Not a duplicate of a ball preset (different mass, material and outcome) nor of EL-169 Cork float (Batch G, another material) |
| Planned package | id `float`, directory `elements/Float/`, title "Float" |
| Status | Not started. Blocked on the engine programme (owner decision 3a), then `ENGINE-LIQUID` |
| Related | [EL-017](EL-017-mechanical-float-valve.md), [EL-018](EL-018-electronic-level-switch.md), [EL-004](EL-004-catch-basin.md) (its water), EL-028, EL-169. The Balloon ([CAT-003](../requirements.md#current-cat-003)) floats in air by a different, delivered law |

## Declaration

Closest delivered shape: the Tennis ball, a dynamic sphere with fixed material (`elements/TennisBall/TennisBallElementDeclaration.cs`).

- **Interfaces.** `ISceneGeometryElement`, `IMechanicalElement`. No liquid interface.
- **Body.** One dynamic body. `PayloadPolicy.None`: it is not a goal ball.
- **Collider (one).** Sphere, radius 0.25; volume 4/3·π·0.25³ = 0.06545 m³.
- **Mass and inertia.** 0.25 kg; solid-sphere inertia 2/5·m·r² = 0.00625 kg·m² on every axis. Mean density 0.25 / 0.06545 = 3.82 kg/m³. Gravity (0, −9.81, 0); linear drag 0.04 s⁻¹, as the delivered balls.
- **Material (f32).** Restitution 0.2, friction 0.4, rolling resistance 0.03, bounce threshold 0.1 m/s: a soft matte float.
- **Ports, stores, parameters.** None. It declares no buoyancy value of its own (the delivered balls' `buoyancy` parameter is a constant acceleration and is not used here, so nothing is counted twice).
- **Presentation.** `Visual` follows the body's pose. **Palette.** `ElementCategory.Motion`; palette order 146 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)); colour coral (240/255, 110/255, 84/255).

### Art and solidity

| Node | Mesh and size | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Ball` | Sphere radius 0.25, coral `#f06e54` lower half, cream `#fff8e9` upper half | solid; the sphere collider | body pose (dynamic) |
| `Visual/Line` | Navy `#293954` band at the unloaded waterline, 0.159 m above the lowest point, flush | decorative: printed flat | body pose |

Icon: a two-tone ball with a band. It differs from the Tennis ball (one colour with a seam) and the Basketball by the horizontal two-tone split and band.

### Behaviour and numbers

All of this is L12 a applied to a sphere; the engine has no float rule.

- **It floats.** In liquid of 16 kg/m³ it rides with 3.82 / 16 = 23.9% of its volume under water: V_sub = 0.01563 m³. For a sphere the cap of height h has volume π·h²·(3r − h)/3; h = **0.159 m** gives 0.01565 m³. So it needs at least 0.16 m of water to lift off the floor.
- **Reserve.** Fully under water the lift is 16 × 9.81 × 0.06545 = 10.27 N against its own 2.45 N: it carries up to (10.27 − 2.45) / 9.81 = **0.80 kg** more before it sinks. A Tennis ball (0.35 kg) rides on it; a Bowling ball (4 kg) sinks it.
- **An empty basin gives no lift.** With no liquid there is no submerged volume and no force.
- **Displacement.** In a Catch basin (1.25 m²) the level rises 0.01563 / 1.25 = 0.0125 m when it enters; the liquid total is unchanged (L4).
- **Bobbing.** At the floating draught the waterplane radius is √(0.159 × 0.341) = 0.233 m, area 0.170 m², so the restoring stiffness is 156.96 × 0.170 = 26.7 N/m and the bob frequency √(26.7 / 0.25) = 10.3 rad/s. Damping acts at c × 23.9% = 0.24 s⁻¹: amplitude decays with a time constant of 2 / 0.24 = 8.4 s and is under 5% after about 25 s. The damping value is a starting value (L12 a): `ENGINE-LIQUID` sets it for a damping ratio near 0.5, so that a float settles within a few seconds, because stable rest is an acceptance target.
- **Release from depth.** Held fully under and let go, it accelerates upward at (10.27 − 2.45) / 0.25 = 31 m/s², under the 64 m/s² force-region clamp of the [capability inventory](../../gpu-f32-physics.md#capability-inventory).
- **Every body obeys the same law.** A Basketball (6.1 kg/m³) floats with 38% under; a Bowling ball (43.5 kg/m³) sinks. That is the lesson D2 chose the density for.

## Engine capabilities

| Row (EL-001 probe) | Mark | Use here |
| --- | --- | --- |
| W21 dynamic body with declared mass and inertia | present | the sphere |
| W22 authored state | present | pose |
| W10 immersion: buoyancy and damping from displaced volume; displaced volume raises the level | missing | `ENGINE-LIQUID` ("a body less dense than the liquid floats at the displacement its mass requires and a denser body sinks") |

Counts: 2 present, 0 untested, 0 narrower, 1 missing; one owner. The package could be declared today; its acceptance cannot run before W10 and a store to float in (W6).

### Engine conformance (22 November 2026)

1. **Declaration data only.** One dynamic body, one sphere collider, one material. Nothing else.
2. **Generic laws.** Floating, sinking under load and "no lift from an empty basin" are L12 a with the declared mass and radius. No rule mentions a float.
3. **Three layers.** *Collider:* one sphere. *Visuals:* follow the body. *Animation:* none.
4. **One-way flow and typing.** Values are f32.
5. **Flat-state counts.** Bodies 1 (dynamic); colliders 1 sphere; joints 0; drives 0; sensors 0; liquid nodes 0; ports 0; bindings 1 body; parameters 0.
6. **Performance fitness.** One sphere, as the Tennis ball. Immersion is evaluated only for bodies whose bounds overlap a store's interior (one broadphase query per store, L18). No island sleeping exists today; a float at rest on still water will sleep once `ENGINE-ISLANDS-SLEEP` lands.
7. **Tests are scenarios.** Rows FL1 to FL7.

## Sources and legacy

- Requirement: "Buoyant body responds to displaced fluid and load"; outcome "Overloaded float sinks; an empty basin provides no buoyant lift." No variants.
- Refinement [S706](../invest/refinements.md#s706) float; component research [water](../../component-research.md#water) lines 16, 33, 52, 56 and 59.
- **Legacy search.** No liquid float. Gas-era buoyancy:

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | Legacy buoyancy = coefficient × world pressure × body mass, every substep, whatever the immersion. | `reference/cpu/MachineWorld.cs@a6c914e:L875-L883` | Not carried: mass-proportional and immersion-independent, so an overloaded float could not sink and a dry basin would still lift. |
| 2 | Balloon parameters (mass 0.5, radius 0.36, buoyancy 11.5, drag 0.4). | `parts/catalog/balloon.tres@a6c914e:L14-L14` | Not carried (CAT-003). Recorded to keep the two laws apart. |

**Files harvested:** `reference/cpu/MachineWorld.cs` (buoyancy lines only).

## Acceptance outline

Authority: [element-016](../requirements.md#element-016). Tolerances L17. Planned class `FloatTests`. Vessel: a Catch basin, filled from a Water tank and Tap or authored through the fixture.

| # | Given / When / Then | Planned test | Control | Boundary |
| --- | --- | --- | --- | --- |
| FL1 | Given a float in a basin authored at `initial_volume` 0.4 (depth 0.32 m), when Run advances 30 s, then it rests with its lowest point 0.159 m below the surface within 0.02 m and does not touch the floor | `FloatTests.TheFloatRidesAtItsDisplacement` | FL2 | — |
| FL2 | Given a float in an empty basin, when Run advances 5 s, then it rests on the floor | `FloatTests.AnEmptyBasinGivesNoLift` | FL1 | depth 0.10 m: it still rests on the floor |
| FL3 | Given a float in a Water tank holding 1.0 m³, inside a 0.60 m square well of four Wall blocks (thickness 0.12 m) standing on the tank floor and rising above the surface, and a Bowling ball lowered onto it, when Run advances 10 s, then both rest on the tank floor (combined 4.25 kg against 2.5 kg of lift) | `FloatTests.AnOverloadedFloatSinks` | a Tennis ball lowered onto it in the same well: the float's lowest point stays above the floor | — |
| FL4 | Given a basin filling under a tap with a float on its floor, then the float leaves the floor once the depth has passed 0.16 m and rises with the surface (stall guard 30 s); `Spilled` stays 0 although the stream may land on the float, because the float is inside the basin's interior (L8) | `FloatTests.TheFloatRisesWithTheWater` | tap closed: it stays down | — |
| FL5 | Given a float lowered into a basin of known volume, then the committed depth rises by 0.0125 m within 0.005 m and the basin's volume is unchanged | `FloatTests.DisplacementRaisesTheLevelNotTheVolume` | — | — |
| FL6 | Given a Bowling ball dropped into deep liquid beside a Basketball, then the Bowling ball rests on the floor and the Basketball floats | `FloatTests.DenserBodiesSinkAndLighterOnesFloat` | — | — |
| FL7 | Given a run, then Reset restores the pose; save and load keep it | `FloatTests.ResetAndSaveLoadKeepThePose` | — | — |

Generic cover: W22 (no parameters, no ports). Chrome recipe (pending): a basin filling from a tank and tap with a Float dropped in; a second lane with a Float in an empty basin.

## Open questions

All resolved; none needs the owner.

1. Buoyancy model: displacement with immersion damping c = 1.0 s⁻¹ (L12 a), one declared value of the liquid.
2. Radius and mass: 0.25 m and 0.25 kg (D1).
3. Does buoyancy apply to every body: yes; a law for one part would be element logic.

## Integration

- **Works with.** Planned: EL-004, EL-001, EL-003, EL-017, EL-018. Delivered bodies it meets: Tennis ball, Basketball, Bowling ball, Domino.
- **Campaign.** Chapter 7, levels 61–70 (Every Drop Counts); reuse 71–90, 114, 126–130, 136–150. First taught at **63**, with the float valve and level switch, before the actuated taps (64) (coordinator decision 36; [per-level allocation](readiness/campaign-coverage.md#6-per-level-allocation-and-progression-check-owner-decisions-24e-and-24f)). Needs the lesson-fixture story (S8).
- **Coverage binding (planned, not yet live).** A binding section in the form main's coverage reader reads is written by the slice only once S7 allows, as EL-001 describes; S7 also widens the reader to admit companion packages and non-EL identities (owner decision 15). **Blocked on the coverage follow-on story S7** (umbrella `todo-337`; `sequence-task-569` not co-anchored with a todo; no `s437` anchor).

## Delivery

- **Slice shape.** Single element.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), then: `ENGINE-LIQUID` including immersion; EL-004 (and EL-001, EL-003 for FL3 and FL4); S7.
- **Files.** Adds `elements/Float/Float.csproj`, `FloatElementDeclaration.cs`, `float.tscn`, `icon.svg`, `CuriousContraptions.tests/FloatTests.cs`; edits `CuriousContraptions.slnx` and this spec.
- **Review tier.** Data-only.
- **Disqualifiers.** Buoyancy that needs a per-body declared value; immersion evaluated for named part kinds only; displaced volume not added to the level.
