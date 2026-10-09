# EL-027 · Canal lock chamber named-identity spec

This is the Story 7.0 full spec for named identity EL-027. The baseline is commit `a6c914e`; every citation uses `path@a6c914e:Lstart-Lend`. Values without a source are marked **proposed**, each with a one-line justification; the owner may revise them.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-027 |
| Name | Canal lock chamber |
| Type | Water |
| Anchor | [requirements.md#element-027](../requirements.md#element-027); [named-elements.md#element-027](../invest/named-elements.md#element-027); scope index [todo-339](../requirements.md#todo-339) |
| Proof owner | S448 |
| CAT spec refined | none. Related: [CAT-062 Spring](CAT-062-spring.md) is the first prismatic slider (Story 6.4) the gates reuse |
| Related identities | [EL-026 Communicating tank](EL-026-communicating-tank.md) (store and equalisation), EL-021 Sluice gate (the gate law), [EL-028 Buoyant platform](EL-028-buoyant-platform.md) and [EL-029 Boat](EL-029-boat.md) (the lifted cargo), EL-002 Header tank (upper supply) |
| Roadmap story | unscheduled |
| Status | not started (no `WorkshopPartKind` member, `engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`) |

## 2. Declaration

- **Bodies and shapes.** A static chamber (part root): interior 2.4 m long × 1.6 m high × 1.2 m wide, walls and floor 0.08 m, Box colliders. The upper end wall has a sill at 0.8 m above the chamber floor; the lower end wall has a sill at the floor. Each end carries one dynamic paddle gate: a Box 0.3 × 0.4 × 0.06 m sliding vertically in a slot over a 0.3 × 0.3 m culvert opening. All **proposed**: 2.4 m holds the default 1.2 m platform with clearance; a 0.8 m rise is visible at bench scale.
- **Mass and material.** Gates 0.5 kg each (**proposed**: light enough for a CAT-039 pusher or a rope over CAT-053 to lift). Chamber: the shared static water-vessel material, restitution 0.12, bounce threshold 0.1 m/s, friction 0.3, rolling resistance 0 (**proposed** reuse of the Receiver material, `engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L57-L63`, as Batch F's vessels use); gate friction 0.3 (**proposed**: the same matte material, so gates slide without sticking).
- **Liquid.** Chamber is a finite store, capacity 4.6 m³, i.e. 73.7 kg at the **proposed** family density ρw = 16 kg/m³ (shared with Batch F; see [EL-023](EL-023-archimedes-screw.md)). Overflow over the walls is conserved.
- **Constraints.** Two prismatic joints (gate ↔ chamber, local Y), each with travel 0 (closed) to 0.3 m (fully open), connected collision disabled (Story 6.4 slider).
- **Typed ports.**

  | Port | Domain | Direction | Local position (m) | Notes |
  | --- | --- | --- | --- | --- |
  | `WaterMouthA` | Water | Bidirectional | (−1.24, −0.65, 0) | Upper culvert; opening = upper gate lift / 0.3 |
  | `WaterMouthB` | Water | Bidirectional | (1.24, −0.65, 0) | Lower culvert; opening = lower gate lift / 0.3 |
  | `UpperHandle` | Mechanical (rope/contact) | Input | (−1.24, 1.0, 0) | Lift point on the upper gate |
  | `LowerHandle` | Mechanical (rope/contact) | Input | (1.24, 1.0, 0) | Lift point on the lower gate |

  **Proposed**; water mouths use the family's bidirectional `WaterMouthA`/`WaterMouthB` names (Batch F), A at the upper end and B at the lower. "Separately controlled openings" means each gate moves only by its own physical actuation; the map row lists no SignalPropagation or ElectricalPower, so the chamber has no signal or power input.
- **Sensors and activation.** None. Level and gate positions are observable by meters and EL-016–EL-018.
- **Work and energy stores.** None. Gate lift costs work against gate weight and friction; flow through each culvert follows the head difference across it, Q = Cd·A·opening·√(2·g·|ΔH|) toward the lower head (Cd 0.6, the water-family orifice coefficient **proposed** in Batch F's [EL-003](EL-003-tap.md)).
- **Parameters.**

  | Name | Type | Range | Default | Unit | Source / justification |
  | --- | --- | --- | --- | --- | --- |
  | `upper_initial_open` | f32 | 0–1 | 0 | fraction | **proposed**: authored gate start position |
  | `lower_initial_open` | f32 | 0–1 | 0 | fraction | **proposed**: same for the lower gate |
  | `initial_volume` | f32 | 0–capacity, step 1/16 | 1.0 | m³ | 1/16 m³ step sourced ([component research](../../component-research.md#water)); default **proposed** (lower-reach level, 16 kg) |

- **Cosmetic curves and UI bindings.** Waterline follows the committed volume; gates follow committed slider positions. No easing.
- **Art.** Cream masonry walls `#fff8e9` with chamfered copings, cyan water `#66b8c9` through a restrained side window, gold gate paddles `#f7cb52`, navy sill marks `#293954` at the upper and lower levels ([DESIGN colour system](../../../DESIGN.md#colour-system)). **Proposed**.
- **Catalogue and inventory entry.** Id `canal_lock`, title "Canal lock chamber", category Water (**proposed**). Counted allowance (`engine/gpu/WorkshopInventory.cs@a6c914e:L9-L23`).

**Variants.** The requirements row lists no variants; EL-027 is one declaration.

## 3. Engine capabilities

Families ([element-map row](../general-engine-element-map.md), [coverage binding](../../coverage/engine/element-01.json) `element-027`): EnvironmentState, FiniteLedger, FluidAdvection, GeometryQuery, JointConstraint, PressureWork, RigidBodyDynamics, StateTransaction (coverage only), TopologyTransaction.

**Exists now**
- Static and dynamic Box bodies, materials: `engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L120`.
- Counted inventory and save codec: `engine/gpu/WorkshopInventory.cs@a6c914e:L9-L23`, `engine/gpu/WorkshopSaveCodec.cs@a6c914e:L6-L10`.

**Missing**
- Prismatic slider with travel limits: Story 6.4.
- Liquid store, culvert advection (S418) and head relation (S419): owner [S416](../invest/decisions.md#s416); unscheduled.
- Buoyancy for the lifted cargo (S420): unscheduled.
- Rope or contact actuation of the gates: Story 10.2 (rope), Story 11.3 (pusher).

**Element dependencies.** An upper supply (EL-002 Header tank or EL-026 tank) and a lower receiver; a gate actuator (CAT-039 Linear pusher, CAT-058 Rope anchor with CAT-053 Pulley); cargo EL-028 or EL-029.

## 4. Sources and legacy

- **Requirement row** ([element-027](../requirements.md#element-027)): "Bounded chamber changes water level through separately controlled openings." Outcome: "Open ends cannot hold a raised level without a balancing supply." No variants.
- **Integration task** [sequence-task-390](../requirements.md#sequence-task-390): "sluice-controlled floating transport". Refinement [S708 sluice](../invest/refinements.md#s708): "Control floating transport with actual discharge."
- **Research** ([component research](../../component-research.md#water)): "Linked stores + sluice nodes + buoyant platform"; "sluices lift a floating platform in order". Recipe 12 "Canal Lift": fill/equalise/drain a lock with two sluices to lift a toy boat or platform.
- **Campaign**: first use 61–70; reuse 81–100, 126–140, 146–150 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)).
- **Legacy.** None found. Searched the Epic 7 deletion scope at `a6c914e` for "lock", "canal", "sluice", "water": no element source ("lock" matches only unrelated `locked` level fields).

**Files harvested:** none.

## 5. Acceptance outline

Follow the [EL-027 row](../requirements.md#element-027).
- **Chrome UI recipe.** In free play place a Header tank (EL-002) feeding a water pipe to `WaterMouthA`, a Catch basin under `WaterMouthB`, the lock chamber, a Buoyant platform (EL-028) with a ball on it inside the chamber, and a Linear pusher (CAT-039) under each gate handle with its own switch. Run and press the upper switch, later the lower.
- **Positive.** Upper gate open, lower closed: the chamber fills from the header to the upper level and the platform rises with its cargo. Closing the upper and opening the lower drains the chamber to the basin and lowers the platform.
- **Negative/control.** Both gates open: water passes through and the level cannot hold above the lower sill without a balancing supply. Neither gate moves when its actuator is unpowered or its rope is slack.
- **Boundaries.** Header exhausted mid-fill (level stops, no invented supply); chamber overfilled (overflow conserved); gate partly open (slower fill); a gate held shut against head stays shut.
- **Run/Reset.** Gate positions, chamber and reach volumes and cargo poses restore exactly.
- **Save/Load.** Initial gate openings, `initial_volume`, pose and links round-trip.
- **Integrations.** EL-028/EL-029 cargo, EL-021 Sluice gate law, recipe "Canal Lift".

## 6. Open questions

1. Whether each lock opening reuses the EL-021 Sluice gate declaration or owns a paddle gate: owner decision (paddle proposed).
2. Whether the gates take a supplied signal input despite the map row (no SignalPropagation listed): owner decision under S257.
3. Whether the upper and lower reaches are part of the chamber or separate tanks: owner decision (separate proposed).
4. Liquid density ρw = 16 kg/m³ (proposed) is one water-family constant shared with Batch F's EL-001–EL-022 specs: owner decision under S418/S420.
5. Shared static water-vessel material (restitution 0.12, friction 0.3, proposed): owner decision.
