# CAT-028 · fan — element spec

**Current delivery:** delivered by Story 12.2 ([native spec](../../../_bmad-output/implementation-artifacts/spec-12-2-fan.md)). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md). Browser qualification is pending. Part A (the post and ring colliders and the printed arrow) is delivered; the other residual slice, EL-209's closure review, is **blocked on the engine programme (owner decision 3a, 22 November 2026)**; the stories that own this part's gaps are `ENGINE-SPATIAL-QUERIES` (airflow line of sight scans every collider per sample), `ENGINE-ISLANDS-SLEEP` (continuous-work trials confined to the affected island; a resting hub is still stepped) and `ENGINE-SUPPLY-DEMAND` (a Motor driving the shaft does not debit its source).

One current text, repaired 22 November 2026 after the Batch D audit, checked against `elements/Fan/` and its tests in checkout `8d1d68aa`. Citations without `@` are `path:line` in that checkout; `Decl` is `elements/Fan/FanElementDeclaration.cs` and `Scene` is `elements/Fan/fan.tscn`. Citations with `@a6c914e` are the legacy harvest and appear only in the closing appendix.

## 1. Identity and disposition

| Field | Value |
| --- | --- |
| CAT ID / kind | CAT-028 · `fan` |
| Requirement anchor | [CAT-028](../requirements.md#current-cat-028); owner key [CAT-028-I](../invest/current-consumers.md#cat-028-i) |
| Disposition | **Delivered.** Package id and catalogue key `fan`, directory `elements/Fan/`, category Power, palette order 66 (`Decl:17`–`:19`). Published in `4378410e`; slice record status done |
| Mapped identity | [EL-209 Fan](EL-209-fan.md) is delivered as the Electrical and Shaft modes of this package and is closed by its own closure review |
| Capability row | [general-engine-element-map, CAT-028](../general-engine-element-map.md) |

## 2. Declaration

Interfaces: `IHingeElement`, `IEnergyStoreElement`, `IAirflowSourceElement`, `ISupplyElement`, `ISignalElement`, `ISceneGeometryElement` (`Decl:10`–`:11`).

| Parameter | Kind | Unit | Min | Max | Default |
| --- | --- | --- | --- | --- | --- |
| `supply` | Choice: Self-contained, Electrical, Shaft | none | 0 | 2 | 0 |
| `powered` | Choice: Off, On (Self-contained) | none | 0 | 1 | 1 |
| `force` | Real | N | 0 | 40 | 9 |
| `reach` | Real | m | smallest positive f32 | 12 | 5 |
| `width` | Real | m | smallest positive f32 | 4 | 0.85 |

All build-only (`Decl:20`–`:24`).

| Item | Declaration |
| --- | --- |
| Bodies | a static base and one dynamic hub; footprint 2 bodies, 1 moving, 1 revolute constraint (`Decl:25`) |
| Colliders | base body: 0 `col_base` box 0.65 × 0.18 × 1 m at (0, −0.55, 0); 1 `col_post` box 0.13 × 0.5 × 0.15 m at (0, −0.3, 0); 2 `col_visual_ring`, a hidden `MeshInstance3D` sharing the ring's mesh resource and rotation, a full torus of centreline 0.56 m and tube 0.07 m centred at the origin with its axis along X (the last two are visible-solid remainder part A's, with the base's material); hub body: sphere r 0.14 m at the origin. Base material restitution 1, hub restitution 0; both friction 0.3, rolling resistance 0, threshold 0.1 m/s (`Decl:33`–`:34`) |
| Hinge | continuous, about local +X, no drive, no bearing friction; hub 0.15 kg, inertia 0.001176 kg·m² (`Decl:36`–`:40`) |
| Energy store | 14,400 J, full at Run, selected only by Self-contained (`Decl:42`–`:44`) |
| Airflow source | origin at the local origin, direction +X; a strict cone of length `reach` and terminal radius `width`; flow 12 m/s; force rating `force`; power rating `force` × 12 W; internal occluders are exactly the base body's colliders 0, 1 and 2 (base, post, ring) and hub collider 0 (`Decl:46`–`:52`), so the Fan's own stream is not shadowed by them; to another Fan's stream the post and ring are ordinary solids |
| Work inputs | Self-contained: the store, enabled by `powered` and the activation memory. Electrical: the supply consumer. Shaft: the hinge coordinate through `DriveIn`, sense −1 (`Decl:53`–`:55`) |
| Signal and supply | one memory node set by `ActivationIn`; one supply consumer with a continuous-work target; electrical node role Consumer, rated voltage 12, resistance 0.3, capacitance 0, maximum power 480 W (`Decl:69`–`:76`) |

| `PortId` | Role | Domain | Direction | Position (m) | Available in |
| --- | --- | --- | --- | --- | --- |
| `ActivationIn` | ActivationIn | Signal, pulse | Input | (0, 0, 0) | Self-contained |
| `PowerIn` | PowerIn | Electrical | Input | (−0.3, −0.55, 0) | Electrical |
| `DriveIn` | DriveIn | Mechanical, rim r 0.14 m | Input | (−0.7, 0, 0) | Shaft |

(`Decl:57`–`:68`.) An inactive mode's port has no connection; a mode edit that would strand a connection is refused atomically. Engagement classes (planned declaration data, [EL-201](EL-201-drive-chain.md#2-declaration), owned by `ENGINE-MECHANICAL-LINKS`): `DriveIn` admits Smooth only, a light rotor on a 0.14 m pulley, so a Drive belt is admitted and a Drive chain is refused there; the delivered rigid link stays admitted and is unchanged (owner decision 2). Presentation: `Visual/BladeAxis/Blades` rotation from `CommittedFlowSpeed` × 1.5 as an integrated rate (Self-contained, Electrical) or from `CommittedRevoluteAngle` (Shaft); `Visual/Hub` is the physical hub body (`Decl:78`–`:88`). Catalogue description: "A fan blows along its arrow. Choose Self-contained activation, Electrical input or Shaft input. Solid obstacles block the cone." (`Decl:18`).

## 3. Art and solidity

| Node | Mesh | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Base` | box 0.65 × 0.18 × 1 m (`Scene:21`–`:23`, `:88`–`:90`) | solid: `col_base` (`Scene:64`–`:66`) | nothing |
| `Visual/Hub/Surface` | sphere r 0.14 m | solid: `col_hub` on the hub body (`Scene:102`) | the hub body's pose |
| `Visual/Post` | box 0.13 × 0.5 × 0.15 m at (0, −0.3, 0) (`Scene:92`–`:94`) | solid: `col_post` (`Scene:68`–`:70`) | nothing |
| `Visual/Ring` | torus, inner 0.49 m, outer 0.63 m, axis X (`Scene:29`–`:32`, `:96`–`:98`) | solid, hollow: the full torus `col_visual_ring` (`Scene:72`–`:75`); a ball stops on the drawn shroud | nothing |
| three blades | boxes 0.16 × 0.6 × 0.1 m under `Visual/BladeAxis/Blades` | decorative: turned by a binding (owner, 21 November 2026) | the flow or shaft binding |
| `Visual/FlowMark` | three flat `QuadMesh` strokes facing up on the base's top face, 0.2 mm above it (y −0.4598), pointing along +X: the shaft 0.14 × 0.03 m from x 0.10 to 0.24, and two head strokes 0.08 × 0.03 m turned −45° and +45° about Y that meet at the tip, x 0.30 (`Scene:131`–`:144`); they replace the free-standing `Arrow` and `Tip`, a rod and cone in the air from x 0.6 to 1.21 m | decorative: printing, in the old arrow's colour | nothing |

The package is in the visible-solid table ([part A](../../../_bmad-output/implementation-artifacts/spec-elements-visible-solid-remainder.md)): the post has a box collider, the ring a torus collider, and the arrow is a flat print on the base's top face; the source's internal occluders are base, post, ring and hub, so its own stream is unchanged. One intended consequence (D6 of that contract, for the owner to see): a ball reaches the hub only through the ring's opening, so the passive-hub test drops a 0.1 m ball inside the opening. Light follows the same rule as bodies ([EL-176 section 10](EL-176-red-laser-emitter.md#optical-conventions), item 6): art with no collider does not occlude, and optics adds no exception. EL-176 lists the blades with the post and ring as visible-solid defects, while the blades here are decorative by the owner's decision of 21 November 2026; that difference is recorded in the batch summary for the coordinator. Icon: a cyan ring with three blades and a gold hub on a pale stand. At a glance it differs from the Windmill by three flat blades on a short post and no side pulley; the Windmill has four pitched vanes on a tower.

## 4. Engine capabilities

| Capability | Mark | Symbol and test |
| --- | --- | --- |
| Conical source with exact internal occluders | present | `IAirflowSourceElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:40`); `ConicalAirflowTests.ConeHasOpenAxialAndRadialBoundaries` (`CuriousContraptions.tests/ConicalAirflowTests.cs:9`), `AirflowRuntimeTests.OnlyExactInternalCollidersAreExemptFromOcclusion` (`CuriousContraptions.tests/AirflowGeneralityTests.cs:216`) |
| Finite store pays every receiver | present | `IEnergyStoreElement` (`MechanicalContracts.cs:35`); `AirflowRuntimeTests.DifferentFlowSpeedsShareOneFiniteStoreInJoules` (`CuriousContraptions.tests/AirflowRuntimeTests.cs:82`) |
| Electrical continuous work | present | `SupplyContinuousWorkTarget` (`CuriousContraptions.Abstractions/SupplyContractValues.cs:50`); `FanIntegrationTests.ElectricalModePaysOnlyExposedWorkAndNeverUsesSelfContainedEnergy` (`CuriousContraptions.tests/FanIntegrationTests.cs:540`) |
| Shaft input pays by actual travel | present | `CoordinateAirflowInput` (`CuriousContraptions.Abstractions/AirflowValues.cs:38`); `FanIntegrationTests.MotorLinkUsesDeclaredInputSenseAndActualShaftTravel` (`:574`) |
| A Choice selects ports and inputs | present | `ChoicePredicate` (`AirflowValues.cs:23`); `FanAdmissionTests.InactiveConnectionAndRetainedWireModeEditPreserveInstallation` (`CuriousContraptions.tests/FanAdmissionTests.cs:191`) |
| Activation-latched enable | present | `SignalMemoryDescriptor` (`CuriousContraptions.Abstractions/SignalContractValues.cs:64`); `FanGuidanceTests.ColdStartRequiresTheConnectedSwitchOccurrenceBeforeAnyTransfer` (`CuriousContraptions.tests/FanGuidanceTests.cs:64`) |
| Flow-speed presentation | present | `PresentationDriver.CommittedFlowSpeed` (`CuriousContraptions.Abstractions/ElementPresentation.cs:36`); `FanIntegrationTests.NativeBladesUseCommittedPhaseAndPrivateArtworkRestoresOnReset` (`:689`) |
| Hinge-carried contact body | present | `IHingeElement` (`MechanicalContracts.cs:20`); `FanIntegrationTests.PassiveHubReceivesExternalContactWithoutEmittingUnpaidFlow` (`:417`) |

Counts: 8 present, 0 untested, 0 narrower, 0 missing.

## 5. Behaviour and numbers

- **Field.** A strict cone: at axial distance x with 0 < x < `reach`, a sample is inside when its radial distance is below `width` × x / `reach`. Any collider on the streamline blocks the air except the declared internal ones; transparent pipe, bend and funnel shells block air although light passes.
- **Transfer.** The push on a receiver falls as the receiver approaches the 12 m/s flow speed and is zero at or above it. It is bounded by the engine's global 64 m/s² linear and 1024 rad/s² angular limits; there is no further cap. Every receiver's reaction returns to the fan.
- **Budget.** Self-contained spends its 14,400 J store on exposed work only: at 9 N × 12 m/s = 108 W that is about 133 s of full demand, a budget and not a timer (no acceptance row waits for it to run out, so no Run length beyond the 30 s default is needed). Electrical draws accepted work from its connected finite supply and has no hidden store. Shaft converts actual shaft work with its reaction load and no further loss. A Motor's drive work does not yet debit the Motor's source: that is `ENGINE-SUPPLY-DEMAND` (sprint key `engine-supply-drive-demand`); the Motor integration proves paid shaft-to-airflow work only.
- **Light and heavy.** 9 N pushes a Tennis ball at 25.7 m/s², a Basketball (1 kg) at 9 m/s² and a Bowling ball (4 kg) at 2.25 m/s².
- **Shared scale.** Flow 12 m/s is also the planned speed of sound ([EL-044](EL-044-tone-selective-sound-meter.md)).
- **Battery limit.** Electrical demand is `force` × 12 W, so above 10 N it exceeds a default Battery's 120 W limit (`elements/Battery/BatteryElementDeclaration.cs:44`). How the allocation scales the push in that case was not opened for this spec; treat it as unverified.
- **Blades** turn from committed flow (or the shaft) and never produce force.

## 6. Acceptance

All executed in the published package.

| Criterion (CAT-028) | Test |
| --- | --- |
| Light ball guided to the Receiver; no supply, empty, away, out of reach, wall and heavy controls | `FanGuidanceTests.ElectricalConeGuidesFallingTennisToReceiverWithMatchedControls` (`:124`) |
| Strict cone boundary | `ConicalAirflowTests.ConeHasOpenAxialAndRadialBoundaries` |
| Partial block removes its own share | `AirflowRuntimeTests.OneOfFourOccludedSamplesRemovesItsOwnQuarterOfDemand` (`AirflowGeneralityTests.cs:138`) |
| Transparent solids block air | `FanGuidanceTests.TransparentCurvedAndTaperedSolidWallsOccludeOnlyWhenInTheStream` (`:45`), `TransparentPipeBlocksAcrossItsShellButAllowsItsOpenBore` (`:97`) |
| Opposing jets | `AirflowRuntimeTests.OpposingJetsUseOneCoupledVelocity` (`AirflowGeneralityTests.cs:162`) |
| Off and vacuum transfer nothing; own hub does not block | `FanIntegrationTests.ActualHubDoesNotOccludeThePaidConeButOffAndVacuumDoNotTransfer` (`:514`) |
| Mixed rotor and body branches share one budget | `RotaryAirflowRuntimeTests.SharedFiniteStorePaysRotaryAndLinearBranchesRegardlessOfDeclarationOrder` (`CuriousContraptions.tests/RotaryAirflowRuntimeTests.cs:116`) |
| Cold start through a Switch | `FanGuidanceTests.ColdStartRequiresTheConnectedSwitchOccurrenceBeforeAnyTransfer` |
| Run, pause, Reset and save/load in each mode | `FanIntegrationTests.ExposedPayloadLifecyclePreservesPaidStateAndRestoresAuthoredInputs` (`:25`), `FanAdmissionTests.SaveLoadPreservesSelectionAndRecreatesAuthoredBalances` (`:216`) |
| Parameter limits and mode refusals | `FanAdmissionTests.PackageScalarEndpointsAndClosedModeAreAdmittedAtomically` (`:267`) |
| Wrong-kind connection | generic `BundlePortsTests.AWrongDomainOrOutputToOutputLinkIsStillRefused` (`CuriousContraptions.tests/BundlePortsTests.cs:262`) |

Chrome recipe: post-catalogue.

## 7. Integration

Works with Tennis ball, Balloon, Basketball, Bowling ball, Wall, Pipe, both bends, Funnel, Impact switch, Battery, Motor, Windmill (delivered); Wind chimes (Story 14.4, planned: its sail is an airflow receiver). Campaign: first use in levels 11–20 ([campaign reservations](../invest/campaign-reservations.md)); 5 levels in `content/puzzles.json` place it, 2 of them as a fixed cold fan. Coverage binding: catalogue key `fan`. A level with a fixed fan uses the lesson-fixture story (S8).

## 8. Delivery

Delivered; part A's colliders and printed arrow are in the visible-solid remainder candidate (engine review tier). Residual work: EL-209's closure review; the Motor's drive demand (`ENGINE-SUPPLY-DEMAND`); the Battery-limit note in section 5; browser proof, post-catalogue; the CAT-028 checkbox is still open in `requirements.md` (see the [batch summary](readiness/batch-d.md)).

**Prerequisites, in order.** Blocked on the engine programme (owner decision 3a, 22 November 2026), which precedes every further element; in particular `ENGINE-SPATIAL-QUERIES`, `ENGINE-ISLANDS-SLEEP` and `ENGINE-SUPPLY-DEMAND`; the EL-209 closure review runs after the programme and re-checks the airflow rows the spatial-query story touches.

## 9. Open questions

None. Selectable supply, the cone, the global transfer bounds, the static material and the finite energy budget were decided by the owner on 19–20 November 2026 and are delivered.

## Engine conformance

Against the owner's conformance addendum of 22 November 2026 (element-free engine gate in [AGENTS.md](../../../AGENTS.md); design contracts D1–D7 in `_bmad/custom/element-free-engine-design.md`). **Verdict: conforms; the fix of point 3 (post, ring and arrow) is part A.**

1. **Declaration data only.** One declaration class of descriptors, one scene, one icon. No runtime project names the part: a search of `engine/`, `ui/` and every `CuriousContraptions.*` runtime project for `fan` and `Fan` as a word returns nothing. The three supply modes are a declared `Choice` with availability predicates on ports and inputs, not a branch in the engine.
2. **Generic laws.** Push on bodies: the conical airflow law. Blocking: that law's occlusion by any collider except the declared internal ones. Budget: the finite energy-store ledger, the supply network's continuous-work target, or work paid by a revolute coordinate. Start by a Switch: a signal memory node set through a pulse port. Hub contact: the ordinary contact law on a hinge-carried body.
3. **Layers.** Colliders: hidden `col_base`, `col_post` and `col_visual_ring` on the base body and `col_hub` on the hub (the post box and ring torus are part A's). Visuals: the nodes of section 3; none is a collider. Animation: one blade binding per mode, reading committed flow speed or the committed hinge angle and writing nothing back.
4. **One-way flow and typing.** Blade spin follows committed flow; it never produces force (`FanIntegrationTests.DeclaredFlowBindingSamplesQueuedInteriorsAndStopsWithItsActualSource`, `:353`). Modes are a closed choice; ports are typed by domain, direction and role.
5. **Flat state.** 2 bodies (1 static, 1 dynamic); 4 colliders (2 boxes, 1 sphere, 1 torus; the post box and ring torus are part A's); 1 revolute joint; 0 drives; 1 energy store; 1 airflow source with 3 declared inputs; 1 signal node; 1 supply node and 1 electrical node; 3 ports; 1 event kind; 3 presentation bindings and 1 body-pose mapping. All fixed at compile time.
6. **Performance.** Few primitive colliders. The field is a bounded cone (reach at most 12 m, terminal radius at most 4 m). Today each receiver sample's line of sight scans every collider (`CuriousContraptions.Simulation/WorkshopPhysicsEngine.Airflow.cs:149`) and continuous work is solved by whole-world trial substeps; the candidates move to `ENGINE-SPATIAL-QUERIES`' segment query and the trials are confined to the affected island by `ENGINE-ISLANDS-SLEEP`. The hub is a dynamic body in every mode although it is driven only in Shaft mode: one body and one joint per fan. Island sleeping is not implemented (`docs/gpu-f32-physics.md:292` describes it); a resting hub is still stepped. This part works around none of these gaps.
7. **Tests are scenarios.** All rows run the production simulation; `AirflowRuntimeTests` (in `AirflowGeneralityTests.cs`) and `FanAdmissionTests.UnfamiliarReorderedSourcesShareTheirExactStoreThroughCompilerWireAndRuntime` (`:109`) run the same laws on declarations that are not this package.

## Appendix: Historical evidence

Evidence of the legacy part at `a6c914e`, kept so the harvest stays traceable. Nothing here is a requirement, a tolerance or a design instruction; sections 1–9 are the spec. The legacy fan had a cylindrical jet, no external input, CPU sweep paths and proof-grade ledgers; none of that is the design. Legacy tick counts and force values in the cited files are history only.

| # | Fact | Citation |
| --- | --- | --- |
| 1 | Jet exposure: a sample inside the field with no blocking collider on its streamline | `engine/physics/AirJetGeometry.cs@a6c914e:L110-L141`; `engine/AirflowNetwork.cs@a6c914e:L64-L74` |
| 2 | Force falls with receiver speed and is zero at or above the flow speed | `engine/physics/JetTransferImpedance.cs@a6c914e:L26-L46` |
| 3 | Conductance scaled by world pressure and sample weight; weights sum to 1 | `engine/AirflowNetwork.cs@a6c914e:L44-L61`, `L77-L79` |
| 4 | One finite reservoir funded every receiver jointly and was never overdrawn | `engine/physics/StoredFlowSource.cs@a6c914e:L28-L72` |
| 5 | Every receiver reaction returned to the nozzle | `engine/physics/NozzleCoupledJetReceiver.cs@a6c914e:L7-L10` |
| 6 | Sources were evaluated in a fixed order; a source never acted on its own part | `engine/AirflowNetwork.cs@a6c914e:L33-L36`, `L64-L64` |
| 7 | On-axis and off-axis force on a ball | `CuriousContraptions.tests/WindChimeTests.cs@a6c914e:L191-L219` |
| 8 | A clear pipe shell, bends and the funnel blocked air while staying optically clear | `CuriousContraptions.tests/WindChimeTests.cs@a6c914e:L212-L237`; `engine/physics/BodyQueryGeometry.cs@a6c914e:L74-L79` |
| 9 | The same jet accelerated a heavier ball less; a saturated source shared by slip | `CuriousContraptions.tests/WindChimeTests.cs@a6c914e:L238-L279` |
| 10 | Two facing fans cancelled; zero pressure gave zero force | `CuriousContraptions.tests/WindChimeTests.cs@a6c914e:L303-L319` |
| 11 | Invalid parameters were refused | `CuriousContraptions.tests/WindChimeTests.cs@a6c914e:L356-L372` |
| 12 | One or two balls shared the reservoir; an unpowered fan transferred nothing; Reset and save restored | `CuriousContraptions.tests/LinearAirflowSupplyTests.cs@a6c914e:L24-L75` |
| 13 | One fan funded one rotor, two rotors, or a rotor and a ball | `CuriousContraptions.tests/RotorAirflowSupplyTests.cs@a6c914e:L25-L112` |
| 14 | Rotor art spun on presentation only, from the committed tick after activation | `CuriousContraptions.tests/SceneAnimationRunTests.cs@a6c914e:L32-L91` |
| 15 | Solved poses, not rendered transforms, set the jet | `CuriousContraptions.tests/NetworkSpatialOwnershipTests.cs@a6c914e:L260-L283` |
| 16 | Lessons: fan in inventory or fixed and unpowered in the cold-start levels | `content/puzzles.json@a6c914e:L495-L706`, `L2093-L2444`; [CAT-028-I](../invest/current-consumers.md#cat-028-i) |
| 17 | Cold-start pattern: a Bowling ball falls on a Switch wired to the fan | `tools/Campaign/Program.cs@a6c914e:L64-L75`, `L372-L374` |
| 18 | Difficulty assistance values for placing a fan | `tools/Campaign/Program.cs@a6c914e:L544-L549` |
| 19 | A transaction-rollback test: a faulting-tick mechanism, not carried | `CuriousContraptions.tests/RemainingReadingCheckpointTests.cs@a6c914e:L32-L57` |
| 20 | Worked impedance examples | `CuriousContraptions.tests/JetTransferImpedanceTests.cs@a6c914e:L8-L23` |
| 21 | Each receiver branch added its own reaction; invalid materials and speeds were refused | `CuriousContraptions.tests/JetTransferImpedanceTests.cs@a6c914e:L25-L79` |
| 22 | Field boundaries were strict; an off-axis sample received force and a moment | `CuriousContraptions.tests/JetReceiverFieldTests.cs@a6c914e:L34-L92` |
| 23 | Exposure queries neither needed nor created supply | `CuriousContraptions.tests/AirJetGeometryTests.cs@a6c914e:L8-L33` |
| 24 | A blocker crossing the jet removed the force for the crossing only | `CuriousContraptions.tests/AirJetOcclusionTests.cs@a6c914e:L14-L90` |
| 25 | An offset receiver returned force and moment to the nozzle body; an empty store moved nothing | `CuriousContraptions.tests/NozzleCoupledJetReceiverTests.cs@a6c914e:L13-L110` |
| 26 | A stored-flow source never supplied more than its store | `CuriousContraptions.tests/StoredFlowWorldTests.cs@a6c914e:L11-L204` |
| 27 | Boundary crossings within a step: a CPU sweep, not carried | `CuriousContraptions.tests/AirJetBoundaryTests.cs@a6c914e:L1-L213` |
| 28 | Certified force-rate bounds: proof-grade certificates, not carried | `CuriousContraptions.tests/JetTransferForcePathTests.cs@a6c914e:L1-L134`; `engine/physics/JetTransferForcePath.cs@a6c914e:L1-L75` |
| 29 | Cylindrical-surface and streamline sweep paths: CPU solver paths, not carried | `engine/physics/AirJetBoundaryPath.cs@a6c914e:L1-L80`; `engine/physics/AirJetStreamlinePath.cs@a6c914e:L1-L45` |
| 30 | Legacy part, art and catalogue entry | `parts/FanPart.cs@a6c914e:L12-L53`; `parts/catalog/fan.tres@a6c914e:L8-L14`; `engine/PartArt.cs@a6c914e:L22-L27` |

**Files harvested:**
- `parts/FanPart.cs`
- `parts/catalog/fan.tres`
- `parts/scenes/fan.tscn` (no element knowledge: script reference only)
- `engine/AirflowNetwork.cs`
- `engine/PartArt.cs` (ring half-width and line radius conventions)
- `engine/physics/AirJetGeometry.cs`
- `engine/physics/JetTransferImpedance.cs`
- `engine/physics/StoredFlowSource.cs`
- `engine/physics/NozzleCoupledJetReceiver.cs`
- `engine/physics/BodyQueryGeometry.cs`
- `engine/physics/AirJetBoundaryPath.cs` (CPU sweep path)
- `engine/physics/AirJetStreamlinePath.cs` (CPU sweep path)
- `engine/physics/JetTransferForcePath.cs` (rate certificates)
- `reference/cpu/MachinePart.cs`
- `reference/cpu/MachineWorld.cs`
- `CuriousContraptions.tests/WindChimeTests.cs`
- `CuriousContraptions.tests/LinearAirflowSupplyTests.cs`
- `CuriousContraptions.tests/RotorAirflowSupplyTests.cs`
- `CuriousContraptions.tests/SceneAnimationRunTests.cs`
- `CuriousContraptions.tests/NetworkSpatialOwnershipTests.cs`
- `CuriousContraptions.tests/RemainingReadingCheckpointTests.cs`
- `CuriousContraptions.tests/JetTransferImpedanceTests.cs`
- `CuriousContraptions.tests/JetReceiverFieldTests.cs`
- `CuriousContraptions.tests/AirJetGeometryTests.cs`
- `CuriousContraptions.tests/AirJetOcclusionTests.cs`
- `CuriousContraptions.tests/NozzleCoupledJetReceiverTests.cs`
- `CuriousContraptions.tests/StoredFlowWorldTests.cs`
- `CuriousContraptions.tests/AirJetBoundaryTests.cs`
- `CuriousContraptions.tests/JetTransferForcePathTests.cs`
- `content/puzzles.json` (kept)
- `tools/Campaign/Program.cs`
