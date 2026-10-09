# EL-002 · Header tank — named-identity spec

Story 7.0 named-identity spec ([readiness spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations `path@a6c914e:Lstart-Lend` resolve with `git show a6c914e:<path>` after the Epic 7 purge. A value marked **proposed** has no legacy or requirement source; it carries a one-line justification and the owner may revise it.

## Identity

| Field | Value |
| --- | --- |
| EL ID / name / type | EL-002 · Header tank · Water |
| Requirement anchor | [element-002](../requirements.md#element-002); source record [todo-334](../requirements.md#todo-334); integration task [sequence-task-384](../requirements.md#sequence-task-384) |
| Named entry | [element-002](../invest/named-elements.md#element-002); per-element proof owner S423 |
| CAT spec refined | None. |
| Related identities | [EL-001 Finite reservoir](EL-001-finite-reservoir.md) (same store law at bench level), EL-008–EL-013 pipe kit, EL-022 Water pump and EL-023 Archimedes screw (fill it), EL-032 Pressure meter, hydraulic piston [todo-428](../requirements.md#todo-428) |
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
- **Bodies and shapes.** One static body: a tall narrow open-top tank on a four-leg stand. Tank outer 0.74 × 1.15 × 0.74 m (W × H × D), walls 0.12 m, base 0.15 m, interior footprint 0.5 × 0.5 m (**proposed**: recipe "Pressure, Not Plenty" asks for an elevated narrow tank, [component research](../../component-research.md#water) line 61). Overflow notch 1.0 m above the interior floor (**proposed**: capacity exactly 0.25 m³). Stand: four 0.08 m square box legs plus one cross-brace box; leg length equals StandHeight. The notch therefore sits StandHeight + 1.15 m above the bench (2.15 m at StandHeight 1.0, 3.15 m at 2.0).
- **Mass and material.** Static, zero mass; shared vessel material.
- **Constraints.** None. Height is authored, not jointed.
- **Typed ports.** `WaterOutlet` mouth through the tank floor facing down, bore top flush with the floor (**proposed**: drains completely and routes naturally into a pipe riser); `OpenMouth` top aperture for pump/screw or stream fill. Domain `Water` (**proposed**, as in EL-001).
- **Sensors and activation.** None owned.
- **Work and energy stores.** Finite liquid store, capacity 0.25 m³ (4 kg). Stored gravitational energy is ρw · g · V · h̄ above the delivery point; it is a consequence of the ledger and elevation, never a separate charge.
- **Parameters.**

  | Name | Type | Range | Default | Unit | Source |
  | --- | --- | --- | --- | --- | --- |
  | InitialVolume | f32 | 0–0.25, step 2⁻⁴ | 0.25 | m³ | parameter named by component research line 26; range/default **proposed** |
  | StandHeight | f32 | 0.5–4.0, step 0.25 | 2.0 | m | "outlet height" named by component research line 26; range/default **proposed** (keeps the tank top below the 6 m Wall maximum, `engine/gpu/WorkshopWall.cs@a6c914e:L6-L10`) |

- **Cosmetic curves and UI bindings.** Waterline ← committed volume (component research line 26). A gold height scale on one leg every 0.5 m (**proposed**: makes "lowering reduces lift" readable).
- **Art.** Cream tank `#fff8e9` (`DESIGN.md@a6c914e:L151-L151`), navy legs and foot `#293954` (`DESIGN.md@a6c914e:L147-L147`), gold scale `#f7cb52` (`DESIGN.md@a6c914e:L154-L154`), water window cyan `#66b8c9` at alpha 0.16 (`DESIGN.md@a6c914e:L181-L181`) (**proposed** reuse of approved colours).
- **Catalogue and inventory entry.** Id `header_tank`, title "Header tank", category "Water", kind `HeaderTank` (all **proposed**). Allowance via `PartAllowance` (`engine/gpu/WorkshopInventory.cs@a6c914e:L9-L23`).

**Variants.** The requirements row lists no variants.

## Engine capabilities

Families ([element map](../general-engine-element-map.md); binding [`element-01.json`](../../coverage/engine/element-01.json), consumer `element/element-002`): EnvironmentState, FiniteLedger, FluidAdvection, GeometryQuery, JointConstraint, PressureWork, RigidBodyDynamics, StateTransaction, TopologyTransaction.

- **Exists now.** Static bodies and box colliders (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L120`); constant gravity (`engine/gpu/WorkshopConstruction.cs@a6c914e:L123-L124`); Run/Reset (`engine/gpu/WorkshopSimulation.cs@a6c914e:L238-L245`); save codec (`engine/gpu/WorkshopSaveCodec.cs@a6c914e:L9-L10`).
- **Missing.** Liquid ledger and advection ([S416](../invest/decisions.md#s416) advection → S418); head transmitted through sealed pipes, PressureWork (S416 pressure-work → S419); Water connection domain (see EL-001 open question 3). Unscheduled.
- **Element dependencies.** Sealed pipe kit (EL-008–EL-012) to deliver head; EL-022 pump or EL-023 screw for the "cycling" check; EL-004 basin or a hydraulic piston as the lifted load.

## Sources and legacy

- Requirements row: "Lowering it reduces available lift; cycling cannot create energy." No variants.
- Family boundary [fluids](../invest/profiles.md#fluids) ("reversed head does not flow uphill"); [S416](../invest/decisions.md#s416) pressure-work ("moves a loaded hydraulic boundary … stops when the supply is depleted"); thermodynamic envelope "no free energy" ([envelope](../../gpu-f32-physics.md#game-grade-envelope)).
- Campaign: first use 61–70 ([campaign table](../requirements.md#campaign-element-coverage)); recipes Up and Over and Pressure, Not Plenty ([component research](../../component-research.md#water), lines 54 and 61).
- **Legacy search.** Same scope and terms as EL-001: no liquid element, level or test exists at a6c914e. No analogue beyond the finite-store rules cited in [EL-001](EL-001-finite-reservoir.md).

## Acceptance outline

Point of truth: [element-002](../requirements.md#element-002).

- **Chrome recipe.** Place a Header tank, set StandHeight with the selected part's height handle (**proposed** UI: resize handle on the legs, as Wall resize works), snap a riser of straight pipes and an elbow to `WaterOutlet`, ending in an open mouth over a Catch basin placed on a Wall-built shelf.
- **Positive.** With the tank surface above the delivery mouth, water rises through the sealed riser and fills the shelf basin.
- **Negative / control.** Lower StandHeight until the tank surface is below the delivery mouth: water stands in the riser at the tank's surface level and nothing is delivered. Empty tank: nothing moves.
- **Cycling control.** Pump (EL-022) returns basin water to the tank: the ledger never exceeds the initial volume and the pump's supplied work is at least ρw · g · V · Δh for each volume lifted; no loop runs without supply.
- **Boundaries.** StandHeight 0.5 and 4.0 admit; outside rejects atomically. A full tank receiving a stream spills from the notch.
- **Run/Reset.** Exact restoration of volume, ledger and pose. **Save/Load.** StandHeight, InitialVolume and joins survive reload.
- **Integrations.** [sequence-task-384](../requirements.md#sequence-task-384) (source record [todo-334](../requirements.md#todo-334)): a tank empties through the chosen tap/gutter into a catch basin with visible capacity, overflow and drain loss; [sequence-task-389](../requirements.md#sequence-task-389) ([todo-338](../requirements.md#todo-338)): lift water with power into the header tank (EL-022). Generic interaction rows [IX-08 Fluid advection](../requirements.md#interaction-08) and [IX-09 Pressure work](../requirements.md#interaction-09). Campaign first use 61–70, reuse 71–90, 114, 126–130, 136–150 ([campaign table](../requirements.md#campaign-element-coverage)).

## Open questions

1. ρw = 16 kg/m³ is one water-family constant, aligned with Batch G (EL-023–EL-036, EL-165–EL-172); the owner confirms or replaces it once for the whole family (S416 buoyancy row → S420).
2. Should StandHeight be continuous or locked to the 0.25 m step?
3. Is head loss in long sealed pipes modelled, or are pipes lossless (EL-008 proposes lossless)?
4. Should the tank itself be resizable (capacity) or only elevated?
