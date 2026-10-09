# CAT-029 · flashlight — declaration readiness spec (CAT-029-D)

## Identity

- **CAT ID / kind:** CAT-029 · `flashlight` (catalogue title "Flashlight", category Power).
- **Requirement anchor:** [CAT-029](../requirements.md#current-cat-029); retained behaviour [todo-134](../requirements.md#todo-134) (shared with CAT-059).
- **Mapped identities:** EL-210 Flashlight (owner S495). Related but distinct: EL-153 general-light receiver (S515) samples flashlight cones; it is a separate identity.
- **Roadmap story:** 13.1 (Flashlight Torch & Collimated Laser Emitters, shared with CAT-036).
- **Status:** not started.

## Declaration

Lengths are legacy scene units; the current engine treats scene units as metres (`MetreVector`). Local +X is the beam direction.

- **Bodies and shapes:** one static rigid body with three opaque boxes: body 1.0 × 0.6 × 0.6 at (−0.1, 0, 0); lens collar 0.25 × 0.86 × 0.86 at (0.5, 0, 0); top button 0.4 × 0.14 × 0.35 at (−0.15, 0.36, 0). The base plate (1.2 × 0.12 × 0.85 at (0, −0.38, 0)) is artwork only. Lens point (0.66, 0, 0).
- **Mass and material:** none — static body. During placement assistance the body moves kinematically toward its target; at strict precision it stays static.
- **Constraints and joints:** none.
- **Typed sockets and ports:** activation input only (the part accepts activation; `delayed_solar` wires delay `ActivationOut` → torch `ActivationIn`). No electrical port: the battery is self-contained.
- **Sensors and activation:** a contact trigger on the top button. A contact latches the torch on when the approach speed ≥ the part's assistance `trigger_threshold` and the local contact point lies in y > 0.3, |x + 0.15| < 0.45, |z| < 0.4. An activation command also latches it on. On stays latched until Reset; there is no off command.
- **Work and energy stores:** none in legacy (self-contained supply is unbounded). See Open questions.
- **Optical emitter (cone):** origin lens point, direction +X, range 8, cone half-angle 15° (cosine 0.9659258), intensity 24 (game unit, not lumens).
- **Parameters:** none (catalogue `Parameters = {}`).
- **Cosmetic curves and UI bindings:** button translates −0.06 on Y over 0.06 s linear (rest Y 0.36 → 0.30) and lens colour slate `#556573` → `#fff0a5` over 0.06 s linear, both driven by owner activity, endpoint drive. A translucent warm-cream cone of four nested shells × 48 directions clips against shared collision proxies; it is presentation only and never supplies optical authority (requirement row and [DESIGN.md](../../../DESIGN.md) row "Light and solar"). Selection pick radius 0.9 (`parts/FlashlightPart.cs@a6c914e:L30-L30`).
- **Art:** body colour (0.96, 0.70, 0.33) `#f5b354` (cylinder r 0.3, length 1.0), cream collar `#fff8e9` (cylinder r 0.43, length 0.25), slate lens (cylinder r 0.34, length 0.03), gold button `#f7cb52` (cylinder r 0.18, length 0.14), navy base `#293954`.
- **Catalogue and inventory entry:** id `flashlight`, title "Flashlight", category Power, colour (0.96, 0.70, 0.33), description "Self-contained battery light: press its top button with a falling object or send an activation command. Its beam points out of the cream lens." Icon `ui/WorkshopIcons.cs@a6c914e:L93-L93` (current, kept). Placed (locked) in `solar_motor`, `solar_shadow`, `delayed_solar`.

## Engine capabilities

Families (map row CAT-029): AnimationEvaluation, AnimationLifecycle, ContactImpulse, ElectricalPower, EnvironmentState, FiniteLedger, GeometryQuery, OpticalTransport, RigidBodyDynamics, SignalPropagation, SlidingFriction.

**Exists now**
- Static box body/colliders: `engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L60`, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L91`.
- Impact contact trigger by approach speed: `engine/gpu/ContactTriggerDeclaration.cs@a6c914e:L7-L18` (the button region predicate is missing).
- Activation latch and input: `engine/gpu/ActivationNetwork.cs@a6c914e:L7-L17`.
- Cosmetic Activation feedback for button and lens: `engine/gpu/WorkshopCosmetic.cs@a6c914e:L7-L7`.
- Binary16 debt: at a6c914e `MetreVector`, `LinearSpeed` (the contact-trigger threshold type) and `AccelerationVector` are `Half` — `engine/gpu/PhysicsDeclarations.cs@a6c914e:L18-L21`; `RigidLocalPose` translation is a `Half` vector bounded to ±16 — `engine/gpu/PhysicsDeclarations.cs@a6c914e:L31-L38`; `WorkshopCosmeticSample.Blend` is `Half` — `engine/gpu/WorkshopCosmetic.cs@a6c914e:L10-L10`. These are remaining lanes in the [f32 migration status](../../gpu-f32-physics.md#f32-migration-status); this element's new values are declared as f32.

**Missing**
- OpticalTransport cone emission with facing, range, inverse-square game falloff and occlusion — Story 13.1; decision owner **S484** (S485 finite-colour, S486 optical-commit, S488 optical-law partial occlusion).
- Local contact-region predicate on a contact trigger (button only) — Story 13.1.
- Cone presentation (nested shells clipped against committed geometry) — Story 13.1 renderer work.
- No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`).

