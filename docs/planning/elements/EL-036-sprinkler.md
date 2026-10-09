# EL-036 · Sprinkler named-identity spec

This is the Story 7.0 full spec for named identity EL-036. The baseline is commit `a6c914e`; every citation uses `path@a6c914e:Lstart-Lend`. Values without a source are marked **proposed**, each with a one-line justification; the owner may revise them.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-036 |
| Name | Sprinkler |
| Type | Water |
| Anchor | [requirements.md#element-036](../requirements.md#element-036); [named-elements.md#element-036](../invest/named-elements.md#element-036); scope index [todo-340](../requirements.md#todo-340) |
| Proof owner | S457 |
| CAT spec refined | none |
| Related identities | EL-013 Water nozzle (single jet; the sprinkler divides into a footprint), [EL-172 Rain collector](EL-172-rain-collector.md) (catches its droplets), EL-002 Header tank and EL-022 Water pump (pressure sources); the pneumatic Sprinkler/jet regions in `docs/finite-gas-foundation.md` are a separate air family |
| Roadmap story | unscheduled |
| Status | not started (no `WorkshopPartKind` member, `engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`) |

## 2. Declaration

- **Bodies and shapes.** A static base (part root): Box 0.3 × 0.1 × 0.3 m, with a riser Box 0.06 × 0.3 × 0.06 m and a head 0.16 m across at local (0, 0.4, 0). **Proposed**: a garden-sprinkler silhouette whose head clears a basin rim.
- **Mass and material.** Static. The shared static water-vessel material: restitution 0.12, bounce threshold 0.1 m/s, friction 0.3, rolling resistance 0 (**proposed** reuse of the Receiver material, `engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L57-L63`, as Batch F's water fixtures use).
- **Liquid.** Family density ρw = 16 kg/m³ (**proposed**, shared with Batch F; see [EL-023](EL-023-archimedes-screw.md)). The riser holds 0.001 m³ in the ledger (**proposed**).
- **Constraints.** None. The head's spin is cosmetic.
- **Typed ports.** `WaterInlet` (Water, Input, local (0, −0.05, 0.15), **proposed**; the water family's inlet name, as in Batch F): a standard sealed inlet mouth; unconnected, it is capped. No signal or power port.
- **Sensors and activation.** None. Its committed spray rate is observable.
- **Work and energy stores.** No store beyond the riser liquid. Available head h is measured at the emission head, including the 0.45 m rise from WaterInlet and upstream losses. Proposed passive regulation limits useful head to h_eff = min(max(h, 0), 1.5 m); excess pressure work ρw·g·Q·(h − h_eff) is dissipated in the regulator. v_max = √(2·g·h_eff), Q = 0.6·A·v_max. Each tick splits Q·dt evenly into jets packets. A proposed shared deterministic spray sampler cycles 64 slots: j = (tickIndex·jets + packetIndex) mod 64; speed fraction f = (floor(j/16) + 1)/4; azimuth = −arc/2 + arc·((j mod 16) + 0.5)/16; elevation 45°. Each packet launches at f·v_max; the remaining per-packet pressure work ½·m·v_max²·(1 − f²) is dissipated in its outlet restriction, never added elsewhere. This produces four sampled radial bands through the sector over a cycle, not continuous wetting of every interior point. At a horizontal capture plane y metres below the head, range is R = f²·h_eff + √((f²·h_eff)² + 2·f²·h_eff·y); R = 2·f²·h_eff only at equal elevation. The preview traces these physical paths and actual occluders; below-head landing can exceed 3 m and is never clamped geometrically. Every packet remains in the ledger until captured or spilled. Scene packet capacity rejects excessive authoring, never deletes water.
- **Parameters.**

  | Name | Type | Range | Default | Unit | Source / justification |
  | --- | --- | --- | --- | --- | --- |
  | `arc` | f32 | 45–360, step 45 | 360 | degrees | **proposed**: a sector lets a level aim spray at one garden |
  | `jets` | u32 | 4–16 | 8 | packets per tick | **proposed**: enough to read as spray within a small packet budget |
  | `nozzle_area` | f32 | fixed | 0.002 | m² | **proposed**: about 0.005 m³/s at 1 m head (one-sixteenth of the 0.083 m³/s full-bore EL-003 tap), a slow even watering |
  | `regulated_head` | f32 | fixed | 1.5 | m | **proposed**: passive pressure loss bounds equal-elevation maximum range to 3 m without truncating ballistic paths |

- **Cosmetic curves and UI bindings.** The head spins at a rate proportional to committed flow; spray arcs follow committed packets. The footprint preview is shown only while the part is selected ("Selected-only footprint preview", [component research](../../component-research.md#water)). "Spray ← committed rate".
- **Art.** Cream base `#fff8e9`, gold rotating head `#f7cb52`, navy riser `#293954`, cyan droplets `#66b8c9`; the selected-only footprint is a thin pale-green outline `#bdf4bd` like the Basket capture area ([DESIGN motion](../../../DESIGN.md#motion-and-state-feedback)). **Proposed**.
- **Catalogue and inventory entry.** Id `sprinkler`, title "Sprinkler", category Water (**proposed**). Counted allowance (`engine/gpu/WorkshopInventory.cs@a6c914e:L9-L23`).

**Variants.** The requirements row lists no variants; EL-036 is one declaration. `arc` is configuration.

## 3. Engine capabilities

Families ([element-map row](../general-engine-element-map.md), [coverage binding](../../coverage/engine/element-01.json) `element-036`): EnvironmentState, FiniteLedger, FluidAdvection, GeometryQuery, JointConstraint, PressureWork, RigidBodyDynamics, StateTransaction (coverage only), TopologyTransaction.

**Exists now**
- Static Box colliders: `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L120`.
- Gravity used for packet ballistics: `engine/gpu/WorkshopConstruction.cs@a6c914e:L123-L123`.

**Missing**
- Free-stream packets with a capture sweep against moving mouths, and a packet budget (FluidAdvection): [S416 advection](../invest/decisions.md#s416) → S418; unscheduled.
- Head/pressure at the inlet (PressureWork): S419; unscheduled.
- Selected-only footprint preview in the Workshop UI: unscheduled.

**Element dependencies.** A pressurised supply (EL-002 Header tank via pipes, EL-022 Water pump); receivers (EL-004 Catch basin, EL-172 Rain collector, EL-034 Sponge).

## 4. Sources and legacy

- **Requirement row** ([element-036](../requirements.md#element-036)): "Supplied pressure divides liquid into a bounded spray footprint." Outcome: "Blocked supply prevents spray and off-footprint receivers remain dry." No variants.
- **Integration task** [sequence-task-531](../requirements.md#sequence-task-531): sprinklers remain individually tracked.
- **Research** ([component research](../../component-research.md#water)): "Distributed packet source with finite budget + collection mouth"; parameters "budget, footprint"; "Selected-only footprint preview; collection is accounted"; "Spray ← committed rate". Free-stream packet law: "Swept against moving mouths so capture never depends on render rate".
- **Campaign**: first use 81–90, reuse 91–100, 131–140, 146–150 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)).
- **Legacy.** None found. Searched the Epic 7 deletion scope at `a6c914e` for "sprinkler", "spray", "water": no element source. `engine/physics/NozzleCoupledJetReceiver.cs@a6c914e:L7-L10` is an air-jet receiver ("transported fluid are not modeled"), not water; no element knowledge for EL-036.

**Files harvested:** `engine/physics/NozzleCoupledJetReceiver.cs` (checked; air jet, no water element knowledge).

## 5. Acceptance outline

Follow the [EL-036 row](../requirements.md#element-036).
- **Chrome UI recipe.** In free play connect a Header tank through a Manual tap and pipes to WaterInlet. With placement handles align the sprinkler head and the two Catch basin mouths at the same height, and start the header surface 1 m above the head (allowing for upstream losses). Place the near basin centred 1.5 m away along one sampled azimuth, with its mouth covering the selected preview's 1.125 m or 2 m band; place the far basin 4 m away at the same mouth height. Set arc with its real control, inspect the preview, deselect and Run with the tap open.
- **Positive.** The near basin intercepts actual sampled trajectories and gains exactly their packet volume. At 1 m useful head the equal-elevation bands lie at 0.125, 0.5, 1.125 and 2 m; measure the capture before the draining header moves the selected band outside the mouth. Header loss equals captured plus spilled plus in-flight and riser volume changes.
- **Negative/control.** Tap closed or inlet capped: no spray. The far basin at the head's elevation stays dry: its nearest edge is beyond the regulated 3 m maximum. A 90° `arc` aimed away from the near basin leaves it dry.
- **Boundaries.** Header draining reduces every band; a body intercepting a path occludes it. Head above 1.5 m increases regulator dissipation, not exit speed. A receiver below the head uses the longer ballistic range in section 2; no arbitrary radius discards its packets. Spray never creates volume.
- **Run/Reset.** In-flight packets, store volumes and head spin restore exactly.
- **Save/Load.** `arc`, `jets`, pose and the water link round-trip; out-of-range values reject.
- **Integrations.** EL-172 Rain collector, EL-034 Sponge.

## 6. Open questions

1. Whether the head physically rotates (sweeping one jet) or sprays all jets every tick (proposed): owner decision.
2. Whether to adopt the proposed 1.5 m passive regulator and four-band sampler, or a different bounded pressure/distribution declaration: owner decision.
3. Packet budget per scene shared with nozzles and spouts: S418 decision.
4. Liquid density ρw = 16 kg/m³ (proposed) is one water-family constant shared with Batch F's EL-001–EL-022 specs: owner decision under S418/S420.
5. Shared static water-vessel material (restitution 0.12, friction 0.3, proposed): owner decision.
