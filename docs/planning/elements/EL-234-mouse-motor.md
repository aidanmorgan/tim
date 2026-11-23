# EL-234 · Mouse motor — named-identity spec

**Current delivery (21 November 2026):** [EL-234 ledger entry](../invest/named-elements.md#element-234), ledgered 21 November 2026; no earlier spec and no legacy to harvest. Code is cited as `path:line` in checkout `spec-drafts` (published main `8d1d68aa`); abstraction files are cited by file name and live under `CuriousContraptions.Abstractions*/`. Values are f32; **proposed** values are starting values ([D1](../invest/delivery-batches.md#3-family-level-decisions)). Tests are native; browser qualification is pending. Repaired 23 November 2026 against the owner decisions of 22 November 2026 ([readiness/owner-decisions-2026-11-22.md](readiness/owner-decisions-2026-11-22.md)).

## 1. Identity and disposition

| Field | Value |
| --- | --- |
| ID / name / type | EL-234 · Mouse motor · Mechanical |
| Requirement anchor | [todo-188](../requirements.md#todo-188) (`sequence-task-367`) |
| Ledger entry / owner | [element-234](../invest/named-elements.md#element-234); owner **S349** (run duration and retrigger policy); slice `named-el-234` |
| Disposition | **To deliver as a single element, after the engine programme (owner decision 3a).** Run "timed and stored drives" in delivery batches 1b. Not a duplicate of the Motor: it has no electrical port, starts on an impact and runs for a set time. |
| Planned package | id `mouse_motor`, directory `elements/MouseMotor/` |
| Related | [CAT-042 Motor](CAT-042-motor.md), [CAT-063 Switch](CAT-063-switch.md) (impact sensing), [CAT-033 Hold timer](CAT-033-hold_timer.md), [EL-085 Rope-driven character wheel](EL-085-rope-driven-character-wheel.md) |
| Status | Not started. Blocked on the engine programme (owner decision 3a), in particular `ENGINE-SUPPLY-DEMAND` (the drive's work debited from the `stamina` store; section 3), `ENGINE-CONTACT-OBSERVATIONS` (the strike pad's contact trigger) and `ENGINE-ISLANDS-SLEEP` (a stopped wheel rests) |

## 2. Declaration

Shape: the Motor's rotor and drive (`elements/Motor/MotorElementDeclaration.cs:88`), enabled by the Impact switch's contact node (`elements/ImpactSwitch/ImpactSwitchElementDeclaration.cs:79`) through a Hold-timer window (`elements/HoldTimer/HoldTimerElementDeclaration.cs:33`), and paid from the Battery's source store (`elements/Battery/BatteryElementDeclaration.cs:71`) declared inside the package.

- **Interfaces.** `IHingeElement` (`MechanicalContracts.cs:20`), `ISupplyElement` (`ElectricalContracts.cs:11`), `ISignalElement` (`SignalContracts.cs:8`), `ISceneGeometryElement` (`SceneGeometry.cs:10`).
- **Cage (body 0, static).** Box 1.0 × 0.8 × 0.5 m with a strike pad 0.6 × 0.08 × 0.5 m on top. Restitution 0, friction 0.3.
- **Wheel (body 1, dynamic).** Cylinder radius 0.3 m, width 0.13 m, mass 0.5 kg, solid-disc inertia (the Motor's rotor).
- **Hinge.** `RevoluteTravel.Continuous`, friction torque 0.05 N·m (spins down from 6 rad/s in about 2.7 s, as the Motor). Drive: target speed `speed`, maximum torque fixed 8 N·m.
- **Signal nodes.** A contact node on body 0 with threshold `trigger_threshold` (`SignalContractValues.cs:51`), wired by one internal edge to a timer `"run"` on `run_seconds`, `Rearm`, `BeforeNetworks`, no elapsed signal (`SignalContractValues.cs:49`).
- **Supply graph.** Source `"stamina"` (`SupplySourceDescriptor`, `SupplyContractValues.cs:39`) → contact closed while the timer counts → consumer with `SupplyDriveTarget` on the hinge. No electrical port.

| Parameter | Type, unit | Range | Default | Editor | Reason |
| --- | --- | --- | --- | --- | --- |
| `run_seconds` | Real, s | 0.5–12 | 3 | Reject, build-only | S349: authored run duration; seconds (D10) |
| `speed` | Real, rad/s | 0–12 | 6 | Reject, build-only | The Motor's default, so Mechanical links and conveyors behave alike |
| `trigger_threshold` | Real, m/s | 0–64 | 0.8 | Clamp | The Impact switch's parameter and default |
| `stamina` | Real, J | 20–2000 | 400 | Reject | Every source is a finite ledger; 400 J is about 8 s at full load |
| `power` | Real, W | 10–120 | 48 | Reject | 8 N·m × 6 rad/s |
| `initial_charge` | Real, fraction | 0–1 | 1 | Reject | Required by the source store |
| `enabled` | Choice | Disabled, Enabled | Enabled | Reject | Required by the source store |

| Port | Domain | Direction, role | Position (m) |
| --- | --- | --- | --- |
| `Drive` | Mechanical | Output, Drive; rim radius 0.3 m on the wheel | (0, 0, 0.35) |

- **Presentation.** Wheel body binding (the wheel visibly turns); a mouse figure whose run cycle follows `CommittedWheelAngle`; stamina bar from `CommittedCharge`.
- **Palette.** Power, "Mouse motor"; order 307 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)).

### Art and solidity

| Node | Mesh | Class |
| --- | --- | --- |
| Cage and strike pad | Boxes; cage sides drawn as bars printed on the box faces, cream `#fff8e9` (owner decision 4); pad coral `#d96b58` | Solid, colliders on body 0 |
| Wheel | Cylinder 0.3 × 0.13 m, sky blue `#7ebcd4`, proud of the cage front by 0.1 m | Solid, cylinder collider on body 1 |
| Mouse | Small figure inside the cage volume | Decorative: enclosed and moved by a binding |
| Stamina bar | Flush strip | Decorative: printed flat |

Icon: a wheel with a mouse silhouette and an impact star. It differs from the Motor by the cage and mouse and by having no lamp or power socket.

### Behaviour and numbers

1. A body striking the pad faster than `trigger_threshold` starts the timer. While it counts, the store supplies the drive and the wheel turns toward `speed` with at most 8 N·m.
2. When the window ends the drive is unsupplied and the wheel spins down under bearing friction.
3. **Retrigger policy (S349, decided here under D16):** an impact during a run is ignored; expiry rearms. This is the Hold timer's rule.
4. Work comes only from `stamina`; when it is spent the wheel no longer runs until Reset. A stalled wheel spends nothing (a stalled drive does no work: `MotorTests.AStalledMotorDoesNoWorkOnceItsShaftIsHeld`); a loaded, turning wheel spends torque × angle. Unloaded running costs about 0.3 W. This holds once `ENGINE-SUPPLY-DEMAND` debits a drive's committed work from its source; drive supply is binary today (`_bmad-output/implementation-artifacts/deferred-work.md:211`), so the store would never run down (section 3).
5. Tick order: the contact commits with the physics tick; the timer boundary `BeforeNetworks` closes the contact before the next supply solve.

## 3. Engine capabilities

| Capability | Mark | Symbol | Test |
| --- | --- | --- | --- |
| Supplied continuous hinge drive, spin-down unsupplied | present | `MechanicalContractValues.cs:138` | `MotorTests.CuttingTheSupplySpinsTheRotorDownUnderBearingFrictionWithNoFurtherWork` |
| Impact contact node on a static body | present | `SignalContractValues.cs:51` | `SwitchElectricalTests.PhysicalImpactDrivesBothDomainsAndExactSaveReset` |
| A physical sensor feeding a timer inside one element | present | `SignalContractValues.cs:16` | `ComposedPhysicalSignalTests.PhysicalSensorDelaysOnlyItsSelectedMemoryInOneElement` |
| Timer window, busy triggers ignored, rearm | present | `SignalContractValues.cs:9` | `HoldTimerTests.BusyInputsDoNotExtendAndExpiryRearmsWithoutAnyOutput` |
| Finite source store | present | `ElectricalContractValues.cs:31` | `ElectricalSupplyTests.UnfamiliarSourcesShareTheSameFiniteAllocationLaw` |
| Mechanical Drive port and Mechanical link | present | `PortDescriptor.cs:20` | `ReverseTransmissionTests.CatalogueMotorDrivesEveryCurrentPairing` |
| A source, a timer contact and a drive consumer in one package with no electrical port | present, untested | `SupplyContractValues.cs:39`–`:42` | Nearest: `ComposableSupplyRoutingTests.MultipleCapabilitiesPerOwnerWithIndependentControlAndRead`; the slice adds `MouseMotorTests.AnImpactRunsTheWheelForItsDuration` |
| **Drive work debiting a package-internal source store** | **missing** | a supplied drive does not debit its source: drive supply is binary (`_bmad-output/implementation-artifacts/deferred-work.md:211`); `SupplySourceDescriptor` (`SupplyContractValues.cs:39`) is debited only by contact-work bindings | Owner: `ENGINE-SUPPLY-DEMAND` (sprint key `engine-supply-drive-demand`); the slice adds `MouseMotorTests.StaminaIsFinite` |

Counts: 6 present, 1 untested, 0 narrower, 1 missing.

### Engine conformance (owner addendum, 22 November 2026)

- **Declaration data only; generic laws.** A static cage, one hinged wheel with a supplied velocity drive, one contact node wired to one timer, one source store, one contact and one rim port. The start is the contact-node rule; the run window is the timer rule closing a supply contact; turning is the drive law; `stamina` is the supply network's finite-store ledger; spin-down is hinge friction. Nothing says "when struck, spin": a sensor feeds a timer that closes a contact. The missing capability is stated for any package: a supplied drive's committed work debited from its source store, owned by `ENGINE-SUPPLY-DEMAND`. The contact node is re-qualified by `ENGINE-CONTACT-OBSERVATIONS` (contacts found by compiled index). No element code, callback or engine branch.
- **Three layers.** Colliders (physics): 2 boxes (cage, strike pad) and 1 cylinder (wheel) as hidden `col_` proxies; cage bars are printed on the box. Visuals: the art nodes, none a collider. Animation: the mouse's run cycle follows the committed wheel angle and the stamina bar the committed charge; neither writes back. What moves each art node: cage, printed bars and strike pad, nothing (static body 0); wheel, body 1's pose; mouse, a presentation binding from the committed wheel angle; stamina bar, a driver binding from the committed charge.
- **Flat state.** 2 bodies; 3 colliders (2 box, 1 cylinder); 1 revolute joint; 1 drive; 1 contact sensor; 2 signal nodes and 1 internal edge; 1 source store; 1 contact; 1 consumer; 1 body-pose and 2 driver bindings.
- **Performance.** One collider more than the Motor (the strike pad). The cage is a static body. Whether a stopped wheel may sleep is `ENGINE-ISLANDS-SLEEP`'s (owner decision 3a); island sleeping is not delivered and nothing here assumes it. The sensor is one collider's contacts, not a region scan.
- **One-way flow, typing, tests.** Physics → animation → renderer; nothing writes back. Closed sets are enums, ids typed, values f32. Every acceptance row is a scenario on the production simulation.

## 4. Sources and legacy

- **Requirement.** [todo-188](../requirements.md#todo-188): "Add a distinct mechanical source, not a disguised electrical switch. Animate the wheel; author run duration and retrigger policy."
- **Campaign.** Chapter 4 "Gears of an Idea" (31–40), "impact-started mouse-motor role"; [sequence-task-277](../requirements.md#sequence-task-277) lists it among drive sources.
- **Legacy.** None.
- **Files harvested.** None.

## 5. Acceptance outline

Tolerances: the run window is a timer contract, asserted to ±1 tick; wheel speed within 10 % of `speed` once reached; "stopped" is below 0.1 rad/s; stall guard 20 s.

| Row | Given / When | Then | Control and boundary | Planned test |
| --- | --- | --- | --- | --- |
| M1 | Bowling ball dropped on the pad from 0.5 m | The wheel reaches `speed` and the drive is supplied for `run_seconds`, then the wheel stops | Control: no impact, the wheel never turns. Boundary: an impact just under and just over the threshold | `MouseMotorTests.AnImpactRunsTheWheelForItsDuration` |
| M2 | A second impact during the run; a third after it | One run for the first two, a second for the third | Boundary: `run_seconds` 0.5 and 12 admitted | `MouseMotorTests.ImpactsDuringARunAreIgnored` |
| M3 | Mechanical link to a Conveyor carrying a Basketball | The ball is carried while the run lasts and stops after | Control: Mechanical link removed | `MouseMotorTests.ItDrivesAConveyorByMechanicalLink` |
| M4 | `stamina` 20 J, joined by a Mechanical link to a Conveyor carrying a Bowling ball (a Cannonball when delivered), struck repeatedly | Cumulative drive work never exceeds 20 J and, once it is spent, later impacts do not turn the wheel | Control: `stamina` 400 J runs again on the next impact | `MouseMotorTests.StaminaIsFinite` |
| M5 | A Battery wired toward it | No electrical socket exists; the connection is refused | — | `MouseMotorTests.ItHasNoElectricalSocket` |
| M6 | Reset mid-run; Save, Load | Wheel at rest, timer ready, store full; parameters and Mechanical link restored; no stale run | — | `MouseMotorTests.ResetAndReloadRestoreTheAuthoredMotor` |
| M7 | `run_seconds` 0.4, `speed` 12.1, NaN | Refused with a typed reason | — | `MouseMotorTests.OutOfRangeValuesAreRefused` |

**Integration.** Conveyor, Reverse transmission, Clutch, Wound spring, Fan shaft mode (delivered); EL-232 Gears and EL-233 Generator (later). A level that places it also needs S8.

**Coverage binding (planned, not yet live).** Published main binds a named-identity package through a final section of its spec that `tools/Coverage/NamedPackageSources.cs` reads (pattern: EL-239 Baseball); that section is not written here, because the reader parses every `EL-*.md` that carries its heading. It is written in the syntax current on main when this identity's slice starts. Planned values: Package `mouse_motor`; Named identity EL-234; Acceptance todo-188 and sequence-task-367; Decision S349; Mode records `Configuration=Adjustable`. Its S owner anchor S349 is resolvable on main (`docs/planning/invest/scope-corrections.md`), and binding still waits for this identity's slice.

**Delivery.** Blocked on the engine programme (owner decision 3a), in particular `ENGINE-SUPPLY-DEMAND`, `ENGINE-CONTACT-OBSERVATIONS` and `ENGINE-ISLANDS-SLEEP`. Other prerequisites: S3 if published (S2 is published at `25df3cd5`; no test-project list edit). Files: `elements/MouseMotor/` (project, declaration and `.uid`, `mouse_motor.tscn`, icon and import), `CuriousContraptions.tests/MouseMotorTests.cs`, and the usual hand-edited lists until S3. Review tier: engine (one untested capability). **Disqualified if** a package-internal source cannot fund its own drive consumer, or a run-control lane is forced on `speed` (the Motor's `speed` is a run control; here it is build-only; finding E11, owned by `ENGINE-CAPABILITY-INSTANCES` in the same programme).

## 6. Open questions

1. *S349 run duration and retrigger policy.* Resolved under D16: duration is the `run_seconds` parameter; impacts during a run are ignored; expiry rearms.
2. *Is an internal supply graph a "disguised electrical switch"?* Resolved: no. The player sees no socket, wire or lamp; the graph is how any paid drive is declared.
3. *Seven parameters.* Resolved by D11: all editable unless the level locks them; lessons lock the four store values.

Needs the owner: nothing.
