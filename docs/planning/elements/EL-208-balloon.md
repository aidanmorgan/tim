# EL-208 · Balloon — named-identity readiness spec

**Current delivery (24 October 2026):** [EL-208 delivery slice](../invest/named-elements.md#element-208). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Rewritten on 22 November 2026 as one current text, checked against `elements/Balloon/` and its tests in checkout `8d1d68aa`. Citations without `@` are `path:line` in that checkout; `Decl` is `elements/Balloon/BalloonElementDeclaration.cs` and bare test line numbers are in `CuriousContraptions.tests/BalloonTests.cs`. `path@a6c914e` citations appear only in section 4 and are source facts. The delivered declaration is recorded once, in [CAT-003](CAT-003-balloon.md).

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-208 · Balloon · Gravity |
| Anchor | [requirements.md#element-208](../requirements.md#element-208) (task `sequence-task-761`); [named-elements entry](../invest/named-elements.md#element-208); scope source [campaign element coverage](../requirements.md#campaign-element-coverage) |
| Owner key / slice | S479; `named-el-208` |
| Refines | [CAT-003 balloon](CAT-003-balloon.md) ([requirement](../requirements.md#current-cat-003)). Neighbours, not merged: TH-25 Hot-air balloon, EL-090 Steerable blimp, [EL-125 Atmosphere](EL-125-authored-atmosphere-preset.md) |
| Row and outcome | "Gas-filled compliant buoyant cargo responds to pressure, mass and puncture properties." Outcome: "Tether force opposes ascent; lost contents alter buoyancy." |
| Status | Delivered through package `balloon`, published in `4424da13` (Story 12.1b). The closure review is blocked on the engine programme (owner decision 3a), including `ENGINE-CONTACT-OBSERVATIONS` and `ENGINE-CAPABILITY-INSTANCES` (payload contract), and runs after it (owner decision 22); repair part C is a repair and may run before it |

### Disposition

**Candidate Duplicate closure of the delivered package `balloon`** (`elements/Balloon/`, palette order 65, `Decl:18`). Delivery batches 2c lists three behaviours of its own (buoyant rise, tether, puncture with gas loss); all three are delivered in the package and tested, so the identity has no delta left (Batch D reached the same result). It gets no package, catalogue entry or palette order.

**Closure check.** The review records, against the published SHA:

1. Tether opposes ascent: `TautTieRestrainsLiftWhileSlackAndDisconnectedControlsRise` (`:95`, opened).
2. Lost contents alter buoyancy: `AConnectedEnvelopeRupturesAndEmptiesWithoutMovingItsDeclaredTie` (`:118`, opened): after rupture the gas mass is zero and the carrier falls.
3. Pressure: `StandardRisesAgainstHeavierControlAndVacuumFallsWithTheSameSealedInventory` (`:22`).
4. Mass, one added test, row A9: at the default lift a heavier Balloon ruptures on the ceiling that holds the default one. The row says "responds to ... mass", and every delivered Balloon test that changes `mass` also changes `buoyancy`, so none shows mass alone.
5. One added test, row A6: a solid wall across the stream leaves a Balloon unguided. CAT-003 names the "occluded jet" control and no test runs it on a Balloon.
6. Reset and save: `AllControlsAndExplicitAtmosphereSurviveSaveAndReset` (`:71`).

Then the `element-208` ledger entry cites `balloon` and those tests. Group: the "Cargo" closure review with EL-189 (decided now, section 6).

Nothing is deliverable now: the closure review is **blocked on the engine programme (owner decision 3a)**. The closure review runs after that programme (owner decision 22, 23 November 2026). Under the coordinator decision of 22 November 2026 the acceptance rows of [CAT-003](CAT-003-balloon.md) are the single authority for this closure identity; the rows in section 5 cite them and add rows A6 and A9, which CAT-003 takes up when its owner next edits it.

## 2. Declaration

Delivered; every value and line is in [CAT-003](CAT-003-balloon.md). In short: one dynamic body with a sphere collider whose radius follows the gas; five build-only parameters, `mass` 0.5 kg (0.125 to 8), `bounce` 0.2 (0 to 1), `radius` 0.36 m in Standard atmosphere (0.18 to 0.5), `buoyancy` 11.5 m/s² (0 to 16), `drag` 0.4 1/s (0 to 4) (`Decl:21`–`:25`); one finite sealed gas store (R 2000 and Cv 3000 J/(kg·K), 300 K, gauge 1000 Pa); one gas shape with a 0.0625 m dry core; one contact-integrity observer (impulse above 3 N·s on one collider pair approaching faster than 0.1 m/s); one ambient load whose lift is `buoyancy` × relative pressure × remaining gas fraction; one airflow receiver; one port `PortId.Tie`, Rope domain, bidirectional, at the body origin (`Decl:38`); one event kind, `IntegrityBroken`.

**Material, as declared data.** Restitution bound to `bounce` by a `MaterialParameterBinding`, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s (`Decl:45`, `:50`–`:51`): the four fields of `ContactMaterialDeclaration` (`CuriousContraptions.Abstractions/PhysicsDeclarationValues.cs:133`–`:143`). The "puncture property" is not a material strength: the contact material has no strength field. It is a declared contact-integrity observation on the body, a general capability any package can declare. Load-limited connections and fragments are a different capability, owned by ENGINE-FRACTURE, and this identity does not need it. Gas values belong to the gas store declaration, not to the contact material.

### Art and solidity

| Node | Class | Moved by |
| --- | --- | --- |
| `Envelope`, sphere scaled to the gas radius, pink `#ed6378` | solid; sphere collider sized from the same gas radius | a presentation binding on the committed gas radius |
| `Neck` | decorative: moved by the gas-radius binding | the same binding |
| `Carrier`, sphere r 0.0625 m | decorative: enclosed by the envelope at every admitted radius | body pose |
| `String`, rod r 0.006 m, 0.85 m long | decorative: the one non-solid strand (Balloon string, owner decision 1), an exception decided on 21 November 2026 and kept by owner decision 1 of 22 November 2026 | body pose |

Two items from `_bmad-output/implementation-artifacts/spec-engine-art-solidity.md:92` and [visible-solid remainder part C](../../../_bmad-output/implementation-artifacts/spec-elements-visible-solid-remainder-c.md). Part C is one of the visible-solid repair parts that may run before the engine programme (owner decision 22, 23 November 2026):

- The package has no row in `CuriousContraptions.tests/VisibleSolidColliderTests.cs`. Part C adds it and proves the drawn envelope equals the collider within 0.1 mm at radius 0.18, 0.36 and 0.5 m. It changes neither scene nor declaration.
- **The 0.0625 m picture.** On construction and on every Reset each binding first draws its zero-input value, so the envelope appears at 0.0625 m against a collider of 0.18 to 0.5 m until the first committed sample arrives. Owner decision, 22 November 2026: if part C's test confirms the picture is only momentary, it is recorded as a named defect with an owning engine slice (the initial projection of a binding driven by committed state, for every package) and part C proceeds. It is a defect of the shared presentation path, not of this declaration, and it does not hold this closure.

A rope tied to the Tie is drawn by the rope connection, whose strand has no collider; that defect belongs to ENGINE-CONNECTION-MATERIALS (solid strand), not to this package. At a glance: the pink envelope with a knot and a string; no ball has either.

## 3. Engine capabilities

### Capability probe

Probe of 22 November 2026. "Opened" means the test body was read for this spec; the others are cited from CAT-003's probe and confirmed by name and line.

| Capability | Status | Symbol | Test |
| --- | --- | --- | --- |
| Lift and drag scaled by the atmosphere preset and the remaining gas fraction | present | `IAmbientLoadElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:124`) | `StandardRisesAgainstHeavierControlAndVacuumFallsWithTheSameSealedInventory` (`:22`) |
| Rope tie on a dynamic body; taut restrains, slack does not | present | `PortRole.Tie`; `TensileConstraintSolver` (`CuriousContraptions.Simulation/TensileConstraintSolver.cs:27`) | `TautTieRestrainsLiftWhileSlackAndDisconnectedControlsRise` (`:95`, opened) |
| Rupture by one contact pair's impulse, opening the store once | present | `IContactIntegrityElement` (`MechanicalContracts.cs:119`) | `AConnectedEnvelopeRupturesAndEmptiesWithoutMovingItsDeclaredTie` (`:118`, opened); `GasRuntimeTests.ExactContactOpensFiniteStoreAndResetRestoresPreparedState` (`CuriousContraptions.tests/GasRuntimeTests.cs:15`) |
| Finite sealed gas store with a compliant envelope | present | `IGasStoreElement` (`MechanicalContracts.cs:109`) | `GasCapabilityTests.PreparedSealedInventoryConservesMassAndAdiabaticEnergy` (`CuriousContraptions.tests/GasCapabilityTests.cs:13`) |
| Collider and art follow the gas radius | present | `IGasShapeElement` (`MechanicalContracts.cs:114`) | `BalloonArtTests.NativeEnvelopeAndNeckFollowCommittedRadiusWithAnIndependentDryCarrierAndCenterTie` (`CuriousContraptions.tests/BalloonArtTests.cs:10`) |
| Airflow receiver, and occlusion of a sample line by any enabled collider | present | `IAirflowReceiverElement` (`MechanicalContracts.cs:45`); `CuriousContraptions.Simulation/WorkshopPhysicsEngine.Airflow.cs:151` | `FanGuidanceTests.ExistingPayloadsReceivePaidLateralGuidanceOnlyWithEnabledFlow` (`CuriousContraptions.tests/FanGuidanceTests.cs:16`, opened, Balloon rows); occlusion on a Tennis ball in `ElectricalConeGuidesFallingTennisToReceiverWithMatchedControls` (`:124`, opened). Not yet run on a Balloon: row A6 |
| Integrity goal on the committed occurrence | present | `SimulationEventKind.IntegrityBroken` (`CuriousContraptions.Abstractions/EventDescriptor.cs:33`) | `RupturePrecedesTimedPartialLossAndPauseStepResumeResetOwnTheOpenStore` (`:198`) |

Counts: 7 present, 0 untested, 0 narrower, 0 missing. Joined stores, lines and valves belong to ENGINE-GAS-NETWORK and are not this identity.

## 4. Sources and legacy

- **Requirements.** Row and outcome as in section 1. CAT-003: zero-pressure, no-fan, occluded-jet and heavier-body controls.

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | Material mass 0.5, bounce 0.2, radius 0.36, buoyancy 11.5, drag 0.4. | `parts/catalog/balloon.tres@a6c914e:L14-L14` | Delivered as the five parameter defaults (f32). |
| 2 | Buoyancy and drag law scaled by world pressure. | `reference/cpu/MachineWorld.cs@a6c914e:L877-L883` | Delivered as the ambient load scaled by the atmosphere preset; the CPU loop is not carried. |
| 3 | Buoyant balls drew a hanging string. | `reference/cpu/BallPart.cs@a6c914e:L26-L27` | Delivered as the `String` art; the Tie is at the body origin. |
| 4 | Ball parameters Radius, Mass, Bounce, Buoyancy, Drag were required; missing ones rejected. | `reference/cpu/BallPart.cs@a6c914e:L4-L13`; `CuriousContraptions.tests/RequiredPhysicsParameterTests.cs@a6c914e:L47-L81` | Delivered as typed parameters with refusal; the string-keyed dictionary is not carried. |
| 5 | No legacy level, puncture or gas behaviour for the balloon. | `content/puzzles.json@a6c914e:L1-L33007` (no `balloon` kind) | Puncture and gas loss were new and are delivered; level 63 `burst_of_motion` now places it. |

Files consulted: `parts/catalog/balloon.tres`, `reference/cpu/MachineWorld.cs`, `reference/cpu/BallPart.cs`, `reference/cpu/MachinePart.cs`, `CuriousContraptions.tests/RequiredPhysicsParameterTests.cs`, `content/puzzles.json`.

## 5. Acceptance outline

Point of truth: [element-208](../requirements.md#element-208) and [CAT-003](../requirements.md#current-cat-003). Chrome proof is pending post-catalogue.

### Behaviour and numbers

- **Rise.** In Standard atmosphere with a full store the net upward acceleration is 11.5 − 9.81 = 1.69 m/s². With drag 0.4 1/s the speed tends to 1.69 / 0.4 = 4.2 m/s. From rest the height gained in 1 s is 4.2 × (1 − (1 − e^−0.4) / 0.4) = 4.2 × 0.176 = 0.74 m, so the free control starting at y = 3 passes the test's 3.5 m.
- **Tether opposes ascent.** The anchor is at y = 1 and the rope is 2 m, so the taut balloon holds with its centre at y = 3 (the test allows ±0.02 m and under 0.05 m/s). The tension at rest is the net lift, 0.5 kg × 1.69 m/s² = 0.85 N. An 8 m rope is slack, and that balloon rises with the untied one (within 0.02 m).
- **Rupture.** The impulse on a static obstacle is m·(1 + e)·v. For the default balloon arriving at its 4.2 m/s limit under a neutral wall (pair restitution 0.2 × 1): 0.5 × 1.2 × 4.2 = 2.5 N·s, under the 3 N·s threshold, so a ceiling holds it. The delivered rupture test uses mass 2 kg and buoyancy 16 m/s²: net 16 − 9.81 = 6.19 m/s²; the threshold needs v above 3 / (2 × 1.2) = 1.25 m/s, reached after 1.25² / (2 × 6.19) = 0.13 m of rise; the balloon has 4.8 − 3.36 = 1.44 m to the ceiling's underside and arrives near √(2 × 6.19 × 1.44) = 4.2 m/s (drag neglected), an impulse near 10 N·s.
- **Responds to mass (row A9).** The lift is a declared acceleration (`buoyancy`, m/s², `Decl:69`–`:71`) and the drag a declared rate, so neither depends on `mass`: a heavier Balloon rises exactly as the default does and arrives with more momentum. In the delivered ceiling scene (`:41`) the Balloon starts at y = 3 and meets the wall's underside (y = 4.8) with its centre at 4.8 − 0.36 = 4.44, a rise of 1.44 m. With dv/dt = a − k·v, a = 1.69 m/s² and k = 0.4 1/s: v = (a/k)(1 − e^−kt) and x = (a/k)(t − (1 − e^−kt)/k), with a/k = 4.225. x = 1.44 m at t = 1.43 s (4.225 × (1.43 − 0.4356/0.4) = 4.225 × 0.341 = 1.44), where v = 4.225 × 0.4356 = 1.84 m/s. The impulse that stops the body is m·v and, with the pair restitution 0.2, at most m·1.2·v. At 0.5 kg: 0.92 to 1.10 N·s, a third of the 3 N·s threshold, so the ceiling holds it (the delivered test). At 1 kg: 1.84 to 2.21 N·s, still 26% under at the most. At 4 kg: 7.4 to 8.8 N·s, 2.4 times the threshold at the least, and the same impulse as the delivered rupture case (2 kg at 3.8 to 4.2 m/s). A 2 kg Balloon at the default lift would give 3.7 to 4.4 N·s, only 23% over at the least, so the row uses 4 kg. The body's mass also includes the sealed gas, which the arithmetic neglects; it is the same in every case.
- **Lost contents alter buoyancy.** Lift is proportional to the remaining gas fraction, so an empty store gives zero lift and the dry carrier falls at g: the test finds it below y = 1 with a store mass of exactly zero (an emptied ledger, not a simulated float).
- **Tick order.** By `docs/gpu-f32-physics.md` the airflow field is evaluated in Phase 2, before the four mechanical substeps of Phase 3 in which contact, the tether and the rupture impulse are solved; the store's discharge starts on the substep after the rupture commits; sensor evaluation and event dispatch are Phase 5.
- **Fit with delivered parts.** 0.72 m across fits the Cannon's 0.6 to 0.84 m window (no test fires one: unverified). A 4 kg Bowling ball ruptures it above about 5.6 m/s closing speed (reduced mass 0.44 kg), which `burst_of_motion` uses.

### Acceptance rows

| # | Given / When / Then | Test | Control and boundary | State |
| --- | --- | --- | --- | --- |
| A1 | Tied to an anchor by a taut rope it holds at the rope's length | `TautTieRestrainsLiftWhileSlackAndDisconnectedControlsRise` | a slack rope and no rope both rise above 3.5 m; ±0.02 m; 3 s | executed |
| A2 | Ruptured, it empties and falls, and the tie does not move | `AConnectedEnvelopeRupturesAndEmptiesWithoutMovingItsDeclaredTie` | one `IntegrityBroken` record; 8 s | executed |
| A3 | It rises in Standard; a heavier body falls; in Vacuum the same sealed inventory falls | `StandardRisesAgainstHeavierControlAndVacuumFallsWithTheSameSealedInventory` | Bowling ball; Vacuum | executed |
| A4 | An ordinary ceiling holds it without rupture | `OrdinaryCeilingContactSupportsTheDefaultEnvelopeWithoutRupture` (`:41`) | the threshold arithmetic above | executed |
| A5 | A stream pushes it sideways; with no flow it is not pushed | `FanGuidanceTests.ExistingPayloadsReceivePaidLateralGuidanceOnlyWithEnabledFlow` | flow disabled: zero sideways speed and zero airflow work | executed |
| A6 | A solid wall across the stream leaves it unguided | planned `FanGuidanceTests.AWallAcrossTheStreamLeavesABalloonUnguided`: the scene of A5 (powered Fan, Balloon at (3, 5, 0)) plus a `wall` 3 × 2 × 0.25 m at (1, 5, 0) turned 90° about Y, as in the delivered `Control.SolidWall` row; 12 ticks. Then the Balloon's sideways speed stays under 0.01 m/s and the airflow work is zero | control: the same scene without the wall exceeds 0.1 m/s (row A5's bound). Boundary: the wall moved to z = 2.5, its near edge at z = 1.0 and 0.64 m clear of the envelope: guided again. 0.01 m/s is a tenth of what row A5 counts as guidance | planned, added by the closure review |
| A7 | All five controls and the atmosphere survive save and Reset; a missing or unknown atmosphere is refused | `AllControlsAndExplicitAtmosphereSurviveSaveAndReset`; `MissingOrUnknownSerializedAtmosphereIsRefusedRatherThanDefaulted` (`:264`) | — | executed |
| A8 | A wrong-kind connection to the Tie is refused | generic `BundlePortsTests.AWrongDomainOrOutputToOutputLinkIsStillRefused` (`CuriousContraptions.tests/BundlePortsTests.cs:262`) | — | executed |
| A9 | At the default lift a heavier Balloon ruptures on the ceiling that holds the default one | planned `BalloonTests.AHeavierBalloonAtTheDefaultLiftRupturesOnTheCeilingThatHoldsTheDefault`: the scene of row A4 (`wall` 3 × 0.4 × 2 m at (0, 5, 0), Balloon at (0, 3, 0)) with only `mass` changed to 4 kg; 960 ticks. Then the integrity observer is Broken, one `IntegrityBroken` record is published and the store's mass is zero | control: row A4, the default 0.5 kg in the same scene stays Intact. Boundary: `mass` 1 kg stays Intact and seated (centre 4.42 to 4.46 m, as row A4). The outcome is a phase, so no tolerance on a float; 8 s stall guard, more than five times the 1.43 s rise | planned, added by the closure review |

### Integration

- **Collaborators.** Delivered: Fan, Wall, Rope anchor, Pulley, Weight, Bowling ball, Receiver. In flight: Bellows (12.4). Both ends of the tie are Rope-domain, bidirectional ports. Interaction rows [IX-10](../requirements.md#interaction-10), [IX-11](../requirements.md#interaction-11).
- **Campaign.** First use 11–20 with tether and pop; level 63 `burst_of_motion` is Playable and places it.
- **Coverage binding.** The catalogue key `balloon` in `docs/coverage/catalogue-elements.json`. A closed duplicate has no binding of its own.
- **Lesson fixture (S8).** Only where a level pre-places it.

### Delivery

Closure review "Cargo": owned diff is the tests of rows A6 and A9 and the ledger row; data-only review tier.

- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), in particular `ENGINE-SPATIAL-QUERIES` (airflow occlusion, row A6), `ENGINE-CONTACT-OBSERVATIONS` (the integrity observer, rows A2, A4 and A9), `ENGINE-SENSOR-VOLUMES`, `ENGINE-ISLANDS-SLEEP` and `ENGINE-CAPABILITY-INSTANCES` (the payload contract). The closure review runs after the programme (owner decision 22, 23 November 2026), against those stories as delivered.

Part C (engine tier) is a repair, may run before the programme (owner decision 22) and is independent of the closure. Disqualifiers: row A6 failing, which would be an engine defect in airflow occlusion and returns to planning; row A9's 1 kg boundary rupturing or its 4 kg case holding, which would mean the integrity measure is not the stopping impulse and returns to planning with the measure read from the solver; a change to the rupture threshold or the tie position.

## 6. Open questions

Resolved:

- **Drag 0.4 1/s.** Admitted: the package's range is 0 to 4 (`Decl:25`).
- **Puncture trigger.** A contact impulse above 3 N·s; a sharp-feature trigger is not needed by any lesson (D19).
- **Scope of Story 12.1b.** It delivered rise, tether and puncture together.
- **Closure group** (decided now, standing delegation). "Cargo", with EL-189. Reason: both are payloads closed against delivered packages by one added test each; a review of its own would cost a second record for the same kind of check.
- **The 0.0625 m picture.** Owner decision, 22 November 2026 (section 2).
- **Closure reviews before the engine programme.** They wait for it (owner decision 22, 23 November 2026): the "Cargo" closure review (rows A6 and A9) runs after the programme; repair part C may run before it.

**Needs the owner.** None.

## Engine conformance (22 November 2026)

Against the owner's addendum of 22 November 2026, `AGENTS.md` (element-free engine gate, Tri-Graph) and `_bmad/custom/element-free-engine-design.md`.

1. **Declaration data only.** One declaration class, one scene, one icon. A search of `engine/`, `CuriousContraptions.Simulation`, `CuriousContraptions.Bundle` and the abstractions projects for `balloon` and `Balloon` found nothing. Two refactors are owed, by shared contracts the part uses and not by the part. First, the gas store can be coupled to a sphere only (`CuriousContraptions.Abstractions/GasCouplingValues.cs:10`, `CuriousContraptions.Abstractions/IsobaricGas.cs:133`; engine design audit finding E10): a product shape, removed by `ENGINE-GAS-NETWORK`. Second, its `PayloadPolicy.CaptureTarget` (`Decl:56`) is the product role that `BodyTargetKind.AllPayloads` builds into the target filter (`CuriousContraptions.Abstractions/ContactWorkDeclarationValues.cs:31`, `:43`; finding E12), replaced by a declared participation mask in `ENGINE-CAPABILITY-INSTANCES`. This package's declaration changes in each of those slices and the closure checks are re-run there.
2. **Generic laws.** Rise and fall: the ambient-load law scaled by atmosphere and gas fraction. Size: the isobaric gas-store law. Rupture: a contact-integrity observation opening the store's outlet. Deflation: the store's conductance law. Tether: the tension-only span law on a declared port. Fan push and shelter: the conical airflow law with occlusion by any enabled collider.
3. **Three layers.** Colliders: one hidden sphere whose radius physics takes from the gas shape. Visuals: envelope, neck, carrier, string; none is a collider. Animation: two presentation bindings read the committed gas radius and write nothing back; the momentary zero-input picture is the recorded defect above.
4. **One-way flow and typing.** Physics publishes the gas radius; presentation reads it. The atmosphere preset is an enum and unknown values are refused.
5. **Flat state.** Bodies 1 (dynamic); colliders 1 (sphere); joints and drives 0; contact-integrity observers 1; gas stores 1; gas shapes 1; ambient loads 1; airflow receivers 1 (4 samples); ports 1; signal and supply nodes 0; event kinds 1; material bindings 1; presentation bindings 2.
6. **Performance.** One primitive. The integrity observer reads only its own collider's accepted contacts; finding its row by compiled handle on any body pair is `ENGINE-CONTACT-OBSERVATIONS`. The airflow pass walks every collider per sample line (`ENGINE-SPATIAL-QUERIES`), and a balloon at rest against a ceiling or on a taut tether is stepped every tick because island sleeping does not exist (`ENGINE-ISLANDS-SLEEP`). All three are engine gaps owned inside the engine programme (owner decision 3a); none is designed around here.
7. **Tests are scenarios.** Every row runs the production simulation; `GasRuntimeTests` uses unfamiliar declarations, so the laws do not depend on this package.

**Verdict: Conforms with fixes** (the visible-solid table row of part C; the recorded initial-projection defect; the E10 and E12 contract refactors owed by `ENGINE-GAS-NETWORK` and `ENGINE-CAPABILITY-INSTANCES`); blocked on the engine programme (owner decision 3a).
