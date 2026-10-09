# EL-032 · Pressure meter named-identity spec

This is the Story 7.0 full spec for named identity EL-032. The baseline is commit `a6c914e`; every citation uses `path@a6c914e:Lstart-Lend`. Values without a source are marked **proposed**, each with a one-line justification; the owner may revise them.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-032 |
| Name | Pressure meter |
| Type | Water |
| Anchor | [requirements.md#element-032](../requirements.md#element-032); [named-elements.md#element-032](../invest/named-elements.md#element-032); scope index [todo-339](../requirements.md#todo-339) |
| Proof owner | S453 |
| CAT spec refined | none. Related: [CAT-060 Sound meter](CAT-060-sound_meter.md) (dial and threshold-output precedent) |
| Related identities | [EL-030 Flow meter](EL-030-flow-meter.md), [EL-031 Volume meter](EL-031-volume-meter.md) (separate readings); [EL-026 Communicating tank](EL-026-communicating-tank.md); [EL-033 Fluid accumulator](EL-033-fluid-accumulator.md); the hydraulic piston ([todo-428](../requirements.md#todo-428)) |
| Roadmap story | unscheduled |
| Status | not started (no `WorkshopPartKind` member, `engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`) |

## 2. Declaration

- **Bodies and shapes.** One static gauge (part root): a short stem Box 0.12 × 0.25 × 0.12 m ending in one water port, under a round dial housing Box 0.35 × 0.35 × 0.12 m. **Proposed**: a dead-ended gauge on a T or tank port, visibly unlike the inline meters.
- **Mass and material.** Static. The shared static water-vessel material: restitution 0.12, bounce threshold 0.1 m/s, friction 0.3, rolling resistance 0 (**proposed** reuse of the Receiver material, `engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L57-L63`, as Batch F's vessels use).
- **Liquid.** Dead-ended: the gauge draws no liquid and holds none in the ledger (**proposed**: measuring must not deplete a store). Family density ρw = 16 kg/m³ (**proposed**, shared with Batch F; see [EL-023](EL-023-archimedes-screw.md)), so each metre of head reads ρw·g·h = 157 Pa.
- **Constraints.** None.
- **Typed ports.**

  | Port | Domain | Direction | Local position (m) | Notes |
  | --- | --- | --- | --- | --- |
  | `WaterMouth` | Water | Input | (0, −0.3, 0) | Single standard sensing mouth; sealed dead end |
  | `PressureOut` | Activation | Output | (0.2, 0.1, 0) | True while gauge pressure meets the threshold |

  `WaterMouth` **proposed** (the water family's single-mouth name, as in Batch F); `ActivationOut` exists (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L10`). Signal only.
- **Sensors and activation.** Reading state is a closed enum `PressureReading { Disconnected, Reading }` (**proposed**: the outcome distinguishes "cannot report supplied pressure" from a reading of zero). `Disconnected` when `WaterMouth` has no water connection or the connected node holds no liquid at the port; otherwise the gauge pressure p = ρw·g·(free-surface height above the port) for open stores, or the network node pressure for sealed routes (S419). `PressureOut` true when state is `Reading` and p ≥ `threshold` for `dwell` ticks; false below 0.8 × `threshold` (**proposed** hysteresis).
- **Work and energy stores.** None.
- **Parameters.**

  | Name | Type | Range | Default | Unit | Source / justification |
  | --- | --- | --- | --- | --- | --- |
  | `threshold` | f32 | 10–2000 | 80 | Pa | **proposed**: 0.51 m of head at 157 Pa per metre, about half a default tank depth |
  | `dwell` | u32 | 1–240 | 6 | ticks | dwell in ticks sourced for level sensors ([component research](../../component-research.md#water)); default **proposed** |
  | `full_scale` | f32 | 200–4000 | 400 | Pa | **proposed**: 2.55 m of head, covering the tallest default stack |

- **Cosmetic curves and UI bindings.** The needle follows the committed pressure ("Needle/dial ← committed reading", [component research](../../component-research.md#water)) with a 0.16 s SmoothStep easing (**proposed**; `engine/gpu/WorkshopCosmetic.cs@a6c914e:L7-L19`). In `Disconnected` the needle rests on a navy "no supply" peg below zero, so disconnection reads differently from 0 Pa. The lamp follows `PressureOut`.
- **Art.** Cream dial `#fff8e9` with navy ticks `#293954`, gold needle `#f7cb52`, cyan stem window `#66b8c9`, slate-to-gold lamp (`#556573` off) ([DESIGN sound meter](../../../DESIGN.md#sound-meter) language). **Proposed**.
- **Catalogue and inventory entry.** Id `pressure_meter`, title "Pressure meter", category Water (**proposed**). Counted allowance (`engine/gpu/WorkshopInventory.cs@a6c914e:L9-L23`).

**Variants.** The requirements row lists no variants; EL-032 is one declaration.

## 3. Engine capabilities

Families ([element-map row](../general-engine-element-map.md), [coverage binding](../../coverage/engine/element-01.json) `element-032`): EnvironmentState, FiniteLedger, FluidAdvection, GeometryQuery, JointConstraint, PressureWork, RigidBodyDynamics, SignalPropagation, StateTransaction (coverage only), TopologyTransaction.

**Exists now**
- Activation output and signal wiring: `engine/gpu/WorkshopConnections.cs@a6c914e:L8-L10`.
- Gravity constant used for hydrostatic head: `engine/gpu/WorkshopConstruction.cs@a6c914e:L123-L123`.

**Missing**
- Head/pressure model at a water port, open and sealed: [S416 pressure-work](../invest/decisions.md#s416) → S419; unscheduled.
- Water ports and the `Disconnected` topology state: S416 with [S257](../invest/decisions.md#s257); unscheduled.
- Threshold-with-dwell evaluation on a fluid reading and a reading feedback source: unscheduled.

**Element dependencies.** A store or sealed route with a free port (EL-026 tank, EL-011 T junction, EL-033 accumulator); a signal consumer.

## 4. Sources and legacy

- **Requirement row** ([element-032](../requirements.md#element-032)): "Measures local supported fluid pressure." Outcome: "Disconnected or depressurised port cannot report supplied pressure." No variants.
- **Integration task** [sequence-task-390](../requirements.md#sequence-task-390); refinement [S708 pressure-meter](../invest/refinements.md#s708): "Measure actual pressure."
- **Research** ([component research](../../component-research.md#water)): separate pressure reading; recipe 13 "Pressure, Not Plenty": choose an elevated narrow tank rather than a large low tank to lift a loaded hydraulic piston.
- **Campaign**: first use 61–70; reuse 81–100, 126–140, 146–150 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)).
- **Legacy.** None found. Searched the Epic 7 deletion scope at `a6c914e` for "pressure meter", "gauge", "water", "liquid": no element source. Legacy "pressure" hits are the world air-pressure scalar for the Balloon, harvested in [CAT-003](CAT-003-balloon.md).

**Files harvested:** none.

## 5. Acceptance outline

Follow the [EL-032 row](../requirements.md#element-032).
- **Chrome UI recipe.** In free play place a Communicating tank (EL-026) with 0.5 m³ and connect the pressure meter's `WaterMouth` to the tank's `WaterMouthA` with a short Straight water pipe, using real snapping; wire `PressureOut` to a signal lamp. Run, then fill the tank further from a tap.
- **Positive.** The needle shows ρw·g·depth at the port (157 Pa per metre) and rises as the tank fills; the lamp lights once 80 Pa (0.51 m above the port) is met.
- **Negative/control.** Remove the pipe (or cap the port): the needle rests on the "no supply" peg and the lamp stays off. Drain the tank below the port: the reading drops to `Disconnected`, not a stale value.
- **Boundaries.** Two tanks of equal depth at different widths read the same pressure; raising the tank on a block raises downstream pressure in a sealed route; a 2.55 m head reaches full scale; the gauge never changes any store's volume.
- **Run/Reset.** Reading state, value and output restore exactly.
- **Save/Load.** `threshold`, `dwell`, `full_scale`, pose and links round-trip.
- **Integrations.** Hydraulic piston, EL-033 accumulator charge level, recipe "Pressure, Not Plenty".

## 6. Open questions

1. Gauge versus absolute pressure (proposed gauge; absolute needs a world atmosphere from EnvironmentState, S470): owner decision.
2. Whether a sealed but liquid-empty route reads `Disconnected` or 0 Pa: S419 decision (proposed `Disconnected`).
3. Liquid density ρw = 16 kg/m³ (proposed), which sets every pressure value, is one water-family constant shared with Batch F's EL-001–EL-022 specs: owner decision under S418/S420.
4. Shared static water-vessel material (restitution 0.12, friction 0.3, proposed): owner decision.
