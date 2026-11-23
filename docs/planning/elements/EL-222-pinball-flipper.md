# EL-222 · Supplied pinball flipper — named-identity spec

**Current delivery (21 November 2026):** [EL-222 ledger entry](../invest/named-elements.md#element-222), ledgered 21 November 2026; no earlier spec and no legacy to harvest. Code is cited as `path:line` in checkout `spec-drafts` (published main `8d1d68aa`); abstraction files are cited by file name and live under `CuriousContraptions.Abstractions*/`. Values are f32; **proposed** values are starting values ([D1](../invest/delivery-batches.md#3-family-level-decisions)). Tests are native; browser qualification is pending. Repaired 23 November 2026 against the owner decisions of 22 November 2026 ([readiness/owner-decisions-2026-11-22.md](readiness/owner-decisions-2026-11-22.md)).

## 1. Identity and disposition

| Field | Value |
| --- | --- |
| ID / name / type | EL-222 · Supplied pinball flipper · Mechanical |
| Requirement anchor | [todo-410](../requirements.md#todo-410) (`sequence-task-375`) |
| Ledger entry / owner | [element-222](../invest/named-elements.md#element-222); slice `named-el-222`, no earlier S owner |
| Disposition | **To deliver as a single element.** Delivery batches 1c lists it after the joint story. Declarable from general capabilities once the engine programme (owner decision 3a) is delivered, with two capabilities untested (section 3); if either fails its check, `ENGINE-JOINT-KINDS` owns the gap. Distinct from the Bumper (radial, unaimed) and the Boxing glove (linear, one timed punch). |
| Planned package | id `flipper`, directory `elements/Flipper/` |
| Related | [CAT-042 Motor](CAT-042-motor.md) (supplied hinge drive), [CAT-034 Impact lever](CAT-034-impact_lever.md) (limited hinge), [EL-237 Boxing glove](EL-237-boxing-glove.md), [CAT-015 Bumper](CAT-015-bumper.md) |
| Status | Not started. Blocked on the engine programme (owner decision 3a), in particular `ENGINE-SUPPLY-DEMAND` (the supply paying the drive's work; today supply is binary), `ENGINE-CAPABILITY-INSTANCES` (the Or node's `FirstIn` terminal, finding E2; the run-control "one driven hinge" assumption, finding E11) and `ENGINE-ISLANDS-SLEEP` |

## 2. Declaration

Shape: the Impact lever's limited hinge (`elements/ImpactLever/ImpactLeverElementDeclaration.cs:91`) with the Motor's supplied drive (`elements/Motor/MotorElementDeclaration.cs:115`).

- **Interfaces.** `IHingeElement` (`MechanicalContracts.cs:20`), `ISupplyElement` (`ElectricalContracts.cs:11`), `ISceneGeometryElement` (`SceneGeometry.cs:10`).
- **Base (body 0, static).** Pivot block 0.4 × 0.4 × 0.5 m. Restitution 0, friction 0.3.
- **Paddle (body 1, dynamic).** Box 1.2 × 0.2 × 0.5 m pivoted 0.1 m from one end; mass 0.6 kg; centre of mass 0.5 m from the pivot; box inertia. Restitution 0.3, friction 0.4.
- **Hinge.** `RevoluteTravel.Limited`, 0 to 0.873 rad (50°). Stiffness fixed 6 N·m/rad and damping fixed 0.4 N·m·s/rad toward 0 (the return spring); friction torque 0; initial angle 0. Drive: target speed `strike_speed`, maximum torque fixed 24 N·m; drive power 24 × `strike_speed`: 120 W at the default, a default Battery's limit (`elements/Battery/BatteryElementDeclaration.cs:44`). Settings above 5 rad/s need a Battery whose `power_limit` is raised to 24 × `strike_speed` (288 W at 12 rad/s; the Battery admits up to 480 W). The Battery's limit binds the drive only once `ENGINE-SUPPLY-DEMAND` pays drive work from its source; drive supply is binary today (`_bmad-output/implementation-artifacts/deferred-work.md:211`).
- **Supply graph.** One Or node (`ElectricalContractValues.cs:25`) powered from `PowerIn` with `StrikeIn` on its first input; its output supplies a consumer with `SupplyDriveTarget` on the hinge (`SupplyContractValues.cs:52`). So the drive is supplied only while both supply and command are present. The supply pays the drive's work once `ENGINE-SUPPLY-DEMAND` is delivered (section 3). The first input is today's fixed `FirstIn` terminal; `ENGINE-CAPABILITY-INSTANCES` replaces the two-input node with an N-input boolean node (finding E2), and `StrikeIn` then takes that node's declared first input.

| Parameter | Type, unit | Range | Default | Editor | Reason |
| --- | --- | --- | --- | --- | --- |
| `strike_speed` | Real, rad/s | 2–12 | 5 | Reject, build-only | The fastest strike a default Battery pays for. Hand calculation, Basketball 0.8 m out: work to the 50° stop 24 × 0.873 = 20.9 J, less gravity (1 × 9.81 × 0.61 + 0.6 × 9.81 × 0.38 = 8.3 J) and the spring (½ × 6 × 0.873² = 2.3 J) leaves 10.4 J in 0.86 kg·m² (paddle 0.22, ball 0.64): 4.9 rad/s at the stop, so the ball leaves at 3.9 m/s and the tip moves at 5.4 m/s; the sweep takes about 0.36 s |

| Port | Domain | Direction, role | Position (m) | Meaning |
| --- | --- | --- | --- | --- |
| `PowerIn` | Electrical | Input, PowerIn | (−0.2, −0.2, 0.25) | Supply |
| `StrikeIn` | Electrical | Input, FirstIn | (0.2, −0.2, 0.25) | Strike while on; hold while held |

- **Presentation.** Paddle body binding; indicator from `CommittedActuatorDrive` (slate to gold, as the Motor lamp).
- **Palette.** Power, "Flipper"; order 310 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)).

### Art and solidity

| Node | Mesh | Class |
| --- | --- | --- |
| Pivot block | Box, cream `#fff8e9` (owner decision 4) | Solid, collider on body 0 |
| Paddle | Tapered box drawn inside the 1.2 × 0.2 × 0.5 m collider, coral `#d96b58` with a gold tip band flush to the face | Solid, box collider on body 1; band printed flat |
| Return spring | Coil drawn inside the pivot block | Decorative: enclosed |
| Indicator | Flush disc | Decorative: printed flat |

Icon: a tapered paddle with a pivot dot and a sweep arc. It differs from the Impact lever (centre pivot, balanced beam) by the end pivot and taper.

### Behaviour and numbers

1. `StrikeIn` on with supply: the drive sweeps the paddle to the upper stop and keeps pressing it there with at most 24 N·m (hold). A stalled drive does no work.
2. `StrikeIn` off, or supply lost: the drive is unsupplied and the spring (5.2 N·m at 50°) returns the paddle in about 0.4 s. Power loss during a hold therefore drops whatever the paddle was holding.
3. A blocked paddle pushes with at most 24 N·m and transfers no more energy than the drive work.
4. Tick order: electrical in Phase 1; the drive acts in the same tick's physics (as the Motor).

## 3. Engine capabilities

| Capability | Mark | Symbol | Test |
| --- | --- | --- | --- |
| Limited hinge with stops | present | `MechanicalContractValues.cs:149` | `ImpactLeverTests.AngularLimitsBoundary_StopsArrestTravelWithoutPenetration` |
| Supplied velocity drive with a torque bound | present | `MechanicalContractValues.cs:138` | `RevoluteDriveTests.ASuppliedDriveSpinsTheRotorUpAtItsTorqueBoundThenHoldsTheTargetSpeed` |
| Stalled drive does no work; overloaded drive adds none | present | same | `RevoluteDriveTests.AStalledDriveDoesNoWorkWhileItsShaftIsBlocked` |
| Supply lost: no torque, no kick | present | same | `RevoluteDriveTests.CuttingTheSupplyStopsTheDriveWithoutAKick` |
| A gate between supply and a drive consumer | present | `SupplyContractValues.cs:41` | `MotorTests.AMotorSuppliedThroughAnEitherGateRunsOnlyWhileTheGateIsOn`; `ComposableSupplyRoutingTests.LogicProvenanceFundsNoWorkWhilePowerSourceFundsWork` |
| A supplied drive on a **limited** hinge, pressing its stop | present, untested | the validator admits a drive on any travel (`CuriousContraptions.Bundle/BundleValidator.cs:822`) | none; every driven hinge delivered is continuous |
| A constant axial spring on a limited hinge | present, untested (package level) | solver `CuriousContraptions.Simulation/WorkshopPhysicsEngine.cs:681`; constant only, a parameter is refused (`BundleValidator.cs:749`, `:799`) | Law level only: `AirflowRuntimeTests.DeclaredElasticCoordinateFundsContinuousWorkWithoutAnInstantaneousDrive` |
| **A supplied drive's committed work debited from its source within the source's power limit** (the Battery sizing in section 2) | **missing** | drive supply is binary (`_bmad-output/implementation-artifacts/deferred-work.md:211`) | Owner: `ENGINE-SUPPLY-DEMAND` (sprint key `engine-supply-drive-demand`) |

Counts: 5 present, 2 untested, 0 narrower, 1 missing. If either untested row fails, the owner is `ENGINE-JOINT-KINDS` (owner decision 3a): a limited-hinge drive pressing its stop, the constant hinge spring, a spring zero equal to the rest angle, and strike-and-hold.

### Engine conformance (owner addendum, 22 November 2026)

- **Declaration data only; generic laws.** A static pivot block, one limited sprung hinge with a supplied velocity drive, one logic node and two ports: the Impact lever's hinge with the Motor's drive. Strike and hold are the drive law pressing the hinge limit within its declared torque; the return is the hinge spring law; "supply and command" is the supply network's gate rule; what the ball does is rigid contact. The missing capability is stated for any package: a supplied drive's committed work debited from its source within the source's power limit, owned by `ENGINE-SUPPLY-DEMAND`. No element code, callback or engine branch.
- **Three layers.** Colliders (physics): 2 boxes (pivot block, paddle) as hidden `col_` proxies; the tapered paddle art is drawn inside its box. Visuals: the art nodes, none a collider. Animation: one indicator binding from the committed actuator drive. What moves each art node: pivot block and the enclosed return-spring coil, nothing (static body 0); paddle and its printed tip band, body 1's pose; indicator, the driver binding only.
- **Flat state.** 2 bodies; 2 box colliders; 1 revolute joint; 1 drive; 0 sensors; 1 logic node; 1 consumer; 0 stores; 1 body-pose and 1 driver binding.
- **Performance.** The same primitive count as the Impact lever. The block is a static body. Whether a resting paddle may sleep is `ENGINE-ISLANDS-SLEEP`'s (owner decision 3a); island sleeping is not delivered and nothing here assumes it. The tip reaches 5.4 m/s at the default and about 13 m/s at the maximum; thin targets rely on the engine's speculative contacts ([numeric authority](../../gpu-f32-physics.md)), not on a check in this part.
- **One-way flow, typing, tests.** Physics → animation → renderer; nothing writes back. Closed sets are enums, ids typed, values f32. Every acceptance row is a scenario on the production simulation.

## 4. Sources and legacy

- **Requirement.** [todo-410](../requirements.md#todo-410): "Battery supplies bounded pivoted paddle; independent signal commands strike/hold, spring returns it. Distinct from radial bumper and linear boxing glove."
- **Campaign.** Chapter 5 (41–50), "pinball flipper"; coverage row "Pusher, boxing glove, flipper, …".
- **Legacy.** None.
- **Files harvested.** None.

## 5. Acceptance outline

Tolerances: angle ±0.05 rad (3°, a tenth of the visible sweep is 5°); speeds as floors and ratios; stall guard 10 s.

| Row | Given / When | Then | Control and boundary | Planned test |
| --- | --- | --- | --- | --- |
| F1 | Basketball resting on the paddle 0.8 m from the pivot; a default Battery; `StrikeIn` on | The paddle reaches 50° and the ball leaves at 3 m/s or more (estimate 3.9 m/s) | Control: no supply, or no command: the paddle stays within 0.05 rad of rest | `FlipperTests.ACommandWithSupplyStrikes` |
| F2 | `StrikeIn` held 3 s with a ball resting against the raised paddle | The paddle holds at the stop and the ball stays behind it | Control: release the command and the ball passes | `FlipperTests.AHeldCommandHoldsTheBall` |
| F3 | Supply cut during a hold | The paddle returns to rest within 2 s | — | `FlipperTests.PowerLossDuringHoldReleases` |
| F4 | A Wall across the sweep at 20° | The paddle stops at the wall, no penetration; drive work stops rising | Control: no wall | `FlipperTests.ABlockedPaddleStallsWithoutWork` |
| F5 | Ball struck 0.2 m from the pivot and 1.0 m from it | The tip hit leaves at least twice as fast as the hinge hit | — | `FlipperTests.TipHitsOutpaceHingeHits` |
| F6 | Ball rolling toward the paddle; command 0.3 s early, on time, 0.3 s late | Only the on-time strike sends the ball into the Funnel | — | `FlipperTests.TimingDecidesTheShot` |
| F7 | Light receiver stand-in (Ball detector) → Delay → Hold timer → `StrikeIn` | The delayed command strikes the waiting ball into a Funnel | Control: Delay unwired | `FlipperTests.ATimerChainFiresTheFlipper` |
| F8 | Run, Reset; Save, Load | Authored pose, parameter and wires restored | — | `FlipperTests.ResetAndReloadRestoreTheAuthoredFlipper` |
| F9 | Activation wire into `StrikeIn`; `strike_speed` 1.9, 12.1, NaN | Refused with a typed reason | Boundary: 2 and 12 admitted | `FlipperTests.WrongKindWiresAndOutOfRangeValuesAreRefused` |

The requirement's "beam receiver" collaborator is optical and undelivered; F7 uses a Ball detector in this slice and is repeated with a light receiver in that part's slice. There the receiver couples only through observation → signal node → signal-state contact, under one receiver law for all light (owner decision 9, 22 November 2026); see [EL-176 Red laser emitter](EL-176-red-laser-emitter.md) section 10.

**Integration.** Battery, Hold timer, Delay, Funnel, Ball detector (delivered); optical receivers (after `ENGINE-OPTICAL`). A level that places it also needs S8.

**Coverage binding (planned, not yet live).** Published main binds a named-identity package through a final section of its spec that `tools/Coverage/NamedPackageSources.cs` reads (pattern: EL-239 Baseball); that section is not written here, because the reader parses every `EL-*.md` that carries its heading. It is written in the syntax current on main when this identity's slice starts. Planned values: Package `flipper`; Named identity EL-222; Acceptance todo-410 and sequence-task-375; owner slice `named-el-222`; Mode records `Configuration=Adjustable`. A slice-owned package is admitted only once main's reader admits one (the coverage follow-on story S7, which owner decision 15 also has widen the reader; EL-223 is paused, owner decision 24g).

**Delivery.** Blocked on the engine programme (owner decision 3a), in particular `ENGINE-SUPPLY-DEMAND`, `ENGINE-CAPABILITY-INSTANCES` and `ENGINE-ISLANDS-SLEEP`. Other prerequisites: the coverage reader admitting a slice-owned package (S7; EL-223 is paused, owner decision 24g); S3 if published (S2 is published at `25df3cd5`; no test-project list edit). Files: `elements/Flipper/` (project, declaration and `.uid`, `flipper.tscn`, icon and import), `CuriousContraptions.tests/FlipperTests.cs`, and the usual hand-edited lists until S3 (`CuriousContraptions.slnx`, `CSharpStaticGateTests.cs:703`, `PackageRuntimeParityTests.cs:596` and `:653`, one `VisibleSolidColliderTests.cs:110` row). Review tier: engine. **Disqualified if** a drive pressing a limit stop chatters or injects energy, or the hinge spring's zero is not the rest angle (both then wait for `ENGINE-JOINT-KINDS`); the revolute drive's target speed must be a run control (the ledger says today's drive "takes only a run-control target speed"; here it is build-only; finding E11, `ENGINE-CAPABILITY-INSTANCES`); or a drive granted less power than it asks for does anything other than move more slowly (not verified; `ENGINE-SUPPLY-DEMAND`).

## 6. Open questions

1. *Command kind: pulse or level?* Resolved under the standing delegation: an electrical level, because the requirement asks for strike and hold and for "sustained commands"; a pulse cannot hold.
2. *Is a position drive needed?* Resolved: no. A velocity drive against a stop gives strike and hold with existing rules; a position drive would be a second rule for the same behaviour.
3. *A declared spring rest position?* Coordinator decision 25, 22 November 2026, was checked and does not apply: the paddle is a striker returned to rest, not a blade, hatch or gate that must close when unpowered. Whether its return spring's zero is the rest angle is the untested row in section 3.

Needs the owner: nothing.
