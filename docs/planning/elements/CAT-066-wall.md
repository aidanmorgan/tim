# CAT-066 · wall — declaration readiness spec

**Status (22 November 2026, checkout `8d1d68aa`):** delivered as package `wall` (`elements/Wall/`) for body contact, air occlusion, resize, save and Reset. Light and sound occlusion wait for their engines. Section 2 is the delivered declaration and section 3 the capability probe of that date. Native proof is current; browser qualification is pending until every named element passes. Citations pinned `@a6c914e` in section 4 are source facts, not instructions. No visible-solid defect is open: one box collider takes the full declared size and the stripes are flush. Every residual slice (the tests-only rows and both occlusion rows) is blocked on the engine programme (owner decision 3a, 22 November 2026); the part's engine gaps are owned by `ENGINE-SPATIAL-QUERIES` (the airflow occlusion pass that reads this collider scans every collider), `ENGINE-OPTICAL` (light occlusion) and Story 14.0 (sound occlusion).

## 1. Identity

| Field | Value |
| --- | --- |
| CAT ID / kind | CAT-066 · `wall` |
| Requirement | [CAT-066](../requirements.md#current-cat-066) |
| Mapped identities | EL-191 Wall (refines this part; §8). Related barrier identities with their own specs: EL-073 Brick barrier, EL-074 Wood barrier, EL-050 Acoustic screen, TH-09 Insulating panel. |
| Disposition | **Delivered.** Package id `wall`, directory `elements/Wall/`. Slice record: [9A.27 generic Wall](../../../_bmad-output/implementation-artifacts/spec-9a-27-generic-wall.md) ([review](../../../_bmad-output/implementation-artifacts/review-9a-27-generic-wall.md)). |
| Roadmap | Stories 1.4 (box contact, local-axis resize), 1.5 (anti-tunnelling), 9A.27 |
| Levels | `wall_and_bumper` (Playable); `solar_shadow` is Planned (`content/puzzles.json`). See [CAT-066-I consumers](../invest/current-consumers.md#cat-066-i). |

## 2. Declaration

Delivered declaration: `elements/Wall/WallElementDeclaration.cs`; scene `elements/Wall/wall.tscn`. Interfaces (`WallElementDeclaration.cs:8`): `IMechanicalElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:7`), `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`), `IParameterizedGeometryElement` (`SceneGeometry.cs:29`). The Ramp is the closest delivered shape; the Wall has a third resize axis and no rails.

| Item | Value and source |
| --- | --- |
| Body | One static body (`KinematicMode.Static`, `:58`; footprint 1 body, 0 moving, no constraints, `:33`), at any full 3D pose. Mass frame zero (`:62`–`68`). |
| Collider | One box baked from the scene (`Colliders => []`, `:47`; `wall.tscn:18`–`19`, node `col_0_0`, `:36`), half extents (width/2, height/2, thickness/2) m at the origin, sized by three `ColliderBindings` (`:40`–`45`). |
| Material (f32) | restitution 1, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s (`:51`). Restitution 1 is neutral under the product pair law. |
| Parameters | `width`: Real, metres, default 3, minimum 0.4, maximum 8 (`:28`). `height`: Real, metres, default 2, minimum 0.4, maximum 6 (`:29`). `thickness`: Real, metres, default 0.25, minimum 0.12, maximum 2 (`:30`). All three take the descriptor defaults `ParameterEditorPolicy.Reject` and `ParameterRunEdit.BuildOnly` (`CuriousContraptions.Abstractions/ParameterDescriptor.cs:60`–`61`). |
| Ports, events, stores, sensors, signal/supply/electrical nodes, anchors | none (`Ports`, `:35`; `Events`, `:54`; `AnchorPoints`, `:70`; `TubeMouthBindings`, `:38`; `SupportedDomains` is Mechanical only, `:56`). `PayloadPolicy.None` (`:60`). It declares nothing optical or acoustic. |
| Presentation | Pick radius 1.5 m; resize axes `PresentationAxes.All` (`:10`). Three parameter bindings on the unit node `Visual`: `ScaleX` = `width`, `ScaleY` = `height`, `ScaleZ` = `thickness` (`:11`–`13`). Sizing: dimensions (3, 2, 0.25) m, radius scale 0.5 on each axis, `width` on X, `height` on Y, `thickness` on Z (`:14`–`15`). No event or state binding. Handle behaviour is the workshop's general resize rule (`DESIGN.md:311`): square handles on the local axes, symmetric about the centre, Shift snaps to 0.1, Escape cancels, one gesture is one Undo. |
| Catalogue | id `wall`, title Wall, category Structure, palette order 5, colour (0.76, 0.56, 0.32) = Physical wall `#c28f52` (`DESIGN.md:237`), icon `res://elements/Wall/icon.svg`, scene `res://elements/Wall/wall.tscn` (`:17`–`31`). Placement reference walls are invisible and are not this part. |

