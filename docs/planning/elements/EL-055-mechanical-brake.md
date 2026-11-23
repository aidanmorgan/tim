# EL-055 · Mechanical brake declaration readiness spec

**Current delivery (24 October 2026):** [EL-055 delivery slice](../invest/named-elements.md#element-055). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 named-identity spec. Baseline commit `a6c914e`. Legacy citations use `path@a6c914e:Lstart-Lend` and stay retrievable from git history after the Epic 7 purge. Values marked **proposed** are design values the owner may revise. Sections 1 to 3 and 5 to 8 were rewritten on 21 November 2026 against checkout `8d1d68aa`; their `path:line` citations are current code, read that day.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-055 |
| Name | Mechanical brake (player title "Brake") |
| Type | Mechanical |
| Requirement anchor | [element-055](../requirements.md#element-055); scope index [todo-375](../requirements.md#todo-375) |
| Named entry | [named-elements.md#element-055](../invest/named-elements.md#element-055); slice `named-el-055`, owner S341 |
| CAT spec refined or extended | none. Related: [CAT-018 clutch](CAT-018-clutch.md) (same control idiom; its requirement keeps "slip/brake variants separate"), [CAT-042 motor](CAT-042-motor.md), [CAT-039 linear pusher](CAT-039-linear_pusher.md) (self-locking hold, not a brake element) |
| Related identities | [EL-056 ratchet](EL-056-ratchet.md), [EL-053](EL-053-rotary-to-linear-converter.md) and [EL-063 cable winch](EL-063-cable-winch.md) (carriers restrained through their shaft), [TH-05 friction brake](TH-05-friction-brake.md) (a later thermal criterion on this package) |
| Roadmap story | [EL-055 delivery slice](../invest/named-elements.md#element-055) |
| Status | Not started; blocked on the engine programme (owner decision 3a), relying most on ENGINE-SUPPLY-DEMAND (its supplied drive and the Motor train it holds) and ENGINE-CONTACT-OBSERVATIONS (the Impact switch's contact trigger, row C6); then S7. No package exists under `elements/`; every descriptor kind the declaration names is present and tested (section 3) |
| Disposition | To deliver as a single element. TH-05 is not a second package: it adds a thermal proof to this one after `ENGINE-THERMAL` |
| Planned package | id `mechanical_brake`, directory `elements/MechanicalBrake/`, class `MechanicalBrakeElementDeclaration` |
| Coverage binding | Planned, not yet live (section 7); blocked on S7. No CAT number |
| delivery-batches class | Mentioned only in [delivery-batches](../invest/delivery-batches.md) 2d (TH-05 extends it); not classified in 1a, 1b or 1c. Its epic text at `25df3cd5` listed "a holding brake" for revolute coordinates as missing (`_bmad-output/planning-artifacts/epics.md@25df3cd5:4459`, the former ENGINE-ASSEMBLY-JOINTS text). Code corrects that for this part: a supplied drive with target speed 0 already holds a shaft as a brake and is tested. Proposed class: 1b run "Rotary" |

## 2. Declaration

Follows the Motor (`elements/Motor/MotorElementDeclaration.cs:16`) for the supplied hinge and the Conveyor (`elements/Conveyor/ConveyorElementDeclaration.cs:42`–`:52`) for a pass-through shaft with both ports on one coordinate. Interfaces: `IHingeElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:20`), `ISupplyElement` (`CuriousContraptions.Abstractions.Electrical/ElectricalContracts.cs:11`), `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`). `KinematicMode.Static`; domains `Mechanical | Electrical`; footprint 2 bodies, 1 moving, 0 prismatic, 1 revolute. No executable logic.

**Coordinator guidance confirmed.** The brake is a pass-through shaft whose hinge carries a supplied drive with target speed 0 and a torque bound equal to the brake capacity. It is applied while electrically supplied and free otherwise. The separate clamp "Carrier mode" of the earlier draft is not built; a carrier is restrained through the shaft that moves it.

**Bodies.**

| Body | Shape (m) | Mass and inertia | Joint |
| --- | --- | --- | --- |
| 0 housing (static) | box 1.1 × 0.7 × 0.4 centred at the origin; navy foot box 1.3 × 0.1 × 0.5 centred (0, −0.4, 0) | static | support |
| 1 brake drum and shaft | cylinder r 0.28, width 0.13, axis local Z | 0.25 kg; axial m·r²/2 = 0.0098, transverse m(3r² + w²)/12 = 0.00525 kg·m² (the Clutch shaft, `elements/Clutch/ClutchElementDeclaration.cs:17`–`:19`) | continuous revolute hinge 0 on body 0 |

- **Hinge 0** (`RevoluteHingeDescriptor`, `CuriousContraptions.Abstractions/MechanicalContractValues.cs:149`): base frame (0, 0, 0.27); `RevoluteTravel.Continuous`; friction torque 0.02 N·m (`ClutchElementDeclaration.cs:20`); drive `RevoluteDriveDescriptor(TargetSpeed: Fixed(0), MaxTorque: FromParameter("torque_capacity"))` (`MechanicalContractValues.cs:138`).
- **Supply.** One `SupplyConsumerDescriptor` with a `SupplyDriveTarget(Revolute 0)` bound to `PowerIn`, exactly as the Motor (`MotorElementDeclaration.cs:120`–`:122`). One electrical node "coil": Consumer, 12 V, 20 W, resistance 12² / 20 = 7.2 Ω, the Clutch coil (`ClutchElementDeclaration.cs:156`–`:158`).
- **Material** for both bodies (restitution, friction, rolling resistance, bounce threshold): (0, 0.3, 0, 0.1) (`ClutchElementDeclaration.cs:89`–`:91`). The lining is the drive's torque bound, not a contact material.
- Thermal (added by TH-05's slice after ENGINE-THERMAL): nodes `drum` on body 1 (30.5 J/K, 1.0 W/K ambient, face the drum's cylinder collider, 64 W/K) and `shoe` on body 0 (21 J/K, 0.5 W/K, face the housing box, 16 W/K); frictional sources from hinge 0's drive row (shares 0.5 and 0.5) and its bearing row (share 1.0 to `drum`), each summed over the tick's substeps; three printed heat marks and two shoe-glow bindings.
  Its probe row "Heat from dissipated brake work" (section 3) stays TH-05's. The thermal values follow the thermal conventions owned by [TH-04 section 11](TH-04-electrical-heating-plate.md#11-thermal-conventions-owned-here-every-thermal-spec-cites-this-section), which this spec cites and does not restate. EL-055's own slice declares none of this.

**Parameters.**

| Key | Kind | Unit | Default | Min | Max | Editor policy | Run edit |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `torque_capacity` | Real | `NewtonMetre` | 30 | 1 | 200 | Reject | BuildOnly (as the Motor's `torque`, `MotorElementDeclaration.cs:44`) |

No stores. The target speed is a constant, so the part has no run control.

**Ports.**

| `PortId` | Role | Domain | Direction | Signal kind | Coordinate | Position (m) |
| --- | --- | --- | --- | --- | --- | --- |
| `DriveIn` | `DriveIn` | Mechanical | Input | none | `Revolute 0`, `CoordinatePerRadian` +1 | (−0.42, −0.2, 0.3), normal +Z; `MechanicalRim` radius 0.12 on body 0 (the drawn pulley) |
| `Drive` | `Drive` | Mechanical | Output | none | `Revolute 0`, `CoordinatePerRadian` +1 | (0.42, −0.2, 0.3), normal +Z; `MechanicalRim` radius 0.12 on body 0 (the drawn pulley) |
| `PowerIn` | `PowerIn` | Electrical | Input | none | none | (0, −0.35, 0.25), normal +Z |

Both mechanical ports name the one coordinate, as the Conveyor does. Each rim is drawn at its pulley's radius, as delivered parts do (`elements/Conveyor/ConveyorElementDeclaration.cs:47`, rim 0.22 on a 0.22 pulley); a rim is presentation only (`CuriousContraptions.Abstractions/MechanicalLinkPresentationValues.cs:10`), so no ratio depends on it. The two 0.12 m pulleys sit low on the housing face, 0.465 m from the drum centre, clear of the 0.28 m drum and of the shoes. The package declares no transmission, so the role rule of `CuriousContraptions.Bundle/MechanicalTransmissionValidation.cs:29` does not apply.

**Presentation.** Body binding `Visual/Drum` → body 1. Bindings, all driven by `PresentationDriver.CommittedActuatorDrive` for `Revolute 0` (`CuriousContraptions.Abstractions/ElementPresentation.cs:23`; the Motor lamp, `MotorElementDeclaration.cs:25`–`:27`): `Visual/ShoeLeft` `LocalX` −0.36 → −0.30 and `Visual/ShoeRight` `LocalX` 0.36 → 0.30 over 0.1 s; `Visual/Lamp` albedo slate `#556573` → gold `#f7cb52` over 0.1 s. `Visual/PulleyIn` and `Visual/PulleyOut` `LocalRotationZ` follow `CommittedRevoluteAngle` (`ElementPresentation.cs:30`) as the Conveyor's output pulley (`ConveyorElementDeclaration.cs:20`). Easing is presentation only; the physical state changes in the tick the supply changes.

**Palette.** Category `ElementCategory.Power` (as the Clutch, `ClutchElementDeclaration.cs:55`), palette order 305 (TH-05 is a later criterion on this package and takes none; coordinator decision 36, [palette allocation](readiness/palette-allocation.md)), colour (0.4, 0.72, 0.79) `#66b8c9` (the motor and clutch colour, `DESIGN.md:239`), title "Brake", icon `res://elements/MechanicalBrake/icon.svg`, scene `mechanical_brake.tscn`.

### Art and solidity

| Node | Mesh and dimensions (m) | Class |
| --- | --- | --- |
| `Housing` | cream box 1.1 × 0.7 × 0.4 with a round window on the front face | solid, box collider on body 0 |
| `Foot` | navy box 1.3 × 0.1 × 0.5 | solid, box collider on body 0 |
| `Visual/Drum` | pale grey drum r 0.28, width 0.13, proud of the front face | solid, cylinder collider on body 1 |
| `Visual/ShoeLeft`, `Visual/ShoeRight` | slate curved shoes 0.06 thick and 0.12 tall beside the drum | decorative: moved by a presentation binding |
| `Visual/PulleyIn`, `Visual/PulleyOut` | ochre link pulleys r 0.12, 0.06 wide, at the two mechanical ports, inside the housing outline | decorative: turned by a presentation binding |
| `Visual/Lamp` | lens r 0.04 set flush in the housing face | decorative: flush, glow |

Colours: Cream `#fff8e9` (part-body cream, owner decision 4), Navy `#1c2836`, Gold `#f7cb52`, Ochre `#d99e32` (`DESIGN.md:80`); slate base and pale grey metal (`DESIGN.md:248`). Icon: a drum between two shoes, with a small lightning mark. At a glance it differs from the Clutch (two discs that close along the shaft axis) by one drum gripped from both sides, and from the Motor by having link pulleys on both sides and no rotor fan.

### Behaviour and numbers

- **Law.** While `PowerIn` is supplied, one velocity row drives the shaft's speed toward 0 with |torque| ≤ `torque_capacity`. That is Coulomb braking: it holds any load below capacity, and above capacity the shaft slips against a constant opposing torque equal to the capacity. A drive that only resists motion adds no work (`CuriousContraptions.tests/RevoluteDriveTests.cs:353`), which is the requirement's "restraint cannot generate shaft work".
- **Released** (unsupplied) the drive applies no torque (`CuriousContraptions.tests/RevoluteDriveTests.cs:259`); only the 0.02 N·m bearing friction remains, the same loss as a Clutch shaft, so "released brake permits motion".
- **Capacity 30 N·m default** exceeds the default Motor's 20 N·m (`MotorElementDeclaration.cs:44`), so a default brake stalls a default Motor. **Range 1 to 200 N·m**: the floor is 50 times the bearing friction so an engaged brake always restrains; the ceiling is twice the Motor's 100 N·m maximum so some setting holds any Motor. The earlier 100 N·m ceiling could only tie the strongest Motor.
- **Slip.** A Motor is a torque-bounded speed drive, so against a slipping brake it still reaches its target speed if its torque exceeds the capacity; the brake then removes capacity × speed watts (30 N·m × 6 rad/s = 180 W at defaults if it were slipping), paid by the Motor. Static and kinetic capacity are equal.
- **Control.** Applied while powered, like the Clutch closing while powered. "Hold until a trigger" is a delivered NOR gate used as an inverter between the trigger and `PowerIn` (`elements/ElectricalNor/ElectricalNorElementDeclaration.cs:25`–`:28`); a Motor is supplied through a gate the same way (`CuriousContraptions.tests/MotorTests.cs:497`).
- **Carrier.** A carrier is held through its shaft: a braked Conveyor roller holds cargo still on the belt; a braked EL-053 shaft holds its carriage because that row is bilateral.
- **Tick order.** The electrical solve commits the supplied state; the drive row reads it in the physics step of the same tick, as for the Motor (`CuriousContraptions.tests/MotorTests.cs:361`).

## 3. Engine capabilities (capability probe)

| Capability | Status | Symbol | Exercising test |
| --- | --- | --- | --- |
| Static housing and moving drum with colliders from scene geometry | present | `ISceneGeometryElement` `CuriousContraptions.Abstractions/SceneGeometry.cs:10` | `CuriousContraptions.tests/MotorTests.cs:1075` |
| Continuous hinge with bearing friction | present | `RevoluteHingeDescriptor` `CuriousContraptions.Abstractions/MechanicalContractValues.cs:149` | `CuriousContraptions.tests/RevoluteHingeFrictionTests.cs:59`; `CuriousContraptions.tests/ClutchTests.cs:567` |
| Supplied revolute drive with target speed 0 holds a loaded shaft; unsupplied it is free | present | `RevoluteDriveDescriptor` `MechanicalContractValues.cs:138`; `RevoluteDriveKind.Supplied` `CuriousContraptions.Abstractions/RevoluteConstraintValues.cs:22` | `CuriousContraptions.tests/RevoluteDriveTests.cs:371` (both supplied and unsupplied); package level `CuriousContraptions.tests/MotorTests.cs:573` |
| An overloaded drive is back-driven and adds no work | present | `RevoluteDriveDeclaration` `RevoluteConstraintValues.cs:28` | `CuriousContraptions.tests/RevoluteDriveTests.cs:353`; `CuriousContraptions.tests/MotorTests.cs:675` |
| A constant (non-parameter) drive target compiled from a package | present | `MechanicalValue.Fixed` `MechanicalContractValues.cs:170`; admission `CuriousContraptions.Bundle/BundleValidator.cs:825` | `CuriousContraptions.tests/ClutchTests.cs:1107` |
| Torque bound from a build-only N·m parameter | present | `BundleValidator.cs:826` | `CuriousContraptions.tests/MotorTests.cs:1033`, `:242` |
| Two ports (`DriveIn`, `Drive`) on one revolute coordinate relaying rotation | present | `PortDescriptor.Coordinate` `CuriousContraptions.Abstractions/PortDescriptor.cs:67` | `CuriousContraptions.tests/ConveyorTests.cs:417` |
| A hinge that has a supplied drive and also takes a Mechanical link on `DriveIn` | present | port admission `BundleValidator.cs:373` | `CuriousContraptions.tests/ReverseTransmissionLoadTests.cs:114` |
| Supply consumer targeting a drive; switched through a logic gate | present | `SupplyDriveTarget` `CuriousContraptions.Abstractions/SupplyContractValues.cs:52` | `CuriousContraptions.tests/MotorTests.cs:990`, `:497` |
| Art bound to the committed supplied state and hinge angle | present | `PresentationDriver.CommittedActuatorDrive` `CuriousContraptions.Abstractions/ElementPresentation.cs:23` | `CuriousContraptions.tests/MotorTests.cs:265`; `CuriousContraptions.tests/ConveyorTests.cs:608` |
| Reset zeroes drive state and restores the pose | present | `RevoluteStateRead` `RevoluteConstraintValues.cs:40` | `CuriousContraptions.tests/RevoluteDriveTests.cs:543` |
| Heat from dissipated brake work (TH-05 only) | missing | none | `ENGINE-THERMAL`; not required by EL-055 |

Counts for EL-055: 11 present, 0 untested, 0 narrower, 0 missing (the thermal row belongs to TH-05). Nothing needs element-specific engine code.

**Not needed, so not claimed.** No revolute holding brake without supply is used. A brake that holds with no supply (spring-applied, power to release) would be a hold of `ENGINE-JOINT-KINDS`, which adds holds without supply only where a consumer needs them ([engine stories index](readiness/engine-stories-index.md#4-contradictions-reconciled) C14); this part does not need one.

**Element dependencies.** Motor, Battery, Conveyor, NOR gate, Impact switch (all delivered).

### Engine conformance

- **Declaration data only.** One hinge with a supplied zero-speed drive and one electrical consumer. No callback, script or engine branch on this part.
- **Generic laws.** Braking is the supplied revolute drive law with target speed 0 and the declared torque bound; release is the same drive unsupplied. The drawn shoes do no physics: nothing reads "shoe touches drum".
- **Layers.** Colliders: 2 static boxes (housing, foot) and 1 drum cylinder. Visuals: the art table, none used as a collider. Animation: `Visual/Drum` follows a body pose; shoes, lamp and link pulleys are moved by presentation bindings that read the committed supplied state and hinge angle, and are decorative; `Housing` and `Foot` are moved by nothing. Visible = solid: housing, foot and drum each have a collider; the shoes and pulleys are binding-moved decorative art that nothing must push or block, and the lamp is flush.
- **One-way flow and typing.** Bindings read committed state and write nothing back. Port roles and domains are enums; `torque_capacity` is a typed f32 parameter.
- **Flat state.** 2 bodies, 3 colliders (2 boxes, 1 cylinder), 1 revolute joint, 1 drive, 1 supply node, 1 electrical node, 0 sensors, 0 stores, 1 body binding and 7 state bindings (2 shoe positions, 3 lamp albedo, 2 pulley rotations).
- **Performance.** Three primitive colliders, as the Motor; the housing is static. The supply debit of the drive is ENGINE-SUPPLY-DEMAND's. No island sleeping exists today; a resting drum costs nothing only once ENGINE-ISLANDS-SLEEP (owner decision 3a) is delivered, ahead of this part.
- **Tests are scenarios.** Rows C1 to C12 run the production simulation through placed parts, Mechanical links (the delivered rigid connection; coordinator decision 36) and wires.

## 4. Sources and legacy

- Requirements row: "Released brake permits motion; restraint cannot generate shaft work." No variants.
- Current research row: "Clutch / Brake (EL-055) — engagement constraint between shafts, controller input; stops its load without deleting upstream energy" (`docs/component-research.md@a6c914e:L77-L77`).

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | A velocity-boundary brake removes exactly the body's kinetic energy (2 J for 1 kg at 2 m/s), supplies zero work, and a later release supplies only paid work. | `CuriousContraptions.tests/ConstrainedPoweredImpulseTests.cs@a6c914e:L12-L34` | carry forward (acceptance arithmetic) | Matches "restraint cannot generate shaft work". |
| 2 | An empty-supply motor cannot start motion but can brake: effort −4, supplied 0, dissipated 4. | `CuriousContraptions.tests/MotorPredictionTests.cs@a6c914e:L136-L147` | carry forward (acceptance) | Braking is dissipative only. |
| 3 | Electrical supply loss is not a brake: an unloaded rotor coasts. | `CuriousContraptions.tests/MechanicalTests.cs@a6c914e:L120-L124` | carry forward | Released brake must not damp the shaft either. |
| 4 | Transmission engagement is a closed enum {Open, Engaged}. | `engine/physics/PhysicsTransmissionJoint.cs@a6c914e:L7-L7` | carry forward (pattern) | Brake state is likewise an enum. |
| 5 | Braking computed by a CPU constrained-impulse predictor. | `CuriousContraptions.tests/ConstrainedPoweredImpulseTests.cs@a6c914e:L20-L20` | do not carry forward | CPU solver path. |

**Files harvested:** `CuriousContraptions.tests/ConstrainedPoweredImpulseTests.cs`, `CuriousContraptions.tests/MotorPredictionTests.cs`, `CuriousContraptions.tests/MechanicalTests.cs`, `engine/physics/PhysicsTransmissionJoint.cs`, `parts/WoundSpringPart.cs`. No brake part exists in `parts/` or `content/puzzles.json`.

## 5. Acceptance outline

Requirement row: [element-055](../requirements.md#element-055). Preserved outcomes: a released brake permits motion; restraint cannot generate shaft work; it restrains a supplied shaft or carrier.

- **Chrome UI recipe (pending, post-catalogue).** Place two Batteries, a Motor, the Brake, a Conveyor, a NOR gate and an Impact switch. Wire Battery → Motor; join Motor `Drive` → Brake `DriveIn` and Brake `Drive` → Conveyor `DriveIn` with Mechanical links; wire the NOR gate's `Supply` → Brake `PowerIn` with the switch feeding a gate input; a ball rolls onto the switch. Verify placement, `torque_capacity` and all links from the committed read. Expected: the Conveyor is held until the ball trips the switch, then runs.

Native rows. Planned tests live in `CuriousContraptions.tests/MechanicalBrakeTests.cs`; all names are planned. "Held" means shaft travel within 0.01 rad over the observation, the bound of `CuriousContraptions.tests/RevoluteDriveTests.cs:371`: 3 mm at the drum rim, below what a player can see.

| Row | Given / When / Then | Planned test | Control | Boundary | Tolerance and stall guard |
| --- | --- | --- | --- | --- | --- |
| C1 | Supplied default Motor linked through a supplied default Brake to a Conveyor carrying a Basketball. For 2 s the shaft is held, the ball moves under 0.02 m, and the Motor's work stays under 1 % of torque × target speed × time | `MechanicalBrakeTests.AnAppliedBrakeHoldsASuppliedMotorAndItsConveyor` | C2 | none | held; stall bound of `RevoluteDriveTests.cs:307`. A creeping held belt would let cargo leak past a "stop" |
| C2 | Same construction, Brake unsupplied: the shaft reaches the Motor's speed and the ball is carried at least 1 m | `MechanicalBrakeTests.AReleasedBrakePermitsMotion` | C1 | none | shaft speed within 2 % of the same run with the Brake removed (`CuriousContraptions.tests/GuideResponseTests.cs:174`): a released brake that dragged would slow every machine. Guard 4 s |
| C3 | A shaft coasting at 6 rad/s with the Motor cut; the Brake is supplied. The shaft stops and never reverses; the brake's committed work stays under 0.01 J and the system's kinetic energy never rises | `MechanicalBrakeTests.BrakingRemovesMotionAndAddsNoWork` | Brake unsupplied: the shaft is still turning after the same time | none | reverse travel under 0.01 rad; energy slack as `RevoluteDriveTests.cs:353`. Guard 2 s |
| C4 | Motor torque 20 N·m against capacity 22 N·m: held. Capacity 18 N·m: the shaft turns more than 1 rad in 2 s | `MechanicalBrakeTests.TheBrakeHoldsBelowItsCapacityAndSlipsAboveIt` | each case is the other's control | capacity 1 and 200 N·m with a 0.5 and a 100 N·m Motor | ±10 % around the threshold so solver softness does not decide the row |
| C5 | While slipping (C4, 18 N·m) the brake's work stays under 0.01 J and total kinetic energy never exceeds the Motor's committed work | `MechanicalBrakeTests.ASlippingBrakeGeneratesNoShaftWork` | none | none | 0.01 J plus the 3 % energy slack of `RevoluteDriveTests.cs:238` |
| C6 | Brake supplied through a NOR gate fed by an Impact switch: held until the ball trips the switch, then C2's motion | `MechanicalBrakeTests.ATriggerReleasesAHeldLoadThroughAnInvertingGate` | no ball: held for the whole run | none | causal order only (switch occurrence before first shaft travel above 0.01 rad). Guard 6 s |
| C7 | Carrier: C1's Basketball on the braked Conveyor is the delivered carrier case. Once EL-053 exists, a vertical Screw slide carrying a Weight is held by an applied Brake and descends when released | `MechanicalBrakeTests.AnAppliedBrakeHoldsACarrierThroughItsShaft` (Conveyor now; the Screw slide case is added by whichever of EL-053 and EL-055 is delivered second) | released Brake: the carrier moves | none | carrier position within 0.02 m |
| C8 | Run then Reset restores drum pose, zero travel and work, lamp slate | S4 row 3 once delivered, else `MechanicalBrakeTests.ResetRestoresTheAuthoredState` | n/a | Reset while slipping | authored values, exact |
| C9 | Save and load round-trip `torque_capacity`, both Mechanical links and the power wire | S4 row 1 once delivered, else `MechanicalBrakeTests.ASavedBrakeLoadsWithItsCapacityAndLinks` | n/a | both bounds | exact field values |
| C10 | `torque_capacity` below 1, above 200 or non-finite is refused atomically | S4 row 2 once delivered, else `MechanicalBrakeTests.AnOutOfRangeCapacityIsRefused` | bounds admitted | both bounds | typed refusal |
| C11 | A signal wire into `PowerIn`, an electrical wire into `DriveIn` and a second Mechanical link into `DriveIn` are refused | S4 row 5 once delivered, else `MechanicalBrakeTests.AWrongKindConnectionIsRefused` | valid links admitted | none | `WorkshopEditRejection.InvalidConnection` (the Motor's equivalent: `CuriousContraptions.tests/MotorTests.cs:316`) |
| C12 | Collider and art agree for housing, foot and drum; shoes and lamp follow the committed supplied state | S4 row 4 once delivered, else `MechanicalBrakeArtTests.TheShoesAndLampFollowTheCommittedSupply` | unsupplied: shoes open, lamp slate | n/a | sizes within 0.005 m |

Integrations: cross-element task [sequence-task-370](../requirements.md#sequence-task-370) under [todo-375](../requirements.md#todo-375), "a clutch/brake chooses drive or restraint": planned `MechanicalBrakeTests.AClutchAndABrakeChooseDriveOrRestraint` (Motor → Clutch → Brake → Conveyor; clutch powered and brake unpowered drives, the reverse holds), each mechanism separately implemented and taught.

## 6. Open questions

**Resolved**

1. Command: continuous electrical supply, applied while powered. Decided now (coordinator decision, confirmed against code): it is the Clutch's idiom, the supplied drive is the tested capability, and an inverting gate gives "hold until a trigger". An activation-latched brake would need a signal-commanded drive, which does not exist.
2. Slip law: constant torque equal to capacity; no separate static and kinetic values. Decided now: one bound is what the drive row provides and no row or lesson needs stick-slip.
3. Carrier mode: not a separate mode or a `Restrains` attachment. Decided now: a carrier is restrained through its shaft, which keeps one law and needs no new enum member; the requirement's carrier outcome keeps row C7.
4. Heat from braking: TH-05, after `ENGINE-THERMAL`. delivery-batches 2d.
5. `target` and `initial_state` parameters: removed (follow from 1 and 3). D19.
6. `torque_capacity` ceiling raised from 100 to 200 N·m so a setting exists that holds the strongest Motor. D1.
7. Category Power and the motor/clutch colour, not Motion and ochre: it is an electrically actuated drive-train control like the Clutch. D9.
8. The ElectricalPower family missing from the map binding: D14.

**Needs the owner**

None. For information: a fail-safe brake (applied with no power) is not provided; if a lesson needs one without an inverting gate it would need a general hold-without-supply capability that no story of owner decision 3a names yet (section 3).

## 7. Integration

- **Collaborators.** Motor, Battery, Conveyor, Clutch, Reverse gear, NOR and NAND gates, Impact switch, Pressure plate (all delivered). EL-053 and EL-063 (not delivered) are carriers held through their shafts; EL-054 uses this part as its jammed consumer.
- **Campaign.** "clutch, brake": first use levels 31–40, reuse 41–50, 61–90, 136–150 (`docs/planning/invest/campaign-reservations.md:98`).
- **Lesson fixtures.** A level that places this part needs S8.

### Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of the identity's spec headed "Package source binding", read by `tools/Coverage/NamedPackageSources.cs` (on `origin/main`, `25df3cd5`; pattern: EL-239 Baseball). The section is not written here: the reader parses every EL spec that carries that heading and refuses one whose preconditions fail. **Blocked on the coverage follow-on story S7.**

| Field | Planned value |
| --- | --- |
| Package | `mechanical_brake` (TH-05 is later recorded on the same package; S7 admits that second identity, `readiness/batch-n.md` section 7 item 9) |
| Named identity | EL-055, `../invest/named-elements.md#element-055` |
| Acceptance | `todo-375` and `sequence-task-608` |
| Decision | S341; `scope-corrections.md` has no `s341` block |
| Mode records | `Configuration=Adjustable` (it declares a parameter; confirmed at slice planning) |

What it lacks today: an `s341` anchor block in `docs/planning/invest/scope-corrections.md`, and its todo-375 anchor is a scope index, not immediately followed by its sequence-task-608 anchor in `requirements.md`. S7 widens the reader (owner decision 15), including for companion packages and TH, RAD and GAP identities.

## 8. Delivery

- **Slice shape.** Single element, one package.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which includes ENGINE-SUPPLY-DEMAND (the brake's supplied drive and the Motor train it holds) and ENGINE-CONTACT-OBSERVATIONS (the Impact switch's contact trigger, row C6); then S7 (the coverage follow-on, which widens the reader, owner decision 15). S3 and S4 reduce the edit list. TH-05's thermal criterion follows later, after ENGINE-GAS-NETWORK, ENGINE-LIQUID and ENGINE-THERMAL (owner decision 18).
- **Files added.** `elements/MechanicalBrake/MechanicalBrake.csproj`, `MechanicalBrakeElementDeclaration.cs`, `mechanical_brake.tscn`, `icon.svg`; `CuriousContraptions.tests/MechanicalBrakeTests.cs` and `MechanicalBrakeArtTests.cs`.
- **Files edited.** No test-project edit (published main compiles every top-level `*.cs` by one rule). Until S3: `CuriousContraptions.slnx`, the trim list ending at `CuriousContraptions.tests/CSharpStaticGateTests.cs:703`, the switches at `CuriousContraptions.tests/PackageRuntimeParityTests.cs:483` and `:641`. At delivery: the binding section of section 7 appended to this spec. This spec and `TODO.md`.
- **Review tier.** Data-only expected.
- **Disqualifiers (the slice's probe re-checks).** (1) A supplied Motor and a supplied zero-target drive in one linked chain do not settle (the shaft hunts or creeps beyond 0.01 rad in C1): two opposed drives on connected coordinates are each tested alone but not together under supply. (2) The bundle validator refuses a drive on a hinge that carries both `DriveIn` and `Drive` (no such rule was found in `BundleValidator.cs:373`–`:397` or `SupplyBundleValidation.cs`). (3) `CommittedActuatorDrive` cannot drive a `LocalX` property (the Motor uses it for albedo only; the validator has no rule against it, `CuriousContraptions.Bundle/BundleValidator.cs:323`–`:326` checks only that an actuator driver names an actuator, so a refusal would come from the compiler or presentation, not admission); then the shoes use the lamp's colour change alone and stay still.
