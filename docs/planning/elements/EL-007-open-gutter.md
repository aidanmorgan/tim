# EL-007 · Open gutter — named-identity spec

Story 7.0 named-identity spec ([readiness spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations `path@a6c914e:Lstart-Lend` resolve with `git show a6c914e:<path>` after the Epic 7 purge. A value marked **proposed** has no legacy or requirement source; it carries a one-line justification and the owner may revise it.

## Identity

| Field | Value |
| --- | --- |
| EL ID / name / type | EL-007 · Open gutter · Water |
| Requirement anchor | [element-007](../requirements.md#element-007); source record [todo-334](../requirements.md#todo-334); integration task [sequence-task-384](../requirements.md#sequence-task-384) |
| Named entry | [element-007](../invest/named-elements.md#element-007); per-element proof owner S428 |
| CAT spec refined | None. Its geometry follows the Ramp's scale ([CAT-054](../requirements.md#current-cat-054)) but it is a separate element. |
| Related identities | [EL-003](EL-003-tap.md), [EL-004](EL-004-catch-basin.md), [EL-006](EL-006-drain.md), [EL-021 Sluice gate](EL-021-sluice-gate.md) (gates a gutter), EL-025 tipping bucket, waterwheel ([todo-336](../requirements.md#todo-336)) |
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
- **Bodies and shapes.** One static body: a U-channel of three boxes (floor and two side walls). Interior width 0.30 m, wall height 0.15 m, wall and floor thickness 0.06 m; outer width 0.42 m (**proposed**: three times the standard bore so a spout stream lands inside it, and shallow so the waterline is visible). Length authored (below). A Basketball (radius 0.34 m) does not fit inside, so balls ride on the rims rather than in the channel.
- **Mass and material.** Static, zero mass; shared vessel material.
- **Constraints.** None.
- **Typed ports.** `OpenMouth` along the whole open top (captures intersecting streams); two `SpillEdge` ends (open, unsealed). A gutter end never joins a sealed pipe mouth as a pressure connection; it only drops a free stream into whatever lies below ([component research](../../component-research.md#water), line 29). Domain `Water` (**proposed**).
- **Sensors and activation.** None.
- **Work and energy stores.** No work store. A distributed open store: capacity 0.045 m³ per metre (0.135 m³ at 3 m), derived from the proposed section. Transport: the channel is split into 0.25 m cells (**proposed**: twelve cells on the default length; discretisation is negotiable inside conservation, [fluids](../invest/profiles.md#fluids)); flow between neighbouring cells follows the shared orifice law on their free-surface height difference, so water only moves toward lower surface. Depth above 0.15 m spills over the side walls as conserved free streams; liquid reaching an end spills from that end.
- **Parameters.**

  | Name | Type | Range | Default | Unit | Source |
  | --- | --- | --- | --- | --- | --- |
  | Length | f32 | 1.0–6.0, step 0.25 | 3.0 | m | parameter named by component research line 29; default matches the Ramp's 3 m (`engine/gpu/WorkshopInstances.cs@a6c914e:L22-L25`); range/step **proposed** |
  | Slope | from rotation | −30° to +30° pitch | 0° | degrees | parameter named by component research line 29; expressed through the rotate gizmo, range **proposed** (steeper than 30° reads as a chute, not a gutter) |

- **Cosmetic curves and UI bindings.** Per-cell waterline ← committed cell volumes; spill ribbon at an overflowing edge ← committed overflow ([component research](../../component-research.md#water), line 29).
- **Art.** Ramp ochre `#c28f52` channel (`DESIGN.md@a6c914e:L169-L169`) with cream inner lining `#fff8e9` (`DESIGN.md@a6c914e:L151-L151`); water cyan `#66b8c9` alpha 0.16 (`DESIGN.md@a6c914e:L181-L181`) (**proposed**: a gutter is a ramp relative).
- **Catalogue and inventory entry.** Id `gutter`, title "Open gutter", category "Water", kind `OpenGutter` (all **proposed**). Resizable through the existing resize-handle pattern used by Ramp and Wall.

**Variants.** The requirements row lists no variants. Component research groups "trough / aqueduct" with the gutter (line 29); they are naming, not separate identities.

## Engine capabilities

Families ([element map](../general-engine-element-map.md); binding [`element-01.json`](../../coverage/engine/element-01.json), consumer `element/element-007`): EnvironmentState, FiniteLedger, FluidAdvection, GeometryQuery, JointConstraint, PressureWork, RigidBodyDynamics, StateTransaction, TopologyTransaction.

- **Exists now.** Static boxes and the resizable-part pattern (Ramp dimensions, `engine/gpu/WorkshopInstances.cs@a6c914e:L22-L25`; Wall bounds, `engine/gpu/WorkshopWall.cs@a6c914e:L6-L10`); Run/Reset (`engine/gpu/WorkshopSimulation.cs@a6c914e:L238-L245`).
- **Missing.** Open-channel cells, side and end spill packets ([S416](../invest/decisions.md#s416) advection → S418). Unscheduled.
- **Element dependencies.** A source (EL-001 + EL-003); a receiver (EL-004, EL-006, EL-014).

## Sources and legacy

- Requirements row: "Adverse slope stalls or spills; no hidden uphill transport." No variants.
- Component research: open gutters flow downhill only, cannot act as an uphill pressure pipe (lines 13 and 29). Lesson First Pour "tank → tap → broad gutter → marked bucket" (line 49); its control "gutter sloped away (water spills to the tray, bucket stays empty)" (line 66).
- **Legacy search.** Same scope and terms as EL-001: no channel element exists at a6c914e.

## Acceptance outline

Point of truth: [element-007](../requirements.md#element-007).

- **Chrome recipe.** Place a Water tank and Tap; place an Open gutter under the spout, tilt it 5° down toward a Catch basin with the rotate gizmo, and resize it with its handle so the low end overhangs the basin.
- **Positive.** Water runs down the channel and fills the basin (First Pour).
- **Negative / control.** Tilt the gutter 5° the other way: water runs to the far end and spills onto the tray; the basin stays empty. Level gutter fed at one end: water spreads, overflows the ends, and never climbs.
- **Boundaries.** Inflow above the channel's carrying rate overtops the side walls at the inflow point, conserved. Length 1.0 and 6.0 admit; outside rejects. A gutter end placed against a pipe mouth does not join it.
- **Run/Reset.** Every cell returns to 0 exactly. **Save/Load.** Length, pose and slope survive reload.
- **Integrations.** [sequence-task-384](../requirements.md#sequence-task-384); waterwheel stream source in Run the Mill.

## Open questions

1. Cell length 0.25 m: confirm, or tie discretisation to S418's advection decision.
2. Channel section fixed, or a wide "trough" variant?
3. Should balls roll inside a wider gutter (shared ball-and-water channel), or stay rim-only?
4. ρw = 16 kg/m³ is one water-family constant, aligned with Batch G (EL-023–EL-036, EL-165–EL-172); the owner confirms or replaces it once for the whole family (S416 buoyancy row → S420). The orifice law and Cd = 0.6 are the S416 advection decision (→ S418).
