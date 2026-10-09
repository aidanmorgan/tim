# EL-148 · Blue selective receiver — named-identity spec

Story 7.0 full spec for a named puzzle-element identity. Baseline commit `a6c914e`. Legacy citations use `path@a6c914e:Lstart-Lend` and stay retrievable from git history after the Epic 7 purge. Lengths are legacy scene units, which the current engine treats as metres (`MetreVector`). The sensitive face looks along local −X.

The selective receivers (EL-146 to EL-152) and the broadband detector (EL-214) share one body, aperture and switched contact; they differ in the acceptance rule, marks and icon.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-148 |
| Name | Blue selective receiver |
| Type | Optical |
| Anchor | [requirements.md#element-148](../requirements.md#element-148); named entry [named-elements.md#element-148](../invest/named-elements.md#element-148); source record [todo-300](../requirements.md#todo-300) |
| Owner | S503 |
| Refines CAT | [CAT-012 blue_receiver](CAT-012-blue_receiver.md) (`blue_receiver`, "Blue receiver"). One catalogue part satisfies both identities; not a second implementation. |
| Related | EL-146, EL-147 (sibling primary channels); EL-149 to EL-152 (mixed); EL-214 broadband; EL-145 blue filter and EL-178 blue laser |
| Roadmap story | 13.2 Broadband & Pure Channel Optical Receivers (CAT-038, CAT-056, CAT-032, CAT-012) |
| Status | not started |

## 2. Declaration

- **Bodies and shapes:** one static rigid body: opaque cream plate 0.26 × 1.4 × 1.4 m at the origin; opaque navy base 0.85 × 0.18 × 1.6 m at (0, −0.88, 0); pick radius 1.1 m. Types: static `RigidBodyDeclaration`, two box `ColliderDeclaration`s (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L60`, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L91`).
- **Mass and material:** static, zero mass; contact material restitution 1, bounce threshold 0.1 m/s, friction 0.3 (`reference/cpu/MachinePart.cs@a6c914e:L236-L236`) via `ContactMaterialDeclaration` (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L54`).
- **Constraints and joints:** none.
- **Typed ports:** optical `Main` at (−0.18, 0, 0), normal −X, finite disc radius 0.55, Absorb, front only; electrical `PowerIn` (Input) at (0, −0.6, 0.65) and `Supply` (Output) at (0, −0.6, −0.65) (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L9`).
- **Sensors and activation:** matches when B ≥ threshold **and** B ≥ 0.9 × (R + G + B), with power summed over all emitters in one snapshot; while matched the contact routes `PowerIn` → `Supply`. No hysteresis; light never creates electricity.
- **Work and energy stores:** none.
- **Parameters:** `threshold` — f32, 0.05–2, default 0.25, game optical power (`ReceiverParameter.Threshold`). Fixed purity 0.9; channel fixed at `Blue`. Mode: "threshold. OpticalChannel=Blue".
- **Cosmetic curves and UI bindings:** disc eases slate `#556573` → gold `#f7cb52` over 0.125 s smoothstep from committed activity (endpoint drive); blue marks show the channel separately from activity ([DESIGN.md](../../../DESIGN.md#colour-optics)).
- **Art (DESIGN.md palette):** cream plate `#fff8e9`, navy foot `#293954`, disc r 0.55, cream ring r 0.33, gold centre r 0.08, three blue bar marks `#5b9cdb` at (−0.15, 0.6, 0), gold port studs.
- **Catalogue and inventory entry:** id `blue_receiver`, title "Blue receiver", category Optics, colour (1, 0.97, 0.91), parameters `{"threshold": 0.25}`. Description: "Requires at least 90% blue light and sufficient channel power. Switches a separate electrical supply." Icon `ui/WorkshopIcons.cs@a6c914e:L73-L73` (current, kept).

### Variants

One; red and green receivers are separate identities. The three independent negative controls are proof cases.

## 3. Engine capabilities

Binding shard ([element-02.json](../../coverage/engine/element-02.json)): FiniteLedger, GeometryQuery, OpticalAbsorption, OpticalTransport, SensibleHeat, SignalPropagation, StateTransaction ([element-map row](../general-engine-element-map.md) omits StateTransaction).

**Exists now:** static body, colliders, material (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L60`, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L91`); `PowerIn`/`Supply` sockets (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L11`).

**Missing:** OpticalTransport Absorb aperture with snapshot readings — Story 13.2 (S484: S485, S486, S488, [decisions](../invest/decisions.md#s484)); switched electrical contact — Story 8.1 plus 13.2 (S257); OpticalAbsorption/SensibleHeat — S489/S543, mode-specific; `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`).

**Element dependencies:** a blue source (EL-178, or amber laser through EL-145), CAT-005 battery, a load (CAT-035 lamp), CAT-066 wall.

## 4. Sources and legacy

**Sources.** Row [element-148](../requirements.md#element-148); [todo-300](../requirements.md#todo-300); CAT row [current-cat-012](../requirements.md#current-cat-012); refinement **optical-pairings** (S694); campaign first use 51–60 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)).

**Legacy** (shared optical facts S1–S7 in [CAT-036-laser](CAT-036-laser.md#legacy-harvest); receiver body, contact, indicator and tests harvested in full in [EL-146](EL-146-red-selective-receiver.md#4-sources-and-legacy) and apply unchanged):
1. Receiver body, aperture, ports, route, threshold validation and indicator — `parts/LightReceiverPart.cs@a6c914e:L11-L59`. Carry forward behaviour; not the per-part callback or Godot materials.
2. Blue mask (0, 0, 1), strength = B, purity 0.9 — `engine/OpticalColour.cs@a6c914e:L10-L16`, `engine/OpticalColour.cs@a6c914e:L35-L48`. Carry forward.
3. Catalogue entry and scene (Colour = 3) — `parts/catalog/blue_receiver.tres@a6c914e:L6-L12`, `parts/scenes/blue_receiver.tscn@a6c914e:L1-L5`. Carry forward the entry.
4. Acceptance: purity and threshold boundaries for blue (0.249 off, 0.25 on, 0.9 + 0.051 off, 0.91 + 0.045 on), separate supply and Reset — `CuriousContraptions.tests/ColourOpticsTests.cs@a6c914e:L128-L168`; contact binding — `CuriousContraptions.tests/ElectricalContactBindingTests.cs@a6c914e:L85-L111`; indicator — `CuriousContraptions.tests/ReceiverAnimationTests.cs@a6c914e:L44-L102`; rollback — `CuriousContraptions.tests/OpticalRuntimeCheckpointTests.cs@a6c914e:L30-L93`. Carry forward.

**Files harvested:** `parts/LightReceiverPart.cs`, `parts/catalog/blue_receiver.tres`, `parts/scenes/blue_receiver.tscn`, `engine/OpticalColour.cs`, `engine/MachineData.cs`, `CuriousContraptions.tests/ColourOpticsTests.cs`, `CuriousContraptions.tests/ElectricalContactBindingTests.cs`, `CuriousContraptions.tests/ReceiverAnimationTests.cs`, `CuriousContraptions.tests/OpticalRuntimeCheckpointTests.cs`.

## 5. Acceptance outline

- **Chrome UI recipe:** place a battery, a triggered amber laser, a blue filter, the blue receiver and a lamp from the actual drawer; wire battery → laser, battery → receiver `PowerIn`, receiver `Supply` → lamp; aim with the gizmo. Run.
- **Positive:** filtered blue (0.32 from amber, just above the 0.25 default) lights the disc and lamp.
- **Negative / controls (independent):** (a) missing blue — beam blocked, or the filtered amber blue split 50/50 by a splitter (0.16); (b) wrong channel — red-only or green-only beam; (c) absent supply — disc lights, lamp off. Also unfiltered amber (B only 15% of total), back face, occluder.
- **Boundaries:** 0.25 / 0.249; purity 0.91 / 0.898; threshold edits 0.05–2; disc edge 0.55 m.
- **Run/Reset:** Reset clears reading, contact and indicator. **Save/Load:** threshold, placement, wiring.
- **Integrations:** Story 13.2 suite `tools/e2e/cat-receivers-primary.test.ts`. Binding criteria: [element-148](../requirements.md#element-148), [current-cat-012](../requirements.md#current-cat-012).

## 6. Open questions

1. "Declared thresholds" versus one authorable threshold and fixed purity (as EL-146 question 1).
2. No hysteresis at the threshold (as EL-146 question 2); owner S486.
3. Heating from absorbed power; owner S489/S543.
4. Confirm CAT-012 alone satisfies EL-148.
