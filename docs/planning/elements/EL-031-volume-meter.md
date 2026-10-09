# EL-031 · Volume meter named-identity spec

This is the Story 7.0 full spec for named identity EL-031. The baseline is commit `a6c914e`; every citation uses `path@a6c914e:Lstart-Lend`. Values without a source are marked **proposed**, each with a one-line justification; the owner may revise them.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-031 |
| Name | Volume meter |
| Type | Water |
| Anchor | [requirements.md#element-031](../requirements.md#element-031); [named-elements.md#element-031](../invest/named-elements.md#element-031); scope index [todo-339](../requirements.md#todo-339) |
| Proof owner | S452 |
| CAT spec refined | none. Related: [CAT-020 Counter](CAT-020-counter.md) (accumulator presentation), [CAT-060 Sound meter](CAT-060-sound_meter.md) (threshold output) |
| Related identities | [EL-030 Flow meter](EL-030-flow-meter.md) (instantaneous rate, separate identity), [EL-032 Pressure meter](EL-032-pressure-meter.md); [EL-014 Water-carrying bucket](EL-014-water-carrying-bucket.md) (the 0.125 m³ default target) |
| Roadmap story | unscheduled |
| Status | not started (no `WorkshopPartKind` member, `engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`) |

## 2. Declaration

- **Bodies and shapes.** One static inline body (part root): a Box 0.4 × 0.35 × 0.3 m with a drum counter window on the front and a straight internal passage between two end ports. **Proposed**: the flow meter's footprint, made taller for the counter drum so the two read differently.
- **Mass and material.** Static. The shared static water-vessel material: restitution 0.12, bounce threshold 0.1 m/s, friction 0.3, rolling resistance 0 (**proposed** reuse of the Receiver material, `engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L57-L63`, as Batch F's vessels use).
- **Liquid.** Passage holds 0.002 m³ (**proposed**). Family density ρw = 16 kg/m³ (**proposed**, shared with Batch F; see [EL-023](EL-023-archimedes-screw.md)). No extra head loss (**proposed**).
- **Constraints.** None.
- **Typed ports.**

  | Port | Domain | Direction | Local position (m) | Notes |
  | --- | --- | --- | --- | --- |
  | `WaterInlet` | Water | Input | (−0.2, 0, 0) | Standard mouth; positive transport enters here |
  | `WaterOutlet` | Water | Output | (0.2, 0, 0) | Standard mouth; positive transport leaves here |
  | `TargetOut` | Activation | Output | (0, 0.175, 0.15) | True once the total reaches `target_volume` |

  Water ports **proposed**, named with the water family's `WaterInlet`/`WaterOutlet` (Batch F); `ActivationOut` exists (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L10`). Signal only; it powers nothing.
- **Sensors and activation.** Total = running sum of the signed volume committed across the passage each tick, `WaterInlet` → `WaterOutlet` positive, so liquid that sloshes back is subtracted and never counted twice (**proposed** net integration). The total is world state committed with the tick; reading it from the UI, selecting the part or repeated inspection changes nothing. `TargetOut` latches true when total ≥ `target_volume` and stays true until Reset (**proposed**: a "filled enough" goal should not flicker).
- **Work and energy stores.** None.
- **Parameters.**

  | Name | Type | Range | Default | Unit | Source / justification |
  | --- | --- | --- | --- | --- | --- |
  | `target_volume` | f32 | 1/16–4, step 1/16 | 0.125 | m³ | the 1/16 m³ step is the family volume scale ([component research](../../component-research.md#water)); default **proposed**: one 0.125 m³ EL-014 bucket (2 kg) |

- **Cosmetic curves and UI bindings.** The drum shows the committed total in 1/16 m³ marks; a gold pointer eases toward each committed mark over 0.16 s (**proposed**; curve vocabulary `engine/gpu/WorkshopCosmetic.cs@a6c914e:L7-L19`). The lamp follows `TargetOut`. A reading feedback source is missing.
- **Art.** Cream body `#fff8e9`, cyan passage window `#66b8c9`, navy drum digits `#293954`, gold pointer `#f7cb52`, slate-to-gold lamp (`#556573` off) ([DESIGN sound meter](../../../DESIGN.md#sound-meter) language). A stacked-bars motif distinguishes it from the flow meter's needle. **Proposed**.
- **Catalogue and inventory entry.** Id `volume_meter`, title "Volume meter", category Water (**proposed**). Counted allowance (`engine/gpu/WorkshopInventory.cs@a6c914e:L9-L23`).

**Variants.** The requirements row lists no variants; EL-031 is one declaration.

## 3. Engine capabilities

Families ([element-map row](../general-engine-element-map.md), [coverage binding](../../coverage/engine/element-01.json) `element-031`): EnvironmentState, FiniteLedger, FluidAdvection, GeometryQuery, JointConstraint, PressureWork, RigidBodyDynamics, SignalPropagation, StateTransaction (coverage only), TopologyTransaction.

**Exists now**
- Activation output and signal wiring: `engine/gpu/WorkshopConnections.cs@a6c914e:L8-L10`.
- Committed, Reset-restorable world state read by the host rather than computed on inspection (sensor read records such as `engine/gpu/OrientationSensorDeclaration.cs@a6c914e:L38-L39`).

**Missing**
- Per-tick transferred volume across a network node (S418): owner [S416](../invest/decisions.md#s416); unscheduled.
- A committed integrating reading (StateTransaction) with latch output: unscheduled.
- Reading feedback source for animation: unscheduled.

**Element dependencies.** A supplied route (reservoir, tap, pipes) and a signal consumer.

## 4. Sources and legacy

- **Requirement row** ([element-031](../requirements.md#element-031)): "Integrates actual transported liquid volume." Outcome: "Repeated inspection does not count the same volume twice." No variants.
- **Integration task** [sequence-task-390](../requirements.md#sequence-task-390); refinement [S708 volume-meter](../invest/refinements.md#s708): "Measure accumulated volume."
- **Research** ([component research](../../component-research.md#water)): "Separate moving-water, accumulated-volume and pressure readings; threshold controls do not power actuators".
- **Store observability** [S730](../invest/refinements.md#s730): "Expose one actual finite store's initial, current, spent and transferred quantity."
- **Campaign**: first use 61–70; reuse 81–100, 126–140, 146–150 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)).
- **Legacy.** None found. Searched the Epic 7 deletion scope at `a6c914e` for "volume meter", "totaliser", "water", "liquid": no element source.

**Files harvested:** none.

## 5. Acceptance outline

Follow the [EL-031 row](../requirements.md#element-031).
- **Chrome UI recipe.** In free play place a reservoir with 0.5 m³, a Manual tap, the volume meter inline, and an EL-014 bucket under the outlet; keep `target_volume` at 0.125 m³ (or set it with the real parameter control) and wire `TargetOut` to a signal lamp. Open the tap; Run.
- **Positive.** The drum counts up with the delivered volume; the lamp lights when 0.125 m³ has passed, matching the bucket's gain (2 kg).
- **Negative/control.** Pause and inspect, select or hover the meter repeatedly: the total does not change. A closed tap leaves it at zero.
- **Boundaries.** Liquid that flows back through the meter is subtracted (no double count); the reservoir holding less than the target never lights the lamp; very slow trickle still reaches the target exactly.
- **Run/Reset.** Total and latch restore to zero exactly.
- **Save/Load.** `target_volume`, pose and links round-trip; the running total is runtime state and is not saved.
- **Integrations.** EL-025 water clock as an alternative batch counter; logic with CAT-020 Counter.

## 6. Open questions

1. Net signed total (proposed) versus forward-only total: owner decision.
2. Whether a reset input (Activation) should zero the total during Run: owner decision (proposed none; Reset only).
3. Latching versus level-following `TargetOut`: owner decision (latch proposed).
4. Liquid density ρw = 16 kg/m³ (proposed) is one water-family constant shared with Batch F's EL-001–EL-022 specs: owner decision under S418/S420.
5. Shared static water-vessel material (restitution 0.12, friction 0.3, proposed): owner decision.
