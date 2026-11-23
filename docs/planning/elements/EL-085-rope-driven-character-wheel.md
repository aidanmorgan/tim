# EL-085 · Rope-driven character wheel — named-identity spec

**Current delivery (24 October 2026):** [EL-085 delivery slice](../invest/named-elements.md#element-085). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

**Build-ready revision, 22 November 2026 (checkout `8d1d68aa`), repaired the same day against the independent audit (`docs/planning/elements/readiness/audits/elements-batch-l.md`) and the owner decisions of 22 November 2026.** A `path:line` citation without a commit is in this checkout; `T/` abbreviates `CuriousContraptions.tests/` and `A/` abbreviates `CuriousContraptions.Abstractions/`. Values marked **starting value** are adopted under decision D1 of [delivery batches](../invest/delivery-batches.md#3-family-level-decisions). Game values are canonical f32. Nothing here was built or simulated; every number is a hand calculation shown beside it.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-085 · Rope-driven character wheel · Character |
| Anchor | [requirements.md#element-085](../requirements.md#element-085); [named entry](../invest/named-elements.md#element-085); scope index [todo-200](../requirements.md#todo-200) |
| Proof owner | S655 |
| Disposition | To deliver as a single element. Not a duplicate of the Pulley (a rope guide with no shaft output), the Impact lever (a limited beam) or the Motor (electrical). It is the rope-to-shaft converter: a rope unwinding from its drum turns a wheel whose shaft drives other parts. |
| Planned package | id `character_wheel`, directory `elements/CharacterWheel/`, class `CharacterWheelElementDeclaration` |
| Refines / extends | [CAT-058 Rope anchor](CAT-058-rope_anchor.md) and [CAT-053 Pulley](CAT-053-pulley.md) (rope attachment and routing; their acceptance is retained by those parts). Shaft pattern of [CAT-042 Motor](CAT-042-motor.md) |
| Status | not started; blocked on the engine programme (owner decision 3a), then ENGINE-TENSILE-EXTENSIONS (winding) and ENGINE-CONNECTION-MATERIALS (solid strand), then S7 |

## 2. Declaration

**What the part is.** A heavy disc wheel on a frame with a small rope drum on its shaft. A ring of running toy figures is printed on the wheel's faces, so they run when it turns. A falling Weight on a rope wound round the drum turns the wheel; the wheel's shaft socket drives a Conveyor or any other shaft part. No controller, script or update loop; the work comes from rope tension through the general tensile and hinge laws.

Interfaces: `IHingeElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:20`), `ISceneGeometryElement` (`A/SceneGeometry.cs:10`), and the winding declaration ENGINE-TENSILE-EXTENSIONS adds. `KinematicMode.Static` base; `SupportedDomains = Mechanical`. Footprint: 2 bodies, 1 moving, 1 revolute constraint. Closest delivered shape: the Motor's housing and rotor (`elements/Motor/MotorElementDeclaration.cs:16`) without a drive.

### Bodies and colliders

Local frame: origin at the centre of the base's underside, +Y up, shaft along Z.

| Body | Colliders (m) | Mass and inertia | Material |
| --- | --- | --- | --- |
| 0 frame (static) | base: box half extents (0.80, 0.05, 0.30) at (0, 0.05, 0); two uprights: boxes half extents (0.05, 0.75, 0.05) at (0, 0.85, ±0.25) | static | restitution 0, friction 0.3, bounce threshold 0.1 m/s |
| 1 wheel (on the hinge) | disc: cylinder r 0.60, half-width 0.05, axis Z, at the hinge origin; drum: cylinder r 0.15, half-width 0.07, axis Z, at (0, 0, 0.12) | 4.0 kg (disc 3.6, drum 0.4); axial ½·3.6·0.60² + ½·0.4·0.15² = 0.6525 kg·m²; transverse about 0.34 kg·m²; centre of mass (0, 0, 0.012); gravity (0, −9.81, 0) | restitution 0, friction 0.3, bounce threshold 0.1 m/s |

Geometry check: the shaft is at y 0.85; the disc's lowest point (0.25) clears the base top (0.10); the disc (z ±0.05) and drum (z 0.05 to 0.19) lie between the uprights' inner faces at z ±0.20; the uprights rise to 1.60, above the disc's top at 1.45. Overall 1.60 × 1.60 × 0.60 m.

### Hinge and winding

One `RevoluteHingeDescriptor` (`A/MechanicalContractValues.cs:149`): `Support: new(0)`, base frame (0, 0.85, 0), axis Z, `RevoluteTravel.Continuous`, stiffness and damping 0, friction torque 0.5 N·m, `BodiesCollide` false, no drive.

One winding (ENGINE-TENSILE-EXTENSIONS): coordinate `Revolute 0`, radius 0.15 m, capacity 4.0 m, minimum 0. The rope's wound end leaves the top of the drum; paying out turns the wheel toward −Z, and tension acts on the coordinate at the 0.15 m radius. At Run start the drum holds the capacity less the rope's connect-time length.

### Ports

| `PortId` | Domain | Direction | Role | Local position |
| --- | --- | --- | --- | --- |
| `Tie` | Rope | Bidirectional | the wound-end attachment role the winding declaration defines, on `PackageBodyIndex(1)` | drum top, (0, 1.00, 0.12) |
| `Drive` | Mechanical | Output | `PortRole.Drive`, `Coordinate: Revolute 0`, `CoordinatePerRadian` +1, rim radius 0.15 | rear upright's outer face, (0, 0.85, −0.31), normal −Z |

The `Drive` port follows the Motor's (`MotorElementDeclaration.cs:52`). Port domains agree with their collaborators: `Tie` joins a Rope connection to a Weight or Rope anchor over Pulleys; `Drive` joins a `DriveIn` of a Conveyor, Reverse transmission or Clutch through the delivered rigid mechanical connection.

### Parameters and stores

None. The drum radius is fixed (D13); the wheel has no store: work enters only as rope tension times rope travel.

### Metadata and bindings

Category `Power`, display name "Runner wheel", palette order 344 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)), colour `#d69c47`. Presentation: one body binding, `Visual/Wheel` → `PackageBodyIndex(1)`. No other binding: the running figures are printed on the wheel and turn with its body.

