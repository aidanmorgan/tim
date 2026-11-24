# CAT-023 · domino — declaration readiness spec

**Status (22 November 2026).** Delivered except the offset centre of mass: package `elements/Domino`, id `domino` (Stories 5.1 and 5.2; generic declared instance by Story 9A.32; pip colliders by `engine-multi-collider-dynamic`, published `28a2d302`). The offset is owned by the existing story CAT-023c; its value (0, −0.05, 0) m is approved (owner decision 8, 22 November 2026). Every residual slice, CAT-023c included, is blocked on the engine programme (owner decision 3a, 22 November 2026); the part's own engine gap is `ENGINE-ISLANDS-SLEEP` (resting tiles are still stepped). Sections 2 and 3 are read from the package and its tests in this checkout (`path:line`; tests are in `CuriousContraptions.tests/`). Browser qualification is pending until the post-catalogue E2E phase. Citations of the form `path@a6c914e` in section 4 are the legacy harvest, not current instructions.

## 1. Identity

| Field | Value |
| --- | --- |
| CAT ID / kind | CAT-023 · `domino` |
| Requirement | [CAT-023](../requirements.md#current-cat-023) |
| Mapped identities | EL-193 Domino ([EL-193 spec](EL-193-domino.md)): a refinement whose only residual is this part's own, the offset centre of mass; it is closed by rows inside CAT-023c |
| Disposition | Delivered, with one requirement criterion owed: "with offset centre of mass", story CAT-023c (`cat-023c-offset-centre-of-mass`, [epics.md, "CAT-023c"](../../../_bmad-output/planning-artifacts/epics.md#cat-023c-domino-offset-centre-of-mass-and-presentation-origin)) |
| Levels | `domino_effect` (Playable); composite domino lanes; see [CAT-023-I consumers](../invest/current-consumers.md#cat-023-i) |

## 2. Declaration

Read from `elements/Domino/DominoElementDeclaration.cs` (cited as `:line`) and `elements/Domino/domino.tscn` (cited as `tscn:line`).

**Interfaces** (`:9`): `IMechanicalElement`, `ISignalElement`, `ISceneGeometryElement`. Domains Mechanical and Signal (`:41`). Kinematic mode Dynamic (`:43`); footprint one body, one moving, no constraints (`:25`).

**Catalogue** (`:14-23`): id `domino`, category Motion, palette order 9, colour (0.91, 0.83, 0.65), display name "Domino".

**Body, colliders, mass and material.** One dynamic body with its origin at the box centre.

| Collider | Shape | Dimensions (m) | Centre (m) | Source |
| --- | --- | --- | --- | --- |
| `col_0_0` | Box | 0.25 × 1.1 × 0.65 | (0, 0, 0) | `tscn:19-20`, `:40-44` |
| `col_visual_pip1` | Sphere | radius 0.045 | (0.14, −0.25, 0) | `tscn:14-17`, `:46-49` |
| `col_visual_pip2` | Sphere | radius 0.045 | (0.14, 0.25, 0) | `tscn:14-17`, `:51-54` |

Mass frame (`:47-53`): 0.4 kg; centre of mass (0, 0, 0); principal moments of the homogeneous box, (0.0544, 0.0162, 0.0424) kg·m² about x, y, z; identity principal frame; no linear drag; gravity (0, −9.81, 0) m/s². Colliders are shape only; they add no mass. Material (`:36`): restitution 0.05, friction 0.6, rolling resistance 0, bounce threshold 0.1 m/s.

**Planned change (CAT-023c).** The centre of mass becomes (0, −0.05, 0) m in the tile frame, a fixed declared value with no parameter, as `EL-193-domino.md` has it. The moments keep the homogeneous values (the parallel-axis difference is 0.001 kg·m², under 2% of the tipping-axis moment). The body origin, the colliders and the art do not move. The offset raises the static tipping angle about a bottom long edge from 12.8° to 14.0°; the delivered 10° rock-back and 30° topple are both outside that band.

**Parameters:** none (`:23`). The placed tile is a generic instance with no editor panel; a save carrying a typed field or a parameter for it is refused (`WorkshopDominoTests.SaveRoundTripsTheGenericDominoAndRefusesAnyTypedFieldOrParameter`).

**Port** (`:29`).

| `PortId` | Role | Domain | Direction | Signal kind | Position (m) |
| --- | --- | --- | --- | --- | --- |
| `ActivationOut` | ActivationOut | Signal | Output | Pulse | (0, 0.55, 0), facing +Y |

**Signal node** (`:59`, `:61-64`): one `SignalOrientationDescriptor`, id `signal`, on body 0, threshold 45°; its output terminal is bound to `ActivationOut`. No signal edge. **Event** (`:39`): `TriggerActivated` on node `signal`.

**Stores, supply and electrical nodes:** none.

**Presentation** (`:11-12`): pick radius 0.6 m; no binding of any kind. The art follows the body pose.

## 3. Engine capabilities

Capability probe, 22 November 2026. Counts: 7 present, 1 present but untested, 2 narrower, 0 missing.

| Capability | Status | Symbol | Test or owner |
| --- | --- | --- | --- |
| Dynamic box with a declared mass frame; a chain that topples by real contact | present | `MassFrameDeclaration` (`CuriousContraptions.Abstractions/MassFrameValues.cs:38`) | `WorkshopDominoTests.DominoCompilesAsOneDynamicBoxWithDeclaredMassMaterialAndGravity`, `.CompiledInertiaIsTheHomogeneousBoxTensorAboutTheBoxCentre`; `DominoGuardTests.TheWiredLessonCascadeTopplesByContactFiresOnceAtFortyFiveDegreesAndSolvesOnce`, `.AGapBreaksTheChainSoTheEndStandsAndTheLampStaysDark` |
| Declared orientation threshold on a dynamic body, firing a pulse output once | present | `SignalOrientationDescriptor` (`CuriousContraptions.Abstractions/SignalContractValues.cs:56`), `OrientationThreshold` (4° to 179°, `CuriousContraptions.Abstractions/OrientationSensorDeclarationValues.cs:9-21`), compiled at `engine/gpu/WorkshopPhysicsCompiler.cs:369-378`, evaluated at `CuriousContraptions.Simulation/WorkshopPhysicsEngine.cs:1345-1362` | `DominoGuardTests.ATileTenDegreesOffUprightRocksBackSilentlyAndThirtyDegreesTopplesAndFires`, `.AnUprightWiredTileStandsAndNeverFires`, `.AnUnwiredEndObservesItsFallButTheLampStaysDark`; `DeclaredOrientationTests.TheCompilerUsesEachDeclaredOrientationThreshold`, `.EveryDeclaredTileCompilesItsOwnSensorIncludingAnUnwiredTile` |
| A connection into a part with no input port is refused; placement bounds | present | port typing in `engine/gpu/WorkshopConnections.cs` | `DominoGuardTests.AnActivationInputIntoADominoIsRefusedAtTheEditAndSaveBoundaries`, `.TheHighestAndLowestPlacementsAreAdmittedAndOneBeyondIsRefused` |
| Reset, save and load | present | generic instance and sensor rearm on admission | `DominoGuardTests.ResetRestoresUprightTilesAndRearmsTheSensorSoTheSecondRunReplaysTheFirst`, `.SaveAndLoadPreserveTheWiredCascade` |
| Several colliders on one dynamic body (the pips) | present | scene colliders `col_visual_pip1`, `col_visual_pip2` | `VisibleSolidColliderTests.EverySolidArtPartHasItsColliderAndNoColliderReachesBeyondTheArt` (row `domino`, `VisibleSolidColliderTests.cs:115`) |
| `domino_effect` solved, unsolved without its cause | present | lesson content in `content/puzzles.json` | `LaunchLessonSolveTests.AuthoredLessonSolvesAndOneMissingCausePreventsTheGoal` (the authored precision, at 60, 120 and 240 Hz); the cascade test above runs precision 0, 0.45 and 1 |
| A centre of mass off the body origin, in free flight | present | `MassFrameDeclaration.CentreOfMass`, read by the solver at `WorkshopPhysicsEngine.cs:457` | `WorkshopPhysicsLawTests.ABodyWhoseCentreOfMassSitsOffItsOriginSpinsAboutThatCentreInFreeFlight` |
| A centre of mass off the body origin, in resting and tipping contact | present, untested | the same symbols; no delivered package declares a non-zero centre | CAT-023c |
| Motion and presentation samples of an offset body | narrower | the worker writes the body origin into the lanes the host reads as the centre of mass; they agree only while the offset is zero (`_bmad-output/implementation-artifacts/deferred-work.md:107`) | CAT-023c |
| "Has fallen" sensing | narrower | the sensor measures total rotation from the admitted pose, so a tile spun 45° about its long axis without tipping fires | accepted (section 6) |

### Engine conformance

**Compiled counts.** Bodies 1 (dynamic). Colliders 3: 1 box, 2 spheres. Joints 0. Drives 0. Sensors 1 (orientation). Domain nodes 1 (signal node `signal`). Stores 0. Presentation bindings 0. Events 1 kind. Every tile compiles its own sensor row, wired or not. All counts are fixed at compile time.

**Art and layers.**

| Art node | Mesh (m) | Classification | Moved by |
| --- | --- | --- | --- |
| `Visual/Visual_Body` | box 0.25 × 1.1 × 0.65, cream (`tscn:10-12`, `:26-27`) | solid, collider `col_0_0` | body pose |
| `Visual/Visual_Pip1` | sphere r 0.045 at (0.14, −0.25, 0), navy (`tscn:14-17`, `:29-31`) | solid, collider `col_visual_pip1` | body pose |
| `Visual/Visual_Pip2` | sphere r 0.045 at (0.14, 0.25, 0), navy (`tscn:33-35`) | solid, collider `col_visual_pip2` | body pose |

The table agrees with `VisibleSolidColliderTests.cs:115`. No open visible-solid finding.

**Generic laws.**

| Behaviour | Law | Declared values |
| --- | --- | --- |
| A tile stands, rocks back from 10° and topples from 30° | Rigid-body dynamics under gravity with contact and Coulomb friction on its support | 0.4 kg, the box moments, friction 0.6, restitution 0.05, bounce threshold 0.1 m/s |
| A chain propagates, and a gap stops it | Contact impulses between tiles; no neighbour scan and no prescribed flip | the same body data; the spacing is level content |
| The output fires once when the tile has turned 45° from its placed pose | Orientation sensor law: \|⟨q, q₀⟩\| ≤ cos(θ/2), sampled at substep endpoints, sticky, rearmed only by admission; the node emits one pulse | threshold 45°; pulse `ActivationOut` |
| A wire into the tile is refused | Port typing: the part declares no input port | the port list |

**Why three colliders.** Visible = solid: each pip stands 6 cm proud of the +X face, so each has a sphere collider (owner's collider-fidelity decision, 25 October 2026). That is one more collider than the nearest delivered moving part, the Basketball (sphere and seam torus), and a twelve-tile lane carries 36 colliders. The alternative, pips printed flush, would save two per tile; it is not proposed, because the delivered pips are tested and no workload measurement asks for it.

**Performance fitness.** One dynamic body per tile. Island sleeping is not implemented; a resting body is still stepped. Owned by `ENGINE-ISLANDS-SLEEP`. The orientation sensor is one row per tile and costs one quaternion dot product per substep while armed, nothing after it fires; it is not a volume and scans nothing. Tunnelling is excluded by the engine's general means, 480 Hz substeps with speculative contact margins (`docs/gpu-f32-physics.md:375`); the tile is 25 cm thick and the pips are 9 cm across. Nothing here is proportional to the whole scene.

**No engine code names the package.** The audit's engine search found no id literal, file or branch for `domino`. Residue: one comment gives the Domino's 45° as an example (`CuriousContraptions.Abstractions/OrientationSensorDeclarationValues.cs:11`); no finding.

## 4. Legacy harvest

The legacy Domino script and its tests were already deleted by Epic 5; at `a6c914e` only level data, authoring knots and the CPU tilt sensor (the legacy orientation sensor) remain.

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | Domino module: Bowling striker at (−2.4, 3, 0); fixed end domino at (0.8, 1, 0); four solution dominoes at x = −2.4, −1.6, −0.8, 0 (0.8 m pitch), y 1; goal Activated(end). | `tools/Campaign/Program.cs@a6c914e:L21-L32` | carry forward the lesson; do not carry forward its poses or striker. Story 9A.32 re-authored `domino_effect` in `content/puzzles.json`: a locked Basketball, four tiles at x = −4, −3, −2, −1 on the bench and the end tile wired to the lamp, because the legacy tiles lay 2 m from the falling ball and the last overlapped the end tile (`CuriousContraptions.tests/LaunchLessonSolveTests.cs:84-86`). |
| 2 | Authoring knots for dominoes: position step 0.20 m, rotation step 5°, window 1.2 m at precision 0 (0.6 at 0.45); trigger threshold 0.15 / 0.3075 / 0.5 at precision 0 / 0.45 / 1. | `tools/Campaign/Program.cs@a6c914e:L544-L562` | carry forward the position and rotation values; do not carry forward the string `part.Kind` switch. The trigger-threshold knot is retired: it has no consumer in the orientation-sensor model (§6, D15). |
| 3 | Composite levels use domino lanes: triple_chain (12 dominoes), double_bridge (8), and others; each lane keeps its own depth plane. | `tools/Campaign/Program.cs@a6c914e:L512-L535` | carry forward as level-data facts. |
| 4 | Legacy tilt sensor: one latched angular-deviation sensor per rigid body, bound at construction to a body key, a local direction and a threshold cosine. The direction is normalised and must be finite and non-zero; the threshold cosine must lie strictly between −1 and 1 − 1e−10. The reference direction is captured from the owned pose at installation, not from rendering. Phases are Waiting and Triggered, with the trigger time recorded. | `engine/physics/PhysicsTiltSensor.cs@a6c914e:L5-L26`; `engine/SceneTiltSensorDeclaration.cs@a6c914e:L5-L6` | carry forward the reference taken from the admitted body pose and the one-shot latch (both already in the Story 5.2 sensor); do not carry forward the f64 direction-cosine record: the current sensor is the f32 half-angle declaration `engine/gpu/OrientationSensorDeclaration.cs@a6c914e:L7-L34`. |
| 5 | A dynamic or kinematic body spinning at 8 rad/s about an axis perpendicular to the sensed direction for π/4 s (one full turn) passes the 45° threshold (cosine 0.7071) and triggers once at t = π/32 ± 1e−8 with one event; Restore then Step replays the same result, tilt states and body states; a further Step adds no second pulse. | `CuriousContraptions.tests/PhysicsTiltSensorTests.cs@a6c914e:L14-L42` | carry forward the one-shot trigger, the absence of a duplicate pulse and the exact Reset replay. |
| 6 | Two legacy behaviours in the same test: (a) the tip is found along the swept path although both step endpoints are upright ("endpoint-only sampling would miss the tip"); (b) spin about the sensed local axis (8 rad/s for π/4 s) never triggers. | `CuriousContraptions.tests/PhysicsTiltSensorTests.cs@a6c914e:L19-L33` | do not carry forward (a): the current contract samples at substep endpoints (480 Hz); do not carry forward (b): the current sensor measures total rotation from the admitted pose, so pure axial spin counts, and §6 keeps that law. |
| 7 | With collision participation disabled, a spinning kinematic body does not trigger; after re-enabling it triggers against the original reference, with trigger time 0.5. | `CuriousContraptions.tests/PhysicsTiltSensorTests.cs@a6c914e:L44-L57` | carry forward that the reference is never re-captured after admission; participation toggling has no current Domino consumer. |
| 8 | Installation is atomic: a duplicate body, an unknown body, a static body or a null declaration rejects the whole batch and leaves the installed sensors unchanged; reading an uninstalled sensor rejects; snapshot restore includes sensor membership. Thresholds of −1, 1, NaN and +∞ reject, as do a zero or NaN direction. | `CuriousContraptions.tests/PhysicsTiltSensorTests.cs@a6c914e:L59-L93` | carry forward the atomic rejection of invalid, duplicate and static-body sensors; the current bounds are 4°–179° with a dynamic body only `engine/gpu/OrientationSensorDeclaration.cs@a6c914e:L11-L33`. |
| 9 | When a later event exceeds the per-step event budget, the step throws and restores the earlier committed motion, sensor states, time and step index; the world returns to Idle and the next step runs. | `CuriousContraptions.tests/PhysicsTiltSensorTests.cs@a6c914e:L95-L109` | do not carry forward: an event-budget fault conflicts with the game-grade envelope (residuals clamp or continue, never fault the tick). |

Files harvested:
- `tools/Campaign/Program.cs`
- `CuriousContraptions.tests/PhysicsTiltSensorTests.cs`
- `engine/physics/PhysicsTiltSensor.cs`
- `engine/SceneTiltSensorDeclaration.cs`

## 5. Acceptance outline

Acceptance: [CAT-023](../requirements.md#current-cat-023) and its delivery notes. Every row runs the production simulation; none depends on a domino-specific code path.

- **Construction.** `domino_effect`: place four dominoes from inventory, wire the end domino to the lamp, Run. When the level is re-authored (completion-ledger OS-06; owner decisions 24f and 24o(3); coordinator decision 41) the lamp and its wire go and the goal stays Captured: the last domino knocks a ball into a Receiving basket.
- **Positive.** A close-gap chain topples by real contact, each tile starting to turn only after the one before it; `ActivationOut` emits once past 45°; Solved once.
- **Negative/control.** A gap breaks the chain; a 10° tilt rocks back without emitting; an upright tile never fires; an unwired end falls but lights nothing; a wire into a domino is refused; the striker does not reach the second tile first (the cascade test asserts the order of first motion).
- **Run/Reset, Save/Load.** Reset restores upright poses and rearms the sensor; save and load restore the chain.
- **Integrations.** Contact and cargo connection audit [sequence-task-285](../requirements.md#sequence-task-285); [IX-01 contact impulse](../requirements.md#interaction-01) for the chain and [IX-07 signal propagation](../requirements.md#interaction-07) for `ActivationOut`; campaign first use 1–10 in the [element coverage ledger](../requirements.md#campaign-element-coverage). Partners: Basketball and Bowling ball strikers, Signal lamp through `ActivationOut`.
- **Owed, in CAT-023c.** The rows are in `EL-193-domino.md` section 5 ("Planned in CAT-023c"): an offset tile tips at the angle its centre of mass gives; the delivered cascade, gap, upright, 10° and 30° cases keep their outcomes with the offset; the drawn tile stays on its collider while it falls (`DominoGuardTests.TheDrawnTileStaysOnItsColliderWhileItFalls`, within 0.005 m, where a double or missing subtraction of the offset shows as 0.05 m); a non-finite centre is refused with a typed reason. Composite domino levels belong to the campaign slices.

## 6. Open questions

Resolved:

- **Trigger-threshold knots (fact 2): retired.** D15: the requirement row governs, and it defines the output as 45° from the start orientation, which the declared sensor is.
- **Axial spin: the delivered total-rotation law is kept** (standing delegation; the audit judged it sound). A standing tile spun at 4 rad/s is stopped by bench friction at 13.5° and no lesson spins a tile, so no play route reaches a false fire. If a level later does, that is a gameplay defect and the sensor law changes in an engine slice (tilt of a declared axis) for every orientation sensor at once.
- **Offset centre of mass: owned by story CAT-023c** (coordinator decision, 22 November 2026, audit H3; reason: the requirement criterion stays, and `EL-193-domino.md` already builds this value under D1). Value (0, −0.05, 0) m in the tile frame, approved by owner decision 8 (22 November 2026); homogeneous moments kept. CAT-023c is engine tier, because it touches the mass-property, compiler, ABI and motion-read callers.

Needs the owner: nothing.

## 7. Integration

- **Collaborators.** Basketball and Bowling ball (strikers), Signal lamp (`ActivationIn`), Delay box, Ramp, Wall, the bench. All delivered. The tile's output is a Signal pulse and matches the lamp's input domain.
- **Campaign.** `domino_effect` (Playable; plan level 4, re-authored without the lamp and with a Captured goal, [campaign coverage](readiness/campaign-coverage.md#chapter-1-re-authoring-and-relocated-authored-levels-owner-decisions-24f-and-24e)) in `content/puzzles.json`; first use 1–10 in the [element coverage ledger](../requirements.md#campaign-element-coverage).
- **Coverage binding.** Catalogue key `domino` in `docs/coverage/catalogue-elements.json`. A level that offers it from the player's inventory needs no lesson-fixture story; a level that locks it as a fixture adds a `FIX-` row to the catalogue fixture table ([current consumers](../invest/current-consumers.md), read by `tools/Coverage/CurrentCatalogue.cs:100`), and needs [S8](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package) only for per-parameter or connection locks.

## 8. Delivery

- **Disposition.** Delivered: package `domino`. Records under `_bmad-output/implementation-artifacts/`: `spec-5-1-dynamic-box-rigid-body-upright-stability.md`, `spec-5-2-domino-cascade-orientation-threshold-sensor.md`, `review-9a-32-domino.md`; pip colliders published at `28a2d302`.
- **Residual.** Blocked on the engine programme (owner decision 3a, 22 November 2026), which precedes every further element: CAT-023c (engine tier, six review layers), with the value approved by owner decision 8. It re-authors `WorkshopDominoTests.cs:46` (`Assert.Equal(default, properties.LocalCentreOfMass)`) and re-runs `DominoGuardTests`; it edits `elements/Domino/DominoElementDeclaration.cs:49` and the generic motion-read callers, with no Domino branch in the solver. EL-193 is closed by rows inside that slice. Chrome proof is queued for the post-catalogue phase.
- **Disqualifiers.** A neighbour scan or prescribed flip; a change of the 45° threshold without re-running `domino_effect` at every precision; `domino_effect` no longer solving with the offset (then the offset is reduced, never the stall guard stretched).
