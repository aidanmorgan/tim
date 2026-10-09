# EL-028 · Buoyant platform named-identity spec

This is the Story 7.0 full spec for named identity EL-028. The baseline is commit `a6c914e`; every citation uses `path@a6c914e:Lstart-Lend`. Values without a source are marked **proposed**, each with a one-line justification; the owner may revise them.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-028 |
| Name | Buoyant platform |
| Type | Water |
| Anchor | [requirements.md#element-028](../requirements.md#element-028); [named-elements.md#element-028](../invest/named-elements.md#element-028); scope index [todo-339](../requirements.md#todo-339) |
| Proof owner | S449 |
| CAT spec refined | none. Related: [CAT-003 Balloon](CAT-003-balloon.md) (gas buoyancy, Story 12.1) shares the Buoyancy family but not the displacement law |
| Related identities | [EL-029 Boat](EL-029-boat.md), [EL-169 Cork float](EL-169-cork-float.md), [EL-170 Raft](EL-170-raft.md) (other flotation identities, each separate); [EL-027 Canal lock chamber](EL-027-canal-lock-chamber.md) (lifts it); [EL-016 Float](EL-016-float.md) |
| Roadmap story | unscheduled |
| Status | not started (no `WorkshopPartKind` member, `engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`) |

## 2. Declaration

- **Bodies and shapes.** One dynamic pontoon deck: a Box 1.2 × 0.3 × 0.8 m (`RigidBodyDeclaration` + `ColliderDeclaration`, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L120`), with a 0.04 m raised rim on the top edges (art only). **Proposed**: 1.2 m carries a 0.68 m Basketball with margin and fits the 2.4 m lock chamber.
- **Mass and material.** Deck mass 2.0 kg, so density 6.9 kg/m³ and about 43% immersed at rest (draft 0.13 m) (**proposed**: visibly floating, with a 2.61 kg payload before the deck submerges). Box inertia from `engine/gpu/RigidMassProperties.cs@a6c914e:L17-L50`. Deck material restitution 0.05, friction 0.6 (**proposed**: the Domino's 0.6 friction keeps cargo from sliding, `engine/gpu/WorkshopDomino.cs@a6c914e:L9-L9`).
- **Liquid.** Family density ρw = 16 kg/m³ (**proposed**, shared with Batch F; see [EL-023](EL-023-archimedes-screw.md)). Buoyant force = ρw·g·V_displaced, applied at the centre of the displaced volume so tilting produces a righting moment; it adds to the declared gravity and never replaces it (fact 2 below).
- **Buoyancy clamp.** Force regions clamp acceleration to ≤ 64 m/s² ([capability inventory](../../gpu-f32-physics.md#capability-inventory), Force Regions row). The largest buoyant acceleration on the deck is when it is fully submerged and unloaded: ρw·V·g / m = 16 × 0.288 × 9.81 / 2.0 = 22.6 m/s², under the clamp, so the clamp never alters flotation.
- **Constraints.** None. The deck is free; cargo rests on it by contact only.
- **Typed ports.** None. The platform has no water, signal or power port; it interacts only through displacement and contact.
- **Sensors and activation.** None. Its committed pose is observable by goals and level sensors.
- **Work and energy stores.** None. Lift work comes only from the liquid's displacement; draining the store removes support.
- **Immersion damping.** Linear damping c = 1.0 1/s is applied to each immersed body's own mass: F_drag = −m_body·c·f_sub·v (**proposed** family law, aligned with EL-016). Dry supported cargo adds inertia, not liquid drag. For a level platform carrying cargo, the small-bob amplitude time constant is τ = 2·m_total/(m_deck·c·f_sub). Unloaded: f_sub = 2/4.608, τ = 4.608 s; with a Basketball: f_sub = 3/4.608, τ remains 4.608 s. Both fall below 5% amplitude after about 14 s. A fully submerged isolated body without surface restoring stiffness has velocity time constant 1/c = 1 s; an attached dry load increases it by m_total/m_body. The current linear-drag bound is 0–0.125 1/s (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L69-L69`); this is a new Buoyancy-family term, not a widened body-drag value.
- **Parameters.** None configurable (**proposed**: one fixed size keeps displacement limits teachable). Load capacity is derived: ρw·V − m = 16 × 0.288 − 2.0 = 2.61 kg.
- **Cosmetic curves and UI bindings.** The deck follows the committed body pose ("Pose ← committed body", [component research](../../component-research.md#water)); a navy waterline mark at the 2.61 kg load line. No easing.
- **Art.** Cream deck `#fff8e9` over cyan side floats `#66b8c9`, gold corner bollards `#f7cb52`, navy load-line mark `#293954` ([DESIGN colour system](../../../DESIGN.md#colour-system)). **Proposed**.
- **Catalogue and inventory entry.** Id `buoyant_platform`, title "Buoyant platform", category Water (**proposed**). Counted allowance (`engine/gpu/WorkshopInventory.cs@a6c914e:L9-L23`).

**Variants.** The requirements row lists no variants; EL-028 is one declaration. The campaign row "distinct hull/load modes retain separate rows" keeps cork, raft, boat and platform as separate identities.

## 3. Engine capabilities

Families ([element-map row](../general-engine-element-map.md), [coverage binding](../../coverage/engine/element-01.json) `element-028`): Buoyancy, ContactImpulse, EnvironmentState, FiniteLedger, FluidAdvection, GeometryQuery, JointConstraint, RigidBodyDynamics, StateTransaction (coverage only), TopologyTransaction.

**Exists now**
- Dynamic Box body, contact friction and impulses: `engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L120`, `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L1271-L1279` (gravity and linear drag).
- A force-region pattern evaluated on the worker (descending planar guide): `engine/gpu/PhysicsDeclarations.cs@a6c914e:L144-L161`, `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L1225-L1268`.

**Missing**
- Displaced-volume buoyancy against a liquid free surface: [S416 buoyancy](../invest/decisions.md#s416) → S420; unscheduled. (Story 12.1 builds gas buoyancy for the Balloon, which is a mass-proportional lift, not displacement.)
- Liquid stores whose free surface moves (S418): unscheduled.
- Immersion damping term: S420; unscheduled.

**Element dependencies.** A liquid store large enough to float it (EL-026 Communicating tank, EL-027 Canal lock chamber, EL-001 Finite reservoir); cargo (CAT-001 Basketball, CAT-014 Bowling ball as overload).

## 4. Sources and legacy

- **Requirement row** ([element-028](../requirements.md#element-028)): "Loaded floating deck carries physical cargo with displacement limits." Outcome: "Overload or lost water removes support." No variants.
- **Integration task** [sequence-task-390](../requirements.md#sequence-task-390): "sluice-controlled floating transport".
- **Decision** [S416 buoyancy](../invest/decisions.md#s416): "a lighter-than-water body floats at a visible waterline, a denser one sinks; the water level stays conserved."
- **Research** ([component research](../../component-research.md#water)): "Dynamic body + displacement", parameters "hull volume, load", "rising water delivers a ball onto a track". Recipe 8 "Quietly Rising": floating ball platform meets a track; a high-level float closes the tap before overflow.
- **Campaign**: first use 61–70; distinct hull/load modes retain separate rows; reuse 81–100, 126–140, 146–150 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)).

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | Legacy lift was buoyancy × world pressure × mass on every dynamic root body, each substep | `reference/cpu/MachineWorld.cs@a6c914e:L875-L883` | do not carry forward | Mass-proportional acceleration (gas model) and a CPU per-substep loop; liquid flotation must use displaced volume (S420) |
| 2 | An external lift equal to weight (19.62 N on 2 kg) holds a body exactly at rest without duplicating gravity | `CuriousContraptions.tests/PhysicsWrenchTests.cs@a6c914e:L33-L39` | carry forward (acceptance fact) | Buoyant force adds to declared gravity; neutral buoyancy gives no drift |
| 3 | Still-fluid drag declared as linear and angular rates | `engine/physics/BodyDragLoad.cs@a6c914e:L6-L18` | do not carry forward | CPU class; current contract has linear drag only; immersion damping is a new S420 term |
| 4 | Current `BallMaterial` carries a `Buoyancy` acceleration (0 for both balls) that is not compiled | `engine/gpu/WorkshopConstruction.cs@a6c914e:L41-L46` | do not carry forward for liquid | A per-kind acceleration cannot express displacement limits |

**Files harvested:** `reference/cpu/MachineWorld.cs`, `CuriousContraptions.tests/PhysicsWrenchTests.cs`, `engine/physics/BodyDragLoad.cs`. No platform-specific legacy exists in `parts/`, `content/puzzles.json`, `tools/Campaign` or `reference/` (searched for "platform", "float", "buoy", "water").

## 5. Acceptance outline

Follow the [EL-028 row](../requirements.md#element-028).
- **Chrome UI recipe.** In free play place a Communicating tank (EL-026) with `initial_volume` 0.5 m³, drop the Buoyant platform into it with the real placement gizmo, then place a Basketball on the deck. Run.
- **Positive.** The platform floats at a visible waterline (about 43% immersed), sinks slightly when the ball lands, bobs and settles (amplitude under 5% within about 14 s both loaded and unloaded), and carries the ball up as the tank is filled from a tap.
- **Negative/control.** Load a Bowling ball (4 kg > 2.61 kg capacity): the deck submerges and the cargo is no longer supported. Drain the tank: the platform settles onto the floor and stops rising. In an empty tank it simply rests.
- **Boundaries.** Off-centre load tilts the deck and recovers (righting moment); load near capacity floats with a few millimetres of freeboard; total liquid volume never changes because of the platform.
- **Run/Reset.** Platform and cargo poses and the store volume restore exactly.
- **Save/Load.** Pose round-trips; the part has no parameters.
- **Integrations.** EL-027 Canal lock lift, EL-016 Float stopping a tap, recipe "Quietly Rising".

## 6. Open questions

1. Liquid density ρw = 16 kg/m³ (proposed), which sets every flotation limit, is one water-family constant shared with Batch F's EL-001–EL-022 specs: owner decision under S420.
2. Immersion damping c = 1.0 1/s scaled by immersion (proposed family constant, also used by EL-016, EL-029, EL-169 and EL-170), any angular term, and the current no-angular-drag contract: owner decision under S420.
3. Whether the platform size is configurable (with derived capacity) or fixed: owner decision.
4. Whether moving liquid (a current) drags floating bodies, which needs an advection velocity field: S418 decision.
