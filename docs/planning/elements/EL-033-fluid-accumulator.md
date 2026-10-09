# EL-033 · Fluid accumulator named-identity spec

This is the Story 7.0 full spec for named identity EL-033. The baseline is commit `a6c914e`; every citation uses `path@a6c914e:Lstart-Lend`. Values without a source are marked **proposed**, each with a one-line justification; the owner may revise them.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-033 |
| Name | Fluid accumulator |
| Type | Water |
| Anchor | [requirements.md#element-033](../requirements.md#element-033); [named-elements.md#element-033](../invest/named-elements.md#element-033); scope index [todo-340](../requirements.md#todo-340) |
| Proof owner | S454 |
| CAT spec refined | none. Related: [CAT-062 Spring](CAT-062-spring.md) (Story 6.4 slider and soft spring, Story 6.5 preload store) |
| Related identities | EL-022 Water pump and EL-002 Header tank (chargers); [EL-032 Pressure meter](EL-032-pressure-meter.md); the hydraulic piston ([todo-428](../requirements.md#todo-428)) as a load; EL-039 Air reservoir is the separate pneumatic store |
| Roadmap story | unscheduled |
| Status | not started (no `WorkshopPartKind` member, `engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`) |

## 2. Declaration

- **Bodies and shapes.** A static cylinder housing (part root) represented by Box colliders, outer 0.5 × 1.0 × 0.5 m, interior 0.4 × 0.8 × 0.4 m. A dynamic piston plate: Box 0.4 × 0.05 × 0.4 m inside the housing, above the liquid chamber. **Proposed**: a 0.16 m² piston and 0.5 m stroke give a visible travel at bench heads of 1–2.5 m.
- **Mass and material.** Piston 0.5 kg (**proposed**: a visibly moving light plate); its 4.905 N weight is included in chamber pressure and stored gravitational energy. Housing: the shared static water-vessel material, restitution 0.12, bounce threshold 0.1 m/s, friction 0.3, rolling resistance 0 (**proposed** reuse of the Receiver material, `engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L57-L63`, as Batch F's vessels use).
- **Liquid.** Chamber volume = piston area × displacement, 0 to 0.08 m³. Proposed height datum: the piston underside at zero stroke is level with WaterMouth; the liquid column height is x, so mouth pressure exceeds piston-face pressure by ρw·g·x. Family density ρw = 16 kg/m³ (**proposed**, shared with Batch F; see [EL-023](EL-023-archimedes-screw.md)), so 157 Pa per metre of head.
- **Constraints.** One prismatic joint piston ↔ housing along local Y, travel 0–0.5 m, plus a soft spring pushing the piston down toward 0 (Story 6.4 unified TGS soft spring). Connected collision disabled.
- **Typed ports.** `WaterMouth` (Water, bidirectional, local (0, −0.5, 0), **proposed**; the water family's single-mouth name, as in Batch F): the only opening, a standard mouth; charge and discharge both pass through it. No signal or power port.
- **Sensors and activation.** None. Stored volume, chamber pressure and stored energy are observable (EL-032; [S730 store-observability](../invest/refinements.md#s730)).
- **Work and energy stores.** At static balance, piston-face pressure p_face = (k·x + m_piston·g)/A; mouth pressure p_mouth = p_face + ρw·g·x. Stored recoverable energy relative to empty is E = ½·k·x² + m_piston·g·x + ½·ρw·g·A·x² (spring, piston gravity, liquid gravity respectively). With defaults and x = 0.5 m, p_face = 430.66 Pa, p_mouth = 509.14 Pa (3.244 m of head), and E = 16 + 2.4525 + 3.1392 = 21.59 J. Intake requires upstream mouth pressure above this pressure plus flow losses; discharge returns at most stored energy and lowers E. No free recharge.
- **Parameters.**

  | Name | Type | Range | Default | Unit | Source / justification |
  | --- | --- | --- | --- | --- | --- |
  | `spring_rate` | f32 | 32–512 | 128 | N/m | **proposed**: a header surface 1 m above the mouth (156.96 Pa) charges it to x = (156.96 × 0.16 − 4.905)/(128 + 156.96 × 0.16) ≈ 0.132 m, 0.0211 m³ and 1.98 J, about 26% of the stroke, leaving room for a pump to charge it further |
  | `initial_charge` | f32 | 0–0.08, step 1/64 | 0 | m³ | **proposed**: authored levels may start charged; the charge's energy counts as an initial store |

- **Cosmetic curves and UI bindings.** Piston and a visible coil follow the committed slider position ("Bladder ← stored energy", [component research](../../component-research.md#water)); a gold gauge band on the housing marks charge level. No easing.
- **Art.** Cream housing `#fff8e9`, restrained cyan liquid window `#66b8c9` below the piston, silver-grey coil and gold piston cap `#f7cb52`, navy foot `#293954` ([DESIGN colour system](../../../DESIGN.md#colour-system)). **Proposed**.
- **Catalogue and inventory entry.** Id `fluid_accumulator`, title "Fluid accumulator", category Water (**proposed**). Counted allowance (`engine/gpu/WorkshopInventory.cs@a6c914e:L9-L23`).

**Variants.** The requirements row lists no variants; EL-033 is one declaration. A gas-bladder accumulator would need GasState, which the map row does not list.

## 3. Engine capabilities

Families ([element-map row](../general-engine-element-map.md), [coverage binding](../../coverage/engine/element-01.json) `element-033`): ElasticStorage, EnvironmentState, FiniteLedger, FluidAdvection, GeometryQuery, JointConstraint, PressureWork, RigidBodyDynamics, StateTransaction (coverage only), TopologyTransaction.

**Exists now**
- Static and dynamic Box bodies: `engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L120`.
- A finite energy-store declaration pattern (initial energy, bounded 0–200 J): `engine/gpu/ContactWorkDeclaration.cs@a6c914e:L21-L36`.

**Missing**
- Prismatic joint and soft spring (ElasticStorage): Story 6.4; preload store pattern Story 6.5.
- Pressure acting on a slider face from a liquid chamber (PressureWork): [S416 pressure-work](../invest/decisions.md#s416) → S419 ("the piston moves under head, stalls against a blocked load and stops when the supply is depleted"); unscheduled.
- Liquid chamber store and port flow (S418): unscheduled.

**Element dependencies.** A charger (EL-002 Header tank or EL-022 Water pump), a load or receiver (hydraulic piston, EL-004 Catch basin), EL-019 Water check valve to hold a charge.

## 4. Sources and legacy

- **Requirement row** ([element-033](../requirements.md#element-033)): "Stores a finite fluid charge against a compliant boundary." Outcome: "Discharging lowers stored energy; no free recharge." No variants.
- **Integration task** [sequence-task-531](../requirements.md#sequence-task-531): "Finite absorption/storage ... can be observed before it produces an action. Accumulators ... remain individually tracked".
- **Research** ([component research](../../component-research.md#water)): "Finite hydraulic energy store with spring/bladder"; parameter "capacity"; "Stores and returns bounded hydraulic energy"; "Bladder ← stored energy".
- **Envelope** ([game-grade envelope](../../gpu-f32-physics.md#game-grade-envelope)): "No free energy: ΔK_system ≤ E_store".
- **Campaign**: first use 81–90 (chapter 9 "Stored Potential"), reuse 91–100, 131–140, 146–150 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)).
- **Legacy.** None found. Searched the Epic 7 deletion scope at `a6c914e` for "accumulator", "hydraulic", "water": no element source. The legacy spring and slider are harvested in [CAT-062](CAT-062-spring.md).

**Files harvested:** none.

## 5. Acceptance outline

Follow the [EL-033 row](../requirements.md#element-033).
- **Chrome UI recipe.** In free play place a Header tank (EL-002) on a block, a Manual tap and Water check valve (EL-019) in a pipe to the accumulator `WaterMouth`, then a second branch through another Manual tap to a Catch basin. Place a Pressure meter (EL-032) on a T. Run with the charging tap open and the discharge tap closed; then reverse the taps in a second Run from a saved charged state.
- **Positive.** The piston rises as the accumulator charges; with the maintained header surface 1 m above the mouth it settles near 0.132 m (about 26% charge), with mouth pressure 156.96 Pa and piston-face pressure about 136.24 Pa; opening the discharge returns liquid to the basin, the piston falls and the reading drops.
- **Negative/control.** With the charger empty or its tap closed, the accumulator never charges. After discharge it does not recharge on its own.
- **Boundaries.** Full stroke (mouth pressure 509.14 Pa, reached only with about 3.244 m of head or a pump; no further intake, the source sees a closed end); head below 0.1953 m (the piston-weight threshold, mouth pressure 30.66 Pa) gives no entry; discharged energy ≤ stored energy; spring at minimum and maximum `spring_rate`.
- **Run/Reset.** Piston position, chamber volume and stored energy restore exactly.
- **Save/Load.** `spring_rate`, `initial_charge`, pose and links round-trip.
- **Integrations.** Hydraulic piston lift, EL-022 pump charging, EL-032 pressure reading.

## 6. Open questions

1. Spring piston (proposed) versus a gas bladder (would add GasState, S471): owner decision.
2. Whether `initial_charge` is allowed in player inventory or only in authored fixtures: owner decision.
3. Liquid density ρw = 16 kg/m³ (proposed) is one water-family constant shared with Batch F's EL-001–EL-022 specs: owner decision under S418/S420.
4. Shared static water-vessel material (restitution 0.12, friction 0.3, proposed): owner decision.
