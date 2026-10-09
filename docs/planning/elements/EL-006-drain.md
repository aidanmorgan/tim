# EL-006 · Drain — named-identity spec

Story 7.0 named-identity spec ([readiness spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations `path@a6c914e:Lstart-Lend` resolve with `git show a6c914e:<path>` after the Epic 7 purge. A value marked **proposed** has no legacy or requirement source; it carries a one-line justification and the owner may revise it.

## Identity

| Field | Value |
| --- | --- |
| EL ID / name / type | EL-006 · Drain · Water |
| Requirement anchor | [element-006](../requirements.md#element-006); source record [todo-334](../requirements.md#todo-334); integration task [sequence-task-384](../requirements.md#sequence-task-384) |
| Named entry | [element-006](../invest/named-elements.md#element-006); per-element proof owner S427 |
| CAT spec refined | None. |
| Related identities | [EL-001](EL-001-finite-reservoir.md), [EL-004](EL-004-catch-basin.md), [EL-007](EL-007-open-gutter.md), pipe kit EL-008–EL-012, EL-031 Volume meter |
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
- **Bodies and shapes.** One static body: a low square grate box 1.0 × 0.08 × 1.0 m lying on the bench, with a recessed intake aperture 0.8 × 0.8 m (**proposed**: flat enough for balls to roll over, wide enough to receive a gutter end or a tipped bucket). Collider: one box for the frame rim; the grate surface is a box top 0.01 m below the rim.
- **Mass and material.** Static, zero mass; shared vessel material.
- **Constraints.** None.
- **Typed ports.** `OpenMouth` intake (the 0.8 × 0.8 m grate plane, facing up) and `WaterInlet` standard mouth on one side so a pipe can discharge directly into the sink. Domain `Water` (**proposed**).
- **Sensors and activation.** Drained-volume total is readable by the goal evaluator and by EL-031 through committed reads (`engine/gpu/WorkshopGoalEvaluator.cs@a6c914e:L5-L8`).
- **Work and energy stores.** An accounted sink: unbounded capacity, drained volume D recorded in the ledger term "sinks" ([component research](../../component-research.md#water), line 12). Liquid is never deleted. Intake rate is bounded by the grate's orifice law with Δh = local pooled depth over the grate, area 0.8 × 0.8 m × open-area fraction 0.5 (**proposed**: a grate is half bars); excess pools on the grate and then spills off its edge as conserved floor water.
- **Parameters.** None authored (**proposed**: a boundary element; position only).
- **Cosmetic curves and UI bindings.** Swirl ring in the grate ← committed intake rate; a small counter dial ← total drained volume D (**proposed**: makes "accounted" visible).
- **Art.** Navy grate `#293954` (`DESIGN.md@a6c914e:L147-L147`) in a cream frame `#fff8e9` (`DESIGN.md@a6c914e:L151-L151`), gold counter needle `#f7cb52` (`DESIGN.md@a6c914e:L154-L154`) (**proposed** reuse).
- **Catalogue and inventory entry.** Id `drain`, title "Drain", category "Water", kind `Drain` (all **proposed**).

**Variants.** The requirements row lists no variants.

## Engine capabilities

Families ([element map](../general-engine-element-map.md); binding [`element-01.json`](../../coverage/engine/element-01.json), consumer `element/element-006`): EnvironmentState, FiniteLedger, FluidAdvection, GeometryQuery, JointConstraint, PressureWork, RigidBodyDynamics, StateTransaction, TopologyTransaction.

- **Exists now.** Static box geometry (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L120`); committed goal reads (`engine/gpu/WorkshopGoalEvaluator.cs@a6c914e:L5-L8`); Run/Reset (`engine/gpu/WorkshopSimulation.cs@a6c914e:L238-L245`).
- **Missing.** Sink ledger term, packet capture across the grate plane, pooled-depth intake ([S416](../invest/decisions.md#s416) advection → S418; "drain under pressure/head", pressure-work → S419). Unscheduled.
- **Element dependencies.** A liquid source (EL-001 + EL-003, EL-007 end, EL-014 tip).

## Sources and legacy

- Requirements row: "Only liquid crossing its intake enters the tracked sink." No variants.
- Component research: "a drain is an accounted sink" (line 30); conservation identity with sinks (line 12); floor handling "visible drains" (line 20). Refinement [S704](../invest/refinements.md#s704) drain: "Drain under pressure/head and empty control." Family boundary [fluids](../invest/profiles.md#fluids).
- **Legacy search.** Same scope and terms as EL-001 plus "drain" and "sink": hits are unrelated (event-queue and allocation-site wording); no liquid sink exists at a6c914e.

## Acceptance outline

Point of truth: [element-006](../requirements.md#element-006).

- **Chrome recipe.** Place a Water tank, Tap and gutter whose low end overhangs a Drain placed with the move gizmo; a second lane overhangs the bench 1 m from the drain.
- **Positive.** Liquid falling on the grate enters the sink; the counter dial rises by what the tank lost, minus transit.
- **Negative / control.** The offset lane: water lands on the floor tray; the drain total stays 0 (only liquid crossing the intake counts). Empty tank: drain total stays 0.
- **Boundaries.** A stream heavier than the grate's intake rate pools and spills off the drain edge onto the tray, still conserved. A Basketball rolled over the drain passes over the grate (no capture of bodies).
- **Run/Reset.** Drain total returns to 0 exactly. **Save/Load.** Pose and joins survive reload.
- **Integrations.** [sequence-task-384](../requirements.md#sequence-task-384): visible drain loss.

## Open questions

1. Grate open-area fraction 0.5 and the resulting intake limit: is a rate-limited drain wanted, or should any liquid crossing the intake enter immediately?
2. Does drained water count as permanently lost for puzzle budgets ("use less water" remix), or may a level reuse it?
3. Should a drain also accept balls (a trapdoor-like sink), or only liquid?
4. ρw = 16 kg/m³ is one water-family constant, aligned with Batch G (EL-023–EL-036, EL-165–EL-172); the owner confirms or replaces it once for the whole family (S416 buoyancy row → S420). The orifice law and Cd = 0.6 are the S416 advection decision (→ S418).
