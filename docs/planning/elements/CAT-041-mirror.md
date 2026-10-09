# CAT-041 · mirror — declaration readiness spec (CAT-041-D)

## Identity

- **CAT ID / kind:** CAT-041 · `mirror` (catalogue title "Flat mirror", category Optics).
- **Requirement anchor:** [CAT-041](../requirements.md#current-cat-041); retained behaviour [todo-286](../requirements.md#todo-286).
- **Mapped identities:** EL-212 Flat mirror (owner S496; campaign 51–60, practice 61–70, reuse 111–120 and 142).
- **Roadmap story:** 13.5 (Planar Reflection Mirror, Beam Splitter & Beam Combiner, shared with CAT-008 and CAT-006).
- **Status:** not started.

## Declaration

Lengths are legacy scene units; the current engine treats scene units as metres (`MetreVector`). The silvered face looks along local −X.

- **Bodies and shapes:** one static rigid body. Opaque backing box 0.26 × 1.55 × 1.55 at the origin; opaque base box 0.9 × 0.2 × 1.65 at (0, −1, 0).
- **Optical aperture:** port `Main`, interaction Mirror, centre (−0.18, 0, 0), normal (−1, 0, 0), finite disc radius 0.65, front face only. Outside the disc the opaque backing blocks.
- **Mass and material:** none — static body. Optical material: retains 0.95 of every RGB channel per bounce.
- **Constraints and joints:** none.
- **Typed sockets and ports:** none (no electrical or activation ports).
- **Sensors and activation:** none.
- **Work and energy stores:** none; it never adds power.
- **Parameters:** none. Orientation is set through the existing three-axis rotation gizmo.
- **Cosmetic curves and UI bindings:** none animated. When selected in construction, a faint outgoing aim guide shows the would-be reflected path from nearby lasers (even unpowered), without powering receivers (preview composition "Separate"). Selection pick radius 1.2 (`parts/MirrorPart.cs@a6c914e:L14-L14`).
- **Art:** cream backing `#fff8e9`, navy base `#293954`, cyan front face `#66b8c9` (disc r 0.65, thickness 0.035 at (−0.18, 0, 0)), ochre rim `#e8b764` (ring r 0.69, tube 0.025), two cream glints 0.018 × 0.5 × 0.035 at (−0.205, 0, ±0.17) tilted 25° about X. See [DESIGN.md](../../../DESIGN.md) row "Flat mirror".
- **Catalogue and inventory entry:** id `mirror`, title "Flat mirror", category Optics, colour (0.4, 0.72, 0.79), description "Rotate the cyan front face to reflect a laser. Selecting the mirror shows a faint outgoing aim guide from nearby lasers, even before power is connected. The rear and frame absorb light." Icon `ui/WorkshopIcons.cs@a6c914e:L74-L74` (current, kept). No legacy level uses it.

## Engine capabilities

Families (map row CAT-041): ContactImpulse, EnvironmentState, FiniteLedger, GeometryQuery, OpticalAbsorption, OpticalTransport, RigidBodyDynamics, SensibleHeat, SlidingFriction.

**Exists now**
- Static box body/colliders: `engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L60`, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L91`.
- Binary16 debt: at a6c914e `MetreVector`, `LinearSpeed` (the contact-trigger threshold type) and `AccelerationVector` are `Half` — `engine/gpu/PhysicsDeclarations.cs@a6c914e:L18-L21`; `RigidLocalPose` translation is a `Half` vector bounded to ±16 — `engine/gpu/PhysicsDeclarations.cs@a6c914e:L31-L38`; `WorkshopCosmeticSample.Blend` is `Half` — `engine/gpu/WorkshopCosmetic.cs@a6c914e:L10-L10`. These are remaining lanes in the [f32 migration status](../../gpu-f32-physics.md#f32-migration-status); this element's new values are declared as f32.

**Missing**
- OpticalTransport reflection about the transformed normal with per-bounce retention, shared range budget and interaction cap — Story 13.5 (emission itself from Story 13.1). Decision owner **S484** ([decisions](../invest/decisions.md#s484)): S485, S486, S488.
- OpticalAbsorption at back/frame and the absorbed 5% heating — S489/S543 heat coupling; applicable only to a heat mode (mode-specific per the map).
- Construction preview of optical paths — Story 13.5.
- No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`).

**Element dependencies:** CAT-036 laser (source; Story 13.1), CAT-038 light_receiver (observer; Story 13.2), CAT-005 battery (laser supply; Story 8.1), CAT-066 wall (occluder; delivered).

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