**Element dependencies:** CAT-001 ball (button strike; delivered), CAT-059 solar_panel (the only legacy cone receiver; Story 13.7), CAT-063 switch and CAT-022 delay (activation; delivered), CAT-066 wall (occluder; delivered), CAT-042 motor (lesson goal; Story 11.1).

## Legacy harvest

Shared optical facts. The flashlight uses the cone network, not the narrow-ray network that routes lasers (mirrors, splitters, filters and receivers ignore it in legacy):
- S6. Optical (cone) readings commit before the electrical solve that reads them. Legacy realised this with the CPU call sequence part pre-network hooks → cone light → narrow-ray optics → acoustic → electrical — `reference/cpu/MachineWorld.cs@a6c914e:L855-L861`. Carry forward the ordering rule (the requirements' snapshot-before-electrical rule, [todo-290](../requirements.md#todo-290)); do not carry forward the CPU call sequence. S486 owns the conflict with the current pipeline, which puts electrical in Phase 1 and optics in Phase 2 ([docs/gpu-f32-physics.md](../../gpu-f32-physics.md#solver-model)).
- C1. Cone emitter and receiver sample records — `engine/LightNetwork.cs@a6c914e:L8-L10`. Carry forward as declaration data.
- C2. Cone law: per receiver sample and emitter, skip if distance > range or < 1e-4, or outside the cone (dot < cosine), or back-facing; occlusion traced against shared opaque proxies excluding emitter and receiver; contribution = intensity × facing × weight ÷ max(1, d²); all readings committed together; no reflection, refraction or ambient sky power — `engine/LightNetwork.cs@a6c914e:L12-L52`. Carry forward the law; do not carry forward the CPU static solve.
- C3. The occlusion trace tests only opaque colliders (Light queries each body's opaque subset), so transparent panes and hollow bores pass cone light — `engine/physics/BodyQueryGeometry.cs@a6c914e:L74-L79`, `engine/WorldGeometry.cs@a6c914e:L173-L190`. Carry forward.

Flashlight facts:
1. Lens (0.66, 0, 0), range 8, cone cosine 0.9659258 (15°), intensity 24 — `parts/FlashlightPart.cs@a6c914e:L10-L13`. Carry forward.
2. Button travel and lens colour curves (0.06 s linear, owner active) — `parts/FlashlightPart.cs@a6c914e:L18-L23`. Carry forward.
3. Light source exists only while active; activation accepted — `parts/FlashlightPart.cs@a6c914e:L24-L27`. Carry forward.
4. Geometry and colours — `parts/FlashlightPart.cs@a6c914e:L28-L44`. Carry forward.
5. Button contact predicate (speed ≥ assistance trigger threshold; y > 0.3, |x + 0.15| < 0.45, |z| < 0.4) — `parts/FlashlightPart.cs@a6c914e:L45-L52`. Carry forward the predicate; do not carry forward the per-part contact callback.
6. Catalogue entry — `parts/catalog/flashlight.tres@a6c914e:L8-L14`; scene — `parts/scenes/flashlight.tscn@a6c914e:L1-L6`. Carry forward the entry.
7. Acceptance: a ball striking the button (torch at 0° and 37° roll) activates it, records an Activated event and turns on the light source; an underside strike or a disabled collider does not; activation uses physics poses, not rendered transforms; Reset restores the saved construction and clears events — `CuriousContraptions.tests/FlashlightContactTests.cs@a6c914e:L19-L60`. Carry forward.
8. Acceptance: cone sample rays lie exactly on the cone angle and within range for 0/90/mixed rotations; a wall 3 units ahead ends every ray at 2.9; mesh = shells × sectors × 9 vertices — `CuriousContraptions.tests/LightTests.cs@a6c914e:L76-L115`. The [requirement row](../requirements.md#current-cat-029) carries 4 shells × 48 directions. Carry forward these presentation facts. The defining LightConeVisual source is absent at baseline a6c914e; shell opacity constants are not sourced by the surviving assertions (Open question 6).
9. Acceptance: rays beside a moving occluder keep full range; moving the occluder away restores all rays — `CuriousContraptions.tests/LightTests.cs@a6c914e:L25-L42`; cone uses committed poses and colliders, not unpublished changes — `CuriousContraptions.tests/CommittedLightConeTests.cs@a6c914e:L12-L66`; cone presentation samples display-interpolated accepted poses — `CuriousContraptions.tests/DisplayClockIntegrationTests.cs@a6c914e:L53-L95`. Carry forward as presentation rules.
10. Acceptance: light requires activation, facing and range; a reversed panel or one at x = 7 (beyond range) reads 0; the torch does not latch the panel's supply — `CuriousContraptions.tests/LightTests.cs@a6c914e:L116-L166`; a panel shifted 3 in depth misses the cone — `CuriousContraptions.tests/LightTests.cs@a6c914e:L305-L320`. Carry forward.
11. Acceptance: cone light uses solved emitter and receiver poses; disabling either body's participation yields 0 — `CuriousContraptions.tests/NetworkSpatialOwnershipTests.cs@a6c914e:L135-L163`. Carry forward.
12. Acceptance: invalid cone declarations reject before Run (NaN origin, zero direction, range 0, cosine 2, intensity −1, missing/foreign/duplicate target) — `CuriousContraptions.tests/LightConeBindingTests.cs@a6c914e:L11-L64`; artwork follows committed activation across pause, hide, Reset and Save/Load (button 0.36 → 0.30) — `CuriousContraptions.tests/FlashlightPresentationTests.cs@a6c914e:L32-L78`. Carry forward behaviour; do not carry forward Godot node bindings.
13. Acceptance: placement assistance moves the torch kinematically to its target at precision 0 and leaves it static at precision 1; Reset restores — `CuriousContraptions.tests/AssistancePhysicsTests.cs@a6c914e:L17-L62`. Carry forward.
14. Levels: torch locked at (2, 3, 0) facing −X with the trigger ball at (2.15, 5, 0) above the button — `content/puzzles.json@a6c914e:L5944-L6225`; torch assistance trigger thresholds 0.2 / 0.47 / 0.8 at precision 0 / 0.45 / 1 — `content/puzzles.json@a6c914e:L5955-L5998`; `solar_shadow` and `delayed_solar` reuse it — `content/puzzles.json@a6c914e:L6226-L6574`, `content/puzzles.json@a6c914e:L6865-L7293`. Campaign source — `tools/Campaign/Program.cs@a6c914e:L225-L246`, `tools/Campaign/Program.cs@a6c914e:L266-L277`, `tools/Campaign/Program.cs@a6c914e:L415-L427`; the Campaign module places the torch at (−2, 3, 0) and the published level rotates the module 180° about Y, so `puzzles.json` has it at (2, 3, 0). Carry forward as lesson set-ups for Epic 15.

**Files harvested:** `parts/FlashlightPart.cs`, `parts/catalog/flashlight.tres`, `parts/scenes/flashlight.tscn`, `engine/LightNetwork.cs`, `engine/WorldGeometry.cs`, `engine/physics/BodyQueryGeometry.cs`, `reference/cpu/MachineWorld.cs`, `CuriousContraptions.tests/FlashlightContactTests.cs`, `CuriousContraptions.tests/FlashlightPresentationTests.cs`, `CuriousContraptions.tests/LightTests.cs`, `CuriousContraptions.tests/CommittedLightConeTests.cs`, `CuriousContraptions.tests/LightConeBindingTests.cs`, `CuriousContraptions.tests/DisplayClockIntegrationTests.cs`, `CuriousContraptions.tests/NetworkSpatialOwnershipTests.cs`, `CuriousContraptions.tests/AssistancePhysicsTests.cs`, `content/puzzles.json`, `tools/Campaign/Program.cs`.

## Acceptance outline

- **Chrome UI recipe:** in the actual Workshop place a flashlight, a ball above its button, a solar panel facing the lens and a motor; wire panel `Supply` → motor `PowerIn`; Run. Variant: wire a switch → delay → torch `ActivationIn` (as `delayed_solar`).
- **Positive:** ball strikes the button; the button depresses, the lens lights, the cone appears, the panel meter fills and the motor turns.
- **Negative / controls:** off (no strike); underside strike; rotated torch missing the panel; wall shadow (`solar_shadow`); panel beyond range 8 or outside 15°; missing panel→motor wire.
- **Boundaries:** cone edge 15°, range 8, partial occlusion reduces rather than erases.
- **Run/Reset:** Reset clears the latch, cone, button and lens exactly. **Save/Load:** round-trip.
- **Integrations:** CAT-059 solar panel; activation chains; [CAT-029](../requirements.md#current-cat-029). Suite: `tools/e2e/cat-029-036.test.ts`.

## Open questions

1. Story 13.1 says "attenuated 35° cone"; the requirement row, legacy and DESIGN.md say 15° half-angle. Unspecified — owner decision (requirement row presumed binding).
2. Intensity 24 and the max(1, d²) falloff are legacy only; the requirement row does not state them. Owner to confirm, under S488.
3. In legacy the cone reaches only solar panels; narrow-ray receivers, mirrors and gates ignore it. EL-153 requires cone sampling by a separate receiver. Must the CAT-038 receiver or mirrors respond to the flashlight? Owner S484.
4. EL-210 says "no supply means no emission", but legacy has a self-contained, unbounded battery and no supply port. Finite store or not? Unspecified — owner decision.
5. Tick order: the requirements commit optical readings before the electrical solve that reads them ([todo-290](../requirements.md#todo-290)); [docs/gpu-f32-physics.md](../../gpu-f32-physics.md#solver-model) puts electrical in Phase 1 and optics in Phase 2. Owner S486.

6. **Shell opacity.** Unspecified — owner decision: baseline caller/test facts retain the cone geometry but do not establish the deleted visual's shell-alpha values. Select opacity under DESIGN.md without presenting it as a6c914e legacy data.
