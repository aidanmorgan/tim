# CAT-034 · impact_lever declaration readiness spec

This is the Story 7.0 CAT-034-D declaration readiness spec for the impact lever. The baseline is commit `a6c914e`. Legacy citations use `path@a6c914e:Lstart-Lend` and stay retrievable from git history after the Epic 7 purge. Current-engine files are cited at the same commit. Legacy world units are carried as metres, as the delivered Switch and Bumper declarations already do (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L109-L122`).

## 1. Identity

| Field | Value |
| --- | --- |
| CAT ID | CAT-034 |
| Kind | `impact_lever`, catalogue title "Impact lever" (`parts/catalog/impact_lever.tres@a6c914e:L6-L7`) |
| Requirement anchor | [CAT-034](../requirements.md#current-cat-034); retained behaviour [sequence-task-307 / todo-409](../requirements.md#sequence-task-307) |
| Mapped identities | None. No EL, TH, RAD or GAP identity in [named-elements.md](../invest/named-elements.md) names an impact lever, teeter-totter or fulcrum beam. Batch A confirms. |
| Roadmap story | [10.3 Balanced Impact Lever & Pivot Fulcrum](../../../_bmad-output/planning-artifacts/epics.md) (Epic 10) |
| Status | Not started. There is no `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3`). |

## 2. Declaration

### Bodies and shapes

- **Beam.** One dynamic box with half extents 1.8 × 0.12 × 0.55 m, centred on the pivot. Its length runs along part-local X (`parts/ImpactLeverPart.cs@a6c914e:L16`, `parts/ImpactLeverPart.cs@a6c914e:L59`). Declare it as one `RigidBodyDeclaration` (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L86`) plus one Box `ColliderDeclaration` (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L120`). A dynamic body admits exactly one homogeneous box or sphere (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L226-L234`), so the beam fits today.
- **Fixture.** One static body that owns three boxes:
  - a base box, full size 1.4 × 0.16 × 1 m (half 0.7 × 0.08 × 0.5) at (0, −1.18, 0);
  - two end-stop boxes with half extents 0.13 × 0.15 × 0.3 m at (±1.5, −1.004, 0).

  Sources: `parts/ImpactLeverPart.cs@a6c914e:L60`, `parts/ImpactLeverPart.cs@a6c914e:L64-L70`, and the full-size convention of `AddBox` in `reference/cpu/MachinePart.cs@a6c914e:L352-L356`. Multiple boxes on one static body already have a precedent in the Switch (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L112-L115`).
- **Pivot post and pin.** These are art only in the legacy, with no collider (`parts/ImpactLeverPart.cs@a6c914e:L61-L63`). A solid fulcrum is an open question.

### Mass and material

- **Beam mass.** Taken from `beam_mass`, default 2 kg. The inertia is the homogeneous box law m(b²+c²)/3 on half extents (`parts/ImpactLeverPart.cs@a6c914e:L30-L40`). That is the law `RigidMassProperties.Compile` already implements for boxes (`engine/gpu/RigidMassProperties.cs@a6c914e:L26-L51`), so compile the inertia there rather than authoring it.
- **Beam material.** Restitution 0, bounce threshold 0.1 m/s, friction 0.3 (`parts/ImpactLeverPart.cs@a6c914e:L41`; the argument order is in `engine/physics/PersistentContactPair.cs@a6c914e:L18-L29`). Declare it as a `ContactMaterialDeclaration` with rolling resistance 0, the current value for non-sphere bodies (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L54`; `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L721-L726`).
- **Fixture material.** The lever does not override it. Static legacy surfaces default to (1, 0.1, 0.3), as recorded for the delivered static parts (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L109-L110`).

### Constraints and joints

- **Hinge.** One revolute hinge joins the beam (body A) to the fixture (body B). Both local frames are identity at the beam centre, so the free axis is part-local Z (`parts/ImpactLeverPart.cs@a6c914e:L20-L27`; the frame's Z is the free axis per `engine/physics/JointEquations.cs@a6c914e:L8-L10`).
- **Collision and travel.** Connected-body collision is Disabled. Travel is bounded to [−π/6, +π/6] rad with direction Both (`parts/ImpactLeverPart.cs@a6c914e:L17`, `parts/ImpactLeverPart.cs@a6c914e:L26`).
- **New declaration type.** No joint declaration exists in `engine/gpu`. Story 6.4 adds the first joint (prismatic). Story 10.3 must add a revolute hinge declaration with a travel interval and a connected-collision policy.

### Typed sockets and ports

- **Legacy.** The legacy lever declares no ports (`parts/ImpactLeverPart.cs@a6c914e:L11-L92` has no port override, and the catalogue entry lists none).
- **Requirement.** The requirement demands end rope sockets ([CAT-034](../requirements.md#current-cat-034)). Rope sockets need a new rope domain in `WorkshopConnectionDomain` (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L13`), built by Story 10.2. Their positions are unspecified (see Open questions).

