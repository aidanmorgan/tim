# CAT-007 · beam_shutter declaration readiness spec

**Current delivery:** [CAT-007 delivery owner](../../../_bmad-output/planning-artifacts/epics.md#catalogue-slice-ownership). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes.

Build-ready plan for an undelivered part, repaired on 22 November 2026 after the independent audit (`docs/planning/elements/readiness/audits/elements-batch-c.md`). Checked against checkout `spec-drafts` (`8d1d68aa`); published main is `25df3cd5`, and its two later commits touch nothing this plan cites except that the test project now compiles every top-level test file by one rule. Sections 1–3 and 5–9 are the current plan. Section 4 is the legacy harvest at `a6c914e`, kept verbatim as evidence; where it differs from the plan (stroke, frame, stripes, material, the `Powered` event, the blade controller), the plan governs.

## 1. Identity

| Field | Value |
| --- | --- |
| CAT ID | CAT-007 |
| Kind | `beam_shutter`, catalogue title "Beam shutter" |
| Requirement anchor | [CAT-007](../requirements.md#current-cat-007); retained behaviour [todo-292](../requirements.md#todo-292); consumers [CAT-007-I](../invest/current-consumers.md#cat-007-i) |
| Mapped identities | None. [RAD-06 Powered radiation shutter](../invest/named-elements.md#radiation-06) is a separate element that may copy this declaration shape |
| Roadmap story | 13.3 Beam Shutter Mechanical Guillotine ([epics](../../../_bmad-output/planning-artifacts/epics.md), Epic 13) |
| Status | **Not delivered**: no `elements/BeamShutter/`. Blocked on the engine programme (owner decision 3a, 22 November 2026), then `ENGINE-JOINT-KINDS`, `ENGINE-OPTICAL`, CAT-036 Laser and CAT-038 Light receiver (section 8) |
| Disposition | To deliver as a single element. Planned package id `beam_shutter`, directory `elements/BeamShutter/`. Not a duplicate or variant of the Powered gate: no tube mouths, a different stroke, a rope port and an optical acceptance |

## 2. Declaration

The closest delivered shape is the Powered gate (`elements/PoweredGate/PoweredGateElementDeclaration.cs:14`–`:96`): a static frame, a sprung blade on a supplied prismatic drive, one `PowerIn`. The shutter follows it and differs only in the values below. Values marked proposed are starting values (D1).

**Geometry decision (coordinator, 22 November 2026, under the owner's standing delegation).** The legacy frame left 0.425 m of open space above the closed blade and 4 cm slits beside it, so a closed shutter did not block every beam through its frame. The frame is resized so that the opening is exactly the blade, and the space the open blade rises into is closed by a back plate behind it: a closed blade leaves no gap above, below or beside it. The raised blade stands in front of the plate between tall rails, with nothing in front of it, so a rope can rise from its face. Reason: "optical blocking follows actual blade pose" must mean that a closed shutter blocks; and ropes are solid (owner decision, 22 November 2026), so a rope may not pass through a housing.

**Spring rest decision (owner decision, 22 November 2026).** The blade's spring declares a rest position so that an unpowered blade closes in every mounting orientation, with no orientation limit. The shutter declares the rest coordinate 0.85 m below closed, outside its travel (a preload). This needs a general capability that does not exist yet: the spring rest position of `ENGINE-JOINT-KINDS` (section 3).

| Item | Planned value | Reason or source |
| --- | --- | --- |
| Interfaces | `ISliderElement`, `ISupplyElement`, `ISceneGeometryElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:15`; `CuriousContraptions.Abstractions.Electrical/ElectricalContracts.cs:11`; `CuriousContraptions.Abstractions/SceneGeometry.cs:10`) | as the gate |
| Supported domains | Mechanical, Electrical and Rope | the gate's two plus Rope for the `Tie` port |
| Footprint | 2 bodies, 1 moving, 1 prismatic, 0 revolute | as the gate (`:34`) |
| Blade (package body 1) | box 0.07 × 1.2 × 1.2 m centred on the body origin (half-extents 0.035, 0.6, 0.6); mass 1 kg; moments (0.24, 0.12041, 0.12041) kg·m² from m(b² + c²)/3; gravity (0, −9.81, 0) | harvest fact 2 |
| Opening | y from −0.6 to 0.6 and z from −0.6 to 0.6: exactly the closed blade | geometry decision above |
| Foot (static) | box 0.65 × 0.2 × 1.65 m at (0, −0.70, 0); its top face is y −0.6, the blade's closed lower edge | moved up 0.08 m from the legacy −0.78 |
| Back plate (static) | box 0.185 × 1.2 × 1.65 m at (−0.1325, 1.2, 0): it spans x −0.225 to −0.04 (5 mm behind the blade), y 0.6 to 1.8, so its lower edge meets the closed blade's top edge and it closes the space the open blade occupies | replaces the legacy header; behind the blade so that nothing stands in front of the raised blade |
| Rails (static) | two boxes 0.18 × 2.4 × 0.12 m at (0, 0.6, ±0.66): inner faces at z ±0.6, the blade's side edges, from the foot top to y 1.8 | they guide the blade over its whole travel; connected bodies do not collide, so the blade slides between flush faces |
| Socket and indicator (static) | socket sphere r 0.075 m at (0, −0.70, 0.86), proud of the foot's end face; indicator sphere r 0.07 m at (0.34, −0.70, 0), proud of the foot's front face | drawn proud, so solid (owner decision, 21 November: sockets keep colliders); on the foot, clear of the blade's travel and of a rope |
| Materials | frame and blade: restitution 0.1, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s | the gate's values (`:47`–`:48`), so a ball meets both blades alike |
| Slider | along local +Y, travel [0, 1.2] m, spring 14 N/m with declared rest coordinate −0.85 m (0.85 m below closed), damping 4 N·s/m, connected bodies do not collide | stroke = blade height, so the open blade clears the whole opening. Spring force toward closed is 14 × (x + 0.85) N: 11.9 N at closed, 28.7 N at open |
| Drive | supplied; target 1.2 m, maximum speed 2.8 m/s, stopping acceleration 14 m/s², effort 60 N, power 120 W | the gate's drive (`:84`–`:85`). Upright full-open load is 28.7 + 9.81 = 38.5 N, inside 60 N. Opening takes about 0.63 s (0.2 s ramp each end over 0.28 m, 0.64 m at cruise) |
| Parameters | none | D13 |
| Port `PowerIn` | `PortId.PowerIn`, Electrical, Input, role PowerIn, (0, −0.70, 0.86), normal +Z | as the gate (`:38`), moved to the foot |
| Port `Tie` | `PortId.Tie`, Rope, Bidirectional, role Tie, attachment `Load`, owned by the blade body, blade-local (0.035, 0.5, 0), normal +X; scene locator `Loc_Port_Tie` is a direct child of `Visual/Blade` | Story 13.3 "opened by signal or rope" and CAT-034 "slack/taut shutter loads". On the blade's upper front face it reaches y 1.7 at full travel, below the rail tops, and nothing stands in front of it. A solid rope strand (owner decision, 22 November 2026) rises from here to a pulley in front and above; whether the tie must stand one strand radius proud of the face is a point for the `ENGINE-CONNECTION-MATERIALS` probe |
| Supply graph | `PowerIn` → one consumer → the prismatic drive | as the gate (`:88`–`:90`) |
| Electrical node | `("motor", Consumer, rated voltage 12 V, resistance 24 Ω, capacitance 0, maximum power 120 W)` | the gate's six fields (`:94`) with maximum power equal to the drive bound. Validated as finite and read by no law (only `Role` is read): unread dead data, to be consumed by the `ENGINE-SUPPLY-DEMAND` story or removed forward-only |
| Stores, events | none | no requirement reads a blade event |
| Presentation | `Visual/Blade` follows package body 1; `Visual/Indicator` albedo by `CommittedActuatorState` (slate idle, gold powered, ochre blocked); pick radius 2.0 m (proposed: the frame reaches 1.8 m above the origin) | as the gate (`:16`–`:21`) |
| Palette | category Optics, palette order 604 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)), colour (0.97, 0.796, 0.322), the gate's gold | the Optics member of `ElementCategory` does not exist yet and is owned by `ENGINE-OPTICAL` (section 3) |
| Description | "Electricity lifts the gold blade to let light through. Whenever supply is lost its spring closes it, in any mounting; a ball in the blade's path stops it closing until cleared." | owner decision, 22 November 2026 |

**Art and solidity (planned scene `beam_shutter.tscn`).** Three layers, declared separately.

| Node | Mesh | Layer and class |
| --- | --- | --- |
| `col_BackPlate`, `col_Foot`, `col_RailLeft`, `col_RailRight`, `col_Socket_PowerIn`, `col_Indicator`; `Visual/Blade/col_Blade` | box and sphere shapes of the sizes above | colliders: hidden proxies, never rendered |
| `Visual/BackPlate`, `Visual/Foot` (navy `#293954`), `Visual/RailLeft`, `Visual/RailRight` (cream `#fff8e9`) | boxes as above | visuals: static solids, each backed by its proxy |
| `Visual/Blade/Plate` | box 0.07 × 1.2 × 1.2 m, gold | visual: solid, moved by the blade body's pose |
| `Visual/Blade/StripeFront`, `StripeBack` | cream quads 0.04 × 1.05 m | visual: decorative, printed 0 to 0.5 mm off the blade face. The harvest's 8 mm thick stripes would stand 6 mm proud and are not carried |
| `Visual/Blade/TieMark` | gold disc r 0.05 m at the `Tie` | visual: decorative, printed flat |
| `Visual/Socket_PowerIn`, `Visual/Indicator` | spheres r 0.075 and 0.07 m | visuals: solid, each backed by its proxy; the indicator's colour is moved by a presentation binding |

Icon: a navy frame with the gold blade half raised and a dashed line crossing the opening. It differs from the Powered gate at a glance by shape: no tube collars or mouths, and a flat blade seen face-on with witness stripes.

## 3. Engine capabilities

Capability families: see the [CAT-007 map row](../general-engine-element-map.md); the slice's probe is the authority (D14).

**Capability probe (22 November 2026).** Tests were found by name and line; none was run. Counts: 6 present, 1 present-untested, 0 narrower, 3 missing. Nothing below needs element-specific engine code.

| Capability | Status | Symbol | Exercising test, or owner |
| --- | --- | --- | --- |
| Sprung prismatic slider with a supplied bounded drive | present | `PrismaticSliderDescriptor`, `PrismaticDriveDescriptor` (`CuriousContraptions.Abstractions/MechanicalContractValues.cs:104`, `:89`) | `DeclaredPrismaticDriveTests.ThePoweredGateOpensToItsTargetWithinItsDeclaredSpeedEffortAndPowerOnEverySubstep` (`CuriousContraptions.tests/DeclaredPrismaticDriveTests.cs:92`) |
| Supply route from `PowerIn` to a drive | present | `SupplyGraphDescriptor`, `SupplyConsumerDescriptor` (`CuriousContraptions.Abstractions/SupplyContractValues.cs:30`, `:42`) | `PoweredGateTests.DeclaredDirectGraphFollowsOnlyItsSource` (`CuriousContraptions.tests/PoweredGateTests.cs:209`) |
| Static and dynamic box colliders, sphere colliders for drawn sockets | present | `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`) | `VisibleSolidColliderTests.EverySolidArtPartHasItsColliderAndNoColliderReachesBeyondTheArt` (`CuriousContraptions.tests/VisibleSolidColliderTests.cs:186`) |
| Actuator-state colour binding on an indicator | present | `PresentationDriver.CommittedActuatorState` (`CuriousContraptions.Abstractions/ElementPresentation.cs:24`) | `DeclaredPresentationTests.GateCueUsesActualCommittedDriveAndAppliesNativeMaterial` (`CuriousContraptions.tests/DeclaredPresentationTests.cs:120`), `TheGateCueIsOchreWhileABallHoldsTheBladeAtRest` (`:184`) |
| A closing sprung blade stopped by a ball | present | generic contact against the slider spring; blocked state from `ActuatorObservations.IsBlocked` (`engine/gpu/PhysicsPresentationRead.cs:15`) | `LatchSupplyFixtureTests.BallPartlyThroughStopsClosingBladeUncrushed` (`CuriousContraptions.tests/LatchSupplyFixtureTests.cs:289`); `DeclaredPrismaticDriveTests.BlockedNeedsContactInteriorAndRestTogether` (`:387`) |
| Run/Reset and save/load of any admitted declaration | present | generic lifecycle | `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad` (`CuriousContraptions.tests/CatalogueIntegrationTests.cs:273`) |
| A package rope port owned by a slider body | present, untested | `PortDescriptor` with a body owner (`CuriousContraptions.Abstractions/PortDescriptor.cs:55`) | only the hinge-body case has a package test (`ImpactLeverTests.cs:414`); `RopeTests.cs:102` is a law fixture, not a package port. The slice adds row S-8. Rope-port admission enumerates the role names `Tie`, `TieA`, `TieB` (`engine/gpu/WorkshopConnections.cs:163`–`:164`); this part uses `Tie`, so it is not blocked by that enumeration, whose removal belongs to `ENGINE-CAPABILITY-INSTANCES` |
| A declared rest coordinate on a coordinate's spring | missing | the slider spring acts from the closed limit; a preload through the limits is refused (`CuriousContraptions.Bundle/MechanicalValueDomain.cs:78`–`:81`) | `ENGINE-JOINT-KINDS`, spring rest position (owner decision 6; coordinator decision 21, 22 November 2026): "A prismatic slider declares its spring rest coordinate, which may lie outside its travel (a preload); `SliderTravel` admits it; the starting offset stays a separate value. An unpowered spring-closed slider closes upright, on its side and inverted." The Powered gate is its other consumer |
| Optical occlusion by the committed pose of a moving body | missing | none: the engine has no beam contract | `ENGINE-OPTICAL`, under the optical conventions of [EL-176](EL-176-red-laser-emitter.md) section 10: every collider with collision participation, static or dynamic, occludes at this tick's final pose (item 6) |
| Palette category for optical parts | missing | `ElementCategory` has Motion, Goals, Power, Control and Structure only (`CuriousContraptions.Abstractions/ElementCategory.cs`) | `ENGINE-OPTICAL` (coordinator decision 5, 22 November): the story adds the enum member, its palette heading and admission, so no element slice edits the enum |

