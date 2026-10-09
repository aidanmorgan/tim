# CAT-066 · wall — declaration readiness spec

## 1. Identity

| Field | Value |
| --- | --- |
| CAT ID / kind | CAT-066 · `wall` |
| Requirement | [CAT-066](../requirements.md#current-cat-066) |
| Mapped identities | EL-191 Wall. Related barrier identities with their own specs: EL-073 Brick barrier, EL-074 Wood barrier, EL-050 Acoustic screen, TH-09 Insulating panel. |
| Roadmap | Stories 1.4 (Box contact, local-axis resize), 1.5 (anti-tunnelling) |
| Status | delivered for body contact and resize; light/air/sound occlusion and remaining levels open (§5) |
| Levels | placed in solar_shadow; inventory in wall_return, wall_and_bumper; see [CAT-066-I consumers](../invest/current-consumers.md#cat-066-i) |

## 2. Declaration

| Item | Value and source |
| --- | --- |
| Body and shape | One static box, half extents (width, height, thickness)/2, identity local pose, full 3D pose `engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L83-L101`, `engine/gpu/WorkshopWall.cs@a6c914e:L30-L40`. |
| Material | restitution 1, bounce threshold 0.1 m/s, friction 0.3, rolling resistance 0 `engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L87-L90`. |
| Constraints and joints | none (static). |
| Sockets and ports | none `engine/gpu/WorkshopConnections.cs@a6c914e:L18-L26`. |
| Sensors and activation | none. |
| Work and energy stores | none. |
| Parameters | width 0.4–8 m (default 3), height 0.4–6 m (default 2), thickness 0.12–2 m (default 0.25); pointer input clamps finite values and rejects non-finite `engine/gpu/WorkshopWall.cs@a6c914e:L6-L28`; resource bits `engine/WallDimensionsResource.cs@a6c914e:L8-L19`. UI rule: square handles on the three local axes, symmetric about the centre, Shift snaps to 0.1, Escape cancels, one gesture is one Undo `DESIGN.md@a6c914e:L252-L252`. |
| Cosmetic curves and UI bindings | none; resize axes All `parts/WallPart.cs@a6c914e:L10-L10`. A contextual "Resize mode" button appears only while a resizable part is selected (§4 fact 13). |
| Art | `parts/WallPart.cs@a6c914e:L13-L27`: unit box scaled to the dimensions (depth 0.998), two cream `#f9e8c9` edge bands 0.035 × 0.94 × 1 at x ±0.46; pick radius half the diagonal. Scene `parts/scenes/wall.tscn`. Palette Physical wall `#c28f52` `DESIGN.md@a6c914e:L178-L178`. Icon `ui/WorkshopIcons.cs@a6c914e:L44-L44`. Placement reference walls are invisible and are not puzzle walls `DESIGN.md@a6c914e:L245-L252`. |
| Catalogue and inventory | `parts/catalog/wall.tres@a6c914e:L7-L21`: id `wall`, title Wall, category Structure, default 3 × 2 × 0.25. |

Current tests: `CuriousContraptions.tests/WorkshopWallTests.cs` (round trip at default and both limits, input clamp, invalid serialized dimensions, resource artwork and projections), `WorkbenchCapacityTests.cs`; Chrome `tools/e2e/engine-core-2a3.test.ts` (deflection, local-axis resize), `engine-core-2a4.test.ts`, `engine-core-2b3.test.ts` (no tunnelling).

## 3. Engine capabilities

Families from the [element map row](../general-engine-element-map.md): ContactImpulse, EnvironmentState, FiniteLedger, GeometryQuery, RigidBodyDynamics, SlidingFriction.

| Exists now | Reference |
| --- | --- |
| Static box contact, speculative contacts | Stories 1.4, 1.5 |
| Local-axis resize, dashed projections | `ui/RotationGizmo.cs`, `ui/PlacementShadows.cs` |

| Missing | Story that builds it |
| --- | --- |
| Light occlusion (wall blocks flashlight/laser) | Epic 13 (Story 13.1 introduces optical transport) |
| Air occlusion (wall blocks fan/bellows jets) | Epic 12 (Stories 12.2, 12.4) |
| Sound occlusion (wall blocks bell → sound meter) | Epic 14 (Stories 14.2, 14.1) |
| f32 declarations: `WallDimensions` (default, minimum and maximum), pose and the `WallDimensionsResource` `*Bits` fields are still binary16 (`Half`); pointer input is clamped before binary16 conversion | Remaining f32 migration ([f32 migration status](../../gpu-f32-physics.md#f32-migration-status)) |

Dependencies: none for contact. Occlusion integrations depend on the emitting elements above.

## 4. Legacy harvest

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | Setting 5 × 3 × 0.6 gives a collision box half (2.5, 1.5, 0.3) and artwork bounds 5 × 3; rotation changes the bounds; Reset restores dimensions, transform and saved construction exactly. | `CuriousContraptions.tests/WallTests.cs@a6c914e:L23-L53` | carry forward. |
| 2 | Out-of-range dimensions clamp to the limits ((−2, 100, 0) → (0.4, 6, 0.12)); a NaN input was silently ignored. | `CuriousContraptions.tests/WallTests.cs@a6c914e:L54-L57` | carry forward the clamp; do not carry forward the silent NaN ignore (current input boundary rejects non-finite explicitly). |
| 3 | Each of the three resize handles follows the rotated local axis; a drag of 0.3 screen-axis lengths grows that dimension by 0.6 snapped to 0.1; position and rotation stay; Cancel restores dimensions and collision. | `CuriousContraptions.tests/WallTests.cs@a6c914e:L62-L97` | carry forward. |
| 4 | A 4 × 3 × 0.4 wall rotated 0°, 45° or 90° reflects a 4 m/s ball along its face normal; the ball ends ≥ 0.2 + radius from the centre plane. | `CuriousContraptions.tests/WallTests.cs@a6c914e:L99-L124` | carry forward. |
| 5 | wall_return and wall_and_bumper are won only with both wall and bumper at precision 0, 0.45 and 1; omitting either fails; Reset restores the reference wall 0.4 × 6 × 1.5. | `CuriousContraptions.tests/WallLessonTests.cs@a6c914e:L10-L44` | carry forward. |
| 6 | Reference solution: wall_1 at (−1, 4, 0), width 0.4, height 6, thickness 1.5; the fixed bumper at (−3.2, 1.5, 0); receiver at (−5, 0.9, 0). solar_shadow shade wall at (−0.5, 3, 0), yaw 90°, 3 × 3 × 0.3. | `tools/Campaign/Program.cs@a6c914e:L104-L123`; `tools/Campaign/Program.cs@a6c914e:L240-L246` | carry forward (content in `content/puzzles.json`). |
| 7 | Resizing is rejected while Running or Paused, without changing dimensions, boxes, the saved construction or the physics world; after Reset the same edit applies and survives Run/Reset. | `CuriousContraptions.tests/ConstructionLifecycleTests.cs@a6c914e:L92-L139` | carry forward (shared with CAT-048). |
| 8 | Light: a 4 × 4 × 0.2 wall 3 m in front of the torch stops every cone ray at x ≈ 2.9; a 3 × 3 × 0.4 wall between torch and solar panel keeps the motor off until its collider is disabled. | `CuriousContraptions.tests/LightTests.cs@a6c914e:L97-L106`; `CuriousContraptions.tests/LightTests.cs@a6c914e:L180-L210` | carry forward as the light-occlusion acceptance; do not carry forward `LightConeVisual` CPU ray sampling. |
| 9 | Sound: a wall at (−1, 6, 0) between bell and sound meter means the meter receives nothing and the gate never opens. | `CuriousContraptions.tests/BellTests.cs@a6c914e:L64-L89` | carry forward as the sound-occlusion acceptance. |
| 10 | Air: a wall at (−2.5, 5.88, 0), yaw 90°, in the bellows jet path makes the expected jet force zero. | `CuriousContraptions.tests/BellowsTests.cs@a6c914e:L150-L150`; `CuriousContraptions.tests/BellowsTests.cs@a6c914e:L192-L199` | carry forward as the air-occlusion acceptance. |
| 11 | Wall catalogue parameters are required; removing one rejects. | `CuriousContraptions.tests/RequiredPhysicsParameterTests.cs@a6c914e:L47-L80` | carry forward the no-default rule. |
| 12 | GPU wall probe fixtures: default face (ball at y 6), maximum face (y 8), rotated edge (yaw 0.3, pitch 0.2, roll −0.35), miss (x 6), initial overlap rejected at admission. | `reference/wall/Program.cs@a6c914e:L21-L60` | carry forward the fixture matrix; ABI byte validation is no element knowledge. |
| 13 | In the Workshop UI the "Resize mode" button is hidden until a wall (or pipe) is placed and selected, then visible; pressing it puts the gizmo in resize mode; a Shift-drag of the X handle by half a world unit grows the width by 1 (symmetric about the centre); one "↺ Undo" restores the original dimensions and keeps the part. | `CuriousContraptions.tests/WorkshopInteractionTests.cs@a6c914e:L703-L741` | carry forward (contextual resize, one gesture is one Undo). |

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

Acceptance: [CAT-066](../requirements.md#current-cat-066).

- **Construction.** Free workshop: place Wall, resize each local axis with the square handles, rotate; wall_return: place one wall right of the fixed bumper.
- **Positive.** Rotated impacts reflect along the face normal; collision, artwork and dashed projections update together.
- **Negative/control.** Limits and invalid dimensions; resize during Run rejected; Cancel and one-gesture Undo.
- **Boundaries.** Minimum 0.4 × 0.4 × 0.12 and maximum 8 × 6 × 2 walls; maximum-speed ball does not tunnel.
- **Run/Reset, Save/Load.** Saved dimensions and pose restored exactly.
- **Integrations.** Contact and cargo connection audit [sequence-task-285](../requirements.md#sequence-task-285) (ramps/walls); wall and bumper teaching [sequence-task-304](../requirements.md#sequence-task-304) and [todo-163](../requirements.md#todo-163); [IX-01 contact impulse](../requirements.md#interaction-01), with the occlusion controls under [IX-12 acoustic propagation](../requirements.md#interaction-12) and [IX-13 optical transport](../requirements.md#interaction-13); campaign first use 1–10 in the [element coverage ledger](../requirements.md#campaign-element-coverage) (physical presets row: wall variants). Partners: Bumper (CAT-015), Receiver (CAT-004), Flashlight and Solar panel (CAT-029, CAT-059) in solar_shadow.
- **Remaining (unmet now).** Light, air and sound occlusion controls (facts 8–10) wait for their emitters; solar_shadow and the wall/bumper levels wait for Flashlight/Solar panel and Bumper (Story 6.3).

## 6. Open questions

none.
