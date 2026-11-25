# EL-220 · Docking lift — named-identity spec

**Current delivery (21 November 2026):** [EL-220 ledger entry](../invest/named-elements.md#element-220), ledgered 21 November 2026; no earlier spec and no legacy to harvest. Code is cited as `path:line` in checkout `spec-drafts` (published main `8d1d68aa`); abstraction files are cited by file name and live under `CuriousContraptions.Abstractions*/`. Values are f32; **proposed** values are starting values ([D1](../invest/delivery-batches.md#3-family-level-decisions)). Tests are native; browser qualification is pending. Propagated 23 November 2026 to the [element-spec decisions of 22 November 2026](readiness/owner-decisions-2026-11-22.md).

## 1. Identity and disposition

| Field | Value |
| --- | --- |
| ID / name / type | EL-220 · Docking lift · Mechanical |
| Requirement anchor | [todo-429](../requirements.md#todo-429) (`sequence-task-372`) |
| Ledger entry / owner | [element-220](../invest/named-elements.md#element-220); slice `named-el-220`, no earlier S owner |
| Disposition | **To deliver as a single element**, declarable from general capabilities once the engine programme (owner decision 3a) is delivered. Run "supplied sliders" in delivery batches 1b. Not a duplicate of the Linear pusher: it carries cargo on a lipped tray over a longer, parameter-bound travel and has its own art. |
| Planned package | id `docking_lift`, directory `elements/DockingLift/` |
| Related | [CAT-039 Linear pusher](CAT-039-linear_pusher.md) (shape), [EL-062 Docking ferry](EL-062-docking-ferry.md) (the horizontal sibling), [EL-221 Parallel gripper](EL-221-parallel-gripper.md) |
| Status | Not started. Blocked on the engine programme (owner decision 3a), in particular `ENGINE-SUPPLY-DEMAND` (sprint key `engine-supply-drive-demand`; the supplied lift's draw on its Battery and the Battery's power limit) and `ENGINE-ISLANDS-SLEEP` (a docked tray and its cargo at rest); then the coverage reader admitting a slice-owned package (S7; EL-223 is paused, owner decision 24g). It declares no sensor or contact node of its own (its endpoint contacts are coordinate-controlled supply contacts), so `ENGINE-SENSOR-VOLUMES` is not among its particular stories. |

## 2. Declaration

Follows `elements/LinearPusher/LinearPusherElementDeclaration.cs:8`, mounted vertically.

- **Interfaces.** `ISliderElement` (`MechanicalContracts.cs:15`), `ISupplyElement` (`ElectricalContracts.cs:11`), `ISceneGeometryElement` (`SceneGeometry.cs:10`), `IParameterizedGeometryElement` (`SceneGeometry.cs:29`) for the mast height.
- **Static frame (body 0).** Base 1.4 × 0.2 × 1.4 m; one mast 0.2 × (travel + 0.4) × 0.2 m at the back, its height bound to `travel`. Restitution 0, friction 0.3, bounce threshold 0.1 m/s.
- **Tray (body 1, dynamic).** Floor 1.2 × 0.1 × 1.2 m; lips 0.12 m high on the back and both sides; a 0.03 m lip at the front; a carriage block 0.3 × 0.3 × 0.2 m joining the tray to the mast. Mass 1 kg, box inertia, friction 0.4.
- **Slider.** Along +Y, limits 0 to `travel`, no spring. Drive `Directional`, `SelfLock` (`MechanicalContractValues.cs:84`–`:85`); power bound force × speed: 120 W at the defaults, a default Battery's limit (`elements/Battery/BatteryElementDeclaration.cs:44`); higher settings need a Battery whose `power_limit` is raised to force × speed (400 W at both maxima; the Battery admits up to 480 W). SelfLock is the holding brake. The Battery's limit binds the drive, and the drive's work is debited from the Battery, only once `ENGINE-SUPPLY-DEMAND` is delivered; drive supply is binary today (`_bmad-output/implementation-artifacts/deferred-work.md:211`).

| Parameter | Type, unit | Range | Default | Editor | Reason |
| --- | --- | --- | --- | --- | --- |
| `travel` | Real, m | 0.5–6 | 3 | Reject, build-only | Reaches from the bench to an upper pipe run |
| `speed` | Real, m/s | 0.25–2 | 1 | Reject | The trip takes 3 s at the defaults |
| `acceleration` | Real, m/s² | 1–6 | 4 | Reject | Kept below 9.81 so cargo never leaves a descending tray |
| `force` | Real, N | 20–200 | 120 | Reject | Tray plus Cannonball weigh 69 N; 120 N lifts about 11 kg of cargo |

| Port | Domain | Direction, role | Position (m) | Glyph |
| --- | --- | --- | --- | --- |
| `PowerIn` | Electrical | Input, PowerIn | (−0.6, −0.05, 0.7) | Power |
| `UpIn` | Electrical | Input, FirstIn | (−0.2, −0.05, 0.7) | Extend |
| `DownIn` | Electrical | Input, SecondIn | (0.2, −0.05, 0.7) | Retract |
| `LowerDockOut` | Electrical | Output, Supply | (0.6, −0.05, 0.7) | Retracted |
| `UpperDockOut` | Electrical | Output, Supply | (0.6, 0.1, 0.7) | Extended |

The supply graph is the pusher's (`LinearPusherElementDeclaration.cs:52`–`:65`): one distribution node, a motor consumer on the slider, two command consumers and two `PrismaticContactControl` endpoint contacts. No signal node, no store.

- **Presentation.** Tray body binding; indicator from `CommittedPrismaticPhase` (held slate, moving cyan, blocked gold); mast scale from `travel`.
- **Palette.** Motion, "Docking lift"; order 138 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)).

### Art and solidity

| Node | Mesh | Class | Moved by |
| --- | --- | --- | --- |
| Base and mast | Boxes, cream `#fff8e9` (owner decision 4); mast with navy tick marks | Solid, colliders on body 0; mast collider bound to `travel` | nothing (mast height set in Build by the `travel` parameter binding) |
| Tray floor and four lips | Boxes, sky blue `#7ebcd4`, gold `#f7cb52` front lip | Solid, colliders on body 1 | body pose |
| Carriage block joining tray to mast | Box 0.3 × 0.3 × 0.2 m | Solid, collider on body 1 | body pose |
| Dock lamps and indicator | Flush discs | Decorative: printed flat | presentation binding (colour) |

Icon: a tray on a vertical mast with up and down arrows. It differs from the Linear pusher by the open tray and vertical mast.

### Behaviour and numbers

- `UpIn` drives toward `travel`, `DownIn` toward 0. Both, neither, or lost supply lock the tray where it is (the pusher's contract). Cargo is carried by contact only; it is never attached or teleported.
- Blocked travel stops the tray and reports Blocked; force is bounded by `force`, so an overloaded tray does not rise.
- Dock outputs close only at the endpoints, on the electrical tick after the tray arrives (as the pusher's endpoint outputs do).
- The loading interlock is wiring, not a hidden rule: "ball waiting AND lower dock opens the inlet" is a Ball detector, a Both gate and `LowerDockOut`; "upper dock opens the exit" is `UpperDockOut` into a Powered gate. All four parts are delivered.
- Whether a docked tray, or cargo resting on it, may sleep is `ENGINE-ISLANDS-SLEEP`'s (owner decision 3a); island sleeping is not delivered and nothing here assumes it.

## 3. Engine capabilities

| Capability | Mark | Symbol | Test |
| --- | --- | --- | --- |
| Vertical supplied slider lifting cargo within its force | present | `MechanicalContractValues.cs:89` | `LinearPusherTests.VerticalPackageLiftsCargoOnlyWithSufficientDeclaredForce` |
| Self-lock on no command or lost supply | present | `MechanicalContractValues.cs:85` | `LinearPusherTests.LosingPowerLocksTheIntermediatePositionAndRetractResumesFromIt`; `DirectionalPrismaticTests.InactiveCommandsCaptureAndHoldWithoutMotorWork` |
| Endpoint outputs | present | `SignalContactControl.cs:13` | `LinearPusherTests.BothEndpointConsumersFollowDepartureReturnAndSupplyRemoval` |
| Blocked phase on obstruction | present | `MechanicalContractValues.cs:87` | `LinearPusherTests.ObstructionStopsTheHeadAndReportsBlockedWhileAnAdjacentWallDoesNot` |
| Command slew within declared acceleration | present | `MechanicalContractValues.cs:92` | `DirectionalPrismaticTests.StartupIsNotBlockedAndCommandSlewDoesNotExceedDeclaredAcceleration` |
| Stroke bound to a parameter | present | `LinearPusherElementDeclaration.cs:31` | `LinearPusherTests.ParameterEndpointsCompileAndProduceUsefulMotion` |
| Collider dimension bound to a parameter | present | `SceneGeometry.cs:29`; `elements/Pipe/PipeElementDeclaration.cs:46` | `HollowElementTests.ABallRollsThroughAStraightPipeOfEveryLengthOnItsWallWithoutCatching` |
| A compound moving body (tray and lips) on a slider | present | `MechanicalContractValues.cs:104` | `LinearPusherTests.CargoTransferResetRestoresEveryAuthoredBodyAndSemanticReplay` |
| **A supplied drive's committed work debited from its source, its draw bounded by the Battery's `power_limit`** | **missing** | drive supply is binary (`_bmad-output/implementation-artifacts/deferred-work.md:211`) | Owner: `ENGINE-SUPPLY-DEMAND` (sprint key `engine-supply-drive-demand`) |

Counts: 8 present, 0 untested, 0 narrower, 1 missing. No acceptance row asserts the debit; the declared power figures in section 2 hold once `ENGINE-SUPPLY-DEMAND` is delivered.

### Engine conformance (owner addendum, 22 November 2026)

- **Declaration data only; generic laws.** A static base and mast, one slider with a supplied directional drive, two endpoint contacts and five ports: the Linear pusher's declaration turned upright. Lifting is the drive law within its declared speed, acceleration, force and power; holding is the drive's self-lock response; docking outputs are the endpoint-contact rule; cargo stays on by contact and gravity. The missing capability is stated for any package: a supplied drive's committed work debited from its source within the source's power bound, owned by `ENGINE-SUPPLY-DEMAND`. No element code, callback or engine branch.
- **Three layers.** Colliders (physics): 8 boxes (base, mast; tray floor, 4 lips, carriage block) as hidden `col_` proxies. Visuals: the art nodes, none a collider. Animation: one indicator colour binding from the committed prismatic phase; the mast's drawn height follows `travel` as its collider does. Per art node: base and mast are moved by nothing; tray floor, lips and carriage block by body 1's pose; dock lamps and indicator by presentation bindings (colour only).
- **Flat state.** 2 bodies; 8 box colliders (0 of any other shape); 1 prismatic joint; 1 drive; 0 sensors; 1 distribution node; 3 consumers; 2 contacts; 0 stores; 1 body-pose, 1 driver and 1 parameter binding.
- **Performance.** Four colliders more than the Linear pusher's four: the tray lips that keep cargo aboard. The frame is static. Whether a docked tray may sleep is `ENGINE-ISLANDS-SLEEP`'s (owner decision 3a); island sleeping is not delivered and nothing here assumes it. Tray speed is at most 2 m/s.
- **One-way flow, typing, tests.** Physics → animation → renderer; nothing writes back. Closed sets are enums, ids typed, values f32. Every acceptance row is a scenario on the production simulation.

## 4. Sources and legacy

- **Requirement.** [todo-429](../requirements.md#todo-429): "Guided supplied tray, upper/lower dock outputs and loading/unloading interlocks; explicit holding brake. Cargo stays physically supported through travel."
- **Campaign.** Chapter 5 (levels 41–50), named "docking lift/ferry"; coverage row "… gripper, lift, ferry …", reuse 61–100, 111–120, 136–150.
- **Legacy.** None.
- **Files harvested.** None.

## 5. Acceptance outline

Tolerances: tray position ±0.02 m (a tenth of the lip height is the clearance that matters); cargo "supported" means its gap to the tray floor stays under 0.02 m; stall guard 30 s.

| Row | Given / When | Then | Control and boundary | Planned test |
| --- | --- | --- | --- | --- |
| L1 | Basketball on the tray, `UpIn` on | Tray and ball reach `travel`; `UpperDockOut` closes and `LowerDockOut` opens | Control: no supply, nothing moves and neither output closes | `DockingLiftTests.ItCarriesABallToTheUpperDock` |
| L2 | Supply cut mid-travel | The tray holds within 0.02 m for 5 s with the ball on it; restoring supply resumes | Control: no cut | `DockingLiftTests.PowerLossHoldsTheLoadedTray` |
| L3 | A Wall 1 m above the tray | The tray stops under it, phase Blocked, no penetration | Control: Wall beside the path, the tray passes | `DockingLiftTests.ABlockedTrayStopsAndReportsIt` |
| L4 | Descending at `acceleration` 6 | The ball stays supported | Boundary: 1 and 6 admitted; 6.1 refused | `DockingLiftTests.CargoStaysOnADescendingTray` |
| L5 | `force` 20 with a Bowling ball (49 N with the tray) | The tray does not rise | Boundary: `force` 60 lifts it | `DockingLiftTests.ItLiftsOnlyWithinItsForce` |
| L6 | Ball resting half over the front lip; ball pushed off between docks | It falls under gravity; it is neither held nor moved to a dock | Control: centred ball stays | `DockingLiftTests.EdgeCargoFallsPhysically` |
| L7 | Ball detector and `LowerDockOut` through a Both gate to an inlet gate; `UpperDockOut` to an exit gate | The inlet opens only with a ball waiting and the tray at the lower dock; the exit only at the upper dock | Control: tray mid-travel, both stay shut | `DockingLiftTests.DockOutputsInterlockLoadingAndUnloading` |
| L8 | Run, Reset; Save, Load | Authored pose, four parameters and connections restored | — | `DockingLiftTests.ResetAndReloadRestoreTheAuthoredLift` |
| L9 | Activation wire into `UpIn`; `travel` 0.4, 6.1, NaN | Refused with a typed reason | Boundary: 0.5 and 6 admitted | `DockingLiftTests.WrongKindWiresAndOutOfRangeValuesAreRefused` |

**Integration.** Ball detector, Both gate, Powered gate, Pipe, Battery (delivered); EL-221 gripper and EL-219 rack (later). The Ball detector's sensor in L7 is that part's own and is re-qualified by `ENGINE-SENSOR-VOLUMES` in the engine programme. A level that places it also needs lesson fixtures bound to a named package (S8).

**Coverage binding (planned, not yet live).** Published main binds a named-identity package through a final section of its spec that `tools/Coverage/NamedPackageSources.cs` reads (pattern: EL-239 Baseball); that section is not written here, because the reader parses every `EL-*.md` that carries its heading. It is written in the syntax current on main when this identity's slice starts. Planned values: Package `docking_lift`; Named identity EL-220; Acceptance todo-429 and sequence-task-372; owner slice `named-el-220`; Mode records `Configuration=Adjustable`. A slice-owned package is admitted only once main's reader admits one (the coverage follow-on story S7, which owner decision 15 also has widen the reader; EL-223 is paused, owner decision 24g).

**Delivery.** One element slice. Prerequisites: blocked on the engine programme (owner decision 3a), in particular `ENGINE-SUPPLY-DEMAND` and `ENGINE-ISLANDS-SLEEP`; then the coverage reader admitting a slice-owned package (S7; EL-223 is paused, owner decision 24g); S3 if published (S2 is published at `25df3cd5`; no test-project list edit). Files: `elements/DockingLift/` (project, declaration and `.uid`, `docking_lift.tscn`, icon and import) and `CuriousContraptions.tests/DockingLiftTests.cs`; hand-edited lists until S3 as for every package (`CuriousContraptions.slnx`, `CSharpStaticGateTests.cs:703`, `PackageRuntimeParityTests.cs:596` and `:653`, one `VisibleSolidColliderTests.cs:128` row). Review tier: data-only if the declaration is constant data and S3 is published; otherwise engine. **Disqualified if** the probe finds the mast collider cannot follow `travel` (then that binding is `ENGINE-PARAMETER-BOUND-FRAMES`'), or a tray with lips is not admitted as one moving body.

## 6. Open questions

1. *Is the holding brake a separate part?* Resolved: it is the slider's `SelfLock`, the same rule the pusher uses; no second brake rule.
2. *A cargo-present output?* Resolved by D19: not added. The requirement's interlock is met with a Ball detector at the dock. Today a contact-load sensor must sit on a static frame (`SignalBundleValidation.cs:190`); a tray-borne sensor becomes declarable with `ENGINE-SENSOR-VOLUMES` (sensors relative to a moving frame) and is still not needed.
3. *Tray size parameter.* Resolved by D13: fixed.

Needs the owner: nothing.
