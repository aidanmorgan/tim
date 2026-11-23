# CAT-057 · reverse_transmission declaration readiness spec

**Native implementation (17 November 2026):** [Story 11.2b implementation and native evidence](../../../_bmad-output/implementation-artifacts/spec-11-2b-reverse-transmission.md) owns the active contract. The package uses two finite-inertia solid-cylinder wheels and the existing untimed −1 ratio law. Generic rigid shaft links use optional explicit rim metadata, animation-worker geometry and committed-travel witnesses; these links are not EL-200's friction/tension Belt or EL-201 Chain. Current typed ports allow one input and one output role per coordinate, with pass-through and supported bounded cycles; historical fan-out observations below are not a new acceptance prerequisite. The shared row now retains its committed reaction impulse so loaded stops do not accumulate false travel/work. Independent native review and the full local solution round passed (6,191/0/0). Normal commit hooks and exact-commit approval precede publication; post-catalogue browser qualification remains pending. Pinned harvest and obsolete capability observations below remain historical evidence.


**Current delivery (24 October 2026):** [CAT-057 delivery owner](../../../_bmad-output/planning-artifacts/epics.md#catalogue-slice-ownership). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 CAT-057-D readiness spec. Baseline commit `a6c914e`. Legacy citations use `path@a6c914e:Lstart-Lend` and stay retrievable after the Epic 7 purge. Current-engine files are cited at the same commit. Current authority for the shaft model is the [rotary transmission contract](../../rotary-transmission-parts.md). This page records the declaration knowledge the legacy holds; it does not restate the requirement row.

## 1. Identity

| Field | Value |
| --- | --- |
| CAT ID | CAT-057 |
| Kind | `reverse_transmission`, catalogue title "Reverse gear", category Motion |
| Requirement anchor | [CAT-057](../requirements.md#current-cat-057); retained behaviour [todo-151](../requirements.md#todo-151); shared rows [sequence-task-192](../requirements.md#sequence-task-192), [sequence-task-193](../requirements.md#sequence-task-193), [sequence-task-277](../requirements.md#sequence-task-277) |
| Mapped identities | [EL-203 Reverse transmission](../invest/named-elements.md#element-203) ([full row](../requirements.md#element-203)), a candidate Duplicate closure of this part (section 8). [EL-200 Drive belt](../invest/named-elements.md#element-200) is related through the link the reverser consumes and is not closed here. |
| Roadmap story | [11.2b Reverse transmission](../../../_bmad-output/planning-artifacts/epics.md#catalogue-slice-ownership); retain every CAT-057 mode and mapped named-identity obligation. |
| Status (22 November 2026) | **Delivered.** Package id `reverse_transmission`, title "Reverse gear", directory `elements/ReverseTransmission`, palette category Motion, order 39 (`elements/ReverseTransmission/ReverseTransmissionElementDeclaration.cs:13`–`:18`). Slice record: [11.2b Reverse transmission](../../../_bmad-output/implementation-artifacts/spec-11-2b-reverse-transmission.md) (status done; commit `c34f6390`, 17 November 2026). Residual (section 8): the index bars and gold marks on both wheels stand proud with no collider. Every residual slice is blocked on the engine programme (owner decision 3a, 22 November 2026); the story that owns this part's engine gap is `ENGINE-ISLANDS-SLEEP` (the resting wheels). Browser qualification pending. Checked on `8d1d68aa`; published main is now `25df3cd5`, and its two extra commits touch none of this package's files. |

## 2. Declaration

### Delivered declaration (22 November 2026)

Read from `elements/ReverseTransmission/ReverseTransmissionElementDeclaration.cs` and `reverse_transmission.tscn`.

- **Interfaces.** `IHingeElement`, `IMechanicalTransmissionElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:20`, `:90`), `ISceneGeometryElement` (`:10`). Footprint: 3 bodies, 2 moving, 2 revolute (`:22`).
- **Static housing colliders (scene).** Base box 1.5 × 0.16 × 0.9 m at (0, −0.4, 0); housing box 1.4 × 0.65 × 0.5 m at (0, 0, −0.08).
- **Two wheels (dynamic).** Cylinders r 0.32 m, 0.14 m wide, axis Z, at (±0.36, 0, 0.32); each 0.25 kg with moments (0.00681, 0.0128, 0.00681) kg·m² (`:49`–`:59`).
- **Material, all three bodies.** Restitution 0, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s (`:35`–`:40`).
- **Hinges.** Two continuous revolutes about +Z; no spring, no drive, **bearing friction 0** (`:54`–`:58`). The Reverse gear adds no loss of its own; a train stops under the Motor's 0.05 N·m and a Conveyor's 0.02 N·m.
- **Transmission.** One bilateral coupling, ratio −1, coordinate 0 to coordinate 1, untimed and always engaged (`:60`–`:61`).
- **Parameters, stores, events.** None (`:21`, `:41`).
- **Ports (`:25`–`:33`).**

  | `PortId` | Role | Domain | Direction | Position (m), normal | Binding |
  | --- | --- | --- | --- | --- | --- |
  | `DriveIn` | `PortRole.DriveIn` | Mechanical | Input | (−0.36, 0, 0.4), +Z | coordinate 0, −1 per radian; rim r 0.32 m |
  | `Drive` | `PortRole.Drive` | Mechanical | Output | (0.36, 0, 0.4), +Z | coordinate 1, −1 per radian; rim r 0.32 m |

  A link's ratio is the output's coordinate-per-radian divided by the input's (`engine/gpu/WorkshopConnections.cs:179`). Motor (+1) → `DriveIn` (−1) is −1, the internal coupling −1 and `Drive` (−1) → Conveyor (+1) −1, so one Reverse gear nets −1 and two net +1. Each coordinate takes one incoming and one outgoing link.

  Signs across the catalogue: +1 per radian on Motor, Conveyor, Clutch and Windmill; −1 on both Reverse gear ports and on the Wound spring's and Fan's `DriveIn`.

  Engagement classes (planned declaration data, [EL-201](EL-201-drive-chain.md#2-declaration), owned by `ENGINE-MECHANICAL-LINKS`): `DriveIn` and `Drive` admit Smooth and Toothed, so either takes a Drive belt or a Drive chain. The delivered rigid link stays admitted at every port and is unchanged (owner decision 2).
- **Presentation (`:23`–`:24`).** Pick radius 0.9 m; `Visual/InputWheel` and `Visual/OutputWheel` follow bodies 1 and 2; no bindings.

### Art
- Palette token: Reverse transmission (0.84, 0.61, 0.28) `#d69c47`; cream housing, navy base and two ochre wheels with navy and gold index marks turning oppositely. Icon `elements/ReverseTransmission/icon.svg`.
- **Art and solidity as shipped (`elements/ReverseTransmission/reverse_transmission.tscn`; table row `CuriousContraptions.tests/VisibleSolidColliderTests.cs:102`–`:105`).**

  | Node | Mesh | Class | Note |
  | --- | --- | --- | --- |
  | `Base`, `Housing` | boxes | solid | collider of the same size on the housing |
  | `InputWheel/Disc`, `OutputWheel/Disc` | cylinders r 0.32 m, 0.14 m | solid | collider on each wheel body |
  | `InputWheel/Index`, `OutputWheel/Index` | navy boxes 0.48 × 0.065 × 0.04 m at wheel-local z 0.09 | **defect: 40 mm proud of the wheel face (z 0.07), no collider** | rides the wheel's hinge body with no binding |
  | `InputWheel/Mark`, `OutputWheel/Mark` | gold spheres r 0.07 m at wheel-local (0.21, 0, 0.1) | **defect: reaches z 0.17, 100 mm proud of the face, no collider** | rides the wheel's hinge body with no binding |

  The four nodes are listed "to be measured" in `spec-engine-art-solidity.md:200` and their owner is visible-solid repair part D (Clutch, Reverse gear, Motor index bar and the Counter's sockets, foot and face panel; coordinator decision 33), which runs after the engine programme. Repair (decided under the standing delegation, by the precedent of part B and `spec-face-markings-flush.md`): reprint each index bar and each mark flat on its wheel face (a quad and a disc, at most 0.5 mm in front of the face), keeping names, positions in the face plane and colours. A collider on an off-axis mark would make a spinning wheel strike cargo with a bump the player did not build. Not a defect but recorded: both wheels float 8 cm in front of the housing (wheel back face at z 0.25, housing front at z 0.17) with no drawn axle. At a glance the Reverse gear differs from the Conveyor (same ochre) by two side-by-side wheels on a cream box, and from the Clutch by having no plates or lamp.

### Catalogue and inventory entry
- Id `reverse_transmission`, title "Reverse gear", category Motion, order 39 (`elements/ReverseTransmission/ReverseTransmissionElementDeclaration.cs:13`–`:18`).
- Level 18 `reverse_belt` stocks one Reverse gear and one Conveyor (section 7).

## 3. Engine capabilities

The capability families are in the [general-engine map row](../general-engine-element-map.md) and the [catalogue coverage](../../coverage/catalogue-elements.json). They are not duplicated here.

**Capability probe (22 November 2026, this checkout).** No engine code names the Reverse gear. Tests are in `CuriousContraptions.tests/`; they were found by name and line, and none was run. `ReverseTransmissionTests` is one partial class in four files; each test is cited with its file.

| # | Capability the behaviour uses | Status | Symbol | Exercising test |
| --- | --- | --- | --- | --- |
| 1 | Two passive continuous hinges with cylinder bodies | present | `RevoluteHingeDescriptor` (`CuriousContraptions.Abstractions/MechanicalContractValues.cs:149`) | `ReverseTransmissionTests.EveryCurrentPairingCarriesCargoWithReversalParity` (`ReverseTransmissionTests.cs:73`), where both wheels turn |
| 2 | Untimed bilateral coupling of two coordinates at ratio −1, with its reaction carried to the source | present | `MechanicalTransmissionDescriptor` (`MechanicalContractValues.cs:258`), `MechanicalTransmissionSolver` (`CuriousContraptions.Simulation/MechanicalTransmissionSolver.cs:29`) | `ReverseTransmissionTests.ReverseTransportsCargoOppositeTheZeroReverserBypass` (`:134`); `ReverseTransmissionTests.ADeclaredOutputLoadReactsThroughReverseToTheSource` (`ReverseTransmissionLoadTests.cs:19`); `ReverseTransmissionTests.CommittedTransmissionReactionOwnsFreshEngineAndAbandonedCandidateExecution` (`ReverseTransmissionStateTests.cs:14`) |
| 3 | Mechanical ports with signed coordinate scale; one link per role; typed refusals | present | `WorkshopConnections.ValidateMechanical` (`engine/gpu/WorkshopConnections.cs:170`) | `ReverseTransmissionTests.ConflictingSourcesAndWrongDirectionAreRefused` (`:192`); `ReverseTransmissionTests.UnsupportedMechanicalConnectionsRefuseAtAuthoringAndSaveBoundaries` (`ReverseTransmissionLifecycleTests.cs:104`) |
| 4 | Link artwork from declared rims and committed travel | present | `MechanicalRimDescriptor` (`CuriousContraptions.Abstractions/MechanicalLinkPresentationValues.cs:10`) | `ReverseTransmissionArtTests.NativeSceneAppliesWheelPosesConveyorWitnessAndEvaluatedLinkArtwork` (`:17`); `MechanicalLinkPresentationTests.DeclaredRimsProduceClosedTangentRoutesAndCommittedMarkers` (`:29`) |
| 5 | Reset; save and load of a rotated construction | present | n/a (generic lifecycle) | `ReverseTransmissionTests.SaveLoadAndResetPreserveRotatedTwoReverserParity` (`ReverseTransmissionLifecycleTests.cs:68`) |
| 6 | Scene colliders match the drawn solids; wheels are ordinary contacts | present | `ColliderShapeKind.Cylinder` (`CuriousContraptions.Abstractions/PhysicsDeclarationValues.cs:12`) | `VisibleSolidColliderTests.EverySolidArtPartHasItsColliderAndNoColliderReachesBeyondTheArt` (`:186`, row `:102`), which lists the index bars and marks as decorative (the defects under Art); `ReverseTransmissionTests.ExposedWheelHasPhysicalContactAndSignedSurfaceMotion` (`ReverseTransmissionLifecycleTests.cs:42`) |

Counts: 6 present, 0 untested, 0 narrower, 0 missing.

**Element dependencies.** A drive source (Motor, Windmill), a load (Conveyor) and the Battery behind the Motor; all delivered.

## 4. Legacy harvest

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | One-to-one reversing gearbox; direction is relative to the marked shaft faces | `parts/ReverseTransmissionPart.cs@a6c914e:L7-L7` | carry forward | Matches EL-203 and the DESIGN sign convention |
| 2 | Ratio −1, always engaged, between two owned hinge coordinates | `parts/ReverseTransmissionPart.cs@a6c914e:L17-L17` | carry forward | Declaration data |
| 3 | Velocity-level, phase-free law: output = ratio × input. Shared inertia sets both speeds; no endpoint gets a copied speed or work allowance. | `engine/physics/PhysicsTransmissionJoint.cs@a6c914e:L9-L13` | carry forward (law); do not carry forward (double-precision CPU class, `Project`/`Sweep` hooks) | CPU solver path; becomes an f32 TGS Soft row |
| 4 | Acceptance: an impulse on the input gives output speed = −input speed in the next step. Total KE stays ≤ the impulse KE + 1e−8, both axis-aligned and rotated (25°, 40°, 15°). | `CuriousContraptions.tests/RotaryTransmissionPartTests.cs@a6c914e:L35-L65` | carry forward (re-freeze tolerances under f32) | Restated as an acceptance fact |
| 5 | Acceptance: same state and inputs replay to identical body states; Reset restores the saved construction and zero input speed | `CuriousContraptions.tests/RotaryTransmissionPartTests.cs@a6c914e:L74-L80` | carry forward | Determinism and Reset |
| 6 | Acceptance: engagement shares real inertia. With I_a = 2, I_b = 3 and ω_a0 = 10, ω_a = 20/(2 + 3r²) and ω_b = r·ω_a for r ∈ {1, 2, −1, −2}. Energy does not increase, and I_a·ω_a + r·I_b·ω_b is conserved. | `CuriousContraptions.tests/TransmissionJointTests.cs@a6c914e:L17-L34` | carry forward | Analytic acceptance check |
| 7 | Acceptance: a downstream load reacts back through the shared solve, α_a = (τ_a + r·τ_load)/(I_a + r²·I_b); the static carrier gets no wrench | `CuriousContraptions.tests/TransmissionJointTests.cs@a6c914e:L36-L51` | carry forward | Load/backdrive criterion |
| 8 | Acceptance: branches share source inertia instead of duplicating drive. With +1 and −1 branches from one source (I = 2, 3, 5; ω0 = 12), all speeds are ±2.4 and KE = 28.8. | `CuriousContraptions.tests/TransmissionJointTests.cs@a6c914e:L53-L64` | carry forward | No copied work |
| 9 | Acceptance: ratio −2 across multiple turns over 0.2 s keeps speed error 0 and energy within 1e−6, and replays exactly | `CuriousContraptions.tests/TransmissionJointTests.cs@a6c914e:L81-L98` | carry forward (behaviour); do not carry forward (1e−6 double tolerance) | Use the game-grade f32 envelope |
| 10 | Hinge and coupling replacement is atomic; a coupling to an undeclared guide is rejected | `CuriousContraptions.tests/TransmissionJointTests.cs@a6c914e:L100-L122` | carry forward | Atomic compile |
| 11 | Ratio 0, NaN and ∞, identical endpoints, and ball-socket endpoints are rejected | `CuriousContraptions.tests/TransmissionJointTests.cs@a6c914e:L143-L154`; `engine/SceneTransmissionJoint.cs@a6c914e:L17-L26` | carry forward | Admission rejection |
| 12 | Acceptance: 0, 1 or 2 reversers between conveyors give sign (−1)^n in the same substep, not only at steady state. Connection order does not matter, and the downstream conveyor may be rotated (30°, 90°, 20°). | `CuriousContraptions.tests/MechanicalTests.cs@a6c914e:L62-L103` | carry forward | Two-reverser and order criteria |
| 13 | Acceptance: each reverser shows output speed = −input speed and |angle diff(−input angle, output angle)| ≤ 1e−4 | `CuriousContraptions.tests/MechanicalTests.cs@a6c914e:L108-L112` | carry forward (re-freeze tolerances under f32) | Wheel witnesses follow committed travel |
| 14 | Acceptance: after supply loss the chain coasts at constant speed, motor supplied work is constant, and total KE is unchanged within 1e−8 over 120 steps (unloaded, ideal hinges) | `CuriousContraptions.tests/MechanicalTests.cs@a6c914e:L113-L124` | carry forward (re-freeze tolerances under f32) | Supply-loss criterion |
| 15 | Steady values: conveyor shaft 6 and surface speed 4 | `CuriousContraptions.tests/MechanicalTests.cs@a6c914e:L104-L107` | do not carry forward | Ideal regulated motor speed; the current motor is bounded effort |
| 16 | Reset zeros the reverser angles and keeps the connection count; re-run restores the sign | `CuriousContraptions.tests/MechanicalTests.cs@a6c914e:L125-L140` | carry forward | Run/Reset |
| 17 | Graph rules: Mechanical-to-PowerIn and Electrical-domain belt links are rejected. A second driver into an occupied DriveIn is rejected. Fan-out from one Drive is allowed. A bad load is rejected atomically. | `CuriousContraptions.tests/MechanicalTests.cs@a6c914e:L185-L208`; `engine/MechanicalNetwork.cs@a6c914e:L15-L39` | carry forward | Typed topology admission |
| 18 | A conveyor→reverser→conveyor loop with no source is rejected | `CuriousContraptions.tests/MechanicalTests.cs@a6c914e:L199-L199` | do not carry forward pending owner | Conflicts with the later loop admission in `CuriousContraptions.tests/MechanicalWorkTests.cs@a6c914e:L98-L114` (see Open questions) |
| 19 | Acceptance: in a motor→reverser→load circuit, load speed = −motor speed within 1e−5 and energy ≤ supplied work. No work accrues after supply loss. | `CuriousContraptions.tests/MechanicalWorkTests.cs@a6c914e:L39-L93` | carry forward (re-freeze tolerances under f32) | Energy bound |
| 20 | Acceptance: an unpowered control gives zero travel and zero motor work. With a reverser, the conveyor shaft speed and cargo travel are negative (> 0.02 m opposite) in axis-aligned and 90°-rotated belts. Presentation `_Process` does not change physics. | `CuriousContraptions.tests/ConveyorRuntimeTests.cs@a6c914e:L65-L112` | carry forward | Transport integration with control |
| 21 | Acceptance: windmill drive through 0, 1 or 2 reversers has the parity sign in the first substep | `CuriousContraptions.tests/WindmillTests.cs@a6c914e:L55-L88` | carry forward (sign rule); do not carry forward (windmill speed values) | Windmill values belong to CAT-070 |
| 22 | Shaft readings come from current physics without callbacks and are 0 after restore | `CuriousContraptions.tests/PartRuntimeCheckpointTests.cs@a6c914e:L104-L144` | carry forward | Committed-state presentation |
| 23 | Lesson `reverse_belt`: goal is ball captured by receiver. Solution wiring: battery Supply → motor PowerIn, motor Drive → reverse_1 DriveIn, reverse_1 Drive → conveyor_1 DriveIn. Motor locked at (−3.5, 1, 0); conveyor_1 at (2, 2.5, 0). | `content/puzzles.json@a6c914e:L4912-L4918`, `content/puzzles.json@a6c914e:L4893-L4897`, `content/puzzles.json@a6c914e:L4965-L4969`, `content/puzzles.json@a6c914e:L5047-L5069` | carry forward | Lesson set-up for Epic 15 |
| 24 | Lesson authoring: ball x = 3, receiver x = −2, battery (−5, 1, 0), motor (−3.5, 1, 0), reverser solution (−0.3, 1, 0) | `tools/Campaign/Program.cs@a6c914e:L159-L171` | carry forward (layout); do not carry forward (tool) | Campaign tool is deleted; Epic 15 rebuilds authoring |
| 25 | Lesson acceptance: every solution connection is required to win. A motor→conveyor bypass spins the belt forward but does not win. | `CuriousContraptions.tests/MechanicalTests.cs@a6c914e:L228-L268` | carry forward | Meaningful negative control for the level |
| 26 | `reverse_1` difficulty rows: max position correction 0.25 and rotation correction 5 at precision 0; 0.1 and 2 at 0.45; 0 at 1 | `content/puzzles.json@a6c914e:L4986-L5026` | carry forward | Existing difficulty evidence preserved |
| 27 | Hint text: "Its paired wheels turn in opposite directions." | `content/puzzles.json@a6c914e:L4653-L4653` | carry forward | Teaching copy |

### Files harvested
- `parts/ReverseTransmissionPart.cs`
- `parts/catalog/reverse_transmission.tres`
- `parts/scenes/reverse_transmission.tscn`
- `reference/cpu/MachinePart.cs` (AddBox semantics only)
- `ui/WorkshopIcons.cs` (pictogram; not deleted by Epic 7)
- `engine/SceneRotaryShaft.cs`
- `engine/SceneTransmissionJoint.cs`
- `engine/physics/PhysicsTransmissionJoint.cs`
- `engine/MechanicalNetwork.cs`
- `engine/ConnectionPort.cs`
- `engine/MachineData.cs` (SocketId enum, L104-L108)
- `engine/physics/JointEquations.cs` (FrameJointKind, L5)
- `CuriousContraptions.tests/RotaryTransmissionPartTests.cs`
- `CuriousContraptions.tests/TransmissionJointTests.cs`
- `CuriousContraptions.tests/MechanicalTests.cs`
- `CuriousContraptions.tests/MechanicalWorkTests.cs`
- `CuriousContraptions.tests/ConveyorRuntimeTests.cs`
- `CuriousContraptions.tests/WindmillTests.cs`
- `CuriousContraptions.tests/PartRuntimeCheckpointTests.cs`
- `content/puzzles.json`
- `tools/Campaign/Program.cs`
- `engine/bridge/` (checked, no element knowledge)
- `reference/P0-022-before/docs/coverage/engine/*.json` (checked, coverage snapshots only, no element knowledge)
- `tools/P0-007-actual-path-probe/inputs.json` (checked, file list only)

## 5. Acceptance outline

Follow [CAT-057](../requirements.md#current-cat-057), [todo-151](../requirements.md#todo-151), [EL-203](../requirements.md#element-203) and Story 11.2. The Chrome observables are in the [rotary contract](../../rotary-transmission-parts.md#chrome-observable-acceptance-post-catalogue).

- **Construction (actual UI).** In `reverse_belt`, or a free-play bench, place the reverser and conveyor from the toolbox. Wire Battery Supply → Motor PowerIn, join Motor Drive → Reverser DriveIn and Reverser Drive → Conveyor DriveIn with Mechanical links. Verify placed poses and typed connections.
- **Positive.** The wheels turn in opposite directions and the belt carries the ball toward the receiver.
- **Negative/control.** A motor→conveyor bypass carries the ball the wrong way. An unpowered motor gives no motion. A disconnected reverser stays at rest.
- **Boundaries.** Each input direction, two reversers in series, a load/backdrive impulse on the output, whole-part rotation, reversed connection order, supply loss with coasting.
- **Run/Reset.** Exact zero angles, speeds and construction.
- **Save/Load.** A rotated two-reverser construction.
- **Integrations.** Conveyor transport, the clutch (CAT-018) reversed-drive case, and later windmill drive.

**Native tests that exist (22 November 2026), in `CuriousContraptions.tests/`.** Each criterion of [CAT-057](../requirements.md#current-cat-057) against its test:

| Criterion | Test (file line) |
| --- | --- |
| Each input direction; two reversers | `ReverseTransmissionTests.EveryCurrentPairingCarriesCargoWithReversalParity` (73), `CatalogueMotorDrivesEveryCurrentPairing` (81) |
| Reverses signed motion; control without a reverser | `ReverseTransmissionTests.ReverseTransportsCargoOppositeTheZeroReverserBypass` (134) |
| Load and backdrive; no duplicated input work (EL-203's outcome) | `ReverseTransmissionTests.ADeclaredOutputLoadReactsThroughReverseToTheSource` (`ReverseTransmissionLoadTests.cs:19`), `AnOutputDriveBackdrivesBothReverseShaftsAndTheUnpoweredMotor` (`:114`) |
| Disconnected graph | `ReverseTransmissionTests.AbsentDriveLeavesBothWheelsAtRest` (154) |
| Order and rotation; save, load and Reset | `ReverseTransmissionTests.SaveLoadAndResetPreserveRotatedTwoReverserParity` (`ReverseTransmissionLifecycleTests.cs:68`); `ClutchTests.ConnectionOrderDoesNotChangeClosureOrTransmission` (`ClutchGeneralityTests.cs:133`) |
| Supply loss adds no work | `ReverseTransmissionTests.SupplyLossAddsNoMotorWorkAndResetRestoresAuthoredState` (166) |
| Wrong-kind and conflicting connections | `ReverseTransmissionTests.ConflictingSourcesAndWrongDirectionAreRefused` (192); `ReverseTransmissionTests.UnsupportedMechanicalConnectionsRefuseAtAuthoringAndSaveBoundaries` (`ReverseTransmissionLifecycleTests.cs:104`) |
| Wheels and link witnesses follow committed travel | `ReverseTransmissionArtTests.NativeSceneAppliesWheelPosesConveyorWitnessAndEvaluatedLinkArtwork` (17) |
| Housing and wheel contact; passive loop; a Clutch downstream | in `ReverseTransmissionLifecycleTests.cs`: `ReverseTransmissionTests.HousingSupportsCargoWhileBeyondFootprintIsClear` (18), `ExposedWheelHasPhysicalContactAndSignedSurfaceMotion` (42), `APassiveTwoReverserLoopIsAdmittedAndRemainsAtRest` (93), `OpeningTheDownstreamClutchIsolatesItsCoastingOutput` (121) |

No requirement criterion is without a test; there is no parameter to refuse. **Residual acceptance for the art repair (section 8):** each index bar and mark is a flat printed surface between 0 and 0.5 mm in front of its wheel face, with its face-plane position and colour unchanged, and a Basketball dropped onto a wheel face of the part laid on its back has its lowest centre within 5 mm of the face plus its radius; control: the same drop 0.5 m beside the wheel reaches the housing or the bench. Planned `ReverseTransmissionArtTests.IndexBarsAndMarksArePrintedFlatOnTheirWheelFaces`. Stall guard 5 s.

## 6. Open questions

**All five are resolved (22 November 2026); none needs the owner.**

| # | Resolution | Authority |
| --- | --- | --- |
| 1 | A source-free loop is admitted when each coordinate has at most one incoming and one outgoing link, and stays at rest with no work created | Story 11.2b decision 8; `ReverseTransmissionTests.APassiveTwoReverserLoopIsAdmittedAndRemainsAtRest` (`ReverseTransmissionLifecycleTests.cs:93`) |
| 2 | The housing boxes collide | delivered |
| 3 | The wheels are true cylinders | Story 10.1 |
| 4 | The catalogue's rigid-part material: restitution 0, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s | Story 11.2b decision 8; delivered |
| 5 | Wheels are ordinary physical contacts, not a declared driven surface. A ball touching a turning rim is moved by ordinary friction; no lesson relies on it | Story 11.2b decision 8; `ReverseTransmissionTests.ExposedWheelHasPhysicalContactAndSignedSurfaceMotion` (`ReverseTransmissionLifecycleTests.cs:42`) |

The questions were: 1 mechanical loop policy; 2 housing collision; 3 wheel collider; 4 wheel contact material; 5 whether wheel-rim contact is a drive path.

## 7. Integration

- **Collaborators (all delivered).** `DriveIn` (Mechanical input) from a Motor, Windmill, Conveyor, Clutch or another Reverse gear `Drive`; `Drive` (Mechanical output) into a Conveyor, Clutch, Wound spring or another Reverse gear. Domains and roles agree at both ends. Signs: +1 per radian on Motor, Conveyor, Clutch and Windmill; −1 on both Reverse gear ports and on the Wound spring's and Fan's `DriveIn`. A link's ratio is the output's sign divided by the input's, so each external link to a +1 port is −1. The pairing test covers Motor, Clutch, Conveyor and Reverse gear only (`ReverseTransmissionTests.cs:17`–`:18`); a Reverse gear driving a Wound spring or a Fan (−1 into −1, ratio +1) and a Windmill source are not in it.
- **Campaign (`content/puzzles.json`).** Level 18 `reverse_belt` (Planned; one `captured` goal; provable from delivered parts after the engine programme, owner decision 3a; [delivery batches section 4](../invest/delivery-batches.md#4-early-level-proof), "Drive" group) stocks one Reverse gear and one Conveyor.
- **Coverage binding.** Catalogue key `reverse_transmission` (CAT-057) in `docs/coverage/catalogue-elements.json`. EL-203 has none of its own.
- **Lesson fixtures.** Not needed for the part.

## 8. Delivery and residuals

Delivered as recorded in section 1. Every residual slice is blocked on the engine programme (owner decision 3a, 22 November 2026), which precedes further element work.

| Residual | Owner | Tier | Prerequisite |
| --- | --- | --- | --- |
| Two index bars and two marks reprinted flat | Repair part D of ELEMENTS-VISIBLE-SOLID-REMAINDER (coordinator decision 33), after the engine programme; it covers the Clutch, the Reverse gear, the Motor's index bar and the Counter's sockets, foot and face panel | Data-only (scene, the table row at `VisibleSolidColliderTests.cs:102` and one art test) | the engine programme (owner decision 3a); a contract for part D |
| Pairing rows for Wound spring, Fan and Windmill | The next slice that touches the package; a missing test is a patch | Data-only | the engine programme (owner decision 3a) |
| Browser qualification | Post-catalogue E2E phase | n/a | n/a |

**EL-203 Reverse transmission** is a candidate Duplicate closure of this part: [its spec](EL-203-reverse-transmission.md) decides against a separate crossed-belt reverser, which leaves it no behaviour, parameter, port, mode or material of its own. Closure check: its ledger outcome, "it cannot reverse by duplicating input work", is executed by `ReverseTransmissionTests.ADeclaredOutputLoadReactsThroughReverseToTheSource` (`ReverseTransmissionLoadTests.cs:19`) and `ReverseTransmissionTests.SupplyLossAddsNoMotorWorkAndResetRestoresAuthoredState`, with the bypass control (row 134) and Reset, save and load (row 68). [Delivery batches](../invest/delivery-batches.md#2-duplicate-closures) lists EL-203 in the 2a "Drive train" group. The closure review runs after the engine programme (owner decision 3a) and before `ENGINE-MECHANICAL-LINKS`, since it closes against today's Mechanical link. **EL-200 Drive belt** and **EL-201 Drive chain** are not closed here. The delivered Mechanical link is a rigid ratio and stays rigid (coordinator decision 36 names it); Drive belt and Drive chain are new declared connection materials a player chooses, and a visible belt or chain loop is thick and therefore solid (owner decision, 22 November 2026).

**What the art slice's capability probe must re-check:** that `ReverseTransmissionArtTests` reads no index or mark transform that the reprint changes, and that the scene edit moves no collider index.

## 9. Engine conformance (owner instruction, 22 November 2026)

**Verdict: Conforms with fixes.** The fix is in the art layer (point 3), owned by repair part D (coordinator decision 33), whose contract is still to be drafted. No engine code names this element (searched `engine/`, `CuriousContraptions.Simulation`, `CuriousContraptions.Bundle` and the abstractions).

1. **Declaration data only.** Three bodies, scene colliders, two hinges, one transmission, two ports, two body-pose bindings. No parameter, no node, no event, no code.
2. **Generic laws.** Reversal: one bilateral transmission between two revolute coordinates with the declared ratio −1; torque and speed follow from that one constraint row and the two hinges. The same descriptor with another ratio is any gear pair; a gear pair's declared ratio follows its tooth counts (owner decision 8, 22 November 2026), and the Reverse gear's two equal wheels give −1.
3. **Three layers.** Colliders: two housing boxes on the static body and one cylinder on each wheel body. Visuals: housing, two wheels, index bars, marks. Animation: none; the wheels' art follows their bodies. **Defect:** the index bars (40 mm proud) and marks (100 mm proud) are raised on spinning bodies with no collider. Repair as section 2: flat printing, because an off-axis collider on a spinning wheel would knock cargo; owner, repair part D (coordinator decision 33).
4. **One-way flow and typing.** Committed wheel poses → art. Typed `CoordinateKind`, `TransmissionConstraintKind`, `PortId`; f32 values.
5. **Flat-state counts** (from `reverse_transmission.tscn` and the declaration). 3 bodies (2 dynamic); 4 colliders (2 housing boxes, 1 cylinder on each wheel); 2 revolute joints; 1 transmission; 0 drives, nodes, stores, events; 2 ports; 0 parameters; 0 presentation bindings and 2 body-pose bindings.
6. **Performance fitness.** The lightest rotary part: four primitives, two joints and one coupling row; static housing. Island sleeping is not implemented; a resting body is still stepped, at the cost of its broadphase proxies and rows every substep (no sleep or deactivation code in `CuriousContraptions.Simulation`). Owned by `ENGINE-ISLANDS-SLEEP` (owner decision 3a, 22 November 2026).
7. **Scenario tests.** `ReverseTransmissionTests` (four files) and `ReverseTransmissionArtTests` run the production simulation.
