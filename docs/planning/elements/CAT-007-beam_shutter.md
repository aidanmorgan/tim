# CAT-007 · beam_shutter declaration readiness spec

Story 7.0 CAT-007-D readiness spec. Baseline commit `a6c914e`. Legacy citations use `path@a6c914e:Lstart-Lend` and stay retrievable from git history after the Epic 7 purge. Current-engine files are cited at the same commit. Values are authored as f32 game values under [the numeric contract](../../gpu-f32-physics.md#numeric-representation-and-precision). The blade law is shared with [CAT-051 powered gate](CAT-051-powered_gate.md) through one legacy controller, `engine/SlidingBlade.cs`. The shared facts are harvested in full there and summarised here.

## 1. Identity

| Field | Value |
| --- | --- |
| CAT ID | CAT-007 |
| Kind | `beam_shutter` (catalogue title "Beam shutter", category Optics) |
| Requirement anchor | [CAT-007](../requirements.md#current-cat-007); retained behaviour [todo-292](../requirements.md#todo-292); consumers [CAT-007-I](../invest/current-consumers.md#cat-007-i) |
| Mapped identities | None directly. [RAD-06 powered radiation shutter](../invest/named-elements.md#radiation-06) is a separate P1 potential element that would reuse the same supplied blade declaration (candidate relation — Batch A confirms). |
| Roadmap story | 13.3 Beam Shutter Mechanical Guillotine ([epics](../../../_bmad-output/planning-artifacts/epics.md), Epic 13) |
| Status | Not started. No `WorkshopPartKind` member exists (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`). |

## 2. Declaration

### Bodies and shapes
All positions are part-local metres; `AddBox` takes full size and stores half extents (`reference/cpu/MachinePart.cs@a6c914e:L352-L356`).
- **Fixed frame (static body)** (`parts/BeamShutterPart.cs@a6c914e:L25-L32`):
  - navy header box at (0, 1.45, 0), full size 0.45 × 0.85 × 1.65;
  - navy foot box at (0, −0.78, 0), full size 0.65 × 0.2 × 1.65;
  - two cream rails at z = ±0.7, centre y 0.2, full size 0.18 × 1.9 × 0.12.
  Use static `RigidBodyDeclaration` plus box `ColliderDeclaration`s (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L120`). There are no tube mouths: unlike the tube gate it has no snap connection ([DESIGN.md "Beam shutter"](../../../DESIGN.md#beam-shutter)).
- **Blade (dynamic body).** Opaque box with half extents (0.035, 0.6, 0.6), body-local at the origin when closed (`parts/BeamShutterPart.cs@a6c914e:L16`, `parts/BeamShutterPart.cs@a6c914e:L32`). Proxy boxes are opaque by default (`reference/cpu/MachinePart.cs@a6c914e:L11`). A box collider and `RigidMassProperties.Compile` cover it now (`engine/gpu/RigidMassProperties.cs@a6c914e:L26-L51`).

### Mass and material
- Blade mass 1 kg with homogeneous box inertia (`engine/SlidingBlade.cs@a6c914e:L14`, `engine/SlidingBlade.cs@a6c914e:L23-L26`); the current formula matches (`engine/gpu/RigidMassProperties.cs@a6c914e:L39-L48`).
- Material: no override, so the legacy part default applied. That default is restitution 1 for a non-dynamic part, bounce threshold 0.1 m/s and friction 0.3 (`reference/cpu/MachinePart.cs@a6c914e:L161-L164`, `reference/cpu/MachinePart.cs@a6c914e:L236`). Whether restitution 1 is intended for a moving blade is Open question 4. Use `ContactMaterialDeclaration` (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L54`) with rolling resistance 0, the current value for non-sphere bodies (`CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L721-L726`).

### Constraints and joints
- One prismatic (slider) guide, as for the powered gate: frame rotation (−π/2, 0, 0), so opening travels along local +Y; travel range [0, 1.3 m], both directions; connected-body collision disabled (`engine/SlidingBlade.cs@a6c914e:L28-L34`; stroke `parts/BeamShutterPart.cs@a6c914e:L15`).
- A return spring of 14 N/m with damping 4 N·s/m acts at all times (`engine/SlidingBlade.cs@a6c914e:L17-L18`, `engine/SlidingBlade.cs@a6c914e:L41-L42`). Story 6.4 adds the slider and spring.

### Typed sockets and ports
- One electrical input, PowerIn, at (0, 1.45, 0.86) (`parts/BeamShutterPart.cs@a6c914e:L23-L24`). It exists in the current enum (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L13`). There is no activation port and no rope socket (Open question 2).

### Sensors and activation
- None. The blade is an optical occluder, not a sensor. Light traces are infinitesimal rays tested against every body's light-query collider (`engine/WorldGeometry.cs@a6c914e:L173-L190`; `engine/OpticalNetwork.cs@a6c914e:L82`). When powered the part records a `Powered` event (`parts/BeamShutterPart.cs@a6c914e:L49`), which is presentation only.

### Work and energy stores
- No store. Bounded powered drive with maximum speed 2.8 m/s, stopping-profile acceleration 14 m/s², effort ≤ 60 N and power ≤ 120 W, aimed at the 1.3 m stroke (`engine/SlidingBlade.cs@a6c914e:L15-L20`, `engine/SlidingBlade.cs@a6c914e:L43-L46`). Return-spring energy is ½·14·x².

### Parameters
None. The catalogue declares no `Parameters` (`parts/catalog/beam_shutter.tres@a6c914e:L6-L11`); the requirement row says "declared geometry, typed ports and any source-owned settings". The fixed constants are stroke 1.3 m (`parts/BeamShutterPart.cs@a6c914e:L15`) plus the shared blade constants in [CAT-051 §2 Parameters](CAT-051-powered_gate.md#parameters).

### Cosmetic curves and UI bindings
- Indicator: Blocked `#e8b764`, powered `#f7cb52`, otherwise `#556573` (`parts/BeamShutterPart.cs@a6c914e:L44-L50`). DESIGN calls these slate, gold and ochre.
- `GateState` classification is shared (`engine/SlidingBlade.cs@a6c914e:L7`, `engine/SlidingBlade.cs@a6c914e:L49-L53`). The blade mesh is a rigid map of the blade body (`parts/BeamShutterPart.cs@a6c914e:L11-L13`).

### Art
- Scene `parts/scenes/beam_shutter.tscn` holds only the script (`parts/scenes/beam_shutter.tscn@a6c914e:L1-L4`). Build meshes (`parts/BeamShutterPart.cs@a6c914e:L25-L39`):
  - navy header and foot `#293954`, cream rails `#fff8e9`, gold blade `#f7cb52`;
  - two cream witness stripes, 0.008 × 0.04 × 1.05, at blade-local x = ±0.037, y = −0.45 (art only, children of the blade);
  - socket sphere radius 0.075, `#e8b764`, at (0, 1.45, 0.86);
  - indicator radius 0.07 at (0.24, 1.45, 0);
  - pick radius 1.4 m.
- Design authority: [DESIGN.md "Beam shutter"](../../../DESIGN.md#beam-shutter) and [todo-292](../requirements.md#todo-292).
- Toolbox pictogram: `ui/WorkshopIcons.cs@a6c914e:L82`.

### Catalogue and inventory entry
- Id `beam_shutter`, title "Beam shutter", category Optics, colour (0.97, 0.8, 0.32). Description: "Electricity lifts the gold blade to let light through. Losing supply closes it again; a ball in the blade's path safely stops closing until cleared. Wire a timer or sensor contact to control it." (`parts/catalog/beam_shutter.tres@a6c914e:L6-L11`). No level in `content/puzzles.json` at `a6c914e` places or stocks it.

## 3. Engine capabilities

Capability families: see the [CAT-007 map row](../general-engine-element-map.md) and [catalogue coverage](../../coverage/catalogue-elements.json). They are not duplicated here.

**Exists now**
- Dynamic box blade and static box frame: `engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L120`, `engine/gpu/RigidMassProperties.cs@a6c914e:L26-L51`.
- Box contact in the TGS Soft solver, in the simulation worker:
  - `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L230-L236` soft parameters;
  - `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L503` box-box;
  - `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L623` prepareContact;
  - `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L774` normal row;
  - `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L878` sweep.
- Electrical domain and PowerIn socket: `engine/gpu/WorkshopConnections.cs@a6c914e:L8-L13`.

**Missing**
- Slider joint with limits; spring and damping along the slider: Story 6.4.
- Electrical supply: Story 8.1.
- Bounded slider drive: Story 11.3, reused by Stories 8.2 and 13.3.
- Optical occlusion by the committed blade pose: Epic 13 (Laser CAT-036 in Story 13.1, light receiver CAT-038 in Story 13.2).

**Element dependencies**
- CAT-005 Battery.
- CAT-036 Laser and CAT-038 Light receiver for the optical half. The roadmap says "Optical shutter integration waits for the optical path, not shutter mechanics" ([vertical delivery](../invest/vertical-delivery.md)). Story 13.3 now follows the Laser (Story 13.1) and the receivers (Story 13.2) in Epic 13 (Open question 1).
- Lever-to-shutter rope integration (CAT-034, CAT-058; Stories 10.3 and 10.2) needs a rope socket that the legacy part lacks (Open question 2).

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

This outline points to [CAT-007](../requirements.md#current-cat-007), [todo-292](../requirements.md#todo-292) and Story 13.3 rather than restating them.
- **Chrome UI recipe.**
  1. In actual Chrome, place a Laser, a Light receiver and the Beam shutter between them, plus a Battery.
  2. Wire Battery Supply to the shutter's PowerIn with the contextual wiring UI.
  3. Verify pose and the typed electrical connection from the committed construction read.
- **Positive.** Supply lifts the blade and the receiver lights once the blade clears the beam (fact 9).
- **Negative/control.** Unpowered, the beam stays blocked and a rolling ball is stopped (facts 9, 11).
- **Boundaries.** Partial travel (fact 9); blocked closing on a supported ball, which the legacy did not test with a trapped ball, so the requirement row governs; clearing and retry; power interruption; rotated mounting (facts 9, 10); a ball obstructing the laser path.
- **Run/Reset.** Reset closes the blade exactly (fact 9).
- **Save/Load.** A round trip preserves pose and connection; the replay is identical.
- **Integrations.** Laser → shutter → receiver, with the Laser (Story 13.1) and receivers (Story 13.2) before Story 13.3; timer or sensor contact supply; lever and rope after Stories 10.3 and 10.2.

## 6. Open questions

1. Story 13.3 acceptance needs a laser path. Resolved by the owner's 9 Oct 2026 reorder: the Laser (Story 13.1) and the receivers (Story 13.2) now precede Story 13.3, so the optical proof no longer has to be deferred.
2. Story 13.3 says "opened by signal or rope", and CAT-034 requires "slack/taut shutter loads". The legacy shutter has no rope socket. What are its position, identity and coupling? Unspecified — owner decision.
3. Which f32 thresholds replace the 1e-5 m and 1e-5 m/s classification tolerances? Unspecified — owner decision.
4. Blade restitution: the legacy inherited 1 by default. Should it be authored explicitly, for example matching the gate's 0.1? Unspecified — owner decision.
5. Is the 14 N/m return spring the declared fail-closed law? This is shared with CAT-051 Open question 1. Unspecified — owner decision.