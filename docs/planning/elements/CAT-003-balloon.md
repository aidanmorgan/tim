# CAT-003 · balloon — element spec

**Current delivery:** delivered by Story 12.1b ([native spec](../../../_bmad-output/implementation-artifacts/spec-12-1b-balloon.md)). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md). Browser and release qualification remain pending. Every residual slice (part C, the two closure-review tests, EL-208's closure) is **blocked on the engine programme (owner decision 3a, 22 November 2026)**; the stories that own this part's gaps are `ENGINE-SPATIAL-QUERIES` (airflow occlusion candidates), `ENGINE-CONTACT-OBSERVATIONS` (the integrity observer), `ENGINE-ISLANDS-SLEEP`, `ENGINE-CAPABILITY-INSTANCES` (payload participation mask) and `ENGINE-GAS-NETWORK` (sphere-only gas store); the tether rows re-run under `ENGINE-CONNECTION-MATERIALS` when the rope gains its collider.

One current text, repaired 22 November 2026 after the Batch D audit, checked against `elements/Balloon/` and its tests in checkout `8d1d68aa`. Citations without `@` are `path:line` in that checkout; `Decl` is `elements/Balloon/BalloonElementDeclaration.cs` and `Scene` is `elements/Balloon/balloon.tscn`. Citations with `@a6c914e` are the legacy harvest and appear only in the closing appendix.

## 1. Identity and disposition

| Field | Value |
| --- | --- |
| CAT ID / kind | CAT-003 · `balloon` |
| Requirement anchor | [CAT-003](../requirements.md#current-cat-003); owner key [CAT-003-I](../invest/current-consumers.md#cat-003-i) |
| Disposition | **Delivered.** Package id and catalogue key `balloon`, directory `elements/Balloon/`, category Motion, palette order 65 (`Decl:16`–`:19`). Published in `4424da13`; slice record status done |
| Mapped identities | [EL-208 Balloon](EL-208-balloon.md): its three behaviours (rise, tether, puncture with gas loss) are delivered here with executed tests (rows 1, 4 and 5), so EL-208 is a closure candidate for its own review, which decides it. Distinct neighbours, not merged: [TH-25 Hot-air balloon](../invest/named-elements.md#thermal-25), [EL-090 Steerable blimp](../invest/named-elements.md#element-090), [EL-125 Authored atmosphere preset](../invest/named-elements.md#element-125) |
| Capability row | [general-engine-element-map, CAT-003](../general-engine-element-map.md) |

## 2. Declaration

Interfaces: `IMechanicalElement`, `ISceneGeometryElement`, `IParameterizedGeometryElement`, `IParameterizedMaterialElement`, `IGasStoreElement`, `IGasShapeElement`, `IContactIntegrityElement`, `IAmbientLoadElement`, `IAirflowReceiverElement` (`Decl:10`–`:12`).

| Parameter | Unit | Min | Max | Default |
| --- | --- | --- | --- | --- |
| `mass` (initial total) | kg | 0.125 | 8 | 0.5 |
| `bounce` | none | 0 | 1 | 0.2 |
| `radius` (in Standard atmosphere) | m | 0.18 | 0.5 | 0.36 |
| `buoyancy` | m/s² | 0 | 16 | 11.5 |
| `drag` | 1/s | 0 | 4 | 0.4 |

All `Real`, build-only, default editor policy (`Decl:21`–`:25`).

| Item | Declaration |
| --- | --- |
| Body | one dynamic body; footprint 1 body, 1 moving (`Decl:36`) |
| Collider | one sphere bound to `radius`; in physics its radius follows the gas shape (`Decl:46`–`:49`, `:67`) |
| Material | restitution bound to `bounce`; friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s (`Decl:45`, `:50`–`:51`) |
| Mass source | centred inventory: declared mass is the initial total; the dry carrier is the total less the reference gas, and current gas is added once. Dry moments 0.4 × (0.0625 m)² × dry mass; gas adds translational mass only (`Decl:58`–`:62`) |
| Gas store | one finite sealed store: R 2000 and Cv 3000 J/(kg·K), reference 300 K, gauge 1000 Pa, admission limits 1 kg/m³ and 200000 Pa; conductance reference mass per (1000 Pa · 1 s) when open (`Decl:63`–`:66`). These are the delivered values. They differ from the gas conventions ([EL-039](EL-039-air-reservoir.md) F1 and F2: pressure in pascals absolute against 101 325 Pa; the one ambient temperature of the thermal conventions, [TH-04](TH-04-electrical-heating-plate.md) section 11, 288 K): the 1000 Pa envelope gauge is an overpressure (101 325 + 1000 = 102 325 Pa absolute) and the 300 K reference is not the ambient. Bringing the delivered store onto the conventions is a forward refactor owned by `ENGINE-GAS-NETWORK` with its first slice, re-running rows 1, 2, 5 and 7 |
| Gas shape | dry radius 0.0625 m; radius³ = dry radius³ + 3V/(4π) (`Decl:67`) |
| Integrity observer | one, on the sphere: ruptures when one contact pair's accepted normal impulse in a substep exceeds 3 N·s with approach above 0.1 m/s; the occurrence opens the store's outlet once (`Decl:68`) |
| Ambient load | lift `buoyancy` and `drag`, scaled by the atmosphere's relative pressure and by the remaining gas fraction (`Decl:69`–`:71`) |
| Airflow receiver | one: four collider-normalised samples of weight 0.25 (`Decl:40`–`:43`) |
| Port | `PortId.Tie`, domain Rope, bidirectional, role Tie, at the body origin on the dry carrier, attachment role Load (`Decl:37`–`:39`). Shrinking never moves it |
| Event | `IntegrityBroken` (`Decl:53`), which a level's goal can select by exact owner and observation |
| Presentation | `Envelope` uniform scale and `Neck` local Y, both from `CommittedGasRadius` (`Decl:27`–`:35`) |

The scene's atmosphere is an explicit build-only preset, Standard (101325 Pa, relative pressure 1) or Vacuum (0 Pa, 0); an unknown or missing saved preset is refused, never defaulted. In Vacuum the same sealed inventory expands, and an expansion that would overlap other geometry is refused atomically at preparation.

## 3. Art and solidity

| Node | Mesh | Class | Moved by |
| --- | --- | --- | --- |
| `Envelope` | unit sphere scaled to the gas radius, pink `#ed6378` (`Scene:11`–`:14`, `:38`–`:40`) | solid: sphere collider (`Scene:55`–`:57`) | the gas-radius binding; its collider follows the same committed gas in physics |
| `Neck` | tapered cylinder 0.06 m long under the envelope (`Scene:16`–`:20`, `:42`–`:44`) | decorative: moved by a binding | the gas-radius binding |
| `Carrier` | sphere r 0.0625 m (`Scene:28`–`:31`) | decorative: enclosed by the envelope at every admitted radius | the body's pose |
| `String` | rod r 0.006 m, 0.85 m long, cream `#e8dccb` (`Scene:22`–`:26`, `:46`–`:48`) | decorative: the Balloon string is one of the two non-solid strand exceptions (owner, 21 November 2026; owner decision 1, 22 November 2026; thin wires are the other) | the body's pose |

The package has no row in the visible-solid table. [Part C](../../../_bmad-output/implementation-artifacts/spec-elements-visible-solid-remainder-c.md) (drafted, blocked on parts B and B2) adds the row, teaches the matcher to size the envelope from its declared gas, and proves the drawn radius equals the collider's within 0.1 mm at radius 0.18, 0.36 and 0.5 m. It changes neither the scene nor the declaration.

**A known gap part C records (its O1).** Each time a Balloon is constructed, and on every Reset, a binding first draws its zero-input value: the envelope appears at 0.0625 m, with the neck at the centre, against a collider of 0.18 to 0.5 m, until the first committed sample arrives. Owner decision, 22 November 2026: if part C's test confirms that the 0.0625 m picture is only momentary (the first sample arrives in Build without Run), it is recorded as a named defect with an owning engine slice and part C proceeds. Correcting the initial projection is a shared-presentation engine change, not a part change, owned by `ENGINE-ONE-WAY-INSTALL` ("a presentation binding driven by committed state draws its first committed sample on construction and Reset, never a zero-input value; proved on two packages"; engine-stories-index section 2, coordinator decision 29). The string keeps its non-solid exception (owner decision 1); no rope, cable, hose, duct, belt or chain inherits it. The rope a Balloon is tied to is a solid strand: its missing collider is a recorded visible-solid defect owned by `ENGINE-CONNECTION-MATERIALS` (solid strand).

Icon: a pink envelope with a knot and a curling string. At a glance it differs from every ball by the knot and string.

## 4. Engine capabilities

| Capability | Mark | Symbol and test |
| --- | --- | --- |
| Finite sealed gas store with a compliant envelope | present | `IGasStoreElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:109`); `GasCapabilityTests.PreparedSealedInventoryConservesMassAndAdiabaticEnergy` (`CuriousContraptions.tests/GasCapabilityTests.cs:13`) |
| Collider and art follow the gas radius | present | `IGasShapeElement` (`MechanicalContracts.cs:114`); `BalloonArtTests.NativeEnvelopeAndNeckFollowCommittedRadiusWithAnIndependentDryCarrierAndCenterTie` (`CuriousContraptions.tests/BalloonArtTests.cs:10`) |
| Rupture by one contact pair's impulse | present | `IContactIntegrityElement` (`MechanicalContracts.cs:119`); `GasRuntimeTests.ExactContactOpensFiniteStoreAndResetRestoresPreparedState` (`CuriousContraptions.tests/GasRuntimeTests.cs:15`) |
| Lift and drag scaled by atmosphere and gas fraction | present | `IAmbientLoadElement` (`MechanicalContracts.cs:124`); `BalloonTests.StandardRisesAgainstHeavierControlAndVacuumFallsWithTheSameSealedInventory` (`CuriousContraptions.tests/BalloonTests.cs:22`) |
| Rope tie on a dynamic body | present | `PortRole.Tie`; `BalloonTests.TautTieRestrainsLiftWhileSlackAndDisconnectedControlsRise` (`:95`) |
| Airflow receiver | present | `IAirflowReceiverElement` (`MechanicalContracts.cs:45`); `FanGuidanceTests.ExistingPayloadsReceivePaidLateralGuidanceOnlyWithEnabledFlow` (`CuriousContraptions.tests/FanGuidanceTests.cs:16`) |
| Integrity goal | present | `SimulationEventKind.IntegrityBroken` (`CuriousContraptions.Abstractions/EventDescriptor.cs:33`); `BalloonTests.RupturePrecedesTimedPartialLossAndPauseStepResumeResetOwnTheOpenStore` (`:202`) |

Counts: 7 present, 0 untested, 0 narrower, 0 missing. Joined stores, lines and valves are not this part: they belong to `ENGINE-GAS-NETWORK`.

## 5. Behaviour and numbers

- **Rise.** Net lift is 11.5 − 9.81 = 1.69 m/s² in Standard atmosphere; with drag 0.4 /s the rise settles near 4.2 m/s. In Vacuum there is no lift and it falls. A heavier body with no lift falls beside it.
- **Ceiling.** Arriving at a static ceiling at that speed gives about 0.5 × 1.2 × 4.2 = 2.5 N·s, under the 3 N·s rupture threshold, which is why a Wall holds it.
- **Rupture.** A Bowling ball (4 kg) ruptures it above about 5.6 m/s closing speed (reduced mass 0.44 kg, restitution 0.2); a Tennis ball does not at ordinary speeds. Discharge starts in the substep after the committed rupture and follows the pressure difference; it is not a timer. Outflow carries its momentum away and adds no thrust. Empty, the balloon has no lift and its dry mass.
- **Tether.** A taut tie restrains the lift; a slack tie leaves it free.
- **Cannon.** Default width 0.72 m lies inside the 0.6–0.84 m seating window. No test fires a Balloon; the outcome is unverified.

## 6. Acceptance

All executed in the published package, except rows 10 and 11, which the EL-208 closure review adds (its rows A6 and A9).

| # | Criterion (CAT-003) | Test (`BalloonTests` unless named) |
| --- | --- | --- |
| 1 | Rises in Standard; heavier control falls; zero pressure falls | `StandardRisesAgainstHeavierControlAndVacuumFallsWithTheSameSealedInventory` |
| 2 | Rests under a ceiling without rupture | `OrdinaryCeilingContactSupportsTheDefaultEnvelopeWithoutRupture` (`:41`) |
| 3 | Lift and drag are configurable | `NondefaultLiftAndStrongDragChangeStandardMotion` (`:140`) |
| 4 | Tether: taut restrains, slack and disconnected rise | `TautTieRestrainsLiftWhileSlackAndDisconnectedControlsRise` |
| 5 | Rupture empties the store; the tie does not move | `AConnectedEnvelopeRupturesAndEmptiesWithoutMovingItsDeclaredTie` (`:118`) |
| 6 | Fan guidance, with the no-flow control | `FanGuidanceTests.ExistingPayloadsReceivePaidLateralGuidanceOnlyWithEnabledFlow` |
| 7 | Save, Reset and explicit atmosphere | `AllControlsAndExplicitAtmosphereSurviveSaveAndReset` (`:71`), `MissingOrUnknownSerializedAtmosphereIsRefusedRatherThanDefaulted` (`:264`) |
| 8 | Vacuum expansion against an obstruction is refused atomically | `VacuumExpansionRefusesObstructionWithoutChangingInstalledConstruction` (`:55`) |
| 9 | Wrong-kind connection to the Tie port is refused | generic `BundlePortsTests.AWrongDomainOrOutputToOutputLinkIsStillRefused` (`CuriousContraptions.tests/BundlePortsTests.cs:262`) |
| 10 | Occluded jet on a Balloon (the requirement's control): a solid wall across the stream leaves it unguided | planned `FanGuidanceTests.AWallAcrossTheStreamLeavesABalloonUnguided` ([EL-208](EL-208-balloon.md) row A6: powered Fan, Balloon at (3, 5, 0), a `wall` 3 × 2 × 0.25 m at (1, 5, 0) turned 90° about Y; 12 ticks; sideways speed under 0.01 m/s and zero airflow work). Control: no wall, above 0.1 m/s. Boundary: wall at z = 2.5, guided again. Occlusion is proven on other payloads today (`FanGuidanceTests.TransparentCurvedAndTaperedSolidWallsOccludeOnlyWhenInTheStream`, `:45`) |
| 11 | Responds to mass: at the default lift a heavier Balloon ruptures on the ceiling that holds the default one | planned `BalloonTests.AHeavierBalloonAtTheDefaultLiftRupturesOnTheCeilingThatHoldsTheDefault` (EL-208 row A9: row 2's ceiling scene with `mass` 4 kg; 960 ticks; observer Broken, one `IntegrityBroken`, store empty). Control: the default 0.5 kg stays Intact. Boundary: 1 kg stays Intact and seated. 8 s stall guard |

Chrome recipe: post-catalogue.

## 7. Integration

Works with Fan, Wall, Rope anchor, Pulley, Weight, Bowling ball (delivered); Bellows (Story 12.4, in flight: its row B1 propels a Balloon). Campaign: owner decision 24e moves its first use to level **9a** in chapter 1, where the Playable `burst_of_motion` (authored position 63) places it; tether and pop continue in chapter 2, levels 11–20 ([campaign reservations](../invest/campaign-reservations.md); [per-level allocation](readiness/campaign-coverage.md#6-per-level-allocation-and-progression-check-owner-decisions-24e-and-24f)). Coverage binding: catalogue key `balloon`. Lesson-fixture story (S8) only where a level pre-places it.

## 8. Delivery

Delivered. Residual work: part C's table row and its O1 outcome (engine review tier); acceptance rows 10 and 11, added by EL-208's closure review "Cargo"; browser proof, post-catalogue; the CAT-003 checkbox is still open in `requirements.md` (see the [batch summary](readiness/batch-d.md)).

**Prerequisites, in order.** Blocked on the engine programme (owner decision 3a, 22 November 2026), which precedes every further element; in particular `ENGINE-SPATIAL-QUERIES` (row 10's occlusion is re-checked after it), `ENGINE-CONTACT-OBSERVATIONS` (rows 2, 5 and 11), `ENGINE-ISLANDS-SLEEP` and `ENGINE-CAPABILITY-INSTANCES`; then part C after parts B and B2; then the closure review. `ENGINE-GAS-NETWORK` removes the sphere-only gas-store coupling (audit E10) with its first slice; `ENGINE-CONNECTION-MATERIALS` re-runs rows 4 and 5 when the rope gains its collider.

## 9. Open questions

None open. Resolved by the delivered package: pressure is an explicit atmosphere preset; drag is admitted from 0 to 4 /s; gas and puncture are a finite sealed store with a contact-impulse rupture; rolling resistance is 0; the five material values are build-only parameters; an authored Wall is the ceiling.

## Engine conformance

Against the owner's conformance addendum of 22 November 2026 (element-free engine gate in [AGENTS.md](../../../AGENTS.md); design contracts D1–D7 in `_bmad/custom/element-free-engine-design.md`). **Verdict: conforms; fix pending (point 3: part C's table row and its O1, the first-drawn envelope).**

1. **Declaration data only.** One declaration class of descriptors, one scene, one icon. No runtime project names the part: a search of `engine/`, `ui/` and every `CuriousContraptions.*` runtime project for `balloon` and `Balloon` returns nothing. There is no per-kind material table and none is to be added: material is this package's declared data.
2. **Generic laws.** Rise and fall: the ambient-load law. Size: the isobaric gas-store law. Rupture: the contact-integrity observation opening the store's outlet. Deflation: the store's conductance law. Tether: the tensile-span law on a declared tie port. Fan push: the conical airflow law on declared samples.
3. **Layers.** Colliders: one hidden sphere `col_0_0`, whose radius the physics graph takes from the gas shape. Visuals: `Envelope`, `Neck`, `Carrier`, `String`; none is a collider. Animation: two bindings read the committed gas radius and write nothing back. Art and collider are driven separately from the same committed state; they disagree only before the first sample (O1 above).
4. **One-way flow and typing.** Physics publishes the committed gas radius; presentation reads it. The atmosphere preset is an enum; unknown values are refused.
5. **Flat state.** 1 dynamic body; 1 sphere collider; 0 joints and drives; 1 contact-integrity observer; 1 gas store; 1 gas shape; 1 ambient load; 1 airflow receiver with 4 samples; 1 rope port; 0 signal or supply nodes; 1 event kind; 2 presentation bindings. All fixed at compile time.
6. **Performance.** One primitive collider. The integrity observer reads only its own collider's accepted contacts, but the engine finds them by scanning all contacts (`ENGINE-CONTACT-OBSERVATIONS`). Island sleeping is not implemented (`docs/gpu-f32-physics.md:273` describes it); a resting Balloon is still stepped. Owned by `ENGINE-ISLANDS-SLEEP`; nothing in this part works around its absence. Shared contracts it uses owe two refactors: the sphere-only gas-store coupling (audit E10, `ENGINE-GAS-NETWORK`) and the `AllPayloads` target role (audit E12, `ENGINE-CAPABILITY-INSTANCES`).
7. **Tests are scenarios.** All rows run the production simulation; unfamiliar-declaration cases in `GasRuntimeTests` show the laws do not depend on this package.

## Appendix: Historical evidence

Evidence of the legacy catalogue entry at `a6c914e`, kept so the harvest stays traceable. Nothing here is a requirement, a tolerance or a design instruction; sections 1–9 are the spec. At that baseline the balloon was a ball with a per-kind material and no gas; that shape no longer exists.

| # | Fact | Citation |
| --- | --- | --- |
| 1 | Legacy material: mass 0.5, bounce 0.2, radius 0.36, buoyancy 11.5, drag 0.4 | `parts/catalog/balloon.tres@a6c914e:L14-L14` |
| 2 | Legacy contact material: restitution = bounce, threshold 0.1, friction 0.3 | `reference/cpu/MachinePart.cs@a6c914e:L236-L236`; `engine/physics/PersistentContactPair.cs@a6c914e:L23-L23` |
| 3 | Legacy lift = buoyancy × world pressure × mass and drag rate = drag × pressure, applied in a per-substep loop that is not the design | `reference/cpu/MachineWorld.cs@a6c914e:L869-L883`; `engine/MachineData.cs@a6c914e:L171-L171` |
| 4 | Legacy drag force law, giving a mass-independent terminal rise speed | `engine/physics/BodyDragLoad.cs@a6c914e:L6-L32` |
| 5 | Legacy tick and substep rates | `reference/cpu/MachineWorld.cs@a6c914e:L20-L21` |
| 6 | Buoyant balls drew a hanging string | `reference/cpu/BallPart.cs@a6c914e:L26-L27` |
| 7 | The balloon had to carry all five ball parameters | `CuriousContraptions.tests/RequiredPhysicsParameterTests.cs@a6c914e:L47-L81` |
| 8 | No level used the balloon at the baseline | `docs/coverage/catalogue-elements.json` |
| 9 | Legacy catalogue entry, colour and icon | `parts/catalog/balloon.tres@a6c914e:L8-L14`; `DESIGN.md@a6c914e:L168-L168`; `ui/WorkshopIcons.cs@a6c914e:L42-L42` |

**Files harvested:**
- `parts/catalog/balloon.tres`
- `parts/scenes/ball.tscn` (no element knowledge: script reference only)
- `reference/cpu/BallPart.cs`
- `reference/cpu/MachinePart.cs`
- `reference/cpu/MachineWorld.cs`
- `engine/MachineData.cs`
- `engine/physics/BodyDragLoad.cs`
- `engine/physics/PersistentContactPair.cs`
- `CuriousContraptions.tests/RequiredPhysicsParameterTests.cs`

Consulted, no balloon knowledge (an unused sealed-gas foundation at the baseline): `engine/physics/SealedGasState.cs`, `engine/physics/IdealGasMaterial.cs`, `engine/physics/PhysicsGasNode.cs`, `CuriousContraptions.tests/SealedGasStateTests.cs`, `CuriousContraptions.tests/GasChamberWorldTests.cs`.
