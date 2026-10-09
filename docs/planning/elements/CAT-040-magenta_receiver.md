# CAT-040 · magenta_receiver — declaration readiness spec (CAT-040-D)

## Identity

- **CAT ID / kind:** CAT-040 · `magenta_receiver` (catalogue title "Magenta receiver", category Optics).
- **Requirement anchor:** [CAT-040](../requirements.md#current-cat-040); retained behaviour [todo-301](../requirements.md#todo-301). Mode record: OpticalChannel = Magenta.
- **Mapped identities:** EL-151 Magenta selective receiver (owner S506).
- **Roadmap story:** 13.6 (Secondary Spectral Receivers — Cyan, Magenta, Yellow, White; shared with CAT-021, CAT-072, CAT-068).
- **Status:** not started.

## Declaration

**Receiver family (shared with CAT-038 broadband, CAT-056/032/012 red/green/blue and CAT-072/021/068 yellow/cyan/white).** One front-facing absorbing disc reads RGB game optical power; a channel rule decides *matched*; matched closes a contact from `PowerIn` to `Supply`. The receiver creates no electricity. Each variant is a separate catalogue entry with its own channel rule. Mixed-channel names are game-channel presence rules, not exact colourimetry. Lengths are legacy scene units; the current engine treats scene units as metres (`MetreVector`).

**Magenta variant values:** required channels Red + Blue (mask (1, 0, 1); scene `Colour = 6`). Strength = min(R, B). Matched iff R ≥ threshold **and** B ≥ threshold **and** R + B ≥ 0.9 × (R + G + B). Channel bars: two rows — **one** red bar `#de7058` and **three** blue bars `#5b9cdb`. Unfiltered amber (1, 0.78, 0.32) has 63% in R + B and is rejected; red- and blue-filtered amber combined at 90% gives (0.9, 0, 0.288) and is accepted (derived from the cited values).

- **Bodies and shapes:** one static rigid body. Opaque target plate 0.26 × 1.4 × 1.4 at the origin; opaque base 0.85 × 0.18 × 1.6 at (0, −0.88, 0).
- **Optical aperture:** port `Main`, interaction Absorb, centre (−0.18, 0, 0), normal (−1, 0, 0), finite disc radius 0.55, front face only. Beams from several emitters arriving at the disc add per channel.
- **Mass and material:** none — static body.
- **Constraints and joints:** none.
- **Typed sockets and ports:** `PowerIn` (Electrical, Input) at (0, −0.6, 0.65); `Supply` (Electrical, Output) at (0, −0.6, −0.65). Route `PowerIn → Supply` is closed while matched.
- **Sensors and activation:** the optical aperture; readings commit together before the electrical solve that reads them (S6).
- **Work and energy stores:** none.
- **Parameters:** `Threshold` — f32, game optical power, finite, range 0.05–2 inclusive (legacy validation, shared with the broadband receiver), default 0.25; applies to each required channel.
- **Cosmetic curves and UI bindings:** target disc slate `#556573` → gold `#f7cb52`, SmoothStep 0.125 s, driven by matched. Selection pick radius 1.1 (`parts/LightReceiverPart.cs@a6c914e:L49-L49`).
- **Art:** cream plate `#fff8e9`, navy base `#293954`, disc, cream ring, gold centre, two grouped bar rows (0.13 apart) centred at (−0.15, 0.6, 0), gold port studs. Two-channel red + blue beams render balloon pink `#ed6378`. See [DESIGN.md](../../../DESIGN.md#combiner-and-mixed-channel-receivers--27-september-2026).
- **Catalogue and inventory entry:** id `magenta_receiver`, title "Magenta receiver", category Optics, colour (1, 0.97, 0.91), `Parameters = {"threshold": 0.25}`, description "Each marked RGB channel must meet the threshold, with at least 90% power in the required channels. Switches a separate electrical supply." Icon `ui/WorkshopIcons.cs@a6c914e:L69-L69` (current, kept). No legacy level uses it.

## Engine capabilities

Families (map row CAT-040): AnimationEvaluation, AnimationLifecycle, ContactImpulse, ElectricalPower, EnvironmentState, FiniteLedger, GeometryQuery, OpticalTransport, RigidBodyDynamics, SignalPropagation, SlidingFriction.

**Exists now**
- Static box body/colliders: `engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L60`, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L91`.
- `PowerIn` and `Supply` sockets: `engine/gpu/WorkshopConnections.cs@a6c914e:L8-L13`.
- Cosmetic blend channel: `engine/gpu/WorkshopCosmetic.cs@a6c914e:L7-L7`.
- Binary16 debt: at a6c914e `MetreVector`, `LinearSpeed` (the contact-trigger threshold type) and `AccelerationVector` are `Half` — `engine/gpu/PhysicsDeclarations.cs@a6c914e:L18-L21`; `RigidLocalPose` translation is a `Half` vector bounded to ±16 — `engine/gpu/PhysicsDeclarations.cs@a6c914e:L31-L38`; `WorkshopCosmeticSample.Blend` is `Half` — `engine/gpu/WorkshopCosmetic.cs@a6c914e:L10-L10`. These are remaining lanes in the [f32 migration status](../../gpu-f32-physics.md#f32-migration-status); this element's new values are declared as f32.

**Missing**
- OpticalTransport absorption with multi-channel presence rule and per-port summation of several beams — Story 13.6 (single-channel receivers in 13.2). Decision owner **S484** ([decisions](../invest/decisions.md#s484)): S485 finite-colour, S486 commit boundary.
- ElectricalPower switched contact — Story 8.1; S257.
- Matched feedback for the disc — Story 13.2/13.6 (P0-022/023).
- No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`).

**Element dependencies:** CAT-036 laser ×2 (Story 13.1), CAT-055 red_filter and CAT-011 blue_filter (Story 13.4), CAT-006 beam_combiner (optional mixing; Story 13.5), CAT-031 green_filter (green-only control), CAT-005 battery (Story 8.1), an electrical load.

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
2. Strength = minimum over required channels; matched = strength ≥ threshold and required-channel power ≥ 0.9 × total — `engine/OpticalColour.cs@a6c914e:L35-L48`. Carry forward.
3. Channel bars: one row per required channel (here Red 1 bar, Blue 3 bars), rows 0.13 apart, bars 0.12 apart — `engine/OpticalColour.cs@a6c914e:L61-L76`; two-channel red + blue beam ink is magenta — `engine/OpticalColour.cs@a6c914e:L49-L60`. Carry forward explicit counts; not ordinal arithmetic.
4. Threshold parameter and validation 0.05–2 — `engine/MachineData.cs@a6c914e:L80-L80`, `parts/LightReceiverPart.cs@a6c914e:L11-L14`, `parts/LightReceiverPart.cs@a6c914e:L40-L46`. Carry forward; not the string-keyed parameter dictionary.
5. Checkpointed reading and matched state; aperture; disc animation; ports and route; reception — `parts/LightReceiverPart.cs@a6c914e:L15-L39`; geometry and bar placement — `parts/LightReceiverPart.cs@a6c914e:L47-L59`. Carry forward behaviour; not the callbacks.
6. Readings from several emitters add per port and commit once; a removed emitter zeroes only its contribution — `CuriousContraptions.tests/OpticalPortsTests.cs@a6c914e:L35-L65`; per-port summation — `engine/OpticalNetwork.cs@a6c914e:L160-L170`. Carry forward.
7. Acceptance: contact reads the checkpointed matched result — `CuriousContraptions.tests/ElectricalContactBindingTests.cs@a6c914e:L85-L111`; disc follows committed feedback through failure, pause and Reset — `CuriousContraptions.tests/ReceiverAnimationTests.cs@a6c914e:L44-L121`; failed tick restores readings — `CuriousContraptions.tests/OpticalRuntimeCheckpointTests.cs@a6c914e:L30-L93`. Carry forward (failed-tick proof at its later gate).
8. Acceptance (family, on the broadband receiver): opaque back, finite disc, occlusion, nearest-target absorption — `CuriousContraptions.tests/OpticalTests.cs@a6c914e:L96-L211`; invalid thresholds rejected — `CuriousContraptions.tests/BeamSplitterTests.cs@a6c914e:L121-L136`. Carry forward.

Magenta facts:
9. Catalogue entry — `parts/catalog/magenta_receiver.tres@a6c914e:L6-L12`; scene `Colour = 6` — `parts/scenes/magenta_receiver.tscn@a6c914e:L1-L5`. Carry forward.
10. Acceptance (Magenta): (0.3, 0, 0.3) accepted at threshold 0.25; removing either required channel rejects; either required channel at 0.249 rejects; (1, 1, 1) rejects (green contamination breaks the 90% rule) — `CuriousContraptions.tests/BeamCombinerTests.cs@a6c914e:L118-L137`. Carry forward.

**Files harvested:** `parts/LightReceiverPart.cs`, `parts/catalog/magenta_receiver.tres`, `parts/scenes/magenta_receiver.tscn`, `engine/OpticalColour.cs`, `engine/OpticalNetwork.cs`, `engine/WorldGeometry.cs`, `engine/physics/BodyQueryGeometry.cs`, `engine/MachineData.cs`, `reference/cpu/MachineWorld.cs`, `CuriousContraptions.tests/BeamCombinerTests.cs`, `CuriousContraptions.tests/OpticalPortsTests.cs`, `CuriousContraptions.tests/ElectricalContactBindingTests.cs`, `CuriousContraptions.tests/ReceiverAnimationTests.cs`, `CuriousContraptions.tests/OpticalRuntimeCheckpointTests.cs`, `CuriousContraptions.tests/OpticalTests.cs`, `CuriousContraptions.tests/BeamSplitterTests.cs`.

## Acceptance outline

- **Chrome UI recipe:** in the actual Workshop place a battery, two triggered lasers (one behind a red filter, one behind a blue filter) both aimed at the magenta receiver's disc (directly, or through a combiner), and a load; wire battery → lasers and receiver `PowerIn`, receiver `Supply` → load; Run.
- **Positive:** red and blue together at or above threshold light the disc and power the load.
- **Negative / controls:** green only; red only; blue only; unfiltered amber (green contamination); one channel below threshold; backside; blocked; missing receiver supply.
- **Boundaries:** per-channel threshold edge (0.249 versus 0.25); 90% set-purity edge.
- **Run/Reset:** Reset clears reading, contact and disc. **Save/Load:** round-trip.
- **Integrations:** filters, combiner. Binding criteria [CAT-040](../requirements.md#current-cat-040). Suite: `tools/e2e/cat-receivers-secondary.test.ts`.

## Open questions

1. The threshold range 0.05–2 is stated in the CAT-038 row only; legacy applies it to every receiver. Confirm for the magenta receiver. Owner decision.
2. Tick order: the CAT-038 row and [todo-290](../requirements.md#todo-290) require optical readings to commit before the electrical solve that reads them (todo-285 assumes the same for the laser); [docs/gpu-f32-physics.md](../../gpu-f32-physics.md#solver-model) puts the electrical solve in Phase 1 and optics in Phase 2. Which order governs? Owner S486.
3. Which electrical load proves the switched contact (legacy used powered_gate)? Owner decision.
