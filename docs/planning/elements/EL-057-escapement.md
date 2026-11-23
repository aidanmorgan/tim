# EL-057 · Escapement declaration readiness spec

**Current delivery (24 October 2026):** [EL-057 delivery slice](../invest/named-elements.md#element-057). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 named-identity spec. Baseline commit `a6c914e`. Legacy citations use `path@a6c914e:Lstart-Lend` and stay retrievable from git history after the Epic 7 purge. Values marked **proposed** are design values the owner may revise. Sections 1 to 3 and 5 to 8 were rewritten on 21 November 2026 against checkout `8d1d68aa`; their `path:line` citations are current code, read that day.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-057 |
| Name | Escapement |
| Type | Mechanical |
| Requirement anchor | [element-057](../requirements.md#element-057); scope index [todo-375](../requirements.md#todo-375) |
| Named entry | [named-elements.md#element-057](../invest/named-elements.md#element-057); slice `named-el-057`, owner S343 |
| CAT spec refined or extended | none. Related: [CAT-017 clock](CAT-017-clock.md) (periodic trigger), [CAT-063 switch](CAT-063-switch.md) (contact trigger), [CAT-071 wound spring](CAT-071-wound_spring.md) (the delivered signal-released retention this part mirrors on a shaft), [CAT-042 motor](CAT-042-motor.md) (a stalled supplied Motor is a loaded drive) |
| Related identities | [EL-056 ratchet](EL-056-ratchet.md), [EL-063 cable winch](EL-063-cable-winch.md) (weight-driven drum as stored drive), [EL-061 indexed carousel](EL-061-indexed-carousel.md). EL-241 Ball hopper with one-ball escapement ([ledger](../invest/named-elements.md#element-241)) is a separate identity that meters balls; EL-057 steps a loaded shaft |
| Roadmap story | [EL-057 delivery slice](../invest/named-elements.md#element-057) |
| Status | Not started; blocked on the engine programme (owner decision 3a), relying most on ENGINE-SUPPLY-DEMAND (the stalled Motor that is its stored drive) and ENGINE-ISLANDS-SLEEP (a held wheel at rest), with ENGINE-CONTACT-OBSERVATIONS for the Impact switch as a pulse source; then ENGINE-JOINT-KINDS (indexed stop, section 3); then S7. No package exists |
| Disposition | To deliver as a single element, after ENGINE-JOINT-KINDS |
| Planned package | id `escapement`, directory `elements/Escapement/`, class `EscapementElementDeclaration` |
| Coverage binding | Planned, not yet live (section 7); blocked on S7. No CAT number |
| delivery-batches class | Not classified in [delivery-batches](../invest/delivery-batches.md). Code places it in 1c: open after the Joints engine slice. Proposed row: Joints, run "One-way and stepped shafts", EL-056 and EL-057 |

## 2. Declaration

The body, hinge, port and signal-input part follows the Conveyor's pass-through roller (`elements/Conveyor/ConveyorElementDeclaration.cs:42`–`:52`) and the Wound spring's pulse-released retention (`elements/WoundSpring/WoundSpringElementDeclaration.cs:57`–`:69`). The indexed stop itself is not declarable today; its descriptor is defined by ENGINE-JOINT-KINDS, and this section states what the package will ask of it. Interfaces: `IHingeElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:20`), `ISignalElement` (`CuriousContraptions.Abstractions.Signal/SignalContracts.cs:8`), `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`), plus the revolute counterpart of `IPrismaticRetentionElement` (`MechanicalContracts.cs:85`) that the engine story adds. `KinematicMode.Static`; domains `Mechanical | Signal`; footprint 2 bodies, 1 moving, 0 prismatic, 1 revolute. No executable logic.

**Coordinator guidance confirmed.** The part needs a latched, stepped revolute release; it is *missing*, owner ENGINE-JOINT-KINDS (indexed stop, owner decision 3a). EL-057 stays the shaft escapement; the campaign's "hopper/escapement" for balls is EL-241.

**Bodies.**

| Body | Shape (m) | Mass and inertia | Joint |
| --- | --- | --- | --- |
| 0 housing (static) | box 1.1 × 0.9 × 0.4 centred at the origin; navy foot box 1.3 × 0.1 × 0.5 centred (0, −0.5, 0) | static | support |
| 1 escape wheel and shaft | cylinder r 0.28, width 0.13, axis local Z | 0.25 kg; axial 0.0098, transverse 0.00525 kg·m² (the Clutch shaft, `elements/Clutch/ClutchElementDeclaration.cs:17`–`:19`, `:120`–`:121`) | continuous revolute hinge 0 on body 0 |

- **Hinge 0** (`RevoluteHingeDescriptor`, `CuriousContraptions.Abstractions/MechanicalContractValues.cs:149`): base frame (0, −0.1, 0.27); `RevoluteTravel.Continuous`; friction torque 0.02 N·m; no drive.
- **Indexed stop on hinge 0** (engine story): a rigid two-sided stop at the index angles k × π/4 (eight per turn), starting locked at the authored angle 0. A Release request frees the wheel; it re-locks at the next index angle it reaches, in whichever direction the load turns it. The stop applies no torque of its own in the direction of motion. It declares a minimum release load of 0.1 N·m: a request while it carries less is answered Empty.
- **Signal.** One `SignalPhysicalRequestDescriptor` (`CuriousContraptions.Abstractions/SignalContractValues.cs:65`) with operation `PhysicalRequestOperation.Release` (`CuriousContraptions.Abstractions/PrismaticRetentionValues.cs:10`), targeting the stop, bound to `ActivationIn`: the same wiring as the Wound spring's release (`WoundSpringElementDeclaration.cs:66`–`:69`). The target kind for a revolute stop does not exist (section 3).
- **Material** for both bodies (restitution, friction, rolling resistance, bounce threshold): (0, 0.3, 0, 0.1) (`ClutchElementDeclaration.cs:89`–`:91`).

**Parameters.** None. Eight steps per turn is fixed (D13); there is no direction or capacity setting (section 6). No stores: the stored drive is upstream.

**Ports.**

| `PortId` | Role | Domain | Direction | Signal kind | Coordinate | Position (m) |
| --- | --- | --- | --- | --- | --- | --- |
| `DriveIn` | `DriveIn` | Mechanical | Input | none | `Revolute 0`, `CoordinatePerRadian` +1 | (−0.42, −0.3, 0.3), normal +Z; `MechanicalRim` radius 0.12 on body 0 (the drawn pulley) |
| `Drive` | `Drive` | Mechanical | Output | none | `Revolute 0`, `CoordinatePerRadian` +1 | (0.42, −0.3, 0.3), normal +Z; `MechanicalRim` radius 0.12 on body 0 (the drawn pulley) |
| `ActivationIn` | `ActivationIn` | Signal | Input | Pulse | none | (0, 0.45, 0.25), normal +Z |

`ActivationIn` admits a Pulse, the kind the System clock and Impact switch emit (`elements/SystemClock/SystemClockElementDeclaration.cs:41`, `elements/ImpactSwitch/ImpactSwitchElementDeclaration.cs:40`). There is no `ActivationOut` (section 6).

**Presentation.** Body binding `Visual/Wheel` → body 1. `Visual/Pallet` `LocalRotationZ` rocks between −0.2 and +0.2 rad on each release, driven by the revolute counterpart of `PresentationDriver.CommittedRetentionReleased` (`CuriousContraptions.Abstractions/ElementPresentation.cs:32`; the Wound spring latch, `WoundSpringElementDeclaration.cs:71`). `Visual/PulleyIn` and `Visual/PulleyOut` follow `CommittedRevoluteAngle` (`ElementPresentation.cs:30`).

**Palette.** Category `ElementCategory.Motion`, palette order 161 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)), colour ochre (0.84, 0.61, 0.28), title "Escapement", icon `res://elements/Escapement/icon.svg`, scene `escapement.tscn`.

### Art and solidity

| Node | Mesh and dimensions (m) | Class |
| --- | --- | --- |
| `Housing` | cream box 1.1 × 0.9 × 0.4 with a tall window on the front face | solid, box collider on body 0 |
| `Foot` | navy box 1.3 × 0.1 × 0.5 | solid, box collider on body 0 |
| `Visual/Wheel` | pale grey escape wheel, 8 long pointed teeth, outer r 0.28, width 0.13, proud of the front face | solid, cylinder collider on body 1 (teeth are relief inside the cylinder) |
| `Visual/Pallet` | gold two-armed anchor 0.4 wide above the wheel, inside the window, flush with the face | decorative: flush; moved by a presentation binding |
| `Visual/PulleyIn`, `Visual/PulleyOut` | ochre link pulleys r 0.12, 0.06 wide | decorative: turned by a presentation binding |

Colours: Cream `#fff8e9` (part-body cream, owner decision 4), Navy `#1c2836`, Gold `#f7cb52` (`DESIGN.md:80`); ochre `#d69c47` (`DESIGN.md:235`); pale grey metal (`DESIGN.md:248`). Icon: an eight-toothed wheel under a two-armed anchor. At a glance it differs from the Ratchet (many saw teeth, one pawl, a direction arrow) by its taller housing, eight long teeth, the two-armed anchor on top and a signal socket.

### Behaviour and numbers

- **Law.** Locked, the wheel is held at an index angle by a rigid stop, so a loaded drive upstream is stalled and does no work (as `CuriousContraptions.tests/RevoluteDriveTests.cs:307`). Each accepted release lets the load turn the wheel exactly one step of π/4 rad (45°), then the stop catches it. The part never turns the wheel itself.
- **No stored drive, no step.** A release that arrives while the stop carries less than its declared minimum release load, 0.1 N·m (five times the bearing friction; below it nothing would move the wheel anyway) is answered Empty and leaves the stop locked, the behaviour of a release on an uncharged Wound spring (`RetentionRequestResult.Empty`, `CuriousContraptions.Abstractions/PrismaticRetentionValues.cs:9`; `CuriousContraptions.tests/WoundSpringControlTests.cs:291`). So a load applied later does not get a free step. This is the requirement outcome and the map note "timer alone does not advance the mechanism".
- **Releases during a step are dropped** (answered AlreadyReleased, the same enum), not queued.
- **Step size 45°.** On a default Conveyor (0.667 m/rad, `elements/Conveyor/ConveyorElementDeclaration.cs:37`) one step moves the belt 0.52 m, one Tennis ball width (0.5 m, `elements/TennisBall/TennisBallElementDeclaration.cs:18`): one trigger advances the belt by one ball place. A rolling ball does not track a start-stop belt exactly, so row E6 asserts belt travel and only the ball's net progress.
- **Step time.** A default Motor (20 N·m, 6 rad/s target, `elements/Motor/MotorElementDeclaration.cs:41`, `:44`) completes a step in about 0.15 s. The System clock's shortest interval is 0.1 s (`SystemClockElementDeclaration.cs:32`), so the fastest clock loses every other pulse; a 0.5 s clock gives clean steps.
- **Rigid stop.** The stop has no torque capacity: it must catch a wheel arriving at up to the Motor's 20 rad/s within 0.02 rad, which a torque-bounded stop of the Brake's size could not do (40 N·m on about 0.04 kg·m² needs 0.2 rad).
- **Tick order.** A pulse on `ActivationIn` is routed in the signal phase and the release takes effect at the next committed physics boundary, as for a retention release (`CuriousContraptions.tests/RetentionRequestTests.cs:125`). One tick (1/120 s) of latency; no lesson times it (D7 reasoning).

## 3. Engine capabilities (capability probe)

| Capability | Status | Symbol | Exercising test, or owner |
| --- | --- | --- | --- |
| Static housing and moving wheel with colliders from scene geometry | present | `ISceneGeometryElement` `CuriousContraptions.Abstractions/SceneGeometry.cs:10` | `CuriousContraptions.tests/MotorTests.cs:1075` |
| Continuous hinge with bearing friction | present | `RevoluteHingeDescriptor` `CuriousContraptions.Abstractions/MechanicalContractValues.cs:149` | `CuriousContraptions.tests/RevoluteHingeFrictionTests.cs:59` |
| `DriveIn` and `Drive` on one revolute coordinate | present | `PortDescriptor.Coordinate` `CuriousContraptions.Abstractions/PortDescriptor.cs:67` | `CuriousContraptions.tests/ConveyorTests.cs:417` |
| A loaded, stalled supplied drive does no work while held | present | `RevoluteDriveDescriptor` `MechanicalContractValues.cs:138` | `CuriousContraptions.tests/RevoluteDriveTests.cs:307`; `CuriousContraptions.tests/MotorTests.cs:732` |
| Pulse input routed to a physical Release request; repeated requests re-arm; Empty when nothing is stored | present (for a prismatic retention target) | `SignalPhysicalRequestDescriptor` `CuriousContraptions.Abstractions/SignalContractValues.cs:65` | `CuriousContraptions.tests/RetentionRequestTests.cs:10`, `:125`; `CuriousContraptions.tests/WoundSpringControlTests.cs:291`, `:168` |
| Indexed stop on a revolute coordinate: rigid hold at a declared angular pitch, released for one step and re-locked at the next index | missing | none. Hinge limits are fixed at declaration (`MechanicalContractValues.cs:153`–`:154`) and a continuous hinge has none (`:127`–`:133`) | ENGINE-JOINT-KINDS (indexed stop) |
| A physical-request target for that stop | missing | `PhysicalRequestTargetKind { None, Retention, FiniteWorkActuator }` `CuriousContraptions.Abstractions/PhysicalRequestValues.cs:6`; `Retention` is prismatic only (`PrismaticRetentionDescriptor` `PrismaticRetentionValues.cs:13`) | ENGINE-JOINT-KINDS |
| Art bound to the stop's released state | narrower | `PresentationDriver.CommittedRetentionReleased` `CuriousContraptions.Abstractions/ElementPresentation.cs:32` names a prismatic retention only (`CuriousContraptions.Bundle/BundleValidator.cs:317`) | ENGINE-JOINT-KINDS (the stop's committed-released presentation driver) |

Counts: 5 present, 0 untested, 1 narrower, 2 missing; one owning story. Nothing may be solved with escapement-specific engine code: the stop is a general indexed revolute retention. EL-061 does not use this stop (it owns a supplied position drive).

**Element dependencies.** A stored or loaded drive: a supplied Motor stalled against the stop (delivered); EL-063 Winch with a Weight (not delivered); the Wound spring has no `Drive` output and cannot be the source. A trigger: Impact switch, System clock (delivered).

### Engine conformance

- **Declaration data only.** One hinge, one declared indexed stop and one signal request node. The step counter is the stop's own state in the engine's tables, not code of this part. The drawn pallet and teeth do no physics.
- **Generic laws.** Holding is the stop's rigid row; a step is the load turning the wheel between two index angles after a physical Release request; "no stored drive, no step" is the stop's declared minimum release load.
- **Layers.** Colliders: 2 static boxes (housing, foot) and 1 wheel cylinder. Visuals: the art table, none used as a collider. Animation: `Visual/Wheel` follows a body pose; the pallet rocks and the pulleys turn by presentation bindings on committed state, and are decorative; `Housing` and `Foot` are moved by nothing. Visible = solid: housing, foot and wheel each have a collider (the teeth are relief inside the wheel cylinder); the pallet is flush in the window and the pulleys are binding-moved decorative art.
- **One-way flow and typing.** Bindings write nothing back. Port roles, domains, the signal kind and the request operation are enums; ids are typed.
- **Flat state.** 2 bodies, 3 colliders (2 boxes, 1 cylinder), 1 revolute joint, 1 indexed stop row, 1 signal node, 0 drives, 0 sensors, 0 stores, 1 body binding and 3 rotation bindings.
- **Performance.** Three primitive colliders; the housing is static; the stop reads one coordinate. No island sleeping exists today; a held wheel at rest costs nothing only once ENGINE-ISLANDS-SLEEP (owner decision 3a) is delivered, ahead of this part.
- **Tests are scenarios.** Rows E1 to E11 run the production simulation through placed parts, Mechanical links (the delivered rigid connection; coordinator decision 36) and signal wires.

## 4. Sources and legacy

- Requirements row: "No stored drive means no step despite a release trigger." No variants.
- Research rows: ratchet/escapement "stepped release of a stored load" (`docs/component-research.md@a6c914e:L78-L78`); "escapement feeder — one-at-a-time release" (`docs/component-research.md@a6c914e:L82-L82`).

No legacy escapement exists. Facts reused from shared legacy mechanisms:

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | Joint travel bounds are finite, ordered and only for sliders or hinges strictly inside (−π, π). | `engine/physics/PhysicsJoint.cs@a6c914e:L104-L113`, `engine/physics/PhysicsJoint.cs@a6c914e:L137-L139` | carry forward (hinge-limit admission) | The stop row is a hinge limit; the principal-angle branch rule matters for a wheel that turns many times. |
| 2 | Holding with zero work does not need an energy source. | `CuriousContraptions.tests/MotorPredictionTests.cs@a6c914e:L149-L150` | carry forward | A locked escapement holds without drawing work. |
| 3 | With no stored energy, a trigger produces no release (`SpringTriggerResult.Empty`, release count 0). | `CuriousContraptions.tests/WoundSpringRuntimeTests.cs@a6c914e:L129-L137` | carry forward (pattern) | Mirrors "no stored drive, no step". |

**Files harvested:** `engine/physics/PhysicsJoint.cs`, `CuriousContraptions.tests/MotorPredictionTests.cs`, `CuriousContraptions.tests/WoundSpringRuntimeTests.cs`, `parts/WoundSpringPart.cs`. Searched with no hit: `parts/`, `engine/`, `content/puzzles.json`, `tools/Campaign`, `reference/` (terms: escapement, pallet, escape).

## 5. Acceptance outline

Requirement row: [element-057](../requirements.md#element-057). Preserved outcome: no stored drive means no step despite a release trigger; a loaded mechanism is released in discrete physical steps.

- **Chrome UI recipe (pending, post-catalogue).** Place an Escapement, a Cable winch with a Weight hanging on its cable (the stored drive) and a Conveyor; join Winch `Drive` → Escapement `DriveIn` and Escapement `Drive` → Conveyor `DriveIn` with Mechanical links; place two Switches each hit by a ball at different times, wired to `ActivationIn` (or a Clock). Verify placement and links. Until the Winch is delivered the stored drive is a supplied Motor linked to `DriveIn`.

Native rows. Planned tests live in `CuriousContraptions.tests/EscapementTests.cs`; all names are planned. A step is "complete" when the wheel is within 0.02 rad of the next index angle and slower than 0.05 rad/s: 0.02 rad is 5.6 mm at the wheel rim and 13 mm of belt, below one tenth of a ball radius, so a ball queue stays aligned over the eight steps of a turn.

| Row | Given / When / Then | Planned test | Control | Boundary | Tolerance and stall guard |
| --- | --- | --- | --- | --- | --- |
| E1 | Supplied default Motor → Escapement → Conveyor, three pulses 1 s apart. After each pulse the wheel has advanced exactly one more step and stopped | `EscapementTests.EachTriggerReleasesExactlyOneStep` | E2 | Motor at 20 rad/s and 100 N·m (fastest arrival at the stop) | step count exact (integer); angle within 0.02 rad of the index. Guard 1 s per step |
| E2 | Same construction, no pulse, 3 s: the wheel is held and the Motor's work stays under 1 % of torque × speed × time | `EscapementTests.ALoadedWheelStaysLockedWithoutATrigger` | E1 | none | travel within 0.01 rad (`CuriousContraptions.tests/RevoluteDriveTests.cs:371`); stall bound of `:307` |
| E3 | Motor unsupplied (no stored drive), three pulses: no step; then the Motor is supplied with no further pulse: still no step | `EscapementTests.WithoutAStoredDriveATriggerReleasesNothing` | E1 | a synthetic shaft torque of 0.05 N·m answers Empty; 0.2 N·m steps (either side of the declared 0.1 N·m minimum release load) | travel within 0.01 rad over 4 s. This is the requirement outcome; the second half excludes a banked release |
| E4 | Two pulses 2 ticks apart while loaded: one step, not two | `EscapementTests.ATriggerDuringAStepIsDropped` | the same two pulses 1 s apart: two steps | none | step count exact |
| E5 | The load reversed (a Reverse gear in the link path): each pulse steps the wheel one index the other way | `EscapementTests.TheStepFollowsTheDirectionOfTheLoad` | E1 | none | step count and sign exact |
| E6 | On a Conveyor of `length` 6 m (eight steps move the belt 4.19 m, more than the default 3.0 m, `elements/Conveyor/ConveyorElementDeclaration.cs:35`), over eight steps the belt travel (roller angle × `surface_per_radian`) is 8 × 0.524 m, and a Tennis ball on it ends at least 1 m further along and never moves back more than 0.1 m | `EscapementTests.EightStepsMakeOneTurnOfTheDrivenBelt` | no pulses: the ball stays | none | belt travel within 2 % (`CuriousContraptions.tests/GuideResponseTests.cs:174`); the ball bounds are progress, not tracking. Guard 12 s |
| E7 | At no tick does the kinetic energy of wheel and consumer exceed the Motor's committed work | `EscapementTests.SteppingAddsNoEnergy` | none | none | 3 % energy slack of `CuriousContraptions.tests/RevoluteDriveTests.cs:238` |
| E8 | Reset after three steps restores wheel angle 0, locked, and no pending release; a pulse sent in the tick before Reset fires nothing afterwards | S4 row 3 once delivered for the pose, plus `EscapementTests.ResetRestoresTheLockedWheelAndDropsAPendingRelease` | n/a | Reset mid-step | authored values, exact (the Wound spring's equivalent: `CuriousContraptions.tests/WoundSpringControlTests.cs:103`) |
| E9 | Save and load round-trip the two Mechanical links and the signal wire | S4 row 1 once delivered, else `EscapementTests.ASavedEscapementLoadsWithItsLinks` | n/a | none | exact field values |
| E10 | A Level signal output or an electrical wire into `ActivationIn`, and an electrical wire into `DriveIn`, are refused. The part has no parameters, so there is no parameter refusal row | S4 row 5 once delivered, else `EscapementTests.AWrongKindConnectionIsRefused` | a Pulse output is admitted | none | `WorkshopEditRejection.InvalidConnection` |
| E11 | Collider and art agree for housing, foot and wheel; the pallet rocks once per accepted release and not for an Empty one | S4 row 4 once delivered, else `EscapementArtTests.ThePalletRocksOncePerAcceptedRelease` | E3's pulses: no rock | n/a | sizes within 0.005 m |

Integrations: cross-element task [sequence-task-370](../requirements.md#sequence-task-370) under [todo-375](../requirements.md#todo-375), "a ratchet/escapement meters motion": planned `EscapementTests.AClockAndAnEscapementMeterASuppliedDrive` (System clock at 0.5 s → Escapement between a Motor and a Conveyor; completed steps equal clock pulse occurrences, checked 0.4 s after the eighth), each mechanism separately implemented and taught.

## 6. Open questions

**Resolved**

1. Triggers during a step: dropped, not queued. D19 (the spec's proposal); it is also the delivered retention's AlreadyReleased behaviour.
2. Over-capacity torque: the stop is rigid and has no capacity; `holding_capacity` is removed. Decided now: a stop that slips would skip or blur steps, against "discrete physical steps", and a torque-bounded stop could not catch a fast wheel inside the step tolerance.
3. Free-running (pendulum) mode: not built. D19: not in the requirement row.
4. SignalPropagation missing from the map binding: D14. `ActivationOut` (step completed) is not built. D19: no requirement row or lesson uses it; the stepped motion is the observable.
5. `tooth_count`: fixed at eight (45° steps). D13.
6. `drive_direction`: removed. Decided now: the stop holds both ways and a step goes the way the load pushes, so one part serves either rotation and the direction enum is not needed.
7. A release with no load is answered Empty and leaves the stop locked. Decided now: it is the requirement outcome, and it prevents a banked step when the drive arrives later.
8. The earlier 0.05 rad/s settle threshold for `ActivationOut` is kept only as the test's "step complete" speed. D1.

**Needs the owner**

None.

## 7. Integration

- **Collaborators.** Motor, Battery, Conveyor, Reverse gear, System clock, Impact switch, Counter (all delivered). EL-063 Winch with a Weight (not delivered) is the stored drive the Chrome recipe names; EL-061 Indexed carousel does not use this stop.
- **Campaign.** "Ratchet, escapement, indexed carousel": first use levels 81–90, reuse 91–100, 131–140, 146–150 (`docs/planning/invest/campaign-reservations.md:287`). The "hopper/escapement" of levels 21–30 (`:73`) is EL-241, not this part.
- **Lesson fixtures.** A level that places this part needs S8.
- **Order.** After EL-056, which shares the engine slice.

### Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of the identity's spec headed "Package source binding", read by `tools/Coverage/NamedPackageSources.cs` (on `origin/main`, `25df3cd5`; pattern: EL-239 Baseball). The section is not written here: the reader parses every EL spec that carries that heading and refuses one whose preconditions fail. **Blocked on the coverage follow-on story S7.**

| Field | Planned value |
| --- | --- |
| Package | `escapement` |
| Named identity | EL-057, `../invest/named-elements.md#element-057` |
| Acceptance | `todo-375` and `sequence-task-610` |
| Decision | S343; `scope-corrections.md` has no `s343` block |
| Mode records | `Configuration=Fixed` |

What it lacks today: an `s343` anchor block in `docs/planning/invest/scope-corrections.md`, and its todo-375 anchor is a scope index, not immediately followed by its sequence-task-610 anchor in `requirements.md`. S7 widens the reader (owner decision 15), including for companion packages and TH, RAD and GAP identities.

## 8. Delivery

- **Slice shape.** Single element, one package, after an engine slice.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which includes ENGINE-SUPPLY-DEMAND, ENGINE-ISLANDS-SLEEP and ENGINE-CONTACT-OBSERVATIONS (the Impact switch's contact trigger, when it is the pulse source); then ENGINE-JOINT-KINDS (the indexed revolute stop, extended as below); then S7 (the coverage follow-on, which widens the reader, owner decision 15). S3 and S4 reduce the edit list.
- **Story text.** ENGINE-JOINT-KINDS owns the one-way hold and the indexed stop on a revolute coordinate (owner decision 3a; `docs/planning/elements/readiness/engine-design-gap.md` section 6 item 9); `ENGINE-ASSEMBLY-JOINTS` keeps only player-made connections and commanded release. Its text must state the stop as this part needs it: "an indexed stop on a revolute coordinate (a rigid hold at a declared angular pitch that a physical Release request frees for exactly one step and that re-locks at the next index; a declared minimum carried torque below which a request is answered Empty; a request during a step answered AlreadyReleased; Reset clearing a pending request; usable on a coordinate carrying both `DriveIn` and `Drive` ports), with its physical-request target kind and its committed-released presentation driver".
- **Files added.** `elements/Escapement/Escapement.csproj`, `EscapementElementDeclaration.cs`, `escapement.tscn`, `icon.svg`; `CuriousContraptions.tests/EscapementTests.cs` and `EscapementArtTests.cs`.
- **Files edited.** No test-project edit (published main compiles every top-level `*.cs` by one rule). Until S3: `CuriousContraptions.slnx`, the trim list ending at `CuriousContraptions.tests/CSharpStaticGateTests.cs:703`, the placement switch at `CuriousContraptions.tests/PackageRuntimeParityTests.cs:641` (no parameter rows). At delivery: the binding section of section 7 appended to this spec. This spec and `TODO.md`.
- **Review tier.** The element slice is data-only only if the engine story publishes the indexed stop with tests first; planned today it is engine tier (one narrower and two missing capabilities).
- **Disqualifiers (the slice's probe re-checks).** (1) The engine story delivers only a one-shot release (no re-lock at the next index): the part cannot step, and the story is reopened. (2) The stop cannot catch a wheel arriving at 20 rad/s within 0.02 rad: lower the test's Motor speed only if a gameplay reason is recorded, otherwise it is an engine defect. (3) The Empty answer for an unloaded stop is not provided: row E3's second half fails and the requirement outcome is not met. (4) A second pulse arriving in the same tick as the first is counted twice.
