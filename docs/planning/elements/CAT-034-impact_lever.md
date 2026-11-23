# CAT-034 · impact_lever declaration readiness spec

This is the CAT-034 spec for the Impact lever. Sections 1 to 3 and 5 to 9 describe the delivered package, checked on 22 November 2026 against checkout `8d1d68aa`; published main is now `25df3cd5`, and its two later commits touch none of this package's files. Section 4 is the legacy harvest at `a6c914e`, kept verbatim as evidence; it is not a description of the current part. Browser qualification is pending until the post-catalogue phase.

## 1. Identity

| Field | Value |
| --- | --- |
| CAT ID | CAT-034 |
| Kind | `impact_lever`, catalogue title "Impact Lever" |
| Requirement anchor | [CAT-034](../requirements.md#current-cat-034); retained behaviour [sequence-task-307 / todo-409](../requirements.md#sequence-task-307) |
| Mapped identities | None. No EL, TH, RAD or GAP identity in [named-elements.md](../invest/named-elements.md) names an impact lever, teeter-totter or fulcrum beam. |
| Roadmap story | 10.3 Balanced Impact Lever and Pivot Fulcrum (Epic 10), with 10.3a and 10.3b |
| Status | **Delivered.** Package id `impact_lever`, directory `elements/ImpactLever`. Slice records: [10.3](../../../_bmad-output/implementation-artifacts/spec-10-3-impact-lever.md) (commit `441af75a`), [10.3a locators and revolute wire](../../../_bmad-output/implementation-artifacts/spec-10-3a-lever-locators-revolute-wire.md), [10.3b pivot hold](../../../_bmad-output/implementation-artifacts/spec-10-3b-lever-pivot-hold.md) (`3ce30ba3`); all `done`. Browser case pending. Every residual slice (section 8) is blocked on the engine programme (owner decision 3a, 22 November 2026); the stories that own this part's gaps are `ENGINE-CAPABILITY-INSTANCES` (rope-port admission that enumerates role names) and `ENGINE-ISLANDS-SLEEP` (the resting beam). |
| Disposition | Delivered catalogue part; no mapped named identity. EL-096 Tipsy platform, EL-223 Passive impact scoop and EL-224 Counterweighted scoop catapult plan to reuse its hinge-with-stops shape (delivery-batches 1b) but are separate elements. |

## 2. Declaration

### As delivered (checked 22 November 2026)

This table is the current declaration (`elements/ImpactLever/ImpactLeverElementDeclaration.cs`).

| Item | Delivered value | Source |
| --- | --- | --- |
| Interfaces | `IHingeElement`, `ISceneGeometryElement` | `:12` |
| Bodies | 2 (1 moving): static fixture (root) and dynamic beam `Visual/Beam` | `:37`, `:67`–`:71` |
| Fixture colliders | `col_base` box 1.4 × 0.16 × 1 m; `col_fulcrum` solid cylinder r 0.22 m, height 1.02 m; `col_stop_left`, `col_stop_right` boxes 0.26 × 0.3 × 0.6 m | `impact_lever.tscn:43`–`:51`, `:58`–`:71` |
| Beam collider | `col_beam` box 3.6 × 0.24 × 1.1 m centred on the pivot | `impact_lever.tscn:53`–`:54`, `:102` |
| Beam mass and inertia | mass = `beam_mass`; box moments m(b² + c²)/3 on half-extents (1.8, 0.12, 0.55), declared as polynomials of `beam_mass`; gravity (0, −9.81, 0) | `:108`–`:117` |
| Materials | fixture: restitution 1, friction 0.3, rolling 0, threshold 0.1 m/s; beam: restitution 0, friction 0.3, rolling 0, threshold 0.1 m/s | `:69`–`:70` |
| Hinge | revolute about local +Z at the origin, `RevoluteTravel.Limited` to ±π/6 rad, no spring or damping, friction torque 2 N·m, initial angle from `initial_angle`, connected bodies do not collide, no drive | `:91`–`:119` |
| Parameters | `beam_mass` Real, kg, default 2, 0.5–20; `initial_angle` Real, degrees, default 0, −30 to 30. Both take the descriptor defaults: editor policy Reject, Build only (`CuriousContraptions.Abstractions/ParameterDescriptor.cs:60`–`:61`) | `:25`–`:26` |
| Ports | `PortId.TieA` at (−1.8, 0, 0), normal −X, and `PortId.TieB` at (1.8, 0, 0), normal +X; both domain Rope, Bidirectional, attachment `Load`, owned by the beam body (package body 1) | `:39`–`:63` |
| Stores, events, payload | none | `:73`, `:79` |
| Presentation | body pose: `Visual/Beam` follows package body 1; no curve; pick radius 2 m | `:29`–`:35` |
| Palette | category Motion, order 34, colour (0.4, 0.72, 0.79), display name "Impact Lever" | `:14`–`:27` |

**Art and solidity.** Not yet in the visible-solid table. From [`spec-engine-art-solidity.md`](../../../_bmad-output/implementation-artifacts/spec-engine-art-solidity.md) (row `impact_lever`): base, fulcrum, both stops and `Beam/Visual_Beam` are solid and each has its collider. **Defect: `Beam/Visual_Inset`** (box 3.38 × 0.014 × 0.82 m) **and `Beam/Visual_Mark_0`–`3`** (boxes 0.035 × 0.016 × 0.3 m) are raised 8–10 mm above the beam's top face with no collider (`impact_lever.tscn:35`–`:41`, `:108`–`:124`). Owned by [visible-solid remainder part A](../../../_bmad-output/implementation-artifacts/spec-elements-visible-solid-remainder.md) (row A7a), status draft: each becomes flat printing no more than 0.5 mm off the face, same position, extent and colour, with no raised volume kept. The harvest art table's separate "Pin" (gold cylinder r 0.18 along Z) is not drawn in the delivered scene.

### Notes on the declaration

- **Passive.** No power, no drive, no automatic launch, no event: all energy comes from contacts, gravity and ropes.
- **Beam.** Its inertia is the homogeneous box law on the half-extents, declared as polynomials of `beam_mass`. `initial_angle` turns the beam about local +Z; the hinge measures it against the authored frame without re-zeroing.
- **Stops.** The hinge limit at ±π/6 arrests the beam; the two stop boxes are drawn where the beam meets that limit and stop other bodies. Connected-body collision is off, so the two never fight.
- **Friction.** The hinge's 2 N·m Coulomb torque is a fixed value with no inspector control. Tipping bound: the lightest Weight (0.25 kg) at a 1.8 m tie port gives 4.4 N·m and must still tip the beam. Holding bound: the equal 4 kg Weight pair's inverted-pendulum stiffness is about 31 N·m/rad, so 2 N·m holds any seed up to about 0.065 rad. A launch loses at most τ·π/3 ≈ 2.1 J of about 90 J.
- **Art.** Cream `#fff8e9` beam, cyan `#66b8c9` inset, four gold `#e8b764` lever-arm marks at x = ±0.6 and ±1.2, gold fulcrum and stops, navy `#293954` base; DESIGN.md "Impact lever". The beam art follows the committed hinge pose directly, with no easing. Icon: `elements/ImpactLever/icon.svg`, a navy outline seesaw.
- **Catalogue.** Category Motion, description "A passive hinged beam flings payloads upward when struck by falling weights." No level places the lever.

## 3. Engine capabilities

The capability families are those in the [CAT-034 map row](../general-engine-element-map.md) and the [catalogue coverage](../../coverage/catalogue-elements.json). They are not repeated here.

**Capability probe (22 November 2026).** Tests were found by name and line; none was run. Counts: 6 present, 2 present-untested, 0 narrower, 0 missing. The lever declares one generic limited hinge and no engine code names the lever, but three rope-port admission sites enumerate the role names only it declares (`TieA`, `TieB`): `engine/gpu/WorkshopConnections.cs:163`–`:164`, `engine/gpu/ElementPortCatalogue.cs:182`, `CuriousContraptions.Bundle/BundleValidator.cs:543`, `:546`. That is a required engine refactor (section 9, point 2).

| Capability | Status | Symbol | Exercising test |
| --- | --- | --- | --- |
| Revolute hinge with a travel interval and connected-collision policy | present | `IHingeElement`, `RevoluteHingeDescriptor`, `RevoluteTravel` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:20`; `CuriousContraptions.Abstractions/MechanicalContractValues.cs:149`, `:129`) | `ImpactLeverTests.AngularLimitsBoundary_StopsArrestTravelWithoutPenetration` (`CuriousContraptions.tests/ImpactLeverTests.cs:380`), `PhysicsSceneDeclaration_RefusesInvalidRevoluteDeclarations` (`:707`) |
| Hinge Coulomb friction torque | present | `RevoluteHingeDescriptor.FrictionTorque` (`elements/ImpactLever/ImpactLeverElementDeclaration.cs:105`) | `ImpactLeverTests.ASmallLoadIsHeldBelowTheFrictionTorqueAndTipsTheBeamAboveIt` (`:275`), `PivotFrictionHoldsOrYieldsPerLeverInEverySimdLane` (`:617`); generic `RevoluteHingeFrictionTests` |
| Initial hinge angle from a parameter | present | `MechanicalValue.FromParameter` (`:106`) | `ImpactLeverTests.RunResetRestoration_RestoresAuthoredStateExactly` (`:484`), `OutOfRangeInitialAngle_IsRefused` (`:476`), `SaveLoadPersistenceRoundtrip_RestoresAuthoredState` (`:675`) |
| Dynamic box with parameter-declared mass and moments | present, untested | `MassFrameDeclaration` (`CuriousContraptions.Abstractions/MassFrameValues.cs:38`) | the beam turns in `AsymmetricLoad_TurnsTheBeamToItsStopAboutTheAuthoredPivot` (`:165`) and range refusal is tested (`:464`), but no test varies `beam_mass` and reads a different motion. Residual row L-R12 |
| Solid cylinder and box colliders on a static body | present, untested | `ColliderShapeKind.Cylinder` (`CuriousContraptions.Abstractions/PhysicsDeclarationValues.cs:12`) | no test brings a body into contact with the fulcrum, the stops or the base (the pivot-hit control lands on the beam above the fulcrum). Part A adds the table row; residual row L-R13 adds contact |
| Rope ports owned by a moving body (angular moment-arm coupling) | present | `PortDescriptor` with a `PackageBodyIndex` owner (`CuriousContraptions.Abstractions/PortDescriptor.cs:55`) | `ImpactLeverTests.RopeLiftingIntegration_FallingWeightLiftsArm` (`:414`), `RopeLiftingIntegration_TieBLiftsArm` (`:516`) |
| Several hinges solved together | present | generic revolute rows | `ImpactLeverTests.MultiJointBatching_SolvesConcurrentlyAcrossSimd` (`:562`) |
| Run/Reset, save and load, parameter refusal at decode | present | generic lifecycle | `ImpactLeverTests.RunResetRestoration_RestoresAuthoredStateExactly` (`:484`), `SaveDecoding_RefusesOutOfRangeValues` (`:690`) |

**Collaborating parts.** Bowling ball as driver, Basketball and Tennis ball as payloads; Wall, Bumper, Pipe, bends and Funnel as obstacles; Rope anchor, Pulley and Weight for ropes. All are delivered. The Beam shutter (CAT-007) is not.

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
| 17 | Acceptance: with the lever at y 3 and balls at x = ±1, y 3.46, the angle stays ≤ 0.01 rad and the ball heights stay within 0.01 of each other (3.45–3.47) for 1,200 ticks (10 s at 120 Hz); the final hinge speed is ≤ 0.02 rad/s. | `CuriousContraptions.tests/ImpactLeverTests.cs@a6c914e:L39-L61` | carry forward | The equal-load ten-second balance, asserted at every tick (`EqualBallLoads_StayBalancedForTenSeconds`). Since Story 10.3b the hinge's 2 N·m friction (Open question 5) holds equal loads indefinitely. Story 10.3's 4 kg Weight pair stays within 0.01 rad and 0.02 rad/s for a whole 30 s run (the default Run length, owner decision 13; the maximum is 300 s, owner decision 17), with no creep and no walking (`SymmetricalLoadBalance_RemainsBalancedForAWholeRun`). A 0.1 N·m Basketball imbalance (x = −1.0 and +1.01 m) is held for 10 s (`UnequalBallLoads_AreHeldByThePivotFriction`), so the balance comes from friction, not mirror symmetry. |
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
- **Positive.** The aligned drop turns the beam to its stop and launches the payload (facts 19 and 32). Equal Basketball loads balance for 10 s (fact 17). A heavy equal Weight pair stays level indefinitely, through a whole run. Hinge friction holds a resting load below 2 N·m and yields above it (Open question 5).
- **Negative and controls.** A depth miss and a pivot hit leave the payload low (fact 19). Insufficient energy gives no launch. Overlap at Run is rejected (fact 21).
- **Boundaries.** The ±30° stops (facts 13, 31 and 35), the parameter extremes 0.5 and 20 kg and ±30°, rejection outside those ranges (fact 9), and high-speed obstacle contact (fact 25).
- **Orientations.** Four 3D orientations (fact 18).
- **Obstacles.** Each obstacle family from facts 23–29, when its element exists.
- **Run/Reset.** Restore the exact pose, angle and zero velocity (fact 20).
- **Save/Load.** The rotation, `beam_mass` and `initial_angle` round-trip (fact 37).
- **Integrations.** Ropes from end sockets (10.2), the lever-to-shutter chain (13.3), and resting or sliding distributed loads and beam-on-beam contact, per the requirement row.

**Native tests that exist per requirement criterion (22 November 2026; none was run for this review).** All in `CuriousContraptions.tests/ImpactLeverTests.cs`. Aligned drop launches the payload: `AlignedBowlingBallImpact_FlingsPayloadUpward` (`:73`). Mass × arm: `AsymmetricLoad_TurnsTheBeamToItsStopAboutTheAuthoredPivot` (`:165`). Equal loads for ten seconds and for a whole run: `EqualBallLoads_StayBalancedForTenSeconds` (`:309`), `SymmetricalLoadBalance_RemainsBalancedForAWholeRun` (`:216`). Friction: `:247`, `:275`. Pivot and miss controls: `:358`, `:336`. Stops: `:380`. Ropes from both end sockets: `:414`, `:516`. Parameter extremes and refusal: `:464`, `:476`, `:690`. Reset: `:484`. Save and load: `:675`.

**Residual acceptance rows (requirement criteria with no test found).** Tolerances are chosen from the beam's 30° travel and its 3.6 m length, not from observed output.

| Row | Given / When / Then | Planned test | Control | Boundary | Tolerance and stall guard |
| --- | --- | --- | --- | --- | --- |
| L-R1 insufficient energy | Given a Tennis ball (0.35 kg) dropped 0.5 m onto one arm with a Basketball on the other, When Run, Then the payload's height never exceeds its start by more than 5 cm | `ImpactLeverTests.ALightSlowImpactDoesNotLaunchThePayload` | the Bowling ball drop of `:73` launches it | drop heights 0.5 m and 4 m | the 5 cm bound (a visible hop) over a 4 s window |
| L-R2 four 3D orientations | Given the lever at the four harvest rotations (0, 0, 0), (15, 30, 0), (90, 0, 0) and (0, 0, 90) degrees with the aligned drop moved with it, When Run, Then at each rotation where gravity still loads the arm the payload is launched along the beam's local path, and at (90, 0, 0), where the hinge axis is vertical, a ball rolled into one arm turns the beam to its stop | `ImpactLeverTests.TheLeverWorksInEachOfFourOrientations` (theory) | a drop left at the unrotated position misses | the four rotations | apex height within 10% of the (0, 0, 0) case where a launch applies; beam within 2° of its stop otherwise; 4 s stall guard |
| L-R3 obstacles | Given, in turn, the workbench surface (the lever placed so an arm end meets the bench before its stop), a Wall, a Bumper, a Pipe, a 90° bend, a Funnel, and a moving obstacle (a Bowling ball rolling through the arm's sweep), When a Weight tips the beam, Then the beam stops on the obstacle without passing through, and an open bore or rim does not block a clear arm | `ImpactLeverTests.TheBeamStopsOnEachObstacleFamilyAndClearsOpenBores` (theory over the seven cases) | the same set-up without the obstacle reaches the stop | obstacle touching at rest; obstacle 1 cm clear | no penetration deeper than 1 cm (the contact slop a player can see); beam angle short of the stop by at least 2°; 3 s stall guard |
| L-R4 beam on beam | Given two levers with one arm resting on the other's, When a Weight loads the upper, Then both turn and neither interpenetrates | `ImpactLeverTests.OneBeamDrivesAnother` | the lower lever alone stays level | arms overlapping by 5 cm and by 50 cm | as L-R3 |
| L-R5 distributed loads | Given a Domino lying across one arm, When the beam tips, Then it slides toward the low end and stays on or leaves the beam without energy gain | `ImpactLeverTests.ALyingDominoSlidesDownATippedBeam` | at 0° it stays put | friction holding angle: the pair's friction is √(0.3 × 0.6) ≈ 0.42 (beam 0.3, Domino 0.6, `elements/Domino/DominoElementDeclaration.cs:36`), so it holds to atan(0.42) ≈ 23°; the beam's 30° exceeds that, so it must slide. The Domino's centre of mass is at (0, −0.05, 0) m (approved default, owner decision, 22 November 2026); lying across the arm that offset does not change the slide criterion | total energy never above the initial value by more than 1%; 6 s window |
| L-R6 Bowling ball against Basketball loading (closes the CAT-014 entry in `deferred-work.md`) | Given a 2 kg Weight resting at x = +1.2 m, When a Bowling ball, then a Basketball, is set on x = −1.2 m, Then the Bowling ball carries the beam to the opposite stop and the Basketball leaves it on the counter-load's stop. Calculation: the counter-load gives 2 × 9.81 × 1.2 = 23.5 N·m; the Bowling ball 4 × 9.81 × 1.2 = 47.1 N·m, a net 23.5 N·m against 2 N·m of friction, so the beam goes over; the Basketball 1 × 9.81 × 1.2 = 11.8 N·m, 11.8 N·m short, so it stays. On an empty beam the Basketball's 11.8 N·m alone would beat the 2 N·m friction, which is why the counter-load is needed | `ImpactLeverTests.ABowlingBallOutweighsACounterLoadThatABasketballCannot` | the Basketball case | none | beam within 2° of the named stop; 3 s |
| L-R7 slack and taut shutter loads | waits for CAT-007 Beam shutter | in the Beam shutter slice | — | — | — |
| L-R8 flush markings | row A7a of visible-solid remainder part A | owned by that contract | — | — | — |
| L-R9 overlap refused | Given a lever whose beam overlaps another part at Run, When Run is requested, Then it is refused with the typed reason `InitialOverlap` and nothing starts | `ImpactLeverTests.ABeamOverlappingAnotherPartAtRunIsRefused` | the same parts 1 cm clear run | none | exact typed refusal |
| L-R10 art follows the pose | Given the beam at rest, at mid travel and at a stop, and after Reset, When the presentation is read, Then the `Visual/Beam` node's rotation equals the committed hinge angle | `ImpactLeverArtTests.TheBeamArtFollowsTheCommittedBeamPoseAndResets` | none | both stops | 0.1° (3 mm at the beam tip) |
| L-R11 wrong-kind link | the shared row `RopeTests.ARopePortRefusesANonRopeLinkAndANonRopePortRefusesARopeLink` ([CAT-067](CAT-067-weight.md), section 5); `BundlePortsTests.cs:262`–`:284` has no Rope case | shared | — | — | — |
| L-R12 beam mass matters | Given two levers with `beam_mass` 0.5 and 20 kg and the same 1 kg load set on one arm, When Run, Then the heavy beam reaches its stop later than the light one | `ImpactLeverTests.AHeavierBeamTurnsMoreSlowlyUnderTheSameLoad` | equal masses arrive together | the two range ends | order only, with arrival times at least 20% apart (inertia differs fortyfold); 6 s |
| L-R13 fixture is solid | Given a Tennis ball rolled along the bench into the base, and one dropped onto a stop box beside the beam, Then each is stopped at the drawn surface | `ImpactLever` cases in the drop theory of `VisibleSolidColliderTests`, with part A's table row | the same ball with the lever removed passes | none | the theory's own bound |

## 6. Open questions

1. **Rope sockets.** `TieA` and `TieB` at the beam ends (±1.8, 0, 0), owned by the beam body, attachment `Load`.
2. **Solid fulcrum — answered.** Owner decision (e), 18 Oct 2026: the fulcrum is a true cylinder. Story 10.1 (ENGINE-CYLINDER) delivered the engine's solid `Cylinder` collider (declared radius, width and mass; exact inertia; closed-form contacts against plane, sphere, box and cylinder; extracted from a `col_cylinder` proxy). The legacy pivot post (radius 0.22, height 1.02) and pin were art only; Story 10.3 declares the fulcrum's cylinder proxy and size in the Impact Lever package.
3. **Authoritative stop.** Both are kept and they agree: the hinge is `Limited` to ±π/6 and the stop boxes are solid; connected-body collision is off, so the hinge limit arrests the beam and the boxes stop other bodies. One rule per behaviour, so this is not a second path.
4. **Stop restitution.** None: the hinge declares no stop restitution and the beam's restitution is 0 (D19; the launch needs none).
5. **Hinge friction — answered (Story 10.3b; the owner delegated the value on 1 November 2026).** The legacy hinge was frictionless, while the requirement lists friction as a closure criterion. Every revolute hinge now declares a Coulomb friction torque τ (N·m, finite and at least 0) as package data. The generic law is a velocity-level friction row on the hinge axis: its per-substep accumulated impulse is clamped to ±τ·h, warm-started and solved with the other joint rows. It holds the beam while the net torque is below τ and resists with τ once the beam turns. τ = 0 is frictionless through the same clamp. The Impact Lever declares τ = 2 N·m, a fixed value with no inspector control:
   - **Tipping bound:** the lightest catalogue body, a 0.25 kg Weight, at the 1.8 m tie port gives 4.4 N·m. It must still tip the lever, so τ is at most half of that.
   - **Holding bound:** the equal 4 kg Weight pair's inverted-pendulum stiffness is about 31 N·m/rad, so τ = 2 N·m holds any seed up to about 0.065 rad. The beam is authored level, so the imbalance it must hold is near zero.
   - **Unaffected outcomes:** rope lifts (about 70 N·m) are unaffected. The 47 N·m single-Weight asymmetry reaches its stop about 4% slower. A launch loses at most τ·π/3 ≈ 2.1 J of about 90 J.

   Hinge damping is not added; load-proportional pivot friction is a possible later refinement.
6. **Parameter controls.** The generic parameter editor edits both in Build (D11). Editor policy is Reject, unlike the Weight's Clamp; kept as delivered, since an out-of-range typed value is refused visibly either way.
7. **Campaign placement.** No authored level uses the lever yet. The plan teaches it at level 9 with the Placeable pivot and Linkage connector ([campaign coverage section 6](readiness/campaign-coverage.md#6-per-level-allocation-and-progression-check-owner-decisions-24e-and-24f)); Epic 15 authors that level; no engine story is needed.
8. **Toolbox icon.** Delivered as `elements/ImpactLever/icon.svg`.
9. **Orientations (decided 22 November 2026 under the standing delegation).** The requirement's "four 3D orientations" are the harvest's four rotations, restored in row L-R2; four yaws would not test a tilted or vertical hinge axis.

No item needs the owner.

## 7. Integration

- **Collaborators, all delivered:** Bowling ball (driver), Basketball and Tennis ball (payloads), Weight, Rope anchor and Pulley (ropes; both ends `SimulationDomain.Rope`, Bidirectional), Wall, Bumper, Pipe, both bends, Funnel, Domino.
- **Not delivered:** Beam shutter CAT-007 (row L-R7).
- **Campaign.** No level in `content/puzzles.json` places an Impact lever; plan level 9 teaches it. S8 is not needed.
- **Coverage binding.** Catalogue key `impact_lever` (CAT-034).
- **Solid rope (owner decision, 22 November 2026).** The rope is solid but is drawn with no collider today; the defect and its repair belong to `ENGINE-CONNECTION-MATERIALS` ([CAT-058](CAT-058-rope_anchor.md)). The two rope-lifting tests (`ImpactLeverTests.cs:414`, `:516`) do not rely on a body passing through a rope. A solid strand tied at a beam end can touch the beam, a stop or the fulcrum as the beam turns: a point for that story's probe, which re-runs both tests and row S-13 of the Beam shutter.
- **Known UI defect (deferred work, from 10.3a):** `ui/WorkshopConnections.cs` places every port in its part's frame and ignores the owning body, so a body-owned socket marker would be drawn off the turned beam. Rope artwork comes from committed spans and is unaffected. Owner: a follow-up UI slice; browser proof post-catalogue.

## 8. Delivery and residuals

- **Delivered** by 10.3, 10.3a and 10.3b.
- **Blocked on the engine programme (owner decision 3a, 22 November 2026).** Every residual slice below waits for it, the tests-only slice included. The stories that own this part's gaps are `ENGINE-CAPABILITY-INSTANCES` (point 2 of section 9) and `ENGINE-ISLANDS-SLEEP`; the rope rows are re-run by `ENGINE-CONNECTION-MATERIALS` (solid strand) as section 7 says.
- **Residual slices.** (1) Visible-solid remainder part A (markings redrawn flush; engine tier as that contract states). (2) One test-only slice for L-R1 to L-R6 in `ImpactLeverTests.cs`: data-only tier if only that file changes; engine tier if a shared harness helper is added. It closes the CAT-014 lever-loading entry in `deferred-work.md`. (3) L-R7 inside the Beam shutter slice.
- **Probe re-check for the test slice:** the lever is static-rooted with one moving body, so a beam-on-beam contact is dynamic box against dynamic box with both hinged; if L-R4 shows interpenetration that is an engine finding returned to planning, not a lever change.

## 9. Engine conformance (owner instruction, 22 November 2026)

**Verdict: Conforms with fixes.** The package fix is the flush redraw of the inset and four marks (section 2, visible-solid remainder part A). No engine code names this element (searched `engine/`, `CuriousContraptions.Simulation`, `CuriousContraptions.Bundle` and the abstractions), but one engine refactor is required because of its port roles (point 2).

1. **Declaration data only.** Two bodies, five scene colliders, two materials, one limited hinge with a friction torque, two parameters, two body-owned rope ports, one body-pose binding. No drive, no canned flip, no event. The legacy bounce counter (harvest fact 30) is not carried.
2. **Generic laws.** The launch: contact impulse on the beam, the hinge constraint about +Z, the limit rows at ±π/6 and contact on the payload. Balance and holding: the hinge's Coulomb friction row clamped to 2 N·m. Rope lifting: the tensile span law acting at a body-owned port, which gives the moment arm. Nothing is triggered by "a ball hits the lever". **Required engine refactor:** rope-port admission enumerates the role names `Tie`, `TieA` and `TieB` at three sites (`engine/gpu/WorkshopConnections.cs:163`–`:164`, `engine/gpu/ElementPortCatalogue.cs:182`, `CuriousContraptions.Bundle/BundleValidator.cs:543`, `:546`). `TieA` and `TieB` exist only because this package has two rope ports, so the engine carries an assumption that came from one element, and a package with three rope ports needs an engine edit (design guide D2). Owned by `ENGINE-CAPABILITY-INSTANCES` (owner decision 3a, 22 November 2026), which removes product shapes from shared contracts, the same class as engine audit findings E3 and E5: "A rope port is admitted by domain Rope, Bidirectional and its declared attachment role; port identity is the typed id, and no admission site enumerates role names. Acceptance: an unfamiliar package with three rope ports is admitted and routes; a rope port refuses a non-rope link with a typed reason."
3. **Three layers.** Colliders: `col_base`, `col_fulcrum`, `col_stop_left`, `col_stop_right` on the static body; `col_beam` on the beam body. Visuals: base, fulcrum, stops; beam, inset and marks under `Visual/Beam`. Animation: none; the beam art follows its body's pose. After part A the inset and marks are flat printing on the beam.
4. **One-way flow and typing.** Committed beam pose → art. Typed `PortId.TieA`/`TieB`, `RevoluteTravel`, `ParameterUnit.Degree` converted at compile; f32 values.
5. **Flat-state counts.** 2 bodies (1 dynamic); 5 colliders (4 boxes, 1 cylinder); 1 revolute joint; 0 drives, sensors, nodes, stores, events; 2 ports; 2 parameters; 1 body-pose binding.
6. **Performance fitness.** Five primitives, one joint; the static fixture is a static body. Island sleeping is not implemented; a resting body is still stepped, at the cost of its broadphase proxies and rows every substep (no sleep or deactivation code in `CuriousContraptions.Simulation`). Owned by `ENGINE-ISLANDS-SLEEP` (owner decision 3a, 22 November 2026). A beam tip moves at most a few metres per second, so tunnelling is not a risk beyond the engine's speculative contacts.
7. **Scenario tests.** Sections 3 and 5 list scenario tests; the residual rows L-R1 to L-R13 are scenarios of the same hinge and contact laws against other packages.
