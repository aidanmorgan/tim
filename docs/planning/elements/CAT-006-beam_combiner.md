# CAT-006 · beam_combiner — declaration readiness spec (CAT-006-D)

## Identity

- **CAT ID / kind:** CAT-006 · `beam_combiner` (catalogue title "Beam combiner", category Optics).
- **Requirement anchor:** [CAT-006](../requirements.md#current-cat-006); retained behaviour [todo-299](../requirements.md#todo-299).
- **Mapped identities:** EL-213 Optical combiner (owner S509; campaign 51–60, practice 71–80, reuse 117 and 142).
- **Roadmap story:** 13.5 (Planar Reflection Mirror, Beam Splitter & Beam Combiner, shared with CAT-041 and CAT-008).
- **Status:** not started.

## Declaration

Lengths are legacy scene units; the current engine treats scene units as metres (`MetreVector`). The outlet faces local +X.

- **Bodies and shapes:** one static rigid body. Opaque cube 1.4 × 1.4 × 1.4 at the origin; opaque base 1.8 × 0.18 × 1.8 at (0, −0.85, 0).
- **Optical apertures (three routed inputs, each radius 0.43, front face only, transmission 0.9 per channel):**

  | Port | Centre | Normal | Engraved marks |
  |---|---|---|---|
  | `First` | (−0.76, 0, 0) | (−1, 0, 0) | 1 bar |
  | `Second` | (0, 0, 0.76) | (0, 0, +1) | 2 bars |
  | `Third` | (0, 0.76, 0) | (0, +1, 0) | 3 bars |

- **Outlet:** (0.76, 0, 0), direction (+1, 0, 0). Every routed ray leaves from the outlet with power × 0.9 and remaining range reduced by the internal distance from its entry point to the outlet. The interaction count increases by one.
- **Mass and material:** none — static body.
- **Constraints and joints:** none.
- **Typed sockets and ports:** none electrical. The three optical inputs accept any RGB; the marks identify ports, not required colours.
- **Sensors and activation:** none. It reads total input power (sum over ports) and actual exiting output power, measured at the captured outlet pose.
- **Work and energy stores:** none; no stored or new light, no electricity.
- **Parameters:** none.
- **Cosmetic curves and UI bindings:** the output lens colour eases (response 12 /s) from slate `#556573` toward the beam ink of the actual output R/G/B (game optical power observations, one per channel), or back to slate when nothing exits. Selected in construction it previews the outgoing path, merging collinear contributions. Selection pick radius 1.3 (`parts/BeamCombinerPart.cs@a6c914e:L52-L52`).
- **Art:** cyan cube `#66b8c9`, navy base `#293954`, input rings cream `#fff8e9` (r 0.48, tube 0.055), slate input lenses (r 0.43, thickness 0.018), cream bar marks 0.01 × 0.1 × 0.035, output lens slate (r 0.34, thickness 0.04), gold outlet rim `#e8b764` (r 0.4, tube 0.045). See [DESIGN.md](../../../DESIGN.md#combiner-and-mixed-channel-receivers--27-september-2026).
- **Catalogue and inventory entry:** id `beam_combiner`, title "Beam combiner", category Optics, colour (0.40, 0.72, 0.79), description "Aim beams into any of the three cream-rimmed inputs. Their existing colours share the gold-rimmed output, retaining 90% power. No electricity or new light is produced." Icon `ui/WorkshopIcons.cs@a6c914e:L80-L80` (current, kept). No legacy level uses it.

## Engine capabilities

Families (map row CAT-006): AnimationEvaluation, AnimationLifecycle, ContactImpulse, EnvironmentState, FiniteLedger, GeometryQuery, OpticalAbsorption, OpticalTransport, RigidBodyDynamics, SensibleHeat, SlidingFriction.

**Exists now**
- Static box body/colliders: `engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L60`, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L91`.
- Cosmetic channel exists but only for Activation/Timer/ContactWork/Capture sources: `engine/gpu/WorkshopCosmetic.cs@a6c914e:L7-L7`.
- Binary16 debt: at a6c914e `MetreVector`, `LinearSpeed` (the contact-trigger threshold type) and `AccelerationVector` are `Half` — `engine/gpu/PhysicsDeclarations.cs@a6c914e:L18-L21`; `RigidLocalPose` translation is a `Half` vector bounded to ±16 — `engine/gpu/PhysicsDeclarations.cs@a6c914e:L31-L38`; `WorkshopCosmeticSample.Blend` is `Half` — `engine/gpu/WorkshopCosmetic.cs@a6c914e:L10-L10`. These are remaining lanes in the [f32 migration status](../../gpu-f32-physics.md#f32-migration-status); this element's new values are declared as f32.

**Missing**
- OpticalTransport routing (entry aperture → declared outlet, internal travel counted, 90% retention) — Story 13.5. Decision owner **S484** ([decisions](../invest/decisions.md#s484)): S485 finite allocation, S486 commit boundary.
- Per-channel output-power observation and spectral colour feedback — Story 13.5 (P0-022/023 shared evaluator/feedback registration).
- OpticalAbsorption/SensibleHeat for lost power — S489/S543, mode-specific.
- No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`).

**Element dependencies:** CAT-036 laser (two or three sources; Story 13.1), CAT-055/031/011 filters (to make distinct channels; Story 13.4), CAT-068 white_receiver or other receivers (Stories 13.2 and 13.6), CAT-005 battery (Story 8.1), CAT-041 mirror (loop control), CAT-066 wall (delivered).

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

Combiner facts:
1. Retention 0.9, outlet (0.76, 0, 0) facing +X — `parts/BeamCombinerPart.cs@a6c914e:L12-L14`. Carry forward.
2. Three Route apertures with centres, normals, radius 0.43 and transmission 0.9 — `parts/BeamCombinerPart.cs@a6c914e:L15-L20`. Carry forward.
3. Input power = sum over ports; output power received from accounting; active iff output non-zero (> 1e-8 squared) — `parts/BeamCombinerPart.cs@a6c914e:L21-L23`, `parts/BeamCombinerPart.cs@a6c914e:L35-L44`. Carry forward behaviour; not the callbacks.
4. Per-channel output observations (game optical power) and checkpointed state — `parts/BeamCombinerPart.cs@a6c914e:L24-L31`. Carry forward; the typed unit enum is `engine/bridge/ScalarRead.cs@a6c914e:L15-L15`.
5. Preview composition MergeCollinear; spectral output lamp (inactive slate, response 12) — `parts/BeamCombinerPart.cs@a6c914e:L33-L34`, `parts/BeamCombinerPart.cs@a6c914e:L45-L49`. Carry forward.
6. Geometry, rings, lenses, port marks and outlet art — `parts/BeamCombinerPart.cs@a6c914e:L50-L75`. Carry forward.
7. Routing law: the outgoing ray starts at the outlet, remaining = remaining − hit distance − distance(hit point, outlet), power × transmission — `engine/OpticalNetwork.cs@a6c914e:L119-L125`; output accounted at the captured outlet pose before callbacks — `engine/OpticalNetwork.cs@a6c914e:L171-L182`. A routed ray that leaves the outlet can be blocked by, or re-enter, its original emitter (S8). Carry forward.
8. Catalogue entry — `parts/catalog/beam_combiner.tres@a6c914e:L6-L11`; scene — `parts/scenes/beam_combiner.tscn@a6c914e:L1-L4`. Carry forward the entry.
9. Acceptance: the outlet indicator shows only light that actually exits: a range-2.1 source enters (input (1, 0, 0)) but the output stays 0 and the lamp dark; range 6 gives output (0.9, 0, 0) — `CuriousContraptions.tests/BeamCombinerTests.cs@a6c914e:L16-L33`. Carry forward.
10. Acceptance: three amber lasers through red/green/blue filters into the three inputs yield beam × 0.9 at a white receiver, input = beam, one merged outgoing segment; the receiver's contact stays unpowered without supply; rotation and id order do not matter; Reset clears paths and readings — `CuriousContraptions.tests/BeamCombinerTests.cs@a6c914e:L39-L90`. Carry forward.
11. Acceptance: for each input, the port id matches, output is (0.9, 0, 0), and external plus internal path totals the range 5 (±0.001); range 2.1 dies inside — `CuriousContraptions.tests/BeamCombinerTests.cs@a6c914e:L91-L117`. Carry forward.
12. Acceptance: a beam 0.6 off-axis hits the housing; incidence from the back does not route; a wall in front of the outlet still occludes downstream — `CuriousContraptions.tests/BeamCombinerTests.cs@a6c914e:L178-L199`. Carry forward.
13. Acceptance: a three-mirror loop keeps one interaction budget (17 segments) with ≥ 4 decreasing receptions — `CuriousContraptions.tests/BeamCombinerTests.cs@a6c914e:L138-L160`. Carry forward.
14. Acceptance (art): co-directed segments from the same origin sum over their overlap and the shorter ends where its range ends; crossing beams from different origins never combine; red + green renders yellow ink — `CuriousContraptions.tests/BeamCombinerTests.cs@a6c914e:L161-L177`; channel inks — `engine/OpticalColour.cs@a6c914e:L49-L60`. Carry forward.
15. Acceptance: per-channel output publishes atomically, equals input × 0.9 for every colour mask, the lamp eases by 1 − e^(−1.2) per 0.1 s, survives rollback, pause, hide, Reset and Save/Load — `CuriousContraptions.tests/OpticalObservationTests.cs@a6c914e:L66-L114`; invalid spectral bindings reject before Run — `CuriousContraptions.tests/SpectralBindingTests.cs@a6c914e:L41-L63`; readings restore on a failed tick — `CuriousContraptions.tests/OpticalRuntimeCheckpointTests.cs@a6c914e:L30-L93`. Carry forward behaviour (failed-tick proof at its later gate).
16. Acceptance: outlet accounting uses the captured outlet, not the rendered pose; disabling the combiner zeroes output — `CuriousContraptions.tests/NetworkSpatialOwnershipTests.cs@a6c914e:L486-L519`; selected preview matches the merged trace — `CuriousContraptions.tests/OpticalPreviewPresentationTests.cs@a6c914e:L45-L82`. Carry forward.

**Files harvested:** `parts/BeamCombinerPart.cs`, `parts/catalog/beam_combiner.tres`, `parts/scenes/beam_combiner.tscn`, `engine/OpticalNetwork.cs`, `engine/OpticalColour.cs`, `engine/WorldGeometry.cs`, `engine/physics/BodyQueryGeometry.cs`, `engine/bridge/ScalarRead.cs`, `reference/cpu/MachineWorld.cs`, `CuriousContraptions.tests/BeamCombinerTests.cs`, `CuriousContraptions.tests/OpticalObservationTests.cs`, `CuriousContraptions.tests/SpectralBindingTests.cs`, `CuriousContraptions.tests/OpticalRuntimeCheckpointTests.cs`, `CuriousContraptions.tests/NetworkSpatialOwnershipTests.cs`, `CuriousContraptions.tests/OpticalPreviewPresentationTests.cs`, `CuriousContraptions.tests/OpticalPortsTests.cs`.

## Acceptance outline

- **Chrome UI recipe:** in the actual Workshop place two supplied, triggered lasers aimed into the `First` and `Second` inputs (one behind a red filter, one behind a green filter) and a receiver in front of the outlet, wired to a supplied load. Run.
- **Positive:** one combined beam leaves the outlet at 90% of each channel; the outlet lens eases to the combined ink; a yellow receiver lights.
- **Negative / controls:** one input only; absent input; beam on the housing or back face; blocked input; source short of range (no outlet light); reordered sources give the same result; no energy duplication.
- **Boundaries:** internal travel counts against range; shared interaction budget.
- **Run/Reset:** Reset clears inputs, output and lamp. **Save/Load:** round-trip.
- **Integrations:** filters, mixed-channel receivers, mirrors. Binding criteria [CAT-006](../requirements.md#current-cat-006). Suite: `tools/e2e/cat-041-008-006.test.ts`.

## Open questions

1. Story 13.5 says combiners "merge orthogonal beams into one"; legacy routes each ray independently and only the renderer merges collinear segments. Must the engine represent one merged ray? Owner S485.
2. Filters (13.4) and receivers (13.2 and 13.6) arrive after Story 13.5. What does 13.5 use to show distinct channels? Owner decision.
3. EL-213 requires "declared coupling loss"; legacy fixes it at 10%. Confirm no adjustable loss. Unspecified — owner decision.
