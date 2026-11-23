# GAP-04 · Driven wheel — named-identity readiness spec

**Current delivery (24 October 2026):** [GAP-04 delivery slice](../invest/named-elements.md#gap-04). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Rewritten on 22 November 2026 as one current text against checkout `spec-drafts` (published main `8d1d68aa`). Code citations are `path:line` in that checkout; `path@a6c914e` citations appear only in section 4. Nothing was built or run: every number is a hand calculation shown here and a starting value under decision D1 of [delivery batches](../invest/delivery-batches.md#3-family-level-decisions).

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | GAP-04 · Driven wheel · Mechanical (P2 potential; adopted for implementation by the 24 October 2026 owner decision) |
| Anchor | [requirements.md#gap-04](../requirements.md#gap-04) (task `sequence-task-537`); [named-elements entry](../invest/named-elements.md#gap-04); decision block [S638](../invest/scope-corrections.md#s638) |
| Owner key | S638 |
| Related identities | [EL-116 Passive wheel](EL-116-passive-wheel.md) and [EL-117 Axle](EL-117-axle.md) (same wheel, no drive); [EL-112 Structural beam](EL-112-structural-beam.md) (chassis); delivered Motor (`motor`), Battery (`battery`), Conveyor (`conveyor`, the `DriveIn` pattern). No CAT refines it. |
| Status | Not started; blocked on the engine programme (owner decision 3a), then `ENGINE-JOINT-KINDS` and ENGINE-ASSEMBLY-JOINTS (section 3). |

### Disposition

**To deliver as a single element** after the engine programme (owner decision 3a: every element is blocked on the engine programme), `ENGINE-JOINT-KINDS` and ENGINE-ASSEMBLY-JOINTS. Planned package id `driven_wheel`, directory `elements/DrivenWheel/`, display name "Driven wheel". It is an element, not a duplicate of the Motor: it has a ground-contact wheel, a dynamic base that rides a chassis and attachment sockets. It is not a variant of EL-116: it adds a supplied drive and two ports.

## 2. Declaration

### Declaration to write

Follows the delivered Motor (`elements/Motor/MotorElementDeclaration.cs:17`): a base body carrying a rotor on a continuous revolute hinge with a supplied velocity drive, fed through `PowerIn`. The differences are a dynamic base, a ground-contact cylinder on the rotor, a `DriveIn` port and attachment sockets. The attachment model and the rule for which assembled bodies collide are the ones stated in [EL-112](EL-112-structural-beam.md), section 2.

- **Interfaces.** `IHingeElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:20`), `ISupplyElement` (`CuriousContraptions.Abstractions.Electrical/ElectricalContracts.cs:11`), `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`), and the attachment-socket contract ENGINE-ASSEMBLY-JOINTS delivers. `KinematicMode.Dynamic`.
- **Body 0, the pod (dynamic).** Box collider `col_pod`, half extents (0.125, 0.125, 0.10) m at the body origin. Mass 0.4 kg; box moments m(b² + c²)/12: about X and Y 0.4 × (0.0625 + 0.04)/12 = 0.00342 kg·m², about Z 0.4 × (0.0625 + 0.0625)/12 = 0.00417 kg·m². Gravity (0, −9.81, 0); linear drag 0. Material: restitution 0, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s (the Motor housing's values, `:59`).
- **Body 1, the wheel (dynamic, on hinge 0).** Cylinder collider `col_wheel`, radius 0.30 m, half width 0.10 m, axis along the hinge's Z, centre (0, 0, 0.21) in the pod frame (0.10 pod half depth + 0.01 clearance + 0.10). Mass 0.5 kg; solid-cylinder moments as the Motor computes them (`:98-102`): axial m·r²/2 = 0.5 × 0.09/2 = 0.0225 kg·m², transverse m(3r² + w²)/12 = 0.5 × (0.27 + 0.04)/12 = 0.0129 kg·m². Material: restitution 0.1, friction 0.8, rolling resistance 0.03, bounce threshold 0.1 m/s: exactly the size, mass and tread values of the Passive wheel ([EL-116](EL-116-passive-wheel.md)), so a driven and a passive wheel differ only in supply.
- **Hinge 0.** `RevoluteTravel.Continuous`, base frame (0, 0, 0.21), `BodiesCollide: false`, stiffness 0, damping 0, friction torque 0.02 N·m (a wheel coasts; the Motor's 0.05 is a spin-down value). Drive: `TargetSpeed` from `speed`, `MaxTorque` from `torque` (`RevoluteDriveDeclaration`, `CuriousContraptions.Abstractions/RevoluteConstraintValues.cs:28`), supplied while the `PowerIn` route is closed. Unsupplied, the drive row's bound is zero and the wheel free-wheels.
- **Parameters.**

  | Name | Kind | Unit | Default | Min | Max | Editor policy |
  | --- | --- | --- | --- | --- | --- | --- |
  | `speed` | Real | rad/s | 8 | −20 | 20 | run control, as the Motor's `speed` (`:39-40`); negative reverses |
  | `torque` | Real | N·m | 2.5 | 0.1 | 100 | build-only; player-editable unless locked (D11) |

  The 2.5 N·m default is chosen so that the reference vehicle's driven wheels grip rather than spin (section 5).
- **Ports.**

  | `PortId` | Domain | Direction / role | Position (pod frame) | Notes |
  | --- | --- | --- | --- | --- |
  | `PowerIn` | Electrical | Input / `PowerIn` | (0, 0.125, 0), facing +Y | feeds the one supply consumer, which targets revolute 0 |
  | `DriveIn` | Mechanical | Input / `DriveIn` | (0, 0, 0.32), facing +Z | `Coordinate` revolute 0, `CoordinatePerRadian` +1, rim radius 0.12 m on the wheel's hub |

  Both ports exist on every instance and neither is a mode: an unconnected `PowerIn` leaves the drive unsupplied, and a mechanical connection couples the coordinate as on the Conveyor (`elements/Conveyor/ConveyorElementDeclaration.cs:44`). The mechanical connection is the delivered rigid ratio coupling, which stays the default (owner decision, 22 November 2026); a belt or chain is a material the player may later choose for it, not something this part declares.
- **Attachment sockets (ENGINE-ASSEMBLY-JOINTS).** Two **rigid mounts** (fixed joints) on body 0: `mount_back` at (0, 0, −0.10), the centre of the face opposite the wheel, and `mount_side` at (−0.125, 0, 0), the centre of the −X face. Neither shares a position with a port: `PowerIn` is alone on the top face.
- **Supply graph and electrical node.** One consumer with one `SupplyDriveTarget` on revolute 0; one consumer node rated at torque × speed = 100 × 20 = 2000 W at 12 V, the Motor's pattern (`:119-127`).
- **Stores.** None. Work comes from the supply or the shaft; coasting spends kinetic energy only.
- **Presentation bindings.** `Visual/Lamp` albedo slate `#556573` to gold `#f7cb52` over 0.1 s ← `PresentationDriver.CommittedActuatorDrive` (three channel bindings, as the Motor, `:25-27`); `Visual/Wheel` follows body 1's pose (a moving root, not a binding).
- **Palette.** Category `ElementCategory.Power` (with the Motor). Order: 308 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)).

