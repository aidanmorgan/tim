# EL-152 · White selective receiver — named-identity spec

Story 7.0 full spec for a named puzzle-element identity. Baseline commit `a6c914e`. Legacy citations use `path@a6c914e:Lstart-Lend` and stay retrievable from git history after the Epic 7 purge. Lengths are legacy scene units, which the current engine treats as metres (`MetreVector`). The sensitive face looks along local −X.

The mixed-channel receivers (EL-149 to EL-152) share the primary receivers' body, aperture and switched contact ([EL-146](EL-146-red-selective-receiver.md)); they differ in the required channel set, marks and icon. "White" names the game-channel set red + green + blue, not colourimetry. It differs from the broadband detector (EL-214), which thresholds the channel mean.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-152 |
| Name | White selective receiver |
| Type | Optical |
| Anchor | [requirements.md#element-152](../requirements.md#element-152); named entry [named-elements.md#element-152](../invest/named-elements.md#element-152); source record [todo-301](../requirements.md#todo-301) |
| Owner | S507 |
| Refines CAT | [CAT-068 white_receiver](CAT-068-white_receiver.md) (`white_receiver`, "White receiver"). One catalogue part satisfies both identities; not a second implementation. |
| Related | EL-149 to EL-151 (sibling mixed sets); EL-146 to EL-148 (primary); EL-214 broadband (mean rule); EL-213 combiner; EL-174 prism (separates what this receiver needs combined) |
| Roadmap story | 13.6 Secondary Spectral Receivers (Cyan, Magenta, Yellow, White) (CAT-021, CAT-040, CAT-072, CAT-068) |
| Status | not started |

## 2. Declaration

- **Bodies and shapes:** one static rigid body: opaque cream plate 0.26 × 1.4 × 1.4 m at the origin; opaque navy base 0.85 × 0.18 × 1.6 m at (0, −0.88, 0); pick radius 1.1 m. Types: static `RigidBodyDeclaration`, two box `ColliderDeclaration`s (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L60`, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L91`).
- **Mass and material:** static, zero mass; contact material restitution 1, bounce threshold 0.1 m/s, friction 0.3 (`reference/cpu/MachinePart.cs@a6c914e:L236-L236`) via `ContactMaterialDeclaration` (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L54`).
- **Constraints and joints:** none.
- **Typed ports:** optical `Main` at (−0.18, 0, 0), normal −X, finite disc radius 0.55, Absorb, front only; electrical `PowerIn` (Input) at (0, −0.6, 0.65) and `Supply` (Output) at (0, −0.6, −0.65) (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L9`).
- **Sensors and activation:** required set {R, G, B}. Matches when min(R, G, B) ≥ threshold (the 90% purity test is always met because the set is every channel). Power is summed over all emitters in one snapshot. While matched the contact routes `PowerIn` → `Supply`. No hysteresis; light never creates electricity.
- **Work and energy stores:** none.
- **Parameters:** `threshold` — f32, 0.05–2, default 0.25, game optical power, applied to each channel (`ReceiverParameter.Threshold`). Fixed purity 0.9 (vacuous here); channel set fixed at `White`. Mode: "threshold. OpticalChannel=White".
- **Cosmetic curves and UI bindings:** disc eases slate `#556573` → gold `#f7cb52` over 0.125 s smoothstep from committed activity (endpoint drive). Three-channel beams keep warm cream ink `#fff0a5` ([DESIGN.md](../../../DESIGN.md#combiner-and-mixed-channel-receivers)).
- **Art (DESIGN.md palette):** cream plate `#fff8e9`, navy foot `#293954`, disc r 0.55, cream ring r 0.33, gold centre r 0.08, gold port studs; at (−0.15, 0.6, 0) three mark rows: one red bar `#de7058`, two green bars `#62aa78`, three blue bars `#5b9cdb`.
- **Catalogue and inventory entry:** id `white_receiver`, title "White receiver", category Optics, colour (1, 0.97, 0.91), parameters `{"threshold": 0.25}`. Description: "Each marked RGB channel must meet the threshold, with at least 90% power in the required channels. Switches a separate electrical supply." Icon `ui/WorkshopIcons.cs@a6c914e:L70-L70` (current, kept).

### Variants

One; yellow, cyan and magenta are separate identities.

## 3. Engine capabilities

Binding shard ([element-02.json](../../coverage/engine/element-02.json)): FiniteLedger, GeometryQuery, OpticalAbsorption, OpticalTransport, SensibleHeat, SignalPropagation, StateTransaction ([element-map row](../general-engine-element-map.md) omits StateTransaction).

**Exists now:** static body, colliders, material (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L60`, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L91`); `PowerIn`/`Supply` sockets (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L11`).

**Missing:** OpticalTransport Absorb aperture with multi-emitter aggregation — Story 13.2/13.6 (S484: S485, S486, S488, [decisions](../invest/decisions.md#s484)); combiner routing — Story 13.5; switched contact — Story 8.1 plus 13.2 (S257); OpticalAbsorption/SensibleHeat — S489/S543, mode-specific; `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`).

**Element dependencies:** an all-channel source (CAT-036 amber laser alone, or EL-213 combining EL-176 to EL-178 or three filtered amber lasers), CAT-005 battery, a load (CAT-035 lamp), CAT-066 wall.

## 4. Sources and legacy

**Sources.** Row [element-152](../requirements.md#element-152): output requires red, green and blue under the declared thresholds and purity contract; three independent negative controls. [todo-301](../requirements.md#todo-301). CAT row [current-cat-068](../requirements.md#current-cat-068). Story 13.6: "White Receiver requires all three primary colors simultaneously". Refinement **optical-pairings** (S694). Campaign first use 51–60 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)).

**Legacy** (shared optical facts S1–S7 in [CAT-036-laser](CAT-036-laser.md#legacy-harvest); receiver body and shared tests in [EL-146](EL-146-red-selective-receiver.md#4-sources-and-legacy); mixed rule in [EL-149](EL-149-yellow-selective-receiver.md#4-sources-and-legacy)):
1. Receiver body, aperture, ports, route, threshold validation and indicator — `parts/LightReceiverPart.cs@a6c914e:L11-L59`. Carry forward behaviour; not the per-part callback or Godot materials.
2. White mask (1, 1, 1); minimum-of-all strength — `engine/OpticalColour.cs@a6c914e:L10-L22`, `engine/OpticalColour.cs@a6c914e:L35-L48`. Carry forward.
3. Mark rows and warm-cream ink for three-channel light — `engine/OpticalColour.cs@a6c914e:L49-L76`. Carry forward as presentation.
4. Catalogue entry and scene (Colour = 7) — `parts/catalog/white_receiver.tres@a6c914e:L6-L12`, `parts/scenes/white_receiver.tscn@a6c914e:L1-L5`. Carry forward the entry.
5. Acceptance: (0.3, 0.3, 0.3) accepted; any channel at 0 or 0.249 rejected; (1, 1, 1) accepted (the purity rejection applies to other sets only) — `CuriousContraptions.tests/BeamCombinerTests.cs@a6c914e:L118-L137`. Carry forward.
6. Acceptance: three amber lasers through red, green and blue filters feed the combiner's three inputs; the white receiver on the outlet reads laser power × 0.9 = (0.9, 0.702, 0.288) within 1e-5 and activates, but supplies nothing without its own supply; the same holds for any rotation of the whole set; Reset clears all paths, combiner input and output, and the receiver — `CuriousContraptions.tests/BeamCombinerTests.cs@a6c914e:L39-L90`. Carry forward.
7. Acceptance: contact binding, indicator and rollback — `CuriousContraptions.tests/ElectricalContactBindingTests.cs@a6c914e:L85-L111`, `CuriousContraptions.tests/ReceiverAnimationTests.cs@a6c914e:L44-L102`, `CuriousContraptions.tests/OpticalRuntimeCheckpointTests.cs@a6c914e:L30-L93`. Carry forward.

**Files harvested:** `parts/LightReceiverPart.cs`, `parts/catalog/white_receiver.tres`, `parts/scenes/white_receiver.tscn`, `engine/OpticalColour.cs`, `engine/MachineData.cs`, `CuriousContraptions.tests/BeamCombinerTests.cs`, `CuriousContraptions.tests/ElectricalContactBindingTests.cs`, `CuriousContraptions.tests/ReceiverAnimationTests.cs`, `CuriousContraptions.tests/OpticalRuntimeCheckpointTests.cs`.

## 5. Acceptance outline

- **Chrome UI recipe:** from the actual drawer place a battery, three lasers with red, green and blue filters (or EL-176 to EL-178), a combiner, the white receiver on its outlet and a lamp; wire with the connection UI; aim with the gizmo. Run.
- **Positive:** all three channels ≥ 0.25 light the disc and lamp.
- **Negative / controls (independent):** (a) missing required channel — block any one of the three inputs (two-channel light fails); (b) wrong channel — a single-channel beam of any colour; (c) absent supply. Back face and occluder.
- **Boundaries:** the weakest channel at 0.25 / 0.249 (the filtered-amber blue after the combiner is 0.288); threshold edits 0.05–2.
- **Run/Reset:** Reset clears reading, contact and indicator. **Save/Load:** threshold, placement, wiring.
- **Integrations:** EL-213 combiner; Story 13.6 suite `tools/e2e/cat-receivers-secondary.test.ts`. Binding criteria: [element-152](../requirements.md#element-152), [current-cat-068](../requirements.md#current-cat-068).

## 6. Open questions

1. The unfiltered amber laser alone (blue 0.32) satisfies the white receiver, so "requires mixing" is not enforced by the source rule. Confirm this is acceptable for teaching, under S485.
2. One shared threshold for all three channels; confirm (as EL-149 question 1).
3. No hysteresis (as EL-146 question 2); owner S486.
4. Confirm CAT-068 alone satisfies EL-152.
