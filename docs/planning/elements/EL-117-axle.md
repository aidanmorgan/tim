# EL-117 · Axle — named-identity readiness spec

**Current delivery (24 October 2026):** [EL-117 delivery slice](../invest/named-elements.md#element-117). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Rewritten on 22 November 2026 as one current text (Batch M). Citations of the form `path:line` are to this checkout (published main `8d1d68aa`); `path@a6c914e` citations appear only in section 4. Numbers marked **starting value** are hand calculations shown here and stand under decision D1 of [delivery batches](../invest/delivery-batches.md#3-family-level-decisions); nothing was simulated. All game values are f32.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-117 · Axle · Construction |
| Anchor | [requirements.md#element-117](../requirements.md#element-117) with its co-anchored `sequence-task-670`; [named-elements entry](../invest/named-elements.md#element-117); umbrella [gap-03](../requirements.md#gap-03) (index only) |
| Owner key | S331 |
| Related identities | [EL-116 Passive wheel](EL-116-passive-wheel.md) (what it carries); [EL-112 Structural beam](EL-112-structural-beam.md) (the chassis); [EL-114 Placeable pivot](EL-114-placeable-pivot.md) (the limited, static counterpart); [GAP-04 Driven wheel](GAP-04-driven-wheel.md). No CAT refines it |
| Status | Not started; blocked on the engine programme (owner decision 3a), then `ENGINE-JOINT-KINDS` and ENGINE-ASSEMBLY-JOINTS (section 3) |

### Disposition

**To deliver as a single element** after the engine programme (owner decision 3a: every element is blocked on the engine programme), `ENGINE-JOINT-KINDS`, ENGINE-ASSEMBLY-JOINTS, EL-112 and EL-116. Planned package id `axle`, directory `elements/Axle/`, title "Axle". Its slice adds the `Hub` point to the Wheel and proves the chassis integration ([sequence-task-536](../requirements.md#sequence-task-536)).

## 2. Declaration

### Declaration to write

Closest delivered shape: `elements/Conveyor/` (a base carrying a body on a continuous revolute hinge with bearing friction, `ConveyorElementDeclaration.cs:80`–`:106`), here with a dynamic base. The attachment model is the one stated in [EL-112](EL-112-structural-beam.md), section 2. The rotation is the package's own hinge, which exists today on a static base; a hinge on a dynamic support is qualified by `ENGINE-JOINT-KINDS`, which also owns the fixed joint, and ENGINE-ASSEMBLY-JOINTS supplies the player-made connections.

- **Interfaces.** `IHingeElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:20`), `ISceneGeometryElement`, and the attachment interface the engine story adds (missing).
- **Base body (dynamic): the bearing collar.** A box 0.30 × 0.30 × 0.40 m centred on the origin, one box collider. Mass 0.3 kg; moments m·(0.30² + 0.40²)/12 = 0.00625 kg·m² about X and about Y, m·(0.30² + 0.30²)/12 = 0.0045 kg·m² about Z; gravity (0, −9.81, 0) m/s². It is 0.05 m proud of a beam's 0.20 × 0.30 m section on every side, so it reads as the collar that clamps the beam.
- **Shaft body (dynamic, on the hinge).** One cylinder collider, radius 0.0625 m, length 1.00 m along the package Z axis, centred on the origin (it passes through the collar). The radius is 1/16 m, the smallest cylinder radius the engine admits (`CuriousContraptions.Abstractions/PhysicsDeclarationValues.cs:240`; the bound is inclusive and 1/16 is exact in f32). Mass 0.2 kg; moments 0.5·m·r² = 0.5·0.2·0.00390625 = 0.00039 kg·m² about its axis and m·(3r² + l²)/12 = 0.2·(0.01172 + 1.0)/12 = 0.01686 kg·m² across it; gravity as the collar.
- **Hinge.** One `RevoluteHingeDescriptor` (`CuriousContraptions.Abstractions/MechanicalContractValues.cs:149`): support body 0, both frames at the origin, free axis Z, travel `Continuous`, stiffness 0, damping 0, friction torque from the parameter `hold_torque`, bodies do not collide, no drive.
- **Sockets.** `Mount`: a **rigid mount** (a fixed joint) on the collar at (−0.15, 0, 0), the centre of its −X face, which takes a point of the chassis; a beam end therefore butts against the collar and lies wholly on its −X side. `HubA` at (0, 0, +0.33) and `HubB` at (0, 0, −0.33): **rigid mounts** on the shaft, each taking a Wheel's `Hub` point. Both wheels of one axle therefore turn together.
  - **Clearances (by hand).** The shaft's surface is at x = ±0.0625, so a beam that ends at x = −0.15 is 0.15 − 0.0625 = 0.0875 m clear of it: the shaft and the beam are not joined to each other, so under the rule in [EL-112](EL-112-structural-beam.md), section 2, they collide and must not overlap. The collar's side face is at z = 0.20. A Wheel is 0.20 m wide, so mounted at z = 0.33 it occupies 0.23 to 0.43: 0.03 m clear of the collar, 0.08 m outside the beam's 0.30 m depth (z = ±0.15), with 0.07 m of shaft showing beyond it. A Wheel overlaps only the shaft it is fixed to. The track is 0.66 m between wheel centres. With 0.30 m wheels the axle line is 0.30 m above the ground and the collar's underside 0.15 m above it.
- **Parameter.**

  | Name | Kind | Unit | Default | Min | Max | Editor policy | Run edit |
  | --- | --- | --- | --- | --- | --- | --- | --- |
  | `hold_torque` | Real (f32) | NewtonMetre | 0 | 0 | 50 | Reject | BuildOnly |

  0 is the free axle. 50 N·m is the locked axle: the largest torque the ground can put on a wheel pair is μ·N·r, and even against a surface of friction 1.0 (pair friction √(0.8·1.0) = 0.894; delivered surfaces declare 0.3) 50 N·m holds an axle load of 50/(0.894·0.30·9.81) = 19 kg, more than any chassis the catalogue can build (a 3 kg beam, an 8 kg Weight and the running gear total under 14 kg on two axles).
- **Ports, stores, sensors, events.** None.
- **Presentation.** Shaft art follows the shaft body. A navy lock band on the collar takes its X scale from `hold_torque` × (1/50) (a parameter row, authoring only), so a locked axle differs from a free one by shape.
- **Catalogue.** Id `axle`, title "Axle", description "A bearing collar with a shaft through it. Fix the collar to a chassis and a wheel to each shaft end; raise its hold to lock it.", category `ElementCategory.Structure`, palette order 509 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)), colour gold `#f7cb52`.
- **Footprint.** Bodies 2, moving bodies 2, revolute constraints 1.

### Art and solidity

| Node | Mesh | Layer that moves it | Class | Collider or reason |
| --- | --- | --- | --- | --- |
| `Visual/Visual_Collar` | cream `#fff8e9` box 0.30 × 0.30 × 0.40 m | collar body pose | solid | the collar box |
| `Visual/Visual_Band` | gold `#f7cb52` band around the collar, flush | collar body pose | decorative | printed flat |
| `Visual/Visual_Lock` | navy `#293954` bar on the collar's top face, flush | collar body pose; parameter scale | decorative | printed flat |
| `Visual/Shaft/Visual_Shaft` | gold cylinder r 0.0625 m, 1.00 m long | shaft body pose | solid | the shaft cylinder |
| `Visual/Shaft/Visual_Key` | navy bar across each shaft end face, flush | shaft body pose | decorative | printed flat; it shows the turn |

Icon: a collar with a shaft through it and a key mark on the shaft end. It differs from the Pivot by having no plinth and a through-shaft.

## 3. Engine capabilities

### Capability probe

| Capability | State | Symbol | Exercising test, or owner |
| --- | --- | --- | --- |
| A continuous revolute hinge that turns past the full circle | present | `RevoluteTravel.Continuous` (`MechanicalContractValues.cs:129`–`:133`) | `RevoluteDriveTests.AContinuousHingeTurnsPastTheFullCircleWithNoStopImpulseAndAccumulatesItsTravel` (`CuriousContraptions.tests/RevoluteDriveTests.cs:195`) |
| Hinge friction that holds below its torque and yields above | present | `FrictionTorque` (`MechanicalContractValues.cs:158`) | `RevoluteHingeFrictionTests.AHingedBarIsHeldWhileItsGravityTorqueIsBelowTheFrictionTorque` (`RevoluteHingeFrictionTests.cs:77`), `.AHingedBarTurnsWithTheTorqueLeftAfterItsFrictionTorque` (`:59`) |
| That friction torque taken from a declared parameter | present, untested | the validator admits a parameter value in newton metres (`CuriousContraptions.Bundle/BundleValidator.cs:801`); every delivered hinge declares a constant | this slice's rows X1 to X4 |
| A hinge whose support is a dynamic body | present, untested | the compiler takes the support from the body list with no static check (`engine/gpu/WorkshopPhysicsCompiler.cs:199`); every delivered hinge base is static | `ENGINE-JOINT-KINDS` ("every joint kind is qualified with a dynamic support body as well as a static one"); row X1 then exercises it on this part |
| Cylinder rolling, slipping and rolling resistance | present | — | `WorkshopPhysicsLawCylinderTests.ACylinderRollsDownARampAtTwoThirdsGSinThetaWithoutSlipping` (`WorkshopPhysicsLawCylinderTests.cs:180`), `.BelowTheNoSlipFrictionTheCylinderSlipsFasterThanItRolls` (`:196`) |
| A fixed joint between two bodies | missing | constraint kinds are revolute, prismatic and tensile (`CoordinateKind`, `CuriousContraptions.Abstractions/PortDescriptor.cs:43`) | `ENGINE-JOINT-KINDS` |
| Mounts joined by the player to points of other placed parts, including two mounts on one hinge-carried body | missing | `AnchorPointDescriptor` is declared and never compiled (`MechanicalContractValues.cs:63`) | ENGINE-ASSEMBLY-JOINTS |
| A holding brake on a revolute coordinate | not needed: the friction row above is the hold | — | (the engine story lists a revolute holding brake as missing; this part does not wait for it) |

Counts: 3 present, 2 present-untested, 0 narrower, 2 missing. Owners: `ENGINE-JOINT-KINDS` (fixed joint; dynamic-support qualification) and ENGINE-ASSEMBLY-JOINTS (connections). Every row also waits for the engine programme (owner decision 3a).

## 4. Sources and legacy

- **Requirements.** Row: "Rotating support couples a wheel to the chassis with declared freedom"; outcome "Locked axle prevents the formerly permitted rotation" ([element-117](../requirements.md#element-117)). Integration: a loaded chassis rolls downhill, slips under insufficient traction and stops against a block; lifted wheels cannot support it; compare locked and free axle modes separately ([sequence-task-536](../requirements.md#sequence-task-536)).
- **Audit.** "Compare roll, slip and blocked wheel; no scripted travel path" (`docs/physics-puzzle-gap-audit.md@a6c914e:L82-L82`).
- **Legacy** (joint framework; no axle part existed):

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | A hinge has exactly one free angular degree of freedom. | `CuriousContraptions.tests/JointConstraintTests.cs@a6c914e:L21-L32` | Carry forward. |
| 2 | Interior motion is not locked; a zero-width range locks the joint. | `CuriousContraptions.tests/JointRangeTests.cs@a6c914e:L89-L102` | Carry forward the outcome (a locked axle does not turn); the mechanism here is the friction row. |
| 3 | A zero-width range was compiled as an extra bilateral row in one block. | `engine/physics/PhysicsJoint.cs@a6c914e:L163-L170` | Do not carry forward: the CPU block solver. |
| 4 | A runtime latch held, released and restored both constraint versions. | `CuriousContraptions.tests/PhysicsJointUpdateTests.cs@a6c914e:L18-L50` | Do not carry forward: the hold is a Build setting, not switched during a Run. |

Files harvested: `CuriousContraptions.tests/JointConstraintTests.cs`, `CuriousContraptions.tests/JointRangeTests.cs`, `CuriousContraptions.tests/PhysicsJointUpdateTests.cs`, `engine/physics/PhysicsJoint.cs`.

## 5. Acceptance outline

### Behaviour and numbers

- **Laws.** A revolute constraint with a Coulomb friction row between collar and shaft; rigid attachments to the chassis and the wheels; contact, friction and rolling resistance at the wheels.
- **Test chassis.** One 2 m beam (1.5 kg); an Axle at `EndA` and one at `EndB` (0.5 kg each), each with its `Mount` on the beam's end point, so the axle lines are 0.15 m beyond the beam ends and the wheelbase is 2.30 m; four Wheels (0.5 kg each); and a 1 kg Weight standing on the beam's middle: M = 5.5 kg. Spinning parts: four wheels at 0.0225 kg·m² and two shafts at 0.00039 kg·m², 0.0908 kg·m² in all, equivalent to 0.0908/0.30² = 1.009 kg at the rim.
- **The standing Weight.** Beam-to-Weight friction is √(0.5·0.3) = 0.39. On the 15° Ramp it needs at most tan 15° = 0.27, so the Weight stays put in rows X1, X2 and X4. On the 35° Ramp, with the chassis sliding at 1.7 m/s², it would need (9.81·sin 35° − 1.7)/(9.81·cos 35°) = 0.49 and would slide off, so row X3 runs without the Weight; the slide condition does not depend on mass.
- **Free, on a 15° Ramp.** Without rolling resistance a = M·g·sin θ/(M + 1.009) = 13.97/6.509 = 2.15 m/s². Rolling resistance of 0.03 on a total normal load of 52.1 N takes about 1.6 N, leaving about 1.9 m/s². A frictionless slide would give 2.54 m/s², a locked chassis 0.
- **Traction limit.** The Ramp and wheel pair has friction √(0.8·0.3) = 0.49. Each axle carries about 26 N, so the ground can turn its wheel pair with at most 0.49·26·0.30 = 3.8 N·m.
- **Partly held (1 N·m).** Below the limit: the wheels turn and each axle resists with 1/0.30 = 3.3 N at the ground, so a falls by 2·3.3/6.509 = 1.0 m/s² to about 0.9 m/s².
- **Locked (50 N·m).** Above the limit: the wheels cannot turn. At 15°, tan θ = 0.27 < 0.49 and the chassis stays. At 35°, tan θ = 0.70 > 0.49 and it slides at g·(sin θ − 0.49·cos θ) = 1.7 m/s² on wheels that do not turn.
- **Tick order.** Mechanical substeps only (Phase 3 of the [solver model](../../gpu-f32-physics.md#solver-model)).

### Acceptance rows

Test class `AxleTests`. Accelerations are means between two samples one second apart. Shaft turn is read relative to the collar. Stall guards 10 game seconds. Chrome proof is pending the post-catalogue phase.

| # | Given / When / Then | Test | Control | Boundary |
| --- | --- | --- | --- | --- |
| X1 | Given the free chassis on a 15° Ramp, when Run starts, then it accelerates at 1.7 to 2.2 m/s² and each wheel's spin is within 5 % of speed / 0.30 m | `AxleTests.AFreeAxleLetsALoadedChassisRollDownhill` | X2 | without the Weight it also rolls |
| X2 | Given `hold_torque` 50 on both axles on the same Ramp, then the chassis moves less than 0.01 m and no shaft turns more than 0.02 rad in 5 s | `AxleTests.ALockedAxleHoldsTheChassisOnAGentleSlope` | X1 | — |
| X3 | Given the locked chassis, without its Weight, on a 35° Ramp, then it descends more than 1 m within 3 s while no shaft turns more than 0.05 rad: it slips | `AxleTests.ALockedChassisSlidesWhereTractionIsInsufficient` | X2 | — |
| X4 | Given `hold_torque` 1 on both axles on the 15° Ramp, then the wheels turn and the acceleration is at least 0.5 m/s² below row X1's | `AxleTests.APartHoldSlowsTheRoll` | X1 | — |
| X5 | Given a Wall across the foot of the Ramp, then the free chassis stops against it (speed under 0.05 m/s within 5 s of contact) | `AxleTests.AChassisStopsAgainstAWall` | X1 | — |
| X6 | Given the chassis placed with its wheels 0.2 m above the bench, then it falls until the wheels bear and rests with the beam's centre 0.30 m above the bench within 0.01 m; with the four Wheels removed it rests on its collars with the beam's centre 0.15 m above the bench (the collar's half-height) | `AxleTests.OnlyBearingWheelsSupportTheChassis` | the chassis without Wheels | — |
| X7 | A `hold_torque` below 0, above 50 or non-finite is refused atomically; a wire to a mount, and a point offered to a point, are refused with a typed reason | conformance suite rows 2 and 5 (S4) once it lands; until then `AxleTests.InvalidHoldAndForeignConnectionsAreRefused` | 0 and 50 are admitted | — |
| X8 | Reset restores every pose, shaft angle and attachment; save and load round-trip `hold_torque`, poses and attachments | `AxleTests.ResetAndSaveLoadRestoreTheChassis` | — | Reset while rolling |

Requirement rows preserved: "couples a wheel to the chassis with declared freedom" (X1); "locked axle prevents the formerly permitted rotation" (X2, X3). Every clause of sequence-task-536 has a row: rolls downhill (X1), slips under insufficient traction (X3), stops against a block (X5), lifted wheels cannot support it (X6), locked and free compared separately (X1 against X2).

### Integration

- **Works with.** Planned: EL-112, EL-116, GAP-04. Delivered: `ramp`, `wall`, `weight`.
- **Campaign.** GAP-03 "Wheel Meet Again": introduction 18 (roll a preloaded beam chassis, then build its axle), practice 19, reuse 37, 68, 138 ([campaign allocation](../requirements.md#campaign-gap-allocation)).
- **Lesson fixtures (S8).** Needed for the preloaded chassis.
- **Coverage binding.** Section 7.

### Delivery

1. Prerequisites, in order: blocked on the engine programme (owner decision 3a); `ENGINE-JOINT-KINDS` (the fixed joint, and a hinge qualified on a dynamic support); ENGINE-ASSEMBLY-JOINTS; EL-112; EL-116.
2. This slice: `elements/Axle/` (project, declaration, scene, icon), the `Hub` point in `elements/Wheel/WheelElementDeclaration.cs`, `CuriousContraptions.tests/AxleTests.cs`, the solution line until S3 lands, the art classification. Expected tier: data-only if every probe row is *present* with a test by then; otherwise engine. No `elements/Axle/` package exists in this checkout, so no delivered shaft is resized.

**Disqualified if** `ENGINE-JOINT-KINDS` does not qualify a revolute hinge on a dynamic support (the Axle waits for that; it is not repaired inside this slice); or if the engine story offers hinged player-made connections and the owner prefers the axle to be one (then the Axle becomes a part with a hinged mount and no shaft body, and `hold_torque` moves to that mount).

## 6. Open questions

**Resolved under the delegation.**

1. Free and locked as a mode: no mode. One `hold_torque` value; 0 is free and 50 N·m holds every load the catalogue can put on it. Reason: it uses the hinge friction law that exists and is tested, and a mode switch would be a second rule.
2. Changing the hold during a Run (a brake): no; BuildOnly. A brake is EL-055's lesson.
3. A non-wheel body on the shaft: anything with a point may be fixed to `HubA` or `HubB`; nothing is refused by kind.
4. One wheel or two per axle: two mounts on one through-shaft, so a chassis stands on four wheels and each pair turns together.
5. Where the chassis mounts: on the collar's −X face, not its centre. A beam ending at the collar's centre would overlap the shaft, which it is not joined to.

**Needs the owner.** None.

## Engine conformance (22 November 2026)

1. **Declaration data only.** Two bodies, two colliders, one hinge, three mounts, one parameter, one parameter-scaled mark.
2. **Generic laws.** Revolute constraint with Coulomb friction; fixed joints; wheel contact. "Locked" is the friction law at a high declared torque, not a lock rule. The missing capabilities are stated element-free, each with one owner: the fixed joint and the dynamic-support qualification (`ENGINE-JOINT-KINDS`) and player-made connections (ENGINE-ASSEMBLY-JOINTS).
3. **Three layers.** Colliders: one box (collar), one cylinder (shaft). Visuals: collar, flush bands, shaft, flush key marks. Animation: none; the shaft and key follow the shaft body, and the lock bar is scaled by an authored parameter.
4. **Flow and typing.** No ports; `hold_torque` is an f32 Real in newton metres; a socket's typed id and role enum are the engine story's (a string name today).
5. **Flat state.** Bodies 2 (dynamic); colliders 2 (1 box, 1 cylinder); joints 1 (revolute) plus up to 3 rigid attachments when connected; drives 0; sensors 0; domain nodes 0; stores 0; presentation: 1 body binding, 1 parameter row.
6. **Performance.** Two primitive colliders, both needed (the collar blocks and bears; the shaft is visible and solid). A four-wheel chassis is 9 bodies, 9 colliders, 2 hinges and 6 attachments in one island. Island sleeping does not exist today; it is owned by `ENGINE-ISLANDS-SLEEP` (owner decision 3a).
7. **Tests are scenarios.** Rows X1 to X8 run the production simulation on a built chassis.

**Verdict: conforms; blocked on the engine programme (owner decision 3a), `ENGINE-JOINT-KINDS` and ENGINE-ASSEMBLY-JOINTS for capabilities stated element-free.**

## 7. Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final package-source-binding section in the identity's spec, read by `tools/Coverage/NamedPackageSources.cs` (pattern: EL-239 Baseball). The section is not written here, because the reader parses every spec that carries its heading and fails the whole coverage run when a field does not resolve. **Blocked on the coverage follow-on story S7**, which widens the reader (owner decision 15, 22 November 2026) and must also admit an entry whose own task is a co-anchored sequence task with no todo link (batch summary, section 4 item 16).

| Field | Planned value |
| --- | --- |
| Package | `axle` |
| Named identity | EL-117, `../invest/named-elements.md#element-117` |
| Acceptance | none available: the ledger entry has no todo link; the identity's own task is `sequence-task-670`, co-anchored with `element-117` |
| Decision | S331, `../invest/scope-corrections.md#s331` |
| Mode records | `Configuration=Adjustable` (`hold_torque`) |

What it lacks today: a todo link in the ledger entry, and an `s331` block in `scope-corrections.md` on published main.
