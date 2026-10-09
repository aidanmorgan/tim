# CAT-038 · light_receiver — declaration readiness spec (CAT-038-D)

## Identity

- **CAT ID / kind:** CAT-038 · `light_receiver` (catalogue title "Laser receiver", category Optics).
- **Requirement anchor:** [CAT-038](../requirements.md#current-cat-038); retained behaviour [todo-290](../requirements.md#todo-290). Mode record: OpticalChannel = Broadband.
- **Mapped identities:** EL-214 Broadband beam detector (owner S497; campaign 51–60, practice 71–80, reuse 110 and 142). Related but separate: EL-153 general-light receiver (S515) samples finite-width cone illumination; it is not this part.
- **Roadmap story:** 13.2 (Broadband & Pure Channel Optical Receivers, shared with CAT-056, CAT-032, CAT-012).
- **Status:** not started.

## Declaration

**Receiver family (shared with CAT-056/032/012 red/green/blue and CAT-072/021/040/068 yellow/cyan/magenta/white).** One front-facing absorbing disc reads RGB game optical power; a channel rule decides *matched*; matched closes a contact from `PowerIn` to `Supply`. The receiver creates no electricity. Each variant is a separate catalogue entry with its own channel rule. Lengths are legacy scene units; the current engine treats scene units as metres (`MetreVector`).

**Broadband variant values:** channel Broadband (enum ordinal 0; the scene sets no `Colour`, so the default applies). Strength = mean of R, G, B. Matched iff strength ≥ threshold; **no purity rule**. No channel bars. An amber laser (1, 0.78, 0.32) reads strength 0.7; a half-power branch 0.35 lights at the default 0.25; a quarter-power branch 0.175 does not.

- **Bodies and shapes:** one static rigid body. Opaque target plate 0.26 × 1.4 × 1.4 at the origin; opaque base 0.85 × 0.18 × 1.6 at (0, −0.88, 0).
- **Optical aperture:** port `Main`, interaction Absorb, centre (−0.18, 0, 0), normal (−1, 0, 0), finite disc radius 0.55, front face only. The opaque back and frame never detect.
- **Mass and material:** none — static body.
- **Constraints and joints:** none.
- **Typed sockets and ports:** `PowerIn` (Electrical, Input) at (0, −0.6, 0.65); `Supply` (Electrical, Output) at (0, −0.6, −0.65). Route `PowerIn → Supply` is closed while matched.
- **Sensors and activation:** the optical aperture is the sensor. Every emitter is traced, then all readings (summed per port) commit together before the electrical solve that reads them, as [todo-290](../requirements.md#todo-290) requires (S6).
- **Work and energy stores:** none.
- **Parameters:** `Threshold` — f32, game optical power, finite, range 0.05–2 inclusive, default 0.25. Out-of-range, NaN or infinite values are rejected.
- **Cosmetic curves and UI bindings:** target disc colour slate `#556573` → gold `#f7cb52`, SmoothStep over 0.125 s, driven by owner activity (matched), endpoint drive. Selection pick radius 1.1 (`parts/LightReceiverPart.cs@a6c914e:L49-L49`).
- **Art:** cream plate `#fff8e9`, navy base `#293954`, disc (r 0.55, thickness 0.035 at (−0.18, 0, 0)), cream concentric ring (r 0.33, tube 0.025 at (−0.21, 0, 0)), gold centre sphere (r 0.08 at (−0.23, 0, 0)), gold port studs (r 0.075). See [DESIGN.md](../../../DESIGN.md) row "Laser receiver".
- **Catalogue and inventory entry:** id `light_receiver`, title "Laser receiver", category Optics, colour (1, 0.97, 0.91), `Parameters = {"threshold": 0.25}`, description "The front target detects a laser beam and closes an electrical contact while lit. Connect a separate battery supply; the beam does not create electricity." Icon `ui/WorkshopIcons.cs@a6c914e:L63-L63` (current, kept). No legacy level uses it.

## Engine capabilities

Families (map row CAT-038): AnimationEvaluation, AnimationLifecycle, ContactImpulse, ElectricalPower, EnvironmentState, FiniteLedger, GeometryQuery, OpticalTransport, RigidBodyDynamics, SignalPropagation, SlidingFriction.

**Exists now**
- Static box body/colliders: `engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L60`, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L91`.
- `PowerIn` and `Supply` sockets in the Electrical domain: `engine/gpu/WorkshopConnections.cs@a6c914e:L8-L13`.
- Cosmetic blend channel (Activation feedback): `engine/gpu/WorkshopCosmetic.cs@a6c914e:L7-L7`.
- Binary16 debt: at a6c914e `MetreVector`, `LinearSpeed` (the contact-trigger threshold type) and `AccelerationVector` are `Half` — `engine/gpu/PhysicsDeclarations.cs@a6c914e:L18-L21`; `RigidLocalPose` translation is a `Half` vector bounded to ±16 — `engine/gpu/PhysicsDeclarations.cs@a6c914e:L31-L38`; `WorkshopCosmeticSample.Blend` is `Half` — `engine/gpu/WorkshopCosmetic.cs@a6c914e:L10-L10`. These are remaining lanes in the [f32 migration status](../../gpu-f32-physics.md#f32-migration-status); this element's new values are declared as f32.

**Missing**
- OpticalTransport absorption at a finite front disc, per-port summation and joint commit — Story 13.2 (emission from 13.1). Decision owner **S484** ([decisions](../invest/decisions.md#s484)): S485, S486 (commit before electrical solve).
- ElectricalPower switched contact route — Story 8.1 (battery and network graph); S257 typed power versus signal.
- Receiver feedback (matched) for the disc animation — Story 13.2 (P0-022/023).
- No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`).

**Element dependencies:** CAT-036 laser (Story 13.1), CAT-005 battery (Story 8.1), an electrical load (legacy tests used CAT-051 powered_gate, Story 8.2), CAT-008 beam_splitter (half/quarter power; Story 13.5), CAT-066 wall (delivered).

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

Receiver family facts:
1. Channel enum and masks; purity constant 0.9 — `engine/OpticalColour.cs@a6c914e:L6-L22`. Carry forward as a closed enum.
2. Strength: Broadband = (R + G + B) / 3; other channels = minimum over required channels — `engine/OpticalColour.cs@a6c914e:L35-L42`. Matched = strength ≥ threshold and (Broadband or required-channel power ≥ 0.9 × total) — `engine/OpticalColour.cs@a6c914e:L43-L48`. Carry forward.
3. Threshold parameter (closed enum `ReceiverParameter {Threshold}`) — `engine/MachineData.cs@a6c914e:L80-L80`, `parts/LightReceiverPart.cs@a6c914e:L11-L14`; validation finite 0.05–2 and channel defined — `parts/LightReceiverPart.cs@a6c914e:L40-L46`. Carry forward; do not carry forward the string-keyed parameter dictionary.
4. Received power and matched state are checkpointed; `Power` = strength — `parts/LightReceiverPart.cs@a6c914e:L15-L20`. Carry forward.
5. Aperture `Main`, Absorb, (−0.18, 0, 0), normal −X, radius 0.55 — `parts/LightReceiverPart.cs@a6c914e:L21-L21`. Carry forward.
6. Disc animation slate → gold, SmoothStep 0.125 s — `parts/LightReceiverPart.cs@a6c914e:L22-L27`. Carry forward.
7. Ports and the matched contact route `PowerIn → Supply` — `parts/LightReceiverPart.cs@a6c914e:L28-L34`. Carry forward.
8. Reception computes matched from the `Main` port reading — `parts/LightReceiverPart.cs@a6c914e:L35-L39`. Carry forward behaviour; not the callback.
9. Geometry, ring, centre and channel-bar placement at (−0.15, 0.6, 0) — `parts/LightReceiverPart.cs@a6c914e:L47-L59`. Carry forward.
10. Acceptance: the electrical contact reads the owned, checkpointed matched result (not presentation activity) and rolls back with it, for every channel — `CuriousContraptions.tests/ElectricalContactBindingTests.cs@a6c914e:L85-L111`. Carry forward.
11. Acceptance: for every channel the disc follows committed feedback (halfway at 0.0625 s), ignores unpublished state, survives a failed tick, eases back after supply loss, and Reset restores pose and colour — `CuriousContraptions.tests/ReceiverAnimationTests.cs@a6c914e:L44-L102`; shared or replaced materials reject — `CuriousContraptions.tests/ReceiverAnimationTests.cs@a6c914e:L104-L121`. Carry forward behaviour.
12. Acceptance: a failed tick restores every reading and the matched contact for every channel — `CuriousContraptions.tests/OpticalRuntimeCheckpointTests.cs@a6c914e:L30-L93`. Carry forward at its later (injected-fault) gate.

Broadband facts:
13. Catalogue entry — `parts/catalog/light_receiver.tres@a6c914e:L6-L12`; scene sets no channel (default Broadband) — `parts/scenes/light_receiver.tscn@a6c914e:L1-L4`. Carry forward.
14. Acceptance: enable × laser supply × receiver supply (8 rows); the optical snapshot commits before the contact-closing electrical solve in the same tick (the rule [todo-290](../requirements.md#todo-290) requires; S6); received power equals the beam; Reset clears — `CuriousContraptions.tests/OpticalTests.cs@a6c914e:L47-L94`. Carry forward.
15. Acceptance: reversed receiver (opaque back) and 1-unit offset (finite disc) stay dark under arbitrary transforms — `CuriousContraptions.tests/OpticalTests.cs@a6c914e:L96-L137`; wall/ball/pipe-collar occlusion — `CuriousContraptions.tests/OpticalTests.cs@a6c914e:L139-L174`; nearest receiver absorbs, no double counting, hidden ≠ disabled — `CuriousContraptions.tests/OpticalTests.cs@a6c914e:L176-L211`. Carry forward.
16. Acceptance: half-power branch lights at default threshold 0.25; quarter branch has power > 0 but stays dark — `CuriousContraptions.tests/BeamSplitterTests.cs@a6c914e:L16-L81`. Carry forward (the requirement row fixes 0.35/0.175 under f32 admission).
17. Acceptance: threshold 0, −1, 3, NaN, +∞ are rejected at placement — `CuriousContraptions.tests/BeamSplitterTests.cs@a6c914e:L121-L136`. Carry forward.
18. Acceptance: reflected optics use solved receiver pose; disabling the receiver removes the reception — `CuriousContraptions.tests/NetworkSpatialOwnershipTests.cs@a6c914e:L165-L199`. Carry forward.

**Files harvested:** `parts/LightReceiverPart.cs`, `parts/catalog/light_receiver.tres`, `parts/scenes/light_receiver.tscn`, `engine/OpticalColour.cs`, `engine/OpticalNetwork.cs`, `engine/WorldGeometry.cs`, `engine/physics/BodyQueryGeometry.cs`, `engine/MachineData.cs`, `reference/cpu/MachineWorld.cs`, `CuriousContraptions.tests/OpticalTests.cs`, `CuriousContraptions.tests/BeamSplitterTests.cs`, `CuriousContraptions.tests/ElectricalContactBindingTests.cs`, `CuriousContraptions.tests/ReceiverAnimationTests.cs`, `CuriousContraptions.tests/OpticalRuntimeCheckpointTests.cs`, `CuriousContraptions.tests/NetworkSpatialOwnershipTests.cs`, `CuriousContraptions.tests/OpticalPortsTests.cs`.

## Acceptance outline

- **Chrome UI recipe:** in the actual Workshop place a battery, a triggered laser, the receiver facing the laser and a load; wire battery → laser `PowerIn`, battery → receiver `PowerIn`, receiver `Supply` → load `PowerIn`; set the threshold through the actual parameter control; Run.
- **Positive:** the beam hits the disc; the disc eases to gold; the load is powered.
- **Negative / controls:** backside incidence; occluded beam; missing receiver supply (disc lights, load unpowered); quarter-power branch below default threshold; laser off.
- **Boundaries:** threshold 0.05 and 2; half-power 0.35 lit and quarter-power 0.175 dark at 0.25 after canonical f32 admission; all emitters traced before any reading commits.
- **Run/Reset:** Reset clears reading, contact and disc. **Save/Load:** threshold and wiring round-trip.
- **Integrations:** splitter, mirror, gates (carrier observers). Binding criteria [CAT-038](../requirements.md#current-cat-038). Suite: `tools/e2e/cat-receivers-primary.test.ts`.

## Open questions

1. Tick order: the CAT-038 row and [todo-290](../requirements.md#todo-290) require optical readings to commit before the electrical solve that reads them (todo-285 assumes the same for the laser); [docs/gpu-f32-physics.md](../../gpu-f32-physics.md#solver-model) puts the electrical solve in Phase 1 and optics in Phase 2. Which order governs? Owner S486.
2. Should the broadband receiver also respond to flashlight cones? Legacy: no (cone light reaches only solar panels); EL-153 is a separate cone receiver. Owner S484.
3. Which electrical load proves the switched contact in Story 13.2 (legacy used powered_gate)? Owner decision.
