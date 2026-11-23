# EL-217 · Reload shuttle with chamber interlock — named-identity spec

**Current delivery (22 November 2026):** [EL-217 ledger entry](../invest/named-elements.md#element-217), ledgered 21 November 2026; no earlier spec and no legacy to harvest. Code is cited as `path:line` in checkout `spec-drafts` (published main `8d1d68aa`); abstraction files are cited by file name and live under `CuriousContraptions.Abstractions*/`. Values are f32; **proposed** values are starting values ([D1](../invest/delivery-batches.md#3-family-level-decisions)). Tests are native; browser qualification is pending. Repaired 22 November 2026 after the independent audit (`docs/planning/elements/readiness/audits/elements-new-identities.md`, F1, F6, F18–F20). Propagated 23 November 2026 to the [element-spec decisions of 22 November 2026](readiness/owner-decisions-2026-11-22.md).

## 1. Identity and disposition

| Field | Value |
| --- | --- |
| ID / name / type | EL-217 · Reload shuttle with chamber interlock · Mechanical |
| Requirement anchor | [todo-398](../requirements.md#todo-398) (`sequence-task-353`) |
| Ledger entry / owner | [element-217](../invest/named-elements.md#element-217); slice `named-el-217`, no earlier S owner |
| Disposition | **To deliver as a single element**, declarable from general capabilities once the engine programme (owner decision 3a) is delivered. Run "supplied sliders" in [delivery batches 1b](../invest/delivery-batches.md#1b-runs-each-its-own-slice-planning-reference-only). Not a duplicate of the Linear pusher: it has a pocket, a shutter, an interlock and its own ports. Not a family with EL-241: ports and control law differ. |
| Planned package | id `reload_shuttle`, directory `elements/ReloadShuttle/` |
| Related | [CAT-039 Linear pusher](CAT-039-linear_pusher.md) (declaration shape), [CAT-016 Cannon](CAT-016-cannon.md), [EL-241 Ball hopper](EL-241-ball-hopper.md) |
| Status | Not started. Blocked on the engine programme (owner decision 3a), in particular `ENGINE-SENSOR-VOLUMES` (the outlet pad's load region), `ENGINE-SUPPLY-DEMAND` (sprint key `engine-supply-drive-demand`; the supplied slider's draw on its Battery), `ENGINE-CAPABILITY-INSTANCES` (the two-input logic terminals, finding E2) and `ENGINE-ISLANDS-SLEEP` (a queue resting in the collar); then the coverage reader admitting a slice-owned package (S7; EL-223 is paused, owner decision 24g). |

## 2. Declaration

Follows `elements/LinearPusher/LinearPusherElementDeclaration.cs:8`: a static frame carrying one supplied slider.

- **Interfaces.** `ISliderElement` (`MechanicalContracts.cs:15`), `ISupplyElement` (`ElectricalContracts.cs:11`), `ISignalElement` (`SignalContracts.cs:8`), `ISceneGeometryElement` (`SceneGeometry.cs:10`).
- **Static frame (body 0).** Deck 2.6 × 0.12 × 0.9 m with a 0.8 m square drop hole at its +X end; an inlet collar (square ring, inner 0.8 m, 0.3 m tall, lower edge 0.8 m above the deck) above the home position; under the hole a throat 0.9 m wide whose floor is a chute sloping 15° down to a **level outlet pad** 0.9 × 0.05 × 0.9 m at the outlet. The pad is the load region: an unrotated box, as the validator requires today (`CuriousContraptions.Bundle/SignalBundleValidation.cs:189`–`:190`). A ball that cannot leave rests on the pad. Material: restitution 0, friction 0.3, bounce threshold 0.1 m/s.
- **Pocket (body 1, dynamic, on the slider).** Two cross walls 0.1 × 0.8 × 0.8 m, 0.8 m apart, two side rails 0.8 m tall (inner footprint 0.8 × 0.8 m), and a shutter plate 1.2 × 0.06 × 0.8 m trailing behind the pocket at the wall tops, just under the collar. Mass 0.8 kg, box inertia. The pocket has no floor: the ball rests on the deck and the walls push it.
- **Slider.** Along +X, limits 0 to 1.2 m, no spring. Drive: `PrismaticTargetSelection.Directional`, `PrismaticInactiveResponse.SelfLock` (`MechanicalContractValues.cs:84`–`:85`), stopping acceleration 8 m/s², power bound force × speed: 40 W at the defaults, inside a default Battery's 120 W (`elements/Battery/BatteryElementDeclaration.cs:44`); 160 W with both at their maxima, which needs a Battery with `power_limit` set to 160 W or more. The Battery's limit binds the drive, and the drive's work is debited from the Battery, only once `ENGINE-SUPPLY-DEMAND` is delivered; drive supply is binary today (`_bmad-output/implementation-artifacts/deferred-work.md:211`).

| Parameter | Type, unit | Range | Default | Editor | Reason |
| --- | --- | --- | --- | --- | --- |
| `speed` | Real, m/s | 0.5–2 | 1 | Reject, build-only | A 1.2 s stroke reads as one step |
| `force` | Real, N | 10–80 | 40 | Reject, build-only | Sliding a 6 kg Cannonball needs at most 0.3 × 6 × 9.81 ≈ 18 N |
| `minimum_mass` | Real, kg | 0.1–1 | 0.1 | Reject, build-only | The pad sensor sees every supported ball; the lightest, the Soccer ball, is 0.45 kg |

| Port | Domain | Direction, role | Position (m) | Meaning |
| --- | --- | --- | --- | --- |
| `PowerIn` | Electrical | Input, PowerIn | (−1.2, −0.2, 0.45) | Supply |
| `AdvanceIn` | Electrical | Input, FirstIn | (−0.6, −0.2, 0.45) | Feed command (a level) |
| `ReturnedIn` | Electrical | Input, SecondIn | (0, −0.2, 0.45) | Launcher has returned |
| `HomeOut` | Electrical | Output, Supply | (−1.2, 0.3, 0.45) | Pocket at home |
| `DeliveredOut` | Electrical | Output, Supply | (1.2, 0.3, 0.45) | Pocket over the outlet |

- **Signal node.** One `SignalContactLoadDescriptor` (`SignalContractValues.cs:58`) on the level outlet pad of body 0, minimum mass from `minimum_mass`, local normal +Y. Its region is evaluated against collider shapes inside the tick once `ENGINE-SENSOR-VOLUMES` is delivered.
- **Supply graph.** `go` = `AdvanceIn` AND `ReturnedIn` AND NOT pad-loaded, built from two And nodes and one Nor node (`ElectricalContractValues.cs:25`) fed by a `SignalStateContactControl` contact on the load node (`SignalContactControl.cs:12`). `go` is the drive's positive command; NOT `go` (a second Nor) is the negative command. `HomeOut` and `DeliveredOut` are `PrismaticContactControl` endpoint contacts (`SignalContactControl.cs:13`). No store. The two-input nodes use today's fixed `FirstIn`/`SecondIn` terminals (`SupplyContractValues.cs:26`), which `ENGINE-CAPABILITY-INSTANCES` replaces with one N-input boolean node (finding E2); the slice declares the graph in the form current on main when it starts (one three-input And if that story has landed).
- **Presentation.** Pocket body binding; indicator colour from `PresentationDriver.CommittedPrismaticPhase` (held slate, moving cyan, blocked gold, as the pusher); outlet lamp from `CommittedContactLoadPhase`.
- **Palette.** Motion, "Reload shuttle"; order 135 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)).

### Art and solidity

| Node | Mesh | Class | Moved by |
| --- | --- | --- | --- |
| Deck, collar, throat walls, chute, outlet pad | Boxes as dimensioned, cream `#fff8e9` (owner decision 4) with navy `#1c2836` edge marks | Solid, one box collider each | nothing |
| Pocket walls, rails, shutter | Boxes, sky blue `#7ebcd4` | Solid, box colliders on body 1 | body pose |
| Indicator and outlet lamp | 0.05 m discs flush in the deck face | Decorative: printed flat | presentation binding (colour) |

Icon: a rail with an open square pocket and an arrow. It differs from the Linear pusher by the open pocket and the drop hole, not by colour.

### Behaviour and numbers

1. At home the pocket is under the collar; one queued ball drops in and rests on the deck. **Supported balls are 0.62–0.78 m across** (owner decision 8, 22 November 2026: the shuttle feeds singly for balls 0.62–0.78 m, with no size setting): the Basketball (0.70 m), and the Soccer ball and Cannonball (0.64 m sphere) when delivered. These are the balls a Cannon takes (its window is 0.6–0.84 m).
2. **Why one ball only (hand calculation).** A second ball dropping from the collar can nestle beside the first. In a pocket of width W, with the first ball on the deck, the lowest its centre can sit is d ÷ 2 + √(d² − 2·(W − d)²). It is left behind only if that is above the shear plane h: the wall tops and shutter, 0.8 m, which must also clear the largest ball (0.78 m). With W = 0.8 m: d = 0.62 gives 0.875 m, d = 0.64 gives 0.92 m, d = 0.70 gives 1.04 m, all above h, so the shutter slides under the second ball and the collar holds it. d = 0.56 (Bowling ball) gives 0.725 m and d = 0.50 (Tennis ball) gives 0.515 m, below h: two would leave. Those balls are outside the supported range. The plan-view rule alone (two balls side by side on the deck need d ≤ 0.47 m) is necessary and not sufficient.
3. When `go` holds, the pocket slides 1.2 m; the shutter closes the collar, so the queue cannot follow. Over the hole the ball drops to the chute, rolls down it (about 1.8 m/s at its foot) and crosses the outlet pad to the launcher in about 0.5 s.
4. While a ball is on the outlet pad (in transit, or resting there because the launcher is full or the outlet is blocked) `go` is false and the pocket returns home and waits. This is the chamber interlock; nothing is deleted.
5. Supply loss mid-stroke: SelfLock holds the pocket where it is, with the ball still on the deck.
6. Tick order: the load reading commits with the physics tick and the supply solve of the next tick reads it (D7 applied to a contact reading); one tick of latency is not visible.
7. Whether a resting shuttle, or a queue resting in its collar, may sleep is `ENGINE-ISLANDS-SLEEP`'s (owner decision 3a); island sleeping is not delivered and nothing here assumes it.

The requirement's "launcher has returned" is an input, not a sensor in the shuttle: wire `ReturnedIn` from the Linear pusher's `RetractedOut` (`elements/LinearPusher/LinearPusherElementDeclaration.cs:47`), or from the supply for a launcher with no return stroke (the Cannon).

## 3. Engine capabilities

| Capability | Mark | Symbol | Test |
| --- | --- | --- | --- |
| Directional supplied slider that self-locks | present | `MechanicalContractValues.cs:89` | `LinearPusherTests.LosingPowerLocksTheIntermediatePositionAndRetractResumesFromIt` |
| Endpoint contacts as outputs | present | `SignalContactControl.cs:13` | `LinearPusherTests.BothEndpointConsumersFollowDepartureReturnAndSupplyRemoval` |
| Obstruction reported as Blocked, no penetration | present | `MechanicalContractValues.cs:87` | `LinearPusherTests.ObstructionStopsTheHeadAndReportsBlockedWhileAnAdjacentWallDoesNot` |
| Slider pushing cargo along a surface | present | `MechanicalContracts.cs:15` | `LinearPusherIntegrationTests.PhysicalSensorAndDelayPushConveyorCargoIntoPipeOnlyWithTheCommandConnection` |
| Contact-load sensor on an unrotated box of a static frame | present; re-qualified as a shape-based, in-tick sensor by `ENGINE-SENSOR-VOLUMES` | `SignalContractValues.cs:58`; `SignalBundleValidation.cs:187`–`:190` | `ComposedPhysicalSignalTests.TwoLoadFacesOnOneStaticFrameKeepTheirOwnState` |
| Package-internal supply edges and logic nodes | present; the two-input terminals are refactored by `ENGINE-CAPABILITY-INSTANCES` (finding E2) | `SupplyContractValues.cs:41` | `ComposableSupplyRoutingTests.PackageInternalSupplyEdgesAdmittedByValidationAndCompileToTopology` |
| A load contact negated by an internal Nor and commanding a drive | present, untested | same symbols | none; the slice adds `ReloadShuttleTests.ALoadedOutletWithdrawsTheAdvanceCommand` |
| **A supplied drive's committed work debited from its source, its draw bounded by the Battery's `power_limit`** | **missing** | drive supply is binary (`_bmad-output/implementation-artifacts/deferred-work.md:211`) | Owner: `ENGINE-SUPPLY-DEMAND` (sprint key `engine-supply-drive-demand`) |

Counts: 6 present, 1 untested, 0 narrower, 1 missing. No acceptance row asserts the debit; the declared power figures in section 2 hold once `ENGINE-SUPPLY-DEMAND` is delivered.

### Engine conformance (owner addendum, 22 November 2026)

- **Declaration data only.** A static frame, one slider with a supplied directional drive, one load sensor, four logic nodes, three contacts and five ports: each a capability the Linear pusher, Pressure plate and gates declare. No element code, callback or engine branch.
- **Generic laws.** One ball per stroke: rigid contact and the declared geometry (item 2). Interlock: the contact-load rule feeding the supply network's And and Nor rules, which command the drive law. Self-lock: the drive's inactive response. The missing capability is stated for any package: a supplied drive's committed work debited from its source within the source's power bound, owned by `ENGINE-SUPPLY-DEMAND`.
- **Three layers.** Colliders (physics): 17 boxes as hidden `col_` proxies (deck 4 around the hole, collar 4, throat walls 2, chute 1, pad 1; pocket 5). Visuals: the art nodes, none a collider. Animation: two colour bindings read from committed phase and load state; they write nothing back. Per art node: frame boxes are moved by nothing; pocket walls, rails and shutter by body 1's pose; indicator and outlet lamp by presentation bindings (colour only).
- **One-way flow and typing.** Physics → animation → renderer. Port roles, drive selection and phases are enums; values f32.
- **Flat state.** 2 bodies; 17 box colliders (0 of any other shape); 1 prismatic joint; 1 drive; 1 load sensor; 4 logic nodes; 3 contacts; 1 consumer; 0 stores; 1 body-pose and 2 driver bindings.
- **Performance.** Thirteen colliders more than the Linear pusher's four, each a wall a ball touches (a hole and a ring cannot be one box). Body 0 is static. Whether a resting shuttle or queue may sleep is `ENGINE-ISLANDS-SLEEP`'s (owner decision 3a); island sleeping is not delivered and nothing here assumes it. The sensor region is one bounded box, queried through the spatial query interface once `ENGINE-SPATIAL-QUERIES` and `ENGINE-SENSOR-VOLUMES` are delivered. Balls move under 2 m/s here.
- **Tests are scenarios.** Every row below runs the production simulation on the package declaration.

## 4. Sources and legacy

- **Requirement.** [todo-398](../requirements.md#todo-398): "Supplied sliding pocket advances one physical queued ball only when chamber is empty and launcher has returned. Show queue, occupied pocket and jam; never delete overflow."
- **Campaign.** Chapter 5 "Ready, Set, Throw!" (levels 41–50); [coverage row](../requirements.md#campaign-element-coverage) "… hopper/escapement, reload shuttle …", shuttle first use 41–50.
- **Legacy.** None.
- **Files harvested.** None.

## 5. Acceptance outline

Tolerances: position ±0.02 m; stall guard 20 s of game time; counts are exact.

| Row | Given / When | Then | Control and boundary | Planned test |
| --- | --- | --- | --- | --- |
| A1 | Two Basketballs queued in the collar, supply and both inputs on | Each stroke puts exactly one ball on the chute | Control: `AdvanceIn` off, none leaves. Boundary: Tennis balls with `radius` 0.31 (0.62 m) and 0.39 (0.78 m) each go singly. Outside the range: two default Tennis balls (0.50 m) may leave together; only the body count is asserted for them | `ReloadShuttleTests.TwoQueuedBallsLeaveOnePerStroke` |
| A2 | `ReturnedIn` off, or a ball resting on the outlet pad (early feeding) | The pocket stays home | Control: clear the pad and it advances | `ReloadShuttleTests.ALoadedOutletWithdrawsTheAdvanceCommand` |
| A3 | `AdvanceIn` held on | A second ball leaves only after the first has left the pad; the body count never changes | Control: outlet blocked, the second ball stays in the pocket | `ReloadShuttleTests.AHeldCommandCannotDumpTheQueue` |
| A4 | Supply cut mid-stroke, then restored | The pocket holds within 0.02 m, the ball stays on the deck, the stroke resumes | Control: no cut | `ReloadShuttleTests.SupplyLossHoldsTheStroke` |
| A5 | Outlet blocked by a Wall beyond the pad | The ball rests on the pad, the committed load phase is loaded and the pocket returns home and stays (committed prismatic phase held at 0); no penetration; every ball still present | Control: wall moved 0.2 m clear, feeding resumes | `ReloadShuttleTests.ABlockedOutletJamsWithoutLosingABall` |
| A6 | Funnel → shuttle → Cannon. `DeliveredOut` drives a Counter (target 3); the Counter's reached output, inverted by an Electrical Nor, is the `AdvanceIn` level | Exactly three balls are fired | Control: counter target 1 | `ReloadShuttleTests.ACounterStopsFeedingAfterThreeDeliveries` |
| A7 | Run, Reset; Save, Load | Authored pose, parameters and five connections restored; a second Run delivers again | — | `ReloadShuttleTests.ResetAndReloadRestoreTheAuthoredShuttle` |
| A8 | An activation wire into `AdvanceIn`; `speed` 0.4 or NaN | Each refused with a typed reason, nothing changes | Boundary: 0.5 and 2 are admitted | `ReloadShuttleTests.WrongKindWiresAndOutOfRangeValuesAreRefused` |

A6 wires the delivered Counter, whose activation kind `ENGINE-CAPABILITY-INSTANCES` renames to its capability name (finding E1); the row uses the Counter's ports as declared on main when the slice starts.

**Integration.** Cannon, Linear pusher, Funnel, Counter, Conveyor, Battery (delivered); EL-241 Ball hopper (chapter 3, delivered before this part; A6 is repeated with it feeding the collar). A level that places it also needs lesson fixtures bound to a named package (S8).

**Coverage binding (planned, not yet live).** Published main binds a named-identity package through a final section of its spec that `tools/Coverage/NamedPackageSources.cs` reads (pattern: EL-239 Baseball); that section is not written here, because the reader parses every `EL-*.md` that carries its heading. It is written in the syntax current on main when this identity's slice starts. Planned values: Package `reload_shuttle`; Named identity EL-217; Acceptance todo-398 and sequence-task-353; owner slice `named-el-217`; Mode records `Configuration=Adjustable`. A slice-owned package is admitted only once main's reader admits one (the coverage follow-on story S7, which owner decision 15 also has widen the reader; EL-223 is paused, owner decision 24g).

**Delivery.** One element slice. Prerequisites: blocked on the engine programme (owner decision 3a), in particular `ENGINE-SENSOR-VOLUMES`, `ENGINE-SUPPLY-DEMAND`, `ENGINE-CAPABILITY-INSTANCES` and `ENGINE-ISLANDS-SLEEP`; then the coverage reader admitting a slice-owned package (S7; EL-223 is paused, owner decision 24g); S3 if published (S2 is published at `25df3cd5`; no test-project list edit). Files: `elements/ReloadShuttle/` (`ReloadShuttle.csproj`, declaration and `.uid`, `reload_shuttle.tscn`, `icon.svg` and `.import`) and `CuriousContraptions.tests/ReloadShuttleTests.cs`; hand-edited lists until S3: `CuriousContraptions.slnx`, `CSharpStaticGateTests.cs:703`, `PackageRuntimeParityTests.cs:596` and `:653`, one `VisibleSolidColliderTests.cs:110` row. Review tier: engine (one untested capability). **Disqualified if** the probe finds a Nor node cannot take a contact's output inside one package; a rolling ball on the pad is not sensed as loaded; a ball in transit holds `go` false for longer than its crossing plus the sensor's release hold; or the Counter's ports cannot be wired as A6 states (then A6 names the delivered adapter).

## 6. Open questions

1. *How does the shuttle know the launcher's chamber is empty?* Resolved under the standing delegation: by its own outlet pad. The Cannon exposes no occupancy output (`elements/Cannon/CannonElementDeclaration.cs:35`), and a staged ball that cannot enter the chamber rests on the pad, which is the state that matters.
2. *Trigger kind.* Resolved (D19): a level command, so a Counter or gate can stop it; no pulse input.
3. *Pocket size.* Resolved by owner decision 8, 22 November 2026: the shuttle feeds singly for balls 0.62–0.78 m, with no size setting (the fixed pocket of D13).

Needs the owner: nothing.
