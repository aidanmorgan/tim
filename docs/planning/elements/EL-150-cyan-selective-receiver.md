# EL-150 · Cyan selective receiver — named-identity spec

Story 7.0 full spec for a named puzzle-element identity. Baseline commit `a6c914e`. Legacy citations use `path@a6c914e:Lstart-Lend` and stay retrievable from git history after the Epic 7 purge. Lengths are legacy scene units, which the current engine treats as metres (`MetreVector`). The sensitive face looks along local −X.

The mixed-channel receivers (EL-149 to EL-152) share the primary receivers' body, aperture and switched contact ([EL-146](EL-146-red-selective-receiver.md)); they differ in the required channel set, marks and icon. "Cyan" names the game-channel combination green + blue, not colourimetry.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-150 |
| Name | Cyan selective receiver |
| Type | Optical |
| Anchor | [requirements.md#element-150](../requirements.md#element-150); named entry [named-elements.md#element-150](../invest/named-elements.md#element-150); source record [todo-301](../requirements.md#todo-301) |
| Owner | S505 |
| Refines CAT | [CAT-021 cyan_receiver](CAT-021-cyan_receiver.md) (`cyan_receiver`, "Cyan receiver"). One catalogue part satisfies both identities; not a second implementation. |
| Related | EL-149, EL-151, EL-152 (sibling mixed sets); EL-146 to EL-148 (primary); EL-213 combiner; EL-177, EL-178 (green and blue lasers) |
| Roadmap story | 13.6 Secondary Spectral Receivers (Cyan, Magenta, Yellow, White) (CAT-021, CAT-040, CAT-072, CAT-068) |
| Status | not started |

## 2. Declaration

- **Bodies and shapes:** one static rigid body: opaque cream plate 0.26 × 1.4 × 1.4 m at the origin; opaque navy base 0.85 × 0.18 × 1.6 m at (0, −0.88, 0); pick radius 1.1 m. Types: static `RigidBodyDeclaration`, two box `ColliderDeclaration`s (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L60`, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L91`).
- **Mass and material:** static, zero mass; contact material restitution 1, bounce threshold 0.1 m/s, friction 0.3 (`reference/cpu/MachinePart.cs@a6c914e:L236-L236`) via `ContactMaterialDeclaration` (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L54`).
- **Constraints and joints:** none.
- **Typed ports:** optical `Main` at (−0.18, 0, 0), normal −X, finite disc radius 0.55, Absorb, front only; electrical `PowerIn` (Input) at (0, −0.6, 0.65) and `Supply` (Output) at (0, −0.6, −0.65) (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L9`).
- **Sensors and activation:** required set {G, B}. Matches when min(G, B) ≥ threshold **and** G + B ≥ 0.9 × (R + G + B), power summed over all emitters in one snapshot. While matched the contact routes `PowerIn` → `Supply`. No hysteresis; light never creates electricity.
- **Work and energy stores:** none.
- **Parameters:** `threshold` — f32, 0.05–2, default 0.25, game optical power, applied to each required channel (`ReceiverParameter.Threshold`). Fixed purity 0.9; channel set fixed at `Cyan`. Mode: "threshold. OpticalChannel=Cyan".
- **Cosmetic curves and UI bindings:** disc eases slate `#556573` → gold `#f7cb52` over 0.125 s smoothstep from committed activity (endpoint drive). Cyan beams draw in `#66b8c9` ([DESIGN.md](../../../DESIGN.md#combiner-and-mixed-channel-receivers)).
- **Art (DESIGN.md palette):** cream plate `#fff8e9`, navy foot `#293954`, disc r 0.55, cream ring r 0.33, gold centre r 0.08, gold port studs; at (−0.15, 0.6, 0) two mark rows: two green bars `#62aa78` and three blue bars `#5b9cdb`.
- **Catalogue and inventory entry:** id `cyan_receiver`, title "Cyan receiver", category Optics, colour (1, 0.97, 0.91), parameters `{"threshold": 0.25}`. Description: "Each marked RGB channel must meet the threshold, with at least 90% power in the required channels. Switches a separate electrical supply." Icon `ui/WorkshopIcons.cs@a6c914e:L68-L68` (current, kept).

### Variants

One; yellow, magenta and white are separate identities.

## 3. Engine capabilities

Binding shard ([element-02.json](../../coverage/engine/element-02.json)): FiniteLedger, GeometryQuery, OpticalAbsorption, OpticalTransport, SensibleHeat, SignalPropagation, StateTransaction ([element-map row](../general-engine-element-map.md) omits StateTransaction).

**Exists now:** static body, colliders, material (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L60`, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L91`); `PowerIn`/`Supply` sockets (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L11`).

**Missing:** OpticalTransport Absorb aperture with multi-emitter aggregation — Story 13.2/13.6 (S484: S485, S486, S488, [decisions](../invest/decisions.md#s484)); combiner routing — Story 13.5; switched contact — Story 8.1 plus 13.2 (S257); OpticalAbsorption/SensibleHeat — S489/S543, mode-specific; `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`).

**Element dependencies:** a green+blue source (EL-213 combiner fed by EL-177 and EL-178, or by green- and blue-filtered amber lasers), CAT-005 battery, a load (CAT-035 lamp), CAT-066 wall.

## 4. Sources and legacy

**Sources.** Row [element-150](../requirements.md#element-150): output requires green and blue under the declared thresholds and purity contract; three independent negative controls. [todo-301](../requirements.md#todo-301). CAT row [current-cat-021](../requirements.md#current-cat-021). Refinement **optical-pairings** (S694). Campaign first use 51–60 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)).

**Legacy** (shared optical facts S1–S7 in [CAT-036-laser](CAT-036-laser.md#legacy-harvest); receiver body and shared tests in [EL-146](EL-146-red-selective-receiver.md#4-sources-and-legacy); mixed rule in [EL-149](EL-149-yellow-selective-receiver.md#4-sources-and-legacy)):
1. Receiver body, aperture, ports, route, threshold validation and indicator — `parts/LightReceiverPart.cs@a6c914e:L11-L59`. Carry forward behaviour; not the per-part callback or Godot materials.
2. Cyan mask (0, 1, 1); minimum-of-required strength; purity over the set — `engine/OpticalColour.cs@a6c914e:L10-L22`, `engine/OpticalColour.cs@a6c914e:L35-L48`. Carry forward.
3. Mark rows and cyan beam ink — `engine/OpticalColour.cs@a6c914e:L49-L76`. Carry forward as presentation.
4. Catalogue entry and scene (Colour = 5) — `parts/catalog/cyan_receiver.tres@a6c914e:L6-L12`, `parts/scenes/cyan_receiver.tscn@a6c914e:L1-L5`. Carry forward the entry.
5. Acceptance: (0, 0.3, 0.3) accepted; G or B at 0 or 0.249 rejected; (1, 1, 1) rejected — `CuriousContraptions.tests/BeamCombinerTests.cs@a6c914e:L118-L137`. Carry forward.
6. Acceptance: contact binding, indicator and rollback — `CuriousContraptions.tests/ElectricalContactBindingTests.cs@a6c914e:L85-L111`, `CuriousContraptions.tests/ReceiverAnimationTests.cs@a6c914e:L44-L102`, `CuriousContraptions.tests/OpticalRuntimeCheckpointTests.cs@a6c914e:L30-L93`. Carry forward.

**Files harvested:** `parts/LightReceiverPart.cs`, `parts/catalog/cyan_receiver.tres`, `parts/scenes/cyan_receiver.tscn`, `engine/OpticalColour.cs`, `engine/MachineData.cs`, `CuriousContraptions.tests/BeamCombinerTests.cs`, `CuriousContraptions.tests/ElectricalContactBindingTests.cs`, `CuriousContraptions.tests/ReceiverAnimationTests.cs`, `CuriousContraptions.tests/OpticalRuntimeCheckpointTests.cs`.

## 5. Acceptance outline

- **Chrome UI recipe:** from the actual drawer place a battery, green and blue sources (EL-177/EL-178, or amber lasers with green and blue filters), a combiner, the cyan receiver on its outlet and a lamp; wire with the connection UI; aim with the gizmo. Run.
- **Positive:** green and blue both ≥ 0.25 at ≥ 90% of total light the disc and lamp (filtered amber via the combiner gives G 0.702, B 0.288).
- **Negative / controls (independent):** (a) missing required channel — green-only or blue-only; (b) wrong channel — red-only, and unfiltered amber (G + B is 52% of total); (c) absent supply. Back face and occluder.
- **Boundaries:** each required channel 0.25 / 0.249; purity 0.9; threshold edits 0.05–2.
- **Run/Reset:** Reset clears reading, contact and indicator. **Save/Load:** threshold, placement, wiring.
- **Integrations:** EL-213 combiner; Story 13.6 suite `tools/e2e/cat-receivers-secondary.test.ts`. Binding criteria: [element-150](../requirements.md#element-150), [current-cat-021](../requirements.md#current-cat-021).

## 6. Open questions

1. One shared threshold for both required channels; confirm (as EL-149 question 1).
2. No hysteresis (as EL-146 question 2); owner S486.
3. With amber sources the blue channel after the combiner (0.288) sits close to the 0.25 default; confirm fixtures use the blue laser (EL-178) for margin, under S485.
4. Confirm CAT-021 alone satisfies EL-150.
