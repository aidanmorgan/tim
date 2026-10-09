# EL-167 · Solenoid tap named-identity spec

This is the Story 7.0 full spec for named identity EL-167. The baseline is commit `a6c914e`; every citation uses `path@a6c914e:Lstart-Lend`. Values without a source are marked **proposed**, each with a one-line justification; the owner may revise them.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-167 |
| Name | Solenoid tap |
| Type | Water |
| Anchor | [requirements.md#element-167](../requirements.md#element-167); [named-elements.md#element-167](../invest/named-elements.md#element-167); scope source [campaign-element-coverage](../requirements.md#campaign-element-coverage) |
| Proof owner | S460 |
| CAT spec refined | none. It refines [EL-003 Tap](EL-003-tap.md) as the supplied, commanded variant. Related: [CAT-005 Battery](CAT-005-battery.md) (supply), [CAT-013 Both gate](CAT-013-both_gate.md) (separate condition and supply sockets precedent) |
| Related identities | [EL-165 Manual tap](EL-165-manual-tap.md), [EL-166 Mechanically actuated tap](EL-166-mechanically-actuated-tap.md); EL-018 Electronic level switch (a typical command source) |
| Roadmap story | unscheduled |
| Status | not started (no `WorkshopPartKind` member, `engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`) |

## 2. Declaration

The three tap variants (EL-165–EL-167) share EL-003's tap body, port names and opening step (**proposed** cross-batch alignment): body 0.3 × 0.25 × 0.25 m, ports `WaterInlet` and `WaterOutlet` (the water family's names, as in Batch F), opening step 0.125. Each variant keeps its own catalogue id. The solenoid tap's only stops are fully closed (0) and fully open (1), both on that step.

