# EL-156 · Single-lobe cam named-identity spec

**Current delivery (24 October 2026):** [EL-156 delivery slice](../invest/named-elements.md#element-156). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 named-identity spec (Batch J). Baseline commit `a6c914e`; every citation is `path@a6c914e:Lstart-Lend` and resolves with `git show a6c914e:<path> | sed -n 'start,endp'`. **Proposed** marks an unsourced design value with a one-line justification; the owner may revise it. All values are canonical IEEE-754 f32 game values inside the [game-grade envelope](../../gpu-f32-physics.md#game-grade-envelope).

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-156 |
| Name | Single-lobe cam |
| Type | Mechanical |
| Anchor | [requirements.md#element-156](../requirements.md#element-156); [named entry](../invest/named-elements.md#element-156); scope indexes [todo-424](../requirements.md#todo-424) and [todo-375](../requirements.md#todo-375) |
| Proof owner | S344 |
| Refines / extends | No CAT spec. Driven through the mechanical input of [CAT-019 conveyor](CAT-019-conveyor.md) / [CAT-042 motor](CAT-042-motor.md); follower uses the slider of [CAT-062 spring](CAT-062-spring.md) |
| Related | EL-157 Double-lobe cam, EL-158 Rise-hold-fall cam (separate identities, same cam unit layout) |
| Roadmap story | [EL-156 delivery slice](../invest/named-elements.md#element-156); canonical proof owner and prerequisite decisions are recorded there. |
| Status | Not started (no cam package on `8d1d68aa`); blocked on the engine programme (owner decision 3a), relying most on ENGINE-SUPPLY-DEMAND (the Battery and Motor train) and ENGINE-ISLANDS-SLEEP; then S7. |
| Disposition (21 November 2026) | Member of the Family slice **Cam profiles** with siblings EL-157 and EL-158. Planned package id `cam_single`, directory `elements/CamSingle/`. The three differ only in the declared collider set and mass frame of the cam body. Not a duplicate of any delivered part |

## 2. Declaration

### Current declaration (21 November 2026)

Checked against this checkout (`8d1d68aa`); it is the whole declaration. Four changes from the earlier proposal (whose text is in git history):

- **The lobe is an eccentric cylinder.** The collider set is Sphere, Box, Plane, Cylinder and three hollow kinds, admitted on static or dynamic bodies (`CuriousContraptions.Abstractions/PhysicsDeclarationValues.cs:9`–`:12`); there is no convex prism. A disc of radius R mounted e off its axis lifts a flat follower by R − e·cos θ, which is exactly the earlier proposal's harmonic profile r(θ) = r₀ + h·(1 − cos θ)/2 with base radius r₀ = R − e and lift h = 2e. So the single lobe needs no new shape.
- **`lift` is fixed at 0.15 m.** No requirement row names a size control (D13). A parameter-bound collider offset is delivered for scene-geometry colliders (`ColliderParameterBinding`, `CuriousContraptions.Abstractions/SceneGeometry.cs:24`; the Ramp binds `OffsetZ`), but nothing here needs one.
- **No `LinkageOut` socket.** Mechanical connections admit revolute coordinates only (`engine/gpu/WorkshopConnections.cs:186`–`:192`). The follower works on other parts by contact.
- **Sign.** `CoordinatePerRadian` is +1, the delivered convention; the legacy −1 is not carried forward.

Values are starting values under D1. The housing, follower, hinge, port and materials below are shared unchanged by EL-157 and EL-158.

**Package.** Id `cam_single`, directory `elements/CamSingle/`, class `CamSingleElementDeclaration : ISliderElement, IHingeElement, ISceneGeometryElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:15`, `:20`). Category Motion, palette order 126 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)), display name "Single-lobe cam", description "One lift of its follower each turn." Footprint: 3 bodies, 2 moving, 1 prismatic, 1 revolute. Closest delivered shapes: Springboard (spring slider) and Conveyor (driven hinge with `DriveIn`).

**Bodies** (metres, element-local, origin at the base centre).

