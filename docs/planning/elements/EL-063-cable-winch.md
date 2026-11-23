# EL-063 · Cable winch declaration readiness spec

**Current delivery (24 October 2026):** [EL-063 delivery slice](../invest/named-elements.md#element-063). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 named-identity spec. Baseline commit `a6c914e`. Legacy citations use `path@a6c914e:Lstart-Lend` and stay retrievable from git history after the Epic 7 purge. Values marked **proposed** are design values the owner may revise. Sections 1 to 3 and 5 to 8 were rewritten on 21 November 2026 against published main `8d1d68aa`; their `path:line` citations are to that checkout.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-063 |
| Name | Cable winch |
| Type | Mechanical |
| Requirement anchor | [element-063](../requirements.md#element-063); scope index [todo-320](../requirements.md#todo-320) |
| Named entry | [named-elements.md#element-063](../invest/named-elements.md#element-063); proof owner S334 |
| CAT spec refined or extended | none. Related: [CAT-042 motor](CAT-042-motor.md) (shaft source), [CAT-058 rope anchor](CAT-058-rope_anchor.md) (rope route law), [CAT-053 pulley](CAT-053-pulley.md), [CAT-067 weight](CAT-067-weight.md) (load) |
| Related identities | [EL-067 steel cable](EL-067-steel-cable.md), [EL-065 load hook](EL-065-load-hook.md), [EL-056 ratchet](EL-056-ratchet.md), [EL-055 brake](EL-055-mechanical-brake.md), [EL-057 escapement](EL-057-escapement.md); recipe-only "winch" record in [gap-historical-followthrough](../requirements.md#gap-historical-followthrough) |
| Roadmap story | [EL-063 delivery slice](../invest/named-elements.md#element-063); canonical proof owner and prerequisite decisions are recorded there. |
| Status | Not started; blocked on the engine programme (owner decision 3a), relying most on ENGINE-SUPPLY-DEMAND (the Motor train that turns the drum) and ENGINE-ISLANDS-SLEEP (a hung load at rest); then ENGINE-TENSILE-EXTENSIONS (winding clause: a tensile span wound by a revolute coordinate) and ENGINE-CONNECTION-MATERIALS (solid strand); then S7. Everything else it needs is delivered and tested |
| Disposition | To deliver as a single element |
| Planned package | `cable_winch`, directory `elements/CableWinch/`, class `CableWinchElementDeclaration` |
| Coverage binding | Planned, not yet live (section 7); blocked on S7. No CAT number |
| delivery-batches class | [delivery-batches](../invest/delivery-batches.md) does not list EL-063 in 1a, 1b or 1c. ENGINE-TENSILE-EXTENSIONS (winding clause, owner decision 3a) owns the winding that `ENGINE-ASSEMBLY-JOINTS` once listed ("a winch that winds a tensile span", `_bmad-output/planning-artifacts/epics.md@25df3cd5:4459`). Code confirms: no span length is coupled to a coordinate today. It belongs in 1c beside the tensile extensions as its own run of one |

## 2. Declaration

Follows the delivered **Conveyor** shape for the driven shaft (`elements/Conveyor/ConveyorElementDeclaration.cs:44`–`:51`, `:91`) and the **Impact lever** shape for a Tie port on a hinged body (`elements/ImpactLever/ImpactLeverElementDeclaration.cs:41`–`:62`).

**Interfaces.** `IHingeElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:20`) and `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`), plus whatever declaration the winding capability adds (section 3). `KinematicMode.Static` root; `SupportedDomains` Mechanical and Rope.

**Bodies and colliders** (local frame: X right, Y up, Z toward the player; the drum axis is +Z, as the Motor's rotor).

| Body | Collider | Shape and size (m) | Centre (m) |
| --- | --- | --- | --- |
| 0 static frame | wall plate | box 0.70 × 0.70 × 0.16 | (0, 0, −0.12) |
| 1 drum (dynamic, hinged) | barrel | cylinder r 0.20, width 0.26, axis Z | (0, 0, 0.17) |
| 1 drum | back flange | cylinder r 0.28, width 0.04, axis Z | (0, 0, 0.02) |
| 1 drum | front flange | cylinder r 0.28, width 0.04, axis Z | (0, 0, 0.32) |

The wall plate sits where the Rope anchor's mount sits (`elements/RopeAnchor/rope_anchor.tscn`, mount at z −0.12), so the two wall-mounted rope parts line up.

**Mass and inertia.** Frame static, mass 0. Drum 0.5 kg, the mass of the Motor's rotor (`elements/Motor/MotorElementDeclaration.cs:18`), declared as constant principal moments of a solid cylinder of mean radius 0.24 m and width 0.34 m: axial ½·m·r² = 0.0144 kg·m², transverse m·(3r² + w²)/12 = 0.0120 kg·m², principal frame turned Rx(90°) as the Motor does (`MotorElementDeclaration.cs:107`–`:111`). Gravity (0, −9.81, 0).

**Material** (`MaterialProperties` order restitution, friction, rolling resistance, bounce threshold): both bodies `(0, 0.3, 0, 0.1)`, the rigid drive-part material (`MotorElementDeclaration.cs:63`–`:64`).

**Hinge.** One `RevoluteHingeDescriptor` (`CuriousContraptions.Abstractions/MechanicalContractValues.cs:149`): support body 0, base frame (0, 0, 0.17), `RevoluteTravel.Continuous`, limits ±π, stiffness 0, damping 0, friction torque 0.02 N·m (the Conveyor and Clutch bearing value), `BodiesCollide: false`, no drive.

**Parameters.** None. Resolved under D13: the requirement row names no size parameter, so drum radius (0.20 m) and cable capacity (6 m) are fixed per kind.

**Ports.**

| `PortId` | Role | Domain | Direction | Signal | Body | Position (m) | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `DriveIn` | `PortRole.DriveIn` | Mechanical | Input | none | 1, revolute coordinate 0 | (0, 0, 0.36) | `CoordinatePerRadian` 1; `MechanicalRim` centre (0, 0, 0.32), radius 0.28 m (the front flange is the sheave; Clutch rim radius) |
| `Tie` | `PortRole.Tie` | Rope | Bidirectional | none | 1 | (0, 0.20, 0.17), the top of the barrel | The winding attachment: see section 3. Until the engine story defines how it is declared, no `RopeAttachmentRole` is chosen here; the existing set is `{ None, Anchor, Load, Guide }` (`CuriousContraptions.Abstractions/PortDescriptor.cs:74`), and the topology refuses `None` (`engine/gpu/RopeTopology.cs:75`–`:84`), so the story must declare the wound end: a new attachment role or a winding descriptor naming the revolute coordinate and radius |

There is no pass-through `Drive` output (resolved, section 6).

**Stores.** None. **Supply graph.** None: the winch has no electrical port.

**Presentation.** `Bodies: [("Visual/Drum", PackageBodyIndex(1))]`, as the Motor binds its rotor (`MotorElementDeclaration.cs:28`). Pick radius 0.6 m. Footprint: 2 bodies, 1 moving, 0 prismatic, 1 revolute.

**Palette.** `ElementCategory.Motion`, palette order 137 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)), id `cable_winch`, display name "Winch", catalogue colour ochre `(0.84, 0.61, 0.28)` (`DESIGN.md:246`, the Pulley and Rope anchor colour).

### Art and solidity

| Node | Mesh and size | Class |
| --- | --- | --- |
| `Visual_Plate` | cream box 0.70 × 0.70 × 0.16 | solid, wall-plate collider |
| `Visual/Drum/Barrel` | ochre cylinder r 0.20, width 0.26 | solid, barrel collider |
| `Visual/Drum/FlangeBack`, `FlangeFront` | cream cylinders r 0.28, width 0.04 | solid, flange colliders |
| `Visual/Drum/Spokes` | four gold marks on the front flange face, flush | decorative: printed flat |

No wound-cable layer is drawn in the first delivery; the turning spokes show winding and the cable itself is drawn by the connection presentation from the `Tie` position. Icon: a flanged drum seen end-on with one strand leaving its top. At a glance it differs from the Pulley by its wall plate and the two wide flanges round a solid barrel; the Pulley is a single open wheel on a post.

**The strand.** The strand is solid (owner decision 1): its collider is a property of the declared connection material (declared diameter, segment rule), compiled from declarations. Blocked on ENGINE-CONNECTION-MATERIALS (solid strand). The delivered rope draws a thick strand with no collider today: a recorded visible-solid defect owned by that story. The winch presents its own wound span, so this part is blocked on that story too; the strand's collider is the connection's, not a collider of this package.

### Behaviour and numbers

- **Winding law.** Paid-out length L = L₀ − r·θ, with r = 0.20 m, θ the drum's committed travel (positive is the Motor's positive spin, which winds in) and L₀ the connect-time measured length (the delivered rope rule, `engine/gpu/WorkshopConnections.cs:41`–`:55`). L stays within [0.05 m, 6 m]: 0.05 m is the tensile span's lower bound (`CuriousContraptions.Abstractions/TensileSpanDeclarationValues.cs:33`) and 6 m the capacity, enough for a tall lift on the bench and a few visible drum turns. At either end the drum stops. Capacity bounds the route's total maximum length (over a Pulley, the sum of its spans); at Run start the drum holds 6 m less the connect-time length.
- **No instantaneous shortening.** L changes only through θ, so |ΔL| per tick is at most r·|ω|·Δt. A Motor at its 20 rad/s maximum (`MotorElementDeclaration.cs:41`) gives 4 m/s. No delivered drive turns the drum faster, and an 8 kg Weight back-driving it over the full 6 m reaches at most √(2·g·6) = 10.8 m/s.
- **Reaction.** Span tension T loads the drum with T·r. A default Motor (20 N·m, `MotorElementDeclaration.cs:44`) pulls 100 N, more than the 78.5 N of the heaviest Weight (8 kg, `elements/Weight/WeightElementDeclaration.cs:33`). An 8 kg Weight needs 15.7 N·m, so a Motor set to 5 N·m is overloaded and back-driven. The lightest Weight (0.25 kg) gives 0.49 N·m, far above the 0.02 N·m bearing, so an unsupplied winch always pays out under load unless a brake or ratchet holds the shaft upstream.
- **Lift speed.** Default Motor speed 6 rad/s gives 1.2 m/s.
- **Lowering under power** needs the delivered Reverse transmission between Motor and winch, because the Motor's `speed` is 0 to 20 rad/s and never negative (`MotorElementDeclaration.cs:41`).
- **Work.** The drive's work ledger is the Motor's; the winch stores nothing. The requirement's "stall and reverse load debit work" is read as: the work ledger stays honest under stall and back-drive, and no energy is created. Stall adds no work and an overloaded drive adds none while back-driven (delivered behaviour, section 3).

## 3. Engine capabilities (capability probe)

| Capability | Status | Symbol | Exercising test, or owner |
| --- | --- | --- | --- |
| Static frame with a dynamic body on a continuous hinge with bearing friction | *present* | `RevoluteHingeDescriptor` `CuriousContraptions.Abstractions/MechanicalContractValues.cs:149` | `CuriousContraptions.tests/MotorTests.cs:330` (spins down under bearing friction) |
| `DriveIn` port on a revolute coordinate, driven through a Mechanical connection | *present* | `PortRole.DriveIn` `CuriousContraptions.Abstractions/PortDescriptor.cs:34`; `ConveyorElementDeclaration.cs:44` | `CuriousContraptions.tests/ConveyorTests.cs:417` |
| A stalled drive does no work; an overloaded drive is back-driven and adds none | *present* | `RevoluteDriveDescriptor` `MechanicalContractValues.cs:138` | `CuriousContraptions.tests/RevoluteDriveTests.cs:307`, `:353`; `CuriousContraptions.tests/MotorTests.cs:675`, `:732` |
| Tie port on a hinged moving body | *present* | `ImpactLeverElementDeclaration.cs:41`–`:51` | `CuriousContraptions.tests/ImpactLeverTests.cs:414` |
| A tensile span fixed to a point on a driven rotor lifts a hung load and is charged only for the lift | *present* (engine fixture, not a port) | `TensileSpanDeclaration` `CuriousContraptions.Abstractions/TensileSpanDeclarationValues.cs:25` | `CuriousContraptions.tests/MotorTests.cs:619` |
| Tension-only span, slack never pushes | *present* | same | `CuriousContraptions.tests/RopeTests.cs:21`, `:34` |
| Moving-body art follows the body pose | *present* | `PresentationBodyBinding` `CuriousContraptions.Abstractions/ElementPresentation.cs:114` | `CuriousContraptions.tests/CompoundMassAndRollingTests.cs:1040` |
| **A span end wound on a revolute coordinate**: paid-out length follows the coordinate at a declared radius, between a declared minimum and capacity, with the tension reaction on the coordinate, a refusal of a connection longer than the capacity, and the wound length in the presentation read | *missing* | none: a span has one fixed `MaximumLength` (`TensileSpanDeclarationValues.cs:25`–`:33`) | ENGINE-TENSILE-EXTENSIONS (winding clause; the wording is Batch I's gap G3, and Batch L adds "a winding may sit on an undriven hinge of any package") |
| A solid strand: a collider for the span from its declared connection material | *missing* | the delivered rope has no collider | ENGINE-CONNECTION-MATERIALS (solid strand, owner decision 1) |

The span tied to a rotor point (`MotorTests.cs:619`) is a crank, not a winch: its length is constant and the load rises by r·sin θ. It is cited as evidence for the reaction and work ledger only.

**Element dependencies.** Delivered: Motor, Battery, Reverse transmission, Clutch, Weight, Pulley, Rope anchor. Not delivered: EL-055 brake, EL-056 ratchet, EL-065 hook, EL-067 steel cable, EL-069 bucket, EL-070 cage. None of the undelivered parts blocks this slice.

### Engine conformance

- **Declaration data only.** One hinge, one mechanical port, one rope port and one declared winding of a tensile span on that hinge. The winding is a general engine capability stated without naming this part. No callback or engine branch.
- **Generic laws.** Lifting is the winding relation L = L₀ − r·θ with the span's tension reacting on the coordinate; stall and back-drive are the upstream drive's torque bound; the span itself is the delivered tension-only law.
- **Layers.** Colliders: 1 static wall-plate box and 3 cylinders on the drum (barrel, 2 flanges); the strand's collider belongs to its connection material (ENGINE-CONNECTION-MATERIALS). Visuals: the art table, none used as a collider; the strand is drawn by the connection presentation. Animation: `Visual/Drum` follows a body pose; the printed spokes turn with it; `Visual_Plate` is moved by nothing; no presentation binding. Visible = solid: plate, barrel and flanges each have their collider; the spokes are printed flat; the strand is solid once ENGINE-CONNECTION-MATERIALS (solid strand) is delivered.
- **One-way flow and typing.** Nothing is read back from art. Port roles, domains and the attachment role are enums; values are f32.
- **Flat state.** 2 bodies, 4 colliders (1 box, 3 cylinders), 1 revolute joint, 1 winding row, 0 drives, 0 sensors, 0 domain nodes, 0 stores, 1 body binding.
- **Performance.** Four primitives; the flanges are real solids that retain the strand and block cargo. The frame is static. No island sleeping exists today; a hung load at rest costs nothing only once ENGINE-ISLANDS-SLEEP (owner decision 3a) is delivered, ahead of this part.
- **Tests are scenarios.** Rows W1 to W11 run the production simulation through placed parts, Mechanical links (the delivered rigid connection; coordinator decision 36) and ropes.

## 4. Sources and legacy

- Requirements row: "Stall and reverse load debit work; no instantaneous rope shortening." No variants.
- GAP follow-through: recipe-only winch references become constituent or dedicated-piece specifications ([requirements.md#gap-historical-followthrough](../requirements.md#gap-historical-followthrough)).

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | A shaft drives a translating coordinate through a declared lead (m/rad) as a bidirectional coupling. | `parts/WoundSpringPart.cs@a6c914e:L89-L98`; `engine/physics/PhysicsTransmissionJoint.cs@a6c914e:L7-L13` | carry forward (law; the drum radius is the lead) | Gives finite-work winding with reaction torque. |
| 2 | With a supplied motor the plunger winds and stored energy ≤ 1.02 × accepted work + 0.01 J; without supply nothing moves and accepted work is 0. | `CuriousContraptions.tests/WoundSpringRuntimeTests.cs@a6c914e:L102-L141` | carry forward (acceptance pattern) | "Stall debits work"; energy bound. |
| 3 | Rope is tension-only, inactive while slack; branching rejected. | `engine/physics/PhysicsRopeJoint.cs@a6c914e:L114-L122`; `engine/RopeNetwork.cs@a6c914e:L69-L78` | carry forward | Cable route law. |
| 4 | Rope length bounds 0.05–200 m; connect-time length. | via [CAT-058](CAT-058-rope_anchor.md) R5–R6 | carry forward | Admission range of the free length. |
| 5 | Weight 0.25–8 kg, radius 0.32·∛m, tie at r + 0.08. | `parts/WeightPart.cs@a6c914e:L25-L26`; `engine/MachineData.cs@a6c914e:L99-L100` | carry forward via CAT-067 | Load scale used for drum sizing. |
| 6 | Rope cutting and moving-block ratios were future work. | `reference/P0-022-before/source-README.md@a6c914e:L99-L99` | carry forward as context | Winch winding was never implemented. |

**Files harvested:** `parts/WoundSpringPart.cs`, `parts/WeightPart.cs`, `engine/physics/PhysicsTransmissionJoint.cs`, `engine/physics/PhysicsRopeJoint.cs`, `engine/RopeNetwork.cs`, `engine/ConnectionPort.cs`, `engine/MachineData.cs`, `CuriousContraptions.tests/WoundSpringRuntimeTests.cs`, `reference/P0-022-before/source-README.md`. Searched with no hit for winch, spool, drum.

## 5. Acceptance outline

Requirement row: [element-063](../requirements.md#element-063).

- **Chrome UI recipe (pending, post-catalogue).** Place Battery, Motor and Winch on a high Wall ledge, a Weight on the bench below; wire Battery → Motor; link Motor `Drive` → Winch `DriveIn`; connect Winch `Tie` → Weight `Tie`. Verify placement and links, Run, Reset.

All tests are planned, in `CuriousContraptions.tests/CableWinchTests.cs`, on the real simulation. Stall guard for every run: 10 s of game time.

| Row | Given / when / then | Planned test | Control | Boundary | Tolerance and its gameplay reason |
| --- | --- | --- | --- | --- | --- |
| W1 | Motor 20 N·m at 2 rad/s, 4 kg Weight hung 3 m below. Run 3 s: the Weight rises by r·Δθ | `CableWinchTests.ASuppliedWinchLiftsItsLoadByTheDrumRadiusTimesItsTurn` | Battery disabled, Weight resting on the bench: it does not rise | Weights of 0.25 kg and 8 kg | Rise within 0.02 m of r·Δθ after 0.2 s: a lift must clear a ledge edge by more than that |
| W2 | Same lift: the drive's work is at least the Weight's potential-energy gain and at most that gain plus kinetic energy, bearing loss and 5 % | `CableWinchTests.TheDriveIsChargedForTheLiftAndNoMore` | The `MotorTests.cs:619` allowance pattern | none | 5 %: a free lift or a double charge would be 100 % out |
| W3 | Motor 5 N·m, 8 kg Weight hung: the drum is back-driven, the Weight descends, the drive's work does not grow | `CableWinchTests.AnOverloadedWinchIsBackDrivenAndAddsNoWork` | Motor 20 N·m lifts the same Weight | Motor 15 N·m (below 15.7 N·m) and 17 N·m (above) | Work growth under 0.1 J over 2 s; kinetic energy never exceeds the Weight's lost potential energy plus 5 % |
| W4 | Lift until the cable is fully wound: the drum stops, L stays at 0.05 m, the drive's work stops growing | `CableWinchTests.AFullyWoundCableStallsTheDriveWithoutWork` | Mid-travel the same drive keeps doing work | Unsupplied, hung 4 kg Weight pays out to exactly the 6 m capacity and hangs there | Length within 0.02 m of each end; stalled work growth under 1 % of the free-running work, as `RevoluteDriveTests.cs:307` |
| W5 | Every tick of W1, W3 and W4, including the first tick of Run: the span's maximum length changes by at most r·\|ω\|·Δt + 1 mm | `CableWinchTests.TheCableNeverShortensFasterThanTheDrumTurns` | none | Motor at 20 rad/s (its maximum), and an unsupplied drum back-driven by an 8 kg Weight over the full 6 m | 1 mm: a jump a player could see is centimetres |
| W6 | Supply cut mid-lift, no brake: the Weight descends and the drum turns the other way by descent / r | `CableWinchTests.AnUnsuppliedWinchPaysOutUnderItsLoad` | The Weight resting on the bench leaves the drum at rest | 0.25 kg Weight still back-drives the 0.02 N·m bearing | Travel × r within 0.02 m of the descent; kinetic energy never exceeds the Weight's lost potential energy plus 5 % |
| W7 | A route from `Tie` whose total measured length exceeds 6 m (over a Pulley, the sum of its spans) is refused with `WorkshopEditRejection.InvalidConnection` and changes nothing | `CableWinchTests.ACableLongerThanTheCapacityIsRefused` | 5 m is admitted | Exactly 6 m admitted | Declared values; exact |
| W8 | Reset after W1: drum angle, cable length and Weight pose are the built values | `CableWinchTests.ResetRestoresTheDrumTheCableAndTheLoad`; S4 row R3 once delivered | none | Reset at the fully wound stop | Authored values exactly |
| W9 | Save and load of the built winch with both links returns the same construction | S4 row R1b once delivered, else `CableWinchTests.ASavedWinchLoadsWithItsLinks` | none | none | Field equality |
| W10 | An electrical or activation link to `Tie` or `DriveIn`, and a rope link to `DriveIn`, are refused | S4 row R5 once delivered, else `CableWinchTests.AWrongKindLinkIsRefused` | The W1 links are admitted | A second span on `Tie` is refused (`engine/gpu/RopeTopology.cs:75`–`:84`) | Typed refusal |
| W11 | Route Winch → Pulley → Weight lifts as W1 | `CableWinchTests.TheWinchLiftsOverAPulley` | The route left open at the Pulley lifts nothing (`CuriousContraptions.tests/WeightTests.cs:1232`) | none | As W1 |

No parameter refusal row: the package declares no parameter. Collaborators not yet delivered (hook, brake, ratchet, cable) add their own rows in their slices.

## 6. Open questions

**Resolved**

1. The effective drum radius does not grow as layers build up. D19 (the spec's proposal): constant radius keeps lift = r·Δθ readable.
2. No integral ratchet. D19: the brake (EL-055) and ratchet (EL-056) are shaft parts placed between Motor and winch.
3. The capability map row omits ShaftTorque. D14: the map is navigation; the probe in section 3 is the authority.
4. Drum radius and capacity are fixed, not parameters. D13.
5. No pass-through `Drive` output. D19: no requirement row or campaign lesson uses one, and the shaft parts that hold a winch sit upstream.
6. Decided now: `Tie` carries rope or steel cable alike, because the material belongs to the connection ([EL-067](EL-067-steel-cable.md)), not to the port.
7. Decided now: the cable end cannot be detached at full pay-out; the span stays fixed to the drum. Reason: the row asks for a finite cable path, not a lost cable.

**Needs the owner**

None.

## 7. Integration

- **Collaborators.** Motor, Battery, Reverse transmission, Clutch, Weight, Pulley (all delivered). EL-065 hook hangs from `Tie` as a `Load`; EL-069 bucket and EL-070 cage likewise; EL-055 and EL-056 hold the shaft; EL-067 changes only the connection's material.
- **Campaign.** Rope and cable parts are first taught at plan levels 12b and 12c (the requirements' reservation is 11–20), load and moving variants in 41–50 (`docs/planning/invest/campaign-reservations.md:44`). Owner decision 24c places the winch in *Catch, Then Lift* at **47** (chapter 5): introduction 47, practice 48, reuse 65 and 138 ([campaign coverage additions](../requirements.md#campaign-coverage-additions); brief in [campaign coverage](readiness/campaign-coverage.md#4-lesson-briefs)). Cross-element task [sequence-task-371](../requirements.md#sequence-task-371) under [todo-320](../requirements.md#todo-320); shared processes [IX-04 tension transmission](../requirements.md#interaction-04) and [IX-05 shaft torque transmission](../requirements.md#interaction-05).
- **Lesson fixture (S8).** A level that places the winch before this slice needs S8.

### Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of the identity's spec headed "Package source binding", read by `tools/Coverage/NamedPackageSources.cs` (on `origin/main`, `25df3cd5`; pattern: EL-239 Baseball). The section is not written here: the reader parses every EL spec that carries that heading and refuses one whose preconditions fail. **Blocked on the coverage follow-on story S7.**

| Field | Planned value |
| --- | --- |
| Package | `cable_winch` |
| Named identity | EL-063, `../invest/named-elements.md#element-063` |
| Acceptance | `todo-320` and `sequence-task-616` |
| Decision | S334; `scope-corrections.md` has no `s334` block |
| Mode records | `Configuration=Fixed` |

What it lacks today: an `s334` anchor block in `docs/planning/invest/scope-corrections.md`, and its todo-320 anchor is a scope index, not immediately followed by its sequence-task-616 anchor in `requirements.md`. S7 widens the reader (owner decision 15), including for companion packages and TH, RAD and GAP identities.

## 8. Delivery

- **Slice shape.** One element slice, after the engine stories.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which includes ENGINE-SUPPLY-DEMAND and ENGINE-ISLANDS-SLEEP; then ENGINE-TENSILE-EXTENSIONS (winding clause, with the wire, save and presentation read it needs) and ENGINE-CONNECTION-MATERIALS (solid strand, without which the winch's own wound span does not conform); then S7 (the coverage follow-on, which widens the reader, owner decision 15). S3 and S4 only shorten the file list.
- **Files.** Adds `elements/CableWinch/CableWinch.csproj`, `CableWinchElementDeclaration.cs`, `cable_winch.tscn`, `icon.svg`, `CuriousContraptions.tests/CableWinchTests.cs`. No test-project edit (published main compiles every top-level `*.cs` by one rule). Edits, until S3 lands: `CuriousContraptions.slnx`, the trim list in `CuriousContraptions.tests/CSharpStaticGateTests.cs` (ends at `:703`) and the placement switches in `CuriousContraptions.tests/PackageRuntimeParityTests.cs` (`:641`; generic list `:653`). At delivery: the binding section of section 7. This spec and TODO.
- **Review tier.** Data-only if the engine story delivers the winding span with tests on a declared fixture; otherwise engine.
- **Disqualifiers the probe must re-check.** The engine story delivers winding without capacity limits or without the over-capacity refusal; a Tie port on a *continuous* hinge body is refused (only a limited hinge is exercised today, `ImpactLeverTests.cs:414`); the winding declaration needs a parameter the package does not have; the solid strand's collider snags the barrel or flanges where the span leaves the drum top (it must leave the drum without catching).
