# EL-196 · Battery — element readiness spec

**Current delivery (24 October 2026):** [EL-196 delivery slice](../invest/named-elements.md#element-196). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 Batch H named-identity spec. Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`. Values marked *proposed* have no legacy or requirement source; each carries a one-line justification, stays inside the [game-grade envelope](../../gpu-f32-physics.md#game-grade-envelope) and is a starting value under decision D1. Box sizes are full extents (legacy `AddBox` takes the full size and stores half-extents, `reference/cpu/MachinePart.cs@a6c914e:L352-L356`). The catalogue harvest (B1–B14, network N1–N21, levels L1–L5) is in [CAT-005](CAT-005-battery.md); this spec adds the finite store the named identity requires.

## 1. Identity

| Item | Value |
| --- | --- |
| Identity / name | EL-196 · Battery |
| Type | Electrical |
| Anchor | [requirements.md#element-196](../requirements.md#element-196); existing record [campaign-element-coverage](../requirements.md#campaign-element-coverage) ("Battery, wire, switch, motor …", first use 11–20); [named-elements entry](../invest/named-elements.md#element-196); owner S302 |
| Related | Refines [CAT-005 Battery](CAT-005-battery.md) ([current-cat-005](../requirements.md#current-cat-005), retained behaviour [todo-146](../requirements.md#todo-146)). Connected by [EL-197 wire](EL-197-electrical-wire.md); switched by [EL-198](EL-198-switch.md); first consumer [CAT-042 Motor](CAT-042-motor.md). |
| Roadmap story | [EL-196 delivery slice](../invest/named-elements.md#element-196); canonical proof owner and prerequisite decisions are recorded there. |
| Status | package delivered with its finite store; the identity stays open on one residual: drive work does not debit the store, so "a depleted source cannot operate a motor" is proven only for a battery that starts empty or is disabled. Residual blocked on the engine programme (owner decision 3a), which comes first, in particular `ENGINE-SUPPLY-DEMAND` (sprint key `engine-supply-drive-demand`); then a closure check of rows B5–B7. |
| Disposition | **Delivered package** `elements/Battery`, id `battery` (Stories 8.1 and 9A.39; published slice records `spec-8-1-battery.md`, `spec-9a-39-battery.md`, integration review `review-6-2-bumper-battery-integration.md`). The identity is **not a closure yet** ([delivery-batches §2c](../invest/delivery-batches.md#2-duplicate-closures): drive work does not yet debit the store, which waits for `ENGINE-SUPPLY-DEMAND`; every other row is delivered). It closes by a recorded check once the residual in §3 row 6 is delivered and rows B5–B7 of §5 run through the delivered package. |
| Planned package | the delivered `battery`; no new package, id or palette entry. Palette order 11 (`elements/Battery/BatteryElementDeclaration.cs:35`). |

## 2. Declaration

The `battery` declaration, art and bindings are [CAT-005 section 2](CAT-005-battery.md#2-declaration), including the finite store, the four parameters, the `Supply` port, the fifteen bindings and the recorded visible-solid defect of the gauge marks and plus sign (owned by CAT-005's art repair); this identity adds none (coordinator decision 27, 23 November 2026). Palette: CAT-005's order 11; no order of its own.

**Residual of this identity (not a declaration delta).** Drive work does not yet debit the store (section 3 row 6, `ENGINE-SUPPLY-DEMAND`).

### Behaviour and numbers

- **Allocation law** (delivered, `ElectricalSupply.Reserve`, `engine/gpu/ElectricalSupply.cs:11`, which grants proportionally through `ProportionalAllocation.Grant`): each phase the source can deliver at most `power_limit` × 1/120 s (`ElectricalSupply.PhaseSeconds`, `:9`; today's code; `ENGINE-SUPPLY-DEMAND` makes it `power_limit` × dt, coordinator decision 30) and at most its remaining charge; when requests exceed that, every recipient is scaled by the same fraction; the balance owner commits accepted payments and debits them; the final positive balance transfers before availability clears.
- **What draws on the store today.** Bindings to contact-work stores, such as the Bumper's (`ComposableSupplyRoutingTests.LogicProvenanceFundsNoWorkWhilePowerSourceFundsWork`). Whether the Fan's electrical airflow input (`SupplyContinuousWorkTarget`) debits the store was not verified here; the probe of `ENGINE-SUPPLY-DEMAND` settles it. **A prismatic or revolute drive does not.** Supply to a drive is binary: a closed route enables it, and its committed work debits nothing (`_bmad-output/implementation-artifacts/deferred-work.md:209`, the binary drive-supply entry; owner decision 3 November 2026 to defer it to its own slice, now `ENGINE-SUPPLY-DEMAND` under owner decision 3a). A Battery therefore never runs down by driving a Motor or a Powered gate.
- **Tick order (D7).** Electrical solve in Phase 1; an oscillator that reads this source sees the post-physics availability of the tick in which the source depletes (`ComposableSignalNodesTests.BothOscillatorsUsePostPhysicsAvailabilityWhenTheirEnabledSourceDepletes`).
- **Consistency.** `Supply` (Output) feeds any `PowerIn`, `FirstIn` or `SecondIn` input. Default Motor demand 20 N·m × 6 rad/s = 120 W equals the default `power_limit`; the Fan's electrical node is rated 480 W, the maximum `power_limit`.

## 3. Engine capabilities

Families ([element map row](../general-engine-element-map.md); [binding](../../coverage/engine/element-02.json), proof owner S302): ElectricalPower, FiniteLedger, SignalPropagation (+ StateTransaction). Map composition: "Finite ElectricalPower/FiniteLedger charge and power limits; no infinite source inferred from Boolean enabled." The harvest-baseline "Missing" list (Story 8.1, the finite store, the gauge source) is delivered. Probe on `8d1d68aa`: 5 present, 0 present-untested, 1 narrower, 0 missing.

| # | Capability | Mark | Symbol | Exercising test |
| --- | --- | --- | --- | --- |
| 1 | Declared finite source store from typed parameters | present | `elements/Battery/BatteryElementDeclaration.cs:71`; `ElectricalSourceValues.cs:12`–`:17` | `ElectricalSupplyTests.SourceDeclarationAdmitsFiniteEnergyIndependentlyOfProductCapacity` (`ElectricalSupplyTests.cs:294`) |
| 2 | Proportional sharing within one phase allowance; never more than the initial store | present | `ElectricalSupply.Reserve`, `engine/gpu/ElectricalSupply.cs:11` | `ElectricalSupplyTests.UnequalDemandSharesProportionallyAndFullLoadReceivesNothing` (`:207`); `RepeatedAllocationCannotOverfillOrSpendMoreThanInitialSource` (`:269`) |
| 3 | Unavailable or exhausted source leaves consumers unpowered | present | `engine/gpu/SupplyRoute.cs:77` | `ComposableSupplyRoutingTests.SourceUnavailableLeavesConsumersUnpowered` (`ComposableSupplyRoutingTests.cs:191`); `DirectRouteVsComposedRouteFiniteEnergyParity` (`:157`, an initially empty battery) |
| 4 | Run-time enable through the run-control lane | present | `ParameterRunEdit.RunControl`, `CuriousContraptions.Abstractions/ParameterDescriptor.cs:44` | `MotorTests.EnablingTheBatteryDuringRunSpinsTheMotorUpFromRestAndLightsItsLamp` (`MotorTests.cs:361`) |
| 5 | Committed supply and charge presentation; editor clamp | present | `PresentationDriver.CommittedSupply`, `CommittedCharge`, `CuriousContraptions.Abstractions/ElementPresentation.cs:23` | `PalettePlacementTests.GenericBatteryEditorClampsItsStoreAndPicksTheEnableStateFromItsOptions` (`PalettePlacementTests.cs:1738`); the gauge binding's own assertion was not located — re-check at planning |
| 6 | A drive's committed work debits its route's source within the power bound, and an exhausted source opens the route | **narrower**: drive supply is binary; `SupplyDriveTarget` (`CuriousContraptions.Abstractions/SupplyContractValues.cs:52`) requests no work | — | none. Owner: engine story **`ENGINE-SUPPLY-DEMAND`** (sprint key `engine-supply-drive-demand`; owner decision 3a), the slice that `deferred-work.md:209` describes ("one demand law for every powered part"); proposed story text is in `readiness/batch-h.md` section 4 |

**Dependencies.** Motor and Powered gate (delivered) as drive consumers; `ENGINE-SUPPLY-DEMAND` for row 6.

### Engine conformance (22 November 2026)

Checked against the owner's engine-conformance standard of 22 November 2026 ([engine-conformance.md](readiness/engine-conformance.md); [element-free engine design](../../../_bmad/custom/element-free-engine-design.md); AGENTS.md, Tri-Graph architecture). Verdict: **Conforms with fixes owned by CAT-005's repairs**: the proud marks and plus sign (visible-solid art repair, §2) and the unread `ElectricalNodes` ratings (CAT-005's recorded forward-only refactor). The drive-demand gap of row 6 has one owning story, `ENGINE-SUPPLY-DEMAND`, and on its own would be Conforms.

1. **Declaration data only.** The package is a declaration, a scene and bindings. `engine/`, `CuriousContraptions.Simulation`, `.Core`, `.Bundle` and the abstractions projects contain no reference to `battery` or its class (searched 22 November 2026).
2. **Generic laws.** Supply: the finite source store and proportional reservation rule of the supply network (`ElectricalSupply.Reserve`), parameterised by `capacity`, `power_limit`, `initial_charge` and `enabled`. Depletion by a drive is the generic demand law that `ENGINE-SUPPLY-DEMAND` adds for every drive; nothing in it names a battery.
3. **Three layers.** Colliders: three on the one static body (two boxes, one cylinder); nothing reads art. Visuals: `Visual_Body`, `Visual_Stripe`, `Terminal`, `Mark_0`–`Mark_3`, `Visual_PlusH`, `Visual_PlusV`; none is a collider; the six proud ones without a collider are the recorded defect, flush prints after CAT-005's repair. Animation: `Terminal` and the four marks are recoloured by presentation bindings (`CommittedSupply`, `CommittedCharge`). What moves each art node: `Terminal` and `Mark_0`–`Mark_3`, a binding (colour only); `Visual_Body`, `Visual_Stripe` and the plus sign, nothing (one static body, no pose change).
4. **One-way flow and typing.** Supply solve → committed charge → presentation; nothing flows back. `enabled` is the closed enum `ElectricalEnable`; ports are `PortId`; values are f32.
5. **Flat state.** Bodies 1 (static); colliders 3 (box 2, cylinder 1); joints 0; drives 0; sensors 0; domain nodes 1 (supply source); stores 1 (electrical charge); presentation bindings 15 rows on five art nodes (`BatteryElementDeclaration.cs:12`–`:26`).
6. **Performance fitness.** Static body; three primitives; the repair adds no collider. Per-tick work is the supply solve's per-node cost. The part makes no spatial query (nothing from `ENGINE-SPATIAL-QUERIES`) and has no dynamic body (nothing waits for `ENGINE-ISLANDS-SLEEP`). No per-part collider or binding limit is stated in the documents or code read here, and none is assumed.
7. **Tests are scenarios.** B1–B7 run the production simulation; none depends on a battery-specific code path.

## 4. Sources and legacy

- Requirement row [element-196](../requirements.md#element-196): "Finite declared electrical store supplies connected loads through bounded energy and power"; outcome "Disconnected or depleted source cannot operate a motor". [current-cat-005](../requirements.md#current-cat-005): "Binary enable is not finite-energy qualification … must name capacity/power/work/depletion and affected consumers explicitly; no invented constant supply".
- Named entry [element-196](../invest/named-elements.md#element-196), owner S302.
- Legacy: binary availability only, no capacity or power (`engine/SceneElectricalSource.cs@a6c914e:L10-L10`); all other battery facts are CAT-005 B1–B14. A supplied motor's work allowance per tick is torque × target speed × dt (`parts/MotorPart.cs@a6c914e:L93-L97`) — carry forward as the motor's power demand; do not carry forward the per-part `PreparePhysics` loop. Legacy ports and art: `parts/BatteryPart.cs@a6c914e:L12-L28`; catalogue `parts/catalog/battery.tres@a6c914e:L6-L14`; four-mark meter pattern `parts/SolarPanelPart.cs@a6c914e:L47-L53`; icon `ui/WorkshopIcons.cs@a6c914e:L47-L47`.
- **Files harvested:** `parts/BatteryPart.cs`, `parts/catalog/battery.tres`, `engine/SceneElectricalSource.cs`, `parts/MotorPart.cs`, `parts/catalog/motor.tres`, `parts/SolarPanelPart.cs` (meter pattern), `reference/cpu/MachinePart.cs` (`AddBox` extents).

## 5. Acceptance outline

Acceptance authority: [element-196](../requirements.md#element-196), [current-cat-005](../requirements.md#current-cat-005). Rows B1–B4 are executed today through the delivered package; B5–B7 are the residual and are planned in class `BatteryDriveDemandTests`, written in the closure check after `ENGINE-SUPPLY-DEMAND`.

| Row | Given / When / Then | Test | Control | Boundary |
| --- | --- | --- | --- | --- |
| B1 | Given an enabled, charged battery wired to a Motor, when Run, then the motor spins up and holds its speed | `MotorTests.ASuppliedMotorSpinsUpToItsSpeedHoldsItAndPaysOnlyItsBearingLoss` (exists) | `MotorTests.AnUnpoweredMotorStaysStillDoesNoWorkAndKeepsItsLampSlate` (no wire: the "disconnected" outcome) | — |
| B2 | Given a disabled battery, when it is enabled during Run, then the motor starts; cutting supply spins it down with no further work | `MotorTests.EnablingTheBatteryDuringRunSpinsTheMotorUpFromRestAndLightsItsLamp`; `CuttingTheSupplySpinsTheRotorDownUnderBearingFrictionWithNoFurtherWork` (exist) | — | — |
| B3 | Given loads that take work from the store, when demand exceeds the limit, then each gets the same fraction and the total debited never exceeds the initial charge | `ElectricalSupplyTests.UnequalDemandSharesProportionallyAndFullLoadReceivesNothing`; `RepeatedAllocationCannotOverfillOrSpendMoreThanInitialSource` (exist) | `UnavailableSourcePreservesBothStores` | `capacity` 60 and 14 400 J, `power_limit` 10 and 480 W: `SourceDeclarationAdmitsFiniteEnergyIndependentlyOfProductCapacity` |
| B4 | Given the four parameters and the wire, when saved, loaded and Reset, then authored values and wiring round-trip and the authored charge is restored; out-of-range and non-finite values are refused at every seam | `ElectricalSupplyTests.F32AuthoringWiringAndSaveRoundTripWithAtomicAmbiguousSourceRejection`; `RawGpuFieldsRejectBeforeNarrowing`; `ComposableSupplyRoutingTests.SaveLoadAndResetPreservesAuthoredStateAndComposableConnections` (exist) | — | `initial_charge` 0 and 1 |
| B5 (residual) | Given the rope-hung load fixture of `MotorTests.ASuppliedMotorThatWinsLiftsItsRopeHungLoadAndChargesOnlyWhatItLifts` (`MotorTests.cs:619`) with the battery set to `capacity` 60 J and `initial_charge` 0.25 (15 J, about 1.5 m of lift for the 1 kg load), when the store is spent, then the motor stops being supplied, never restarts in the run, the load has risen and stopped rising, and the lit gauge mark has gone out | `BatteryDriveDemandTests.ASmallBatterySpentByItsMotorStopsItAndNeverRestartsIt` (planned) | the same machine with 3 600 J still runs at the end of the window | stall guard 10 s (15 J at up to 120 W lasts at least 0.125 s; a stalled drive does no work and a free rotor pays only bearing loss, hence the moving load) |
| B6 (residual) | Given `initial_charge` 0, then the motor never turns and the charge stays 0 | `BatteryDriveDemandTests.AnEmptyBatteryNeverOperatesAMotor` (planned; expected to pass today, since availability already needs Q > 0) | charged battery turns it | — |
| B7 (residual) | Given two loaded Motors on one battery whose demand exceeds `power_limit`, then each is granted the same fraction, and over the run the energy debited from the store is at least the two drives' committed work, above it by no more than the ledger residual, and never more than the initial charge | `BatteryDriveDemandTests.TwoMotorsShareOnePowerLimitAndNeverSpendMoreThanTheStore` (planned) | one Motor alone gets its full demand | debited energy never below the committed work and above it by no more than the finite-energy ledger's stated residual (engine-stories-index C20): a debit below the work is energy created |

**Tolerances.** Existing tests keep their own. For the planned rows the only float bound is B7's ledger ratio, chosen from the quantity (joules debited against joules of committed drive work) and its consequence (a battery that creates energy, or visibly runs short). The Motor fixtures' 3 % slack is 3 % of rotor kinetic energy (`MotorTests.cs:42`–`:45`), not of charge, and is not reused here. Stopping is asserted as a state (unsupplied), not as a tick number.

**Generic coverage today.** Admission, Run/Reset, Save/Load: `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad`. Wrong-kind wire: `MotorTests.AnActivationWireIntoThePowerInputIsRefused`, `PackageLocalPortsTests.WrongDomainAndWrongDirection_RefusesAtomically`. Parameter refusal: B4.

**Chrome recipe (pending, post-catalogue).** Place Battery and Motor; select the battery, Connect, click the motor — the sole compatible pair `Supply → PowerIn`; set `capacity`/`initial_charge` through the contextual configuration control; run with and without the wire, disabled, and with a small capacity.

## 6. Open questions

All resolved; needs the owner: none.

1. **Finite store defaults — resolved 10 October 2026.** Owner approved 3 600 J, 120 W, initially full and enabled. The ranges are the delivered ones (D1).
2. **Voltage label — resolved 10 October 2026.** Energy and power model; no voltage simulation.
3. **Recharge — decided: none in this identity** (D19). No requirement row or campaign lesson uses it; [EL-211](EL-211-solar-panel.md) output is not stored. A later charger identity would be its own slice.
4. **Whether drive work debits the store — owner decided 3 November 2026:** yes, in its own engine story, now `ENGINE-SUPPLY-DEMAND` (owner decision 3a). Within the engine programme it is this identity's only specific blocker.
5. **Coordinator decisions, 22 November 2026 (owner's standing delegation).** (a) The proud gauge marks and plus sign are recorded as a visible-solid defect, owned by CAT-005's art repair (Art and solidity above); reason: visible = solid, and the earlier "printed flat" classification was wrong against the scene. (b) The drive-demand story is `ENGINE-SUPPLY-DEMAND` with sprint key `engine-supply-drive-demand`, by owner decision 3a, which retired the other names. (c) Coverage stays the catalogue key CAT-005.

## 7. Integration

- **Collaborators.** Motor, Powered gate, Bumper, Fan, System clock, the five gates, Impact switch, State latch, Hold timer, Counter (all delivered). Level 14 `battery_motor` grants one battery; twelve levels place a locked battery (CAT-005 L4).
- **Campaign.** "Battery" is named in the power row of [campaign-element-coverage](../requirements.md#campaign-element-coverage), first use 11–20 (reuse 31–50, 61–80, 136–150). `battery_motor` has a single `turned` goal, a kind the engine does not have, so it waits for `ENGINE-GOALS` ([delivery-batches §4](../invest/delivery-batches.md#4-early-level-proof)).
- **Processes.** CAT-005 retained behaviour [todo-146](../requirements.md#todo-146); switched supply [todo-147](../requirements.md#todo-147); [IX-06 electrical power transfer](../requirements.md#interaction-06) ("Allocate finite supplied work among loads/storage; disconnected or exhausted sources deliver none", first lesson 11).
- **Coverage binding (planned, not yet live).** Catalogue key CAT-005 (`battery`). A named binding (the package binding section that main's `tools/Coverage/NamedPackageSources.cs` reads, in the form published for EL-239) is not in the published form for a catalogue package; it is blocked on the coverage follow-on story S7, which decides whether a refinement identity gets one. Lacking today in any case: the EL-196 ledger entry has no `todo-NNN` link and owner S302 has no anchor in `scope-corrections.md`. The ledger row cites the part and tests when the identity closes.

## 8. Delivery

- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), in particular `ENGINE-SUPPLY-DEMAND` (sprint key `engine-supply-drive-demand`); the programme comes first. Then the closure check below. S7 is not needed for the closure check.
- **Slice shape.** No element slice. (1) Engine story `ENGINE-SUPPLY-DEMAND`, slice `engine-supply-drive-demand` (engine tier, six layers): one demand law for every powered drive; it re-qualifies the Powered gate and Motor supply cases. (2) A closure check for EL-196, either inside that slice's record or in one closure review: rows B5–B7 as executed tests, and the ledger row citing `battery` and the tests.
- **Files.** The engine slice's own; then `CuriousContraptions.tests/BatteryDriveDemandTests.cs` (compiled by the test project's one top-level rule; no project-file line), the EL-196 ledger entry, sprint status. Nothing under `elements/Battery/` changes for this identity.
- **Residual outside this identity.** The visible-solid defect of the gauge marks and plus sign (Art and solidity) is owned by CAT-005's art repair, carried out by a visible-solid repair contract, which edits `elements/Battery/battery.tscn` and the Battery row of `VisibleSolidColliderTests`. It does not block EL-196's closure and is not closed by it.
- **Review tier.** Engine tier for the engine slice; the closure check alone is tests and ledger rows (data-only tier).
- **Disqualifiers (re-check at planning).** The engine slice changing the Battery's parameters or ports (then the declaration table above is re-read); a consumer other than a drive still not debiting the store; the gauge binding lacking an executed assertion (add one row).
