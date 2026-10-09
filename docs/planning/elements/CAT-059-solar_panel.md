# CAT-059 · solar_panel — declaration readiness spec (CAT-059-D)

## Identity

- **CAT ID / kind:** CAT-059 · `solar_panel` (catalogue title "Solar panel", category Power).
- **Requirement anchor:** [CAT-059](../requirements.md#current-cat-059); retained behaviour [todo-134](../requirements.md#todo-134) (shared with CAT-029).
- **Mapped identities:** EL-211 Solar panel (owner S508). Related but separate: TH-07 solar absorber plate (S573) converts light to heat, not electricity; EL-154 light-charge receiver (S516) stores charge.
- **Roadmap story:** 13.7 (Photovoltaic Solar Panel Energy Conversion).
- **Status:** not started.

## Declaration

Lengths are legacy scene units; the current engine treats scene units as metres (`MetreVector`). The cell face looks along local −X.

- **Bodies and shapes:** one static rigid body. Opaque frame box 0.22 × 1.2 × 1.5 at the origin (full size given to `AddBox`, stored as half-extents). Base plate 0.9 × 0.16 × 1.2 at (0, −0.8, 0) and gold post are artwork only.
- **Light samples (sensor):** nine equal-weight samples (weight 1/9 each) at (−0.13, y, z) with y ∈ {−0.35, 0, 0.35} and z ∈ {−0.45, 0, 0.45}, each with normal (−1, 0, 0).
- **Irradiance law (cone light only):** irradiance = Σ over samples and active cone emitters of intensity × max(0, facing) × weight ÷ max(1, d²), counting only samples within the emitter's range and cone and with clear line of sight through shared opaque geometry. Ambient rendered light contributes nothing.
- **Mass and material:** none — static body.
- **Constraints and joints:** none.
- **Typed sockets and ports:** `Supply` (Electrical, Output) at (0.2, −0.5, 0.6). No input.
- **Sensors and activation:** active when irradiance ≥ threshold; records a Powered event while active. The irradiance reading commits before the electrical solve that reads it (S6).
- **Work and energy stores:** none. Output is a binary electrical source: supplied iff irradiance ≥ 1 (game irradiance). No storage, voltage or current model.
- **Parameters:** none player-adjustable (catalogue `Parameters = {}`). Fixed threshold 1.0 game irradiance.
- **Cosmetic curves and UI bindings:** four meter marks, each slate `#556573` → gold `#f7cb52` over 0.1 s SmoothStep when the committed irradiance (typed unit GameIrradiance) is at least 0.25, 0.5, 0.75 and 1.0 respectively. Four gold marks mean supply is available. Selection pick radius 1 (`parts/SolarPanelPart.cs@a6c914e:L40-L40`).
- **Art:** cream frame `#fff8e9`; nine blue cells 0.025 × 0.29 × 0.39 in the catalogue colour (0.27, 0.39, 0.61) `#45639c` at (−0.125, y, z); navy base `#293954`; gold post (cylinder r 0.1, length 0.3 at (0, −0.65, 0)); meter marks 0.025 × 0.08 × 0.17 at (−0.145, −0.51, −0.3 + 0.2·i); gold supply stud (r 0.085). See [DESIGN.md](../../../DESIGN.md) row "Light and solar".
- **Catalogue and inventory entry:** id `solar_panel`, title "Solar panel", category Power, colour (0.27, 0.39, 0.61), description "Face the blue cells toward an active flashlight. Walls and objects block light; four gold meter marks mean electrical supply is available." Icon `ui/WorkshopIcons.cs@a6c914e:L94-L94` (current, kept). Inventory (1) in `solar_motor`, `solar_shadow`, `delayed_solar`.

## Engine capabilities

Families (map row CAT-059): AnimationEvaluation, AnimationLifecycle, ContactImpulse, ElectricalPower, EnvironmentState, FiniteLedger, GeometryQuery, OpticalAbsorption, OpticalTransport, RigidBodyDynamics, SensibleHeat, SlidingFriction.

**Exists now**
- Static box body/collider: `engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L60`, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L91`.
- `Supply` socket in the Electrical domain: `engine/gpu/WorkshopConnections.cs@a6c914e:L8-L13`.
- Binary16 debt: at a6c914e `MetreVector`, `LinearSpeed` (the contact-trigger threshold type) and `AccelerationVector` are `Half` — `engine/gpu/PhysicsDeclarations.cs@a6c914e:L18-L21`; `RigidLocalPose` translation is a `Half` vector bounded to ±16 — `engine/gpu/PhysicsDeclarations.cs@a6c914e:L31-L38`; `WorkshopCosmeticSample.Blend` is `Half` — `engine/gpu/WorkshopCosmetic.cs@a6c914e:L10-L10`. These are remaining lanes in the [f32 migration status](../../gpu-f32-physics.md#f32-migration-status); this element's new values are declared as f32.

**Missing**
- OpticalTransport cone sampling with occlusion, facing and range (from Story 13.1) and OpticalAbsorption-to-electrical conversion — Story 13.7. Decision owner **S484** ([decisions](../invest/decisions.md#s484)): S485 finite allocation, S486 commit, S488 partial occlusion, S489 absorption.
- ElectricalPower source that a motor can draw — Story 8.1 (network) and Story 11.1 (CAT-042 motor load); S257 typed power versus signal.
- Scalar (irradiance) feedback for the meter — Story 13.7 (P0-022/023).
- SensibleHeat — S543, mode-specific only.
- No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`).

