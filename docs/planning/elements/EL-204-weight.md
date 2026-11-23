# EL-204 · Weight — named-identity readiness spec

**Current delivery (24 October 2026):** [EL-204 delivery slice](../invest/named-elements.md#element-204). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Rewritten on 22 November 2026 as one current text, checked against `elements/Weight/` and its tests in checkout `8d1d68aa`. Citations without `@` are `path:line` in that checkout; `Decl` is `elements/Weight/WeightElementDeclaration.cs` and bare test line numbers are in `CuriousContraptions.tests/WeightTests.cs`. `path@a6c914e` citations appear only in section 4 and are source facts. The delivered declaration is recorded once, in [CAT-067](CAT-067-weight.md).

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-204 · Weight · Gravity |
| Anchor | [requirements.md#element-204](../requirements.md#element-204) (task `sequence-task-757`); [named-elements entry](../invest/named-elements.md#element-204); scope source [campaign element coverage](../requirements.md#campaign-element-coverage) |
| Owner key / slice | S306; `named-el-204` |
| Refines | [CAT-067 weight](CAT-067-weight.md) ([requirement](../requirements.md#current-cat-067)). Partners: Rope ([EL-205](EL-205-rope.md)), Fixed pulley ([EL-206](EL-206-fixed-pulley.md)), Moving pulley ([EL-207](EL-207-moving-pulley.md)), [CAT-053 Pulley](CAT-053-pulley.md), [CAT-058 Rope anchor](CAT-058-rope_anchor.md) |
| Row and outcome | "Movable load exchanges gravitational potential and tension through declared sockets." Outcome: "Raising it requires actual work." |
| Status | Delivered through package `weight` (Stories 10.2a, 10.2b, 10.2c, 10.4). The closure review is blocked on the engine programme (owner decision 3a), in particular `ENGINE-SPATIAL-QUERIES` and `ENGINE-ISLANDS-SLEEP`, and runs after it (owner decision 22) |

### Disposition

**Candidate Duplicate closure of the delivered package `weight`** (`elements/Weight/`, palette order 31, `Decl:28`), in the closure-review group "Rope parts" ([delivery batches 2a](../invest/delivery-batches.md#2-duplicate-closures)). The identity names no parameter, port, mode or material beyond the package's declaration: no delta. It gets no package, catalogue entry or palette order.

Nothing is deliverable now: the closure review is **blocked on the engine programme (owner decision 3a)**. The closure review runs after that programme (owner decision 22, 23 November 2026). Under the coordinator decision of 22 November 2026 the acceptance rows of [CAT-067](CAT-067-weight.md) are the single authority for this closure identity; the rows in section 5 cite them.

**Closure check.** The review records, against the published SHA:

1. Positive: `Counterweight_LiftsLighterLoadOverPulley` (`:1153`, opened). A 1 kg load rises only as a 4 kg counterweight descends by the same distance.
2. Controls, no lift without a cause: `Counterweight_EqualMassesOverPulleyRemainBalanced` (`:1179`) and `Counterweight_OpenRouteFallsFreelyToBench` (`:1232`).
3. No work from nowhere: `SuspendedPendulum_DampsGraduallyWithoutEnergyGrowth` (`:1021`, opened): total energy stays at or below 1.005 of its start at every tick.
4. Reset and save: `SuspendedPendulum_ResetRestoresAuthoredPoseAndZeroVelocity` (`:1275`), `SuspendedPendulum_SaveLoadRoundtripPreservesSwingingState` (`:1309`).

Then the `element-204` ledger entry cites `weight` and those tests.

**The rope's missing collider does not hold this closure** (decided now, standing delegation). Ropes are solid (owner decision, 22 November 2026), and the delivered rope connection draws its strand with no collider. That is a recorded visible-solid defect of the rope connection, owned by ENGINE-CONNECTION-MATERIALS (solid strand; proposed in the [Batch I summary](readiness/batch-i.md)). It is not a defect of the Weight package, whose every drawn solid has a collider, and the outcome closed here (lifting needs work) is a result of the tensile law, which a strand collider does not change. When that story lands, its slice re-runs the four checks above.

## 2. Declaration

Delivered; every value and line is in [CAT-067 §2](CAT-067-weight.md#2-declaration). In short: one dynamic body; a solid cylinder with a base cylinder, two band tori and a standing eye torus, all linear in `mass` (radius 0.352 m and height 0.559 m at 4 kg); mass = `mass`, solid-cylinder moments as polynomials of `mass`, gravity (0, −9.81, 0) m/s² (`Decl:96`–`:107`); one parameter `mass` (Real, kg, default 4, 0.25 to 8, clamped in the editor, build-only; `Decl:33`–`:34`); one port `PortId.Tie`, Rope domain, bidirectional, attachment role `Load`, at the eye, 0.5771 m above the base at 4 kg (`Decl:39`–`:45`); no store, sensor or event.

**Material, as declared data.** Restitution 0.08, friction 0.3, rolling resistance 0.03, bounce threshold 0.1 m/s, no drag (`Decl:52`), the four fields of `ContactMaterialDeclaration` (`CuriousContraptions.Abstractions/PhysicsDeclarationValues.cs:133`–`:143`). There is no density field: mass is the declared parameter and size follows it by a declared linear law, so "heavier" is always declared kilograms. No strength, thermal or optical property is needed or present.

### Art and solidity

`Visual_Body`, `Visual_Base`, both bands and `Visual_Eye` each have a collider (`col_cylinder` and four `col_visual_` proxies); the hand table agrees (`CuriousContraptions.tests/VisibleSolidColliderTests.cs:123`, `Fit: 4e-6f`), and `spec-engine-art-solidity.md` lists no finding for `weight` and keeps the declared fit (its D4). The art follows the body pose and is scaled in Build by one parameter binding. The rope strand that hangs it belongs to the rope connection, not to this package (Disposition). At a glance: a blue cylinder (`#45639c`) with two bands and a lifting eye; the Bowling ball shares the colour and is a plain sphere.

## 3. Engine capabilities

### Capability probe

Probe of 22 November 2026. "Opened" means the test body was read for this spec; the others are cited from CAT-067's probe (which opened them) and confirmed by name and line.

| Capability | Status | Symbol | Test |
| --- | --- | --- | --- |
| Tension-only span of measured length between declared ports | present | `TensileSpanDeclaration` (`CuriousContraptions.Abstractions/TensileSpanDeclarationValues.cs:25`), `TensileConstraintSolver` (`CuriousContraptions.Simulation/TensileConstraintSolver.cs:27`) | `RopeTests.ACompleteRopeSupportsItsLoadWhileAnUnconnectedLoadFalls` (`CuriousContraptions.tests/RopeTests.cs:21`); `SlackRopeDoesNotAccelerateTheFallingLoad` (`:34`) |
| Route over finite-radius guides | present | `RopeGeometry.EvaluateRoute` (`engine/gpu/RopeGeometry.cs:36`) | `Counterweight_LiftsLighterLoadOverPulley` (`:1153`, opened) |
| Rope port with attachment role and parameter-bound position | present | `PortDescriptor`, `RopeAttachmentRole`, `PortPositionBinding` (`CuriousContraptions.Abstractions/PortDescriptor.cs:55`, `:74`, `:77`) | `RopeTests.TieTracksTheActualMassScaledEye` (`RopeTests.cs:250`) |
| Declared mass frame and parameter-bound colliders | present | `MassFrameDeclaration` (`CuriousContraptions.Abstractions/MassFrameValues.cs:38`), `ColliderParameterBinding` | `TheCompiledMassSizeCentreAndInertiaFollowTheMassParameter` (`:139`) |
| Several colliders, tori included, on one dynamic body | present | `ColliderShapeKind.Torus` (`CuriousContraptions.Abstractions/PhysicsDeclarationValues.cs:12`) | `CompoundRollingRetainsItsHeadingAcrossTheRampEdge` (`:448`) |
| Contact material with rolling resistance; stable rest | present | the scene body's material (`Decl:52`) | `DroppedEndDownItRestsOnItsBaseWithoutSinkingDriftOrEnergyGain` (`:268`) |
| Energy is not created by the span or the contact | present | the solver's energy-removing regularisation (CAT-058 decision 2) | `SuspendedPendulum_DampsGraduallyWithoutEnergyGrowth` (`:1021`, opened) |
| Typed parameter admission and refusal | present | `ParameterDescriptor` (`CuriousContraptions.Abstractions/ParameterDescriptor.cs:39`) | `AnOutOfRangeOrNonFiniteMassIsRefusedAtEverySeamAndChangesNothing` (`:96`) |
| Run/Reset and save round trip | present | generic lifecycle | `ResetRestoresTheBuiltPosesAndAReplayRepeatsTheRunIncludingAfterAMassEdit` (`:782`); `SaveRoundTripsPoseRotationAndMassExactlyWithNoWireChange` (`:805`) |
| Parameter-driven uniform scale of the art | present | `PresentationProperty.UniformScale` (`CuriousContraptions.Abstractions/ElementPresentation.cs:14`) | `VisibleSolidColliderTests.EverySolidArtPartHasItsColliderAndNoColliderReachesBeyondTheArt` (`CuriousContraptions.tests/VisibleSolidColliderTests.cs:186`, opened to its collider matching): its parameter sets (`:176`–`:182`) are the defaults and each collider-bound parameter at its minimum and maximum, so the Weight's art is built at `mass` 0.25, 4 and 8 kg and each drawn solid is matched to its collider within the declared fit. CAT-067 still lists this as untested (its residual R1) |

Counts: 10 present, 0 untested, 0 narrower, 0 missing.

## 4. Sources and legacy

- **Requirements.** Row and outcome as in section 1. CAT-067: equal and unequal ratios, slack release, floor contact, energy and length bounds. The CAT-067 row still reads "Canonical authored mass and spherical body/socket" (`docs/planning/requirements.md:860`); the delivered part is a cylinder with a Rope-domain `Tie` port, decided in Story 10.2a. Proposed rewording, made with the plan-document updates: "Canonical authored mass and a solid cylindrical body with a rope tie support lifting, counterbalance and 3D pendulum motion."

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | Two weights over two fixed pulleys accelerate at g (m₂ − m₁)/(m₁ + m₂) within 0.015 m/s after 30 ticks; heights stay complementary; rope taut at its length; replay identical. | `CuriousContraptions.tests/RopeTests.cs@a6c914e:L21-L68` | Delivered as `RopeTests.TwoPulleyAtwoodAcceleratesUnequalWeightsWithTheoreticalAtwoodDynamics`; replay identity is not acceptance. |
| 2 | Invalid masses −1, 0, 20 and NaN reject before an instance is added. | `CuriousContraptions.tests/RopeTests.cs@a6c914e:L110-L124` | Delivered (0.25 to 8 kg; refusal at every seam). |
| 3 | Slack rope does not push; a taut tether stops extension. | `CuriousContraptions.tests/RopeTests.cs@a6c914e:L126-L156` | Delivered. |
| 4 | An unfinished pulley route cannot lift. | `CuriousContraptions.tests/RopeTests.cs@a6c914e:L158-L189` | Delivered (closure check 2). |
| 5 | A 2 kg 3D pendulum stays within its rope (±2 mm) and never gains energy (+0.5 J tolerance). | `CuriousContraptions.tests/RopeTests.cs@a6c914e:L191-L215` | Delivered with a re-derived bound: energy at most 1.005 of its start. |
| 6 | Counterweight contacts cannot pull bodies through the floor. | `CuriousContraptions.tests/RopeTests.cs@a6c914e:L217-L241` | Delivered (`Counterweight_FloorContactRelaxesTensionAndRestsOnBench`). |
| 7 | Lessons `counterweight` (load at (−2, 1, 0.12), pulleys at (±2, 6, 0), solution weight at (2, 5, 0.12), rope spans 5 / 4 / 0.812 m) and `pulley_depth`. | `content/puzzles.json@a6c914e:L5071-L5506`, `content/puzzles.json@a6c914e:L5507-L5943` | Planned authored levels 19 and 20 (plan levels 12b and 12c), Early level proof group "Ropes". |
| 8 | Parameter keys bound through a string dictionary. | `parts/WeightPart.cs@a6c914e:L11-L12` | Not carried: one typed parameter. |

Files consulted: `parts/WeightPart.cs`, `parts/catalog/weight.tres`, `parts/scenes/weight.tscn` (script reference only), `engine/MachineData.cs`, `reference/cpu/MachinePart.cs`, `CuriousContraptions.tests/RopeTests.cs`, `content/puzzles.json`.

## 5. Acceptance outline

Point of truth: [element-204](../requirements.md#element-204) and [CAT-067](../requirements.md#current-cat-067). Chrome proof is pending post-catalogue.

### Behaviour and numbers

- **Lifting needs work.** In the delivered scene a 4 kg counterweight and a 1 kg load hang over two pulleys. The ideal acceleration is g·(4 − 1)/(4 + 1) = 9.81 × 0.6 = 5.89 m/s², so in the test's 0.5 s each moves ½ × 5.89 × 0.25 = 0.74 m and reaches 5.89 × 0.5 = 2.94 m/s. Energy over that travel: the counterweight gives up 4 × 9.81 × 0.74 = 28.9 J; raising the load takes 1 × 9.81 × 0.74 = 7.2 J; the remaining 21.7 J is the kinetic energy of both, ½ × 5 × 2.94² = 21.6 J. Every joule the load gains comes from the counterweight's descent. The test asserts at least 0.2 m each way and displacements equal within 0.05 m, which leaves room for the rope taking up its slack.
- **Controls.** With equal masses the net work available is zero and neither moves. With an open route there is no tension and the weight falls freely.
- **Energy never grows.** A 4 kg weight on a 2 m rope released at 15° swings for 10 s; kinetic plus potential energy never exceeds 1.005 of its start. Half a percent is the solver's bound, far below what would make a swing visibly higher than its release.
- **Fit with delivered parts.** The 4 kg default presses a default 0.5 kg Pressure plate and the 0.25 kg minimum does not; both ends of every rope link are Rope-domain, bidirectional ports (Weight `Load`, anchor `Anchor`, Pulley `Guide`).

### Acceptance rows

| # | Given / When / Then | Test | Control and boundary | State |
| --- | --- | --- | --- | --- |
| A1 | A heavier counterweight lifts a lighter load by the distance it descends | `Counterweight_LiftsLighterLoadOverPulley` | equal masses stay balanced; an open route falls freely; 0.5 s | executed |
| A2 | A swinging weight never gains energy and damps gradually | `SuspendedPendulum_DampsGraduallyWithoutEnergyGrowth` | 1.005 of the initial energy at every tick; 10 s | executed |
| A3 | Floor contact relaxes the rope; slack does not push | `Counterweight_FloorContactRelaxesTensionAndRestsOnBench` (`:1204`); `RopeTests.SlackRopeDoesNotAccelerateTheFallingLoad` | — | executed |
| A4 | Mass bounds are admitted and anything outside is refused | `TheBoundaryMassesAreAdmittedAtEverySeam` (`:70`); `AnOutOfRangeOrNonFiniteMassIsRefusedAtEverySeamAndChangesNothing` | 0.25 and 8 kg; NaN and infinities | executed |
| A5 | Reset and save/load restore pose, mass and rope | `SuspendedPendulum_ResetRestoresAuthoredPoseAndZeroVelocity`; `SuspendedPendulum_SaveLoadRoundtripPreservesSwingingState` | generic `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad` | executed |
| A6 | A wrong-kind connection to the Tie is refused | generic `BundlePortsTests.AWrongDomainOrOutputToOutputLinkIsStillRefused` (`CuriousContraptions.tests/BundlePortsTests.cs:262`) | — | executed |

The other CAT-067 rows (pendulum period, direction invariance, collaborators) are listed with their tests in [CAT-067 §5](CAT-067-weight.md#5-acceptance-outline).

### Integration

- **Collaborators, all delivered:** Rope anchor, Pulley, Impact lever, Trampoline, Pressure plate, Domino, Ramp, Wound spring. Planned parts that carry or lift a Weight: EL-063, EL-065, EL-069, EL-207. Interaction rows [IX-01](../requirements.md#interaction-01), [IX-03](../requirements.md#interaction-03).
- **Campaign.** Authored levels 19 `counterweight` and 20 `pulley_depth` (Planned; plan levels 12b, the Weight's and Rope's first teaching, and 12c), provable after the engine programme in the Early level proof group "Ropes" (owner decision 22). First use 11–20.
- **Coverage binding.** The catalogue key `weight` in `docs/coverage/catalogue-elements.json`. A closed duplicate has no binding of its own.
- **Lesson fixture (S8).** Not needed unless a level's authoring finds a fixture the playable shape cannot express.

### Delivery

Closure review "Rope parts" (with EL-206; EL-205's own verdict now waits for the solid strand): one record, one candidate commit, one approval; owned diff is the ledger row and the citations. Data-only review tier.

- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), in particular `ENGINE-SPATIAL-QUERIES`, `ENGINE-SENSOR-VOLUMES` and `ENGINE-ISLANDS-SLEEP` (a hanging or resting weight is the kind of body sleeping changes). The closure review runs after the programme (owner decision 22, 23 November 2026), against those stories as delivered; its four checks are re-run when ENGINE-CONNECTION-MATERIALS gives the strand a collider. Disqualifiers: a change to the collider indices or their bindings; ENGINE-CONNECTION-MATERIALS or `ENGINE-TENSILE-EXTENSIONS` changing the span law so that any of the four checks no longer holds.

## 6. Open questions

Resolved:

- **Collider under the cylinder art.** The collider is the art's solid cylinder (Story 10.2a).
- **Rolling resistance.** 0.03, the Bowling ball's (Story 10.2a, by game intent).
- **Does the rope's missing collider block this closure?** No (Disposition).
- **Closure reviews before the engine programme.** They wait for it (owner decision 22, 23 November 2026): the "Rope parts" closure review runs after the programme.

**Needs the owner.** None.

## Engine conformance (22 November 2026)

Against the owner's addendum of 22 November 2026, `AGENTS.md` (element-free engine gate, Tri-Graph) and `_bmad/custom/element-free-engine-design.md`.

1. **Declaration data only.** One declaration class, one scene, one icon. A search of `engine/`, `CuriousContraptions.Simulation`, `CuriousContraptions.Bundle` and the abstractions projects for the id `weight` and the class name found no code that names the part; "weight" there is a sample weight in airflow code. No refactor is owed.
2. **Generic laws.** Falls, rests, rolls: rigid-body dynamics with contact, friction and rolling resistance. Hangs, lifts, swings: the tension-only span law on a declared `Load` port. Size and inertia: the declared mass frame and collider bindings evaluated from `mass`.
3. **Three layers.** Colliders: one cylinder and four hidden proxies on the one body. Visuals: body, base, two bands, eye; none is a collider. Animation: none; one build-time scale binding.
4. **One-way flow and typing.** Typed `PortId`, `SimulationDomain`, `RopeAttachmentRole` and `ParameterUnit`; f32 values; nothing reads art.
5. **Flat state.** Bodies 1 (dynamic); colliders 5 (2 cylinders, 3 tori); joints, drives, sensors, domain nodes, stores and events 0; ports 1; parameters 1; collider bindings 17; presentation bindings 1 (build-time).
6. **Performance.** Five colliders against a ball's one, each owed to a drawn solid (visible = solid); redrawing the two bands flush would remove two with no gameplay loss, an optional saving CAT-067 records. A resting weight is stepped every tick because island sleeping does not exist; that gap is owned by `ENGINE-ISLANDS-SLEEP` in the engine programme (owner decision 3a), whose islands also join bodies through tensile routes, so a weight at rest on its rope sleeps with what it hangs from.
7. **Tests are scenarios.** Every row runs the production simulation.

**Verdict: Conforms**; blocked on the engine programme (owner decision 3a). The solid-strand defect is recorded against the rope connection and ENGINE-CONNECTION-MATERIALS, not against this package.
