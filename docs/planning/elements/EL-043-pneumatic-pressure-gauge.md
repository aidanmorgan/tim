# EL-043 · Pneumatic pressure gauge — element readiness spec

Story 7.0 Batch H named-identity spec. Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`. Values marked *proposed* have no legacy or requirement source; each carries a one-line justification, stays inside the [game-grade envelope](../../gpu-f32-physics.md#game-grade-envelope) and may be revised by the owner. Box sizes are full extents. Shared sealed-gas facts P1–P20, the `Gas` domain and the "Air" material are in [EL-039](EL-039-air-reservoir.md#shared-sealed-gas-family-facts).

## 1. Identity

| Item | Value |
| --- | --- |
| Identity / name | EL-043 · Pneumatic pressure gauge |
| Type | Pneumatic |
| Anchor | [requirements.md#element-043](../requirements.md#element-043); scope index [todo-376](../requirements.md#todo-376); [named-elements entry](../invest/named-elements.md#element-043); owner S478 |
| Related | Refines no CAT spec. Pattern sibling of [CAT-060 Sound meter](CAT-060-sound_meter.md) (needle without supply, separately supplied contact with hysteresis); reads [EL-039](EL-039-air-reservoir.md) or any gas node through [EL-038](EL-038-pneumatic-hose.md). |
| Roadmap story | unscheduled |
| Status | not started |

## 2. Declaration

| Item | Declaration (value · source) |
| --- | --- |
| Bodies and shapes | Static root body, collider box 0.9 × 0.9 × 0.35 m at (0, 0.15, 0) **proposed** (a round dial housing smaller than the 1.6 m Sound meter cabinet); navy foot 1.0 × 0.14 × 0.5 m at (0, −0.37, 0) **proposed**. |
| Mass and material | Static; restitution 1, bounce threshold 0.1 m/s, friction 0.3 (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L109-L110`). |
| Constraints | none. |
| Typed ports | `GasIn` (Gas, Input) tap at (0, −0.3, −0.18) **proposed**; `PowerIn` (Electrical, Input) at (−0.55, −0.3, 0) and `Supply` (Electrical, Output) at (0.55, −0.3, 0) **proposed** (lower-left/right electrical pair, the Sound meter layout at (∓0.92, −0.5, 0), harvest M1). |
| Sensors and activation | Reading = committed absolute pressure of the node joined to `GasIn`, read afresh on every committed tick, including the first tick after Run or Reset; the gauge keeps no remembered reading of its own. With no `GasIn` connection the reading is ambient (EL-039 P15). The reading needs no electrical supply ("independently of electrical controls"). Threshold state uses separate on/off levels: on when p ≥ `threshold`, off when p ≤ 0.9 × `threshold` (the Sound meter's 90% off hysteresis, harvest M2); at Run start and after Reset it is initialised from the first reading (on iff p ≥ `threshold`), never carried over from the previous run. The contact `PowerIn` → `Supply` is closed only while the threshold state is on and `PowerIn` is supplied; it never creates supply. |
| Work and energy stores | none; the tap draws no gas (zero internal volume) **proposed** (a gauge must not deplete what it measures). |
| Parameters | `threshold`: f32 absolute, 120–600 kPa, default 300 kPa **proposed** (between ambient and the 400 kPa default charge, so one stroke can cross it downward). Hysteresis factor fixed 0.9 (harvest M2). Parameter keys: closed enum `PressureGaugeParameter { Threshold }`. |
| Cosmetic curves and UI bindings | Needle ← committed gauge pressure (p − ambient), 0 at rest to full scale at 500 kPa gauge **proposed**, eased by the animation worker like the Sound meter needle (harvest M3); output lamp slate `#556573` → gold `#f7cb52` follows the supplied closed contact, 0.1 s SmoothStep (harvest M3). A cream threshold tick marks `threshold` on the dial. |
| Art | Cream `#fff8e9` dial face with navy `#293954` ticks and a gold `#e8b764` needle in a cyan `#66b8c9` round bezel; navy foot; gold `#f7cb52` port spheres r 0.075 ([DESIGN.md Sound meter](../../../DESIGN.md#sound-meter) pattern: needle shows the reading without supply, lamp shows supplied output). Geometry **proposed**. |
| Catalogue / inventory | Id `pressure_gauge`, Title "Pressure gauge", Category `Air` **proposed**. Description **proposed**: "The needle shows the air pressure where it is connected, with or without electricity. Supply it to pass power while pressure stays above the mark." |

**Variants.** The requirements row names no variant; the base declaration is the only required mode.

## 3. Engine capabilities

Families ([element map row](../general-engine-element-map.md); [binding](../../coverage/engine/element-01.json), proof owner S478): EnvironmentState, FiniteLedger, FluidAdvection, GasState, GeometryQuery, JointConstraint, PressureWork, RigidBodyDynamics, TopologyTransaction (+ StateTransaction).

**Exists now**
- Static box body/collider: `engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L58`, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L90`.
- `PowerIn`/`Supply` socket enum values and Electrical domain (not admitted): `engine/gpu/WorkshopConnections.cs@a6c914e:L8-L9`.

**Missing**
- Gas domain and a pressure read of a gas node — [EL-039 §3](EL-039-air-reservoir.md#3-engine-capabilities); owners [S470](../invest/decisions.md#s470), [S257](../invest/decisions.md#s257).
- Supplied contact in the electrical network driven by a scalar threshold state — Story 8.1 network ([CAT-005](CAT-005-battery.md) N12–N14: contact condition kinds).
- Scalar needle cosmetic source (committed continuous value) — current feedback sources are discrete only (`engine/gpu/WorkshopCosmetic.cs@a6c914e:L7-L7`).

**Dependencies.** A gas node to read (EL-039, charged by EL-037 or authored); Battery (CAT-005) and a supplied load for the contact.

## 4. Sources and legacy

- Requirement row [element-043](../requirements.md#element-043): "Measures local gas pressure independently of electrical controls"; outcome "Empty reservoir reads ambient rather than a stale charged state". No variants listed.
- Named entry [element-043](../invest/named-elements.md#element-043), owner S478. Gas design: "Sensor on a gas node | On/Off thresholds | Needle reads pressure; switches a supplied load at threshold | Needle ← committed pressure"; sensors use "threshold with separate On/Off hysteresis, sampled at substep endpoints".

| # | Fact | Source | Disposition |
| --- | --- | --- | --- |
| M1 | Meter sockets: `PowerIn` (−0.92, −0.5, 0), `Supply` (0.92, −0.5, 0), contact route PowerIn → Supply closed while the boolean threshold state is true. | `parts/SoundMeterPart.cs@a6c914e:L50-L57` | carry forward the pattern |
| M2 | Hysteresis: once above, the state stays on while level > 0.9 × threshold; from below it needs level ≥ threshold. | `parts/SoundMeterPart.cs@a6c914e:L64-L71` and `CuriousContraptions.tests/SoundMeterTests.cs@a6c914e:L55-L71` | carry forward |
| M3 | Needle follows the committed level with an eased response; lamp slate → gold 0.1 s SmoothStep on owner-active. | `parts/SoundMeterPart.cs@a6c914e:L40-L47` | carry forward as declared bindings |
| M4 | Reset clears the meter's reading and threshold state, because its source (sound) is silent after Reset. | `CuriousContraptions.tests/SoundMeterTests.cs@a6c914e:L49-L51` | carry forward the rule "no state survives Reset"; for the gauge the restored source is the tank's authored charge, so the first reading after Reset is that authored pressure (ambient for an empty or unconnected tank) |

- **Files harvested:** `parts/SoundMeterPart.cs`, `CuriousContraptions.tests/SoundMeterTests.cs` (pattern facts); gas files as listed in EL-039.

## 5. Acceptance outline

Acceptance authority: [element-043](../requirements.md#element-043), [pneumatics profile](../invest/profiles.md#pneumatics).

- **Construction (actual Chrome UI).** Charged Air tank; Pressure gauge; Connect tank `GasOut` → gauge `GasIn` (choose the socket pair); Battery → gauge `PowerIn`; gauge `Supply` → a supplied load (e.g. Motor or Powered gate once available).
- **Positive.** Needle shows the charge with no battery connected; with the battery, the load runs while pressure stays above the mark.
- **Negative / control.** Vent the tank through an exhaust valve: the needle falls to ambient and stays there, never showing the earlier charge (outcome); a disconnected gauge reads ambient; with no `PowerIn` supply the load never runs even above threshold.
- **Boundaries.** Rising exactly to `threshold` closes; falling to 0.9 × `threshold` opens; a value between keeps the previous state.
- **Run/Reset.** Reset restores the tank's authored charge; on the first committed tick the gauge reads that restored node afresh, so the needle shows the authored charge (not the vented value from the run, and not a remembered one), and the threshold state and contact are recomputed from it.
- **Save/Load.** `threshold` and connections round-trip; the reading is not persisted.
- **Integrations.** Pneumatic integration task [sequence-task-393](../requirements.md#sequence-task-393) (reservoir charge and depletion that the gauge displays; "Show stroke and reservoir state" in [todo-426](../requirements.md#todo-426)); interaction processes [IX-40 gas state evolution](../requirements.md#interaction-40), [IX-07 signal propagation](../requirements.md#interaction-07) (threshold condition) and [IX-06 electrical power transfer](../requirements.md#interaction-06) (supplied contact). Campaign: the pneumatic row of [campaign-element-coverage](../requirements.md#campaign-element-coverage) reserves first use 71–80 (reuse 91–100, 136–150); the gauge is not named in that row, so its lesson slot is still unassigned.

## 6. Open questions

1. **Contact scope.** The named entry says only "measures"; the gas design adds a supplied threshold contact. Whether the contact is part of EL-043 or a separate pressure-switch element: owner decision.
2. **Absolute or gauge display.** The dial shows gauge pressure (proposed) while thresholds are absolute: owner decision on a single convention.
