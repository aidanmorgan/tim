# EL-029 · Boat named-identity spec

This is the Story 7.0 full spec for named identity EL-029. The baseline is commit `a6c914e`; every citation uses `path@a6c914e:Lstart-Lend`. Values without a source are marked **proposed**, each with a one-line justification; the owner may revise them.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-029 |
| Name | Boat |
| Type | Water |
| Anchor | [requirements.md#element-029](../requirements.md#element-029); [named-elements.md#element-029](../invest/named-elements.md#element-029); scope index [todo-339](../requirements.md#todo-339) |
| Proof owner | S450 |
| CAT spec refined | none. Related: [CAT-003 Balloon](CAT-003-balloon.md) (gas buoyancy precedent, Story 12.1) |
| Related identities | [EL-028 Buoyant platform](EL-028-buoyant-platform.md), [EL-169 Cork float](EL-169-cork-float.md), [EL-170 Raft](EL-170-raft.md) (separate flotation identities); [EL-014 Water-carrying bucket](EL-014-water-carrying-bucket.md) and [EL-015 Leaky bucket](EL-015-leaky-bucket.md) (open container store and leak precedent); [EL-027 Canal lock chamber](EL-027-canal-lock-chamber.md) |
| Roadmap story | unscheduled |
| Status | not started (no `WorkshopPartKind` member, `engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`) |

## 2. Declaration

- **Bodies and shapes.** One dynamic open hull: outer 1.0 × 0.4 × 0.6 m, wall and bottom 0.04 m, as a compound of five Box colliders on one body (bottom, two sides, bow, stern). **Proposed**: the 0.92 × 0.52 m well holds one 0.68 m Basketball; the hull fits the 2.4 m lock.
- **Mass and material.** Hull 1.2 kg (**proposed**: empty draft 1.2 / (16 × 1.0 × 0.6) = 0.125 m; payload 16 × 0.24 − 1.2 = 2.64 kg before the rim reaches the surface). Inertia of the compound about its centre of mass. Material restitution 0.05, friction 0.5 (**proposed**: cargo stays in the well without gluing).
- **Liquid.** Family density ρw = 16 kg/m³ (**proposed**, shared with Batch F; see [EL-023](EL-023-archimedes-screw.md)). Buoyancy from the displaced volume of the closed hull envelope (0.24 m³ to the rim), at the centre of buoyancy. The hull well is also a finite store: liquid entering it (through the leak, over the rim, or poured in) adds mass at its own free surface without double counting ([component research](../../component-research.md#water) buoyancy row).
- **Buoyancy clamp.** Force regions clamp acceleration to ≤ 64 m/s² ([capability inventory](../../gpu-f32-physics.md#capability-inventory), Force Regions row). The largest buoyant acceleration is the empty hull pushed fully under: 16 × 0.24 × 9.81 / 1.2 = 31.4 m/s², under the clamp.
- **Constraints.** None. Motion comes only from contacts, ropes, gravity and buoyancy: "no authored route animation".
- **Typed ports.** `LeakInlet` (Water, Input, local (0, −0.18, 0), **proposed**): a non-joinable orifice in the bottom through which outside liquid enters the well, inactive when `leak_area` is 0; named in the water family's style (Batch F's EL-015 has the mirror `LeakOutlet`). No signal or power port.
- **Sensors and activation.** None. Pose and contained volume are observable.
- **Work and energy stores.** None. Leak inflow Q = Cd·A·√(2·g·d), d = outside surface height above the inside surface at the leak, Cd 0.6 (the water-family orifice coefficient **proposed** in Batch F's [EL-003](EL-003-tap.md)); it stops when inside and outside levels meet. Flooded hulls sink when hull mass + cargo + contained liquid exceeds ρw·V_hull.
- **Immersion damping.** Proposed family c = 1.0 1/s acts on the hull's own mass: F_drag = −m_hull·c·f_sub·v (EL-028); dry cargo adds inertia only. Empty: f_sub = 1.2/3.84, τ = 2·1.2/(1.2·f_sub) = 6.4 s. With a Basketball: f_sub = 2.2/3.84, τ = 2·2.2/(1.2·f_sub) = 6.4 s again. Small-bob amplitude falls below 5% after about 19 s in either case. These estimates assume cargo remains supported and the hull is not flooded; flooded liquid mass and actual submerged bodies participate in the full coupled model. Current linear-drag bound 0–0.125 1/s (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L69-L69`) requires the separate S420 immersion term.
- **Parameters.**

  | Name | Type | Range | Default | Unit | Source / justification |
  | --- | --- | --- | --- | --- | --- |
  | `leak_area` | f32 | 0–0.02 | 0 | m² | **proposed**: 0 is a sealed hull; at the 0.1 m draft head the leak passes about 0.0168 m³/s per 0.02 m², so 0.02 m² floods the 0.17 m³ well in about 10 s and 0.002 m² takes about 100 s, a slow leak for timing puzzles |

- **Cosmetic curves and UI bindings.** Hull follows the committed pose; inside waterline follows committed contained volume; a small cyan drip at the leak while flow is nonzero. No easing.
- **Art.** Cream hull `#fff8e9` with a gold gunwale `#f7cb52`, navy keel stripe `#293954`, cyan contained water `#66b8c9`; the leak shown as a navy plug ring when sealed ([DESIGN colour system](../../../DESIGN.md#colour-system)). **Proposed**.
- **Catalogue and inventory entry.** Id `boat`, title "Boat", category Water (**proposed**). Counted allowance (`engine/gpu/WorkshopInventory.cs@a6c914e:L9-L23`).

**Variants.** The requirements row lists no variants; EL-029 is one declaration. A leaking hull is the `leak_area` configuration of the same part.

## 3. Engine capabilities

Families ([element-map row](../general-engine-element-map.md), [coverage binding](../../coverage/engine/element-01.json) `element-029`): Buoyancy, ContactImpulse, EnvironmentState, FiniteLedger, FluidAdvection, GeometryQuery, JointConstraint, RigidBodyDynamics, StateTransaction (coverage only), TopologyTransaction.

**Exists now**
- Dynamic Box bodies, contact friction and impulses, gravity and linear drag: `engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L120`, `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L1271-L1279`.

**Missing**
- Compound collider set on one dynamic body (the Receiver's five boxes are static): unscheduled; also needed by EL-014.
- Displaced-volume buoyancy and immersion damping: [S416](../invest/decisions.md#s416) buoyancy → S420; unscheduled.
- Moving container store with leak port and mass coupling: S418; unscheduled.
- Drag toward a liquid current, if adopted: S418; unscheduled.

**Element dependencies.** A liquid store (EL-026, EL-027, EL-001); cargo (CAT-001 Basketball, CAT-014 Bowling ball as overload); movers such as CAT-039 Linear pusher or CAT-058 Rope anchor.

## 4. Sources and legacy

- **Requirement row** ([element-029](../requirements.md#element-029)): "Hull displaces liquid and transports its actual cargo." Outcome: "Leak or overload changes flotation; no authored route animation." No variants.
- **Decision** [S416 buoyancy](../invest/decisions.md#s416): lighter floats at a visible waterline, denser sinks, water level conserved.
- **Research** ([component research](../../component-research.md#water)): "Dynamic body + displacement"; recipe 12 "Canal Lift" lifts "a toy boat or platform".
- **Campaign**: first use 61–70; distinct hull/load modes retain separate rows; reuse 81–100, 126–140, 146–150 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)).

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | Legacy lift = buoyancy × world pressure × mass per dynamic root body | `reference/cpu/MachineWorld.cs@a6c914e:L875-L883` | do not carry forward | Mass-proportional gas model in a CPU loop; hulls need displaced volume (S420) |
| 2 | An external lift equal to weight holds a 2 kg body at rest without duplicating gravity | `CuriousContraptions.tests/PhysicsWrenchTests.cs@a6c914e:L33-L39` | carry forward (acceptance fact) | Buoyancy adds to gravity |
| 3 | Still-fluid linear and angular drag rates | `engine/physics/BodyDragLoad.cs@a6c914e:L6-L18` | do not carry forward | CPU class; no angular drag in the current contract |

**Files harvested:** `reference/cpu/MachineWorld.cs`, `CuriousContraptions.tests/PhysicsWrenchTests.cs`, `engine/physics/BodyDragLoad.cs`. Searched the Epic 7 deletion scope for "boat", "hull", "leak", "water": no boat-specific legacy.

## 5. Acceptance outline

Follow the [EL-029 row](../requirements.md#element-029).
- **Chrome UI recipe.** In free play place a Communicating tank with 0.5 m³, drop the Boat in with the real gizmo and place a Basketball in its well. Place a Linear pusher (CAT-039) at the bow height with a switch. Run, then press the switch.
- **Positive.** The boat floats at a visible waterline (0.125 m draft empty), sits lower with the ball, and moves only when pushed; the ball travels with it.
- **Negative/control.** Bowling ball cargo (4 kg > 2.64 kg): the rim submerges, the hull floods over it and sinks. `leak_area` 0.02 m²: water enters, the boat settles within seconds and sinks in about 10 s. With no pusher the boat does not travel.
- **Boundaries.** `leak_area` 0.002 m² (slow settling over about 100 s); cargo placed off-centre (heel and recovery); pouring water into the well from a nozzle (contained mass grows); boat grounded when the tank drains.
- **Run/Reset.** Hull and cargo poses and the contained and tank volumes restore exactly.
- **Save/Load.** Pose and `leak_area` round-trip; out-of-range values reject.
- **Integrations.** EL-027 Canal lock lift, recipe "Canal Lift".

## 6. Open questions

1. Liquid density ρw = 16 kg/m³ (proposed) is one water-family constant shared with Batch F's EL-001–EL-022 specs: owner decision under S420.
2. Whether a liquid current in a channel drags the boat (an advection velocity field) or boats move only by contact and rope: S418 decision.
3. Whether a leak can be opened or closed during Run (for example by contact) or is fixed by `leak_area`: owner decision (fixed proposed).
4. Immersion damping c = 1.0 1/s scaled by immersion (proposed family constant) and any angular term: owner decision under S420.
