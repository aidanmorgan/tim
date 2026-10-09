# CAT-036 · laser — declaration readiness spec (CAT-036-D)

## Identity

- **CAT ID / kind:** CAT-036 · `laser` (catalogue title "Laser emitter", category Optics).
- **Requirement anchor:** [CAT-036](../requirements.md#current-cat-036); retained behaviour [todo-285](../requirements.md#todo-285).
- **Mapped identities:** none one-to-one. EL-176/177/178 (red/green/blue laser emitters, owners S492–S494) are separate named identities with their own channels; this amber laser does not substitute for them, and they do not fold into it.
- **Roadmap story:** 13.1 (Flashlight Torch & Collimated Laser Emitters, shared with CAT-029).
- **Status:** not started.

## Declaration

Lengths are legacy scene units; the current engine treats scene units as metres (`MetreVector`). Local +X is the beam direction.

- **Bodies and shapes:** one static rigid body. Opaque box 1.25 × 0.85 × 0.80 at the origin; opaque base box 1.5 × 0.16 × 1.0 at (0, −0.55, 0). Lens point (0.72, 0, 0).
- **Mass and material:** none — static body (no dynamic mass in the legacy part; a Static declaration must carry zero mass, motion and forces — `engine/gpu/PhysicsDeclarations.cs@a6c914e:L70-L76`).
- **Constraints and joints:** none.
- **Typed sockets and ports:** `PowerIn` (Electrical, Input) at (−0.7, 0, 0); `ActivationIn` (Activation, Input) at (0, 0.52, 0). Current socket enum already names `PowerIn` and `ActivationIn` (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L13`).
- **Sensors and activation:** accepts only the Trigger activation command; it latches an *enable* flag until Reset. Any other command is rejected.
- **Work and energy stores:** none. Emission requires both the latched enable and real electrical supply on `PowerIn`; the laser stores no energy.
- **Optical emitter:** origin = lens point, direction +X, range 16, power = linear-RGB (1.00, 0.78, 0.32) game optical power ("amber").
- **Parameters:** none (catalogue has no `Parameters` entry). Requirement row: "audit configuration rather than infer none" — see Open questions.
- **Cosmetic curves and UI bindings:** lens colour follows owner activity (beam path non-empty) from slate `#556573` to `#fff0a5`, exponential follow rate 12 /s (blend 1 − e^(−1.2) after 0.1 s). Beam path drawn from committed optical state. Construction preview: a selected downstream optic shows the laser's would-be path even when unpowered, without activating anything. Selection pick radius 1 (`parts/LaserPart.cs@a6c914e:L54-L54`).
- **Art:** ochre housing `#e8b764`, navy base `#293954`, cream lens collar `#fff8e9` (cylinder r 0.38, length 0.12 at (0.66, 0, 0)), slate lens (cylinder r 0.23, length 0.035), gold port studs `#f7cb52` (sphere r 0.075) at both ports. See [DESIGN.md](../../../DESIGN.md) element table row "Laser emitter".
- **Catalogue and inventory entry:** id `laser`, title "Laser emitter", category Optics, colour (0.91, 0.72, 0.39). Description: "A trigger enables the laser until Reset. Connect electricity separately; loss of supply extinguishes the beam. Aim the narrow beam at the front of a laser receiver." Toolbox icon: `ui/WorkshopIcons.cs@a6c914e:L62-L62` (current file, kept). No level places it.

## Engine capabilities

Capability families (from [general-engine-element-map](../general-engine-element-map.md) row CAT-036 and its binding shard): AnimationEvaluation, AnimationLifecycle, ContactImpulse, ElectricalPower, EnvironmentState, FiniteLedger, GeometryQuery, OpticalTransport, RigidBodyDynamics, SignalPropagation, SlidingFriction.

**Exists now**
- Static box body and collider: `engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L60`, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L91`.
- Activation input, latch and edges: `engine/gpu/ActivationNetwork.cs@a6c914e:L7-L17`.
- Typed port enums (Activation and Electrical domains): `engine/gpu/WorkshopConnections.cs@a6c914e:L8-L13`.
- Cosmetic feedback channel (Activation source only): `engine/gpu/WorkshopCosmetic.cs@a6c914e:L7-L7`.
- Binary16 debt: at a6c914e `MetreVector`, `LinearSpeed` (the contact-trigger threshold type) and `AccelerationVector` are `Half` — `engine/gpu/PhysicsDeclarations.cs@a6c914e:L18-L21`; `RigidLocalPose` translation is a `Half` vector bounded to ±16 — `engine/gpu/PhysicsDeclarations.cs@a6c914e:L31-L38`; `WorkshopCosmeticSample.Blend` is `Half` — `engine/gpu/WorkshopCosmetic.cs@a6c914e:L10-L10`. These are remaining lanes in the [f32 migration status](../../gpu-f32-physics.md#f32-migration-status); this element's new values are declared as f32.

**Missing**
- OpticalTransport (narrow-ray emission, nearest-hit occlusion, range budget, committed beam path) — built by Story 13.1. Decision owner **S484** ([decisions](../invest/decisions.md#s484)): S485 finite-colour allocation, S486 optical-commit snapshot boundary, S488 interval transport law.
- ElectricalPower supply on `PowerIn` — Story 8.1 (CAT-005 battery and network graph); S257 typed power versus signal.
- Optical-power feedback for the lens follow and beam renderer — built with Story 13.1 (P0-022/023 shared evaluator/feedback registration).
- No `WorkshopPartKind` member yet (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`).

**Element dependencies:** CAT-005 battery (supply), CAT-063 switch or CAT-022 delay (trigger; delivered), CAT-038 light_receiver (observable target; Story 13.2), CAT-066 wall (opaque occluder; delivered).

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

Laser facts:
1. Range 16, lens (0.72, 0, 0), power (1, 0.78, 0.32) — `parts/LaserPart.cs@a6c914e:L12-L14`. Carry forward.
2. Emission only when enabled **and** `PowerIn` is powered; construction preview source exists regardless of power — `parts/LaserPart.cs@a6c914e:L36-L38`. Carry forward.
3. Only `Trigger` is accepted; it sets the enable latch (deferred) — `parts/LaserPart.cs@a6c914e:L39-L44`. Carry forward.
4. Ports `PowerIn` (−0.7, 0, 0) and `ActivationIn` (0, 0.52, 0) — `parts/LaserPart.cs@a6c914e:L31-L35`. Carry forward.
5. The beam path is owned as an immutable copy; `Active` = path non-empty — `parts/LaserPart.cs@a6c914e:L45-L51`. Carry forward behaviour; do not carry forward the per-part callback.
6. Enable and path are checkpointed and restored on a failed tick — `parts/LaserPart.cs@a6c914e:L17-L25`; `CuriousContraptions.tests/OpticalControlCheckpointTests.cs@a6c914e:L86-L127`. Carry forward as a later-gate (injected-fault) acceptance fact.
7. Geometry, colours and lens follow rate 12 — `parts/LaserPart.cs@a6c914e:L26-L29`, `parts/LaserPart.cs@a6c914e:L52-L62`. Carry forward.
8. Catalogue entry — `parts/catalog/laser.tres@a6c914e:L6-L11`; scene binds only the script — `parts/scenes/laser.tscn@a6c914e:L1-L4`. Carry forward the entry; not the Godot scene.
9. Acceptance: for all 8 combinations of enable × laser supply × receiver supply, the receiver lights iff enable ∧ laser supply, and the load is powered iff all three; received power equals (1, 0.78, 0.32); a laser 6 apart from a receiver draws a 5.09–5.11 path; supply loss leaves the receiver lit for the current snapshot and clears it at the next; Reset clears enable, path and reading — `CuriousContraptions.tests/OpticalTests.cs@a6c914e:L47-L94`. Carry forward.
10. Acceptance: the aperture and beam follow full 3D transforms (0/90/mixed Euler); a reversed receiver (opaque back) and one offset 1 unit off-axis (finite disc) stay dark; presentation-only rotation cannot change the captured aperture — `CuriousContraptions.tests/OpticalTests.cs@a6c914e:L96-L137`. Carry forward.
11. Acceptance: a wall, a ball, or a pipe collar on the ray stops the beam (path < 5); moving it away restores; presentation moves of the blocker do not count — `CuriousContraptions.tests/OpticalTests.cs@a6c914e:L139-L174`. Carry forward.
12. Acceptance: nearest absorbing target wins (no double counting); hidden is not disabled; disabling the near receiver's participation lights the far one; with nothing hit the path length equals range 16 (±1e-5) — `CuriousContraptions.tests/OpticalTests.cs@a6c914e:L176-L211`. Carry forward.
13. Acceptance: the lens shows only committed powered emission for supplied × triggered; it eases back to slate after supply loss; Reset and Save/Load restore the exact construction; invalid follow declarations reject before Run — `CuriousContraptions.tests/LaserAnimationTests.cs@a6c914e:L33-L111`. Carry forward behaviour; do not carry forward Godot material bindings.
14. Lasers serve as fixtures for mirrors, splitters, combiners, filters, receivers and gates (see those specs), and solved emitter pose is used, not presentation — `CuriousContraptions.tests/NetworkSpatialOwnershipTests.cs@a6c914e:L165-L199`. Carry forward.

**Files harvested:** `parts/LaserPart.cs`, `parts/catalog/laser.tres`, `parts/scenes/laser.tscn`, `engine/OpticalNetwork.cs`, `engine/WorldGeometry.cs`, `engine/physics/BodyQueryGeometry.cs`, `reference/cpu/MachineWorld.cs`, `CuriousContraptions.tests/OpticalTests.cs`, `CuriousContraptions.tests/OpticalControlCheckpointTests.cs`, `CuriousContraptions.tests/LaserAnimationTests.cs`, `CuriousContraptions.tests/OpticalPortsTests.cs`, `CuriousContraptions.tests/NetworkSpatialOwnershipTests.cs`.

## Acceptance outline

- **Chrome UI recipe:** from the actual Workshop drawer place a battery, a switch with a falling ball, a laser and a broadband receiver 6 units along the laser's +X; wire battery `Supply` → laser `PowerIn` and switch `ActivationOut` → laser `ActivationIn` with the connection UI; rotate with the existing gizmo. Run.
- **Positive:** the ball triggers the switch; the beam appears and reaches the receiver; lens eases to `#fff0a5`.
- **Negative / controls:** no trigger; no supply; supply removed mid-run (beam clears at the next optical snapshot, enable survives, restored supply relights without retrigger); opaque wall on the ray; rotated laser missing the target; range beyond 16.
- **Boundaries:** range 16 exactly; finite receiver disc; per-path interaction and per-emitter segment caps (S2).
- **Run/Reset:** Reset clears enable, path and lens colour exactly. **Save/Load:** placement and wiring round-trip.
- **Integrations:** CAT-041/008/006/011/031/055 routing, receivers, gates, CAT-007 beam shutter.
- Binding criteria: [CAT-036](../requirements.md#current-cat-036). Suite named by Story 13.1: `tools/e2e/cat-029-036.test.ts`.

## Open questions

1. Story 13.1 text says "infinite collimated beam"; the requirement row and legacy say range 16. Unspecified — owner decision (requirement row presumed binding).
2. Configuration audit: no laser parameters exist in legacy (no adjustable colour, range or power). Confirm none are required.
3. Tick order: [todo-285](../requirements.md#todo-285) assumes, and the CAT-038 row and [todo-290](../requirements.md#todo-290) require, that optical readings commit before the electrical solve that reads them; [docs/gpu-f32-physics.md](../../gpu-f32-physics.md#solver-model) puts the electrical solve in Phase 1 and optics in Phase 2. Which order governs, and which snapshot sees supply loss? Owner S486.
4. Story 13.4 refers to a "white light beam"; no white emitter exists. The amber laser carries all three channels. Confirm amber is the filter fixture source.
