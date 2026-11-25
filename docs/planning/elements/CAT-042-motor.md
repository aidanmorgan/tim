# CAT-042 · motor declaration readiness spec

**Current delivery (24 October 2026):** [CAT-042 delivery owner](../../../_bmad-output/planning-artifacts/epics.md#catalogue-slice-ownership). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

This is the Story 7.0 CAT-042-D declaration readiness spec for the electric motor. The baseline is commit `a6c914e`. Every citation uses the form `path@a6c914e:Lstart-Lend`, which stays retrievable from git history after the Epic 7 purge. Current-engine files are cited at the same commit.

## 1. Identity

| Field | Value |
| --- | --- |
| CAT ID | CAT-042 |
| Kind | `motor`, catalogue title "Electric motor" |
| Requirement anchor | [CAT-042](../requirements.md#current-cat-042); retained behaviour [todo-146](../requirements.md#todo-146) |
| Mapped identities | [EL-199 Electric motor](../invest/named-elements.md#element-199) |
| Roadmap story | [11.1a Motor](../../../_bmad-output/planning-artifacts/epics.md#catalogue-slice-ownership); retain every CAT-042 mode and mapped named-identity obligation. |
| Status (22 November 2026) | **Delivered.** Package id `motor`, directory `elements/Motor`, palette category Power, order 36 (`elements/Motor/MotorElementDeclaration.cs:31`–`:36`). Slice records: [11.1a Motor](../../../_bmad-output/implementation-artifacts/spec-11-1a-motor.md) (status done; commit `5603a834`, 4 November 2026) on the engine slice [engine-revolute-drive](../../../_bmad-output/implementation-artifacts/spec-engine-revolute-drive.md); speed during Run by [engine-revolute-drive-target](../../../_bmad-output/implementation-artifacts/spec-engine-revolute-drive-target.md) (commit `66da1ae7`); the `Drive` socket by [11.1b Conveyor](../../../_bmad-output/implementation-artifacts/spec-11-1b-conveyor.md) (commit `a0f6c4c0`). Residuals: supply drain, the index-bar art repair and the `turned`/`powered_after` goal kinds (section 8). Every residual slice is blocked on the engine programme (owner decision 3a, 22 November 2026); the stories that own this part's gaps are `ENGINE-SUPPLY-DEMAND` (drain; EL-199 closes inside it), `ENGINE-GOALS` (the goal kinds) and `ENGINE-ISLANDS-SLEEP` (the resting rotor). Browser qualification pending. Checked on `8d1d68aa`; published main is now `25df3cd5`, and its two extra commits touch none of this package's files. |

## 2. Declaration

### Bodies and shapes
- **Housing (static, part root).** Base box 1.25 × 0.16 × 1 m at local (0, −0.43, 0); body box 1 × 0.8 × 0.85 m at the origin; housing cylinder radius 0.48 m, length 0.86 m, turned onto local Z; the gold PowerIn stud (sphere r 0.1 m at (−0.55, 0, 0)) and the supply lamp (sphere r 0.075 m at (0.32, 0.28, 0.45)). The stud and lamp are drawn, so they are solid.
- **Rotor (dynamic shaft body).** A true cylinder collider, r 0.3 m, 0.13 m wide, turned onto the hinge's +Z, its own body under `Visual/Rotor` at (0, 0, 0.54).
- **Index bar.** `Rotor/Visual_Index` is a gold box 0.48 × 0.07 × 0.004 m at rotor-local z 0.067 (`elements/Motor/motor.tscn:51`–`:53`, `:135`). The rotor's half-width is 0.065 m, so the bar stands 4 mm proud of the face, on a hinge body, with no binding and no collider. That is a visible-solid defect (the print band elsewhere is 0 to 0.5 mm, `CuriousContraptions.tests/FaceMarkingTests.cs:60`); the repair, a print at most 0.5 mm proud, belongs to visible-solid repair part D (coordinator decision 33), which runs after the engine programme (section 8).

### Mass and material
- Rotor: declared constant mass frame, 0.5 kg, moments (0.01195, 0.0225, 0.01195) kg·m² in its cylinder's axes (axial m·r²/2, transverse m·(3r² + w²)/12), principal frame Rx(90°), which lays the axial moment on the hinge's +Z.
- Both bodies take the catalogue's rigid-part contact material: restitution 0, friction 0.3, bounce threshold 0.1 m/s (decision 3).

### Constraints and joints
- One revolute hinge about local +Z at the rotor centre joins the rotor to the housing. Travel is unbounded in both directions, and collision between the connected bodies is disabled. Source: `parts/MotorPart.cs@a6c914e:L30-L35`.
- The hinge drive is a bounded motor row; see Work and energy stores.
- **Generic capabilities the Motor declares (engine slice `engine-revolute-drive`).** The Motor adds no motor code; its package declares one `RevoluteHingeDescriptor` that uses these generic hinge capabilities:
  - `Travel: RevoluteTravel.Continuous` with limits exactly [−π, π] and no axial spring. The stop rows stay inert, so the shaft turns without end.
  - `Drive: RevoluteDriveDescriptor(TargetSpeed, MaxTorque)`, bound to the `speed` and `torque` parameters. This is one velocity-level row on the hinge axis. It drives the rotor's relative spin toward ω_t, and its accumulated impulse is clamped to ±τ_d·h while supplied and to [0, 0] otherwise. The same expression bounds solver and admission.
  - Supply through the owner's `PowerIn` port. The compiler names the hinge's drive as a typed `SupplyConsumer.Revolute` on the Motor's consumer node; the bundle validator refuses a driven hinge without an electrical PowerIn input.
  - **Committed state per hinge:** the supplied flag, the signed travel (the sum of ω_rel·h) and the positive drive work Σ max(0, λ_drive·ω_rel). Run and Reset zero all three. Read them with `PhysicsGpuAbi.ReadRevoluteState`.
  - **Effects:** a stalled or braking drive adds no work, and an unsupplied rotor coasts, slowed only by any declared `FrictionTorque`. Battery drain is not delivered; its owner is the engine story `ENGINE-SUPPLY-DEMAND` (sprint key `engine-supply-drive-demand`; owner decision 3a, 22 November 2026). The drive's target is committed state: a `RevoluteDriveTarget` run control changes it from the next committed step (engine-revolute-drive-target, open question 5).

### Typed sockets and ports
Delivered ports (`elements/Motor/MotorElementDeclaration.cs:49`–`:55`):

  | `PortId` | Role | Domain | Direction | Position (m), normal | Binding |
  | --- | --- | --- | --- | --- | --- |
  | `PowerIn` | `PortRole.PowerIn` | Electrical | Input | (−0.55, 0, 0), −X | the supply consumer node, whose target is the hinge drive (`:120`–`:122`) |
  | `Drive` | `PortRole.Drive` | Mechanical | Output | (0, 0, 0.65), +Z | revolute coordinate 0, `CoordinatePerRadian` **+1**; link rim radius 0.3 m centred (0, 0, 0.605) on the housing |

  Engagement classes (planned declaration data, [EL-201](EL-201-drive-chain.md#2-declaration), owned by `ENGINE-MECHANICAL-LINKS`): `Drive` admits Smooth and Toothed, being the source of loads a belt cannot carry, so it takes a Drive belt or a Drive chain. The delivered rigid link stays admitted and is unchanged (owner decision 2).

  The delivered sign is +1, not the legacy −1: a link's ratio is the output's coordinate-per-radian divided by the input's (`engine/gpu/WorkshopConnections.cs:179`), so Motor → Conveyor and Motor → Clutch are +1 and a Reverse transmission (both ports −1, internal ratio −1) nets −1. Each coordinate takes one outgoing and one incoming link (`engine/gpu/WorkshopConnections.cs:182`–`:183`); the legacy fan-out from `Drive` is not admitted.

### Sensors and activation
- There is no activation input. The Motor is driven only by electrical supply on `PowerIn`; an Activation wire into it is refused (`MotorTests.AnActivationWireIntoThePowerInputIsRefused`, `MotorTests.cs:316`).
- The package declares no event row. The `turned` and `powered_after` goal kinds will read the hinge's committed travel and supply; they are owned by `ENGINE-GOALS`.

### Work and energy stores
- The drive is one velocity row whose impulse is at most τ_d·h each substep, so it delivers at most τ_d·|ω_t| of power and τ_d·|ω|·Δt of work, and nothing at stall, with no separate work budget (open question 6).
- An unsupplied drive's impulse is clamped to [0, 0]: it neither drives nor brakes, and only the declared 0.05 N·m bearing friction slows the rotor (open question 9).
- Committed work is the hinge's positive drive work; it reads back from committed state and is zero after Reset.

### Parameters
| Name | Type | Range | Default | Unit | Source |
| --- | --- | --- | --- | --- | --- |
| `speed` | f32 | 0 to 20 inclusive | 6 | rad/s target shaft speed (`ParameterUnit.RadianPerSecond`) | `parts/MotorPart.cs@a6c914e:L55-L63`, `parts/catalog/motor.tres@a6c914e:L14-L14` |
| `torque` | f32 | 0.1 to 100 inclusive (decision 1; legacy > 0 to 100) | 20 | N·m maximum effort (`ParameterUnit.NewtonMetre`) | `parts/MotorPart.cs@a6c914e:L55-L63`, `parts/catalog/motor.tres@a6c914e:L14-L14` |

`speed` is a run control (`ParameterRunEdit.RunControl`, `elements/Motor/MotorElementDeclaration.cs:41`–`:42`); `torque` is build-only. Both use the descriptor's default editor policy, Reject (open question 5).

### Cosmetic curves and UI bindings
- **Supply lamp.** A sphere of radius 0.075 m at (0.32, 0.28, 0.45) blends from slate `#556573` to gold `#f7cb52` over 0.1 s with a SmoothStep curve.
- **Lamp bindings.** The lamp's three colour bindings use `CommittedActuatorDrive` with a 0.1 s SmoothStep. That driver reads the committed actuator read, which now has one row per drive slot (prismatic slots, then revolute slots), keyed by the typed `SupplyConsumer`; its supplied value is the very supply input of the committed tick, the same expression for every slot. A revolute row reads its committed travel and relative spin and never reports moving contact.
- **Rotor and index.** The rotor art follows the committed rotor pose; the index bar is a child of the rotor art and turns with it.
- The pick radius is 0.9 m.

### Art
- Palette: catalogue colour Electric motor `#66b8c9`, i.e. (0.40, 0.72, 0.79) (`parts/catalog/motor.tres@a6c914e:L13-L13`; [DESIGN colour system](../../../DESIGN.md#colour-system)). Navy base `#293954`, cream wheel `#fff8e9`, gold `#f7cb52` power stud and index (`parts/MotorPart.cs@a6c914e:L68-L89`). [DESIGN mechanical work feedback](../../../DESIGN.md#mechanical-work-feedback) governs the motion presentation.
- Icon: `elements/Motor/icon.svg`.
- **Art and solidity as shipped (`elements/Motor/motor.tscn`; table row `CuriousContraptions.tests/VisibleSolidColliderTests.cs:99`–`:100`).** Solid, each with a collider of its drawn shape: `Visual_Base` (box), `Visual_Body` (box), `Visual_Housing` (cylinder), `Visual_Stud` (sphere r 0.1 m), `Lamp` (sphere r 0.075 m) on the housing, and `Rotor/Visual_Rotor` (cylinder r 0.3 m, 0.13 m wide) on the rotor body. **Defect:** `Rotor/Visual_Index` stands 4 mm proud (Bodies and shapes) while the table lists it as decorative; it joins repair part D (coordinator decision 33). At a glance it differs from the Clutch (same cyan catalogue colour) by one wheel on a boxed housing rather than two wheels with gold plates between them.

### Catalogue and inventory entry
- Id `motor`, title "Electric motor", category Power, order 36, description "Electricity turns its shaft." (`elements/Motor/MotorElementDeclaration.cs:31`–`:36`). The drive socket is labelled in the scene, so the description is kept (section 8). Supported domains: Mechanical and Electrical.
- The Motor is a locked fixture in its 12 levels, never in inventory ([current consumers](../invest/current-consumers.md#cat-042-i)).

## 3. Engine capabilities

The capability families are those in the [CAT-042 map row](../general-engine-element-map.md) and [catalogue-elements.json](../../coverage/catalogue-elements.json); they are not repeated here.

**Capability probe (22 November 2026, this checkout).** The package implements `IHingeElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:20`), `ISupplyElement` (`CuriousContraptions.Abstractions.Electrical/ElectricalContracts.cs:11`) and `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`), and no engine code names it. Tests are in `CuriousContraptions.tests/`; Tests were found by name and line; none was run.

| # | Capability the behaviour uses | Status | Symbol | Exercising test |
| --- | --- | --- | --- | --- |
| 1 | Continuous revolute hinge with Coulomb bearing friction | present | `RevoluteHingeDescriptor`, `RevoluteTravel.Continuous` (`CuriousContraptions.Abstractions/MechanicalContractValues.cs:149`, `:129`) | `RevoluteDriveTests.AContinuousHingeTurnsPastTheFullCircleWithNoStopImpulseAndAccumulatesItsTravel` (`RevoluteDriveTests.cs:195`); `RevoluteHingeFrictionTests.AHingedBarTurnsWithTheTorqueLeftAfterItsFrictionTorque` (`:59`); `MotorTests.CuttingTheSupplySpinsTheRotorDownUnderBearingFrictionWithNoFurtherWork` (`MotorTests.cs:330`) |
| 2 | Supplied velocity drive bounded by a maximum torque, with committed travel and work | present | `RevoluteDriveDescriptor` (`MechanicalContractValues.cs:138`), `RevoluteConstraintSolver` (`CuriousContraptions.Simulation/RevoluteConstraintSolver.cs:44`) | `RevoluteDriveTests.ADriveNeverDeliversMoreEnergyThanItsWorkOrMoreWorkThanItsPowerBound` (`:238`); `MotorTests.ASuppliedMotorSpinsUpToItsSpeedHoldsItAndPaysOnlyItsBearingLoss` (`:217`) |
| 3 | On/off supply from `PowerIn` to the drive | present | `SupplyDriveTarget` (`CuriousContraptions.Abstractions/SupplyContractValues.cs:52`) | `MotorTests.TheMotorCompilesAContinuousDrivenHingeConsumedByItsOwnPowerInput` (`:990`), `TheMotorWithoutItsPowerInputIsRefusedByTheBundleValidator` (`:1008`), `AnUnpoweredMotorStaysStillDoesNoWorkAndKeepsItsLampSlate` (`:294`) |
| 4 | Drive target as a run control (speed during Run) | present | `ParameterRunEdit.RunControl`, `RunControlLane.RevoluteDriveTarget` (`CuriousContraptions.Abstractions/ParameterDescriptor.cs:44`, `:50`; `engine/gpu/WorkshopRunControl.cs:15`) | `RunControlTableTests.RunCompilesADriveTargetRowWithItsParameterBoundsAtItsDriveIndex` (`:222`); `MotorTests.ASpeedChangeDuringRunDrivesTheShaftToTheNewSpeedFromTheNextCommittedStep` (`:397`) |
| 5 | Solid cylinder collider with a declared constant mass frame | present | `ColliderShapeKind.Cylinder` (`CuriousContraptions.Abstractions/PhysicsDeclarationValues.cs:12`) | `VisibleSolidColliderTests.EverySolidArtPartHasItsColliderAndNoColliderReachesBeyondTheArt` (`:237`, `motor` row `:99`); `MotorTests.TheLowestTorqueAcceleratesTheRotorAtItsNetTorqueOverItsInertia` (`:242`) |
| 6 | Mechanical output port on a revolute coordinate, with a link rim | present | `PortDescriptor.Coordinate` and `MechanicalRim` (`CuriousContraptions.Abstractions/PortDescriptor.cs:55`), `WorkshopConnections.ValidateMechanical` (`engine/gpu/WorkshopConnections.cs:170`) | `ConveyorTests.PoweredTransport_AcceleratesBallAlongPositiveX` (`ConveyorTests.cs:73`), `InvalidMechanicalConnections_RefusedAtomically` (`:529`); `MechanicalLinkPresentationTests.DeclaredRimsProduceClosedTangentRoutesAndCommittedMarkers` (`:29`) |
| 7 | Lamp driven by the committed drive supply; rotor art following its body | present | `PresentationDriver.CommittedActuatorDrive` (`CuriousContraptions.Abstractions/ElementPresentation.cs:23`) | `MotorTests.TheSupplyLampBlendsFromSlateToGoldOverATenthOfASecondWhenSupplied` (`:265`); `MotorRotorArtTests.TheRotorArtFollowsTheCommittedRotorPoseAndResetsToItsScenePose` (`MotorTests.cs:1075`) |
| 8 | Reset of drive state; save and load of parameters, pose and wire | present | `RevoluteDriveTargetRead` (`CuriousContraptions.Abstractions/RevoluteConstraintValues.cs:45`) | `MotorTests.ResetRestoresTheAuthoredRotorAndZeroesTheDriveStateAndLamp` (`:812`), `ASavedMotorLoadsWithItsSpeedTorquePoseAndWireAndSpinsAtItsSavedSpeed` (`:837`), `ASaveDuringRunAndResetKeepTheAuthoredSpeedAfterASpeedChange` (`:469`) |
| 9 | Typed parameter bounds refused at every seam | present | `ParameterDescriptor` bounds (`elements/Motor/MotorElementDeclaration.cs:41`–`:44`) | `MotorTests.AnOutOfRangeOrNonFiniteParameterIsRefusedAtEverySeamAndChangesNothing` (`:1044`) |
| 10 | Drive work debiting the source's finite store (supply drain) | **missing** | none: `ElectricalSupply.Reserve` (`engine/gpu/ElectricalSupply.cs:11`) grants store charging and electrical airflow demand only; no drive's work is a request | `ENGINE-SUPPLY-DEMAND` (owner decision 3a, 22 November 2026); the [deferred work](../../../_bmad-output/implementation-artifacts/deferred-work.md) entry (`:221`) names the same `ElectricalSupply.Reserve` |
| 11 | `turned` and `powered_after` goal kinds reading committed travel and supply | **missing** | the engine evaluates `None`, `Captured`, `ActivatedAfter`, `IntegrityBroken` only (`engine/gpu/WorkshopPuzzle.cs:9`) | `ENGINE-GOALS` (its goal slices, five by owner decision 12) |

Counts: 9 present, 0 untested, 0 narrower, 2 missing.

**Element dependencies.** Supply: Battery, Impact switch, Hold timer and the electrical gates (delivered); Solar panel (CAT-059, undelivered). Drive consumers: Conveyor, Clutch, Reverse gear, Wound spring and Fan (delivered).

## 4. Legacy harvest

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | Rotor 0.5 kg, r 0.3 m, w 0.13 m at (0, 0, 0.54); cylinder inertia formulas | `parts/MotorPart.cs@a6c914e:L19-L27` | carry forward | Authored finite rotor inertia that the requirement demands. |
| 2 | Housing boxes, cylinder and port layout | `parts/MotorPart.cs@a6c914e:L49-L53`, `parts/MotorPart.cs@a6c914e:L64-L72` | carry forward | Declaration geometry. |
| 3 | Free hinge about +Z; connected collision disabled; travel both ways | `parts/MotorPart.cs@a6c914e:L30-L35` | carry forward | Becomes a generic hinge declaration. |
| 4 | Mechanical socket sign: clockwise-positive viewed from +Z, binding −1 | `parts/MotorPart.cs@a6c914e:L54-L54`, `parts/MotorPart.cs@a6c914e:L96-L97` | carry forward | Signed port convention the requirement references. |
| 5 | `speed` 0–20, `torque` (0, 100]; defaults 6 and 20; out-of-range rejected | `parts/MotorPart.cs@a6c914e:L55-L63`, `parts/catalog/motor.tres@a6c914e:L14-L14`, `CuriousContraptions.tests/MechanicalWorkTests.cs@a6c914e:L132-L146` | `speed` and the defaults carried forward; the torque range superseded (decision 1: [0.1, 100] N·m) | f32 admission decided in Story 11.1a. |
| 6 | Supply: effort = torque only while powered; work torque·speed·Δt; power cap torque·speed | `parts/MotorPart.cs@a6c914e:L91-L97` | carry forward (law), do not carry forward (per-part `PreparePhysics` loop) | The bounded-supply law stays; per-element update loops are banned. |
| 7 | Zero effort disables actuation; zero work allows only braking; budgets finite and nonnegative | `engine/physics/PhysicsMotorCommand.cs@a6c914e:L5-L22` | superseded (open question 6: there is no separate work budget or zero-effort mode; an unsupplied drive is clamped to [0, 0] and only bearing friction slows the rotor); "supply loss adds no work" is carried forward | The drive row's bound is the only budget. |
| 8 | Outward-rounded budget spending, `BitDecrement` quotients, error bounds | `engine/physics/PhysicsMotorCommand.cs@a6c914e:L69-L77`, `engine/physics/MotorPredictionSupply.cs@a6c914e:L50-L90` | do not carry forward | Proof-grade CPU rounding; the game-grade envelope applies. |
| 9 | Accepted: supplied motor reaches 6 rad/s; a driven conveyor shares the same shaft speed on the same substep | `CuriousContraptions.tests/MechanicalTests.cs@a6c914e:L62-L143` | carry forward | Positive behaviour. |
| 10 | Accepted: after supply loss an unloaded rotor coasts at constant speed, kinetic energy is unchanged within 1e-8, and no new work is supplied | `CuriousContraptions.tests/MechanicalTests.cs@a6c914e:L62-L143`, `CuriousContraptions.tests/ConveyorRuntimeTests.cs@a6c914e:L114-L135` | superseded: coasting at constant speed (owner, open question 9, 3 November 2026: the rotor spins down under 0.05 N·m bearing friction); "no new work after loss" is carried forward | Loss erases no momentum instantly; the declared friction law removes it. |
| 11 | Accepted: rotor kinetic energy ≤ supplied work; unsupplied rotor energy ≤ 1e-14 over 180 ticks; plain and 25/40/15° rotated mounts | `CuriousContraptions.tests/SourceRotorTests.cs@a6c914e:L80-L123` | carry forward (re-freeze tolerances under f32) | Energy bound and rotated control. |
| 12 | Accepted: locked guide blocks the rotor (speed and travel ≤ 1e-7); unlocking resumes > 0.1 rad/s | `CuriousContraptions.tests/SourceRotorTests.cs@a6c914e:L125-L148` | carry forward (re-freeze tolerances under f32) | Stall control. |
| 13 | Accepted: disconnected motor keeps an applied impulse's speed and energy and reports 0 work | `CuriousContraptions.tests/SourceRotorTests.cs@a6c914e:L150-L167` | superseded: keeping the impulse's speed (owner, open question 9, 3 November 2026: bearing friction spins it down); "0 work while disconnected" is carried forward | The only damping is the declared bearing friction. |
| 14 | Accepted: torque 0.01 gives first-tick speed = torque × axial inverse inertia × tick | `CuriousContraptions.tests/SourceRotorTests.cs@a6c914e:L214-L230` | superseded (decision 1: the torque floor is 0.1 N·m, twice the bearing friction; the low-torque boundary is 0.1 N·m accelerating at (τ − τ_f)/I) | Low-torque acceleration boundary. |
| 15 | Accepted: contact blocking a drive spends no work; reverse command releases | `CuriousContraptions.tests/PhysicsMotorTests.cs@a6c914e:L52-L78`, `CuriousContraptions.tests/MotorPredictionTests.cs@a6c914e:L207-L217` | carry forward | Stall and obstruction. |
| 16 | Accepted: finite work caps acceleration; braking spends no supply; zero supply brakes but never reverses | `CuriousContraptions.tests/PhysicsMotorTests.cs@a6c914e:L169-L193` | superseded (open question 6: the drive's τ_d·h impulse bound caps acceleration with no work budget; an unsupplied drive is clamped to [0, 0] and does not brake, only bearing friction slows the rotor); "braking spends no supply" is carried forward | Torque-bounded drive. |
| 17 | Accepted: a peak-power cap is not replaceable by average work; zero watts cannot start but can brake | `CuriousContraptions.tests/MotorPredictionTests.cs@a6c914e:L52-L81` | superseded (open question 6: the power bound is τ_d·\|ω_t\| from the drive's impulse bound; an unsupplied drive is clamped to [0, 0] and cannot brake, only bearing friction slows the rotor); prediction solver not carried forward | The CPU continuous prediction is not the current solver. |
| 18 | Accepted: motor runs only with a connected, enabled supply; reversed wire rejected; Reset zeroes angle and speed | `CuriousContraptions.tests/ElectricalTests.cs@a6c914e:L96-L135` | carry forward | Negative control and Reset. |
| 19 | Accepted: runtime speed input 0 and 20 applied; −1 and 20.001 out of range; NaN and ∞ rejected; rollback with the tick | `CuriousContraptions.tests/RuntimeBinaryInputTests.cs@a6c914e:L28-L69` | carry forward (bounds) | Carried forward by engine-revolute-drive-target: 0 and 20 applied, −1 and 20.001 refused `RunControlOutOfRange`, NaN/∞ refused at the wire, a held control survives a discarded tick. |
| 20 | Accepted: powered motor reaches commanded speed 3 within 10 ticks (±1e-5); speed 0 supplies no work | `CuriousContraptions.tests/RuntimeBinaryInputTests.cs@a6c914e:L70-L85` | carry forward (re-freeze tolerances under f32) | Speed-regulated bounded drive. |
| 21 | Supply lamp slate→gold over 0.1 s SmoothStep; presentation never alters physics, work or travel | `parts/MotorPart.cs@a6c914e:L40-L45`, `CuriousContraptions.tests/MotorIndicatorTests.cs@a6c914e:L21-L68` | carry forward | Cosmetic binding to committed state. |
| 22 | Index spins at the accepted rate and stays at baseline when speed is 0; Reset restores it | `CuriousContraptions.tests/AngularVelocityAnimationTests.cs@a6c914e:L107-L170` | carry forward | Committed-motion presentation. |
| 23 | Work reads from owned physics, is not double counted, and restores with capture/restore | `CuriousContraptions.tests/MotorAccountingOwnershipTests.cs@a6c914e:L28-L76` | carry forward | Exact Run/Reset. |
| 24 | `Turned` after travel ≥ 2π; `Powered` while supplied; goal kinds `turned` and `powered_after` | `parts/MotorPart.cs@a6c914e:L98-L103`, `engine/MachineData.cs@a6c914e:L152-L152` | carry forward | Objective inputs for the motor levels. |
| 25 | Socket and goal names as string wire tokens (`power_in`, `drive`) | `engine/MachineData.cs@a6c914e:L104-L108` | do not carry forward (string domain use) | Enums end to end; strings only at the serialization boundary. |
| 26 | Level `battery_motor`: locked motor at (3, 1, 0); place a battery; Supply→PowerIn; goal `turned` | `content/puzzles.json@a6c914e:L3425-L3504`, `content/puzzles.json@a6c914e:L3570-L3578` | carry forward | First lesson set-up. |
| 27 | Level `switched_motor`: a bowling ball triggers a placed switch that gates supply; goals `turned` and `powered_after` | `content/puzzles.json@a6c914e:L3581-L3591`, `content/puzzles.json@a6c914e:L3780-L3791`, `content/puzzles.json@a6c914e:L3857-L3872` | carry forward | Switched-supply integration. |
| 28 | Rotated mount: `solar_motor` motor yawed 180° at (−4, 1, −2), supplied by a solar panel | `content/puzzles.json@a6c914e:L6080-L6142`, `content/puzzles.json@a6c914e:L6216-L6224` | carry forward | Rotated control fixture (Epic 13 dependency). |
| 29 | `motor` drives `launcher` (wound spring) in `wind_then_release` | `content/puzzles.json@a6c914e:L31442-L31504`, `content/puzzles.json@a6c914e:L31830-L31859` | carry forward | Story 11.4 integration. |

### Files harvested
- `parts/MotorPart.cs`
- `parts/catalog/motor.tres`
- `parts/scenes/motor.tscn` (script attachment only)
- `engine/SceneRotaryShaft.cs`
- `engine/physics/PhysicsMotorCommand.cs`
- `engine/physics/MotorPredictionSupply.cs`
- `engine/MachineData.cs`
- `engine/MachineEvent.cs`
- `engine/MechanicalNetwork.cs` (coupling; harvested in CAT-019)
- `engine/ConnectionPort.cs` (`MechanicalBinding`; harvested in CAT-019)
- `CuriousContraptions.tests/SourceRotorTests.cs`
- `CuriousContraptions.tests/PhysicsMotorTests.cs`
- `CuriousContraptions.tests/MotorPredictionTests.cs`
- `CuriousContraptions.tests/MotorIndicatorTests.cs`
- `CuriousContraptions.tests/MotorAccountingOwnershipTests.cs`
- `CuriousContraptions.tests/ElectricalTests.cs`
- `CuriousContraptions.tests/RuntimeBinaryInputTests.cs`
- `CuriousContraptions.tests/AngularVelocityAnimationTests.cs`
- `CuriousContraptions.tests/MechanicalTests.cs`
- `CuriousContraptions.tests/MechanicalWorkTests.cs`
- `CuriousContraptions.tests/ConveyorRuntimeTests.cs`
- `content/puzzles.json` (levels `battery_motor`, `switched_motor`, `solar_motor`, `wind_then_release`)
- `ui/WorkshopIcons.cs` (pictogram)
- `engine/physics/AccelerationDrive.cs`, `engine/physics/MechanicalPowerPort.cs`, `engine/physics/MechanicalPortSpeedPath.cs`, `CuriousContraptions.tests/AccelerationDriveTests.cs` (checked, generic CPU drive solver, no motor-specific values)
- `reference/cpu/MachineWorld.cs` (checked, duplicate of the `DriveMotor` command path)
- `reference/p054-guide/puzzles-candidate.json` (checked, duplicate candidate copy of the same levels)
- `reference/p025-production-isolated-20261004/src/ui/WorkshopIcons.cs` (checked, duplicate of the pictogram)

## 5. Acceptance outline

Follow the [CAT-042 row](../requirements.md#current-cat-042) and Story 11.1; do not restate them.
- **Chrome UI recipe.** Open `battery_motor` in the Workshop. Place the battery from the toolbox with actual pointer controls. Use the contextual Connect from battery `Supply` to motor `PowerIn`, then Run. Record bundle identity, the committed rotor pose sequence and the `turned` goal.
- **Positive.** The rotor accelerates to its bounded speed, the lamp turns gold, and supplied work grows (facts 9 and 20).
- **Negative/control.** No battery, a disabled battery, or an activation wire to the motor gives no rotation and zero work (fact 18). A wire into `Drive` from a non-mechanical domain is rejected (Story 11.1b).
- **Boundaries.** Low torque 0.1 N·m, accelerating at (τ − τ_f)/I (decision 1); a stalled or locked shaft (facts 12, 15); supply loss spinning down under the 0.05 N·m bearing friction with no new work (open question 9); rotated mount (`solar_motor`, fact 28); speed 0 and 20.
- **Speed during Run (engine-revolute-drive-target).** A running supplied Motor set to a new speed drives toward it from the next committed step (12 rad/s within 2 % after its Δω ramp plus a tick; 0 rad/s within the rest envelope); set while unsupplied it is committed and spins up once supplied; while Paused a second change replaces the first and Step applies it; out-of-range and wrong-kind values are refused with typed reasons.
- **Run/Reset.** Rotor angle, speed, lamp and supplied work return exactly to authored state (facts 22, 23); a committed Reset clears a held speed control and re-admits the authored target.
- **Save/Load.** Speed, torque, pose and typed connections round-trip through `WorkshopSaveCodec`; a save made during Run after a speed change holds the authored speed.
- **Integrations.** Motor→Conveyor (`conveyor_courier`, CAT-019), `switched_motor`, and later the CAT-057, CAT-018 and CAT-071 consumers.

**Native tests that exist (22 November 2026), `CuriousContraptions.tests/MotorTests.cs`.** Each requirement criterion of [CAT-042](../requirements.md#current-cat-042) against its test:

| Criterion | Test (line) | Control and boundary in the same file |
| --- | --- | --- |
| Finite inertia and bounded effort, not assigned velocity | `ASuppliedMotorSpinsUpToItsSpeedHoldsItAndPaysOnlyItsBearingLoss` (217) | `TheLowestTorqueAcceleratesTheRotorAtItsNetTorqueOverItsInertia` (242); `TheDeclaredParameterBoundsAreAdmitted` (1033) |
| Load and stall | `ASuppliedMotorThatWinsLiftsItsRopeHungLoadAndChargesOnlyWhatItLifts` (619); `AStalledMotorDoesNoWorkOnceItsShaftIsHeld` (732); `ALockedMotorShaftDoesNoWork` (767) | `AnOverloadedMotorIsBackDrivenAndAddsNoWorkWhileItIs` (675); `ASuppliedMotorAtSpeedZeroHoldsItsLoadedShaftStill` (573) |
| Missing and lost supply; explicit damping law | `AnUnpoweredMotorStaysStillDoesNoWorkAndKeepsItsLampSlate` (294); `CuttingTheSupplySpinsTheRotorDownUnderBearingFrictionWithNoFurtherWork` (330) | `EnablingTheBatteryDuringRunSpinsTheMotorUpFromRestAndLightsItsLamp` (361); `AMotorSuppliedThroughAnEitherGateRunsOnlyWhileTheGateIsOn` (497) |
| Wrong-kind connection | `AnActivationWireIntoThePowerInputIsRefused` (316) | an Electrical wire into a `DriveIn` and a second feed are refused in `ConveyorTests.InvalidMechanicalConnections_RefusedAtomically` (529) |
| Rotated mount and connected conveyor | `ARotatedMotorSpinsAboutItsTurnedAxisAtTheSameSpeed` (792) | `ConveyorTests.RotatedConveyor_AlignsTransportAlongRotatedLocalX` (339) |
| Speed during Run | `ASpeedChangeDuringRunDrivesTheShaftToTheNewSpeedFromTheNextCommittedStep` (397) | `ASpeedOfZeroDuringRunBringsTheShaftToRestAndHoldsIt` (425); `ASpeedSetWhileUnsuppliedIsCommittedAndDrivesTheShaftOnceSupplied` (445) |
| Reset, save and load | `ResetRestoresTheAuthoredRotorAndZeroesTheDriveStateAndLamp` (812); `ASavedMotorLoadsWithItsSpeedTorquePoseAndWireAndSpinsAtItsSavedSpeed` (837) | `ASaveDuringRunAndResetKeepTheAuthoredSpeedAfterASpeedChange` (469) |
| Rotor, lamp and link observe committed state | `TheSupplyLampBlendsFromSlateToGoldOverATenthOfASecondWhenSupplied` (265); `MotorRotorArtTests.TheRotorArtFollowsTheCommittedRotorPoseAndResetsToItsScenePose` (1075, same file) | `InAMixedSceneEachLampReadsItsOwnDrivenSlotAndAnUndrivenHingeHasItsOwnRow` (897) |

**Not yet covered (residual acceptance, owned in section 8).**
- *Supply drain.* Given a Battery with a finite store wired to a Motor lifting a rope-hung Weight, when it runs, then the store falls by the drive's committed positive work (within 5 % of the work, which is the bearing-loss share at default values) and the route opens when the store is empty; control: an unloaded Motor at speed 0 draws nothing; boundary: a store smaller than one tick's work opens the route without a negative balance. Planned `MotorTests.ADrivingMotorDrainsItsBatteryByItsCommittedWorkAndStopsWhenItIsEmpty`. Stall guard 30 s of game time.
- *`turned` goal.* Given level 14 `battery_motor` authored playable, when the Motor is supplied, then the `turned` goal completes once committed travel reaches one full turn; control: unsupplied, it never completes within a 20 s guard; boundary: travel just under 2π does not complete. Planned `BatteryMotorLevelTests.TheSuppliedMotorCompletesTheTurnedGoal`, in the goal-evaluation slice that adds the kind.

## 6. Open questions

1. Admitted f32 ranges and units for `speed` (legacy 0–20 rad/s) and `torque` (legacy (0, 100] N·m): **decided by game intent (Story 11.1a).** `speed` admits [0, 20] rad/s inclusive, default 6; `torque` admits [0.1, 100] N·m inclusive, default 20. The torque floor is twice the 0.05 N·m bearing friction, so the weakest Motor still visibly starts (0.0185 rad/s per tick); it replaces the legacy open interval and harvest row 14's 0.01. Non-finite and out-of-range values are refused at every seam; the run control admits the same [0, 20] rad/s and refuses a value outside it with `RunControlOutOfRange`. +`speed` turns the rotor counterclockwise about its +Z (the engine convention); the Drive socket's clockwise-positive sign belongs to 11.1b.
2. Rotor collider primitive (legacy 24-gon prism) under the current Sphere/Box/Plane set: **decided by game intent (Story 11.1a):** the true cylinder collider (Story 10.1), r 0.3 m, height 0.13 m, with its exact inertia.
3. Rotor contact material: legacy inherits restitution 1, friction 0.3: **decided by game intent (Story 11.1a):** the catalogue's rigid-part material, restitution 0, friction 0.3, bounce threshold 0.1 m/s, for the rotor and the housing. A spinning rotor should not bounce what it touches.
4. Whether motor work debits a finite battery store (Story 8.1) or only needs a binary supply: **decided and deferred** (owner, 3 November 2026). Supply is binary for now; the drain becomes one demand law for every powered part (EL-196) in its own slice, the engine story `ENGINE-SUPPLY-DEMAND` (owner decision 3a, 22 November 2026; [deferred work](../../../_bmad-output/implementation-artifacts/deferred-work.md)).
5. Whether `speed` stays a runtime scalar control during Run (legacy `SpeedInput`) or is configuration only: **decided (owner, 3 November 2026): `speed` is adjustable during Run**, delivered by the engine slice `engine-revolute-drive-target` as the generic `RevoluteDriveTarget` run-control lane with a numeric control in the run panel. `torque` stays build-only.
6. Whether the per-tick work allowance torque·speed·Δt and power cap torque·speed are retained as the bounded-supply law: **confirmed by derivation and test (Story 11.1a).** The generic drive row's impulse is at most τ_d·h each substep, so it delivers at most τ_d·|ω|·Δt of work and τ_d·|ω_t| of power with no separate budget; see Work and energy stores. The Motor tests check the committed work against τ_d·|ω_t|·t at every tick, and a held Motor pays only its bearing loss τ_f·ω_t.
7. `turned` goal semantics: cumulative absolute travel ≥ 2π (legacy) versus signed winding: **moved to level work** with the `Turned`/`Powered` events and goal kinds. The committed signed travel the goal will read already exists (`RevoluteStateRead.Travel`).
8. Cylinder collider and cylinder inertia for the rotor: Story 10.1 (ENGINE-CYLINDER), scheduled before the first shaft wheel by the owner on 9 Oct 2026.
9. **Epic story vs requirement conflict.** Story 11.1 says cutting motor power "decelerates the belt to a halt under friction". CAT-042 says supply loss adds no work and does not erase momentum, and any damping is an explicit physical law ([CAT-042](../requirements.md#current-cat-042)); the legacy rotor coasts at constant speed (fact 10). Which governs, and which explicit friction law, if any, stops the belt? **Decided (owner, 3 November 2026): the rotor spins down** under the generic hinge's declared Coulomb bearing friction, τ_f = 0.05 N·m: from 6 rad/s it decelerates at τ_f/I = 2.2 rad/s² and stops after about 2.7 s, with no work added after the cut. Supply loss erases no momentum instantly; the explicit friction law removes it. The coasting claims of facts 10 (constant speed after a cut) and 13 (an impulse kept with no damping) are superseded by this law; fact 11's energy bound still holds.
10. **Epic story vs requirement conflict.** Story 11.1 supplies the motor from a "12V battery", but no voltage is declared by CAT-005, CAT-042 or the legacy binary supply. Is voltage a declared quantity? **Decided by game intent (Story 11.1a):** no. Supply is on/off (owner, 3 November 2026, with drain deferred, question 4). The Motor's electrical node is `("motor", Consumer, 12 V, 0.072 Ω, 0 F, 2000 W)` (`MotorElementDeclaration.cs:124`–`:127`): validated as finite and read by no law (only `Role` is read): unread dead data, to be consumed by `ENGINE-SUPPLY-DEMAND` or removed forward-only.
11. **The `Drive` port, the `Mechanical` connection domain and socket, and the "non-mechanical wire into Drive refused" negative** were delivered by Story 11.1b (commit `a0f6c4c0`, 17 November 2026): the Motor declares `Drive` (section 2), `WorkshopConnectionDomain.Mechanical` exists (`engine/gpu/WorkshopConnections.cs:11`) and `ConveyorTests.InvalidMechanicalConnections_RefusedAtomically` holds the cross-domain refusal. Story 11.1a shipped PowerIn only; an Activation wire into PowerIn is refused (`InvalidConnection`).
12. **Rope ↔ drive creep (found in Story 11.1a).** A rope tied to a supplied Motor's rim did not hold it at rest. Story 11.1a recorded a rim-tied rotor creeping at about 0.0082 rad/s per N·m of torque, and a supplied speed-0 Motor slowly lifting a rope-hung load. The diagnosis corrected both symptoms:
    - The stall creep was a ledger artefact. The drive's committed travel and work grew, while the rotor's pose turned only about 1.5e-4 rad/s.
    - The speed-0 hold really slipped, in the load's direction, while the ledger read the opposite sign.

    No player can build this yet. **Owner decision, 4 November 2026:** 11.1a proves its stall row against a locked hinge and its speed-0 row on an unloaded shaft; the rope variants are delivered by the engine slice `engine-tensile-drive-coupling` (4 November 2026; [deferred work](../../../_bmad-output/implementation-artifacts/deferred-work.md#tensile-revolute-drive-creep)): the rope's length row was soft in the solver's relax pass and committed a false velocity; it is now rigid there, like contacts. `MotorTests.AStalledMotorDoesNoWorkOnceItsShaftIsHeld` and `ASuppliedMotorAtSpeedZeroHoldsItsLoadedShaftStill` prove both rope variants on the drive's ledger and on the committed pose. `ASuppliedMotorThatWinsLiftsItsRopeHungLoadAndChargesOnlyWhatItLifts` proves a supplied Motor lifting its rope-hung load at its target speed, charging no more work than the lift and the bearing loss.
13. **Index bar.** Story 11.1a intended the bar printed flush. As delivered it is a 4 mm thick box standing 4 mm proud of the rotor face with no collider (section 2): a visible-solid defect, repaired by repair part D (coordinator decision 33) as a print at most 0.5 mm proud. It stays part of the rotor art and turns with the committed rotor pose.

No question remains open and none needs the owner. Questions 4, 5 and 9 are the owner's decisions of 3 November 2026; question 7 is owned by `ENGINE-GOALS`.

## 7. Integration

- **Supply collaborators (all delivered; each `Supply` is an Electrical output into the Motor's Electrical `PowerIn`).** Battery (`elements/Battery/BatteryElementDeclaration.cs:56`), Impact switch (`elements/ImpactSwitch/ImpactSwitchElementDeclaration.cs:42`), Hold timer (`elements/HoldTimer/HoldTimerElementDeclaration.cs:28`) and the electrical gates (`MotorTests.AMotorSuppliedThroughAnEitherGateRunsOnlyWhileTheGateIsOn`). Solar panel (CAT-059) is not delivered.
- **Drive collaborators (all delivered; each `DriveIn` is a Mechanical input on a revolute coordinate).** Conveyor, Clutch, Reverse transmission, Wound spring and Fan (`elements/Fan/FanElementDeclaration.cs:64`–`:66`). Signs: +1 per radian on Motor, Conveyor, Clutch and Windmill; −1 on both Reverse gear ports and on the Wound spring's and Fan's `DriveIn`. A rope tied to the rotor's rim is proven in `MotorTests` (rows 573, 619, 732).
- **Campaign (`content/puzzles.json`; numbers are authored positions before decision 24d, each paired with its plan level in [campaign coverage, section 5](readiness/campaign-coverage.md#5-removed-and-converted-levels-owner-decision-24d)).** Playable: 59 `wind_then_release` and 60 `saved_for_later` (run by `LaunchLessonSolveTests.cs:93`). Planned: 14 `battery_motor` (needs the `turned` goal kind), 15 `switched_motor`, 16 `conveyor_courier`, 17 `belt_relay`, 18 `reverse_belt` (16 to 18 are provable from delivered parts after the engine programme, owner decision 3a; [delivery batches section 4](../invest/delivery-batches.md#4-early-level-proof)), 49 `double_cold_start` and 58 `grand_contraption` (multi-goal; after goal evaluation), and 21 `solar_motor`, 22 `solar_shadow` and 24 `delayed_solar` (blocked on the Flashlight and Solar panel).
- **Coverage binding.** Catalogue key `motor` (CAT-042) in `docs/coverage/catalogue-elements.json`. EL-199 has no binding of its own ([EL-199](EL-199-electric-motor.md)).
- **Lesson fixtures.** The Motor is a locked fixture in every level that uses it: each such level records a `FIX-` row in the catalogue fixture table ([current consumers](../invest/current-consumers.md), read by `tools/Coverage/CurrentCatalogue.cs:100`), which admits catalogue parts today; a level that also locks the Motor's parameters or connections needs [S8](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package). Each Planned level is authored into the playable shape by its Early level proof slice.

## 8. Delivery and residuals

Delivered as recorded in section 1. Nothing in the package is to be rebuilt. Every residual slice is blocked on the engine programme (owner decision 3a, 22 November 2026), which precedes further element work.

| Residual | Owner | Tier | Prerequisite |
| --- | --- | --- | --- |
| Supply drain: the drive's committed positive work debits the route's sources; an exhausted source opens the route | `ENGINE-SUPPLY-DEMAND` (sprint key `engine-supply-drive-demand`); it also closes EL-196 and the electrical half of EL-199 and re-qualifies the Powered gate | Engine (solver and supply law) | the engine programme, of which it is a story; it must re-run `MotorTests` rows 217, 573, 619 and 732 unchanged |
| `turned` and `powered_after` goal kinds | `ENGINE-GOALS` | Engine | the engine programme, then the `ENGINE-GOALS` slices |
| Index bar 4 mm proud with no collider | Visible-solid repair part D (Clutch, Reverse gear, Motor, Counter; coordinator decision 33), after the engine programme | Data-only if only the scene and its table row change | the engine programme; a contract for part D |
| Browser qualification | Post-catalogue E2E phase | n/a | every named element delivered |

**EL-199 Electric motor** is not a duplicate closure: it has one delta, the debit of source energy, and closes by acceptance rows inside the `ENGINE-SUPPLY-DEMAND` slice ([delivery batches 2c](../invest/delivery-batches.md#2-duplicate-closures)). It gets no `efficiency` parameter; the declared loss is the 0.05 N·m bearing friction.

**What the `ENGINE-SUPPLY-DEMAND` slice's capability probe must re-check.** That a drive's committed work is still read per hinge (`PhysicsGpuAbi.ReadRevoluteState`); that drive work becomes a request lane of the existing `ElectricalSupply.Reserve`, not a second allocator; that the Motor's unread electrical node (12 V, 0.072 Ω, 0 F, 2000 W, `MotorElementDeclaration.cs:124`–`:127`) is either consumed by the demand law or deleted; and that the Powered gate (120 W drive) and Clutch coil (20 W) are covered by the same law.

## 9. Engine conformance (owner instruction, 22 November 2026)

**Verdict: Conforms with fixes.** One fix in the package: the index bar (point 3). No engine code names this element: a search of `engine/`, `CuriousContraptions.Simulation`, `CuriousContraptions.Bundle` and the abstractions finds "motor" only as the general word for a drive (for example `PrismaticMotorRead`), never the package id or class.

1. **Declaration data only.** Two bodies, scene colliders, one continuous hinge with a declared drive and friction torque, two parameters, two ports, one electrical node, a one-consumer supply graph, three presentation bindings. The electrical node's numbers are validated as finite and read by no law (only `Role` is read): unread dead data, to be consumed by `ENGINE-SUPPLY-DEMAND` or removed forward-only.
2. **Generic laws.** Turning: the supplied revolute drive law, a velocity row toward `speed` with impulse clamped by `torque` inside its own row. Spin-down: the hinge's Coulomb friction row (0.05 N·m), the owner's bearing-friction decision. On and off: the supply network rule on the `PowerIn` route. Speed during Run: the engine's run-control lane `RevoluteDriveTarget`, which any package with one driven hinge may declare. Driving other parts: a mechanical link from the `Drive` port's coordinate. Battery drain is not a Motor behaviour: it is the general demand law, the engine story `ENGINE-SUPPLY-DEMAND`.
3. **Three layers.** Colliders: base and body boxes, housing cylinder, stud and lamp spheres on the static body; the rotor cylinder on its own body. Visuals: the matching meshes and the rotor's index bar. Animation: the lamp's albedo from `CommittedActuatorDrive`. The rotor art follows its body's pose. **Defect:** the index bar is a 4 mm proud box with no collider on a body with no binding; part D reprints it at most 0.5 mm proud.
4. **One-way flow and typing.** Physics → committed drive state → presentation. The run control enters as a typed command through the run-control table, not from presentation. Typed `RunControlLane`, `RevoluteTravel`, `PortId`; f32 values.
5. **Flat-state counts** (from `motor.tscn` and the declaration). 2 bodies (1 dynamic); 6 colliders (2 boxes, 2 cylinders, 2 spheres); 1 revolute joint; 1 drive; 0 sensors, stores, events; 1 electrical node; 1 supply consumer; 2 ports; 2 parameters (1 run control); 3 presentation bindings and 1 body-pose binding.
6. **Performance fitness.** Six primitives and one joint; the housing is static; the revolute drive bounds its torque in its own row, with no world snapshot. Island sleeping is not implemented; a resting body is still stepped, at the cost of its broadphase proxies and rows every substep (no sleep or deactivation code in `CuriousContraptions.Simulation`). Owned by `ENGINE-ISLANDS-SLEEP` (owner decision 3a, 22 November 2026). No small fast body.
7. **Scenario tests.** `MotorTests`, `MotorRotorArtTests` and the generic `RevoluteDriveTests` run the production simulation; the residual rows are scenarios.
