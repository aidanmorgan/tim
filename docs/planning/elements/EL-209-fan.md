# EL-209 · Fan named-identity spec

**Current delivery (24 October 2026):** [EL-209 delivery slice](../invest/named-elements.md#element-209). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 named-identity spec (Batch J). Harvest citations are `path@a6c914e:Lstart-Lend`. **Build-ready revision, 21 November 2026:** sections 1–3, 5 and 6 were verified against checkout `8d1d68aa`; citations of the form `path:line` are in that checkout, and test files are under `CuriousContraptions.tests/`. All values are canonical IEEE-754 f32 game values inside the [game-grade envelope](../../gpu-f32-physics.md#game-grade-envelope).

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-209 |
| Name | Fan |
| Type | Mechanical |
| Anchor | [requirements.md#element-209](../requirements.md#element-209); [named entry](../invest/named-elements.md#element-209); scope [campaign-element-coverage](../requirements.md#campaign-element-coverage) |
| Proof owner | S313 |
| Refines / extends | Extends [CAT-028 fan](CAT-028-fan.md) ([requirement](../requirements.md#current-cat-028)) with explicit Electrical and Shaft supply |
| Related | EL-108 Powered airlift (same field), EL-196 Battery, EL-199 Electric motor (shaft source), CAT-069 Wind chimes and CAT-070 Windmill (receivers) |
| Delivered part | Package `fan`, directory `elements/Fan` (Story 12.2, record `spec-12-2-fan.md`, status done; commits `4378410e` and `b5b9b233`) |
| Disposition | **Delivered as two modes of the one CAT-028 package** (owner decision, 19 November 2026: one catalogue Fan with a `supply` selection). EL-209 is not a duplicate in the no-delta sense: it adds the Electrical and Shaft modes, with their ports. Both are implemented and tested, so no slice of its own remains. It is closed by a recorded closure check (section 5) in one closure review, which adds one missing control. No package, palette or catalogue entry of its own, and no package source binding (an identity delivered as a mode of another identity's package carries none) |
| Status | Native implementation and independent review complete and published. One control test to add at closure. Closure review not yet run; blocked on the engine programme (owner decision 3a), which comes first and re-qualifies the delivered package (relying most on ENGINE-SPATIAL-QUERIES and ENGINE-SUPPLY-DEMAND). Browser qualification pending |

**What EL-209 adds over the self-contained Fan, and where it is.** Requirement: "Electrical or Shaft selection on the single CAT-028 Fan drives its bounded conical air field; Self-contained remains the catalogue default. Shaft conversion is ideal with paid reaction load; Electrical draws exposed accepted source work without a hidden Fan store." Outcome: "Blocked flow or absent supply prevents remote force."

| Delta | Declared at (`elements/Fan/FanElementDeclaration.cs`) | Tested by |
| --- | --- | --- |
| `supply` choice with Electrical and Shaft | `:20` | `FanAdmissionTests.PackageScalarEndpointsAndClosedModeAreAdmittedAtomically` (`FanAdmissionTests.cs:267`) |
| `PowerIn`, available only in Electrical | `:61`–`:63` | `FanIntegrationTests.ElectricalModePaysOnlyExposedWorkAndNeverUsesSelfContainedEnergy` (`FanIntegrationTests.cs:507`) |
| `DriveIn` on the hub's revolute coordinate, available only in Shaft | `:64`–`:68` | `FanIntegrationTests.MotorLinkUsesDeclaredInputSenseAndActualShaftTravel` (`:541`) |
| Electrical work input with no Fan store | `:54`, `:72`–`:76` | `:507` asserts zero energy stores and a Battery debit within [work, 1.01 × work] |
| Shaft work input with reaction on the shaft | `:55` | `:541` asserts the shaft is held below the Motor's target while it pays |

## 2. Declaration

The `fan` declaration, art and bindings, including the `supply` choice and the field and supply contract of the Electrical and Shaft modes, are [CAT-028 section 2](CAT-028-fan.md#2-declaration); this identity adds none (coordinator decision 27, 23 November 2026). Palette: no order of its own (not placeable as a separate part: two modes of CAT-028's `fan`, order 66; coordinator decision 36).

### Variants

Three modes of one part. EL-209 names two: **Electrical** (`PowerIn` from a Battery) and **Shaft** (`DriveIn` from a Motor or other shaft). A mode edit that would leave a connection on a port the new mode does not have is refused atomically (`FanAdmissionTests.cs:191`).

### Art and solidity

[CAT-028 section 3](CAT-028-fan.md#3-art-and-solidity), including the recorded visible-solid defects (`Post`, `Ring`, `Arrow`, `Tip`) owned by `spec-elements-visible-solid-remainder.md` part A, not by this closure.

## 3. Engine capabilities

Map families: AerodynamicDrag, ElectricalPower, EnvironmentState, FiniteLedger, FiniteWorkActuation, GeometryQuery, JointConstraint, RigidBodyDynamics, ShaftTorque, StateTransaction ([map row](../general-engine-element-map.md); navigation only, decision D14). The harvest's "Missing" list is superseded.

| Capability | Status | Evidence |
| --- | --- | --- |
| Conical airflow source with paid work and solid occlusion | present | `IAirflowSourceElement` (`MechanicalContracts.cs:40`); `FanGuidanceTests.TransparentCurvedAndTaperedSolidWallsOccludeOnlyWhenInTheStream` (`FanGuidanceTests.cs:45`) |
| Electrical work input: the source pays only exposed accepted work | present | `ElectricalAirflowInput` (`CuriousContraptions.Abstractions/AirflowValues.cs:37`); `FanIntegrationTests.cs:507` |
| Coordinate (shaft) work input with reaction on the coordinate | present | `CoordinateAirflowInput` (`AirflowValues.cs:38`); `FanIntegrationTests.cs:541` |
| Continuous-work supply target | present | `SupplyContinuousWorkTarget` (`CuriousContraptions.Abstractions/SupplyContractValues.cs:50`); `FanIntegrationTests.ThousandSharedConsumersCommitPaidWorkThroughInstalledValidation` (`:109`) |
| Choice-selected ports, inputs, stores and bindings | present | `FanIntegrationTests.SelectedModeCompilesOneWorkInputAndOneBladeWriterWithoutHiddenStores` (`:452`) |
| Flow-speed and revolute-angle presentation drivers | present | `FanIntegrationTests.CommittedFlowPhaseHoldsOnPauseStepsOnceAndResetsWithoutUnexposedDebit` (`:586`) |
| A Motor's shaft work debiting its Battery | **missing** (not a Fan capability) | ENGINE-SUPPLY-DEMAND (sprint key `engine-supply-drive-demand`; owner decision 3a); `FanIntegrationTests.cs:579` asserts the Motor's Battery debit is zero today. It limits what the Shaft mode can teach about batteries; the closure's own rows do not depend on it, and the programme re-qualifies the Shaft mode |

Nothing here needs element-specific engine code.

## 4. Sources and legacy

- Requirement: "Explicit electrical or shaft supply drives a bounded air field"; outcome "Blocked flow or absent supply prevents remote force" ([element-209](../requirements.md#element-209)).
- The air-jet facts below match the [CAT-028 harvest](CAT-028-fan.md#appendix-historical-evidence) (its facts 23, 24, 27 and 29); CAT-028 holds the full fan harvest.

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | Emitter: reach, width, supply with reference speed 12 and power force × 12; impedance force/12 | `parts/FanPart.cs@a6c914e:L19-L23` | carry forward | Bounded field. |
| 2 | Parameter bounds and finite checks | `parts/FanPart.cs@a6c914e:L24-L31` | carry forward | Admission. |
| 3 | Self-contained 14,400 J store; `powered` flag; "no external electrical input yet" | `parts/FanPart.cs@a6c914e:L12-L16`; `parts/catalog/fan.tres@a6c914e:L11-L11` | do not carry forward for EL-209 | EL-209 is explicitly supplied. |
| 4 | Accepted: acceleration 6/mass for 1 kg and 4 kg; a blocker gives zero force and no released energy; kinetic energy ≤ released energy | `CuriousContraptions.tests/AirJetWorldTests.cs@a6c914e:L18-L66` | carry forward | Blocked-flow control. |
| 5 | Level `wind_signal`: moving air strikes the switch to power the lamp; inventory fan 1 | `content/puzzles.json@a6c914e:L1811-L2092` | carry forward | Lesson set-up. |
| 6 | Accepted: a blocker moving through the jet cuts the force only while it overlaps the jet (receiver speed 0.4 m/s for one blocker, 0.3 m/s for a two-part blocker over 0.6 s); released energy equals 4 W × exposed time; only exposed work is committed and the rest is dissipated | `CuriousContraptions.tests/AirJetOcclusionTests.cs@a6c914e:L13-L40` | carry forward (behaviour at substep resolution) | Exposed-only work. |
| 7 | Accepted: exposure and occlusion neither need nor create supply; an unpowered load has zero demand and changes no body; excluded bodies do not occlude | `CuriousContraptions.tests/AirJetGeometryTests.cs@a6c914e:L8-L32` | carry forward | Exposure never creates supply. |
| 8 | Accepted: a stationary blocker occludes a moving receiver only while its collider participates; a non-colliding blocker never occludes | `CuriousContraptions.tests/AirJetOcclusionTests.cs@a6c914e:L48-L72` | carry forward | Non-colliding parts do not block. |
| 9 | Accepted: inlet, outlet and rim crossings in both directions switch exposure on and off; a source whose collider is disabled emits no field | `CuriousContraptions.tests/AirJetBoundaryTests.cs@a6c914e:L16-L45`, `CuriousContraptions.tests/AirJetBoundaryTests.cs@a6c914e:L126-L135` | carry forward (force starts and stops at the field boundary) | Field edges. |
| 10 | Continuous root-finding sweeps for boundary and occlusion times (tolerance 1e-7, tangency and initially-on-surface handling, sweep validation), and whole-world capture/restore replay | `CuriousContraptions.tests/AirJetOcclusionTests.cs@a6c914e:L41-L45`, `CuriousContraptions.tests/AirJetOcclusionTests.cs@a6c914e:L74-L90`, `CuriousContraptions.tests/AirJetBoundaryTests.cs@a6c914e:L102-L124`, `CuriousContraptions.tests/AirJetBoundaryTests.cs@a6c914e:L136-L143` | do not carry forward | CPU continuous-sweep solver path and rollback; the worker samples at 480 Hz substeps and never rolls back a tick. |

**Files harvested:**
- `parts/FanPart.cs`
- `parts/catalog/fan.tres`
- `parts/ConveyorPart.cs` (mechanical input convention)
- `engine/AirflowNetwork.cs`
- `CuriousContraptions.tests/AirJetWorldTests.cs`
- `CuriousContraptions.tests/AirJetOcclusionTests.cs`
- `CuriousContraptions.tests/AirJetGeometryTests.cs`
- `CuriousContraptions.tests/AirJetBoundaryTests.cs`
- `content/puzzles.json` (level `wind_signal`)
- The remaining fan and air-jet files are harvested in [CAT-028](CAT-028-fan.md#appendix-historical-evidence) (Batch D). Searched with no hit for an electrically or shaft-supplied fan at the harvest baseline: `parts/`, `engine/` (including `engine/physics/` and `engine/bridge/`), `CuriousContraptions.tests/`, `tools/`, `reference/`, `diagnostics/`.

## 5. Acceptance outline

Follow [element-209](../requirements.md#element-209) and [current-cat-028](../requirements.md#current-cat-028). The Chrome recipe (place the fan across a ball's fall, a battery on `PowerIn` or a motor on `DriveIn`, a receiver downwind, Run) stays queued for the post-catalogue phase.

### Closure check

| Criterion | Test |
| --- | --- |
| Electrical, positive: a wired Battery pays and the ball is pushed | `FanIntegrationTests.cs:507` (wired row); `FanGuidanceTests.ElectricalConeGuidesFallingTennisToReceiverWithMatchedControls` (`FanGuidanceTests.cs:124`) |
| **Absent supply, Electrical:** unwired, no push, no work, no debit | `:507` (unwired row) |
| Shaft, positive: a Motor on `DriveIn` drives the field and is loaded by it | `:541` (connected, 6 rad/s, exposed) |
| **Absent supply, Shaft:** disconnected shaft or speed 0, no work and no push | `:541` (disconnected row; speed 0 row) |
| **Blocked flow:** a solid in the stream stops the force; the Fan's own hub does not | `FanGuidanceTests.cs:45`, `:97`; `FanIntegrationTests.ActualHubDoesNotOccludeThePaidConeButOffAndVacuumDoNotTransfer` (`:481`) |
| No hidden store: Electrical compiles no energy store; each mode has one work input | `:507`; `:452` |
| Exposure alone costs nothing | `:541` (connected, unexposed row: the shaft runs at the Motor's speed); `:586` |
| Boundaries: parameter endpoints and the closed mode set | `FanAdmissionTests.cs:267` |
| Parameter refusal; invalid references | `FanAdmissionTests.cs:267`, `:325` |
| Wrong-kind and inactive-mode connection refusal | `FanAdmissionTests.InactiveConnectionAndRetainedWireModeEditPreserveInstallation` (`:191`); wrong domain by the generic `BundlePortsTests.AWrongDomainOrOutputToOutputLinkIsStillRefused` (`CuriousContraptions.tests/BundlePortsTests.cs:262`) |
| Run/Reset; Save/Load | `FanIntegrationTests.ExposedPayloadLifecyclePreservesPaidStateAndRestoresAuthoredInputs` (`:25`); `FanAdmissionTests.SaveLoadPreservesSelectionAndRecreatesAuthoredBalances` (`:216`) |
| Light versus heavy payload | `FanGuidanceTests.ExistingPayloadsReceivePaidLateralGuidanceOnlyWithEnabledFlow` (`:16`) |

**One control to add at closure.** A start-empty Battery is covered (`FanGuidanceTests.cs:112`, `:133`, control `EmptyBattery`). Not covered: a Battery that runs out mid-run. Add `FanIntegrationTests.ADepletedBatteryStopsTheElectricalCone`:

- **Given** an Electrical Fan wired to a Battery whose authored energy is less than the run's exposed work, and a Tennis ball in the cone, **when** the Battery reaches zero, **then** the source's committed work stops growing, the Battery's remaining energy is at least 0 and no more than 1 % of its authored energy (the debit is quantised, `FanIntegrationTests.cs:526`), and the ball's speed along the axis stops increasing. Stall guard 10 s of game time.
- **Control.** The same scene with a full Battery keeps paying over the same time.
- **Boundary.** The tick that empties the Battery pays no more than what remained (the committed work total never exceeds the Battery's authored energy, with the 1% allowance of `:526`).

### Integration

- **Collaborators.** Battery, Motor, Tennis ball, Bowling ball, Wall, Pipe, Receiver, Impact switch, Windmill: all delivered. CAT-069 Wind chimes: not delivered; its integration is that part's criterion.
- **Campaign.** Row 2 ("… fan"): first use 11–20, reuse 31–50, 61–80, 136–150 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)); fan → windmill → belt in [todo-190](../requirements.md#todo-190). The Fan is in 22 levels of `content/puzzles.json`; 3 `air_mail`, 5 `third_dimension` and 8 `wind_signal` are provable now (Early level proof), and 9 `cold_start` and the composite levels wait for `ENGINE-GOALS`. Those levels use the Self-contained mode. No authored level yet uses Electrical or Shaft; the first that does needs no new part.
- **Coverage binding.** Catalogue key `fan` (CAT-028). EL-209 has no package of its own and so no package source binding. No lesson-fixture story (S8) is needed by the closure.

### Delivery

- **Slice.** One closure review ("Already-built behaviour", with EL-197 if its check also passes). Owned diff: the one test above and ledger rows. Review tier: data-only.
- **Prerequisites.** Blocked on the engine programme (owner decision 3a), which comes first; the closure review then re-runs its rows on the re-qualified package. It is serialised with visible-solid part A, which edits `FanIntegrationTests.cs`; whichever publishes second rebases its test.
- **Files.** `CuriousContraptions.tests/FanIntegrationTests.cs`; `docs/planning/invest/named-elements.md` (the element-209 entry: replace "Backlog: implementation and native proof must be established" with the delivered statement and the tests above); `docs/planning/invest/delivery-batches.md` (EL-209 leaves the 1b "Already-built behaviour" run); this spec's status.
- **What would disqualify the plan.** The depleted-Battery test failing (a real supply defect, repaired in an engine slice). A requirement that the Shaft mode prove battery-to-air conversion end to end: that waits for ENGINE-SUPPLY-DEMAND and is recorded there, not here.

## 6. Resolved decisions

One catalogue Fan with three selectable supply modes and a cone; ideal shaft conversion with paid reaction load; force-rated transfer under global 64 m/s² and 1024 rad/s² bounds, without a separate 16 m/s² transfer cap. Width is terminal radius, sourced static material remains, and 14,400 J is a finite Self-contained budget rather than a timer. Full design and current evidence live in the [native contract](../../../_bmad-output/implementation-artifacts/spec-12-2-fan.md).

No question is open. Decided in this revision under the standing delegation:

1. Does EL-209 need a package or slice of its own? **No.** It is the Electrical and Shaft modes of `fan` (owner, 19 November 2026); decision D8 gives a mode no catalogue entry.
2. Is a shaft-driven Fan required beyond the delivered one? **It is delivered** (`:541`); nothing further is required of the Fan. Battery drain through a Motor is ENGINE-SUPPLY-DEMAND's.

## Engine conformance (22 November 2026)

Checked against the owner addendum of 22 November 2026, with the package opened (`elements/Fan/`) and the engine projects searched for the package id and name (no hit).

1. **Declaration data only.** A static body with a hub, one declared airflow source with three selected inputs (self-contained store, electrical, shaft), a `supply` selection, ports, parameters and bindings. The two EL-209 modes are selections of declared inputs, not branches in engine code.
2. **Generic laws.** The conical airflow law with paid work (force by exposure and relative speed, capped by force and power), the supply rule for the Electrical input and the coordinate airflow input for the Shaft mode. Occlusion is the general line test against colliders.
3. **Three layers.** Colliders: the base box on the static body and the hub sphere on the hinge body. Visuals: housing, hub, blades, streaks; none is a collider. Animation: the blades are turned by one mode-selected binding (`CommittedFlowSpeed` in Self-contained and Electrical, `CommittedRevoluteAngle` in Shaft) and the streaks are a glow; decorative, nothing written back. The hub art follows the hinge body's pose. Visible-solid items for this package are those listed in visible-solid remainder part A.
4. **Flow and typing.** `supply` is a closed Choice carried as an enum (`SupplyMode`); `PowerIn` is Electrical and `DriveIn` Mechanical; f32 Real parameters.
5. **Flat state.** Bodies 2 (1 static base, 1 hub on a hinge); colliders 2 (box 1: base; sphere 1: hub) until visible-solid part A adds the post box and the ring torus; airflow sources 1 with 3 inputs; joints 1 revolute (the hub, for the Shaft mode); stores, signal latch, electrical nodes and supply consumers as delivered; presentation bindings: the hub body and one blade-turning binding selected by mode.
6. **Performance.** One bounded cone sampled at declared receiver samples. Today the airflow pass sweeps every receiver sample against every collider; ENGINE-SPATIAL-QUERIES supplies the cone's candidates, bounding the work by the bodies inside it. No island sleeping exists today; a resting ball beside an idle Fan costs nothing only once ENGINE-ISLANDS-SLEEP (owner decision 3a) is delivered, ahead of this part.
7. **Tests are scenarios.** The delivered rows and the added depleted-Battery control run the production simulation.

**Verdict: conforms.**
