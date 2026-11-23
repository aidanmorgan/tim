# CAT-053 · pulley declaration readiness spec

This is the CAT-053 spec for the fixed Pulley. Rope route, length and tension facts shared by every rope part are in [CAT-058 rope_anchor](CAT-058-rope_anchor.md). Sections 1 to 3 and 5 to 9 describe the delivered package, checked on 22 November 2026 against checkout `8d1d68aa`; published main is now `25df3cd5`, and its two later commits touch none of this package's files. Section 4 is the legacy harvest at `a6c914e`, kept verbatim as evidence; it is not a description of the current part. Browser qualification is pending until the post-catalogue phase.

## 1. Identity

| Field | Value |
| --- | --- |
| CAT ID | CAT-053 |
| Kind | `pulley`, catalogue title "Pulley" |
| Requirement anchor | [CAT-053](../requirements.md#current-cat-053); retained behaviour [todo-140](../requirements.md#todo-140) and [todo-141](../requirements.md#todo-141) |
| Mapped identities | [EL-206 Fixed pulley](../invest/named-elements.md#element-206), a candidate Duplicate closure; [EL-207 Moving pulley](../invest/named-elements.md#element-207), a separate element (section 8) |
| Roadmap story | [10.2c Pulley](../../../_bmad-output/planning-artifacts/epics.md#catalogue-slice-ownership); retain every CAT-053 mode and mapped named-identity obligation. |
| Status | **Delivered.** Package id `pulley`, directory `elements/Pulley`. Slice records: [10.2c pulley wheel](../../../_bmad-output/implementation-artifacts/spec-10-2c-pulley-wheel.md) (`done`), [9A.46a declared route guides](../../../_bmad-output/implementation-artifacts/spec-9a-46a-declared-route-guides.md) (commit `25b18d17`), [10.4](../../../_bmad-output/implementation-artifacts/spec-10-4-counterweight.md). Browser case pending (post-catalogue E2E). Every residual slice (section 8) is blocked on the engine programme (owner decision 3a, 22 November 2026); the story that owns this part's gap is `ENGINE-CAPABILITY-INSTANCES` (rope-port admission by role names), and the rope's missing strand collider belongs to `ENGINE-CONNECTION-MATERIALS` (solid strand). |
| Disposition | Delivered catalogue part. EL-206 Fixed pulley is a candidate Duplicate closure of it; EL-207 Moving pulley is a separate single element ([section 8](#8-delivery-and-residuals)). |

## 2. Declaration

### As delivered (checked 22 November 2026)

This table is the current declaration (`elements/Pulley/PulleyElementDeclaration.cs`).

| Item | Delivered value | Source |
| --- | --- | --- |
| Interfaces | `IMechanicalElement`, `ISceneGeometryElement` | `:7` |
| Body | one static body (footprint 1 body, 0 moving); zero mass frame | `:18`, `:29`, `:31` |
| Colliders | `col_mount` box 0.5 × 0.15 × 0.4 m; `col_wheel` cylinder r 0.4 m, length 0.17 m (the true wheel, not the legacy sphere) | `pulley.tscn:49`–`:64` |
| Material | restitution 1, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s | `:26` |
| Parameters | none | `:12` |
| Port | `PortId.Tie`, domain Rope, Bidirectional, role `Tie`, attachment `Guide`, position (0, 0.4, 0.12), normal +Y, guide radius 0.4 m, guide offset (0, 0, 0.12) | `:21`–`:23` |
| Stores, events, payload | none | `:27`, `:30` |
| Presentation | `Visual/Wheel` local Z rotation driven by `CommittedWheelAngle` of guide port `Tie`, identity mapping; pick radius 0.65 m | `:13`–`:17` |
| Palette | category Motion, order 33, colour (0.84, 0.61, 0.28) | `:9`–`:12` |

**Art and solidity.** The package is not yet in the visible-solid table. Classification from [`spec-engine-art-solidity.md`](../../../_bmad-output/implementation-artifacts/spec-engine-art-solidity.md) (row `pulley`) and the scene:

- solid, with colliders: `Visual_Mount` (`col_mount`), `Wheel/Visual_WheelBody` (`col_wheel`);
- **defect: `Visual_Post`**, a cream box 0.12 × 0.55 × 0.12 m (`pulley.tscn:20`–`:22`, `:75`–`:77`), has no collider. Owned by [visible-solid remainder part A](../../../_bmad-output/implementation-artifacts/spec-elements-visible-solid-remainder.md) (row A2), status draft;
- **`Wheel/Visual_Rim`**, a torus of ring radius 0.4 m and tube radius 0.035 m at z 0.1 (`pulley.tscn:31`–`:34`, `:85`–`:87`), stands 3.5 cm proud of the wheel radius. It is a solid of revolution about the turning axis, so the turn does not move it and a static torus matches it at every angle. Part A gives it a torus collider (row A3) under that contract's decision D4, a coordinator decision of 21 November 2026 under the owner's standing delegation;
- decorative, turned by the wheel binding: `Wheel/Visual_Spoke`, `Wheel/Visual_Marker`.

### Notes on the declaration

- **Fixed and frictionless.** The Pulley declares no wheel body and no joint: the wheel collider is static, and the rope's travel round it is the route law's, so it redirects tension without storing or dissipating work. Finite-inertia and frictional sheaves are separate future models, as the requirement says.
- **Guide.** Groove radius 0.4 m and rope-plane offset 0.12 m are fixed declaration constants, not player parameters. The route length includes the tangent legs and the wrapped arc.
- **Material.** A static restitution of 1 means the ball's own restitution governs the rebound (product rule), the convention the Rope anchor and the Impact lever base also use.
- **Wheel art.** `Visual/Wheel` turns by committed rope travel divided by the guide radius, and only while the route is complete and taut.
- **Art.** Navy `#293954` mount, cream `#fff8e9` post and rim, wheel in the catalogue colour `#d69c47`, navy spoke, gold `#f7cb52` marker. Icon: `elements/Pulley/icon.svg`, a wheel with hub, two hanging legs and a top bracket.
- **Catalogue.** Category Motion, description "Redirect a continuous rope. Connect both sides to loads or anchors; unfinished ropes cannot carry tension." Planned level `counterweight` places two locked pulleys; `pulley_depth` places one locked pulley and stocks one more.

## 3. Engine capabilities

Capability families: see the CAT-053 row of the [general-engine element map](../general-engine-element-map.md) and the `pulley` entry in [catalogue-elements.json](../../coverage/catalogue-elements.json). Not duplicated here.

**Capability probe (22 November 2026).** Tests were found by name and line; none was run. Counts: 6 present, 1 present-untested, 0 narrower, 0 missing. No engine code names the Pulley: the engine reads a guide from any port that declares `RopeAttachmentRole.Guide` with a radius. One admission rule is not general (section 9, point 2).

| Capability | Status | Symbol | Exercising test |
| --- | --- | --- | --- |
| Static box and cylinder colliders from scene proxies | present, untested | `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`) | no test brings a body into contact with `col_mount` or `col_wheel`; the rope tests place the part and never touch it. Part A's drop rows cover the post (A2) and rim (A3) and its table row proves the colliders match the art; residual row P-R4 adds the wheel face |
| Rope port with `Guide` role, radius and offset | present | `PortDescriptor.GuideRadius`, `RopeAttachmentRole` (`CuriousContraptions.Abstractions/PortDescriptor.cs:69`, `:74`) | `RopeGuideTests.StructuralValidationRefusesInvalidGuideDeclarations` (`CuriousContraptions.tests/RopeGuideTests.cs:290`), `MultipleGuidePortsOnSameBodyHaveDistinctIdsAndPublishBothObservations` (`:414`) |
| Route length with finite-radius tangents and wrapped arcs; automatic winding side | present | `RopeGeometry.EvaluateRoute`, `ChooseWinding` (`engine/gpu/RopeGeometry.cs:83`, `:188`) | `RopeGuideTests.EvaluateRouteSupportsMultipleGuidesWithDistinctRadiiAndOffsets` (`:72`), `BoundaryWrap180DegreeTurnAndSmallDeflection` (`:265`); control `StraightRouteControlHasZeroGuidesAndLineSegmentLength` (`:244`) |
| Tension through a guided route; slack and open routes carry none | present | `TensileSpanDeclaration` (`CuriousContraptions.Abstractions/TensileSpanDeclarationValues.cs:25`), `TensileConstraintSolver` (`CuriousContraptions.Simulation/TensileConstraintSolver.cs:27`) | `RopeTests.TwoPulleyAtwoodBalancesEqualWeightsAtRest` (`:699`), `TwoPulleyAtwoodAcceleratesUnequalWeightsWithTheoreticalAtwoodDynamics` (`:721`), `AtwoodSlackTransitionOnFloorContactReleasesTension` (`:842`) |
| Graph admission: branch, cycle, guide as endpoint, missing or all-static participants | present | `RopeTopology` (`engine/gpu/RopeTopology.cs:9`) | `RopeTests.MalformedGuideTopologyBranchingCycleAndGuideEndpointsRefuse` (`:949`), `StaticFeasibleRoutesDisplayAndImmovableOverlengthRoutesRefuseAtomically` (`:490`), `TensileAdmissionRejectsMissingParticipantsAndUnsupportedMotion` (`:545`) |
| Committed wheel angle from rope travel, read by a presentation binding | present | `PresentationDriver.CommittedWheelAngle` (`CuriousContraptions.Abstractions/ElementPresentation.cs:25`) | `RopeTests.WorkshopDeclaredPresentationAdvancesPulleyWheelRotationAngle` (`:967`), `RopeGuideTests.DynamicSimulationRotatesDistinctGuidesInInverseProportionToRadius` (`:109`) |
| Run/Reset, save and load, runtime cut | present | generic lifecycle and the typed cut command | `RopeTests.TwoPulleyRouteSurvivesResetAndSaveLoad` (`:905`), `RopeGuideTests.ResetRestoresInitialRouteStateAndZeroGuideObservations` (`:356`), `RopeTests.AtwoodCutSpanReleasesTensionAtomicallyAndWeightsFallFreely` (`:882`) |

**Collaborating parts.** Two dynamic ends, or an anchor and a load: [CAT-067 weight](CAT-067-weight.md), [CAT-058 rope_anchor](CAT-058-rope_anchor.md). The Impact lever's rope sockets ([CAT-034](CAT-034-impact_lever.md)) route through pulleys. The lessons use the Switch and Lamp.

## 4. Legacy harvest

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| P1 | Legacy pulley: fixed, frictionless, no moving-block ratio, no wheel inertia. | `parts/PulleyPart.cs@a6c914e:L6` | carry forward (fixed, frictionless, no inertia) | Matches the requirement; moving-block and inertia variants stay separate. |
| P2 | Groove radius 0.4 m; tie socket at (0, 0.4, 0.12), so rope spans lie in the plane z = 0.12 of the pulley frame. | `engine/MachineData.cs@a6c914e:L95-L98`; `parts/PulleyPart.cs@a6c914e:L9`, `parts/PulleyPart.cs@a6c914e:L14-L18` | carry forward | Authored geometry (re-declare as f32); level loads sit at z 0.12 to share that plane. |
| P3 | A guide accepts two spans; a route ending at a guide is incomplete and transmits nothing ("unfinished ropes cannot carry tension"). | `engine/RopeNetwork.cs@a6c914e:L34-L35`, `engine/RopeNetwork.cs@a6c914e:L76-L77`; `parts/catalog/pulley.tres@a6c914e:L11` | carry forward | Requirement: slack/open route transmits no tension. |
| P4 | Colliders: navy mount box half-extents (0.25, 0.075, 0.2) at (0, 0.55, −0.1) and a 0.4 m sphere at the wheel centre; pick radius 0.65. | `parts/PulleyPart.cs@a6c914e:L21-L24` | carry forward the box; sphere subject to Open question 1 | A sphere is not a wheel; the requirement wants true groove geometry. |
| P5 | Wheel artwork: post, cylinder, rim ring, spoke bar and gold marker (dimensions in Art). | `parts/PulleyPart.cs@a6c914e:L22-L32` | carry forward | Authored art; palette preserved. |
| P6 | Wheel angle advanced by distance / radius, wrapped to 2π, stored as transactional runtime state of the part. | `parts/PulleyPart.cs@a6c914e:L11-L13`, `parts/PulleyPart.cs@a6c914e:L34-L38` | carry forward the relation; do not carry forward the per-part state and `AdvanceRope` call | Per-element update path; the animation worker derives spin from committed rope travel. |
| P7 | Guides animate only when the route is complete and taut; each guide uses its own distance delta along the route. | `engine/RopeNetwork.cs@a6c914e:L50-L58` | carry forward the rule | Wheel travel follows committed physical travel. |
| P8 | Acceptance: with unequal masses the wheel turns; with equal masses it does not; an open route leaves it at 0; Reset returns every wheel to angle 0. | `CuriousContraptions.tests/RopeTests.cs@a6c914e:L58-L62`, `CuriousContraptions.tests/RopeTests.cs@a6c914e:L171`; `CuriousContraptions.tests/RopeOwnershipTests.cs@a6c914e:L54-L59` | carry forward | Wheel witness follows physical travel. |
| P9 | Acceptance: two fixed pulleys between masses m1, m2 accelerate at g (m2 − m1)/(m1 + m2); after 0.25 s each load's vertical speed is within 0.015 m/s of that (1e-5 in the direct binding test), the sum of the two heights is conserved within 0.001 m, the route stays taut at its length ±0.001 m, and reversing link direction and order gives the same motion. Cases (1, 4), (4, 1), (1, 1). | `CuriousContraptions.tests/RopeTests.cs@a6c914e:L21-L68`; `CuriousContraptions.tests/SceneRopeBindingTests.cs@a6c914e:L87-L144`; `CuriousContraptions.tests/RopeOwnershipTests.cs@a6c914e:L23-L63` | carry forward (re-freeze tolerances under f32) | Requirement: equal/unequal masses and declaration order. |
| P10 | Acceptance: over 900 ticks neither load penetrates the bench and the route never exceeds its length + 0.002 m. | `CuriousContraptions.tests/RopeTests.cs@a6c914e:L217-L241` | carry forward | Requirement todo-141 floor contact. |
| P11 | Acceptance: a fixed guide transfers tension but never pushes; a dynamic guide shares the reaction with both loads in one solve. | `CuriousContraptions.tests/RoutedRopeTests.cs@a6c914e:L18-L45` | carry forward the behaviour; do not carry forward the point-guide numbers | Point-guide geometry is superseded by finite-radius grooves. |
| P12 | Levels: `counterweight` (authored 19, plan level 12b, "A helping weight") places locked pulleys at (−2, 6, 0) and (2, 6, 0); `pulley_depth` (authored 20, plan level 12c, "Around the corner") places a locked pulley at (−2, 6, 0), stocks one pulley, and its solution adds `pulley_1` at (2, 6, 2). Solution spans: 5, 4, 0.81203175 m and 5, 4.472136, 0.81203175 m. | `content/puzzles.json@a6c914e:L5146-L5271`, `content/puzzles.json@a6c914e:L5473-L5505`, `content/puzzles.json@a6c914e:L5508-L5517`, `content/puzzles.json@a6c914e:L5583-L5645`, `content/puzzles.json@a6c914e:L5846-L5908`, `content/puzzles.json@a6c914e:L5910-L5942` | carry forward | Lesson set-ups to rebuild in Epic 15; lengths follow the connect-time rule (CAT-058 R6). |
| P13 | Lesson authoring: the depth lesson is the counterweight lesson with the right pulley unlocked, renamed `pulley_1`, moved to z 2, and the solution weight moved to z 2.12; hint texts. | `tools/Campaign/Program.cs@a6c914e:L189-L223`, `tools/Campaign/Program.cs@a6c914e:L408-L413` | carry forward | Campaign rebuild input. |
| P14 | Artwork route geometry: the rim arc lies on the 0.4 m radius in the wheel plane at depth 0.12; free legs stay outside the wheel; tangency is measured in the wheel plane even when a leg changes depth; the wrapped sweep is below π; a close starting load threads over the top (clockwise); symmetric routes use the top; a 1 mm move does not flip the winding; reversing endpoints mirrors the route. The `PulleyRopeRoute` source is already absent at `a6c914e`; only these test assertions remain. | `CuriousContraptions.tests/PulleyRopeRouteTests.cs@a6c914e:L24-L56`, `CuriousContraptions.tests/PulleyRopeRouteTests.cs@a6c914e:L112-L140` | carry forward | Requirement: rim tangent/arc artwork agrees with physical length; the same tangent/arc geometry defines the physical route. |
| P15 | Rope artwork reads committed poses only: scene edits and an unpublished solver step do not move it; it rebuilds identically after Reset; two endpoint knot spheres are visible on a complete pulley route. | `CuriousContraptions.tests/PulleyRopeRouteTests.cs@a6c914e:L58-L102`, `CuriousContraptions.tests/PulleyRopeRouteTests.cs@a6c914e:L142-L169` | carry forward the behaviour | Presentation reads committed state; the legacy `RopeVisual` class is not carried. |
| P16 | Pulley wheel accumulator rolled back after a failed tick. | `CuriousContraptions.tests/RemainingReadingCheckpointTests.cs@a6c914e:L57-L93` | do not carry forward | CPU transactional rollback; the current worker never faults a tick. |
| P17 | Point-guide route geometry and analytic stop sweeps. | `engine/physics/PhysicsRopeJoint.cs@a6c914e:L20-L22`; `CuriousContraptions.tests/RoutedRopeTests.cs@a6c914e:L104-L159` | do not carry forward | Superseded approximation and CPU solver path (CAT-058 R24, R26). |
| P18 | Historical context: "counterweight/3D pulley-routing lessons at 19–20"; fixed pulley routing supported, moving-block ratios future work. | `reference/P0-022-before/source-README.md@a6c914e:L29`, `reference/P0-022-before/source-README.md@a6c914e:L99` | carry forward as context | Confirms lesson order and the moving-pulley gap. |

### Files harvested

- `parts/PulleyPart.cs`
- `parts/catalog/pulley.tres`
- `parts/scenes/pulley.tscn`
- `engine/MachineData.cs`
- `engine/RopeNetwork.cs`
- `engine/physics/PhysicsRopeJoint.cs`
- `reference/cpu/MachinePart.cs`
- `reference/P0-022-before/source-README.md`
- `ui/WorkshopIcons.cs` (pictogram; not deleted by Epic 7)
- `reference/p025-production-isolated-20261004/src/ui/WorkshopIcons.cs` (checked, duplicate of the surviving pictogram)
- `tools/Campaign/Program.cs`
- `content/puzzles.json`
- `CuriousContraptions.tests/RopeTests.cs`
- `CuriousContraptions.tests/RoutedRopeTests.cs`
- `CuriousContraptions.tests/PulleyRopeRouteTests.cs`
- `CuriousContraptions.tests/RopeOwnershipTests.cs`
- `CuriousContraptions.tests/SceneRopeBindingTests.cs`
- `CuriousContraptions.tests/RemainingReadingCheckpointTests.cs`
- `engine/SceneRopeJoint.cs`, `engine/ConnectionPort.cs` (harvested into CAT-058; no pulley-only knowledge)
- `reference/p054-guide/puzzles-candidate.json` (checked; same pulley levels as `content/puzzles.json` apart from the orientation encoding)
- `engine/bridge/*` (checked, no element knowledge)

## 5. Acceptance outline

Requirement row: [CAT-053](../requirements.md#current-cat-053); story 10.2.

- **Chrome UI recipe.** Place two Pulleys high, a light Weight under the left and a heavy Weight under the right with toolbox and gizmo; connect weight → left pulley → right pulley → weight as rope links with the contextual wiring UI. Verify the committed configuration, the four-socket route and its measured length.
- **Positive.** Run: the heavy weight descends and lifts the light one; both wheels turn with committed rope travel.
- **Negative/control.** Equal masses stay balanced and wheels stay still; a route ending at a pulley (open) is dashed, transmits nothing and the load falls.
- **Boundaries.** Unequal and equal masses, reversed declaration order, 3D (depth) route as in `pulley_depth`, pendulum through a pulley, invalid graphs (branch, loop, missing length), impossible all-static fixed-length route rejected, route without a dynamic participant rejected.
- **Run/Reset.** Reset restores weight poses, wheel angles to 0, and exact span lengths; replay matches.
- **Save/Load.** Save/load keeps pulley identities, `tie` sockets and span lengths.
- **Integrations.** Lever rope sockets (CAT-034), anchors (CAT-058), counterweight lessons with Switch → Lamp (CAT-063, CAT-035).

**Native tests that exist per requirement criterion (22 November 2026; none was run for this review).** Equal and unequal masses: `RopeTests.cs:699`, `:721`, `WeightTests.Counterweight_LiftsLighterLoadOverPulley` (`WeightTests.cs:1153`), `_EqualMassesOverPulleyRemainBalanced` (`:1179`). Declaration order: `WeightTests.Counterweight_PulleyWiringDirectionInvariance` (`:1254`). Slack and open route: `RopeTests.cs:842`, `RopeTests.OpenGuideTopologyIsIncompleteAndCyclesOrBranchesRefuse` (`:230`), `WeightTests.Counterweight_OpenRouteFallsFreelyToBench` (`:1232`). Invalid graphs, all-static routes, missing bodies: `RopeTests.cs:949`, `:937`, `:490`, `:545`. Wheel travel agrees with physical length: `RopeTests.cs:721` (Δθ = Δs / 0.4) and `:967`. Reset, save and load: `RopeTests.cs:905`. Connected-body collision stays enabled: `RopeTests.SlackAndCutConnectionsKeepOrdinaryLoadContact` (`:385`).

**Residual acceptance rows (no test found by name or by reading the two rope suites).**

| Row | Given / When / Then | Planned test | Control | Boundary | Tolerance and stall guard |
| --- | --- | --- | --- | --- | --- |
| P-R1 3D direction | Given a Pulley yawed 90° so its groove plane is the YZ plane, an anchor and a 4 kg Weight offset in depth, When Run, Then the Weight hangs supported and the route length uses the rotated groove | `RopeTests.ARotatedPulleyRedirectsTheRopeInDepth` | the same Weight with the pulley link removed falls to the bench | pulley yaw 0° and 90° | route length within 1 cm of the tangent-and-arc length (rope knots are drawn 4 cm wide); rest judged over 3 s after a 5 s stall guard |
| P-R2 pendulum under a pulley | Given anchor → pulley → Weight with the Weight released 15° off the vertical below the wheel, When Run, Then it swings about the tangent point with the free span as its length and no energy growth | `RopeTests.AWeightSwingsBelowAPulley` | equal set-up released at 0° stays at rest | free span 1 m and 3 m | period within 5% of 2π√(L/g), the tolerance the anchor pendulum uses; energy ≤ 1.005 × initial; 10 s observation |
| P-R3 rim and post solid | the drop rows A2 and A3 of visible-solid remainder part A | owned by that contract | — | — | — |
| P-R4 wheel solid | Given a Basketball dropped onto the wheel's face with the Pulley on its back, When it lands, Then it stops at the drawn face | a `PulleyWheel` case in the drop theory of `VisibleSolidColliderTests` | a drop beside the wheel reaches the bench | none | the theory's own bound: within 5 mm of the drawn top plus the ball's radius |
| P-R5 wrong-kind link | the shared row `RopeTests.ARopePortRefusesANonRopeLinkAndANonRopePortRefusesARopeLink` ([CAT-067](CAT-067-weight.md), section 5) | shared | — | — | — |

## 6. Open questions

1. ~~Wheel collider~~ **Answered.** Owner decision (e), 18 Oct 2026: the wheel is a true cylinder. Story 10.1 (ENGINE-CYLINDER) delivered the engine capability: the closed shape set gains `Cylinder` (radius, width and mass declared; axis local +Y; exact inertia, axial $m r^2/2$ and transverse $m(3r^2+w^2)/12$), extracted from a `col_cylinder` `CylinderShape3D` or untapered `CylinderMesh` proxy, with closed-form contacts against plane, sphere, box and cylinder. Story 10.2 declares the wheel as that collider (the legacy r 0.4, width 0.17 art) in the Pulley package; the legacy sphere stand-in is not carried forward.
2. Groove radius 0.4 m and rope-plane offset 0.12 m are fixed declaration constants (`PulleyElementDeclaration.cs:22`–`:23`); the size is not configurable (D13).
3. The winding side is chosen by the route geometry, never authored per connection (`engine/gpu/RopeGeometry.cs:188`–`:199`: the higher wrap first, then the shorter sweep). One rule for every guide. A guide cannot declare another preference; recorded as a generality residue in section 9.
4. EL-207 Moving pulley is a separate single element with its own package (`moving_pulley`); finite-inertia and frictional sheaves stay future models.
5. Any placement rotation is admitted: the guide frame carries the part's rotation (decided under the standing delegation, D19). The proof is residual row P-R1.
6. Material as delivered: restitution 1, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s.
7. Runtime cutting is in scope and delivered by 10.2b ([CAT-058](CAT-058-rope_anchor.md) decision 7); through a pulley it is exercised at `RopeTests.cs:882`.

No item needs the owner.

## 7. Integration

- **Collaborators, all delivered:** Weight (`Load`), Rope anchor (`Anchor`), Impact lever (`TieA`/`TieB`, `Load`); Switch and Signal lamp in the lessons. Every rope end is `SimulationDomain.Rope`, Bidirectional; a guide accepts two spans and never an end.
- **Not delivered:** EL-207 Moving pulley, EL-063 Cable winch, EL-065 Load hook, EL-069/EL-070 moving containers, EL-067 Steel cable.
- **Campaign.** Authored levels 19 `counterweight` (plan level 12b; two locked pulleys) and 20 `pulley_depth` (plan level 12c, where the Pulley is taught; one locked, one stocked) are `Planned` with a single `activated` goal; both are in the "Ropes" group of delivery-batches section 4, provable from delivered parts after the engine programme (owner decision 3a). Level 20 is the first authored use of a depth route, so it should follow residual P-R1. S8 is not needed unless authoring finds a fixture the playable shape cannot express.
- **Coverage binding.** Catalogue key `pulley` (CAT-053).
- **Solid rope (owner decision, 22 November 2026).** The rope is solid but is drawn with no collider today; the defect and its repair belong to `ENGINE-CONNECTION-MATERIALS` ([CAT-058](CAT-058-rope_anchor.md)). No pulley test found by name relies on a body passing through a rope. A solid strand lies on the wheel and rim colliders it wraps, so that story's probe must settle how a strand and its own guide's colliders are filtered, and re-run the rope suites.

## 8. Delivery and residuals

- **Delivered** by 9A.46a (declared guides), 10.2c (wheel and guided route) and 10.4 (qualification).
- **Blocked on the engine programme (owner decision 3a, 22 November 2026).** Every residual slice and closure review below waits for it, the tests-only rows and the level proof included. The story that owns this part's gap is `ENGINE-CAPABILITY-INSTANCES` (section 9, point 2).
- **Residual slices.** (1) Visible-solid remainder part A: post and rim colliders and the table row; engine tier, as that contract states. (2) P-R1 and P-R2: tests only, added to `RopeTests.cs`; data-only tier if nothing but test files change. (3) Early level proof "Ropes".
- **EL-206 Fixed pulley: candidate Duplicate closure** (delivery-batches 2a, "Rope parts"); no delta. Its exact closure check is recorded in [EL-206](EL-206-fixed-pulley.md); it cites the tests in the table above. The ledger outcome "disconnected or misrouted rope receives no invisible coupling" is the open-route and graph-refusal rows. The closure review runs after the engine programme (owner decision 3a).
- **EL-207 Moving pulley:** not a duplicate (a dynamic body, mass and a second port); its own slice in the "Tensile" run. It has an engine blocker: the tensile row applies no reaction to a dynamic guide body (`CuriousContraptions.Simulation/TensileConstraintSolver.cs:64`–`:69`), so a pulley that hangs from the rope it guides would not be lifted by it. Owned by `ENGINE-TENSILE-EXTENSIONS` (owner decision 3a: tension reaction on a dynamic guide), after the engine programme.
- **Probe re-check for a residual slice:** collider indices 0 and 1 keep their order (part A appends); the guide radius still equals the wheel collider radius, so the rope sits on the drawn wheel.

## 9. Engine conformance (owner instruction, 22 November 2026)

**Verdict: Conforms with fixes.** The fixes are the two art-layer repairs of section 2 (post and rim colliders, visible-solid remainder part A); nothing else changes. No engine code names this element (searched `engine/`, `CuriousContraptions.Simulation`, `CuriousContraptions.Bundle` and the abstractions for its id and class name).

1. **Declaration data only.** One static body, two scene colliders, one material, one guide port, one presentation binding. No pulley code: the engine routes a rope round any port that declares `RopeAttachmentRole.Guide` with a radius.
2. **Generic laws.** Redirection and counterbalance: the tensile route law over declared guides (tangent legs and wrapped arc from the guide radius and offset). The pulley is frictionless and massless because it declares no wheel body: the wheel collider is static. Ball contact: ordinary contact with the declared material. **Required engine refactor, not a Pulley defect:** rope-port admission enumerates the role names `Tie`, `TieA` and `TieB` at three sites (`engine/gpu/WorkshopConnections.cs:163`–`:164`, `engine/gpu/ElementPortCatalogue.cs:182`, `CuriousContraptions.Bundle/BundleValidator.cs:543`, `:546`), so a package with a differently named rope port needs an engine edit. Owned by `ENGINE-CAPABILITY-INSTANCES` (owner decision 3a, 22 November 2026), which removes product shapes from shared contracts, the class of engine audit findings E3 and E5: admission by domain Rope, Bidirectional and the declared attachment role. The driver name `CommittedWheelAngle` is product-flavoured for what is a guide's committed travel angle, a naming refactor of the same story. Generality residue, recorded only: `ChooseWinding` prefers the higher wrap and a guide cannot declare otherwise.
3. **Three layers.** Colliders: `col_mount` (box) and `col_wheel` (cylinder) on the static body; part A appends a box for the post and a torus for the rim. Visuals: `Visual_Mount`, `Visual_Post` and the `Wheel` subtree. Animation: `Visual/Wheel` rotation from `CommittedWheelAngle`, which reads committed rope travel and writes nothing back; the spoke and marker it turns are decorative. The rim is drawn on the turning wheel but is rotationally symmetric, so a static torus matches it at every angle.
4. **One-way flow and typing.** Rope travel → committed wheel angle → presentation. Typed `PortId`, `SimulationDomain`, `RopeAttachmentRole`, `PresentationDriver`; f32 values.
5. **Flat-state counts.** 1 static body; 2 colliders (1 box, 1 cylinder), 4 after part A; 0 joints, drives, sensors, nodes, stores, events; 1 port; 0 parameters; 1 presentation binding.
6. **Performance fitness.** A static body with two primitives today and four after part A. The rim torus is the one collider whose gameplay value is small (3.5 cm proud); part A's decision D4 chose it over a flush redraw. Island sleeping is not implemented; a resting body is still stepped (no sleep or deactivation code in `CuriousContraptions.Simulation`). Owned by `ENGINE-ISLANDS-SLEEP` (owner decision 3a, 22 November 2026). The Pulley's body is static, so the gap concerns the loads it carries, not the part. No per-tick work beyond the routes that use the guide.
7. **Scenario tests.** Sections 3 and 5 list scenario tests only; the residual rows P-R1 and P-R2 are scenarios of the same route law.
