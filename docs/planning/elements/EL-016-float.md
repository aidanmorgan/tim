# EL-016 · Float — named-identity spec

Story 7.0 named-identity spec ([readiness spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations `path@a6c914e:Lstart-Lend` resolve with `git show a6c914e:<path>` after the Epic 7 purge. A value marked **proposed** has no legacy or requirement source; it carries a one-line justification and the owner may revise it.

## Identity

| Field | Value |
| --- | --- |
| EL ID / name / type | EL-016 · Float · Water |
| Requirement anchor | [element-016](../requirements.md#element-016); source record [todo-337](../requirements.md#todo-337); integration task [sequence-task-387](../requirements.md#sequence-task-387) |
| Named entry | [element-016](../invest/named-elements.md#element-016); per-element proof owner S437 |
| CAT spec refined | None. The Balloon [CAT-003](../requirements.md#current-cat-003) floats in air (gas buoyancy, Story 12.1); this float displaces liquid. |
| Related identities | [EL-017 Mechanical float valve](EL-017-mechanical-float-valve.md) and [EL-018 level switch](EL-018-electronic-level-switch.md) (float-based controls), [EL-004](EL-004-catch-basin.md) (its water), EL-028 Buoyant platform, EL-169 Cork float (separate light-material identity) |
| Roadmap story | Unscheduled; ELEMENT-n row of the [roadmap](../invest/vertical-delivery.md#rolling-playable-roadmap), campaign chapter 7, levels 61–70. |
| Status | Not started. No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`). |

## Declaration

**Shared water values** (identical in every Water-1 spec, EL-001 to EL-022; each is one family constant shared with Batch G):
- Liquid density ρw = 16 kg/m³, one constant for the whole water family, aligned between Batch F (EL-001–EL-022) and Batch G (EL-023–EL-036, EL-165–EL-172) (**proposed**: catalogue bodies have mean densities of 2–44 kg/m³ — Domino 2.2, Basketball 6.1, Bowling ball 43.5; at 16 the instances this batch chose for the S416 buoyancy construction behave as it asks — the Basketball, lighter than water, floats and the Bowling ball, denser, sinks — and water loads stay comparable to ball masses).
- Volume step 2⁻⁴ m³ ([component research, water](../../component-research.md#water), line 26), which weighs 1 kg at ρw.
- Gravity 9.81 m/s² (`engine/gpu/WorkshopConstruction.cs@a6c914e:L123-L123`).
- Orifice discharge Q = Cd · s · A · √(2 g Δh), Cd = 0.6 (**proposed**: the standard sharp-edged orifice coefficient; one bounded law for every aperture; the law itself is owned by S416 advection → S418).
- Standard water mouth: bore radius 0.10 m, collar outer radius 0.16 m, collar length 0.06 m (**proposed**: visibly narrower than the 0.65 m ball-pipe bore, `engine/gpu/WorkshopPipe.cs@a6c914e:L6-L11`, so water and ball pipes are never confused).
- Static water-vessel contact material, one family constant: restitution 0.12, bounce threshold 0.1 m/s, friction 0.3, rolling resistance 0, reusing the Receiver's values (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L57-L63`) (**proposed** reuse: vessels are the same matte toy material as the Basket).
- Buoyancy of floating bodies: immersion damping c = 1.0 1/s, one family constant (**proposed**: aligned with Batch G; full law in [EL-016](EL-016-float.md)); every floating body's buoyant acceleration must stay under the 64 m/s² force-region clamp ([capability inventory](../../gpu-f32-physics.md#capability-inventory)).

**Element declaration:**
- **Bodies and shapes.** One dynamic sphere, radius 0.25 m (the Tennis ball's radius; **proposed**: a ball-scale float that fits the 0.40 m deep basin), volume 0.0654 m³. Sphere radius is inside the 1/16–16 m admission bound (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L120`).
- **Mass and material.** Mass 0.25 kg (**proposed**: density 3.8 kg/m³ floats with 24% of its volume submerged; maximum lift ρw · g · V = 10.3 N supports 0.80 kg of extra load, so a Bowling ball sinks it). Restitution 0.2, friction 0.4, bounce threshold 0.1 m/s, rolling resistance 0.03, drag 0.04 1/s (**proposed**: a soft matte float; drag and rolling resistance match the delivered balls, `engine/gpu/WorkshopConstruction.cs@a6c914e:L45-L53`). All inside the material and body bounds (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L82`).
- **Constraints.** None (free body).
- **Typed ports.** None. It responds to any liquid store whose free surface it crosses.
- **Sensors and activation.** None owned; EL-017/EL-018 sample float-like bodies.
- **Work and energy stores.** None. Buoyancy law for every floating body in the water family:
  - Upward force ρw · g · V_sub, where V_sub is the spherical-cap volume below the containing store's committed waterline ([component research](../../component-research.md#water), line 16). Displaced volume raises the store's waterline, so the level stays conserved (S416 buoyancy row).
  - Immersion damping: acceleration −c · f_sub · v with c = 1.0 1/s and f_sub = V_sub / V (the family constant above). It must live in the buoyancy force, because body linear drag is bounded to ≤ 0.125 1/s (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L82`). Bobbing at the 24% floating draught (f_sub ≈ 0.239): the waterplane area is about 0.171 m², so the surface stiffness is k = ρw · g · A_wp = 16 · 9.81 · 0.171 ≈ 26.8 N/m and ω = √(k / m) ≈ 10.4 rad/s, far above the damping rate c · f_sub ≈ 0.24 1/s. The bob is therefore lightly damped and its amplitude decays as e^(−c · f_sub · t / 2): time constant 2 / (c · f_sub) ≈ 8.3 s, below 5% after ln 20 × 8.3 ≈ 25 s. Fully submerged (f_sub = 1) and with no restoring surface stiffness, the relative velocity decays with time constant 1 / c = 1 s.
  - Clamp: the fully submerged buoyant acceleration ρw · g · V / m = 41 m/s² is under the 64 m/s² force-region clamp ([capability inventory](../../gpu-f32-physics.md#capability-inventory)), so the clamp never engages for this float; a float lighter than ρw · V · g / 64 = 0.16 kg would hit it.
- **Parameters.** None (**proposed**: material fixed by kind, as `BallMaterial.Validate` fixes ball materials, `engine/gpu/WorkshopConstruction.cs@a6c914e:L54-L62`).
- **Cosmetic curves and UI bindings.** Float pose ← committed body ([component research](../../component-research.md#water), line 33). A painted waterline band at the 24% draught (**proposed**: shows the unloaded float line).
- **Art.** Switch coral `#f06e54` lower half and cream `#fff8e9` upper half (`DESIGN.md@a6c914e:L173-L173`, `DESIGN.md@a6c914e:L151-L151`) (**proposed**: a classic two-tone float, readable against cyan water).
- **Catalogue and inventory entry.** Id `float`, title "Float", category "Water", kind `Float` (all **proposed**).

**Variants.** The requirements row lists no variants. Float valve and level switch are separate identities EL-017 and EL-018; cork float is EL-169.

## Engine capabilities

Families ([element map](../general-engine-element-map.md); binding [`element-01.json`](../../coverage/engine/element-01.json), consumer `element/element-016`): Buoyancy, ContactImpulse, EnvironmentState, FiniteLedger, FluidAdvection, GeometryQuery, JointConstraint, RigidBodyDynamics, StateTransaction, TopologyTransaction.

- **Exists now.** Dynamic spheres, contact impulse, friction, rolling resistance and drag (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L32-L42`); `BallMaterial.Buoyancy` is carried but not consumed (`engine/gpu/WorkshopConstruction.cs@a6c914e:L41-L44`).
- **Missing.** Displacement Buoyancy: decision [S416](../invest/decisions.md#s416) buoyancy → S420 ("a lighter-than-water body floats at a visible waterline, a denser one sinks; the water level stays conserved"). Liquid store and waterline (S418). Unscheduled; Story 12.1 builds gas buoyancy for CAT-003, which is a different law.
- **Element dependencies.** A liquid store with depth (EL-004 or EL-001); a load body (Domino, Bowling ball).

## Sources and legacy

- Requirements row: "Overloaded float sinks; an empty basin provides no buoyant lift." No variants.
- Refinement [S706](../invest/refinements.md#s706) float; recipes Every Drop Counts, Quietly Rising, Dark at High Tide (component research lines 52, 56, 59).
- **Legacy search.** No liquid float. Gas-era buoyancy:

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | Legacy buoyancy force = buoyancy coefficient × world pressure × body mass, applied every substep regardless of immersion. | `reference/cpu/MachineWorld.cs@a6c914e:L875-L883` | Do not carry forward: mass-proportional and immersion-independent, so an overloaded float could not sink and a dry basin would still lift (both contradict the requirement); also a CPU per-substep loop. |
| 2 | Balloon parameters mass 0.5, bounce 0.2, radius 0.36, buoyancy 11.5, drag 0.4. | `parts/catalog/balloon.tres@a6c914e:L14-L14` | Do not carry forward (CAT-003 gas buoyancy). Recorded to keep the two buoyancy laws apart. |

## Acceptance outline

Point of truth: [element-016](../requirements.md#element-016).

- **Chrome recipe.** Place a Catch basin and a Water tank with a Tap filling it; drop a Float into the basin with the move gizmo; in a second lane place a Float in an empty basin.
- **Positive.** As the basin fills the float rises with the waterline and rides at its 24% draught, settling within about 25 s of the water stopping.
- **Negative / control.** Empty basin: the float rests on the floor; no lift. Place a Bowling ball (4 kg) on a floating float: it sinks to the floor.
- **Boundaries.** A Domino (0.4 kg) laid across the float stays above water (inside the 0.80 kg reserve). The basin's waterline rises by the displaced volume when the float enters; total liquid unchanged. A float held fully under and released accelerates up at no more than 41 m/s² (below the clamp).
- **Run/Reset.** Pose and volumes restore exactly. **Save/Load.** Pose survives reload.
- **Integrations.** [sequence-task-387](../requirements.md#sequence-task-387): float variants selectable and separately verified; Every Drop Counts.

## Open questions

1. Buoyancy model is S420's decision; confirm displacement with immersion damping c = 1.0 1/s (a slower settle than a heavily damped float).
2. Float radius and mass (0.25 m, 0.25 kg): confirm.
3. Does buoyancy apply to every dynamic body (the Basketball floats too) as the generic rule requires? This spec assumes yes.
4. ρw = 16 kg/m³ is one water-family constant, aligned with Batch G (EL-023–EL-036, EL-165–EL-172); the owner confirms or replaces it once for the whole family (S416 buoyancy row → S420).
