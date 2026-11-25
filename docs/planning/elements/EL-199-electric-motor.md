# EL-199 · Electric motor named-identity spec

**Current delivery (24 October 2026):** [EL-199 delivery slice](../invest/named-elements.md#element-199). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 named-identity spec (Batch J). Harvest citations are `path@a6c914e:Lstart-Lend`. **Build-ready revision, 21 November 2026:** sections 1–3, 5 and 6 were verified against checkout `8d1d68aa`; citations of the form `path:line` are in that checkout, and test files are under `CuriousContraptions.tests/`. All values are canonical IEEE-754 f32 game values inside the [game-grade envelope](../../gpu-f32-physics.md#game-grade-envelope).

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-199 |
| Name | Electric motor |
| Type | Mechanical |
| Anchor | [requirements.md#element-199](../requirements.md#element-199); [named entry](../invest/named-elements.md#element-199); scope [campaign-element-coverage](../requirements.md#campaign-element-coverage) |
| Proof owner | S305 |
| Refines / extends | Refines [CAT-042 motor](CAT-042-motor.md) ([requirement](../requirements.md#current-cat-042)); the full legacy harvest is in that spec |
| Related | EL-196 Battery, EL-202 Conveyor, EL-203 Reverse transmission, EL-110 Flywheel, EL-209 Fan (shaft supply) |
| Delivered part | Package `motor`, directory `elements/Motor` (Story 11.1a, commit `5603a834`; run-time speed by `engine-revolute-drive-target`; Drive coupling by Story 11.1b) |
| Disposition | **Refinement with one undelivered residual; not a duplicate closure until checked.** The mechanical half of the identity (bounded torque, declared loss, stall) is delivered and tested. The electrical half ("supplied electrical work becomes … shaft torque") is not: supply is on/off and the Battery is never debited. That residual is recorded as deferred work (`_bmad-output/implementation-artifacts/deferred-work.md:209`–`:213`) and owned by ENGINE-SUPPLY-DEMAND (owner decision 3a; sprint key `engine-supply-drive-demand`), a general engine story in the programme ahead of every element. EL-199 is closed by acceptance rows inside that story. It gets no package, palette or catalogue entry of its own, and no `efficiency` parameter (section 6) |
| Status | Delivered except source debit; closure not yet run. Blocked on the engine programme (owner decision 3a), whose ENGINE-SUPPLY-DEMAND carries rows B1–B4 and closes this identity; the programme also re-qualifies the delivered package (ENGINE-ISLANDS-SLEEP, ENGINE-CAPABILITY-INSTANCES for the single-driven-hinge run control). Browser qualification pending |

**What EL-199 adds over the delivered part.** Ledger sentence: "Supplied electrical work becomes bounded shaft torque with declared losses." Exact source outcome: "Stalled shaft consumes only supported work and cannot bypass load limits."

| Clause | State | Where |
| --- | --- | --- |
| Bounded shaft torque | delivered | `torque` bounds the drive impulse; `MotorTests.cs:242` |
| Declared losses | delivered as the declared bearing friction, 0.05 N·m (`elements/Motor/MotorElementDeclaration.cs:100`) | `MotorTests.ASuppliedMotorSpinsUpToItsSpeedHoldsItAndPaysOnlyItsBearingLoss` (`:217`) |
| A stalled shaft does no work and cannot exceed its load limit | delivered | `:732`, `:767`, `:675` |
| Supplied *electrical work* pays for it | **not delivered** | ENGINE-SUPPLY-DEMAND (owner decision 3a) |

## 2. Declaration

Delivered declaration, read from `elements/Motor/MotorElementDeclaration.cs`. No change is planned to it by this identity.

- **Interfaces.** `IHingeElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:20`), `ISupplyElement` (`CuriousContraptions.Abstractions.Electrical/ElectricalContracts.cs:11`), `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`) (`:16`).
- **Bodies.** Static housing (base, body, housing, stud and lamp colliders from the scene) and one dynamic rotor: solid cylinder, radius 0.3 m, width 0.13 m, 0.5 kg, axial moment 0.0225 kg·m² (`:18`–`:20`, `:103`–`:114`). Footprint 2 bodies, 1 moving, 1 revolute (`:47`).
- **Material.** Housing and rotor: restitution 0, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s (`:61`–`:65`).
- **Hinge and drive.** Continuous revolute about local +Z at (0, 0, 0.54); bearing friction 0.05 N·m (stops the rotor from 6 rad/s in about 2.7 s); supplied velocity drive with target `speed` and maximum torque `torque` (`:88`–`:116`).
- **Parameters.**

  | Name | Unit | Min | Max | Default | Editing |
  | --- | --- | --- | --- | --- | --- |
  | `speed` | rad/s | 0 | 20 | 6 | Run control (`ParameterRunEdit.RunControl`); Reset and saves keep the authored value |
  | `torque` | N·m | 0.1 | 100 | 20 | build-only |

  (`:41`–`:44`). The harvest's "> 0 to 100" torque range is superseded: the floor is 0.1 N·m, twice the bearing friction, so the weakest Motor still starts.
- **Ports.** `PortId.PowerIn` (role `PowerIn`, Electrical, input) at (−0.55, 0, 0) facing −X; `PortId.Drive` (role `Drive`, Mechanical, output) at (0, 0, 0.65) facing +Z on revolute coordinate 0 with +1 coordinate per radian and a 0.3 m rim (`:51`–`:54`). The harvest's "−1 per radian" is superseded.
- **Supply.** One consumer whose target is the revolute drive, bound to `PowerIn` (`:120`–`:122`). One inert electrical node: 12 V, 0.072 Ω, 2000 W, the drive's peak mechanical power 100 N·m × 20 rad/s (`:124`–`:127`).
- **Stores.** None.
- **Presentation.** Rotor follows its body; `Visual/Lamp` blends slate `#556573` to gold `#f7cb52` over 0.1 s (SmoothStep) on the committed drive supply (`:23`–`:28`).
- **Palette.** Category Power, order 36, id `motor`, title "Electric motor" (`:31`–`:36`).

### Art and solidity

Classified in `VisibleSolidColliderTests.cs:99`: solid with colliders are `Visual_Base`, `Visual_Body`, `Visual_Housing`, `Visual_Stud`, `Lamp` and `Rotor/Visual_Rotor`; decorative is `Rotor/Visual_Index` (printed flush on the rotor face). No visible-solid defect is listed for this package in `spec-engine-art-solidity.md` or `spec-elements-visible-solid-remainder.md`. Colours: motor `#66b8c9`, navy `#293954` base ([DESIGN colour system](../../../DESIGN.md#colour-system)). It is told from the Fan by its side-facing disc rotor and supply lamp.

## 3. Engine capabilities

Map families: ElectricalPower, EnvironmentState, FiniteLedger, FiniteWorkActuation, JointConstraint, RigidBodyDynamics, ShaftTorque, StateTransaction ([map row](../general-engine-element-map.md); navigation only, decision D14). The harvest's "Missing" list is superseded.

| Capability | Status | Evidence |
| --- | --- | --- |
| Continuous revolute hinge with Coulomb bearing friction | present | `RevoluteConstraintDeclaration` (`CuriousContraptions.Abstractions/RevoluteConstraintValues.cs:14`); `RevoluteDriveTests.ADriveHoldsItsTargetSpeedAgainstBearingFrictionAndPaysItsLoss` (`RevoluteDriveTests.cs:219`) |
| Supplied velocity drive bounded by maximum torque | present | `RevoluteDriveDeclaration` (`RevoluteConstraintValues.cs:28`); `RevoluteDriveTests.ASuppliedDriveSpinsTheRotorUpAtItsTorqueBoundThenHoldsTheTargetSpeed` (`:176`) |
| Run-control target speed | present | `RevoluteDriveTargetRead` (`RevoluteConstraintValues.cs:45`); `MotorTests.ASpeedChangeDuringRunDrivesTheShaftToTheNewSpeedFromTheNextCommittedStep` (`MotorTests.cs:397`) |
| Committed drive state: supplied, travel, positive work | present | `RevoluteStateRead` (`RevoluteConstraintValues.cs:40`); `MotorTests.cs:217`, `:732` |
| Supply route enabling a drive, including through a gate | present | `SupplyDriveTarget` (`CuriousContraptions.Abstractions/SupplyContractValues.cs:52`); `MotorTests.AMotorSuppliedThroughAnEitherGateRunsOnlyWhileTheGateIsOn` (`:497`) |
| Mechanical `Drive` port with rim, driving other parts | present | `ConveyorTests.PoweredTransport_AcceleratesBallAlongPositiveX` (`ConveyorTests.cs:73`); `FanIntegrationTests.MotorLinkUsesDeclaredInputSenseAndActualShaftTravel` (`FanIntegrationTests.cs:574`) |
| Drive-supply presentation driver | present | `MotorTests.TheSupplyLampBlendsFromSlateToGoldOverATenthOfASecondWhenSupplied` (`:265`) |
| A drive's committed positive work debits its route's finite source | **missing** | Owner: ENGINE-SUPPLY-DEMAND (owner decision 3a; recorded as deferred work at `deferred-work.md:209`–`:213`). `FanIntegrationTests.cs:612` asserts the debit is zero today |

The missing capability is ENGINE-SUPPLY-DEMAND, named by owner decision 3a and not yet written in `epics.md`; its text is in the batch summary with Batch H's demand clause. No change to the Motor package may add a private drain: the law is one demand law for every powered part.

## 4. Sources and legacy

- Requirement: "Supplied electrical work becomes bounded shaft torque with declared losses"; outcome "Stalled shaft consumes only supported work and cannot bypass load limits" ([element-199](../requirements.md#element-199)).

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | Rotor mass, radius, width, inertia and centre | `parts/MotorPart.cs@a6c914e:L19-L27` | carry forward | Finite rotor. |
| 2 | Supply law: torque only while powered; work torque·speed·Δt; power cap torque·speed | `parts/MotorPart.cs@a6c914e:L91-L97` | carry forward (law), do not carry forward (per-part `PreparePhysics`) | Per-element loops are banned. |
| 3 | Finite non-negative budgets; zero effort disables; zero work only brakes | `engine/physics/PhysicsMotorCommand.cs@a6c914e:L5-L22` | carry forward | Bounded drive. |
| 4 | Accepted: a locked guide stops the rotor (speed and travel ≤ 1e-7); unlocking resumes > 0.1 rad/s | `CuriousContraptions.tests/SourceRotorTests.cs@a6c914e:L125-L148` | carry forward (behaviour), do not carry forward (1e-7) | Stall control. |
| 5 | Accepted: after supply loss an unloaded rotor coasts with constant energy and no new work | `CuriousContraptions.tests/MechanicalTests.cs@a6c914e:L113-L123` | superseded (coasting); carry forward (no new work) | Owner, 3 November 2026: an unpowered rotor spins down under bearing friction (`MotorTests.cs:330`). |
| 6 | No efficiency or loss term in the legacy | `parts/MotorPart.cs@a6c914e:L91-L97` | resolved | The declared loss is the bearing friction; see section 6. |

**Files harvested:**
- `parts/MotorPart.cs`
- `parts/catalog/motor.tres`
- `engine/physics/PhysicsMotorCommand.cs`
- `CuriousContraptions.tests/SourceRotorTests.cs`
- `CuriousContraptions.tests/MechanicalTests.cs`
- The remaining motor files are harvested in [CAT-042](CAT-042-motor.md#4-legacy-harvest) (Batch C). Searched with no hit for a motor efficiency or loss term: `parts/`, `engine/` (including `engine/physics/` and `engine/bridge/`), `CuriousContraptions.tests/`, `tools/`, `reference/`, `diagnostics/`.

## 5. Acceptance outline

Follow [element-199](../requirements.md#element-199) and the [CAT-042 outline](CAT-042-motor.md#5-acceptance-outline). The Chrome recipe (open `battery_motor`, place the battery, connect `Supply` → `PowerIn`, Run) stays queued for the post-catalogue phase.

### Executed today (all in `MotorTests.cs`)

| Criterion | Test |
| --- | --- |
| Supplied: spins up to `speed`, holds it, pays only its bearing loss | `:217` |
| Bounded torque: acceleration is net torque over inertia at the lowest torque | `:242` |
| **Exact source outcome:** a stalled or locked shaft does no work; an overloaded Motor is back-driven and adds none | `AStalledMotorDoesNoWorkOnceItsShaftIsHeld` (`:732`); `ALockedMotorShaftDoesNoWork` (`:767`); `AnOverloadedMotorIsBackDrivenAndAddsNoWorkWhileItIs` (`:675`) |
| Controls: unpowered stays still; supply cut spins down with no further work | `:294`; `:330` |
| Boundaries: speed 0 holds a loaded shaft; declared bounds admitted | `:555`, `:573`; `:1033` |
| Wrong-kind connection refusal; parameter refusal | `AnActivationWireIntoThePowerInputIsRefused` (`:316`); `AnOutOfRangeOrNonFiniteParameterIsRefusedAtEverySeamAndChangesNothing` (`:1044`) |
| Run/Reset; Save/Load | `:812`; `:837`, `:469` |
| Collaborators: Battery and gate, rope-hung Weight, Conveyor, Fan | `:497`; `:619`; `ConveyorTests.cs:73`; `FanIntegrationTests.cs:574` |

### Planned in ENGINE-SUPPLY-DEMAND (the rows that close EL-199)

| # | Given / When / Then | Test | Control | Boundary |
| --- | --- | --- | --- | --- |
| B1 | Given a Battery wired to a Motor turning a loaded shaft, when it runs for 5 s of game time, then the Battery's debit equals the Motor's committed positive drive work | `MotorTests.TheBatteryPaysExactlyTheMotorsCommittedDriveWork`: debit never below the committed work and above it by no more than the ledger residual (the ENGINE-SUPPLY-DEMAND acceptance): no unpaid output, and rounding may not shorten a lesson | An unwired Battery is debited nothing | An unloaded Motor at speed is debited only its bearing loss, 0.05 N·m × travelled angle |
| B2 | Given the same Motor with its shaft locked, when it runs, then the debit is zero | `MotorTests.AStalledMotorDrawsNothingFromItsBattery` | B1 is the moving control | Speed 0 with a held load: zero debit |
| B3 | Given a Battery with less energy than the run needs, when it is exhausted, then the route opens, the lamp returns to slate and the rotor spins down under bearing friction | `MotorTests.ADepletedBatteryStopsDrivingAndTheRotorSpinsDown` (stall guard 10 s) | A full Battery keeps the speed | The tick the store reaches zero pays no more than what remains |
| B4 | Run/Reset restores the Battery's authored energy and the rotor; Save/Load keeps the authored values | Extends `:812` and `:837` | — | — |

No arrival tick or hash is asserted. Speeds use the delivered Motor speed envelope of ±2% of target.

### Integration

- **Collaborators.** Battery, Conveyor, Reverse transmission, Clutch, Fan (shaft mode), Weight and Rope: delivered. EL-110 Flywheel: planned after this identity; nothing here depends on it.
- **Campaign.** Row 2: first use 11–20, reuse 31–50, 61–80, 136–150 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)); [todo-146](../requirements.md#todo-146). In `content/puzzles.json` (numbers are authored positions before decision 24d, each paired with its plan level in [campaign coverage, section 5](readiness/campaign-coverage.md#5-removed-and-converted-levels-owner-decision-24d)): Playable levels 59 `wind_then_release` and 60 `saved_for_later`; level 14 `battery_motor` and 15 `switched_motor` (planned; they need the `turned` goal kind from `ENGINE-GOALS`), levels 16 `conveyor_courier`, 17 `belt_relay` and 18 `reverse_belt` (planned; provable from delivered parts after the engine programme, owner decision 3a, in the Early level proof "Drive" group), and planned levels 21 `solar_motor`, 22 `solar_shadow`, 24 `delayed_solar`, 49 `double_cold_start` and 58 `grand_contraption`.
- **Coverage binding.** Catalogue key `motor` (CAT-042). EL-199 has no package of its own and so no package source binding. No lesson-fixture story (S8) is needed.
- **Consistency with the Battery.** When the drain lands, an unloaded Motor at 6 rad/s draws 0.3 W and one at full rating up to 2000 W. The drain slice must check the Battery's default energy against the longest Motor lesson so no delivered lesson newly runs out; that check belongs to its planning.

### Delivery

- **Slice.** None of its own. ENGINE-SUPPLY-DEMAND (sprint key `engine-supply-drive-demand`) carries rows B1–B4 and closes EL-199 and the Motor half of EL-196 together. Review tier: engine (all six layers).
- **Prerequisites.** Blocked on the engine programme (owner decision 3a), which includes ENGINE-SUPPLY-DEMAND itself; the closure rows run inside that story, after DOC-ENGINE-STATUS, ENGINE-SPATIAL-QUERIES, ENGINE-CONTACT-OBSERVATIONS, ENGINE-SENSOR-VOLUMES, ENGINE-ISLANDS-SLEEP and ENGINE-ONE-WAY-INSTALL in the programme's order. Independent of S3–S8.
- **Files (in that slice, for this identity).** `CuriousContraptions.tests/MotorTests.cs`; `FanIntegrationTests.cs:612`, whose zero-debit assertion becomes a positive one; `docs/planning/elements/CAT-042-motor.md` §6 item 4; the element-199 ledger entry; this spec. No change to `elements/Motor` is expected: the consumer and its drive target are already declared.
- **What would disqualify the plan.** The drain slice needing a per-part efficiency or standing-demand value to make B1 hold: then the Motor declares it as data and this spec's section 6 item 1 is reopened.

## 6. Open questions

None open.

1. "Declared losses": efficiency ratio, winding resistance or both? **Decided: neither; the declared loss is the bearing friction** (standing delegation, decision D19). Reason: the loss is already declared data (0.05 N·m), already paid by the drive and already tested. An `efficiency` parameter would be a control no requirement row or lesson uses, and it would have no observable effect while supply is on/off. Once the drain exists the source pays the drive's committed positive work, which includes the bearing loss, so "electrical work in exceeds useful shaft work out by the declared loss" holds with no new number.
2. Must a stalled motor draw stall power as heat? **Decided no** (D19, and the exact source outcome: "consumes only supported work"). A stalled drive does no work (`MotorTests.cs:732`), so it will draw none (row B2).
3. The [CAT-042 open questions](CAT-042-motor.md#6-open-questions) are all decided there (items 1–10, Story 11.1a and owner, 3 November 2026); item 4 is the deferred drain.
4. **Coordinator decision under the owner's standing delegation (22 November 2026): bearing friction is the declared loss and there is no `efficiency` parameter.** This confirms item 1 and closes the audit's owner item O3, although `docs/planning/invest/delivery-batches.md:145` names `efficiency` as this identity's delta; that line is corrected in the batch summary. Reason: one declared loss already exists, is paid by the drive and is tested; a second loss term would be a control no row or lesson uses.
5. Owner of the drain: ENGINE-SUPPLY-DEMAND, sprint key `engine-supply-drive-demand` (owner decision 3a, 22 November 2026, which retires the earlier part-named keys). Its demand clause is Batch H's (`readiness/batch-h.md` section 4): each supplied drive's demand is its maximum torque × |target speed|; the grant scales the drive's limit; committed positive work, never above the grant, is debited; an exhausted source opens the route on the next solve.

## Engine conformance (22 November 2026)

Checked against the owner addendum of 22 November 2026, with the package opened (`elements/Motor/`) and the engine projects searched for the package id and name. "Motor" appears in engine code only as the generic name of a drive's motor read (`PrismaticMotorRead`, `actuator.Motor`), not as this package.

1. **Declaration data only.** A static housing, one hinge-carried rotor with a declared revolute drive, a supply consumer, typed ports, two parameters and bindings. No engine code names the package.
2. **Generic laws.** The revolute drive law (target speed and torque bound from declared parameters), the hinge's declared friction torque (the declared loss), the supply-network rule that enables the drive, and the bilateral coupling of a mechanical connection. The missing half is general and stated element-free: every drive's committed positive work is debited from its route's finite sources within their power bounds, a stalled drive debits nothing, and an exhausted source opens the route. One owner: ENGINE-SUPPLY-DEMAND (owner decision 3a).
3. **Three layers.** Colliders: the housing and rotor proxies. Visuals: housing, rotor, lamp, index; none is a collider. Animation: the lamp's colour from the committed actuator drive (`elements/Motor/MotorElementDeclaration.cs:25`–`:27`); decorative, nothing written back. The rotor art follows the rotor body's pose. Recorded visible-solid item: the rotor's index is in the visible-solid remainder as a flush redraw.
4. **Flow and typing.** Typed Electrical `PowerIn` and Mechanical `Drive` ports; `speed` and `torque` are f32 Reals with units; `speed` is a run control.
5. **Flat state.** Bodies 2 (1 static, 1 dynamic); colliders as delivered; joints 1 revolute; drives 1; sensors 0; electrical nodes and supply consumers as delivered; stores 0; presentation bindings: the rotor body binding and three lamp colour channels.
6. **Performance.** One driven hinge. The debit is per drive from committed work already computed in the step, with no per-tick scan of the scene; that belongs in ENGINE-SUPPLY-DEMAND's acceptance and its measured before-and-after (owner decision 3a). No island sleeping exists today; a resting Motor train costs nothing only once ENGINE-ISLANDS-SLEEP (owner decision 3a) is delivered.
7. **Tests are scenarios.** The delivered rows and the planned rows B1 to B4 run the production simulation.

**Verdict: conforms; blocked on the engine programme (owner decision 3a), whose ENGINE-SUPPLY-DEMAND owns the one general supply capability it lacks.**
