# EL-111 · Centrifugal governor named-identity spec

**Current delivery (24 October 2026):** [EL-111 delivery slice](../invest/named-elements.md#element-111). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 named-identity spec (Batch J). Baseline commit `a6c914e`; every citation is `path@a6c914e:Lstart-Lend` and resolves with `git show a6c914e:<path> | sed -n 'start,endp'`. **Proposed** marks an unsourced design value with a one-line justification; the owner may revise it. All values are canonical IEEE-754 f32 game values inside the [game-grade envelope](../../gpu-f32-physics.md#game-grade-envelope).

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-111 |
| Name | Centrifugal governor |
| Type | Mechanical |
| Anchor | [requirements.md#element-111](../requirements.md#element-111); [named entry](../invest/named-elements.md#element-111); scope index [todo-377](../requirements.md#todo-377) |
| Proof owner | S338 |
| Refines / extends | No CAT spec. Driven like [CAT-019 conveyor](CAT-019-conveyor.md) through a mechanical input; uses the slider spring of [CAT-062 spring](CAT-062-spring.md) |
| Related | EL-110 Flywheel (same scope index), EL-199 Electric motor, EL-184 Mechanical ball gate (worked by the bracket pad's contact) |
| Roadmap story | [EL-111 delivery slice](../invest/named-elements.md#element-111); canonical proof owner and prerequisite decisions are recorded there. |
| Status | Not started (no `elements/Governor/` package on `8d1d68aa`); blocked on the engine programme (owner decision 3a), relying most on ENGINE-SUPPLY-DEMAND (the Battery and Motor train) and ENGINE-ISLANDS-SLEEP; then ENGINE-JOINT-KINDS (dynamic-support hinge) only if the probe fixture fails; then S7. |
| Disposition (21 November 2026) | To deliver as a single element. Planned package id `governor`, directory `elements/Governor/`. Not a duplicate and not a family member: no delivered part carries a hinge on a spinning body |

## 2. Declaration

### Current declaration (21 November 2026)

Checked against this checkout (`8d1d68aa`); it is the whole declaration. Three things change from the earlier proposal (whose text is in git history), each forced by current code:

- A slider has no support field and always rides the package's base body (`PrismaticSliderDescriptor`, `CuriousContraptions.Abstractions/MechanicalContractValues.cs:104`), so the sleeve cannot slide on the spinning spindle. The output is instead a **lift bracket** sliding on a static post above the spindle.
- There is no distance joint. The bracket is tied to the flyball arms by declared ratio couplings (`MechanicalTransmissionDescriptor`, `MechanicalContractValues.cs:258`), the capability the Wound spring uses between a revolute and a prismatic coordinate (`elements/WoundSpring/WoundSpringElementDeclaration.cs:52`–`:54`).
- A mechanical connection admits revolute coordinates only (`engine/gpu/WorkshopConnections.cs:186`–`:192`), so there is no `LinkageOut` socket. The bracket acts on other parts by contact.

The flyballs are smaller than first proposed (arm 0.3 m, ball radius 0.08 m) so their sweep, 0.48 m at full swing, stays over the base. All values are starting values under D1.

**Package.** Id `governor`, directory `elements/Governor/`, class `GovernorElementDeclaration : ISliderElement, IHingeElement, IMechanicalTransmissionElement, ISceneGeometryElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:15`, `:20`, `:90`). Category Motion, palette order 164 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)), display name "Centrifugal governor", description "Spinning weights swing out and raise a bracket." Footprint: 6 bodies, 5 moving, 1 prismatic, 4 revolute. Closest delivered shapes: Reverse transmission (two hinges and an internal coupling) and Wound spring (revolute-to-prismatic coupling).

**Bodies** (metres, element-local, origin at the base centre; material restitution 0, friction 0.3, rolling 0, bounce threshold 0.1 throughout, the catalogue rigid-part material).

| Body | Joint | Colliders | Mass, inertia |
| --- | --- | --- | --- |
| 0 housing, static | — | base box half extents (0.5, 0.08, 0.6); post box half extents (0.06, 0.75, 0.06) centred (0, 0.83, −0.55) | static |
| 1 lift bracket | slider 0 on the post, axis +Y, travel 0 to 0.2 m, spring stiffness from `spring_stiffness`, damping 2 N·s/m, compression 0, `BodiesCollide` false | beam box half extents (0.06, 0.03, 0.33) centred (0, 1.25, −0.25) at rest; pad box half extents (0.15, 0.02, 0.15) centred (0, 1.30, 0) | 0.2 kg, box moments |
| 2 input wheel | hinge 0 about Z at (0, 0.3, 0.25), continuous, friction 0.02 N·m | cylinder radius 0.15, half-width 0.04 | 0.25 kg, solid disc |
| 3 spindle | hinge 1 about the vertical (base frame turned so hinge Z is +Y) at (0, 0.08, 0), continuous, friction 0 | shaft cylinder radius 0.015 from y 0.08 to 1.08 (repair of 22 November 2026, audit H7: each flyball, radius 0.08 centred 0.10 m from the axis, reaches in to 0.02 m, so a 0.06 m shaft overlapped it by 0.04 m at rest; 0.015 leaves 5 mm); bevel disc radius 0.15, half-height 0.02 at y 0.3; crossbar box half extents (0.14, 0.03, 0.03) at y 1.0 | 0.3 kg, cylinder about its axis |
| 4, 5 flyball arms | hinges 2 and 3, **support body 3**, pivots 0.1 m either side of the spindle axis at y 1.0, axis tangential, `RevoluteTravel.Limited` 0 to 1.396 rad (80°) outward, friction 0.005 N·m, `BodiesCollide` false | rod box half extents (0.02, 0.15, 0.02) centred 0.15 m below the pivot; ball sphere radius 0.08 centred 0.3 m below the pivot | 0.5 kg each, centre of mass at the ball centre, moments 0.00128 kg·m² on each axis (solid sphere) |

**Couplings** (all `TransmissionConstraintKind.Bilateral`): input wheel to spindle, ratio 1 (the bevel pair); arm A to arm B, ratio 1 (both swing together); arm A to bracket, 0.1432 m per radian (0.2 m of lift at 80°).

**Parameter.**

| Name | Type | Unit | Default | Minimum | Maximum | Run edit | Editor policy |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `spring_stiffness` | Real f32 | `ParameterUnit.NewtonPerMetre` | 40 | 10 | 100 | `BuildOnly` | out-of-range and non-finite refused |

**Port.** One: `PortId.DriveIn`, `PortRole.DriveIn`, Mechanical, Input, at (0, 0.3, 0.3), normal +Z, revolute coordinate 0, `CoordinatePerRadian` +1 (the delivered convention; the legacy −1 is not carried forward), cosmetic rim radius 0.15 on body 0. No `Drive` output and no electrical port: nothing in the requirement row or the campaign row uses one (D19).

**Presentation.** Five body bindings: `Visual/Bracket` → 1, `Visual/InputWheel` → 2, `Visual/Spindle` → 3, `Visual/ArmA` → 4, `Visual/ArmB` → 5. No curves.

### Art and solidity (21 November 2026)

Every mesh is a solid with the collider in the table above: `Base`, `Post`, `Visual/Bracket` (beam and pad), `Visual/InputWheel`, `Visual/Spindle` (shaft, bevel disc, crossbar), `Visual/ArmA` and `Visual/ArmB` (rod and ball). Bevel teeth are drawn as a flat inlay on the two discs (decorative, printed flat). Two link rods join each arm to the bracket. They are required: without them nothing drawn connects the flyballs to the bracket they raise. They are decorative art moved by span bindings, because a rod drawn as a fixed proud solid would need a collider. The span binding the Springboard uses for its coil (`elements/Springboard/SpringboardElementDeclaration.cs:14`) must be able to join two body-bound nodes: the slice's probe confirms that first, and if it cannot, the question goes to the owner before anything is built. Colours: flyballs gold `#f7cb52`; spindle and arms pale grey `#ccd8dc`; housing navy `#293954` with a cream `#fff8e9` post; bracket in the drivetrain colour `#d69c47` ([DESIGN colour system](../../../DESIGN.md#colour-system)). Icon: a vertical shaft with two balls swung out in a V and a short bar above. It differs from the Flywheel (one disc) and the Windmill (blades on a horizontal shaft) by the two hanging balls.

### Behaviour and numbers (21 November 2026)

Per arm, about its pivot, with m = 0.5 kg, L = 0.3 m, pivot offset a = 0.1 m, bracket mass 0.2 kg, lift ratio ρ = 0.1432 m/rad and arm angle φ:

m·ω²·(a + L·sin φ)·L·cos φ = m·g·L·sin φ + (0.2·g + k·ρ·φ)·ρ/2

| Spindle speed (rad/s) | k = 40 N/m: angle, lift | Note |
| --- | --- | --- |
| 0 to 3.0 | 0°, 0 m | below onset: the bracket's weight holds the arms on their lower stop; onset is where 0.015·ω² = 0.14 N·m |
| 6 (Motor default) | about 33°, 0.08 m | k = 10 gives about 0.105 m, k = 100 about 0.05 m |
| 10 | about 69°, 0.17 m | |
| 15 and above | 80°, 0.2 m | upper stop |

So the Motor's 0–20 rad/s `speed` control sweeps the whole travel, and a stationary shaft does not actuate it. The lift is produced by rigid-body dynamics on the arm hinges; no speed threshold, sensor or cosmetic pose is declared. The pad carries cargo by contact: at 0.08 m it lifts a Tennis ball (0.35 kg) resting on it, since the spring and centrifugal margin exceed 3.4 N from about 5 rad/s. No signal read is involved, so no tick-order rule applies.

### Harvest-era subsections — removed; the text is in git history.

The current declaration above is the whole declaration (no named variants, [element-111](../requirements.md#element-111)): no `LinkageOut`, `CoordinatePerRadian` +1, the bracket on its post in place of the earlier sleeve. The bracket's displacement is physical; no speed threshold or cosmetic pose replaces it (map row). The spring stores ½·k·x² at lift x; all energy comes from the driven spindle, and a stationary spindle leaves the bracket at rest on its lower stop.

## 3. Engine capabilities

### Capability probe (21 November 2026)

The harvest-era "Exists now" and "Missing" lists (baseline `a6c914e`) are removed; their text is in git history.

| Capability | Status | Symbol | Exercising test |
| --- | --- | --- | --- |
| Continuous and limited revolute hinges with friction | present | `RevoluteHingeDescriptor`, `MechanicalContractValues.cs:149` | `RevoluteHingeFrictionTests.AHingedBarIsHeldWhileItsGravityTorqueIsBelowTheFrictionTorque` (`:77`); `ClutchGeneralityTests.AuthoredHingeDegreesAreValidatedAgainstRadianLimits` (`:500`) |
| Hinge axis turned away from element Z | present | `HingeFrame.Rotation`, `MechanicalContractValues.cs:118` | `MechanicalCoordinateBindingsTests.RotatedJointFramesTransmitLoadBackToTheSource` (`:931`) |
| Slider with a parameter-bound spring | present | `PrismaticSliderDescriptor.Stiffness`, `MechanicalContractValues.cs:109`; Springboard `:70` | `WorkshopPhysicsLawPrismaticTests.DeclaredDampingRemovesEnergyComparedWithTheSameStopFreeSpring` (`:71`); admission in `WoundSpringContractTests.OrdinarySliderAndHingeValidatorsOwnSpringUnitsAndNonNegativeRanges` (`:156`) |
| Internal revolute-to-revolute coupling | present | `ReverseTransmissionElementDeclaration.Transmissions` | `ReverseTransmissionTests.ReverseTransportsCargoOppositeTheZeroReverserBypass` (`:134`) |
| Several couplings sharing one coordinate, passive law | present | `MechanicalTransmissionDescriptor` | `MechanicalCoordinateBindingsTests.ChainsAndClosedRelationsUseTheSamePassiveLaw` (`:954`) |
| Mechanical input port and connection | present | `engine/gpu/WorkshopPhysicsCompiler.cs:583`–`:599` | `ConveyorTests.ChainedConveyors_DriveRelaysRotationToDownstreamConveyor` (`:417`) |
| Several colliders and an offset centre of mass on one moving body | present | `HingeBodyDescriptor.Colliders`, `MechanicalContractValues.cs:123` | `CompoundMassAndRollingTests.RotatingOffsetChildSweepsFromDeclaredCentreIntoAnObstacle` (`:47`); `OffCentreImpactUsesDeclaredMassCentreAndRotatedPrincipalInertia` (`:221`) |
| Art follows each committed moving body | present | `ElementPresentation` body bindings | `MotorTests.TheRotorArtFollowsTheCommittedRotorPoseAndResetsToItsScenePose` (`:1075`) |
| A hinge supported by another hinge's moving body | present, untested | `RevoluteHingeDescriptor.Support`, `HingeSupportOrder.Resolve` (`MechanicalContractValues.cs:268`), used at `engine/gpu/WorkshopPhysicsCompiler.cs:193` | admission and compiled membership only (`ClutchGeneralityTests.HingeSupportIsRequiredAndRejectsMissingSelfAndCyclicReferences` `:473`, `CompiledMembershipPreservesExplicitOwnersAcrossSupportsAndRejectsInvalidConstraints` `:275`); no test runs a hinge on a spinning carrier, and no package uses one |
| Bilateral coupling from a revolute to a prismatic coordinate | present | `MechanicalTransmissionDescriptor` | `RetentionWorkTests.DeclaredSharedSupportTransfersSignedWorkAndBackdrivesWithoutCreatingEnergy` (`CuriousContraptions.tests/RetentionWorkTests.cs:16`, coupling declared at `:38`–`:39`) |
| A coupling whose driving hinge rides a moving support | present, untested | same | none |

Counts: 9 present, 2 present-untested, 0 narrower, 0 missing. A linear mechanical socket is narrower in code (revolute only) but is not needed, so it is not counted. Beyond the engine programme (owner decision 3a), no specific engine story is a prerequisite unless the slice's probe fails; then ENGINE-JOINT-KINDS (dynamic-support hinge) is (see Delivery).

Families: EnvironmentState, FiniteLedger, JointConstraint, RigidBodyDynamics, ShaftTorque, plus StateTransaction ([map row](../general-engine-element-map.md); [element-02.json](../../coverage/engine/element-02.json)).

**Dependencies.** CAT-042 motor (Story 11.1) as the drive; a part the bracket pad works on by contact, such as EL-184.

## 4. Sources and legacy

- Requirement: "Rotating masses displace a linkage according to actual speed"; outcome "Stationary shaft does not actuate it" ([element-111](../requirements.md#element-111)). Map: "Actual rotating mass/linkage response supplies governor displacement; no speed-threshold cosmetic pose replacement."
- Integration under [todo-377](../requirements.md#todo-377): "a governor responds to actual speed". Campaign: first use 81–90 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)).

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | Joint kinds BallSocket, Hinge, Slider | `engine/physics/JointEquations.cs@a6c914e:L5-L5` | carry forward (closed set) | Enum of joint kinds the governor composes. |
| 2 | Shaft hinge about one axis, both directions, connected collision disabled | `engine/SceneRotaryShaft.cs@a6c914e:L25-L28` | carry forward | Spindle guide. |
| 3 | Slider spring and damping loads added each step to a slider guide | `engine/SlidingBlade.cs@a6c914e:L36-L42` | carry forward (law), do not carry forward (per-step `Prepare` loop) | Per-element update loops are banned; the spring becomes declared constraint data. |

No legacy governor exists.

**Files harvested:**
- `engine/physics/JointEquations.cs`
- `engine/SceneRotaryShaft.cs`
- `engine/SlidingBlade.cs`
- `parts/ConveyorPart.cs` (mechanical socket convention)
- Searched with no hit for `governor` or `flyball`: `parts/`, `engine/` (including `engine/physics/` and `engine/bridge/`), `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/`, `reference/`, `diagnostics/`.

## 5. Acceptance outline

Follow [element-111](../requirements.md#element-111) and the [mechanics profile](../invest/profiles.md#mechanics).
- **Chrome UI recipe.** Place battery, motor and governor; connect `Supply` → `PowerIn` and motor `Drive` → governor `DriveIn`; rest a Tennis ball on the bracket pad. Run.
- **Positive.** As the spindle reaches speed the flyballs swing out and the bracket rises, lifting what rests on its pad.
- **Negative/control.** A stationary or disconnected spindle leaves the bracket down; a slow speed below the lift onset does not actuate.
- **Boundaries.** `spring_stiffness` 10 and 100; motor speed 0 and 20 rad/s; supply loss lowers the bracket as the spindle slows.
- **Run/Reset.** Spindle, flyballs and bracket return to rest.
- **Save/Load.** `spring_stiffness`, pose and Mechanical links round-trip.
- **Integrations.** Governor-responds-to-actual-speed integration task under [todo-377](../requirements.md#todo-377) (with EL-110 and TH-22/TH-23); campaign row "Ratchet, escapement, indexed carousel, flywheel, governor ...": first use 81–90, reuse 91–100, 131–140, 146–150 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)); driven by CAT-042 motor; the bracket pad works on a part such as EL-184 by contact.

### Acceptance rows (21 November 2026)

Native tests, class `GovernorTests`. Lift is the bracket's committed slider coordinate. It is compared in bands, not to a value: the lesson distinguishes down (under 0.01 m), part-raised and fully raised (0.18 m or more), so a band 0.03 m wide either side of the computed lift is the tolerance. Each run waits for the lift to settle (change under 0.005 m over 1 s) with a 30 s guard.

| # | Given / When / Then | Test | Control | Boundary |
| --- | --- | --- | --- | --- |
| G1 | Given Battery → Motor → Governor, when the Motor runs at 6 and then 20 rad/s, then the lift is within 0.05–0.12 m and then at least 0.18 m | `AFasterSpindleRaisesTheBracketFurther` | G2 | upper stop |
| G2 | Source outcome. Given the same construction with the Motor unpowered, or with no Mechanical link, when Run lasts 5 s, then the lift stays under 0.005 m and the arms stay on their lower stop | `AStationaryOrDisconnectedSpindleLeavesTheBracketDown` | G1 | — |
| G3 | Given Motor `speed` 2 rad/s, then the lift stays under 0.01 m; at 5 rad/s it exceeds 0.02 m (computed 0.04 m: the balance in section 2 gives an arm angle of 0.28 rad) | `BelowTheOnsetSpeedTheBracketStaysDown` | G1 | either side of the 3.0 rad/s onset |
| G4 | Given the bracket raised at 6 rad/s, when the supply is cut, then the lift returns under 0.01 m within the guard | `TheBracketDropsAsTheSpindleSlows` | G1 | — |
| G5 | Given `spring_stiffness` 10 and 100 at 6 rad/s, then the lift at 10 exceeds the lift at 100 by at least 0.03 m | `AStifferSpringLiftsLess` | — | parameter minimum and maximum |
| G6 | Given a Tennis ball resting on the pad, when the spindle reaches 10 rad/s, then the ball has risen at least 0.1 m while staying on the pad | `TheRaisedPadCarriesARestingBall` | the ball does not move in G2 | — |
| G7 | Given a Reverse transmission before the governor, then the lift equals G1's band (the response does not depend on the turning sense) | `TheLiftDoesNotDependOnTheTurningSense` | G1 | sign |
| G8 | Reset returns all five moving bodies to their authored poses and zero speed; a saved construction loads with `spring_stiffness`, pose and Mechanical link | `ResetRestoresEveryBodyAndASavedGovernorLoadsWithItsSpringAndBelt` | — | — |
| G9 | `spring_stiffness` 9.99, 100.01, NaN, +∞ are refused at the editor, input and save decode with nothing changed | `AnOutOfRangeOrNonFiniteStiffnessIsRefusedAtEverySeam` (deleted when S4 row 2 covers it) | 10 and 100 admitted | — |
| G10 | An electrical or activation wire to `DriveIn`, or a second Mechanical link into it, is refused | generic: `MechanicalCoordinateBindingsTests.WorkshopConnections_ValidateControls_RefuseInvalidMechanicalConnections` (`:456`) | — | — |
| G11 | Every art body follows its committed pose and every solid mesh has its collider | `EveryGovernorBodyArtFollowsItsCommittedPose`; the package's row in `VisibleSolidColliderTests` | — | — |

G1 to G3 satisfy the governor part of the cross-element task under [todo-377](../requirements.md#todo-377) ("a governor responds to actual speed", with a visible control).

### Integration (21 November 2026)

- Works with: Battery and Motor (delivered); a contact consumer on the pad such as a ball, a Pressure plate above it or an Impact switch (delivered); EL-110 Flywheel and EL-184 Mechanical ball gate (planned; the gate would be worked by contact, not by a socket).
- Campaign: chapter 9, levels 81–90, reuse 91–100, 131–140, 146–150.
- Coverage binding: a final package-source-binding section in this spec, as EL-239 on published main; blocked on S7 (section 7).
- A level that places it as an authored fixture needs the lesson-fixture story (S8).

### Delivery (21 November 2026)

- **Slice shape.** Single element. Deliver after EL-110, which proves the simpler shaft first.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which includes ENGINE-SUPPLY-DEMAND (the Motor's work debited from its Battery) and ENGINE-ISLANDS-SLEEP; then ENGINE-JOINT-KINDS (dynamic-support hinge, coordinator decision 16) only if the probe fixture fails; then S7 (the coverage follow-on, which widens the reader, owner decision 15). S3 preferred.
- **Files.** Adds `elements/Governor/Governor.csproj`, `GovernorElementDeclaration.cs`, `governor.tscn`, `icon.svg`; `CuriousContraptions.tests/GovernorTests.cs`. Edits `CuriousContraptions.slnx`, the `VisibleSolidColliderTests` table until ENGINE-ART-SOLIDITY, the ledger entry's status and this spec (the binding section once S7 admits it).
- **Review tier.** Engine tier: two capabilities are present-untested.
- **Disqualifies the plan** (the slice's probe runs a declared fixture before the package is written): an arm hinge on the spinning spindle is unstable or gains energy at 20 rad/s; a coupling cannot name a hinge whose support moves; two couplings from one driving coordinate are refused; bodies of one package that share no joint (ball and bevel disc, pad and crossbar) interfere at rest. If either of the first two holds, the missing item is a joint kind qualified on a dynamic support body (a hinge, and a coupling through a hinge, whose support turns), which ENGINE-JOINT-KINDS owns (dynamic-support hinge, coordinator decision 16; it qualifies every joint kind with a dynamic support body as well as a static one); if the third holds, it is an engine defect against a present capability (several couplings sharing one coordinate), repaired in its own slice; if the last holds, the collision rule between bodies of one package is a participation mask of ENGINE-CAPABILITY-INSTANCES. The governor then waits for that story and no governor-specific engine code is written.

## 6. Open questions

None open. Resolved 21 November 2026:

1. Output coupling: by contact, through the bracket pad. A typed linear socket does not exist (mechanical connections are revolute only) and no requirement or campaign row needs one. D19.
2. The governor reports speed by displacement only; it does not regulate the Motor. The requirement row asks for displacement according to actual speed. D19.
3. Arm length 0.3 m, ball 0.5 kg, onset about 3.0 rad/s: starting values, D1.
4. Spindle shaft radius 0.015 m and required link rods drawn by span bindings (22 November 2026, audit H7 and M9).

## Engine conformance (22 November 2026)

Checked against the owner addendum of 22 November 2026 (AGENTS.md element-free engine gate and Tri-Graph rule; `_bmad/custom/element-free-engine-design.md`; `docs/gpu-f32-physics.md`).

1. **Declaration data only.** Six bodies, one slider, four hinges, three declared ratio couplings, one port, one parameter and presentation bindings. No governor law exists in code: no solver branch, callback or engine test on this package.
2. **Generic laws.** Rigid-body dynamics of masses on hinges carried by a turning support (the outward swing is their own inertia), gravity, the slider's declared spring, and bilateral ratio couplings. The lift-against-speed table is a hand consequence of those laws, not a rule the engine evaluates.
3. **Three layers.** Colliders: twelve primitives (boxes 7: base, post, beam, pad, crossbar, two rods; cylinders 3: input wheel, shaft, bevel disc; spheres 2). Visuals: one mesh per collider, bevel teeth as a flat inlay; none is a collider. Animation: the two link rods only, drawn by span bindings between body-bound nodes; they read committed poses, push nothing and write nothing back. Every other art node follows its body's pose.
4. **Flow and typing.** One typed Mechanical input; `spring_stiffness` is an f32 Real in N/m; nothing flows back from presentation.
5. **Flat state.** Bodies 6 (1 static, 5 dynamic); colliders 12; joints 5 (1 prismatic, 4 revolute); couplings 3; drives 0; sensors 0; domain nodes 0; stores 0; presentation bindings 7 (5 body, 2 span).
6. **Performance.** Twelve colliders and five moving bodies, against the Reverse transmission's two wheels. Reason: a working linkage needs the spindle, two arms and the bracket as separate bodies, and each visible solid has a collider. No island sleeping exists today; a resting governor costs nothing only once ENGINE-ISLANDS-SLEEP (owner decision 3a) is delivered, ahead of this part. The rods carry no collider or joint. No engine gap is assumed; if the probe shows a hinge on a turning support is unstable at 20 rad/s, that is an engine defect for ENGINE-JOINT-KINDS (dynamic-support hinge), not a reason for a governor rule.
7. **Tests are scenarios.** Rows G1 to G11 run the production simulation; none needs a code path of this part.

**Verdict: conforms.**

## 7. Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of the identity's spec headed "Package source binding", read by `tools/Coverage/NamedPackageSources.cs` (pattern: EL-239 Baseball on published main). The section is not written here: the reader parses every spec that carries that heading and fails the whole coverage run when a field does not resolve. **Blocked on the coverage follow-on story S7.** S7 widens the reader (owner decision 15), including for companion packages and TH, RAD and GAP identities.

| Field | Planned value |
| --- | --- |
| Package | `governor` |
| Named identity | EL-111, `../invest/named-elements.md#element-111` |
| Acceptance | `todo-377`; no co-anchored sequence task yet |
| Decision | S338, `../invest/scope-corrections.md#s338` |
| Mode records | `Configuration=Adjustable` (`spring_stiffness`) |

What it lacks today: `scope-corrections.md` has no `s338` block on published main; `todo-377` is a scope index shared by EL-110, EL-111, TH-22 and TH-23 with no sequence-task anchor directly after it, which the reader requires; and one task source can bind only one package.