### Art and solidity

| Node | Mesh | Layer and class |
| --- | --- | --- |
| `Visual/Base`, `Visual/UprightFront`, `Visual/UprightBack` | navy `#293954` boxes | solid, box colliders; static |
| `Visual/Wheel` | cream `#fff8e9` disc r 0.60 with gold `#f7cb52` spokes and a ring of eight original running toy figures, all printed flat on both faces | solid, cylinder collider; follows body 1 |
| `Visual/Wheel/Drum` | ochre `#d69c47` drum r 0.15 with rope turns drawn inside its radius | solid, cylinder collider; follows body 1 |
| the rope strand | drawn by the connection, not by this package | **solid** (owner decision 1 and coordinator decision 16, 22 November 2026): its collider is a property of the declared connection material, owned by ENGINE-CONNECTION-MATERIALS |

Icon: a spoked wheel on an A-frame with running figures round its rim and a rope leaving the hub. It differs from the Pulley by its frame, its size and its shaft socket.

### Behaviour and numbers

1. **Drive (tensile law on a hinge coordinate).** With a 4 kg Weight hanging from the rope, tension T acts at 0.15 m: I·α = T·0.15 − 0.5 and m·a = m·g − T with a = 0.15·α, so α = (4·9.81·0.15 − 0.5)/(0.6525 + 4·0.15²) = 5.386/0.7425 = 7.25 rad/s² and the Weight accelerates at 1.09 m/s².
2. **A 2 m fall** takes √(2·2/1.09) = 1.92 s and leaves the wheel at 13.9 rad/s, inside the 20 rad/s the Motor is allowed. Energy check: 4·9.81·2 = 78.5 J = wheel ½·0.6525·13.9² = 63.0 J + Weight ½·4·2.09² = 8.7 J + hinge friction 0.5·(2/0.15) = 6.7 J.
3. **Slack rope supplies no work.** A tensile span only resists lengthening (`T/RopeTests.cs:34`). While the rope is slack, and after the Weight lands, the tension is zero and the wheel only slows, at 0.5/0.6525 = 0.77 rad/s² (from 13.9 rad/s it stops in 18 s).
4. **Too light a load does nothing.** The Weight must exceed 0.5/(9.81·0.15) = 0.34 kg to overcome hinge friction.
5. **Under load.** Whatever the `Drive` socket turns takes its torque from the same balance; at stall the 4 kg Weight offers 5.9 N·m at the shaft. A held shaft holds the Weight.
6. **Capacity.** The drum pays out at most 4.0 m; a route longer than that is refused at connection.
7. **Tick order.** No signal is read. Rope, hinge and shaft coupling are solved together in the physics step.

