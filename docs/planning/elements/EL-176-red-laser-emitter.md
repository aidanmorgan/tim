# EL-176 · Red laser emitter — named-identity spec

Story 7.0 full spec for a named puzzle-element identity. Baseline commit `a6c914e`. Legacy citations use `path@a6c914e:Lstart-Lend` and stay retrievable from git history after the Epic 7 purge. Lengths are legacy scene units, which the current engine treats as metres (`MetreVector`). Local +X is the beam direction.

The three channel lasers (EL-176 red, EL-177 green, EL-178 blue) are separate identities from the amber [CAT-036 laser](CAT-036-laser.md). Legacy had no channel laser as a catalogue part; the body, ports, enable latch, range and lens curve are the legacy amber laser's (sourced), and only the channel power and channel art are **proposed**. Legacy tests did use single-channel laser fixtures with power equal to the channel mask.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-176 |
| Name | Red laser emitter |
| Type | Optical |
| Anchor | [requirements.md#element-176](../requirements.md#element-176); named entry [named-elements.md#element-176](../invest/named-elements.md#element-176); source record [campaign-element-coverage](../requirements.md#campaign-element-coverage) |
| Owner | S492 |
| Refines CAT | none one-to-one; extends [CAT-036 laser](CAT-036-laser.md) (same body and control law, different channel). |
| Related | EL-177, EL-178 (sibling channels); EL-143 red filter; EL-146 red receiver; EL-149, EL-151, EL-152 mixed receivers; EL-213 combiner; EL-174 prism (red-only control) |
| Roadmap story | unscheduled (Story 13.1 covers only CAT-029 and CAT-036) |
| Status | not started |

## 2. Declaration

- **Bodies and shapes:** one static rigid body: opaque housing box 1.25 × 0.85 × 0.80 m at the origin; opaque base 1.5 × 0.16 × 1.0 m at (0, −0.55, 0); lens point (0.72, 0, 0); pick radius 1.0 m (legacy laser). Static `RigidBodyDeclaration` and two box `ColliderDeclaration`s (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L60`, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L91`).
- **Mass and material:** static, zero mass; contact material restitution 1, bounce threshold 0.1 m/s, friction 0.3 (`reference/cpu/MachinePart.cs@a6c914e:L236-L236`; `engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L54`).
- **Constraints and joints:** none.
- **Typed ports:** `PowerIn` (Electrical, Input) at (−0.7, 0, 0); `ActivationIn` (Activation, Input) at (0, 0.52, 0) — both already in the current socket enum (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L9`). Optical emitter at the lens, direction +X.
- **Sensors and activation:** accepts only the Trigger activation command, which latches *enable* until Reset; any other command is rejected.
- **Work and energy stores:** none. Emission requires enable **and** real supply on `PowerIn`; supply loss clears the beam at the next optical snapshot while enable survives.
- **Optical emitter:** narrow ray, range 16, power (1.0, 0, 0) game optical power (**proposed**: the amber laser's maximum channel value and the legacy single-channel test fixtures, which used the channel mask as power; 4× the default 0.25 receiver threshold, so it survives a 50/50 split, two mirrors and a combiner).
- **Parameters:** none (legacy laser had none; a channel selector would merge three identities, which the rules forbid).
- **Cosmetic curves and UI bindings:** lens follows committed activity (beam path non-empty) from slate `#556573` to red `#de7058` at exponential rate 12 /s (**proposed** on-colour: the red channel accent; rate is the legacy laser's). The beam draws in red ink `#de7058` with opacity from its maximum channel. Construction preview: a selected downstream optic shows the would-be path even when unpowered, never activating.
- **Art (DESIGN.md palette):** ochre housing `#e8b764`, navy base `#293954`, red collar `#de7058` in place of the cream collar (**proposed**: channel identity on the part), slate lens, gold port studs `#f7cb52`; one raised cream bar on the collar (red = one bar, the filters' convention) (**proposed**).
- **Catalogue and inventory entry:** id `red_laser`, title "Red laser", category Optics, colour (0.87, 0.44, 0.35) (**proposed**: the `#de7058` accent). Description (**proposed**): "A trigger enables the red laser until Reset. Connect electricity separately. Its beam carries only red light."

### Variants

One. Green and blue lasers are separate identities.

## 3. Engine capabilities

Binding shard ([element-02.json](../../coverage/engine/element-02.json)): ElectricalPower, FiniteLedger, GeometryQuery, OpticalTransport, StateTransaction ([element-map row](../general-engine-element-map.md) omits StateTransaction).

**Exists now:** static box body and colliders (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L60`, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L91`); activation latch and edges (`engine/gpu/ActivationNetwork.cs@a6c914e:L7-L17`); typed ports (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L13`).

**Missing**
- OpticalTransport narrow-ray emission with per-channel power, occlusion and committed path — Story 13.1 (built for the amber laser; this part reuses it). Decision owner **S484** ([decisions](../invest/decisions.md#s484)): S485, S486, S488.
- ElectricalPower supply on `PowerIn` — Story 8.1; S257.
- `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`).

**Element dependencies:** CAT-005 battery (Story 8.1); a trigger (CAT-063 switch or CAT-022 delay, delivered); a red-sensitive observer (EL-146 / CAT-056, Story 13.2).

## 4. Sources and legacy

**Sources.** Row [element-176](../requirements.md#element-176): a supplied, enabled emitter produces a bounded red optical channel; missing supply or enable prevents output. [campaign-element-coverage](../requirements.md#campaign-element-coverage): "each laser/receiver channel" first use 51–60; separate rows per element. Scope index [todo-215](../requirements.md#todo-215) lists the red emitter as an individual optical contract. Refinement **optical-pairings** (S694).

**Legacy** (shared optical facts S1–S7 in [CAT-036-laser](CAT-036-laser.md#legacy-harvest)):
1. Range 16, lens (0.72, 0, 0); amber power (1, 0.78, 0.32) — `parts/LaserPart.cs@a6c914e:L12-L14`. Carry forward range and lens; do not carry forward the amber power (this identity is red-only).
2. Emission only when enabled and `PowerIn` powered; preview source exists regardless — `parts/LaserPart.cs@a6c914e:L36-L38`. Carry forward.
3. Trigger-only enable latch; ports — `parts/LaserPart.cs@a6c914e:L30-L44`. Carry forward.
4. Geometry, colours, lens follow rate 12 — `parts/LaserPart.cs@a6c914e:L26-L29`, `parts/LaserPart.cs@a6c914e:L52-L62`. Carry forward (collar colour replaced, proposed).
5. Single-channel laser fixtures: a laser subclass emitting power = channel mask drives each receiver colour — `CuriousContraptions.tests/ReceiverAnimationTests.cs@a6c914e:L21-L25`, `CuriousContraptions.tests/ReceiverAnimationTests.cs@a6c914e:L53-L72`. Carry forward as the basis for the proposed (1, 0, 0).
6. Acceptance pattern: all 8 combinations of enable × laser supply × receiver supply; supply loss clears at the next snapshot; Reset clears enable, path and reading — `CuriousContraptions.tests/OpticalTests.cs@a6c914e:L47-L94`; full 3D transforms, back face and off-axis misses — `CuriousContraptions.tests/OpticalTests.cs@a6c914e:L96-L137`; opaque occluders — `CuriousContraptions.tests/OpticalTests.cs@a6c914e:L139-L174`. Carry forward.
7. Red beam ink `#de7058` and visibility — `engine/OpticalColour.cs@a6c914e:L49-L60`; `CuriousContraptions.tests/ColourOpticsTests.cs@a6c914e:L213-L230`. Carry forward.

**Files harvested:** `parts/LaserPart.cs`, `engine/OpticalColour.cs`, `reference/cpu/MachinePart.cs`, `CuriousContraptions.tests/ReceiverAnimationTests.cs`, `CuriousContraptions.tests/OpticalTests.cs`, `CuriousContraptions.tests/ColourOpticsTests.cs`.

## 5. Acceptance outline

- **Chrome UI recipe:** from the actual drawer place a battery, a switch with a falling ball, the red laser and a red receiver 6 m along +X with a lamp; wire battery → laser `PowerIn`, switch `ActivationOut` → laser `ActivationIn`, battery → receiver `PowerIn`, receiver `Supply` → lamp. Run.
- **Positive:** the ball triggers the switch; the red beam reaches the receiver at (1, 0, 0); the lamp lights.
- **Negative / controls:** (named) missing supply — no beam; missing enable (no trigger) — no beam; also a green receiver in the beam stays dark (wrong channel); a wall on the ray; supply removed mid-run (beam clears, enable survives).
- **Boundaries:** range 16; output exactly (1, 0, 0) — no green or blue; finite receiver disc; interaction cap.
- **Run/Reset:** Reset clears enable, path and lens. **Save/Load:** placement and wiring round-trip.
- **Integrations:** EL-213 combiner with EL-177/EL-178 to make yellow, magenta and white; EL-174 prism red-only control; EL-143 filter pass. Binding criteria: [element-176](../requirements.md#element-176).

## 6. Open questions

1. Channel power (1, 0, 0), collar colour, catalogue id and title are proposed; owner to confirm.
2. Scheduling: Stories 13.2, 13.4 and 13.6 need single-channel sources, and stories follow their dependencies (owner decision); the earliest consumer is now Story 13.2 (receivers). Candidate: schedule EL-176 to EL-178 before Story 13.2, for example by folding them into Story 13.1 with the amber laser (open scheduling items (a) and (d) in `epics.md`). Owner to confirm.
3. "Bounded red optical channel": confirm range 16 and no authorable power.
