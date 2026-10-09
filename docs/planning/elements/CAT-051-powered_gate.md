# CAT-051 · powered_gate declaration readiness spec

Story 7.0 CAT-051-D readiness spec. Baseline commit `a6c914e`. Legacy citations use `path@a6c914e:Lstart-Lend` and stay retrievable from git history after the Epic 7 purge. Current-engine files are cited at the same commit. Values are authored as f32 game values under [the numeric contract](../../gpu-f32-physics.md#numeric-representation-and-precision). The blade law is shared with [CAT-007 beam shutter](CAT-007-beam_shutter.md) through one legacy controller, `engine/SlidingBlade.cs`. The two parts differ only in geometry, material, mouths and integrations.

## 1. Identity

| Field | Value |
| --- | --- |
| CAT ID | CAT-051 |
| Kind | `powered_gate` (catalogue title "Powered gate", category Motion) |
| Requirement anchor | [CAT-051](../requirements.md#current-cat-051); retained behaviour [todo-244](../requirements.md#todo-244); consumers [CAT-051-I](../invest/current-consumers.md#cat-051-i) |
| Mapped identities | None found in [named-elements](../invest/named-elements.md). EL-184 mechanical ball gate (linkage-driven) and EL-163 powered ball diverter are separate elements. |
| Roadmap story | 8.2 Powered Gate Passageway Barrier ([epics](../../../_bmad-output/planning-artifacts/epics.md), Epic 8) |
| Status | Not started. No `WorkshopPartKind` member exists (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`). |

## 2. Declaration

### Bodies and shapes
All positions are part-local metres; `AddBox` takes full size and stores half extents (`reference/cpu/MachinePart.cs@a6c914e:L352-L356`).
- **Fixed frame (static body)** (`parts/PoweredGatePart.cs@a6c914e:L32-L46`):
  - central translucent sleeve: tube proxy with half-length 0.4, inner radius = bore, outer radius 0.70, not opaque;
  - two cream collars at x = ±0.4: half-length 0.09, inner = bore, outer 0.78, opaque;
  - navy actuator housing box at (0, 1.65, 0), full size 0.8 × 0.65 × 1.8;
  - two cream rails at z = ±0.82, centre y 0.65, full size 0.24 × 1.7 × 0.12.
  The tube proxy field order is pose, half-length, inner, outer, opaque (`engine/TubeProxy.cs@a6c914e:L6`). The sleeve and collar radii equal the current pipe profile: bore 0.65, middle 0.70, end 0.78, collar 0.09 (`engine/gpu/WorkshopPipe.cs@a6c914e:L9-L19`). The gate's legacy bore reference `PipePart.BoreRadius` no longer exists at `a6c914e` (Open question 2). The hollow colliders need the annular profile from Stories 6.6 and 6.7 (`engine/gpu/AnnularProfile.cs`).
- **Blade (dynamic body).** Box half extents (0.06, 0.72, 0.72), body-local at the origin when closed (`parts/PoweredGatePart.cs@a6c914e:L22`, `parts/PoweredGatePart.cs@a6c914e:L46`). Its full height of 1.44 m exceeds the 1.3 m bore diameter, so a closed blade covers the bore. A box collider and `RigidMassProperties.Compile` cover it now (`engine/gpu/RigidMassProperties.cs@a6c914e:L26-L51`).

### Mass and material
- Blade mass 1 kg, homogeneous box inertia m(b² + c²)/3 per axis (`engine/SlidingBlade.cs@a6c914e:L14`, `engine/SlidingBlade.cs@a6c914e:L23-L26`). The current box formula matches (`engine/gpu/RigidMassProperties.cs@a6c914e:L39-L48`).
- Material: restitution 0.1, bounce threshold 0.1 m/s, friction 0.3 (`parts/PoweredGatePart.cs@a6c914e:L23`; field order `engine/physics/PersistentContactPair.cs@a6c914e:L18-L29`). Use `ContactMaterialDeclaration` (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L54`) with rolling resistance 0, the current value for non-sphere bodies (`CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L721-L726`).

### Constraints and joints
- One prismatic (slider) guide from blade to frame. Both frames sit at the origin with rotation vector (−π/2, 0, 0), so opening travels along part-local +Y. Travel range [0, 1.55 m], both directions, connected-body collision disabled (`engine/SlidingBlade.cs@a6c914e:L28-L34`; stroke `parts/PoweredGatePart.cs@a6c914e:L15`; +Y confirmed by `CuriousContraptions.tests/SceneBodyDynamicsTests.cs@a6c914e:L311`). Story 6.4 adds the slider.
- A return spring of stiffness 14 N/m at rest 0, with damping 4 N·s/m in both directions, acts on the guide whether or not the gate is powered (`engine/SlidingBlade.cs@a6c914e:L17-L18`, `engine/SlidingBlade.cs@a6c914e:L41-L42`).

### Typed sockets and ports
- One electrical input, PowerIn, at (0, 1.65, 0.92) (`parts/PoweredGatePart.cs@a6c914e:L24-L25`). It exists in the current enum (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L13`) but no gate port table exists yet.
- The activation domain is rejected: a Delay cannot connect to the gate, while a Battery connects electrically (`CuriousContraptions.tests/PoweredGateTests.cs@a6c914e:L118-L128`).
- Tube mouths Start at (−0.49, 0, 0) facing −X and End at (0.49, 0, 0) facing +X, each with the common bore (`parts/PoweredGatePart.cs@a6c914e:L26-L30`; record `engine/TubeMouth.cs@a6c914e:L9`). They snap to pipe mouths (`parts/catalog/powered_gate.tres@a6c914e:L11`).

### Sensors and activation
- None. The gate emits no activation output. When powered it records a `Powered` event (`parts/PoweredGatePart.cs@a6c914e:L62`), which is presentation only.

### Work and energy stores
- No store. Powered drive: target speed = sign(d)·min(2.8, √(2·14·|d|)), where d = 1.55 − opening. Effort is ≤ 60 N, power ≤ 120 W and work ≤ 120 W × step (`engine/SlidingBlade.cs@a6c914e:L15-L20`, `engine/SlidingBlade.cs@a6c914e:L43-L46`). The return spring stores elastic energy ½·14·x².

### Parameters
None. The catalogue declares no `Parameters` (`parts/catalog/powered_gate.tres@a6c914e:L6-L13`), and the requirement row says "declared geometry, typed ports and any source-owned settings; no parameter absence inferred". The fixed declaration constants are:

| Constant | Type | Value | Unit | Source |
| --- | --- | --- | --- | --- |
| stroke | f32 | 1.55 | m | `parts/PoweredGatePart.cs@a6c914e:L15` |
| blade mass | f32 | 1 | kg | `engine/SlidingBlade.cs@a6c914e:L14` |
| maximum speed | f32 | 2.8 | m/s | `engine/SlidingBlade.cs@a6c914e:L15` |
| acceleration (stopping profile) | f32 | 14 | m/s² | `engine/SlidingBlade.cs@a6c914e:L16` |
| return stiffness | f32 | 14 | N/m | `engine/SlidingBlade.cs@a6c914e:L17` |
| damping | f32 | 4 | N·s/m | `engine/SlidingBlade.cs@a6c914e:L18` |
| drive effort | f32 | 60 | N | `engine/SlidingBlade.cs@a6c914e:L19` |
| drive power | f32 | 120 | W | `engine/SlidingBlade.cs@a6c914e:L20` |

### Cosmetic curves and UI bindings
- Indicator: Blocked `#e8b764`, powered `#f7cb52`, otherwise `#556573` (`parts/PoweredGatePart.cs@a6c914e:L57-L63`). DESIGN calls these slate, gold and ochre ([DESIGN.md](../../../DESIGN.md), "Powered gate" row).
- State enum `GateState { Closed, Opening, Open, Closing, Blocked }`, classified from the committed coordinate and speed (`engine/SlidingBlade.cs@a6c914e:L7`, `engine/SlidingBlade.cs@a6c914e:L49-L53`). The blade mesh is a rigid map of the blade body (`parts/PoweredGatePart.cs@a6c914e:L11-L13`). Use a cosmetic binding (`engine/gpu/WorkshopCosmetic.cs@a6c914e:L7-L20`); no gate-state feedback source exists yet.

### Art
- Scene `parts/scenes/powered_gate.tscn` holds only the script (`parts/scenes/powered_gate.tscn@a6c914e:L1-L6`). Build meshes (`parts/PoweredGatePart.cs@a6c914e:L32-L51`):
  - sleeve colour (0.40, 0.72, 0.79, alpha 0.16), cream collars `#fff8e9`, navy housing `#293954`, cream rails;
  - gold blade `#f7cb52`;
  - socket sphere radius 0.09, `#e8b764`, at (0, 1.65, 0.92);
  - indicator radius 0.07 at (0.42, 1.65, 0.55);
  - pick radius 1.5 m.
- Design authority: [DESIGN.md](../../../DESIGN.md) "Powered gate" row: blade retracts along local +Y; one original icon; standard move/rotate gizmo; mouth snapping.
- Toolbox pictogram: `ui/WorkshopIcons.cs@a6c914e:L88`.

### Catalogue and inventory entry
- Id `powered_gate`, title "Powered gate", category Motion, colour (0.97, 0.796, 0.322), description "Electricity retracts the gold shutter. Without power it returns closed, stopping if a ball blocks its path. Its mouths snap to tubes." (`parts/catalog/powered_gate.tres@a6c914e:L8-L13`). No level in `content/puzzles.json` at `a6c914e` places or stocks it.

## 3. Engine capabilities

Capability families: see the [CAT-051 map row](../general-engine-element-map.md) and [catalogue coverage](../../coverage/catalogue-elements.json). They are not duplicated here.

**Exists now**
- Dynamic box blade and static box frame parts: `engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L120`, `engine/gpu/RigidMassProperties.cs@a6c914e:L26-L51`.
- Box contact in the TGS Soft solver, in the simulation worker:
  - `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L230-L236` soft parameters;
  - `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L503` box-box;
  - `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L623` prepareContact;
  - `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L774` normal row;
  - `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L878` sweep.
- Pipe profile constants: `engine/gpu/WorkshopPipe.cs@a6c914e:L6-L20` (parked CAT-048 work).
- Electrical domain and PowerIn socket: `engine/gpu/WorkshopConnections.cs@a6c914e:L8-L13`.

**Missing**
- Slider joint with limits; spring and damping along the slider: Story 6.4.
- Hollow annular sleeve and collar colliders; mouth snapping: Stories 6.6 and 6.7 (CAT-048).
- Electrical supply and powered state: Story 8.1.
- Bounded slider drive with a stopping-speed profile: Story 11.3 builds it for the pusher; Story 8.2 reuses it for the gate.
- Gate port table, part kind and indicator binding: Story 8.2.

**Element dependencies**
- CAT-005 Battery.
- CAT-048 Pipe for mouths and routes.
- Integration partners: CAT-033 Hold timer, CAT-020 Counter, CAT-052 Pressure plate, CAT-016 Cannon (muzzle obstruction) and CAT-071 Wound spring (plunger block). See facts 22–25.

## 4. Legacy harvest

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | Electric retracting shutter; shared rigid-body motion drives the visible blade | `parts/PoweredGatePart.cs@a6c914e:L7` | Carry forward | One-way data flow |
| 2 | Stroke 1.55 m; blade half extents (0.06, 0.72, 0.72); material (0.1, 0.1, 0.3) | `parts/PoweredGatePart.cs@a6c914e:L15`, `parts/PoweredGatePart.cs@a6c914e:L22-L23` | Carry forward | Authored values |
| 3 | PowerIn electrical input at (0, 1.65, 0.92); mouths ±0.49 m with the common bore | `parts/PoweredGatePart.cs@a6c914e:L24-L30` | Carry forward | Typed port and mouths |
| 4 | Frame geometry: sleeve, collars, housing and rails as in section 2 | `parts/PoweredGatePart.cs@a6c914e:L32-L46` | Carry forward | Committed functional geometry |
| 5 | Indicator colours; `Powered` event while powered | `parts/PoweredGatePart.cs@a6c914e:L57-L63` | Carry forward colours; event as presentation only | Per-element observe loop not carried forward |
| 6 | Controller constants: mass 1, maximum speed 2.8, acceleration 14, stiffness 14, damping 4, effort 60, power 120, endpoint tolerance 1e-5 | `engine/SlidingBlade.cs@a6c914e:L14-L21` | Carry forward magnitudes except the tolerance | Tolerance re-frozen in f32 (Open question 3) |
| 7 | Slider declaration: frame rotation (−π/2, 0, 0), range [0, stroke], connected collision disabled | `engine/SlidingBlade.cs@a6c914e:L28-L34` | Carry forward | Declaration data |
| 8 | Each tick: spring and damping loads always; drive only when powered, with the stopping profile and a work budget of power × Δt | `engine/SlidingBlade.cs@a6c914e:L36-L47` | Carry forward the law | The per-element `Prepare` loop is a CPU path |
| 9 | Classification: Closed ≤ 1e-5; Open within 1e-5 of stroke; Blocked when \|speed\| < 1e-5; otherwise Opening if powered, Closing if not | `engine/SlidingBlade.cs@a6c914e:L49-L53` | Carry forward as a typed enum; re-freeze thresholds | f32 |
| 10 | Opening is read from the live guide joint; a detached or missing guide rejects instead of defaulting | `CuriousContraptions.tests/PoweredGateTests.cs@a6c914e:L13-L52` | Carry forward | Explicit rejection |
| 11 | Acceptance: a gate rotated 90° about Z supports a settled ball (y 3.3999–3.4002 after 360 ticks); powering it drops the ball below y 1 within 180 ticks; unpowered it holds; Reset is exact | `CuriousContraptions.tests/PoweredGateTests.cs@a6c914e:L79-L116` | Carry forward the behaviour | Horizontal mounting control |
| 12 | Acceptance: a Delay cannot connect (activation rejected). Under Battery supply the opening rises ≤ 2.8 m/s × tick per tick at speed 0–2.8, reaching Open at exactly 1.55 m within 180 ticks. Reset gives Closed, 0 and 0 and keeps the connection | `CuriousContraptions.tests/PoweredGateTests.cs@a6c914e:L118-L150` | Carry forward | Typed domain; bounded motion |
| 13 | Acceptance: at rotations (0, 0, 0) and (20°, 30°, 40°), a ball at 1 m/s from local x −2 passes to x 1.99–2.01 when powered and stops at x < −0.39 when unpowered | `CuriousContraptions.tests/PoweredGateTests.cs@a6c914e:L152-L177` | Carry forward | Rotated passage |
| 14 | Acceptance: when supply is removed with a ball moving at 0.5 m/s at x −0.25, the closing blade traps it against the lower tube. State Blocked, opening between 0.001 and stroke − 0.001, both speeds ≤ 1e-7, contacts with blade and frame, separation ≥ −1e-6. Without removal there is no contact. Reset replays bit-identically | `CuriousContraptions.tests/PoweredGateTests.cs@a6c914e:L179-L240` | Carry forward the behaviour; not the 1e-6/1e-7 values | Game-grade envelope replaces CPU tolerances |
| 15 | Acceptance: unpowered return follows a spring-damper with energy (KE + ½kx²) never increasing and no drive use; Reset replay is exact, both rotated and excited at 2 m/s | `CuriousContraptions.tests/SlidingBladeDynamicsTests.cs@a6c914e:L24-L83` | Carry forward energy and replay; not the exact substep recurrence | Recurrence is the CPU integrator |
| 16 | Queries (all trace media) use the solved blade: closed blocks at about 1.9–2 m from local x −2; a moved blade clears; restoring blocks again; presentation never rewrites construction | `CuriousContraptions.tests/SlidingBladeOwnershipTests.cs@a6c914e:L25-L92` | Carry forward | Blade is the occluder |
| 17 | Acceptance: the powered blade keeps a stable shape; the rendered pose equals the physics pose within 1e-6; after 180 ticks it moves > 1.2 m and the passage sweep and light trace are clear; Reset restores pose and sweep | `CuriousContraptions.tests/ActuatorBodyGeometryTests.cs@a6c914e:L32-L88` | Carry forward | Committed geometry |
| 18 | Acceptance: an unpowered blade keeps its closed pose for 60 ticks and obstructs the sweep | `CuriousContraptions.tests/ActuatorBodyGeometryTests.cs@a6c914e:L90-L108` | Carry forward | Negative control |
| 19 | Acceptance: exactly one slider joint; per-substep drive work ≤ 120 × h and impulse ≤ 60 × h; travel stays within range; after supply loss there is no drive use and the blade returns to 0 at rest within 240 ticks; replay is identical | `CuriousContraptions.tests/ActuatorBodyGeometryTests.cs@a6c914e:L110-L165` | Carry forward | Finite work; fail-closed |
| 20 | The return spring uses the owned orientation, not presentation; corrupting the scene transform does not change the outcome | `CuriousContraptions.tests/ActuatorBodyGeometryTests.cs@a6c914e:L167-L217` | Carry forward | One-way data flow |
| 21 | The blade is dynamic, mass 1 kg, with positive-definite inertia; velocity = part +Y × opening speed; zero angular velocity; Reset restores dynamics | `CuriousContraptions.tests/SceneBodyDynamicsTests.cs@a6c914e:L286-L321` | Carry forward | Body declaration |
| 22 | Integration: a battery-supplied hold timer powers the gate while counting and drops it on expiry; retrigger repowers it | `CuriousContraptions.tests/HoldTimerTests.cs@a6c914e:L10-L26`, `CuriousContraptions.tests/HoldTimerTests.cs@a6c914e:L59-L100` | Carry forward | Electrical consumer |
| 23 | Integration: a counter powers the gate only at target and only with supply; a pressed, supplied pressure plate opens it; unloading closes it within 180 ticks | `CuriousContraptions.tests/CounterTests.cs@a6c914e:L46-L70`; `CuriousContraptions.tests/PressurePlateTests.cs@a6c914e:L44-L82` | Carry forward | Electrical consumer |
| 24 | Integration: a closed gate in front of a cannon muzzle makes the shot Obstructed with charge retained (45 J); opening it (> 1.1 m in 180 ticks) then a fresh trigger fires | `CuriousContraptions.tests/CannonTests.cs@a6c914e:L452-L498` | Carry forward | Cross-element obstruction |
| 25 | Integration: a timer-driven gate rotated 90° blocks a released wound-spring plunger (spring Blocked, charge retained) | `CuriousContraptions.tests/WoundSpringTests.cs@a6c914e:L457-L501` | Carry forward | Cross-element obstruction |

### Files harvested
- `parts/PoweredGatePart.cs`
- `parts/catalog/powered_gate.tres`
- `parts/scenes/powered_gate.tscn`
- `engine/SlidingBlade.cs`
- `engine/TubeProxy.cs`
- `engine/TubeMouth.cs`
- `engine/physics/PersistentContactPair.cs` (ContactMaterial field order only)
- `reference/cpu/MachinePart.cs` (AddBox semantics only)
- `ui/WorkshopIcons.cs` (pictogram; not deleted by Epic 7)
- `CuriousContraptions.tests/PoweredGateTests.cs`
- `CuriousContraptions.tests/SlidingBladeDynamicsTests.cs`
- `CuriousContraptions.tests/SlidingBladeOwnershipTests.cs`
- `CuriousContraptions.tests/ActuatorBodyGeometryTests.cs`
- `CuriousContraptions.tests/SceneBodyDynamicsTests.cs` (blade section)
- `CuriousContraptions.tests/AxialMotionOwnershipTests.cs` (unpowered moving blade reads Closing, L87)
- `CuriousContraptions.tests/HoldTimerTests.cs` (gate consumer)
- `CuriousContraptions.tests/CounterTests.cs` (gate consumer)
- `CuriousContraptions.tests/PressurePlateTests.cs` (gate consumer)
- `CuriousContraptions.tests/CannonTests.cs` (muzzle gate section)
- `CuriousContraptions.tests/WoundSpringTests.cs` (gate section)
- `CuriousContraptions.tests/BodyLocalBoxTests.cs` (checked, no element-specific knowledge)
- `content/puzzles.json` (checked, no element knowledge)
- `tools/P0-007-actual-path-probe/inputs.json` (checked, no element knowledge: file hashes only)
- `reference/P0-022-before/docs/coverage/engine/capabilities-02.json` (checked, no element knowledge: catalogue id lists only)

## 5. Acceptance outline

This outline points to [CAT-051](../requirements.md#current-cat-051), [todo-244](../requirements.md#todo-244) and Story 8.2 rather than restating them.
- **Chrome UI recipe.**
  1. In actual Chrome, place a Ramp and a Pipe run with the Powered gate snapped between pipe mouths.
  2. Place a Basketball upstream and a Battery.
  3. Wire Battery Supply to the gate's PowerIn with the contextual wiring UI.
  4. Verify the mouth alignment and the typed electrical connection from the committed construction read.
- **Positive.** Supply opens the blade at bounded acceleration and the ball rolls through (facts 12, 13).
- **Negative/control.** Unpowered, the ball stays upstream (fact 13). A Delay link is rejected (fact 12).
- **Boundaries.** Loaded or blocked closing stops at the ball without crushing it (fact 14); supply interruption mid-stroke; clearing and retry; rotated passage (fact 13); horizontal mounting (fact 11).
- **Run/Reset.** Reset closes the blade exactly and keeps connections (fact 12).
- **Save/Load.** A round trip preserves pose, mouths and connection; the replay is identical.
- **Integrations.** Hold timer, counter and pressure plate supplies; cannon muzzle and wound-spring plunger obstruction (facts 22–25), each after its partner element's story.

## 6. Open questions

1. Is the return spring (14 N/m) the declared fail-closed law, or should closing be an explicit actuator? The requirement says "acceleration-limited physical gold blade" but names no spring. Unspecified — owner decision.
2. The legacy gate cites `PipePart.BoreRadius`, which no longer exists at `a6c914e`. Confirm that the gate bore is `PipeDimensions.BoreRadius` (0.65 m). Unspecified — owner decision.
3. Which f32 thresholds replace the 1e-5 m endpoint tolerance and the 1e-5 m/s Blocked speed? Unspecified — owner decision.
4. Blocked classification ignores power: a powered blade jammed while opening reads Blocked. Should there be distinct blocked-opening and blocked-closing states? Unspecified — owner decision.
5. Does the supply draw finite energy from the Battery (CAT-005) or stay a binary enable, as legacy did? Unspecified — owner decision.
6. **Epic story vs requirement conflict.** Story 8.2 opens the gate on "an activation pulse", but CAT-051 is electrical-only and rejects activation links ([CAT-051](../requirements.md#current-cat-051); fact 12). Is the story corrected to electrical supply, or is an activation-to-supply relay intended? Unspecified — owner decision.
