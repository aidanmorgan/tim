# EL-110 · Flywheel named-identity spec

**Current delivery (24 October 2026):** [EL-110 delivery slice](../invest/named-elements.md#element-110). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 named-identity spec (Batch J). Baseline commit `a6c914e`; every citation is `path@a6c914e:Lstart-Lend` and resolves with `git show a6c914e:<path> | sed -n 'start,endp'`. **Proposed** marks an unsourced design value with a one-line justification; the owner may revise it. All values are canonical IEEE-754 f32 game values inside the [game-grade envelope](../../gpu-f32-physics.md#game-grade-envelope).

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-110 |
| Name | Flywheel |
| Type | Mechanical |
| Anchor | [requirements.md#element-110](../requirements.md#element-110); [named entry](../invest/named-elements.md#element-110); scope index [todo-377](../requirements.md#todo-377) |
| Proof owner | S337 |
| Refines / extends | No CAT spec. Uses the finite-inertia shaft and mechanical ports of [CAT-042 motor](CAT-042-motor.md), [CAT-019 conveyor](CAT-019-conveyor.md) and [CAT-057 reverse transmission](CAT-057-reverse_transmission.md) |
| Related | EL-111 Centrifugal governor (same scope index), EL-199 Electric motor, EL-200 Drive belt |
| Roadmap story | [EL-110 delivery slice](../invest/named-elements.md#element-110); canonical proof owner and prerequisite decisions are recorded there. |
| Status | Not started (no `elements/Flywheel/` package on `8d1d68aa`); blocked on the engine programme (owner decision 3a), relying most on ENGINE-SUPPLY-DEMAND (the Battery and Motor train) and ENGINE-ISLANDS-SLEEP; then ENGINE-RUN-LENGTH (row A1's 60 s Run); then S7. |
| Disposition (21 November 2026) | To deliver as a single element. Planned package id `flywheel`, directory `elements/Flywheel/`. Not a duplicate: no delivered part has a free shaft with a player-set inertia. Listed in the Rotary run of [delivery batches 1b](../invest/delivery-batches.md#1b-runs-each-its-own-slice-planning-reference-only) |

## 2. Declaration

### Current declaration (21 November 2026)

This subsection was checked against this checkout (`8d1d68aa`) and is the whole declaration. The shape follows the Conveyor's relay shaft (`elements/Conveyor/ConveyorElementDeclaration.cs:42`–`:52`, `:80`–`:91`) and the Motor's rotor (`elements/Motor/MotorElementDeclaration.cs:88`–`:116`). Values not sourced from a delivered package are starting values under decision D1.

**Package.** Id `flywheel`, directory `elements/Flywheel/`, class `FlywheelElementDeclaration : IHingeElement, ISceneGeometryElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:20`; `CuriousContraptions.Abstractions/SceneGeometry.cs:10`). Category `ElementCategory.Motion`, palette order 163 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)), display name "Flywheel", description "A heavy wheel that keeps turning." Footprint: 2 bodies, 1 moving, 0 prismatic, 1 revolute. `KinematicMode.Static`, canonical zero base mass frame, `PayloadPolicy.None`, `SupportedDomains` Mechanical. No events, stores, sensors or anchor points.

**Bodies and colliders** (metres, element-local; colliders come from the scene through `SceneBodies`, as in every drivetrain package).

| Body | Shape | Half extents | Centre | Material (restitution, friction, rolling resistance, bounce threshold) |
| --- | --- | --- | --- | --- |
| 0 housing, static | box (base) | 0.5, 0.08, 0.4 | 0, 0, 0 | 0, 0.3, 0, 0.1 |
| 0 housing, static | box (bearing block) | 0.15, 0.35, 0.1 | 0, 0.43, −0.2 | 0, 0.3, 0, 0.1 |
| 1 wheel, dynamic | cylinder, radius 0.5, half-width 0.075, turned Rx(90°) so its axis is the hinge Z | — | hinge origin | 0, 0.3, 0, 0.1 |

The material is the catalogue rigid-part material (`elements/Motor/MotorElementDeclaration.cs:61`–`:65`); it replaces the proposed restitution 0.1, which no delivered rigid part uses. The wheel clears the base by 0.04 m and the block by 0.125 m.

**Hinge 0** (`RevoluteHingeDescriptor`, `CuriousContraptions.Abstractions/MechanicalContractValues.cs:149`): support body 0; base frame at (0, 0.62, 0.1), identity; `RevoluteTravel.Continuous`, limits ±π; stiffness 0, damping 0; friction torque 0.02 N·m (the Conveyor roller's value, `ConveyorElementDeclaration.cs:91`); initial angle 0; `BodiesCollide` false; no drive. Moving body: mass from `mass`; principal moments by `PrincipalMomentOperation.ScaledMassLengthSquared` (`CuriousContraptions.Abstractions/PrincipalMomentDeclaration.cs:6`) with length 0.5 m and coefficients (0.2575, 0.5, 0.2575), which are the solid-disc values (3r² + w²)/(12r²) and ½ for r = 0.5 m, w = 0.15 m; principal frame Rx(90°) as the Motor rotor (`MotorElementDeclaration.cs:111`); gravity (0, −9.81, 0); no drag.

**Parameter.**

| Name | Type | Unit | Default | Minimum | Maximum | Run edit | Editor policy |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `mass` | Real f32 | `ParameterUnit.Kilogram` | 4 | 1 | 8 | `BuildOnly` | out-of-range and non-finite refused, as for every delivered Real parameter |

**Ports.** Both bind revolute coordinate 0 with `CoordinatePerRadian` +1, the delivered convention (`MotorElementDeclaration.cs:53`; `ConveyorElementDeclaration.cs:46`, `:50`). The legacy −1 scale is not carried forward.

| `PortId` | Role | Domain | Direction | Position | Normal | Rim (cosmetic) |
| --- | --- | --- | --- | --- | --- | --- |
| `DriveIn` | `PortRole.DriveIn` | Mechanical | Input | 0, 0.62, −0.33 | −Z | support 0, radius 0.22 |
| `Drive` | `PortRole.Drive` | Mechanical | Output | 0, 0.62, 0.22 | +Z | support 0, radius 0.22 |

A mechanical output coordinate accepts one connection and so does an input (`engine/gpu/WorkshopConnections.cs:182`), so the legacy "an output may fan out" does not hold; a train is built by relaying through `Drive`, as Conveyors do.

**Presentation.** One body binding, `Visual/Wheel` to body 1 (`MotorElementDeclaration.cs:28` is the pattern). No curves or colour bindings.

### Art and solidity (21 November 2026)

| Node | Mesh | Dimensions (m) | Class | Collider or reason |
| --- | --- | --- | --- | --- |
| `Base` | box | 1.0 × 0.16 × 0.8 | solid | static box above |
| `BearingBlock` | box | 0.3 × 0.7 × 0.2 | solid | static box above |
| `Visual/Wheel` | cylinder | radius 0.5, width 0.15 | solid | wheel cylinder on body 1 |
| `Visual/Wheel/IndexBar`, `Visual/Wheel/IndexDot` | flat inlays in the wheel face | bar 0.8 × 0.08, dot radius 0.05, flush | decorative | printed flat |
| `Visual/Wheel/Axle` | cylinder | radius 0.04, from the block face to the wheel | decorative | enclosed between the block and the wheel face |

Colours: wheel `#d69c47` (the Reverse transmission's), navy `#293954` base and index bar, cream `#fff8e9` bearing block, gold `#f7cb52` dot ([DESIGN colour system](../../../DESIGN.md#colour-system)). Icon: one large disc with a thick rim and a single radial bar on a short base. At a glance it differs from the Reverse transmission (two small wheels side by side) and the Motor (small rotor with a lamp) by its single wheel as wide as its base.

### Behaviour and numbers (21 November 2026)

- Axial inertia I = ½·m·r² = 0.125·m kg·m²: 0.125 at 1 kg, 0.5 at 4 kg, 1.0 at 8 kg. Stored energy ½·I·ω²: 9 J at the Motor's default 6 rad/s and 100 J at its 20 rad/s limit (4 kg). All speeds stay far inside the 128 rad/s angular envelope.
- Spin-up from the default Motor (20 N·m, `MotorElementDeclaration.cs:44`): about 0.15 s to 6 rad/s at 4 kg. A Motor at its 0.1 N·m floor has 0.03 N·m left after its own 0.05 N·m and the wheel's 0.02 N·m friction and takes about 100 s; this is the "too weak to charge" boundary.
- Coast-down is linear in time, ω(t) = ω₀ − τ·t/I. Alone from 6 rad/s at 4 kg: 150 s. Joined by Mechanical links to an unpowered Motor (0.05 N·m) and a Conveyor roller (0.02 N·m): about 35 s, against about 2 s for the same train without the flywheel. That ratio is the supply-gap lesson and the reason the friction is small but not zero (owner, 3 November 2026: unpowered shafts spin down).
- The wheel is solid: cargo touching the rim is carried by friction 0.3. No guard is declared.
- No signal, optical or acoustic read is involved, so no tick-order rule applies.

### Harvest-era subsections — removed; the text is in git history.

The current declaration above is the whole declaration (no named variants, [element-110](../requirements.md#element-110)). The wheel's kinetic energy ½·I·ω² is its only store: it charges only through `DriveIn` and gives up energy only as work through `Drive` or contact, never gaining energy without input (no-free-energy rule, [envelope](../../gpu-f32-physics.md#game-grade-envelope)). An 8 kg wheel stores eight times the energy of a 1 kg one at equal speed (I ∝ m), so it bridges a longer supply gap.

## 3. Engine capabilities

### Capability probe (21 November 2026)

The harvest-era "Exists now" and "Missing" lists (baseline `a6c914e`) are removed; their text is in git history. The hinge, the cylinder collider and the mechanical domain they called missing are delivered.

| Capability | Status | Symbol | Exercising test |
| --- | --- | --- | --- |
| Continuous revolute hinge with Coulomb friction torque | present | `RevoluteHingeDescriptor`, `MechanicalContractValues.cs:149` | `RevoluteHingeFrictionTests.AHingedBarTurnsWithTheTorqueLeftAfterItsFrictionTorque` (`:59`); `RevoluteDriveTests.AContinuousHingeTurnsPastTheFullCircleWithNoStopImpulseAndAccumulatesItsTravel` (`:195`) |
| Solid cylinder collider and cylinder moments | present | `ColliderShapeKind.Cylinder`, `CuriousContraptions.Abstractions/ColliderDescriptor.cs:13` | `WorkshopPhysicsLawCylinderTests.CompiledMomentsAreTheSolidCylinderMomentsOfTheDeclaredGeometry` (`:31`) |
| Mechanical connection compiled to a bilateral ratio coupling | present | `engine/gpu/WorkshopPhysicsCompiler.cs:583`–`:599` | `MechanicalCoordinateBindingsTests.PositiveTransmission_DrivingCoordinateTransmitsProportionalAngularMotionToDrivenCoordinate` (`:499`) |
| Relay: `DriveIn` and `Drive` on one coordinate | present | `ConveyorElementDeclaration.cs:44`–`:51` | `ConveyorTests.ChainedConveyors_DriveRelaysRotationToDownstreamConveyor` (`:417`) |
| Load reaction carried back through a train | present | `MechanicalTransmissionDescriptor`, `MechanicalContractValues.cs:258` | `ReverseTransmissionLoadTests.ADeclaredOutputLoadReactsThroughReverseToTheSource` (`:19`) |
| Passive coupling adds no energy | present | same | `MechanicalCoordinateBindingsTests.SharedCarrierUsesOneCombinedBodyJacobian` (`:901`) |
| Committed travel read of an undriven hinge | present | `RevoluteStateRead`, `CuriousContraptions.Abstractions/RevoluteConstraintValues.cs:40` | `MotorTests.InAMixedSceneEachLampReadsItsOwnDrivenSlotAndAnUndrivenHingeHasItsOwnRow` (`:897`) |
| Mechanical link route presentation between rims | present | `MechanicalRimDescriptor`, `CuriousContraptions.Abstractions/MechanicalLinkPresentationValues.cs:10` | `MechanicalLinkPresentationTests.DeclaredRimsProduceClosedTangentRoutesAndCommittedMarkers` (`:29`) |
| Art follows the committed moving body | present | `ElementPresentation` body binding, `MotorElementDeclaration.cs:28` | `MotorTests.TheRotorArtFollowsTheCommittedRotorPoseAndResetsToItsScenePose` (`:1075`) |
| Parameter-bound mass and scaled moments on a hinge-carried body | present, untested | `PrincipalMomentOperation.ScaledMassLengthSquared`, `PrincipalMomentDeclaration.cs:6`; validator path `CuriousContraptions.Bundle/BundleValidator.cs:768` | none: every delivered hinge body declares a constant mass frame; the operation is exercised only on a free body (Tennis ball) |
| Cargo contact against a spinning hinge-carried cylinder | present, untested | scene-body collider on a hinge body, `MotorElementDeclaration.cs:61`–`:65` | none found that asserts contact against a spinning rotor |

Counts: 9 present, 2 present-untested, 0 narrower, 0 missing. Beyond the engine programme (owner decision 3a), the one specific engine story is ENGINE-RUN-LENGTH, for row A1's 60 s Run. Nothing here needs element-specific engine code.

Families: EnvironmentState, FiniteLedger, JointConstraint, RigidBodyDynamics, ShaftTorque, plus StateTransaction ([map row](../general-engine-element-map.md); [element-02.json](../../coverage/engine/element-02.json)).

**Dependencies.** CAT-042 motor (Story 11.1) to charge it; CAT-019 conveyor or CAT-057 reverser as loads; CAT-005 battery to show the supply gap.

## 4. Sources and legacy

- Requirement: "Rotating inertia stores work and coasts under load"; outcome "Stored energy falls as output work is delivered" ([element-110](../requirements.md#element-110)).
- Integration task under [todo-377](../requirements.md#todo-377): "A flywheel bridges a measured supply gap using stored rotational energy ... each has a visible depletion/cooling control." Campaign: ratchet/escapement/flywheel row, first use 81–90, reuse 91–100, 131–140, 146–150 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)).

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | Shaft inertia axial m·r²/2, transverse m·(3r² + w²)/12; mass and dimensions finite and positive | `engine/SceneRotaryShaft.cs@a6c914e:L13-L24` | carry forward | Generic finite-inertia law. |
| 2 | Shaft hinge about local Z, travel both ways, connected collision disabled | `engine/SceneRotaryShaft.cs@a6c914e:L25-L28` | carry forward | Hinge declaration. |
| 3 | 24-sided convex prism shaft collider | `engine/SceneRotaryShaft.cs@a6c914e:L29-L42` | carry forward (shape intent), open | Cylinder primitive is an owner decision. |
| 4 | Accepted: after supply loss an unloaded shaft keeps its speed, total kinetic energy changes ≤ 1e-8, supplied work stays constant | `CuriousContraptions.tests/MechanicalTests.cs@a6c914e:L113-L123` | carry forward (behaviour), do not carry forward (1e-8 literal) | Coasting without hidden damping; tolerance is game-grade. |
| 5 | Link ratio = input coordinate per radian ÷ output coordinate per radian, always engaged | `engine/MechanicalNetwork.cs@a6c914e:L46-L49` | carry forward | Shaft coupling rule. |

No legacy flywheel exists.

**Files harvested:**
- `engine/SceneRotaryShaft.cs`
- `engine/MechanicalNetwork.cs`
- `CuriousContraptions.tests/MechanicalTests.cs`
- `parts/MotorPart.cs` (housing scale)
- `parts/ConveyorPart.cs` (mechanical socket convention)
- `parts/ReverseTransmissionPart.cs` (wheel art and sign convention)
- `parts/WeightPart.cs` (mass range reference)
- Searched with no hit for `flywheel`: `parts/`, `engine/` (including `engine/physics/` and `engine/bridge/`), `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/`, `reference/`, `diagnostics/`.

## 5. Acceptance outline

Follow [element-110](../requirements.md#element-110) and the [mechanics profile](../invest/profiles.md#mechanics).
- **Chrome UI recipe.** Place battery, switch-gated motor, flywheel and conveyor; connect `Supply` → `PowerIn`, motor `Drive` → flywheel `DriveIn`, flywheel `Drive` → conveyor `DriveIn`. Run, then open the supply.
- **Positive.** The flywheel keeps the conveyor carrying a ball for a while after supply loss; its speed falls as the ball is carried.
- **Negative/control.** Without the flywheel the conveyor stops sooner; an unloaded flywheel coasts at constant speed; an unconnected flywheel never moves.
- **Boundaries.** `mass` 1 and 8; stall against a blocked load; reverse rotation via a reverser.
- **Run/Reset.** Wheel angle and speed return to zero.
- **Save/Load.** `mass`, pose and mechanical links round-trip.
- **Integrations.** Flywheel supply-gap integration task under [todo-377](../requirements.md#todo-377) (with EL-111 and TH-22/TH-23); campaign row "Ratchet, escapement, indexed carousel, flywheel ...": first use 81–90, reuse 91–100, 131–140, 146–150 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)); charged by CAT-042 motor, loaded by CAT-019 conveyor.

### Acceptance rows (21 November 2026)

Native tests through the Workshop integration harness, class `FlywheelTests` unless stated. Speeds are compared with ±0.15 rad/s, which is the 0.1 rad/s coupled-speed allowance the transmission tests use plus the friction loss of one sample; a wrong inertia or a missing friction term moves the result by more than 0.5 rad/s. Stall guards are game seconds. No arrival tick, trajectory or hash is asserted.

| # | Given / When / Then | Test | Control | Boundary |
| --- | --- | --- | --- | --- |
| A1 | Given Battery → Motor (`speed` 1 rad/s) → Flywheel → Conveyor (`length` 10 m) carrying a Tennis ball, when the supply is cut at speed, then the roller turns at least 4 times as far after the cut as in the control and the ball moves further (expected ratio about 15). Guard 60 s; Run length 60 s (ENGINE-RUN-LENGTH, owner decision 13) | `AFlywheelKeepsTheConveyorCarryingAfterTheSupplyIsCut` | the same train without the flywheel | `mass` 1 kg still exceeds the control by 2 times |
| A2 | Source outcome. Given Battery → Motor → Flywheel at 6 rad/s (wheel energy ½·I·ω² = 9.0 J at the default 4 kg, I = 0.5 kg·m²) with a Wound spring on `Drive`, when the supply is cut and the spring winds, then at every sample the wheel's ½·I·ω² is lower than in the control by at least the spring's stored ½·k·x² (from its committed stroke) within the suite's energy envelope, and it never rises between samples by more than 1 % | `StoredEnergyFallsAsOutputWorkIsDelivered` | the same Motor and Flywheel with nothing on `Drive`: over 5 s it loses only bearing friction (0.02 N·m on the wheel and the unpowered Motor's own, about 2 J over 30 rad) | — |
| A3 | Given an unconnected flywheel, when Run lasts 5 s, then its travel is 0 (an intentional zero: nothing can apply a torque, so any motion is a defect) | `AnUnconnectedFlywheelNeverMoves` | A1 | — |
| A4 | Given `mass` 1 and 8, when each is spun to 6 rad/s and cut with the same load, then after 5 s the 8 kg wheel is faster than the 1 kg wheel by at least 1 rad/s | `AHeavierWheelCoastsLonger` | — | parameter minimum and maximum |
| A5 | Given the Wound spring of A2 wound to its stroke stop, when the supply is cut, then wheel and spring shaft stop together and no energy sample rises | `ABlockedLoadStopsTheWheelWithoutCreatingEnergy` | A2 | stall |
| A6 | Given a Reverse transmission between Motor and Flywheel, when supplied, then the wheel's travel is negative and its speed magnitude matches A1 | `AReverserTurnsTheWheelTheOtherWay` | A1 | sign |
| A7 | Given a run with the wheel spinning, when Reset, then angle and speed are the authored zero; a saved construction loads with `mass`, pose and both connections | `ResetRestoresTheWheelAndASavedFlywheelLoadsWithItsMassAndLinks` | — | `mass` 1 and 8 round-trip |
| A8 | Given `mass` 0.99, 8.01, NaN and +∞, when set through the editor, input and save decode, then each is refused with its typed reason and nothing changes | `AnOutOfRangeOrNonFiniteMassIsRefusedAtEverySeam` (deleted when the S4 conformance row 2 covers it) | 1 and 8 admitted | — |
| A9 | Given an electrical or activation wire drawn to `DriveIn`, or a second Mechanical link into it, then the edit is refused | covered generically by `MechanicalCoordinateBindingsTests.WorkshopConnections_ValidateControls_RefuseInvalidMechanicalConnections` (`:456`) and `ClutchGeneralityTests.OccupiedMechanicalInputIsRefusedWhenLoadingSavedConstruction` (`:632`); the slice adds the flywheel as a row only if those are not package-generic | — | — |
| A10 | The wheel art follows the committed wheel pose and every solid mesh has its collider | `TheWheelArtFollowsTheCommittedWheelPose`; the package's row in `VisibleSolidColliderTests` | — | — |

Generic Run/Reset/save admission is also covered by `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad`. The cross-element task under [todo-377](../requirements.md#todo-377) (flywheel bridges a measured supply gap, with a visible depletion control) is satisfied for the flywheel by A1 and A2; its governor and thermal parts stay open with EL-111, TH-22 and TH-23.

### Integration (21 November 2026)

- Works with: Battery, Motor, Conveyor, Reverse transmission, Clutch (all delivered); EL-111 Centrifugal governor and EL-200/EL-201 belt and chain kinds (planned, not prerequisites).
- Campaign: chapter 9, levels 81–90 (first use), reuse 91–100, 131–140, 146–150 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)).
- Coverage binding: a final package-source-binding section in this spec (Package, Named identity, Acceptance, Decision, Mode records), as EL-239 on published main; blocked on S7 (section 7). No catalogue number.
- A level that places the flywheel as an authored fixture needs the lesson-fixture story (S8); inventory use does not.

### Delivery (21 November 2026)

- **Slice shape.** Single element.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which includes ENGINE-SUPPLY-DEMAND (the Motor's work debited from its Battery) and ENGINE-ISLANDS-SLEEP; then ENGINE-RUN-LENGTH (row A1 needs a 60 s Run, owner decisions 13 and 17); then S7 (the coverage follow-on, which widens the reader, owner decision 15). S3 is preferred, not required; without it the slice also edits the trim-analysis list and the placement switches named in `spec-s3-remaining-per-package-lists.md`. No other element.
- **Files.** Adds `elements/Flywheel/Flywheel.csproj`, `FlywheelElementDeclaration.cs`, `flywheel.tscn`, `icon.svg`; `CuriousContraptions.tests/FlywheelTests.cs`. Edits `CuriousContraptions.slnx` (one line, kept explicit by S3 decision O1), the `VisibleSolidColliderTests` hand table until ENGINE-ART-SOLIDITY, the ledger entry's status, and this spec (the binding section once S7 admits it). The test project compiles every top-level `*.cs` by one rule. Registry, bundle, palette, capacity, coverage membership and impact mapping are derived (`spec-registration-derivation.md`).
- **Review tier.** Engine tier (all six layers), because two capabilities are present-untested. If the slice's probe finds an existing test for both, it is data-only.
- **Disqualifies the plan** (the slice's probe re-checks): the bundle refuses a parameter-bound mass frame on a hinge body; a train of three bilateral couplings gains energy in A2; a ball resting on the spinning rim is ejected with more energy than the rim's surface speed allows. Each is an engine defect to fix in its own slice first, not a reason for flywheel-specific code.

## 6. Open questions

None open. Resolved 21 November 2026:

1. Bearing friction: declared, 0.02 N·m. Reason: the owner's 3 November 2026 decision that an unpowered Motor spins down applies the same intent to every shaft, and a visible depletion control is required by todo-377. Decided under the standing delegation.
2. `mass` is the only player control; radius is fixed. Decision D13.
3. The rim collides with cargo. Owner rule visible = solid (25 October 2026).
4. A2 and A5 load the wheel with a Wound spring (22 November 2026, audit M5 and M6). Reason: the wheel has no drive of its own and a Mechanical link cannot be removed during Run, so an "alone" control could not be built; and a Conveyor moves cargo through a driven surface, so a Wall-held box does not block its roller.

## Engine conformance (22 November 2026)

Checked against the owner addendum of 22 November 2026 (AGENTS.md element-free engine gate and Tri-Graph rule; `_bmad/custom/element-free-engine-design.md`; `docs/gpu-f32-physics.md`).

1. **Declaration data only.** A static housing, one hinge-carried wheel, two mechanical ports, one parameter-bound mass frame and one body binding. No solver branch, callback or engine test on this package.
2. **Generic laws.** Rigid-body rotation with the declared inertia, the revolute hinge's declared friction torque, and the bilateral ratio coupling of a mechanical connection. "Stores energy" is ½·I·ω² of an ordinary body; there is no store object and no depletion rule.
3. **Three layers.** Colliders: base box and bearing-block box on the static body, one cylinder on the wheel body. Visuals: base, block and wheel meshes with flush inlaid marks; none is a collider, and each visible solid (base, block, wheel) has its collider. Animation: none. `Visual/Wheel` follows body 1's committed pose; nothing else moves.
4. **Flow and typing.** Two typed Mechanical ports on revolute coordinate 0; `mass` is an f32 Real in kilograms; nothing flows back from presentation.
5. **Flat state.** Bodies 2 (1 static, 1 dynamic); colliders 3 (boxes 2, cylinder 1); joints 1 revolute; drives 0; sensors 0; domain nodes 0; stores 0; presentation bindings 1 (body).
6. **Performance.** Three primitive colliders, as the Motor. No island sleeping exists today; a resting wheel costs nothing only once ENGINE-ISLANDS-SLEEP (owner decision 3a) is delivered, ahead of this part. No collider or joint is cosmetic.
7. **Tests are scenarios.** Rows A1 to A10 run the production simulation with delivered parts and need no code path of this part.

**Verdict: conforms.**

## 7. Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of the identity's spec headed "Package source binding", read by `tools/Coverage/NamedPackageSources.cs` (pattern: EL-239 Baseball on published main). The section is not written here: the reader parses every spec that carries that heading and fails the whole coverage run when a field does not resolve. **Blocked on the coverage follow-on story S7.** S7 widens the reader (owner decision 15), including for companion packages and TH, RAD and GAP identities.

| Field | Planned value |
| --- | --- |
| Package | `flywheel` |
| Named identity | EL-110, `../invest/named-elements.md#element-110` |
| Acceptance | `todo-377`; no co-anchored sequence task yet |
| Decision | S337, `../invest/scope-corrections.md#s337` |
| Mode records | `Configuration=Adjustable` (`mass`) |

What it lacks today: `scope-corrections.md` has no `s337` block on published main; `todo-377` is a scope index shared by EL-110, EL-111, TH-22 and TH-23 with no sequence-task anchor directly after it (`sequence-task-663` stands at its own anchor, `docs/planning/requirements.md:5277`), which the reader requires; and one task source can bind only one package.
