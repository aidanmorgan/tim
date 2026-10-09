# EL-172 · Rain collector named-identity spec

This is the Story 7.0 full spec for named identity EL-172. The baseline is commit `a6c914e`; every citation uses `path@a6c914e:Lstart-Lend`. Values without a source are marked **proposed**, each with a one-line justification; the owner may revise them.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-172 |
| Name | Rain collector |
| Type | Water |
| Anchor | [requirements.md#element-172](../requirements.md#element-172); [named-elements.md#element-172](../invest/named-elements.md#element-172); scope index [todo-340](../requirements.md#todo-340) |
| Proof owner | S465 |
| CAT spec refined | none. Related: [CAT-030 Funnel](CAT-030-funnel.md) (ball funnel geometry, a different domain) |
| Related identities | [EL-036 Sprinkler](EL-036-sprinkler.md) and EL-013 Water nozzle (droplet sources); EL-004 Catch basin and EL-005 Liquid funnel (stream catchers, separate); EL-008 pipes (outlet route) |
| Roadmap story | unscheduled |
| Status | not started (no `WorkshopPartKind` member, `engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`) |

## 2. Declaration

- **Bodies and shapes.** One static part: an open catch mouth 1.0 × 1.0 m at the top of a shallow tapered hopper (four inclined Box walls) over a cylindrical store represented by a Box 0.5 × 0.8 × 0.5 m interior, total height 1.2 m. **Proposed**: a 1 m² mouth intercepts a fair share of a 2 m sprinkler footprint.
- **Mass and material.** Static. The shared static water-vessel material: restitution 0.12, bounce threshold 0.1 m/s, friction 0.3, rolling resistance 0 (**proposed** reuse of the Receiver material, `engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L57-L63`, as Batch F's vessels use). The map row lists no RigidBodyDynamics, so the collector never moves.
- **Liquid.** Finite store capacity 0.2 m³ (the 0.5 × 0.8 × 0.5 m interior, **proposed**), 3.2 kg at the **proposed** family density ρw = 16 kg/m³ (shared with Batch F; see [EL-023](EL-023-archimedes-screw.md)). Overflow at the hopper rim leaves as free-stream packets and stays in the ledger.
- **Constraints.** None.
- **Typed ports.**

  | Port | Domain | Direction | Local position (m) | Notes |
  | --- | --- | --- | --- | --- |
  | `OpenMouth` | Water | Input | (0, 0.6, 0) | Open catch surface; captures only packets whose swept path crosses it |
  | `WaterOutlet` | Water | Output | (0, −0.6, 0.25) | Bottom standard mouth; capped when unconnected |

  **Proposed**; names follow the water family's vocabulary (`OpenMouth`, `WaterOutlet`, as in Batch F). No signal or power port.
- **Sensors and activation.** None. Stored volume is observable.
- **Work and energy stores.** None. Capture is purely geometric: a free-stream packet adds its volume only when its swept path between substep endpoints intersects the open mouth and no other collider intercepts it first ([component research](../../component-research.md#water): "Swept against moving mouths so capture never depends on render rate"). Packets that miss or are occluded stay outside. Outlet flow follows the store head as for any store.
- **Parameters.** None configurable (**proposed**: one size; capacity limits are the teaching point).
- **Cosmetic curves and UI bindings.** Waterline in a restrained side window follows the committed volume; splash rings on the mouth where packets are captured. No easing.
- **Art.** Cream hopper `#fff8e9` with a gold rim `#f7cb52`, cyan level window `#66b8c9`, navy stand and graduations `#293954` every 1/16 m³ ([DESIGN colour system](../../../DESIGN.md#colour-system)). **Proposed**.
- **Catalogue and inventory entry.** Id `rain_collector`, title "Rain collector", category Water (**proposed**). Counted allowance (`engine/gpu/WorkshopInventory.cs@a6c914e:L9-L23`).

**Variants.** The requirements row lists no variants; EL-172 is one declaration.

## 3. Engine capabilities

Families ([element-map row](../general-engine-element-map.md), [coverage binding](../../coverage/engine/element-02.json) `element-172`): FiniteLedger, FluidAdvection, GeometryQuery, StateTransaction (coverage only), TopologyTransaction. It is the smallest family set in this batch: no rigid dynamics, no pressure work, no joints.

**Exists now**
- Static Box colliders: `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L120`.
- Endpoint-sampled sweep against a planar aperture for bodies, with sub-tick interpolation ([game-grade envelope](../../gpu-f32-physics.md#game-grade-envelope) row "Sensor Crossing"); the same sweep pattern applies to liquid packets.

**Missing**
- Free-stream packets, sweep capture against an open mouth and occlusion by other colliders (FluidAdvection, GeometryQuery): [S416 advection](../invest/decisions.md#s416) → S418; unscheduled.
- Liquid store and outlet port (FiniteLedger, TopologyTransaction): S418 with [S257](../invest/decisions.md#s257); unscheduled.

**Element dependencies.** A droplet source (EL-036 Sprinkler, EL-013 nozzle) supplied by a store (EL-001, EL-002); an outlet route (EL-008 pipes) and receiver.

## 4. Sources and legacy

- **Requirement row** ([element-172](../requirements.md#element-172)): "Open catch surface gathers only intersecting droplets into finite storage." Outcome: "An occluded or missed stream does not fill it." No variants.
- **Research** ([component research](../../component-research.md#water)): "Sprinkler / Rain collector (EL-036, EL-172) — Distributed packet source with finite budget + collection mouth"; "collection is accounted". Free-stream packet law: "Swept against moving mouths so capture never depends on render rate".
- **Integration task** [sequence-task-531](../requirements.md#sequence-task-531): finite storage observable before it acts.
- **Campaign**: first use 81–90, reuse 91–100, 131–140, 146–150 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)).
- **Legacy.** None found. Searched the Epic 7 deletion scope at `a6c914e` for "rain", "collector", "droplet", "water": no element source.

**Files harvested:** none.

## 5. Acceptance outline

Follow the [EL-172 row](../requirements.md#element-172).
- **Chrome UI recipe.** In free play build the EL-036 sprinkler construction. Using placement handles raise its head to the Rain collector mouth height and start the header surface 1 m above that head, so capture uses the equal-elevation bands. Centre the collector mouth 1.5 m along a sampled azimuth; its 1 m-wide opening contains the 1.125 m band. Connect WaterOutlet through a Straight water pipe to a Catch basin below it. Inspect the selected preview to confirm a trajectory crosses the mouth before Run.
- **Positive.** Droplets landing on the mouth fill the collector; the outlet passes collected water to the basin; the header's loss equals collected + missed + spilled volume.
- **Negative/control.** Place a Wall or Ramp over the mouth: occluded droplets are deflected and the collector stays empty. Move the collector outside the footprint: it stays empty. A stream aimed beside the mouth adds nothing.
- **Boundaries.** Collector full with the outlet capped (rim overflow conserved); droplets grazing the rim edge (captured only if the sweep crosses the open area); high-speed packets do not tunnel past the mouth.
- **Run/Reset.** Stored volume and in-flight packets restore exactly.
- **Save/Load.** Pose and the outlet link round-trip.
- **Integrations.** EL-036 Sprinkler, EL-025 tipping bucket fed from its outlet.

## 6. Open questions

1. Whether a weather/rain source exists in the world (EnvironmentState) or droplets come only from placed sprinklers and nozzles: owner decision (placed sources proposed; the map row lists no EnvironmentState).
2. Whether the collector also captures a continuous stream from a spout (proposed yes, since streams are packets) or only spray droplets: owner decision.
3. Liquid density ρw = 16 kg/m³ (proposed) is one water-family constant shared with Batch F's EL-001–EL-022 specs: owner decision under S418/S420.
4. Shared static water-vessel material (restitution 0.12, friction 0.3, proposed): owner decision.