| Body | Joint | Colliders | Mass, inertia | Material (restitution, friction, rolling, threshold) |
| --- | --- | --- | --- | --- |
| 0 housing, static | — | base box half extents (0.5, 0.08, 0.4); guide plate box half extents (0.25, 0.65, 0.04) centred (0, 0.73, −0.14), standing on the base top | static | 0, 0.3, 0, 0.1 |
| 1 follower | slider 0, axis +Y, rest with its foot on the cam's lowest point (centre at y 1.025), travel −0.005 to +0.3 m, stiffness 20 N/m, damping 1 N·s/m, compression 0, `BodiesCollide` false | box half extents (0.22, 0.3, 0.06) | 0.3 kg, box moments | 0, 0.1, 0, 0.1 |
| 2 cam | hinge 0 about Z at (0, 0.50, 0), `RevoluteTravel.Continuous`, friction torque 0.02 N·m, initial angle 0 (lobe down), `BodiesCollide` false, no drive | cylinder radius 0.3, half-width 0.06, axis along hinge Z, centre (0, −0.075, 0) in the cam frame | 0.5 kg, centre of mass at the disc centre, moments 0.0225 axial and 0.01185 transverse kg·m² | 0, 0.1, 0, 0.1 |

The lower slider stop sits 5 mm below the lowest profile point so that the cam, never the stop, carries the follower. The guide plate stands 0.04 m behind the cam and follower.

**Clearances (hand check, repair of 22 November 2026).** The base top is at y 0.08. With the hinge at y 0.50 the disc's lowest point, lobe down, is 0.50 − 0.075 − 0.3 = 0.125, 0.045 m clear of the base; the draft's hinge at 0.45 put it at 0.075, 5 mm inside the base. The cam's top at rest is 0.50 + 0.225 = 0.725, so the follower's centre rests at 1.025. The follower's foot is 0.44 m wide for the whole family: EL-158's end cylinder meets the follower 0.208 m off the axis (0.24·sin 60°), which a 0.30 m foot would have taken on its edge.

**Parameters.** None.

**Port.** `PortId.DriveIn`, `PortRole.DriveIn`, Mechanical, Input, at (0, 0.50, 0.12), normal +Z, revolute coordinate 0, `CoordinatePerRadian` +1, cosmetic rim radius 0.22 on body 0. No `Drive` output (D19).

**Presentation.** Body bindings `Visual/Follower` → 1 and `Visual/Cam` → 2. No curves.

### Art and solidity (21 November 2026)

| Node | Mesh | Class | Collider or reason |
| --- | --- | --- | --- |
| `Base`, `GuidePlate` | boxes 1.0 × 0.16 × 0.8 and 0.5 × 1.3 × 0.08 | solid | the two static boxes |
| `Visual/Follower` | box 0.44 × 0.6 × 0.12 | solid | follower box |
| `Visual/Cam` | disc radius 0.3, width 0.12, centred 0.075 off the axle | solid | cam cylinder |
| `Visual/Cam/LobeMark` | gold dot inlaid flush at the rim point furthest from the axle | decorative | printed flat |
| `Visual/Cam/AxleCap` | disc radius 0.05 inlaid flush in the cam face at the axle | decorative | printed flat |

