# EL-144 · Green optical filter — named-identity spec

Story 7.0 full spec for a named puzzle-element identity. Baseline commit `a6c914e`. Legacy citations use `path@a6c914e:Lstart-Lend` and stay retrievable from git history after the Epic 7 purge. Lengths are legacy scene units, which the current engine treats as metres (`MetreVector`). The pane normal is local −X; the filter works from either face.

The three channel filters (EL-143 red, EL-144 green, EL-145 blue) share one body and one filter law; they differ only in the passed channel, pane ink, channel marks and icon.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-144 |
| Name | Green optical filter |
| Type | Optical |
| Anchor | [requirements.md#element-144](../requirements.md#element-144); named entry [named-elements.md#element-144](../invest/named-elements.md#element-144); source record [todo-297](../requirements.md#todo-297) |
| Owner | S499 |
| Refines CAT | [CAT-031 green_filter](CAT-031-green_filter.md) (`green_filter`, "Green filter"). One catalogue part satisfies both identities; not a second implementation. |
| Related | EL-143, EL-145 (sibling channels); EL-147 green receiver and EL-177 green laser (channel partners); EL-174 prism |
| Roadmap story | 13.4 Spectral Color Bandpass Filters (Red, Green, Blue) (CAT-055, CAT-031, CAT-011) |
| Status | not started |

## 2. Declaration

- **Bodies and shapes:** one static rigid body: two horizontal frame bars 0.18 × 0.18 × 1.65 m at (0, ±0.74, 0); two vertical bars 0.18 × 1.3 × 0.18 m at (0, 0, ±0.74); base 0.9 × 0.2 × 1.65 m at (0, −1, 0), all opaque; pane collider 0.03 × 1.3 × 1.3 m at the origin that collides with bodies but does not occlude light. Pick radius 1.2 m. Types: static `RigidBodyDeclaration` and box `ColliderDeclaration`s (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L60`, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L91`); the pane needs a light-transparent collider flag.
- **Mass and material:** static, zero mass. Contact material restitution 1, bounce threshold 0.1 m/s, friction 0.3 (`reference/cpu/MachinePart.cs@a6c914e:L236-L236`) via `ContactMaterialDeclaration` (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L54`).
- **Constraints and joints:** none.
- **Typed ports (optical aperture):** `Main` (`OpticalPortId`), centre (0, 0, 0), normal −X, finite disc radius 0.65, Filter, transmission (0, 1, 0); either face accepts incidence; the frame blocks outside the disc. No electrical or activation ports.
- **Sensors and activation:** none.
- **Work and energy stores:** none. Output = input × (0, 1, 0) on the same direction with the remaining range; red and blue are absorbed; nothing is recoloured or created.
- **Parameters:** none authorable. Channel fixed at `Green` (`OpticalColour`; only Red, Green or Blue admitted). Declared loss on the passed channel: 0 (legacy transmission 1.0).
- **Cosmetic curves and UI bindings:** none animated. Selected-only faint aim preview (Separate) in idle construction, never activating. Traced green beams use ink `#62aa78`; opacity follows maximum channel power ([DESIGN.md](../../../DESIGN.md#colour-optics)).
- **Art (DESIGN.md palette):** cream frame `#fff8e9`; navy foot `#293954`; translucent pane in green accent `#62aa78` at alpha 0.3, double-sided, no shadow; two raised green bar marks on each frame face (green = two bars).
- **Catalogue and inventory entry:** id `green_filter`, title "Green filter", category Optics, colour (1, 0.97, 0.91). Description: "Passes only the green energy already present in a beam. Other channels are absorbed." Icon `ui/WorkshopIcons.cs@a6c914e:L65-L65` (current, kept). No legacy level places it.

### Variants

One; red and blue filters are separate identities (EL-143, EL-145).

## 3. Engine capabilities

Binding shard ([element-02.json](../../coverage/engine/element-02.json)): FiniteLedger, GeometryQuery, OpticalAbsorption, OpticalTransport, SensibleHeat, SignalPropagation, StateTransaction ([element-map row](../general-engine-element-map.md) omits StateTransaction).

**Exists now:** static box body and colliders (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L60`, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L91`); contact material (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L54`).

**Missing**
- OpticalTransport Filter interaction and light-transparent solid colliders — Story 13.4 (emission Story 13.1). Decision owner **S484** ([decisions](../invest/decisions.md#s484)): S485, S486, S488.
- OpticalAbsorption and SensibleHeat for absorbed channels — S489, S543 (mode-specific).
- Construction preview — Story 13.5. No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`).

**Element dependencies:** a source carrying green (CAT-036 amber laser, EL-177, EL-213 output; Story 13.1/13.5), CAT-005 battery (Story 8.1), an observer (EL-147 / CAT-032 or CAT-038; Story 13.2), CAT-066 wall (delivered).

## 4. Sources and legacy

**Sources.** Row [element-144](../requirements.md#element-144): passes only the existing green channel with declared loss; absent green input cannot be recoloured; opaque frame blocks rays. [todo-297](../requirements.md#todo-297). CAT row [current-cat-031](../requirements.md#current-cat-031). [todo-309](../requirements.md#todo-309). Refinement **optical-pairings** (S694). Campaign: filters first use 51–60 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)).

**Legacy** (shared optical facts S1–S7 in [CAT-036-laser](CAT-036-laser.md#legacy-harvest); filter body and law harvested in full in [EL-143](EL-143-red-optical-filter.md#4-sources-and-legacy), unchanged here):
1. Aperture and Filter transmission = mask — `parts/ColourFilterPart.cs@a6c914e:L10-L12`; green mask (0, 1, 0) — `engine/OpticalColour.cs@a6c914e:L15-L15`. Carry forward.
2. Channel validation; frame, pane and art — `parts/ColourFilterPart.cs@a6c914e:L15-L42`; green ink `#62aa78` and two marks — `engine/OpticalColour.cs@a6c914e:L27-L27`, `engine/OpticalColour.cs@a6c914e:L61-L76`. Carry forward behaviour; not Godot materials.
3. Two-sided straight transmission — `engine/OpticalNetwork.cs@a6c914e:L94-L98`, `engine/OpticalNetwork.cs@a6c914e:L113-L126`. Carry forward.
4. Catalogue entry and scene (Colour = 2) — `parts/catalog/green_filter.tres@a6c914e:L6-L11`, `parts/scenes/green_filter.tscn@a6c914e:L1-L5`. Carry forward the entry.
5. Acceptance: front, back and rotated green pass equals source × (0, 1, 0); a source lacking green yields no reception — `CuriousContraptions.tests/ColourOpticsTests.cs@a6c914e:L69-L103`; green then red extinguishes, two greens do not amplify — `CuriousContraptions.tests/ColourOpticsTests.cs@a6c914e:L104-L127`; frame stops an off-centre ray and the pane bounces a ball — `CuriousContraptions.tests/ColourOpticsTests.cs@a6c914e:L169-L212`; green beam ink stays visible without red — `CuriousContraptions.tests/ColourOpticsTests.cs@a6c914e:L213-L230`. Carry forward.
6. Integration: green-filtered laser feeds the combiner's second input — `CuriousContraptions.tests/BeamCombinerTests.cs@a6c914e:L39-L90`. Carry forward.

**Files harvested:** `parts/ColourFilterPart.cs`, `parts/catalog/green_filter.tres`, `parts/scenes/green_filter.tscn`, `engine/OpticalColour.cs`, `engine/OpticalNetwork.cs`, `reference/cpu/MachinePart.cs`, `CuriousContraptions.tests/ColourOpticsTests.cs`, `CuriousContraptions.tests/BeamCombinerTests.cs`.

## 5. Acceptance outline

- **Chrome UI recipe:** place a battery, a triggered amber laser, the green filter and a green receiver in line from the actual drawer; wire with the connection UI; rotate the filter with the gizmo, including 180°. Run.
- **Positive:** the receiver reads exactly the source's green channel; the beam beyond the filter draws in green ink.
- **Negative / controls:** a red-only or blue-only source gives nothing beyond the filter; green then red/blue filter extinguishes; frame strike stops the ray; a ball bounces off the pane.
- **Boundaries:** disc edge 0.65 m; identical stacked filters do not amplify; rotated incidence; range and interaction budget.
- **Run/Reset:** Reset clears traced beams. **Save/Load:** placement and orientation round-trip.
- **Integrations:** combiner, receivers, gates; Story 13.4 suite `tools/e2e/cat-filters.test.ts`. Binding criteria: [element-144](../requirements.md#element-144), [current-cat-031](../requirements.md#current-cat-031).

## 6. Open questions

1. "Declared loss" versus legacy loss 0 (as EL-143 question 1).
2. Story 13.4 "white light beam" has no white emitter (as EL-143 question 2).
3. Heating from absorbed red and blue power; owner S489/S543, mode-specific.
4. Confirm CAT-031 alone satisfies EL-144.
