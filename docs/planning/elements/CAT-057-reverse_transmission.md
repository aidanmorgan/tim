# CAT-057 · reverse_transmission declaration readiness spec

Story 7.0 CAT-057-D readiness spec. Baseline commit `a6c914e`. Legacy citations use `path@a6c914e:Lstart-Lend` and stay retrievable after the Epic 7 purge. Current-engine files are cited at the same commit. Current authority for the shaft model is the [rotary transmission contract](../../rotary-transmission-parts.md). This page records the declaration knowledge the legacy holds; it does not restate the requirement row.

## 1. Identity

| Field | Value |
| --- | --- |
| CAT ID | CAT-057 |
| Kind | `reverse_transmission`, catalogue title "Reverse gear", category "Motion" (`parts/catalog/reverse_transmission.tres@a6c914e:L8-L10`) |
| Requirement anchor | [CAT-057](../requirements.md#current-cat-057); retained behaviour [todo-151](../requirements.md#todo-151); shared rows [sequence-task-192](../requirements.md#sequence-task-192), [sequence-task-193](../requirements.md#sequence-task-193), [sequence-task-277](../requirements.md#sequence-task-277) |
| Mapped identities | [EL-203 Reverse transmission](../invest/named-elements.md#element-203) ([full row](../requirements.md#element-203)). Candidate: [EL-200 Drive belt](../invest/named-elements.md#element-200), because the legacy belt link is the socket-to-socket ratio row the reverser consumes. Batch A confirms. |
| Roadmap story | [Story 11.2](../../../_bmad-output/planning-artifacts/epics.md) Mechanical Clutch & Reverse Transmission (CAT-018, CAT-057) |
| Status | Not started. `WorkshopPartKind` has no reverser member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`). |

## 2. Declaration

### Bodies and shapes
- **Static housing (root body).** Navy base box centred (0, −0.4, 0), size 1.5 × 0.16 × 0.9 m. Cream housing box centred (0, 0, −0.08), size 1.4 × 0.65 × 0.5 m (`parts/ReverseTransmissionPart.cs@a6c914e:L36-L37`). The legacy `AddBox` helper survives only in `reference/cpu/MachinePart.cs@a6c914e:L352-L356`: it always adds a root-body collider box with half extents = size/2, and its `draw` flag controls art only. So these boxes collide. Use `ColliderDeclaration` box shapes (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L120`).
- **Input wheel and output wheel.** Two dynamic shaft bodies, radius 0.32 m and width 0.14 m. Their hinge anchors are at (−0.36, 0, 0.32) and (0.36, 0, 0.32) in the part frame, about local Z (`parts/ReverseTransmissionPart.cs@a6c914e:L11-L16`). Each has a 24-sided convex prism hull (`parts/ReverseTransmissionPart.cs@a6c914e:L40-L41`, `engine/SceneRotaryShaft.cs@a6c914e:L29-L42`). The current `ColliderShapeKind` has only Sphere, Box and Plane (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L7-L7`).

### Mass and material
- Each wheel: mass 0.25 kg (`parts/ReverseTransmissionPart.cs@a6c914e:L11-L12`). Inertia: axial m·r²/2, transverse m·(3r² + w²)/12 (`engine/SceneRotaryShaft.cs@a6c914e:L13-L24`). The current `RigidMassProperties` compiles only a sphere or a box (`engine/gpu/RigidMassProperties.cs@a6c914e:L17-L50`).
- Material: the part's initial contact material (`engine/SceneRotaryShaft.cs@a6c914e:L22-L22`). No values are recorded. Use `ContactMaterialDeclaration` (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L54`). Wheels are excluded from static queries (same line).

### Constraints and joints
- Two hinges, each linking a wheel to the housing, about local Z, travel in both directions. Hinge-to-housing collision is disabled (`engine/SceneRotaryShaft.cs@a6c914e:L25-L28`).
- One always-engaged, phase-free ratio row of −1 between the two hinge coordinates (`parts/ReverseTransmissionPart.cs@a6c914e:L13-L18`). The row has no positional stop (`engine/physics/PhysicsTransmissionJoint.cs@a6c914e:L66-L73`), and connected-body collision is enabled on the row (`engine/physics/PhysicsTransmissionJoint.cs@a6c914e:L22-L23`).
- Add a hinge declaration and a ratio-coupling declaration. CAT-018 shares them with ratio +1 and enum engagement. Nothing exists in `engine/gpu/*`.

### Typed sockets and ports
| Socket | Domain | Direction | Local position (m) | Source |
| --- | --- | --- | --- | --- |
| DriveIn | Mechanical | Input | (−0.36, 0, 0.4) | `parts/ReverseTransmissionPart.cs@a6c914e:L27-L27` |
| Drive | Mechanical | Output | (0.36, 0, 0.4) | `parts/ReverseTransmissionPart.cs@a6c914e:L28-L28` |

Bindings: DriveIn maps to the input hinge and Drive to the output hinge, each with coordinate-per-radian −1 (`parts/ReverseTransmissionPart.cs@a6c914e:L30-L30`). A belt link compiles to a ratio row equal to input coordinate-per-radian ÷ output coordinate-per-radian, always engaged (`engine/MechanicalNetwork.cs@a6c914e:L46-L49`). The current sockets lack Drive and DriveIn and a Mechanical domain (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L13`).

### Sensors and activation
None. The reverser has no electrical or activation port (`parts/ReverseTransmissionPart.cs@a6c914e:L25-L29`). The legacy `Active` flag meant |input speed| > 1e−6 and was observation only (`parts/ReverseTransmissionPart.cs@a6c914e:L53-L56`).

### Work and energy stores
None. Kinetic energy lives only in the two finite-inertia wheels; the row creates no work (`engine/physics/PhysicsTransmissionJoint.cs@a6c914e:L9-L13`).

### Parameters
None. The catalogue declares `Parameters = {}` (`parts/catalog/reverse_transmission.tres@a6c914e:L14-L14`). The −1 ratio is fixed declaration data, not a player setting.

### Cosmetic curves and UI bindings
- Wheel art follows each committed hinge body. Readings are clockwise positive: speed is −(hinge speed) and angle is −(hinge coordinate) (`parts/ReverseTransmissionPart.cs@a6c914e:L21-L24`).
- Positive speed is clockwise viewed from the marked local +Z face, and whole-part rotation does not change the transmission sign (`DESIGN.md@a6c914e:L279-L279`).
- Pick radius 0.9 (`parts/ReverseTransmissionPart.cs@a6c914e:L34-L34`).

### Art
- Scene `parts/scenes/reverse_transmission.tscn` is a bare script node (`parts/scenes/reverse_transmission.tscn@a6c914e:L1-L6`).
- Meshes: navy `#293954` base; cream `#fff8e9` housing; wheel discs in the catalogue colour; navy index bar 0.48 × 0.065 × 0.04 at z 0.09; gold `#f7cb52` index sphere, radius 0.07, at (0.21, 0, 0.1) (`parts/ReverseTransmissionPart.cs@a6c914e:L36-L52`).
- Palette token: Reverse transmission (0.84, 0.61, 0.28) `#d69c47` (`DESIGN.md@a6c914e:L183-L183`, `parts/catalog/reverse_transmission.tres@a6c914e:L13-L13`). The motion contract describes cream housing, navy base and two ochre wheels with navy/gold index marks turning oppositely (`DESIGN.md@a6c914e:L279-L279`).
- Toolbox pictogram: `ui/WorkshopIcons.cs@a6c914e:L98`.

### Catalogue and inventory entry
- `Id = "reverse_transmission"`, title "Reverse gear", category "Motion". Description: reverses rotation; two reversers restore the original direction (`parts/catalog/reverse_transmission.tres@a6c914e:L8-L14`).
- Level `reverse_belt` ("The other way round", subtitle "18 / Reverse transmission"): inventory reverse_transmission 1 and conveyor 1 (`content/puzzles.json@a6c914e:L4649-L4657`). The reverser is an unlocked solution part `reverse_1` at (−0.3, 1, 0) (`content/puzzles.json@a6c914e:L4983-L5045`).
- Add a `WorkshopPartKind` member and an inventory entry (`engine/gpu/WorkshopInventory.cs@a6c914e:L9-L23`).

## 3. Engine capabilities

The capability families are in the [general-engine map row](../general-engine-element-map.md) and the [catalogue coverage](../../coverage/catalogue-elements.json). They are not duplicated here.

**Exists now**
- Rigid bodies, box/sphere/plane colliders and materials (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L120`).
- Box2D v3 TGS Soft solve at 480 Hz substeps (`CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L208-L208`, `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L225-L226`, `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L230-L236`, `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L878-L884`).
- Save codec (`engine/gpu/WorkshopSaveCodec.cs@a6c914e:L6-L10`).
- Cosmetic curves (`engine/gpu/WorkshopCosmetic.cs@a6c914e:L7-L20`).

**Missing**
- Cylinder/hull collider and cylinder inertia. No owning story — owner decision (see Open questions).
- Hinge joint: Story 10.3.
- Mechanical socket domain, belt ratio rows and shaft torque: Story 11.1.
- Fixed −1 ratio row in the shared solver: Story 11.2.

**Element dependencies.** A drive source is needed to observe motion: CAT-042 Motor (11.1), or later CAT-070 Windmill (12.3). CAT-019 Conveyor (11.1) is the load in `reverse_belt` and in the legacy tests. CAT-005 Battery (8.1) powers the motor.

## 4. Legacy harvest

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | One-to-one reversing gearbox; direction is relative to the marked shaft faces | `parts/ReverseTransmissionPart.cs@a6c914e:L7-L7` | carry forward | Matches EL-203 and the DESIGN sign convention |
| 2 | Ratio −1, always engaged, between two owned hinge coordinates | `parts/ReverseTransmissionPart.cs@a6c914e:L17-L17` | carry forward | Declaration data |
| 3 | Velocity-level, phase-free law: output = ratio × input. Shared inertia sets both speeds; no endpoint gets a copied speed or work allowance. | `engine/physics/PhysicsTransmissionJoint.cs@a6c914e:L9-L13` | carry forward (law); do not carry forward (double-precision CPU class, `Project`/`Sweep` hooks) | CPU solver path; becomes an f32 TGS Soft row |
| 4 | Acceptance: an impulse on the input gives output speed = −input speed in the next step. Total KE stays ≤ the impulse KE + 1e−8, both axis-aligned and rotated (25°, 40°, 15°). | `CuriousContraptions.tests/RotaryTransmissionPartTests.cs@a6c914e:L35-L65` | carry forward (re-freeze tolerances under f32) | Restated as an acceptance fact |
| 5 | Acceptance: same state and inputs replay to identical body states; Reset restores the saved construction and zero input speed | `CuriousContraptions.tests/RotaryTransmissionPartTests.cs@a6c914e:L74-L80` | carry forward | Determinism and Reset |
| 6 | Acceptance: engagement shares real inertia. With I_a = 2, I_b = 3 and ω_a0 = 10, ω_a = 20/(2 + 3r²) and ω_b = r·ω_a for r ∈ {1, 2, −1, −2}. Energy does not increase, and I_a·ω_a + r·I_b·ω_b is conserved. | `CuriousContraptions.tests/TransmissionJointTests.cs@a6c914e:L17-L34` | carry forward | Analytic acceptance check |
| 7 | Acceptance: a downstream load reacts back through the shared solve, α_a = (τ_a + r·τ_load)/(I_a + r²·I_b); the static carrier gets no wrench | `CuriousContraptions.tests/TransmissionJointTests.cs@a6c914e:L36-L51` | carry forward | Load/backdrive criterion |
| 8 | Acceptance: branches share source inertia instead of duplicating drive. With +1 and −1 branches from one source (I = 2, 3, 5; ω0 = 12), all speeds are ±2.4 and KE = 28.8. | `CuriousContraptions.tests/TransmissionJointTests.cs@a6c914e:L53-L64` | carry forward | No copied work |
| 9 | Acceptance: ratio −2 across multiple turns over 0.2 s keeps speed error 0 and energy within 1e−6, and replays exactly | `CuriousContraptions.tests/TransmissionJointTests.cs@a6c914e:L81-L98` | carry forward (behaviour); do not carry forward (1e−6 double tolerance) | Use the game-grade f32 envelope |
| 10 | Hinge and coupling replacement is atomic; a coupling to an undeclared guide is rejected | `CuriousContraptions.tests/TransmissionJointTests.cs@a6c914e:L100-L122` | carry forward | Atomic compile |
| 11 | Ratio 0, NaN and ∞, identical endpoints, and ball-socket endpoints are rejected | `CuriousContraptions.tests/TransmissionJointTests.cs@a6c914e:L143-L154`; `engine/SceneTransmissionJoint.cs@a6c914e:L17-L26` | carry forward | Admission rejection |
| 12 | Acceptance: 0, 1 or 2 reversers between conveyors give sign (−1)^n in the same substep, not only at steady state. Connection order does not matter, and the downstream conveyor may be rotated (30°, 90°, 20°). | `CuriousContraptions.tests/MechanicalTests.cs@a6c914e:L62-L103` | carry forward | Two-reverser and order criteria |
| 13 | Acceptance: each reverser shows output speed = −input speed and |angle diff(−input angle, output angle)| ≤ 1e−4 | `CuriousContraptions.tests/MechanicalTests.cs@a6c914e:L108-L112` | carry forward (re-freeze tolerances under f32) | Wheel witnesses follow committed travel |
| 14 | Acceptance: after supply loss the chain coasts at constant speed, motor supplied work is constant, and total KE is unchanged within 1e−8 over 120 steps (unloaded, ideal hinges) | `CuriousContraptions.tests/MechanicalTests.cs@a6c914e:L113-L124` | carry forward (re-freeze tolerances under f32) | Supply-loss criterion |
| 15 | Steady values: conveyor shaft 6 and surface speed 4 | `CuriousContraptions.tests/MechanicalTests.cs@a6c914e:L104-L107` | do not carry forward | Ideal regulated motor speed; the current motor is bounded effort |
| 16 | Reset zeros the reverser angles and keeps the connection count; re-run restores the sign | `CuriousContraptions.tests/MechanicalTests.cs@a6c914e:L125-L140` | carry forward | Run/Reset |
| 17 | Graph rules: Mechanical-to-PowerIn and Electrical-domain belt links are rejected. A second driver into an occupied DriveIn is rejected. Fan-out from one Drive is allowed. A bad load is rejected atomically. | `CuriousContraptions.tests/MechanicalTests.cs@a6c914e:L185-L208`; `engine/MechanicalNetwork.cs@a6c914e:L15-L39` | carry forward | Typed topology admission |
| 18 | A conveyor→reverser→conveyor loop with no source is rejected | `CuriousContraptions.tests/MechanicalTests.cs@a6c914e:L199-L199` | do not carry forward pending owner | Conflicts with the later loop admission in `CuriousContraptions.tests/MechanicalWorkTests.cs@a6c914e:L98-L114` (see Open questions) |
| 19 | Acceptance: in a motor→reverser→load circuit, load speed = −motor speed within 1e−5 and energy ≤ supplied work. No work accrues after supply loss. | `CuriousContraptions.tests/MechanicalWorkTests.cs@a6c914e:L39-L93` | carry forward (re-freeze tolerances under f32) | Energy bound |
| 20 | Acceptance: an unpowered control gives zero travel and zero motor work. With a reverser, the conveyor shaft speed and cargo travel are negative (> 0.02 m opposite) in axis-aligned and 90°-rotated belts. Presentation `_Process` does not change physics. | `CuriousContraptions.tests/ConveyorRuntimeTests.cs@a6c914e:L65-L112` | carry forward | Transport integration with control |
| 21 | Acceptance: windmill drive through 0, 1 or 2 reversers has the parity sign in the first substep | `CuriousContraptions.tests/WindmillTests.cs@a6c914e:L55-L88` | carry forward (sign rule); do not carry forward (windmill speed values) | Windmill values belong to CAT-070 |
| 22 | Shaft readings come from current physics without callbacks and are 0 after restore | `CuriousContraptions.tests/PartRuntimeCheckpointTests.cs@a6c914e:L104-L144` | carry forward | Committed-state presentation |
| 23 | Lesson `reverse_belt`: goal is ball captured by receiver. Solution wiring: battery Supply → motor PowerIn, motor Drive → reverse_1 DriveIn, reverse_1 Drive → conveyor_1 DriveIn. Motor locked at (−3.5, 1, 0); conveyor_1 at (2, 2.5, 0). | `content/puzzles.json@a6c914e:L4912-L4918`, `content/puzzles.json@a6c914e:L4893-L4897`, `content/puzzles.json@a6c914e:L4965-L4969`, `content/puzzles.json@a6c914e:L5047-L5069` | carry forward | Lesson set-up for Epic 15 |
| 24 | Lesson authoring: ball x = 3, receiver x = −2, battery (−5, 1, 0), motor (−3.5, 1, 0), reverser solution (−0.3, 1, 0) | `tools/Campaign/Program.cs@a6c914e:L159-L171` | carry forward (layout); do not carry forward (tool) | Campaign tool is deleted; Epic 15 rebuilds authoring |
| 25 | Lesson acceptance: every solution connection is required to win. A motor→conveyor bypass spins the belt forward but does not win. | `CuriousContraptions.tests/MechanicalTests.cs@a6c914e:L228-L268` | carry forward | Meaningful negative control for the level |
| 26 | `reverse_1` difficulty rows: max position correction 0.25 and rotation correction 5 at precision 0; 0.1 and 2 at 0.45; 0 at 1 | `content/puzzles.json@a6c914e:L4986-L5026` | carry forward | Existing difficulty evidence preserved |
| 27 | Hint text: "Its paired wheels turn in opposite directions." | `content/puzzles.json@a6c914e:L4653-L4653` | carry forward | Teaching copy |

### Files harvested
- `parts/ReverseTransmissionPart.cs`
- `parts/catalog/reverse_transmission.tres`
- `parts/scenes/reverse_transmission.tscn`
- `reference/cpu/MachinePart.cs` (AddBox semantics only)
- `ui/WorkshopIcons.cs` (pictogram; not deleted by Epic 7)
- `engine/SceneRotaryShaft.cs`
- `engine/SceneTransmissionJoint.cs`
- `engine/physics/PhysicsTransmissionJoint.cs`
- `engine/MechanicalNetwork.cs`
- `engine/ConnectionPort.cs`
- `engine/MachineData.cs` (SocketId enum, L104-L108)
- `engine/physics/JointEquations.cs` (FrameJointKind, L5)
- `CuriousContraptions.tests/RotaryTransmissionPartTests.cs`
- `CuriousContraptions.tests/TransmissionJointTests.cs`
- `CuriousContraptions.tests/MechanicalTests.cs`
- `CuriousContraptions.tests/MechanicalWorkTests.cs`
- `CuriousContraptions.tests/ConveyorRuntimeTests.cs`
- `CuriousContraptions.tests/WindmillTests.cs`
- `CuriousContraptions.tests/PartRuntimeCheckpointTests.cs`
- `content/puzzles.json`
- `tools/Campaign/Program.cs`
- `engine/bridge/` (checked, no element knowledge)
- `reference/P0-022-before/docs/coverage/engine/*.json` (checked, coverage snapshots only, no element knowledge)
- `tools/P0-007-actual-path-probe/inputs.json` (checked, file list only)

## 5. Acceptance outline

Follow [CAT-057](../requirements.md#current-cat-057), [todo-151](../requirements.md#todo-151), [EL-203](../requirements.md#element-203) and Story 11.2. The Chrome observables are in the [rotary contract](../../rotary-transmission-parts.md#chrome-observable-acceptance).

- **Construction (actual UI).** In `reverse_belt`, or a free-play bench, place the reverser and conveyor from the toolbox. Wire Battery Supply → Motor PowerIn, belt Motor Drive → Reverser DriveIn and Reverser Drive → Conveyor DriveIn. Verify placed poses and typed connections.
- **Positive.** The wheels turn in opposite directions and the belt carries the ball toward the receiver.
- **Negative/control.** A motor→conveyor bypass carries the ball the wrong way. An unpowered motor gives no motion. A disconnected reverser stays at rest.
- **Boundaries.** Each input direction, two reversers in series, a load/backdrive impulse on the output, whole-part rotation, reversed connection order, supply loss with coasting.
- **Run/Reset.** Exact zero angles, speeds and construction.
- **Save/Load.** A rotated two-reverser construction.
- **Integrations.** Conveyor transport, the clutch (CAT-018) reversed-drive case, and later windmill drive.

## 6. Open questions

1. Mechanical loops: `MechanicalTests` rejects a source-less loop, while `MechanicalWorkTests` and `ClutchTests` admit loops. Which topology rule stands — owner decision.
2. Housing collision: settled by the legacy `AddBox` helper (`reference/cpu/MachinePart.cs@a6c914e:L352-L356`), so the boxes collide. No owner decision remains.
3. Solid cylinder/hull collider and cylinder inertia for wheels: Story 10.1 (ENGINE-CYLINDER), scheduled before the first shaft wheel by the owner on 9 Oct 2026.
4. Wheel contact material values: unspecified — owner decision.
5. Whether wheel-rim contact with cargo is ever a supported drive path, or wheels are witnesses only: unspecified — owner decision.