Colours: cam `#d69c47` with a gold `#f7cb52` lobe mark, follower cream `#fff8e9`, navy `#293954` base and guide plate ([DESIGN colour system](../../../DESIGN.md#colour-system)). Icon: an off-centre disc under a flat-footed bar. It differs from its siblings by outline: one round disc (EL-157 is a two-lobed peanut, EL-158 a disc with a raised arc band), and from the Flywheel by the follower bar above it.

### Behaviour and numbers (21 November 2026)

- Follower height above its rest: y(θ) = 0.075·(1 − cos θ), θ the committed cam angle from lobe-down. One lift of 0.15 m per revolution; a partial turn gives exactly y(θ).
- The follower stays on the cam while the cam's peak downward acceleration, 0.075·ω², is below g + k·y/m = 9.81 + 10 = 19.8 m/s² at the top: up to about 16 rad/s. Above that it floats, inside the Motor's 0–20 rad/s range; this is physical and not a defect.
- Peak shaft load is about 0.8 N·m (follower weight and spring at a 0.075 m lever, plus the cam's own 0.37 N·m imbalance). The default Motor (20 N·m) turns it freely; a Motor at its 0.1 N·m floor stalls on the rise, which is the load-aware control.
- The follower top at rest is 1.325 m above the base centre and 1.475 m at full lift. A Tennis ball (0.35 kg) on it adds 3.4 N and is carried.
- No signal read; no tick-order rule.

### Harvest-era subsections — removed; the text is in git history.

The current declaration above is the whole declaration: no parameters (lift fixed at 0.15 m), no `LinkageOut`, `CoordinatePerRadian` +1. The follower's top face works on other parts by contact. Its spring energy ½·k·y² and potential energy are all supplied through `DriveIn`; there is no internal source. One outcome in the row: EL-157 and EL-158 are separate identities, not variants of this one ([todo-424](../requirements.md#todo-424)).

## 3. Engine capabilities

### Capability probe (21 November 2026)

The harvest-era "Exists now" and "Missing" lists (baseline `a6c914e`) are removed; their text is in git history. The convex prism they called missing is not needed for this profile.

| Capability | Status | Symbol | Exercising test |
| --- | --- | --- | --- |
| Continuous revolute hinge with friction | present | `RevoluteHingeDescriptor`, `CuriousContraptions.Abstractions/MechanicalContractValues.cs:149` | `RevoluteDriveTests.AContinuousHingeTurnsPastTheFullCircleWithNoStopImpulseAndAccumulatesItsTravel` (`:195`) |
| Solid cylinder collider | present | `ColliderShapeKind.Cylinder`, `CuriousContraptions.Abstractions/ColliderDescriptor.cs:13` | `WorkshopPhysicsLawCylinderTests.AHorizontalCylinderIsCradledByTwoBoxEdges` (`:824`) |
| Collider offset from the hinge axis, swept by rotation, with a declared offset centre of mass | present, untested on a hinge | `HingeBodyDescriptor`, `MechanicalContractValues.cs:123` | `CompoundMassAndRollingTests.RotatingOffsetChildSweepsFromDeclaredCentreIntoAnObstacle` (`:47`) sweeps an offset child on a free body; no test turns one on a hinge-carried body |
| Slider with a declared spring and stops | present | `PrismaticSliderDescriptor`, `MechanicalContractValues.cs:104`; Springboard `:70` | `WorkshopPhysicsLawPrismaticTests.DeclaredDampingRemovesEnergyComparedWithTheSameStopFreeSpring` (`:71`); admission in `WoundSpringContractTests.OrdinarySliderAndHingeValidatorsOwnSpringUnitsAndNonNegativeRanges` (`:156`) |
| Mechanical input port and connection | present | `engine/gpu/WorkshopPhysicsCompiler.cs:583`–`:599` | `ConveyorTests.ChainedConveyors_DriveRelaysRotationToDownstreamConveyor` (`:417`) |
| A supplied drive holds a loaded shaft still at speed 0 (used to stop the cam mid-rise) | present | `RevoluteDriveDescriptor`, `MechanicalContractValues.cs:138` | `MotorTests.ASuppliedMotorAtSpeedZeroHoldsItsLoadedShaftStill` (`:573`) |
| Art follows committed moving bodies | present | `ElementPresentation` body bindings | `MotorTests.TheRotorArtFollowsTheCommittedRotorPoseAndResetsToItsScenePose` (`:1075`) |
| A slider-carried box riding a hinge-carried eccentric cylinder (contact between two moving bodies of one package that share no joint) | present, untested | the rows above in combination | none: no package or law test runs a follower on a rotating cam |

Counts: 6 present, 2 present-untested, 0 narrower, 0 missing. Beyond the engine programme (owner decision 3a), no specific engine story is a prerequisite. [Delivery batches 1a](../invest/delivery-batches.md#1a-families-one-slice-each-if-the-probe-passes) lists the cams as the Family slice "Cam profiles" waiting for no further story, as this probe found: the contact cam needs nothing from ENGINE-ASSEMBLY-JOINTS.

Families: ContactImpulse, EnvironmentState, FiniteLedger, GeometryQuery, JointConstraint, RigidBodyDynamics, ShaftTorque, plus StateTransaction ([map row](../general-engine-element-map.md); [element-02.json](../../coverage/engine/element-02.json)).

**Dependencies.** CAT-042 motor (Story 11.1) or another shaft source.

## 4. Sources and legacy

- Requirement: "One geometric lobe drives one follower lift per revolution"; outcome "Partial rotation yields only its actual profile displacement" ([element-156](../requirements.md#element-156)).
- Integration under [todo-375](../requirements.md#todo-375): "A cam profile controls a visible stroke ... Each named mechanism is separately implemented and taught." Campaign: "each cam profile", first use 31–40 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)).

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | Finite-inertia shaft and hinge guide | `engine/SceneRotaryShaft.cs@a6c914e:L13-L28` | carry forward | Cam shaft declaration. |
| 2 | Convex-hull prism collider for shafts | `engine/SceneRotaryShaft.cs@a6c914e:L29-L42` | carry forward (shape intent) | A profile prism is the same shape class. |
| 3 | Slider joint with travel interval and connected collision disabled | `parts/SpringPart.cs@a6c914e:L35-L48` | carry forward | Follower guide. |

**Files harvested:**
- `engine/SceneRotaryShaft.cs`
- `parts/MotorPart.cs` (rotor radius and mass as scale references)
- `parts/SpringPart.cs` (slider pattern)
- `parts/ConveyorPart.cs` (mechanical input convention)
- Checked, no cam knowledge: `CuriousContraptions.tests/PrescribedBodyTests.cs` (a variable named `follower`), `reference/P0-022-before/docs/coverage/engine/task-017.json` (coverage text naming "wheel/cam/drop" lessons).
- Searched with no hit for `cam`, `lobe` or `follower`: `parts/`, `engine/` (including `engine/physics/` and `engine/bridge/`), `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/`, `reference/`, `diagnostics/`.

## 5. Acceptance outline

Follow [element-156](../requirements.md#element-156) and the [mechanics profile](../invest/profiles.md#mechanics).
- **Chrome UI recipe.** Place battery, motor and the cam; connect `Supply` → `PowerIn` and `Drive` → `DriveIn`; put a ball on a ledge beside the follower top. Run.
- **Positive.** Each revolution produces exactly one follower lift of 0.15 m; the follower tips the ball off the ledge.
- **Negative/control.** A quarter turn (supply stopped early) lifts the follower only by the profile value at that angle; an unpowered cam produces no lift.
- **Boundaries.** Shaft speeds 1 and 20 rad/s (the follower stays in contact below about 16 rad/s and floats above it); reverse rotation.
- **Run/Reset.** Cam angle and follower height return to authored values.
- **Save/Load.** Pose and Mechanical links round-trip.
- **Integrations.** Cam, crank, clutch/brake and ratchet integration task under [todo-375](../requirements.md#todo-375); campaign row "Gears ... each cam profile ...": first use 31–40, reuse 41–50, 61–90, 136–150 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)); driven by CAT-042 motor; the follower works on a part such as EL-184 by contact.

### Acceptance rows (21 November 2026)

Native tests, class `CamTests` (shared by the family; rows for this identity below). Lift is the follower's committed height above rest; the cam angle is the hinge's committed travel. Lift is compared with ±0.02 m: contact slop and settling are a few millimetres, while a wrong eccentricity or a second lobe changes it by 0.03 m or more. A lift is counted when the follower passes above 0.10 m and then returns below 0.03 m. Guards are game seconds.

| # | Given / When / Then | Test | Control | Boundary |
| --- | --- | --- | --- | --- |
| C1 | Given Battery → Motor (`speed` 2 rad/s) → cam, when the cam has turned three revolutions (guard 30 s), then exactly three lifts are counted and each peaks within 0.15 ± 0.02 m | `ASingleLobeCamLiftsItsFollowerOncePerRevolution` | C3 | — |
| C2 | Source outcome. Given the cam turning at 1 rad/s, when the Motor's `speed` run control is set to 0 during the rise and the shaft has stopped, then the lift equals 0.075·(1 − cos θ) for the committed angle θ within ±0.02 m, and stays there for 5 s. Repeated for stops in each quarter of the rise | `APartialTurnLiftsOnlyTheProfileHeightAtThatAngle` | C1 | angle-independent: no stop angle is prescribed |
| C3 | Given the Motor unpowered or the Mechanical link absent, when Run lasts 5 s, then the lift stays under 0.005 m | `AnUndrivenCamNeverLifts` | C1 | — |
| C4 | Given `speed` 12 rad/s, then lifts still equal revolutions; given 20 rad/s, then the follower stays inside its travel and returns to the cam (floating is allowed) | `TheFollowerKeepsCountBelowTheFloatSpeedAndStaysBoundedAboveIt` | C1 | either side of the 16 rad/s float speed |
| C5 | Given a Reverse transmission before the cam, then one lift per revolution as in C1 | `ReverseRotationLiftsOncePerRevolution` | C1 | sign |
| C6 | Given Motor `torque` 0.1 N·m, then the cam stalls on the rise and the lift never reaches 0.10 m in 20 s | `AMotorTooWeakToLiftStallsOnTheRise` | C1 | Motor torque minimum |
| C7 | Given a Tennis ball resting on the follower top, when one lift completes, then the ball rose at least 0.12 m with the follower | `TheFollowerCarriesARestingBall` | C3 | — |
| C8 | Reset returns cam angle and follower height to the authored values; a saved construction loads with pose and Mechanical link | `ResetAndLoadRestoreEachCam` (theory over the three packages) | — | — |
| C9 | An electrical or activation wire to `DriveIn`, or a second Mechanical link, is refused | generic: `MechanicalCoordinateBindingsTests.WorkshopConnections_ValidateControls_RefuseInvalidMechanicalConnections` (`:456`) | — | — |
| C10 | Art follows the committed cam and follower; every solid mesh has its collider | `EachCamArtFollowsItsCommittedBodies`; the package's row in `VisibleSolidColliderTests` | — | — |

There is no parameter, so there is no parameter refusal row. C1, C2 and C7 satisfy the cam part of the cross-element task under [todo-375](../requirements.md#todo-375) ("a cam profile controls a visible stroke"); the crank, clutch/brake and ratchet parts belong to their own identities.

### Integration (21 November 2026)

- Works with: Battery, Motor, Reverse transmission, Clutch (delivered); cargo and contact parts above the follower (balls, Pressure plate, Impact switch; delivered); EL-184 Mechanical ball gate (planned, worked by contact).
- Campaign: "each cam profile", first use levels 31–40, reuse 41–50, 61–90, 136–150.
- Coverage binding: a final package-source-binding section in this spec (Package, Named identity, Acceptance, Decision, Mode records), as EL-239 on published main; blocked on S7 (section 7).
- A level that places it as an authored fixture needs the lesson-fixture story (S8).

### Delivery (21 November 2026)

- **Slice shape.** Family slice `family-cam-profiles`: EL-156, EL-157, EL-158, three packages, one shared test class with per-identity rows. EL-158 leaves the slice if its own probe item fails (see its spec); EL-156 and EL-157 do not depend on it.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which includes ENGINE-SUPPLY-DEMAND (the Motor's work debited from its Battery) and ENGINE-ISLANDS-SLEEP; then S7 (the coverage follow-on, which widens the reader, owner decision 15). S3 preferred. For the family slice EL-158 may also need ENGINE-PROFILE-COLLIDER (its spec); EL-156 and EL-157 do not.
- **Files.** Adds `elements/CamSingle/CamSingle.csproj`, `CamSingleElementDeclaration.cs`, `cam_single.tscn`, `icon.svg`, the same four for `CamDouble` and `CamDwell`, and `CuriousContraptions.tests/CamTests.cs` (compiled by the test project's one `*.cs` rule). Edits `CuriousContraptions.slnx` (three lines), the `VisibleSolidColliderTests` table until ENGINE-ART-SOLIDITY, and the three specs (status, and the binding section once S7 admits it).
- **Review tier.** Engine tier: two capabilities are present-untested.
- **Disqualifies the plan** (the probe runs a declared fixture first): the follower box does not rest stably on the turning eccentric cylinder at 2 rad/s (jitter above 0.01 m, or the follower is driven sideways through its slider); two moving bodies of one package without a joint between them do not collide. Either is an engine contact defect for its own slice, not cam-specific code.

## 6. Open questions

None open. Resolved 21 November 2026:

1. Output: contact-only follower. No linear mechanical socket exists and no row needs one. D19.
2. Profile law: harmonic, because that is what an eccentric disc gives with a delivered collider. D19 (the spec's proposal).
3. Cam phase is not a parameter; the cam starts each Run lobe-down. D13.
4. `lift` is fixed at 0.15 m. D13.
5. Hinge raised from y 0.45 to 0.50, the guide plate lengthened to stand on the base, and the family follower widened to 0.44 m (22 November 2026, audit H6 and the EL-158 pickup geometry). Reasons in the Clearances paragraph of section 2.

## Engine conformance (22 November 2026)

Checked against the owner addendum of 22 November 2026 (AGENTS.md element-free engine gate and Tri-Graph rule; `_bmad/custom/element-free-engine-design.md`; `docs/gpu-f32-physics.md`). The addendum names cams: a cam here is ordinary colliders on an ordinary hinge body, with no cam capability in the engine.

1. **Declaration data only.** A static housing, a follower on a sprung slider, a cam body on a continuous hinge and one mechanical input. There is no cam profile function, lift table, follower rule or angle-to-height coupling anywhere: the lift is geometry in contact.
2. **Generic laws.** Rigid contact between a box and a cylinder carried on different joints, the slider's declared spring and stops, the hinge's declared friction, gravity, and the bilateral coupling of the mechanical connection. y(θ) = 0.075·(1 − cos θ) is a hand consequence of an eccentric disc under a flat face; the engine never evaluates it.
3. **Three layers.** Colliders: four (base box and guide-plate box on the static body, follower box, cam cylinder). Visuals: one mesh per collider; lobe mark and axle cap inlaid flush. Animation: none. `Visual/Follower` and `Visual/Cam` follow their bodies' poses.
4. **Flow and typing.** One typed Mechanical `DriveIn` on revolute coordinate 0; no parameters; f32 values.
5. **Flat state.** Bodies 3 (1 static, 2 dynamic); colliders 4 (boxes 3, cylinder 1); joints 2 (1 prismatic, 1 revolute); drives 0; sensors 0; domain nodes 0; stores 0; presentation bindings 2 (body).
6. **Performance.** Four primitive colliders, as the Motor or Springboard. No island sleeping exists today; a resting cam and follower cost nothing only once ENGINE-ISLANDS-SLEEP (owner decision 3a) is delivered, ahead of this part. Nothing is cosmetic in physics.
7. **Tests are scenarios.** Rows C1 to C10 run the production simulation with a delivered Motor and need no cam-specific code path.

**Verdict: conforms.**

## 7. Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of the identity's spec headed "Package source binding", read by `tools/Coverage/NamedPackageSources.cs` (pattern: EL-239 Baseball on published main). The section is not written here: the reader parses every spec that carries that heading and fails the whole coverage run when a field does not resolve. **Blocked on the coverage follow-on story S7.** S7 widens the reader (owner decision 15), including for companion packages and TH, RAD and GAP identities.

| Field | Planned value |
| --- | --- |
| Package | `cam_single` |
| Named identity | EL-156, `../invest/named-elements.md#element-156` |
| Acceptance | `todo-424`; no sequence task yet |
| Decision | S344, `../invest/scope-corrections.md#s344` |
| Mode records | `Configuration=Fixed` |

What it lacks today: `scope-corrections.md` has no `s344` block on published main; `todo-424` is a scope-index row shared by EL-156, EL-157 and EL-158 with no sequence-task anchor directly after it, which the reader requires; and one task source can bind only one package.
