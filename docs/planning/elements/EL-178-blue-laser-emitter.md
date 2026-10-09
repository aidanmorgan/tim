# EL-178 · Blue laser emitter — named-identity spec

Story 7.0 full spec for a named puzzle-element identity. Baseline commit `a6c914e`. Legacy citations use `path@a6c914e:Lstart-Lend` and stay retrievable from git history after the Epic 7 purge. Lengths are legacy scene units, which the current engine treats as metres (`MetreVector`). Local +X is the beam direction.

The channel lasers (EL-176 to EL-178) are separate identities from the amber [CAT-036 laser](CAT-036-laser.md). Body, ports, enable latch, range and lens curve are the legacy amber laser's (sourced); channel power and channel art are **proposed**. The full legacy harvest is in [EL-176](EL-176-red-laser-emitter.md#4-sources-and-legacy).

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-178 |
| Name | Blue laser emitter |
| Type | Optical |
| Anchor | [requirements.md#element-178](../requirements.md#element-178); named entry [named-elements.md#element-178](../invest/named-elements.md#element-178); source record [campaign-element-coverage](../requirements.md#campaign-element-coverage) |
| Owner | S494 |
| Refines CAT | none one-to-one; extends [CAT-036 laser](CAT-036-laser.md). |
| Related | EL-176, EL-177 (sibling channels); EL-145 blue filter; EL-148 blue receiver; EL-150, EL-151, EL-152 mixed receivers; EL-213 combiner |
| Roadmap story | unscheduled (Story 13.1 covers only CAT-029 and CAT-036) |
| Status | not started |

## 2. Declaration

- **Bodies and shapes:** one static rigid body: opaque housing 1.25 × 0.85 × 0.80 m at the origin; opaque base 1.5 × 0.16 × 1.0 m at (0, −0.55, 0); lens (0.72, 0, 0); pick radius 1.0 m. Static `RigidBodyDeclaration`, two box colliders (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L60`, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L91`).
- **Mass and material:** static, zero mass; restitution 1, bounce threshold 0.1 m/s, friction 0.3 (`reference/cpu/MachinePart.cs@a6c914e:L236-L236`; `engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L54`).
- **Constraints and joints:** none.
- **Typed ports:** `PowerIn` (Electrical, Input) at (−0.7, 0, 0); `ActivationIn` (Activation, Input) at (0, 0.52, 0) (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L9`); optical emitter at the lens, +X.
- **Sensors and activation:** Trigger-only enable latch until Reset; other commands rejected.
- **Work and energy stores:** none; emission requires enable and real supply; supply loss clears the beam at the next snapshot.
- **Optical emitter:** narrow ray, range 16, power (0, 0, 1.0) game optical power (**proposed**: legacy single-channel fixtures used the channel mask as power; much stronger than the amber laser's 0.32 blue, giving real margin over the 0.25 default threshold).
- **Parameters:** none.
- **Cosmetic curves and UI bindings:** lens slate `#556573` → blue `#5b9cdb` at rate 12 /s on committed activity (**proposed** on-colour; legacy rate); blue beam ink `#5b9cdb`, opacity from maximum channel so it stays visible against the sky; selected-only construction preview.
- **Art (DESIGN.md palette):** ochre housing `#e8b764`, navy base `#293954`, blue collar `#5b9cdb` with three raised cream bars (blue = three bars), slate lens, gold port studs `#f7cb52` (**proposed**).
- **Catalogue and inventory entry:** id `blue_laser`, title "Blue laser", category Optics, colour (0.36, 0.61, 0.86) (**proposed**: the `#5b9cdb` accent). Description (**proposed**): "A trigger enables the blue laser until Reset. Connect electricity separately. Its beam carries only blue light."

### Variants

One; red and green lasers are separate identities.

## 3. Engine capabilities

Binding shard ([element-02.json](../../coverage/engine/element-02.json)): ElectricalPower, FiniteLedger, GeometryQuery, OpticalTransport, StateTransaction ([element-map row](../general-engine-element-map.md) omits StateTransaction).

**Exists now:** static body and colliders (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L60`, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L91`); activation latch (`engine/gpu/ActivationNetwork.cs@a6c914e:L7-L17`); typed ports (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L13`).

**Missing:** OpticalTransport narrow-ray emission with opaque-geometry occlusion — Story 13.1 (S484: S485, S486, S488, [decisions](../invest/decisions.md#s484)); ElectricalPower supply — Story 8.1 (S257); `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`).

**Element dependencies:** CAT-005 battery; a trigger (CAT-063 or CAT-022, delivered); a blue observer (EL-148 / CAT-012, Story 13.2); an occluder (CAT-066 wall, delivered, or CAT-007 beam shutter, Story 13.3).

## 4. Sources and legacy

**Sources.** Row [element-178](../requirements.md#element-178): a supplied, enabled emitter produces a bounded blue optical channel; a blocked aperture prevents downstream illumination. [campaign-element-coverage](../requirements.md#campaign-element-coverage); scope index [todo-215](../requirements.md#todo-215); refinement **optical-pairings** (S694).

**Legacy** (shared facts S1–S7 in [CAT-036-laser](CAT-036-laser.md#legacy-harvest)):
1. Laser range, lens, enable/supply rule, trigger latch, ports, geometry and lens rate — `parts/LaserPart.cs@a6c914e:L12-L62`. Carry forward all but the amber power.
2. Single-channel fixture power = channel mask — `CuriousContraptions.tests/ReceiverAnimationTests.cs@a6c914e:L21-L25`. Carry forward as the proposal basis.
3. A wall, ball or pipe collar on the ray stops the beam (path < 5); moving it away restores; presentation moves of the blocker do not count — `CuriousContraptions.tests/OpticalTests.cs@a6c914e:L139-L174`. Carry forward (the named blocked-aperture control).
4. Enable × supply matrix and transforms — `CuriousContraptions.tests/OpticalTests.cs@a6c914e:L47-L137`. Carry forward.
5. Blue beam ink stays visible without red — `CuriousContraptions.tests/ColourOpticsTests.cs@a6c914e:L213-L230`. Carry forward.

**Files harvested:** `parts/LaserPart.cs`, `engine/OpticalColour.cs`, `reference/cpu/MachinePart.cs`, `CuriousContraptions.tests/ReceiverAnimationTests.cs`, `CuriousContraptions.tests/OpticalTests.cs`, `CuriousContraptions.tests/ColourOpticsTests.cs`.

## 5. Acceptance outline

- **Chrome UI recipe:** from the actual drawer place a battery, a switch with a falling ball, the blue laser and a blue receiver with a lamp; wire supplies, trigger and lamp with the connection UI. Run.
- **Positive:** the blue receiver reads (0, 0, 1) and lights.
- **Negative / controls:** (named) a wall placed directly in front of the lens blocks the aperture — nothing downstream lights; also no supply, no trigger, a red receiver in the beam (wrong channel).
- **Boundaries:** range 16; output exactly (0, 0, 1); blocker removed restores the beam; interaction cap.
- **Run/Reset:** Reset clears enable, path and lens. **Save/Load:** placement and wiring round-trip.
- **Integrations:** combiner mixes (cyan, magenta, white); EL-145 filter pass. Binding criteria: [element-178](../requirements.md#element-178).

## 6. Open questions

1. Channel power, collar colour, id and title are proposed; owner to confirm.
2. Scheduling: Stories 13.2, 13.4 and 13.6 need single-channel sources, and stories follow their dependencies (owner decision); the earliest consumer is now Story 13.2 (receivers). Candidate: schedule EL-176 to EL-178 before Story 13.2, for example by folding them into Story 13.1 with the amber laser (open scheduling items (a) and (d) in `epics.md`). Owner to confirm.
