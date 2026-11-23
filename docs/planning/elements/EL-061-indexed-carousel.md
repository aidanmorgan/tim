# EL-061 · Indexed carousel declaration readiness spec

**Current delivery (24 October 2026):** [EL-061 delivery slice](../invest/named-elements.md#element-061). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 named-identity spec. Baseline commit `a6c914e`. Legacy citations use `path@a6c914e:Lstart-Lend` and stay retrievable from git history after the Epic 7 purge. Values marked **proposed** are design values the owner may revise.

Reworked on 22 November 2026 after the Batch I audit, against checkout `8d1d68aa`: the carousel is a self-driven indexer with a supplied position drive (coordinator decision under the owner's standing delegation). The shaft input, ratio transmission and detent of the earlier draft are removed, not kept beside it. Values are starting values under [D1](../invest/delivery-batches.md#3-family-level-decisions).

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-061 |
| Name | Indexed carousel |
| Type | Mechanical |
| Requirement anchor | [element-061](../requirements.md#element-061) (sequence-task-614); scope index [todo-320](../requirements.md#todo-320); integration [sequence-task-371](../requirements.md#sequence-task-371) |
| Named entry | [named-elements.md#element-061](../invest/named-elements.md#element-061); proof owner S371 |
| CAT spec refined or extended | none. Related: [CAT-042 motor](CAT-042-motor.md) (supplied revolute drive), [CAT-039 linear pusher](CAT-039-linear_pusher.md) (the delivered supplied position drive and endpoint contacts, on a slider), [CAT-071 wound spring](CAT-071-wound_spring.md) (pulse-commanded physical request), [CAT-051 powered gate](CAT-051-powered_gate.md) (a loader gated by the interlock) |
| Related identities | [EL-062 docking ferry](EL-062-docking-ferry.md) (the other carrier; same interlock contract), [EL-057 escapement](EL-057-escapement.md) (steps a shaft; shares nothing with this part) |
| Roadmap story | [EL-061 delivery slice](../invest/named-elements.md#element-061); canonical proof owner and prerequisite decisions are recorded there. |
| Status | Not started; blocked on the engine programme (owner decision 3a), relying most on ENGINE-SUPPLY-DEMAND (its supplied drive) and ENGINE-ISLANDS-SLEEP (cargo and a self-locked rotor at rest), with ENGINE-CONTACT-OBSERVATIONS for the Impact switch as a pulse source; then ENGINE-JOINT-KINDS (revolute position drive, and the rest-window observation it publishes) and ENGINE-SIGNAL-NODES (the rest-window supply contact; engine-stories-index C3), section 3; then S7. No package exists |
| Disposition | To deliver as a single element after its engine story |
| Planned package | id `indexed_carousel`, directory `elements/IndexedCarousel/`, class `IndexedCarouselElementDeclaration` |
| Coverage binding | Planned, not yet live (section 7); blocked on S7. No CAT number |
| delivery-batches class | Section 1b, run "Joints" (EL-061, indexing), its own slice. The revolute position drive is ENGINE-JOINT-KINDS' (owner decision 3a; `docs/planning/elements/readiness/engine-design-gap.md` section 6 item 9) and its rest-window contact ENGINE-SIGNAL-NODES' (engine-stories-index C3), split out of the "signal-commanded strike-and-hold or position drive" that `ENGINE-ASSEMBLY-JOINTS` listed at `25df3cd5` (`_bmad-output/planning-artifacts/epics.md@25df3cd5:4459`) |

**Why the design changed.** The requirement reads "A finite-work drive advances a carrier through indexed positions". The earlier draft was a passive geared turntable: no delivered drive could index it (the Wound spring it named has no `Drive` output, `elements/WoundSpring/WoundSpringElementDeclaration.cs:57`–`:65`, and a running Motor never completed an index). The carousel now owns its drive: each accepted pulse advances the rotor one 90° step with bounded torque and power, paid from `PowerIn`.

## 2. Declaration

Shape followed: the Motor (`elements/Motor/MotorElementDeclaration.cs:16`) for a static housing carrying one body on a continuous hinge with a supplied drive; the Linear pusher's position contact as a supplied output (`elements/LinearPusher/LinearPusherElementDeclaration.cs:52`–`:65`); the Wound spring's pulse input bound to a physical request (`WoundSpringElementDeclaration.cs:66`–`:69`).

**Interfaces.** `IHingeElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:20`), `ISupplyElement` (`CuriousContraptions.Abstractions.Electrical/ElectricalContracts.cs:11`), `ISignalElement` (`CuriousContraptions.Abstractions.Signal/SignalContracts.cs:8`), `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`). Base `KinematicMode.Static`, `PayloadPolicy.None`, domains Mechanical | Electrical | Signal, footprint 2 bodies, 1 moving, 0 prismatic, 1 revolute. No executable logic.

**Bodies and colliders.** Local Y is up and is the rotor axis; the unloading station faces +X. Angles are measured about +Y from +X toward −Z, which is the turning direction (counter-clockwise seen from above), so 90° is the −Z pocket and 180° the −X pocket.

| Body | Collider | Shape (m) | Placement (m) |
| --- | --- | --- | --- |
| 0 base (static) | floor | box 2.9 × 0.3 × 2.9 | centre (0, 0, 0); top at y 0.15 is the surface cargo rolls on |
| 0 | fence, 12 boxes | each 0.52 × 0.5 × 0.06 | chords of a 16-sided ring of inner radius 1.24, y 0.15 to 0.65; the four chords between −45° and +45° about +X are omitted, leaving a station opening 1.75 m wide |
| 0 | control post | box 0.5 × 0.6 × 0.5 | centre (−1.7, 0.15, 0) |
| 0 | sockets, 3 | sphere r 0.075 | tangent to the post's +Z face: (−1.7, 0.0, 0.325), (−1.7, 0.2, 0.325), (−1.7, 0.4, 0.325) |
| 1 rotor (hinge 0) | hub | cylinder r 0.2, height 0.52, axis Y | from y 0.15 to 0.67 (centre (0, 0.41, 0)), seated on the floor top with no gap; hinge frame at (0, 0.42, 0), axis Y; the hinge does not collide with the base |
| 1 | paddles, 4 | box 0.95 × 0.5 × 0.06 | radial, centred at radius 0.675, at 45°, 135°, 225°, 315°; bottom 0.02 above the floor |

The four paddles divide the floor inside the fence into four 90° pockets centred on ±X and ±Z. Cargo rests on the static floor and is pushed by a paddle; it does not ride a turning disc.

**Mass and inertia.** Rotor 2.0 kg, declared constant mass frame, axial moment 0.86 kg·m² (four 0.4 kg paddles at radius 0.675 plus the hub), transverse 0.45 kg·m²; gravity (0, −9.81, 0); carried by its hinge.

**Material** (restitution, friction, rolling resistance, bounce threshold): (0, 0.3, 0, 0.1) on both bodies, the delivered drive-part values (`elements/ReverseTransmission/ReverseTransmissionElementDeclaration.cs:37`–`:39`).

**Joint and drive.**

- Hinge 0, rotor: support body 0, `RevoluteTravel.Continuous`, no spring, Coulomb friction torque 0.1 N·m, bodies do not collide with the base.
- **Supplied position drive on hinge 0 (needs the engine story).** The revolute counterpart of the delivered prismatic position drive (`PrismaticDriveDescriptor`, `CuriousContraptions.Abstractions/MechanicalContractValues.cs:89`). Target angle = accepted step count × pitch, on the unwrapped angle. Declared values: pitch π/2 rad; maximum speed 1.5 rad/s; stopping acceleration 3 rad/s²; maximum torque 20 N·m; maximum power 30 W; inactive response self-lock. Enabled while the `PowerIn` route is closed.
- **Advance request.** One `SignalPhysicalRequestDescriptor` (`CuriousContraptions.Abstractions/SignalContractValues.cs:65`) bound to `ActivationIn`, targeting the drive, with an advance-one-step operation (needs the engine story). A request while a step is in progress, or while the drive is unsupplied, is refused with a typed answer and banks nothing.

**Parameters.** None. Pocket count, pitch and drive bounds are fixed per kind (D13).

**Ports.** All on body 0, on the control post, normal +Z.

| `PortId` | Role | Domain | Direction | Signal kind | Position (m) | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| `PowerIn` | PowerIn | Electrical | Input | — | (−1.7, 0.0, 0.325) | pays the drive and feeds the interlock |
| `ActivationIn` | ActivationIn | Signal | Input | Pulse | (−1.7, 0.2, 0.325) | one accepted pulse advances one index |
| `IndexedOut` (minted `PortId`) | Supply | Electrical | Output | — | (−1.7, 0.4, 0.325) | supplied while powered and resting at an index |

**Supply.** One `SupplyConsumerDescriptor` `drive` with a `SupplyDriveTarget(Revolute 0)` (`CuriousContraptions.Abstractions/SupplyContractValues.cs:52`) bound to `PowerIn`, as the Motor. One `SupplyContactDescriptor` `indexed`, fed directly from `PowerIn` (as the Pressure plate binds its contact; no bus node), whose control (needs the engine story) is closed while hinge 0 is within ±3° of an index angle and slower than 0.05 rad/s; `IndexedOut` is bound to its output. Electrical nodes: consumer 12 V, 30 W, 4.8 Ω (R = V²/MaxPower); switch 12 V, 0.05 Ω, 100 W. Supply is on or off today; the debit of the drive's committed work from its source is ENGINE-SUPPLY-DEMAND's (sprint key `engine-supply-drive-demand`), as for the Motor. No stores, no events.

**Presentation.** `PickRadius` 1.9. Body binding (`PresentationBodyBinding`, `CuriousContraptions.Abstractions/ElementPresentation.cs:114`): `Visual/Rotor` → body 1. Three albedo bindings (r, g, b) on `Visual/Lamp`, slate to gold, driven by `PresentationDriver.CommittedActuatorDrive` for Revolute 0 (`ElementPresentation.cs:23`; the Motor lamp, `MotorElementDeclaration.cs:25`–`:27`).

**Palette.** Category `ElementCategory.Motion`, palette order 162 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)), display name "Indexed carousel", colour `#d69c47` (the drive-train family, `DESIGN.md:242`).

### Art and solidity

| Node | Mesh and dimensions (m) | Class |
| --- | --- | --- |
| `Visual/Base` | box 2.9 × 0.3 × 2.9 | solid, floor collider |
| `Visual/Fence` | 270° band, inner radius 1.24, 0.06 thick, 0.5 high | arc chord: 12 box colliders |
| `Visual/Post` | box 0.5 × 0.6 × 0.5 | solid |
| `Visual/Socket_PowerIn`, `_ActivationIn`, `_IndexedOut` | spheres r 0.075 | solid |
| `Visual/Rotor` (hub and four paddles) | as body 1 | solid on moving body 1; follows the body pose |
| `Visual/Lamp` | disc on the post top, flush | decorative: printed flat; colour moved by a binding |
| `Visual/StationArrow` | arrow on the floor in the opening, flush | decorative: printed flat |
| `Visual/IndexPointer` and four hub notches | pointer on the fence top and marks on the hub top, flush | decorative: printed flat; alignment of pointer and notch shows an index without a binding |

Colours (`DESIGN.md:80`, `:242`): floor and post Navy `#1c2836`, fence Sky Blue `#7ebcd4`, paddles Ochre `#d99e32`, hub Cream `#fff8e9` (part-body cream, owner decision 4), pointer and arrow Gold `#f7cb52`. Icon: a top view of a ring with one open quarter, four spokes and a ball in one pocket. At a glance it differs from the Reverse gear (two small wheels on a block) by its large fenced floor and four-spoke rotor.

### Behaviour and numbers

- **Laws.** One revolute coordinate about Y. A torque- and power-bounded position drive moves it toward the target on a stopping profile (speed ≤ 1.5 rad/s, |acceleration| ≤ 3 rad/s², |torque| ≤ 20 N·m, power ≤ 30 W); Coulomb friction 0.1 N·m; rigid contact between paddles, cargo, floor and fence. Unsupplied or uncommanded, the drive self-locks the rotor where it is.
- **Finite work.** A step does at most torque × pitch = 20 × π/2 = 31.4 J. A free step needs about 0.97 J of kinetic energy (½ × 0.86 × 1.5²) and 0.16 J against friction.
- **Step time.** 0.5 s to reach 1.5 rad/s (0.375 rad), 0.82 rad at speed (0.55 s), 0.5 s to stop: about 1.55 s per index.
- **Obstruction.** The torque bound is the whole mechanism: 20 N·m at a 0.9 m cargo radius is 22 N, enough to push any delivered ball along the floor and not enough to move a Wall. A blocked rotor stalls short of its target, the drive does no further work (the stalled-drive rule, `CuriousContraptions.tests/RevoluteDriveTests.cs:307`), and the index does not complete.
- **Missing power.** With `PowerIn` open the drive is disabled, a pulse is refused and nothing moves. Power lost mid-step locks the rotor between indexes; restoring it resumes the same step.
- **Why four fixed pockets.** A parameter can scale a collider (`ColliderParameterBinding`, `SceneGeometry.cs:27`) but cannot add or remove one, so pocket count is a property of the scene. Four 90° pockets each hold a Basketball: a 0.34 m ball touching the fence sits 0.90 m from the axis, where the pocket is 1.27 m wide.
- **Why cargo rolls on the static floor.** A ball on a turning disc rolls outward and leaves anywhere; pushed along a fixed floor inside a fixed fence it can only leave where the fence is open. That is the requirement's "carrier", and it avoids friction carry on a moving body.
- **Interlock and tick order.** `IndexedOut` is read from the committed pose at the start of the electrical phase, like the delivered endpoint contacts, so a consumer is supplied on the tick after the rotor settles (`CuriousContraptions.tests/LinearPusherTests.cs:52`). It is closed at Run start, when the rotor rests at index 0 and `PowerIn` is supplied. It informs a loader; it never moves or supports cargo. A pulse is routed in the signal phase and takes effect at the next committed physics boundary, as a retention release does (`CuriousContraptions.tests/RetentionRequestTests.cs:125`).
- **Unloading.** The station pocket is open across its whole 90°. A ball pushed by the trailing paddle loses the fence's inward force at the opening and leaves along its tangent as its pocket enters the station; pockets elsewhere are closed.

## 3. Engine capabilities (capability probe)

| Capability | Status | Symbol | Exercising test or owning story |
| --- | --- | --- | --- |
| Static multi-box body including a ring of chord boxes | *present* | `ISceneGeometryElement` `SceneGeometry.cs:10`; Powered gate's 16-box sleeve `elements/PoweredGate/PoweredGateElementDeclaration.cs:10`–`:13` | `CuriousContraptions.tests/PoweredGateTests.cs:109` |
| Continuous hinge carrying a multi-collider body | *present* | `RevoluteHingeDescriptor` `CuriousContraptions.Abstractions/MechanicalContractValues.cs:149`; `RevoluteTravel.Continuous` `:132` | `CuriousContraptions.tests/RevoluteDriveTests.cs:195`; `CuriousContraptions.tests/PackageRuntimeParityTests.cs:219` |
| Hinge Coulomb friction that holds below its torque | *present* | `FrictionTorque` `MechanicalContractValues.cs:158` | `CuriousContraptions.tests/RevoluteHingeFrictionTests.cs:77` |
| A revolute drive enabled by the package's own `PowerIn` | *present* | `SupplyDriveTarget` `SupplyContractValues.cs:52` | `CuriousContraptions.tests/MotorTests.cs:990`, `:361` |
| A torque-bounded supplied drive stalls against an obstruction and then does no work | *present* (velocity drive) | `RevoluteDriveDescriptor` `MechanicalContractValues.cs:138` | `RevoluteDriveTests.cs:307`; `MotorTests.cs:732` |
| Pulse input routed to a physical request with typed answers | *present* (prismatic retention target) | `SignalPhysicalRequestDescriptor` `SignalContractValues.cs:65` | `CuriousContraptions.tests/RetentionRequestTests.cs:10`, `:125` |
| Supply contact and supplied output port | *present* | `SupplyContactDescriptor` `SupplyContractValues.cs:40` | `CuriousContraptions.tests/LinearPusherTests.cs:65` |
| Moving art follows its body; lamp follows the committed drive state | *present* | `PresentationBodyBinding` `ElementPresentation.cs:114`; `CommittedActuatorDrive` `:23` | `MotorTests.cs:1075`, `:265` |
| Run/Reset and save/load | *present* | generic | `CuriousContraptions.tests/CatalogueIntegrationTests.cs:273` |
| A driven hinged body sweeping resting cargo along a static floor | *present, untested* | generic contact | nearest: `CuriousContraptions.tests/ImpactLeverTests.cs:73` (a struck beam flings a payload); `:380` is a limit-stop test. First exercised by row C8; if it fails, an engine repair slice owns it |
| A supplied position drive on a revolute coordinate, stepped by a declared pitch, with speed, acceleration, torque and power bounds and self-lock | *missing* | the revolute drive is velocity only (`MechanicalContractValues.cs:138`); the prismatic counterpart exists (`PrismaticDriveDescriptor` `:89`, tested at `CuriousContraptions.tests/DirectionalPrismaticTests.cs:19`) | ENGINE-JOINT-KINDS (revolute position drive); the wording to state is Batch I's gap G2 (`readiness/batch-i.md` section 5) |
| A physical-request target and an advance operation for that drive | *missing* | `PhysicalRequestTargetKind { None, Retention, FiniteWorkActuator }` (`CuriousContraptions.Abstractions/PhysicalRequestValues.cs:6`); the existing finite-work actuator (`CuriousContraptions.Abstractions/FiniteWorkActuatorValues.cs:6`) was not read in full, and the planning scout checks whether the stepped drive is declared as one | ENGINE-JOINT-KINDS |
| A supply contact controlled by a **revolute** coordinate resting in a periodic angular window below a declared speed | *narrower* | only `PrismaticContactControl` exists (`CuriousContraptions.Abstractions/SignalContactControl.cs:13`; refusal of any other coordinate kind `CuriousContraptions.Bundle/SupplyBundleValidation.cs:81`–`:83`) | ENGINE-SIGNAL-NODES (the contact; index C3), reading the window observation that ENGINE-JOINT-KINDS publishes (gap G2 wording) |

Counts: 9 present, 1 present-untested, 1 narrower, 2 missing; two owning stories, one per capability: ENGINE-JOINT-KINDS (the drive, its request and the published window observation) and ENGINE-SIGNAL-NODES (the contact). Nothing may be solved with carousel-specific engine code: the drive is the general revolute counterpart of a delivered prismatic capability, and the contact generalises one existing control.

**Element dependencies.** Battery, a pulse source (Impact switch, System clock), Powered gate, balls and Wall, all delivered.

### Engine conformance

- **Declaration data only.** One hinge, one declared drive, one request node, one contact, three ports. No per-tick callback, state machine or engine branch on this part; the step count is the drive's own state in the engine's tables.
- **Generic laws.** Indexing is a bounded position-drive law; obstruction is the torque bound plus contact; the interlock is a contact control reading a committed coordinate; unloading is contact geometry.
- **Layers.** Colliders: 1 floor box, 12 fence boxes, 1 post box, 3 socket spheres (static); 1 hub cylinder and 4 paddle boxes (rotor). Visuals: the art table above, none used as a collider. Animation: `Visual/Rotor` follows a body pose; `Visual/Lamp` colour is a presentation binding reading committed drive state; every other node is moved by nothing. Visible = solid: base, fence (by its 12 chord boxes, none in the station opening), post, sockets, hub and paddles each have colliders; the lamp, station arrow, pointer and notches are printed flat.
- **One-way flow and typing.** The binding reads committed state and writes nothing back. Ports, roles, domains and the request operation are enums; ids are typed; values are f32.
- **Flat state.** 2 bodies, 22 colliders (18 boxes, 3 spheres, 1 cylinder on the base and rotor as listed), 1 revolute joint, 1 drive, 1 signal node, 2 supply nodes, 2 electrical nodes, 0 stores, 0 sensors, 1 body binding and 3 albedo bindings.
- **Performance.** The base is static. The 12 fence chords follow the delivered Powered gate's 16-box sleeve and exist to keep cargo in, not for looks. The contact reads one coordinate; nothing scans the scene. No budget for collider counts is stated in the engine documents; none is invented here. No island sleeping exists today; resting cargo and a self-locked rotor cost nothing only once ENGINE-ISLANDS-SLEEP (owner decision 3a) is delivered, ahead of this part.
- **Tests are scenarios.** Section 5 runs the production simulation through placed parts and wires only.

## 4. Sources and legacy

- Requirements row: "Obstruction or missing power prevents the completed index." No variants.
- todo-320 integration: carriers transfer cargo only when actually docked or indexed.

No legacy implementation. Shared facts:

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | Ratio coupling between axial coordinates is bidirectional and phase-free; shared inertia sets reactions. | `engine/physics/PhysicsTransmissionJoint.cs@a6c914e:L7-L13` | carry forward | A blocked platter loads the drive. |
| 2 | Hinge travel bounds must stay strictly inside (−π, π). | `engine/physics/PhysicsJoint.cs@a6c914e:L137-L139` | do not carry forward as a limit on the platter | A carousel turns indefinitely; index angles must be computed on the unwrapped angle. |
| 3 | Motor source: target speed with bounded torque and work (default 6 rad/s, 20 N·m). | [CAT-042 spec](CAT-042-motor.md) legacy facts 5–6 | carry forward via CAT-042 | Drive scale for the parameters above. |

**Files harvested:** `engine/physics/PhysicsTransmissionJoint.cs`, `engine/physics/PhysicsJoint.cs`. Searched with no hit: `parts/`, `engine/`, `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/Campaign`, `reference/` (terms: carousel, turntable, index, rotary table).

## 5. Acceptance outline

Requirement row: [element-061](../requirements.md#element-061). Fact 1 no longer applies to this package: it declares no ratio coupling.

- **Chrome UI recipe (pending, post-catalogue).** Place a Battery, an Impact switch, an Indexed carousel and a Powered gate; wire Battery → carousel `PowerIn`, switch `ActivationOut` → `ActivationIn`, `IndexedOut` → gate `PowerIn`; drop a Basketball into the pocket opposite the station and roll balls onto the switch. Verify placement and links. Run: each strike advances one index, the gate opens only while the rotor rests at an index, and the ball leaves at the station after two steps.

Planned tests are in `IndexedCarouselTests`. "Angle" is the committed rotor hinge coordinate; "indexes" means it comes to rest within 3° of the next multiple of 90°, the interlock's own window. The standard rig is a run-controlled Battery on `PowerIn` and a pulse source on `ActivationIn`.

| Row | Given / when / then | Planned test | Control | Boundary | Tolerance and stall guard |
| --- | --- | --- | --- | --- | --- |
| C1 | Powered; one pulse. Then the rotor rests within 3° of 90°, and the gate on `IndexedOut` is unsupplied during the step and supplied again after it | `APulseAdvancesOneIndexAndTheInterlockRecloses` | C2, C3, C5 | three pulses 3 s apart end at 270° | guard 5 s per step (derived 1.55 s); the gate must be unsupplied at some tick while the angle is between 10° and 80° |
| C2 | Requirement outcome, power: `PowerIn` never supplied; three pulses. Then the angle stays within 1° of 0, and supplying power afterwards with no further pulse moves nothing | `WithoutPowerNoIndexCompletesAndNoStepIsBanked` | C1 | power cut mid-step: the rotor locks between indexes, the gate stays unsupplied, and restoring power completes that step | observed 10 s; lock within 1° of where it stopped |
| C3 | Requirement outcome, obstruction: a Wall (`width` 0.4, `height` 0.6, `thickness` 0.12; the delivered minimums are 0.4, 0.4 and 0.12) standing on the floor as a radial plate centred at radius 0.7 (so from 0.5 to 0.9 m), its near face on the 75° radial line, in the −Z pocket ahead of the 45° paddle; at Run start it touches nothing. One pulse. Then the rotor stalls near 27° (75° − 45° − asin(0.03/0.5) = 26.6°: the paddle's 0.03 m half-thickness meets the plate's inner end first), the drive's work stops growing, and the gate is unsupplied once the rotor has left index 0 | `AnObstructionPreventsTheCompletedIndex` | C1 | — | stalled angle between 10° and 60° (a missing obstruction reaches 90°; a Wall overlapping the paddle at start would stall near 0°); drive work over the last second under 1 % of torque × speed × 1 s (the stalled-work rule of `MotorTests.cs:732`); observed 10 s |
| C4 | Two pulses 2 ticks apart: one index, not two | `APulseDuringAStepIsDropped` | the same two pulses 3 s apart: two indexes | — | step count exact |
| C5 | Powered, no pulse, 5 s: the rotor stays within 1° of 0 and the gate stays supplied | `WithoutAPulseTheRotorRestsAtItsIndex` | C1 | — | exact supplied flag |
| C6 | The drive's committed positive work over one free step is at least the rotor's peak kinetic energy (½ × 0.86 × 1.5² = 0.97 J) and at most torque × pitch = 20 × π/2 = 31.4 J. Friction is not in the lower bound: during the stop friction helps brake the rotor, so the drive's positive work is about 0.97 + 0.1 × (0.375 + 0.82) = 1.09 J, below the 1.13 J a bound with whole-step friction would demand | `AStepDoesFiniteWork` | C3 (stalled: no growth) | a Bowling ball in the moving pocket: the same upper bound | upper bound plus 5 %; a drive that worked without bound, or one that moved the rotor without paying its kinetic energy, fails |
| C8 | Cargo: a Basketball in the pocket opposite the station (180°). After two index steps it has left through the station opening; a second Basketball in the −Z pocket (90°, the one 90° past the station in the turning direction) is still inside the fence after one and after two steps (it is then at 180° and 270°, both fenced) | `CargoLeavesOnlyAtTheStation` | no pulse: both balls stay in their pockets | — | "left": centre more than 1.6 m from the axis within 30° of +X; "inside": centre under 1.0 m from the axis; guard 30 s |
| C10 | Wrong-kind connections refused: an electrical wire into `ActivationIn`, an activation wire into `PowerIn`, a Level output into `ActivationIn`, an activation wire from `IndexedOut` | S4 row R5 once delivered, else `ConnectionsAcceptTheirOwnKindOnly` (pattern `CuriousContraptions.tests/ConveyorTests.cs:529`) | the rig's links are admitted | — | exact refusal |
| C11 | Parameter refusal | not applicable: no parameters. S4 row R2 has nothing to run | — | — | — |
| C12 | Reset, and save then load, taken mid-step restore angle 0, step count 0, both balls and wires, and a second Run repeats C1; a pulse sent in the tick before Reset fires nothing afterwards | `ResetAndSaveLoadRestoreTheRotorAndCargo`; placed alone: `CatalogueIntegrationTests.cs:273` | — | — | authored state as the generic test compares it |
| C13 | Rotor art follows its body; every solid node has its collider; the fence has no collider in the station opening; the lamp follows the committed drive state | `IndexedCarouselArtTests.ArtFollowsBodiesAndCollidersMatch` (pattern `MotorTests.cs:1075`, `PoweredGateTests.cs:109`) | — | — | exact counts; pose within 1 mm |

The interlock window (closed at 1.5° and open at 4.5° from an index) and the drive's stop accuracy are qualified on the engine story's own fixtures, which can set a start angle; a package cannot.

## 6. Open questions

**Resolved**

1. Passive turntable or self-driven indexer? **Self-driven, with a supplied position drive. Coordinator decision under the owner's standing delegation (22 November 2026).** Reason: the requirement asks for a finite-work drive that advances the carrier; no delivered drive could index the passive design. The shaft input, 4:1 transmission and periodic detent are removed.
2. How is a step commanded? By a Pulse on `ActivationIn`. Decided now: it is the delivered idiom for a commanded physical request (Wound spring), and one pulse per index is the readable lesson.
3. Does the index output fire on every index? Yes, and it is a held state, not an occurrence: `IndexedOut` is supplied whenever the powered rotor rests at any index. A loader interlock needs a state, and it matches the ferry's dock outputs.
4. Signal output or electrical contact? An electrical contact (role Supply), the one existing position-contact idiom, so the engine story generalises one thing.
5. Direction and reverse? One direction, counter-clockwise seen from above. D19: no row or lesson reverses a carousel.
6. Binding omits ContactImpulse and GeometryQuery. D14: the map row is navigation; the probe above is the authority.
7. `positions`, `reduction`, `detent_stiffness` parameters? Dropped: D13, and pocket count cannot be a parameter at all.
8. Turning disc or paddles over a fixed floor? Paddles over a fixed floor with a fenced perimeter (section 2), so cargo leaves only at the station.

**Needs the owner**

None.

## 7. Integration

- **Collaborators.** Battery, Impact switch, System clock, Pressure plate, Powered gate, balls, Wall (all delivered). Port kinds agree: a `Supply` (Electrical, Output) into `PowerIn`; a Pulse `ActivationOut` into `ActivationIn` (Signal, Input, Pulse); `IndexedOut` (Electrical, Output, Supply) into a `PowerIn`.
- **Shared contract with EL-062.** Both carriers expose their "cargo may transfer" state as supplied electrical outputs fed from their own `PowerIn`.
- **Sorting and carrier task** [sequence-task-371](../requirements.md#sequence-task-371): rows C1–C3 and C8 are the carousel's separate obligation.
- **Campaign.** First use levels 81–90 beside the ratchet and escapement, reuse 91–100, 131–140 and 146–150 (`docs/planning/invest/campaign-reservations.md:287`).
- **S8.** A lesson that places the carousel needs the lesson-fixture story S8 first.

### Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of the identity's spec headed "Package source binding", read by `tools/Coverage/NamedPackageSources.cs` (on `origin/main`, `25df3cd5`; pattern: EL-239 Baseball). The section is not written here: the reader parses every EL spec that carries that heading and refuses one whose preconditions fail. **Blocked on the coverage follow-on story S7.**

| Field | Planned value |
| --- | --- |
| Package | `indexed_carousel` |
| Named identity | EL-061, `../invest/named-elements.md#element-061` |
| Acceptance | `todo-320` and `sequence-task-614` |
| Decision | S371; `scope-corrections.md` has no `s371` block |
| Mode records | `Configuration=Fixed` |

What it lacks today: an `s371` anchor block in `docs/planning/invest/scope-corrections.md`, and its todo-320 anchor is a scope index, not immediately followed by its sequence-task-614 anchor in `requirements.md`. S7 widens the reader (owner decision 15), including for companion packages and TH, RAD and GAP identities.

## 8. Delivery

- **Slice shape.** One element after its engine story; then declaration data, scene, icon and tests.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which includes ENGINE-SUPPLY-DEMAND, ENGINE-ISLANDS-SLEEP and ENGINE-CONTACT-OBSERVATIONS (the Impact switch's contact trigger, when it is the pulse source); then ENGINE-JOINT-KINDS, at least its revolute position drive with the advance request and the published rest-window observation, and ENGINE-SIGNAL-NODES' rest-window supply contact that reads it (Batch I's gap G2 wording; index C3); then S7 (the coverage follow-on, which widens the reader, owner decision 15); S3 optional.
- **Files added.** `elements/IndexedCarousel/IndexedCarousel.csproj`, `IndexedCarouselElementDeclaration.cs`, `indexed_carousel.tscn`, `icon.svg`; `CuriousContraptions.tests/IndexedCarouselTests.cs`, `IndexedCarouselArtTests.cs`.
- **Files edited.** No test-project edit (published main compiles every top-level `*.cs` by one rule); until S3 `CuriousContraptions.slnx`, `CuriousContraptions.tests/CSharpStaticGateTests.cs:703` and the placement switch at `CuriousContraptions.tests/PackageRuntimeParityTests.cs:641` (generic list `:653`); at delivery the binding section of section 7; this spec and `TODO.md`.
- **Review tier.** Engine tier unless ENGINE-JOINT-KINDS has published the drive and the request and ENGINE-SIGNAL-NODES the contact, with tests, and row C8's contact case is already exercised; then data-only.
- **Disqualifiers the probe must re-check.** The engine story delivers the position drive without a torque bound or without self-lock (then obstruction or power loss would not prevent the index, and the story is reopened); the target is computed on a wrapped angle (the fourth step would fail); the rest-window contact has no speed condition; an advance request while unsupplied is banked; twelve fence boxes plus sockets in one static body breach a collider-count rule not read here.
