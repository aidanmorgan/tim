# CAT-019 · conveyor declaration readiness spec

This is the Story 7.0 CAT-019-D declaration readiness spec for the conveyor belt. The baseline is commit `a6c914e`. Every citation uses the form `path@a6c914e:Lstart-Lend`, which stays retrievable from git history after the Epic 7 purge. Current-engine files are cited at the same commit.

## 1. Identity

| Field | Value |
| --- | --- |
| CAT ID | CAT-019 |
| Kind | `conveyor`, catalogue title "Conveyor belt" |
| Requirement anchor | [CAT-019](../requirements.md#current-cat-019); retained behaviour [todo-151](../requirements.md#todo-151) |
| Mapped identities | [EL-202 Conveyor](../invest/named-elements.md#element-202); [EL-200 Drive belt](../invest/named-elements.md#element-200) is a candidate related identity for the authored belt link (candidate — Batch A confirms) |
| Roadmap story | [Story 11.1](../../../_bmad-output/planning-artifacts/epics.md) Electric Motor & Continuous Tangential Conveyor Belt (CAT-042, CAT-019) |
| Status | not started (no `WorkshopPartKind` member, `engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`) |

## 2. Declaration

### Bodies and shapes
- **Carrier (static, part root).** A box of length × 0.24 m × width, centred at the origin, so the belt top sits at local y = +0.12 m. Source: `parts/ConveyorPart.cs@a6c914e:L84-L90`; payloads are seated at 0.12 m + radius in `CuriousContraptions.tests/ConveyorRuntimeTests.cs@a6c914e:L35-L63`. Use a static `RigidBodyDeclaration` and a `ColliderDeclaration` box (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L120`).
- **Driven roller (dynamic shaft body).** Radius 0.16 m, axial length width + 0.1 m, centred at (−length/2 + 0.15, −0.07, 0), collider a 24-sided convex prism. Source: `parts/ConveyorPart.cs@a6c914e:L20-L24`, `parts/ConveyorPart.cs@a6c914e:L93-L102`. Cylinders are not an admitted `ColliderShapeKind` (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L7-L7`).
- **Idle roller.** At (+length/2 − 0.15, −0.07, 0); art only, with no physical body (`parts/ConveyorPart.cs@a6c914e:L103-L110`).

### Mass and material
- Roller mass 0.5 kg. Axial inertia m·r²/2. Transverse inertia m·(3r² + (width + 0.1)²)/12. Source: `parts/ConveyorPart.cs@a6c914e:L30-L39`. Cylinder inertia is missing from `RigidMassProperties` (`engine/gpu/RigidMassProperties.cs@a6c914e:L17-L50`).
- Belt contact material: restitution 0.05, bounce threshold 0.1 m/s, friction 0.3 (`parts/ConveyorPart.cs@a6c914e:L72-L72`; constructor order in `engine/physics/PersistentContactPair.cs@a6c914e:L18-L29`). Declare it as a `ContactMaterialDeclaration` (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L54`).

### Constraints and joints
- A free revolute hinge about local +Z joins the roller to the carrier. Collision between the connected bodies is disabled, and travel is unbounded both ways (`parts/ConveyorPart.cs@a6c914e:L40-L45`).
- **Driven surface.** The carrier face with local normal +Y moves along local +X at travel-per-radian = −1 × `surface_per_radian` times the roller's relative hinge rate (`parts/ConveyorPart.cs@a6c914e:L46-L49`; law in `engine/physics/DrivenSurface.cs@a6c914e:L17-L19`). Admission: hinge only, a dynamic shaft, orthonormal unit face axes, and a finite nonzero ratio (`engine/physics/DrivenSurface.cs@a6c914e:L21-L31`). Only contacts whose outward normal matches the face normal within 1 − 1e-8 are driven (`engine/physics/DrivenSurface.cs@a6c914e:L32-L36`). One face cannot carry two competing drives (`engine/physics/PhysicsWorld.cs@a6c914e:L409-L413`).
- No joint or driven-surface declaration exists in `engine/gpu/*` yet.

### Typed sockets and ports
| Socket | Domain | Direction | Local position (m) | Source |
| --- | --- | --- | --- | --- |
| `DriveIn` | Mechanical | Input | (−L/2 + 0.15, −0.07, W/2 + 0.18) | `parts/ConveyorPart.cs@a6c914e:L64-L70` |
| `Drive` | Mechanical | Output | (L/2 − 0.15, −0.07, W/2 + 0.18) | `parts/ConveyorPart.cs@a6c914e:L64-L70` |

- Both sockets bind to the same roller hinge with −1 coordinate per radian, so output relays the input shaft (`parts/ConveyorPart.cs@a6c914e:L71-L71`; binding type `engine/ConnectionPort.cs@a6c914e:L23-L38`).
- Link coupling ratio = input coordinate-per-radian ÷ output coordinate-per-radian (`engine/MechanicalNetwork.cs@a6c914e:L46-L49`). One authored belt per input; one output may fan out (`engine/MechanicalNetwork.cs@a6c914e:L28-L38`, `CuriousContraptions.tests/MechanicalTests.cs@a6c914e:L185-L211`).
- There is no electrical or activation port. Current `WorkshopConnections` has no `Mechanical` domain (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L13`).

### Sensors and activation
- There is no activation input. An activation command is not mechanical energy (`CuriousContraptions.tests/MechanicalTests.cs@a6c914e:L145-L183`).
- Legacy `Transported` event: for each driven contact with |surface speed| > 1e-6, a dynamic owned receiver whose point velocity along the belt direction has the surface's sign is marked transported (`parts/ConveyorPart.cs@a6c914e:L140-L149`). No goal kind consumes it (`engine/MachineData.cs@a6c914e:L152-L152`).

### Work and energy stores
- None of its own. Work arrives only through the coupled roller shaft from an upstream motor; the belt is "no copied-speed work source" ([CAT-019 row](../requirements.md#current-cat-019)). Stored energy is the roller's finite kinetic energy, which coasts after supply loss (`CuriousContraptions.tests/ConveyorRuntimeTests.cs@a6c914e:L114-L135`).

### Parameters
| Name | Type | Range | Default | Unit | Source |
| --- | --- | --- | --- | --- | --- |
| `length` | f32 | finite, > 0 (no legacy upper bound) | 3.0 | m | `parts/ConveyorPart.cs@a6c914e:L74-L82`, `parts/catalog/conveyor.tres@a6c914e:L14-L14` |
| `width` | f32 | finite, > 0 (no legacy upper bound) | 1.2 | m | same |
| `surface_per_radian` | f32 | finite, ≠ 0, signed | 0.666666667 | m of belt travel per rad of roller | same |

The keys are a closed enum `ConveyorParameter { Length, Width, SurfacePerRadian }` (`parts/ConveyorPart.cs@a6c914e:L9-L9`). The retired properties `powered`, `speed` and `traction` are rejected (`CuriousContraptions.tests/MechanicalTests.cs@a6c914e:L213-L226`, `CuriousContraptions.tests/MechanicalWorkTests.cs@a6c914e:L148-L159`).

### Cosmetic curves and UI bindings
- **Treads.** Ten boxes 0.045 × 0.015 × 0.9·W m at y = 0.13, colour `#88b7a9`. Phase = roller winding × ratio, modulo L/10 (`parts/ConveyorPart.cs@a6c914e:L120-L122`, `parts/ConveyorPart.cs@a6c914e:L136-L139`).
- **Port pulleys.** Cream (`#fff8e9`) wheels, radius 0.22 m and width 0.1 m, with gold `#f7cb52` spokes. They rotate with −(shaft angle) (`parts/ConveyorPart.cs@a6c914e:L111-L119`, `parts/ConveyorPart.cs@a6c914e:L134-L134`).
- **Direction arrow.** Lines in `#f2d78c` flip 180° when surface speed is negative, only while |shaft speed| > 1e-6 (`parts/ConveyorPart.cs@a6c914e:L123-L127`, `parts/ConveyorPart.cs@a6c914e:L133-L135`).
- **Belt link artwork.** Four cylinder lines and two marks follow socket transforms. The marks move only while running, at shaft speed × 0.16 (`CuriousContraptions.tests/MechanicalTests.cs@a6c914e:L17-L60`). The legacy visual `ui/MechanicalBeltVisual.cs` was already deleted before `a6c914e`. All of these need committed-shaft cosmetic bindings, and the current `AnimationFeedbackSource` lacks them (`engine/gpu/WorkshopCosmetic.cs@a6c914e:L7-L16`).
- The pick radius is 0.85 m (`parts/ConveyorPart.cs@a6c914e:L89-L89`).

### Art
- The scene only attaches the script (`parts/scenes/conveyor.tscn@a6c914e:L1-L7`).
- Carrier `#273744`; side rails L × 0.16 × 0.08 m at z = ±W/2 in the catalogue colour; supports 0.12 × 0.6 × 0.8·W m in `#546876` (`parts/ConveyorPart.cs@a6c914e:L90-L92`, `parts/ConveyorPart.cs@a6c914e:L109-L109`).
- Catalogue colour Conveyor `#d69c47`, i.e. (0.84, 0.61, 0.28) (`parts/catalog/conveyor.tres@a6c914e:L13-L13`; [DESIGN colour system](../../../DESIGN.md#colour-system); [DESIGN mechanical work feedback](../../../DESIGN.md#mechanical-work-feedback)).
- Toolbox pictogram SVG path: `ui/WorkshopIcons.cs@a6c914e:L99-L99`.

### Catalogue and inventory entry
- Id `conveyor`, title "Conveyor belt", category Motion. The description tells the player to connect a motor, windmill or another conveyor to the input pulley; the output passes rotation onward, and a reverse transmission reverses travel (`parts/catalog/conveyor.tres@a6c914e:L8-L14`).
- Add `WorkshopPartKind.Conveyor` with a counted allowance (`engine/gpu/WorkshopInventory.cs@a6c914e:L9-L23`). It is in inventory in 9 levels and placed in `belt_relay` and `double_cold_start` ([current consumers](../invest/current-consumers.md#cat-019-i)).

## 3. Engine capabilities

The capability families are those in the [CAT-019 map row](../general-engine-element-map.md) and [catalogue-elements.json](../../coverage/catalogue-elements.json); they are not repeated here.

**Exists now**
- Static carrier box, dynamic bodies, contact materials: `engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L120`.
- TGS Soft contact with Coulomb friction rows: `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L230-L236` (soft parameters), `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L623-L656` (contact preparation), `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L702-L719` (friction).
- Counted inventory and save codec: `engine/gpu/WorkshopInventory.cs@a6c914e:L9-L23`, `engine/gpu/WorkshopSaveCodec.cs@a6c914e:L6-L10`.

**Missing**
- Free hinge for the roller: Story 10.3 builds the hinge; Story 11.1 uses it.
- A driven-surface friction law that couples face contacts to roller angular momentum (shaft reaction): Story 11.1.
- The `Mechanical` domain, `Drive`/`DriveIn` sockets and +1/−1 shaft coupling between parts: Story 11.1, with −1 reversal in Story 11.2.
- Cylinder collider and inertia for the roller: Story 10.1 (ENGINE-CYLINDER).
- Tread, pulley, arrow and belt-link cosmetic bindings to committed shaft state: Story 11.1.

**Element dependencies.** The conveyor needs CAT-042 Motor (Story 11.1) as its drive source and, through it, CAT-005 Battery (Story 8.1). It is downstream of CAT-057 Reverse transmission and CAT-018 Clutch (Story 11.2). CAT-070 Windmill (Story 12.3) is a later source. The payloads are CAT-001 Basketball and CAT-004 Receiver.

## 4. Legacy harvest

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | Carrier box L × 0.24 × W; top at +0.12 m | `parts/ConveyorPart.cs@a6c914e:L84-L90` | carry forward | Declaration geometry. |
| 2 | Roller 0.5 kg, r 0.16 m, length W + 0.1 m, at (−L/2 + 0.15, −0.07, 0); cylinder inertia | `parts/ConveyorPart.cs@a6c914e:L20-L39` | carry forward | Finite-inertia driven surface the requirement demands. |
| 3 | Free hinge about +Z; connected collision disabled | `parts/ConveyorPart.cs@a6c914e:L40-L45` | carry forward | Generic hinge declaration. |
| 4 | Driven face normal +Y, direction +X, travel per radian −`surface_per_radian` | `parts/ConveyorPart.cs@a6c914e:L46-L49` | carry forward | Signed port/hinge convention. |
| 5 | Surface speed = ratio × axis·(ω_shaft − ω_carrier); face admission rules | `engine/physics/DrivenSurface.cs@a6c914e:L9-L39` | carry forward (law), do not carry forward (CPU class) | The law becomes worker declaration data. |
| 6 | Tangent rows gain shaft/carrier angular terms; no copied speed | `engine/physics/MaterialContact.cs@a6c914e:L37-L70` | carry forward (behaviour), do not carry forward (CPU solver path) | The TGS worker must implement it generically. |
| 7 | Competing drives on one face rejected | `engine/physics/PhysicsWorld.cs@a6c914e:L409-L413` | carry forward | Admission rule. |
| 8 | Sockets `DriveIn`/`Drive` at the roller ends, offset W/2 + 0.18; both bound −1 to one hinge | `parts/ConveyorPart.cs@a6c914e:L64-L71` | carry forward | Port layout and relay. |
| 9 | One belt per input; fan-out allowed; wrong domain or socket rejected; invalid load rejects atomically | `engine/MechanicalNetwork.cs@a6c914e:L15-L45`, `CuriousContraptions.tests/MechanicalTests.cs@a6c914e:L185-L211` | carry forward | Typed topology. |
| 10 | Loop policy conflict: one test rejects `a→b, b→a` before a source exists; a later test accepts both links and reports zero speed | `CuriousContraptions.tests/MechanicalTests.cs@a6c914e:L185-L211`, `CuriousContraptions.tests/MechanicalWorkTests.cs@a6c914e:L98-L114` | do not carry forward | The legacy contradicts itself; open question 2. |
| 11 | Material restitution 0.05, bounce 0.1 m/s, friction 0.3 | `parts/ConveyorPart.cs@a6c914e:L72-L72` | carry forward | Authored material. |
| 12 | Parameter validation: L, W finite > 0; ratio finite ≠ 0; defaults 3, 1.2, 2/3 | `parts/ConveyorPart.cs@a6c914e:L74-L82`, `parts/catalog/conveyor.tres@a6c914e:L14-L14`, `CuriousContraptions.tests/MechanicalWorkTests.cs@a6c914e:L116-L130` | carry forward | Rejection boundaries. |
| 13 | Retired properties `powered`, `speed` and `traction` rejected | `CuriousContraptions.tests/MechanicalTests.cs@a6c914e:L213-L226`, `CuriousContraptions.tests/MechanicalWorkTests.cs@a6c914e:L148-L159` | carry forward (rejection) | No self-powered belt; strings only at the serialization boundary. |
| 14 | Accepted: steady roller 6 rad/s and surface 4 m/s with the default motor and ratio; the downstream conveyor matches on the same substep; reversers flip the sign | `CuriousContraptions.tests/MechanicalTests.cs@a6c914e:L62-L143` | carry forward | Positive and chained behaviour. |
| 15 | Accepted: after supply loss the unloaded belt coasts at constant speed and kinetic energy (1e-8) without new work; Reset zeroes angles | `CuriousContraptions.tests/MechanicalTests.cs@a6c914e:L62-L143` | carry forward (re-freeze tolerances under f32) | Coasting and Reset. |
| 16 | Accepted: disconnected conveyors stay at 0; disconnecting during Run is refused until Reset | `CuriousContraptions.tests/MechanicalTests.cs@a6c914e:L145-L183` | carry forward | Disconnected-drive control. |
| 17 | Accepted: ball transported along local +X at yaw 0° and 90° (> 1 m in 180 ticks) | `CuriousContraptions.tests/ConveyorTests.cs@a6c914e:L30-L55` | carry forward | Rotated control. |
| 18 | Accepted: unpowered belt holds the ball (x stays −0.8 for 180 ticks) until the switch supplies the motor, then x > 0.5 | `CuriousContraptions.tests/ConveyorTests.cs@a6c914e:L57-L83` | carry forward | Negative then positive. |
| 19 | Accepted: a ball at z = 2 (outside W = 1.2) is not transported | `CuriousContraptions.tests/ConveyorTests.cs@a6c914e:L85-L106` | carry forward | Finite-width boundary. |
| 20 | Accepted: Unpowered/Forward/Reverse/Serial × plain/90°: travel > 0.02 m in 60 ticks with the mode's sign; unpowered gives zero travel and work; construction restores exactly | `CuriousContraptions.tests/ConveyorRuntimeTests.cs@a6c914e:L65-L112` | carry forward | Mode matrix and Reset. |
| 21 | Accepted: two payloads both advance > 0.01 m; motor work ≤ 120 W × 60 ticks; total kinetic energy ≤ work + 0.01 J | `CuriousContraptions.tests/ConveyorRuntimeTests.cs@a6c914e:L137-L155` | carry forward | Loaded, energy-bounded transport. |
| 22 | Accepted: only the authored top face drives; side and underside contacts are ordinary | `CuriousContraptions.tests/DrivenSurfaceWorldTests.cs@a6c914e:L75-L97` | carry forward | Requirement: side/underside stay ordinary. |
| 23 | Accepted: stored roller energy transfers to the payload without gain; sustained slip ends without reversal | `CuriousContraptions.tests/DrivenSurfaceWorldTests.cs@a6c914e:L29-L73` | carry forward | Energy bound. |
| 24 | Accepted: ratio ±1, 2 and reversed pair order all conserve the coupled momentum; a dynamic carrier gets the reaction; rotation invariant | `CuriousContraptions.tests/DrivenContactTests.cs@a6c914e:L28-L48`, `CuriousContraptions.tests/DrivenContactTests.cs@a6c914e:L108-L131` | carry forward | Signed ratio and reaction. |
| 25 | Accepted: one circular friction budget; non-loaded or separating contacts are never driven; two loads share one shaft | `CuriousContraptions.tests/DrivenContactTests.cs@a6c914e:L50-L106` | carry forward | Load-aware frictional transport. |
| 26 | Bounded drive never overdraws work or impulse in signed cases | `CuriousContraptions.tests/SurfaceMotorBudgetTests.cs@a6c914e:L15-L46` | carry forward (behaviour), do not carry forward (CPU impulse helper) | Covered by the motor row. |
| 27 | Tread phase, pulley spin, arrow flip, belt marks only while running | `parts/ConveyorPart.cs@a6c914e:L130-L139`, `CuriousContraptions.tests/MechanicalTests.cs@a6c914e:L17-L60` | carry forward (bindings), do not carry forward (per-part `ObservePhysics` loop) | Animation is declared bindings. |
| 28 | `Transported` event rule | `parts/ConveyorPart.cs@a6c914e:L140-L149` | carry forward | Observable witness; no goal uses it (open question 6). |
| 29 | `conveyor_courier`: ball (−3, 5, 0), receiver (2, 0.9, 0), locked battery and motor; solution conveyor at (−2, 2.5, 0); Supply→PowerIn, Drive→DriveIn | `content/puzzles.json@a6c914e:L3875-L3882`, `content/puzzles.json@a6c914e:L4145-L4207`, `content/puzzles.json@a6c914e:L4209-L4224` | carry forward | First lesson. |
| 30 | `belt_relay`: locked relay conveyor at (1, 4.5, 0) drives a placed conveyor | `content/puzzles.json@a6c914e:L4488-L4550`, `content/puzzles.json@a6c914e:L4624-L4646` | carry forward | Chained conveyor. |
| 31 | `reverse_belt`: motor → reverse transmission → conveyor; a direct belt bypass must fail | `content/puzzles.json@a6c914e:L4983-L5045`, `content/puzzles.json@a6c914e:L5047-L5069`, `CuriousContraptions.tests/MechanicalTests.cs@a6c914e:L228-L272` | carry forward | Reversed ratio and negative control. |
| 32 | Lessons need every drive link; removing any one fails at precision 0, 0.45 and 1 | `CuriousContraptions.tests/MechanicalTests.cs@a6c914e:L228-L272` | carry forward | Level integration. |
| 33 | `double_cold_start`: locked conveyor started by a switched supply; `depth_delivery` conveyor yawed 90° | `content/puzzles.json@a6c914e:L21344-L21406`, `content/puzzles.json@a6c914e:L25866-L25928` | carry forward | Gated start and rotated fixture. |
| 34 | Campaign modules `conveyor`, `conveyor_signal` (receiver → switch → lamp) and `gated_belt` (switch-gated supply, `powered_after`) | `tools/Campaign/Program.cs@a6c914e:L34-L63`, `tools/Campaign/Program.cs@a6c914e:L76-L91` | carry forward | Lesson authoring set-ups for Epic 15. |

### Files harvested
- `parts/ConveyorPart.cs`
- `parts/catalog/conveyor.tres`
- `parts/scenes/conveyor.tscn` (script attachment only)
- `engine/SceneDrivenSurface.cs`
- `engine/physics/DrivenSurface.cs`
- `engine/physics/MaterialContact.cs`
- `engine/physics/PhysicsWorld.cs`
- `engine/physics/PersistentContactPair.cs`
- `engine/MechanicalNetwork.cs`
- `engine/ConnectionPort.cs`
- `engine/MachineData.cs`
- `CuriousContraptions.tests/ConveyorTests.cs`
- `CuriousContraptions.tests/ConveyorRuntimeTests.cs`
- `CuriousContraptions.tests/DrivenContactTests.cs`
- `CuriousContraptions.tests/DrivenSurfaceWorldTests.cs`
- `CuriousContraptions.tests/SurfaceMotorBudgetTests.cs`
- `CuriousContraptions.tests/MechanicalTests.cs`
- `CuriousContraptions.tests/MechanicalWorkTests.cs`
- `content/puzzles.json` (levels `conveyor_courier`, `belt_relay`, `reverse_belt`, `double_cold_start`, `depth_delivery`)
- `tools/Campaign/Program.cs`
- `ui/WorkshopIcons.cs` (pictogram)
- `engine/physics/ContactSlipPath.cs` (checked, CPU slip integration of driven surfaces, no element values)
- `reference/p054-guide/puzzles-candidate.json` (checked, duplicate candidate copy of the levels)
- `reference/P0-022-before/docs/coverage/engine/capabilities-02.json` (checked, coverage snapshot, no element knowledge)

## 5. Acceptance outline

Follow the [CAT-019 row](../requirements.md#current-cat-019) and Story 11.1; do not restate them.
- **Chrome UI recipe.** Open `conveyor_courier`. Drag the conveyor from the toolbox under the ball with the real placement controls. Connect battery `Supply` → motor `PowerIn` and motor `Drive` → conveyor `DriveIn` with the contextual wiring UI. Run, then record the captured goal, roller and payload poses, and bundle identity.
- **Positive.** The payload is carried along local +X into the receiver; treads, pulleys and arrow follow committed travel (facts 14, 17, 27).
- **Negative/control.** An unpowered motor or missing belt link gives no motion; a wire from the activation domain is rejected; a ball outside the belt width is untouched (facts 16, 18, 19).
- **Boundaries.** Stalled or loaded belt, coasting after supply loss, reversed ratio, downstream chained conveyor (`belt_relay`), rotated belt (`depth_delivery`), side and underside contacts (facts 15, 20–22, 30, 33).
- **Run/Reset.** Roller angle, speed, tread phase and payload return exactly (fact 20).
- **Save/Load.** `length`, `width`, `surface_per_radian`, pose and typed mechanical links round-trip; retired properties are rejected (fact 13).
- **Integrations.** Motor (CAT-042), Reverse transmission and Clutch (Story 11.2), Basketball/Receiver capture.

## 6. Open questions

1. Upper bounds, UI control and f32 admission ranges for `length`, `width` and `surface_per_radian` (legacy has only positivity or nonzero checks): unspecified — owner decision.
2. Mechanical loop policy: reject a source-free `a→b, b→a` loop, or admit it with zero motion (fact 10): unspecified — owner decision.
3. Roller collider primitive under the current Sphere/Box/Plane set, and whether the idle roller stays art-only: unspecified — owner decision.
4. Whether the 24-gon roller's own contact with payloads should also be a driven contact or only the declared top face: unspecified — owner decision.
5. How the worker realises the driven face (shaft-coupled tangent rows versus another TGS formulation), given "no copied-speed work source": Story 11.1 design decision for the owner to approve.
6. Whether the `Transported` witness event is retained when no goal consumes it: unspecified — owner decision.
7. Cylinder collider and cylinder inertia for the roller: Story 10.1 (ENGINE-CYLINDER), scheduled before the first shaft wheel by the owner on 9 Oct 2026.
8. **Epic story vs requirement conflict.** Story 11.1 says cutting motor power "decelerates the belt to a halt under friction", but CAT-019 requires a coasting case and CAT-042 says supply loss does not erase momentum ([CAT-019](../requirements.md#current-cat-019), [CAT-042](../requirements.md#current-cat-042)); the legacy belt coasts at constant speed (fact 15). Which governs, and which explicit friction law, if any, stops the belt? Unspecified — owner decision.