### Sensors and activation

None is required. The legacy counted contacts with approach speed ≥ 0.45 m/s and published a `Bounced` event (`parts/ImpactLeverPart.cs@a6c914e:L42-L44`, `parts/ImpactLeverPart.cs@a6c914e:L81-L91`), but no requirement asks for a lever output. That counter is not carried forward (fact 30).

### Work and energy stores

None. The lever is passive: there is no power and no automatic launch (`parts/catalog/impact_lever.tres@a6c914e:L9`). All energy comes from contacts and gravity.

### Parameters

| Name | Type | Range | Default | Unit | Source |
| --- | --- | --- | --- | --- | --- |
| `beam_mass` | `Kilograms` (f32) | 0.5–20 | 2.0 | kg | `parts/ImpactLeverPart.cs@a6c914e:L46-L53`; `parts/catalog/impact_lever.tres@a6c914e:L12` |
| `initial_angle` | angle (f32), converted to radians at compile | −30 to 30 | 0.0 | degrees | `parts/ImpactLeverPart.cs@a6c914e:L49-L52`, `parts/ImpactLeverPart.cs@a6c914e:L58`; `parts/catalog/impact_lever.tres@a6c914e:L12` |

`initial_angle` rotates the beam about part-local +Z, and the hinge measures it against the authored frame without re-zeroing (`parts/ImpactLeverPart.cs@a6c914e:L78`; `CuriousContraptions.tests/SceneJointDeclarationTests.cs@a6c914e:L186-L202`). Closed parameter identities are an enum, `ImpactLeverParameter { BeamMass, InitialAngle }` (`parts/ImpactLeverPart.cs@a6c914e:L8`). Wire names convert only at the serialization boundary (`CuriousContraptions.tests/ImpactLeverTests.cs@a6c914e:L31-L37`).

### Cosmetic curves and UI bindings

