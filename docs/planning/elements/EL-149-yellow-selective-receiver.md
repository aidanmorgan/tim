# EL-149 · Yellow selective receiver — named-identity spec

Story 7.0 full spec for a named puzzle-element identity. Baseline commit `a6c914e`. Legacy citations use `path@a6c914e:Lstart-Lend` and stay retrievable from git history after the Epic 7 purge. Lengths are legacy scene units, which the current engine treats as metres (`MetreVector`). The sensitive face looks along local −X.

The mixed-channel receivers (EL-149 yellow, EL-150 cyan, EL-151 magenta, EL-152 white) share the primary receivers' body, aperture and switched contact ([EL-146](EL-146-red-selective-receiver.md)); they differ in the required channel set, marks and icon. "Yellow" names a game-channel combination (red + green), not colourimetry.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-149 |
| Name | Yellow selective receiver |
| Type | Optical |
| Anchor | [requirements.md#element-149](../requirements.md#element-149); named entry [named-elements.md#element-149](../invest/named-elements.md#element-149); source record [todo-301](../requirements.md#todo-301) |
| Owner | S504 |
| Refines CAT | [CAT-072 yellow_receiver](CAT-072-yellow_receiver.md) (`yellow_receiver`, "Yellow receiver"). One catalogue part satisfies both identities; not a second implementation. |
| Related | EL-150 to EL-152 (sibling mixed sets); EL-146 to EL-148 (primary); EL-213 combiner (mixes the input); EL-176, EL-177 (red and green lasers) |
| Roadmap story | 13.6 Secondary Spectral Receivers (Cyan, Magenta, Yellow, White) (CAT-021, CAT-040, CAT-072, CAT-068) |
| Status | not started |

## 2. Declaration

- **Bodies and shapes:** one static rigid body: opaque cream plate 0.26 × 1.4 × 1.4 m at the origin; opaque navy base 0.85 × 0.18 × 1.6 m at (0, −0.88, 0); pick radius 1.1 m. Types: static `RigidBodyDeclaration`, two box `ColliderDeclaration`s (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L60`, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L91`).
- **Mass and material:** static, zero mass; contact material restitution 1, bounce threshold 0.1 m/s, friction 0.3 (`reference/cpu/MachinePart.cs@a6c914e:L236-L236`) via `ContactMaterialDeclaration` (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L54`).
- **Constraints and joints:** none.
- **Typed ports:** optical `Main` at (−0.18, 0, 0), normal −X, finite disc radius 0.55, Absorb, front only; electrical `PowerIn` (Input) at (0, −0.6, 0.65) and `Supply` (Output) at (0, −0.6, −0.65) (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L9`).
- **Sensors and activation:** required set {R, G}. Matches when min(R, G) ≥ threshold **and** R + G ≥ 0.9 × (R + G + B), with power summed over all emitters in one snapshot. While matched the contact routes `PowerIn` → `Supply`. No hysteresis; light never creates electricity. These are explicit presence rules, not exact spectral matching.
- **Work and energy stores:** none.
- **Parameters:** `threshold` — f32, 0.05–2, default 0.25, game optical power, applied to each required channel (`ReceiverParameter.Threshold`). Fixed purity 0.9; channel set fixed at `Yellow` (`OpticalColour`). Mode: "threshold. OpticalChannel=Yellow".
- **Cosmetic curves and UI bindings:** disc eases slate `#556573` → gold `#f7cb52` over 0.125 s smoothstep from committed activity (endpoint drive). Required channels show as grouped coloured bar rows; yellow beams draw in gold `#f7cb52` ([DESIGN.md](../../../DESIGN.md#combiner-and-mixed-channel-receivers)).
- **Art (DESIGN.md palette):** cream plate `#fff8e9`, navy foot `#293954`, disc r 0.55, cream ring r 0.33, gold centre r 0.08, gold port studs; at (−0.15, 0.6, 0) two mark rows: one red bar `#de7058` and two green bars `#62aa78`.
- **Catalogue and inventory entry:** id `yellow_receiver`, title "Yellow receiver", category Optics, colour (1, 0.97, 0.91), parameters `{"threshold": 0.25}`. Description: "Each marked RGB channel must meet the threshold, with at least 90% power in the required channels. Switches a separate electrical supply." Icon `ui/WorkshopIcons.cs@a6c914e:L67-L67` (current, kept).

### Variants

One; cyan, magenta and white are separate identities. Missing-channel, wrong-channel and absent-supply controls are proof cases.

## 3. Engine capabilities

Binding shard ([element-02.json](../../coverage/engine/element-02.json)): FiniteLedger, GeometryQuery, OpticalAbsorption, OpticalTransport, SensibleHeat, SignalPropagation, StateTransaction ([element-map row](../general-engine-element-map.md) omits StateTransaction).

**Exists now:** static body, colliders, material (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L60`, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L91`); `PowerIn`/`Supply` sockets (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L11`).

**Missing:** OpticalTransport Absorb aperture with multi-emitter aggregation in one snapshot — Story 13.2/13.6 (S484: S485, S486, S488, [decisions](../invest/decisions.md#s484)); combiner routing to produce mixed beams — Story 13.5; switched electrical contact — Story 8.1 plus 13.2 (S257); OpticalAbsorption/SensibleHeat — S489/S543, mode-specific; `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`).

**Element dependencies:** a red+green source (EL-213 combiner fed by EL-176 and EL-177, or by filtered amber lasers; Stories 13.1, 13.4 and 13.5), CAT-005 battery, a load (CAT-035 lamp), CAT-066 wall.

## 4. Sources and legacy

**Sources.** Row [element-149](../requirements.md#element-149): output requires red and green under the declared thresholds and channel-purity contract; missing required channel, wrong channel and absent supply are independent negative controls. [todo-301](../requirements.md#todo-301): every named channel at threshold and at least 90% of total power in that set. CAT row [current-cat-072](../requirements.md#current-cat-072): blue-only or either missing channel fails. Refinement **optical-pairings** (S694). Campaign: combiner/mixed receivers first use 51–60 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)).

**Legacy** (shared optical facts S1–S7 in [CAT-036-laser](CAT-036-laser.md#legacy-harvest); receiver body, contact, indicator and shared tests in [EL-146](EL-146-red-selective-receiver.md#4-sources-and-legacy), unchanged here):
1. Receiver body, aperture, ports, route, threshold validation and indicator — `parts/LightReceiverPart.cs@a6c914e:L11-L59`. Carry forward behaviour; not the per-part callback or Godot materials.
2. Yellow mask (1, 1, 0); strength = minimum over masked channels; accept iff strength ≥ threshold and power·mask ≥ 0.9 × total — `engine/OpticalColour.cs@a6c914e:L10-L22`, `engine/OpticalColour.cs@a6c914e:L35-L48`. Carry forward.
3. Grouped mark rows (one row per required channel, bar count = channel index) — `engine/OpticalColour.cs@a6c914e:L61-L76`; yellow beam ink gold — `engine/OpticalColour.cs@a6c914e:L49-L55`. Carry forward as presentation.
4. Catalogue entry and scene (Colour = 4) — `parts/catalog/yellow_receiver.tres@a6c914e:L6-L12`, `parts/scenes/yellow_receiver.tscn@a6c914e:L1-L5`. Carry forward the entry.
5. Acceptance: (0.3, 0.3, 0) accepted at threshold 0.25; dropping R or G to 0, or to 0.249, rejects; (1, 1, 1) rejects (purity) — `CuriousContraptions.tests/BeamCombinerTests.cs@a6c914e:L118-L137`. Carry forward.
6. Acceptance: contact binding, indicator and rollback per colour — `CuriousContraptions.tests/ElectricalContactBindingTests.cs@a6c914e:L85-L111`, `CuriousContraptions.tests/ReceiverAnimationTests.cs@a6c914e:L44-L102`, `CuriousContraptions.tests/OpticalRuntimeCheckpointTests.cs@a6c914e:L30-L93`. Carry forward.
7. Presentation: co-directed red and green paths from one origin sum to yellow ink over their overlap only; crossing beams stay separate — `CuriousContraptions.tests/BeamCombinerTests.cs@a6c914e:L161-L177`. Carry forward.

**Files harvested:** `parts/LightReceiverPart.cs`, `parts/catalog/yellow_receiver.tres`, `parts/scenes/yellow_receiver.tscn`, `engine/OpticalColour.cs`, `engine/MachineData.cs`, `CuriousContraptions.tests/BeamCombinerTests.cs`, `CuriousContraptions.tests/ElectricalContactBindingTests.cs`, `CuriousContraptions.tests/ReceiverAnimationTests.cs`, `CuriousContraptions.tests/OpticalRuntimeCheckpointTests.cs`.

## 5. Acceptance outline

- **Chrome UI recipe:** from the actual drawer place a battery, red and green lasers (or two amber lasers with red and green filters), a combiner, the yellow receiver on the combiner outlet and a lamp; wire supplies, receiver `PowerIn` and `Supply` → lamp with the connection UI; aim with the gizmo. Run.
- **Positive:** red and green both ≥ 0.25 at the receiver (≥ 90% of total) light the disc and lamp.
- **Negative / controls (independent):** (a) missing required channel — red-only or green-only input; (b) wrong channel — blue-only input, and unfiltered amber (R + G is 85% of total, below purity); (c) absent supply — disc lights, lamp off. Back face and occluder.
- **Boundaries:** each required channel 0.25 / 0.249; purity at 0.9; threshold edits 0.05–2.
- **Run/Reset:** Reset clears reading, contact and indicator. **Save/Load:** threshold, placement, wiring.
- **Integrations:** EL-213 combiner; Story 13.6 suite `tools/e2e/cat-receivers-secondary.test.ts`. Binding criteria: [element-149](../requirements.md#element-149), [current-cat-072](../requirements.md#current-cat-072).

## 6. Open questions

1. "Declared thresholds": one threshold applies to both required channels; confirm per-channel thresholds are not wanted.
2. No hysteresis at the threshold (as EL-146 question 2); owner S486.
3. The unfiltered amber laser fails yellow purity (85%). Confirm this is the intended teaching (mixing needed), under S485.
4. Confirm CAT-072 alone satisfies EL-149.
