# EL-005 · Liquid funnel — named-identity spec

Story 7.0 named-identity spec ([readiness spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations `path@a6c914e:Lstart-Lend` resolve with `git show a6c914e:<path>` after the Epic 7 purge. A value marked **proposed** has no legacy or requirement source; it carries a one-line justification and the owner may revise it.

## Identity

| Field | Value |
| --- | --- |
| EL ID / name / type | EL-005 · Liquid funnel · Water |
| Requirement anchor | [element-005](../requirements.md#element-005); source record [todo-334](../requirements.md#todo-334); integration task [sequence-task-384](../requirements.md#sequence-task-384) |
| Named entry | [element-005](../invest/named-elements.md#element-005); per-element proof owner S426 |
| CAT spec refined | None. The ball funnel [CAT-030](../requirements.md#current-cat-030) is a separate catalogue element (Story 6.11); the campaign table lists "water funnel" separately from the ball funnel ([campaign table](../requirements.md#campaign-element-coverage)). |
| Related identities | [EL-004](EL-004-catch-basin.md), [EL-008 pipe](EL-008-straight-water-pipe.md), [EL-012 cap](EL-012-water-pipe-cap.md), [EL-014 bucket](EL-014-water-carrying-bucket.md) (recipe "Catch the Escape") |
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
- **Bodies and shapes.** One static body: an inverted frustum, inlet radius 0.50 m, outlet radius 0.10 m (standard bore), height 0.60 m, shell 0.04 m, with a 0.06 m outlet collar (**proposed**: catches a stream within half a metre yet stays smaller than the 1.3 m ball-funnel inlet, `parts/FunnelPart.cs@a6c914e:L9-L10`). Contact geometry: the frustum shell needs a hollow cone collider, which does not exist (Sphere, Box, Plane only, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L120`); until then eight box staves approximate the shell (**proposed**).
- **Mass and material.** Static, zero mass; shared vessel material.
- **Constraints.** None.
- **Typed ports.** `OpenMouth` inlet disc (radius 0.50 m) and `WaterOutlet` standard mouth at the tip, facing down. An unjoined outlet discharges as a free stream; a joined outlet feeds the pipe. Domain `Water` (**proposed**).
- **Sensors and activation.** None.
- **Work and energy stores.** Finite liquid store equal to the frustum interior, (π · 0.6 / 3)(0.25 + 0.05 + 0.01) ≈ 0.195 m³ (about 3 steps, 3.1 kg), derived from the proposed geometry. Outflow follows the orifice law with Δh = level above the outlet; inflow beyond outflow raises the level until the rim spills.
- **Parameters.** None authored (**proposed**: a passive part; orientation by the rotate gizmo). Capacity is fixed by geometry.
- **Cosmetic curves and UI bindings.** Fill ← committed volume ([component research](../../component-research.md#water), line 30).
- **Art.** Cream shell `#fff8e9` (`DESIGN.md@a6c914e:L151-L151`) with gold rim band `#f7cb52` (`DESIGN.md@a6c914e:L154-L154`) and navy outlet collar `#293954` (`DESIGN.md@a6c914e:L147-L147`); water cyan `#66b8c9` alpha 0.16 (`DESIGN.md@a6c914e:L181-L181`) (**proposed** reuse; the water funnel is cream, not the clear-pipe teal of the ball funnel family).
- **Catalogue and inventory entry.** Id `liquid_funnel`, title "Water funnel", category "Water", kind `LiquidFunnel` (all **proposed**).

**Variants.** The requirements row lists no variants.

## Engine capabilities

Families ([element map](../general-engine-element-map.md); binding [`element-01.json`](../../coverage/engine/element-01.json), consumer `element/element-005`): EnvironmentState, FiniteLedger, FluidAdvection, GeometryQuery, JointConstraint, PressureWork, RigidBodyDynamics, StateTransaction, TopologyTransaction.

- **Exists now.** Static body and box colliders (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L120`); Run/Reset (`engine/gpu/WorkshopSimulation.cs@a6c914e:L238-L245`).
- **Missing.** Hollow frustum collider (CAT-030's Story 6.11 builds the ball-funnel frustum; reuse it when delivered). Packet capture across the inlet disc, store and orifice outflow ([S416](../invest/decisions.md#s416) advection → S418). Water mouth joins (EL-001 open question 3). Unscheduled.
- **Element dependencies.** A stream source (EL-003, EL-007, EL-013 or a tipping EL-014); EL-012 cap for the blocked control; EL-004 below the outlet.

## Sources and legacy

- Requirements row: "Blocked outlet fills and spills; no remote capture." No variants.
- Component research row EL-004–006 (finite store with capacity, line 30); recipe "Catch the Escape" (rope tilts a bucket into a movable funnel, line 62).
- **Legacy search.** Same scope and terms as EL-001: no liquid funnel. The ball funnel gives a geometric pattern only:

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | Ball funnel: half-length 0.9 m, inlet radius 1.3 m, shell 0.05 m; inlet mouth at −(0.9 + 0.09) m on the axis, outlet mouth at +(0.9 + 0.09) m with the ball-pipe bore; contact material (0.15, 0.1, 0.3). | `parts/FunnelPart.cs@a6c914e:L6-L17` | Do not carry forward the dimensions (CAT-030 owns them). Carry forward the pattern: a wide inlet mouth plus a standard-bore outlet mouth offset by its collar, so the funnel joins the pipe kit. |
| 2 | Mouth record: id, position at the outside face of the collar, outward normal, bore radius. | `engine/TubeMouth.cs@a6c914e:L6-L14` | Carry forward as the Water mouth declaration shape. Do not carry forward the Godot `Vector3`/`float` types. |

## Acceptance outline

Point of truth: [element-005](../requirements.md#element-005).

- **Chrome recipe.** Place a Water tank with a Tap; place a Water funnel under the spout with the move gizmo; snap a straight pipe to its outlet leading to a Catch basin.
- **Positive.** The stream enters the inlet disc, the funnel level stays low, and the basin receives the flow through the pipe.
- **Negative / control.** Cap (EL-012) on the funnel outlet: the funnel fills to its rim (≈ 0.195 m³) and then spills onto the tray; the basin stays empty. Funnel moved 0.6 m off the stream: nothing is captured (no remote capture).
- **Boundaries.** Inflow above the outlet's orifice rate raises the level and spills at the rim; a stream hitting the gold rim band is split by the inlet-disc sweep only.
- **Run/Reset.** Exact restoration. **Save/Load.** Pose and joins survive reload.
- **Integrations.** [sequence-task-384](../requirements.md#sequence-task-384) (source record [todo-334](../requirements.md#todo-334)): each collector has its own catalogue contract and proof inside the tank → tap/gutter → catch-basin route; the funnel's outlet joins the pipe kit of [sequence-task-385](../requirements.md#sequence-task-385) ([todo-335](../requirements.md#todo-335)). Generic interaction row [IX-08 Fluid advection](../requirements.md#interaction-08). Campaign first use 61–70 ("water funnel"), reuse 71–90, 114, 126–130, 136–150 ([campaign table](../requirements.md#campaign-element-coverage)).

## Open questions

1. Inlet radius 0.5 m versus matching the ball funnel's 1.3 m.
2. Should the water funnel also accept balls (shared contact shell), or is ball passage CAT-030 only?
3. Does a funnel outlet joined to a sealed pipe deliver head above its own rim (it cannot: the free surface is the funnel level) — confirm this teaching point.
4. ρw = 16 kg/m³ is one water-family constant, aligned with Batch G (EL-023–EL-036, EL-165–EL-172); the owner confirms or replaces it once for the whole family (S416 buoyancy row → S420). The orifice law and Cd = 0.6 are the S416 advection decision (→ S418).
