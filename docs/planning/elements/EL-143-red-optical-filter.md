# EL-143 · Red optical filter — named-identity spec

Story 7.0 full spec for a named puzzle-element identity. Baseline commit `a6c914e`. Legacy citations use `path@a6c914e:Lstart-Lend` and stay retrievable from git history after the Epic 7 purge. Lengths are legacy scene units, which the current engine treats as metres (`MetreVector`). The pane normal is local −X; the filter works from either face.

The three channel filters (EL-143 red, EL-144 green, EL-145 blue) share one body and one filter law; they differ only in the passed channel, pane ink, channel marks and icon. Each is specified in full in its own file.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-143 |
| Name | Red optical filter |
| Type | Optical |
| Anchor | [requirements.md#element-143](../requirements.md#element-143); named entry [named-elements.md#element-143](../invest/named-elements.md#element-143); source record [todo-297](../requirements.md#todo-297) |
| Owner | S498 |
| Refines CAT | [CAT-055 red_filter](CAT-055-red_filter.md) (`red_filter`, "Red filter"). One catalogue part satisfies both identities; not a second implementation. |
| Related | EL-144, EL-145 (sibling channels); EL-146 red receiver and EL-176 red laser (channel partners); EL-174 prism (separates channels instead of absorbing them) |
| Roadmap story | 13.4 Spectral Color Bandpass Filters (Red, Green, Blue) (CAT-055, CAT-031, CAT-011) |
| Status | not started |

## 2. Declaration

- **Bodies and shapes:** one static rigid body with five opaque boxes and one transparent pane: two horizontal frame bars 0.18 × 0.18 × 1.65 m at (0, ±0.74, 0); two vertical bars 0.18 × 1.3 × 0.18 m at (0, 0, ±0.74); base 0.9 × 0.2 × 1.65 m at (0, −1, 0); pane collider 0.03 × 1.3 × 1.3 m (half-extents 0.015, 0.65, 0.65) at the origin, which collides with bodies but does not occlude light. Pick radius 1.2 m. Types: static `RigidBodyDeclaration` and box `ColliderDeclaration`s (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L60`, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L91`); the pane needs a light-transparent collider flag the current declaration lacks.
- **Mass and material:** static, zero mass. Contact material restitution 1, bounce threshold 0.1 m/s, friction 0.3 (`reference/cpu/MachinePart.cs@a6c914e:L236-L236`) via `ContactMaterialDeclaration` (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L54`).
- **Constraints and joints:** none.
- **Typed ports (optical aperture):** `Main` (closed enum `OpticalPortId`), centre (0, 0, 0), normal −X, finite disc radius 0.65, interaction Filter, transmission (1, 0, 0). Two-sided: either face accepts incidence. Outside the disc the opaque frame blocks. No electrical or activation ports.
- **Sensors and activation:** none.
- **Work and energy stores:** none. Output = input × (1, 0, 0): the existing red channel continues on the same direction with its remaining range; green and blue are absorbed. The filter never creates or recolours a channel.
- **Parameters:** none authorable. Channel fixed at `Red` (closed enum `OpticalColour`; a filter admits only Red, Green or Blue). Declared loss on the passed channel: 0 (legacy transmission 1.0).
- **Cosmetic curves and UI bindings:** none animated. Selected-only faint outgoing aim preview (composition Separate) in idle construction, never activating receivers. Traced red beams use ink `#de7058`; opacity follows maximum channel power ([DESIGN.md](../../../DESIGN.md#colour-optics)).
- **Art (DESIGN.md palette):** cream frame `#fff8e9`; navy foot `#293954`; translucent pane 0.03 × 1.3 × 1.3 m in red accent `#de7058` at alpha 0.3, double-sided, casting no shadow; one raised red bar mark at (±0.1, 0.74, 0) on each frame face (red = one bar).
- **Catalogue and inventory entry:** id `red_filter`, title "Red filter", category Optics, colour (1, 0.97, 0.91). Description: "Passes only the red energy already present in a beam. Other channels are absorbed." Icon `ui/WorkshopIcons.cs@a6c914e:L64-L64` (current, kept). No legacy level places it.

### Variants

One. The row names no variants; green and blue filters are separate identities (EL-144, EL-145).

## 3. Engine capabilities

Binding shard ([element-02.json](../../coverage/engine/element-02.json)): FiniteLedger, GeometryQuery, OpticalAbsorption, OpticalTransport, SensibleHeat, SignalPropagation, StateTransaction ([element-map row](../general-engine-element-map.md) omits StateTransaction).

**Exists now:** static box body and colliders (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L60`, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L91`); contact material (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L54`).

**Missing**
- OpticalTransport Filter interaction (two-sided aperture, per-channel transmission, shared range and interaction budget) and light-transparent solid colliders — Story 13.4 (emission from Story 13.1). Decision owner **S484** ([decisions](../invest/decisions.md#s484)): S485, S486, S488 (transparent and opaque paths).
- OpticalAbsorption and SensibleHeat for the absorbed channels — S489 and S543; applicable only to a heat mode (mode-specific per the map).
- Construction preview of optical paths — Story 13.5.
- No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`).

**Element dependencies:** a multi-channel source (CAT-036 amber laser or EL-213 combiner output; Story 13.1/13.5), CAT-005 battery (Story 8.1), an observer (EL-146 / CAT-056 red receiver or CAT-038; Story 13.2), CAT-066 wall (delivered).

## 4. Sources and legacy

**Sources.** Row [element-143](../requirements.md#element-143): passes only the existing red channel through its finite aperture with declared loss; absent red input cannot be recoloured; opaque frame blocks rays. [todo-297](../requirements.md#todo-297): finite two-sided apertures, transparent solid panes, opaque frames; unlike stacked filters extinguish. CAT row [current-cat-055](../requirements.md#current-cat-055): front, back and rotated paths; red artwork does not recolour numerical power. [todo-309](../requirements.md#todo-309): passive optics cannot amplify. Refinement **optical-pairings** (S694, [refinements](../invest/refinements.md)). Campaign: filters first use 51–60 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)).

**Legacy** (shared optical facts S1–S7 in [CAT-036-laser](CAT-036-laser.md#legacy-harvest)):
1. Aperture `Main` at origin, normal −X, radius 0.65, Filter, transmission = channel mask — `parts/ColourFilterPart.cs@a6c914e:L10-L12`; red mask (1, 0, 0) — `engine/OpticalColour.cs@a6c914e:L11-L16`. Carry forward.
2. Only Red, Green or Blue is a valid filter channel; others reject — `parts/ColourFilterPart.cs@a6c914e:L15-L19`. Carry forward.
3. Frame, base and non-opaque pane collider; pick radius 1.2 — `parts/ColourFilterPart.cs@a6c914e:L20-L29`; `AddBox` stores half the authored size — `reference/cpu/MachinePart.cs@a6c914e:L352-L356`. Carry forward.
4. Pane art, marks and preview — `parts/ColourFilterPart.cs@a6c914e:L30-L42`; inks and mark counts (Red = 1) — `engine/OpticalColour.cs@a6c914e:L23-L34`, `engine/OpticalColour.cs@a6c914e:L61-L76`. Carry forward behaviour; not Godot materials.
5. Filter apertures accept either face; the branch continues straight with power × transmission and the remaining range — `engine/OpticalNetwork.cs@a6c914e:L94-L98`, `engine/OpticalNetwork.cs@a6c914e:L113-L126`. Carry forward.
6. Catalogue entry and scene (Colour = 1) — `parts/catalog/red_filter.tres@a6c914e:L6-L11`, `parts/scenes/red_filter.tscn@a6c914e:L1-L5`. Carry forward the entry.
7. Acceptance: laser → filter → red receiver, front and back (180°): two segments, one reception equal to source × mask; the receiver activates but does not supply without its own supply; a source lacking red gives no reception and one segment; an arbitrary rotation (23°, 37°, 11°) keeps the same reception — `CuriousContraptions.tests/ColourOpticsTests.cs@a6c914e:L69-L103`. Carry forward.
8. Acceptance: red then blue filters extinguish (two segments, no reception); two red filters give three segments and do not amplify (reception = first filtered power) — `CuriousContraptions.tests/ColourOpticsTests.cs@a6c914e:L104-L127`. Carry forward.
9. Acceptance: a ray 0.74 above centre hits the opaque frame and stops before the filter; a ball at 4 m/s bounces off the pane (impact recorded, ball returns), and Reset replays identically — `CuriousContraptions.tests/ColourOpticsTests.cs@a6c914e:L169-L212`. Carry forward.
10. Acceptance: red beam ink equals `#de7058`, and a 0.32-power beam renders with alpha 0.223–0.225 — `CuriousContraptions.tests/ColourOpticsTests.cs@a6c914e:L213-L230`. Carry forward as presentation.
11. Integration: red filter in front of an amber laser feeds the combiner's first input — `CuriousContraptions.tests/BeamCombinerTests.cs@a6c914e:L39-L90`. Carry forward.

**Files harvested:** `parts/ColourFilterPart.cs`, `parts/catalog/red_filter.tres`, `parts/scenes/red_filter.tscn`, `engine/OpticalColour.cs`, `engine/OpticalNetwork.cs`, `reference/cpu/MachinePart.cs`, `CuriousContraptions.tests/ColourOpticsTests.cs`, `CuriousContraptions.tests/BeamCombinerTests.cs`.

## 5. Acceptance outline

- **Chrome UI recipe:** from the actual drawer place a battery, a triggered amber laser, the red filter 3 m along the beam and a red receiver 3 m beyond; wire the battery to the laser and through the receiver to a lamp with the connection UI; rotate the filter with the gizmo, including 180°. Run.
- **Positive:** the receiver reads exactly the source's red channel; the beam beyond the filter draws in red ink.
- **Negative / controls:** a green-or-blue-only source (EL-177/EL-178) gives nothing beyond the filter; red then green/blue filter extinguishes; a ray striking the frame stops; a ball bounces off the pane.
- **Boundaries:** disc edge 0.65 m; stacked identical filters do not amplify; rotated incidence; shared range and interaction budget.
- **Run/Reset:** Reset clears traced beams and any receiver state; the filter itself holds no state. **Save/Load:** placement and orientation round-trip.
- **Integrations:** feeding the combiner, receivers and gates; Story 13.4 suite `tools/e2e/cat-filters.test.ts`. Binding criteria: [element-143](../requirements.md#element-143), [current-cat-055](../requirements.md#current-cat-055).

## 6. Open questions

1. The row requires "declared loss"; legacy passes the red channel at exactly 1.0 (loss 0). Owner to confirm 0 or set a nonzero loss.
2. Story 13.4 tests a "white light beam"; no white emitter exists. The amber laser (1, 0.78, 0.32) or a combiner output must stand in. Owner decision.
3. Heating from the absorbed green and blue power (OpticalAbsorption, SensibleHeat) has no legacy behaviour; owner S489/S543, mode-specific.
4. Confirm CAT-055 alone satisfies EL-143.
