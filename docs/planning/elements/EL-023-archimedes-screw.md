# EL-023 · Archimedes screw named-identity spec

This is the Story 7.0 full spec for named identity EL-023. The baseline is commit `a6c914e`; every citation uses `path@a6c914e:Lstart-Lend`. Values without a source are marked **proposed**, each with a one-line justification; the owner may revise them.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-023 |
| Name | Archimedes screw |
| Type | Water |
| Anchor | [requirements.md#element-023](../requirements.md#element-023); [named-elements.md#element-023](../invest/named-elements.md#element-023); scope index [todo-338](../requirements.md#todo-338) |
| Proof owner | S444 |
| CAT spec refined | none. Related: [CAT-042 Motor](CAT-042-motor.md) is the drive source; [EL-022 Water pump](EL-022-water-pump.md) is the sibling lifter |
| Related identities | EL-001 Finite reservoir and EL-004 Catch basin (intake store), EL-002 Header tank, EL-007 Open gutter and [EL-014 Water-carrying bucket](EL-014-water-carrying-bucket.md) (discharge), the waterwheel ([todo-336](../requirements.md#todo-336)) as an alternative drive |
| Roadmap story | unscheduled |
| Status | not started (no `WorkshopPartKind` member, `engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`) |

## 2. Declaration

The element is declaration data over generic families; it has no solver, kernel branch or update loop of its own ([compilation model](../../gpu-f32-physics.md#compilation-model)).

- **Bodies and shapes.** A static housing (part root): an open-topped inclined trough of length 2.0 m and inner radius 0.25 m, built from Box colliders, inclined by `incline`. A dynamic rotor body: a helix on a shaft, radius 0.22 m, length 1.9 m, collider a Box of 0.44 × 0.44 × 1.9 m along the axis (cylinders are not an admitted `ColliderShapeKind`, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L7-L7`). An intake mouth (open, lower end, 0.3 m long submerged zone) and a discharge spout (upper end). All **proposed**: 2 m makes a 1 m lift at 30°, about the height of the Receiver walls (`engine/gpu/ReceiverGeometry.cs@a6c914e:L10-L17`).
- **Mass and material.** Rotor mass 2.0 kg, cylinder inertia m·r²/2 about the axis (**proposed**: between the 1 kg Basketball and 4 kg Bowling ball, so a default motor spins it up in under 1 s). Housing: the shared static water-vessel material, restitution 0.12, bounce threshold 0.1 m/s, friction 0.3, rolling resistance 0 (**proposed** reuse of the Receiver material, `engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L57-L63`, shared with Batch F's water specs).
- **Liquid.** Family density ρw = 16 kg/m³ (**proposed**, shared water-family constant with Batch F's [EL-001](EL-001-finite-reservoir.md)–EL-022 specs: a 2⁻⁴ m³ step weighs 1 kg; the 0.125 m³ EL-014 bucket holds 2 kg). Gravity 9.81 m/s² (`engine/gpu/WorkshopConstruction.cs@a6c914e:L123-L123`).
- **Constraints.** One revolute hinge rotor ↔ housing about the housing axis, free in both directions, connected collision disabled (built by Story 10.3, see [CAT-034](CAT-034-impact_lever.md)).
- **Typed ports.**

  | Port | Domain | Direction | Local position (m), incline 0 | Notes |
  | --- | --- | --- | --- | --- |
  | `DriveIn` | Mechanical | Input | (−0.95, 0, 0.35) | Shaft socket on the rotor, Story 11.1 domain |
  | `WaterInlet` | Water | Input | (−1.0, −0.25, 0) | Open intake; draws only from a store whose free surface covers it |
  | `WaterOutlet` | Water | Output | (+1.0, 0, 0) | Discharge spout; connects to a water mouth or emits free-stream packets |

  Positions are **proposed** (rotor ends); water port names follow the water family's vocabulary (`WaterInlet`/`WaterOutlet`, as in Batch F). `Mechanical` and `Water` domains do not exist yet (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L10`).
- **Sensors and activation.** None. The screw emits no signal; meters (EL-030–032) observe its flow.
- **Work and energy stores.** No store of its own. Each revolution in the lifting direction moves at most `volume_per_rev` from intake to discharge and charges the shaft reaction torque ρw·g·h·V_rev/2π (h = lift height): about 1.0 N·m at defaults (16 × 9.81 × 1.0 × 0.04 / 2π), well under the 20 N·m Motor torque ([CAT-042](CAT-042-motor.md)). In-transit liquid sits in the flights and is part of the finite ledger. Reverse rotation returns flight contents to the intake; it never delivers.
- **Parameters.**

  | Name | Type | Range | Default | Unit | Source / justification |
  | --- | --- | --- | --- | --- | --- |
  | `incline` | f32 | 10–45 | 30 | degrees | **proposed**: real screws run at 22–35°; above 45° flights spill |
  | `length` | f32 | 1.0–3.0, step 0.25 | 2.0 | m | **proposed**: bench is 16.8 m wide; 3 m caps lift at 2.1 m |
  | `volume_per_rev` | f32 | 0.01–0.08 | 0.04 | m³/rev | **proposed**: at the default 6 rad/s motor (0.95 rev/s) it lifts 0.038 m³/s, filling the 0.125 m³ EL-014 bucket in about 3.3 s |
  | `hand` | enum `ScrewHand { Right, Left }` | closed | Right | — | **proposed**: defines which shaft direction lifts, so "wrong rotation" is a declared fact |

- **Cosmetic curves and UI bindings.** The helix follows the committed hinge angle directly (no curve). The water in the flights follows the committed in-transit volume. No `CosmeticCurveDeclaration` is needed (`engine/gpu/WorkshopCosmetic.cs@a6c914e:L7-L19`).
- **Art.** Cream housing (`#fff8e9`), gold helix (`#f7cb52`), navy foot (`#293954`), and liquid in Clear-pipe cyan `#66b8c9` seen through a restrained window ([DESIGN colour system](../../../DESIGN.md#colour-system); [common visual contract](../requirements.md#individual-element-register)). The pictogram is a navy line helix in a tilted tube. All **proposed** within the approved palette.
- **Catalogue and inventory entry.** Id `archimedes_screw`, title "Archimedes screw", category Water (**proposed**: no water category exists among the legacy Control, Goals, Motion, Optics, Power, Ropes, Sound and Structure). Counted allowance through `WorkshopInventory` (`engine/gpu/WorkshopInventory.cs@a6c914e:L9-L23`).

**Variants.** The requirements row lists no variants; EL-023 is one declaration. The `hand` and `incline` parameters are configuration, not variants.

## 3. Engine capabilities

Families from the [element-map row](../general-engine-element-map.md) and [coverage binding](../../coverage/engine/element-01.json) (`element-023`, proof owner S444): ElectricalPower, EnvironmentState, FiniteLedger, FiniteWorkActuation, FluidAdvection, GeometryQuery, JointConstraint, PressureWork, RigidBodyDynamics, ShaftTorque, StateTransaction (coverage only), TopologyTransaction.

**Exists now**
- Static and dynamic bodies, Box colliders, contact materials: `engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L120`; box inertia: `engine/gpu/RigidMassProperties.cs@a6c914e:L17-L50`.
- Gravity integration on the worker: `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L1271-L1279`.
- Counted inventory and canonical save codec: `engine/gpu/WorkshopInventory.cs@a6c914e:L9-L23`, `engine/gpu/WorkshopSaveCodec.cs@a6c914e:L6-L10`.

**Missing**
- FluidAdvection, FiniteLedger for liquid, PressureWork: decision owner [S416](../invest/decisions.md#s416) (advection → S418, pressure-work → S419); unscheduled.
- Water connection domain and typed ports (TopologyTransaction): S416 with [S257](../invest/decisions.md#s257) port rules; unscheduled.
- Revolute hinge (JointConstraint): Story 10.3.
- Mechanical domain, shaft coupling and ShaftTorque reaction: Story 11.1.
- Cylinder collider and inertia for the rotor: Story 10.1 (ENGINE-CYLINDER); Motor is its first definite consumer in Story 11.1.
- ElectricalPower (motor supply upstream): Story 8.1.

**Element dependencies.** A drive source: CAT-042 Motor (Story 11.1) with CAT-005 Battery (Story 8.1), or the waterwheel. An intake store: EL-001 Finite reservoir or EL-004 Catch basin. A discharge receiver: EL-002 Header tank, EL-004 Catch basin, EL-007 Open gutter or EL-014 bucket.

## 4. Sources and legacy

- **Requirement row** ([element-023](../requirements.md#element-023)): "Rotating bounded screw lifts fluid through its real intake and discharge." Outcome: "Wrong rotation or a dry intake cannot deliver the target volume." No variants.
- **Named entry** ([element-023](../invest/named-elements.md#element-023)): owner S444; shared visual, generic-interaction, campaign and per-element proof contracts apply.
- **Integration task** [sequence-task-389](../requirements.md#sequence-task-389): each flow component creates its decision; "lift water with power"; no generic timed valve substitutes. Refinement [S707 pump](../invest/refinements.md#s707): "Convert finite supplied work to head."
- **Family contract** [profiles#fluids](../invest/profiles.md#fluids): empty supply emits nothing; reversed head does not flow uphill; Reset/save exact.
- **Research** `docs/component-research.md` water table row "Water pump / Archimedes screw": mechanical or electrical input → head transfer; parameters lift head and rate; "a dry intake is visibly idle"; binding "Screw/impeller ← committed rate" ([component research](../../component-research.md#water)). Recipe 6 "Up and Over": battery/motor → screw pump → elevated header tank → gravity gutter.
- **Campaign** ([campaign-element-coverage](../requirements.md#campaign-element-coverage)): first use 61–70, reuse 71–90, 114, 126–130, 136–150.
- **Decisions**: [S416](../invest/decisions.md#s416) advection and pressure-work rows; [S257](../invest/decisions.md#s257) mechanical-port; [S470](../invest/decisions.md#s470) is named in the map row as the liquid/gas boundary owner.
- **Legacy.** None found. Searched `parts/`, `parts/catalog/`, `engine/physics/`, `engine/*.cs`, `engine/bridge/`, `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/Campaign` and `reference/` at `a6c914e` for "archimedes", "screw", "water", "liquid", "fluid": no part, catalogue entry, level, lesson or test exists.

**Files harvested:** none.

## 5. Acceptance outline

Follow the [EL-023 row](../requirements.md#element-023) and the [common architecture/proof contract](../requirements.md#individual-element-register).
- **Chrome UI recipe.** In free play, drag a Finite reservoir (EL-001) to the bench, then the Archimedes screw with its intake inside the reservoir mouth, then a Header tank (EL-002) under the discharge, using the real placement and rotation gizmo. Place a Battery and Motor; wire `Supply` → `PowerIn` and Motor `Drive` → screw `DriveIn` with the contextual connection UI. Run.
- **Positive.** The reservoir waterline falls, liquid visibly climbs the flights and the header tank fills by exactly the volume the reservoir lost.
- **Negative/control.** Reverse the drive (Reverse transmission, Story 11.2) or set `hand` = Left: no liquid reaches the discharge. Start with an empty reservoir or the intake above the free surface: the rotor turns, nothing is delivered. Disconnect the motor: the rotor stays still.
- **Boundaries.** Intake partly covered (delivery falls as the surface drops below it); discharge store full (overflow is conserved, not deleted); `incline` at 10° and 45°; stalled motor; shaft reaction torque never exceeds the motor's work budget (no free energy).
- **Run/Reset.** Rotor angle and speed, in-flight volume and both store levels restore exactly.
- **Save/Load.** Pose, `incline`, `length`, `volume_per_rev`, `hand` and typed mechanical and water links round-trip; unknown enum values reject atomically.
- **Integrations.** Motor and Battery; waterwheel drive; EL-030 Flow meter on the discharge; recipe "Up and Over".

## 6. Open questions

1. Liquid density ρw = 16 kg/m³ (proposed) is one water-family constant shared with Batch F's EL-001–EL-022 specs: owner decision under S418/S420.
2. Whether the screw lifts through a discrete per-revolution transfer or a continuous rate-limited advection row: S418 decision.
3. Whether the intake may also draw from an open stream (free-stream packets) or only from a store: owner decision.
4. Whether a Left-hand screw is a parameter or should not exist: owner decision.
5. The Water catalogue category name and palette treatment of liquid (cyan `#66b8c9` with alpha proposed): owner decision.
6. Shared static water-vessel material (restitution 0.12, friction 0.3, proposed reuse of the Receiver's): owner decision.