**Engine gaps this part inherits (not designed around).** Every bounded prismatic drive enforces its power bound by capturing a whole-world snapshot each substep while any such drive is active and re-solving the substep up to eleven times when the bound is exceeded (`CuriousContraptions.Simulation/WorkshopPhysicsEngine.Motors.cs:35`–`:66`): scene-proportional work, owned by `ENGINE-ISLANDS-SLEEP` (owner decision 3a, 22 November 2026), whose contract confines drive-budget trials to the island they affect, so the cost becomes island-proportional, not removed. Island sleeping is not implemented; a resting body is still stepped, at the cost of its broadphase proxies and rows every substep (no sleep or deactivation code in `CuriousContraptions.Simulation`). Owned by `ENGINE-ISLANDS-SLEEP`. The supplied drive debits nothing from its source; owned by `ENGINE-SUPPLY-DEMAND` (sprint key `engine-supply-drive-demand`).

**Element dependencies.** Battery (delivered). CAT-036 Laser and CAT-038 Light receiver (undelivered) for the optical rows. Weight, Pulley, Rope anchor and Impact lever (delivered) for the rope rows.

## 4. Legacy harvest

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | Fail-closed rectangular optical shutter; its moving physical box is the beam blocker | `parts/BeamShutterPart.cs@a6c914e:L7` | Carry forward | Requirement: blocking follows actual pose |
| 2 | Stroke 1.3 m; blade half extents (0.035, 0.6, 0.6) | `parts/BeamShutterPart.cs@a6c914e:L15-L16` | Carry forward | Authored values |
| 3 | PowerIn electrical input at (0, 1.45, 0.86) | `parts/BeamShutterPart.cs@a6c914e:L23-L24` | Carry forward | Typed port |
| 4 | Frame, blade and witness-stripe geometry as in section 2 | `parts/BeamShutterPart.cs@a6c914e:L25-L39` | Carry forward | Committed functional geometry and art |
| 5 | Indicator colours; `Powered` event while powered | `parts/BeamShutterPart.cs@a6c914e:L44-L50` | Carry forward colours; event as presentation only | Per-element observe loop not carried forward |
| 6 | Shared blade controller constants, slider, per-tick spring, damping and drive law, and classification | `engine/SlidingBlade.cs@a6c914e:L7-L53` | Carry forward the law and magnitudes; not the loop or the 1e-5 thresholds | See CAT-051 facts 6–9 |
| 7 | Default material restitution 1 for a non-dynamic part, bounce threshold 0.1, friction 0.3 | `reference/cpu/MachinePart.cs@a6c914e:L236` | Do not carry forward without decision | Implicit inheritance, not an authored choice (Open question 4) |
| 8 | Light trace: a ray tested against every body's light collider, skipping the emitter (and receiver) | `engine/WorldGeometry.cs@a6c914e:L173-L190` | Carry forward the behaviour | CPU query path not carried forward |
| 9 | Acceptance, unrotated and with the whole assembly rotated (0.3, 0.7, −0.2) rad: closed, the laser reaches no receiver and hits the blade face at local x −0.035. Powered, opening rises ≤ 2.8 m/s × tick per tick. The rendered blade pose equals the body pose within 1e-6. Below opening 0.59 m nothing is received; between 0.61 and 1.2 m the receiver is lit (partial clearance). It ends Open and lit. After supply loss it is Closed and unlit within 180 ticks. Reset gives Closed, 0 and 0 with the exact construction | `CuriousContraptions.tests/BeamShutterTests.cs@a6c914e:L39-L104` | Carry forward | Optical follows the actual blade pose |
| 10 | Acceptance (zero gravity, unrotated and rotated (20°, 30°, 40°)): closing onto a ball moving at 0.5 m/s at local x −0.25 contacts it only when supply is removed, and the shutter still reaches Closed. With no lower tube the ball is pushed aside. Without removal, opening > ball radius + 0.6. Reset replays bit-identically | `CuriousContraptions.tests/BeamShutterTests.cs@a6c914e:L106-L160` | Carry forward | Moving-cargo control |
| 11 | Acceptance: the blade is physical. A ball at 4 m/s from x −14 passes to x 1.99–2.01 when powered and stops at x < −0.35 when unpowered | `CuriousContraptions.tests/BeamShutterTests.cs@a6c914e:L162-L181` | Carry forward | Not just an optical flag |
| 12 | Unpowered return energy never increases; no drive use; exact replay (shared with the gate) | `CuriousContraptions.tests/SlidingBladeDynamicsTests.cs@a6c914e:L24-L83` | Carry forward energy and replay; not the CPU recurrence | CPU integrator |
| 13 | All trace media query the solved blade; presentation never rewrites construction (shared) | `CuriousContraptions.tests/SlidingBladeOwnershipTests.cs@a6c914e:L25-L92` | Carry forward | Committed geometry |
| 14 | Powered travel > 1.2 m in 180 ticks with a clear sweep and light trace. Unpowered, it holds closed and obstructs. Drive work ≤ 120 × h and impulse ≤ 60 × h per substep. After supply loss it returns to 0 within 240 ticks. Orientation comes from owned state (shared) | `CuriousContraptions.tests/ActuatorBodyGeometryTests.cs@a6c914e:L32-L217` | Carry forward | Finite work; fail-closed |
| 15 | The blade is dynamic, 1 kg, velocity along part +Y, zero angular velocity; Reset restores dynamics (shared) | `CuriousContraptions.tests/SceneBodyDynamicsTests.cs@a6c914e:L286-L321` | Carry forward | Body declaration |
| 16 | An externally moved, unpowered blade reads Closing | `CuriousContraptions.tests/AxialMotionOwnershipTests.cs@a6c914e:L88` | Carry forward | Classification |

