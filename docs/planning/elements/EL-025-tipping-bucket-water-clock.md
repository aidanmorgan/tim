# EL-025 · Tipping-bucket water clock named-identity spec

This is the Story 7.0 full spec for named identity EL-025. The baseline is commit `a6c914e`; every citation uses `path@a6c914e:Lstart-Lend`. Values without a source are marked **proposed**, each with a one-line justification; the owner may revise them.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-025 |
| Name | Tipping-bucket water clock |
| Type | Water |
| Anchor | [requirements.md#element-025](../requirements.md#element-025); [named-elements.md#element-025](../invest/named-elements.md#element-025); scope index [todo-338](../requirements.md#todo-338) |
| Proof owner | S446 |
| CAT spec refined | none. Related: [CAT-034 Impact lever](CAT-034-impact_lever.md) (hinge precedent), [CAT-009 Bell](CAT-009-bell.md) and [CAT-020 Counter](CAT-020-counter.md) (downstream of the tip) |
| Related identities | EL-003/EL-165 taps and EL-013 nozzle (inflow), EL-004 Catch basin (discharge receiver), [EL-014 Water-carrying bucket](EL-014-water-carrying-bucket.md) (moving container precedent) |
| Roadmap story | unscheduled |
| Status | not started (no `WorkshopPartKind` member, `engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`) |

## 2. Declaration

- **Bodies and shapes.** A static frame (part root): navy foot 1.0 × 0.1 × 0.7 m and two uprights 0.06 × 0.9 × 0.06 m at z = ±0.3 m carrying the pivot axis (local Z) at (0, 0.8, 0). A dynamic cup body: an open-topped box of outer 0.6 × 0.5 × 0.5 m, walls and floor 0.03 m (interior 0.54 × 0.47 × 0.44 m), built from five Box colliders, hung with its centre at (0.15, 0.55, 0) so its rim is level with the pivot. A counterweight Box 0.12 × 0.12 × 0.4 m centred at (−0.25, 0.8, 0) on the same body. All **proposed**: a 0.6 m cup catches a nozzle stream and reads clearly beside the 0.68 m Basketball.
- **Mass and material.** Cup 0.3 kg; counterweight compiled from `tip_volume` (below), 0.78 kg at the default. Material restitution 0.05, friction 0.4 (**proposed**: a light, non-bouncy moving container, as for EL-014).
- **Liquid.** Family density ρw = 16 kg/m³ (**proposed**, shared with Batch F; see [EL-023](EL-023-archimedes-screw.md)), so one 2⁻⁴ m³ step weighs 1.0 kg. Brim volume 0.54 × 0.44 × 0.47 = 0.112 m³; fill line 0.29 m above the interior floor, holding 0.54 × 0.44 × 0.29 = 0.069 m³ (**proposed**: the largest `tip_volume` must fit below it with margin before the lip). The contained liquid adds mass and moves the cup's centre of mass with the fill level ("carried water adds body mass without double counting", [component research](../../component-research.md#water)).
- **Pivot arms and counterweight.** In the upright pose the cup's centre and its liquid centroid lie a_c = 0.15 m on the dump side of the pivot; the counterweight centre lies a_w = 0.25 m on the other side (**proposed** geometry). Taking moments about the pivot, the cup leaves its upright stop when (m_cup + ρw·V)·a_c > m_w·a_w, so the compiler sets m_w = (m_cup + ρw·V_tip)·a_c / a_w. Default: (0.3 + 1.0) × 0.15 / 0.25 = 0.78 kg; at the minimum tip volume (0.5 kg of liquid) 0.48 kg. Once the cup turns, the liquid shifts toward the lip and lengthens its arm, so the tip completes decisively. Empty, the cup's moment (0.3 × 0.15 = 0.045 N·m/g) is well below the counterweight's (0.78 × 0.25 = 0.195 N·m/g), so it returns upright after dumping.
- **Constraints.** One revolute hinge cup ↔ frame about local Z with travel limits 0° (upright stop) to 110° (dump stop), connected collision disabled (Story 10.3 hinge with limits, [CAT-034](CAT-034-impact_lever.md)).
- **Typed ports.**

  | Port | Domain | Direction | Local position (m) | Notes |
  | --- | --- | --- | --- | --- |
  | `OpenMouth` | Water | Input | (0.15, 0.8, 0) | Open cup mouth; captures intersecting free-stream packets only |
  | `TipOut` | Activation | Output | (−0.45, 0.1, 0) | One occurrence per qualifying tip |

  `OpenMouth` is **proposed** (the water family's open-aperture port name, as in Batch F's EL-004 and EL-014); `ActivationOut` already exists as a socket (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L10`).
- **Sensors and activation.** A rearmable orientation threshold on the cup: one occurrence when the hinge angle first reaches 60° from upright, rearmed only after the cup returns below 10° (**proposed**: hysteresis prevents one dump firing twice). The current orientation sensor is sticky once per world (`engine/gpu/OrientationSensorDeclaration.cs@a6c914e:L24-L34`), so a rearm rule is missing. The cup lip can also strike a placed bell physically (ContactImpulse) without any signal.
- **Work and energy stores.** None. The tip is paid by the liquid's potential energy; the return by the counterweight. Spilled liquid leaves through the real lip as free-stream packets and stays in the ledger.
- **Parameters.**

  | Name | Type | Range | Default | Unit | Source / justification |
  | --- | --- | --- | --- | --- | --- |
  | `tip_volume` | f32 | 0.03125–0.0625, step 1/64 | 0.0625 | m³ | **proposed**: one 2⁻⁴ m³ step (the family volume scale, [component research](../../component-research.md#water)) = 1.0 kg of liquid; every value fits under the 0.069 m³ fill line; the counterweight is compiled from it |

  "tip volume" as the parameter name is sourced (component research Tipping-bucket row).
- **Cosmetic curves and UI bindings.** The cup follows the committed hinge angle ("Cup ← committed hinge angle", [component research](../../component-research.md#water)); waterline follows committed contained volume. No easing.
- **Art.** Cream cup `#fff8e9` with a cyan liquid window `#66b8c9`, gold pivot and counterweight `#f7cb52`, navy foot `#293954`; a small navy tick mark at the tip-volume line ([DESIGN colour system](../../../DESIGN.md#colour-system)). **Proposed**.
- **Catalogue and inventory entry.** Id `tipping_bucket`, title "Tipping-bucket water clock", category Water (**proposed**). Counted allowance (`engine/gpu/WorkshopInventory.cs@a6c914e:L9-L23`).

**Variants.** The requirements row lists no variants; EL-025 is one declaration. Driving a bell physically or a counter by signal are integrations, not variants.

## 3. Engine capabilities

Families ([element-map row](../general-engine-element-map.md), [coverage binding](../../coverage/engine/element-01.json) `element-025`): ContactImpulse, EnvironmentState, FiniteLedger, FluidAdvection, GeometryQuery, JointConstraint, RigidBodyDynamics, SignalPropagation, StateTransaction (coverage only), TopologyTransaction.

**Exists now**
- Dynamic Box bodies, compound colliders, materials: `engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L120`; inertia `engine/gpu/RigidMassProperties.cs@a6c914e:L17-L50`.
- Orientation-threshold sensor (sticky, no rearm): `engine/gpu/OrientationSensorDeclaration.cs@a6c914e:L24-L34`.
- Contact trigger by approach speed (for the bell strike): `engine/gpu/ContactTriggerDeclaration.cs@a6c914e:L7-L18`.
- Activation output socket and wiring: `engine/gpu/WorkshopConnections.cs@a6c914e:L8-L10`.

**Missing**
- Hinge with travel limits: Story 10.3.
- Rearmable orientation threshold: unscheduled (generic sensor extension; owner of the sensor family).
- Moving container store with mass and centre-of-mass coupling, spill through the lip: [S416](../invest/decisions.md#s416) advection (S418); unscheduled.
- Free-stream capture by an open mouth: S418; unscheduled.
- Several colliders on one dynamic body (cup walls plus counterweight): unscheduled; also needed by EL-014.

**Element dependencies.** An inflow (EL-003/EL-165 tap or EL-013 nozzle on a reservoir), a discharge receiver (EL-004). Optional CAT-009 Bell (Story 14.2) and CAT-020 Counter (Story 9.2).

## 4. Sources and legacy

- **Requirement row** ([element-025](../requirements.md#element-025)): "Collected liquid shifts a pivoted bucket until it tips and discharges." Outcome: "Subthreshold volume does not emit a tip event." No variants.
- **Integration task** [sequence-task-389](../requirements.md#sequence-task-389): "count real tipping batches". Refinement [S707 water-clock](../invest/refinements.md#s707): "Accumulate finite flow and reset exactly."
- **Research** ([component research](../../component-research.md#water)): "Hinged cup body + finite store + contact trigger"; parameter "tip volume"; "Fills, tips a batch, strikes a chime or pulses a counter, returns". Recipe 9 "Three Splashes": tipping bucket strikes a chime once per dump → sound detector → counter → hatch after three pours.
- **Campaign**: first use 61–70, reuse 71–90, 114, 126–130, 136–150 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)).
- **Decisions**: [S416](../invest/decisions.md#s416) advection; [S257](../invest/decisions.md#s257) typed power versus signal (the tip output is signal only).
- **Legacy.** None found. Searched the Epic 7 deletion scope (`parts/`, `engine/physics/`, `engine/*.cs`, `engine/bridge/`, `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/Campaign`, `reference/`) at `a6c914e` for "tipping", "bucket", "water clock", "water": no element source. The legacy hinge belongs to CAT-034 and is harvested in [CAT-034](CAT-034-impact_lever.md).

**Files harvested:** none.

## 5. Acceptance outline

Follow the [EL-025 row](../requirements.md#element-025).
- **Chrome UI recipe.** In free play place a Finite reservoir (EL-001) with a Tap (EL-165) and Water nozzle (EL-013) aimed into the cup mouth, the tipping bucket below, a Catch basin under the dump side, and a Counter (CAT-020) wired from `TipOut` with the contextual connection UI. Set the tap open; Run.
- **Positive.** The cup fills, tips once it holds the tip volume (1.0 kg at the default), dumps through the lip into the basin, returns upright and the counter advances once per dump; the interval follows the inflow rate.
- **Negative/control.** Close the tap partway through a fill or give the reservoir less than one tip volume: the cup never tips and no occurrence is emitted. Aim the nozzle beside the mouth: nothing is captured.
- **Boundaries.** Inflow during the dump (liquid that misses or overflows is conserved); very slow fill (exactly one occurrence at threshold); `tip_volume` at 0.03125 and 0.0625 m³ (both tip, each below the fill line); a cup held by a blocking body cannot tip and emits nothing; a held-tipped cup does not repeat occurrences.
- **Run/Reset.** Hinge angle, contained volume, sensor armed state and counter restore exactly.
- **Save/Load.** Pose, `tip_volume` and typed links round-trip; out-of-range values reject.
- **Integrations.** Bell strike (CAT-009), counter (CAT-020), recipe "Three Splashes".

## 6. Open questions

1. Tip angle (60°) and rearm angle (10°) for the rearmable threshold: owner decision.
2. Whether the bell strike uses the cup's physical contact only, or the tip occurrence may also drive a bell by signal: owner decision under S257.
3. Single cup with counterweight versus the classic two-chamber seesaw: owner decision (single cup proposed, matching "returns" in the research row).
4. Pivot arms a_c = 0.15 m and a_w = 0.25 m (proposed), which set the compiled counterweight: owner decision.
5. Liquid density ρw = 16 kg/m³ (proposed) is one water-family constant shared with Batch F's EL-001–EL-022 specs: owner decision under S418/S420.