**Element dependencies:** CAT-029 flashlight (Story 13.1; the only legacy light source for the panel), CAT-042 motor (Story 11.1), CAT-001 ball (button strike; delivered), CAT-066 wall (shade; delivered), CAT-063 switch and CAT-022 delay (`delayed_solar`; delivered).

## Legacy harvest

Shared optical facts. The panel uses the cone network, not the narrow-ray network that routes lasers (it has no optical aperture, so lasers never reach it in legacy):
- S6. Optical (cone) readings commit before the electrical solve that reads them. Legacy realised this with the CPU call sequence part pre-network hooks → cone light → narrow-ray optics → acoustic → electrical — `reference/cpu/MachineWorld.cs@a6c914e:L855-L861`. Carry forward the ordering rule (the requirements' snapshot-before-electrical rule, [todo-290](../requirements.md#todo-290)); do not carry forward the CPU call sequence. S486 owns the conflict with the current pipeline, which puts electrical in Phase 1 and optics in Phase 2 ([docs/gpu-f32-physics.md](../../gpu-f32-physics.md#solver-model)).
- C1. Cone emitter and sample records — `engine/LightNetwork.cs@a6c914e:L8-L10`. Carry forward as declaration data.
- C2. Cone law (range, cone, facing, occlusion excluding emitter and receiver, inverse-square with max(1, d²), joint commit, no ambient sky) — `engine/LightNetwork.cs@a6c914e:L12-L52`. Carry forward the law; do not carry forward the CPU static solve.
- C3. The occlusion trace tests only opaque colliders (Light queries each body's opaque subset), so transparent panes and hollow bores pass cone light — `engine/physics/BodyQueryGeometry.cs@a6c914e:L74-L79`, `engine/WorldGeometry.cs@a6c914e:L173-L190`. Carry forward.

Solar facts:
1. Purpose comment: nine equal-area samples, binary supply, no ambient power, storage or voltage/current model — `parts/SolarPanelPart.cs@a6c914e:L8-L9`. Carry forward.
2. Threshold 1; irradiance state checkpointed; scalar observation in typed unit GameIrradiance — `parts/SolarPanelPart.cs@a6c914e:L12-L18`; unit enum — `engine/bridge/ScalarRead.cs@a6c914e:L15-L15`. Carry forward.
3. Meter animation (SmoothStep 0.1 s, slate → gold) and `Supply` port — `parts/SolarPanelPart.cs@a6c914e:L19-L25`. Carry forward.
4. Nine samples, weight 1/9, normal −X — `parts/SolarPanelPart.cs@a6c914e:L26-L35`. Carry forward.
5. Electrical source: supplied iff irradiance ≥ threshold — `parts/SolarPanelPart.cs@a6c914e:L36-L37`. Carry forward.
6. Geometry, cells, meter marks at 1/4, 2/4, 3/4, 4/4 of threshold — `parts/SolarPanelPart.cs@a6c914e:L38-L55`. Carry forward.
7. Activity and Powered event set in a per-part physics hook — `parts/SolarPanelPart.cs@a6c914e:L56-L60`. Do not carry forward the per-element update loop; carry forward the observable result (active ⇔ supplied).
8. Catalogue entry — `parts/catalog/solar_panel.tres@a6c914e:L8-L14`; scene — `parts/scenes/solar_panel.tscn@a6c914e:L1-L6`. Carry forward the entry.
9. Acceptance: just below 1.0 (one f32 step) → no supply; exactly 1.0 → supply; NaN/±∞ readings are rejected and the settled output is kept; just above 1.0 → supply; 0 → none; a rolled-back reading restores supply — `CuriousContraptions.tests/ElectricalSourceTests.cs@a6c914e:L84-L106`. Carry forward (rejection is admission, not tick faulting).
10. Acceptance: each meter mark lights exactly at its threshold from the committed read; it ignores unpublished changes and holds through failure, pause and hide; Reset and Save/Load restore all marks to slate — `CuriousContraptions.tests/SolarAnimationTests.cs@a6c914e:L28-L80`; changed scalar topology (unit, slot, count) rejects before animation — `CuriousContraptions.tests/SolarAnimationTests.cs@a6c914e:L81-L115`; feedback with a missing slot, wrong unit or missing owner rejects before Run — `CuriousContraptions.tests/ScalarObservationTests.cs@a6c914e:L100-L129`. Carry forward behaviour.
11. Acceptance: no light before activation; activated torch → irradiance > 1 and motor turns; reversed panel → 0; panel beyond range → 0; supply is not latched (the motor stops when its supply link is switched off while the panel stays lit) and darkness drops supply — `CuriousContraptions.tests/LightTests.cs@a6c914e:L116-L166`. Carry forward.
12. Acceptance: a rotated wall or heavy weight between torch and panel blocks it; disabling the occluder's participation restores supply — `CuriousContraptions.tests/LightTests.cs@a6c914e:L180-L210`; a small ball occluder reduces irradiance without erasing it — `CuriousContraptions.tests/LightTests.cs@a6c914e:L211-L226`; a panel shifted out of the cone in depth reads 0 — `CuriousContraptions.tests/LightTests.cs@a6c914e:L305-L320`. Carry forward.
13. Acceptance: irradiance and motor travel are independent of part order; Reset clears irradiance and torch — `CuriousContraptions.tests/LightTests.cs@a6c914e:L227-L253`; solved poses and participation drive the reading — `CuriousContraptions.tests/NetworkSpatialOwnershipTests.cs@a6c914e:L135-L163`; a failed tick restores irradiance and supply — `CuriousContraptions.tests/OpticalRuntimeCheckpointTests.cs@a6c914e:L30-L93`. Carry forward.
14. Acceptance (lessons): `solar_motor` and `solar_shadow` win at precision 0, 0.45 and 1, and replay in the same tick count; removing the trigger ball or the wire fails; in `solar_shadow` a panel behind the wall at (−1, 3, 0) reads 0 and fails — `CuriousContraptions.tests/LightTests.cs@a6c914e:L254-L304`. With the panel 1.02 off in depth, placement assistance recovers a win at precision 0 and 0.45 but not 1; at 1.1 only precision 0 wins (corrections 0.25 / 0.1 / 0) — `CuriousContraptions.tests/LightTests.cs@a6c914e:L43-L75`. Carry forward as campaign facts.
15. Levels (published coordinates): `solar_motor` "A little sunshine" (21 / Light into electricity): torch locked at (2, 3, 0), ball above the button, motor at (−4, 1, −2), solution panel at (−1, 3, 0) wired `Supply` → motor `PowerIn`, goal motor turned — `content/puzzles.json@a6c914e:L5944-L6225`; panel assistance knots (position correction 0.25 / 0.1 / 0, rotation 5° / 2° / 0° at precision 0 / 0.45 / 1) — `content/puzzles.json@a6c914e:L6153-L6196`. `solar_shadow` "Out of the shade" (22): 3 × 3 × 0.3 wall `shade` at (0.5, 3, 0), torch at (3, 3, 0), solution panel at (1.5, 3, 0) — `content/puzzles.json@a6c914e:L6226-L6574`, panel knots `content/puzzles.json@a6c914e:L6502-L6545`. `delayed_solar` "A later sunrise" (24): switch → delay → torch activation, panel → motor, goals motor turned and powered at least 1 s after the switch — `content/puzzles.json@a6c914e:L6865-L7293`. Carry forward as lesson set-ups.
16. Campaign authoring of the three lessons, titles and hints — `tools/Campaign/Program.cs@a6c914e:L225-L246`, `tools/Campaign/Program.cs@a6c914e:L266-L277`, `tools/Campaign/Program.cs@a6c914e:L415-L427`. The Campaign module coordinates are mirrored relative to `puzzles.json`: each lesson is added with a 180° yaw (the `("solar_motor", 0, 0, 180)` argument at `tools/Campaign/Program.cs@a6c914e:L415-L417`), so the module's torch (−2, 3, 0), panel (1, 3, 0), motor (4, 1, 2) and shade (−0.5, 3, 0) appear in `puzzles.json` as (2, 3, 0), (−1, 3, 0), (−4, 1, −2) and (0.5, 3, 0). Use the `puzzles.json` coordinates. Carry forward the lesson intent; the tool itself is deleted (Epic 15 rebuilds authoring).

**Files harvested:** `parts/SolarPanelPart.cs`, `parts/catalog/solar_panel.tres`, `parts/scenes/solar_panel.tscn`, `engine/LightNetwork.cs`, `engine/WorldGeometry.cs`, `engine/physics/BodyQueryGeometry.cs`, `engine/bridge/ScalarRead.cs`, `reference/cpu/MachineWorld.cs`, `CuriousContraptions.tests/ElectricalSourceTests.cs`, `CuriousContraptions.tests/SolarAnimationTests.cs`, `CuriousContraptions.tests/ScalarObservationTests.cs`, `CuriousContraptions.tests/LightTests.cs`, `CuriousContraptions.tests/NetworkSpatialOwnershipTests.cs`, `CuriousContraptions.tests/OpticalRuntimeCheckpointTests.cs`, `content/puzzles.json`, `tools/Campaign/Program.cs`.

## Acceptance outline

- **Chrome UI recipe:** load `solar_motor` (or build it in the Workshop): drag the panel from the drawer in front of the torch lens with cells facing it, rotate with the gizmo, wire panel `Supply` → motor `PowerIn` with the connection UI; Run.
- **Positive:** the ball presses the torch button; the meter fills to four gold marks; the motor turns.
- **Negative / controls:** no light source (no trigger); shadow (`solar_shadow` wall); missing wire; backside facing; panel beyond range 8 or outside the 15° cone.
- **Boundaries:** threshold exactly 1.0 versus one f32 step below; partial occlusion reduces, full occlusion zeroes; supply drops immediately when light is blocked.
- **Run/Reset:** Reset clears irradiance, meter and supply. **Save/Load:** placement and wiring round-trip.
- **Integrations:** CAT-029 flashlight, CAT-042 motor, `delayed_solar` activation chain. Binding criteria [CAT-059](../requirements.md#current-cat-059). Suite: `tools/e2e/cat-059.test.ts`.

## Open questions

1. Story 13.7 says "illuminated by a Flashlight or Laser"; legacy panels respond only to the flashlight cone. Must lasers power the panel, and through which law? Owner S484.
2. EL-211 requires "declared efficiency" and "output never exceeds absorbed energy", and the CAT-059 row requires paid downstream work to obey the admitted source law; legacy output is a binary supply with no energy accounting. Finite-power model? Unspecified — owner decision.
3. Intensity 24, threshold 1.0 and the max(1, d²) falloff are legacy game units not stated in requirements. Confirm. Owner S488.
4. Tick order: the requirements commit optical readings before the electrical solve that reads them ([todo-290](../requirements.md#todo-290)); [docs/gpu-f32-physics.md](../../gpu-f32-physics.md#solver-model) puts the electrical solve in Phase 1 and optics in Phase 2. Which order governs the panel's supply? Owner S486.
