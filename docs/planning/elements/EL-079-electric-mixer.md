# EL-079 · Electric mixer — named-identity spec

**Current delivery (24 October 2026):** [EL-079 delivery slice](../invest/named-elements.md#element-079). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

**Build-ready revision, 22 November 2026 (checkout `8d1d68aa`), repaired the same day against the independent audit (`docs/planning/elements/readiness/audits/elements-batch-l.md`) and the owner decisions of 22 November 2026.** A `path:line` citation without a commit is in this checkout, and `path@a6c914e:L…` is the legacy harvest. Values marked **starting value** are adopted under decision D1 of [delivery batches](../invest/delivery-batches.md#3-family-level-decisions). Every number is a hand calculation; nothing was simulated. Game values are canonical f32.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-079 · Electric mixer · Specialist |
| Anchor | [requirements.md#element-079](../requirements.md#element-079); [named entry](../invest/named-elements.md#element-079); scope index [todo-218](../requirements.md#todo-218) (shared with EL-078); [sequence-task-632](../requirements.md#sequence-task-632) |
| Proof owner | S649 |
| Disposition | To deliver as a single element. It is not a variant or duplicate of the Motor: it has a bowl, a paddle that pushes cargo, a vertical shaft and no `Drive` port. The ledger also retains [CAT-042 Motor](CAT-042-motor.md) acceptance for the shared drive law; the rows that repeat it cite the delivered Motor tests. |
| Planned package | id `mixer`, directory `elements/Mixer/`, class `MixerElementDeclaration` |
| Refines / extends | Declaration shape of the Motor (`elements/Motor/MotorElementDeclaration.cs:16`) |
| Status | not started; blocked on the engine programme (owner decision 3a), which includes ENGINE-SUPPLY-DEMAND, then S7 |

## 2. Declaration

The package implements `IHingeElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:20`), `ISupplyElement` (`CuriousContraptions.Abstractions.Electrical/ElectricalContracts.cs:11`) and `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`), as the Motor does. `KinematicMode.Static` base, `SupportedDomains = Mechanical | Electrical`, footprint 2 bodies, 1 moving, 1 revolute constraint. The part origin is on the bench under the bowl's centre.

**What it does for a puzzle.** Balls dropped into the bowl stay there. A supplied paddle sweeps them round and pushes them over a low sill on one side. The "processed result" of the requirement is that physical exit, which any delivered sensor or receiver downstream can observe; the mixer itself reports nothing.

### Bodies and colliders

The bowl is a regular hexagon with its inner faces 0.85 m from the axis (apothem), 0.5 m deep. A hexagon, not a square, so that no ball can rest out of the paddle's reach (Behaviour). Five sides are walls; the side facing +X is a sill.

Static body 0, material restitution 0, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s (the Motor's rigid-part values, `MotorElementDeclaration.cs:63`):

| Collider | Shape and size (m) | Centre (m) | Turned about Y |
| --- | --- | --- | --- |
| floor | box 2.4 × 0.1 × 2.2 | (0, 0.05, 0) | 0° |
| five walls | boxes 0.1 thick (radial) × 0.5 high × 1.10 long | (0.90·cos θ, 0.35, 0.90·sin θ) for θ = 60°, 120°, 180°, 240°, 300° | each so its thickness lies along its radius |
| sill | box 0.1 × 0.1 × 0.98 | (0.90, 0.15, 0) | 0° |
| column | box 0.2 × 1.4 × 0.2 | (−1.05, 0.80, 0) | 0° |
| head | box 1.3 × 0.3 × 0.5 | (−0.5, 1.35, 0) | 0° |
| spindle | box 0.08 × 0.75 × 0.08 | (0, 0.825, 0) | 0° |
| lamp | sphere radius 0.06 | (−0.5, 1.53, 0) | — |
| socket | sphere radius 0.075 | (−1.15, 1.35, 0) | — |

Wall length: the outer face is 0.95 m from the axis, where a hexagon's side is 2 × 0.95 × tan 30° = 1.097 m, so 1.10 m boxes close every corner. The inner side is 2 × 0.85 × tan 30° = 0.98 m, the sill's length and the opening's width: a Basketball (0.68 m) passes with 0.15 m each side. The spindle is a box because the smallest admitted cylinder radius is 1/16 m (`CuriousContraptions.Abstractions/PhysicsDeclarationValues.cs:240`) and a 0.04 m rod would be refused; it runs from the head's underside (y = 1.2) to the paddle's top (y = 0.45) and is drawn as the paddle's shaft. The floor extends 0.25 m beyond the sill as an apron. The head covers only x from −1.15 to 0.15 and z within ±0.25, 0.6 m above the rim, so balls drop in freely on either side of it.

Hinge body 1, the paddle: one box 1.4 × 0.3 × 0.04 centred on the axis at y = 0.30 (from 0.15 to 0.45, 0.05 m above the floor). Mass 0.5 kg (**starting value**); moment about the axis 0.5 × (1.4² + 0.04²)/12 = 0.082 kg·m²; the other two by the box law. Material restitution 0.2, friction 0.5. The hinge's `BodiesCollide` is false.

### Hinge and drive

One `RevoluteHingeDescriptor` (`CuriousContraptions.Abstractions/MechanicalContractValues.cs:149`): support body 0; base frame at (0, 0.30, 0) turned −90° about X so the hinge's local Z is world +Y; `RevoluteTravel.Continuous`; stiffness 0, damping 0; `FrictionTorque` 0.05 N·m (the Motor's bearing, `MotorElementDeclaration.cs:100`); initial angle 0; drive `RevoluteDriveDescriptor` (`MechanicalContractValues.cs:138`) with target speed from `speed` and maximum torque from `torque`. Positive speed turns the paddle counter-clockwise seen from above.

### Parameters

| Key | Type, unit | Minimum | Maximum | Default | Editor policy | Run edit | Reason |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `speed` | Real, rad/s | 1 | 10 | 6 | Reject | RunControl (`RunControlLane.RevoluteDriveTarget`) | the Motor's default; adjustable during Run as the Motor's is |
| `torque` | Real, N·m | 1 | 12 | 8 | Reject | BuildOnly | bounded so the largest power is 12 × 10 = 120 W, the default Battery's limit |

Default power is at most 8 × 6 = 48 W, 0.4 J per tick at 120 Hz, inside the Battery's 120 W and 1 J per tick. The earlier draft's `direction` parameter is dropped (section 6).

### Ports, supply and stores

| `PortId` | Domain | Direction | Role | Glyph | Local position |
| --- | --- | --- | --- | --- | --- |
| `PowerIn` | Electrical | Input | `PowerIn` | Power | (−1.15, 1.35, 0), normal −X |

Supply graph: one consumer whose target is `SupplyDriveTarget(Revolute, 0)`, bound to `PowerIn`, the Motor's graph (`MotorElementDeclaration.cs:120`). Electrical node `motor`, `ElectricalRole.Consumer`, 12 V, 1.2 Ω, 0 F, `MaxPower` 120 W: a declared rating, read by nothing. The real debit is ENGINE-SUPPLY-DEMAND's, which precedes this part (owner decision 3a): the source is debited by the drive's committed work each tick and an exhausted source stops the drive (rows A10 and A11). No stores. A wire to `PowerIn` is the delivered thin non-solid wire (owner decision 1, 22 November 2026).

### Metadata and bindings

Category `Power`, display name "Electric mixer", palette order 342 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)), colour (0.40, 0.72, 0.79). Presentation: body binding `Visual/Paddle` → `PackageBodyIndex(1)`; `Visual/Lamp` albedo from slate `#556573` to gold `#f7cb52` over 0.1 s on `PresentationDriver.CommittedActuatorDrive`, the Motor's binding (`MotorElementDeclaration.cs:25`–`:28`).

