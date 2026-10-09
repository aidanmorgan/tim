# EL-003 · Tap — named-identity spec

Story 7.0 named-identity spec ([readiness spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations `path@a6c914e:Lstart-Lend` resolve with `git show a6c914e:<path>` after the Epic 7 purge. A value marked **proposed** has no legacy or requirement source; it carries a one-line justification and the owner may revise it.

## Identity

| Field | Value |
| --- | --- |
| EL ID / name / type | EL-003 · Tap · Water |
| Requirement anchor | [element-003](../requirements.md#element-003); source record [todo-334](../requirements.md#todo-334); integration task [sequence-task-384](../requirements.md#sequence-task-384) |
| Named entry | [element-003](../invest/named-elements.md#element-003); per-element proof owner S424 |
| CAT spec refined | None. |
| Related identities | Actuation refinements, each a separate identity: [EL-165 Manual tap](EL-165-manual-tap.md), [EL-166 Mechanically actuated tap](EL-166-mechanically-actuated-tap.md), [EL-167 Solenoid tap](EL-167-solenoid-tap.md) (campaign "manual/mechanical/solenoid tap variants", [campaign table](../requirements.md#campaign-element-coverage)). Supply: [EL-001](EL-001-finite-reservoir.md), [EL-002](EL-002-header-tank.md). |
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

**Element declaration.** EL-003 is the base tap declaration: Batch G's EL-165, EL-166 and EL-167 copy its body, ports and opening step. The water family names ports descriptively everywhere (`WaterInlet`, `WaterOutlet`, and `WaterMouthA`/`WaterMouthB` on symmetric fittings).
- **Bodies and shapes.** One static inline valve body: box 0.30 × 0.25 × 0.25 m (full extents), with a quarter-turn handle on top (cosmetic, no collider) (**proposed**: a short pipe-section length so it drops into a route; the handle is large enough to read its angle).
- **Mass and material.** Static, zero mass; shared vessel material.
- **Constraints.** None for this declaration (the handle is not a joint; EL-166 adds linkage).
- **Typed ports.** Two standard mouths, domain `Water` (**proposed** positions and new domain):

  | Port | Domain | Direction | Local position (m) | Notes |
  | --- | --- | --- | --- | --- |
  | `WaterInlet` | Water | Input | (−0.15, 0, 0) | Joins a supply mouth |
  | `WaterOutlet` | Water | Output | (0.15, 0, 0) | Joins a pipe, or emits a free stream when unjoined |

- **Sensors and activation.** None.
- **Work and energy stores.** None. The tap only scales the supply's own discharge: Q = Cd · s · A · √(2 g Δh), where Δh is the upstream surface height above `WaterOutlet` and s the opening; it never adds head.
- **Parameters.**

  | Name | Type | Range | Default | Unit | Source |
  | --- | --- | --- | --- | --- | --- |
  | Opening | f32 | 0–1, step 0.125 | 0 | fraction of the bore area | range named by component research line 27; step **proposed** (eighth-turn detents read on the handle, nine positions); default closed **proposed** (a placed tap must be opened deliberately) |

- **Cosmetic curves and UI bindings.** Handle angle ← committed opening, 0° closed to 90° open ([component research](../../component-research.md#water), line 27). A pale stream ribbon from an unjoined `WaterOutlet` follows committed outflow; cosmetic particles never decide capture (component research line 20).
- **Art.** Gold handle `#f7cb52` (`DESIGN.md@a6c914e:L154-L154`), cream valve body `#fff8e9` (`DESIGN.md@a6c914e:L151-L151`), navy collar rings `#293954` (`DESIGN.md@a6c914e:L147-L147`) (**proposed** reuse). Open/closed also shown by handle orientation, not colour alone ([common visual contract](../requirements.md#individual-puzzle-elements)).
- **Catalogue and inventory entry.** Id `tap`, title "Tap", category "Water", kind `Tap` (all **proposed**).

**Variants.** The requirements row lists no variants. Manual, mechanical and solenoid actuation are separate identities EL-165, EL-166 and EL-167, specified in their own files (Batch G).

## Engine capabilities

Families ([element map](../general-engine-element-map.md); binding [`element-01.json`](../../coverage/engine/element-01.json), consumer `element/element-003`): EnvironmentState, FiniteLedger, FluidAdvection, GeometryQuery, JointConstraint, PressureWork, RigidBodyDynamics, StateTransaction, TopologyTransaction.

- **Exists now.** Static body and box collider (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L120`); Run/Reset (`engine/gpu/WorkshopSimulation.cs@a6c914e:L238-L245`); typed port declarations by kind (`engine/gpu/WorkshopConnections.cs@a6c914e:L15-L25`), without a Water domain.
- **Missing.** Water network valve node and orifice discharge ([S416](../invest/decisions.md#s416) advection → S418); head (S416 pressure-work → S419); Water mouth joins (EL-001 open question 3). Unscheduled.
- **Element dependencies.** A supply (EL-001 or EL-002) on `WaterInlet`; EL-004 basin or EL-007 gutter as receiver.

## Sources and legacy

- Requirements row: "Closing stops routed flow; an unconnected tap emits nothing." No variants.
- Component research row "Tap / stopcock (EL-003, EL-165–167)": network valve node, opening 0–1, "regulates existing supply … never free water" ([component research](../../component-research.md#water), line 27).
- Family boundary [fluids](../invest/profiles.md#fluids); refinement [S704](../invest/refinements.md#s704); lesson First Pour (tank → tap → gutter → bucket, component research line 49).
- **Legacy search.** Same scope and terms as EL-001 plus "valve" and "stopcock": no liquid valve exists at a6c914e. The only valve legacy is gas-domain (`engine/physics/AdiabaticGasDischarge.cs`), which belongs to the pneumatic family and is not harvested here.

## Acceptance outline

Point of truth: [element-003](../requirements.md#element-003).

- **Chrome recipe.** Place a Water tank (EL-001), drag a Tap until its `WaterInlet` snaps to the tank outlet, set Opening with the handle control (**proposed** UI: drag the handle through its eight detents), place a Catch basin where the free stream from `WaterOutlet` lands.
- **Positive.** Opening 1: the basin fills at the full-bore rate; Opening 0.5 roughly halves the initial rate.
- **Negative / control.** Opening 0: nothing leaves the tank. A tap placed alone, `WaterInlet` unjoined: Run emits nothing at any opening.
- **Boundaries.** Opening 0, 0.125 and 1 admit; values off the step or outside 0–1 reject atomically. Emptying the tank stops the flow even with the tap open. A tap whose `WaterInlet` is joined to a mouth of an incompatible domain is refused at the join.
- **Run/Reset.** Exact restoration of opening, volumes and ledger. **Save/Load.** Opening and joins survive reload.
- **Integrations.** [sequence-task-384](../requirements.md#sequence-task-384) chosen-tap route; First Pour.

## Open questions

1. Is EL-003 its own catalogue entry, or the shared valve body that EL-165 (manual) ships as? If one entry, EL-003's "fixed during Run" behaviour is EL-165's outcome and EL-003 owns only the aperture law.
2. Opening step 0.125 versus continuous.
3. Does a partly open tap throttle linearly in area (proposed) or follow a valve characteristic?
4. ρw = 16 kg/m³ is one water-family constant, aligned with Batch G (EL-023–EL-036, EL-165–EL-172); the owner confirms or replaces it once for the whole family (S416 buoyancy row → S420). The orifice law and Cd = 0.6 are the S416 advection decision (→ S418).
