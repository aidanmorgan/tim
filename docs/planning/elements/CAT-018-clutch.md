# CAT-018 · clutch declaration readiness spec

Story 7.0 CAT-018-D readiness spec. Baseline commit `a6c914e`. Legacy citations use `path@a6c914e:Lstart-Lend` and stay retrievable after the Epic 7 purge. Current-engine files are cited at the same commit. Current authority for the shaft model is the [rotary transmission contract](../../rotary-transmission-parts.md). This page records the declaration knowledge the legacy holds; it does not restate the requirement row.

## 1. Identity

| Field | Value |
| --- | --- |
| CAT ID | CAT-018 |
| Kind | `clutch`, catalogue title "Clutch", category "Power" (`parts/catalog/clutch.tres@a6c914e:L6-L8`) |
| Requirement anchor | [CAT-018](../requirements.md#current-cat-018); retained behaviour [sequence-task-310](../requirements.md#sequence-task-310); shared rows [sequence-task-192](../requirements.md#sequence-task-192), [sequence-task-193](../requirements.md#sequence-task-193), [sequence-task-277](../requirements.md#sequence-task-277), [sequence-task-280](../requirements.md#sequence-task-280) |
| Mapped identities | None. No EL/TH/RAD/GAP names a clutch. [EL-055 Mechanical brake](../invest/named-elements.md#element-055) and [TH-05 Friction brake](../invest/named-elements.md#thermal-05) are separate elements; sequence-task-310 keeps brake and slip/torque-limiting variants separate. Batch A confirms. |
| Roadmap story | [Story 11.2](../../../_bmad-output/planning-artifacts/epics.md) Mechanical Clutch & Reverse Transmission (CAT-018, CAT-057) |
| Status | Not started. `WorkshopPartKind` has no clutch member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`). |

## 2. Declaration

### Bodies and shapes
- **Static housing (root body).** Navy base box centred (0, −0.48, 0), size 1.9 × 0.16 × 1.1 m. Two cream bearing blocks centred (±0.65, −0.1, 0), each 0.35 × 0.7 × 0.65 m (`parts/ClutchPart.cs@a6c914e:L77-L79`). The legacy `AddBox` helper survives only in `reference/cpu/MachinePart.cs@a6c914e:L352-L356`: it always adds a root-body collider box with half extents = size/2, and its `draw` flag controls art only. So these boxes collide. Use `ColliderDeclaration` box shapes (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L120`).
- **Input shaft and output shaft.** Two dynamic wheel bodies, radius 0.28 m and width 0.13 m. Their hinge anchors are at (−0.65, 0, 0.43) and (0.65, 0, 0.43) in the part frame, about local Z (`parts/ClutchPart.cs@a6c914e:L18-L19`, `parts/ClutchPart.cs@a6c914e:L23-L24`). Each has a 24-sided convex prism hull of that radius and width (`engine/SceneRotaryShaft.cs@a6c914e:L29-L42`). The current `ColliderShapeKind` has only Sphere, Box and Plane (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L7-L7`), so a cylinder or hull shape is required.
- **Plates (cosmetic).** Two gold discs, radius 0.3 m and length 0.18 m. Their rest x is ±0.09 m, opening by 0.16 m × (1 − closure) (`parts/ClutchPart.cs@a6c914e:L65-L66`, `parts/ClutchPart.cs@a6c914e:L95-L100`). The legacy declares no plate collider.

### Mass and material
- Each shaft: mass 0.25 kg (`parts/ClutchPart.cs@a6c914e:L18-L19`). Inertia: axial m·r²/2, transverse m·(3r² + w²)/12 (`engine/SceneRotaryShaft.cs@a6c914e:L13-L24`). The current `RigidMassProperties` compiles only a sphere or a box (`engine/gpu/RigidMassProperties.cs@a6c914e:L17-L50`), so a cylinder inertia law must be added.
- Shaft contact material: the part's initial contact material (`engine/SceneRotaryShaft.cs@a6c914e:L22-L22`). No values are recorded. Use `ContactMaterialDeclaration` (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L54`).
- Shafts are excluded from static world queries (`engine/SceneRotaryShaft.cs@a6c914e:L22-L22`).

### Constraints and joints
- Two hinges, each linking a shaft to the housing, about local Z, travel in both directions. Hinge-to-housing collision is disabled (`engine/SceneRotaryShaft.cs@a6c914e:L25-L28`).
- One coupling row between the two hinge coordinates: ratio +1, typed engagement `Open | Engaged` (`parts/ClutchPart.cs@a6c914e:L20-L26`, `engine/physics/PhysicsTransmissionJoint.cs@a6c914e:L7-L7`). An Open coupling contributes no velocity or acceleration rows; the hinges and identities remain (`engine/physics/PhysicsTransmissionJoint.cs@a6c914e:L50-L63`).
- A new declaration type is required, such as a hinge declaration and a ratio-coupling declaration with an enum engagement. None exists in `engine/gpu/*`.

### Typed sockets and ports
| Socket | Domain | Direction | Local position (m) | Source |
| --- | --- | --- | --- | --- |
| DriveIn | Mechanical | Input | (−0.65, 0, 0.55) | `parts/ClutchPart.cs@a6c914e:L40-L40` |
| Drive | Mechanical | Output | (0.65, 0, 0.55) | `parts/ClutchPart.cs@a6c914e:L41-L41` |
| PowerIn | Electrical | Input | (0, −0.35, 0.5) | `parts/ClutchPart.cs@a6c914e:L42-L42` |

Mechanical bindings: DriveIn maps to the input hinge and Drive to the output hinge, each with coordinate-per-radian −1 (`parts/ClutchPart.cs@a6c914e:L44-L44`). The current `WorkshopSocket` has PowerIn but no Drive or DriveIn, and `WorkshopConnectionDomain` has no Mechanical member (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L13`).

### Sensors and activation
- No activation input. The coil is driven only by electrical supply on PowerIn (`parts/ClutchPart.cs@a6c914e:L52-L52`).
- The legacy set `Active` when Engaged and raised a `Powered` event while supplied (`parts/ClutchPart.cs@a6c914e:L56-L56`, `parts/ClutchPart.cs@a6c914e:L68-L68`).

### Work and energy stores
None. The clutch stores no energy. Its phase/closure state is discrete runtime state, not a store (`parts/ClutchPart.cs@a6c914e:L27-L31`).

### Parameters
| Name | Type | Range | Default | Unit | Source |
| --- | --- | --- | --- | --- | --- |
| close_seconds | duration (enum-typed key `ClutchParameter.CloseSeconds`) | 0.05–2 inclusive; non-finite rejected | 0.2 | s | `parts/catalog/clutch.tres@a6c914e:L12-L12`, `parts/ClutchPart.cs@a6c914e:L45-L49`, [CAT-018](../requirements.md#current-cat-018) |

### Cosmetic curves and UI bindings
- Plate offset follows the committed closure fraction (`parts/ClutchPart.cs@a6c914e:L65-L66`). The plates' spin follows the input and output hinge angles (`parts/ClutchPart.cs@a6c914e:L72-L72`). Spokes follow each shaft's own signed speed ([DESIGN.md clutch](../../../DESIGN.md#electrically-controlled-clutch)).
- Indicator lamp: gold `#f7cb52` when engaged, cyan `#66b8c9` when supplied but not engaged, slate `#556573` otherwise (`parts/ClutchPart.cs@a6c914e:L67-L67`).
- Readings are signed with clockwise positive: speed is −(hinge speed) and angle is −(hinge coordinate) (`parts/ClutchPart.cs@a6c914e:L32-L35`).
- Pick radius 1.1 (`parts/ClutchPart.cs@a6c914e:L76-L76`).

### Art
- Scene `parts/scenes/clutch.tscn` is a bare script node (`parts/scenes/clutch.tscn@a6c914e:L1-L4`). All geometry is built in code (`parts/ClutchPart.cs@a6c914e:L74-L100`).
- Meshes: navy `#293954` base and axle (cylinder radius 0.055, length 1.3); cream `#fff8e9` blocks and pulleys; gold `#e8b764` plate discs, pulley spokes and the PowerIn stud; cyan `#66b8c9` coil ring (radius 0.38, thickness 0.065); navy plate bars (`parts/ClutchPart.cs@a6c914e:L77-L99`).
- Catalogue colour (0.4, 0.72, 0.79) (`parts/catalog/clutch.tres@a6c914e:L11-L11`). The DESIGN.md colour table has no clutch row. Form follows [DESIGN.md clutch](../../../DESIGN.md#electrically-controlled-clutch).
- Toolbox pictogram: `ui/WorkshopIcons.cs@a6c914e:L32`.

### Catalogue and inventory entry
- `Id = "clutch"`, title "Clutch", category "Power", default parameters `{close_seconds: 0.2}` (`parts/catalog/clutch.tres@a6c914e:L6-L12`).
- No `content/puzzles.json` level places the clutch or lists it in inventory at a6c914e. A `WorkshopPartKind` member and a `PartInventory` entry must be added (`engine/gpu/WorkshopInventory.cs@a6c914e:L9-L23`).

## 3. Engine capabilities

The capability families are in the [general-engine map row](../general-engine-element-map.md) and the [catalogue coverage](../../coverage/catalogue-elements.json). They are not duplicated here.

**Exists now**
- Dynamic and static rigid bodies, with box/sphere/plane colliders and materials (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L120`).
- Box2D v3 TGS Soft contact solve at 480 Hz substeps, with 8 biased and 4 relax iterations (`CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L208-L208`, `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L225-L226`, `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L230-L236`, `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L774-L793`, `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L878-L884`).
- Typed Electrical domain and PowerIn socket (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L13`).
- Cosmetic curve declarations for the animation worker (`engine/gpu/WorkshopCosmetic.cs@a6c914e:L7-L20`).
- Save codec (`engine/gpu/WorkshopSaveCodec.cs@a6c914e:L6-L10`).

**Missing**
- Cylinder/hull collider and cylinder inertia. No owning story — owner decision (see Open questions). Story 6.6 builds a hollow pipe collider, not a solid wheel.
- Hinge joint: Story 10.3.
- Shaft torque, motor drive and Mechanical socket domain: Story 11.1.
- Ratio coupling with enum engagement, and coil closure phases: Story 11.2.
- Electrical supply source and network: Story 8.1.

**Element dependencies.** CAT-042 Motor (11.1) is the upstream drive. CAT-005 Battery (8.1) supplies the coil. CAT-019 Conveyor (11.1) is the first downstream load in the legacy fixtures. CAT-057 Reverse transmission (11.2) appears in the signed-drive control.

## 4. Legacy harvest

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | Phases Open, Closing, Engaged, Opening as an enum | `parts/ClutchPart.cs@a6c914e:L9-L9` | carry forward | Matches the requirement and is a closed enum |
| 2 | Closure moves linearly toward 1 when supplied and toward 0 otherwise, at rate 1/close_seconds per second. The phase is derived from supply and closure (Engaged only at closure = 1, Open only at 0). | `parts/ClutchPart.cs@a6c914e:L50-L55` | carry forward (law); do not carry forward (`PreparePhysics` per-part update) | A per-element update loop is forbidden; declare the closure as a generic timed-phase law |
| 3 | The coupling is engaged only when the phase is Engaged. Supply loss sets Opening and opens the coupling on the next step while the plates keep opening. | `parts/ClutchPart.cs@a6c914e:L20-L20`; `CuriousContraptions.tests/ClutchTests.cs@a6c914e:L111-L113` | carry forward | Matches the contract |
| 4 | Engagement change was applied by replacing the joint list in the CPU world | `parts/ClutchPart.cs@a6c914e:L57-L64` | do not carry forward | CPU solver path; use a typed engagement lane in the declared coupling row |
| 5 | Coupling is phase-free, velocity-level, output = ratio × input, with no positional stop and no copied speed | `engine/physics/PhysicsTransmissionJoint.cs@a6c914e:L9-L13`, `engine/physics/PhysicsTransmissionJoint.cs@a6c914e:L66-L73` | carry forward (law); do not carry forward (double-precision CPU joint class) | f32 TGS Soft row in the shared solver |
| 6 | Acceptance: engaged closure between a moving and a stationary shaft shares speed without energy gain. With equal inertia, speed halves; KE after ≤ KE before + 1e−8. | `CuriousContraptions.tests/RotaryTransmissionPartTests.cs@a6c914e:L85-L102` | carry forward (re-freeze tolerances under f32) | Restated as an acceptance fact |
| 7 | Acceptance: after opening, an impulse on the input does not change the output speed | `CuriousContraptions.tests/RotaryTransmissionPartTests.cs@a6c914e:L103-L111` | carry forward | Restated as an acceptance fact |
| 8 | Acceptance: an open clutch transfers no impulse; output speed stays 0. Axis-aligned and rotated (25°, 40°, 15°) cases both apply. | `CuriousContraptions.tests/RotaryTransmissionPartTests.cs@a6c914e:L35-L65` | carry forward | Rotated control |
| 9 | Acceptance: both coil and mechanical input are required. Missing either gives output 0. Reversed drive gives a negative output. Connection order does not matter. | `CuriousContraptions.tests/ClutchTests.cs@a6c914e:L35-L80` | carry forward | Positive and negative matrix |
| 10 | Output is 0 on the first step because the plates are not yet closed | `CuriousContraptions.tests/ClutchTests.cs@a6c914e:L69-L70` | carry forward | Engages only when fully closed |
| 11 | The legacy motor fixture reached steady shaft speed 6 (rad/s) within 120 steps | `CuriousContraptions.tests/ClutchTests.cs@a6c914e:L71-L74`, `CuriousContraptions.tests/ClutchTests.cs@a6c914e:L180-L181` | do not carry forward | Ideal regulated motor speed; the current motor is bounded effort, not assigned velocity ([CAT-042](../requirements.md#current-cat-042)) |
| 12 | Acceptance: after power loss, a load impulse on the disconnected output does not affect the input. The output coasts at its own speed, and the downstream conveyor shaft equals the clutch output. | `CuriousContraptions.tests/ClutchTests.cs@a6c914e:L110-L128` | carry forward | Independent signed momentum |
| 13 | Acceptance: re-supply enters Closing with the coupling still Open; output momentum is unchanged until full closure, after which input = output | `CuriousContraptions.tests/ClutchTests.cs@a6c914e:L129-L135` | carry forward | Re-engagement control |
| 14 | Frame-rate presentation (`_Process`) must not advance closure | `CuriousContraptions.tests/ClutchTests.cs@a6c914e:L125-L125` | carry forward | Presentation observes committed state |
| 15 | Acceptance: a clutch→belt→clutch loop is admitted and stays at rest. A second input belt into an occupied DriveIn is rejected at connect and at load. | `CuriousContraptions.tests/ClutchTests.cs@a6c914e:L141-L162`; `engine/MechanicalNetwork.cs@a6c914e:L35-L35` | carry forward | Topology rule; conflicts with an older loop ban (see Open questions) |
| 16 | Acceptance: whole-part rotation about x, y or z by 90° keeps the port domains (Mechanical, Mechanical, Electrical) and the drive sign | `CuriousContraptions.tests/ClutchTests.cs@a6c914e:L166-L185` | carry forward | Rotated mounting |
| 17 | Acceptance: a timed supply (hold timer) engages and then releases the clutch while the motor keeps running; the output keeps its momentum after release | `CuriousContraptions.tests/ClutchTests.cs@a6c914e:L186-L206` | carry forward | Timed-release integration |
| 18 | Acceptance: closure increments are never more than tick/close_seconds. Engagement time is close_seconds within one tick early or two ticks late, for 0.05, 0.2 and 2 s. | `CuriousContraptions.tests/ClutchTests.cs@a6c914e:L212-L232` | carry forward (bound); do not carry forward (legacy tick) | Re-express against the 120 Hz worker tick |
| 19 | close_seconds of 0, 3 and NaN are rejected at placement | `CuriousContraptions.tests/ClutchTests.cs@a6c914e:L238-L246`, `parts/ClutchPart.cs@a6c914e:L45-L49` | carry forward | Atomic rejection |
| 20 | Reset restores Open, closure 0 and zero angles; a save/load round trip reproduces the run | `CuriousContraptions.tests/ClutchTests.cs@a6c914e:L81-L87`, `CuriousContraptions.tests/ClutchTests.cs@a6c914e:L233-L234` | carry forward | Run/Reset and Save/Load |
| 21 | A failed tick restores activation, power, visibility, phase, closure and body state | `CuriousContraptions.tests/PartRuntimeCheckpointTests.cs@a6c914e:L44-L82` | carry forward; deferred to the injected-fault gate | The worker owns rollback; the legacy transaction mechanism is not carried forward |
| 22 | Shaft readings come from current physics with no observer callback, and return to 0 after restore | `CuriousContraptions.tests/PartRuntimeCheckpointTests.cs@a6c914e:L104-L144` | carry forward | Committed-state presentation |
| 23 | With one motor energy supply, total energy is at most supplied work plus lost height. An open clutch leaves the load at rest; an engaged one makes the load speed equal the motor speed within 1e−5. After supply loss, supplied work stays constant. | `CuriousContraptions.tests/MechanicalWorkTests.cs@a6c914e:L39-L93` | carry forward (re-freeze tolerances under f32) | Energy bound |
| 24 | Coupling rejects ratio 0, NaN and ∞, identical endpoints, ball-socket endpoints and undefined engagement | `CuriousContraptions.tests/TransmissionJointTests.cs@a6c914e:L124-L154` | carry forward | Admission rejection |
| 25 | Shaft hinges and the coupling must be replaced together; a partial replacement is rejected and the state is unchanged | `CuriousContraptions.tests/TransmissionJointTests.cs@a6c914e:L100-L122` | carry forward | Atomic compile |
| 26 | Catalogue text: "Uses ideal shaft speeds, without torque, slip or freewheel inertia" | `parts/catalog/clutch.tres@a6c914e:L9-L9` | do not carry forward | Superseded by the finite-inertia requirement; rewrite the description |
| 27 | Parameter key strings resolve through `PartParameterName.Of(ClutchParameter.CloseSeconds)` = "close_seconds", and an undefined enum value is rejected | `CuriousContraptions.tests/PartParameterNameTests.cs@a6c914e:L35-L35`, `CuriousContraptions.tests/PartParameterNameTests.cs@a6c914e:L50-L50` | carry forward | Serialization boundary name for an enum key |

### Files harvested
- `parts/ClutchPart.cs`
- `parts/catalog/clutch.tres`
- `parts/scenes/clutch.tscn`
- `reference/cpu/MachinePart.cs` (AddBox semantics only)
- `ui/WorkshopIcons.cs` (pictogram; not deleted by Epic 7)
- `engine/SceneRotaryShaft.cs`
- `engine/SceneTransmissionJoint.cs`
- `engine/physics/PhysicsTransmissionJoint.cs`
- `engine/MechanicalNetwork.cs`
- `engine/ConnectionPort.cs`
- `engine/MachineData.cs` (SocketId enum, L104-L108)
- `engine/physics/JointEquations.cs` (FrameJointKind, L5)
- `CuriousContraptions.tests/ClutchTests.cs`
- `CuriousContraptions.tests/RotaryTransmissionPartTests.cs`
- `CuriousContraptions.tests/TransmissionJointTests.cs`
- `CuriousContraptions.tests/PartRuntimeCheckpointTests.cs`
- `CuriousContraptions.tests/MechanicalWorkTests.cs`
- `CuriousContraptions.tests/PartParameterNameTests.cs`
- `content/puzzles.json` (checked, no clutch level)
- `engine/bridge/` (checked, no element knowledge)
- `tools/Campaign/Program.cs` (checked, no clutch lesson)
- `reference/P0-022-before/docs/coverage/engine/*.json` (checked, coverage snapshots only, no element knowledge)
- `tools/P0-007-actual-path-probe/inputs.json` (checked, file list only)

## 5. Acceptance outline

Follow [CAT-018](../requirements.md#current-cat-018), [sequence-task-310](../requirements.md#sequence-task-310) and Story 11.2. The Chrome observables are in the [rotary contract](../../rotary-transmission-parts.md#chrome-observable-acceptance).

- **Construction (actual UI).** Place Battery, Motor, Clutch and Conveyor from the toolbox. Wire Battery Supply → Motor PowerIn and Battery Supply → Clutch PowerIn. Belt Motor Drive → Clutch DriveIn and Clutch Drive → Conveyor DriveIn. Place a ball on the belt. Verify placed poses and typed connections in the construction read.
- **Positive.** The plates close over close_seconds, then the output turns with the input and the belt carries the ball.
- **Negative/control.** Remove the coil supply and the output stays at rest while the input turns. Remove the motor supply and nothing turns. A disconnected-output impulse control is required.
- **Boundaries.** close_seconds of 0.05, 0.2 and 2 are admitted; 0, 3 and NaN are rejected. Rotated mounting. Reversed upstream via CAT-057. Re-engagement under load.
- **Run/Reset.** Exact restore of the Open phase, closure 0 and zero angles.
- **Save/Load.** Rotated construction including close_seconds.
- **Integrations.** Hold timer or switch supply release, conveyor load, reverse transmission.

## 6. Open questions

1. close_seconds: the requirement gives a continuous range of 0.05–2, but the [rotary contract](../../rotary-transmission-parts.md#declaration-data) calls it "an enum-typed parameter choice". If it is a choice, the admitted set of values is unspecified — owner decision.
2. Plate collision: unspecified — owner decision. The housing and bearing blocks collide (legacy `AddBox`, `reference/cpu/MachinePart.cs@a6c914e:L352-L356`), but the plates are built as art without collider boxes.
3. Solid cylinder/hull collider and cylinder inertia for shaft wheels: Story 10.1 (ENGINE-CYLINDER), scheduled before the first shaft wheel by the owner on 9 Oct 2026.
4. Shaft contact material values (restitution, friction): unspecified — owner decision.
5. Mechanical loops: ClutchTests and MechanicalWorkTests admit loops, but `CuriousContraptions.tests/MechanicalTests.cs@a6c914e:L185-L199` rejects a loop with no source. Which rule stands — owner decision.
6. Plate closure easing: the legacy is linear in time; the DESIGN text says plates "separate smoothly". The easing curve is unspecified — owner decision.
7. Supply semantics: is the coil a binary supplied/unsupplied consumer, or does it draw finite electrical power from the Battery store (8.1)? Unspecified — owner decision.
8. **Epic story vs requirement conflict.** Story 11.2 disengages the clutch "via signal" and says the output "freewheels to a stop". CAT-018 is governed by an electrical coil supply, and after disengagement each shaft keeps its own signed motion and momentum ([CAT-018](../requirements.md#current-cat-018)). Is the story corrected to coil supply loss and coasting, or are a signal input and an explicit stopping law intended? Unspecified — owner decision.