### Art and solidity

| Node | Mesh | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Bowl` (floor, five walls, sill) | cream `#fff8e9` panels, the colliders' sizes | solid, one box each | nothing |
| `Visual/Column`, `Visual/Head` | cyan `#66b8c9` boxes | solid, one box each | nothing |
| `Visual/Spindle` | slate square rod | solid, box | nothing |
| `Visual/Paddle` | slate bar with gold end marks printed flat | solid, box on body 1 | body 1 pose |
| `Visual/Lamp` | sphere on the head | solid, sphere | colour by binding; it does not move |
| `Visual/Socket` | gold stud | solid, sphere | nothing |
| `Visual/Arrow` | turning arrow printed on the head's top | decorative: printed flat | nothing |

Icon: an open six-sided bowl seen from above with a bar across it and a curved arrow, and one low side. It differs from the Motor (a housing with a disc) by the bowl and bar.

### Behaviour and numbers

Laws: the supplied revolute velocity drive (torque-bounded, enabled while the `PowerIn` route is closed), rigid contact and friction. Supply reaches the drive in the electrical phase and the paddle turns in the physics step of the same tick ([solver model](../../gpu-f32-physics.md#solver-model)).

**Reach: no dead corner.** The paddle's tips sweep a 0.70 m radius. Against a flat wall a ball's nearest point to the axis is 0.85 − 2r: 0.35 m (Tennis, r 0.25), 0.29 m (Bowling, r 0.28), 0.17 m (Basketball, r 0.34). In a 120° corner the ball's centre sits r/sin 60° inside the vertex, which is 0.85/cos 30° = 0.981 m out, so its nearest point is 0.981 − r/sin 60° − r: 0.443 m (Tennis), 0.378 m (Bowling), 0.248 m (Basketball). Every figure is below 0.70 m, so the paddle meets every ball wherever it rests. The paddle touches a ball with its flat face, never its end: the contact lies √(ρ² − (r + 0.02)²) along the bar, at most 0.638 m (a Tennis ball in a corner, ρ = 0.693 m), inside the 0.70 m half-length. With the earlier square bowl a Tennis ball in a corner was 0.60 m from the axis and a 0.50 m paddle never reached it; that is the defect the audit found.

**Paddle height.** It spans 0.15 to 0.45 m; ball centres rest at 0.35 (Tennis), 0.38 (Bowling) and 0.44 m (Basketball), all inside it.

**Exit: a quasi-static push over the sill.** The drive is torque-bounded, so the argument is by force, not speed. The paddle's face pushes a ball at right angles to the bar with P = torque ÷ (contact distance along the bar). A ball swept counter-clockwise arrives at the sill side from its upstream corner, where the bar lies behind it at an angle β from the sill's normal and the push has an outward part P·sin β. Lifting a ball of mass m and radius r over a step h needs a horizontal push of m·g·√(r² − (r − h)²)/(r − h).

| Ball | Needed over 0.1 m | At the upstream corner: ρ, contact distance, β | Outward push at torque 8 | Margin |
| --- | --- | --- | --- | --- |
| Tennis (0.35 kg) | 0.35 × 9.81 × 0.20/0.15 = 4.6 N | 0.693 m, 0.638 m, 30° + asin(0.27/0.693) = 52.9° | (8/0.638) × sin 52.9° = 10.0 N | 2.2 |
| Basketball (1 kg) | 9.81 × 0.241/0.24 = 9.8 N | 0.589 m, 0.466 m, 30° + asin(0.36/0.589) = 67.7° | (8/0.466) × sin 67.7° = 15.9 N | 1.6 |
| Bowling (4 kg) | 39.2 × 0.2145/0.18 = 46.8 N | 0.658 m, 0.586 m, 30° + asin(0.30/0.658) = 57.1° | (8/0.586) × sin 57.1° = 11.5 N | 0.25: it stays in |

Friction at the sill and the floor is neglected; the margins are what allow for it. At `torque` 12 the Bowling ball is still pushed with only 17 N and stays in the bowl; it is the "too heavy to process" case, not a stall.

**Low-torque boundary.** The Tennis ball needs 4.6 N, which is torque 3.7 N·m; the Basketball needs torque 5.0 N·m. At `torque` 2 and `speed` 1 neither law ejects anything: the push is 2.5 N at most and the fastest ball moves 0.7 m/s, below the 1.18 m/s (0.7·v² = 9.81 × 0.1) a rolling ball needs to carry a 0.1 m step.

**Unpowered.** A ball dropped in has no sideways push and cannot cross the sill. **Stall.** A Wall standing in the bowl across the paddle's path stops it; a stalled drive does no work (delivered law), so nothing behind the Wall is moved. **Spin-up.** 8 N·m on 0.082 kg·m² reaches 6 rad/s in 0.06 s.

## 3. Engine capabilities

Map row families: ContactImpulse, EnvironmentState, FiniteLedger, FiniteWorkActuation, GeometryQuery, JointConstraint, RigidBodyDynamics, SignalPropagation, StateTransaction, TypedContracts (navigation only, D14; the row's ProgrammableController and TimedCommand are not used).

| Capability | Status | Evidence |
| --- | --- | --- |
| Supplied continuous revolute drive on a static base, torque-bounded, holding its target speed | present | `MechanicalContractValues.cs:138`; `RevoluteDriveTests.ASuppliedDriveSpinsTheRotorUpAtItsTorqueBoundThenHoldsTheTargetSpeed` (`CuriousContraptions.tests/RevoluteDriveTests.cs:176`); `MotorTests.ASuppliedMotorSpinsUpToItsSpeedHoldsItAndPaysOnlyItsBearingLoss` (`CuriousContraptions.tests/MotorTests.cs:217`) |
| A stalled drive does no work | present | `RevoluteDriveTests.AStalledDriveDoesNoWorkWhileItsShaftIsBlocked` (`RevoluteDriveTests.cs:307`); `MotorTests.AStalledMotorDoesNoWorkOnceItsShaftIsHeld` (`MotorTests.cs:732`) |
| No supply, no motion and no work | present | `MotorTests.AnUnpoweredMotorStaysStillDoesNoWorkAndKeepsItsLampSlate` (`MotorTests.cs:294`) |
| Target speed as a run-time control | present | `ParameterRunEdit.RunControl` (`CuriousContraptions.Abstractions/ParameterDescriptor.cs:44`); `MotorTests.ASpeedChangeDuringRunDrivesTheShaftToTheNewSpeedFromTheNextCommittedStep` (`MotorTests.cs:397`) |
| A Signal wire into `PowerIn` is refused | present | `MotorTests.AnActivationWireIntoThePowerInputIsRefused` (`MotorTests.cs:316`) |
| A driven hinge about a vertical axis, through a package | present | `MotorTests.ARotatedMotorSpinsAboutItsTurnedAxisAtTheSameSpeed` (`MotorTests.cs:792`): a Motor turned so its axis is vertical holds its speed about that axis; frames: `JointFrameTests.RotatedHingeMovingFrame_ChildOrientationMatchesInverseMovingFrame` (`CuriousContraptions.tests/JointFrameTests.cs:130`) |
| Art follows the hinge body; lamp follows the committed drive | present | `MotorTests.TheRotorArtFollowsTheCommittedRotorPoseAndResetsToItsScenePose` (`MotorTests.cs:1075`) |
| A driven hinge body that pushes free cargo by its flat face | present, untested | every delivered driven rotor carries a rim or a belt, not a paddle; contact between a hinge body and a ball is the general contact law, with no test of this arrangement |
| Drive work debits its source; an exhausted source stops the drive | **missing** | ENGINE-SUPPLY-DEMAND (owner decision 3a; `docs/planning/elements/readiness/engine-design-gap.md`, section 6 item 7); it precedes every element |

## 4. Sources and legacy

- **Requirement row.** [element-079](../requirements.md#element-079): a supplied rotating tool moves material through actual torque and contact; stall or missing contents cannot create a processed result.
- **Research reference.** The inventory cited by [todo-218](../requirements.md#todo-218).
- **Legacy search.** `git grep -i "mixer"` at a6c914e over every Epic 7 deletion path returned nothing. [CAT-042](CAT-042-motor.md) holds the full motor harvest.

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| L1 | Motor catalogue defaults: speed 6 rad/s, torque 20 | `parts/catalog/motor.tres@a6c914e:L14-L14` | Carry forward the speed default; torque is 8 N·m here so the power stays inside one Battery |
| L2 | Motor: electricity turns its shaft | `parts/catalog/motor.tres@a6c914e:L11-L11` | Carry forward the supply rule |
| L3 | Positive actuator work is bounded by available work | `engine/physics/PoweredImpulse.cs@a6c914e:L7-L26` | Carry forward the rule; the delivered drive tests prove it. Do not carry forward the CPU implementation |

**Files harvested:** `parts/catalog/motor.tres`, `engine/physics/PoweredImpulse.cs` (generic rule only).

## 5. Acceptance outline

Requirement row: [element-079](../requirements.md#element-079). Planned test class `MixerTests`. "In the bowl" means a ball's centre is less than 0.85 m from the axis; "out" means its centre has x above 1.3 m.

| # | Given / When / Then | Planned test | Control | Boundary and tolerance |
| --- | --- | --- | --- | --- |
| A1 | Given a Battery on `PowerIn` and two Tennis balls and a Basketball dropped into the bowl, one of them resting in a corner, when Run proceeds, then every ball is out | `ASuppliedPaddlePushesTheBallsOutOverTheSill` | A2 | all out inside a 15 s guard |
| A2 | Given the same balls and no supply (no battery, disabled battery, unwired battery), then every ball stays in the bowl and the paddle's drive does no work | `WithoutSupplyNothingLeavesTheBowl` (theory) | A1 | all in for 15 s; committed drive work 0 |
| A3 | Given supply and an empty bowl, then a Ball detector at the opening never fires and a Receiver beyond it captures nothing | `AnEmptyBowlProducesNoResult` | A1 with the same detector fires once per ball | no occurrence in 15 s |
| A4 | Given a Wall 0.4 × 0.4 × 0.12 m (width, height, thickness: the Wall's minimum sizes) resting on the bowl floor across radius 0.2–0.6 m, so it stands in the paddle's path, and a Tennis ball beyond it, then the paddle stops at the Wall, the drive's work stops growing and the ball stays in | `AStalledPaddleMovesNothing` | A1 | paddle angle changes by at most 0.05 rad over 5 s after contact; work growth at most 1 % of torque × speed × time (the delivered stall bound) |
| A5 | Given `speed` 1 and then `speed` 6 at the default torque, then the balls leave later at `speed` 1 than at `speed` 6, or not at all | `ASlowerPaddleEjectsNoSooner` | — | ordering only; no time is asserted |
| A6 | Given `torque` 2 and `speed` 1, then the balls are pushed round the bowl and none leaves | `AWeakSlowPaddleStirsButDoesNotEject` | A1 | all in for 20 s; each ball's centre moves more than 1 m; the push is at most 2.5 N against the 4.6 N the lightest ball needs |
| A7 | Given a Bowling ball and default settings, then it is pushed round and stays in the bowl | `ABallTooHeavyForTheSillStaysIn` | A1 | in for 15 s; 11.5 N against 46.8 N |
| A8 | Given `speed` changed from 6 to 1 during Run, then the committed target follows from the next step; Reset and a save keep the authored 6 | `ASpeedChangeDuringRunTakesEffectAndIsNotSaved` | — | the Motor's run-control rows are the law's proof |
| A9 | Parameter refusal: `speed` 0.5 and 10.5, `torque` 0.5 and 12.5 and non-finite values are refused at every seam; the four bounds are admitted | `OutOfRangeParametersAreRefused` (theory) | the bounds | exact bounds |
| A10 | Given A1, then the Battery's debit equals the drive's committed work | `TheBatteryPaysForThePaddlesWork` | A2 debits nothing | debit never below the committed work and above it by no more than the ledger residual |
| A11 | Given a 60 J Battery (its minimum capacity) at `initial_charge` 0.002 (0.12 J) on `PowerIn` and A1's balls, then the drive stops doing work when the source is exhausted, the paddle coasts to rest and no ball leaves | `AnExhaustedSourceStopsThePaddle` | A1 | the drive's committed work stays at or below 0.12 J; no ball leaves for 10 s after the paddle comes to rest. Hand check: 0.12 J spins the paddle to at most √(2 × 0.12 ÷ 0.082) = 1.7 rad/s; a Tennis ball struck at radius 0.5 m (paddle effective mass 0.082 ÷ 0.5² = 0.33 kg, restitution 0.2 × 0.85 = 0.17) leaves at most 1.17 × 0.33 ÷ 0.68 × 0.86 = 0.49 m/s, under the 1.18 m/s needed to carry the 0.1 m sill, so a correct build keeps every ball in; an exhausted source that kept driving would eject them as in A1 |
| A12 | Reset restores the paddle angle, drive state, lamp, charge and every ball; save and load keep parameters, pose and the wire | `ResetAndSaveLoadRestoreTheMixer` | — | generic row: `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad` (`CuriousContraptions.tests/CatalogueIntegrationTests.cs:273`) |

Wrong-kind connection: a Signal output offered to `PowerIn` is refused, proved for this declaration shape by `MotorTests.AnActivationWireIntoThePowerInputIsRefused` and generically by S4 when it lands; `MixerTests.AnActivationWireIntoThePowerInputIsRefused` repeats it for this package. Every row fits the default 30 s Run length.

### Integration

- Supply: Battery, and any switched supply (Impact switch, State latch, Counter, Hold timer, gates). Cargo: Tennis ball, Basketball, Bowling ball. Observers downstream: Ball detector, Receiver, Pressure plate. All delivered.
- Campaign: chapter 10 "Oddly Satisfying", levels 91–100, "opener/mixer" (`docs/planning/requirements.md:2410`, chapter row `:2295`); reuse 111–120, 136–150.
- A level that fixes the mixer in place needs the lesson-fixture story S8.

### Delivery

- **Shape.** Single element slice.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which includes ENGINE-SUPPLY-DEMAND; no further engine story; then S7. S3 preferred.
- **Files.** `elements/Mixer/` (project, declaration, `mixer.tscn`, `icon.svg`); `CuriousContraptions.tests/MixerTests.cs`; one `CuriousContraptions.slnx` line; the binding section once S7 admits it; until S3, the remaining per-package lists.
- **Form.** Port roles, node arity, target masks and mouth identities are written in the form ENGINE-CAPABILITY-INSTANCES leaves; the names here are today's. This part uses the single-driven-hinge run control (`RunControlLane.RevoluteDriveTarget`), which that story replaces by controls named by local id.
- **Review tier.** Engine tier because of the one *present, untested* row; data-only if the probe's fixture shows a driven paddle pushing a ball.
- **Disqualifiers the probe re-checks.** A dropped ball that bounces over the sill unpowered (then the sill rises and the push table is recalculated); friction at the sill large enough to hold a Basketball at the default torque (margin 1.6); the Battery's default limit of 120 W; placement of a part inside the bowl is admitted (A4's Wall on the bowl floor must not meet the placement refusal `IntoPart`, `engine/PlacementPose.cs:16`).

## 6. Open questions

All resolved.

1. **"Processed" is physical.** The result is the balls' exit from the bowl, observed by ordinary sensors. No material-state flag exists or is added. Decided under the standing delegation: it gives the part a use (a powered release for cargo) without any outcome command.
2. **The bowl is part of the mixer** (one placed part), and it is six-sided so that every resting place is inside the paddle's sweep (audit H7).
3. **`direction` dropped** (D19): the sill is placed for one direction of rotation; turning the part serves the other layouts.
4. **Speed, torque, sizes and masses** are starting values (D1). The torque range is narrower than the Motor's so the declared power never exceeds one default Battery.
5. **No stall cue.** The lamp shows supply, as the Motor's does; a stalled paddle is visibly still.

Needs the owner: none.

## Engine conformance (22 November 2026)

Checked against the owner addendum of 22 November 2026 (AGENTS.md element-free engine gate and Tri-Graph rule; `_bmad/custom/element-free-engine-design.md`; `docs/gpu-f32-physics.md`).

1. **Declaration data only.** A static frame, one hinge-carried paddle with a declared drive, one supply consumer, one port, two parameters and two presentation bindings. No solver branch, callback or "mixed" state.
2. **Generic laws.** The supplied revolute velocity drive within its torque bound; contact and friction between paddle, balls, floor, walls and sill; the supply ledger's debit of drive work. Ejection is the consequence of the paddle's contact force against the step-climb force of the sill.
3. **Three layers.** Colliders: twelve static primitives and the paddle box. Visuals: one mesh per collider, with flat printed marks. Animation: the lamp's colour from the committed drive state; the paddle's art follows body 1's pose.
4. **Flow and typing.** One Electrical input with role `PowerIn`; `ParameterRunEdit`, `RunControlLane` and `RevoluteTravel` are enums; values are f32.
5. **Flat state.** Bodies 2 (1 static, 1 dynamic); colliders 13 (boxes 11, spheres 2); joints 1 revolute; drives 1; sensors 0; domain nodes: 1 supply consumer and 1 electrical node; stores 0; presentation bindings 4 (one body, three colour channels).
6. **Performance.** Thirteen colliders against the Motor's few. Reason: the bowl is a floor, five walls and a sill, six-sided to close the dead corners a square bowl has, and the column, head, spindle, lamp and socket are visible solids. Twelve are static. The paddle is one box on one body; a powered hinge does not sleep, and nothing else here relies on sleeping, which does not exist until ENGINE-ISLANDS-SLEEP.
7. **Tests are scenarios.** Every row runs the production simulation with delivered balls, supply and sensors.

**Verdict: conforms.**

## 7. Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of the identity's spec headed "Package source binding", read by `tools/Coverage/NamedPackageSources.cs` (pattern: EL-239 Baseball). The section is not written here: the reader parses every spec that carries that heading and fails the whole coverage run when a field does not resolve. **Blocked on the coverage follow-on story S7.**

| Field | Planned value |
| --- | --- |
| Package | `mixer` |
| Named identity | EL-079, `../invest/named-elements.md#element-079` |
| Acceptance | `todo-218`, `sequence-task-632` (`docs/planning/requirements.md:5029`) |
| Decision | S649, `../invest/scope-corrections.md#s649` |
| Mode records | `Configuration=Fixed` |

What it lacks today: `scope-corrections.md` has no `s649` block; `todo-218` is a scope index shared with EL-078, and `sequence-task-632` is anchored beside the element row, not beside that todo, which is the pairing the reader expects.