- **Curves.** None. The beam artwork follows the committed hinge pose directly, with no easing (fact 22; [DESIGN.md Impact lever](../../../DESIGN.md#impact-lever)). No `CosmeticCurveDeclaration` (`engine/gpu/WorkshopCosmetic.cs@a6c914e:L16-L17`) is needed.
- **UI.** The legacy pick radius is 2 m (`parts/ImpactLeverPart.cs@a6c914e:L57`). Use the existing move and rotate controls only; add no inspector (DESIGN.md).

### Art

| Item | Value | Source |
| --- | --- | --- |
| Scene | `parts/scenes/impact_lever.tscn`, a root `Node3D` with the part script and no authored children | `parts/scenes/impact_lever.tscn@a6c914e:L1-L4` |
| Beam | cream `#fff8e9`, size 3.6 × 0.24 × 1.1 | `parts/ImpactLeverPart.cs@a6c914e:L73` |
| Upper inset | cyan `#66b8c9`, size 3.38 × 0.014 × 0.82 at y 0.121 | `parts/ImpactLeverPart.cs@a6c914e:L74` |
| Lever-arm marks | four gold `#e8b764` bars, size 0.035 × 0.016 × 0.3, at x = ±0.6 and ±1.2, y 0.122 | `parts/ImpactLeverPart.cs@a6c914e:L75-L77` |
| Pivot post | gold cylinder, radius 0.22, height 1.02, at y −0.59 | `parts/ImpactLeverPart.cs@a6c914e:L61` |
| Pin | gold cylinder, radius 0.18, length 1.25, along Z | `parts/ImpactLeverPart.cs@a6c914e:L62-L63` |
| Stop caps | gold cylinders, radius 0.13, height 0.3, at the stop boxes | `parts/ImpactLeverPart.cs@a6c914e:L68` |
| Foot | navy `#293954` base box | `parts/ImpactLeverPart.cs@a6c914e:L60` |
| Catalogue colour | RGB 0.4, 0.72, 0.79 (`#66b8c9`) | `parts/catalog/impact_lever.tres@a6c914e:L11` |

The palette tokens are cream, cyan inset, gold pivot, marks and stops, and a navy foot, with the navy outline seesaw pictogram ([DESIGN.md Impact lever](../../../DESIGN.md#impact-lever)).

### Catalogue and inventory entry

- **Catalogue.** Id `impact_lever`, title "Impact lever", category "Motion". Description: "A passive hinged beam. A falling load turns one end and can lift a lighter load at the other. Gold marks show the lever arms; fixed stops limit its swing. No power or automatic launch." (`parts/catalog/impact_lever.tres@a6c914e:L6-L9`).
- **Inventory.** Story 10.3 adds a `WorkshopPartKind` member and a `PartInventory` allowance (`engine/gpu/WorkshopInventory.cs@a6c914e:L9-L23`). No level at `a6c914e` places the lever or stocks it in an inventory: `content/puzzles.json` has zero `impact_lever` occurrences at that commit.

## 3. Engine capabilities

The capability families are those in the [CAT-034 map row](../general-engine-element-map.md) and the [catalogue coverage](../../coverage/catalogue-elements.json). They are not repeated here.

**Exists now:**
- **Dynamic box body.** Box rigid body, homogeneous box inertia and box-box, box-sphere and box-plane contact: `engine/gpu/RigidMassProperties.cs@a6c914e:L26-L51` and `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L503` (`collideBoxBox`). The Domino is the nearest dynamic-box precedent (`engine/gpu/WorkshopDomino.cs@a6c914e:L7-L24`).
- **Contact solver.** TGS Soft contact solve at 480 Hz substeps (`CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L208`), with 8 biased and 4 relax iterations (`CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L225-L226`), `makeSoft` (`CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L230-L236`), `prepareContact` (`CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L623`), `solveNormalRow` (`CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L774`) and `sweepManifolds` (`CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L878`).
- **Static fixture.** Multi-box static bodies and scene capacity (33 bodies, 64 colliders): `engine/gpu/PhysicsDeclarations.cs@a6c914e:L164-L168`.
- **Save codec.** Part pose and rotation: `engine/gpu/WorkshopSaveCodec.cs@a6c914e:L6-L10`.

**Missing:**
- **Revolute hinge.** A hinge joint row with a travel interval (end stops) and a connected-collision policy in the worker solver. Story 10.3 builds it, reusing the generic joint rows that Story 6.4 introduces for the prismatic slider.
- **Rope sockets.** Rope sockets on a moving body, with angular moment-arm coupling. Story 10.2 builds them.
- **Lever-to-shutter integration.** Story 13.3 (CAT-007).
- **Bend obstacles.** CAT-049/050 bends; Batch A schedules those stories.
- **Frustum obstacles.** The funnel (CAT-030), also scheduled by Batch A.
- **Tube obstacles.** The pipe (CAT-048, Stories 6.6 and 6.7).

**Element dependencies:**
- **Driver and payload.** Bowling ball (CAT-014) as the driver and Basketball (CAT-001) as the payload; both are delivered.
- **Obstacles.** Wall (CAT-066, delivered) and Bumper (CAT-015, 6.2).
- **Ropes.** Rope anchor, pulley and weight (CAT-058/053/067, 10.2 and 10.4).

## 4. Legacy harvest

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | The lever is a passive fixed-pivot beam whose pose follows finite-inertia contact, never a scripted flip. | `parts/ImpactLeverPart.cs@a6c914e:L10-L11` | carry forward | Matches the requirement's "no canned flip". |
| 2 | The beam's half extents are 1.8 × 0.12 × 0.55. | `parts/ImpactLeverPart.cs@a6c914e:L16` | carry forward | Authored geometry. The magnitudes are carried as f32 metres. |
| 3 | The hinge limit is ±π/6 rad (30°). | `parts/ImpactLeverPart.cs@a6c914e:L17` | carry forward | The end-stop interval. |
| 4 | There is one hinge: A = beam, B = static root, identity frames, connected collision Disabled, direction Both. | `parts/ImpactLeverPart.cs@a6c914e:L20-L27` | carry forward | The joint topology. Re-express it as a typed `engine/gpu` hinge declaration. |
| 5 | The beam body is excluded from static queries. | `parts/ImpactLeverPart.cs@a6c914e:L28-L29` | do not carry forward | A legacy CPU query policy. The current solver owns broadphase. |
| 6 | Beam inertia is m(y²+z²)/3, m(x²+z²)/3 and m(x²+y²)/3 on half extents; the beam starts at rest. | `parts/ImpactLeverPart.cs@a6c914e:L30-L40`; `CuriousContraptions.tests/SceneBodyDynamicsTests.cs@a6c914e:L260-L284` | carry forward | Identical to `RigidMassProperties.Compile` for boxes. Compile it there; do not author it. |
| 7 | Beam material: restitution 0, bounce threshold 0.1, friction 0.3. | `parts/ImpactLeverPart.cs@a6c914e:L41`; `engine/physics/PersistentContactPair.cs@a6c914e:L18-L29` | carry forward | Authored material values. |
| 8 | Legacy material mixing: product of restitutions, maximum of thresholds, geometric-mean friction. | `engine/physics/PersistentContactPair.cs@a6c914e:L31-L36` | do not carry forward | A CPU solver path. The worker owns coefficient mixing. |
| 9 | Parameters are validated as `beam_mass` 0.5–20 and `initial_angle` −30 to 30 degrees, finite, otherwise rejected. | `parts/ImpactLeverPart.cs@a6c914e:L46-L53` | carry forward | Ranges and atomic rejection. |
| 10 | Catalogue defaults are `beam_mass` 2.0 and `initial_angle` 0.0. | `parts/catalog/impact_lever.tres@a6c914e:L12` | carry forward | Defaults. |
| 11 | The initial angle converts from degrees to radians and rotates the beam about +Z. | `parts/ImpactLeverPart.cs@a6c914e:L58`, `parts/ImpactLeverPart.cs@a6c914e:L78` | carry forward | Authoring convention. |
| 12 | The base box is 1.4 × 0.16 × 1 (full size) at y −1.18 on the static root. | `parts/ImpactLeverPart.cs@a6c914e:L60`; `reference/cpu/MachinePart.cs@a6c914e:L352-L356` | carry forward | Fixture geometry. |
| 13 | The stop boxes have half extents 0.13 × 0.15 × 0.3 at (±1.5, −1.004, 0) on the static root, and "meet the underside at the authored angular bounds". | `parts/ImpactLeverPart.cs@a6c914e:L64-L70` | carry forward | Solid gold stops. Their tops (y −0.854) equal the beam underside at 1.5 m arm and 30°. |
| 14 | The pivot post and pin are visual cylinders only. | `parts/ImpactLeverPart.cs@a6c914e:L61-L63` | do not carry forward | The requirement asks for a solid fulcrum. See Open questions. |
| 15 | Beam artwork, inset, marks and palette are as tabulated in §2 Art. | `parts/ImpactLeverPart.cs@a6c914e:L71-L78` | carry forward | Approved palette. |
| 16 | The closed parameter set is an enum; undefined values reject at the name boundary. | `parts/ImpactLeverPart.cs@a6c914e:L8`; `CuriousContraptions.tests/ImpactLeverTests.cs@a6c914e:L31-L37` | carry forward | Enum end-to-end. The name strings exist only at serialization. |
| 17 | Acceptance: with the lever at y 3 and balls at x = ±1, y 3.46, the angle stays ≤ 0.01 rad and the ball heights stay within 0.01 of each other (3.45–3.47) for 1,200 ticks (10 s at 120 Hz); the final hinge speed is ≤ 0.02 rad/s. | `CuriousContraptions.tests/ImpactLeverTests.cs@a6c914e:L39-L61` | carry forward | The equal-load ten-second balance. Re-qualify the tolerances at f32 game grade. |
| 18 | Acceptance: at orientations (0,0,0), (15,30,0), (90,0,0) and (0,0,90), with zero gravity, a bowling ball at local (−1.2, 1.3, 0) moving 5 m/s along local −Y turns the beam more than 0.05 rad. Total kinetic energy never exceeds 1.01 × the initial value. | `CuriousContraptions.tests/ImpactLeverTests.cs@a6c914e:L63-L90` | carry forward | The four 3D orientations and no energy gain. |
| 19 | Acceptance: with a bowling ball dropped from (−1.2, 6, 0) and a ball payload at (1.2, 3.46, 0), the payload peaks above y 4 with upward speed above 2 m/s. A depth miss (z = 2) or a pivot hit (x = 0) keeps the payload below y 3.6. The angle stays within ±π/6, and energy stays ≤ 1.01 × initial over 480 ticks. | `CuriousContraptions.tests/ImpactLeverTests.cs@a6c914e:L92-L126` | carry forward | Positive and negative pair (aligned, missed and pivot). |
| 20 | Acceptance: Reset restores the saved construction exactly and the beam's presented transform equals its authored transform; a replay after Reset reproduces the state signature. | `CuriousContraptions.tests/ImpactLeverTests.cs@a6c914e:L127-L134` | carry forward | Run/Reset restoration. |
| 21 | Acceptance: a beam overlapping a wall, sphere fixture, sloping shell, another beam, a bend shell or a free ball at Run start is rejected before the run, leaving the construction unchanged. | `CuriousContraptions.tests/ImpactLeverObstructionTests.cs@a6c914e:L20-L35`; `CuriousContraptions.tests/ImpactLeverSphereObstructionTests.cs@a6c914e:L41-L47`; `CuriousContraptions.tests/ImpactLeverFrustumObstructionTests.cs@a6c914e:L53-L69`; `CuriousContraptions.tests/HingeConstructionTests.cs@a6c914e:L17-L72` | carry forward | Overlap admission boundary. |
| 22 | Acceptance: the presented beam pose equals the physics pose within 1e-6 after contact drives it. | `CuriousContraptions.tests/RuntimePhysicsCutoverTests.cs@a6c914e:L89-L110`; `CuriousContraptions.tests/SceneBodyGeometryTests.cs@a6c914e:L67-L98` | carry forward | Artwork follows committed motion. |
| 23 | Acceptance: a wall of 0.6 × 0.4 × 1.4 at (1.5, 3.6, 0) caps the beam angle at 0.15–0.17 rad under a dropped bowling ball, the beam cannot be driven through it by repeated pressure, and Reset clears the contact. | `CuriousContraptions.tests/ImpactLeverObstructionTests.cs@a6c914e:L37-L69` | carry forward | Fixed obstacle. |
| 24 | Acceptance: after a 3 rad/s push, a wall or the deck (lever at y 0.2) stops the beam at 0.05–0.4 rad with zero speed and energy. A further 5 rad/s push stays stopped, and a reverse push moves the beam away. A depth miss reaches the +π/6 stop. | `CuriousContraptions.tests/ImpactLeverObstructionTests.cs@a6c914e:L71-L108` | carry forward | Workbench obstacle and miss control. |
| 25 | Acceptance: a sphere fixture (bumper with strength 0, radius 0.65) at (±1.5, 4, 0) stops the beam at acos(0.77/√3.25) − atan(1.5) rad for pushes of ±3 and ±20,000 rad/s, with no tunnelling. Removing the sphere lets the beam reach the stop. | `CuriousContraptions.tests/ImpactLeverSphereObstructionTests.cs@a6c914e:L23-L81` | carry forward | Analytic sphere contact angle and high-speed boundary. |
| 26 | Acceptance: sphere contact follows rotated lever orientations; the empty corners of the sphere's bounding box do not block; non-uniform or negative fixture scale rejects at Run. | `CuriousContraptions.tests/ImpactLeverSphereObstructionTests.cs@a6c914e:L83-L140` | carry forward | Orientation, broadphase-corner and scale boundaries. |
| 27 | Acceptance: a falling bowling ball cannot force the beam through a bumper (maximum 0.14–0.16 rad, final speed ≈ 0), and the bumper registers the beam as an impact participant. | `CuriousContraptions.tests/ImpactLeverSphereObstructionTests.cs@a6c914e:L142-L169` | carry forward | A moving beam is a real contact body. |
| 28 | Acceptance: an inverted funnel's sloping shell stops the beam at 0.15–0.45 rad, and its bore stops it in the same band when the beam enters the bore. A depth miss reaches the stop. | `CuriousContraptions.tests/ImpactLeverFrustumObstructionTests.cs@a6c914e:L20-L51`, `CuriousContraptions.tests/ImpactLeverFrustumObstructionTests.cs@a6c914e:L71-L104` | carry forward | Frustum and open-bore obstacles (after CAT-030). |
| 29 | Acceptance: a pipe of 1 × 1.3 × 1.3 stops the beam at 0.1–0.25 rad on its shell. With the beam inside the bore it stops at 0.12–0.14 rad against the bore wall. A depth miss reaches the stop. | `CuriousContraptions.tests/ImpactLeverTubeObstructionTests.cs@a6c914e:L20-L91` | carry forward | Tube and open-bore obstacles (after CAT-048). |
| 30 | The lever counts contacts of ≥ 0.45 m/s in a per-part callback and adds `Bounced` events, rolled back with a failed tick. | `parts/ImpactLeverPart.cs@a6c914e:L81-L91`; `CuriousContraptions.tests/ImpactRuntimeCheckpointTests.cs@a6c914e:L32-L105` | do not carry forward | A per-element update loop with no requirement for a lever output. |
| 31 | Acceptance: hinge error stays ≤ 1e-7; an impulse at a 1.5 m arm drives the beam to the −π/6 stop with zero angular velocity; replay is exact; this also holds for a rotated part. | `CuriousContraptions.tests/SceneJointDeclarationTests.cs@a6c914e:L97-L142` | carry forward | Stops are inelastic and the pivot holds. The 1e-7 tolerance is proof-grade; re-qualify it at game grade. |
| 32 | Acceptance: an aimed ball contact drives the hinge below −0.1 rad, while a miss leaves the angle unchanged. | `CuriousContraptions.tests/SceneJointDeclarationTests.cs@a6c914e:L144-L184` | carry forward | Positive and negative pair. |
| 33 | An axial (Z) impulse can neither move nor spin the constrained beam. | `CuriousContraptions.tests/SceneJointDeclarationTests.cs@a6c914e:L204-L222` | carry forward | The hinge locks five degrees of freedom. |
| 34 | Duplicate, absent or unknown body and joint identities reject without a fallback. | `CuriousContraptions.tests/SceneJointDeclarationTests.cs@a6c914e:L224-L246` | carry forward | Typed identity admission. |
| 35 | A hinge range must be finite, ordered and strictly inside (−π, π); range rows are unilateral and inactive in the interior; a stop arrests high-speed travel and releases inward. | `engine/physics/PhysicsJoint.cs@a6c914e:L102-L115`, `engine/physics/PhysicsJoint.cs@a6c914e:L130-L141`; `CuriousContraptions.tests/JointRangeTests.cs@a6c914e:L48-L72`, `CuriousContraptions.tests/JointRangeTests.cs@a6c914e:L176-L187`; `CuriousContraptions.tests/JointBoundaryTests.cs@a6c914e:L14-L24` | carry forward | Generic hinge-limit law for Story 10.3. |
| 36 | Joint stops use CPU analytic sweeps and a position projector. | `engine/physics/PhysicsJoint.cs@a6c914e:L209-L219` | do not carry forward | A CPU solver path. Use TGS Soft joint rows. |
| 37 | Acceptance: a gizmo-edited basis survives save, load, run and Reset over five cycles with an identical physics replay. | `CuriousContraptions.tests/AuthoredOrientationSceneTests.cs@a6c914e:L33-L77` | carry forward | Save/Load and rotation. |
| 38 | Rope paths measured socket positions from the part's root pose. | `engine/RopeNetwork.cs@a6c914e:L40-L49` | do not carry forward | A socket on the moving beam must follow the beam body, not the static root. |

### Files harvested

- `parts/ImpactLeverPart.cs`
- `parts/catalog/impact_lever.tres`
- `parts/scenes/impact_lever.tscn`
- `engine/physics/PhysicsJoint.cs`
- `engine/physics/JointEquations.cs`
- `engine/physics/PersistentContactPair.cs`
- `engine/RopeNetwork.cs`
- `engine/SceneJointDeclaration.cs` (checked; generic frame-joint binding; no lever values)
- `reference/cpu/MachinePart.cs`
- `CuriousContraptions.tests/ImpactLeverTests.cs`
- `CuriousContraptions.tests/ImpactLeverObstructionTests.cs`
- `CuriousContraptions.tests/ImpactLeverSphereObstructionTests.cs`
- `CuriousContraptions.tests/ImpactLeverFrustumObstructionTests.cs`
- `CuriousContraptions.tests/ImpactLeverTubeObstructionTests.cs`
- `CuriousContraptions.tests/LeverFixture.cs` (checked; test harness only; no element knowledge)
- `CuriousContraptions.tests/HingeConstructionTests.cs`
- `CuriousContraptions.tests/ImpactRuntimeCheckpointTests.cs`
- `CuriousContraptions.tests/SceneJointDeclarationTests.cs`
- `CuriousContraptions.tests/SceneBodyDynamicsTests.cs`
- `CuriousContraptions.tests/SceneBodyGeometryTests.cs`
- `CuriousContraptions.tests/RuntimePhysicsCutoverTests.cs`
- `CuriousContraptions.tests/AuthoredOrientationSceneTests.cs`
- `CuriousContraptions.tests/JointRangeTests.cs`
- `CuriousContraptions.tests/JointBoundaryTests.cs`
- `CuriousContraptions.tests/BodyLocalBoxTests.cs` (checked; generic body-slot box offsets; no lever values)
- `CuriousContraptions.tests/JointConstraintTests.cs`, `PhysicsJointTests.cs`, `PhysicsJointUpdateTests.cs`, `PhysicsImpactJointTests.cs`, `JointDiagnosticTests.cs`, `SceneJointIdentityTests.cs` (checked; generic joint or rope laws; no lever values)
- `engine/physics/JointConstraints.cs`, `engine/physics/PhysicsJointChange.cs`, `engine/MachineData.cs` (checked; no element knowledge)
- `content/puzzles.json` (checked; no `impact_lever` placement or inventory)
- `reference/P0-022-before/docs/coverage/engine/*.json`, `tools/P0-007-actual-path-probe/CuriousContraptions.csproj`, `tools/P0-007-actual-path-probe/inputs.json` (checked; capability lists and hashes; no element knowledge)

## 5. Acceptance outline

The requirement row [CAT-034](../requirements.md#current-cat-034), its retained behaviour [sequence-task-307](../requirements.md#sequence-task-307) and Story 10.3 govern. This outline does not restate them.

- **Chrome UI construction.** In Workshop free play, place an Impact lever from the toolbox, then a Bowling ball above one arm (local x −1.2) and a Basketball resting on the other arm (local x +1.2). Do this through the actual toolbox, placement and rotate controls, never setters or imports. Legacy placement recipes survive in the current docs `docs/impact-lever-ui-recipes.json`, `docs/impact-lever-obstruction-recipes.json`, `docs/impact-lever-sphere-recipes.json`, `docs/impact-lever-frustum-recipes.json`, `docs/impact-lever-tube-recipes.json` and `docs/impact-lever-stop-correction.json`. They are outside the Epic 7 deletion scope.
- **Positive.** The aligned drop turns the beam to its stop and launches the payload (facts 19 and 32). Equal loads balance for 10 s (fact 17).
- **Negative and controls.** A depth miss and a pivot hit leave the payload low (fact 19). Insufficient energy gives no launch. Overlap at Run is rejected (fact 21).
- **Boundaries.** The ±30° stops (facts 13, 31 and 35), the parameter extremes 0.5 and 20 kg and ±30°, rejection outside those ranges (fact 9), and high-speed obstacle contact (fact 25).
- **Orientations.** Four 3D orientations (fact 18).
- **Obstacles.** Each obstacle family from facts 23–29, when its element exists.
- **Run/Reset.** Restore the exact pose, angle and zero velocity (fact 20).
- **Save/Load.** The rotation, `beam_mass` and `initial_angle` round-trip (fact 37).
- **Integrations.** Ropes from end sockets (10.2), the lever-to-shutter chain (13.3), and resting or sliding distributed loads and beam-on-beam contact, per the requirement row.

## 6. Open questions

1. **Rope sockets.** The requirement requires end rope sockets, but the legacy declared none. Their local positions, count and port identities are unspecified — owner decision.
2. **Solid fulcrum.** The requirement asks for one, but the legacy pivot post (radius 0.22, height 1.02) and pin are art only, and the current engine has no cylinder collider; a cylinder collider is scheduled in Story 10.1 (ENGINE-CYLINDER), scheduled before the first shaft wheel by the owner on 9 Oct 2026. The fulcrum collider shape and size are unspecified — owner decision.
3. **Authoritative stop.** The legacy declares both a hinge travel range (±π/6) and solid stop boxes that meet at the same angle. Which one is authoritative, or whether both are retained, is unspecified — owner decision.
4. **Stop restitution.** The legacy stops are perfectly inelastic (fact 31). Story 10.3's "strikes its end stop and launches the payload" needs no stop restitution, but a non-zero value is unspecified — owner decision.
5. **Hinge friction and damping.** The legacy hinge is frictionless, while the requirement lists friction as a closure criterion. The hinge friction or damping law and its value are unspecified — owner decision.
6. **Parameter controls.** There is no current UI control for `beam_mass` or `initial_angle`. Choose between a selectable authored qualification fixture and a new control — owner decision.
7. **Campaign placement.** No level uses the lever, so its Epic 15 chapter placement is unspecified.
8. **Toolbox icon.** No tracked lever icon asset was found under its name. The source of the "navy outline seesaw pictogram" needs confirming.
