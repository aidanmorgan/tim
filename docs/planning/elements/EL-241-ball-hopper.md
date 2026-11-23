# EL-241 · Ball hopper with one-ball escapement — named-identity spec

**Current delivery (21 November 2026):** [EL-241 ledger entry](../invest/named-elements.md#element-241), ledgered 21 November 2026; no earlier spec and no legacy to harvest. Code is cited as `path:line` in checkout `spec-drafts` (published main `8d1d68aa`); abstraction files are cited by file name and live under `CuriousContraptions.Abstractions*/`. Values are f32; **proposed** values are starting values ([D1](../invest/delivery-batches.md#3-family-level-decisions)). Tests are native; browser qualification is pending.

## 1. Identity and disposition

| Field | Value |
| --- | --- |
| ID / name / type | EL-241 · Ball hopper with one-ball escapement · Mechanical |
| Requirement anchor | [sequence-task-351](../requirements.md#sequence-task-351) |
| Ledger entry / owner | [element-241](../invest/named-elements.md#element-241); slice `named-el-241`, no earlier S owner |
| Disposition | **To deliver as a single element, after the engine programme (owner decision 3a).** Run "selective passage" in delivery batches 1b. Separate from [EL-057 Escapement](EL-057-escapement.md), which steps a loaded mechanism, and from [EL-217 Reload shuttle](EL-217-reload-shuttle.md), which is level-commanded and interlocked to a launcher. |
| Planned package | id `ball_hopper`, directory `elements/BallHopper/` |
| Related | [CAT-030 Funnel](CAT-030-funnel.md), [EL-237 Boxing glove](EL-237-boxing-glove.md) (same pulse-to-stroke composition) |
| Status | Not started. Blocked on the engine programme (owner decision 3a), in particular `ENGINE-SUPPLY-DEMAND` (the drive's work paid from the Battery and its 120 W shared with a Clock and two gates; supply is binary today) and `ENGINE-ISLANDS-SLEEP` (a resting queue); then `ENGINE-JOINT-KINDS` (the declared spring rest position of the escapement, owner decision 6 as extended by coordinator decision 25). |

## 2. Declaration

Shape: a static bin over the Boxing glove's composition (a sprung slider driven for one timer window per pulse).

- **Interfaces.** `ISliderElement` (`MechanicalContracts.cs:15`), `ISupplyElement` (`ElectricalContracts.cs:11`), `ISignalElement` (`SignalContracts.cs:8`), `ISceneGeometryElement` (`SceneGeometry.cs:10`).
- **Bin and deck (body 0, static).** Bin inner 1.6 × 0.9 × 0.8 m (length, height, depth: one ball deep) with a floor sloping 20° to a 0.8 m square chute at one end, whose lower edge is 0.8 m above the deck; under the chute a deck 2.2 × 0.12 × 0.9 m with a 0.8 m square outlet hole 1.1 m from the chute axis. The bin's front wall is clear (alpha 0.16, as the Pipe shell) so the queue is visible. Restitution 0, friction 0.3. The bin holds three or four Basketballs (D13: fixed).
- **Escapement pocket (body 1, dynamic).** Two cross walls 0.1 × 0.8 × 0.8 m, 0.8 m apart, side rails 0.8 m tall (inner footprint 0.8 × 0.8 m), and a shutter plate 1.1 × 0.06 × 0.8 m at the wall tops that closes the chute while the pocket is away. Mass 0.7 kg. No floor: the ball rests on the deck.
- **Slider.** Along the deck, limits 0 to 1.1 m; spring 20 N/m with its rest position declared at −0.4 m, outside the travel, and damping 5 N·s/m, returns the pocket home (owner decision 6, extended by coordinator decision 25 to every spring-returned gate; `ENGINE-JOINT-KINDS`). At the home stop the preload is 20 × 0.4 = 8 N, more than the pocket's own weight (0.7 × 9.81 = 6.9 N), so an unpowered pocket returns to its stop in any mounting orientation. Drive: fixed target 1.1 m, maximum speed 1.6 m/s, stopping acceleration 20 m/s², effort 45 N (spring at full travel 20 × 1.5 = 30 N, 15 N to spare; a pushed ball rolls, so its drag is small), power 45 × 1.6 = 72 W, inactive response `Free`. 72 W leaves 48 W of a default Battery's 120 W (`elements/Battery/BatteryElementDeclaration.cs:44`) for a Clock and two Powered gates (20 W each, `elements/PoweredGate/PoweredGateElementDeclaration.cs:94`).
- **Signal node.** One timer `"stroke"` on `stroke_seconds`, `Rearm`, `BeforeNetworks`, no elapsed signal (`SignalContractValues.cs:49`).
- **Supply graph.** `PowerIn` → contact closed while the timer counts → drive consumer on the slider.

| Parameter | Type, unit | Range | Default | Editor | Reason |
| --- | --- | --- | --- | --- | --- |
| `stroke_seconds` | Real, s | 1.4–4 | 1.5 | Reject, build-only | The pocket needs 1.1 ÷ 1.6 = 0.69 s to cross (0.73 s with acceleration) and a 0.78 m ball √(2 × 0.78 ÷ 9.81) = 0.40 s to fall its own diameter clear of the returning wall: 1.13 s. Seconds, not ticks (D10) |

| Port | Domain | Direction, role, kind | Position (m) |
| --- | --- | --- | --- |
| `PowerIn` | Electrical | Input, PowerIn | (−1.1, −0.2, 0.5) |
| `ActivationIn` | Signal | Input, ActivationIn, Pulse | (−0.7, −0.2, 0.5) |

- **Presentation.** Pocket body binding; indicator from `CommittedTimerActive`.
- **Palette.** Motion, "Ball hopper"; order 114 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)).

### Art and solidity

| Node | Mesh | Class |
| --- | --- | --- |
| Bin walls, sloped floor, chute walls | Boxes; back and sides cream `#fff8e9` (owner decision 4), front clear sky blue `#7ebcd4` at alpha 0.16 | Solid, colliders on body 0 (the clear wall is solid too) |
| Deck with outlet hole | Four boxes around the hole | Solid |
| Pocket walls, rails, shutter | Boxes, gold `#f7cb52` | Solid, colliders on body 1 |
| Indicator | Flush disc | Decorative: printed flat |

Icon: a bin with three stacked circles and one circle below an arrow. It differs from the Funnel by the visible queue and the single released ball.

### Behaviour and numbers

1. At home the pocket is under the chute and holds one ball; the rest queue behind it on the sloped floor. **Supported balls are 0.62–0.78 m across** (Basketball; Soccer ball and Cannonball when delivered), with no size setting (owner decision 8, 22 November 2026). Hand calculation: a second ball in the chute can nestle beside the first; its centre sits no lower than d ÷ 2 + √(d² − 2·(0.8 − d)²), which must be above the shear plane (wall tops and shutter, 0.8 m, set by the largest ball). d = 0.62 gives 0.875 m, 0.70 gives 1.04 m (held back); d = 0.56 gives 0.725 m and 0.50 gives 0.515 m (carried: two leave). Tennis balls, Baseballs and Bowling balls are outside the supported range.
2. One accepted pulse gives one stroke: the pocket carries its ball to the outlet hole, the ball drops, the window ends and the preloaded spring returns the pocket to its home stop, which reloads. The shutter holds the queue while the pocket is away, so one stroke cannot release two balls.
3. A pulse during a stroke is ignored (`Rearm` window). A held source cannot hold the gate open: the input admits pulses only, and a Level output is refused at connection (`SignalContracts.cs:29`).
4. Empty hopper: the stroke runs and nothing leaves. Blocked outlet: the ball cannot drop, the spring carries it home in the pocket, the queue stays behind the shutter and nothing is duplicated or deleted.
5. No supply: no stroke.

## 3. Engine capabilities

| Capability | Mark | Symbol | Test |
| --- | --- | --- | --- |
| Sprung slider with a fixed-target supplied drive | present | `elements/PoweredGate/PoweredGateElementDeclaration.cs:76` | `DeclaredPrismaticDriveTests.ThePoweredGateOpensToItsTargetWithinItsDeclaredSpeedEffortAndPowerOnEverySubstep` |
| A ball resting against a blade passes when it moves away | present | same | `DeclaredPrismaticDriveTests.ABallRestingAgainstTheClosedTiltedBladePassesDownstreamWhenEnabledDuringRun` |
| Timer window, busy pulses ignored, rearm | present | `SignalContractValues.cs:9` | `HoldTimerTests.BusyInputsDoNotExtendAndExpiryRearmsWithoutAnyOutput` |
| Contact closed while a timer counts | present | `SignalContactControl.cs:12` | `HoldTimerTests.PhysicalTriggerHoldsExactWindowWithoutGeneratingSupply` |
| Static compound geometry with a sloped floor and a hole | present | `SceneGeometry.cs:10` | `VisibleSolidColliderTests.EverySolidArtPartHasItsColliderAndNoColliderReachesBeyondTheArt` (Funnel and Receiver rows) |
| A Level source refused on a Pulse input | present | `SignalContracts.cs:29` | `StateLatchTests.Output_LevelDrivesOnlySignalLampsAndRejectsInvalidShapes` |
| The timer contact supplying a drive inside the same package | present, untested | `SupplyContractValues.cs:40`, `:52` | Shared with EL-237; the slice adds `BallHopperTests.OnePulseReleasesExactlyOneBall` |
| Several balls stacked under their own weight on a slope behind a shutter, at rest | present, untested | contact solver | Nearest: `CannonIntegrationTests.TwoPhysicalOccupantsPresentUnavailableAndResetToEmpty`; the slice adds `BallHopperTests.AFullQueueRestsWithoutCreep` |
| A drive's committed work debited from its supplying Battery, several consumers sharing one source's power | **missing** | supply is binary: a supplied drive does not debit its source (`_bmad-output/implementation-artifacts/deferred-work.md:211`) | Owner: `ENGINE-SUPPLY-DEMAND` (sprint key `engine-supply-drive-demand`) |
| **A prismatic spring rest position declared outside the travel (a preload)** | **missing** | the slider spring acts toward its zero only | Owner: `ENGINE-JOINT-KINDS` (the spring rest position of owner decision 6; coordinator decisions 21 and 25) |

Counts: 6 present, 2 untested, 0 narrower, 2 missing. Each missing row has one owning story of the engine programme or its first-consumer stories.

### Engine conformance (owner addendum, 22 November 2026)

- **Declaration data only; generic laws.** A static bin and deck, one sprung slider with a supplied fixed-target drive, one timer node, one contact and two ports: capabilities the Powered gate and Hold timer declare. One ball per stroke is rigid contact on declared geometry; the stroke is the timer rule closing a supply contact that feeds the drive law; the return is the spring law with a declared rest position. The missing capabilities are stated for any package: drive work debited from its source (`ENGINE-SUPPLY-DEMAND`) and a spring rest position outside the travel (`ENGINE-JOINT-KINDS`). No element code, callback or engine branch.
- **Three layers.** Colliders (physics): 17 boxes as hidden `col_` proxies (bin walls 4, sloped floor 1, chute walls 3, deck 4 around the hole; pocket 5). Visuals: the art nodes, including the clear wall, none a collider. Animation: one indicator binding from committed timer state. What moves each art node: bin, chute, deck and indicator disc nothing (static body 0; the indicator's colour is a binding); pocket walls, rails and shutter the body 1 pose.
- **Flat state.** 2 bodies; 17 box colliders; 1 prismatic joint; 1 drive; 0 sensors; 1 timer node; 1 contact; 1 consumer; 0 stores; 1 body-pose and 1 driver binding.
- **Performance.** More colliders than the Powered gate because a bin, a chute and a holed deck are walls a ball touches; none is cosmetic. Body 0 is static. Whether a resting queue may sleep is `ENGINE-ISLANDS-SLEEP`'s (owner decision 3a); island sleeping is not delivered and nothing here assumes it. No region scans the scene.
- **One-way flow, typing, tests.** Physics → animation → renderer; nothing writes back. Closed sets are enums, ids typed, values f32. Every acceptance row is a scenario on the production simulation.

## 4. Sources and legacy

- **Requirement.** [sequence-task-351](../requirements.md#sequence-task-351): "hopper with a one-ball escapement, visible queue and one release per trigger. Example: feed two destinations alternately without releasing the whole supply."
- **Campaign.** Chapter 3 "Pipe Dreams" (levels 21–30): "hopper/escapement"; lesson "Queue the applause"; coverage row first use 21–30, reuse 41–50, 111–120, 136–150.
- **Legacy.** None.
- **Files harvested.** None.

## 5. Acceptance outline

Tolerances: ball counts are exact; rest means every queued ball moves less than 0.02 m over 5 s; the stroke window is asserted to ±1 tick; stall guard 20 s.

| Row | Given / When | Then | Control and boundary | Planned test |
| --- | --- | --- | --- | --- |
| H1 | Four Basketballs loaded, supplied, three pulses each after the previous window | Exactly three balls pass the outlet, one per pulse | Control: no pulse, none leaves. Boundary: Tennis balls with `radius` 0.31 (0.62 m) and 0.39 (0.78 m) also leave singly. Outside the range: default Tennis balls (0.50 m) may leave two at a stroke; only the body count is asserted | `BallHopperTests.OnePulseReleasesExactlyOneBall` |
| H2 | Two pulses inside one window | One ball | Boundary: `stroke_seconds` 1.4 and 4 admitted | `BallHopperTests.APulseDuringAStrokeIsIgnored` |
| H3 | Empty hopper, one pulse | The stroke runs; no ball leaves; the body count is unchanged | Control: one ball loaded leaves | `BallHopperTests.AnEmptyHopperReleasesNothing` |
| H4 | Outlet blocked by a Wall under the hole, one pulse | The ball returns home in the pocket; the queue is intact; nothing passes | Control: Wall removed, the ball leaves | `BallHopperTests.ABlockedOutletNeitherDumpsNorDuplicates` |
| H5 | Four balls loaded, no pulse, 5 s | The queue rests | — | `BallHopperTests.AFullQueueRestsWithoutCreep` |
| H6 | Clock 1 (period T) pulses the hopper; Clock 2 (period 2T) supplies Powered gate A directly and gate B through an Electrical Nor, so successive balls meet opposite gates | Balls alternate between two Receivers; the supply is not exhausted in one go | Control: clocks unpowered | `BallHopperTests.AClockFeedsTwoDestinationsAlternately` (a cross-part row owned by the Electrical NOR slice: the Repeating clock is taught at 28 and the NOR at 32, after the hopper at 26a; coordinator decisions 36 and 41) |
| H7 | Reset mid-stroke; Save, Load | Every ball back at its authored pose; pocket home; a second Run releases again | — | `BallHopperTests.ResetAndReloadRestoreTheQueue` |
| H8 | Pressure plate level into `ActivationIn`; electrical wire into `ActivationIn`; `stroke_seconds` 1.3 | Refused with a typed reason | — | `BallHopperTests.WrongKindWiresAndOutOfRangeValuesAreRefused` |
| H9 | Supplied, one pulse, supply cut mid-stroke; once with the hopper upright and once rotated so the slider is vertical with its home stop uppermost | In both the pocket returns to its home stop and rests there within 0.02 m (8 N preload against 6.9 N of pocket weight) | Control: upright with supply kept, the stroke completes | `BallHopperTests.AnUnpoweredPocketReturnsHomeInAnyOrientation` |

**Integration.** Battery, Clock, Impact switch, Ball detector, Counter, Powered gate, Pipe, Conveyor, Receiver (delivered); EL-217 Reload shuttle and EL-164 alternating diverter (later).

**Coverage binding (planned, not yet live).** Published main binds a named-identity package through a final section of its spec that `tools/Coverage/NamedPackageSources.cs` reads (pattern: EL-239 Baseball); that section is not written here, because the reader parses every `EL-*.md` that carries its heading. It is written in the syntax current on main when this identity's slice starts. Planned values: Package `ball_hopper`; Named identity EL-241; Acceptance `sequence-task-351` alone (it has no `todo` anchor; a sequence-task-only acceptance anchor is proposed for the ledger in the [batch summary](readiness/new-identities.md)); owner slice `named-el-241`; Mode records `Configuration=Adjustable`. A slice-owned package with a sequence-task-only anchor is admitted only once main's reader admits both (the coverage follow-on story S7, which owner decision 15 also has widen the reader; EL-223 is paused, owner decision 24g). A level that places it also needs lesson fixtures bound to a named package (S8).

**Delivery.** One element slice. Prerequisites, in order: blocked on the engine programme (owner decision 3a), in particular `ENGINE-SUPPLY-DEMAND` and `ENGINE-ISLANDS-SLEEP`; then `ENGINE-JOINT-KINDS` (spring rest position); the coverage reader admitting this binding; S3 if published (S2 is published at `25df3cd5`; no test-project list edit). Delivered before EL-237 (first taught at 26a against the Boxing glove's 41b; owner decision 24a), so this slice proves the shared timer-contact composition once and EL-237 reuses it. Files: `elements/BallHopper/` (project, declaration and `.uid`, `ball_hopper.tscn`, icon and import), `CuriousContraptions.tests/BallHopperTests.cs`, and the usual hand-edited lists until S3. Review tier: engine (untested capabilities). **Disqualified if** a stacked queue does not rest, the timer contact cannot feed the drive in-package, or the Clock's output kind does not allow H6's wiring (then a State latch toggled by a Ball detector replaces Clock 2).

## 6. Open questions

1. *Vertical pin escapement or sliding pocket?* Resolved under the standing delegation: sliding pocket. Two pins release one ball only for one ball diameter; a pocket releases one ball for every preset.
2. *An "empty" output.* Resolved by D19: not added; no lesson uses it.
3. *Mechanical (unsupplied) trigger.* Resolved by D19: not added; the stroke needs paid work and the supply is its source.
4. *Pocket size.* Resolved by D13 and owner decision 8 (22 November 2026): fixed; the hopper feeds singly for balls 0.62–0.78 m, with no size setting. The earlier claim that one pocket serves every preset was wrong (item 1).
5. *Spring rest position.* Resolved by coordinator decision 25 (owner decision 6 extended to every spring-returned gate): the escapement's spring declares a rest position outside the travel; stiffness lowered from 30 to 20 N/m so the preload exceeds the pocket's weight while the drive keeps 15 N to spare.

Needs the owner: nothing.
