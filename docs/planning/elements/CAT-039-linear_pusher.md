# CAT-039 · linear_pusher declaration readiness spec

Story 7.0 CAT-039-D readiness spec. Baseline commit `a6c914e`. Legacy citations use `path@a6c914e:Lstart-Lend` and stay retrievable from git history after the Epic 7 purge. Current-engine files are cited at the same commit. Values are authored as f32 game values under [the numeric contract](../../gpu-f32-physics.md#numeric-representation-and-precision); legacy `double`/`float` magnitudes carry forward only as authored values.

## 1. Identity

| Field | Value |
| --- | --- |
| CAT ID | CAT-039 |
| Kind | `linear_pusher` (catalogue title "Linear pusher", category Power) |
| Requirement anchor | [CAT-039](../requirements.md#current-cat-039); retained behaviour [sequence-task-309](../requirements.md#sequence-task-309); consumers [CAT-039-I](../invest/current-consumers.md#cat-039-i) |
| Mapped identities | None found in [named-elements](../invest/named-elements.md). EL-053 rotary-to-linear converter is a separate shaft-driven element, not this electric servo. |
| Roadmap story | 11.3 Linear Pusher Telescopic Actuator ([epics](../../../_bmad-output/planning-artifacts/epics.md), Epic 11) |
| Status | Not started. No `WorkshopPartKind` member exists (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`). |

## 2. Declaration

### Bodies and shapes
- **Fixed frame (static body).** Base box centre (0, −0.5, 0), full size 1.6 × 0.2 × 1 m; barrel box centre (0, 0, 0), full size 1.2 × 0.65 × 0.65 m (`parts/LinearPusherPart.cs@a6c914e:L165-L166`; `AddBox` takes full size and stores half extents, `reference/cpu/MachinePart.cs@a6c914e:L352-L356`). Declare as a static `RigidBodyDeclaration` plus `ColliderDeclaration` boxes (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L120`).
- **Head (dynamic body).** Solid sphere, radius 0.25 m, rest centre at frame-local (0.85, 0, 0) (`parts/LinearPusherPart.cs@a6c914e:L18-L19`, `parts/LinearPusherPart.cs@a6c914e:L24-L27`, `parts/LinearPusherPart.cs@a6c914e:L169-L172`). The sphere collider and `RigidMassProperties.Compile` cover it now (`engine/gpu/RigidMassProperties.cs@a6c914e:L26-L51`).
- **Telescoping rod.** Opaque collider box between the barrel face at x = 0.6 and the head centre x: centre ((0.6 + x)/2, 0, 0), half extents ((x − 0.6)/2, 0.06, 0.06) (`parts/LinearPusherPart.cs@a6c914e:L160-L161`). It is physical and blocks rays and bodies (`CuriousContraptions.tests/RuntimeQueryOwnershipTests.cs@a6c914e:L262-L281`). Its new representation is a Story 11.3 design decision (Open question 3).

### Mass and material
- Head mass 0.5 kg (`parts/LinearPusherPart.cs@a6c914e:L20`). Material: restitution 0, bounce threshold 0.1 m/s, friction 0.3 (`parts/LinearPusherPart.cs@a6c914e:L83`; field order `engine/physics/PersistentContactPair.cs@a6c914e:L18-L29`). Use `ContactMaterialDeclaration` (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L54`); rolling resistance is unspecified (Open question 6).

### Constraints and joints
- One prismatic (slider) guide from head to frame. Frame anchor (0.85, 0, 0); axis is frame +X (rotation vector (0, π/2, 0) on both frames); travel range [0, stroke]; both directions; connected-body collision disabled (`parts/LinearPusherPart.cs@a6c914e:L39-L50`). No slider declaration exists in `engine/gpu`; Story 6.4 adds it.
- Self-locking brake: Hold mode locks the coordinate at its current value (`engine/physics/PhysicsServo.cs@a6c914e:L52`).

### Typed sockets and ports
All electrical. Local positions in metres (`parts/LinearPusherPart.cs@a6c914e:L84-L91`):

| Socket | Direction | Local position |
| --- | --- | --- |
| PowerIn | Input | (−0.45, −0.4, 0.5) |
| ExtendIn | Input | (0.15, −0.4, 0.5) |
| RetractIn | Input | (0.55, −0.4, 0.5) |
| RetractedOut | Output | (−0.45, 0.4, 0.45) |
| ExtendedOut | Output | (0.45, 0.4, 0.45) |

The current `WorkshopSocket` enum has only ActivationOut, ActivationIn, Supply and PowerIn (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L13`). Story 11.3 adds ExtendIn, RetractIn, ExtendedOut and RetractedOut as enum members. The legacy wire names are snake_case, such as `extend_in` and `retracted_out` (`engine/MachineData.cs@a6c914e:L103-L137`).

### Sensors and activation
- Endpoint contacts: PowerIn routes to RetractedOut at the lower endpoint and to ExtendedOut at the upper endpoint, within the endpoint tolerance (`parts/LinearPusherPart.cs@a6c914e:L92-L96`; contact kind `engine/SceneElectricalContact.cs@a6c914e:L49-L55`).
- No activation-domain port. The part consumes no `ActivationNetwork` node (`engine/gpu/ActivationNetwork.cs@a6c914e:L7-L14`).

### Work and energy stores
- No store. Bounded drive: effort ≤ force, power ≤ force × speed, work per step ≤ power × step (`parts/LinearPusherPart.cs@a6c914e:L28-L32`; `engine/physics/PhysicsServo.cs@a6c914e:L78-L79`). Supply is binary, not a battery-energy model (`parts/LinearPusherPart.cs@a6c914e:L12`).

### Parameters

| Name | Type | Range | Default | Unit | Source |
| --- | --- | --- | --- | --- | --- |
| stroke | f32 | 0.25–3 | 2.4 | m | `parts/LinearPusherPart.cs@a6c914e:L99`; `parts/catalog/linear_pusher.tres@a6c914e:L12` |
| speed | f32 | 0.25–4 | 1.5 | m/s | `parts/LinearPusherPart.cs@a6c914e:L100`; `parts/catalog/linear_pusher.tres@a6c914e:L12` |
| acceleration | f32 | 1–30 | 12 | m/s² | `parts/LinearPusherPart.cs@a6c914e:L101`; `parts/catalog/linear_pusher.tres@a6c914e:L12` |
| force | f32 | 1–100 | 40 | N | `parts/LinearPusherPart.cs@a6c914e:L102`; `parts/catalog/linear_pusher.tres@a6c914e:L12` |

Maximum power = force × speed (W) is derived, not authored. The typed enum `PusherParameter { Stroke, Speed, Acceleration, Force }` names the parameters (`parts/LinearPusherPart.cs@a6c914e:L10`).

### Cosmetic curves and UI bindings
- Phase indicator colour: Extending or Retracting `#66b8c9`; Blocked or Conflict `#f7cb52`; otherwise `#556573` (`parts/LinearPusherPart.cs@a6c914e:L124-L128`). The phases are Holding, Extending, Retracting, Blocked, Conflict and Unpowered (`parts/LinearPusherPart.cs@a6c914e:L9`). Declare the indicator as a cosmetic binding (`engine/gpu/WorkshopCosmetic.cs@a6c914e:L7-L20`); no phase feedback source exists yet.
- The rod mesh is an axis-affine map of the head coordinate (`parts/LinearPusherPart.cs@a6c914e:L148-L159`).

### Art
- Scene `parts/scenes/linear_pusher.tscn` holds only the script (`parts/scenes/linear_pusher.tscn@a6c914e:L1-L4`). Meshes come from `Build` (`parts/LinearPusherPart.cs@a6c914e:L162-L179`):
  - navy base `#293954` and cream barrel `#fff8e9`;
  - gold rod `#e8b764`, 0.12 m square;
  - cream head sphere with a cyan ring (radius 0.2, tube 0.035, `#66b8c9`);
  - five gold travel marks, 0.025 × 0.025 × 0.25 m, at x = −0.4 + 0.2i, y = 0.34;
  - port spheres of radius 0.065 (outputs cyan, inputs gold) and an indicator of radius 0.07 at (0, 0.42, 0);
  - pick radius 1.15 m.
- Design authority: [DESIGN.md "Electric linear pusher"](../../../DESIGN.md#electric-linear-pusher).
- Socket pictograms `extend_input`, `retract_input`, `extended_output` and `retracted_output`: `ui/WorkshopIcons.cs@a6c914e:L28-L31`. That file has no `linear_pusher` part pictogram.

### Catalogue and inventory entry
- Id `linear_pusher`, title "Linear pusher", category Power, colour (0.4, 0.72, 0.79) = `#66b8c9`, defaults as above (`parts/catalog/linear_pusher.tres@a6c914e:L6-L12`). No level in `content/puzzles.json` at `a6c914e` places or stocks it.

## 3. Engine capabilities

Capability families: see the [CAT-039 map row](../general-engine-element-map.md) and [catalogue coverage](../../coverage/catalogue-elements.json). They are not duplicated here.

**Exists now**
- Dynamic sphere head and static box frame: `engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L120`, `engine/gpu/RigidMassProperties.cs@a6c914e:L26-L51`.
- Contact and friction in the TGS Soft solver, in the simulation worker:
  - `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L230-L236` soft parameters;
  - `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L623` prepareContact;
  - `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L702-L719` friction;
  - `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L774` normal row;
  - `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L878` sweep.
- Electrical connection domain and PowerIn socket: `engine/gpu/WorkshopConnections.cs@a6c914e:L8-L13`.
- Save codec: `engine/gpu/WorkshopSaveCodec.cs@a6c914e:L6-L10`.

**Missing**
- Prismatic slider joint with travel limits and connected-body collision suppression: Story 6.4.
- Electrical supply source and powered state: Story 8.1 (CAT-005 Battery).
- Story 11.3 builds:
  - a bounded joint drive with acceleration ramp, endpoint targets and self-locking hold;
  - endpoint electrical contacts;
  - the telescoping rod collider;
  - the four new sockets.

**Element dependencies**
- CAT-005 Battery (Story 8.1).
- An electrical command source for ExtendIn and RetractIn. The current CAT-063 Switch is activation-only (`engine/gpu/WorkshopConnections.cs@a6c914e:L32-L35`); see Open question 5.
- Integration: sensor → delay → pusher, moving conveyor cargo into a tube ([sequence-task-309](../requirements.md#sequence-task-309)). This needs CAT-019 Conveyor and CAT-048 Pipe.

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

This outline points to [CAT-039](../requirements.md#current-cat-039), [sequence-task-309](../requirements.md#sequence-task-309) and Story 11.3 rather than restating them.
- **Chrome UI recipe.**
  1. In actual Chrome, place a Battery and the Linear pusher from the toolbox.
  2. With the contextual wiring UI, wire Battery Supply to PowerIn, and wire two electrical command sources to ExtendIn and RetractIn.
  3. Place a Bowling ball in front of the head.
  4. Verify the placed pose and the typed connections from the committed construction read.
- **Positive.** Extend pushes the ball forward at bounded speed. The head halts and holds at full stroke, and ExtendedOut powers its load on the next electrical tick.
- **Negative/control.** Without supply the head holds at zero extension with zero work. Both commands together brake. A miss (no cargo) moves only the head.
- **Boundaries.** Parameter range edges and rejections (facts 18, 25); stall against a wall (facts 16, 26); vertical low-force support (fact 20); rotated mounting (fact 15).
- **Run/Reset.** Reset at mid-stroke restores the construction with zero extension and zero work (fact 17).
- **Save/Load.** A round trip preserves parameters, ports and connections, and the replay is identical (fact 23).
- **Integrations.** Sensor → delay → pusher → conveyor cargo → tube, after CAT-019 and CAT-048.

## 6. Open questions

1. Is the self-locking hold ideal (the legacy locked range) or bounded by `force`? Unspecified — owner decision.
2. **Epic story vs requirement conflict.** Story 11.3 configures a 1.0 m stroke while the catalogue default is 2.4 m (`parts/catalog/linear_pusher.tres@a6c914e:L12`), and it wires the pusher to a switch, but the current CAT-063 Switch is activation-only (`engine/gpu/WorkshopConnections.cs@a6c914e:L32-L35`) while ExtendIn and RetractIn are electrical. How is stroke configured in the UI (or which qualification fixture is used), and which electrical source replaces the switch? Unspecified — owner decision.
3. How is the telescoping rod represented under one-way data flow: as a compound child of the head, a separate guided body or a frame collider? Unspecified — Story 11.3 design decision.
4. Which f32 tolerances replace the 1e-6 m endpoint tolerance and the 1e-6 m/s Blocked speed? Unspecified — owner decision.
5. Which current parts supply electrical ExtendIn and RetractIn commands, given that CAT-063 Switch is activation-only (see Open question 2)? Unspecified — owner decision.
6. What rolling resistance does the head material use? The current declaration requires a value. Unspecified — owner decision.
7. Does placement assistance apply to the pusher frame and head? Unspecified — owner decision.
