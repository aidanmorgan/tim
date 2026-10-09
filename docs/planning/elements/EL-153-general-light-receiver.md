# EL-153 · General-light receiver — named-identity spec

Story 7.0 full spec for a named puzzle-element identity. Baseline commit `a6c914e`. Legacy citations use `path@a6c914e:Lstart-Lend` and stay retrievable from git history after the Epic 7 purge. Lengths in metres. The sensing face looks along local −X.

No legacy part implements this identity. The legacy cone-light law and the solar panel's nine-sample face are the nearest mechanisms and are cited as such; every other value is **proposed** with a justification, for the owner to revise.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-153 |
| Name | General-light receiver |
| Type | Optical |
| Anchor | [requirements.md#element-153](../requirements.md#element-153); named entry [named-elements.md#element-153](../invest/named-elements.md#element-153); scope index [todo-291](../requirements.md#todo-291) |
| Owner | S515 |
| Refines CAT | none one-to-one. Extends the cone-sampling mechanism of [CAT-059 solar_panel](CAT-059-solar_panel.md) to a separately supplied contact; distinct from [CAT-038 light_receiver](CAT-038-light_receiver.md), which is laser-only. Source: [CAT-029 flashlight](CAT-029-flashlight.md) / EL-210. |
| Related | EL-154 light-charge receiver (charging sibling); EL-210 flashlight; EL-155 rope-operated light; EL-214 broadband beam detector (narrow-ray sibling) |
| Roadmap story | unscheduled |
| Status | not started |

## 2. Declaration

- **Bodies and shapes:** one static rigid body: opaque cream plate 0.22 × 1.2 × 1.2 m at the origin (**proposed**: the solar panel's 0.22 m depth and 1.2 m height, squared, so torch lessons transfer); opaque navy foot 0.9 × 0.16 × 1.2 m at (0, −0.8, 0) (**proposed**: the solar panel foot). Static `RigidBodyDeclaration` and two box `ColliderDeclaration`s (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L60`, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L91`).
- **Mass and material:** static, zero mass; contact material restitution 1, bounce threshold 0.1 m/s, friction 0.3, as legacy static parts (`reference/cpu/MachinePart.cs@a6c914e:L236-L236`; `engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L54`).
- **Constraints and joints:** none.
- **Typed ports:**
  - Sensing aperture: nine light samples on the front face at x = −0.13, y ∈ {−0.35, 0, 0.35}, z ∈ {−0.35, 0, 0.35}, normal −X, weight 1/9 each (**proposed**: the solar panel's legacy 3 × 3 pattern on a square face, giving partial shadow without a continuum model).
  - Electrical `PowerIn` (Input) at (0, −0.5, 0.5) and `Supply` (Output) at (0, −0.5, −0.5) (**proposed** positions: below the face like the laser receiver's sockets). Socket identities exist (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L9`).
- **Sensors and activation:** irradiance E = Σ over cone emitters and samples of intensity × facing × weight ÷ max(1, d²), counting only samples inside the emitter's cone, within its range, front-facing and unoccluded (legacy cone law). The contact closes (`PowerIn` → `Supply`) when E ≥ threshold and opens when E < 0.9 × threshold (**proposed** hysteresis: the gates' 0.225/0.25 ratio, so a moving shadow edge does not chatter). Light never creates electricity.
- **Work and energy stores:** none (the charging variant is EL-154).
- **Parameters:** `threshold` — f32, range 0.25–24, default 1.0, unit game irradiance (**proposed**: default equals the solar panel's legacy threshold 1, so a 24-intensity torch lights it out to about 4.9 m; 24 is the torch's maximum reading at ≤ 1 m).
- **Cosmetic curves and UI bindings:** gold centre ring eases slate `#556573` → gold `#f7cb52` over 0.125 s smoothstep on committed contact state (**proposed**: the receiver family's legacy indicator); four gold meter marks fill at 25/50/75/100% of threshold (**proposed**: the solar panel's legacy meter).
- **Art (DESIGN.md palette):** cream plate `#fff8e9`; nine warm-cream cells `#fff0a5` (**proposed**: the torch-light ink, distinguishing it from the blue solar cells and from laser targets); navy foot `#293954`; gold socket studs `#f7cb52`.
- **Catalogue and inventory entry:** id `light_sensor`, title "Light sensor", category Optics (**proposed**). Description (**proposed**): "Face it toward a flashlight. Enough light across its cells closes a contact; connect a separate supply." Icon: an original navy line pictogram (to be drawn).

### Variants

One. The charging behaviour named in [sequence-task-398](../requirements.md#sequence-task-398) is EL-154, a separate identity.

## 3. Engine capabilities

Binding shard ([element-02.json](../../coverage/engine/element-02.json)): FiniteLedger, GeometryQuery, OpticalAbsorption, OpticalTransport, SensibleHeat, SignalPropagation, StateTransaction ([element-map row](../general-engine-element-map.md) omits StateTransaction).

**Exists now:** static body, colliders and material (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L60`, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L91`); `PowerIn`/`Supply` sockets (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L11`).

**Missing**
- OpticalTransport cone emission and multi-sample cone reception with shared-proxy occlusion — Story 13.1 (torch) and Story 13.7 (solar sampling). Decision owner **S484** ([decisions](../invest/decisions.md#s484)): S486 optical-commit, S488 interval law (partial occlusion). Refinements S522 cone-visibility and S523 moving-shadow ([refinements](../invest/refinements.md)).
- ElectricalPower switched contact — Story 8.1 plus an Epic 13 slice; S257.
- OpticalAbsorption/SensibleHeat — S489/S543, mode-specific.
- `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`).

**Element dependencies:** EL-210 / CAT-029 flashlight (Story 13.1), CAT-005 battery (Story 8.1), a load (CAT-035 lamp), CAT-066 wall (delivered).

## 4. Sources and legacy

**Sources.** Row [element-153](../requirements.md#element-153): samples actual finite-width illumination over a declared aperture; an occluded flashlight cone and absent independent supply cannot activate output. [todo-291](../requirements.md#todo-291) and [sequence-task-398](../requirements.md#sequence-task-398): responds to the documented source; laser-only reception is not silently treated as flashlight reception. [todo-309](../requirements.md#todo-309): finite-width coverage. Campaign: first use 51–60 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)).

**Legacy** (no part; mechanisms only):
1. Cone emitter and sample records — `engine/LightNetwork.cs@a6c914e:L8-L10`; cone law: range, cone cosine, facing, shared-proxy occlusion excluding emitter and receiver, intensity × facing × weight ÷ max(1, d²), all readings committed together, no reflection or ambient — `engine/LightNetwork.cs@a6c914e:L12-L52`. Carry forward the law; do not carry forward the CPU static solve.
2. Nine equal-weight front samples and threshold 1 — `parts/SolarPanelPart.cs@a6c914e:L8-L12`, `parts/SolarPanelPart.cs@a6c914e:L26-L34`; four-step meter — `parts/SolarPanelPart.cs@a6c914e:L47-L53`. Carry forward as the proposed pattern.
3. Torch source: lens (0.66, 0, 0), range 8, cone cosine 0.9659258 (15°), intensity 24 — `parts/FlashlightPart.cs@a6c914e:L10-L13`. Carry forward (source facts owned by [CAT-029](CAT-029-flashlight.md)).
4. Acceptance facts for cone reception: reversed or out-of-range (x = 7) receiver reads 0; restoring the pose restores the same reading — `CuriousContraptions.tests/LightTests.cs@a6c914e:L116-L166`; a small occluder reduces rather than erases — `CuriousContraptions.tests/LightTests.cs@a6c914e:L210-L224`; readings are insertion-order independent and Reset clears them — `CuriousContraptions.tests/LightTests.cs@a6c914e:L226-L258`. Carry forward.

**Files harvested:** `engine/LightNetwork.cs`, `parts/SolarPanelPart.cs`, `parts/FlashlightPart.cs`, `reference/cpu/MachinePart.cs`, `CuriousContraptions.tests/LightTests.cs`. No legacy level or lesson uses a general-light receiver.

## 5. Acceptance outline

- **Chrome UI recipe:** from the actual drawer place a flashlight with a ball above its button, the light sensor 3 m along the torch axis facing it, a battery and a lamp; wire battery `Supply` → sensor `PowerIn` and sensor `Supply` → lamp `PowerIn`. Run.
- **Positive:** the ball presses the button; the cone covers the cells; the meter fills and the lamp lights.
- **Negative / controls:** a wall between torch and sensor (occluded cone); sensor `PowerIn` unwired (meter fills, lamp off); sensor reversed; sensor beyond 8 m or outside 15°; a laser aimed at the sensor (narrow-ray light does not count — see Open questions).
- **Boundaries:** threshold edge and the 0.9 × release; partial shadow over some cells reduces E; cone edge and range edge.
- **Run/Reset:** Reset clears irradiance, contact and meter. **Save/Load:** threshold, placement and wiring round-trip.
- **Integrations:** EL-210, EL-155 (rope-operated light as source), EL-154. Binding criteria: [element-153](../requirements.md#element-153).

## 6. Open questions

1. Must the receiver also sample narrow laser rays, and in what unit (game optical power versus game irradiance)? Proposed: cone light only until S488 unifies the units.
2. All geometry, threshold, hysteresis, art and catalogue values are proposed; owner to confirm.
3. Is the 0.9 × release hysteresis wanted, or should it match the solar panel's single threshold?
4. Roadmap: unscheduled; propose an Epic 13 story after 13.7.
