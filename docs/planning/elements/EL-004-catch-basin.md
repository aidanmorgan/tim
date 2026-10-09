# EL-004 · Catch basin — named-identity spec

Story 7.0 named-identity spec ([readiness spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations `path@a6c914e:Lstart-Lend` resolve with `git show a6c914e:<path>` after the Epic 7 purge. A value marked **proposed** has no legacy or requirement source; it carries a one-line justification and the owner may revise it.

## Identity

| Field | Value |
| --- | --- |
| EL ID / name / type | EL-004 · Catch basin · Water |
| Requirement anchor | [element-004](../requirements.md#element-004); source record [todo-334](../requirements.md#todo-334); integration task [sequence-task-384](../requirements.md#sequence-task-384) |
| Named entry | [element-004](../invest/named-elements.md#element-004); per-element proof owner S425 |
| CAT spec refined | None. The ball Receiver ([CAT-004](../requirements.md#current-cat-004)) is a different element; the basin borrows its open-box geometry pattern only. |
| Related identities | [EL-001](EL-001-finite-reservoir.md), [EL-003](EL-003-tap.md), [EL-005 Liquid funnel](EL-005-liquid-funnel.md), [EL-006 Drain](EL-006-drain.md), [EL-007 Open gutter](EL-007-open-gutter.md), [EL-016 Float](EL-016-float.md) (floats in it), EL-031 Volume meter |
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
- **Bodies and shapes.** One static body, five box colliders forming an open tray as the Receiver does (`engine/gpu/ReceiverGeometry.cs@a6c914e:L8-L19`): outer 1.49 × 0.55 × 1.24 m (W × H × D), walls 0.12 m, base 0.15 m; interior 1.25 × 0.40 × 1.0 m to the rim (**proposed**: about the Receiver's 1.5 m footprint, shallow enough to read the waterline and to hold a floating EL-016).
- **Mass and material.** Static, zero mass; shared vessel material.
- **Constraints.** None.
- **Typed ports.** `OpenMouth`: the rim-plane aperture 1.25 × 1.0 m. No outlet mouth (the drain is EL-006). Domain `Water` (**proposed**).
- **Sensors and activation.** Committed contained volume is readable by the puzzle goal (a fill band target), through the goal evaluator's committed-read rule (`engine/gpu/WorkshopGoalEvaluator.cs@a6c914e:L5-L8`). Capture is a swept test of free-stream packets against the rim aperture, never a render-rate overlap ([component research](../../component-research.md#water), line 15).
- **Work and energy stores.** Finite liquid store, capacity 0.5 m³ (8 steps, 8 kg) (**proposed**: half a tank, so one full EL-001 overflows it — the overflow control is reachable in one construction). Initial volume 0.
- **Parameters.**

  | Name | Type | Range | Default | Unit | Source |
  | --- | --- | --- | --- | --- | --- |
  | Capacity | f32, fixed by geometry | 0.5 | 0.5 | m³ | parameter named by component research line 30; value **proposed** |
  | TargetBand (puzzle-authored only) | f32 pair | 0–0.5, step 2⁻⁴ | none | m³ | **proposed**: needed by "Two Gardens" (fill bands, component research line 53) |

- **Cosmetic curves and UI bindings.** Fill height ← committed volume ([component research](../../component-research.md#water), line 30). When a TargetBand is authored, two gold marks show it; the rim turns pale green `#bdf4bd` while the volume is inside the band, mirroring the Basket rim cue (`DESIGN.md@a6c914e:L278-L278`) (**proposed**).
- **Art.** Basket teal `#4aab94` body (`DESIGN.md@a6c914e:L170-L170`) with cream rim `#fff8e9` (`DESIGN.md@a6c914e:L151-L151`); water cyan `#66b8c9` at alpha 0.16 (`DESIGN.md@a6c914e:L181-L181`) (**proposed**: a collector reads as a relative of the Basket).
- **Catalogue and inventory entry.** Id `catch_basin`, title "Catch basin", category "Water", kind `CatchBasin` (all **proposed**).

**Variants.** The requirements row lists no variants.

## Engine capabilities

Families ([element map](../general-engine-element-map.md); binding [`element-01.json`](../../coverage/engine/element-01.json), consumer `element/element-004`): EnvironmentState, FiniteLedger, FluidAdvection, GeometryQuery, JointConstraint, PressureWork, RigidBodyDynamics, StateTransaction, TopologyTransaction.

- **Exists now.** Static open-box geometry (Receiver pattern, `engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L57-L82`); committed-read goal evaluation (`engine/gpu/WorkshopGoalEvaluator.cs@a6c914e:L5-L8`); Run/Reset (`engine/gpu/WorkshopSimulation.cs@a6c914e:L238-L245`).
- **Missing.** Free-stream packet capture sweep and liquid ledger ([S416](../invest/decisions.md#s416) advection → S418); a volume feedback source for the cosmetic (`engine/gpu/WorkshopCosmetic.cs@a6c914e:L6-L16`); a volume-band goal kind. Unscheduled.
- **Element dependencies.** A source: EL-001 + EL-003, EL-005, EL-007 or EL-013.

## Sources and legacy

- Requirements row: "A missed stream remains outside; overflow remains conserved." No variants.
- Component research row "Catch basin / Liquid funnel / Drain (EL-004–006)": finite store with capacity, explicit overflow ([component research](../../component-research.md#water), line 30). Floor handling "shallow catch tray, bounded puddles or visible drains" (line 20).
- Family boundary [fluids](../invest/profiles.md#fluids); refinement [S704](../invest/refinements.md#s704) overflow.
- **Legacy search.** Same scope and terms as EL-001: no liquid collector exists at a6c914e. The analogue finite-store rules (capacity bound, no overfill) are cited in [EL-001](EL-001-finite-reservoir.md) facts 1–2; here too, excess must spill rather than be discarded.

## Acceptance outline

Point of truth: [element-004](../requirements.md#element-004).

- **Chrome recipe.** Place a Water tank with a Tap, then place a Catch basin with the move gizmo so the spout lies over its rim; a second lane places the basin 1 m to the side.
- **Positive.** Run: the basin's fill rises by exactly what the tank loses (minus water still in transit).
- **Negative / control.** Offset basin: the stream lands on the floor tray beside it; the basin stays at 0 and the tray holds the spilled volume.
- **Boundaries.** Fill a basin from a full 1 m³ tank: it holds 0.5 m³, and the remaining 0.5 m³ spills over the rim onto the tray, conserved. A stream grazing the rim edge is captured only where packets cross the rim aperture.
- **Run/Reset.** Basin returns to 0, tray to 0, ledger exact. **Save/Load.** Pose and authored TargetBand survive reload.
- **Integrations.** [sequence-task-384](../requirements.md#sequence-task-384) (visible remaining capacity and overflow); First Pour bucket mark analogue.

## Open questions

1. Is capacity fixed or resizable?
2. Is the fill-band goal a new goal kind, or expressed through an EL-031 volume meter driving the existing activation goal?
3. Does spilled floor water persist as bounded puddles or flow to the nearest drain? (component research line 20 allows either)
4. ρw = 16 kg/m³ is one water-family constant, aligned with Batch G (EL-023–EL-036, EL-165–EL-172); the owner confirms or replaces it once for the whole family (S416 buoyancy row → S420). Free-stream packet volume and budget are unspecified — owner S418.
