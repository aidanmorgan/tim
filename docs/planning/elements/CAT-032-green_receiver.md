# CAT-032 · green_receiver — declaration readiness spec (CAT-032-D)

## Identity

- **CAT ID / kind:** CAT-032 · `green_receiver` (catalogue title "Green receiver", category Optics).
- **Requirement anchor:** [CAT-032](../requirements.md#current-cat-032); retained behaviour [todo-300](../requirements.md#todo-300). Mode record: OpticalChannel = Green.
- **Mapped identities:** EL-147 Green selective receiver (owner S502).
- **Roadmap story:** 13.2 (Broadband & Pure Channel Optical Receivers, shared with CAT-038, CAT-056, CAT-012).
- **Status:** not started.

## Declaration

**Receiver family (shared with CAT-038 broadband, CAT-056/012 red/blue and CAT-072/021/040/068 yellow/cyan/magenta/white).** One front-facing absorbing disc reads RGB game optical power; a channel rule decides *matched*; matched closes a contact from `PowerIn` to `Supply`. The receiver creates no electricity. Each variant is a separate catalogue entry with its own channel rule. Lengths are legacy scene units; the current engine treats scene units as metres (`MetreVector`).

**Green variant values:** channel Green (mask (0, 1, 0); scene `Colour = 2`). Strength = G. Matched iff G ≥ threshold **and** G ≥ 0.9 × (R + G + B). Channel bars: one row of **two** green bars `#62aa78`. Unfiltered amber (1, 0.78, 0.32) has only 37% green and is rejected; green-filtered amber (0, 0.78, 0) is accepted (derived from the cited values).

- **Bodies and shapes:** one static rigid body. Opaque target plate 0.26 × 1.4 × 1.4 at the origin; opaque base 0.85 × 0.18 × 1.6 at (0, −0.88, 0).
- **Optical aperture:** port `Main`, interaction Absorb, centre (−0.18, 0, 0), normal (−1, 0, 0), finite disc radius 0.55, front face only. The opaque back and frame never detect.
- **Mass and material:** none — static body.
- **Constraints and joints:** none.
- **Typed sockets and ports:** `PowerIn` (Electrical, Input) at (0, −0.6, 0.65); `Supply` (Electrical, Output) at (0, −0.6, −0.65). Route `PowerIn → Supply` is closed while matched.
- **Sensors and activation:** the optical aperture is the sensor; readings from all emitters are summed and committed together before the electrical solve that reads them (S6).
- **Work and energy stores:** none.
- **Parameters:** `Threshold` — f32, game optical power, finite, range 0.05–2 inclusive (legacy validation, shared with the broadband receiver), default 0.25.
- **Cosmetic curves and UI bindings:** target disc slate `#556573` → gold `#f7cb52`, SmoothStep 0.125 s, driven by matched, endpoint drive. Channel bars identify the required channel; the disc shows current activity. Selection pick radius 1.1 (`parts/LightReceiverPart.cs@a6c914e:L49-L49`).
- **Art:** cream plate `#fff8e9`, navy base `#293954`, disc (r 0.55, thickness 0.035), cream concentric ring (r 0.33, tube 0.025), gold centre (r 0.08), two green bars (0.015 × 0.1 × 0.04, 0.12 apart) centred at (−0.15, 0.6, 0), gold port studs (r 0.075). See [DESIGN.md](../../../DESIGN.md#colour-optics-addition--27-september-2026).
- **Catalogue and inventory entry:** id `green_receiver`, title "Green receiver", category Optics, colour (1, 0.97, 0.91), `Parameters = {"threshold": 0.25}`, description "Requires at least 90% green light and sufficient channel power. Switches a separate electrical supply." Icon `ui/WorkshopIcons.cs@a6c914e:L72-L72` (two bars; current, kept). No legacy level uses it.

## Engine capabilities

Families (map row CAT-032): AnimationEvaluation, AnimationLifecycle, ContactImpulse, ElectricalPower, EnvironmentState, FiniteLedger, GeometryQuery, OpticalTransport, RigidBodyDynamics, SignalPropagation, SlidingFriction.

**Exists now**
- Static box body/colliders: `engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L60`, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L91`.
- `PowerIn` and `Supply` sockets: `engine/gpu/WorkshopConnections.cs@a6c914e:L8-L13`.
- Cosmetic blend channel: `engine/gpu/WorkshopCosmetic.cs@a6c914e:L7-L7`.
- Binary16 debt: at a6c914e `MetreVector`, `LinearSpeed` (the contact-trigger threshold type) and `AccelerationVector` are `Half` — `engine/gpu/PhysicsDeclarations.cs@a6c914e:L18-L21`; `RigidLocalPose` translation is a `Half` vector bounded to ±16 — `engine/gpu/PhysicsDeclarations.cs@a6c914e:L31-L38`; `WorkshopCosmeticSample.Blend` is `Half` — `engine/gpu/WorkshopCosmetic.cs@a6c914e:L10-L10`. These are remaining lanes in the [f32 migration status](../../gpu-f32-physics.md#f32-migration-status); this element's new values are declared as f32.

**Missing**
- OpticalTransport absorption with per-channel threshold and 90% purity — Story 13.2. Decision owner **S484** ([decisions](../invest/decisions.md#s484)): S485 finite-colour, S486 commit boundary.
- ElectricalPower switched contact — Story 8.1; S257 typed power versus signal.
- Matched feedback for the disc — Story 13.2 (P0-022/023).
- Closed `OpticalChannel` enum in the current declaration model — Story 13.4/13.2.
- No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`).

**Element dependencies:** CAT-036 laser (Story 13.1), CAT-031 green_filter (pure green source; Story 13.4), CAT-055/CAT-011 (wrong-colour controls), CAT-005 battery (Story 8.1), an electrical load (legacy: CAT-051 powered_gate, Story 8.2).

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
2. Strength = minimum over required channels (Broadband: mean); matched = strength ≥ threshold and required-channel power ≥ 0.9 × total — `engine/OpticalColour.cs@a6c914e:L35-L48`. Carry forward.
3. Channel bars: one row per required channel, bar count = channel ordinal (Green 2), rows spaced 0.13, bars 0.12 — `engine/OpticalColour.cs@a6c914e:L61-L76`. Carry forward explicit counts; not ordinal arithmetic.
4. Threshold parameter (`ReceiverParameter {Threshold}`) and validation 0.05–2, finite, channel defined — `engine/MachineData.cs@a6c914e:L80-L80`, `parts/LightReceiverPart.cs@a6c914e:L11-L14`, `parts/LightReceiverPart.cs@a6c914e:L40-L46`. Carry forward; not the string-keyed parameter dictionary.
5. Checkpointed reading and matched state; aperture; disc animation; ports and route; reception — `parts/LightReceiverPart.cs@a6c914e:L15-L39`. Carry forward behaviour; not the callbacks.
6. Geometry and bar placement — `parts/LightReceiverPart.cs@a6c914e:L47-L59`. Carry forward.
7. Acceptance: contact reads the checkpointed matched result, not presentation activity — `CuriousContraptions.tests/ElectricalContactBindingTests.cs@a6c914e:L85-L111`; disc follows committed feedback through failure, pause and Reset — `CuriousContraptions.tests/ReceiverAnimationTests.cs@a6c914e:L44-L121`; failed tick restores readings — `CuriousContraptions.tests/OpticalRuntimeCheckpointTests.cs@a6c914e:L30-L93`. Carry forward (failed-tick proof at its later gate).
8. Acceptance (family, shown on the broadband receiver): opaque back, finite disc, occlusion, nearest-target absorption — `CuriousContraptions.tests/OpticalTests.cs@a6c914e:L96-L211`; invalid thresholds 0, −1, 3, NaN, +∞ rejected — `CuriousContraptions.tests/BeamSplitterTests.cs@a6c914e:L121-L136`. Carry forward.

Green facts:
9. Catalogue entry — `parts/catalog/green_receiver.tres@a6c914e:L6-L12`; scene `Colour = 2` — `parts/scenes/green_receiver.tscn@a6c914e:L1-L5`. Carry forward.
10. Acceptance (Green): (1, 1, 1) fails purity; (0, 0.249, 0) fails threshold; (0, 0.25, 0) passes; 0.9 green with 0.051 in each other channel fails; 0.91 green with 0.045 in each other passes; a green-filtered laser lights the receiver but the load stays unpowered until the receiver's own supply is wired; wiring is refused during Run; with supply the load is powered; Reset keeps the channel and clears reading and activity — `CuriousContraptions.tests/ColourOpticsTests.cs@a6c914e:L128-L168`. Carry forward.
11. Acceptance: through a green filter (front or back, rotated) the reception equals source × (0, 1, 0) and the receiver activates without supplying electricity — `CuriousContraptions.tests/ColourOpticsTests.cs@a6c914e:L69-L103`. Carry forward.

**Files harvested:** `parts/LightReceiverPart.cs`, `parts/catalog/green_receiver.tres`, `parts/scenes/green_receiver.tscn`, `engine/OpticalColour.cs`, `engine/OpticalNetwork.cs`, `engine/WorldGeometry.cs`, `engine/physics/BodyQueryGeometry.cs`, `engine/MachineData.cs`, `reference/cpu/MachineWorld.cs`, `CuriousContraptions.tests/ColourOpticsTests.cs`, `CuriousContraptions.tests/ElectricalContactBindingTests.cs`, `CuriousContraptions.tests/ReceiverAnimationTests.cs`, `CuriousContraptions.tests/OpticalRuntimeCheckpointTests.cs`, `CuriousContraptions.tests/OpticalTests.cs`, `CuriousContraptions.tests/BeamSplitterTests.cs`, `CuriousContraptions.tests/OpticalPortsTests.cs`.

## Acceptance outline

- **Chrome UI recipe:** in the actual Workshop place a battery, a triggered laser, a green filter on its ray, the green receiver facing it and a load; wire battery → laser, battery → receiver `PowerIn`, receiver `Supply` → load; Run. Swap the filter for red and blue.
- **Positive:** green light at or above threshold with ≥ 90% purity lights the disc and powers the load.
- **Negative / controls:** red-only or blue-only light; unfiltered amber (impure); green below threshold; backside; blocked; missing receiver supply.
- **Boundaries:** threshold edge (0.249 versus 0.25); purity edge (0.9 + 2 × 0.051 rejected, 0.91 + 2 × 0.045 accepted).
- **Run/Reset:** Reset clears reading, contact and disc. **Save/Load:** threshold and wiring round-trip.
- **Integrations:** filters, combiner, mirror. Binding criteria [CAT-032](../requirements.md#current-cat-032). Suite: `tools/e2e/cat-receivers-primary.test.ts`.

## Open questions

1. The threshold range 0.05–2 is stated in the CAT-038 row only; legacy applies it to every receiver. Confirm for the green receiver. Owner decision.
2. Tick order: the CAT-038 row and [todo-290](../requirements.md#todo-290) require optical readings to commit before the electrical solve that reads them (todo-285 assumes the same for the laser); [docs/gpu-f32-physics.md](../../gpu-f32-physics.md#solver-model) puts the electrical solve in Phase 1 and optics in Phase 2. Which order governs? Owner S486.
3. Which electrical load proves the switched contact (legacy used powered_gate)? Owner decision.