## 3. Engine capabilities

| Capability | Status | Evidence |
| --- | --- | --- |
| Hinge on a static base with a declared friction torque | present | `RevoluteHingeFrictionTests.AHingedBarTurnsWithTheTorqueLeftAfterItsFrictionTorque` (`T/RevoluteHingeFrictionTests.cs:59`) |
| A shaft output turning a downstream shaft part | present | `ConveyorTests.ChainedConveyors_DriveRelaysRotationToDownstreamConveyor` (`T/ConveyorTests.cs:417`) |
| Tension-only rope: a slack span applies no force and a landed load releases tension | present | `RopeTests.SlackRopeDoesNotAccelerateTheFallingLoad` (`T/RopeTests.cs:34`); `RopeTests.AtwoodSlackTransitionOnFloorContactReleasesTension` (`T/RopeTests.cs:842`) |
| A rope end tied to a hinge-carried body | present | `elements/ImpactLever/ImpactLeverElementDeclaration.cs:40`; `ImpactLeverTests.RopeLiftingIntegration_FallingWeightLiftsArm` (`T/ImpactLeverTests.cs:414`) |
| A hinge body carrying two cylinder colliders, and a `Drive` port on an undriven continuous hinge | present, untested | admitted by `HingeBodyDescriptor` (`A/MechanicalContractValues.cs:123`); delivered rotors carry one collider and a drive |
| A rope wound on a revolute coordinate of any package, driven or not: pay-out follows the angle at a declared radius within a capacity, and tension reacts on the coordinate | missing | **ENGINE-TENSILE-EXTENSIONS** (owner decision 3a; the winding wording is Batch I's gap G3) |
| A solid rope strand | missing | **ENGINE-CONNECTION-MATERIALS** (owner decision 1 and coordinator decision 16) |

## 4. Sources and legacy

- Requirement: "Rope work drives a visible carrier mechanism under load"; outcome "Slack rope supplies no work" ([element-085](../requirements.md#element-085)).
- The element map lists ProgrammableController for this row; under D14 the map is navigation. The wheel has no controller: the figures are printing.
- Legacy search at `a6c914e` for "wheel", "hamster", "monkey", "treadmill" and "character" found no character-wheel source. [CAT-058](CAT-058-rope_anchor.md) holds the full rope harvest.

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| L1 | A rope is Open if incomplete, Slack below its maximum length, otherwise Taut | `engine/RopeNetwork.cs@a6c914e:L37-L38` | Carry forward the states. Do not carry forward the 0.001 m CPU tolerance. |
| L2 | A rope is tension-only: inactive while slack; when active it only resists lengthening | `engine/physics/PhysicsRopeJoint.cs@a6c914e:L114-L122` | Carry forward the law. Do not carry forward the CPU impulse code. |
| L3 | The Weight is a rope-linked load, default mass 4 | `parts/catalog/weight.tres@a6c914e:L11-L14` | Carry forward as the driving load |

**Files harvested:** `engine/RopeNetwork.cs`, `engine/physics/PhysicsRopeJoint.cs`, `parts/catalog/weight.tres`.

## 5. Acceptance outline

Requirement row: [element-085](../requirements.md#element-085). Planned class `CharacterWheelTests`. The standard scene: a 4 kg Weight hangs from a rope that runs over one Pulley to the wheel's `Tie`. Wheel travel is the committed revolute travel.

| # | Given / When / Then | Planned test | Control | Boundary and tolerance |
| --- | --- | --- | --- | --- |
| R1 | Given the standard scene with a taut rope, when run, then the Weight descends and the wheel turns with it | `AFallingWeightTurnsTheWheel` | no rope: wheel travel under 0.01 rad in 5 s | Weight drop equals 0.15 × wheel travel within 0.02 m plus 2 %; at least 6 rad within a 5 s guard (about 1.3 s expected) |
| R2 | Given `Drive` joined to a Conveyor carrying a Basketball, then the ball is carried along | `TheWheelDrivesAConveyorUnderLoad` | `Drive` not connected: the ball stays within 0.05 m | ball moves at least 0.3 m within a 6 s guard |
| R3 | Given the rope 1 m longer than taut, then the wheel does not turn until the Weight has fallen that metre | `ASlackRopeTurnsNothing` | R1 | wheel travel under 0.01 rad while the drop is under 0.95 m |
| R4 | Given a shelf that catches the Weight after 1 m, then from that moment the wheel's speed never rises and it comes to rest | `OnceTheRopeGoesSlackTheWheelOnlySlows` | R1 | no tick-to-tick rise above 0.05 rad/s; at rest within a 30 s guard |
| R5 | Given R1, then the wheel's kinetic energy plus the Weight's never exceeds the Weight's lost height energy | `TheWheelGainsNoMoreThanTheWeightGivesUp` | — | allowance 0.5 J on about 78 J over 2 m |
| R6 | Given a 0.2 kg load, then the wheel does not turn; with 1 kg it does | `ALoadBelowHingeFrictionDoesNotTurnIt` (theory) | — | 0.2 and 1 kg stand clear of the 0.34 kg threshold; "does not turn" is under 0.05 rad in 5 s |
| R7 | Given `Drive` joined to a Motor supplied at speed 0 (a holding shaft), then the Weight hangs still | `AHeldShaftHoldsTheWeight` | R1 | drop under 0.02 m over 3 s |
| R8 | Reset restores the wheel angle, the wound length and the Weight; save and load keep the rope route and the shaft connection | `ResetAndSaveLoadRestoreTheWheelAndItsRope` | — | generic: `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad` (`T/CatalogueIntegrationTests.cs:273`) |
| R9 | An electrical wire offered to `Drive` or `Tie` is refused; a rope route longer than 4.0 m is refused | `WrongKindWiresAndOverlongRopesAreRefused` | a Rope connection to `Tie` and a Mechanical one to `Drive` are accepted | typed refusal, nothing changed; no parameters, so no parameter refusal applies |
| R10 | Given a Tennis ball rolled into the taut rope, then the rope deflects it | `TheRopeIsSolid` | the same ball with no rope passes | the strand's collider is the connection material's, not this part's |

### Integration

- Collaborators: CAT-067 Weight, CAT-053 Pulley, CAT-058 Rope anchor, CAT-019 Conveyor, CAT-057 Reverse transmission, CAT-018 Clutch, CAT-042 Motor (all delivered); the Rope connection.
- Campaign: chapter 10, levels 91–100, "rope-driven character motor" (`docs/planning/requirements.md:2295`); coverage ledger "rope-to-drive character motor", reuse 136–150 (`:2411`).
- A level that fixes the wheel or pre-wires its rope needs the lesson-fixture story (S8).

### Delivery

- **Shape.** Single element slice.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which includes ENGINE-SPATIAL-QUERIES, ENGINE-CONTACT-OBSERVATIONS, ENGINE-ISLANDS-SLEEP, ENGINE-SUPPLY-DEMAND and ENGINE-CAPABILITY-INSTANCES; then ENGINE-TENSILE-EXTENSIONS (the winding) and ENGINE-CONNECTION-MATERIALS (the solid strand, without which the part's rope does not conform); then S7.
- **Names.** Port roles, node arity, target masks and mouth identities are written in the form ENGINE-CAPABILITY-INSTANCES leaves; the names here are today's.
- **Files.** `elements/CharacterWheel/` (project, declaration, `character_wheel.tscn`, `icon.svg`); `CuriousContraptions.tests/CharacterWheelTests.cs`; one `CuriousContraptions.slnx` line; the coverage binding once S7 admits it.
- **Review tier.** Engine tier (one *present, untested* row).
- **Disqualifiers the probe re-checks.** A winding that cannot sit on an undriven hinge (it must not be tied to a supplied winch); a `Drive` port refused on a hinge with no drive; a Conveyor that the 5.9 N·m stall torque cannot start.

## 6. Open questions

All resolved.

1. **The rope winds on a drum.** A rope tied to a point on the rim was considered because every capability for it exists; it turns the wheel at most 170° and then the wheel's momentum hauls the Weight back up, so it is a pendulum, not a motor. The campaign calls this part a "rope-driven character motor", so it waits for the winding capability. Decided under the standing delegation.
2. **Teaching load**: a Conveyor (D19).
3. **The figures are printed flat on the wheel** and run because the wheel turns (audit M7): nothing proud is moved by a binding.
4. **Rewinding** is whatever the general laws give: driving the shaft backward winds the rope and lifts the Weight. No extra rule.
5. **No `drum_radius` or `rotation_direction` parameters** (D13, D19): the player turns the part to reverse it.
6. **The rope is solid** (owner decision 1 and coordinator decision 16, 22 November 2026).

Needs the owner: none.

## Engine conformance (22 November 2026)

1. **Declaration data only.** A static frame, one hinge body with two colliders, one winding, two ports and one body binding. No controller, script or engine branch on this package.
2. **Generic laws.** The tensile-span law acting on a revolute coordinate at a declared radius; the hinge with Coulomb friction; the mechanical connection's ratio coupling; contact for the frame and wheel.
3. **Three layers.** Colliders: three static boxes and two cylinders on the wheel. Visuals: one mesh per collider; spokes and figures printed flat. Animation: none. The wheel follows its body pose.
4. **Flow and typing.** Physics → presentation only. Port roles, domains and travel mode are enums.
5. **Flat state.** Bodies 2 (1 static, 1 dynamic); colliders 5 (3 boxes, 2 cylinders); joints 1 revolute; drives 0; sensors 0; windings 1; signal and supply nodes 0; stores 0; presentation bindings 1 (body).
6. **Performance.** Five primitives, three static. The disc is one cylinder, not a ring of boxes with spokes. No sleeping exists today; ENGINE-ISLANDS-SLEEP will let a wheel at rest sleep with the rope and Weight in its island, and nothing here depends on it. The rim moves at up to 8.3 m/s; nothing small or thin is involved. The strand's collider cost is the connection material's, bounded by its compiled declaration.
7. **Tests are scenarios.** Every row hangs a real Weight on a real rope in the production simulation.

**Verdict: conforms.** Not deliverable until the engine programme and the two named stories exist; each missing capability has one owning story.

## 7. Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of the identity's spec headed "Package source binding", read by `tools/Coverage/NamedPackageSources.cs` (pattern: EL-239 Baseball). The section is not written here because the reader fails the whole coverage run on a spec whose fields do not resolve. **Blocked on the coverage follow-on story S7.**

| Field | Planned value |
| --- | --- |
| Package | `character_wheel` |
| Named identity | EL-085, `../invest/named-elements.md#element-085` |
| Acceptance | `todo-200` and `sequence-task-638` (`docs/planning/requirements.md:5077`) |
| Decision | S655; `scope-corrections.md` has no `s655` block |
| Mode records | `Configuration=Fixed` |

What it lacks today: a decision block for S655; `todo-200` is a scope index shared by EL-082 to EL-085 (`docs/planning/requirements.md:4091`), and its sequence task sits beside the element row, not directly after the todo anchor as the reader requires.