At a glance: a tall plain panel with two flush vertical cream stripes near its ends. The Ramp shares the colour but is a thin slab with raised rails; the barrier family (EL-073, EL-074) must differ by surface pattern (brick courses, wood grain), not by colour alone.

## 3. Engine capabilities

Probe of 22 November 2026. Each row cites the symbol and an existing test, or the one owning story.

| Capability | Status | Symbol | Test or owner |
| --- | --- | --- | --- |
| Static box contact at any rotation, without tunnelling up to the 64 m/s limit | present | `WorkshopCollisionKernels.CollideBoxSphere`, `AdvanceSpeculativeSphereContact` (`CuriousContraptions.Simulation/WorkshopCollisionKernels.cs:349`, `:1940`) | `WorkshopWallTests.RealSimulationRotatedWallBlocksMovingBallWhileAbsentWallDoesNot` (yaw 0°, 45°, 90° at 4 m/s; 45° at 64 m/s) |
| Product-law rebound off a static face | present | the shared contact solver (`CuriousContraptions.Simulation/WorkshopConstraintSolver.cs`) | `VisibleSolidColliderTests.ABallDroppedOntoAStaticWallReboundsAtItsOwnRestitution` |
| Three declared parameters size the collider | present | `ColliderParameterBinding`, applied at `CuriousContraptions.Bundle/BundleGeometry.cs:28` | `WorkshopWallTests.ThreeCanonicalDimensionsPoseAndIdentityRoundTripAtDefaultAndBothLimits` |
| Art, projection and three resize axes follow the parameters and pose | present | `PresentationParameterBinding`, `PresentationSizing` (`CuriousContraptions.Abstractions/ElementPresentation.cs:119`, `:123`) | `WorkshopWallTests.ActualResourceThreeAxesArtworkAndProjectionFollowCanonicalFullPose` |
| Air occlusion: an enabled collider between source and receiver removes that sample's transfer | present | `CuriousContraptions.Simulation/WorkshopPhysicsEngine.Airflow.cs:151` (every enabled collider other than the receiver occludes; no rule names a part) | `FanGuidanceTests.ElectricalConeGuidesFallingTennisToReceiverWithMatchedControls`, row `Control.SolidWall`: a `wall` package (3 × 2 × 0.25 m, turned 90°) across the stream leaves the ball undelivered and the airflow work zero |
| Light occlusion | missing | none | [ENGINE-OPTICAL](../../../_bmad-output/planning-artifacts/epics.md#story-engine-optical-beam-emission-transport-surface-interaction-and-reception) |
| Sound occlusion | missing | none | [Story 14.0 ENGINE-ACOUSTIC](../../../_bmad-output/planning-artifacts/epics.md#story-140-acoustic-propagation-occlusion--reception-engine-engine-acoustic) |

Counts: 5 present, 0 untested, 0 narrower, 2 missing.

### Engine conformance

The package is declaration data and art. It has no script, callback, update loop or state machine, and no engine code names it: the audit's search of `engine/`, the `CuriousContraptions.*` engine projects and `ui/` for the id, class name and lesson ids found no literal and no branch. No residue is recorded against this spec. Occlusion of light and of sound must arrive as general rules over colliders or declared materials, as air occlusion did; a rule that names the Wall would fail the element-free gate.

**Compiled counts.** Bodies 1 (static). Colliders 1: 1 box. Joints 0. Drives 0. Sensors 0. Domain nodes 0. Stores 0. Presentation bindings 3 (all parameter bindings). Collider parameter bindings 3. All fixed at compile time.

**Layers.**

| Art node (`wall.tscn`) | Mesh | Classification | Collider or reason | Moved by |
| --- | --- | --- | --- | --- |
| `Visual/Visual_Body` (`:25`) | unit box 1 × 1 × 0.998, scaled to the dimensions | solid | `col_0_0`, the full box | nothing during Run |
| `Visual/Visual_StripeLeft` (`:28`), `Visual/Visual_StripeRight` (`:32`) | boxes 0.035 × 0.94 × 1 at x ∓0.46 of the unit box, cream (0.976, 0.91, 0.788) | decorative: printed flat | flush with the collider's faces and inside it; nothing stands proud of the collider | nothing during Run |

The body is drawn at 0.998 of the thickness so the stripes do not z-fight with it; the collider takes the full thickness, so it is 0.001 × thickness thicker per face than the drawn body, at most 2 mm at the 2 m maximum. The hand table records this as a stated fit (`CuriousContraptions.tests/VisibleSolidColliderTests.cs:136`, `Fit: .002f`). The three presentation bindings size the `Visual` node from the three `BuildOnly` parameters and the three collider bindings size the box from the same parameters: static sizing in Build, not animation.

**Generic laws.**

| Behaviour | Law | Declared values |
| --- | --- | --- |
| Blocks and deflects a ball at any rotation | contact of a dynamic body with a static box at the placed pose | half extents (width/2, height/2, thickness/2); the pose |
| The ball rebounds by its own liveliness | restitution, pair value the product | 1 (neutral) |
| The ball slides along a face | Coulomb friction | 0.3 |
| A fast ball does not pass through | speculative sphere contact; committed speed limit | none of the part's; engine limit 64 m/s |
| Shelters a ball from a stream | airflow occlusion by any enabled collider on the sample line | the box |
| A resized wall behaves as drawn | collider and art sized from one parameter per axis | `width`, `height`, `thickness` |

**Collider count.** One, the minimum.

**Performance fitness.** A static body with one box. Tunnelling is excluded by the engine's speculative contact and speed limit, proven against this package at 64 m/s with the default 0.25 m thickness. No sensor, no region, nothing proportional to the scene in the package. Engine gap, not an element fault: the airflow occlusion pass that reads this collider walks every collider per sample line (`WorkshopPhysicsEngine.Airflow.cs:151`); that scene-proportional pass is owned by `ENGINE-SPATIAL-QUERIES` (owner decision 3a), and the optical and acoustic occlusion rules take their candidates from the same query interface instead of adding another scan.

## 4. Legacy harvest

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | Setting 5 × 3 × 0.6 gives a collision box half (2.5, 1.5, 0.3) and artwork bounds 5 × 3; rotation changes the bounds; Reset restores dimensions, transform and saved construction exactly. | `CuriousContraptions.tests/WallTests.cs@a6c914e:L23-L53` | carry forward. |
| 2 | Out-of-range dimensions clamp to the limits ((−2, 100, 0) → (0.4, 6, 0.12)); a NaN input was silently ignored. | `CuriousContraptions.tests/WallTests.cs@a6c914e:L54-L57` | not carried for submitted or saved values: the delivered policy is `Reject`, so an out-of-range or non-finite value is refused atomically (`WorkshopWallTests.InvalidSubmittedParametersRejectAtomicallyWhileExactF32ValuesRoundTrip`). The limits themselves are carried. A handle drag stopping at the bound is UI behaviour (browser proof). |
| 3 | Each of the three resize handles follows the rotated local axis; a drag of 0.3 screen-axis lengths grows that dimension by 0.6 snapped to 0.1; position and rotation stay; Cancel restores dimensions and collision. | `CuriousContraptions.tests/WallTests.cs@a6c914e:L62-L97` | carry forward. |
| 4 | A 4 × 3 × 0.4 wall rotated 0°, 45° or 90° reflects a 4 m/s ball along its face normal; the ball ends ≥ 0.2 + radius from the centre plane. | `CuriousContraptions.tests/WallTests.cs@a6c914e:L99-L124` | carry forward. |
| 5 | wall_return and wall_and_bumper are won only with both wall and bumper at precision 0, 0.45 and 1; omitting either fails; Reset restores the reference wall 0.4 × 6 × 1.5. | `CuriousContraptions.tests/WallLessonTests.cs@a6c914e:L10-L44` | carried for `wall_and_bumper`, which is played at precision 0.45 with the wall removed as its control. `wall_return` is Planned and has no content yet. |
| 6 | Reference solution: wall_1 at (−1, 4, 0), width 0.4, height 6, thickness 1.5; the fixed bumper at (−3.2, 1.5, 0); receiver at (−5, 0.9, 0). solar_shadow shade wall at (−0.5, 3, 0), yaw 90°, 3 × 3 × 0.3. | `tools/Campaign/Program.cs@a6c914e:L104-L123`; `tools/Campaign/Program.cs@a6c914e:L240-L246` | carry forward as level data; the current authored poses are those in `content/puzzles.json`. |
| 7 | Resizing is rejected while Running or Paused, without changing dimensions, boxes, the saved construction or the physics world; after Reset the same edit applies and survives Run/Reset. | `CuriousContraptions.tests/ConstructionLifecycleTests.cs@a6c914e:L92-L139` | carry forward (shared with CAT-048); native row planned in §5. |
| 8 | Light: a 4 × 4 × 0.2 wall 3 m in front of the torch stops every cone ray at x ≈ 2.9; a 3 × 3 × 0.4 wall between torch and solar panel keeps the motor off until its collider is disabled. | `CuriousContraptions.tests/LightTests.cs@a6c914e:L97-L106`; `CuriousContraptions.tests/LightTests.cs@a6c914e:L180-L210` | carry forward as the light-occlusion acceptance; `LightConeVisual` CPU ray sampling is not carried. |
| 9 | Sound: a wall at (−1, 6, 0) between bell and sound meter means the meter receives nothing and the gate never opens. | `CuriousContraptions.tests/BellTests.cs@a6c914e:L64-L89` | carry forward as the sound-occlusion acceptance. |
| 10 | Air: a wall at (−2.5, 5.88, 0), yaw 90°, in the bellows jet path makes the expected jet force zero. | `CuriousContraptions.tests/BellowsTests.cs@a6c914e:L150-L150`; `CuriousContraptions.tests/BellowsTests.cs@a6c914e:L192-L199` | carried: air occlusion by this package is proven with the Fan (`Control.SolidWall`). The Bellows row arrives with the Bellows. |
| 11 | Wall catalogue parameters are required; removing one rejects. | `CuriousContraptions.tests/RequiredPhysicsParameterTests.cs@a6c914e:L47-L80` | carry forward the no-default rule. |
| 12 | GPU wall probe fixtures: default face (ball at y 6), maximum face (y 8), rotated edge (yaw 0.3, pitch 0.2, roll −0.35), miss (x 6), initial overlap rejected at admission. | `reference/wall/Program.cs@a6c914e:L21-L60` | carry forward the fixture matrix; ABI byte validation is no element knowledge. |
| 13 | In the Workshop UI the "Resize mode" button is hidden until a wall (or pipe) is placed and selected, then visible; pressing it puts the gizmo in resize mode; a Shift-drag of the X handle by half a world unit grows the width by 1 (symmetric about the centre); one "↺ Undo" restores the original dimensions and keeps the part. | `CuriousContraptions.tests/WorkshopInteractionTests.cs@a6c914e:L703-L741` | carry forward (contextual resize, one gesture is one Undo; browser proof). |

Files harvested:
- `CuriousContraptions.tests/WallTests.cs`
- `CuriousContraptions.tests/WallLessonTests.cs`
- `CuriousContraptions.tests/ConstructionLifecycleTests.cs`
- `CuriousContraptions.tests/LightTests.cs`
- `CuriousContraptions.tests/BellTests.cs`
- `CuriousContraptions.tests/BellowsTests.cs`
- `CuriousContraptions.tests/RequiredPhysicsParameterTests.cs`
- `CuriousContraptions.tests/WorkshopInteractionTests.cs` (L703-L741)
- `tools/Campaign/Program.cs`
- `reference/wall/` (Program.cs; inputs.json and csproj hold no further knowledge)

## 5. Acceptance outline

Acceptance: [CAT-066](../requirements.md#current-cat-066). Every row runs the production simulation; none depends on a wall-specific code path.

| Given / When / Then | Test (exists unless marked planned) | Control and boundary |
| --- | --- | --- |
| A rotated wall blocks a moving ball | `WorkshopWallTests.RealSimulationRotatedWallBlocksMovingBallWhileAbsentWallDoesNot` | the same shot with no wall crosses the plane; 64 m/s does not tunnel |
| Dimensions, pose and identity round-trip at the default and both limits | `WorkshopWallTests.ThreeCanonicalDimensionsPoseAndIdentityRoundTripAtDefaultAndBothLimits`; `GenericShapeEnvelopeAcceptsSixteenAndRejectsTheNextF32Value` | 0.4 × 0.4 × 0.12 and 8 × 6 × 2 |
| Invalid dimensions are refused atomically | `WorkshopWallTests.InvalidSubmittedParametersRejectAtomicallyWhileExactF32ValuesRoundTrip` | exact f32 values that are not exact in half precision round-trip |
| Reset and save/load restore the wall and its contact | `WorkshopWallTests.RealSimulationResetAndSaveLoadRestoreGenericWallAndItsContact` | — |
| Art, projection and collision follow the three axes and full pose | `WorkshopWallTests.ActualResourceThreeAxesArtworkAndProjectionFollowCanonicalFullPose`; `VisibleSolidColliderTests.EverySolidArtPartHasItsColliderAndNoColliderReachesBeyondTheArt` | parameter sweep in the theory |
| A wire into a wall, and a wall in a lesson that does not offer one, are refused | `WorkshopWallTests.WallPopulationPuzzleAndForeignPortsReject` | two walls in free play are admitted |
| `wall_and_bumper` solves, and not without the wall | `LaunchLessonSolveTests.AuthoredLessonSolvesAndOneMissingCausePreventsTheGoal` (precision 0.45, at 60, 120 and 240 Hz) | the control removes the wall |
| A ball rebounds off a wall face at the product restitution | `VisibleSolidColliderTests.ABallDroppedOntoAStaticWallReboundsAtItsOwnRestitution` | — |
| A wall across a stream stops the stream reaching a ball | `FanGuidanceTests.ElectricalConeGuidesFallingTennisToReceiverWithMatchedControls` (`Control.SolidWall`) | `Control.Supplied`: with no wall the ball is delivered |

Residuals. The first three are one tests-only patch. Their numbers are chosen from part sizes and the existing wall test, not from a run, and are confirmed when the tests are written.

- **Resize refused during Run** (fact 7; requirement "saved properties"). No native test of it was found by name. Planned `WorkshopWallTests.AResizeWhileRunningOrPausedIsRefusedAndTheSameEditAppliesAfterReset`: a default wall in a Run, then in Pause; a `width` edit from 3 to 4 is submitted. Then a typed refusal is returned, the read-back parameters are still (3, 2, 0.25) and a ball still stops at the original face; after Reset the same edit applies and the width reads 4. Control: the edit in Build applies at once. The refusal's enum member is the one the command path returns for an edit outside Build (a Save during Run returns `WorkshopRejection.WrongPhase` in `LaunchLessonSolveTests`); the slice confirms it by reading the command handler.
- **A gap lets a ball pass** (EL-191 outcome: "a geometric gap allows passage; invisible silhouette bounds do not block it"). Planned `WorkshopWallTests.ABallPassingBesideTheWallEdgeIsNotBlockedAndOneInsideTheEdgeIs`: the scene of the rotated-wall test at yaw 0 (wall 3 × 6 × 0.25 m at (0, 3, 0), ball launched at 4 m/s along +z from z = −2, 90 ticks). From x = 1.95 the ball is 0.10 m clear of the wall's edge at x = 1.5 (0.35 m at the seam); it crosses the wall's plane and its furthest z exceeds 0.3. From x = 1.0 it is blocked and its furthest z stays below −0.2. Both bounds are the existing test's. Added case in the same theory, inside the silhouette ([EL-191](EL-191-wall.md) row A2): the same wall turned 45° about Y and a Basketball sent at 4 m/s along the face on the line 0.6 m in front of it (0.725 m from the centre plane, so its seam clears the face by 0.25 m), for 90 ticks; it passes inside the wall's bounding box and pick radius, its distance from the face plane stays within 0.05 m of 0.725 m, and it travels more than 2.8 m along the face. 0.05 m is a fifth of the 0.25 m clearance, so a graze by any oversized bound fails.
- **Minimum thickness at the speed limit.** Planned `WorkshopWallTests.AMinimumThicknessWallStopsABallAtTheSpeedLimit`: the rotated-wall scene at yaw 0 with `thickness` 0.12 and the ball launched at 64 m/s. Then its furthest z stays below −0.2; with no wall it exceeds 0.3. The failure excluded is a fast ball passing through the thinnest wall: at 64 m/s a ball travels 0.133 m in one 480 Hz substep, more than the 0.12 m minimum thickness, and the existing 64 m/s row uses the 0.25 m default.
- **Light occlusion** (fact 8, `solar_shadow`): waits for ENGINE-OPTICAL, Flashlight (CAT-029) and Solar panel (CAT-059). The wall's box occludes with the default response Absorb (EL-176 section 10); the package declares nothing optical. Row planned there: a 3 × 3 × 0.3 wall between source and panel keeps the consumer off; removing it turns the consumer on.
- **Sound occlusion** (fact 9): waits for Story 14.0, Bell and Sound meter. Row planned there: the meter stays below threshold with the wall between; above it without.
- **Cancel, one-gesture Undo and the handle stopping at a bound**: UI behaviour; browser proof pending post-catalogue (`9r-14-cat-066-wall-suite`, backlog).

Consistency with delivered parts: the default 2 m height and the 0.4 m minimum are both taller than half of any of the four delivered balls (Basketball 0.70 m at its seam, Bowling ball 0.56 m, Tennis ball 0.50 m, Baseball 0.54 m at its band, the repair values of coordinator decision 36), so a wall standing on the bench always blocks a rolling ball; a thin wall at the speed limit depends on the engine's speculative contact (residual above).

## 6. Open questions

Resolved:

- A submitted or saved out-of-range dimension is refused (delivered `Reject` policy); only an interactive handle drag stops at the bound (standing delegation). Reason: one rule for saves, commands and lessons, and a player dragging a handle should feel a stop, not an error.
- Light occlusion is decided: every participating collider occludes with the default response Absorb (EL-176 section 10, item 6), so the Wall declares nothing optical.
- Sound occlusion is decided by Story 14.0 at planning (D16): either the collider occludes, as for air, or a declared material does. EL-050 Acoustic screen attenuates and does not reflect (owner, 21 November 2026); the Wall blocks.
- No further Fan row is planned for air occlusion: `Control.SolidWall` already uses this package (no uncovered failure).

**Needs the owner:** nothing.

## 7. Integration

- **Collaborators.** Delivered: every ball kind, Bumper (`wall_and_bumper`), Receiver, Fan (air occlusion). Not delivered: Flashlight (CAT-029), Solar panel (CAT-059), Bell (CAT-009), Sound meter (CAT-060), Bellows (12.4, in flight).
- **Campaign.** `wall_and_bumper` (Playable); `solar_shadow` (Planned). First use 1–10 in the [element coverage ledger](../requirements.md#campaign-element-coverage) (physical presets row: wall variants); wall and bumper teaching [sequence-task-304](../requirements.md#sequence-task-304) and [todo-163](../requirements.md#todo-163).
- **Coverage binding.** Catalogue key `wall` in `docs/coverage/catalogue-elements.json`.
- **Lesson fixture.** None needed (S8 not required).

## 8. Delivery

- **Disposition.** Delivered. No slice is owed for the part.
- **Residual work, in order.** Every residual slice is blocked on the engine programme (owner decision 3a, 22 November 2026), which precedes every further element. (1) The three tests-only rows (resize during Run, the gap with its 45° case, minimum thickness at the speed limit): one patch, data-only review tier. (2) Light occlusion inside the first optical element slice after ENGINE-OPTICAL. (3) Sound occlusion inside the first acoustic element slice after Story 14.0. (4) Browser suite 9R.14, post-catalogue.
- **EL-191 planning check** (delivery-batches 2c: optical and acoustic occlusion by the same box). EL-191 stays open until rows (1) to (3) pass; it gets no package of its own.
- **Family note.** The barrier-materials family (EL-073, EL-074) follows this declaration shape; it must take its own palette orders (5 and 8 are used by Wall and Ramp) and differ by pattern.
- **Disqualifiers.** Either engine story requiring Wall-specific code or a second occlusion rule beside its general one (element-free gate); a change to the bounds; removal of the declared `Fit` without redrawing the body at full thickness.