- **Bodies and shapes.** A static inline valve body (part root): Box 0.3 × 0.25 × 0.25 m with a cylindrical coil housing on top (Box 0.16 × 0.18 × 0.16 m art and collider). An internal plunger body: Box 0.06 × 0.08 × 0.06 m on a vertical slider, travel 0.03 m. **Proposed**: the coil reads as a supplied device beside the manual handle.
- **Mass and material.** Plunger 0.05 kg (**proposed**: small, fast travel). Valve body: the shared static water-vessel material, restitution 0.12, bounce threshold 0.1 m/s, friction 0.3, rolling resistance 0 (**proposed** reuse of the Receiver material, `engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L57-L63`, as Batch F's water fixtures use).
- **Liquid.** Passage 0.001 m³; family density ρw = 16 kg/m³ (**proposed**, shared with Batch F; see [EL-023](EL-023-archimedes-screw.md)).
- **Constraints.** One prismatic joint plunger ↔ body along local Y, travel 0 (closed) to 0.03 m (open), with a return spring toward closed (Story 6.4 slider and soft spring). Normally closed.
- **Typed ports.**

  | Port | Domain | Direction | Local position (m) | Notes |
  | --- | --- | --- | --- | --- |
  | `WaterInlet` | Water | Input | (−0.15, 0, 0) | Standard water mouth (0.10 m bore) |
  | `WaterOutlet` | Water | Output | (0.15, 0, 0) | Standard water mouth or spout |
  | `ActivationIn` | Activation | Input | (0, 0.25, −0.08) | Command; existing socket |
  | `PowerIn` | Electrical | Input | (0, 0.25, 0.08) | Supply; existing socket |

  Signal sockets exist (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L10`); positions and water ports **proposed**. Command and supply are distinct sockets, as for the Both gate ([DESIGN supplied electrical logic](../../../DESIGN.md#supplied-electrical-logic)).
- **Sensors and activation.** None emitted. The valve opens only while the command is true **and** the supply delivers power; "signal true alone cannot actuate" ([element map](../general-engine-element-map.md)).
- **Work and energy stores.** None of its own. While commanded and supplied, the coil draws `coil_power` from the supply and drives the plunger to open against its return spring within 0.1 s (**proposed**: a visible click, not instant). Losing either condition releases the plunger; the spring closes it. `opening` = plunger travel / 0.03 m. Flow follows EL-003's law as in [EL-165](EL-165-manual-tap.md): Q = Cd·s·A·√(2·g·Δh), Cd 0.6, A = 0.0314 m², about 0.083 m³/s fully open at 1 m head.
- **Parameters.**

  | Name | Type | Range | Default | Unit | Source / justification |
  | --- | --- | --- | --- | --- | --- |
  | `coil_power` | f32 | fixed | 6 | W | **proposed**: a small fraction of the 120 W motor budget used in the conveyor tests, so one battery can hold several valves |

- **Cosmetic curves and UI bindings.** The plunger and a small gold indicator ring follow the committed slider position; the coil glows faintly only while supplied (activation feedback source `Activation`, `engine/gpu/WorkshopCosmetic.cs@a6c914e:L7-L19`); a cyan stream follows committed flow.
- **Art.** Cream valve body `#fff8e9`, cyan coil housing `#66b8c9` with a gold ring `#f7cb52`, navy foot `#293954`; the command socket is a raised bar and the supply socket a gold ring, so they are distinguishable without colour. **Proposed**.
- **Catalogue and inventory entry.** Id `solenoid_tap`, title "Solenoid tap", category Water (**proposed**; variant-specific id). Counted allowance (`engine/gpu/WorkshopInventory.cs@a6c914e:L9-L23`).

**Variants.** EL-167 is the solenoid variant of the tap family; its requirements row lists no further variants.

## 3. Engine capabilities

Families ([element-map row](../general-engine-element-map.md), [coverage binding](../../coverage/engine/element-02.json) `element-167`): EnvironmentState, FiniteLedger, FiniteWorkActuation, FluidAdvection, GeometryQuery, JointConstraint, PressureWork, RigidBodyDynamics, SignalPropagation, StateTransaction (coverage only), TopologyTransaction; plus a declared ElectricalPower supply (map row "Source-specific composition").

**Exists now**
- `ActivationIn` and `PowerIn` sockets, Activation and Electrical domains, typed wiring: `engine/gpu/WorkshopConnections.cs@a6c914e:L8-L10`.
- Activation signal propagation from switches and delays to inputs (current Workshop activation network).

**Missing**
- ElectricalPower supply network with per-input availability: Story 8.1 ([CAT-005](CAT-005-battery.md)); finite battery capacity is an open owner decision there.
- Prismatic slider with return spring: Story 6.4.
- Supplied actuator that moves a joint only when command and supply hold (FiniteWorkActuation): unscheduled (pattern shared with CAT-051 Powered gate, Story 8.2).
- Valve node and head (S418/S419): owner [S416](../invest/decisions.md#s416); unscheduled.

**Element dependencies.** CAT-005 Battery (Story 8.1); a command source such as CAT-063 Switch, CAT-002 Ball detector or EL-018 level switch; a supply and route as EL-165.

## 4. Sources and legacy

- **Requirement row** ([element-167](../requirements.md#element-167)): "Independent supply moves a valve on a control input." Outcome: "A true command without supply cannot open the valve." No variants.
- **Map row**: "Add declared ElectricalPower supply for solenoid work; signal true alone cannot actuate."
- **Research** ([component research](../../component-research.md#water)): "actuated variants need their lever or supply, never free water"; recipe 11 "Dark at High Tide" uses receiver control of a supplied pump, the same command-plus-supply pattern.
- **Decisions**: [S257 electrical-port](../invest/decisions.md#s257): source/load/control roles "without conflating signal with power".
- **Family contract** [profiles#electrical](../invest/profiles.md#electrical): "a true signal alone powers nothing".
- **Campaign**: first use 61–70 with individually exercised construction objectives; reuse 71–90, 114, 126–130, 136–150 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)).
- **Legacy.** None found. Searched the Epic 7 deletion scope at `a6c914e` for "solenoid", "tap", "valve", "water": no element source. Legacy battery/supply behaviour is harvested in [CAT-005](CAT-005-battery.md).

**Files harvested:** none.

## 5. Acceptance outline

Follow the [EL-167 row](../requirements.md#element-167).
- **Chrome UI recipe.** In free play place a Finite reservoir, the solenoid tap on its outlet, a gutter and a Catch basin; place a Battery and an impact Switch. Wire Battery `Supply` → tap `PowerIn` and Switch `ActivationOut` → tap `ActivationIn` with the contextual connection UI; Run and roll a ball onto the switch.
- **Positive.** With supply connected, the switch command opens the valve; flow runs and the basin fills; when the command ends the spring closes it.
- **Negative/control.** Command true but no battery wired (or the battery disabled): the valve stays closed. Battery wired but no command: closed. Wiring the switch into `PowerIn` is refused at connection time.
- **Boundaries.** Supply lost while open (closes); command pulses shorter than the 0.1 s travel (partial opening, then close); an open valve with no reservoir emits nothing.
- **Run/Reset.** Plunger position, volumes and supply state restore exactly.
- **Save/Load.** Pose, water links and typed Activation and Electrical links round-trip; unknown sockets reject.
- **Integrations.** CAT-005 Battery, EL-018 level switch closing the tap at a high level, logic gates (Epic 9).

## 6. Open questions

1. Normally closed (proposed) versus a selectable normally-open behaviour, which would be a new variant: owner decision.
2. Coil power (6 W proposed) and how it debits a finite battery once Story 8.1 settles capacity: owner decision.
3. Whether EL-003 Tap ships as its own catalogue entry alongside the variant entries, and the shared body/ports/step alignment with EL-003 (Batch F): owner decision.
4. Liquid density ρw = 16 kg/m³ (proposed) is one water-family constant shared with Batch F's EL-001–EL-022 specs: owner decision under S418/S420.
5. Shared static water-vessel material (restitution 0.12, friction 0.3, proposed): owner decision.
