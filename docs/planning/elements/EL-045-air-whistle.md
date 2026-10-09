# EL-045 · Air whistle — element readiness spec

Story 7.0 Batch H named-identity spec. Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`. Values marked *proposed* have no legacy or requirement source; each carries a one-line justification, stays inside the [game-grade envelope](../../gpu-f32-physics.md#game-grade-envelope) and may be revised by the owner. Box sizes are full extents. Shared acoustic facts A1–A13 are in [EL-044](EL-044-tone-selective-sound-meter.md#shared-acoustic-family-facts); shared sealed-gas facts P1–P20 in [EL-039](EL-039-air-reservoir.md#shared-sealed-gas-family-facts). This spec defines the **sustained emission rule (S1)** that [EL-179](EL-179-continuous-tone-speaker.md) and [EL-052](EL-052-water-tuned-bottle.md) reuse.

## 1. Identity

| Item | Value |
| --- | --- |
| Identity / name | EL-045 · Air whistle |
| Type | Sound |
| Anchor | [requirements.md#element-045](../requirements.md#element-045); scope index [todo-352](../requirements.md#todo-352); [named-elements entry](../invest/named-elements.md#element-045); owner S533 |
| Related | Refines no CAT spec. Airflow sources [CAT-028 Fan](CAT-028-fan.md), [CAT-010 Bellows](CAT-010-bellows.md), [EL-042 Air nozzle](EL-042-air-nozzle.md); gas source via [EL-040](EL-040-pneumatic-release-valve.md)/[EL-038](EL-038-pneumatic-hose.md) (piston "exhaust → whistle", [todo-426](../requirements.md#todo-426)); heard by [CAT-060](CAT-060-sound_meter.md)/[EL-044](EL-044-tone-selective-sound-meter.md). Airflow-excited sibling: [CAT-069 Wind chimes](CAT-069-wind_chimes.md). |
| Roadmap story | unscheduled |
| Status | not started |

## 2. Declaration

| Item | Declaration (value · source) |
| --- | --- |
| Bodies and shapes | Static root body, collider box 0.7 × 0.3 × 0.3 m at the origin **proposed** (a toy whistle large enough to pick, smaller than a Basketball's 0.68 m diameter); navy foot 0.8 × 0.1 × 0.4 m at (0, −0.2, 0) **proposed**; inlet mouth at local (−0.35, 0, 0) facing −X **proposed**. |
| Mass and material | Static; restitution 1, bounce threshold 0.1 m/s, friction 0.3 (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L109-L110`). |
| Constraints | none. |
| Typed ports | Variant-specific inlet (below). No electrical or activation port: only actual flow excites it. |
| Sensors and activation | Flow sensor with separate on/off levels: sounding starts when committed inlet flow ≥ `on_flow` and stops when it falls to ≤ 0.9 × `on_flow` (90% off hysteresis, the Sound meter rule `parts/SoundMeterPart.cs@a6c914e:L64-L71`). Phase is the closed enum `WhistlePhase { Silent, Sounding }`. |
| Work and energy stores | none. Acoustic strength while sounding = min(1, flow / `full_flow`) **proposed** (bounded by the flow that excites it, never above the pulse maximum 1, A4). The whistle takes no work from the flow (stationary receiver, zero receiver work, [finite-gas-foundation](../../finite-gas-foundation.md#airflow-transfer)). |
| Parameters | `tone`: `ToneBand` (A1), default High **proposed** (a small whistle is high-pitched; distinct from the default Mid speaker). Flow thresholds per variant (below). Pattern Omnidirectional **proposed** (a whistle radiates all round, like the bell's omnidirectional pulse, A1). |
| Cosmetic curves and UI bindings | Reed flutter ← committed flow (component-research binding "Reed ← committed flow", [component research](../../component-research.md#sound)); thin translucent wavefronts per emitted pulse, as on the bell ([DESIGN.md Impact bell](../../../DESIGN.md#impact-bell)). Audio voice at the tone's presentation frequency (A11) is presentation-only. |
| Art | Gold `#f7cb52` whistle tube with a cream `#fff8e9` mouthpiece and navy `#293954` window slot and foot; tone marks one/two/three raised navy bars (`parts/SpeakerPart.cs@a6c914e:L101-L102` rule). Geometry **proposed**. |
| Catalogue / inventory | Airflow variant Id `air_whistle`, Title "Air whistle"; gas variant Id `pipe_whistle`, Title "Pipe whistle" **proposed**; Category Sound. Description **proposed**: "Sounds its tone only while enough air actually flows through it. Blocked or still air stays silent." |

**S1 — sustained emission rule** **proposed** (the legacy has only single pulses, A2): while Sounding, the source emits one pulse of its tone at the start of every 0.15 s pulse duration window (back-to-back pulses using the A2 speed, range and duration), each with the current strength. Receivers therefore see a continuous level; a meter triggers once on the rising crossing and stays above threshold. Each pulse is a distinct occurrence; stopping the flow stops new pulses and the last pulse expires by A3.

**Variants** (the requirements row names none; the sources name two inlet contracts — open airflow in [finite-gas-foundation](../../finite-gas-foundation.md#sealed-gas) "open airflow ≠ pressurised tubing", and a piston exhaust line in [todo-426](../requirements.md#todo-426)):

| Variant | Inlet | Flow measure | `on_flow` default / range | `full_flow` |
| --- | --- | --- | --- | --- |
| Airflow | Open-airflow receiver at the mouth: four weighted samples, occluded samples weigh zero ([finite-gas-foundation](../../finite-gas-foundation.md#airflow-transfer)) | committed flow speed along −X at the mouth (m/s) | 4 m/s, 1–12 m/s **proposed** (a third of the 12 m/s reference flow, so a fan at default reach sounds it and a weak puff does not) | 12 m/s (`engine/AirflowNetwork.cs@a6c914e:L24-L24`) |
| Gas | `GasIn` (Gas, Input) at (−0.35, 0, 0), exhausting to ambient through the whistle | committed mass rate through the whistle (kg/s), nozzle law with throat 2 × 10⁻⁵ m² **proposed** (EL-039 P6) | 0.002 kg/s, 0.0005–0.02 kg/s **proposed** (with Air, 293.15 K and ambient back pressure the subsonic rate reaches 0.002 kg/s at about 106 kPa absolute, ≈ 5 kPa gauge: 106 kPa gives u = 86.8 m/s, ρ = 1.220 kg/m³, 0.00212 kg/s; it falls silent at 0.9 × 0.002 = 0.0018 kg/s, about 105 kPa — so a tank sounds it until it is nearly empty) | 0.01 kg/s **proposed** (≈ 212 kPa absolute; the flow chokes above 191.8 kPa, so full strength needs a well-charged tank) |

## 3. Engine capabilities

Families ([element map row](../general-engine-element-map.md); [binding](../../coverage/engine/element-01.json), proof owner S533): AcousticPropagation, AerodynamicDrag, EnvironmentState, FiniteLedger, GasState, GeometryQuery, JointConstraint, PressureWork, RigidBodyDynamics, SignalPropagation (+ StateTransaction).

**Exists now**
- Static boxes: `engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L58`, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L90`.

**Missing**
- Open-airflow receiver samples and occlusion — Story 12.2 (CAT-028 Fan).
- Gas inlet and nozzle flow — [EL-039 §3](EL-039-air-reservoir.md#3-engine-capabilities); [S470](../invest/decisions.md#s470) (S697 open versus sealed decides the two inlets).
- AcousticPropagation and sustained emission (S1) — Stories 14.1–14.3 for pulses; S1 is new under [S528](../invest/decisions.md#s528) (S529 event identity).
- Flow-threshold sensor with hysteresis on the worker — new sensor binding.

**Dependencies.** A flow source (Fan, Bellows, Air nozzle, or tank + valve); a Sound meter to observe the sound.

## 4. Sources and legacy

- Requirement row [element-045](../requirements.md#element-045): "Actual gas flow excites an acoustic source"; outcome "Blocked or absent flow produces no sound event". Scope index [todo-352](../requirements.md#todo-352): "a whistle needs airflow"; [todo-378](../requirements.md#todo-378) "One Breath (stored air/piston/whistle)".
- Named entry [element-045](../invest/named-elements.md#element-045), owner S533. Component research: "Airflow receiver + sustained acoustic source | flow threshold, release threshold | Sounds above the flow threshold; open airflow ≠ pressurised tubing | Reed ← committed flow".
- Legacy: no whistle part, level or lesson. Applicable facts: acoustic model A1–A13; airflow emitter shape and 12 m/s reference flow (`engine/AirflowNetwork.cs@a6c914e:L11-L13`, `engine/AirflowNetwork.cs@a6c914e:L24-L24`); hysteresis pattern (`parts/SoundMeterPart.cs@a6c914e:L64-L71`). The legacy Wind chimes ring only on real tube contact, not on airflow overlap (harvested by [CAT-069](CAT-069-wind_chimes.md)); the whistle differs by sounding from flow itself.
- **Files harvested:** `engine/AirflowNetwork.cs`, `parts/SoundMeterPart.cs`, `parts/SpeakerPart.cs` (tone marks); acoustic files as listed in EL-044.

## 5. Acceptance outline

Acceptance authority: [element-045](../requirements.md#element-045), [acoustic profile](../invest/profiles.md#acoustic), [pneumatics profile](../invest/profiles.md#pneumatics).

- **Construction (actual Chrome UI).** Airflow variant: Fan aimed at the whistle mouth 2 m away; supplied Sound meter 3 m from the whistle. Gas variant: charged tank → release valve → hose → `GasIn`; Switch → valve.
- **Positive.** Running fan (or opened valve): reed flutters, wavefronts expand, the meter needle rises and triggers once.
- **Negative / control.** Wall between fan and whistle, fan facing away, fan stopped, closed valve or empty tank: no sound event at all (outcome). A sound meter with no whistle in the airflow reads 0 (the fan itself is silent).
- **Boundaries.** Flow just above `on_flow` sounds; dropping below 0.9 × `on_flow` silences (gas variant: a draining tank goes quiet near 105 kPa); strength caps at 1 at `full_flow`.
- **Run/Reset.** Reset silences, clears pulses and restores the flow source.
- **Save/Load.** Variant id, `tone`, `on_flow` and connections round-trip; muted and audible outcomes identical.
- **Integrations.** Acoustic integration task [sequence-task-407](../requirements.md#sequence-task-407) ("a whistle needs airflow"); spring-return piston row [todo-426](../requirements.md#todo-426) ("exhaust → whistle") under [sequence-task-393](../requirements.md#sequence-task-393); combination [todo-378](../requirements.md#todo-378) "One Breath" (stored air/piston/whistle); interaction processes [IX-12 acoustic propagation](../requirements.md#interaction-12), [IX-11 aerodynamic drag](../requirements.md#interaction-11) (airflow inlet) and [IX-08 fluid advection](../requirements.md#interaction-08) (gas inlet). Campaign: "whistle" is named in the sound row of [campaign-element-coverage](../requirements.md#campaign-element-coverage), first use 71–80 (reuse 81–100, 117–125, 136–150).

## 6. Open questions

1. **Inlet contract.** Whether both the airflow and gas inlets are required, and as one element or two catalogue entries: owner decision under S697.
2. **Sustained emission.** Back-to-back pulses (S1, proposed) versus a single long-lived occurrence with start and stop: owner decision under S529.
3. **Flow drain.** Whether a sounding whistle should remove a small amount of flow energy (receiver work): owner decision.
