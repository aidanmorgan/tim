# EL-195 · Pinball bumper named-identity spec

**Current delivery (24 October 2026):** [EL-195 delivery slice](../invest/named-elements.md#element-195). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 named-identity spec (Batch J). Harvest citations are `path@a6c914e:Lstart-Lend`. **Build-ready revision, 21 November 2026:** sections 1–3, 5 and 6 were verified against checkout `8d1d68aa`; citations of the form `path:line` are in that checkout, and test files are under `CuriousContraptions.tests/`. All values are canonical IEEE-754 f32 game values inside the [game-grade envelope](../../gpu-f32-physics.md#game-grade-envelope).

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-195 |
| Name | Pinball bumper |
| Type | Mechanical |
| Anchor | [requirements.md#element-195](../requirements.md#element-195); [named entry](../invest/named-elements.md#element-195); scope [campaign-element-coverage](../requirements.md#campaign-element-coverage) |
| Proof owner | S301 |
| Refines / extends | Refines [CAT-015 bumper](CAT-015-bumper.md) ([requirement](../requirements.md#current-cat-015)) |
| Related | CAT-005 Battery (recharge), CAT-066 Wall (`wall_and_bumper`), EL-194 Springboard |
| Delivered part | Package `bumper`, directory `elements/Bumper` (Stories 6.2 and 6.3, records `spec-6-2-bumper-battery-integration.md` and `spec-6-3-bumper-multi-angle-contacts.md`, both done; generic conversion `c13d07c5`; solid cap `057df5ae`) |
| Disposition | **Candidate Duplicate closure of CAT-015.** Its earlier delta, "a supplied variant with its own `PowerIn`", is already part of the delivered package and is tested. The identity then has no behaviour, parameter, port, mode or material of its own. It is closed in the "Launch parts" closure review ([delivery batches 2a](../invest/delivery-batches.md#2-duplicate-closures)), with one order: the part check runs in that review; the CAT-015 row and the EL-195 ledger row are ticked only after Story 15.0's isolated-assistance control (`readiness/batch-a.md`, EL-195 row). EL-194 is not in that review: it is a refinement with its own acceptance rows (owner decision 7). No package, palette or catalogue entry of its own |
| Status | Behaviour delivered; closure review not yet run; blocked on the engine programme (owner decision 3a), which comes first and re-qualifies the delivered package (relying most on ENGINE-CONTACT-OBSERVATIONS and ENGINE-SUPPLY-DEMAND). The CAT-015 and EL-195 ledger rows are ticked only after Story 15.0's isolated-assistance control. Browser qualification pending |

**What EL-195 adds over the delivered part: nothing that is undelivered.** Ledger sentence: "Bounded supplied or explicitly charged contact actuator repels touching cargo." Exact source outcome: "No available energy or missed contact means no powered kick." The delivered Bumper is both things in one part: it is explicitly charged (a finite store preloaded at Run) and supplied (the store is refilled only by real work through a wired `PowerIn`). The earlier proposal of a second "Supplied" variant with a `supply` enum and no reservoir is withdrawn (section 6).

## 2. Declaration

Delivered declaration, read from `elements/Bumper/BumperElementDeclaration.cs`.

- **Interfaces.** `IContactWorkElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:70`), `IEnergyStoreElement` (`:35`), `ISupplyElement` (`CuriousContraptions.Abstractions.Electrical/ElectricalContracts.cs:11`), `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`) (`:14`).
- **Body.** One static body with three scene colliders: the head sphere (radius 0.65 m), the upper ring (a full torus, 2 cm proud of the head) and the cap (`elements/Bumper/bumper.tscn:73`–`:84`). Footprint 1 body, 0 moving (`:47`).
- **Material.** Restitution 1, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s (`:58`): with no payable energy it is an ordinary elastic sphere.
- **Contact work.** One `ContactWorkDescriptor`: target speed from `strength`, qualifying approach speed 0.05 m/s, per-target cooldown 72 physics steps (0.15 s at 480 Hz) (`:39`–`:40`).
- **Store.** One energy store whose initial energy and capacity are both the preload ½ · 1 kg · `strength`²: 32 J at the default 8 m/s, 200 J at 20 m/s, empty at 0 (`:42`–`:45`).
- **Parameter.** `strength`: real, m/s, 0–20, default 8, editor policy Clamp (`:36`). The harvest's proposed `supply` enum is not declared and is not planned.
- **Port.** `PortId.PowerIn` (role `PowerIn`, Electrical, input) at (0, 0, 0.65) facing +Z (`:51`).
- **Supply.** One consumer whose target is the energy store, bound to `PowerIn` (`:79`–`:81`); electrical node "coil": 12 V, 10 Ω, `MaxPower` 50 W (`:83`–`:86`). That 50 W is metadata and does not bound charging: `ContactWorkInstanceTests.SuppliedContactStoreRetainsSourceLimitedChargingAboveFiftyWatts` (`CuriousContraptions.tests/ContactWorkInstanceTests.cs:275`–`:296`) asserts that a 120 W Battery delivers 1 J per 1/120 s. The refill rate is the source's `power_limit` (Battery 10 to 480 W, default 120, `elements/Battery/BatteryElementDeclaration.cs:44`): 120 W refills the default 32 J store in 0.27 s, and 480 W in 0.07 s, inside the 0.15 s cooldown.
- **Event.** `Impact` (`:61`).
- **Presentation.** On each paid impact the head scales 1 → 0.88 and the impact ring scales 1 → 1.24 and blends cream to gold, sine-squared pulse over 0.32 s, saturating sum (`:16`–`:23`). Unpaid contacts do not pulse. The head's squash scales the art of `Visual/BumperHead` while its sphere collider keeps its size: an accepted cosmetic exception (owner decision 8).
- **Palette.** Category Motion, order 7, id `bumper`, title "Pinball bumper" (`:26`–`:31`).

### Art and solidity

Classified in `VisibleSolidColliderTests.cs:131`: solid with colliders are `Visual/BumperHead` and `Visual/Visual_Cap`; hollow (torus collider `col_ring2`) is `Visual/Visual_Ring2`; decorative are `Visual/Visual_Base` (enclosed by the head) and `Visual/ImpactRing` (scaled by the impact binding). No visible-solid defect is listed for this package in `spec-engine-art-solidity.md` or `spec-elements-visible-solid-remainder.md`. Colours: head `#de7059`, base `#273744`, cap `#f7cb52`, rings `#fff0c2` ([DESIGN colour system](../../../DESIGN.md#colour-system)). It is told from a Ball by its capped, ringed dome on a fixed base.

## 3. Engine capabilities

Map families: ContactImpulse, EnvironmentState, FiniteLedger, FiniteWorkActuation, GeometryQuery, JointConstraint, RigidBodyDynamics, StateTransaction ([map row](../general-engine-element-map.md); navigation only, decision D14). The harvest's "Missing" list is superseded: every capability is present.

| Capability | Status | Evidence |
| --- | --- | --- |
| Contact work paid from a finite store, radial, bounded by target speed | present | `ContactWorkDescriptor` (`CuriousContraptions.Abstractions/MechanicalContractValues.cs:217`); `ContactWorkInstanceTests.StrengthEightLaunchesTheBallPaidAtTheTargetSpeedAndTheLedgerCloses` (`ContactWorkInstanceTests.cs:158`) |
| Qualification threshold and per-target cooldown | present | `WorkshopPhysicsLawSensorWorkTests.RestingSeparatingBelowThresholdAndGrazingGapContactsRecordNoOccurrence` (`WorkshopPhysicsLawSensorWorkTests.cs:231`); `CooldownEligibilityIsPerBodyAndOpensAtTheExactSeventyTwoStepBoundary` (`:253`) |
| Supply charging an energy store within the source's power bound | present | `SupplyEnergyStoreTarget` (`CuriousContraptions.Abstractions/SupplyContractValues.cs:51`); `ContactWorkInstanceTests.ADirectlyWiredBatteryRechargesTheStoreAndAnUnwiredOneDoesNot` (`:258`), `SuppliedContactStoreRetainsSourceLimitedChargingAboveFiftyWatts` (`:276`) |
| Torus and multi-collider static body | present | `VisibleSolidColliderTests.ABallDroppedOntoTheBumpersUpperRingStopsAtItsDrawnTube` (`VisibleSolidColliderTests.cs:826`) |
| Event-driven impact presentation | present | `ContactWorkInstanceTests.ExposedCapContactsUseTheFiniteStoreAndOnlyPaidWorkEmitsAPulse` (`:215`) |

Contact work works today because this owner is static: a contact-work row needs a static owner and is found by scanning each contact. ENGINE-CONTACT-OBSERVATIONS (owner decision 3a) finds rows by compiled handle on either side of a pair, and re-qualifies this part's rows when it lands.

Nothing here needs element-specific engine code.

## 4. Sources and legacy

- Requirement: "Bounded supplied or explicitly charged contact actuator repels touching cargo"; outcome "No available energy or missed contact means no powered kick" ([element-195](../requirements.md#element-195)). CAT-015 row: strength, per-body cooldown, missed/grazing/resting controls, expanding gold rings ([current-cat-015](../requirements.md#current-cat-015)).

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | Harvest-era worker set velocity to target speed on contact with zero debit | `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L1155-L1222` | do not carry forward | Replaced by the finite work law (Story 6.2). |
| 2 | Accepted: the round bumper launches away from the contact point on every axis | `CuriousContraptions.tests/BumperTests.cs@a6c914e:L35-L72` | carry forward | Radial repulsion. |
| 3 | Accepted: a glancing contact turns motion into spin; a ball in another depth plane misses | `CuriousContraptions.tests/BumperTests.cs@a6c914e:L73-L114` | carry forward | Grazing and depth controls. |
| 4 | Accepted: cooldown is per ball; the pulse finishes after physics stops | `CuriousContraptions.tests/BumperTests.cs@a6c914e:L115-L181` | carry forward | Per-body cooldown. |
| 5 | Accepted: stationary or separating bodies do not trigger; replay identical | `CuriousContraptions.tests/BumperTests.cs@a6c914e:L221-L254` | carry forward | Resting control. |
| 6 | Levels `bumper_sidekick`, `bumper_depth`, `wall_and_bumper` | `content/puzzles.json@a6c914e:L2445-L2655`, `content/puzzles.json@a6c914e:L2656-L2866`, `content/puzzles.json@a6c914e:L3145-L3423` | carry forward | Lessons. |

**Files harvested:**
- `parts/BumperPart.cs`
- `parts/catalog/bumper.tres`
- `CuriousContraptions.tests/BumperTests.cs`
- `content/puzzles.json` (levels `bumper_sidekick`, `bumper_depth`, `wall_and_bumper`)
- The remaining bumper files (for example `engine/BumperWorkResource.cs` and `CuriousContraptions.tests/BumperOccurrenceTests.cs`) belong to [CAT-015](CAT-015-bumper.md) (Batch A). Searched with no hit for an electrically supplied bumper at the harvest baseline: `parts/`, `engine/` (including `engine/physics/` and `engine/bridge/`), `CuriousContraptions.tests/`, `tools/`, `reference/`, `diagnostics/`.

## 5. Acceptance outline

Follow [element-195](../requirements.md#element-195) and [current-cat-015](../requirements.md#current-cat-015). The Chrome recipe (open `bumper_sidekick`, place the bumper left of the falling ball with the gizmo, set strength, Run; with a battery, connect `Supply` → `PowerIn`) stays queued for the post-catalogue phase.

### Closure check

| Criterion | Test |
| --- | --- |
| Positive, charged: a touching ball is repelled radially at the target speed and the store is debited | `ContactWorkInstanceTests.cs:158`; `WorkshopBumperTests.FiniteBudgetCapsTheNormalBoostAndNeverPaysPastTarget` (`WorkshopBumperTests.cs:360`) |
| Positive, supplied: a wired Battery refills the store by real work; an unwired one does not | `ContactWorkInstanceTests.cs:258`, `:276`, `:300` |
| **Exact source outcome, no energy:** an empty or exhausted store gives an ordinary unpaid bounce | `ContactWorkInstanceTests.StrengthZeroIsAnEmptyStoreWhoseContactsAreUnpaid` (`ContactWorkInstanceTests.cs:192`); `ContactWorkInstanceTests.APartiallyAffordableBoostEmptiesTheStoreAndALaterContactIsAnOrdinaryUnpaidBounce` (`ContactWorkInstanceTests.cs:176`) |
| **Exact source outcome, missed contact:** no work and no pulse | `VisibleSolidColliderTests.ABallOutsideTheBumperSolidsFallsPastWithoutWorkOrPulse` (`VisibleSolidColliderTests.cs:806`); resting, separating and grazing (`WorkshopPhysicsLawSensorWorkTests.cs:231`); depth miss (`:217`) |
| Boundaries: strength 0 and 20; cooldown boundary; cap then ring share the cooldown | `ContactWorkInstanceTests.StrengthEndpointsHoldAnEmptyAndATwoHundredJouleStore` (`ContactWorkInstanceTests.cs:148`); `WorkshopPhysicsLawSensorWorkTests.CooldownEligibilityIsPerBodyAndOpensAtTheExactSeventyTwoStepBoundary` (`WorkshopPhysicsLawSensorWorkTests.cs:253`); `ContactWorkInstanceTests.ARealCapThenRingContactSharesTheCooldownEvenWithRechargedWorkAvailable` (`ContactWorkInstanceTests.cs:317`) |
| Run/Reset and Save/Load | `ContactWorkInstanceTests.RunResetAndSaveLoadAreExact` (`:129`); `WorkshopBumperTests.ConstructionRoundTripPreservesTheStrengthAndRefusesForgedStores` (`:457`) |
| Parameter refusal | `WorkshopBumperTests.StrengthBoundariesAndPopulationRejectAtomically` (`:500`) |
| Wrong-kind connection refusal | Generic port check; confirm at closure that a Signal wire into `PowerIn` of a Bumper is among the cases of `BundlePortsTests`. If not, add `ContactWorkInstanceTests.AnActivationWireIntoTheBumpersPowerInputIsRefused`, on the pattern of `MotorTests.cs:316` |
| Lessons with Wall and Receiver | `LaunchLessonSolveTests.EachLaunchRouteSolvesOrStaysUnsolvedAndResetRestoresTheBench` (`LaunchLessonSolveTests.cs:239`): routes SidekickPaid, SidekickUnpaid, DepthPaid, DepthMiss, WallPaid, WallMiss |

The closure reviewer also confirms that a saved `PowerIn` wire reloads and recharges (the save half of `:129` or `ElectricalSupplyTests.cs:39`); if neither covers a Bumper wire, one save/load case is added beside `:258`.

### Integration

- **Collaborators.** Battery, Wall, Receiver, Ball, Impact switch: all delivered.
- **Campaign.** Row 1 (bumper): first use 1–10, reuse 21–30, 41–50, 91–100, 136–150 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)); retained lessons [todo-157](../requirements.md#todo-157). In `content/puzzles.json` (numbers are authored positions before decision 24d, each paired with its plan level in [campaign coverage, section 5](readiness/campaign-coverage.md#5-removed-and-converted-levels-owner-decision-24d)): levels 10 `bumper_sidekick`, 11 `bumper_depth` and 13 `wall_and_bumper` are Playable; 12 `wall_return` was removed under owner decision 24d.
- **Coverage binding.** Catalogue key `bumper` (CAT-015). EL-195 has no package of its own and so no package source binding. No lesson-fixture story (S8) is needed.
- **Value checks.** The default 8 m/s kick is what the three delivered lessons are tuned to. Refilling the default 32 J store inside the 0.15 s cooldown needs 32 / 0.15 = 213 W, so with a source limited below about 213 W (the default Battery's 120 W included) rapid repeat hits weaken, which is the supply lesson; at 213 W and above they do not.

### Delivery

- **Slice.** One closure-review slice, "Launch parts" (EL-195). Order: the part check runs in that review; the CAT-015 row and the EL-195 ledger row are ticked only after Story 15.0's isolated-assistance control. Owned diff: ledger rows and at most the two confirming tests above. Review tier: data-only.
- **Prerequisites.** Blocked on the engine programme (owner decision 3a), which comes first; the closure review then re-runs its rows on the re-qualified package. The ledger ticks then wait for Story 15.0's isolated-assistance control.
- **Files.** `docs/planning/invest/named-elements.md` (the element-195 entry cites `bumper` and the tests above), this spec's status, `docs/planning/invest/delivery-batches.md` (EL-195 moves from 2c to 2a), the closure record, and `ContactWorkInstanceTests.cs` if a confirming test is added.
- **What would disqualify the plan.** A reviewer reading the row's "supplied" as "draws per kick with no reservoir": that is a different law for the same behaviour and would be a second path; it goes to the owner as a scope question rather than into the closure.

## 6. Open questions

None open.

1. Is the Supplied variant an EL-195 requirement, or a separate part? **Decided: neither; it is the delivered part** (standing delegation, decision D19, and the owner's 9 October 2026 decision that recharging takes real work from a connected source). One Bumper is charged at Run and refilled through `PowerIn`. No `supply` enum and no second catalogue entry (D8: a closed duplicate gets none).
2. Supplied energy per kick. **Resolved by the delivered law:** a kick is paid from the store by the contact-work rule; supply only refills the store, at the source's power limit.
3. What bounds the recharge. **Decided by the coordinator under the owner's standing delegation (22 November 2026): the source's limit.** The requirement's "bounded supplied" is met by the Battery's `power_limit`; the coil node's 50 W does not bound anything (audit H10). Reason: that is the delivered and tested behaviour, and a second bound on the consumer would be a second rule for one quantity.
4. Head squash. **Decided by the owner (owner decision 8, 22 November 2026):** the head's 0.32 s squash (scale 1 → 0.88 over a sphere collider that keeps its size) is an accepted cosmetic exception to visible = solid.

## Engine conformance (22 November 2026)

Checked against the owner addendum of 22 November 2026, with the package opened (`elements/Bumper/`) and the engine projects searched for the package id and name.

1. **Declaration data only.** A static body, a declared contact-work source with an energy store and cooldown, a supply consumer targeting the store, one port and parameters. No engine code names the package; the word appears in three comments (`engine/gpu/ContactWorkImpulse.cs:11`, `:26`; `CuriousContraptions.Abstractions/ContactWorkDeclarationValues.cs:56`), to be reworded element-free when those files are next touched.
2. **Generic laws.** The contact-work rule (a finite stored energy paid into a contact as an impulse, with a declared cooldown) and the supply rule that refills a store within the source's power bound. The kick is not "when a ball touches the bumper": it is that law with this declaration's store and target speed.
3. **Three layers.** Colliders: head sphere, upper-ring torus and cap cylinder on the static body; `Visual/BumperHead` and `Visual/Visual_Cap` are solid with colliders, `Visual/Visual_Ring2` is hollow with its torus, `Visual/Visual_Base` is enclosed and `Visual/ImpactRing` is decorative. Visuals: body, cap and ring meshes; none is a collider. Animation: the head squash and the ring's scale and colour are event bindings on committed paid impacts; decorative, nothing written back. The head squash is the accepted cosmetic exception of owner decision 8.
4. **Flow and typing.** One Electrical `PowerIn` input; f32 Real parameters. One declared value has no effect: the coil node's `MaxPower` of 50 W is metadata; the source's `power_limit` bounds recharge. It is recorded as a CAT-015 residual for the batch summary (either the general supply rule honours a consumer node's rated power for every package, or the declaration stops implying a bound); it is not an element rule.
5. **Flat state.** Bodies 1 (static); colliders 3 (sphere 1, torus 1, cylinder 1); joints 0; drives 0; contact-work sources 1; stores 1; electrical nodes 1; supply consumers 1; presentation bindings 5 (head scale; impact-ring scale and three colour channels).
6. **Performance.** A static body; the contact-work source acts only on its own contacts. Today those rows are found by scanning each contact; ENGINE-CONTACT-OBSERVATIONS finds them by handle.
7. **Tests are scenarios.** The delivered rows run the production simulation.

**Verdict: conforms (three engine comments to reword; one inert declared value recorded).**
