# CAT-018 · clutch declaration readiness spec

**Current delivery (24 October 2026):** [CAT-018 delivery owner](../../../_bmad-output/planning-artifacts/epics.md#catalogue-slice-ownership). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 CAT-018-D readiness spec. Baseline commit `a6c914e`. Legacy citations use `path@a6c914e:Lstart-Lend` and stay retrievable after the Epic 7 purge. Current-engine files are cited at the same commit. Current authority for the shaft model is the [rotary transmission contract](../../rotary-transmission-parts.md). This page records the declaration knowledge the legacy holds; it does not restate the requirement row.

## 1. Identity

| Field | Value |
| --- | --- |
| CAT ID | CAT-018 |
| Kind | `clutch`, catalogue title "Clutch", category "Power" (`parts/catalog/clutch.tres@a6c914e:L6-L8`) |
| Requirement anchor | [CAT-018](../requirements.md#current-cat-018); retained behaviour [sequence-task-310](../requirements.md#sequence-task-310); shared rows [sequence-task-192](../requirements.md#sequence-task-192), [sequence-task-193](../requirements.md#sequence-task-193), [sequence-task-277](../requirements.md#sequence-task-277), [sequence-task-280](../requirements.md#sequence-task-280) |
| Mapped identities | None. No EL/TH/RAD/GAP names a clutch. [EL-055 Mechanical brake](../invest/named-elements.md#element-055) and [TH-05 Friction brake](../invest/named-elements.md#thermal-05) are separate elements; sequence-task-310 keeps brake and slip/torque-limiting variants separate. |
| Roadmap story | [11.2a Clutch](../../../_bmad-output/planning-artifacts/epics.md#catalogue-slice-ownership); retain every CAT-018 mode and mapped named-identity obligation. |
| Status (22 November 2026) | **Delivered.** Package id `clutch`, directory `elements/Clutch`, palette category Power, order 38 (`elements/Clutch/ClutchElementDeclaration.cs:54`–`:59`). Slice record: [11.2a Clutch and timed mechanical transmission](../../../_bmad-output/implementation-artifacts/spec-11-2a-clutch.md) (status done; commit `e9b7f794`, 17 November 2026). Residuals (section 8): two proud shaft spokes and the exposed axle have no collider; coil drain. Every residual slice is blocked on the engine programme (owner decision 3a, 22 November 2026); the stories that own this part's gaps are `ENGINE-SUPPLY-DEMAND` (coil drain) and `ENGINE-ISLANDS-SLEEP` (resting shafts). Browser qualification pending. Checked on `8d1d68aa`; published main is now `25df3cd5`, and its two extra commits touch none of this package's files. |

## 2. Declaration

### Delivered declaration (22 November 2026)

Read from `elements/Clutch/ClutchElementDeclaration.cs` and `elements/Clutch/clutch.tscn`.

- **Interfaces.** `IHingeElement`, `IMechanicalTransmissionElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:20`, `:90`), `ISupplyElement` (`CuriousContraptions.Abstractions.Electrical/ElectricalContracts.cs:11`), `ISceneGeometryElement` (`:15`). Footprint: 3 bodies, 2 moving, 2 revolute (`:67`).
- **Static housing colliders (scene).** Base box 1.9 × 0.16 × 1.1 m at (0, −0.48, 0); two bearing blocks 0.35 × 0.7 × 0.65 m at (±0.65, −0.1, 0); coil cylinder r 0.38 m, 0.065 m long, axis X, at the origin; `PowerIn` stud sphere r 0.1 m at (0, −0.35, 0.5); lamp sphere r 0.075 m at (0, 0.4, 0.18).
- **Two shafts (dynamic).** Cylinders r 0.28 m, 0.13 m wide, axis Z, at (±0.65, 0, 0.43); each 0.25 kg with moments (0.00525, 0.0098, 0.00525) kg·m² (`:17`–`:19`, `:118`–`:144`).
- **Material, all three bodies.** Restitution 0, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s (`:87`–`:92`).
- **Hinges.** Two continuous revolutes about +Z, no spring, no drive, bearing friction 0.02 N·m each (`:112`–`:144`).
- **Transmission.** One bilateral coupling, ratio +1, from coordinate 0 to coordinate 1, closing over `close_seconds` (`:146`–`:150`); it couples only in phase `Engaged`.
- **Parameter.** `close_seconds`: `Real`, seconds, minimum 0.05, maximum 2, default 0.2, build-only (`:64`).
- **Ports (`:69`–`:81`).**

  | `PortId` | Role | Domain | Direction | Position (m), normal | Binding |
  | --- | --- | --- | --- | --- | --- |
  | `DriveIn` | `PortRole.DriveIn` | Mechanical | Input | (−0.65, 0, 0.55), +Z | coordinate 0, +1; rim r 0.28 m |
  | `Drive` | `PortRole.Drive` | Mechanical | Output | (0.65, 0, 0.55), +Z | coordinate 1, +1; rim r 0.28 m |
  | `PowerIn` | `PortRole.PowerIn` | Electrical | Input | (0, −0.35, 0.5), +Z | supply consumer whose target is the transmission (`:152`–`:154`) |

  Signs: +1 per radian on Motor, Conveyor, Clutch and Windmill; −1 on both Reverse gear ports and on the Wound spring's and Fan's `DriveIn`.

  Engagement classes (planned declaration data, [EL-201](EL-201-drive-chain.md#2-declaration), owned by `ENGINE-MECHANICAL-LINKS`): `DriveIn` and `Drive` admit Smooth and Toothed, so either takes a Drive belt or a Drive chain. The delivered rigid link stays admitted at every port and is unchanged (owner decision 2).
- **Electrical node.** `("coil", Consumer, 12 V, 7.2 Ω, 0 F, 20 W)` (`:156`–`:159`): validated as finite and read by no law (only `Role` is read): unread dead data, to be consumed by `ENGINE-SUPPLY-DEMAND` or removed forward-only. Supply is on/off.
- **Stores, events.** None. Phase and closure are the transmission's committed state, not a store.
- **Presentation (`:24`–`:51`).** Pick radius 1.1 m. Plates slide on `LocalX` with `CommittedTransmissionClosure` (±0.25 → ±0.09 m); each plate's `Spin` turns with `CommittedRevoluteAngle` of its hinge; the lamp takes one colour per phase from `CommittedTransmissionPhase` (slate Open and Opening, cyan Closing, gold Engaged); both shaft nodes follow their bodies.

### Art
- Catalogue colour (0.4, 0.72, 0.79); navy base and axle, cream blocks and shafts, gold plates, spokes and stud, cyan coil. Form follows [DESIGN.md clutch](../../../DESIGN.md#electrically-controlled-clutch). Icon `elements/Clutch/icon.svg`.
- **Art and solidity as shipped (`elements/Clutch/clutch.tscn`; table row `CuriousContraptions.tests/VisibleSolidColliderTests.cs:112`–`:117`).**

  | Node | Mesh | Class | Note |
  | --- | --- | --- | --- |
  | `Visual_Base`, `Visual_BlockLeft`, `Visual_BlockRight`, `Visual_Coil`, `Visual_Stud`, `Lamp` | boxes, cylinder, spheres | solid | collider of the drawn shape on the housing |
  | `InputShaft/Visual_InputShaft`, `OutputShaft/Visual_OutputShaft` | cylinders r 0.28 m, 0.13 m | solid | collider on each shaft body |
  | `PlateLeft/*`, `PlateRight/*` | disc r 0.3 m, 0.18 m long; bar 0.19 × 0.43 × 0.04 m | decorative | moved by the `LocalX` closure binding and turned by the spin binding, which a static collider cannot follow |
  | `InputShaft/Visual_InputSpoke`, `OutputShaft/Visual_OutputSpoke` | boxes 0.44 × 0.065 × 0.04 m at shaft-local z 0.08 | **defect: 35 mm proud of the shaft face (z 0.065), no collider** | each rides its shaft's hinge body with no binding. Repair: reprint flat, keeping the node names (`ClutchArtTests` reads them, `ClutchTests.cs:1504`–`:1505`). Owner: repair part D (coordinator decision 33) |
  | `Visual_Axle` | cylinder r 0.055 m, 1.3 m long, axis X, at the origin | **defect: drawn solid, no collider** | only its ends are inside the bearing blocks (\|x\| ≥ 0.475 m) and its middle 0.065 m inside the coil; with the plates open it is directly visible over \|x\| 0.0325–0.16 m and 0.34–0.475 m. Repair: a static cylinder collider of the drawn size. Owner: repair part D (coordinator decision 33) |

  Repair (decided under the standing delegation, by the precedent of part B): reprint both spokes flat on their shaft faces at most 0.5 mm proud, and give the axle a static cylinder collider of its drawn size on the housing. Both are scene data, owned by visible-solid repair part D (Clutch, Reverse gear, Motor index bar and the Counter's sockets, foot and face panel; coordinator decision 33), which runs after the engine programme; see section 8.

### Catalogue and inventory entry
- Id `clutch`, title "Clutch", category Power, order 38 (`elements/Clutch/ClutchElementDeclaration.cs:54`–`:59`). No level in `content/puzzles.json` places or stocks it.

## 3. Engine capabilities

The capability families are in the [general-engine map row](../general-engine-element-map.md) and the [catalogue coverage](../../coverage/catalogue-elements.json). They are not duplicated here.

**Capability probe (22 November 2026, this checkout).** No engine code names the Clutch. Tests are in `CuriousContraptions.tests/`; they were found by name and line, and none was run.

| # | Capability the behaviour uses | Status | Symbol | Exercising test |
| --- | --- | --- | --- | --- |
| 1 | Two passive continuous hinges with bearing friction and cylinder bodies | present | `RevoluteHingeDescriptor` (`CuriousContraptions.Abstractions/MechanicalContractValues.cs:149`) | `ClutchTests.MidRunCoilCut_UncouplesImmediatelyPreservesOutputMomentumAndOpensLinearly` (`ClutchTests.cs:567`), where each uncoupled shaft keeps its momentum under its own bearing friction |
| 2 | Timed coupling of two coordinates with phases Open, Closing, Engaged, Opening | present | `MechanicalTransmissionDescriptor.CloseSeconds`, `TransmissionEngagementPhase` (`MechanicalContractValues.cs:258`, `:247`), `MechanicalTransmissionSolver` (`CuriousContraptions.Simulation/MechanicalTransmissionSolver.cs:29`) | `ClutchTests.PositiveSpinUp_StaysDecoupledWhileClosingThenEngagesWithinWindowAndDrivesConveyor` (`:244`), `EqualInertiaDeclaredClosure_SplitsSpinHalfAndNeverCreatesKineticEnergy` (`:317`); `ClutchTests.GenericDurationUsesClockTimeBeyondProductBounds` (`ClutchGeneralityTests.cs:89`) |
| 3 | On/off supply from `PowerIn` to the transmission | present | `SupplyTransmissionTarget` (`CuriousContraptions.Abstractions/SupplyContractValues.cs:53`) | `ClutchTests.UnpoweredControls_KeepOutputIdle_AndInvalidPortWiresAreRefused` (`:502`), `MidRunCoilCut_UncouplesImmediatelyPreservesOutputMomentumAndOpensLinearly` (`:567`) |
| 4 | Mechanical input and output ports on separate coordinates; one link per role | present | `WorkshopConnections.ValidateMechanical` (`engine/gpu/WorkshopConnections.cs:170`) | `ClutchTests.RotatedMounts_PreserveDomainsAndSign_ClosedLoopStaysAtRest_AndDuplicateDriveInRejected` (`:807`); `ClutchTests.OccupiedMechanicalInputIsRefusedWhenLoadingSavedConstruction` (`ClutchGeneralityTests.cs:632`) |
| 5 | Presentation from committed closure, phase and hinge angle | present | `PresentationDriver.CommittedTransmissionClosure`, `CommittedTransmissionPhase`, `CommittedRevoluteAngle` (`CuriousContraptions.Abstractions/ElementPresentation.cs:28`–`:30`) | `ClutchArtTests.ClutchScene_AnimatesPlatesLampAndShaftSpokesAcrossLifecycleAndResetsCleanly` (`ClutchTests.cs:1448`); `ClutchTests.FourPhasesAreIndependentlyMappedAndCosmeticsDoNotControlCoupling` (`ClutchGeneralityTests.cs:54`) |
| 6 | Parameter bounds; Reset; save and load | present | `ParameterDescriptor` (`ClutchElementDeclaration.cs:64`) | `ClutchTests.CloseSeconds_RefusesInvalidValuesAcrossParametersInputAndSaveDecode` (`:791`), `CloseSeconds_AdmitsExactEndpoints` (`:775`), `RunResetAndSaveLoad_RestoreAuthoredStateAndReproduceCoupling` (`:886`) |
| 7 | Scene colliders match the drawn solids | present | `ColliderShapeKind.Cylinder` (`CuriousContraptions.Abstractions/PhysicsDeclarationValues.cs:12`) | `VisibleSolidColliderTests.EverySolidArtPartHasItsColliderAndNoColliderReachesBeyondTheArt` (`:237`, `clutch` row `:112`), which lists the spokes and axle as decorative (the defects under Art) |
| 8 | The coil's supply debiting its source (drain) | **missing** | supply is binary (section 6, decision 7) | `ENGINE-SUPPLY-DEMAND` (sprint key `engine-supply-drive-demand`; owner decision 3a, 22 November 2026) |

Counts: 7 present, 0 untested, 0 narrower, 1 missing.

**Element dependencies.** Motor (upstream drive), Battery (coil supply), Conveyor (downstream load), Reverse gear (signed-drive control); all delivered.

## 4. Legacy harvest

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | Phases Open, Closing, Engaged, Opening as an enum | `parts/ClutchPart.cs@a6c914e:L9-L9` | carry forward | Matches the requirement and is a closed enum |
| 2 | Closure moves linearly toward 1 when supplied and toward 0 otherwise, at rate 1/close_seconds per second. The phase is derived from supply and closure (Engaged only at closure = 1, Open only at 0). | `parts/ClutchPart.cs@a6c914e:L50-L55` | carry forward (law); do not carry forward (`PreparePhysics` per-part update) | A per-element update loop is forbidden; declare the closure as a generic timed-phase law |
| 3 | The coupling is engaged only when the phase is Engaged. Supply loss sets Opening and opens the coupling on the next step while the plates keep opening. | `parts/ClutchPart.cs@a6c914e:L20-L20`; `CuriousContraptions.tests/ClutchTests.cs@a6c914e:L111-L113` | carry forward | Matches the contract |
| 4 | Engagement change was applied by replacing the joint list in the CPU world | `parts/ClutchPart.cs@a6c914e:L57-L64` | do not carry forward | CPU solver path; use a typed engagement lane in the declared coupling row |
| 5 | Coupling is phase-free, velocity-level, output = ratio × input, with no positional stop and no copied speed | `engine/physics/PhysicsTransmissionJoint.cs@a6c914e:L9-L13`, `engine/physics/PhysicsTransmissionJoint.cs@a6c914e:L66-L73` | carry forward (law); do not carry forward (double-precision CPU joint class) | f32 TGS Soft row in the shared solver |
| 6 | Acceptance: engaged closure between a moving and a stationary shaft shares speed without energy gain. With equal inertia, speed halves; KE after ≤ KE before + 1e−8. | `CuriousContraptions.tests/RotaryTransmissionPartTests.cs@a6c914e:L85-L102` | carry forward (re-freeze tolerances under f32) | Restated as an acceptance fact |
| 7 | Acceptance: after opening, an impulse on the input does not change the output speed | `CuriousContraptions.tests/RotaryTransmissionPartTests.cs@a6c914e:L103-L111` | carry forward | Restated as an acceptance fact |
| 8 | Acceptance: an open clutch transfers no impulse; output speed stays 0. Axis-aligned and rotated (25°, 40°, 15°) cases both apply. | `CuriousContraptions.tests/RotaryTransmissionPartTests.cs@a6c914e:L35-L65` | carry forward | Rotated control |
| 9 | Acceptance: both coil and mechanical input are required. Missing either gives output 0. Reversed drive gives a negative output. Connection order does not matter. | `CuriousContraptions.tests/ClutchTests.cs@a6c914e:L35-L80` | carry forward | Positive and negative matrix |
| 10 | Output is 0 on the first step because the plates are not yet closed | `CuriousContraptions.tests/ClutchTests.cs@a6c914e:L69-L70` | carry forward | Engages only when fully closed |
| 11 | The legacy motor fixture reached steady shaft speed 6 (rad/s) within 120 steps | `CuriousContraptions.tests/ClutchTests.cs@a6c914e:L71-L74`, `CuriousContraptions.tests/ClutchTests.cs@a6c914e:L180-L181` | do not carry forward | Ideal regulated motor speed; the current motor is bounded effort, not assigned velocity ([CAT-042](../requirements.md#current-cat-042)) |
| 12 | Acceptance: after power loss, a load impulse on the disconnected output does not affect the input. The output coasts at its own speed, and the downstream conveyor shaft equals the clutch output. | `CuriousContraptions.tests/ClutchTests.cs@a6c914e:L110-L128` | carry forward | Independent signed momentum |
| 13 | Acceptance: re-supply enters Closing with the coupling still Open; output momentum is unchanged until full closure, after which input = output | `CuriousContraptions.tests/ClutchTests.cs@a6c914e:L129-L135` | carry forward | Re-engagement control |
| 14 | Frame-rate presentation (`_Process`) must not advance closure | `CuriousContraptions.tests/ClutchTests.cs@a6c914e:L125-L125` | carry forward | Presentation observes committed state |
| 15 | Acceptance: a clutch→belt→clutch loop is admitted and stays at rest. A second input belt into an occupied DriveIn is rejected at connect and at load. | `CuriousContraptions.tests/ClutchTests.cs@a6c914e:L141-L162`; `engine/MechanicalNetwork.cs@a6c914e:L35-L35` | carry forward | Topology rule; conflicts with an older loop ban (see Open questions) |
| 16 | Acceptance: whole-part rotation about x, y or z by 90° keeps the port domains (Mechanical, Mechanical, Electrical) and the drive sign | `CuriousContraptions.tests/ClutchTests.cs@a6c914e:L166-L185` | carry forward | Rotated mounting |
| 17 | Acceptance: a timed supply (hold timer) engages and then releases the clutch while the motor keeps running; the output keeps its momentum after release | `CuriousContraptions.tests/ClutchTests.cs@a6c914e:L186-L206` | carry forward | Timed-release integration |
| 18 | Acceptance: closure increments are never more than tick/close_seconds. Engagement time is close_seconds within one tick early or two ticks late, for 0.05, 0.2 and 2 s. | `CuriousContraptions.tests/ClutchTests.cs@a6c914e:L212-L232` | carry forward (bound); do not carry forward (legacy tick) | Re-express against the 120 Hz worker tick |
| 19 | close_seconds of 0, 3 and NaN are rejected at placement | `CuriousContraptions.tests/ClutchTests.cs@a6c914e:L238-L246`, `parts/ClutchPart.cs@a6c914e:L45-L49` | carry forward | Atomic rejection |
| 20 | Reset restores Open, closure 0 and zero angles; a save/load round trip reproduces the run | `CuriousContraptions.tests/ClutchTests.cs@a6c914e:L81-L87`, `CuriousContraptions.tests/ClutchTests.cs@a6c914e:L233-L234` | carry forward | Run/Reset and Save/Load |
| 21 | A failed tick restores activation, power, visibility, phase, closure and body state | `CuriousContraptions.tests/PartRuntimeCheckpointTests.cs@a6c914e:L44-L82` | carry forward; deferred to the injected-fault gate | The worker owns rollback; the legacy transaction mechanism is not carried forward |
| 22 | Shaft readings come from current physics with no observer callback, and return to 0 after restore | `CuriousContraptions.tests/PartRuntimeCheckpointTests.cs@a6c914e:L104-L144` | carry forward | Committed-state presentation |
| 23 | With one motor energy supply, total energy is at most supplied work plus lost height. An open clutch leaves the load at rest; an engaged one makes the load speed equal the motor speed within 1e−5. After supply loss, supplied work stays constant. | `CuriousContraptions.tests/MechanicalWorkTests.cs@a6c914e:L39-L93` | carry forward (re-freeze tolerances under f32) | Energy bound |
| 24 | Coupling rejects ratio 0, NaN and ∞, identical endpoints, ball-socket endpoints and undefined engagement | `CuriousContraptions.tests/TransmissionJointTests.cs@a6c914e:L124-L154` | carry forward | Admission rejection |
| 25 | Shaft hinges and the coupling must be replaced together; a partial replacement is rejected and the state is unchanged | `CuriousContraptions.tests/TransmissionJointTests.cs@a6c914e:L100-L122` | carry forward | Atomic compile |
| 26 | Catalogue text: "Uses ideal shaft speeds, without torque, slip or freewheel inertia" | `parts/catalog/clutch.tres@a6c914e:L9-L9` | do not carry forward | Superseded by the finite-inertia requirement; rewrite the description |
| 27 | Parameter key strings resolve through `PartParameterName.Of(ClutchParameter.CloseSeconds)` = "close_seconds", and an undefined enum value is rejected | `CuriousContraptions.tests/PartParameterNameTests.cs@a6c914e:L35-L35`, `CuriousContraptions.tests/PartParameterNameTests.cs@a6c914e:L50-L50` | carry forward | Serialization boundary name for an enum key |

### Files harvested
- `parts/ClutchPart.cs`
- `parts/catalog/clutch.tres`
- `parts/scenes/clutch.tscn`
- `reference/cpu/MachinePart.cs` (AddBox semantics only)
- `ui/WorkshopIcons.cs` (pictogram; not deleted by Epic 7)
- `engine/SceneRotaryShaft.cs`
- `engine/SceneTransmissionJoint.cs`
- `engine/physics/PhysicsTransmissionJoint.cs`
- `engine/MechanicalNetwork.cs`
- `engine/ConnectionPort.cs`
- `engine/MachineData.cs` (SocketId enum, L104-L108)
- `engine/physics/JointEquations.cs` (FrameJointKind, L5)
- `CuriousContraptions.tests/ClutchTests.cs`
- `CuriousContraptions.tests/RotaryTransmissionPartTests.cs`
- `CuriousContraptions.tests/TransmissionJointTests.cs`
- `CuriousContraptions.tests/PartRuntimeCheckpointTests.cs`
- `CuriousContraptions.tests/MechanicalWorkTests.cs`
- `CuriousContraptions.tests/PartParameterNameTests.cs`
- `content/puzzles.json` (checked, no clutch level)
- `engine/bridge/` (checked, no element knowledge)
- `tools/Campaign/Program.cs` (checked, no clutch lesson)
- `reference/P0-022-before/docs/coverage/engine/*.json` (checked, coverage snapshots only, no element knowledge)
- `tools/P0-007-actual-path-probe/inputs.json` (checked, file list only)

## 5. Acceptance outline

Follow [CAT-018](../requirements.md#current-cat-018), [sequence-task-310](../requirements.md#sequence-task-310) and Story 11.2. The Chrome observables are in the [rotary contract](../../rotary-transmission-parts.md#chrome-observable-acceptance-post-catalogue).

- **Construction (actual UI).** Place Battery, Motor, Clutch and Conveyor from the toolbox. Wire Battery Supply → Motor PowerIn and Battery Supply → Clutch PowerIn. Join Motor Drive → Clutch DriveIn and Clutch Drive → Conveyor DriveIn with Mechanical links. Place a ball on the belt. Verify placed poses and typed connections in the construction read.
- **Positive.** The plates close over close_seconds, then the output turns with the input and the belt carries the ball.
- **Negative/control.** Remove the coil supply and the output stays at rest while the input turns. Remove the motor supply and nothing turns. A disconnected-output impulse control is required.
- **Boundaries.** close_seconds of 0.05, 0.2 and 2 are admitted; 0, 3 and NaN are rejected. Rotated mounting. Reversed upstream via CAT-057. Re-engagement under load.
- **Run/Reset.** Exact restore of the Open phase, closure 0 and zero angles.
- **Save/Load.** Rotated construction including close_seconds.
- **Integrations.** Hold timer or switch supply release, conveyor load, reverse transmission.

**Native tests that exist (22 November 2026).** Each criterion of [CAT-018](../requirements.md#current-cat-018) against its test in `CuriousContraptions.tests/ClutchTests.cs` unless another file is named:

| Criterion | Test (line) |
| --- | --- |
| Separate coil and upstream shaft; four phases | `ClutchAdmission_StartsOpenUncoupledWithZeroClosureAndUnpowered` (211); `PositiveSpinUp_StaysDecoupledWhileClosingThenEngagesWithinWindowAndDrivesConveyor` (244) |
| `close_seconds` 0.05 to 2, default 0.2; engages only fully closed | `CloseSeconds_ClosesWithinWindowAcrossCadences` (743); `CloseSeconds_AdmitsExactEndpoints` (775); `CloseSeconds_RefusesInvalidValuesAcrossParametersInputAndSaveDecode` (791) |
| +1 coupling cannot create energy | `EqualInertiaDeclaredClosure_SplitsSpinHalfAndNeverCreatesKineticEnergy` (317); `HardwareClutchClosureUnderDisabledMotor_LocksSharedSpinWithoutEnergyCreation` (440) |
| Supply loss disconnects work at once while the plates finish opening; each shaft keeps its momentum | `MidRunCoilCut_UncouplesImmediatelyPreservesOutputMomentumAndOpensLinearly` (567); `ReverseTransmissionTests.OpeningTheDownstreamClutchIsolatesItsCoastingOutput` (`ReverseTransmissionLifecycleTests.cs:121`) |
| Missing either supply; wrong-kind wires | `UnpoweredControls_KeepOutputIdle_AndInvalidPortWiresAreRefused` (502) |
| Re-engagement under load | `ReengagementAfterPartialOpening_ClosesFromCurrentClosureAndLocksUnderLoad` (659) |
| Rotated controls; an open route does not hide invalid topology | `RotatedMounts_PreserveDomainsAndSign_ClosedLoopStaysAtRest_AndDuplicateDriveInRejected` (807); `ClutchTests.OccupiedMechanicalInputIsRefusedWhenLoadingSavedConstruction` (`ClutchGeneralityTests.cs:632`) |
| Reset, save and load | `RunResetAndSaveLoad_RestoreAuthoredStateAndReproduceCoupling` (886) |
| Supply released by a Hold timer | `ClutchTests.HoldTimerSupplyReleasesCouplingWhileMotorKeepsRunning` (`ClutchGeneralityTests.cs:649`) |
| Plates, lamp and spokes follow committed state | `ClutchArtTests.ClutchScene_AnimatesPlatesLampAndShaftSpokesAcrossLifecycleAndResetsCleanly` (1448, same file) |

No requirement criterion is without a test. Slip and brake variants stay separate identities (EL-055; sequence-task-310). **Residual acceptance for the art repair (section 8).** Drop row `ClutchAxle` in `VisibleSolidColliderTests`: a Tennis ball of radius 0.07 m dropped at x 0.40, z 0 has its lowest centre within 5 mm of 0.055 m + its radius above the axle line; control: the same drop at z 0.2 reaches the base. A ball cannot rest on a horizontal cylinder of radius 0.055 m, so the row reads the lowest centre, not a rest. Printed spokes: a fact in `ClutchArtTests` that each spoke lies between 0 and 0.5 mm in front of its shaft face. Stall guard 5 s.

## 6. Resolved decisions (Story 11.2a)

1. `close_seconds`: Real placement parameter (`BuildOnly`, unit `Second`, range `[0.05, 2.0]` s, default `0.2` s) on the `clutch` package. The shared `MechanicalTransmissionDeclaration` capability validates the general mathematical domain (canonical +0 for untimed, otherwise a finite positive duration within the physical clock's representable span).
2. Geometry: base box, bearing blocks, coil cylinder (`r = 0.38 m`), stud sphere (`r = 0.1 m`), lamp sphere (`r = 0.075 m`) and the two rotating cylinder shafts (`r = 0.28 m`, `w = 0.13 m`) are solid colliders. The sliding plate assemblies (`Visual/PlateLeft`, `Visual/PlateRight`) are decorative, moved by bindings. The central axle rod (`r = 0.055 m`) and both shaft spokes are visible-solid defects with the repair in section 2 (decided 22 November 2026 under the standing delegation; reason: the owner's visible = solid rule and the part B precedent).
3. Solid cylinder collider and cylinder inertia for shaft wheels: Reuses Story 10.1 cylinder collider and inertia with `Rx(90°)` principal frame (`r = 0.28 m`, `w = 0.13 m`, `m = 0.25 kg`).
4. Shaft contact material values: Reuses catalogue rigid-part material (`restitution = 0`, `friction = 0.3`, `rollingResistance = 0`, `bounceThreshold = 0.1`), matching `motor` and `conveyor`.
5. Mechanical loops: Admitted in `WorkshopConnections` and `PhysicsDeclarationValidation` whenever each hinge input/output role is unique (`DriveIn -> Hinges[0]`, internal `Hinges[0] -> Hinges[1]`, `Drive -> Hinges[1]`); a closed loop such as `Clutch -> Conveyor -> Clutch` settles stably at rest with zero energy creation.
6. Plate closure easing: Linear in time from an integer physical-time transition origin and starting closure, re-anchored on supply direction changes; clamp at endpoints without a snap epsilon, mapped linearly onto `LocalX` of `Visual/PlateLeft` (`-0.25 -> -0.09`) and `Visual/PlateRight` (`0.25 -> 0.09`) via `CommittedTransmissionClosure`. Four independently mapped phases retain the three package lamp colors (Opening and Open both slate); each plate's spin child reads its explicit hinge's admission-relative committed travel via `CommittedRevoluteAngle`. The plate parent supplies an authored 90-degree Y axis frame, so the existing local-Z spin channel turns the disc about the package X axle while parent LocalX controls closure independently.
7. Supply semantics: Binary electrical supply on `PowerIn` via `SupplyTransmissionTarget` -> `SupplyConsumer.MechanicalTransmission` -> dedicated `TransmissionClosed` supply channel; electrical drain is the engine story `ENGINE-SUPPLY-DEMAND`, as for `motor` and `powered_gate`.
8. Coil trigger source: Electrical supply on `PowerIn` (per [CAT-018](../requirements.md#current-cat-018)). Supply loss immediately sets `Opening` and uncouples the shafts while each keeps its own signed angular momentum under bearing `FrictionTorque = 0.02 N·m`.
No question is open and none needs the owner.

## 7. Integration

- **Collaborators (all delivered).** Supply into `PowerIn` (Electrical) from the Battery, Impact switch, Hold timer or a gate. `DriveIn` (Mechanical input) from a Motor, Windmill, Conveyor or Reverse gear `Drive`; `Drive` (Mechanical output) into a Conveyor, Reverse gear or Wound spring `DriveIn`. Domains and roles agree at both ends. Signs: +1 per radian on Motor, Conveyor, Clutch and Windmill; −1 on both Reverse gear ports and on the Wound spring's and Fan's `DriveIn`.
- **Campaign.** No level in `content/puzzles.json` places or stocks the Clutch. It is taught by a level still to be authored (the 150-level target); nothing blocks that authoring.
- **Coverage binding.** Catalogue key `clutch` (CAT-018) in `docs/coverage/catalogue-elements.json`. No named identity maps to it.
- **Lesson fixtures.** Not needed for the part.

## 8. Delivery and residuals

Delivered as recorded in section 1. Every residual slice is blocked on the engine programme (owner decision 3a, 22 November 2026), which precedes further element work.

| Residual | Owner | Tier | Prerequisite |
| --- | --- | --- | --- |
| Spokes reprinted flat; axle given a collider | Repair part D of ELEMENTS-VISIBLE-SOLID-REMAINDER (coordinator decision 33), after the engine programme; it covers the Clutch, the Reverse gear, the Motor's index bar and the Counter's sockets, foot and face panel | Data-only (scene, the table row at `VisibleSolidColliderTests.cs:112` and tests) | the engine programme (owner decision 3a); a contract for part D |
| Coil drain | `ENGINE-SUPPLY-DEMAND` (sprint key `engine-supply-drive-demand`) | Engine | the engine programme (owner decision 3a), of which it is a story |
| Browser qualification | Post-catalogue E2E phase | n/a | n/a |

No named identity is closed by this part. **What the art slice's capability probe must re-check:** that a collider appended to the housing moves no existing collider index; that the axle collider does not touch either shaft body (shafts sit at z 0.43, the axle at z 0, and hinge bodies do not collide with their base); and that the spin binding still reads `Visual/PlateLeft/Spin` and `Visual/PlateRight/Spin` after the scene edit.

## 9. Engine conformance (owner instruction, 22 November 2026)

**Verdict: Conforms with fixes.** The fix is in the art layer (point 3), owned by repair part D (coordinator decision 33), whose contract is still to be drafted. No engine code names this element (searched `engine/`, `CuriousContraptions.Simulation`, `CuriousContraptions.Bundle` and the abstractions).

1. **Declaration data only.** Three bodies, scene colliders, two hinges, one transmission, one parameter, three ports, one electrical node, a one-consumer supply graph, seven presentation bindings. No clutch controller. The electrical node's numbers are validated as finite and read by no law (only `Role` is read): unread dead data, to be consumed by `ENGINE-SUPPLY-DEMAND` or removed forward-only.
2. **Generic laws.** Coupling: a bilateral ratio-1 transmission between two revolute coordinates whose engagement is a supply target: supplied, it closes over `close_seconds`; unsupplied, it opens. Slip while closing and the closure fraction are the transmission's own committed state. The shafts turn by hinge dynamics and whatever links drive them. No row depends on which part is linked.
3. **Three layers.** Colliders: housing boxes and spheres and the cylinder on the static body, as section 2 lists; one cylinder on each shaft body. Visuals: housing, plates, axle, spokes, lamp, stud. Animation: plate separation from `CommittedTransmissionClosure`, plate spin from `CommittedRevoluteAngle`, lamp albedo from `CommittedTransmissionPhase`; all read committed state. **Defect:** the axle is drawn solid with no collider and both shaft spokes stand 35 mm proud on bodies with no binding. Repair as section 2: a static cylinder for the axle and flat printing for the spokes, owned by repair part D (coordinator decision 33).
4. **One-way flow and typing.** Physics → committed transmission state → presentation. Typed `TransmissionConstraintKind`, `SupplyTerminal`, `CoordinateKind`; seconds and ratios as f32 (D10: a duration in game seconds).
5. **Flat-state counts** (from `clutch.tscn` and the declaration). 3 bodies (2 dynamic); 8 colliders: 6 on the housing (3 boxes, 1 cylinder, 2 spheres) and 1 cylinder on each shaft; 2 revolute joints; 1 transmission; 0 drives; 1 electrical node; 1 supply consumer; 3 ports; 1 parameter; 7 presentation bindings and 2 body-pose bindings.
6. **Performance fitness.** Eight primitives, two joints and one coupling row. Seven bindings is the most in this batch; each drives a visible cue and none costs physics time. The housing is static. Island sleeping is not implemented; a resting body is still stepped, at the cost of its broadphase proxies and rows every substep (no sleep or deactivation code in `CuriousContraptions.Simulation`). Owned by `ENGINE-ISLANDS-SLEEP` (owner decision 3a, 22 November 2026).
7. **Scenario tests.** `ClutchTests` (files `ClutchTests.cs` and `ClutchGeneralityTests.cs`) and `ClutchArtTests` run the production simulation; the tests in `ClutchGeneralityTests.cs` exercise the timed transmission with declarations that are not the Clutch.
