# EL-018 · Electronic level switch — named-identity spec

Story 7.0 named-identity spec ([readiness spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations `path@a6c914e:Lstart-Lend` resolve with `git show a6c914e:<path>` after the Epic 7 purge. A value marked **proposed** has no legacy or requirement source; it carries a one-line justification and the owner may revise it.

## Identity

| Field | Value |
| --- | --- |
| EL ID / name / type | EL-018 · Electronic level switch · Water |
| Requirement anchor | [element-018](../requirements.md#element-018); source record [todo-337](../requirements.md#todo-337); integration task [sequence-task-387](../requirements.md#sequence-task-387) |
| Named entry | [element-018](../invest/named-elements.md#element-018); per-element proof owner S439 |
| CAT spec refined | None. Supplied by the Battery ([CAT-005](CAT-005-battery.md), Story 8.1); follows the supplied-contact pattern of the Switch ([CAT-063](../requirements.md#current-cat-063)). |
| Related identities | [EL-017](EL-017-mechanical-float-valve.md) (unsupplied mechanical alternative), [EL-016](EL-016-float.md), [EL-004](EL-004-catch-basin.md) (sensed vessel), [EL-022](EL-022-water-pump.md) (typical load), EL-167 Solenoid tap |
| Roadmap story | Unscheduled; ELEMENT-n row of the [roadmap](../invest/vertical-delivery.md#rolling-playable-roadmap), campaign chapter 7, levels 61–70. Supply prerequisite: Story 8.1. |
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
- **Bodies and shapes.** One static body: a head box 0.30 × 0.25 × 0.25 m with a rim bracket, and a probe rod 0.06 × 1.0 × 0.06 m hanging into the vessel (**proposed**: the 1.0 m sensing span covers a full EL-001 tank and a basin). Colliders: two boxes.
- **Mass and material.** Static, zero mass; shared vessel material.
- **Constraints.** None.
- **Typed ports** (pattern from the legacy sound meter, fact 1):
  - `PowerIn`, Electrical, Input (left of head);
  - `Supply`, Electrical, Output (right of head) — carries the input's power only while the switch state is Wet;
  - `ActivationOut`, Activation, Output (top) — one occurrence on each Dry → Wet transition, only while powered.
  Socket and domain enums already exist (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L11`); `WorkshopPorts.For` has no row for this kind yet (`engine/gpu/WorkshopConnections.cs@a6c914e:L15-L25`).
- **Sensors and activation.** Observes the committed free-surface height h of the liquid store containing the probe tip, sampled at substep endpoints ([component research](../../component-research.md#water), line 18). Closed state enum `LevelSwitchState { Dry, Wet }`. Dry → Wet when h ≥ HighThreshold for DwellTicks; Wet → Dry when h ≤ LowThreshold for DwellTicks (separate thresholds, line 18). Sensing works unpowered; switching power and emitting activation need `PowerIn` supplied — "SignalPropagation alone does not supply ElectricalPower" ([element map](../general-engine-element-map.md)).
- **Work and energy stores.** None; it passes the battery's power (the network owns the energy).
- **Parameters.**

  | Name | Type | Range | Default | Unit | Source |
  | --- | --- | --- | --- | --- | --- |
  | HighThreshold | f32 | 0.05–1.0, step 0.05 | 0.30 | m above the probe tip | thresholds named by component research line 18; values **proposed** (inside the rod span) |
  | LowThreshold | f32 | 0–0.95, step 0.05, and ≤ High − 0.05 | 0.20 | m above the probe tip | **proposed** (0.05 m minimum band prevents chatter) |
  | DwellTicks | u32 | 1–60 | 6 | ticks at 120 Hz | dwell in ticks named by component research line 18; values **proposed** (50 ms rejects ripples, still prompt) |

- **Cosmetic curves and UI bindings.** Level needle on the head ← committed h (works unpowered); output lamp slate `#556573` → gold `#f7cb52` ← supplied Wet state (legacy sound-meter lamp colours, `parts/SoundMeterPart.cs@a6c914e:L43-L47`). Requires a feedback source for scalar level (`engine/gpu/WorkshopCosmetic.cs@a6c914e:L6-L16`).
- **Art.** Cyan head `#66b8c9` with cream dial `#fff8e9` and navy ticks `#293954` (`DESIGN.md@a6c914e:L172-L172`, `DESIGN.md@a6c914e:L151-L151`, `DESIGN.md@a6c914e:L147-L147`), matching the sound meter's look (`DESIGN.md@a6c914e:L72-L72`) (**proposed**: one visual family for supplied meters).
- **Catalogue and inventory entry.** Id `level_switch`, title "Level switch", category "Water", kind `LevelSwitch` (all **proposed**).

**Variants.** The requirements row lists no variants.

## Engine capabilities

Families ([element map](../general-engine-element-map.md); binding [`element-01.json`](../../coverage/engine/element-01.json), consumer `element/element-018`): EnvironmentState, FiniteLedger, FluidAdvection, GeometryQuery, JointConstraint, PressureWork, RigidBodyDynamics, SignalPropagation, StateTransaction, TopologyTransaction.

- **Exists now.** Activation network with typed nodes and edges (`engine/gpu/ActivationNetwork.cs@a6c914e:L7-L12`); Electrical/Supply/PowerIn enum members (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L11`); Run/Reset (`engine/gpu/WorkshopSimulation.cs@a6c914e:L238-L245`).
- **Missing.** ElectricalPower network and supplied-contact routes (Story 8.1; decision [S257](../invest/decisions.md#s257) electrical-port → S270); a level-sampling sensor over a liquid store ([S416](../invest/decisions.md#s416) → S418); a new `ActivationNodeKind` for a level source. Unscheduled.
- **Element dependencies.** CAT-005 Battery; a load (Lamp CAT-035, Motor CAT-042, or EL-022 pump); a vessel (EL-001/EL-004).

## Sources and legacy

- Requirements row: "Dry and unpowered controls cannot switch the output." No variants.
- Element map: "Add separately supplied output contact/level observation". Refinement [S706](../invest/refinements.md#s706) level-switch. Component research: "the electronic contact needs supply" (line 33); recipe Dark at High Tide (line 59).
- **Legacy search.** No level switch. Supplied threshold-sensor analogue (Sound meter):

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | Ports `PowerIn` (Electrical in), `Supply` (Electrical out), `ActivationOut`; one electrical route PowerIn → Supply gated by the boolean threshold state. | `parts/SoundMeterPart.cs@a6c914e:L50-L57` | Carry forward the port set and gated route. |
| 2 | Threshold admitted 0.05–1; hysteresis: once above, it stays above until the level drops below 0.9 × threshold. | `parts/SoundMeterPart.cs@a6c914e:L58-L71` | Carry forward hysteresis as explicit separate Low/High thresholds (component research line 18), not a hidden 0.9 factor. |
| 3 | Active only when above threshold and powered; a rising edge emits one activation only if powered. | `parts/SoundMeterPart.cs@a6c914e:L72-L80` | Carry forward the semantics. Do not carry forward the per-part `PreparePhysics` loop (no element update loop). |

## Acceptance outline

Point of truth: [element-018](../requirements.md#element-018).

- **Chrome recipe.** Clamp a Level switch on a Catch basin rim; wire Battery `Supply` → switch `PowerIn` and switch `Supply` → Lamp `PowerIn` with the wiring tool; fill the basin from a Water tank and Tap.
- **Positive.** The needle rises with the water; at 0.30 m the lamp lights.
- **Negative / control.** Dry: lamp stays dark. Battery wire removed: the needle still rises but the lamp never lights and no activation is emitted (unpowered). Probe outside the basin: never Wet.
- **Boundaries.** Draining the basin below 0.20 m turns the lamp off; between 0.20 and 0.30 m the state holds (hysteresis). A brief splash shorter than DwellTicks does not switch. High < Low + 0.05 rejects at authoring.
- **Run/Reset.** State returns to Dry, needle to 0. **Save/Load.** Thresholds, dwell, pose and wires survive reload.
- **Integrations.** [sequence-task-387](../requirements.md#sequence-task-387): supplied level-switch variant separately verified; Dark at High Tide.

## Open questions

1. Normally-open only (proposed), or also a normally-closed (pump-until-full) mode? A second mode would be a new variant, not in the row.
2. Is the rising-edge `ActivationOut` wanted in addition to the supplied contact?
3. Does a probe sense any liquid store it is inside (proposed) or only the vessel its bracket clamps?
4. ρw = 16 kg/m³ is one water-family constant, aligned with Batch G (EL-023–EL-036, EL-165–EL-172); the owner confirms or replaces it once for the whole family (S416 buoyancy row → S420).