Mirror facts:
1. Aperture `Main`, Mirror, centre (−0.18, 0, 0), normal −X, radius 0.65 — `parts/MirrorPart.cs@a6c914e:L9-L9`. Carry forward.
2. Reflection direction d − 2(d·n)n from the transformed normal; branch power × 0.95; the remaining range continues from the hit point; depth increments per interaction — `engine/OpticalNetwork.cs@a6c914e:L112-L127`. Carry forward.
3. Preview composition Separate — `parts/MirrorPart.cs@a6c914e:L10-L11`. Carry forward.
4. Geometry and art — `parts/MirrorPart.cs@a6c914e:L12-L29`. Carry forward.
5. Catalogue entry — `parts/catalog/mirror.tres@a6c914e:L6-L11`; scene binds the script only — `parts/scenes/mirror.tscn@a6c914e:L1-L4`. Carry forward the entry.
6. Acceptance: a 45° mirror reflects a laser 90° into a receiver 3 below; the preview has 2 segments, powers nothing and delivers beam × 0.95 (±1e-5); for any 3D rotation of the whole set the outgoing direction equals the reflection of the incoming about the transformed normal (±1e-5); running lights the receiver with a 2-segment path; Reset empties the path — `CuriousContraptions.tests/MirrorTests.cs@a6c914e:L15-L54`. Carry forward.
7. Acceptance: two mirrors multiply losses (× 0.95²) and share one range budget; with range 4 the total path is 4 ± 0.001 and no receiver is reached — `CuriousContraptions.tests/MirrorTests.cs@a6c914e:L55-L79`. Carry forward.
8. Acceptance: a wall below absorbs the reflected ray (second segment < 1); the reversed mirror's back gives one segment and no reflection; a ray 0.73 off-axis (outside the 0.65 disc, inside the square backing) stops at the backing — `CuriousContraptions.tests/MirrorTests.cs@a6c914e:L80-L109`. A ray reflected back toward the laser can be stopped by the laser's own housing (S8). Carry forward.
9. Acceptance: two facing mirrors with a range-100 ray terminate at 17 segments (16 interactions + 1), power strictly decreasing, no receptions — `CuriousContraptions.tests/MirrorTests.cs@a6c914e:L126-L143`. Carry forward.
10. Acceptance: a placement ghost (not in the world) keeps its preview hidden — `CuriousContraptions.tests/MirrorTests.cs@a6c914e:L110-L125`; invalid preview declarations (missing/foreign target, undefined composition, non-preview visual) reject before any write — `CuriousContraptions.tests/OpticalPreviewBindingTests.cs@a6c914e:L34-L57`. Carry forward behaviour; not the Godot visual types.
11. Acceptance: the selected preview matches the construction trace (endpoints ±2e-5, beam ink); moving the laser off-target or zero preview power shows nothing; deselect hides it; it is hidden during Run and refused outside idle construction; Reset and Save/Load preserve identity — `CuriousContraptions.tests/OpticalPreviewPresentationTests.cs@a6c914e:L45-L82`; preview tracing only in idle construction — `engine/OpticalNetwork.cs@a6c914e:L137-L151`. Carry forward.
12. Acceptance: reflection uses solved mirror/laser/receiver poses, not presentation; disabling the mirror's participation removes the reception — `CuriousContraptions.tests/NetworkSpatialOwnershipTests.cs@a6c914e:L165-L199`. Carry forward.
13. Integration: three mirrors looping a beam back into a combiner keep one interaction budget (17 segments) with strictly decreasing power at each combiner reception — `CuriousContraptions.tests/BeamCombinerTests.cs@a6c914e:L138-L160`. Carry forward.

**Files harvested:** `parts/MirrorPart.cs`, `parts/catalog/mirror.tres`, `parts/scenes/mirror.tscn`, `engine/OpticalNetwork.cs`, `engine/WorldGeometry.cs`, `engine/physics/BodyQueryGeometry.cs`, `reference/cpu/MachineWorld.cs`, `CuriousContraptions.tests/MirrorTests.cs`, `CuriousContraptions.tests/OpticalPreviewBindingTests.cs`, `CuriousContraptions.tests/OpticalPreviewPresentationTests.cs`, `CuriousContraptions.tests/OpticalPortsTests.cs`, `CuriousContraptions.tests/NetworkSpatialOwnershipTests.cs`, `CuriousContraptions.tests/BeamCombinerTests.cs`.

## Acceptance outline

- **Chrome UI recipe:** in the actual Workshop place a supplied, triggered laser, a mirror on its ray and a receiver 3 units off-axis; rotate the mirror 45° with the gizmo; select it and observe the aim guide; Run.
- **Positive:** the reflected ray reaches the receiver at 95% power; the receiver contact closes.
- **Negative / controls:** rear incidence; ray outside the 0.65 disc (hits backing); wall on the reflected leg; returning ray occluded by the laser's housing or the mirror's own mount (S8).
- **Boundaries:** 16-reflection cap; one shared range budget; preview equals committed reflection.
- **Run/Reset:** Reset clears paths; preview reappears only in construction. **Save/Load:** orientation round-trips.
- **Integrations:** CAT-008, CAT-006, filters, gates. Binding criteria [CAT-041](../requirements.md#current-cat-041). Suite: `tools/e2e/cat-041-008-006.test.ts`.

## Open questions

1. Observer for reflection: resolved by the 9 Oct 2026 reorder. The receiver (CAT-038) now arrives in Story 13.2, before Story 13.5, so it is available as the observer.
2. EL-212 cites a "material model"; legacy has only a fixed 0.95 retention. Is any other material parameter required? Unspecified — owner decision.
3. Heating from the absorbed 5% (SensibleHeat in the capability row) has no legacy behaviour. Owner S489/S543, mode-specific.