### Art and solidity

| Art node | Mesh | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Pod` cyan `#66b8c9` | box 0.25 × 0.25 × 0.20 | solid, `col_pod` | body 0 pose |
| `Visual/Wheel` cream disc `#fff8e9` with wood tread `#c28f52` | cylinder r 0.30, width 0.20 | solid, `col_wheel` | body 1 pose |
| `Visual/Wheel/Spoke` gold `#f7cb52` | bar printed flush on both wheel faces | decorative: printed flat; shows spin because it rides body 1 | body 1 pose |
| `Visual/Collar` gold | ring printed flush on the pod's wheel-side face | decorative: printed flat (a proud collar would need its own collider in a 0.01 m gap) | body 0 pose |
| `Visual/Lamp` | bolt-shaped mark printed flush on the pod top | decorative: printed flat, coloured by a binding | presentation binding |

Icon: a wheel with a square pod behind it and a bolt mark. It differs from the Passive wheel by the pod silhouette and the bolt-shaped mark, not by colour alone. The supply wire is a thin wire and stays a non-solid drawing (owner decision, 22 November 2026).

## 3. Engine capabilities

### Capability probe

| Capability | State | Symbol | Exercising test (body read), or owner |
| --- | --- | --- | --- |
| Supplied velocity drive on a continuous hinge, bounded by a declared torque | present | `RevoluteDriveKind.Supplied`, `CuriousContraptions.Abstractions/RevoluteConstraintValues.cs:22` | `RevoluteDriveTests.AnOverloadedDriveIsBackDrivenAndAddsNoWork` |
| A stalled drive does no work | present | same | `RevoluteDriveTests.AStalledDriveDoesNoWorkWhileItsShaftIsBlocked` |
| An unsupplied drive applies nothing; cutting supply leaves the rotor coasting under hinge friction | present | same | `RevoluteDriveTests.AnUnsuppliedDriveAppliesNoImpulseAndDoesNoWork`; `MotorTests.CuttingTheSupplySpinsTheRotorDownUnderBearingFrictionWithNoFurtherWork` |
| A mechanical input coupled to a revolute coordinate | present | `PortId.DriveIn` with `CoordinateReference`, `elements/Conveyor/ConveyorElementDeclaration.cs:44` | `ConveyorTests.ChainedConveyors_DriveRelaysRotationToDownstreamConveyor` |
| Rolling and slipping of a cylinder under Coulomb friction | present | `ColliderShapeKind.Cylinder`, `CuriousContraptions.Abstractions/PhysicsDeclarationValues.cs:12` | `WorkshopPhysicsLawCylinderTests.ACylinderRollsDownARampAtTwoThirdsGSinThetaWithoutSlipping`; `.BelowTheNoSlipFrictionTheCylinderSlipsFasterThanItRolls` |
| A hinge whose support body is dynamic | present, untested | `RevoluteHingeDescriptor.Support`; all eleven delivered hinge packages have a static base, and no refusal of a dynamic base was found in `CuriousContraptions.Bundle/BundleValidator.cs` | `ENGINE-JOINT-KINDS` ("every joint kind is qualified with a dynamic support body as well as a static one") |
| A hinge-carried cylinder driving its base along the ground through friction | present, untested | the two rows above combined | row W1, after `ENGINE-JOINT-KINDS` |
| An electrical connection to a port on a moving body | present, untested | connections are topological (`engine/gpu/WorkshopConnections.cs`) | row W1 |
| A mechanical input coupling a coordinate that also carries a supplied drive (here unsupplied, so its bound is zero) | present, untested: the Conveyor's `DriveIn` coordinate has no drive of its own | `MechanicalTransmissionDescriptor` with the drive row on one coordinate | row W5; if it is refused or the two rows fight, the finding goes to `ENGINE-JOINT-KINDS` planning |
| A fixed joint between two bodies | **missing** | constraint kinds are revolute, prismatic and tensile (`CoordinateKind`, `CuriousContraptions.Abstractions/PortDescriptor.cs:43`) | `ENGINE-JOINT-KINDS` |
| Mounting the pod on another placed part at a socket | **missing** | `AnchorPointDescriptor` is declared and never compiled, and its `Name` is a string (`CuriousContraptions.Abstractions/MechanicalContractValues.cs:63`) | ENGINE-ASSEMBLY-JOINTS (typed socket id, role enum and the connection) |
| Drive work debited from the source | **missing** | supply to a drive is on or off (`elements/Motor/MotorElementDeclaration.cs:119`) | `ENGINE-SUPPLY-DEMAND` (sprint key `engine-supply-drive-demand`), part of the engine programme |

