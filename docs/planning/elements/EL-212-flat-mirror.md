# EL-212 · Flat mirror — named-identity spec

Story 7.0 full spec for a named puzzle-element identity. Baseline commit `a6c914e`. Legacy citations use `path@a6c914e:Lstart-Lend` and stay retrievable from git history after the Epic 7 purge. Lengths are legacy scene units, which the current engine treats as metres (`MetreVector`). The silvered face looks along local −X.

EL-212 refines the catalogue mirror [CAT-041 mirror](CAT-041-mirror.md), whose spec holds the full legacy harvest (13 facts). This file restates the declaration and the named identity's own acceptance: a finite front surface reflects according to its normal and material model, while the opaque frame and back stay physical.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-212 |
| Name | Flat mirror |
| Type | Optical |
| Anchor | [requirements.md#element-212](../requirements.md#element-212); named entry [named-elements.md#element-212](../invest/named-elements.md#element-212); scope index [todo-215](../requirements.md#todo-215) |
| Owner | S496 |
| Refines CAT | [CAT-041 mirror](CAT-041-mirror.md) (`mirror`, "Flat mirror"). One catalogue part satisfies both identities; not a second implementation. |
| Related | CAT-008 beam splitter (half-silvered sibling); EL-213 combiner; EL-175 optical fibre (must not erase the mirror challenge); CAT-036 and EL-176 to EL-178 lasers |
| Roadmap story | 13.5 Planar Reflection Mirror, Beam Splitter & Beam Combiner (CAT-041, CAT-008, CAT-006) |
| Status | not started |

## 2. Declaration

- **Bodies and shapes:** one static rigid body: opaque cream backing box 0.26 × 1.55 × 1.55 m at the origin (half-extents 0.13, 0.775, 0.775); opaque navy base 0.9 × 0.2 × 1.65 m at (0, −1, 0). Pick radius 1.2 m. Static `RigidBodyDeclaration`, two box colliders (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L60`, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L91`).
- **Mass and material:** static, zero mass; contact material restitution 1, bounce threshold 0.1 m/s, friction 0.3 (`reference/cpu/MachinePart.cs@a6c914e:L236-L236`). Optical material: front face retains 0.95 of every RGB channel per bounce; the backing and frame absorb.
- **Constraints and joints:** none.
- **Typed ports (optical aperture):** `Main`, interaction Mirror, centre (−0.18, 0, 0) (0.05 m proud of the backing face), normal −X, finite disc radius 0.65, front incidence only. Outside the disc the square backing blocks. No electrical or activation ports.
- **Sensors and activation:** none.
- **Work and energy stores:** none; reflection never adds power.
- **Optical law:** reflected direction d − 2(d·n)n about the transformed normal; branch power × 0.95; the remaining range continues from the hit point; each reflection consumes one of 16 interactions per path.
- **Parameters:** none. Orientation is set through the existing three-axis rotation gizmo. Requirement mode: declared geometry only.
- **Cosmetic curves and UI bindings:** no animation. Selected-only faint outgoing aim guide in idle construction (preview composition Separate), never powering receivers, hidden during Run and on placement ghosts. Running beams render each reflected segment with reduced opacity as power falls; reflection geometry is never eased away from the actual normal.
- **Art (DESIGN.md palette):** cream backing `#fff8e9`, navy foot `#293954`, cyan front disc `#66b8c9` (r 0.65, thickness 0.035), ochre rim `#e8b764` (ring r 0.69, tube 0.025), two cream glints 0.018 × 0.5 × 0.035 at (−0.205, 0, ±0.17) tilted 25° about X; DESIGN.md row "Flat mirror".
- **Catalogue and inventory entry:** id `mirror`, title "Flat mirror", category Optics, colour (0.4, 0.72, 0.79); description "Rotate the cyan front face to reflect a laser. Selecting the mirror shows a faint outgoing aim guide from nearby lasers, even before power is connected. The rear and frame absorb light." Icon `ui/WorkshopIcons.cs@a6c914e:L74-L74` (current, kept). No legacy level uses it.

### Variants

One. The row names no variants; the beam splitter (CAT-008) is a separate catalogue element with no named identity.

## 3. Engine capabilities

Binding shard ([element-03.json](../../coverage/engine/element-03.json)): FiniteLedger, GeometryQuery, OpticalAbsorption, OpticalTransport, SensibleHeat, SignalPropagation, StateTransaction ([element-map row](../general-engine-element-map.md) omits StateTransaction).

**Exists now:** static box body and colliders (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L60`, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L91`).

**Missing**
- OpticalTransport Mirror interaction with per-bounce retention, shared range and interaction budget, and construction preview — Story 13.5 (emission Story 13.1). Decision owner **S484** ([decisions](../invest/decisions.md#s484)): S485, S486, S488.
- OpticalAbsorption/SensibleHeat for the absorbed 5% and back-face light — S489/S543, mode-specific.
- `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`).

**Element dependencies:** a laser (CAT-036 or EL-176 to EL-178; Story 13.1), CAT-005 battery (Story 8.1), a receiver (CAT-038 / EL-214; Story 13.2), CAT-066 wall (delivered).

## 4. Sources and legacy

**Sources.** Row [element-212](../requirements.md#element-212): a finite front surface reflects supported optical power according to its normal and material model; the opaque frame and back remain physical; rear incidence and occluded paths do not reflect through the housing. Campaign: first use 51–60; practice 61–70; reuse 111–120 and 142 as separate objectives ([campaign-reservations](../invest/campaign-reservations.md)). CAT row [current-cat-041](../requirements.md#current-cat-041): 95% RGB per bounce, one range budget, up to 16 reflections, back/frame/mount/returning-ray occlusion, preview equals committed reflection.

**Legacy** (shared optical facts S1–S7 in [CAT-036-laser](CAT-036-laser.md#legacy-harvest); full mirror harvest in [CAT-041-mirror](CAT-041-mirror.md#legacy-harvest); key facts):
1. Aperture `Main` (−0.18, 0, 0), −X, radius 0.65, Mirror; preview composition Separate — `parts/MirrorPart.cs@a6c914e:L9-L11`. Carry forward.
2. Geometry and art (backing and base authored full sizes; `AddBox` stores halves) — `parts/MirrorPart.cs@a6c914e:L12-L29`; `reference/cpu/MachinePart.cs@a6c914e:L352-L356`. Carry forward.
3. Reflection law, retention 0.95, range continuation, depth increment; 16 interactions — `engine/OpticalNetwork.cs@a6c914e:L31-L33`, `engine/OpticalNetwork.cs@a6c914e:L112-L127`. Carry forward.
4. Front-face-only acceptance for non-split apertures — `engine/OpticalNetwork.cs@a6c914e:L94-L98`. Carry forward (rear incidence control).
5. Catalogue entry — `parts/catalog/mirror.tres@a6c914e:L6-L11`. Carry forward.
6. Acceptance: 45° mirror reflects 90° into a receiver 3 below with power × 0.95; reflection follows any 3D rotation; Reset empties the path — `CuriousContraptions.tests/MirrorTests.cs@a6c914e:L15-L54`; two mirrors × 0.95² share one range budget — `CuriousContraptions.tests/MirrorTests.cs@a6c914e:L55-L79`; wall on the reflected leg, reversed mirror and a ray 0.73 off-axis all fail to reflect — `CuriousContraptions.tests/MirrorTests.cs@a6c914e:L80-L109`; facing mirrors stop at 17 segments with falling power — `CuriousContraptions.tests/MirrorTests.cs@a6c914e:L126-L143`. Carry forward.
7. Acceptance: preview hidden for placement ghosts, matches the committed trace, hidden during Run — `CuriousContraptions.tests/MirrorTests.cs@a6c914e:L110-L125`, `CuriousContraptions.tests/OpticalPreviewPresentationTests.cs@a6c914e:L45-L82`. Carry forward.

**Files harvested:** `parts/MirrorPart.cs`, `parts/catalog/mirror.tres`, `engine/OpticalNetwork.cs`, `reference/cpu/MachinePart.cs`, `CuriousContraptions.tests/MirrorTests.cs`, `CuriousContraptions.tests/OpticalPreviewPresentationTests.cs`.

## 5. Acceptance outline

- **Chrome UI recipe:** in the actual Workshop place a supplied, triggered laser, the mirror on its ray and a receiver 3 m off-axis; rotate the mirror 45° with the gizmo; select it and observe the aim guide; Run.
- **Positive:** the reflected ray reaches the receiver at 95% power; the receiver contact closes.
- **Negative / controls:** (named) rear incidence — the beam stops on the back; an occluded path (wall on either leg) — nothing reflects through the housing; ray outside the 0.65 m disc stops on the backing.
- **Boundaries:** 16-reflection cap; one shared range budget; preview equals committed reflection; arbitrary 3D orientation.
- **Run/Reset:** Reset clears paths; preview reappears only in construction. **Save/Load:** orientation round-trips.
- **Integrations:** practice and reuse objectives (61–70, 111–120, 142); CAT-008, EL-213, filters, gates. Story 13.5 suite `tools/e2e/cat-041-008-006.test.ts`. Binding criteria: [element-212](../requirements.md#element-212), [current-cat-041](../requirements.md#current-cat-041).

## 6. Open questions

1. "Material model": legacy has only fixed 0.95 retention for all channels. Is a per-channel or authorable reflectance required? Owner decision (CAT-041 question 2).
2. Heating from the absorbed 5% and from back-face light has no legacy behaviour; owner S489/S543.
3. Observer for Story 13.5: resolved by the 9 Oct 2026 reorder; the receivers (Story 13.2) now precede it (CAT-041 question 1).
4. Confirm CAT-041 alone satisfies EL-212.
