# EL-053 · Rotary-to-linear converter declaration readiness spec

**Current delivery (24 October 2026):** [EL-053 delivery slice](../invest/named-elements.md#element-053). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 named-identity spec. Baseline commit `a6c914e`. Legacy citations use `path@a6c914e:Lstart-Lend` and stay retrievable from git history after the Epic 7 purge. Values marked **proposed** are design values the owner may revise. Sections 1 to 3 and 5 to 8 were rewritten on 21 November 2026 against checkout `8d1d68aa`; their `path:line` citations are current code, read that day.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-053 |
| Name | Rotary-to-linear converter (player title "Screw slide") |
| Type | Mechanical |
| Requirement anchor | [element-053](../requirements.md#element-053); scope index [todo-211](../requirements.md#todo-211) |
| Named entry | [named-elements.md#element-053](../invest/named-elements.md#element-053); slice `named-el-053`, owner S339 |
| CAT spec refined or extended | none. Related: [CAT-042 motor](CAT-042-motor.md) (shaft source), [CAT-039 linear pusher](CAT-039-linear_pusher.md) (supplied linear drive, different law), [CAT-071 wound spring](CAT-071-wound_spring.md) (the delivered shaft-to-slider coupling) |
| Related identities | [EL-054](EL-054-linear-to-rotary-converter.md) (inverse conversion); [EL-055](EL-055-mechanical-brake.md) (holds this carriage through its shaft); EL-218 Crank-slider and EL-219 Rack-and-pinion slide ([ledger](../invest/named-elements.md#element-219)); EL-156 to EL-158 cams (todo-424) |
| Roadmap story | [EL-053 delivery slice](../invest/named-elements.md#element-053) |
| Status | Not started; blocked on the engine programme (owner decision 3a), relying most on ENGINE-SUPPLY-DEMAND (the Motor and Battery train that turns its shaft) and ENGINE-ISLANDS-SLEEP; then S7. No package exists under `elements/`; every descriptor kind the declaration names is present and tested (section 3) |
| Disposition | To deliver as a single element |
| Planned package | id `rotary_to_linear_converter`, directory `elements/RotaryToLinearConverter/`, class `RotaryToLinearConverterElementDeclaration` |
| Coverage binding | Planned, not yet live (section 7); blocked on S7. No CAT number |
| delivery-batches class | Not classified: [delivery-batches](../invest/delivery-batches.md) lists it in none of 1a, 1b, 1c or 2 ("Not classified here", end of that file). Code corrects this: it is declaration data today, the Wound spring shape without the retention. Proposed class: 1b run "Rotary" |

## 2. Declaration

Follows `elements/WoundSpring/WoundSpringElementDeclaration.cs:8` with the retention, signal node and spring removed. Interfaces: `ISliderElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:15`), `IHingeElement` (`:20`), `IMechanicalTransmissionElement` (`:90`), `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`). `KinematicMode.Static`; domains `Mechanical`; footprint 3 bodies, 2 moving, 1 prismatic, 1 revolute. No executable logic.

**Bodies.** Local frame: X along the rail, Y up, Z toward the player.

| Body | Shape (m) | Mass and inertia | Joint |
| --- | --- | --- | --- |
| 0 housing and rail (static) | box 1.6 × 0.3 × 0.5 centred at the origin; navy foot box 1.6 × 0.1 × 0.5 centred (0, −0.2, 0), no deeper than the housing so the shaft pulley (z 0.255 to 0.385) clears it | static | support |
| 1 carriage | box 0.5 × 0.3 × 0.5 | 1 kg; solid box moments (0.0283, 0.0417, 0.0283) kg·m² | prismatic slider 0 on body 0 |
| 2 input shaft pulley | cylinder r 0.28, width 0.13, axis local Z | 0.25 kg; solid disc: axial m·r²/2 = 0.0098, transverse m(3r² + w²)/12 = 0.00525 kg·m² (the Clutch shaft, `elements/Clutch/ClutchElementDeclaration.cs:17`–`:19`, `:120`–`:121`) | continuous revolute hinge 0 on body 0 |

- **Slider 0** (`PrismaticSliderDescriptor`, `CuriousContraptions.Abstractions/MechanicalContractValues.cs:104`): base frame at (−0.5, 0.3, 0) turned −90° about Z so the slide axis (frame Y) is element +X; lower limit `Fixed(0)`, upper limit `Fixed(1.0)`; stiffness, damping and compression 0; `BodiesCollide` false; gravity (0, −9.81, 0); no drive. The carriage rests on the rail top and its centre travels x = −0.5 to +0.5, so it stays inside the 1.6 m rail.
- **Hinge 0** (`RevoluteHingeDescriptor`, `:149`): base frame (−0.8, 0, 0.32); `RevoluteTravel.Continuous`; friction torque 0.02 N·m (the Clutch bearing, `ClutchElementDeclaration.cs:20`); no drive.
- **Transmission 1** (`MechanicalTransmissionDescriptor`, `:258`): driving `Revolute 0`, driven `Prismatic 0`, ratio `FromParameter("lead")`, `TransmissionConstraintKind.Bilateral`. The ratio unit for a revolute driving a prismatic coordinate is m/rad (`CuriousContraptions.Bundle/MechanicalTransmissionValidation.cs:56`–`:58`).
- **Material** for all three bodies (`SceneBodyGeometry`, order restitution, friction, rolling resistance, bounce threshold): (0, 0.3, 0, 0.1), the rigid drive-part material (`ClutchElementDeclaration.cs:89`–`:91`). A dead pusher face moves cargo without bouncing it.

**Parameters.**

| Key | Kind | Unit | Default | Min | Max | Editor policy | Run edit |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `lead` | Real | `MetrePerRadian` | 0.05 | 0.025 | 0.2 | Reject | BuildOnly (required of a ratio parameter, `MechanicalTransmissionValidation.cs:70`) |

Stroke is fixed at 1.0 m (D13). No stores.

**Ports.**

| `PortId` | Role | Domain | Direction | Signal kind | Coordinate | Position (m) |
| --- | --- | --- | --- | --- | --- | --- |
| `DriveIn` | `DriveIn` | Mechanical | Input | none | `Revolute 0`, `CoordinatePerRadian` +1 | (−0.8, 0, 0.4), normal +Z; `MechanicalRim` radius 0.28 on body 0 |

Positive shaft rotation (the Motor's positive direction) extends the carriage toward +X. There is no output port: the carriage acts on cargo by contact, and a driving coordinate may not carry a `Drive` port (`MechanicalTransmissionValidation.cs:29`).

**Presentation.** Body bindings `Visual/Carriage` → body 1 and `Visual/Shaft` → body 2 (as `WoundSpringElementDeclaration.cs:74`). One binding. `PresentationProperty` has only `LocalRotationZ` among rotations (`CuriousContraptions.Abstractions/ElementPresentation.cs:12`–`:16`), so `Visual/ScrewAxis` is a fixed node turned so its local Z lies along the rail, and its child `Visual/ScrewAxis/Screw` takes `LocalRotationZ` from `PresentationDriver.CommittedRevoluteAngle` (`ElementPresentation.cs:30`) for hinge 0, as the Conveyor's output pulley (`elements/Conveyor/ConveyorElementDeclaration.cs:20`).

**Palette.** Category `ElementCategory.Motion`, palette order 122 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)), colour ochre (0.84, 0.61, 0.28), title "Screw slide", icon `res://elements/RotaryToLinearConverter/icon.svg`, scene `rotary_to_linear_converter.tscn`.

### Art and solidity

| Node | Mesh and dimensions (m) | Class |
| --- | --- | --- |
| `Housing` | cream box 1.6 × 0.3 × 0.5 | solid, box collider on body 0 |
| `Foot` | navy box 1.6 × 0.1 × 0.5 | solid, box collider on body 0 |
| `Visual/ScrewAxis/Screw` | pale grey threaded rod r 0.05, length 1.5, lying in a channel cut 0.1 deep into the rail top | decorative: inside the housing collider's envelope and turned by a presentation binding |
| `Visual/Carriage` | ochre box 0.5 × 0.3 × 0.5 | solid, box collider on body 1 |
| `Visual/Shaft` | ochre pulley r 0.28, width 0.13, with navy spokes | solid, cylinder collider on body 2 |
| witness marks | gold marks printed on the screw and pulley face | decorative: printed flat |

Colours: Cream `#fff8e9` (part-body cream, owner decision 4), Navy `#1c2836`, Gold `#f7cb52` (`DESIGN.md:80`); drive-train ochre `#d69c47` (`DESIGN.md:235`); metal details pale grey (`DESIGN.md:248`). Icon: a horizontal screw with a block on it and a curved arrow turning into a straight arrow. At a glance it differs from EL-054 by lying flat with a block riding a screw (EL-054 stands upright with a pad on a toothed rack), and from the Linear pusher by its exposed screw and link pulley instead of a rod and electrical sockets.

### Behaviour and numbers

- **Law.** Carriage speed (m/s) = `lead` (m/rad) × shaft speed (rad/s), enforced as one bilateral row, so force and torque pass both ways: carriage force = shaft torque / `lead`. A blocked carriage therefore stalls the shaft and everything upstream ("blocked output loads the input"). With no torque on the shaft nothing moves ("no command-only displacement"): the package has no drive of its own.
- **Lead 0.05 m/rad default.** The default Motor (6 rad/s, 20 N·m, `elements/Motor/MotorElementDeclaration.cs:41`, `:44`) moves the carriage at 0.3 m/s, crossing the stroke in 3.3 s, slow enough to read. Range 0.025 to 0.2 is the delivered winding-lead range (`WoundSpringElementDeclaration.cs:17`).
- **Speed and force envelope.** Fastest: 20 rad/s × 0.2 = 4 m/s, the Linear pusher's maximum speed (`elements/LinearPusher/LinearPusherElementDeclaration.cs:15`). Force at defaults 20 / 0.05 = 400 N; at the weakest Motor (0.1 N·m) and longest lead 0.5 N; at the strongest Motor and shortest lead 100 / 0.025 = 4,000 N. The slice checks the end stop at that corner (section 8).
- **Back-driving is allowed.** The row is bilateral, so a load on a vertical carriage turns an unsupplied shaft: the 1 kg carriage alone gives 9.81 N × 0.05 = 0.49 N·m, above the Motor's 0.05 N·m bearing friction (`MotorElementDeclaration.cs:100`). Holding it is the Brake's job (EL-055).
- **Stroke 1.0 m** exceeds the Basketball diameter (0.68 m, `elements/Ball/BallElementDeclaration.cs:29`) so a ball is pushed clear of its starting place.
- **End stops.** At either limit the slider stop blocks the carriage, the shaft stalls and a supplied Motor stops doing work.
- No signal, optical or acoustic read; tick order is the mechanical solve only.

## 3. Engine capabilities (capability probe)

| Capability | Status | Symbol | Exercising test |
| --- | --- | --- | --- |
| Static and moving bodies with colliders from scene geometry | present | `ISceneGeometryElement` `CuriousContraptions.Abstractions/SceneGeometry.cs:10` | `CuriousContraptions.tests/SliderInstanceTests.cs:76` |
| Prismatic slider with lower and upper stops | present | `PrismaticSliderDescriptor` `CuriousContraptions.Abstractions/MechanicalContractValues.cs:104` | `CuriousContraptions.tests/SliderInstanceTests.cs:61` |
| Continuous revolute hinge with Coulomb bearing friction | present | `RevoluteHingeDescriptor` `MechanicalContractValues.cs:149` | `CuriousContraptions.tests/RevoluteHingeFrictionTests.cs:59`; `CuriousContraptions.tests/ClutchTests.cs:567` |
| Bilateral revolute-to-prismatic ratio row compiled from a package, work passing both ways | present | `MechanicalTransmissionDescriptor` `MechanicalContractValues.cs:258`; `TransmissionConstraintKind.Bilateral` `:255` | `CuriousContraptions.tests/RetentionWorkTests.cs:16` (a declared hinge drives a declared slider at 0.1 m/rad, then is back-driven, with no energy created) |
| Ratio bound to a build-only m/rad parameter | present | `MechanicalTransmissionValidation.cs:60`–`:73` | `CuriousContraptions.tests/WoundSpringContractTests.cs:15` |
| `DriveIn` port on the driving hinge, joined by a Mechanical link (the delivered rigid connection; coordinator decision 36) from another part's `Drive` | present | `PortDescriptor.Coordinate` `CuriousContraptions.Abstractions/PortDescriptor.cs:67`; role rule `MechanicalTransmissionValidation.cs:29` | `CuriousContraptions.tests/MechanicalCoordinateBindingsTests.cs:499`; `CuriousContraptions.tests/WoundSpringIntegrationTests.cs:17` |
| A blocked driven coordinate loads the source, which then does no work | present | `CuriousContraptions.Simulation/MechanicalTransmissionSolver.cs:77` | `CuriousContraptions.tests/ReverseTransmissionLoadTests.cs:19`; `CuriousContraptions.tests/PrismaticRetentionTests.cs:419`; `CuriousContraptions.tests/RevoluteDriveTests.cs:307` |
| A slider body pushes cargo by contact | present | slider moving-body colliders, `SliderBodyDescriptor` `MechanicalContractValues.cs:75` | `CuriousContraptions.tests/LinearPusherTests.cs:307`; `CuriousContraptions.tests/PrismaticRetentionTests.cs:237` |
| Art bound to a committed hinge angle and to moving bodies | present | `PresentationDriver.CommittedRevoluteAngle` `CuriousContraptions.Abstractions/ElementPresentation.cs:30` | `CuriousContraptions.tests/ConveyorTests.cs:630` |
| Mechanical connection save/load and refusal of an invalid connection | present | `WorkshopConnections` (validated at `CuriousContraptions.Bundle/BundleValidator.cs:373`) | `CuriousContraptions.tests/MechanicalCoordinateBindingsTests.cs:681`, `:456` |

Counts: 10 present, 0 untested, 0 narrower, 0 missing. Nothing needs element-specific engine code.

**Element dependencies.** A shaft source: Motor with Battery (both delivered). No dependency on an undelivered part.

### Engine conformance

- **Declaration data only.** A slider, a hinge and one bilateral ratio row, all delivered descriptor kinds. No callback, script or engine branch on this part.
- **Generic laws.** Conversion is the ratio constraint with the declared `lead`; "blocked output loads the input" is that row's reaction; end stops are slider limits; pushing is contact.
- **Layers.** Colliders: 2 static boxes (housing, foot), 1 carriage box, 1 shaft cylinder. Visuals: the art table, none used as a collider. Animation: `Visual/Carriage` and `Visual/Shaft` follow body poses; the screw is turned by a presentation binding that reads the committed hinge angle and is decorative (it lies inside the housing collider); witness marks are moved by nothing. Visible = solid: every proud visible solid (housing, foot, carriage, pulley) has its own collider; the screw lies inside the housing collider's envelope and the marks are printed flat.
- **One-way flow and typing.** The binding reads committed state and writes nothing back. Port role and domain are enums; `lead` is a typed f32 parameter.
- **Flat state.** 3 bodies, 4 colliders (3 boxes, 1 cylinder), 1 prismatic and 1 revolute joint, 1 transmission row, 0 drives, 0 sensors, 0 domain nodes, 0 stores, 2 body bindings and 1 rotation binding.
- **Performance.** Four primitive colliders, one more than the Wound spring's moving parts need; none is cosmetic. The housing is static. No island sleeping exists today; a resting carriage costs nothing only once ENGINE-ISLANDS-SLEEP (owner decision 3a) is delivered, ahead of this part. No budget is stated in the engine documents and none is invented here.
- **Tests are scenarios.** Rows A1 to A11 run the production simulation through placed parts and Mechanical links.

## 4. Sources and legacy

- Requirements row: outcome "Blocked output loads the input; no command-only displacement". No variants.
- Element map row: Source D owner freezes the linked mechanism. No decisions.md row applies.
- Current research note: crank and cam conversions are separate EL-156..158 rows (`docs/component-research.md@a6c914e:L76-L76`).

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | A transmission couples two axial coordinates bidirectionally: output speed = ratio × input speed; shared inertia sets both speeds and reactions; no copied speed or work. | `engine/physics/PhysicsTransmissionJoint.cs@a6c914e:L7-L13` | carry forward (law) | This is the converter law and gives "blocked output loads the input". |
| 2 | A shaft-to-slider coupling uses a winding lead in m/rad, admitted 0.025–0.2. | `parts/WoundSpringPart.cs@a6c914e:L97-L98`, `parts/WoundSpringPart.cs@a6c914e:L118-L124` | carry forward | Sourced lead range. |
| 3 | A mechanical socket binds one owned axial guide with a nonzero coordinate-per-radian. | `engine/ConnectionPort.cs@a6c914e:L23-L38`; `engine/MechanicalNetwork.cs@a6c914e:L22-L25` | carry forward | Typed port to joint binding. |
| 4 | Finite-inertia shaft: axial inertia m r²/2, transverse m(3r² + w²)/12. | `engine/SceneRotaryShaft.cs@a6c914e:L13-L19` | carry forward | Standard cylinder inertia. |
| 5 | Without supply the motor accepts no work and the plunger does not move. | `CuriousContraptions.tests/WoundSpringRuntimeTests.cs@a6c914e:L102-L141` | carry forward | Acceptance fact for "no command-only displacement". |
| 6 | Transmission and joint rows solved by the CPU prediction solver. | `engine/physics/PhysicsTransmissionJoint.cs@a6c914e:L38-L40` | do not carry forward | CPU solver path; replaced by a TGS Soft row in the worker. |

**Files harvested:** `engine/physics/PhysicsTransmissionJoint.cs`, `engine/SceneTransmissionJoint.cs`, `engine/SceneRotaryShaft.cs`, `engine/ConnectionPort.cs`, `engine/MechanicalNetwork.cs`, `engine/MachineData.cs`, `parts/WoundSpringPart.cs`, `parts/SpringPart.cs`, `CuriousContraptions.tests/WoundSpringRuntimeTests.cs`. Searched with no converter-specific hit: `content/puzzles.json`, `tools/Campaign`, `reference/` (terms: crank, rack, pinion, screw, converter).

## 5. Acceptance outline

Requirement row: [element-053](../requirements.md#element-053). Preserved outcome: blocked output loads the input; no command-only displacement.

- **Chrome UI recipe (pending, post-catalogue).** In free play place a Battery, a Motor and the converter from the toolbox; place a Basketball in front of the carriage. Wire Battery → Motor (`PowerIn`) and join Motor `Drive` → converter `DriveIn` with a Mechanical link through the contextual wiring control. Verify the placed parts, the lead and the typed connections from the committed read.

Native rows. Planned tests live in `CuriousContraptions.tests/RotaryToLinearConverterTests.cs`; all names are planned. Tolerances reuse existing quantities: 2 % shaft speed (`CuriousContraptions.tests/GuideResponseTests.cs:174`), 0.01 m carriage position (2 % of the carriage length, below what a player can see), 0.01 rad held shaft (`CuriousContraptions.tests/RevoluteDriveTests.cs:371`).

| Row | Given / When / Then | Planned test | Control | Boundary | Tolerance and stall guard |
| --- | --- | --- | --- | --- | --- |
| A1 | Battery, default Motor, Mechanical link, converter. Run 2 s. The carriage has travelled `lead` × the shaft's committed travel | `RotaryToLinearConverterTests.ACarriageTravelsItsLeadTimesTheShaftTravel` | link removed: carriage travel stays within 0.01 m of 0 | `lead` 0.025 and 0.2 | travel ratio within 2 %: a wrong ratio or sign would mis-time every pushed ball. Guard 2 s |
| A2 | No supply (Battery disabled) for 2 s: carriage and shaft do not move and the Motor's work is 0 | `RotaryToLinearConverterTests.WithoutShaftWorkTheCarriageDoesNotMove` | A1 | none | 0.01 m and 0.01 rad: movement with no shaft work is command-only displacement |
| A3 | A Wall in the carriage path. The carriage stops at the Wall, the Motor shaft stalls and its work grows by under 1 % of torque × target speed × time while blocked | `RotaryToLinearConverterTests.ABlockedCarriageStallsTheMotor` | no Wall: the carriage passes that position | none | the stall bound of `RevoluteDriveTests.cs:307`; a carriage that pushed through a Wall would break every blocking puzzle. Guard 3 s |
| A4 | A Basketball ahead of the carriage is pushed at least 0.5 m | `RotaryToLinearConverterTests.TheCarriagePushesCargo` | ball placed 0.6 m beside the rail does not move more than 0.05 m | Bowling ball (4 kg) is also pushed | displacement only. Guard 5 s |
| A5 | Each end stop: the carriage stops within 0.01 m of 0 and of 1.0 m and the shaft stalls; a negative Motor speed is not available, so the return uses a Reverse gear in the link path | `RotaryToLinearConverterTests.TheCarriageStopsAtBothEndsOfItsStroke` | none | strongest Motor (100 N·m) at `lead` 0.025 | 0.01 m: a carriage leaving its rail is a visible defect. Guard 8 s |
| A6 | Vertical placement, unsupplied Motor: the carriage descends and turns the shaft; the system's kinetic energy never exceeds the carriage's lost potential energy | `RotaryToLinearConverterTests.AVerticalCarriageBackDrivesAnUnsuppliedShaft` | horizontal placement: nothing moves | none | energy bound with the allowance of `SpringTestEnergy.Allowance(scene)` (0.005 J + 0.0981 J per kg of dynamic mass, `CuriousContraptions.tests/SpringTestEnergy.cs:44`–`:47`), the bound `RetentionWorkTests.cs:16` uses |
| A7 | Run then Reset restores the carriage, shaft and cargo to their authored poses | S4 row 3 once delivered, else `RotaryToLinearConverterTests.ResetRestoresTheAuthoredState` | n/a | Reset at an end stop | authored values, exact |
| A8 | Save and load round-trip `lead` and the `DriveIn` Mechanical link | S4 row 1 once delivered, else `RotaryToLinearConverterTests.ASavedConverterLoadsWithItsLeadAndLink` | n/a | `lead` at both bounds | exact field values |
| A9 | `lead` below 0.025, above 0.2 or non-finite is refused atomically | S4 row 2 once delivered, else `RotaryToLinearConverterTests.AnOutOfRangeLeadIsRefused` | bounds admitted | both bounds | typed refusal, construction unchanged |
| A10 | An electrical wire into `DriveIn`, and a second Mechanical link into `DriveIn`, are refused | S4 row 5 once delivered, else `RotaryToLinearConverterTests.AWrongKindConnectionIsRefused` | the Motor's link is admitted | none | `WorkshopEditRejection.InvalidConnection` |
| A11 | Collider and art agree for housing, foot, carriage and shaft | S4 row 4 once delivered, else `RotaryToLinearConverterArtTests.EveryVisibleSolidHasItsCollider` | n/a | n/a | sizes within 0.005 m |

Integrations: the scope index [todo-211](../requirements.md#todo-211) defines no separate integration task; shared interactions use [IX-05 shaft torque transmission](../requirements.md#interaction-05) (A3) and [IX-01 contact impulse](../requirements.md#interaction-01) (A4).

## 6. Open questions

**Resolved**

1. Carriage start position: always travel 0. D19 (the spec's proposal); a second start position has no row or lesson.
2. Pass-through `Drive` output: none. Decided by code: a transmission's driving coordinate may not carry a `Drive` port (`MechanicalTransmissionValidation.cs:29`), and D19.
3. ContactImpulse missing from the map binding: contact is implied. D14 (map rows are navigation; the probe is the authority).
4. Back-driving: allowed. Decided now: the requirement's "blocked output loads the input" needs a bilateral row, and a self-locking screw would be a second law; the Brake holds a loaded carriage.
5. Stroke parameter: removed; stroke is fixed at 1.0 m. D13.
6. Shaft dimensions changed from the harvested r 0.26 m to the Clutch shaft (r 0.28 m, width 0.13 m). Decided now: one shaft size across EL-053 to EL-057 so Mechanical links and rims match.
7. Package id is `rotary_to_linear_converter` (batch convention), replacing the earlier proposal `rotary_linear_converter`. D1, D9.
8. Relation to EL-219 Rack-and-pinion slide: EL-219's own row adds an attachment carriage (a part fixed to the carriage) and travel-per-turn teaching; EL-053 pushes by contact only. Recorded as a gap for the ledger (section 7).

**Needs the owner**

None.

## 7. Integration

- **Collaborators.** Motor, Battery, Reverse gear, Clutch, Wall, the balls (all delivered). EL-055 Brake (not delivered) holds the carriage; its carrier acceptance row uses this part once both exist.
- **Campaign.** "Both conversion directions": first use levels 31–40, reuse 41–50, 61–90, 136–150 (`docs/planning/invest/campaign-reservations.md:98`).
- **Lesson fixtures.** A level that places this part needs S8 (lesson fixtures bind to a ledger-bound part).
- **Overlap with EL-219.** With the constant-ratio row, end stops and a `DriveIn` shaft, this package already is a load-aware "signed shaft drives a finite carriage with end stops". EL-219 is distinct only by its attachment carriage, which needs the player-made attachment of `ENGINE-ASSEMBLY-JOINTS`. If EL-219's probe finds no other delta, it is a variant of this package (toothed-rail art) or a Duplicate closure candidate, not a second mechanism.

### Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of the identity's spec headed "Package source binding", read by `tools/Coverage/NamedPackageSources.cs` (on `origin/main`, `25df3cd5`; pattern: EL-239 Baseball). The section is not written here: the reader parses every EL spec that carries that heading and refuses one whose preconditions fail. **Blocked on the coverage follow-on story S7.**

| Field | Planned value |
| --- | --- |
| Package | `rotary_to_linear_converter` |
| Named identity | EL-053, `../invest/named-elements.md#element-053` |
| Acceptance | `todo-211` and `sequence-task-606` |
| Decision | S339; `scope-corrections.md` has no `s339` block |
| Mode records | `Configuration=Adjustable` (it declares a parameter; the reader admits Fixed or Adjustable, confirmed at slice planning) |

What it lacks today: an `s339` anchor block in `docs/planning/invest/scope-corrections.md`, and its todo-211 anchor is a scope index, not immediately followed by its sequence-task-606 anchor in `requirements.md`; its ledger entry already has one `Owner **S339**` and one todo link. S7 widens the reader (owner decision 15), including for companion packages and TH, RAD and GAP identities.

## 8. Delivery

- **Slice shape.** Single element, one package.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which includes ENGINE-SUPPLY-DEMAND (the Motor and Battery train that turns the shaft) and ENGINE-ISLANDS-SLEEP; then S7 (the coverage follow-on, which widens the reader, owner decision 15). S3 and S4 only reduce the edit list.
- **Files added.** `elements/RotaryToLinearConverter/RotaryToLinearConverter.csproj`, `RotaryToLinearConverterElementDeclaration.cs`, `rotary_to_linear_converter.tscn`, `icon.svg`; `CuriousContraptions.tests/RotaryToLinearConverterTests.cs` and `RotaryToLinearConverterArtTests.cs`.
- **Files edited.** No test-project edit: published main compiles every top-level `*.cs` by one rule. Until S3: `CuriousContraptions.slnx`, the trim list ending at `CuriousContraptions.tests/CSharpStaticGateTests.cs:703`, and the parameter and placement switches at `CuriousContraptions.tests/PackageRuntimeParityTests.cs:483` and `:641`. At delivery: the binding section of section 7 appended to this spec. This spec and `TODO.md`.
- **Review tier.** Data-only expected: every capability is present with a cited test and the diff is package, tests and list lines.
- **Disqualifiers (the slice's probe re-checks).** (1) The slider stop does not hold the carriage within 0.01 m at 4,000 N (strongest Motor, shortest lead): then either `lead`'s minimum rises or a stop-strength repair makes the slice engine tier. (2) A bundle entry with a `DriveIn` port on the driving hinge of a bilateral revolute-to-prismatic row is refused by a rule not read here. (3) The cylinder collider on the shaft pulley and the box collider on the carriage cannot both come from scene geometry on moving bodies. (4) A `LocalRotationZ` binding on a node under a rotated parent does not turn about the parent's Z (not verified in code): then the screw is drawn without rotation and the pulley witness mark carries the motion.
