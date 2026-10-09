# EL-017 · Mechanical float valve — named-identity spec

Story 7.0 named-identity spec ([readiness spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations `path@a6c914e:Lstart-Lend` resolve with `git show a6c914e:<path>` after the Epic 7 purge. A value marked **proposed** has no legacy or requirement source; it carries a one-line justification and the owner may revise it.

## Identity

| Field | Value |
| --- | --- |
| EL ID / name / type | EL-017 · Mechanical float valve · Water |
| Requirement anchor | [element-017](../requirements.md#element-017); source record [todo-337](../requirements.md#todo-337); integration task [sequence-task-387](../requirements.md#sequence-task-387) |
| Named entry | [element-017](../invest/named-elements.md#element-017); per-element proof owner S438 |
| CAT spec refined | None. Its pivot uses the hinge capability first built for the Impact lever ([CAT-034](CAT-034-impact_lever.md), Story 10.3). |
| Related identities | [EL-016 Float](EL-016-float.md) (buoyancy law), [EL-003 Tap](EL-003-tap.md) (aperture law), [EL-018](EL-018-electronic-level-switch.md) (supplied alternative), EL-166 Mechanically actuated tap |
| Roadmap story | Unscheduled; ELEMENT-n row of the [roadmap](../invest/vertical-delivery.md#rolling-playable-roadmap), campaign chapter 7, levels 61–70. Hinge prerequisite: Story 10.3. |
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
- **Bodies and shapes.** Three bodies (**proposed** layout: a classic ballcock, every part visible):
  1. Static valve body: inline housing 0.40 × 0.30 × 0.30 m box (full extents) with a rim bracket that clamps over a vessel wall.
  2. Dynamic arm: box 0.60 × 0.04 × 0.04 m (full extents), mass 0.10 kg (**proposed**: light enough that the float dominates).
  3. Dynamic float: sphere radius 0.20 m, mass 0.15 kg (**proposed**: maximum lift ρw · g · V = 5.3 N; net lift at half immersion ≈ 1.2 N, about 0.7 N·m at the arm end), rigidly fixed to the arm's far end.
- **Mass and material.** Arm and float as above; float uses EL-016's material (**proposed** there) and the family buoyancy law with c = 1.0 1/s. Clamp check: fully submerged buoyant acceleration on the float body alone is 5.3 N / 0.15 kg ≈ 35 m/s² (≈ 21 m/s² if arm and float are one 0.25 kg body), under the 64 m/s² force-region clamp. Housing static with the shared vessel material.
- **Constraints.** One hinge between the housing and the arm at the housing end, axis horizontal, limits −30° to +10° from horizontal (**proposed**: the float hangs open at −30° in an empty vessel and stops just above horizontal when full). Arm-to-float is a fixed joint (or the two are one compound body).
- **Typed ports.** `WaterInlet` and `WaterOutlet` standard mouths on the housing; the outlet discharges inside the vessel the bracket clamps. Domain `Water` (**proposed**). No electrical or activation port: "the mechanical valve needs no battery" ([component research](../../component-research.md#water), line 33).
- **Sensors and activation.** None: the aperture is a declared function of the committed hinge angle θ, not a sensor event. Opening s = 1 for θ ≤ −20°, s = 0 for θ ≥ 0°, linear between (**proposed**: a 20° travel band gives a visible throttle before shut-off).
- **Work and energy stores.** None. The valve has no hydraulic torque on the arm (**proposed** simplification; see Open questions), so only buoyancy, gravity and contacts move the linkage.
- **Parameters.**

  | Name | Type | Range | Default | Unit | Source |
  | --- | --- | --- | --- | --- | --- |
  | ShutOffAngle | f32 | −20° to +10°, step 5° | 0° | degrees | "High/Low thresholds" named by component research line 33; mapping to an arm angle **proposed** (sets the high-water level the valve holds) |

- **Cosmetic curves and UI bindings.** Float and arm ← committed bodies ([component research](../../component-research.md#water), line 33); a valve stem indicator ← committed opening (**proposed**).
- **Art.** Float coral `#f06e54` / cream (`DESIGN.md@a6c914e:L173-L173`), arm gold `#f7cb52` (`DESIGN.md@a6c914e:L154-L154`), housing cream `#fff8e9` (`DESIGN.md@a6c914e:L151-L151`) with navy collars `#293954` (`DESIGN.md@a6c914e:L147-L147`) (**proposed** reuse).
- **Catalogue and inventory entry.** Id `float_valve`, title "Float valve", category "Water", kind `FloatValve` (all **proposed**).

**Variants.** The requirements row lists no variants. The supplied electronic switch is EL-018.

## Engine capabilities

Families ([element map](../general-engine-element-map.md); binding [`element-01.json`](../../coverage/engine/element-01.json), consumer `element/element-017`): Buoyancy, ContactImpulse, EnvironmentState, FiniteLedger, FluidAdvection, GeometryQuery, JointConstraint, RigidBodyDynamics, StateTransaction, TopologyTransaction.

- **Exists now.** Dynamic spheres and boxes with contacts (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L32-L56`); Run/Reset (`engine/gpu/WorkshopSimulation.cs@a6c914e:L238-L245`).
- **Missing.** Hinge with limits (JointConstraint; Story 10.3 pivot, solver joints per the [capability inventory](../../gpu-f32-physics.md#capability-inventory)); displacement buoyancy ([S416](../invest/decisions.md#s416) → S420); a valve node whose opening is bound to a committed joint angle (S418); multi-body parts. Unscheduled.
- **Element dependencies.** A supply into `WaterInlet` (EL-001/EL-002 + pipes); a vessel to fill (EL-004); EL-016's buoyancy law.

## Sources and legacy

- Requirements row: "Immobilised linkage prevents closure even at high water." No variants.
- Refinement [S706](../invest/refinements.md#s706) float-valve; recipe Quietly Rising "high-level float closes the tap before overflow" (component research line 56).
- **Legacy search.** No float valve, float or liquid valve exists at a6c914e. Hinge-limit behaviour belongs to CAT-034's harvest ([CAT-034 spec](CAT-034-impact_lever.md)).

## Acceptance outline

Point of truth: [element-017](../requirements.md#element-017).

- **Chrome recipe.** Header tank → pipe → Float valve clamped on the rim of a Catch basin (move gizmo onto the rim; the bracket seats); set ShutOffAngle with the selected part's control.
- **Positive.** Run: the basin fills, the float rises, the arm reaches the shut-off angle and inflow stops below the rim; no overflow.
- **Negative / control.** Place a Wall block above the arm so it cannot rise (immobilised linkage): the water rises over the float, the valve stays open and the basin overflows onto the tray.
- **Boundaries.** ShutOffAngle −20° holds a lower level than +10°. Supply exhausted before shut-off: the level simply stops rising. Draining the basin (a Drain or tipped bucket) reopens the valve.
- **Run/Reset.** Arm, float and volumes restore exactly. **Save/Load.** ShutOffAngle, pose and joins survive reload.
- **Integrations.** [sequence-task-387](../requirements.md#sequence-task-387): mechanical valve variant selectable and separately verified.

## Open questions

1. Should water pressure on the seat push back on the arm (realistic, harder) or be ignored (proposed)?
2. Is the linkage one catalogue part (proposed) or assembled by the player from Float + lever + Tap (EL-166)?
3. Arm length, hinge limits and float size: confirm.
4. ρw = 16 kg/m³ is one water-family constant, aligned with Batch G (EL-023–EL-036, EL-165–EL-172); the owner confirms or replaces it once for the whole family (S416 buoyancy row → S420). The orifice law and Cd = 0.6 are the S416 advection decision (→ S418).
