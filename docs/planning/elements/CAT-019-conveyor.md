# CAT-019 · conveyor declaration readiness spec

**Current delivery (24 October 2026):** [CAT-019 delivery owner](../../../_bmad-output/planning-artifacts/epics.md#catalogue-slice-ownership). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

This is the Story 7.0 CAT-019-D declaration readiness spec for the conveyor belt. The baseline is commit `a6c914e`. Every citation uses the form `path@a6c914e:Lstart-Lend`, which stays retrievable from git history after the Epic 7 purge. Current-engine files are cited at the same commit.

## 1. Identity

| Field | Value |
| --- | --- |
| CAT ID | CAT-019 |
| Kind | `conveyor`, catalogue title "Conveyor belt" |
| Requirement anchor | [CAT-019](../requirements.md#current-cat-019); retained behaviour [todo-151](../requirements.md#todo-151) |
| Mapped identities | [EL-202 Conveyor](../invest/named-elements.md#element-202), a candidate Duplicate closure of this part; [EL-200 Drive belt](../invest/named-elements.md#element-200) is a related identity for the authored belt link and is not closed by this part (section 8) |
| Roadmap story | [11.1b Conveyor](../../../_bmad-output/planning-artifacts/epics.md#catalogue-slice-ownership); retain every CAT-019 mode and mapped named-identity obligation. |
| Status (25 November 2026) | **Delivered, with one requirement criterion not met.** CAT-019's "length/width define the surface" criterion is not met: `length` and `width` bind nothing. Package id `conveyor`, directory `elements/Conveyor`, palette category Motion, order 37 (`elements/Conveyor/ConveyorElementDeclaration.cs:25`–`:30`). Slice record: [11.1b Conveyor](../../../_bmad-output/implementation-artifacts/spec-11-1b-conveyor.md) (status done; commit `a0f6c4c0`, 17 November 2026); link artwork by [11.2b](../../../_bmad-output/implementation-artifacts/spec-11-2b-reverse-transmission.md) (commit `c34f6390`). Other residuals (section 8): treads and direction arrow not drawn. The solid pulleys and flat spokes (R1) are implemented, natively tested and independently reviewed in visible-solid remainder part B. Every residual slice is blocked on the engine programme (owner decision 3a, 22 November 2026); the stories that own this part's gaps are `ENGINE-PARAMETER-BOUND-FRAMES` (dimensions) and `ENGINE-ISLANDS-SLEEP` (the resting roller), and `ENGINE-CAPABILITY-INSTANCES` (the tread-wrapping value mapping, capability row 7; coordinator decision 29). Browser qualification pending. Checked on `37fbd3a4` (part B's baseline) with part B's candidate. |

## 2. Declaration

### Delivered declaration (22 November 2026)

Read from `elements/Conveyor/ConveyorElementDeclaration.cs` and `elements/Conveyor/conveyor.tscn`.

- **Interfaces.** `IHingeElement`, `IDrivenSurfaceElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:20`, `:80`) and `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`) (`:13`). Footprint: 2 bodies, 1 moving, 1 revolute (`:40`).
- **Static carrier colliders (scene).** Carrier box 3 × 0.24 × 1.2 m at the origin (belt top at y +0.12); two rails 3 × 0.16 × 0.08 m at (0, −0.12, ±0.6); idle roller cylinder r 0.16 m, 1.3 m long, axis Z, at (1.35, −0.07, 0); two supports 0.12 × 0.6 × 0.96 m at (±1.35, −0.4, 0).
- **Driven roller (dynamic).** Cylinder r 0.16 m, 1.3 m long, axis Z, at (−1.35, −0.07, 0); mass 0.5 kg; moments (0.0736, 0.0064, 0.0736) kg·m² from m(3r² + w²)/12 and mr²/2 (`:15`–`:17`, `:94`–`:105`).
- **Material, both bodies.** Restitution 0.05, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s (`:56`–`:60`).
- **Hinge.** Continuous revolute about +Z at (−1.35, −0.07, 0); no spring, no drive; bearing friction 0.02 N·m; roller and carrier do not collide (`:80`–`:107`).
- **Driven surface.** Carrier collider 0, face +Y, direction +X, travel per radian bound to `surface_per_radian` (`:109`–`:114`). The delivered sign is positive: positive roller rate carries cargo toward +X.
- **Parameters (all `Real`, build-only, editable in the Workbench unless a level locks them, D11).**

  | Name | Unit | Min | Max | Default | Bound to |
  | --- | --- | --- | --- | --- | --- |
  | `length` | m | 1.0 | 10.0 | 3.0 | **nothing** (residual R2) |
  | `width` | m | 0.6 | 4.0 | 1.2 | **nothing** (residual R2) |
  | `surface_per_radian` | m/rad | 0.1 | 5.0 | 0.6666667 | the driven surface |

- **Ports (`:42`–`:52`).** Both bind revolute coordinate 0 with `CoordinatePerRadian` +1 and a link rim of radius 0.22 m on the carrier.

  | `PortId` | Role | Domain | Direction | Position (m), normal |
  | --- | --- | --- | --- | --- |
  | `DriveIn` | `PortRole.DriveIn` | Mechanical | Input | (−1.35, −0.07, 0.78), +Z |
  | `Drive` | `PortRole.Drive` | Mechanical | Output | (1.35, −0.07, 0.78), +Z |

  Engagement classes (planned declaration data, [EL-201](EL-201-drive-chain.md#2-declaration), owned by `ENGINE-MECHANICAL-LINKS`): `DriveIn` and `Drive` admit Smooth only, as a belt roller, so a Drive belt is admitted and a Drive chain is refused there. The delivered rigid link stays admitted at every port and is unchanged (owner decision 2).

- **Stores, events.** None (`:62`). No `Transported` event is declared (section 6, question 6).
- **Presentation (`:19`–`:22`).** Pick radius 0.85 m; `Visual/Roller` follows body 1; `Visual/PulleyOut` turns about Z with `CommittedRevoluteAngle` of coordinate 0.

### Art
- Catalogue colour Conveyor `#d69c47`, (0.84, 0.61, 0.28) ([DESIGN colour system](../../../DESIGN.md#colour-system)); icon `elements/Conveyor/icon.svg`. [DESIGN mechanical work feedback](../../../DESIGN.md#mechanical-work-feedback) governs the motion presentation.
- **Art and solidity (`elements/Conveyor/conveyor.tscn`; the `conveyor` row of `CuriousContraptions.tests/VisibleSolidColliderTests.cs`), with [part B](../../../_bmad-output/implementation-artifacts/spec-elements-visible-solid-remainder-b.md)'s candidate (implemented, natively tested and independently reviewed).**

  | Node | Mesh | Class | Note |
  | --- | --- | --- | --- |
  | `Visual_Carrier`, `Visual_RailA`, `Visual_RailB`, `Visual_SupportA`, `Visual_SupportB` | boxes | solid | collider of the same size on the carrier |
  | `Visual_IdleRoller`; `Roller/Visual_Roller` | cylinders r 0.16 m, 1.3 m | solid | static collider; the driven roller's collider is on its own body |
  | `Visual/PulleyOut/Wheel` | cylinder r 0.22 m, 0.1 m long, axis Z, at (1.35, −0.07, 0.78) | solid | `col_pulley_out`, a cylinder of the same size and pose on the carrier, base collider 6 (part B row B1). Its binding turns it about its own axis, which the static cylinder follows exactly |
  | `Visual/Roller/PulleyIn/Wheel` | the same cylinder at roller-local (0, 0, 0.78) | solid | `col_pulley_in` on the roller's hinge body, roller collider 1 (row B2) |
  | `PulleyIn/Spoke`, `PulleyOut/Spoke` | quads 0.33 × 0.045 m at z 0.0502, 0.2 mm in front of the wheel face (z 0.05) | printed | were boxes 27.5 mm proud; `Mesh_PulleySpoke` is deleted (row B9) |
  | ten treads, direction arrow | not in the scene | **not drawn** | residual R3 |

  The new wheel colliders grow the part's +z reach from 0.64 to 0.83 m, so placement refuses it 0.19 m sooner at the bench's +z edge; every authored Conveyor stays inside. At a glance the Conveyor differs from the Reverse gear (same ochre catalogue colour) by its long flat belt between two end pulleys.

### Catalogue and inventory entry
- Id `conveyor`, title "Conveyor belt", category Motion, order 37 (`elements/Conveyor/ConveyorElementDeclaration.cs:25`–`:30`).
- It is in inventory in 4 levels and placed in `belt_relay` and `double_cold_start` ([current consumers](../invest/current-consumers.md#cat-019-i)).

## 3. Engine capabilities

The capability families are those in the [CAT-019 map row](../general-engine-element-map.md) and [catalogue-elements.json](../../coverage/catalogue-elements.json); they are not repeated here.

**Capability probe (22 November 2026, this checkout).** No engine code names the Conveyor. Tests are in `CuriousContraptions.tests/`; they were found by name and line, and none was run.

| # | Capability the behaviour uses | Status | Symbol | Exercising test |
| --- | --- | --- | --- | --- |
| 1 | Passive continuous hinge with bearing friction and a cylinder body | present | `RevoluteHingeDescriptor` (`CuriousContraptions.Abstractions/MechanicalContractValues.cs:149`) | `ConveyorTests.MidRunPowerCut_DeceleratesToStopUnderFriction` (`ConveyorTests.cs:141`); `RevoluteHingeFrictionTests.AHingedBarTurnsWithTheTorqueLeftAfterItsFrictionTorque` (`:59`) |
| 2 | Driven surface on one box face, coupled to a revolute coordinate with reaction on the shaft | present | `DrivenSurfaceDescriptor` (`MechanicalContractValues.cs:241`), `DrivenSurfaceAdmission` (`CuriousContraptions.Simulation/DrivenSurfaceAdmission.cs:6`) | `DrivenSurfaceBindingTests.OnlyTheDeclaredFaceCarriesCargoInItsSelectedCoordinatesDirection` (`:96`), `SharedBodyJacobianIncludesCrossTermsAndReciprocalImpulseWork` (`:282`), `DrivenSurfaceBindingTests.CoastingSourcePaysForCargoMotionAndUnloadedControlKeepsItsSpin` (`:379`), `ABlockedLoadReactsOnTheSelectedShaft` (`:576`); `ConveyorTests.PoweredTransport_AcceleratesBallAlongPositiveX` (`:73`), `UnloadedBelt_ConservesEnergy` (`:226`) |
| 3 | Mechanical input and output ports on one coordinate; link ratio out ÷ in; one link per role | present | `WorkshopConnections.ValidateMechanical` (`engine/gpu/WorkshopConnections.cs:170`) | `ConveyorTests.ChainedConveyors_DriveRelaysRotationToDownstreamConveyor` (`:417`), `InvalidMechanicalConnections_RefusedAtomically` (`:529`); `MechanicalCoordinateBindingsTests.AliasesCannotReuseEitherCoordinateRole` (`MechanicalCoordinateBindingsTests.cs:874`) |
| 4 | Presentation: body-following roller, pulley turned by committed hinge angle, link artwork | present | `PresentationDriver.CommittedRevoluteAngle` (`CuriousContraptions.Abstractions/ElementPresentation.cs:30`), `MechanicalRimDescriptor` (`CuriousContraptions.Abstractions/MechanicalLinkPresentationValues.cs:10`) | `ConveyorArtTests.TheRollerArtFollowsTheCommittedRollerPoseAndResetsToItsScenePose` (`ConveyorTests.cs:630`); `ReverseTransmissionArtTests.NativeSceneAppliesWheelPosesConveyorWitnessAndEvaluatedLinkArtwork` (`:17`) |
| 5 | Parameter bounds, retired keys and save/load | present | `ParameterDescriptor` (`ConveyorElementDeclaration.cs:35`–`:37`) | `ConveyorTests.InvalidParameters_RefusedAtomically` (`:496`), `RetiredParameters_RefusedAtomically` (`:516`), `SaveLoadRoundtrip_PreservesParametersAndMechanicalConnections` (`:451`) |
| 6 | A parameter resizing the carrier, rails and both rollers and moving the hinge frame, the link rims and both ports | **narrower** | a collider component of the base body can follow one parameter (`ColliderParameterBinding`, `CuriousContraptions.Abstractions/SceneGeometry.cs:27`; bundle evaluation in `CuriousContraptions.Bundle/BundleGeometry.cs:28`–`:48`) and a port position one parameter (`PortPositionBinding`, `CuriousContraptions.Abstractions/PortDescriptor.cs:77`); a child body's collider cannot, the hinge `BaseFrame` and `MechanicalRimDescriptor.Centre` are constants, and the ports need two parameters (`length` for x, `width` for z) | `ENGINE-PARAMETER-BOUND-FRAMES` (owner decision 3a, 22 November 2026; element-free text in the batch summary), then the data slice `conveyor-dimensions` |
| 7 | Tread phase wrapping with committed surface travel; arrow direction from its sign | **narrower** | `CommittedRevoluteAngle` gives the travel, but the value mappings are `Identity`, `SineCyclesToUnit`, `StateValues` and `ClampToEndpoints` (`ElementPresentation.cs:39`): none wraps a travel into a repeating tread pitch | owner: `ENGINE-CAPABILITY-INSTANCES` (wrap mapping: "a value mapping that wraps a committed coordinate into a declared pitch"; coordinator decision 29) |

Counts: 5 present, 0 untested, 2 narrower, 0 missing.

**Element dependencies.** Drive sources: Motor, Windmill, another Conveyor, with a Clutch or Reverse gear between them (all delivered). Cargo and goal: the balls and the Receiver.

## 4. Legacy harvest

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | Carrier box L × 0.24 × W; top at +0.12 m | `parts/ConveyorPart.cs@a6c914e:L84-L90` | carry forward | Declaration geometry. |
| 2 | Roller 0.5 kg, r 0.16 m, length W + 0.1 m, at (−L/2 + 0.15, −0.07, 0); cylinder inertia | `parts/ConveyorPart.cs@a6c914e:L20-L39` | carry forward | Finite-inertia driven surface the requirement demands. |
| 3 | Free hinge about +Z; connected collision disabled | `parts/ConveyorPart.cs@a6c914e:L40-L45` | carry forward | Generic hinge declaration. |
| 4 | Driven face normal +Y, direction +X, travel per radian −`surface_per_radian` | `parts/ConveyorPart.cs@a6c914e:L46-L49` | carry forward | Signed port/hinge convention. |
| 5 | Surface speed = ratio × axis·(ω_shaft − ω_carrier); face admission rules | `engine/physics/DrivenSurface.cs@a6c914e:L9-L39` | carry forward (law), do not carry forward (CPU class) | The law becomes worker declaration data. |
| 6 | Tangent rows gain shaft/carrier angular terms; no copied speed | `engine/physics/MaterialContact.cs@a6c914e:L37-L70` | carry forward (behaviour), do not carry forward (CPU solver path) | The TGS worker must implement it generically. |
| 7 | Competing drives on one face rejected | `engine/physics/PhysicsWorld.cs@a6c914e:L409-L413` | carry forward | Admission rule. |
| 8 | Sockets `DriveIn`/`Drive` at the roller ends, offset W/2 + 0.18; both bound −1 to one hinge | `parts/ConveyorPart.cs@a6c914e:L64-L71` | carry forward | Port layout and relay. |
| 9 | One belt per input; fan-out allowed; wrong domain or socket rejected; invalid load rejects atomically | `engine/MechanicalNetwork.cs@a6c914e:L15-L45`, `CuriousContraptions.tests/MechanicalTests.cs@a6c914e:L185-L211` | carry forward | Typed topology. |
| 10 | Loop policy conflict: one test rejects `a→b, b→a` before a source exists; a later test accepts both links and reports zero speed | `CuriousContraptions.tests/MechanicalTests.cs@a6c914e:L185-L211`, `CuriousContraptions.tests/MechanicalWorkTests.cs@a6c914e:L98-L114` | do not carry forward | The legacy contradicts itself; open question 2. |
| 11 | Material restitution 0.05, bounce 0.1 m/s, friction 0.3 | `parts/ConveyorPart.cs@a6c914e:L72-L72` | carry forward | Authored material. |
| 12 | Parameter validation: L, W finite > 0; ratio finite ≠ 0; defaults 3, 1.2, 2/3 | `parts/ConveyorPart.cs@a6c914e:L74-L82`, `parts/catalog/conveyor.tres@a6c914e:L14-L14`, `CuriousContraptions.tests/MechanicalWorkTests.cs@a6c914e:L116-L130` | carry forward | Rejection boundaries. |
| 13 | Retired properties `powered`, `speed` and `traction` rejected | `CuriousContraptions.tests/MechanicalTests.cs@a6c914e:L213-L226`, `CuriousContraptions.tests/MechanicalWorkTests.cs@a6c914e:L148-L159` | carry forward (rejection) | No self-powered belt; strings only at the serialization boundary. |
| 14 | Accepted: steady roller 6 rad/s and surface 4 m/s with the default motor and ratio; the downstream conveyor matches on the same substep; reversers flip the sign | `CuriousContraptions.tests/MechanicalTests.cs@a6c914e:L62-L143` | carry forward | Positive and chained behaviour. |
| 15 | Accepted: after supply loss the unloaded belt coasts at constant speed and kinetic energy (1e-8) without new work; Reset zeroes angles | `CuriousContraptions.tests/MechanicalTests.cs@a6c914e:L62-L143` | carry forward (re-freeze tolerances under f32) | Coasting and Reset. |
| 16 | Accepted: disconnected conveyors stay at 0; disconnecting during Run is refused until Reset | `CuriousContraptions.tests/MechanicalTests.cs@a6c914e:L145-L183` | carry forward | Disconnected-drive control. |
| 17 | Accepted: ball transported along local +X at yaw 0° and 90° (> 1 m in 180 ticks) | `CuriousContraptions.tests/ConveyorTests.cs@a6c914e:L30-L55` | carry forward | Rotated control. |
| 18 | Accepted: unpowered belt holds the ball (x stays −0.8 for 180 ticks) until the switch supplies the motor, then x > 0.5 | `CuriousContraptions.tests/ConveyorTests.cs@a6c914e:L57-L83` | carry forward | Negative then positive. |
| 19 | Accepted: a ball at z = 2 (outside W = 1.2) is not transported | `CuriousContraptions.tests/ConveyorTests.cs@a6c914e:L85-L106` | carry forward | Finite-width boundary. |
| 20 | Accepted: Unpowered/Forward/Reverse/Serial × plain/90°: travel > 0.02 m in 60 ticks with the mode's sign; unpowered gives zero travel and work; construction restores exactly | `CuriousContraptions.tests/ConveyorRuntimeTests.cs@a6c914e:L65-L112` | carry forward | Mode matrix and Reset. |
| 21 | Accepted: two payloads both advance > 0.01 m; motor work ≤ 120 W × 60 ticks; total kinetic energy ≤ work + 0.01 J | `CuriousContraptions.tests/ConveyorRuntimeTests.cs@a6c914e:L137-L155` | carry forward | Loaded, energy-bounded transport. |
| 22 | Accepted: only the authored top face drives; side and underside contacts are ordinary | `CuriousContraptions.tests/DrivenSurfaceWorldTests.cs@a6c914e:L75-L97` | carry forward | Requirement: side/underside stay ordinary. |
| 23 | Accepted: stored roller energy transfers to the payload without gain; sustained slip ends without reversal | `CuriousContraptions.tests/DrivenSurfaceWorldTests.cs@a6c914e:L29-L73` | carry forward | Energy bound. |
| 24 | Accepted: ratio ±1, 2 and reversed pair order all conserve the coupled momentum; a dynamic carrier gets the reaction; rotation invariant | `CuriousContraptions.tests/DrivenContactTests.cs@a6c914e:L28-L48`, `CuriousContraptions.tests/DrivenContactTests.cs@a6c914e:L108-L131` | carry forward | Signed ratio and reaction. |
| 25 | Accepted: one circular friction budget; non-loaded or separating contacts are never driven; two loads share one shaft | `CuriousContraptions.tests/DrivenContactTests.cs@a6c914e:L50-L106` | carry forward | Load-aware frictional transport. |
| 26 | Bounded drive never overdraws work or impulse in signed cases | `CuriousContraptions.tests/SurfaceMotorBudgetTests.cs@a6c914e:L15-L46` | carry forward (behaviour), do not carry forward (CPU impulse helper) | Covered by the motor row. |
| 27 | Tread phase, pulley spin, arrow flip, belt marks only while running | `parts/ConveyorPart.cs@a6c914e:L130-L139`, `CuriousContraptions.tests/MechanicalTests.cs@a6c914e:L17-L60` | carry forward (bindings), do not carry forward (per-part `ObservePhysics` loop) | Animation is declared bindings. |
| 28 | `Transported` event rule | `parts/ConveyorPart.cs@a6c914e:L140-L149` | carry forward | Observable witness; no goal uses it (open question 6). |
| 29 | `conveyor_courier`: ball (−3, 5, 0), receiver (2, 0.9, 0), locked battery and motor; solution conveyor at (−2, 2.5, 0); Supply→PowerIn, Drive→DriveIn | `content/puzzles.json@a6c914e:L3875-L3882`, `content/puzzles.json@a6c914e:L4145-L4207`, `content/puzzles.json@a6c914e:L4209-L4224` | carry forward | First lesson. |
| 30 | `belt_relay`: locked relay conveyor at (1, 4.5, 0) drives a placed conveyor | `content/puzzles.json@a6c914e:L4488-L4550`, `content/puzzles.json@a6c914e:L4624-L4646` | carry forward | Chained conveyor. |
| 31 | `reverse_belt`: motor → reverse transmission → conveyor; a direct belt bypass must fail | `content/puzzles.json@a6c914e:L4983-L5045`, `content/puzzles.json@a6c914e:L5047-L5069`, `CuriousContraptions.tests/MechanicalTests.cs@a6c914e:L228-L272` | carry forward | Reversed ratio and negative control. |
| 32 | Lessons need every drive link; removing any one fails at precision 0, 0.45 and 1 | `CuriousContraptions.tests/MechanicalTests.cs@a6c914e:L228-L272` | carry forward | Level integration. |
| 33 | `double_cold_start`: locked conveyor started by a switched supply; `depth_delivery` conveyor yawed 90° | `content/puzzles.json@a6c914e:L21344-L21406`, `content/puzzles.json@a6c914e:L25866-L25928` | carry forward | Gated start and rotated fixture. |
| 34 | Campaign modules `conveyor`, `conveyor_signal` (receiver → switch → lamp) and `gated_belt` (switch-gated supply, `powered_after`) | `tools/Campaign/Program.cs@a6c914e:L34-L63`, `tools/Campaign/Program.cs@a6c914e:L76-L91` | carry forward | Lesson authoring set-ups for Epic 15. |

### Files harvested
- `parts/ConveyorPart.cs`
- `parts/catalog/conveyor.tres`
- `parts/scenes/conveyor.tscn` (script attachment only)
- `engine/SceneDrivenSurface.cs`
- `engine/physics/DrivenSurface.cs`
- `engine/physics/MaterialContact.cs`
- `engine/physics/PhysicsWorld.cs`
- `engine/physics/PersistentContactPair.cs`
- `engine/MechanicalNetwork.cs`
- `engine/ConnectionPort.cs`
- `engine/MachineData.cs`
- `CuriousContraptions.tests/ConveyorTests.cs`
- `CuriousContraptions.tests/ConveyorRuntimeTests.cs`
- `CuriousContraptions.tests/DrivenContactTests.cs`
- `CuriousContraptions.tests/DrivenSurfaceWorldTests.cs`
- `CuriousContraptions.tests/SurfaceMotorBudgetTests.cs`
- `CuriousContraptions.tests/MechanicalTests.cs`
- `CuriousContraptions.tests/MechanicalWorkTests.cs`
- `content/puzzles.json` (levels `conveyor_courier`, `belt_relay`, `reverse_belt`, `double_cold_start`)
- `tools/Campaign/Program.cs`
- `ui/WorkshopIcons.cs` (pictogram)
- `engine/physics/ContactSlipPath.cs` (checked, CPU slip integration of driven surfaces, no element values)
- `reference/p054-guide/puzzles-candidate.json` (checked, duplicate candidate copy of the levels)
- `reference/P0-022-before/docs/coverage/engine/capabilities-02.json` (checked, coverage snapshot, no element knowledge)

## 5. Acceptance outline

Follow the [CAT-019 row](../requirements.md#current-cat-019) and Story 11.1; do not restate them.
- **Chrome UI recipe.** Open `conveyor_courier`. Drag the conveyor from the toolbox under the ball with the real placement controls. Connect battery `Supply` → motor `PowerIn` and motor `Drive` → conveyor `DriveIn` with the contextual wiring UI. Run, then record the captured goal, roller and payload poses, and bundle identity.
- **Positive.** The payload is carried along local +X into the receiver; treads, pulleys and arrow follow committed travel (facts 14, 17, 27).
- **Negative/control.** An unpowered motor or missing Mechanical link gives no motion; a wire from the activation domain is rejected; a ball outside the belt width is untouched (facts 16, 18, 19).
- **Boundaries.** Stalled or loaded belt, coasting after supply loss, reversed ratio, downstream chained conveyor (`belt_relay`), rotated belt, side and underside contacts (facts 15, 20–22, 30, 33).
- **Run/Reset.** Roller angle, speed, tread phase and payload return exactly (fact 20).
- **Save/Load.** `length`, `width`, `surface_per_radian`, pose and typed mechanical links round-trip; retired properties are rejected (fact 13).
- **Integrations.** Motor (CAT-042), Reverse transmission and Clutch (Story 11.2), Basketball/Receiver capture.

**Native tests that exist (22 November 2026), `CuriousContraptions.tests/ConveyorTests.cs`.** Each criterion of [CAT-019](../requirements.md#current-cat-019) against its test:

| Criterion | Test (line) | Control or boundary |
| --- | --- | --- |
| Loaded transport by friction | `PoweredTransport_AcceleratesBallAlongPositiveX` (73); `MultipleConcurrentLoads_TransportBothSimultaneously` (280) | `UnpoweredBelt_BallStaysStationaryWithNoWork` (105), with a disabled battery and with the mechanical link removed |
| Unloaded belt; no copied-speed work source | `UnloadedBelt_ConservesEnergy` (226) | the same test's motor-work bound |
| Coasting after supply loss | `MidRunPowerCut_DeceleratesToStopUnderFriction` (141) | owner decision of 3 November 2026: it stops under the declared bearing friction |
| Reversed ratio | `ReversedDrive_SurfaceRunsAlongNegativeX` (180); `ReverseTransmissionTests.ReverseTransportsCargoOppositeTheZeroReverserBypass` (`ReverseTransmissionTests.cs:134`) | the zero-reverser bypass |
| Downstream chained conveyor | `ChainedConveyors_DriveRelaysRotationToDownstreamConveyor` (417) | none in the file |
| Side contacts stay ordinary | `OffBeltMiss_SideContactRemainsOrdinaryRigid` (311); `DrivenSurfaceBindingTests.AdjacentOrdinaryFaceDoesNotAcquireItsNeighboursDrive` (`:323`) | n/a |
| Rotated belt | `RotatedConveyor_AlignsTransportAlongRotatedLocalX` (339) | n/a |
| Reset, save and load | `ResetAndReplay_RestoresExactAuthoredState` (367); `SaveLoadRoundtrip_PreservesParametersAndMechanicalConnections` (451) | n/a |
| Parameter and connection refusal | `InvalidParameters_RefusedAtomically` (496); `RetiredParameters_RefusedAtomically` (516); `InvalidMechanicalConnections_RefusedAtomically` (529) | below minimum, above maximum and non-finite for each parameter |
| Roller and link witnesses follow committed travel | `ConveyorArtTests.TheRollerArtFollowsTheCommittedRollerPoseAndResetsToItsScenePose` (same file); `ReverseTransmissionArtTests.NativeSceneAppliesWheelPosesConveyorWitnessAndEvaluatedLinkArtwork` (17) | n/a |
| Solid pulley wheels, flat spokes (part B) | `VisibleSolidColliderTests.ABallDroppedOntoEachLowRestitutionFeatureStopsAtItsDrawnSurface` (`ConveyorOutputPulley`, `ConveyorInputPulley`); `VisibleSolidColliderTests.EverySolidArtPartHasItsColliderAndNoColliderReachesBeyondTheArt` (`conveyor`); `BothPulleySpokesArePrintedFlatOnTheirWheels` | without a wheel collider the ball stops on the roller's end, 0.18 m lower |

**Not yet covered (residual acceptance, owned in section 8).**
- *Stalled belt.* Given a ball on the belt held by a Wall at the +X end, when the Motor is supplied with torque 1 N·m, then the roller turns less than 0.05 rad in 2 s and the Motor's committed work is at most 0.05 J; control: without the Wall the ball travels more than 0.5 m in 2 s. Planned `ConveyorTests.StalledBelt_HoldsItsShaftAndTakesNoWork`. (At 0.1 N·m the net torque after bearing friction is 0.03 N·m and the control ball would move about 0.07 m, so the lower torque cannot discriminate.)
- *Underside contact.* Given a Conveyor rolled 180° so that its local −Y face is uppermost, a Basketball resting on that face between the supports, and the Motor supplied, then the ball's speed along the belt stays below 0.05 m/s for 2 s; control: the same ball on the +Y face of an upright Conveyor travels more than 0.5 m in 2 s. Planned `ConveyorTests.UndersideContact_IsNotDriven`.
- *Dimensions (R2).* Given `length` 6 m and `width` 2 m and a supplied Motor, when a ball is dropped at x = 2.5 m, z = 0.8 m (outside the default 3 × 1.2 m belt), then it lands on the belt top (centre within 5 mm of y = 0.12 m + radius at first rest) and has moved more than 0.5 m along +X two seconds later; control: at the default size the same drop falls past the belt; boundaries: at the minimum and maximum of each parameter the roller, both ports and both rims compile at ±(length/2 − 0.15) and width/2 + 0.18, within 1 mm. Planned `ConveyorTests.LengthAndWidth_ResizeTheBeltItsRollersAndItsPorts`.
- *Treads and arrow (R3).* Given a running belt, then each tread's position along the belt equals committed roller travel × `surface_per_radian` modulo the tread pitch within 5 mm (a tread is 45 mm wide), it reverses with the roller, and the arrow points along the surface velocity; control: at rest neither moves. Planned `ConveyorArtTests.TreadsAndArrowFollowCommittedSurfaceTravel`.

## 6. Open questions

**All eight are resolved (22 November 2026); none needs the owner.** The tread-wrapping value mapping (capability row 7) is owned by `ENGINE-CAPABILITY-INSTANCES` (wrap mapping; coordinator decision 29).

| # | Resolution | Authority |
| --- | --- | --- |
| 1 | `length` [1, 10] m, `width` [0.6, 4] m, `surface_per_radian` [0.1, 5] m/rad, all build-only reals; non-finite and out-of-range values are refused. Decided under the standing delegation: `surface_per_radian` is positive only, and reversal is the Reverse gear's job (one way to reverse a belt) | Delivered (Story 11.1b); `ConveyorTests.InvalidParameters_RefusedAtomically` |
| 2 | A source-free mechanical loop is admitted when every coordinate has at most one incoming and one outgoing link, and stays at rest | Story 11.2a decision 5; `ReverseTransmissionTests.APassiveTwoReverserLoopIsAdmittedAndRemainsAtRest` (`ReverseTransmissionLifecycleTests.cs:93`) |
| 3 | The idle roller collides as a cylinder (visible = solid) | owner, 25 October 2026; Story 10.1 |
| 4 | Only the declared +Y face is driven. The roller is an ordinary contact | Delivered; `DrivenSurfaceBindingTests.AdjacentOrdinaryFaceDoesNotAcquireItsNeighboursDrive` |
| 5 | The driven face is a generic `DrivenSurfaceDescriptor`: friction rows on the face take their target from the shaft's rate and return their reaction to the shaft | Delivered (Story 11.1b); `DrivenSurfaceBindingTests.SharedBodyJacobianIncludesCrossTermsAndReciprocalImpulseWork` |
| 6 | The `Transported` event is not retained: no goal kind or lesson reads it | Decided now under D19 |
| 7 | The rollers are solid `Cylinder` colliders | Story 10.1 |
| 8 | The belt decelerates to rest under declared friction: 0.02 N·m at its own roller and 0.05 N·m at the Motor. No work is added after the cut and no momentum is erased instantly | Owner, 3 November 2026 (Motor spins down by bearing friction); `ConveyorTests.MidRunPowerCut_DeceleratesToStopUnderFriction` |

The questions were: 1 parameter bounds; 2 mechanical loop policy; 3 idle roller solidity; 4 which faces are driven; 5 how the driven face is realised; 6 the `Transported` event; 7 the roller collider; 8 whether a belt coasts or stops after supply loss.

## 7. Integration

- **Drive sources (delivered).** Motor (`Drive`, Mechanical output, +1), Windmill (`elements/Windmill/WindmillElementDeclaration.cs:47`), another Conveyor's `Drive`, and a Clutch or Reverse gear between them. Supply reaches the Motor from the Battery, Impact switch, Hold timer or a gate.
- **Cargo and goals (delivered).** Basketball, Tennis ball, Bowling ball, Receiver.
- **Campaign (`content/puzzles.json`), all Planned; numbers are authored positions before decision 24d, each paired with its plan level in [campaign coverage, section 5](readiness/campaign-coverage.md#5-removed-and-converted-levels-owner-decision-24d).** 16 `conveyor_courier`, 17 `belt_relay` and 18 `reverse_belt` have one `captured` goal and are provable from delivered parts after the engine programme (owner decision 3a) ([delivery batches section 4](../invest/delivery-batches.md#4-early-level-proof)); 49 `double_cold_start` and 58 `grand_contraption` wait for goal evaluation and their conversion (completion-ledger OS-08). Every authored Conveyor uses the default 3 × 1.2 m size.
- **Coverage binding.** Catalogue key `conveyor` (CAT-019) in `docs/coverage/catalogue-elements.json`. EL-202 has none of its own.
- **Lesson fixtures.** A level that offers it from the player's inventory needs no lesson-fixture story; a level that locks it as a fixture adds a `FIX-` row to the catalogue fixture table ([current consumers](../invest/current-consumers.md), read by `tools/Coverage/CurrentCatalogue.cs:100`), and needs [S8](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package) only for per-parameter or connection locks. Each level is authored by its Early level proof slice.

## 8. Delivery and residuals

Delivered as recorded in section 1. Every residual slice is blocked on the engine programme (owner decision 3a, 22 November 2026), which precedes further element work.

| # | Residual | Owner | Tier | Prerequisite |
| --- | --- | --- | --- | --- |
| R1 | Both pulley wheels solid; both spokes printed flat | [ELEMENTS-VISIBLE-SOLID-REMAINDER part B](../../../_bmad-output/implementation-artifacts/spec-elements-visible-solid-remainder-b.md): implemented, natively tested and independently reviewed | Engine, as that contract states (it changes the shared matcher) | part A (published) |
| R2 | Requirement criterion not met: `length` and `width` are validated, saved and shown but resize nothing. The scene is a fixed 3 × 1.2 m belt, and the hinge frame, rims and ports are constants. A player who edits them sees no change | New slice `conveyor-dimensions`, after the engine story `ENGINE-PARAMETER-BOUND-FRAMES` (capability row 6). Not a scope cut: the parameters stay | Engine for the prerequisite; data-only for the package slice once it is published | the engine programme, then `ENGINE-PARAMETER-BOUND-FRAMES` |
| R3 | Ten treads and the direction arrow of [DESIGN.md](../../../DESIGN.md) are not in the scene | `conveyor-dimensions` (the treads must scale with length) | as R2 | as R2, and an owning story for capability row 7 (none yet) |
| R4 | Stalled-belt and underside-contact tests | The next slice that touches the package (R1 or R2); a missing test is a patch | Data-only | the engine programme |
| R5 | Browser qualification | Post-catalogue E2E phase | n/a | n/a |

**EL-202 Conveyor** is a candidate Duplicate closure of this part ([EL-202](EL-202-conveyor.md); [delivery batches 2a](../invest/delivery-batches.md#2-duplicate-closures), "Drive train"). Closure check: its ledger outcome, "unpowered or noncontact cargo is not carried along a hidden path", is executed by `UnpoweredBelt_BallStaysStationaryWithNoWork` (both rows) and `OffBeltMiss_SideContactRemainsOrdinaryRigid`, with Reset and save/load by rows 367 and 451. R2 does not block the closure, because EL-202 names no size parameter. The closure review runs after the engine programme (owner decision 3a) and before `ENGINE-MECHANICAL-LINKS`, since it closes against today's Mechanical link. **EL-200 Drive belt** is not closed by the Conveyor. The delivered Mechanical link is a rigid ratio and stays rigid (coordinator decision 36 names it); Drive belt and Drive chain are new declared connection materials a player chooses, and a visible belt or chain loop is thick and therefore solid (owner decision, 22 November 2026).

**What a residual slice's capability probe must re-check.** For R2: whether `PortPositionBinding` still takes one parameter; whether a scene child body's collider can follow a parameter; whether a rail offset and a roller length can share one binding kind; and that the driven surface's face follows the resized collider 0 with no second rule. For R1: that appended colliders move no existing collider index, since the driven surface names carrier collider 0.

Float-rule debt in the delivered tests, to fix at their next edit: exact `Vector3` equality on replayed state at `ConveyorTests.cs:411`–`:412`.

## 9. Engine conformance (owner instruction, 22 November 2026)

**Verdict: Conforms with fixes.** Two fixes: declared parameters that parameterise nothing (point 2) and two drawn wheels with no collider (point 3). No engine code names this element (searched `engine/`, `CuriousContraptions.Simulation`, `CuriousContraptions.Bundle` and the abstractions).

1. **Declaration data only.** Two bodies, scene colliders, one hinge, one driven surface, three parameters, two ports, one presentation binding. No belt code, no `Transported` event.
2. **Generic laws.** Transport: a driven surface, whose contact rows take a tangential surface velocity equal to the hinge's angular speed times `surface_per_radian`, with ordinary friction carrying the cargo. Drive relay: mechanical links on the roller's coordinate. **Finding:** `length` and `width` are declared, validated and saved but bound to no collider, frame, rim or port, so they are data that no law reads, and the requirement's "length/width define the surface" is not met. The repair is the element-free story `ENGINE-PARAMETER-BOUND-FRAMES`, which also makes bundle admission refuse a declared parameter that binds nothing. Until then the delivered behaviour is a fixed 3 × 1.2 m belt.
3. **Three layers.** Colliders: carrier, rails and supports (boxes) and the idle roller (cylinder) on the static body; the driven roller cylinder on its hinge body. Visuals: the matching meshes, two pulley wheels and their printed spokes. Animation: `Visual/PulleyOut` rotation from `CommittedRevoluteAngle`. The two pulley wheels, drawn solid with no collider, get cylinder colliders on the body that carries each, and the spokes are printed flat (part B rows B1, B2 and B9). The requirement's moving treads need a wrapping value mapping the presentation contract lacks; it is a general presentation capability owned by `ENGINE-CAPABILITY-INSTANCES` (wrap mapping, capability row 7; coordinator decision 29).
4. **One-way flow and typing.** Physics → committed hinge angle → presentation. Typed ports, `CoordinateKind`; f32 values.
5. **Flat-state counts.** 2 bodies (1 dynamic); 9 colliders with part B (5 boxes, 4 cylinders: 7 on the carrier, 2 on the roller), 7 before it; 1 revolute joint; 0 drives of its own; 1 driven surface; 0 nodes, stores, events; 2 ports; 3 parameters; 1 presentation binding and 1 body-pose binding.
6. **Performance fitness.** Nine primitives with part B (seven before it) and one joint; the carrier is static. The surface velocity is a property of existing contact rows, so it adds no per-tick work beyond the contacts already on the belt. Island sleeping is not implemented; a resting body is still stepped, at the cost of its broadphase proxies and rows every substep (no sleep or deactivation code in `CuriousContraptions.Simulation`). Owned by `ENGINE-ISLANDS-SLEEP` (owner decision 3a, 22 November 2026).
7. **Scenario tests.** `ConveyorTests` runs the production simulation. Its save/load test asserts parameter values and the link only; when dimensions are bound it must also assert the compiled geometry.
