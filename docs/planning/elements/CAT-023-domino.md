# CAT-023 · domino — declaration readiness spec

**Current delivery (24 October 2026):** [CAT-023 delivery owner](../../../_bmad-output/planning-artifacts/epics.md#catalogue-slice-ownership). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

## 1. Identity

| Field | Value |
| --- | --- |
| CAT ID / kind | CAT-023 · `domino` |
| Requirement | [CAT-023](../requirements.md#current-cat-023) |
| Mapped identities | EL-193 Domino |
| Roadmap | Stories 5.1 (CAT-023a dynamic box), 5.2 (CAT-023b cascade and orientation sensor); offset centre of mass re-deferred as roadmap item CAT-023c |
| Status | delivered except the offset centre of mass (§5): package `elements/Domino`, a generic instance whose orientation sensor is selected by its declared threshold (Story 9A.32); browser qualification pending (post-catalogue E2E) |
| Levels | domino_effect admitted (Playable in `content/puzzles.json`); see [CAT-023-I consumers](../invest/current-consumers.md#cat-023-i) |

## 2. Declaration

| Item | Value and source |
| --- | --- |
| Body and shape | One dynamic homogeneous box, half extents (0.125, 0.55, 0.325) m (the scene-baked box 0.25 × 1.1 × 0.65 of `elements/Domino/domino.tscn`), body origin at the box centre; compiled by the shared generic compiler from the package (`engine/gpu/WorkshopPhysicsCompiler.cs`). |
| Mass and material | 0.4 kg (`RigidBodyMassProperties`); bounce 0.05; friction 0.6; bounce threshold 0.1 m/s; rolling resistance 0 (the scene body's `MaterialProperties`), all declared in `elements/Domino/DominoElementDeclaration.cs`. No authored value can carry another material: the placed tile is a zero-parameter generic instance, and a save or command record carrying a typed field or a parameter for it is refused. Inertia derived by the shared solver from extents and mass (`engine/gpu/RigidMassProperties.cs`). |
| Constraints and joints | none. |
| Sockets and ports | ActivationOut only (Activation, Output) at local (0, 0.55, 0); an activation input is rejected at the port check `engine/gpu/WorkshopConnections.cs@a6c914e:L27-L31`, `engine/gpu/WorkshopConnections.cs@a6c914e:L47-L55`. |
| Sensors and activation | Declared orientation threshold (`SignalOrientationDescriptor(ThresholdDegrees: 45)` in f32, `IOrientationSensorElement`; validated: finite, 4–179°, a pulse ActivationOut and a Dynamic body, one node-selecting capability per element). While the tile's ActivationOut is wired, the shared compiler builds its orientation sensor (`WorkshopPhysicsCompiler.OrientationSensorFor`, an f32 half-angle cosine lane since Story 9A.44) and its OrientationSource node (`WorkshopActivationCompiler.Compile`): it fires once per world when the tile has turned 45° in total from its admitted pose (|⟨q, q₀⟩| ≤ cos 22.5°), evaluated at substep endpoints, rearmed only by admission (`engine/gpu/OrientationSensorDeclaration.cs`). An unwired tile compiles neither. There is no Domino record or arm. |
| Work and energy stores | none. |
| Parameters | none: the placed tile is a generic instance of its package with `Parameters: []`, and the editor shows no panel. The package's `toppled` trigger (0.707) and `SetReset` latch mode are inert metadata; nothing reads them and the threshold is not derived from them. |
| Cosmetic curves and UI bindings | none; pips follow the committed body pose. |
| Art | `parts/DominoPart.cs@a6c914e:L10-L19`: box 0.25 × 1.1 × 0.65 in the part colour, two navy `#384757` pips r 0.045 at (0.14, ±0.25, 0); pick radius 0.6. Scene `parts/scenes/domino.tscn`. Palette Domino `#e8d4a6` `DESIGN.md@a6c914e:L174-L174`; design rule: show the committed toppling pose, the historical scripted local-Z tilt is not a physical authority `DESIGN.md@a6c914e:L275-L275`. Icon `ui/WorkshopIcons.cs@a6c914e:L53-L53`. |
| Catalogue and inventory | `parts/catalog/domino.tres@a6c914e:L6-L14`: id `domino`, title Domino, category Motion. domino_effect inventory 4 dominoes. |
| Puzzle data | `domino_effect` authored as content data in `content/puzzles.json` (locked Basketball, end tile and lamp; four-tile inventory; goal ActivatedAfter(end tile → lamp)). Its authored solution is four tiles at x = −4, −3, −2, −1 on the bench (y = 0.1) and `solution_connections` end → lamp, which solves natively at every precision (`LaunchLessonSolveTests.TheAuthoredSolutionSolvesAtEveryPrecision`). The rows were corrected in Story 9A.32 from the legacy x = −2.4…0, y = 1, which the falling Basketball never reached and whose last tile overlapped the end tile (`_bmad-output/implementation-artifacts/evidence-9a-32/candidate/domino-effect-solution/`). |

Current tests: `CuriousContraptions.tests/DominoGuardTests.cs` (9A.32 suite-first guards on the production simulation: the wired domino_effect cascade topples tile by tile by contact, fires once crossing 45° and solves once at every precision; a gap breaks the chain; an unwired end compiles no sensor or node; an upright tile never fires; a 10° tile rocks back silently and a 30° tile topples and fires; activation input refused at edit and save; highest and lowest placement; Reset rearms in one simulation; Save/Load replays tick for tick), `DeclaredOrientationTests.cs` (selection by declaration with a rotated tile and a switch control, the helper's own cosine per declared threshold, base-encoder saves refused typed), `WorkshopDominoTests.cs`, `OrientationSensorTests.cs`, `DominoEffectTests.cs`, `RigidMassPropertiesTests.cs`, `WorkbenchCapacityTests.cs`, `PalettePlacementTests.cs` (drawer placement as generic, no panel); Chrome `tools/e2e/cat-023a.test.ts`, `cat-023b.test.ts`, `cat-014.test.ts` (pending re-run in the post-catalogue E2E phase).

## 3. Engine capabilities

Families from the [element map row](../general-engine-element-map.md): ContactImpulse, EnvironmentState, FiniteLedger, GeometryQuery, RigidBodyDynamics, SignalPropagation, SlidingFriction.

| Exists now | Reference |
| --- | --- |
| Dynamic box rigid body, box–box and box–plane manifolds, resting stability | Story 5.1 |
| Orientation-threshold sensor and ActivationOut | Story 5.2 |
| Declared orientation capability (`IOrientationSensorElement`, bundle `SignalPayload.Orientation`), compiled by one shared helper | Story 9A.32; `CuriousContraptions.Abstractions.Signal/SignalContracts.cs`, `CuriousContraptions.Bundle/BundleValidator.cs` |

| Missing | Story that builds it |
| --- | --- |
| Offset centre of mass (body origin ≠ mass centre) in the shared rigid-body record | CAT-023c follow-up owned by the CAT-023 delivery contract, before its first offset-COM consumer |
| f32 lanes: the package declares the extents, mass, material and threshold in f32, and the compiled body, collider, material, half-angle cosine and inertia lanes are f32 (Story 9A.44) | Delivered by Story 9A.44 ([precision status](../../gpu-f32-physics.md#precision-status)) |

Dependencies: a striker (Bowling or Basketball) and a supporting surface.

## 4. Legacy harvest

The legacy Domino script and its tests were already deleted by Epic 5; at `a6c914e` only level data, authoring knots and the CPU tilt sensor (the legacy orientation sensor) remain.

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | Domino module: Bowling striker at (−2.4, 3, 0); fixed end domino at (0.8, 1, 0); four solution dominoes at x = −2.4, −1.6, −0.8, 0 (0.8 m pitch), y 1; goal Activated(end). | `tools/Campaign/Program.cs@a6c914e:L21-L32` | carry forward (content in `content/puzzles.json` domino_effect). |
| 2 | Authoring knots for dominoes: position step 0.20 m, rotation step 5°, window 1.2 m at precision 0 (0.6 at 0.45); trigger threshold 0.15 / 0.3075 / 0.5 at precision 0 / 0.45 / 1. | `tools/Campaign/Program.cs@a6c914e:L544-L562` | carry forward the values; do not carry forward the string `part.Kind` switch. The trigger-threshold knot has no consumer in the orientation-sensor model (see §6). |
| 3 | Composite levels use domino lanes: triple_chain (12 dominoes), double_bridge (8), and others; each lane keeps its own depth plane. | `tools/Campaign/Program.cs@a6c914e:L512-L535` | carry forward as level-data facts. |
| 4 | Legacy tilt sensor: one latched angular-deviation sensor per rigid body, bound at construction to a body key, a local direction and a threshold cosine. The direction is normalised and must be finite and non-zero; the threshold cosine must lie strictly between −1 and 1 − 1e−10. The reference direction is captured from the owned pose at installation, not from rendering. Phases are Waiting and Triggered, with the trigger time recorded. | `engine/physics/PhysicsTiltSensor.cs@a6c914e:L5-L26`; `engine/SceneTiltSensorDeclaration.cs@a6c914e:L5-L6` | carry forward the reference taken from the admitted body pose and the one-shot latch (both already in the Story 5.2 sensor); do not carry forward the f64 direction-cosine record: the current sensor is the f32 half-angle declaration `engine/gpu/OrientationSensorDeclaration.cs@a6c914e:L7-L34`. |
| 5 | A dynamic or kinematic body spinning at 8 rad/s about an axis perpendicular to the sensed direction for π/4 s (one full turn) passes the 45° threshold (cosine 0.7071) and triggers once at t = π/32 ± 1e−8 with one event; Restore then Step replays the same result, tilt states and body states; a further Step adds no second pulse. | `CuriousContraptions.tests/PhysicsTiltSensorTests.cs@a6c914e:L14-L42` | carry forward the one-shot trigger, the absence of a duplicate pulse and the exact Reset replay. |
| 6 | Two legacy behaviours in the same test: (a) the tip is found along the swept path although both step endpoints are upright ("endpoint-only sampling would miss the tip"); (b) spin about the sensed local axis (8 rad/s for π/4 s) never triggers. | `CuriousContraptions.tests/PhysicsTiltSensorTests.cs@a6c914e:L19-L33` | do not carry forward (a): the current contract samples at substep endpoints (480 Hz); do not carry forward (b) as a requirement: the current sensor measures total rotation from the admitted pose, so pure axial spin counts. Whether axial spin should fire is recorded in §6. |
| 7 | With collision participation disabled, a spinning kinematic body does not trigger; after re-enabling it triggers against the original reference, with trigger time 0.5. | `CuriousContraptions.tests/PhysicsTiltSensorTests.cs@a6c914e:L44-L57` | carry forward that the reference is never re-captured after admission; participation toggling has no current Domino consumer. |
| 8 | Installation is atomic: a duplicate body, an unknown body, a static body or a null declaration rejects the whole batch and leaves the installed sensors unchanged; reading an uninstalled sensor rejects; snapshot restore includes sensor membership. Thresholds of −1, 1, NaN and +∞ reject, as do a zero or NaN direction. | `CuriousContraptions.tests/PhysicsTiltSensorTests.cs@a6c914e:L59-L93` | carry forward the atomic rejection of invalid, duplicate and static-body sensors; the current bounds are 4°–179° with a dynamic body only `engine/gpu/OrientationSensorDeclaration.cs@a6c914e:L11-L33`. |
| 9 | When a later event exceeds the per-step event budget, the step throws and restores the earlier committed motion, sensor states, time and step index; the world returns to Idle and the next step runs. | `CuriousContraptions.tests/PhysicsTiltSensorTests.cs@a6c914e:L95-L109` | do not carry forward: an event-budget fault conflicts with the game-grade envelope (residuals clamp or continue, never fault the tick). |

Files harvested:
- `tools/Campaign/Program.cs`
- `CuriousContraptions.tests/PhysicsTiltSensorTests.cs`
- `engine/physics/PhysicsTiltSensor.cs`
- `engine/SceneTiltSensorDeclaration.cs`

## 5. Acceptance outline

Acceptance: [CAT-023](../requirements.md#current-cat-023) and its delivery notes.

- **Construction.** domino_effect: place four dominoes from inventory, wire the end domino → lamp where required, Run.
- **Positive.** Close-gap chain topples by real contact; ActivationOut emits once past 45°; Solved once.
- **Negative/control.** Three dominoes leave a gap and the chain breaks; a 10° tilt rocks back without emitting; activation input into a domino is rejected; the striker must not hit the second tile directly.
- **Run/Reset, Save/Load.** Reset restores upright poses and rearms the sensor; Save/Load restores the chain.
- **Integrations.** Contact and cargo connection audit [sequence-task-285](../requirements.md#sequence-task-285) (domino); [IX-01 contact impulse](../requirements.md#interaction-01) for the chain and [IX-07 signal propagation](../requirements.md#interaction-07) for ActivationOut; campaign first use 1–10 in the [element coverage ledger](../requirements.md#campaign-element-coverage) (physical presets row). Partners: Bowling and Basketball strikers (CAT-014, CAT-001), Signal lamp (CAT-035) through ActivationOut.
- **Remaining (unmet now).** Offset centre of mass (CAT-023c); pips following the mass-frame pose once the offset exists; composite domino levels.

## 6. Open questions

- Offset centre of mass value (vector in the tile frame): not recorded in the legacy or the requirements at `a6c914e`: unspecified — owner decision.
- Whether the authored domino trigger-threshold knots (fact 2) have any meaning under the 45° orientation sensor, or are retired: unspecified — owner decision.
- Whether a domino spun about one axis without tipping should fire the orientation sensor (the legacy tilt sensor ignored axial spin, fact 6; the current sensor counts total rotation): unspecified — owner decision. The coordinator's game-intent reading (23 Oct 2026) is "has fallen", so axial spin should not fire. Measured on the shipped engine (9A.32, `_bmad-output/implementation-artifacts/evidence-9a-32/baseline/capture/axial-spin.txt`, identical after the conversion): a tile spun about its long axis with no tilt fires once its yaw passes 45° (floating at 4 rad/s: fired at 45.8°; standing on the bench at 20 rad/s: fired at tick 5); standing at 4 rad/s, bench friction stops it at 13.5° and it stays armed. Meeting that reading therefore needs a sensor-law change (measure the tilt of the declared axis rather than the total rotation). The coordinator decided (23 Oct 2026) that the 9A.32 conversion preserves the total-rotation behaviour exactly, so this item stays open for a future decision.