Counts: present 5, untested 4, narrower 0, missing 3. Owners: the fixed joint and the dynamic-support qualification are `ENGINE-JOINT-KINDS`; mounting is ENGINE-ASSEMBLY-JOINTS; the debit is `ENGINE-SUPPLY-DEMAND`, needed by one later row only. Every row also waits for the engine programme (owner decision 3a). Nothing needs code that names this part.

## 4. Sources and legacy

- **Requirements.** "Supply drives a loaded vehicle; missing supply produces no new work, an overloaded drive stalls and a low-friction wheel slips. Coasting comes only from stored motion. Verify connected drivetrain and driven-wheel Reset independently of GAP-03" ([gap-04](../requirements.md#gap-04)).
- **Decision block.** "Freeze the drive input mode and finite supplied-work boundary from the source; one wheel/chassis start, stop and reversed drive … No passive axle is silently powered" ([S638](../invest/scope-corrections.md#s638)).
- **Audit.** "GAP-03 and supported torque/load model. Powered travel, power-loss/coasting and overloaded/stalled controls" (`docs/physics-puzzle-gap-audit.md@a6c914e:L83-L83`).

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | Motor supply model: target speed, maximum torque only while powered, work budget torque × speed × Δt. | `parts/MotorPart.cs@a6c914e:L91-L97` | Carried by the delivered supplied revolute drive; the per-part `PreparePhysics` loop does not carry forward. |
| 2 | Motor speed 0 to 20 rad/s, torque up to 100 N·m, out-of-range rejected. | `parts/MotorPart.cs@a6c914e:L55-L63` | Carry forward the magnitudes; this part adds the sign. |
| 3 | Mechanical sockets were clockwise-positive about the hinge +Z. | `parts/MotorPart.cs@a6c914e:L54-L54` | Do not carry forward: the delivered convention is the port's declared `CoordinatePerRadian`. |

Files consulted: `parts/MotorPart.cs`.

## 5. Acceptance outline

Point of truth: [gap-04](../requirements.md#gap-04). Chrome proof through real UI controls is pending the post-catalogue phase.

### Behaviour and numbers

Laws: the supplied revolute drive law (a velocity row bounded by ±`torque`·h per substep), Coulomb contact with pair friction √(μ₁μ₂), rolling resistance (the larger of a pair's two coefficients, `CuriousContraptions.Simulation/WorkshopConstraintSolver.cs:231`), and the fixed joints of the engine stories.

- **Reference vehicle (it stands on four wheels).** One 2 m Structural beam (1.5 kg, [EL-112](EL-112-structural-beam.md)) lying along X with its centre 0.30 m above the ground.
  - At `EndB`: one Axle (0.5 kg, [EL-117](EL-117-axle.md)) with its `Mount` on the beam's end point and a Passive wheel (0.5 kg) on each hub. Its axle line is 0.15 m beyond the beam end, at x = +1.15 m.
  - At `EndA`: two Driven wheels (0.9 kg each), one on each side, both `mount_back` sockets on the beam's `EndA` point (two mounts may share one point). Each pod then lies between z = 0 and ±0.20 m and its wheel between ±0.21 and ±0.41 m, 0.06 m outside the beam's 0.30 m depth (z = ±0.15). The wheel axes pass through the pod centres, on the beam's centre line at x = −1.0 m. The second pod is the first turned half a turn about the vertical, so its hinge axis is reversed and it runs at `speed` −8 to drive the same way.
  - On the beam's middle: a 4 kg Weight, standing (0.58 m tall, radius 0.35 m).
  - What overlaps: each pod overlaps the beam end and the other pod. Both pairs are joined at one point, so they do not collide ([EL-112](EL-112-structural-beam.md), section 2). The wheels, the Axle's shaft and the Weight overlap nothing: the shaft (radius 0.0625 m, [EL-117](EL-117-axle.md)) is on the axle line 0.15 m beyond the beam end, 0.15 − 0.0625 = 0.0875 m clear of it.
- **Mass and axle loads.** 1.5 + 0.5 + 2 × 0.5 + 2 × 0.9 + 4 = 8.8 kg, weight 8.8 × 9.81 = 86.3 N, wheelbase 1.0 + 1.15 = 2.15 m. Moments about the driven axle line: beam and Weight, 5.5 kg at 1.0 m; Axle and its wheels, 1.5 kg at 2.15 m: 5.5 + 3.225 = 8.725 kg·m. The passive axle carries 8.725/2.15 = 4.06 kg (39.8 N); the driven pair carries 8.8 − 4.06 = 4.74 kg (46.5 N), 23.3 N on each driven wheel.
- **Rim force.** 2.5 N·m / 0.30 m = 8.3 N per driven wheel, 16.7 N for the pair. **Traction limit** on a Ramp surface (friction 0.3, `elements/Ramp/RampElementDeclaration.cs:56`): √(0.8 × 0.3) = 0.490; 0.490 × 23.3 = 11.4 N per wheel. 8.3 < 11.4, so the default wheels grip with 27 % in hand. (At 4 N·m the rim force would be 13.3 N and the wheels would spin, which is why the default is 2.5.)
- **Rolling drag.** 0.03 × 86.3 = 2.59 N.
- **5° slope.** Needs 86.3 × sin 5° + 2.59 = 7.52 + 2.59 = 10.1 N; 16.7 N climbs it. **15° slope.** Needs 86.3 × 0.2588 + 2.59 = 24.9 N; 16.7 N does not.
- **The climb.** The Ramp is a slab 0.18 m thick (`elements/Ramp/ramp.tscn:9`), so the vehicle cannot drive onto it from the bench: a 0.30 m wheel meeting a 0.18 m step has cos α = (0.30 − 0.18)/0.30 = 0.4, tan α = 2.29, and the leading passive axle's 39.8 N needs about 39.8 × 2.29 = 91 N of push against the 16.7 N available. Row W1 therefore starts the vehicle at rest wholly on a 4 m Ramp (the Ramp's longest, `elements/Ramp/RampElementDeclaration.cs:27`) at 5°, passive axle uphill, its driven axle 0.35 m up the slab so the 0.30 m driven wheels lie wholly on it; the passive axle is then 0.35 + 2.15 = 2.50 m up the slab, with 1.50 m of slab ahead of it. Raising the Weight 0.08 m takes 0.08 / sin 5° = 0.92 m along the slab, at (16.7 − 10.1)/9.8 = 0.67 m/s²: about √(2 × 0.92 / 0.67) = 1.7 s, reaching 1.1 m/s, under the 2.4 m/s top speed. The passive axle then stands 3.42 m up the slab and its wheel's leading edge at 3.72 m, short of the 4.0 m end. Unwired on the same slab the vehicle rolls back at about (7.52 − 2.59)/9.8 = 0.50 m/s². On a 15° slab the drive's 16.7 N is short of the 24.9 N needed, so it rolls back at about (22.3 − 16.7 − 2.59)/9.8 = 0.31 m/s² and its Weight loses height; the Weight still stays on the beam there ((9.81 × sin 15° − 0.31)/(9.81 × cos 15°) = 0.24 < 0.39).
- **Top speed.** 8 rad/s × 0.30 m = 2.4 m/s. From rest on the level: (16.7 − 2.59) N over 9.8 kg (8.8 kg plus four wheels' rotary share 4 × 0.0225/0.09 = 1.0 kg) = 1.4 m/s², so 2.4 m/s after 1.7 s.
- **Stall.** Against a Wall the rim force 8.3 N is below the 11.4 N traction limit, so the wheels stop turning rather than spinning; a stalled drive does no work.
- **Slip.** With the treads treated by the Coating station (friction 0.05): √(0.05 × 0.3) = 0.122; 0.122 × 23.3 = 2.9 N < 8.3 N, so the wheels spin faster than the vehicle moves.
- **Coast.** After a supply cut the drive bound is zero; the vehicle slows at (2.59 + 2 × 0.02/0.30) / 9.8 = 2.72/9.8 = 0.28 m/s².
- **The standing Weight.** Beam-to-Weight friction is √(0.5 × 0.3) = 0.39. The largest demand is the level start, 1.4/9.81 = 0.15, and on the 5° climb (9.81 × sin 5° + 0.67)/(9.81 × cos 5°) = 0.16: it stays put.
- **Fit.** Wheel diameter 0.60 m; the vehicle is 0.82 m wide over its driven wheels and rolls on a default 1.3 m wide Ramp. The 8.8 kg vehicle presses a default 0.5 kg Pressure plate. Fastest rim speed 20 × 0.30 = 6 m/s, below a Bumper's 8 m/s.
- **Tick order.** The supply route is resolved in Phase 1 and the drive row solved in Phase 3 of the same tick ([solver model](../../gpu-f32-physics.md#solver-model)); a speed change during Run applies from the next committed step, as on the Motor.

### Acceptance rows

Planned class `DrivenWheelTests`. Stall guard: 10 s of game time unless stated.

| # | Given / When / Then | Planned test | Control | Boundary | Tolerance |
| --- | --- | --- | --- | --- | --- |
| W1 | Given the reference vehicle at rest wholly on a 4 m Ramp at 5°, passive axle uphill and its driven axle 0.35 m up the slab, with a Battery wired to both `PowerIn` ports and `speed` 8 and −8, when Run, then its Weight's highest committed height is more than 0.08 m above its start | `ASuppliedVehicleClimbsAGentleSlope` | unwired on the same slab it rolls back: its Weight never rises more than 0.01 m and is lower after 2 s; on the level and unwired it moves less than 0.02 m; on the level with both Driven wheels replaced by a second Axle and two Passive wheels and the Battery placed beside it, it also moves less than 0.02 m (no passive axle is powered) | placed the same way wholly on a 15° Ramp, its Weight never rises more than 0.01 m and is lower after 2 s | heights as stated: 0.08 m needs 0.92 m of the 1.50 m of slab ahead of the passive axle |
| W2 | Given the vehicle at speed on the level, when the Battery is disabled, then the drives' committed work stops growing, and 1 s later the vehicle is still moving and slower | `CuttingSupplyLeavesOnlyCoasting` | W1 | none | work growth 0; speed strictly between 0 and the speed at the cut |
| W3 | Given the vehicle driven against a Wall, then each driven wheel's spin stays under 0.5 rad/s and drive work grows by at most 1 % of torque × speed × time | `AnOverloadedWheelStallsWithoutSpinningOrWork` | free of the Wall it reaches 8 rad/s within 5 % | none | the 1 % bound is the delivered stalled-drive bound |
| W4 | Given both `speed` values reversed (−8 and 8), then the vehicle travels the opposite way by more than 1 m | `ReversedSpeedsReverseTheDrive` | W1 | −20 and 20 admitted | sign of travel |
| W5 | Given one pod mounted by `mount_back` on the `Mid` point of a beam held at both ends by two Pivots, and a Motor's `Drive` connected to `DriveIn`, `PowerIn` unwired, then the wheel turns with the Motor within 5 % of its speed | `AShaftConnectionTurnsTheWheelWithoutElectricalSupply` | with the mechanical connection removed the wheel stays at rest | none | 5 %, the delivered Motor speed allowance |
| W6 | An activation wire into `PowerIn`, and an electrical wire into `DriveIn`, are each refused and change nothing | `WrongDomainConnectionsAreRefused` | W1 and W5 connect | none | typed refusal |
| W7 | Reset after W1 restores every body's authored pose, zero spin, the wiring and the attachments; a second Run repeats W1 | `ResetRestoresTheVehicleAndTheDrive` | none | none | pose within 0.001 m; outcome repeated |
| W8 | Save and load keep `speed`, `torque`, the wires, the mechanical connection and the attachments | `SaveAndLoadKeepParametersConnectionsAndMounts` | none | none | exact values |
| W9 | `speed` beyond ±20 and `torque` below 0.1 or above 100 are refused atomically | `InvalidDriveParametersAreRefused` (deleted when S4 covers it) | bounds admitted | −20, 20, 0.1, 100 | typed refusal |
| W10 | Given both driven treads treated by a Coating station, then over the first second the rim speed exceeds the vehicle speed by more than half | `ALowFrictionTreadSlips`, **later-partner row** (EL-118) | untreated: rim speed within 10 % of vehicle speed | none | ratios as stated |
| W11 | Given a Battery too small for the climb, then the vehicle stops gaining height once the store is empty and total drive work never exceeds the energy drawn | `AnExhaustedSourceStopsTheDrive`, **later-partner row** (`ENGINE-SUPPLY-DEMAND`) | W1 | none | set by that story |

Collaborators the requirement names: supply (W1, W2, W11), load and traction (W1, W3, W10), a connected drivetrain (W5), Reset independent of the passive-wheel identity (W7).

### Integration

- **Works with.** Battery, Motor, Ramp, Wall, Weight (delivered); EL-112, EL-116, EL-117 (undelivered, needed for W1 to W4 and W7); EL-114 (W5); EL-118 (W10).
- **Campaign.** "Driven to Deliver": introduction 37, practice 38, reuse 48, 68, 138 ([campaign allocation](../requirements.md#campaign-gap-allocation)). A level that ships a pre-built vehicle needs the lesson-fixture story (S8) with fixed attachments.
- **Coverage.** Section 7.

### Delivery

- **Shape.** Single element, engine tier (first supplied drive on a dynamic-base hinge, first use of attachment sockets by a driven part).
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a); `ENGINE-JOINT-KINDS` (the fixed joint, and a hinge qualified on a dynamic support); ENGINE-ASSEMBLY-JOINTS; EL-112; EL-116 with EL-117; EL-114 for row W5; S7 widened to GAP identities; S3 preferred. W10 after the Treatment stations slice; W11 after `ENGINE-SUPPLY-DEMAND`, which is part of the engine programme.
- **Files.** `elements/DrivenWheel/` (`DrivenWheel.csproj`, `DrivenWheelElementDeclaration.cs`, `driven_wheel.tscn`, `icon.svg`), `CuriousContraptions.tests/DrivenWheelTests.cs`, this spec, the ledger entry and `TODO.md`.
- **Disqualifies the plan.** `ENGINE-JOINT-KINDS` does not qualify a supplied drive on a hinge with a dynamic support; a mechanical input cannot couple a coordinate that also carries a drive (row W5; then `DriveIn` is dropped and the owner is told the drivetrain clause needs another part); two mounts cannot share one point; EL-116's tread values differ from these, which would make W1's control compare two things.

## 6. Open questions

Resolved:

1. **Input modes.** Both inputs are declared and neither is a mode parameter (decided now: one current path; the Fan already carries electrical and shaft inputs on one package). The vehicle rows use `PowerIn`; the drivetrain row W5 uses `DriveIn` with the pod mounted on a fixed part, because the delivered Motor is static and cannot ride a chassis.
2. **Unsupplied behaviour.** Free-wheels (the delivered drive law: zero bound when unsupplied). A brake would be a second rule.
3. **Defaults.** 8 rad/s and 2.5 N·m, starting values (D1). 2.5 N·m, not 4: with 23.3 N on each driven wheel of the reference vehicle the traction limit is 11.4 N, and 4 N·m asks for 13.3 N.
4. **Reversal.** A signed `speed`, so S638's "reversed drive" needs no second part; a pod mounted the other way round runs at the opposite sign.
5. **Slip control.** Uses the Coating station on the tread (EL-118 decides every dynamic body is treatable).
6. **Catalogue entry.** Its own entry (D8), category Power.
7. **Connection kind.** The mechanical connection into `DriveIn` is the rigid default (owner decision, 22 November 2026).
8. **The reference vehicle.** Four wheels, with the load standing on the beam, so that it stands and its load stays on. A single driven wheel beside one passive wheel on one side of a beam falls over, and a ball rolls off a 0.30 m beam.
9. **Sockets.** `mount_back` and `mount_side`; none on the top face, which carries `PowerIn`.

Needs the owner: none.

## Engine conformance (22 November 2026)

1. **Declaration data only.** Two bodies, two colliders, one hinge with a supplied drive, two ports, two sockets, one supply consumer, bindings. No update loop, handler or engine branch on this part.
2. **Generic laws.** The supplied revolute drive, Coulomb contact, rolling resistance, ratio coupling on a mechanical connection, fixed joints. Stall, slip and coasting are consequences of those laws and the declared torque and friction; no row is "when the wheel meets X, do Y".
3. **Three layers.** Colliders: one box on the pod, one cylinder on the wheel. Visuals: pod and wheel meshes with their colliders; spoke, collar and lamp printed flat. Animation: the lamp colour from the committed drive state; the wheel art follows its body's pose. Nothing in physics reads art.
4. **Flow and typing.** `PowerIn` is Electrical, `DriveIn` Mechanical; both typed by `PortRole` and `SimulationDomain`. Parameters are f32. A socket's typed id and role enum are the engine story's (a string name today).
5. **Flat state.** Bodies 2 (dynamic); colliders 2 (1 box, 1 cylinder); joints 1 revolute; drives 1; sensors 0; electrical nodes 1; supply consumers 1; attachment sockets 2; stores 0; presentation bindings 3, moving roots 1.
6. **Performance.** Two primitive colliders, one more than the Passive wheel because the pod is a separate solid. At 6 m/s a substep moves the wheel 6/480 = 0.0125 m, far below its 0.20 m width. Tunnelling is excluded by the engine's swept and speculative contact, which is qualified for spheres at 40 m/s (`PayloadInstanceTests.APayloadDrivenIntoTheBenchAtFortyMetresPerSecondNeverTunnels`) and for a cylinder at 64 m/s (`WorkshopPhysicsLawCylinderTests.ACylinderAtSixtyFourMetresPerSecondNeverPassesAOneCentimetreWall`); the remaining shapes and high-spin cases are `ENGINE-TUNNEL-QUALIFICATION`'s. A parked vehicle is one island. Island sleeping does not exist today; it is owned by `ENGINE-ISLANDS-SLEEP` (owner decision 3a).
7. **Tests are scenarios.** W1 to W11 run the production simulation.

**Verdict: conforms; blocked on the engine programme (owner decision 3a), `ENGINE-JOINT-KINDS` and ENGINE-ASSEMBLY-JOINTS for capabilities stated element-free.**

## 7. Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of its spec that `tools/Coverage/NamedPackageSources.cs` reads. **A GAP identity cannot be bound at all today:** the reader lists only `EL-*.md` files and its pattern admits only an `EL-NNN` identity with an `element-NNN` ledger anchor. **Blocked on the coverage follow-on story S7**, which widens the reader to GAP, TH and RAD identities and to companion packages (owner decision 15, 22 November 2026).

| Field | Planned value |
| --- | --- |
| Package | `driven_wheel` |
| Named identity | GAP-04, `../invest/named-elements.md#gap-04` |
| Acceptance | none resolvable today; the row's own task is `sequence-task-537` |
| Decision | S638, `../invest/scope-corrections.md#s638` (the block exists on published main) |
| Mode records | `Configuration=Adjustable` (`speed`, `torque`) |

What it lacks: GAP admission in the reader; a todo link in the ledger entry (the task is co-anchored with `gap-04`, not with a todo).
