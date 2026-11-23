# CAT-067 · weight declaration readiness spec

This is the CAT-067 spec for the Weight: its body, its `mass` parameter and its integrations. Rope route, length and tension facts are in [CAT-058 rope_anchor](CAT-058-rope_anchor.md); two-pulley counterbalance facts are in [CAT-053 pulley](CAT-053-pulley.md). Sections 1 to 3 and 5 to 9 describe the delivered package, checked on 22 November 2026 against checkout `8d1d68aa`; published main is now `25df3cd5`, and its two later commits touch none of this package's files. Section 4 is the legacy harvest at `a6c914e`, kept verbatim as evidence; it is not a description of the current part. Browser qualification is pending until the post-catalogue phase.

**Visible = solid: delivered.** The base, both bands and the lifting eye collide as drawn. The scene carries four hidden proxies beside `col_cylinder` (`elements/Weight/weight.tscn:76`–`:94`: `col_visual_base` cylinder, `col_visual_bandlower`, `col_visual_bandupper` and `col_visual_eye` tori), each bound to `mass` (`elements/Weight/WeightElementDeclaration.cs:63`–`:75`), and the package's visible-solid row lists the body and base as solid and the bands and eye as hollow with a declared fit of 4e-6 m (`CuriousContraptions.tests/VisibleSolidColliderTests.cs:123`). No repair contract names the Weight; `spec-engine-art-solidity.md` keeps its `Fit` as a declared per-entry tolerance (decision D4 there).

## 1. Identity