### Files harvested
- `parts/BeamShutterPart.cs`
- `parts/catalog/beam_shutter.tres`
- `parts/scenes/beam_shutter.tscn`
- `engine/SlidingBlade.cs`
- `engine/WorldGeometry.cs` (light trace only)
- `engine/OpticalNetwork.cs` (laser trace call only)
- `reference/cpu/MachinePart.cs` (BoxProxy opacity, AddBox semantics, default material)
- `ui/WorkshopIcons.cs` (pictogram; not deleted by Epic 7)
- `CuriousContraptions.tests/BeamShutterTests.cs`
- `CuriousContraptions.tests/SlidingBladeDynamicsTests.cs`
- `CuriousContraptions.tests/SlidingBladeOwnershipTests.cs`
- `CuriousContraptions.tests/ActuatorBodyGeometryTests.cs`
- `CuriousContraptions.tests/SceneBodyDynamicsTests.cs` (blade section)
- `CuriousContraptions.tests/AxialMotionOwnershipTests.cs` (shutter line)
- `CuriousContraptions.tests/BodyLocalBoxTests.cs` (checked, no element-specific knowledge)
- `content/puzzles.json` (checked, no element knowledge)
- `tools/P0-007-actual-path-probe/inputs.json` (checked, no element knowledge: file hashes only)
- `reference/P0-022-before/docs/coverage/engine/capabilities-02.json` (checked, no element knowledge: catalogue id lists only)

