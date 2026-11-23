# CAT-039 · linear_pusher declaration readiness spec

**Current delivery:** [CAT-039 delivery owner](../../../_bmad-output/planning-artifacts/epics.md#catalogue-slice-ownership). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes.

Current contract for a delivered part, repaired on 22 November 2026 after the independent audit (`docs/planning/elements/readiness/audits/elements-batch-c.md`). Checked against `elements/LinearPusher/` on checkout `spec-drafts` (`8d1d68aa`); published main is `25df3cd5`, and its two later commits touch neither this package nor its tests. Sections 1–3 and 5–9 are current. Section 4 is the legacy harvest at `a6c914e`, kept verbatim as evidence; where it differs from the delivered declaration, the declaration governs. Values are f32 game values under [the numeric contract](../../gpu-f32-physics.md#numeric-representation-and-precision).

## 1. Identity

| Field | Value |
| --- | --- |
| CAT ID | CAT-039 |
| Kind | `linear_pusher` (catalogue title "Linear pusher", category Power) |
| Requirement anchor | [CAT-039](../requirements.md#current-cat-039); retained behaviour [sequence-task-309](../requirements.md#sequence-task-309); consumers [CAT-039-I](../invest/current-consumers.md#cat-039-i) |
| Mapped identities | None found in [named-elements](../invest/named-elements.md). EL-053 rotary-to-linear converter is a separate shaft-driven element, not this electric servo. |
| Roadmap story | 11.3 Linear Pusher Telescopic Actuator ([epics](../../../_bmad-output/planning-artifacts/epics.md), Epic 11) Current [CAT-039 delivery owner](../../../_bmad-output/planning-artifacts/epics.md#catalogue-slice-ownership). |
| Status | **Delivered.** Package id `linear_pusher`, directory `elements/LinearPusher`. Slice record [11.3](../../../_bmad-output/implementation-artifacts/spec-11-3-linear-pusher.md) (`done`, commit `f476e279`). Browser case pending. Every residual slice (section 8) is blocked on the engine programme (owner decision 3a, 22 November 2026); the stories that own this part's gaps are `ENGINE-ISLANDS-SLEEP` (power-bound trials and the resting head), `ENGINE-SUPPLY-DEMAND` (drive drain) and `ENGINE-CAPABILITY-INSTANCES` (the hard-coded socket glyphs). |
| Disposition | Delivered catalogue part; no mapped named identity. EL-217 Reload shuttle, EL-220 Docking lift, EL-221 Parallel gripper and EL-237 Boxing glove plan to reuse its supplied-slider shape as separate elements (delivery-batches 1b, "Supplied sliders"). |

## 2. Declaration

The current declaration is `elements/LinearPusher/LinearPusherElementDeclaration.cs`; the scene is `elements/LinearPusher/linear_pusher.tscn`.

| Item | Delivered value | Source |
| --- | --- | --- |
| Interfaces | `ISliderElement`, `ISupplyElement`, `ISceneGeometryElement`, `IClippedExtrusionElement` | `:8` |
| Bodies | 2 (1 moving): static frame (root) and the head `Visual/Head`; one prismatic constraint | `:18`, `:27` |
| Frame colliders | foot and barrel boxes (drawn by their own proxies `col_Foot`, `col_Barrel`; see Art) | `linear_pusher.tscn:50`–`:53` |
| Head | sphere collider r 0.25 m (`col_Head`); mass 0.5 kg, moments (0.0125, 0.0125, 0.0125) kg·m² (a solid sphere, 0.4·m·r²), gravity (0, −9.81, 0) | `:33`; `linear_pusher.tscn:65` |
| Rod | a clipped extrusion on the prismatic coordinate, half-thickness 0.06 m, material of the head body, clipped by the frame: the exposed length is a collider | `:40`–`:41`; `linear_pusher.tscn:67` |
| Materials | frame and head: restitution 0, friction 0.3, rolling resistance 0, threshold 0.1 m/s | `:27` |
| Slider | frame anchor (0.85, 0, 0), axis local +X, travel [0, `stroke`], no spring, no damping, connected bodies do not collide | `:30`–`:32` |
| Drive | supplied; target `stroke`, speed `speed`, acceleration `acceleration`, force `force`, power `force` × `speed`; `PrismaticTargetSelection.Directional` from supply nodes `positive` and `negative`; inactive response `SelfLock` | `:34`–`:38` |
| Parameters | `stroke` m, default 2.4, 0.25–3; `speed` m/s, 1.5, 0.25–4; `acceleration` m/s², 12, 1–30; `force` N, 40, 1–100. All Real, editor policy Reject, Build only (descriptor defaults) | `:14`–`:17` |
| Ports (all Electrical) | `PowerIn` Input, role PowerIn, (−0.45, −0.4, 0.5); `ExtendIn` Input, role FirstIn, (0.15, −0.4, 0.5); `RetractIn` Input, role SecondIn, (0.55, −0.4, 0.5); `RetractedOut` Output, role Supply, (−0.45, 0.4, 0.45); `ExtendedOut` Output, role Supply, (0.45, 0.4, 0.45); normals +Z | `:42`–`:49` |
| Electrical nodes | `motor`: Consumer, 12 V, 24 Ω, capacitance 0, maximum 400 W. `distribution`: LogicGate, 12 V, 0.05 Ω, capacitance 0, maximum 400 W. Field order is (NodeId, Role, RatedVoltage, Resistance, Capacitance, MaxPower) (`CuriousContraptions.Abstractions/ElectricalContractValues.cs:15`–`:21`). Each is validated as finite and read by no law (only `Role` is read): unread dead data, to be consumed by the `ENGINE-SUPPLY-DEMAND` story or removed forward-only | `:50`–`:51` |
| Supply graph | `PowerIn` → `power` node → the motor consumer and the two endpoint contacts (lower → `RetractedOut`, upper → `ExtendedOut`); `ExtendIn` → `positive`, `RetractIn` → `negative` | `:52`–`:65` |
| Stores, events | none | `:26` |
| Presentation | head follows package body 1; span `Visual/Rod` from the barrel face (0.6, 0, 0) to the head; `Visual/Indicator` albedo by `CommittedPrismaticPhase` (held, moving, blocked colours); pick radius 1.3 m | `:66`–`:76` |
| Palette | category Power, order 40, colour (0.4, 0.72, 0.79) | `:11`–`:13` |

### Art and solidity (delivered scene)

| Node | Mesh and pose | Layer and class |
| --- | --- | --- |
| `Visual/col_Foot` | navy box 1.6 × 0.2 × 1 m at (0, −0.5, 0) | **defect:** a collider proxy that is also drawn. Part B (row B8) separates it into an art mesh and a hidden proxy |
| `Visual/col_Barrel` | cream box 1.2 × 0.65 × 0.65 m at the origin | the same defect and repair |
| `Visual/Head/Sphere` with `col_Head` | cream sphere r 0.25 m; a separate `CollisionShape3D` | visual and collider already separate; moved by the head body's pose |
| `Visual/Rod` with `col_Rod` | gold box, 0.12 m square, drawn as a span from the barrel face (0.6, 0, 0) to the head; the collider is the head body's clipped extrusion | visual moved by the span; solid through the clipped collider (row B6 checks they coincide) |
| `Visual/Head/Ring` | cyan torus, inner radius 0.235 m, outer 0.255 m (centreline 0.245 m, tube 0.01 m), axis along travel | **defect:** proud by 5 mm, no collider. Part B (row B5) appends a torus on the head body |
| `Visual/Indicator` | sphere r 0.065 m at (−0.4, 0.2, 0.34) | **defect:** drawn solid, no collider (row B4). Colour moved by a presentation binding |
| `Visual/Socket_PowerIn`, `Socket_ExtendIn`, `Socket_RetractIn`, `Socket_RetractedOut`, `Socket_ExtendedOut` | navy spheres r 0.07 m at the five port positions | **defect:** drawn solid, no collider (rows B3, B4). Sockets keep their colliders (owner decision, 21 November 2026) |
| `Visual/TravelMark0`–`4` | gold boxes 0.022 × 0.1 × 0.012 m at x −0.2, −0.05, 0.1, 0.25, 0.4 and z 0.333, on the barrel's front face | **defect:** raised, standing up to 14 mm off the face. Part B (row B9) redraws them as flat printing 0 to 0.5 mm in front of the face |

Floating art, recorded and not repaired by part B: the `RetractedOut` and `ExtendedOut` sockets sit 7.6 cm clear of the barrel with nothing drawn between them and it. Part B is [visible-solid remainder part B](../../../_bmad-output/implementation-artifacts/spec-elements-visible-solid-remainder-b.md) (status draft, follows part A); the classification source is [`spec-engine-art-solidity.md`](../../../_bmad-output/implementation-artifacts/spec-engine-art-solidity.md), row `linear_pusher`. Design authority: [DESIGN.md "Electric linear pusher"](../../../DESIGN.md#electric-linear-pusher). No level in `content/puzzles.json` places or stocks the part.

## 3. Engine capabilities

Capability families: see the [CAT-039 map row](../general-engine-element-map.md) and [catalogue coverage](../../coverage/catalogue-elements.json). They are not duplicated here.

**Capability probe (22 November 2026).** Tests were found by name and line; none was run. Counts: 7 present, 0 present-untested, 0 narrower, 0 missing. Every row is a generic capability driven by this package's declaration; the engine holds no pusher code.

| Capability | Status | Symbol | Exercising test |
| --- | --- | --- | --- |
| Prismatic slider with parameter-bound travel | present | `ISliderElement`, `PrismaticSliderDescriptor` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:15`; `CuriousContraptions.Abstractions/MechanicalContractValues.cs:104`) | `LinearPusherTests.ParameterEndpointsCompileAndProduceUsefulMotion` (`CuriousContraptions.tests/LinearPusherTests.cs:228`) |
| Supplied drive bounded in speed, acceleration, force and power, with two directional commands | present | `PrismaticDriveDescriptor`, `PrismaticTargetSelection.Directional` (`MechanicalContractValues.cs:89`, `:84`) | `LinearPusherTests.ExtensionReportsMotorWorkAndEndpointPowerOnTheNextElectricalTick` (`:52`); generic `DirectionalPrismaticTests.StartupIsNotBlockedAndCommandSlewDoesNotExceedDeclaredAcceleration` (`CuriousContraptions.tests/DirectionalPrismaticTests.cs:53`) |
| Self-locking hold on no command, both commands or lost supply | present | `PrismaticInactiveResponse.SelfLock` (`MechanicalContractValues.cs:85`) | `LinearPusherTests.IndependentElectricalInputsHoldWithoutDeliveringWork` (`:38`), `LosingPowerLocksTheIntermediatePositionAndRetractResumesFromIt` (`:104`) |
| Composed supply graph: logic node, consumers, endpoint contacts on a coordinate | present | `SupplyGraphDescriptor`, `SupplyContactDescriptor`, `PrismaticContactControl` (`CuriousContraptions.Abstractions/SupplyContractValues.cs:30`, `:40`; `SignalContactControl.cs:13`) | `LinearPusherTests.BothEndpointConsumersFollowDepartureReturnAndSupplyRemoval` (`:65`), `OneSourceCanSupplyBothPowerAndCommandWithAnUnwiredCommandControl` (`:249`) |
| Clipped extrusion collider (the rod) | present | `IClippedExtrusionElement`, `ClippedExtrusionDescriptor` (`CuriousContraptions.Abstractions/SceneGeometry.cs:19`, `:16`) | `ClippedExtrusionTests.ExposedMidspanPhysicallyBlocksAnApproachingBodyButAdjacentSpaceIsClear` (`CuriousContraptions.tests/ClippedExtrusionTests.cs:85`); `LinearPusherTests.ObstructionStopsTheHeadAndReportsBlockedWhileAnAdjacentWallDoesNot` (`:125`) |
| Committed prismatic phase driving a state-valued presentation binding; span art | present | `PresentationDriver.CommittedPrismaticPhase` (`CuriousContraptions.Abstractions/ElementPresentation.cs:31`) | `LinearPusherArtTests.NativeHeadSpanAndIndicatorFollowCommittedMotionAndRestoreOnReset` (`CuriousContraptions.tests/LinearPusherArtTests.cs:13`) |
| Save, load, Pause, Step and Reset | present | generic lifecycle | `LinearPusherTests.SaveLoadPauseStepAndResetPreserveCommandsParametersAndAuthoredState` (`:148`), `CargoTransferResetRestoresEveryAuthoredBodyAndSemanticReplay` (`:333`) |

**Engine gaps this part relies on (stated, not designed around).**

- **Power bound by whole-world replay.** While any prismatic drive is active, each substep captures a whole-world snapshot, and when step work exceeds the declared bound the engine restores and re-solves the substep up to eleven times (`CuriousContraptions.Simulation/WorkshopPhysicsEngine.Motors.cs:35`–`:66`). That is work proportional to the scene. Engine gap owned by `ENGINE-ISLANDS-SLEEP` (owner decision 3a, 22 November 2026), whose contract confines drive-budget trials to the island they affect: the cost becomes proportional to the drive's island, not removed.
- **Drive drain.** A supplied drive debits nothing from its source. Engine gap owned by `ENGINE-SUPPLY-DEMAND` (sprint key `engine-supply-drive-demand`).
- **No island sleeping.** Island sleeping is not implemented; a resting body is still stepped, at the cost of its broadphase proxies and rows every substep (no sleep or deactivation code in `CuriousContraptions.Simulation`). Owned by `ENGINE-ISLANDS-SLEEP` (owner decision 3a, 22 November 2026).
- **Socket glyphs.** The four socket glyphs `Extend`, `Retract`, `Extended` and `Retracted` are hard-coded SVG in `ui/WorkshopConnections.cs:41`–`:44` and used by this package alone. Owned by `ENGINE-CAPABILITY-INSTANCES` (socket glyphs as declared lists).

**Element dependencies.** Battery; an Electrical output for each command; Conveyor and Pipe for the lesson chain. All delivered.

## 4. Legacy harvest

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | Ideal self-locking electric servo; supply is binary, not battery energy | `parts/LinearPusherPart.cs@a6c914e:L12` | Carry forward | Matches DESIGN and the requirement |
| 2 | Head is a 0.25 m sphere of 0.5 kg at rest x 0.85 m; material (0, 0.1, 0.3) | `parts/LinearPusherPart.cs@a6c914e:L18-L20`, `parts/LinearPusherPart.cs@a6c914e:L83` | Carry forward | Authored values; f32-representable |
| 3 | Endpoint tolerance 1e-6 m | `parts/LinearPusherPart.cs@a6c914e:L21` | Do not carry forward | CPU double tolerance; freeze an f32 value (Open question 4) |
| 4 | Servo: maximum speed = speed, acceleration, maximum effort = force, maximum power = force × speed | `parts/LinearPusherPart.cs@a6c914e:L28-L32` | Carry forward | Declaration data |
| 5 | Command decoding: no PowerIn → Unpowered; both commands → Conflict; Extend → Extending; Retract → Retracting; none → Holding | `parts/LinearPusherPart.cs@a6c914e:L35-L38` | Carry forward as a typed enum | Closed set stays an enum |
| 6 | Unpowered, Conflict and Holding map to Hold; Extending targets the upper end; Retracting targets the lower end | `parts/LinearPusherPart.cs@a6c914e:L110-L119` | Carry forward the law | The per-element `PreparePhysics` loop is not carried forward |
| 7 | Phase reads Holding at the commanded endpoint, and Blocked when commanded with speed < 1e-6 | `parts/LinearPusherPart.cs@a6c914e:L66-L75` | Carry forward the behaviour | Threshold re-frozen in f32 |
| 8 | Hold locks the joint range to [hold, hold]. Ramp: command speed = sign(d)·min(vmax, √(2a·\|d\|)), slewed by a·Δt | `engine/physics/PhysicsServo.cs@a6c914e:L52`, `engine/physics/PhysicsServo.cs@a6c914e:L62-L77` | Carry forward the behaviour | The joint-replacement mechanism is a CPU solver path |
| 9 | The telescoping shaft collider is rewritten each tick in `ObservePhysics`; "continuous deformation within a substep remains a qualification gap" | `parts/LinearPusherPart.cs@a6c914e:L129-L146` | Do not carry forward | Per-element update loop that mutates collision |
| 10 | Electrical routes PowerIn → RetractedOut or ExtendedOut at the endpoints | `parts/LinearPusherPart.cs@a6c914e:L92-L96` | Carry forward | Typed ports |
| 11 | Construction rejects non-finite or out-of-range parameters | `parts/LinearPusherPart.cs@a6c914e:L97-L109` | Carry forward | Atomic rejection |
| 12 | Acceptance: with no supply, retract-at-home or conflict, extension and delivered work stay 0 for 120 ticks; the phases are Unpowered, Holding, Conflict and Holding | `CuriousContraptions.tests/LinearPusherTests.cs@a6c914e:L104-L121` | Carry forward | Negative controls |
| 13 | Acceptance: with supply, RetractedOut powers its load after one tick. A full extend in 300 ticks sets Extended and moves power to ExtendedOut. Supply loss drops ExtendedOut on the next tick, and retracting restores RetractedOut. Reset restores the saved construction and zero work | `CuriousContraptions.tests/LinearPusherTests.cs@a6c914e:L123-L148` | Carry forward | Endpoint outputs; next-tick visibility |
| 14 | Acceptance: supply loss or conflict at mid-stroke holds the exact extension at zero speed for 120 ticks; resuming extends further | `CuriousContraptions.tests/LinearPusherTests.cs@a6c914e:L150-L167` | Carry forward | Brake and conflict |
| 15 | Acceptance: at yaw 0°, 90° and 180°, a ball at local (1.55, 0, 0) is pushed more than 1 m along the axis. Per-substep impulse ≤ force/480; per-tick work ≤ force × speed/120; ball speed ≤ 1.501 m/s | `CuriousContraptions.tests/LinearPusherTests.cs@a6c914e:L169-L196` | Carry forward | Bounded impulse and work; orientation |
| 16 | Acceptance: a ball pinned against a thin wall stops the head (extension < 0.35 m); neither the ball nor the head penetrates; the pusher is not Extended | `CuriousContraptions.tests/LinearPusherTests.cs@a6c914e:L198-L216` | Carry forward the behaviour | Numeric bounds re-derived under the game-grade envelope |
| 17 | Acceptance: Reset at mid-stroke gives extension 0, speed 0, work 0, Retracted and the exact construction | `CuriousContraptions.tests/LinearPusherTests.cs@a6c914e:L218-L235` | Carry forward | Run/Reset |
| 18 | Acceptance: rejects stroke 0 and 4, speed NaN and 5, acceleration 0 and 31, force 0 and +∞ | `CuriousContraptions.tests/LinearPusherTests.cs@a6c914e:L237-L255` | Carry forward | Boundaries |
| 19 | Acceptance: speed changes by ≤ acceleration/120 per tick, including through a mid-stroke reversal | `CuriousContraptions.tests/LinearPusherTests.cs@a6c914e:L257-L275` | Carry forward | Acceleration bound |
| 20 | Acceptance: a vertical pusher under 9.81 m/s² with force 1 N cannot lift a ball; with force 40 N it lifts the ball more than 0.5 m in 180 ticks | `CuriousContraptions.tests/LinearPusherTests.cs@a6c914e:L277-L297` | Carry forward | Low-force gravity support |
| 21 | Acceptance: 4 kg and 8 kg Weight cargo is pushed past x 2.55 m in 240 ticks; cargo kinetic energy ≤ delivered work + 0.01 J | `CuriousContraptions.tests/LinearPusherTests.cs@a6c914e:L299-L320` | Carry forward | No energy creation |
| 22 | Acceptance: a head that initially overlaps a wall rejects Run before it starts; extension and work stay 0 | `CuriousContraptions.tests/LinearPusherTests.cs@a6c914e:L322-L337` | Carry forward | Atomic admission |
| 23 | Acceptance: Save/Load replay reproduces extension, work and cargo pose exactly after 80 ticks | `CuriousContraptions.tests/LinearPusherTests.cs@a6c914e:L339-L358` | Carry forward | Save/Load determinism |
| 24 | The head is one dynamic sphere of 0.5 kg. The held joint range is locked; the driven range is [0, stroke] under the same joint identity | `CuriousContraptions.tests/LinearPusherTests.cs@a6c914e:L360-L396` | Carry forward the identity and lock, not the replacement mechanism | CPU joint replacement |
| 25 | Parameter wire names are `stroke`, `speed`, `acceleration` and `force`; undefined enum values reject | `CuriousContraptions.tests/LinearPusherTests.cs@a6c914e:L398-L412` | Carry forward | Serialization boundary |
| 26 | Acceptance: a thin wall face at x 1.99 m stops the head at extension 0.889–0.891 m, phase Blocked. Work is > 0 before contact, then at most 1e-6 J more over 120 ticks | `CuriousContraptions.tests/LinearPusherTests.cs@a6c914e:L414-L435` | Carry forward the behaviour | A stalled drive delivers no work |
| 27 | Run captures the servo from the parameters (range, speed, acceleration, effort, power = force × speed). Edits during Run reject. Capture/restore replays servo, bodies and motor totals bit-exactly, at 0° and 37° | `CuriousContraptions.tests/PusherServoOwnershipTests.cs@a6c914e:L11-L64` | Carry forward | Compile at Run; checkpoint |
| 28 | Reported delivered work equals the solver's motor report; repeated observation does not double count; a disabled supply reports 0 | `CuriousContraptions.tests/MotorAccountingOwnershipTests.cs@a6c914e:L28-L72` | Carry forward | Work accounting |
| 29 | One supply may feed both PowerIn and ExtendIn. A failed tick rolls back the phase. A restored zero velocity under an extend command reads Blocked | `CuriousContraptions.tests/RemainingReadingCheckpointTests.cs@a6c914e:L57-L93` | Carry forward; rollback deferred to the injected-fault gate | Wiring admission; rollback |
| 30 | The shaft collider blocks rays and bodies; replacing it keeps material and participation | `CuriousContraptions.tests/RuntimeQueryOwnershipTests.cs@a6c914e:L237-L299` | Carry forward the behaviour | The rod is physical |
| 31 | Under placement assistance the frame moves kinematically and the head follows to its rest position within 1e-4 m | `CuriousContraptions.tests/AssistancePhysicsTests.cs@a6c914e:L17-L62` | Do not carry forward as acceptance | Assistance policy is undecided (Open question 7) |
| 32 | Motion reads come from the committed joint, not from presentation; unpowered speed is zero | `CuriousContraptions.tests/AxialMotionOwnershipTests.cs@a6c914e:L61-L110` | Carry forward | One-way data flow |

### Files harvested
- `parts/LinearPusherPart.cs`
- `parts/catalog/linear_pusher.tres`
- `parts/scenes/linear_pusher.tscn`
- `engine/physics/PhysicsServo.cs`
- `engine/SceneServoDeclaration.cs`
- `engine/SceneElectricalContact.cs`
- `engine/MachineData.cs` (SocketId only)
- `engine/physics/PersistentContactPair.cs` (ContactMaterial field order only)
- `reference/cpu/MachinePart.cs` (AddBox semantics only)
- `ui/WorkshopIcons.cs` (socket pictograms; not deleted by Epic 7)
- `CuriousContraptions.tests/LinearPusherTests.cs`
- `CuriousContraptions.tests/PusherServoOwnershipTests.cs`
- `CuriousContraptions.tests/MotorAccountingOwnershipTests.cs`
- `CuriousContraptions.tests/RemainingReadingCheckpointTests.cs`
- `CuriousContraptions.tests/RuntimeQueryOwnershipTests.cs`
- `CuriousContraptions.tests/AssistancePhysicsTests.cs`
- `CuriousContraptions.tests/AxialMotionOwnershipTests.cs`
- `content/puzzles.json` (checked, no element knowledge)
- `tools/P0-007-actual-path-probe/inputs.json` (checked, no element knowledge: file hashes only)
- `reference/P0-022-before/docs/coverage/engine/capabilities-02.json` (checked, no element knowledge: catalogue id lists only)

## 5. Acceptance outline

Requirement: [CAT-039](../requirements.md#current-cat-039) and [sequence-task-309](../requirements.md#sequence-task-309). The Chrome recipe (place a Battery and the pusher, wire Supply to `PowerIn` and two Electrical sources to `ExtendIn` and `RetractIn`, place a Bowling ball before the head, verify from the committed construction read) is queued for the post-catalogue browser phase.

**Native tests that exist per requirement criterion (22 November 2026; none was run for this review).** In `CuriousContraptions.tests/LinearPusherTests.cs`. Independent supply and commands, conflict and no command hold: `:38`, `:249`. Power loss locks, retract resumes (interruption and reversal): `:104`. Endpoint outputs on the next electrical tick: `:52`, `:65`. Low-force gravity support: `VerticalPackageLiftsCargoOnlyWithSufficientDeclaredForce` (`:322`). Stall and obstruction: `:125`. Miss and rotated mounting: `RotatedSphericalHeadPushesCargoAndLateralClearanceMisses` (`:307`). Parameter range edges: `:228`. Head and rod collider, indicator and span follow motion: `:125`, `LinearPusherArtTests.cs:13`. Rope and cargo: `HeadTransfersLoadThroughCargoToARopeAndDisconnectingTheRopeReleasesTravel` (`:179`). Lifecycle: `:148`, `:333`. Lesson chain, sensor → delay → pusher → conveyor cargo → pipe, with its unconnected control: `LinearPusherIntegrationTests.PhysicalSensorAndDelayPushConveyorCargoIntoPipeOnlyWithTheCommandConnection` (`CuriousContraptions.tests/LinearPusherIntegrationTests.cs:58`). Wrong-kind connection, generic: `BundlePortsTests.AWrongDomainOrOutputToOutputLinkIsStillRefused` (`BundlePortsTests.cs:262`).

**Residual acceptance rows.**

| Row | Given / When / Then | Planned test | Control | Boundary | Tolerance and stall guard |
| --- | --- | --- | --- | --- | --- |
| LP-R1 parameter refusal | Given a saved or edited pusher whose `stroke`, `speed`, `acceleration` or `force` is below its minimum, above its maximum, NaN or infinite, When it is admitted, Then it is refused with a typed reason and the construction is unchanged | `LinearPusherTests.AnOutOfRangeOrNonFiniteParameterIsRefusedAndChangesNothing` (theory over the four keys); superseded by row R2 of the conformance suite (S4) if that lands first. The refusal code is one generic seam (`engine/gpu/InstanceParameters.cs:37`–`:49`) | the range edges of `:228` are admitted | one f32 step outside each bound | exact refusal; no float tolerance |
| LP-R2 travel marks print flush; sockets, indicator and ring solid; foot and barrel proxies separated | rows B3–B6, B8 and B9 of visible-solid remainder part B | owned by that contract | — | — | — |

Test to correct at its next edit: `LinearPusherTests.cs:52` uses a 1e-6 J allowance, which is not a gameplay bound under the [float rules](../../testing-guidance.md#float-comparison-and-effort-rules).

## 6. Decisions

Every question the harvest raised is decided; none needs the owner.

1. **Hold.** Ideal self-locking, not bounded by `force`: the slice record states "power loss engages the ideal self-locking brake" and the declaration uses `PrismaticInactiveResponse.SelfLock`.
2. **Stroke and command source.** Stroke is the `stroke` parameter in the generic editor (default 2.4 m). `ExtendIn` and `RetractIn` are Electrical inputs, so a command comes from an Electrical output. A Signal-domain sensor reaches them through a supplied part: the delivered lesson chain is Impact switch → Delay box → Hold timer, whose Supply output feeds `ExtendIn` (`LinearPusherIntegrationTests.cs:58`). One source may feed both power and a command (`LinearPusherTests.cs:249`).
3. **Rod.** A clipped extrusion owned by the head body and clipped by the frame (`IClippedExtrusionElement`).
4. **Tolerances.** No legacy 1e-6 figures are carried; tests take their bounds from the quantity under the float rules.
5. **Electrical command sources.** Delivered parts with an Electrical output (role Supply): Battery, Impact switch, Hold timer, Pressure plate, Counter, State latch, and this part's own endpoint outputs. The Ball detector and Delay box are Signal-domain only.
6. **Head rolling resistance.** Zero (slice record: the guide constrains rolling).
7. **Placement assistance.** None of its own: generic authored poses in Build; the frame is static during Run.
8. **Travel marks (decided 22 November 2026 under the standing delegation).** The five marks are a fixed printed scale on the barrel. The requirement's "travel marks … follow actual motion/work" (`docs/planning/requirements.md:692`) is met by the head and rod moving against that scale, asserted in `LinearPusherArtTests.cs:13`. Reason: a scale that moved with the head would measure nothing.

## 7. Integration

- **Collaborators, all delivered:** Battery (Supply → `PowerIn`; Electrical Output to Input at both ends), Impact switch, Hold timer, Pressure plate, Counter, State latch (Electrical outputs for the commands), Delay box and Ball detector (through a supplied part), Conveyor, Pipe, Wall, balls, Weight, Rope anchor. Both endpoint outputs have role Supply and feed any `PowerIn`.
- **Ropes are solid (owner decision, 22 November 2026).** Ropes get colliders through the engine story `ENGINE-CONNECTION-MATERIALS`; the delivered rope is drawn without one, a visible-solid defect owned by that story. The pusher's rope test (`LinearPusherTests.cs:179`: the head pushes cargo that is tied to a rope) is re-run when strands collide: a solid strand may touch the barrel or foot, a point for that story's probe.
- **Numbers against collaborators.** The default 40 N pushes a 4 kg Bowling ball along the bench (friction 0.3 × 4 × 9.81 ≈ 11.8 N) and lifts about 3.5 kg of cargo plus its own 0.5 kg head vertically, so it cannot lift a Bowling ball at the default and can at 100 N. Stroke 2.4 m spans a pipe bore, 1.3 m across.
- **Campaign.** No level in `content/puzzles.json` places a Linear pusher. S8 is not needed.
- **Coverage binding.** Catalogue key `linear_pusher` (CAT-039).

## 8. Delivery and residuals

- **Delivered** by 11.3.
- **Blocked on the engine programme (owner decision 3a, 22 November 2026).** Every residual slice below waits for it. The stories that own this part's gaps are `ENGINE-ISLANDS-SLEEP`, `ENGINE-SUPPLY-DEMAND` and `ENGINE-CAPABILITY-INSTANCES` (section 3).
- **Residual slices.** (1) Visible-solid remainder part B (engine tier, as that contract states; it changes the shared matcher). (2) LP-R1: one test, data-only tier, or absorbed by S4.
- **Supply drain.** A supplied drive debits nothing today, for this part as for the Motor and the Powered gate. Owned by the engine story `ENGINE-SUPPLY-DEMAND`, which also decides whether the two electrical nodes' unread values become the standing demand or are deleted. The pusher needs no declaration change beyond that.
- **Probe re-check for part B:** collider order on both bodies is unchanged (head sphere, then rod, then the appended ring), and the declared footprint stays 2 bodies, 1 moving, 1 prismatic.

## 9. Engine conformance (owner instruction, 22 November 2026)

**Verdict: Conforms with fixes.** The package is declaration data only. Fixes: the layer-separation defect in the scene (point 3), owned by visible-solid remainder part B; unread electrical node values (point 1); and the engine gaps the part relies on (section 3 and point 6), each owned by an engine story of the programme (owner decision 3a). No engine, bundle, abstraction or `ui/` source names this element; the only hits are coverage and browser-tool tables under `tools/`.

1. **Declaration data only.** Two bodies, scene colliders, one slider with a declared drive, one clipped extrusion, four parameters, five ports, two electrical nodes, a six-node supply graph and presentation bindings. The six phases are the engine's `PrismaticDrivePhase` enum for any directional drive, not a state machine in the package. Fix: the two electrical nodes carry values no law reads (section 2); `ENGINE-SUPPLY-DEMAND` consumes or deletes them.
2. **Generic laws.** Travel: the prismatic constraint with travel bounds. Push: the supplied drive law (speed, acceleration, force and power bounds) and contact on the cargo. Hold: the drive's declared inactive response, `SelfLock`. Conflict and lost supply: the supply network rule, a NOR logic node feeding the motor consumer. Endpoint outputs: supply contacts closed by a coordinate at its lower or upper end, read by the electrical solve of the next tick. Blocked: the drive's committed phase when contact stops travel. No row acts on a named collaborator.
3. **Three layers.** Colliders: frame foot and barrel boxes; head sphere and the clipped rod on the head body. **Defect:** `col_Foot` and `col_Barrel` are visible meshes, so two collider proxies are also the art, against the scene contract that a proxy is a separate node that is not rendered; the head already has separate art and a `CollisionShape3D`. Part B (row B8) separates the two. Visuals: head sphere and ring, rod, indicator, sockets, travel marks. Animation: indicator albedo from `CommittedPrismaticPhase`; the rod is a span stretched between the barrel face and the head pose. The head art is moved by its body's pose; the frame art by nothing. Travel marks become flat printing.
4. **One-way flow and typing.** Physics → committed drive phase → presentation; electrical outputs flow through the supply graph, not back from presentation. Typed `PortId`, `PortRole`, `SupplyTerminal`, `LogicGateOperation`, `CoordinateKind`, `PrismaticDrivePhase`; f32 values.
5. **Flat-state counts (from the scene and declaration).** 2 bodies (1 dynamic); 4 colliders: 2 boxes on the frame, 1 sphere and 1 clipped-extrusion box on the head (11 after part B: plus 6 spheres on the frame and 1 torus on the head); 1 prismatic joint; 1 drive; 0 sensors and stores; 2 electrical nodes; 6 supply nodes and 3 internal edges; 5 ports; 4 parameters; 3 presentation bindings, 1 body-pose binding, 1 span.
6. **Performance fitness.** Four colliders and one joint; the frame is a static body. Part B adds seven small colliders because the sockets, indicator and ring are drawn proud; sockets keep their colliders by the owner's 21 November decision. The head moves at up to 4 m/s with a 0.25 m radius; tunnelling is excluded by the engine's speculative contacts, not a per-element check. Stated truthfully: the drive's power bound is enforced by a whole-world snapshot and up to eleven re-solves per substep (`WorkshopPhysicsEngine.Motors.cs:35`–`:66`), work proportional to the scene; engine gap owned by `ENGINE-ISLANDS-SLEEP`, which confines the trials to the drive's island. Island sleeping is not implemented; a resting body is still stepped, at the cost of its broadphase proxies and rows every substep (no sleep or deactivation code in `CuriousContraptions.Simulation`). Owned by `ENGINE-ISLANDS-SLEEP` (owner decision 3a, 22 November 2026).
7. **Scenario tests.** All tests in sections 3 and 5 run the production simulation; the generic drive and extrusion tests use fixtures, not this package.
