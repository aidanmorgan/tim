# EL-015 · Leaky bucket — named-identity spec

Story 7.0 named-identity spec ([readiness spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations `path@a6c914e:Lstart-Lend` resolve with `git show a6c914e:<path>` after the Epic 7 purge. A value marked **proposed** has no legacy or requirement source; it carries a one-line justification and the owner may revise it.

## Identity

| Field | Value |
| --- | --- |
| EL ID / name / type | EL-015 · Leaky bucket · Water |
| Requirement anchor | [element-015](../requirements.md#element-015); source record [todo-337](../requirements.md#todo-337) and historical task [todo-212](../requirements.md#todo-212); integration task [sequence-task-387](../requirements.md#sequence-task-387) |
| Named entry | [element-015](../invest/named-elements.md#element-015); per-element proof owner S436 |
| CAT spec refined | None. Integrates with [CAT-058 rope anchor](CAT-058-rope_anchor.md) and [CAT-053 pulley](CAT-053-pulley.md). |
| Related identities | [EL-014 Water-carrying bucket](EL-014-water-carrying-bucket.md) (identical body and store; this spec adds the leak), [EL-004](EL-004-catch-basin.md) (catches the leak) |
| Roadmap story | Unscheduled; ELEMENT-n row of the [roadmap](../invest/vertical-delivery.md#rolling-playable-roadmap), campaign chapter 7, levels 61–70. Rope prerequisites: Story 10.2. |
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
- **Bodies and shapes.** Identical to [EL-014](EL-014-water-carrying-bucket.md): dynamic five-box bucket, outer 0.60 × 0.5223 × 0.60 m, interior 0.52 × 0.4623 × 0.52 m (full extents), rope tie 0.25 m above the rim (all **proposed** there), plus a leak hole in the base centre.
- **Mass and material.** Empty 0.5 kg; total 0.5 + ρw · V up to 2.5 kg; vessel contact material; drag 0 (as EL-014, **proposed** there). Centre of mass moves down and the total falls as the bucket drains.
- **Constraints.** None owned; rope tie as EL-014.
- **Typed ports.** `RopeTie` (Rope, Load), `OpenMouth` rim aperture, and `LeakOutlet`, a non-joinable free-stream emitter at the base hole facing down. Domain `Water` (**proposed**).
- **Sensors and activation.** None.
- **Work and energy stores.** Finite liquid store, capacity ≈ 0.125 m³ (as EL-014). Leak discharge Q = Cd · π r_leak² · √(2 g h), h = current fill height above the hole; the stream leaves with the bucket's own velocity added, so a swinging bucket sprays along its path. Q is zero exactly when the store is empty. Drain time from full: (A_bucket / (Cd · A_leak)) · √(2 h₀ / g) ≈ 27.5 s at the default radius (derived from the proposed values).
- **Parameters.**

  | Name | Type | Range | Default | Unit | Source |
  | --- | --- | --- | --- | --- | --- |
  | InitialVolume | f32 | 0–0.125, step 2⁻⁴ | 0.125 | m³ | **proposed**: a leaky bucket starts full so the delayed counterbalance plays at once |
  | LeakRadius | f32 | 0.03–0.06, step 0.01 | 0.04 | m | "leak rate" named by component research line 32; values **proposed** (full drain ≈ 49 s at 0.03, 27.5 s at 0.04, 12 s at 0.06 — a puzzle-length delay) |

- **Cosmetic curves and UI bindings.** Tilt ← committed pose; fill ← committed volume; leak stream ribbon ← committed leak rate ([component research](../../component-research.md#water), line 32 "leaks drain visibly into other containers").
- **Art.** As EL-014 (Weight blue `#45639c`, `DESIGN.md@a6c914e:L184-L184`; cream rim `#fff8e9`, `DESIGN.md@a6c914e:L151-L151`), plus a slate drip ring `#556573` around the hole (`DESIGN.md@a6c914e:L277-L277`) (**proposed**: the hole is shown by shape and a ring, not colour alone).
- **Catalogue and inventory entry.** Id `leaky_bucket`, title "Leaky bucket", category "Water", kind `LeakyBucket` (all **proposed**).

**Variants.** The requirements row lists no variants.

## Engine capabilities

Families ([element map](../general-engine-element-map.md); binding [`element-01.json`](../../coverage/engine/element-01.json), consumer `element/element-015`): EnvironmentState, FiniteLedger, FluidAdvection, GeometryQuery, JointConstraint, PressureWork, RigidBodyDynamics, StateTransaction, TopologyTransaction.

- **Exists now.** Dynamic boxes (`engine/gpu/RigidMassProperties.cs@a6c914e:L16-L18`); Run/Reset (`engine/gpu/WorkshopSimulation.cs@a6c914e:L238-L245`).
- **Missing.** Everything listed for EL-014 (compound dynamic body, run-time variable mass, liquid store, rope) plus a moving free-stream emitter whose packets inherit body velocity ([S416](../invest/decisions.md#s416) advection → S418). Unscheduled.
- **Element dependencies.** EL-014's dependencies; a receiver (EL-004) for the leak.

## Sources and legacy

- Requirements row: "Caught leakage equals lost inventory; empty bucket stops leaking." No variants.
- Historical task [todo-212](../requirements.md#todo-212): "Leaky bucket loses mass over time" (The Incredible Machine 2 manual); "Extend moving containers with conserved water outflow, changing load and visible fill level; catch leaks in other containers. Teach delayed counterbalance … countdown-only mass loss is no longer the proposed contract."
- Refinement [S706](../invest/refinements.md#s706) bucket-leak "A leaking bucket supplies another receiver"; recipe Borrowed Weight "a leaking bucket that reverses the balance later" (component research line 51).
- **Legacy search.** Same scope and terms as EL-001 plus "leak": the hits are prediction and solver "cannot leak" assertions with no element knowledge. The countdown mass-loss bucket of todo-212 was never implemented in the tracked legacy. Rope-load facts are harvested in [EL-014](EL-014-water-carrying-bucket.md).

## Acceptance outline

Point of truth: [element-015](../requirements.md#element-015).

- **Chrome recipe.** Rope anchor + Pulley + Weight at 1.5 kg on one side and a full Leaky bucket on the other (rope tool); a Catch basin placed under the bucket's lowest travel point.
- **Positive.** Run: the full bucket (2.5 kg) descends and lifts the Weight; as it leaks below 1 kg of water it becomes lighter than the Weight and rises again (delayed reversal). The basin's gain equals the bucket's loss at every committed tick (minus packets in flight).
- **Negative / control.** InitialVolume 0: no stream ever leaves the bucket. An EL-014 bucket in the same rig never reverses.
- **Boundaries.** At empty the leak stops exactly (no negative volume). LeakRadius 0.03 and 0.06 admit and change the reversal time; outside rejects. A leak stream missing the basin lands on the tray, still conserved.
- **Run/Reset.** Volume, pose and rope restore exactly. **Save/Load.** InitialVolume, LeakRadius and the rope route survive reload.
- **Integrations.** [sequence-task-387](../requirements.md#sequence-task-387): a leaking bucket supplies another receiver.

## Open questions

1. Should LeakRadius be authored, or fixed per kind with one radius?
2. Should a pipe be joinable to the leak hole (turning it into an outlet), or is it always a free stream (proposed)?
3. Variable mass at run time: see EL-014 open question 1.
4. ρw = 16 kg/m³ is one water-family constant, aligned with Batch G (EL-023–EL-036, EL-165–EL-172); the owner confirms or replaces it once for the whole family (S416 buoyancy row → S420). The orifice law and Cd = 0.6 are the S416 advection decision (→ S418).