## 5. Acceptance outline

Requirement: [CAT-007](../requirements.md#current-cat-007), [todo-292](../requirements.md#todo-292) and Story 13.3. The Chrome recipe (place a Laser, a Light receiver, the shutter between them and a Battery; wire Supply to `PowerIn` through the wiring UI; verify from the committed construction read) is queued for the post-catalogue browser phase.

**Behaviour and numbers.**

- **Blade law.** m·ẍ = F_drive − 14·(x + 0.85) − 4·ẋ − m·g·(n·ŷ) + contact, with x in [0, 1.2] m along the blade's local +Y axis n. While supplied the drive pushes toward 1.2 m within 2.8 m/s, 14 m/s², 60 N and 120 W; unsupplied it applies nothing and the spring closes the blade.
- **Closing force.** Upright: 11.9 + 9.81 = 21.7 N at closed, up to 38.5 N at open. A Basketball, Bowling ball or Tennis ball resting on a support carries that, so the blade stops on it.
- **Rope.** A 6 kg Weight over a Pulley pulls 58.9 N and lifts the blade fully (38.5 N needed at open). A 1 kg Weight pulls 9.8 N, under half of the 21.7 N that holds the blade closed, and cannot start it. That pair is the rope positive and control.
- **Beam.** The blade's lower edge is at −0.6 + x. A beam at height y in the opening is cut while x < y + 0.6 and passes once x exceeds it: 0.6 m for a centre beam, 0.9 m for a beam 0.3 m above centre, 1.2 m for the top of the opening. A collimated beam is a zero-width ray ([EL-176](EL-176-red-laser-emitter.md) section 10, item 3).
- **Tick order (D7).** As [EL-176](EL-176-red-laser-emitter.md) section 10, items 4 and 9 state: the optical solve runs after the last substep against this tick's final collider poses, readings act from the next tick, and a receiver reaches supply only through observation → signal node → signal-state contact. Light at a receiver therefore follows the committed blade pose, never the supply state.
- **Mounting (owner decision, 22 November 2026).** The declared rest coordinate makes the unpowered blade close in every orientation. Inverted, gravity pulls the blade open with 9.81 N and the spring closes it with at least 11.9 N, a margin of 2.1 N (21%) at closed that grows to 18.9 N at open. On its side, gravity has no component along travel and the spring alone closes it with 11.9 to 28.7 N. Without the rest coordinate an inverted blade would hang 9.81 ÷ 14 = 0.70 m open, as the delivered Powered gate does today (`DeclaredPrismaticDriveTests.cs:335`–`:351`).

**Acceptance rows.** Planned classes `BeamShutterTests`, `BeamShutterOpticalTests` (S-10, S-11) and `BeamShutterArtTests` (S-14). Each runs the production simulation through the Workshop harness. Stall guards are game seconds at the selected cadence.

| Row | Given / When / Then | Planned test | Control | Boundary | Tolerance and stall guard |
| --- | --- | --- | --- | --- | --- |
| S-1 opens when supplied | Given a Battery wired to `PowerIn`, When Run, Then the blade reaches the open end and holds | `TheSuppliedBladeOpensFullyAndHolds` | unwired: the blade stays closed | none | open means x within 2 cm of 1.2 m; 4 s stall guard |
| S-2 speed and ramp limited | Given S-1, When opening, Then blade speed is at most 2.8 m/s + 1e-3 on every tick, and the ramp from rest to cruise takes at least 0.2 s less one tick | `TheBladeOpensWithinItsDeclaredSpeedAndRamp` | none | none | the bounds are the declared drive values (2.8 ÷ 14 = 0.2 s); no trajectory is pinned |
| S-3 loss closes | Given an open blade, When the Battery's `RunControlLane.SourceEnable` control is switched off, Then the blade returns to closed | `LosingSupplyClosesTheBlade` | supply kept: stays open | switched off at half travel: it reverses from there | closed means x under 2 cm; 6 s stall guard (the spring is soft) |
| S-4 blocked closing | Given a Basketball resting on a Wall whose top is level with the foot top, under an open blade, When supply is lost, Then the blade stops on the ball, the ball stays put and the committed state is blocked | `AClosingBladeStopsOnASupportedBall` | no ball: closes fully | a Tennis ball in place of the Basketball | ball centre moves under 2 cm; blade lower edge within 2 cm of the ball's top; judged over 3 s after a 6 s guard |
| S-5 clearing and retry | Given S-4, When a second ball pushes the first clear, Then the blade closes fully without a new command | `ClearingTheBallLetsTheBladeClose` | S-4 without the push | none | as S-3 |
| S-6 ball stopped by the closed blade | Given a Bowling ball rolling at 3 m/s along a Wall whose top is level with the foot top (y −0.6), When it reaches the closed blade, Then it does not pass the blade plane | `ARollingBallIsStoppedByTheClosedBlade` | open blade: it passes | 8 m/s (a Bumper's launch speed) | ball centre never beyond the blade plane; 4 s |
| S-7 every mounting closes | Given the shutter upright, rolled 90° onto its side and inverted (180°), When supplied and then unsupplied, Then in each it opens fully and, unsupplied, returns to closed and stays closed | `TheBladeOpensAndClosesInEveryMounting` (theory over the three orientations) | the same three with the rest coordinate set to 0 in a fixture declaration: the inverted blade hangs open (proves the rest coordinate is what closes it) | inverted is the boundary: 2.1 N net closing force at closed | open and closed as S-1 and S-3; 8 s stall guard for the inverted close (2.1 N against 4 N·s/m damping is slow) |
| S-8 rope | Given a 6 kg Weight tied through a Pulley (placed at x ≥ 0.3 in front of the frame and above the rail tops) to `Tie`, no supply, When Run, Then the blade lifts to the open end | `AHeavyWeightLiftsTheBladeByRopeAndALightOneDoesNot` | a 1 kg Weight: the blade stays closed | rope cut while open: the blade closes | as S-1 and S-3. Ropes are solid (owner decision, 22 November 2026): once `ENGINE-CONNECTION-MATERIALS` gives the strand a collider this row is re-run, and the strand must not touch the rails or foot |
| S-9 indicator | Given idle, supplied and blocked blades, When read, Then the indicator shows slate, gold and ochre | `TheIndicatorShowsIdlePoweredAndBlocked` | none | none | exact declared colour values |
| S-10 light follows the pose | Given Laser → shutter → Light receiver, When the blade is closed, opening and open, Then the receiver reports light only after the blade's lower edge has cleared the beam, whatever the supply state | `BeamShutterOpticalTests.TheReceiverLightsOnlyWhenTheBladeHasClearedTheBeam` | supplied but held closed by a rope: no light | beams at the centre, 0.3 m above it and 0.55 m above it: each lights later than the one below; a closed blade blocks all three | causal order: light never precedes clearance. Optical tolerances are the one set of coordinator decision 21 (±2 % power, 1 s guard, 0.5° direction, ±0.02 m end points); 4 s stall guard for the opening |
| S-11 ball in the beam | Given an open shutter and a Basketball resting in the Laser path beyond it, Then the receiver is dark; removing the ball lights it | `BeamShutterOpticalTests.ABallInTheBeamBlocksItWithTheBladeOpen` | no ball | none | the optical set of coordinator decision 21: the lit reading within ±2 % of the unobstructed reading after the 1 s guard |
| S-12 Reset, save and load | Given a run stopped mid-travel, When Reset, Then the blade is at 0 with zero velocity; a saved construction reloads with pose, the supply link and the rope link | `ResetClosesTheBladeAndSaveLoadKeepsItsLinks` | generic row `CatalogueIntegrationTests.cs:273` | Reset while blocked | authored state exact |
| S-13 lever to shutter | Given an Impact lever whose `TieB` is roped to `Tie`, When a Bowling ball drops on the other arm, Then the rope goes taut and the blade lifts; with a slack rope it does not | `ImpactLeverTests.ALeverLiftsTheShutterBladeByRope` (row L-R7 of [CAT-034](CAT-034-impact_lever.md)) | slack rope | none | blade rise over 10 cm against under 1 cm; 4 s |
| S-14 art follows the pose | Given the blade closed, at mid-travel and open, then Reset, Then the blade art's pose equals the committed blade body pose | `BeamShutterArtTests.TheBladeArtFollowsTheCommittedBladePoseAndResets` | none | the three positions and Reset | 1 mm |

Covered by shared rows, not repeated: a non-rope link to `Tie` and a rope link to `PowerIn` are refused by the shared row `RopeTests.ARopePortRefusesANonRopeLinkAndANonRopePortRefusesARopeLink` that the batch plans (the existing `BundlePortsTests.cs:262` has no Rope case). There is no parameter to refuse. Every requirement criterion maps to a row: acceleration-limited (S-2), loss closes (S-3), collider and visible pose agree (S-14 and the visible-solid table row the slice adds), partial travel (S-3, S-10), blocked closing (S-4), clearing and retry (S-5), power interruption (S-3), rotated mounting (S-7), ball obstructing the Laser path (S-11), optical blocking follows the pose (S-10).

## 6. Decisions and owner items

Decided (22 November 2026, under the owner's standing delegation unless a source is named):

1. **Order.** The Laser (13.1) and Light receiver (13.2) precede this part (owner reorder, 9 October 2026).
2. **Rope port.** One `Tie` port on the blade body, attachment `Load`, at the blade's lower front edge. A requirement row uses it (D19). Rope and supply act on the same coordinate and need no extra rule.
3. **Thresholds.** No classification tolerance is carried from the legacy. The indicator reads the engine's committed actuator state; tests use the 2 cm position bounds above.
4. **Blade restitution.** 0.1, the Powered gate's value, declared explicitly.
5. **Return spring.** 14 N/m with 4 N·s/m is the fail-closed law, as in the Powered gate (`PoweredGateElementDeclaration.cs:74`–`:79`), with a declared rest coordinate 0.85 m below closed (owner decision, 22 November 2026: the blade closes in every mounting orientation, no orientation limit).
6. **Geometry.** Opening equal to the blade, stroke 1.2 m, a back plate behind the raised blade, tall rails, socket and indicator on the foot (section 2).
7. **Ropes are solid (owner decision, 22 November 2026).** The rope to `Tie` gets a collider through `ENGINE-CONNECTION-MATERIALS`. The frame is laid out so that a strand rising from the blade's front face meets nothing.

No item needs the owner.

## 7. Integration

- **Delivered collaborators:** Battery, Impact switch, Hold timer, State latch, Pressure plate and Counter (Electrical Output, role Supply → `PowerIn`, Electrical Input); Weight, Pulley, Rope anchor and Impact lever (Rope, Bidirectional, both ends); the balls; Wall.
- **Undelivered collaborators, both before this part:** CAT-036 Laser and CAT-038 Light receiver, after `ENGINE-OPTICAL`. Later users: optical gates and filters downstream of a shutter; RAD-06.
- **Campaign.** No level in `content/puzzles.json` places a Beam shutter. S8 is not needed.
- **Coverage binding.** Catalogue key `beam_shutter` (CAT-007), a planned identity in `docs/coverage/catalogue-elements.json`.

## 8. Delivery

- **Slice shape.** One single-element slice, Story 13.3.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a, 22 November 2026), which precedes every further element; in particular `ENGINE-ISLANDS-SLEEP` (drive-budget trials confined to the blade's island; the resting blade) and `ENGINE-SUPPLY-DEMAND` (the drive's work debits its source; the electrical node's values are consumed or deleted), and, before their first consumer, `ENGINE-JOINT-KINDS` (spring rest position); then `ENGINE-OPTICAL`, which also adds the Optics palette category; CAT-036 Laser; CAT-038 Light receiver; then this slice. `ENGINE-CONNECTION-MATERIALS` (solid strand) is needed before the rope rows (S-8, S-13) can be accepted. S3 and S4 only reduce hand edits and tests.
- **Files.** Adds `elements/BeamShutter/BeamShutter.csproj`, `BeamShutterElementDeclaration.cs`, `beam_shutter.tscn`, `icon.svg`; `CuriousContraptions.tests/BeamShutterTests.cs`, `BeamShutterOpticalTests.cs` and `BeamShutterArtTests.cs` (no `Compile` line: published main compiles every top-level test file by one rule); one visible-solid table row; row L-R7 in `ImpactLeverTests.cs`. Registry, bundle, palette, capacity, coverage membership and impact mapping are derived (`spec-registration-derivation.md`). Until S3 lands it also edits the per-package lists that contract names (`spec-s3-remaining-per-package-lists.md`): the trim-analysis project list in `CSharpStaticGateTests.cs`, the placement switches and the slider rows in `PackageRuntimeParityTests.cs` (`:383`–`:400`), the identity list in `BasketballResourceTests.cs:34`, the palette listing in `WorkshopIntegrationHarnessTests.cs:691`–`:699`, and `CuriousContraptions.slnx`.
- **Review tier.** Engine: one capability is present-untested, so the slice adds its test.
- **What would disqualify the plan (the probe must re-check).** `ENGINE-OPTICAL` occludes only static colliders, or samples a commanded state instead of the committed pose of a dynamic body; the spring rest coordinate is not admitted for a prismatic coordinate, or its documented range excludes −0.85 m; a package rope port on a slider body does not compile or couple; a solid rope strand cannot leave the blade face without touching the blade; the blade passes through a supported ball under the 38.5 N closing force.

## 9. Engine conformance (owner instruction, 22 November 2026)

**Verdict: Conforms with fixes.** The plan is declaration data only and names no element-specific engine behaviour. The fixes are engine-side prerequisites or inherited gaps, not package logic: the three missing rows of section 3 (two owned by `ENGINE-OPTICAL`, one by `ENGINE-JOINT-KINDS`), the solid rope strand owned by `ENGINE-CONNECTION-MATERIALS`, and the bounded-drive cost under point 6 (`ENGINE-ISLANDS-SLEEP`), all behind the engine programme (owner decision 3a).

1. **Declaration data only.** Two bodies, hidden collider proxies, one sprung slider with a supplied drive, two ports, one electrical node, a one-consumer supply graph and presentation bindings. No controller, script, event handler or state machine. The legacy blade controller, its classification and its `Powered` event in section 4 are not carried.
2. **Generic laws.** Opening: the supplied prismatic drive law with its speed, acceleration, effort and power bounds. Closing in any orientation: the slider's spring with its declared rest coordinate, its damper and gravity. Blocked closing and retry: contact between the blade and a supported body; the blade resumes when the contact goes. Rope opening: the tensile span law at a body-owned port. Blocking a ball: contact. Blocking light: beam transport occluded by solid colliders at their committed pose, the general capability `ENGINE-OPTICAL` owns; the shutter declares no optical behaviour. No capability exists only for this part.
3. **Three layers.** Colliders: four boxes and two spheres on the frame and one box on the blade, all hidden `col_` proxies from the engine's admitted shapes; physics reads no art. Visuals: back plate, foot, rails, blade plate, printed stripes and tie mark, socket, indicator; each proud static solid has its own proxy. Animation: indicator albedo from `CommittedActuatorState`, evaluated off the physics thread. The blade art is moved by its body's pose; the stripes and tie mark ride that pose; the frame art is moved by nothing.
4. **One-way flow and typing.** Physics → committed pose → the optical solve after the last substep → receiver reading → signal node → next tick's supply ([EL-176](EL-176-red-laser-emitter.md) section 10, items 4 and 9); physics → presentation. Nothing flows back. Typed `PortId`, `SimulationDomain`, `RopeAttachmentRole`, `CoordinateKind`, `ElementCategory`; f32 values.
5. **Flat-state counts (from the plan).** 2 bodies (1 dynamic); 7 colliders (5 boxes, 2 spheres); 1 prismatic joint; 1 drive; 0 sensors, stores and events; 1 electrical node; 1 supply consumer; 2 ports; 0 parameters; 3 presentation bindings and 1 body-pose binding. All fixed at compile time.
6. **Performance fitness.** Seven primitive colliders against the Powered gate's nine; the two spheres exist because the socket and indicator are drawn proud. The frame is a static body. The blade moves at up to 2.8 m/s and is 7 cm thick; a ball arriving at 8 m/s covers 1.7 cm per 480 Hz substep and the engine's speculative contacts cover the approach, with no per-element check. Stated truthfully: the drive's power bound is enforced today by a whole-world snapshot and up to eleven re-solves per substep (`WorkshopPhysicsEngine.Motors.cs:35`–`:66`), which is work proportional to the scene; engine gap owned by `ENGINE-ISLANDS-SLEEP` (owner decision 3a, 22 November 2026), which confines the trials to the drive's island. Island sleeping is not implemented; a resting body is still stepped, at the cost of its broadphase proxies and rows every substep (no sleep or deactivation code in `CuriousContraptions.Simulation`). Owned by `ENGINE-ISLANDS-SLEEP`.
7. **Scenario tests.** Rows S-1 to S-14 are scenarios through the production simulation; S-10 and S-11 prove occlusion with a real emitter and receiver, not a shutter-specific path.
