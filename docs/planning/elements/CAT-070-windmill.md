# CAT-070 · windmill — element spec

**Current delivery:** delivered and published by Story 12.3 ([native spec](../../../_bmad-output/implementation-artifacts/spec-12-3-windmill.md)). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md). Browser, remote and release qualification are unverified. Every residual slice (part C) is **blocked on the engine programme (owner decision 3a, 22 November 2026)**; the stories that own this part's gaps are `ENGINE-SPATIAL-QUERIES` (each capture sample's line of sight scans every collider) and `ENGINE-ISLANDS-SLEEP` (continuous-work trials confined to the affected island; a calm rotor is still stepped).

One current text, repaired 22 November 2026 after the Batch D audit, checked against `elements/Windmill/` and its tests in checkout `8d1d68aa`. Citations without `@` are `path:line` in that checkout; `Decl` is `elements/Windmill/WindmillElementDeclaration.cs` and `Scene` is `elements/Windmill/windmill.tscn`. Citations with `@a6c914e` are the legacy harvest and appear only in the closing appendix.

## 1. Identity and disposition

| Field | Value |
| --- | --- |
| CAT ID / kind | CAT-070 · `windmill` |
| Requirement anchor | [CAT-070](../requirements.md#current-cat-070) with [sequence-task-311](../requirements.md#sequence-task-311); owner key [CAT-070-I](../invest/current-consumers.md#cat-070-i) |
| Disposition | **Delivered and published.** Package id and catalogue key `windmill`, directory `elements/Windmill/`, category Power, palette order 67 (`Decl:13`–`:16`). Commit `47e01bdb`, on main since `4f193bb2`; slice record status done. No named identity maps to it |
| Capability row | [general-engine-element-map, CAT-070](../general-engine-element-map.md) |

## 2. Declaration

Interfaces: `IHingeElement`, `IRotaryAirflowCaptureElement`, `ICoordinateTravelElement`, `ISceneGeometryElement` (`Decl:8`–`:9`).

| Parameter | Kind | Unit | Min | Max | Default |
| --- | --- | --- | --- | --- | --- |
| `radians_per_force` | Real | rad/s per N | 0.1 | 4 | 0.6666667 |

(`Decl:17`.)

| Item | Declaration |
| --- | --- |
| Bodies | a static support and one dynamic rotor; footprint 2 bodies, 1 moving, 1 revolute constraint (`Decl:18`) |
| Support colliders | base box 1.1 × 0.16 × 0.9 m at (0.25, −1.08, 0); tower box 0.22 × 1.1 × 0.25 m at (0.25, −0.55, 0); nacelle box 0.65 × 0.45 × 0.55 m at (0.25, −0.1, 0); guard Tube at (−0.12, 0, 0), axis X, bore radius 0.745 m, outer radius 0.815 m, half-length 0.035 m; output wheel cylinder r 0.24 m, 0.12 m long, at (0.25, −0.1, 0.44), axis Z (`col_output`, base collider index 4, visible-solid remainder part C) |
| Rotor colliders | hub sphere r 0.14 m; four blade boxes with half-extents (0.0375, 0.26, 0.105) m, centred 0.4 m out on arms 90° apart, each pitched 30° about its arm |
| Material | support and rotor: restitution 0, friction 0.3, rolling resistance 0, threshold 0.1 m/s (`Decl:24`–`:25`) |
| Hinge | continuous, at (−0.12, 0, 0) about local X, no drive, no bearing friction; rotor 1 kg, axial inertia 0.245 kg·m², transverse (3 × 0.7² + 0.075²)/12 kg·m² (`Decl:12`, `:26`–`:32`) |
| Rotary capture | on the hinge coordinate: four samples of weight 0.25 at (−0.12, ±0.42, 0) and (−0.12, 0, ±0.42) in the support's frame; pitch limit 0.4 m; response `radians_per_force`; cut-in 0.05 N; unloaded target cap 12 rad/s; its own ten colliders, the output wheel's included, are the exempt internal occluders (`Decl:33`–`:40`) |
| Travel observer | one, on the hinge coordinate, threshold 2π of absolute accepted travel; publishes `CoordinateTravelReached` (`Decl:41`–`:44`) |

| `PortId` | Role | Domain | Direction | Position (m) | Binding |
| --- | --- | --- | --- | --- | --- |
| `Drive` | Drive | Mechanical | Output | (0.25, −0.1, 0.52) | hinge coordinate 1:1; rim r 0.24 m centred (0.25, −0.1, 0.44) (`Decl:45`–`:49`) |

Engagement classes (planned declaration data, [EL-201](EL-201-drive-chain.md#2-declaration), owned by `ENGINE-MECHANICAL-LINKS`): `Drive` admits Smooth only, a light sail shaft, so a Drive belt is admitted and a Drive chain is refused there. The delivered rigid link stays admitted and is unchanged (owner decision 2).

Presentation: `Visual/Output` rotation = −1 × the committed hinge angle; `Visual/Rotor` is the physical rotor body (`Decl:50`–`:54`). Catalogue description: "Airflow turns the blades and drives connected machines. Rear airflow reverses rotation; loads slow the shaft." (`Decl:14`).

## 3. Art and solidity

| Node | Class | Moved by |
| --- | --- | --- |
| `Visual/Base`, `Tower`, `Nacelle` | solid: box colliders `col_base`, `col_tower`, `col_nacelle` (`Scene:80`–`:88`) | nothing |
| `Visual/Guard` (imported ring) | hollow: Tube collider `col_guard` (`Scene:92`) | nothing |
| `Visual/Rotor/Hub` and four `Blade` boxes | solid, on the hinge body: `col_hub` and four `col_blade` (`Scene:130`–`:193`) | the rotor body's pose |
| four blade `Tip`s | decorative: each lies inside its blade's box | the rotor body's pose |
| `Visual/Output/Wheel` (cylinder r 0.24 m, 0.12 m long, at (0.25, −0.1, 0.44)) | solid: cylinder collider `col_output` (`Scene:99`, part C); it stands 0.105 m in front of the nacelle, which repair part D seats (coordinator decision 39) | the output binding (about its own axis) |
| `Visual/Output/Spoke0`, `Spoke1` | decorative: turned by the output binding | the output binding |

[Visible-solid remainder part C](../../../_bmad-output/implementation-artifacts/spec-elements-visible-solid-remainder-c.md) adds the package's row to the visible-solid table: a static cylinder collider `col_output` for the wheel at base collider index 4 (base colliders 4 → 5, its D5), added to the capture's own exemptions (ten in all) so rear airflow still reaches the samples, and a matcher that reads the imported guard ring (bore 0.745 m, outer 0.815 m, half-length 0.035 m), which matches `col_guard`. Colours: navy base, cream tower, nacelle, guard and wheel, cyan blades, gold hub, tips and spoke marks. Icon: four cyan vanes in a ring on a cream tower. At a glance it differs from the Fan by four pitched vanes, the tower and the side pulley.

## 4. Engine capabilities

| Capability | Mark | Symbol and test |
| --- | --- | --- |
| Rotary airflow capture over real pose samples | present | `IRotaryAirflowCaptureElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:55`); `RotaryAirflowRuntimeTests.FourActualPoseSamplesLoseOnlyOccludedWeight` (`CuriousContraptions.tests/RotaryAirflowRuntimeTests.cs:91`) |
| Capture law (pitch selection, inclusive cut-in) | present | `CuriousContraptions.Abstractions/RotaryAirflowLaw.cs`; `RotaryAirflowLawTests.LowFlowSelectsFirstZeroRatherThanFarSideOfPlateau` (`CuriousContraptions.tests/RotaryAirflowLawTests.cs:10`), `CutInIsInclusiveForceThreshold` (`:87`) |
| Finite-inertia rotor that coasts and accepts backdrive | present | `IHingeElement` (`MechanicalContracts.cs:20`); `RotaryAirflowRuntimeTests.PassiveRotorAcceptsBackdriveAboveTargetCapAndCoastsWithoutSourceWork` (`:75`) |
| Accepted-travel occurrence | present | `ICoordinateTravelElement` (`MechanicalContracts.cs:50`); `WindmillIntegrationTests.RotorTravelEventPaidMotionAndLifecycleUseCommittedPhysicalState` (`CuriousContraptions.tests/WindmillIntegrationTests.cs:13`) |
| Shaft output through a Mechanical link to driven loads | present | `PortRole.Drive`; `WindmillIntegrationTests.PaidShaftTransportsRemoteCargoAndReverseStagesControlDirection` (`:79`) |
| Hollow Tube collider | present | `ColliderShapeKind.Tube` (`CuriousContraptions.Abstractions/PhysicsDeclarationValues.cs:12`); `WindmillAdmissionTests.ActualPackageSolidsContactProbesWhileBoreAndMissRemainClear` (`CuriousContraptions.tests/WindmillAdmissionTests.cs:177`) |

Counts: 6 present, 0 untested, 0 narrower, 0 missing.

## 5. Behaviour and numbers

- **Drive.** The signed net force of the exposed samples along the axis sets an unloaded target speed of force × `radians_per_force`, capped at 12 rad/s: a default Fan (9 N) asks for 6 rad/s. Below 0.05 N of net force the rotor is passive (0.049 N passive, 0.05 N active).
- **No velocity is assigned.** Finite inertia, load and backdrive decide the actual motion. Active branches share the Fan's force, power and energy allocation and debit each source; a calm, blocked or depleted source leaves passive resistance only, and the rotor coasts to rest.
- **Direction and exposure.** Rear airflow reverses rotation; edge-on air and equal opposed sources do not drive. A Wall over one sample removes that sample's quarter; moving exposure is re-sampled each substep.
- **Pitch selection.** The capture chooses the first boundary of the positive-drive interval from zero pitch, up to the 0.4 m limit; in the low-flow clipped case torque is zero at and above the target in either direction.
- **Output.** The drive port turns loads through its 0.24 m rim; a heavy load slows the shaft, and an external drive can turn the windmill above its aerodynamic target.
- **Travel.** One occurrence when absolute accepted travel first reaches a full turn, including across a reversal.
- **Bellows.** The planned Bellows must carry cargo past the same Conveyor at default settings (Story 12.4 row B3); whether a short burst suffices against a 0.245 kg·m² rotor is that slice's finding, and it may not be met by raising this part's response.

## 6. Acceptance

All executed in the published package.

| Criterion (CAT-070) | Test (`WindmillIntegrationTests` unless named) |
| --- | --- |
| Four samples; partial occlusion removes its share | `RotaryAirflowRuntimeTests.FourActualPoseSamplesLoseOnlyOccludedWeight` |
| Signed rear drive, edge-on and opposing controls | `ActualSourcePoseAndSignedExposureDetermineDrive` (`:267`), `OpposingSourcesPayTheirOwnWorkAndInactiveBranchesPayNothing` (`:413`), `RotaryAirflowLawTests.EqualOppositionAndEdgeOnArePassive` (`:96`) |
| Response 0.1–4 admitted, others refused | `WindmillAdmissionTests.ResponseAdmitsOnlyCanonicalFiniteRange` (`:273`), `WindmillArtworkTests.ResponseEditorRefusesInvalidValuesWithoutChangingUndoOrInstalledConstruction` (`CuriousContraptions.tests/WindmillArtworkTests.cs:11`) |
| Cut-in 0.05 N inclusive | `RotaryAirflowRuntimeTests.InclusiveCutInControlsActualTransfer` (`:43`) |
| Unloaded target and cap | `RotaryAirflowRuntimeTests.PaidFiniteRotorApproachesItsSignedUnloadedOperatingRegion` (`:16`) |
| Load, backdrive and coast | `HeavyCargoLoadsTheFiniteShaftAndUnloadedRigAcceleratesAgain` (`:151`), `ConnectedExternalDriveBackdrivesActualWindmillAboveAerodynamicTarget` (`:375`), `ElectricalCutPreservesCoastThenReenableRestartsCapture` (`:179`) |
| Connected and disconnected cargo, reversers, blocked jet | `PaidShaftTransportsRemoteCargoAndReverseStagesControlDirection` |
| Reversal passes through zero | `ReversingBeforeOneTurnAccumulatesAbsoluteTravelWithoutInstantaneousFlip` (`:207`) |
| Works moved and turned | `TranslatingAndRotatingTheAssemblyPreservesItsLocalCaptureBehavior` (`:242`) |
| Art follows committed state; Reset restores | `WindmillArtworkTests.NativeRotorAndOutputFollowCommittedStateAndResetAuthoredTransforms` (`:43`) |
| Save, load and Reset with a connection | `ConnectedNondefaultSaveLoadDeliversPaidCargoAndResetsAuthoredState` (`:316`) |
| Invalid ports and wrong-kind links refused | `WindmillAdmissionTests.InvalidPortsLeaveTheInstalledConstructionUntouched` (`:148`); generic `BundlePortsTests.AWrongDomainOrOutputToOutputLinkIsStillRefused` (`CuriousContraptions.tests/BundlePortsTests.cs:262`) |

Chrome recipe: post-catalogue.

## 7. Integration

Works with Fan (all three supplies), Conveyor, Reverse transmission, Motor, Wall, Battery (delivered); Bellows (Story 12.4, in flight: the burst integration is that slice's row B3). Campaign: first use in levels 31–40 ([campaign reservations](../invest/campaign-reservations.md)); no level in `content/puzzles.json` places it yet. Coverage binding: catalogue key `windmill`. Lesson-fixture story (S8) only where a level pre-places it.

## 8. Delivery

Delivered and published. Part C adds the wheel collider, its capture exemption and the table row (engine review tier). Residual work: seating the wheel on the nacelle (repair part D); the Bellows integration; browser proof, post-catalogue. `requirements.md` still says publication is pending and keeps the CAT-070 checkbox open (see the [batch summary](readiness/batch-d.md)). The blades collide, as delivered: [DESIGN.md](../../../DESIGN.md) "Windmill" is being updated to say so (owner decision 4, 22 November 2026).

**Prerequisites, in order.** Blocked on the engine programme (owner decision 3a, 22 November 2026), which precedes every further element; in particular `ENGINE-SPATIAL-QUERIES` and `ENGINE-ISLANDS-SLEEP`; then repair part D, which seats the wheel. Part C runs before the programme (owner decision 22). The Bellows row waits for the Bellows slice (CAT-010).

## 9. Open questions

None. Catalogue wording, the cut-in unit (a force, 0.05 N), the contact material and reuse of the Tube collider for the guard were decided in Story 12.3 and are delivered.

## Engine conformance

Against the owner's conformance addendum of 22 November 2026 (element-free engine gate in [AGENTS.md](../../../AGENTS.md); design contracts D1–D7 in `_bmad/custom/element-free-engine-design.md`). **Verdict: conforms (point 3: the output wheel's collider is added by part C).**

1. **Declaration data only.** One declaration class of descriptors, one scene, one imported guard mesh, one icon. No runtime project names the part: a search of `engine/`, `ui/` and every `CuriousContraptions.*` runtime project for `windmill` and `Windmill` returns nothing. There is no per-part occurrence, "active" rule or observe loop: the part declares a generic travel observer.
2. **Generic laws.** Spin-up, reversal and cut-in: the rotary airflow capture law on a declared revolute coordinate. Coast and backdrive: the hinge with finite inertia and no drive. Partial blocking: occlusion of each sample by any collider except the declared internal ones. Output: the mechanical transmission law through a declared drive port and rim. First full turn: the coordinate-travel observation with a declared threshold.
3. **Layers.** Colliders: hidden `col_base`, `col_tower`, `col_nacelle`, `col_guard`, `col_output` (part C), `col_hub` and four `col_blade`. Visuals: the nodes of section 3; none is a collider. Animation: one binding turns `Visual/Output` from the committed hinge angle; the wheel's art turns about its own axis, which a static cylinder collider matches at every angle.
4. **One-way flow and typing.** Art reads the committed coordinate; nothing flows back. The port is typed Mechanical, Output, Drive.
5. **Flat state.** 2 bodies (1 static, 1 dynamic); 10 colliders (7 boxes, 1 tube, 1 sphere, 1 cylinder; 9 before part C); 1 revolute joint; 0 drives; 1 rotary capture with 4 samples; 1 travel observer; 0 stores and domain nodes; 1 port; 1 event kind; 1 presentation binding and 1 body-pose mapping. All fixed at compile time.
6. **Performance.** Ten colliders: each of the four blades is a drawn solid on a moving body that a ball can strike, the output wheel is a drawn solid a ball can land on, and the guard is a true hollow ring. None is cosmetic. Four capture samples cost four occlusion queries per source per substep; today each query scans every collider, and `ENGINE-SPATIAL-QUERIES` replaces the scan with its segment query. A calm rotor comes to rest. Island sleeping is not implemented (`docs/gpu-f32-physics.md:292` describes it); a resting rotor is still stepped. Owned by `ENGINE-ISLANDS-SLEEP`; nothing in this part works around either gap.
7. **Tests are scenarios.** All rows run the production simulation; `WindmillAdmissionTests.UnfamiliarRepeatedCapturesKeepExactFramesAndHingesWhenReordered` (`:14`) runs the same law on declarations that are not this package.

## Appendix: Historical evidence

Evidence of the legacy part at `a6c914e`, kept so the harvest stays traceable. Nothing here is a requirement, a tolerance or a design instruction; sections 1–9 are the spec. The legacy part had its own `Turned` occurrence, an "active while" rule, a per-part observe loop, CPU calibration paths and work-ledger certificates; none of that is the design. Legacy tick counts, speed bands and calibration values in the cited files are history only.

| # | Fact | Citation |
| --- | --- | --- |
| 1 | Legacy unloaded response, cut-in and effective resistance of the capture | `engine/physics/RotaryCaptureMaterial.cs@a6c914e:L47-L127`; `CuriousContraptions.tests/WindmillTests.cs@a6c914e:L25-L36` |
| 1a–1d | Legacy calibration cases and the cut-in boundary | `CuriousContraptions.tests/RotaryCaptureMaterialTests.cs@a6c914e:L15-L33`, `L49-L65` |
| 2 | Accepted air force fell as the rotor turned | `CuriousContraptions.tests/WindmillTests.cs@a6c914e:L37-L42` |
| 3 | Air force was read from committed transfer impulses along the hinge axis | `engine/AirflowNetwork.cs@a6c914e:L102-L121`; `parts/WindmillPart.cs@a6c914e:L63-L67` |
| 4 | Fan and windmill: spin-up, belted conveyor, reversers, fan off and coast, Reset, save and load; back wind reversed each | `CuriousContraptions.tests/WindmillTests.cs@a6c914e:L47-L110` |
| 5 | The assembly worked in five orientations and had exactly one mechanical port | `CuriousContraptions.tests/WindmillTests.cs@a6c914e:L111-L133` |
| 6 | Exposure: full, partial with one sample blocked, and blocked | `CuriousContraptions.tests/WindmillTests.cs@a6c914e:L134-L156` |
| 7 | A ball on a conveyor was carried only when belted, unblocked and the fan was on | `CuriousContraptions.tests/WindmillTests.cs@a6c914e:L157-L181` |
| 8 | Edge-on, weak and opposing air gave no drive | `CuriousContraptions.tests/WindmillTests.cs@a6c914e:L182-L210` |
| 9 | Reversing the wind crossed zero along the finite-inertia motion | `CuriousContraptions.tests/WindmillTests.cs@a6c914e:L211-L279` |
| 10 | The shaft capped; a Motor could not also drive the belted conveyor; out-of-range gains were refused | `CuriousContraptions.tests/WindmillTests.cs@a6c914e:L280-L298` |
| 11 | Invalid sample weights failed before any receiver changed | `CuriousContraptions.tests/WindmillTests.cs@a6c914e:L299-L322` |
| 12 | Forward and reverse air drove the rotor; it coasted down after the fan stopped | `CuriousContraptions.tests/SourceRotorTests.cs@a6c914e:L54-L66`, `L169-L212` |
| 13 | Solved poses, not rendered transforms, set the rotor force | `CuriousContraptions.tests/NetworkSpatialOwnershipTests.cs@a6c914e:L260-L283` |
| 14 | One fan funded two rotors, or a rotor and a ball, from one reservoir | `CuriousContraptions.tests/RotorAirflowSupplyTests.cs@a6c914e:L25-L112` |
| 15 | Bellows, windmill and conveyor chain | `CuriousContraptions.tests/BellowsTests.cs@a6c914e:L82-L254` |
| 16 | Shaft angle, speed and travel were read from the owned hinge, at two placements, and restored on Reset | `CuriousContraptions.tests/AxialMotionOwnershipTests.cs@a6c914e:L48-L142`; `CuriousContraptions.tests/BodyLocalBoxTests.cs@a6c914e:L25-L77` |
| 17 | Declaring a transfer on an undeclared rotor body was refused before Run | `CuriousContraptions.tests/SceneMechanicalTransferTests.cs@a6c914e:L18-L32` |
| 18 | No level used the windmill | `docs/coverage/catalogue-elements.json` (empty fixture list) |
| 19 | More calibration cases; invalid capture declarations refused | `CuriousContraptions.tests/RotaryCaptureMaterialTests.cs@a6c914e:L35-L178` |
| 20 | Requested speed was continuous before the cut-in | `CuriousContraptions.tests/RotaryCaptureForcePathTests.cs@a6c914e:L153-L164` |
| 21 | In calm air a hinge with no branches damped rotor speed and added no energy | `CuriousContraptions.tests/RotaryCaptureMotionTests.cs@a6c914e:L71-L91`; `CuriousContraptions.tests/RotaryCaptureDeclarationTests.cs@a6c914e:L291-L304` |
| 22 | A rotating source was re-sampled within the step | `CuriousContraptions.tests/RotaryCaptureMotionTests.cs@a6c914e:L11-L69` |
| 23 | Supply lifecycle: disabling, partial exposure, mixed branches, exhaustion and recharge | `CuriousContraptions.tests/RotaryCaptureDeclarationTests.cs@a6c914e:L89-L289` |
| 24 | Legacy part, art and catalogue entry | `parts/WindmillPart.cs@a6c914e:L9-L98`; `parts/catalog/windmill.tres@a6c914e:L6-L12`; `engine/TubeProxy.cs@a6c914e:L6-L6`; `engine/PartArt.cs@a6c914e:L22-L23` |

**Files harvested:**
- `parts/WindmillPart.cs`
- `parts/catalog/windmill.tres`
- `parts/scenes/windmill.tscn` (no element knowledge: script reference only)
- `engine/AirflowNetwork.cs`
- `engine/PartArt.cs` (ring half-width convention)
- `engine/TubeProxy.cs`
- `engine/physics/RotaryCaptureMaterial.cs`
- `engine/physics/RotaryCaptureDeclaration.cs` (CPU preparation and trajectory certificates)
- `engine/physics/RotaryCaptureForcePath.cs` (CPU calibration-path certificates)
- `reference/cpu/MachinePart.cs`
- `CuriousContraptions.tests/WindmillTests.cs`
- `CuriousContraptions.tests/SourceRotorTests.cs`
- `CuriousContraptions.tests/RotorAirflowSupplyTests.cs`
- `CuriousContraptions.tests/NetworkSpatialOwnershipTests.cs`
- `CuriousContraptions.tests/BellowsTests.cs`
- `CuriousContraptions.tests/AxialMotionOwnershipTests.cs`
- `CuriousContraptions.tests/BodyLocalBoxTests.cs`
- `CuriousContraptions.tests/SceneMechanicalTransferTests.cs`
- `CuriousContraptions.tests/RotaryCaptureMaterialTests.cs`
- `CuriousContraptions.tests/RotaryCaptureForcePathTests.cs`
- `CuriousContraptions.tests/RotaryCaptureMotionTests.cs`
- `CuriousContraptions.tests/RotaryCaptureDeclarationTests.cs`
