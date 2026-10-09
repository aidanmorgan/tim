# EL-169 · Cork float named-identity spec

This is the Story 7.0 full spec for named identity EL-169. The baseline is commit `a6c914e`; every citation uses `path@a6c914e:Lstart-Lend`. Values without a source are marked **proposed**, each with a one-line justification; the owner may revise them.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-169 |
| Name | Cork float |
| Type | Water |
| Anchor | [requirements.md#element-169](../requirements.md#element-169); [named-elements.md#element-169](../invest/named-elements.md#element-169); scope source [campaign-element-coverage](../requirements.md#campaign-element-coverage) |
| Proof owner | S462 |
| CAT spec refined | none. Related: [CAT-003 Balloon](CAT-003-balloon.md) (gas buoyancy, Story 12.1) |
| Related identities | [EL-016 Float](EL-016-float.md) (a level-sensing float, separate); [EL-028 Buoyant platform](EL-028-buoyant-platform.md), [EL-029 Boat](EL-029-boat.md), [EL-170 Raft](EL-170-raft.md) (other flotation identities; the raft is built from joined buoyant members) |
| Roadmap story | unscheduled |
| Status | not started (no `WorkshopPartKind` member, `engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`) |

## 2. Declaration

- **Bodies and shapes.** One dynamic Box body 0.4 × 0.25 × 0.4 m (0.04 m³) with rounded cork art (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L120`). **Proposed**: big enough to carry a Domino, small beside the 1.2 m platform.
- **Mass and material.** Mass 0.154 kg, density 3.84 kg/m³ = 0.24 × ρw, the real cork-to-water ratio (**proposed**). Box inertia from `engine/gpu/RigidMassProperties.cs@a6c914e:L17-L50`. Material restitution 0.3, friction 0.7 (**proposed**: cork is grippy and slightly springy).
- **Liquid.** Family density ρw = 16 kg/m³ (**proposed**, shared with Batch F; see [EL-023](EL-023-archimedes-screw.md)). Lift = ρw·g·V_displaced at the centre of buoyancy, adding to gravity. Maximum supported load = ρw·V − m = 16 × 0.04 − 0.154 = 0.486 kg: it carries a 0.4 kg Domino (`engine/gpu/WorkshopDomino.cs@a6c914e:L9-L9`) but not a 1 kg Basketball.
- **Buoyancy clamp.** Force regions clamp acceleration to ≤ 64 m/s² ([capability inventory](../../gpu-f32-physics.md#capability-inventory), Force Regions row). The largest buoyant acceleration is the unloaded cork held fully under: 16 × 0.04 × 9.81 / 0.154 = 40.8 m/s², under the clamp.
- **Constraints.** None. Other parts may be tied to it by rope (CAT-058) where rope attachments allow.
- **Typed ports.** None.
- **Sensors and activation.** None. Its committed pose is observable (for example by an aperture sensor or a goal).
- **Work and energy stores.** None. Lift exists only where liquid is displaced; on dry ground the cork is an ordinary light block.
- **Immersion damping.** Proposed family c = 1.0 1/s acts on the cork's own mass: F_drag = −0.154·c·f_sub·v (EL-028). Dry Domino cargo adds inertia only. Unloaded f_sub = 0.154/0.64; loaded f_sub = 0.554/0.64. The small-bob amplitude time constant τ = 2·m_total/(0.154·c·f_sub) is 8.31 s in both cases; settling below 5% takes about 25 s, including with the Domino. A fully submerged isolated cork without restoring surface stiffness has velocity time constant 1/c = 1 s; attached cargo changes the coupled inertia. This S420 term is separate from the current 0–0.125 1/s body-drag bound (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L69-L69`).
- **Parameters.** None (**proposed**: a fixed material body; size variety comes from the other flotation identities).
- **Cosmetic curves and UI bindings.** Follows the committed pose ("Pose ← committed body", [component research](../../component-research.md#water)). No easing.
- **Art.** Warm cork tone from the Ramp/Wall hue `#c28f52` with a cream band `#fff8e9` and navy grain dashes `#293954`, so it reads as a material, not a ball ([DESIGN colour system](../../../DESIGN.md#colour-system)). **Proposed**.
- **Catalogue and inventory entry.** Id `cork_float`, title "Cork float", category Water (**proposed**). Counted allowance (`engine/gpu/WorkshopInventory.cs@a6c914e:L9-L23`).

**Variants.** The requirements row lists no variants; EL-169 is one declaration.

## 3. Engine capabilities

Families ([element-map row](../general-engine-element-map.md), [coverage binding](../../coverage/engine/element-02.json) `element-169`): Buoyancy, ContactImpulse, EnvironmentState, FiniteLedger, FluidAdvection, GeometryQuery, JointConstraint, RigidBodyDynamics, StateTransaction (coverage only), TopologyTransaction.

**Exists now**
- Dynamic Box body, contact impulses, gravity and linear drag: `engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L120`, `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L1271-L1279`.

**Missing**
- Displaced-volume buoyancy and immersion damping: [S416 buoyancy](../invest/decisions.md#s416) → S420; unscheduled.
- Liquid stores with a free surface (S418): unscheduled.

**Element dependencies.** A liquid store (EL-001, EL-004, EL-026); light cargo (CAT-023 Domino); overload cargo (CAT-001 Basketball).

## 4. Sources and legacy

- **Requirement row** ([element-169](../requirements.md#element-169)): "Light material body displaces actual liquid under its measured load." Outcome: "Dry or overloaded cork cannot provide unlimited lift." No variants.
- **Decision** [S416 buoyancy](../invest/decisions.md#s416): "a lighter-than-water body floats at a visible waterline, a denser one sinks; the water level stays conserved."
- **Research** ([component research](../../component-research.md#water)): "Cork float / Raft / Buoyant platform / Boat — Dynamic body + displacement; hull volume, load; Flotation from displaced volume and load".
- **Campaign**: "cork/raft/boat/floating platform" first use 61–70, "distinct hull/load modes retain separate rows"; reuse 81–100, 126–140, 146–150 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)).

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | Legacy lift = buoyancy × world pressure × mass on dynamic root bodies | `reference/cpu/MachineWorld.cs@a6c914e:L875-L883` | do not carry forward | Mass-proportional gas model in a CPU loop; liquid flotation needs displaced volume (S420) |
| 2 | An external lift equal to weight holds a 2 kg body at rest without duplicating gravity | `CuriousContraptions.tests/PhysicsWrenchTests.cs@a6c914e:L33-L39` | carry forward (acceptance fact) | Buoyancy adds to declared gravity |

**Files harvested:** `reference/cpu/MachineWorld.cs`, `CuriousContraptions.tests/PhysicsWrenchTests.cs`. Searched the Epic 7 deletion scope for "cork", "float", "water": no cork-specific legacy.

## 5. Acceptance outline

Follow the [EL-169 row](../requirements.md#element-169).
- **Chrome UI recipe.** In free play place a Communicating tank with 0.25 m³, drop the cork float in with the real gizmo, and place a Domino lying flat on it. Run; then fill the tank further from a tap.
- **Positive.** The cork floats at a visible waterline, sits lower with the Domino and lifts it as the level rises.
- **Negative/control.** Basketball on the cork (1 kg > 0.486 kg): the cork submerges fully below the waterline (its top face goes under) and sinks to the floor or stays pinned under the ball. Cork in an empty tank: it rests on the floor and lifts nothing.
- **Boundaries.** Load at 0.45 kg floats with minimal freeboard; off-centre load tips the cork and sheds the cargo; tank volume is unchanged by the cork's presence.
- **Run/Reset.** Cork and cargo poses and the store volume restore exactly.
- **Save/Load.** Pose round-trips; no parameters.
- **Integrations.** EL-170 Raft (joined corks), rope tether (CAT-058), a float-raised ball reaching a track.

## 6. Open questions

1. Whether the Raft's members are Cork floats joined together or a separate member body: owner decision.
2. Immersion damping c = 1.0 1/s scaled by immersion (proposed family constant) and any angular term: owner decision under S420.
3. Liquid density ρw = 16 kg/m³ (proposed), which sets the 0.486 kg limit, is one water-family constant shared with Batch F's EL-001–EL-022 specs: owner decision under S420.
