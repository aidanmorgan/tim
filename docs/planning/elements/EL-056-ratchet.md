# EL-056 · Ratchet declaration readiness spec

**Current delivery (24 October 2026):** [EL-056 delivery slice](../invest/named-elements.md#element-056). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 named-identity spec. Baseline commit `a6c914e`. Legacy citations use `path@a6c914e:Lstart-Lend` and stay retrievable from git history after the Epic 7 purge. Values marked **proposed** are design values the owner may revise. Sections 1 to 3 and 5 to 8 were rewritten on 21 November 2026 against checkout `8d1d68aa`; their `path:line` citations are current code, read that day.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-056 |
| Name | Ratchet |
| Type | Mechanical |
| Requirement anchor | [element-056](../requirements.md#element-056); scope index [todo-375](../requirements.md#todo-375) |
| Named entry | [named-elements.md#element-056](../invest/named-elements.md#element-056); slice `named-el-056`, owner S342 |
| CAT spec refined or extended | none as a part. The one-way row is shared with [CAT-071 wound spring](CAT-071-wound_spring.md) (its winding relation). Related: [CAT-042 motor](CAT-042-motor.md), [CAT-019 conveyor](CAT-019-conveyor.md) |
| Related identities | [EL-057 escapement](EL-057-escapement.md), [EL-055 brake](EL-055-mechanical-brake.md) (same shaft, two-way and powered), [EL-054](EL-054-linear-to-rotary-converter.md) (its gained rotation is kept by a ratchet), [EL-063 cable winch](EL-063-cable-winch.md) (hold a lifted load) |
| Roadmap story | [EL-056 delivery slice](../invest/named-elements.md#element-056) |
| Status | Not started; blocked on the engine programme (owner decision 3a), relying most on ENGINE-SUPPLY-DEMAND (the Motor rows) and ENGINE-ISLANDS-SLEEP (a held load at rest); then ENGINE-JOINT-KINDS (one-way hold, which exists today only in a narrower form, section 3); then S7. No package exists |
| Disposition | To deliver as a single element, after ENGINE-JOINT-KINDS |
| Planned package | id `ratchet`, directory `elements/Ratchet/`, class `RatchetElementDeclaration` |
| Coverage binding | Planned, not yet live (section 7); blocked on S7. No CAT number |
| delivery-batches class | Not classified in [delivery-batches](../invest/delivery-batches.md). Code places it in 1c: open after the Joints engine slice. Proposed row: Joints, run "One-way and stepped shafts", EL-056 and EL-057 |

## 2. Declaration

The body, hinge and port part follows the Conveyor's pass-through roller (`elements/Conveyor/ConveyorElementDeclaration.cs:42`–`:52`, `:80`–`:107`) and is writable today. The one-way holding row is not declarable today; its descriptor is defined by ENGINE-JOINT-KINDS, and this section states what the package will ask of it. Interfaces: `IHingeElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:20`), `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`), plus whichever abstraction interface the engine story adds for the row. `KinematicMode.Static`; domains `Mechanical`; footprint 2 bodies, 1 moving, 0 prismatic, 1 revolute. No executable logic.

**Coordinator guidance confirmed, with detail.** The Ratchet needs a unilateral row on one coordinate against its support, with a holding capacity. Today's unilateral row is narrower in four ways (section 3), so the classification is *narrower*, owner ENGINE-JOINT-KINDS (one-way hold, owner decision 3a).

**Bodies.**

| Body | Shape (m) | Mass and inertia | Joint |
| --- | --- | --- | --- |
| 0 housing (static) | box 1.1 × 0.7 × 0.4 centred at the origin; navy foot box 1.3 × 0.1 × 0.5 centred (0, −0.4, 0) | static | support |
| 1 ratchet wheel and shaft | cylinder r 0.28, width 0.13, axis local Z | 0.25 kg; axial 0.0098, transverse 0.00525 kg·m² (the Clutch shaft, `elements/Clutch/ClutchElementDeclaration.cs:17`–`:19`, `:120`–`:121`) | continuous revolute hinge 0 on body 0 |

- **Hinge 0** (`RevoluteHingeDescriptor`, `CuriousContraptions.Abstractions/MechanicalContractValues.cs:149`): base frame (0, 0, 0.27); `RevoluteTravel.Continuous`; friction torque 0.02 N·m; no drive.
- **One-way hold on hinge 0** (engine story): free in the direction `free_direction` selects; in the other direction a velocity-level row resists with |torque| ≤ `holding_capacity`, pushing against the support body. It is an impulse bound, not a clamped commanded velocity, as the element-map note requires.
- **Material** for both bodies (restitution, friction, rolling resistance, bounce threshold): (0, 0.3, 0, 0.1) (`ClutchElementDeclaration.cs:89`–`:91`).

**Parameters.**

| Key | Kind | Unit | Default | Min | Max | Editor policy | Run edit |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `free_direction` | Choice: "Counter-clockwise", "Clockwise" | `None` | 0 (Counter-clockwise) | 0 | 1 | Reject | BuildOnly |
| `holding_capacity` | Real | `NewtonMetre` | 40 | 1 | 200 | Reject | BuildOnly |

Counter-clockwise about local +Z is the Motor's positive direction (`elements/Motor/MotorElementDeclaration.cs:11`), so a default Ratchet lets a default Motor drive through it. The earlier draft said clockwise was positive; the delivered Motor corrects that. No stores.

**Ports.**

| `PortId` | Role | Domain | Direction | Signal kind | Coordinate | Position (m) |
| --- | --- | --- | --- | --- | --- | --- |
| `DriveIn` | `DriveIn` | Mechanical | Input | none | `Revolute 0`, `CoordinatePerRadian` +1 | (−0.42, −0.2, 0.3), normal +Z; `MechanicalRim` radius 0.12 on body 0 (the drawn pulley) |
| `Drive` | `Drive` | Mechanical | Output | none | `Revolute 0`, `CoordinatePerRadian` +1 | (0.42, −0.2, 0.3), normal +Z; `MechanicalRim` radius 0.12 on body 0 (the drawn pulley) |

Port positions, rim radius and shaft equal the Brake's (EL-055), so the two parts are interchangeable in a chain of Mechanical links (the delivered rigid connection; coordinator decision 36).

**Presentation.** Body binding `Visual/Wheel` → body 1. `Visual/PulleyIn` and `Visual/PulleyOut` `LocalRotationZ` follow `PresentationDriver.CommittedRevoluteAngle` (`CuriousContraptions.Abstractions/ElementPresentation.cs:30`). The pawl and the direction arrow each exist as two mirrored nodes, one shown per `free_direction` option through a binding `Selection` (`ChoicePredicate`, `ElementPresentation.cs:112`). The pawl is static; a click animation is not specified.

**Palette.** Category `ElementCategory.Motion`, palette order 160 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)), colour ochre (0.84, 0.61, 0.28), title "Ratchet", icon `res://elements/Ratchet/icon.svg`, scene `ratchet.tscn`.

### Art and solidity

| Node | Mesh and dimensions (m) | Class |
| --- | --- | --- |
| `Housing` | cream box 1.1 × 0.7 × 0.4 with a round window on the front face | solid, box collider on body 0 |
| `Foot` | navy box 1.3 × 0.1 × 0.5 | solid, box collider on body 0 |
| `Visual/Wheel` | pale grey saw-toothed wheel, 12 teeth, outer r 0.28, width 0.13, proud of the front face | solid, cylinder collider on body 1 (teeth are relief inside the cylinder) |
| `Visual/PawlCcw`, `Visual/PawlCw` | gold pawl 0.2 long, drawn inside the housing window, flush with the face | decorative: flush; shown by a presentation binding |
| `Visual/ArrowCcw`, `Visual/ArrowCw` | navy curved arrow printed on the housing face | decorative: printed flat |
| `Visual/PulleyIn`, `Visual/PulleyOut` | ochre link pulleys r 0.12, 0.06 wide | decorative: turned by a presentation binding |

Colours: Cream `#fff8e9` (part-body cream, owner decision 4), Navy `#1c2836`, Gold `#f7cb52` (`DESIGN.md:80`); ochre `#d69c47` (`DESIGN.md:235`); pale grey metal (`DESIGN.md:248`). Icon: a saw-toothed wheel with a pawl and a one-way curved arrow. At a glance it differs from the Brake (smooth drum between two shoes, a lamp) by its saw teeth, single pawl and arrow, and from the Escapement by having no rocking two-armed pallet and no signal socket.

### Behaviour and numbers

- **Law.** In the free direction the shaft turns with only its 0.02 N·m bearing friction. In the other direction the row applies whatever torque stops the shaft, up to `holding_capacity`; above it the shaft slips backward against a constant torque equal to the capacity. The row can only oppose reverse motion, so it never moves the shaft forward ("without inventing forward movement") and never adds work.
- **No tooth backlash.** The catch is continuous: a held shaft stays within 0.01 rad. The 12 teeth are art. The earlier draft's "up to one tooth of back-travel, slip by whole teeth" needs an angular index that no requirement row asks for (see section 6).
- **Capacity 40 N·m default** is twice the default Motor torque (20 N·m, `MotorElementDeclaration.cs:44`), so a load a default Motor can lift is always held. **Range 1 to 200 N·m** matches the Brake so the two compare directly; the ceiling is twice the strongest Motor.
- **Typical load.** An 8 kg Weight (the Weight's maximum, `elements/Weight/WeightElementDeclaration.cs:33`) on a 0.28 m rim gives 22 N·m, held at the default; at capacity 10 N·m it slips.
- **Consistency.** With EL-054 upstream, a descending rack turns the shaft in the Motor-positive direction (EL-054's port sign), which is this part's default free direction.
- No signal, optical or acoustic read; no tick-order question.

## 3. Engine capabilities (capability probe)

| Capability | Status | Symbol | Exercising test, or owner |
| --- | --- | --- | --- |
| Static housing and moving wheel with colliders from scene geometry | present | `ISceneGeometryElement` `CuriousContraptions.Abstractions/SceneGeometry.cs:10` | `CuriousContraptions.tests/MotorTests.cs:1075` |
| Continuous hinge with bearing friction | present | `RevoluteHingeDescriptor` `CuriousContraptions.Abstractions/MechanicalContractValues.cs:149` | `CuriousContraptions.tests/RevoluteHingeFrictionTests.cs:59`; `CuriousContraptions.tests/ClutchTests.cs:567` |
| `DriveIn` and `Drive` on one revolute coordinate relaying rotation | present | `PortDescriptor.Coordinate` `CuriousContraptions.Abstractions/PortDescriptor.cs:67` | `CuriousContraptions.tests/ConveyorTests.cs:417` |
| One-way row on one revolute coordinate against its support | narrower | `TransmissionConstraintKind.NonPositiveVelocity` `MechanicalContractValues.cs:255`; clamp `CuriousContraptions.Simulation/MechanicalTransmissionSolver.cs:85` | Exists only as a relation between two coordinates of one package, exercised only as a retention's winding relation (`CuriousContraptions.tests/PrismaticRetentionTests.cs:16`, `CuriousContraptions.tests/WoundSpringContractTests.cs:15`). It cannot sit on a pass-through shaft: a coordinate with a `Drive` port may not be driving and one with a `DriveIn` port may not be driven (`CuriousContraptions.Bundle/MechanicalTransmissionValidation.cs:29`). Owner: ENGINE-JOINT-KINDS (one-way hold) |
| Torque capacity on that hold, with slip above it | missing | none. Hinge Coulomb friction (`MechanicalContractValues.cs:158`) is the same in both directions; the supplied drive's torque bound (`:138`) is two-sided and needs supply | ENGINE-JOINT-KINDS |
| Free direction chosen by a Choice parameter | missing | none. A relation's sign comes from its ratio, and a ratio parameter must not span zero (`MechanicalTransmissionValidation.cs:72`) | ENGINE-JOINT-KINDS |
| Art bound to hinge angle | present | `CommittedRevoluteAngle` `CuriousContraptions.Abstractions/ElementPresentation.cs:30` | `CuriousContraptions.tests/ConveyorTests.cs:630` |
| Art node shown or hidden per Choice option | present, untested | binding `Selection` (`ChoicePredicate`, `ElementPresentation.cs:112`) with `PresentationProperty.Visible` (`:15`) | none found: no test shows a node shown or hidden per option. Row D10 is its first test |

Counts: 4 present, 1 present-untested, 1 narrower, 2 missing; one owning story, ENGINE-JOINT-KINDS, for the three engine gaps. Its text must state the capacity and the Choice-bound direction (see section 8). Nothing may be solved with ratchet-specific engine code: the row is a general "one-way hold with capacity on a revolute coordinate" that the Escapement's stop and a winch's pawl also use.

**Element dependencies.** A drive (Motor, delivered) and a reverse load. No delivered part is a tested reverse shaft load: a ball on a held inclined Conveyor belt rolls back instead of loading the roller, and no Conveyor test reacts cargo weight into the roller on an incline. Until EL-063 exists the rows use D2's synthetic reverse-torque shaft; EL-054's rack is a reverse load once delivered. EL-063 Winch with a Weight is the natural load but is not delivered.

### Engine conformance

- **Declaration data only.** One hinge and one declared one-way hold with a capacity and a Choice-bound direction. The hold is a general revolute capability stated without naming this part; the drawn pawl and teeth do no physics. No callback, script or engine branch.
- **Generic laws.** Free rotation is the hinge with bearing friction; holding and slip are the one-way hold's unilateral row with its torque bound.
- **Layers.** Colliders: 2 static boxes (housing, foot) and 1 wheel cylinder. Visuals: the art table, none used as a collider. Animation: `Visual/Wheel` follows a body pose; the link pulleys turn by a presentation binding on the committed hinge angle; one pawl and one arrow are shown per `free_direction` option by a parameter binding; all four are decorative; `Housing` and `Foot` are moved by nothing. Visible = solid: housing, foot and wheel each have a collider (the teeth are relief inside the wheel cylinder); the pawls are flush, the arrows printed flat, and the pulleys are binding-moved decorative art.
- **One-way flow and typing.** Bindings write nothing back. `free_direction` is a closed Choice, never a string in logic; `holding_capacity` is a typed f32 parameter.
- **Flat state.** 2 bodies, 3 colliders (2 boxes, 1 cylinder), 1 revolute joint, 1 one-way hold row, 0 drives, 0 sensors, 0 domain nodes, 0 stores, 1 body binding, 2 rotation bindings and 4 visibility bindings.
- **Performance.** Three primitive colliders, as the Motor; the housing is static. No island sleeping exists today; a held wheel at rest costs nothing only once ENGINE-ISLANDS-SLEEP (owner decision 3a) is delivered, ahead of this part.
- **Tests are scenarios.** Rows D1 to D10 run the production simulation; none needs a ratchet-specific path.

## 4. Sources and legacy

- Requirements row: "Reverse load holds within capacity without inventing forward movement." No variants.
- Research row: "Ratchet / Escapement — one-way hinge limit; tooth count; holds gained motion" (`docs/component-research.md@a6c914e:L78-L78`).

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | Direction policy is an enum {Both, Positive, Negative} allowed only on axial (hinge or slider) joints. | `engine/physics/PhysicsJoint.cs@a6c914e:L117-L117`, `engine/physics/PhysicsJoint.cs@a6c914e:L131-L134` | carry forward | Typed direction; ratchet needs a hinge. |
| 2 | A one-way joint adds a unilateral row on the travel axis whose impulse is bounded to [0, ∞) or (−∞, 0]. | `engine/physics/PhysicsJoint.cs@a6c914e:L170-L178` | carry forward (law) | Impulse bound, not a clamped velocity, satisfies the map note. |
| 3 | `Both` is the negative control: travel bounds alone do not make a ratchet; a one-way joint hit in reverse stops dead (zero reverse velocity) and replays identically after restore. | `CuriousContraptions.tests/PhysicsJointTests.cs@a6c914e:L106-L145` | carry forward (acceptance) | Positive/control pair. |
| 4 | A ratchet stops a reverse impulse without advancing its lower bound: position retained within 1e-7 m and speed 0 after a −2 N·s impulse. | `CuriousContraptions.tests/ScenePlungerBindingTests.cs@a6c914e:L119-L142` | carry forward (re-freeze tolerance under f32) | "Holds without inventing forward movement". |
| 5 | Ratchet and drive losses may dissipate energy, never create it. | `CuriousContraptions.tests/WoundSpringRuntimeTests.cs@a6c914e:L280-L280` | carry forward | Energy bound. |
| 6 | Event-chatter suppression and CPU acceleration rows for active ratchets. | `CuriousContraptions.tests/JointBoundaryTests.cs@a6c914e:L112-L112`; `engine/physics/PhysicsJoint.cs@a6c914e:L192-L198` | do not carry forward | CPU solver event machinery. |

**Files harvested:** `engine/physics/PhysicsJoint.cs`, `CuriousContraptions.tests/PhysicsJointTests.cs`, `CuriousContraptions.tests/ScenePlungerBindingTests.cs`, `CuriousContraptions.tests/JointBoundaryTests.cs`, `CuriousContraptions.tests/SharedContactMigrationTests.cs` (checked; plunger-contact ratchet, same law), `CuriousContraptions.tests/WoundSpringRuntimeTests.cs`, `parts/WoundSpringPart.cs`.

## 5. Acceptance outline

Requirement row: [element-056](../requirements.md#element-056). Preserved outcome: a reverse load holds within capacity without inventing forward movement.

- **Chrome UI recipe (pending, post-catalogue).** Place Battery, Motor, Ratchet and Cable winch with a Weight on its cable; wire Battery → Motor; join Motor `Drive` → Ratchet `DriveIn` and Ratchet `Drive` → Winch `DriveIn` with Mechanical links; a Switch cuts motor supply mid-lift (or the battery is omitted in the control). Verify placement, direction and links. Until the Winch is delivered there is no playable reverse load, so the recipe waits for EL-063 or EL-054.

Native rows. Planned tests live in `CuriousContraptions.tests/RatchetTests.cs`; all names are planned. "Held" means shaft travel within 0.01 rad over the observation (the bound of `CuriousContraptions.tests/RevoluteDriveTests.cs:371`). Legacy fact 4's 1e-7 m tolerance is not reused: it has no gameplay meaning under f32.

| Row | Given / When / Then | Planned test | Control | Boundary | Tolerance and stall guard |
| --- | --- | --- | --- | --- | --- |
| D1 | Default Motor linked through a default Ratchet to a Conveyor. The shaft reaches the Motor's speed | `RatchetTests.TheFreeDirectionTurnsAsIfTheRatchetWereAbsent` | same chain with `free_direction` Clockwise: held, and the Motor stalls with work under 1 % of torque × speed × time | none | speed within 2 % of the chain without a Ratchet (`CuriousContraptions.tests/GuideResponseTests.cs:174`): drag in the free direction would slow every machine. Guard 4 s |
| D2 | A reverse torque of 22 N·m (below capacity 40) on the output with no Motor supply, for 3 s: the shaft is held | `RatchetTests.AReverseLoadWithinCapacityIsHeld` | `free_direction` flipped so the same torque is in the free direction: the shaft turns more than 1 rad | none | held. A creeping hold would lower a lifted load during a puzzle. The load is a synthetic supplied shaft, as `CuriousContraptions.tests/ReverseTransmissionLoadTests.cs:114`, until the Winch exists |
| D3 | During D2 the shaft never moves in the free direction and total kinetic energy never rises above its starting value | `RatchetTests.HoldingInventsNoForwardMovementAndNoEnergy` | none | none | forward travel under 0.01 rad; 0.005 J allowance (`CuriousContraptions.tests/RetentionWorkTests.cs:16`). This is the requirement outcome |
| D4 | Capacity 40 N·m against reverse torque 36 N·m: held. Against 44 N·m: the shaft turns backward more than 1 rad in 2 s | `RatchetTests.TheRatchetHoldsBelowItsCapacityAndSlipsAboveIt` | each case is the other's control | capacity 1 and 200 N·m | ±10 % around the threshold so solver softness does not decide the row |
| D5 | A Motor turns the shaft through the Ratchet against D2's synthetic reverse torque of 22 N·m; supply is cut. The shaft stays within 0.01 rad of its angle at the cut for 3 s. Once EL-063 exists the same row also runs with a Weight on the winch, and the load stays within 0.02 m of its height | `RatchetTests.AGainedLiftIsKeptWhenTheDriveStops` | Ratchet removed: the shaft turns back more than 1 rad (with the winch: the load descends more than 0.2 m) | none | 0.01 rad of shaft is under 3 mm at the rim; 0.02 m allows cargo settling. Guard 6 s |
| D6 | Run then Reset restores wheel pose and zero hold state | S4 row 3 once delivered, else `RatchetTests.ResetRestoresTheAuthoredState` | n/a | Reset while holding a load | authored values, exact |
| D7 | Save and load round-trip `free_direction`, `holding_capacity` and both Mechanical links | S4 row 1 once delivered, else `RatchetTests.ASavedRatchetLoadsWithItsDirectionCapacityAndLinks` | n/a | both options, both bounds | exact field values |
| D8 | `holding_capacity` out of range or non-finite, and an undefined `free_direction` option, are refused atomically | S4 row 2 once delivered, else `RatchetTests.AnInvalidParameterIsRefused` | bounds admitted | both bounds | typed refusal |
| D9 | An electrical wire into `DriveIn` and a second Mechanical link into `DriveIn` are refused | S4 row 5 once delivered, else `RatchetTests.AWrongKindConnectionIsRefused` | valid links admitted | none | `WorkshopEditRejection.InvalidConnection` |
| D10 | Collider and art agree for housing, foot and wheel; exactly one pawl and one arrow are shown, matching `free_direction` | S4 row 4 once delivered, else `RatchetArtTests.TheShownPawlAndArrowMatchTheFreeDirection` | the other option shows the other pair | n/a | sizes within 0.005 m |

Integrations: cross-element task [sequence-task-370](../requirements.md#sequence-task-370) under [todo-375](../requirements.md#todo-375), "a ratchet/escapement meters motion": planned `RatchetTests.ARatchetKeepsEachPushOfARepeatedInput` (a rack drive struck twice, or a Motor pulsed by a Clock, advances the output and never returns), each mechanism separately implemented and taught.

## 6. Open questions

**Resolved**

1. Over-capacity behaviour: slips backward against a constant torque equal to the capacity. Decided now: the requirement says "holds within capacity", which implies a finite bound; continuous slip is the same law as the Brake.
2. Tooth pitch: not modelled; the catch is continuous and `tooth_count` is removed. Decided now: no requirement row or lesson asks for backlash, and it would add an angular-index capability to the engine story for a visual detail. The legacy row was also continuous (legacy fact 2). D19.
3. Linear ratchet (slider form): not a mode of this element. D19; the Wound spring already carries the slider one-way relation.
4. Release command (pawl lift): not built. D19: release is a separate element (the Escapement, or a Clutch upstream).
5. Positive direction: counter-clockwise about +Z, corrected from the earlier draft to agree with the delivered Motor. D15.
6. `holding_capacity` 1 to 200 N·m, default 40: kept. D1.

**Needs the owner**

None.

## 7. Integration

- **Collaborators.** Motor, Conveyor, Clutch, Reverse gear, System clock (delivered). EL-054 (source of intermittent rotation), EL-063 Winch (lifted load) and EL-057 (shares the hold row) are not delivered.
- **Campaign.** "Ratchet, escapement, indexed carousel": first use levels 81–90, reuse 91–100, 131–140, 146–150 (`docs/planning/invest/campaign-reservations.md:287`).
- **Lesson fixtures.** A level that places this part needs S8.
- **Order.** Planned before EL-057, which reuses the one-way hold and adds the stepped release.

### Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of the identity's spec headed "Package source binding", read by `tools/Coverage/NamedPackageSources.cs` (on `origin/main`, `25df3cd5`; pattern: EL-239 Baseball). The section is not written here: the reader parses every EL spec that carries that heading and refuses one whose preconditions fail. **Blocked on the coverage follow-on story S7.**

| Field | Planned value |
| --- | --- |
| Package | `ratchet` |
| Named identity | EL-056, `../invest/named-elements.md#element-056` |
| Acceptance | `todo-375` and `sequence-task-609` |
| Decision | S342; `scope-corrections.md` has no `s342` block |
| Mode records | `Configuration=Adjustable` (it declares parameters; confirmed at slice planning) |

What it lacks today: an `s342` anchor block in `docs/planning/invest/scope-corrections.md`, and its todo-375 anchor is a scope index, not immediately followed by its sequence-task-609 anchor in `requirements.md`. S7 widens the reader (owner decision 15), including for companion packages and TH, RAD and GAP identities.

## 8. Delivery

- **Slice shape.** Single element, one package, after an engine slice. Nothing in this package may be written as engine code.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which includes ENGINE-SUPPLY-DEMAND and ENGINE-ISLANDS-SLEEP; then ENGINE-JOINT-KINDS (the one-way revolute hold with capacity and a choice-bound direction); then S7 (the coverage follow-on, which widens the reader, owner decision 15). S3 and S4 reduce the edit list.
- **Story text.** ENGINE-JOINT-KINDS owns the one-way hold and the indexed stop on a revolute coordinate (owner decision 3a; `docs/planning/elements/readiness/engine-design-gap.md` section 6 item 9); `ENGINE-ASSEMBLY-JOINTS` keeps only player-made connections and commanded release. Its text must state the hold as this part needs it: "a one-way hold on a revolute coordinate against its support (free in one declared direction, resisting the other up to a declared torque capacity, the direction selectable by a Choice parameter), usable on a coordinate that carries both `DriveIn` and `Drive` ports".
- **Files added.** `elements/Ratchet/Ratchet.csproj`, `RatchetElementDeclaration.cs`, `ratchet.tscn`, `icon.svg`; `CuriousContraptions.tests/RatchetTests.cs` and `RatchetArtTests.cs`.
- **Files edited.** No test-project edit (published main compiles every top-level `*.cs` by one rule). Until S3: `CuriousContraptions.slnx`, the trim list ending at `CuriousContraptions.tests/CSharpStaticGateTests.cs:703`, the switches at `CuriousContraptions.tests/PackageRuntimeParityTests.cs:483` and `:641`. At delivery: the binding section of section 7 appended to this spec. This spec and `TODO.md`.
- **Review tier.** The element slice is data-only only if the engine story publishes the hold with tests first; planned today it is engine tier (one narrower and two missing capabilities).
- **Disqualifiers (the slice's probe re-checks).** (1) The engine story delivers the hold without a capacity, or with a fixed direction: then `holding_capacity` or `free_direction` is not declarable and the story is reopened, not worked around in the package. (2) The hold cannot sit on a pass-through coordinate. (3) Per-option art selection is not exercised by any test: then that binding is *present, untested* and the slice stays engine tier.
