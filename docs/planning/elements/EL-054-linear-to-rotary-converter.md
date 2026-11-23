# EL-054 · Linear-to-rotary converter declaration readiness spec

**Current delivery (24 October 2026):** [EL-054 delivery slice](../invest/named-elements.md#element-054). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 named-identity spec. Baseline commit `a6c914e`. Legacy citations use `path@a6c914e:Lstart-Lend` and stay retrievable from git history after the Epic 7 purge. Values marked **proposed** are design values the owner may revise. Sections 1 to 3 and 5 to 8 were rewritten on 21 November 2026 against checkout `8d1d68aa`; their `path:line` citations are current code, read that day.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-054 |
| Name | Linear-to-rotary converter (player title "Rack drive") |
| Type | Mechanical |
| Requirement anchor | [element-054](../requirements.md#element-054); scope index [todo-211](../requirements.md#todo-211) |
| Named entry | [named-elements.md#element-054](../invest/named-elements.md#element-054); slice `named-el-054`, owner S340 |
| CAT spec refined or extended | none. Related: [CAT-019 conveyor](CAT-019-conveyor.md) and [CAT-057 reverse transmission](CAT-057-reverse_transmission.md) (shaft consumers), [CAT-067 weight](CAT-067-weight.md) and the balls (input loads) |
| Related identities | [EL-053](EL-053-rotary-to-linear-converter.md) (inverse conversion); [EL-056](EL-056-ratchet.md) (keeps the gained rotation); EL-219 Rack-and-pinion slide ([ledger](../invest/named-elements.md#element-219)), which is shaft-driven, the opposite direction. The earlier reference to "EL-158 rack-and-pinion slide" was wrong: EL-158 is the Rise-hold-fall cam |
| Roadmap story | [EL-054 delivery slice](../invest/named-elements.md#element-054) |
| Status | Not started; blocked on the engine programme (owner decision 3a), relying most on ENGINE-ISLANDS-SLEEP (a resting rack and load); then S7. No package exists under `elements/`. One capability is present but untested in the composed form, two transmissions in series, whose qualification is `ENGINE-MECHANICAL-LINKS`' (coordinator decision 29; section 3) |
| Disposition | To deliver as a single element |
| Planned package | id `linear_to_rotary_converter`, directory `elements/LinearToRotaryConverter/`, class `LinearToRotaryConverterElementDeclaration` |
| Coverage binding | Planned, not yet live (section 7); blocked on S7. No CAT number |
| delivery-batches class | Not classified in [delivery-batches](../invest/delivery-batches.md) (1a, 1b, 1c and 2 all omit it). Code corrects this: it is declaration data today. Proposed class: 1b run "Rotary", with the probe item "a prismatic coordinate driving a revolute one, compiled from a package" |

## 2. Declaration

Follows `elements/WoundSpring/WoundSpringElementDeclaration.cs:8` with the coupling reversed (slider drives hinge) and no retention, signal node or spring. Interfaces: `ISliderElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:15`), `IHingeElement` (`:20`), `IMechanicalTransmissionElement` (`:90`), `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`). `KinematicMode.Static`; domains `Mechanical`; footprint 3 bodies, 2 moving, 1 prismatic, 1 revolute. No executable logic.

**Bodies.** Local frame: Y along the rack (up in the default pose), Z toward the player.

| Body | Shape (m) | Mass and inertia | Joint |
| --- | --- | --- | --- |
| 0 housing (static) | box 0.8 × 1.3 × 0.5 centred (0, −0.05, 0), spanning y −0.7 to 0.6 | static | support |
| 1 rack with pad | rack box 0.3 × 1.3 × 0.3; pad box 0.5 × 0.06 × 0.5 centred 0.68 above the rack centre | 0.2 kg; solid box moments of the rack (0.0297, 0.003, 0.0297) kg·m², pad mass neglected | prismatic slider 0 on body 0 |
| 2 output pinion | cylinder r 0.28, width 0.13, axis local Z | 0.25 kg; axial 0.0098, transverse 0.00525 kg·m² (the Clutch shaft, `elements/Clutch/ClutchElementDeclaration.cs:17`–`:19`) | continuous revolute hinge 0 on body 0 |

- **Slider 0** (`PrismaticSliderDescriptor`, `CuriousContraptions.Abstractions/MechanicalContractValues.cs:104`): base frame at (0, 0.6, 0), identity rotation (slide axis = local Y); lower limit `Fixed(−0.6)`, upper limit `Fixed(0)`, as the Wound spring's plunger travels downward from 0 (`WoundSpringElementDeclaration.cs:33`); stiffness, damping, compression 0; `BodiesCollide` false; gravity (0, −9.81, 0); no drive. At travel 0 the rack spans y −0.05 to 1.25 and the pad top is at 1.31; at −0.6 the rack spans −0.65 to 0.65 (inside the housing, which ends at −0.7) and the pad top is at 0.71, still 0.11 above the housing top, so a load on the pad never touches the housing.
- **Hinge 0** (`RevoluteHingeDescriptor`, `:149`): base frame (0.25, −0.3, 0.32) on the housing front; `RevoluteTravel.Continuous`; friction torque `Fixed(1.2)` N·m; no drive.
- **Transmission 1** (`MechanicalTransmissionDescriptor`, `:258`): driving `Prismatic 0`, driven `Revolute 0`, ratio `FromParameter("rack_pitch")`, `Bilateral`. A prismatic coordinate driving a revolute one takes a rad/m ratio (`CuriousContraptions.Bundle/MechanicalTransmissionValidation.cs:56`–`:58`). The earlier `pinion_radius` in m/rad would be refused.
- **Material** (restitution, friction, rolling resistance, bounce threshold): housing and pinion (0, 0.3, 0, 0.1), the rigid drive-part material (`ClutchElementDeclaration.cs:89`–`:91`); rack and pad (0.1, 0.5, 0, 0.1), a dead, grippy pad that turns a landing into stroke rather than a rebound.

**Parameters.**

| Key | Kind | Unit | Default | Min | Max | Editor policy | Run edit |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `rack_pitch` | Real | `RadianPerMetre` | 5 | 2.5 | 10 | Reject | BuildOnly (required, `MechanicalTransmissionValidation.cs:70`) |

Stroke is fixed at 0.6 m (D13). No stores.

**Ports.**

| `PortId` | Role | Domain | Direction | Signal kind | Coordinate | Position (m) |
| --- | --- | --- | --- | --- | --- | --- |
| `Drive` | `Drive` | Mechanical | Output | none | `Revolute 0`, `CoordinatePerRadian` −1 | (0.25, −0.3, 0.4), normal +Z; `MechanicalRim` radius 0.28 on body 0 |

`CoordinatePerRadian` −1 (as the Reverse gear and Wound spring ports, `elements/ReverseTransmission/ReverseTransmissionElementDeclaration.cs:28`) makes a descending rack turn a linked consumer (joined by a Mechanical link, the delivered rigid connection; coordinator decision 36) in the Motor's positive direction. A driven coordinate may carry a `Drive` port but not a `DriveIn` (`MechanicalTransmissionValidation.cs:29`). Mechanical ports bind revolute coordinates only (`CuriousContraptions.Bundle/BundleValidator.cs:389`), so the rack has no port; it takes work by contact.

**Presentation.** Body bindings `Visual/Rack` → body 1 and `Visual/Pinion` → body 2. No other binding.

**Palette.** Category `ElementCategory.Motion`, palette order 123 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)), colour ochre (0.84, 0.61, 0.28), title "Rack drive", icon `res://elements/LinearToRotaryConverter/icon.svg`, scene `linear_to_rotary_converter.tscn`.

### Art and solidity

| Node | Mesh and dimensions (m) | Class |
| --- | --- | --- |
| `Housing` | cream box 0.8 × 1.3 × 0.5 with a navy base band printed on its lowest 0.1 | solid, box collider on body 0 |
| `Visual/Rack` | pale grey toothed bar 0.3 × 1.3 × 0.3, teeth as shallow relief within that envelope | solid, box collider on body 1 (the part inside the housing is enclosed) |
| `Visual/Rack/Pad` | cream pad 0.5 × 0.06 × 0.5 | solid, box collider on body 1 |
| `Visual/Pinion` | ochre toothed wheel r 0.28, width 0.13, gold index mark printed on its face | solid, cylinder collider on body 2 |

Colours: Cream `#fff8e9` (part-body cream, owner decision 4), Navy `#1c2836`, Gold `#f7cb52` (`DESIGN.md:80`); ochre `#d69c47` (`DESIGN.md:235`); pale grey metal (`DESIGN.md:248`). The art pinion keeps one size at every `rack_pitch`; the setting is read from the Workbench control, not from the wheel. Icon: an upright toothed bar with a pad on top, a straight arrow turning into a curved arrow. At a glance it differs from EL-053 by standing upright with a pad to drop things on, and from the Springboard by its toothed bar and side wheel.

### Behaviour and numbers

- **Law.** Pinion speed (rad/s) = `rack_pitch` (rad/m) × rack speed (m/s), one bilateral row; pinion torque = rack force / `rack_pitch`. All work enters through the rack by contact and leaves through the shaft; the package has no drive and no store, so "an unmoving input supplies no rotation energy" holds by construction.
- **Bearing friction 1.2 N·m** holds the rack against its own weight in every pose: 0.2 kg × 9.81 = 1.96 N gives at most 1.96 / 2.5 = 0.78 N·m on the pinion. The hinge holds while the net torque is below its friction torque (`CuriousContraptions.tests/RevoluteHingeFrictionTests.cs:77`). The rack therefore stays where it stops; there is no return spring. The friction loss over a full stroke at the default pitch is 1.2 N·m × 3 rad = 3.6 J.
- **Load needed to start the rack**, upright: (1.2 × pitch − 1.96) N. At pitch 5 that is 4.0 N: a resting Tennis ball (0.35 kg, 3.4 N, `elements/TennisBall/TennisBallElementDeclaration.cs:16`) does not move it, a Basketball (1 kg, `elements/Ball/BallElementDeclaration.cs:51`) and a Bowling ball (4 kg, `elements/BowlingBall/BowlingBallElementDeclaration.cs:54`) do. At pitch 10 only loads above 1.02 kg start it (the 1 kg Basketball sits on that threshold, so no acceptance row uses it there); at 2.5, loads above 0.11 kg. A higher pitch gives more turns and needs more weight: that is the gearing lesson.
- **Output.** A full 0.6 m stroke turns the pinion 3 rad at the default (1.5 to 6 rad across the range), which moves a default Conveyor belt 2 m (0.667 m/rad, `elements/Conveyor/ConveyorElementDeclaration.cs:37`). A Bowling ball's 0.6 m descent offers 23.5 J.
- **Speed.** Unloaded, a Bowling ball drives the pinion to about 15 rad/s at pitch 5 and about 26 rad/s at pitch 10. That exceeds the Motor's 20 rad/s maximum (`elements/Motor/MotorElementDeclaration.cs:41`) but is well inside the 128 rad/s drive envelope (`BundleValidator.cs:825`); a linked consumer lowers it.
- **End stop.** At −0.6 m the rack stops and the pinion stops with it; the load rests on the pad.
- No signal, optical or acoustic read.

## 3. Engine capabilities (capability probe)

| Capability | Status | Symbol | Exercising test |
| --- | --- | --- | --- |
| Static and moving bodies with colliders from scene geometry | present | `ISceneGeometryElement` `CuriousContraptions.Abstractions/SceneGeometry.cs:10` | `CuriousContraptions.tests/SliderInstanceTests.cs:76` |
| Prismatic slider with stops, loaded by contact from above | present | `PrismaticSliderDescriptor` `CuriousContraptions.Abstractions/MechanicalContractValues.cs:104` | `CuriousContraptions.tests/SliderInstanceTests.cs:61`; `CuriousContraptions.tests/WorkshopPhysicsLawPrismaticTests.cs:182` |
| Continuous hinge whose Coulomb friction holds a load below its torque | present | `RevoluteHingeDescriptor.FrictionTorque` `MechanicalContractValues.cs:158` | `CuriousContraptions.tests/RevoluteHingeFrictionTests.cs:77`, `:59` |
| Bilateral row with a prismatic coordinate driving a revolute one, in the solver | present | `MechanicalTransmissionDescriptor` `MechanicalContractValues.cs:258` | `CuriousContraptions.tests/PrismaticRetentionTests.cs:179` (a slider turns a transfer shaft at 10 rad/m); admission `CuriousContraptions.tests/RetentionBoundaryTests.cs:205` |
| Bundle admission of a prismatic-driving row with a rad/m parameter | present | `MechanicalTransmissionValidation.cs:56`–`:73` | `CuriousContraptions.tests/WoundSpringContractTests.cs:245` |
| The same row compiled from a package declaration and run with a linked consumer on the driven hinge's `Drive` port (two transmissions in series when the consumer is a Reverse gear) | present, untested | compiler path shared with the revolute-driving case (`CuriousContraptions.tests/RetentionWorkTests.cs:16` covers that direction only) | `ENGINE-MECHANICAL-LINKS` qualifies two transmissions in series, the composed ratio being the product of the two (coordinator decision 29; [engine stories index](readiness/engine-stories-index.md) §2); this slice's first test re-checks it |
| `Drive` port on a driven hinge, joined by a Mechanical link to another part's `DriveIn` | present | role rule `MechanicalTransmissionValidation.cs:29` | `CuriousContraptions.tests/MechanicalCoordinateBindingsTests.cs:499`; `CuriousContraptions.tests/ClutchTests.cs:244` |
| Load on the consumer reacts back to the source coordinate | present | `CuriousContraptions.Simulation/MechanicalTransmissionSolver.cs:77` | `CuriousContraptions.tests/MechanicalCoordinateBindingsTests.cs:931`; `CuriousContraptions.tests/ReverseTransmissionLoadTests.cs:19` |
| Mechanical connection save/load and invalid-connection refusal | present | `CuriousContraptions.Bundle/BundleValidator.cs:373` | `CuriousContraptions.tests/MechanicalCoordinateBindingsTests.cs:681`, `:456` |

Counts: 8 present, 1 present-untested, 0 narrower, 0 missing. Nothing needs element-specific engine code.

**Element dependencies.** A shaft consumer to show rotation (Conveyor or Reverse gear, delivered) and an input load (balls and Weight, delivered).

### Engine conformance

- **Declaration data only.** A slider, a hinge and one bilateral ratio row. No callback, script or engine branch on this part.
- **Generic laws.** Rotation comes from the ratio constraint with the declared `rack_pitch`; the rack is held by hinge Coulomb friction; "an unmoving input supplies no rotation energy" follows from the package having no drive or store; loading is contact.
- **Layers.** Colliders: 1 static housing box, rack box and pad box on the slider body, 1 pinion cylinder. Visuals: the art table, none used as a collider. Animation: `Visual/Rack` and `Visual/Pinion` follow body poses; `Housing` is moved by nothing; no presentation binding. Visible = solid: housing, rack, pad and pinion each have a collider; the rack's teeth are shallow relief inside its box and the index mark is printed flat.
- **One-way flow and typing.** Nothing is read back from art. Port role and domain are enums; `rack_pitch` is a typed f32 parameter.
- **Flat state.** 3 bodies, 4 colliders (3 boxes, 1 cylinder), 1 prismatic and 1 revolute joint, 1 transmission row, 0 drives, 0 sensors, 0 domain nodes, 0 stores, 2 body bindings.
- **Performance.** Four primitive colliders, none cosmetic; the housing is static. No island sleeping exists today; a resting rack costs nothing only once ENGINE-ISLANDS-SLEEP (owner decision 3a) is delivered, ahead of this part.
- **Tests are scenarios.** Rows B1 to B11 run the production simulation; none needs a path specific to this part.

## 4. Sources and legacy

- Requirements row: "An unmoving input supplies no rotation energy." No variants.
- Element map: Source D owner. No decisions.md row applies.

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | Bidirectional phase-free axial coupling; speeds and reactions set by shared inertia; no copied work allowance. | `engine/physics/PhysicsTransmissionJoint.cs@a6c914e:L7-L13` | carry forward (law) | Gives energy-honest linear-to-rotary conversion. |
| 2 | Transmission endpoints must be axial (hinge or slider) and distinct; ratio finite and nonzero. | `engine/physics/PhysicsTransmissionJoint.cs@a6c914e:L22-L32` | carry forward | Admission rules. |
| 3 | Mechanical output socket `Drive`; one Mechanical link per input. | `engine/MachineData.cs@a6c914e:L104-L108`; `engine/MechanicalNetwork.cs@a6c914e:L35-L35` | carry forward | Typed port. |
| 4 | Linear input braking dissipates exactly the removed kinetic energy and supplies no work (2 J at 2 m/s, 1 kg). | `CuriousContraptions.tests/ConstrainedPoweredImpulseTests.cs@a6c914e:L12-L34` | carry forward (as acceptance arithmetic) | Bounds energy at the end stop. |
| 5 | CPU impulse solver implementation. | `engine/physics/PhysicsTransmissionJoint.cs@a6c914e:L38-L40` | do not carry forward | CPU solver path. |

**Files harvested:** `engine/physics/PhysicsTransmissionJoint.cs`, `engine/MachineData.cs`, `engine/MechanicalNetwork.cs`, `parts/WoundSpringPart.cs`, `parts/WeightPart.cs`, `parts/SpringPart.cs`, `CuriousContraptions.tests/ConstrainedPoweredImpulseTests.cs`. No element-specific legacy (searched `parts/`, `engine/`, `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/Campaign`, `reference/` for rack, pinion, plunger, converter).

## 5. Acceptance outline

Requirement row: [element-054](../requirements.md#element-054). Preserved outcome: an unmoving input supplies no rotation energy.

- **Chrome UI recipe (pending, post-catalogue).** In free play place the converter, a Conveyor (or Reverse gear) and a Bowling ball above the pad; join converter `Drive` → consumer `DriveIn` with a Mechanical link. Verify placement and the typed connection from the committed read.

Native rows. Planned tests live in `CuriousContraptions.tests/LinearToRotaryConverterTests.cs`; all names are planned.

| Row | Given / When / Then | Planned test | Control | Boundary | Tolerance and stall guard |
| --- | --- | --- | --- | --- | --- |
| B1 | A Bowling ball dropped from 0.3 m above the pad, converter linked to a Conveyor. The rack descends more than 0.5 m and the pinion and Conveyor roller turn by `rack_pitch` × rack travel | `LinearToRotaryConverterTests.ALoadOnThePadTurnsThePinionByThePitchTimesTheStroke` | link removed: the Conveyor roller turns less than 0.01 rad | `rack_pitch` 2.5 and 10 | angle ratio within 2 % (`CuriousContraptions.tests/GuideResponseTests.cs:174`): a wrong ratio changes how far a belt carries cargo. Guard 5 s |
| B2 | No load for 2 s, upright, sideways and inverted: rack travel under 0.01 m, pinion and consumer under 0.01 rad | `LinearToRotaryConverterTests.AnUnmovingRackSuppliesNoRotation` | B1 | `rack_pitch` 2.5 (least holding margin) | 0.01 m and 0.01 rad: visible creep with no input would be free rotation. This is the requirement outcome |
| B3 | Throughout B1 the kinetic energy of pinion, roller and any carried ball never exceeds the Bowling ball's lost potential plus initial kinetic energy | `LinearToRotaryConverterTests.RotationEnergyNeverExceedsTheLoadsLostEnergy` | none | none | the allowance of `SpringTestEnergy.Allowance(scene)` (0.005 J + 0.0981 J per kg of dynamic mass, `CuriousContraptions.tests/SpringTestEnergy.cs:44`–`:47`), the bound `CuriousContraptions.tests/RetentionWorkTests.cs:16` uses; with the 4 kg ball that is about 0.44 J |
| B4 | A resting Tennis ball at pitch 5 does not move the rack; a resting Basketball does | `LinearToRotaryConverterTests.OnlyAHeavyEnoughLoadStartsTheRack` | the two balls are each other's control | pitch 10 with the Tennis ball (3.4 N against 10 N): does not start | 0.01 m held; started means more than 0.1 m within 3 s. Loads sit 15 % or more from the threshold, so friction error does not decide the row |
| B5 | At full stroke the rack stops within 0.01 m of −0.6 m and the pinion stops; the ball rests on the pad | `LinearToRotaryConverterTests.TheRackStopsAtFullStrokeAndThePinionStopsWithIt` | none | pitch 10 | 0.01 m; pinion speed under 0.05 rad/s after 1 s at the stop |
| B6 | The linked consumer's shaft is held still. A Basketball on the pad does not descend | `LinearToRotaryConverterTests.ABlockedOutputHoldsTheRack` | free consumer: the rack descends | none | 0.01 m. Until EL-055 is delivered the held shaft is a synthetic limited hinge, as in `CuriousContraptions.tests/ReverseTransmissionLoadTests.cs:19`; afterwards the engaged Brake |
| B7 | Run then Reset restores rack, pinion, ball and consumer | S4 row 3 once delivered, else `LinearToRotaryConverterTests.ResetRestoresTheAuthoredState` | n/a | Reset at full stroke | authored values, exact |
| B8 | Save and load round-trip `rack_pitch` and the `Drive` Mechanical link | S4 row 1 once delivered, else `LinearToRotaryConverterTests.ASavedConverterLoadsWithItsPitchAndLink` | n/a | both bounds | exact field values |
| B9 | `rack_pitch` below 2.5, above 10 or non-finite is refused atomically | S4 row 2 once delivered, else `LinearToRotaryConverterTests.AnOutOfRangePitchIsRefused` | bounds admitted | both bounds | typed refusal |
| B10 | A Mechanical link from another part's `Drive` into this `Drive`, and an electrical wire into `Drive`, are refused | S4 row 5 once delivered, else `LinearToRotaryConverterTests.AWrongKindConnectionIsRefused` | the link to the Conveyor's `DriveIn` is admitted | none | `WorkshopEditRejection.InvalidConnection` |
| B11 | Collider and art agree for housing, rack, pad and pinion | S4 row 4 once delivered, else `LinearToRotaryConverterArtTests.EveryVisibleSolidHasItsCollider` | n/a | n/a | sizes within 0.005 m |

Integrations: the scope index [todo-211](../requirements.md#todo-211) defines no separate integration task; shared interactions use [IX-05 shaft torque transmission](../requirements.md#interaction-05) (B1, B6) and [IX-01 contact impulse](../requirements.md#interaction-01) (load on the pad). IX-04 tension is no longer used: the rope input is removed (section 6).

## 6. Open questions

**Resolved**

1. Rack return: none; the rack stays where it stops. D19 (the spec's proposal). Decided now how it stays: the pinion's 1.2 N·m bearing friction holds it, a tested capability, instead of a zero-gravity rack, which no delivered package uses.
2. Rope `Tie` input: not built. D19: no requirement row or campaign lesson uses it, and contact is "actual translation and force".
3. Orientation: free rotation of the whole part. D19; row B2 covers three poses.
4. Ratio parameter: `rack_pitch` in rad/m, 2.5 to 10, default 5, replacing `pinion_radius` 0.025 to 0.2 m/rad. Decided by code: the validator requires rad/m for a prismatic driving coordinate. The range was narrowed (equivalent radius 0.1 to 0.4 m) so that unloaded shaft speed stays near the Motor's range and the rack's own weight is always held. D1.
5. Stroke parameter: removed; stroke is fixed at 0.6 m. D13.
6. Rack mass lowered from 0.5 kg to 0.2 kg so bearing friction holds it at every pitch. D1.
7. Package id is `linear_to_rotary_converter` (batch convention), replacing `linear_rotary_converter`. D1, D9.

**Needs the owner**

None.

## 7. Integration

- **Collaborators.** Conveyor, Reverse gear, Clutch, balls, Weight (all delivered). EL-056 Ratchet (not delivered) keeps the gained rotation when the load leaves; EL-055 Brake gives row B6 a real jam.
- **Campaign.** "Both conversion directions": first use levels 31–40, reuse 41–50, 61–90, 136–150 (`docs/planning/invest/campaign-reservations.md:98`).
- **Lesson fixtures.** A level that places this part needs S8.
- **Order.** Independent of EL-053; either may be delivered first. They share one shaft size and material so one planner can reuse the probe.

### Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of the identity's spec headed "Package source binding", read by `tools/Coverage/NamedPackageSources.cs` (on `origin/main`, `25df3cd5`; pattern: EL-239 Baseball). The section is not written here: the reader parses every EL spec that carries that heading and refuses one whose preconditions fail. **Blocked on the coverage follow-on story S7.**

| Field | Planned value |
| --- | --- |
| Package | `linear_to_rotary_converter` |
| Named identity | EL-054, `../invest/named-elements.md#element-054` |
| Acceptance | `todo-211` and `sequence-task-607` |
| Decision | S340; `scope-corrections.md` has no `s340` block |
| Mode records | `Configuration=Adjustable` (it declares a parameter; confirmed at slice planning) |

What it lacks today: an `s340` anchor block in `docs/planning/invest/scope-corrections.md`, and its todo-211 anchor is a scope index, not immediately followed by its sequence-task-607 anchor in `requirements.md`. S7 widens the reader (owner decision 15), including for companion packages and TH, RAD and GAP identities.

## 8. Delivery

- **Slice shape.** Single element, one package.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which includes ENGINE-ISLANDS-SLEEP; `ENGINE-MECHANICAL-LINKS` before its first consumer (two transmissions in series; owner decision 3a, coordinator decision 29); then S7 (the coverage follow-on, which widens the reader, owner decision 15). The present-but-untested row is this slice's first test (disqualifier 1). S3 and S4 reduce the edit list.
- **Files added.** `elements/LinearToRotaryConverter/LinearToRotaryConverter.csproj`, `LinearToRotaryConverterElementDeclaration.cs`, `linear_to_rotary_converter.tscn`, `icon.svg`; `CuriousContraptions.tests/LinearToRotaryConverterTests.cs` and `LinearToRotaryConverterArtTests.cs`.
- **Files edited.** No test-project edit (published main compiles every top-level `*.cs` by one rule). Until S3: `CuriousContraptions.slnx`, the trim list ending at `CuriousContraptions.tests/CSharpStaticGateTests.cs:703`, the switches at `CuriousContraptions.tests/PackageRuntimeParityTests.cs:483` and `:641`. At delivery: the binding section of section 7 appended to this spec. This spec and `TODO.md`.
- **Review tier.** Engine tier: one capability is present but untested, even though the expected diff is package and tests only.
- **Disqualifiers (the slice's probe re-checks).** (1) The package-compiled prismatic-driving row does not run, or does not carry load to a linked consumer: stop and return it to `ENGINE-MECHANICAL-LINKS`' qualification of two transmissions in series, not a change to this package. (2) Hinge friction on the driven coordinate does not hold the rack through the row (creep above 0.01 m in 2 s at pitch 2.5): raise the friction torque or the pitch minimum before changing anything else. (3) A fast landing (Bowling ball from 1 m) tunnels the pad or drives the rack through its lower stop by more than 0.01 m.
