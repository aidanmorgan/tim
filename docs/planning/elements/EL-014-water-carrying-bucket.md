# EL-014 · Water-carrying bucket — named-identity spec

Story 7.0 named-identity spec ([readiness spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations `path@a6c914e:Lstart-Lend` resolve with `git show a6c914e:<path>` after the Epic 7 purge. A value marked **proposed** has no legacy or requirement source; it carries a one-line justification and the owner may revise it.

## Identity

| Field | Value |
| --- | --- |
| EL ID / name / type | EL-014 · Water-carrying bucket · Water |
| Requirement anchor | [element-014](../requirements.md#element-014); source record [todo-337](../requirements.md#todo-337); integration task [sequence-task-387](../requirements.md#sequence-task-387) |
| Named entry | [element-014](../invest/named-elements.md#element-014); per-element proof owner S435 |
| CAT spec refined | None. Integrates with [CAT-058 rope anchor](CAT-058-rope_anchor.md), [CAT-053 pulley](CAT-053-pulley.md) and the Weight [CAT-067](../requirements.md#current-cat-067). |
| Related identities | [EL-015 Leaky bucket](EL-015-leaky-bucket.md) (same body plus a leak), [EL-004](EL-004-catch-basin.md), [EL-005](EL-005-liquid-funnel.md) ("Catch the Escape"), EL-025 Tipping-bucket water clock; campaign "bucket/cage" rope load ([campaign table](../requirements.md#campaign-element-coverage)) |
| Roadmap story | Unscheduled; ELEMENT-n row of the [roadmap](../invest/vertical-delivery.md#rolling-playable-roadmap), campaign chapter 7, levels 61–70. Rope prerequisites: Epic 10, Story 10.2 (CAT-053, CAT-058). |
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
- **Bodies and shapes.** One dynamic body: an open box bucket of five box colliders — base 0.60 × 0.06 × 0.60 m and four walls 0.04 m thick — outer 0.60 × 0.5223 × 0.60 m, interior 0.52 × 0.4623 × 0.52 m (**proposed**: interior sized so the brim holds ≈ 0.125 m³; footprint smaller than a Receiver so it hangs clear of walls). These are full extents; the box colliders store halves. A cosmetic bail arc carries the rope tie 0.25 m above the rim on the vertical axis (**proposed**: keeps the hanging bucket's centre of mass below the tie, so it hangs upright).
- **Mass and material.** Empty mass 0.5 kg (**proposed**: half a Basketball, so the empty bucket is clearly lighter than a 1 kg counterweight); total mass = 0.5 + ρw · V, up to 2.5 kg full — within the 1/1024–1024 kg admission bound (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L82`). Centre of mass = mass-weighted mean of the shell centroid and the contents centroid (half the fill height). Contact material: shared vessel material. Linear drag 0 (**proposed**, matching the legacy Weight's zero drag, `parts/WeightPart.cs@a6c914e:L28-L34`).
- **Constraints.** None owned. A rope (CAT-058/CAT-053) attaches to the tie as a distance-limit constraint; slack carries nothing, taut lifts (S257 rope-port, [decisions](../invest/decisions.md#s257)).
- **Typed ports.** `RopeTie` (domain Rope, bidirectional, attachment kind Load — the legacy Weight's pattern, fact 1) and `OpenMouth` (the rim aperture, catches streams). Domain `Water` for the mouth (**proposed**).
- **Sensors and activation.** None.
- **Work and energy stores.** Finite liquid store, capacity 0.125 m³ (2 steps, 2 kg) (**proposed**: two steps make "filled outweighs a 1.5 kg counterweight, empty does not" a clean control). Spill rule: the free surface stays level with gravity; contents above the lowest rim point leave as a conserved free stream from that point (geometric; [component research](../../component-research.md#water), line 32 "tilt spills").
- **Parameters.**

  | Name | Type | Range | Default | Unit | Source |
  | --- | --- | --- | --- | --- | --- |
  | InitialVolume | f32 | 0–0.125, step 2⁻⁴ | 0 | m³ | capacity named by component research line 32; range/default **proposed** (an empty bucket is the Borrowed Weight start) |

- **Cosmetic curves and UI bindings.** Tilt ← committed pose; fill ← committed volume ([component research](../../component-research.md#water), line 32). Two etched marks at 1 and 2 steps (**proposed**).
- **Art.** Weight blue `#45639c` shell (`DESIGN.md@a6c914e:L184-L184`) with a cream rim `#fff8e9` (`DESIGN.md@a6c914e:L151-L151`) and a gold bail eye `#f7cb52` (`DESIGN.md@a6c914e:L154-L154`), the legacy Weight's eye colour (`parts/WeightPart.cs@a6c914e:L38-L38`) (**proposed**: a rope load reads as a relative of the Weight).
- **Catalogue and inventory entry.** Id `water_bucket`, title "Bucket", category "Water", kind `WaterBucket` (all **proposed**).

**Variants.** The requirements row lists no variants. The leaking bucket is the separate identity EL-015.

## Engine capabilities

Families ([element map](../general-engine-element-map.md); binding [`element-01.json`](../../coverage/engine/element-01.json), consumer `element/element-014`): EnvironmentState, FiniteLedger, FluidAdvection, GeometryQuery, JointConstraint, PressureWork, RigidBodyDynamics, StateTransaction, TopologyTransaction.

- **Exists now.** Dynamic box bodies with derived inertia (`engine/gpu/RigidMassProperties.cs@a6c914e:L16-L18`; Domino, `engine/gpu/WorkshopDomino.cs@a6c914e:L6-L10`); Run/Reset (`engine/gpu/WorkshopSimulation.cs@a6c914e:L238-L245`).
- **Missing.** Compound dynamic bodies (five boxes, one body); run-time variable mass and centre of mass driven by the liquid ledger — `RigidMassProperties` is immutable at compile; liquid store, capture and geometric spill ([S416](../invest/decisions.md#s416) advection → S418); JointConstraint rope (Story 10.2, rope-port decision S257 → S688). Unscheduled.
- **Element dependencies.** CAT-058 rope anchor and CAT-053 pulley (Story 10.2); a counterweight (CAT-067 Weight or a ball); a source (EL-001 + EL-003).

## Sources and legacy

- Requirements row: "Filled load changes rope balance; empty control does not." No variants.
- Integration [sequence-task-387](../requirements.md#sequence-task-387) and refinement [S706](../invest/refinements.md#s706) bucket-balance, bucket-tip ("Tipping spills real contents"). Component research: "carried water adds body mass without double counting" (line 16); recipes Borrowed Weight and Catch the Escape (lines 51 and 62).
- **Legacy search.** No liquid bucket. Rope-load analogue:

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | The Weight is a dynamic rope load: one `Tie` socket, domain Rope, bidirectional, attachment kind Load, tie above the body. | `parts/WeightPart.cs@a6c914e:L13-L21`; `engine/ConnectionPort.cs@a6c914e:L11-L11` | Carry forward the port pattern for the bucket's `RopeTie`. Do not carry forward the Godot-bound record types. |
| 2 | Weight mass admitted 0.25–8 kg (default 4 kg), radius 0.32 · ∛m, tie at radius + 0.08 m, bounce 0.08, drag 0. | `parts/WeightPart.cs@a6c914e:L22-L34`; `engine/MachineData.cs@a6c914e:L99-L100`; `parts/catalog/weight.tres@a6c914e:L8-L14` | Carry forward as the counterweight scale: a 1.5 kg Weight balances between an empty (0.5 kg) and full (2.5 kg) bucket. |
| 3 | Legacy connection domains were {Unknown, Activation, Electrical, Signal, Mechanical, Rope}: no water domain ever existed. | `engine/MachineData.cs@a6c914e:L68-L68` | Recorded: the Water domain is new work. |

## Acceptance outline

Point of truth: [element-014](../requirements.md#element-014).

- **Chrome recipe.** Place a Rope anchor high on a Wall, a Pulley, a Weight at 1.5 kg and a Bucket; route the rope anchor → pulley → bucket tie and the Weight on the other side with the real rope tool; place a Water tank and Tap above the bucket.
- **Positive.** Run with the tap open: the bucket fills past 1 kg of water, outweighs the Weight, descends and lifts it.
- **Negative / control.** Tap closed: the empty bucket (0.5 kg) rises and the Weight descends; same rope, no water. A slack rope lifts nothing.
- **Boundaries.** Filling beyond 0.125 m³ spills over the rim (conserved). Tilting the full bucket (a ramp or pusher strikes it) spills exactly the volume above the lowest rim point; the remaining load sets the new balance. Total mass never exceeds 2.5 kg.
- **Run/Reset.** Pose, volume and rope state restore exactly. **Save/Load.** InitialVolume, pose and rope route survive reload.
- **Integrations.** [sequence-task-387](../requirements.md#sequence-task-387): a filled rope bucket changes a lift's balance; tipping spills real contents.

## Open questions

1. Variable mass at run time conflicts with compile-time `RigidMassProperties`; the S416 decision must say whether the contents are a second body or a mass update per tick.
2. Does sloshing (contents lag) matter, or is the free surface instantaneous (proposed)?
3. Empty mass 0.5 kg and capacity 0.125 m³: confirm.
4. ρw = 16 kg/m³ is one water-family constant, aligned with Batch G (EL-023–EL-036, EL-165–EL-172); the owner confirms or replaces it once for the whole family (S416 buoyancy row → S420).