| Field | Value |
| --- | --- |
| CAT ID | CAT-067 |
| Kind | `weight`, catalogue title "Weight" |
| Requirement anchor | [CAT-067](../requirements.md#current-cat-067); retained behaviour [todo-141](../requirements.md#todo-141) and [todo-140](../requirements.md#todo-140) |
| Mapped identities | [EL-204 Weight](../invest/named-elements.md#element-204), a candidate Duplicate closure (section 8) |
| Roadmap story | [10.2a Weight](../../../_bmad-output/planning-artifacts/epics.md#catalogue-slice-ownership); retain every CAT-067 mode and mapped named-identity obligation. |
| Status | **Delivered.** Package id `weight`, directory `elements/Weight`. Slice records: body [10.2a](../../../_bmad-output/implementation-artifacts/spec-10-2a-weight.md) (commit `28a2d302`), Tie and rope [10.2b](../../../_bmad-output/implementation-artifacts/spec-10-2b-rope-tie-load.md) (`d2862ebe`), pendulum and counterweight qualification [10.4](../../../_bmad-output/implementation-artifacts/spec-10-4-counterweight.md). Browser case `cat-067` pending (post-catalogue E2E). Every residual slice (section 8) is blocked on the engine programme (owner decision 3a, 22 November 2026); the story that owns this part's gap is `ENGINE-ISLANDS-SLEEP` (a resting Weight), and the rope's missing strand collider belongs to `ENGINE-CONNECTION-MATERIALS` (solid strand). |
| Disposition | Delivered catalogue part. EL-204 is a candidate Duplicate closure of this part ([section 8](#8-delivery-and-residuals)). |

## 2. Declaration

### As delivered (checked 22 November 2026)

This table is the current declaration (`elements/Weight/WeightElementDeclaration.cs`).

| Item | Delivered value | Source |
| --- | --- | --- |
| Interfaces | `IMechanicalElement`, `ISceneGeometryElement`, `IParameterizedGeometryElement` | `:12` |
| Body | one dynamic body, footprint 1 body / 1 moving, no constraint | `:37`, `:85` |
| Colliders (at 4 kg) | 0 cylinder r 0.35166 m, half-height 0.27926 m, centre y 0.27926; 1 base cylinder r 0.36, half-height 0.025, y 0.0517; 2 and 3 band tori, ring radius 0.3558, tube 0.0075, y 0.1862 and 0.3724; 4 eye torus, ring radius 0.14, tube 0.0175, y 0.5771, standing. All linear in `mass` | `:57`–`:76`; `weight.tscn:13`–`:41` |
| Mass and inertia | mass = `mass`; solid-cylinder moments (transverse, axial, transverse) as polynomials of `mass`; centre of mass at the half-height; gravity (0, −9.81, 0); the added colliders contribute no mass | `:96`–`:107` |
| Material | restitution 0.08, friction 0.3, rolling resistance 0.03, bounce threshold 0.1 m/s; no drag | `:52` |
| Parameter | `mass`, Real, kg, default 4, minimum 0.25, maximum 8, editor policy Clamp, Build only | `:33`–`:34` |
| Port | `PortId.Tie`, domain Rope, Bidirectional, role `Tie`, attachment `Load`, normal +Y, position (0, 0.5771 × (0.136734 × mass + 0.453070), 0): 0.5771 m at 4 kg | `:39`–`:45` |
| Stores, events, payload | none; `PayloadPolicy.None` | `:81`, `:93` |
| Presentation | `Visual` uniform scale = 0.136734 × mass + 0.453070; pick radius 0.7 m | `:14`–`:20` |
| Palette | category Motion, order 31, colour (0.27, 0.39, 0.61) | `:22`–`:30` |

### Notes on the declaration

- **Shape and size.** One dynamic solid cylinder, axis +Y. The body origin is the base centre and the centre of mass is one half-height above it, so a mass edit grows the Weight upward. Size is linear in mass and exact at 0.25 and 8 kg against the legacy art law (section 4): r = 0.04808397 × mass + 0.15932827 and half-height h = 0.03818433 × mass + 0.12652539. Values: 0.25 kg → r 0.17135, h 0.13607; 4 kg → 0.35166, 0.27926; 8 kg → 0.544, 0.432. A linear law keeps the collider bindings linear, and heavier Weights are visibly larger.
- **Inertia.** Declared in the mass frame as polynomials of `mass` for a solid cylinder, m·r²/2 about the axis and m·(r²/4 + h²/3) across it (`:95`–`:107`). It is not compiled from the colliders; the base, band and eye colliders add no mass.
- **Material.** Restitution 0.08, friction 0.3, bounce threshold 0.1 m/s, no drag; rolling resistance 0.03, the Bowling ball's value, chosen in 10.2a because the legacy had none.
- **Parameter.** The generic parameter editor edits `mass` in kg in Build (Clamp policy). Admission, save validation and wire decode refuse an out-of-range, non-finite, missing or unknown value, with no default fill.
- **Port.** The `Tie` is at the centre of the drawn eye and moves with `mass` through one declared position binding, which the route measurement, the compiler and the art all read.
- **Joints, sensors, stores.** None. As a rope end it has role `Load`; the rope law belongs to the connection ([CAT-058](CAT-058-rope_anchor.md)).
- **"Locked"** in a level means fixed during editing only, never immovable in simulation.
- **Art.** Blue cylinder (catalogue colour (0.27, 0.39, 0.61), `#45639c`), cream `#fff8e9` base and two fixed cream bands, gold `#f7cb52` standing eye; DESIGN.md: "Blue cylindrical loads with cream bands and gold eyes; heavier loads are visibly larger". All five meshes sit under `Visual`, which a declared parameter binding scales uniformly by 0.136734 × mass + 0.453070. There is no cosmetic curve. Icon: `elements/Weight/icon.svg`, a trapezoid body with a ring on top and two band strokes.
- **Catalogue.** Category Motion (the legacy "Ropes" is not an `ElementCategory`), description "A heavy cylindrical load. Larger weights are heavier." The Free palette lists it from the bundle. Planned levels `counterweight` and `pulley_depth` (authored positions 19 and 20, placed at plan levels 12b and 12c) each place a locked mass-1 load and stock one Weight; the solution Weight has mass 4.

## 3. Engine capabilities

Capability families: see the CAT-067 row of the [general-engine element map](../general-engine-element-map.md) and the `weight` entry in [catalogue-elements.json](../../coverage/catalogue-elements.json). Not duplicated here.

**Capability probe (22 November 2026).** Tests were found by name and line; none was run. No engine code names the Weight. Counts: 10 present, 0 present-untested, 0 narrower, 0 missing.

| Capability | Status | Symbol | Exercising test |
| --- | --- | --- | --- |
| Solid cylinder collider on a dynamic body, size bound to a parameter | present | `ColliderParameterBinding`, `IParameterizedGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:27`, `:29`) | `WeightTests.TheCompiledMassSizeCentreAndInertiaFollowTheMassParameter` (`CuriousContraptions.tests/WeightTests.cs:139`) |
| Declared mass frame with parameter-polynomial moments | present | `MassFrameDeclaration` (`CuriousContraptions.Abstractions/MassFrameValues.cs:38`) | same test, and `TheSizeLawIsTheLegacyCubeRootAtItsEndsAndTheSceneColliderAtTheDefault` (`:182`) |
| Several colliders, tori included, on one dynamic body | present | `ColliderShapeKind.Torus` (`CuriousContraptions.Abstractions/PhysicsDeclarationValues.cs:12`) | `VisibleSolidColliderTests.EverySolidArtPartHasItsColliderAndNoColliderReachesBeyondTheArt` (`CuriousContraptions.tests/VisibleSolidColliderTests.cs:186`, row `:123`); `WeightTests.CompoundRollingRetainsItsHeadingAcrossTheRampEdge` (`:448`) |
| Contact material with rolling resistance; stable rest and stacking | present | `SceneBodyGeometry` material (`elements/Weight/WeightElementDeclaration.cs:52`) | `WeightTests.DroppedEndDownItRestsOnItsBaseWithoutSinkingDriftOrEnergyGain` (`:268`), `AStackEndDownHoldsWithoutBounceOrCreep` (`:585`) |
| Rope port with attachment role and parameter-bound position | present | `PortDescriptor`, `RopeAttachmentRole`, `PortPositionBinding` (`CuriousContraptions.Abstractions/PortDescriptor.cs:55`, `:74`, `:77`) | `RopeTests.TieTracksTheActualMassScaledEye` (`CuriousContraptions.tests/RopeTests.cs:250`) |
| Tension-only span of measured length | present | `TensileSpanDeclaration` (`CuriousContraptions.Abstractions/TensileSpanDeclarationValues.cs:25`), `TensileConstraintSolver` (`CuriousContraptions.Simulation/TensileConstraintSolver.cs:27`) | `RopeTests.ACompleteRopeSupportsItsLoadWhileAnUnconnectedLoadFalls` (`:21`), `SlackRopeDoesNotAccelerateTheFallingLoad` (`:34`) |
| Route over finite-radius guides | present | `RopeGeometry.EvaluateRoute` (`engine/gpu/RopeGeometry.cs:83`) | `WeightTests.Counterweight_LiftsLighterLoadOverPulley` (`:1153`), `RopeTests.TwoPulleyAtwoodAcceleratesUnequalWeightsWithTheoreticalAtwoodDynamics` (`:721`) |
| Typed parameter admission and refusal at every seam | present | `ParameterDescriptor` with `ParameterEditorPolicy`, `ParameterRunEdit` (`CuriousContraptions.Abstractions/ParameterDescriptor.cs:39`, `:44`) | `WeightTests.AnOutOfRangeOrNonFiniteMassIsRefusedAtEverySeamAndChangesNothing` (`:96`), `AMissingMassOrAnUnknownKeyIsRefusedWithoutDefaultFill` (`:118`) |
| Run/Reset restoration and save round trip | present | generic lifecycle | `WeightTests.ResetRestoresTheBuiltPosesAndAReplayRepeatsTheRunIncludingAfterAMassEdit` (`:782`), `SaveRoundTripsPoseRotationAndMassExactlyWithNoWireChange` (`:805`), `SuspendedPendulum_ResetRestoresAuthoredPoseAndZeroVelocity` (`:1275`) |
| Parameter-driven uniform scale of the art | present | `PresentationProperty.UniformScale` (`CuriousContraptions.Abstractions/ElementPresentation.cs:14`) | `WeightTests.TheCompiledMassSizeCentreAndInertiaFollowTheMassParameter` asserts the declared binding and that scale ÷ radius is constant (`:174`–`:178`). The evaluated scene node is checked against the colliders by `VisibleSolidColliderTests.EverySolidArtPartHasItsColliderAndNoColliderReachesBeyondTheArt`, which builds the art at each bound of `mass` (`VisibleSolidColliderTests.cs:176`–`:182`) with the `UniformScale` binding applied (`:259`–`:274`) |

**Collaborating parts.** [CAT-058 rope_anchor](CAT-058-rope_anchor.md) for hanging and pendulum; [CAT-053 pulley](CAT-053-pulley.md) for counterbalance; the lessons use the Switch and Lamp.

## 4. Legacy harvest

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| W1 | Translating load with a spherical collision envelope and a rope eye; Locked is edit-time only. | `parts/WeightPart.cs@a6c914e:L7-L8`, `parts/WeightPart.cs@a6c914e:L13` | carry forward the rope eye and edit-time Locked; the envelope is the art's cylinder (Q1, 10.2a) | A locked weight still moves in Run. |
| W2 | Mass must be finite and within 0.25–8; −1, 0, 20 and NaN reject before any instance is added. | `parts/WeightPart.cs@a6c914e:L22-L27`; `CuriousContraptions.tests/RopeTests.cs@a6c914e:L110-L124` | carry forward | Authored range and atomic rejection. |
| W3 | Catalogue default mass 4; lesson fixture loads use mass 1. | `parts/catalog/weight.tres@a6c914e:L14`; `content/puzzles.json@a6c914e:L5142-L5144` | carry forward | Requirement: default 4 is not the fixture mass 1. |
| W4 | Radius 0.32 ∛m; tie height r + 0.08. | `engine/MachineData.cs@a6c914e:L99-L100`; `parts/WeightPart.cs@a6c914e:L19-L20`, `parts/WeightPart.cs@a6c914e:L32` | carry forward | Size encodes mass; rope length depends on the tie height. |
| W5 | Solid-sphere dynamics with authored initial velocity. | `parts/WeightPart.cs@a6c914e:L14-L15`; `engine/BodyDynamics.cs@a6c914e:L39-L44` | superseded by the solid-cylinder law (10.1, 10.2a); do not carry forward `BodyDynamics` | `RigidMassProperties` compiles the cylinder moments from the bound collider. |
| W6 | Restitution 0.08, drag 0, bounce threshold 0.1 m/s, friction 0.3. | `parts/WeightPart.cs@a6c914e:L33-L34`; `reference/cpu/MachinePart.cs@a6c914e:L236` | carry forward as candidate values (Open question 2) | Authored legacy values; current material also needs rolling resistance. |
| W7 | Artwork: cylinder body, base band, gold eye, ⌈mass⌉ bands (dimensions in Art). | `parts/WeightPart.cs@a6c914e:L36-L46` | carry forward the art; the collider follows it (Q1, 10.2a); two fixed bands replace ⌈mass⌉ | Requirement: solid pose and rope attachment agree. |
| W8 | Rope eye socket at (0, r + 0.08, 0); role `Load` takes one rope end. | `parts/WeightPart.cs@a6c914e:L16-L21`; `engine/RopeNetwork.cs@a6c914e:L76-L77` | carry forward | Typed socket. |
| W9 | Levels: `counterweight` — locked `load` mass 1 at (−2, 1, 0.12), solution `weight_1` mass 4 at (2, 5, 0.12), inventory weight 1. `pulley_depth` — same load, solution `weight_1` mass 4 at (2, 5, 2.12), inventory weight 1 and pulley 1. Goal: lamp activated via the overhead Switch at (−2, 3.2, 0.12), rotated 180°. | `content/puzzles.json@a6c914e:L5072-L5145`, `content/puzzles.json@a6c914e:L5273-L5333`, `content/puzzles.json@a6c914e:L5399-L5471`, `content/puzzles.json@a6c914e:L5508-L5582`, `content/puzzles.json@a6c914e:L5781-L5845` | carry forward | Lesson set-ups to rebuild in Epic 15. |
| W10 | Lesson authoring source for both levels (positions, masses, rope spans by connect-time distance). | `tools/Campaign/Program.cs@a6c914e:L173-L223`, `tools/Campaign/Program.cs@a6c914e:L408-L413` | carry forward | Campaign rebuild input. |
| W11 | Acceptance: both lessons are won at precision 0, 0.45 and 1 within 1500 ticks; removing any one rope span fails the lesson; changing `weight_1` to mass 1 fails ("Equal counterweights cannot raise the load to the switch"). | `CuriousContraptions.tests/RopeTests.cs@a6c914e:L70-L108` | carry forward | Requirement: equal/unequal ratios, slack release/open route. |
| W12 | Acceptance: counterbalance acceleration, height-sum conservation and order independence through two pulleys. | `CuriousContraptions.tests/RopeTests.cs@a6c914e:L21-L68` (detail in CAT-053 P9) | carry forward | Shared with CAT-053. |
| W13 | Acceptance: floor contact — a mass-1 and a mass-4 weight never sink below the bench while roped over pulleys for 900 ticks. | `CuriousContraptions.tests/RopeTests.cs@a6c914e:L217-L241` | carry forward | Requirement: floor contact. |
| W14 | Acceptance: 3D pendulum stays within rope length and gains no energy (detail in CAT-058 R20). | `CuriousContraptions.tests/RopeTests.cs@a6c914e:L191-L215` | carry forward | Requirement: 3D pendulum, energy/length bounds. |
| W15 | Acceptance: a slack rope between two weights does not suppress their contact; equal masses exchange velocities scaled by the product of restitutions. | `CuriousContraptions.tests/SceneRopeBindingTests.cs@a6c914e:L223-L255` | carry forward | Connected-body collision stays enabled. |
| W16 | Acceptance: Reset rebuilds identical bodies, masses and ordered routes. | `CuriousContraptions.tests/SceneRopeBindingTests.cs@a6c914e:L257-L280` | carry forward | Exact Reset. |
| W17 | Integration (CAT-071): the default weight (r ≈ 0.508) is wider than the wound-spring bore, rests on the rim, never enters it, receives a physical tap and rises by more than 0.01 m but no more than (stored energy + initial KE)/(m g). | `CuriousContraptions.tests/WoundSpringTests.cs@a6c914e:L591-L597`, `CuriousContraptions.tests/WoundSpringTests.cs@a6c914e:L618-L623`, `CuriousContraptions.tests/WoundSpringTests.cs@a6c914e:L655-L661` | carry forward | Oversized-payload control for the wound spring. |
| W18 | Integration (optics): a mass-8 weight occludes a torch–solar path using its committed rotated body; disabling its collider restores the path. | `CuriousContraptions.tests/LightTests.cs@a6c914e:L180-L208` | carry forward | Occlusion follows committed body geometry (Epic 13 integration). |
| W19 | Integration (CAT-065): a default weight tethered over a trampoline (see CAT-058 R23). | `CuriousContraptions.tests/TrampolineRopeTests.cs@a6c914e:L45-L47` | carry forward | Tether integration. |
| W20 | Parameter name boundary `WeightParameter.Mass` ↔ `"mass"`; undefined enum values throw. | `engine/MachineData.cs@a6c914e:L92`; `CuriousContraptions.tests/SceneRopeBindingTests.cs@a6c914e:L308-L309` | carry forward | Typed parameter, string only at serialization. |

### Files harvested

- `parts/WeightPart.cs`
- `parts/catalog/weight.tres`
- `parts/scenes/weight.tscn`
- `engine/MachineData.cs`
- `engine/BodyDynamics.cs`
- `engine/RopeNetwork.cs`
- `reference/cpu/MachinePart.cs`
- `ui/WorkshopIcons.cs` (pictogram; not deleted by Epic 7)
- `reference/p025-production-isolated-20261004/src/ui/WorkshopIcons.cs` (checked, duplicate of the surviving pictogram)
- `tools/Campaign/Program.cs`
- `content/puzzles.json`
- `CuriousContraptions.tests/RopeTests.cs`
- `CuriousContraptions.tests/SceneRopeBindingTests.cs`
- `CuriousContraptions.tests/WoundSpringTests.cs` (weight usage only)
- `CuriousContraptions.tests/LightTests.cs` (weight occluder only)
- `CuriousContraptions.tests/TrampolineRopeTests.cs` (weight usage only)
- `CuriousContraptions.tests/RopeOwnershipTests.cs` (harvested into CAT-053)
- `CuriousContraptions.tests/WoundSpringTests.cs.orig` (checked; superseded copy of WoundSpringTests, no extra weight knowledge)
- `CuriousContraptions.tests/LinearPusherTests.cs`, `CuriousContraptions.tests/TrampolineTests.cs` (checked; they set `WeightParameter.Mass` on `ball` parts, not weights)
- `reference/p054-guide/puzzles-candidate.json` (checked; same lessons apart from the orientation encoding)

## 5. Acceptance outline

Requirement row: [CAT-067](../requirements.md#current-cat-067); stories 10.2 and 10.4.

- **Chrome UI recipe.** Place a Weight from the toolbox; select its mass through the admitted control or authored fixture (Open question 4); place a Rope anchor or Pulleys and connect `tie` sockets with the contextual wiring UI. Verify committed mass, radius and connection.
- **Positive.** Hanging: the weight hangs taut from an anchor. Counterbalance: a mass-4 weight lifts a mass-1 load through two pulleys into the Switch and lights the Lamp. Pendulum: released from an angle, it swings in 3D with period T = 2π√(L/g) and damps gradually (story 10.4).
- **Negative/control.** Equal masses stay balanced; an unconnected or open-route weight falls freely and rests on the bench.
- **Boundaries.** Mass 0.25 and 8 accepted, out-of-range rejected atomically; slack release; floor contact; direction/order; energy and length bounds.
- **Run/Reset.** Reset restores initial pose, zero velocity and angular displacement exactly; replay matches.
- **Save/Load.** `mass` and rope endpoints survive save/load; unknown parameters reject.
- **Integrations.** Pulley (CAT-053), anchor (CAT-058), trampoline tether (CAT-065), wound-spring oversized control (CAT-071), optical occlusion (Epic 13).

**Native tests that exist for each requirement criterion (22 November 2026; none was run for this review).** All in `CuriousContraptions.tests/WeightTests.cs` unless a file is named.

| Requirement criterion | Test (line) | Control and boundary in the same suite |
| --- | --- | --- |
| Unequal ratio lifts | `Counterweight_LiftsLighterLoadOverPulley` (`:1153`); `RopeTests.TwoPulleyAtwoodAcceleratesUnequalWeightsWithTheoreticalAtwoodDynamics` (`RopeTests.cs:721`) | equal masses: `Counterweight_EqualMassesOverPulleyRemainBalanced` (`:1179`) |
| Slack release, open route | `SuspendedPendulum_SteepAngleSlackReleaseTransitionsSmoothly` (`:1118`); `Counterweight_OpenRouteFallsFreelyToBench` (`:1232`) | open route is the control of the lift |
| Floor contact | `Counterweight_FloorContactRelaxesTensionAndRestsOnBench` (`:1204`) | none needed |
| Direction and declaration order | `Counterweight_PulleyWiringDirectionInvariance` (`:1254`) | both wiring orders |
| 3D pendulum, energy and length bounds | `SuspendedPendulum_OscillatesWithTheoreticalNaturalPeriod` (`:936`, period within 5% of 2π√(L/g)), `_PeriodScalesWithSquareRootOfLength` (`:964`), `_PeriodIsInvariantWithMass` (`:992`), `_DampsGraduallyWithoutEnergyGrowth` (`:1021`, energy ≤ 1.005 × initial every tick), `_ThreeDimensionalNonPlanarPrecession` (`:1079`) | 0.25 kg and 8 kg in the mass-invariance test |
| Solid pose and rope attachment agree | `RopeTests.TieTracksTheActualMassScaledEye` (`RopeTests.cs:250`) | several masses |
| `mass` mode: bounds and refusal | `TheBoundaryMassesAreAdmittedAtEverySeam` (`:70`), `AnOutOfRangeOrNonFiniteMassIsRefusedAtEverySeamAndChangesNothing` (`:96`), `AMissingMassOrAnUnknownKeyIsRefusedWithoutDefaultFill` (`:118`) | 0.25 and 8 kg admitted; outside, NaN and infinities refused |
| Run/Reset, save and load | `ResetRestoresTheBuiltPosesAndAReplayRepeatsTheRunIncludingAfterAMassEdit` (`:782`), `SaveRoundTripsPoseRotationAndMassExactlyWithNoWireChange` (`:805`), `SuspendedPendulum_ResetRestoresAuthoredPoseAndZeroVelocity` (`:1275`), `SuspendedPendulum_SaveLoadRoundtripPreservesSwingingState` (`:1309`) | generic row: `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad` (`CatalogueIntegrationTests.cs:273`) |
| Wrong-kind connection refusal | rope graph: `RopeTests.OpenGuideTopologyIsIncompleteAndCyclesOrBranchesRefuse` (`RopeTests.cs:230`). No test links a rope port with a non-rope kind: `BundlePortsTests.AWrongDomainOrOutputToOutputLinkIsStillRefused` (`BundlePortsTests.cs:262`–`:284`) has no Rope case. Planned shared row `RopeTests.ARopePortRefusesANonRopeLinkAndANonRopePortRefusesARopeLink`, typed `InvalidConnection`; control: a Rope link between the same two ports is admitted | one row for every rope part |
| Collaborators | Pressure plate: `CatalogueIntegrationTests.AWeightLoadsItsPlateAndOnlyTheWiredSupplyOpensTheGate` (`:139`); Domino: `AWeightRollingOffARampTopplesADominoAndARestingWeightBesideOneLeavesItStanding` (`:617`); Trampoline tether: `TrampolineTests.ATautTetherHoldsTheWeightAboveTheBedAndASlackOneLetsItRebound` (`TrampolineTests.cs:2112`); Impact lever: `ImpactLeverTests.RopeLiftingIntegration_FallingWeightLiftsArm` (`ImpactLeverTests.cs:414`) | each has its own control |

**Not yet covered.** "Default mass 4 is not the fixed fixture mass 1" is a level fact: authored levels 19 `counterweight` and 20 `pulley_depth` (plan levels 12b and 12c) are `Planned` in `content/puzzles.json`, so no test places the locked mass-1 load beside a stocked mass-4 Weight. It is proven by the Early level proof group "Ropes" (section 7). Optical occlusion waits for `ENGINE-OPTICAL`. Several existing tests assert exact replay or exact save values (`:660`, `:805`); exact authored state and serialized field values are allowed, a per-tick trajectory pin is not a gameplay criterion, and a future edit of those tests applies the [float rules](../../testing-guidance.md#float-comparison-and-effort-rules).

## 6. Open questions

1. Collision shape versus artwork: legacy collides as a sphere but draws a cylinder (0.85 r radius, 1.35 r tall). Keep the sphere and reshape the art, or change the collider? The shared cylinder prerequisite is Story 10.1 (ENGINE-CYLINDER). **Answered (10.2a):** the collider is the art's solid cylinder, origin at the base centre (D1).
2. Contact material: confirm restitution 0.08, friction 0.3, threshold 0.1 m/s, drag 0, and set a rolling-resistance coefficient (none in legacy; current materials require one). **Answered (10.2a, by game intent under owner delegation):** those values, with rolling resistance 0.03 (the Bowling ball's).
3. Confirm the radius law 0.32 ∛m and tie offset 0.08 m. **Answered (10.2a, by game intent):** the size is linear in mass, exact at 0.25 and 8 kg against the legacy art (r = 0.85 · 0.32∛m, h = 0.675 · 0.32∛m), monotone in between. The tie offset is 10.2b's (it needs a size binding).
4. How the player sets `mass`: a UI control, or authored fixtures only? No current control exists. **Answered (10.2a):** the generic parameter editor, kg, Clamp policy, Build only.
5. Pendulum damping: no damping law is added (decided 22 November 2026 under the standing delegation). The Weight declares no drag and the rope no material loss; the only loss is the tensile row's declared regularisation (60 Hz, damping ratio 10, [CAT-058](CAT-058-rope_anchor.md) decision 2). The delivered test asserts what a player can see: later peaks are lower than the first and energy never grows (`WeightTests.cs:1021`). Reason: a pendulum that keeps swinging for a puzzle's length is the useful behaviour, and a second loss mechanism would be an unprescribed law.
6. Pendulum length for T = 2π√(L/g): the rope length plus the distance from the Tie to the centre of mass (`TieOffsetY(mass) − HalfHeight(mass)`, `WeightTests.cs:945`).

No item needs the owner.

## 7. Integration

- **Collaborators, all delivered:** Rope anchor (`rope_anchor`), Pulley (`pulley`), Impact lever (`TieA`/`TieB`), Trampoline, Pressure plate (the 4 kg default exceeds the plate's 0.5 kg default minimum; 0.25 kg does not), Domino, Ramp, Wound spring. Both ends of every rope link are `SimulationDomain.Rope`, `PortDirection.Bidirectional`; the Weight is `Load`, the anchor `Anchor`, the Pulley `Guide`, the lever ends `Load`.
- **Not delivered:** optical occlusion (Epic 13, `ENGINE-OPTICAL`); EL-063 Cable winch, EL-065 Load hook, EL-069 Moving bucket and EL-207 Moving pulley plan to carry or lift a Weight.
- **Campaign.** Authored levels 19 `counterweight` and 20 `pulley_depth` (plan levels 12b, where the Weight and Rope are taught, and 12c, where the Rope anchor and Pulley are; both `Planned`, single `activated` goal on the lamp) each stock a Weight; delivery-batches section 4 lists both in the "Ropes" group, provable from delivered parts after the engine programme (owner decision 3a). Each level is authored into the playable shape in that slice; neither needs the lesson-fixture story (S8) unless its authoring finds a fixture the playable shape cannot express.
- **Coverage binding.** Catalogue key `weight` (CAT-067). It has no named-identity package binding section: that form is for named identities.
- **Solid rope (owner decision, 22 November 2026).** The rope a Weight hangs from is solid but is drawn with no collider today; the defect and its repair belong to `ENGINE-CONNECTION-MATERIALS` ([CAT-058](CAT-058-rope_anchor.md)). No Weight test found by name relies on a body passing through a rope. When the strand gains a collider the pendulum and counterweight tests are re-run; a rope must not collide with the eye it is tied to, which is a filter rule for that story.

## 8. Delivery and residuals

- **Delivered** by 10.2a (body), 10.2b (Tie, rope), 10.2c (pulley route) and 10.4 (qualification). Contradiction to clear: `spec-10-2a-weight.md` still has `status: review` although the package is on published main; a status edit, owned by the next slice that touches the Weight.
- **Requirements row.** The CAT-067 row in `docs/planning/requirements.md` now describes the delivered cylindrical body with a rope tie (the 10.2a decision); no gap remains.
- **Blocked on the engine programme (owner decision 3a, 22 November 2026).** The level proof and the closure review below wait for it. The story that owns this part's gap is `ENGINE-ISLANDS-SLEEP` (section 9, point 6).
- **Art scale: covered, no residual.** The declared scale binding is tested (`WeightTests.cs:174`–`:178`), and the evaluated `Visual` node is compared with the colliders at each bound of `mass` with the `UniformScale` binding applied (`VisibleSolidColliderTests.cs:176`–`:182`, `:259`–`:274`).
- **R2, level proof.** Early level proof "Ropes" (levels 19 and 20): authoring plus a native proof; data-only tier while every path is on the allow-list.
- **EL-204 Weight: candidate Duplicate closure** (delivery-batches 2a, "Rope parts"). Its ledger outcome is "Raising it requires actual work". No delta was found: EL-204 names no parameter, port, mode or material beyond this declaration. **Closure check:** the closure review records, against the published SHA, `Counterweight_LiftsLighterLoadOverPulley` (`:1153`, positive), `Counterweight_EqualMassesOverPulleyRemainBalanced` (`:1179`) and `Counterweight_OpenRouteFallsFreelyToBench` (`:1232`) as controls (no lift without a heavier load or without a complete route), `SuspendedPendulum_DampsGraduallyWithoutEnergyGrowth` (`:1021`, no energy from nowhere), and the Reset and save tests at `:1275` and `:1309`; then the `element-204` ledger entry cites `weight` and those tests. The closure has no package and no palette entry. The closure review runs after the engine programme (owner decision 3a), and `ENGINE-CONNECTION-MATERIALS` re-runs these closure checks when the rope strand gains a collider.
- **What a later slice's probe must re-check:** that the five collider indices and their bindings are unchanged (a new collider must be appended, never inserted), and that any light body added to the catalogue still exceeds the Pressure plate and Trampoline bounds the Weight's 0.25 kg minimum was checked against.

## 9. Engine conformance (owner instruction, 22 November 2026)

**Verdict: Conforms.** The package needs no change. Checked against the design guide (`_bmad/custom/element-free-engine-design.md`, D1–D7), `docs/gpu-f32-physics.md` and the package. A search of `engine/`, `CuriousContraptions.Simulation`, `CuriousContraptions.Bundle` and the abstractions for the package id and class name found no engine code that names this element. One engine gap touches it and every other resting part: island sleeping (point 6, `ENGINE-ISLANDS-SLEEP`).

1. **Declaration data only.** One declaration class of data: a mass frame, five bound colliders, one material, one parameter, one port, one presentation parameter binding. No script, callback, state machine or update loop. The legacy part script in section 4 is history.
2. **Generic laws.** Falling, resting, rolling and stacking: rigid-body dynamics with contact, Coulomb friction, restitution and rolling resistance from the declared material. Size and inertia: the declared mass frame and collider bindings evaluated from `mass`. Hanging, lifting and swinging: the tensile span law on a `Load` port. No row is "when X touches the Weight, do Y".
3. **Three layers.** Colliders: `col_cylinder` and four hidden proxies (`col_visual_base`, `col_visual_bandlower`, `col_visual_bandupper`, `col_visual_eye`), all on the one body. Visuals: `Visual_Body`, `Visual_Base`, both bands and `Visual_Eye` under `Visual`, moved by the body pose and scaled by the declared parameter binding; none is a collider. Animation: none.
4. **One-way flow and typing.** The art reads the committed pose; nothing is written back. `PortId`, `ParameterId`, `SimulationDomain`, `RopeAttachmentRole`, `ColliderShapeKind` and `ParameterUnit` are typed; values are f32; the key `mass` is a string only at the declaration and save boundary.
5. **Flat-state counts.** 1 dynamic body; 5 colliders (2 cylinders, 3 tori); 0 joints, drives, sensors, domain nodes, stores and events; 1 port; 1 parameter; 17 collider bindings; 1 presentation parameter binding. All fixed at compile time.
6. **Performance fitness.** Five colliders on a moving body is above the single collider a plain ball uses. The reason is the visible = solid rule (base, two bands, eye). The two band tori stand 1.2 cm proud; redrawing the bands flush would remove two colliders, which is a choice for whoever next owns this art, not a defect. Island sleeping is not implemented; a resting body is still stepped, at the cost of its broadphase proxies and rows every substep (no sleep or deactivation code in `CuriousContraptions.Simulation`). Owned by `ENGINE-ISLANDS-SLEEP` (owner decision 3a, 22 November 2026). At up to 8 kg and 0.54 m radius the Weight is not a small fast body; tunnelling is excluded by the engine's speculative contacts, with no per-element check.
7. **Scenario tests.** Every test in section 5 runs the production simulation through the Workshop harness or the compiled scene and asserts a consequence of those laws.
