# CAT-031 · green_filter — declaration readiness spec (CAT-031-D)

## Identity

- **CAT ID / kind:** CAT-031 · `green_filter` (catalogue title "Green filter", category Optics).
- **Requirement anchor:** [CAT-031](../requirements.md#current-cat-031); retained behaviour [todo-297](../requirements.md#todo-297). Mode record: OpticalChannel = Green.
- **Mapped identities:** EL-144 Green optical filter (owner S499).
- **Roadmap story:** 13.4 (Spectral Color Bandpass Filters, shared with CAT-055 red and CAT-011 blue).
- **Status:** not started.

## Declaration

**Filter family (shared with CAT-055 red and CAT-011 blue).** One frame, one pane and one two-sided Filter aperture whose per-channel transmission is the channel mask. A filter keeps only energy already present in its channel; it never recolours, never gains, never creates a missing channel. Each variant is a separate catalogue entry with its own channel. Lengths are legacy scene units; the current engine treats scene units as metres (`MetreVector`).

**Green variant values:** channel Green; transmission mask (0, 1, 0); ink `#62aa78` (green, the gizmo Y accent); **two** raised green bars; scene enum value `Colour = 2`. An amber laser (1, 0.78, 0.32) leaves as (0, 0.78, 0).

- **Bodies and shapes:** one static rigid body. Opaque cream frame: rails 0.18 × 0.18 × 1.65 at (0, ±0.74, 0) and posts 0.18 × 1.3 × 0.18 at (0, 0, ±0.74); opaque base 0.9 × 0.2 × 1.65 at (0, −1, 0). Transparent collidable pane, half-extents (0.015, 0.65, 0.65) at the origin: it stops balls and passes light.
- **Optical aperture:** port `Main`, interaction Filter, centre (0, 0, 0), normal (−1, 0, 0), disc radius 0.65, **both faces**, transmission (0, 1, 0). The ray continues straight.
- **Mass and material:** none — static body.
- **Constraints and joints:** none.
- **Typed sockets and ports:** none.
- **Sensors and activation:** none.
- **Work and energy stores:** none.
- **Parameters:** the channel is fixed by the catalogue entry (closed enum `OpticalColour`, admitted values Red/Green/Blue only; any other value is rejected). No player-adjustable parameter.
- **Cosmetic curves and UI bindings:** none animated. Selected in construction it previews the filtered path (composition "Separate"), non-activating. Running single-channel beams use the channel ink; beam opacity follows the maximum channel power. Selection pick radius 1.2 (`parts/ColourFilterPart.cs@a6c914e:L22-L22`).
- **Art:** cream frame `#fff8e9`, navy base `#293954`, translucent green pane (`#62aa78`, alpha 0.3, 0.03 × 1.3 × 1.3, no shadow, double-sided), channel bars (0.015 × 0.1 × 0.04) on the frame top at (±0.1, 0.74, 0). See [DESIGN.md](../../../DESIGN.md#colour-optics-addition--27-september-2026).
- **Catalogue and inventory entry:** id `green_filter`, title "Green filter", category Optics, colour (1, 0.97, 0.91), description "Passes only the green energy already present in a beam. Other channels are absorbed." Icon `ui/WorkshopIcons.cs@a6c914e:L65-L65` (two bars; current, kept). No legacy level uses it.

## Engine capabilities

Families (map row CAT-031): ContactImpulse, EnvironmentState, FiniteLedger, GeometryQuery, OpticalAbsorption, OpticalTransport, RigidBodyDynamics, SensibleHeat, SlidingFriction.

**Exists now**
- Static box body/colliders: `engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L60`, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L91`. No optical opacity flag yet.
- Binary16 debt: at a6c914e `MetreVector`, `LinearSpeed` (the contact-trigger threshold type) and `AccelerationVector` are `Half` — `engine/gpu/PhysicsDeclarations.cs@a6c914e:L18-L21`; `RigidLocalPose` translation is a `Half` vector bounded to ±16 — `engine/gpu/PhysicsDeclarations.cs@a6c914e:L31-L38`; `WorkshopCosmeticSample.Blend` is `Half` — `engine/gpu/WorkshopCosmetic.cs@a6c914e:L10-L10`. These are remaining lanes in the [f32 migration status](../../gpu-f32-physics.md#f32-migration-status); this element's new values are declared as f32.

**Missing**
- OpticalTransport per-channel transmission at a two-sided aperture — Story 13.4. Decision owner **S484** ([decisions](../invest/decisions.md#s484)): S485 finite-colour, S488 transparent pane versus opaque frame.
- OpticalAbsorption of the removed channels; SensibleHeat of absorbed light — S489/S543, mode-specific.
- Closed `OpticalChannel` enum in the current declaration model — Story 13.4.
- No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`).

**Element dependencies:** CAT-036 laser (Story 13.1), CAT-032 green_receiver (observer; Story 13.2), CAT-005 battery (Story 8.1), CAT-055/CAT-011 (unlike-stack controls), CAT-001 ball (pane collision control; delivered).

## Legacy harvest

Shared optical transport facts (identical in every narrow-ray Epic 13 spec):
- S1. Ports and interactions are closed enums: `OpticalPortId {Main, First, Second, Third, Carrier}`, `OpticalInteraction {Absorb, Mirror, Split, Filter, Route}` — `engine/OpticalNetwork.cs@a6c914e:L8-L13`. Carry forward as enums.
- S2. Limits: 16 interactions per path, 128 segments per emitter, mirror retention 0.95 — `engine/OpticalNetwork.cs@a6c914e:L31-L33`. Carry forward.
- S3. Aperture validation: unique port ids, finite centre/normal, radius > 0, transmission per channel in [0, 1], Route requires a finite directed outlet; invalid declarations are rejected, never substituted — `engine/OpticalNetwork.cs@a6c914e:L38-L67`; restated by `CuriousContraptions.tests/OpticalPortsTests.cs@a6c914e:L110-L141`. Carry forward.
- S4. A ray stops when remaining range ≤ 1e-4 or power² < 1e-8; split/filter apertures accept either face, all others front face only; hit = nearest finite disc within the opaque-geometry distance — `engine/OpticalNetwork.cs@a6c914e:L79-L104`. Carry forward.
- S5. All emitters are traced from one captured snapshot (ordered by part id) and every receiver reading is committed together after tracing — `engine/OpticalNetwork.cs@a6c914e:L153-L183`; `CuriousContraptions.tests/OpticalPortsTests.cs@a6c914e:L91-L109`. Carry forward the behaviour; do not carry forward the CPU static solve or per-part callbacks.
- S6. Optical readings commit before the electrical solve that reads them. Legacy realised this with the CPU call sequence part pre-network hooks → cone light → optical → acoustic → electrical — `reference/cpu/MachineWorld.cs@a6c914e:L855-L861`. Carry forward the ordering rule (required by [todo-290](../requirements.md#todo-290), assumed by [todo-285](../requirements.md#todo-285)); do not carry forward the CPU call sequence. S486 owns the conflict with the current pipeline, which puts electrical in Phase 1 and optics in Phase 2 ([docs/gpu-f32-physics.md](../../gpu-f32-physics.md#solver-model)).
- S7. Optical path identity is a string (`OpticalPathOwner`) — `engine/OpticalNetwork.cs@a6c914e:L14-L22`. Do not carry forward: string-typed identity; use a typed id.
- S8. Occlusion uses opaque colliders only: Light (and Sound) traces query each body's opaque subset, so transparent panes and hollow bores pass light while frames, walls and balls block it — `engine/physics/BodyQueryGeometry.cs@a6c914e:L74-L79`, `engine/WorldGeometry.cs@a6c914e:L173-L190`. The emitter's own geometry and apertures are skipped only on the first segment (depth 0); every reflected, split, filtered or routed segment can be blocked by, or strike, its own emitter — `engine/OpticalNetwork.cs@a6c914e:L82-L82`, `engine/OpticalNetwork.cs@a6c914e:L89-L89`. This is the mechanism behind returning-ray occlusion. Carry forward.

Filter family facts:
1. Channel enum `OpticalColour {Broadband, Red, Green, Blue, Yellow, Cyan, Magenta, White}` and masks (Green = (0, 1, 0)) — `engine/OpticalColour.cs@a6c914e:L6-L22`. Carry forward as a closed enum.
2. Channel inks (Green `#62aa78`) — `engine/OpticalColour.cs@a6c914e:L23-L34`; single-channel beam ink equals the channel ink — `engine/OpticalColour.cs@a6c914e:L49-L60`. Carry forward.
3. Channel bars: one row per required channel; bar count = channel ordinal (Green 2) — `engine/OpticalColour.cs@a6c914e:L61-L76`. Carry forward the count rule; do not carry forward ordinal arithmetic (state counts explicitly).
4. Aperture `Main`, Filter, origin, normal −X, radius 0.65, transmission = mask — `parts/ColourFilterPart.cs@a6c914e:L10-L12`. Carry forward.
5. Only Red/Green/Blue admitted; anything else rejected — `parts/ColourFilterPart.cs@a6c914e:L15-L19`. Carry forward.
6. Filter is two-sided; the straight branch carries power × transmission — `engine/OpticalNetwork.cs@a6c914e:L94-L97`, `engine/OpticalNetwork.cs@a6c914e:L126-L126`. Carry forward.
7. Frame, pane (alpha 0.3), bars at (±0.1, 0.74, 0) — `parts/ColourFilterPart.cs@a6c914e:L20-L42`; transparent proxy flag — `reference/cpu/MachinePart.cs@a6c914e:L11-L11`; the pane passes light because light traces query only opaque children (S8). Carry forward geometry.
8. Green variant: catalogue entry — `parts/catalog/green_filter.tres@a6c914e:L6-L11`; scene sets `Colour = 2` — `parts/scenes/green_filter.tscn@a6c914e:L1-L5`. Carry forward the entry and channel; not the Godot scene.
9. Acceptance (Green, front and back): a filtered laser gives 2 segments and a single reception at the green receiver equal to source × (0, 1, 0); the receiver activates but supplies nothing without a battery; preview does not activate the laser; a source lacking green yields 1 segment and no reception; rotating the filter (23°, 37°, 11°) keeps the same reception — `CuriousContraptions.tests/ColourOpticsTests.cs@a6c914e:L69-L103`. Carry forward.
10. Acceptance (Green then Red): unlike stacked filters extinguish (no reception, 2 segments); changing the second filter to Green gives 3 segments and the reception equals the first filtered segment (no amplification) — `CuriousContraptions.tests/ColourOpticsTests.cs@a6c914e:L104-L127`. Carry forward.
11. Acceptance: a ray 0.74 above centre stops at the frame before x = 0; a ball at 4 /s collides with the pane and rebounds; Reset restores the construction and the replay is identical — `CuriousContraptions.tests/ColourOpticsTests.cs@a6c914e:L169-L212`. Carry forward.
12. Acceptance (art): the green beam ink equals the channel ink and stays visible without red power (alpha 0.223–0.225 at channel power 0.32) — `CuriousContraptions.tests/ColourOpticsTests.cs@a6c914e:L213-L230`. Carry forward the visibility rule; not the Godot material.
13. Acceptance: the selected green-filter preview matches the trace with miss/zero-power/deselect/Run/Reset/Load controls — `CuriousContraptions.tests/OpticalPreviewPresentationTests.cs@a6c914e:L45-L82`. Carry forward.
14. Integration: green-filtered amber light into a combiner input forms part of a white mix — `CuriousContraptions.tests/BeamCombinerTests.cs@a6c914e:L39-L90`. Carry forward.

**Files harvested:** `parts/ColourFilterPart.cs`, `parts/catalog/green_filter.tres`, `parts/scenes/green_filter.tscn`, `engine/OpticalColour.cs`, `engine/OpticalNetwork.cs`, `engine/WorldGeometry.cs`, `engine/physics/BodyQueryGeometry.cs`, `reference/cpu/MachinePart.cs`, `reference/cpu/MachineWorld.cs`, `CuriousContraptions.tests/ColourOpticsTests.cs`, `CuriousContraptions.tests/OpticalPreviewPresentationTests.cs`, `CuriousContraptions.tests/BeamCombinerTests.cs`, `CuriousContraptions.tests/OpticalPortsTests.cs`.

## Acceptance outline

- **Chrome UI recipe:** in the actual Workshop place a supplied, triggered laser, the green filter on its ray and a green receiver behind it, wired to a supplied load; select the filter to see its preview; Run. Repeat with the filter rotated 180° and at an oblique angle.
- **Positive:** only green energy passes; the green receiver lights; the beam after the filter is green.
- **Negative / controls:** red-only or blue-only input extinguishes; a red filter before the green filter extinguishes (Story 13.4's own control); ray through the opaque frame stops; ball stopped by the pane.
- **Boundaries:** two green filters in series do not amplify; no recolouring of artwork into numerical power.
- **Run/Reset:** Reset clears paths. **Save/Load:** placement and orientation round-trip.
- **Integrations:** combiner, mixed-channel receivers. Binding criteria [CAT-031](../requirements.md#current-cat-031). Suite: `tools/e2e/cat-filters.test.ts`.

## Open questions

1. EL-144 says "with declared loss"; legacy transmits the green channel at exactly 1.0 and the requirement row forbids gain but names no loss. Loss value? Unspecified — owner decision.
2. Story 13.4 tests a "white light beam"; no white emitter exists, and the amber laser carries all three channels. Confirm amber as the source.
3. Observer for Story 13.4: resolved by the 9 Oct 2026 reorder. The Story 13.2 receivers now precede Story 13.4, so the green receiver the requirements name is available. The receivers' own colour source is open scheduling item (d) in `epics.md`.
4. Heating of the pane by absorbed red/blue (SensibleHeat) has no legacy behaviour. Owner S543, mode-specific.
