# EL-030 · Flow meter named-identity spec

This is the Story 7.0 full spec for named identity EL-030. The baseline is commit `a6c914e`; every citation uses `path@a6c914e:Lstart-Lend`. Values without a source are marked **proposed**, each with a one-line justification; the owner may revise them.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-030 |
| Name | Flow meter |
| Type | Water |
| Anchor | [requirements.md#element-030](../requirements.md#element-030); [named-elements.md#element-030](../invest/named-elements.md#element-030); scope index [todo-339](../requirements.md#todo-339) |
| Proof owner | S451 |
| CAT spec refined | none. Related: [CAT-060 Sound meter](CAT-060-sound_meter.md) (meter presentation and threshold-output precedent) |
| Related identities | [EL-031 Volume meter](EL-031-volume-meter.md) and [EL-032 Pressure meter](EL-032-pressure-meter.md) (distinct readings, separate identities); EL-008 Straight water pipe (inline connection); [EL-003 Tap](EL-003-tap.md) (reference flow) |
| Roadmap story | unscheduled |
| Status | not started (no `WorkshopPartKind` member, `engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`) |

## 2. Declaration

- **Bodies and shapes.** One static inline body (part root): a Box 0.4 × 0.3 × 0.3 m with a round dial face on the front and a straight internal passage between two end ports. **Proposed**: the same footprint as a short pipe section, so it drops into a route.
- **Mass and material.** Static. The shared static water-vessel material: restitution 0.12, bounce threshold 0.1 m/s, friction 0.3, rolling resistance 0 (**proposed** reuse of the Receiver material, `engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L57-L63`, as Batch F's vessels use).
- **Liquid.** The passage holds 0.002 m³ in the ledger (**proposed**: small enough not to matter, nonzero so a full route is consistent). Family density ρw = 16 kg/m³ (**proposed**, shared with Batch F; see [EL-023](EL-023-archimedes-screw.md)). The meter adds no head loss beyond the equivalent straight pipe (**proposed**: measuring does not change what is measured).
- **Reference flow.** EL-003's law Q = Cd·s·A·√(2·g·Δh) with Cd 0.6, the standard 0.10 m bore (A = 0.0314 m²), s = 1 and Δh = 1 m gives about 0.083 m³/s ([EL-003](EL-003-tap.md); values **proposed** there). The thresholds below are set against it.
- **Constraints.** None.
- **Typed ports.**

  | Port | Domain | Direction | Local position (m) | Notes |
  | --- | --- | --- | --- | --- |
  | `WaterInlet` | Water | Input | (−0.2, 0, 0) | Standard mouth; positive flow enters here |
  | `WaterOutlet` | Water | Output | (0.2, 0, 0) | Standard mouth; positive flow leaves here |
  | `FlowOut` | Activation | Output | (0, 0.15, 0.15) | True while the reading meets the threshold |

  Water ports **proposed**, named with the water family's `WaterInlet`/`WaterOutlet` (Batch F); `ActivationOut` exists (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L10`). The output is a signal only: "threshold controls do not power actuators" ([component research](../../component-research.md#water)).
- **Sensors and activation.** Reading = signed volume that actually crossed the passage during the committed tick ÷ tick duration (m³/s), positive `WaterInlet` → `WaterOutlet`. `FlowOut` turns true when |reading| ≥ `threshold` for `dwell` consecutive ticks and false when it falls below 0.8 × `threshold` (**proposed** hysteresis band against chatter). Sampled at tick boundaries from committed state.
- **Work and energy stores.** None.
- **Parameters.**

  | Name | Type | Range | Default | Unit | Source / justification |
  | --- | --- | --- | --- | --- | --- |
  | `threshold` | f32 | 0.001–0.2 | 0.02 | m³/s | **proposed**: about a quarter of the 0.083 m³/s reference tap flow, so a half-open tap at 1 m head trips it and a trickle does not |
  | `dwell` | u32 | 1–240 | 6 | ticks | dwell in ticks is sourced for level sensors ([component research](../../component-research.md#water)); default **proposed** (0.05 s at 120 Hz) |
  | `full_scale` | f32 | 0.05–0.5 | 0.1 | m³/s | **proposed**: the dial covers the 0.083 m³/s reference flow and the 0.038 m³/s default screw with headroom |

- **Cosmetic curves and UI bindings.** The needle follows the committed reading ("Needle/dial ← committed reading", [component research](../../component-research.md#water)) with a short SmoothStep easing of 0.16 s (**proposed**, matching the existing activation curve duration, `engine/gpu/WorkshopCosmetic.cs@a6c914e:L7-L19`); reverse flow swings the needle left of zero. The lamp follows `FlowOut`. `AnimationFeedbackSource` has no reading source yet.
- **Art.** Cream body `#fff8e9`, cyan passage window `#66b8c9`, navy dial ticks `#293954`, gold needle `#f7cb52`, slate-to-gold lamp (`#556573` off), following the Sound meter's language ([DESIGN sound meter](../../../DESIGN.md#sound-meter)); a raised arrow on the body marks `WaterInlet` → `WaterOutlet`. **Proposed**.
- **Catalogue and inventory entry.** Id `flow_meter`, title "Flow meter", category Water (**proposed**). Counted allowance (`engine/gpu/WorkshopInventory.cs@a6c914e:L9-L23`).

**Variants.** The requirements row lists no variants; EL-030 is one declaration. Volume and pressure readings are separate identities (EL-031, EL-032).

## 3. Engine capabilities

Families ([element-map row](../general-engine-element-map.md), [coverage binding](../../coverage/engine/element-01.json) `element-030`): EnvironmentState, FiniteLedger, FluidAdvection, GeometryQuery, JointConstraint, PressureWork, RigidBodyDynamics, SignalPropagation, StateTransaction (coverage only), TopologyTransaction.

**Exists now**
- Activation output socket, typed wiring and signal propagation to lamps and delays: `engine/gpu/WorkshopConnections.cs@a6c914e:L8-L10`.
- Static Box collider: `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L120`.

**Missing**
- Water passage on a network node and per-tick transferred volume (S418): owner [S416](../invest/decisions.md#s416); unscheduled.
- Committed meter reading exposed to the animation worker as a feedback source: unscheduled.
- Threshold-with-dwell evaluation on a fluid reading (generic sensor kind): unscheduled.

**Element dependencies.** A supplied route (EL-001 reservoir, EL-003/EL-165 tap, EL-008 pipes); a signal consumer (CAT-022 Delay, signal lamp, CAT-020 Counter).

## 4. Sources and legacy

- **Requirement row** ([element-030](../requirements.md#element-030)): "Measures actual volume per simulation time through its ports." Outcome: "Stopped flow reads zero despite upstream stored volume." No variants.
- **Integration task** [sequence-task-390](../requirements.md#sequence-task-390): "flow/volume/pressure measurements react to the actual fluid state; each device/measurement remains a distinct item". Refinement [S708 flow-meter](../invest/refinements.md#s708): "Measure actual flow."
- **Research** ([component research](../../component-research.md#water)): "Sensors on a network node"; "Separate moving-water, accumulated-volume and pressure readings; threshold controls do not power actuators"; "Needle/dial ← committed reading".
- **Decisions**: [S416](../invest/decisions.md#s416) advection; [S257](../invest/decisions.md#s257) typed power versus signal.
- **Campaign**: first use 61–70; reuse 81–100, 126–140, 146–150 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)).
- **Legacy.** None found. Searched the Epic 7 deletion scope at `a6c914e` for "flow meter", "flow", "water", "liquid": no element source. The legacy sound meter's threshold presentation is harvested in [CAT-060](CAT-060-sound_meter.md).

**Files harvested:** none.

## 5. Acceptance outline

Follow the [EL-030 row](../requirements.md#element-030).
- **Chrome UI recipe.** In free play place a filled Finite reservoir (EL-001), a Manual tap (EL-165) on its outlet, the flow meter inline after it with a Straight water pipe, and a Catch basin at the end; wire `FlowOut` to a signal lamp with the contextual connection UI. Set the tap open; Run.
- **Positive.** The needle rises to the actual flow (near the 0.083 m³/s reference at full opening and 1 m head), the lamp lights once 0.02 m³/s is met for the dwell, and the reading falls as the reservoir head drops.
- **Negative/control.** Tap closed with a full reservoir upstream: the reading is exactly zero and the lamp stays off. A meter not in the route reads zero.
- **Boundaries.** Reverse flow reads negative and still meets |threshold|; a flow just below threshold (tap at 0.125 opening, about 0.010 m³/s) never lights the lamp; the reservoir emptying returns the reading to zero; the meter never creates or deletes volume.
- **Run/Reset.** Reading, output state and passage volume restore exactly.
- **Save/Load.** `threshold`, `dwell`, `full_scale`, pose and links round-trip.
- **Integrations.** EL-023 screw discharge, EL-024 siphon outlet, logic via CAT-022 Delay or CAT-020 Counter.

## 6. Open questions

1. Signed reading versus magnitude with a separate direction flag: owner decision (signed proposed).
2. Whether the activation output is supply-free signal (proposed, as the map lists no ElectricalPower) or needs a supply like the Sound meter's lamp: owner decision under S257.
3. Liquid density ρw = 16 kg/m³ (proposed) is one water-family constant shared with Batch F's EL-001–EL-022 specs: owner decision under S418/S420.
4. Shared static water-vessel material (restitution 0.12, friction 0.3, proposed): owner decision.
