# EL-021 · Sluice gate — named-identity spec

Story 7.0 named-identity spec ([readiness spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations `path@a6c914e:Lstart-Lend` resolve with `git show a6c914e:<path>` after the Epic 7 purge. A value marked **proposed** has no legacy or requirement source; it carries a one-line justification and the owner may revise it.

## Identity

| Field | Value |
| --- | --- |
| EL ID / name / type | EL-021 · Sluice gate · Water |
| Requirement anchor | [element-021](../requirements.md#element-021); source record [todo-338](../requirements.md#todo-338); integration tasks [sequence-task-389](../requirements.md#sequence-task-389) and [sequence-task-390](../requirements.md#sequence-task-390) |
| Named entry | [element-021](../invest/named-elements.md#element-021); per-element proof owner S442 |
| CAT spec refined | None. Its slider uses the prismatic joint first built for the Springboard ([CAT-062](../requirements.md#current-cat-062), Story 6.4); the legacy ball-tube Powered gate ([CAT-051](../requirements.md#current-cat-051)) is the nearest analogue. |
| Related identities | [EL-007 Open gutter](EL-007-open-gutter.md) (the channel it gates), [EL-003 Tap](EL-003-tap.md) (sealed aperture counterpart), EL-027 Canal lock chamber and EL-028 Buoyant platform (sluice-controlled transport), [CAT-058 rope](CAT-058-rope_anchor.md) and [CAT-039 pusher](CAT-039-linear_pusher.md) (actuators) |
| Roadmap story | Unscheduled; ELEMENT-n row of the [roadmap](../invest/vertical-delivery.md#rolling-playable-roadmap), campaign chapter 7, levels 61–70. Slider prerequisite: Story 6.4. |
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
- **Bodies and shapes.** Two bodies (**proposed** sizes fit the EL-007 channel: 0.30 m wide inside, 0.42 m outside, 0.15 m deep). All sizes are full extents.
  1. Static frame: two posts 0.06 × 0.60 × 0.15 m standing outside the gutter with a 0.44 m opening between their inner faces (0.01 m clearance to each 0.42 m gutter side), plus a top beam 0.56 × 0.06 × 0.15 m spanning the posts 0.60 m above the channel floor.
  2. Dynamic blade: box 0.29 × 0.22 × 0.04 m, mass 1.0 kg (**proposed**: 0.005 m clearance to each 0.30 m channel wall, so it slides inside the channel and closes on its floor; one Basketball's weight, so a Weight or pusher visibly does work to lift it and gravity closes it).
- **Mass and material.** Blade 1.0 kg, contact material restitution 0.1, bounce threshold 0.1 m/s, friction 0.3 (the legacy gate blade's material, fact 2; **proposed** reuse), drag 0. Frame static with the shared vessel material.
- **Constraints.** One prismatic slider, frame ↔ blade, vertical axis, travel 0–0.25 m above the channel floor (**proposed**: lift beyond the 0.15 m channel depth fully clears the flow); lower limit = closed on the floor. The blade is guided by the joint, not by contact with the posts; at full lift its top (0.47 m) stays below the beam (0.60 m).
- **Typed ports.** `RopeTie` on the blade top (Rope, bidirectional, attachment kind Load, as the legacy Weight, see [EL-014](EL-014-water-carrying-bucket.md)); the blade's top face is a contact surface for a pusher. No Water mouths: the gate acts on the open channel it spans. Water coupling via a declared channel-gate region aligned with the gutter cell under the blade (**proposed**).
- **Sensors and activation.** None.
- **Work and energy stores.** None. Aperture area = 0.30 m (channel width) × min(lift, upstream depth), where lift is the committed slider coordinate; the 2 × 0.005 m side clearances are collision clearance only and carry no modelled leakage (**proposed**: the requirement makes the closed gate retain upstream volume). Discharge under the gate follows the orifice law on the upstream–downstream free-surface difference. With lift = 0 no water passes; upstream inflow accumulates behind the blade and, above the 0.15 m channel walls (below the 0.22 m blade top), spills over the gutter sides as conserved free streams — never deleted.
- **Parameters.**

  | Name | Type | Range | Default | Unit | Source |
  | --- | --- | --- | --- | --- | --- |
  | InitialLift | f32 | 0–0.25, step 0.05 | 0 | m | opening named by component research line 34; values **proposed** (closed by default; gravity holds it unless lifted) |

- **Cosmetic curves and UI bindings.** Blade ← committed slider pose ([component research](../../component-research.md#water), line 34); a gold lift scale on one post every 0.05 m (**proposed**).
- **Art.** Ramp ochre frame `#c28f52` (`DESIGN.md@a6c914e:L169-L169`) matching the gutter, gold blade `#f7cb52` (`DESIGN.md@a6c914e:L154-L154`) — the legacy gate blade colour (fact 2) — with a navy top beam `#293954` (`DESIGN.md@a6c914e:L147-L147`) (**proposed**).
- **Catalogue and inventory entry.** Id `sluice_gate`, title "Sluice gate", category "Water", kind `SluiceGate` (all **proposed**).

**Variants.** The requirements row lists no variants.

## Engine capabilities

Families ([element map](../general-engine-element-map.md); binding [`element-01.json`](../../coverage/engine/element-01.json), consumer `element/element-021`): EnvironmentState, FiniteLedger, FluidAdvection, GeometryQuery, JointConstraint, PressureWork, RigidBodyDynamics, StateTransaction, TopologyTransaction.

- **Exists now.** Dynamic and static boxes (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L120`); Run/Reset (`engine/gpu/WorkshopSimulation.cs@a6c914e:L238-L245`).
- **Missing.** Prismatic slider with limits (JointConstraint, Story 6.4); a channel aperture bound to a committed joint coordinate ([S416](../invest/decisions.md#s416) advection → S418); rope tie (Story 10.2, S257 rope-port → S688). Unscheduled.
- **Element dependencies.** EL-007 gutter; an actuator (CAT-058 rope + CAT-067 Weight, or CAT-039 pusher); a source.

## Sources and legacy

- Requirements row: "Closed gate retains upstream volume without deleting inflow." No variants.
- Refinements [S707](../invest/refinements.md#s707) and [S708](../invest/refinements.md#s708) sluice "Control floating transport with actual discharge"; integration [sequence-task-390](../requirements.md#sequence-task-390) "sluice-controlled floating transport"; recipes The Mill's Song and Canal Lift (component research lines 58 and 60).
- **Legacy search.** No liquid gate. The ball-tube Powered gate is a moving-blade analogue:

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | Opening is read from the committed axial coordinate of a blade body on a guide joint; stroke 1.55 m; state classified from motion and power. | `parts/PoweredGatePart.cs@a6c914e:L10-L19` | Carry forward "aperture = committed slider coordinate". Do not carry forward the ball dimensions or the electric drive (this gate is lifted by rope or pusher). |
| 2 | Blade half-extents (0.06, 0.72, 0.72) m (full 0.12 × 1.44 × 1.44 m), gold `#f7cb52`; contact material (0.1, 0.1, 0.3). | `parts/PoweredGatePart.cs@a6c914e:L22-L23`; `parts/PoweredGatePart.cs@a6c914e:L46-L47` | Carry forward the colour and material; not the size. |
| 3 | Blade motion prepared by the part's own `PreparePhysics` every substep. | `parts/PoweredGatePart.cs@a6c914e:L54-L55` | Do not carry forward (no per-element update loop). |

## Acceptance outline

Point of truth: [element-021](../requirements.md#element-021).

- **Chrome recipe.** Water tank + Tap feeding an Open gutter sloped 5° toward a Catch basin; place a Sluice gate across the gutter mid-way (posts outside the gutter, blade inside the channel); route a rope from the blade tie over a Pulley to a Weight of 1.5 kg held by a Delay-released latch, or leave it closed.
- **Positive.** Gate lifted (Weight drops): water passes under the blade and fills the basin.
- **Negative / control.** Gate left closed: water pools upstream of the blade, then spills over the gutter sides onto the tray; the basin stays empty and the ledger still balances (no deleted inflow).
- **Boundaries.** InitialLift 0.05 passes visibly less than 0.25. A Weight lighter than the 1.0 kg blade cannot lift it. Lift beyond channel depth gives no extra flow. The closed blade seals the channel: no flow passes its side clearances.
- **Run/Reset.** Blade pose, cell volumes and rope restore exactly. **Save/Load.** InitialLift, pose and rope survive reload.
- **Integrations.** [sequence-task-389](../requirements.md#sequence-task-389) gate a route; [sequence-task-390](../requirements.md#sequence-task-390) sluice-controlled transport (with EL-027/EL-028).

## Open questions

1. Is the gate's lift actuated only by physical rope/pusher work (proposed), or also by a powered or activation input?
2. Canal locks (EL-027) need a larger sluice: separate size or the same part?
3. Does water pressure push on the blade (affecting the lift force)? Proposed: no hydraulic load on the blade.
4. ρw = 16 kg/m³ is one water-family constant, aligned with Batch G (EL-023–EL-036, EL-165–EL-172); the owner confirms or replaces it once for the whole family (S416 buoyancy row → S420). The orifice law and Cd = 0.6 are the S416 advection decision (→ S418).
