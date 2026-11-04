# CAT-042 · motor declaration readiness spec

**Current delivery (24 October 2026):** [CAT-042 delivery owner](../../../_bmad-output/planning-artifacts/epics.md#catalogue-slice-ownership). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

This is the Story 7.0 CAT-042-D declaration readiness spec for the electric motor. The baseline is commit `a6c914e`. Every citation uses the form `path@a6c914e:Lstart-Lend`, which stays retrievable from git history after the Epic 7 purge. Current-engine files are cited at the same commit.

## 1. Identity

| Field | Value |
| --- | --- |
| CAT ID | CAT-042 |
| Kind | `motor`, catalogue title "Electric motor" |
| Requirement anchor | [CAT-042](../requirements.md#current-cat-042); retained behaviour [todo-146](../requirements.md#todo-146) |
| Mapped identities | [EL-199 Electric motor](../invest/named-elements.md#element-199) |
| Roadmap story | [11.1a Motor](../../../_bmad-output/planning-artifacts/epics.md#catalogue-slice-ownership); retain every CAT-042 mode and mapped named-identity obligation. |
| Harvest-baseline status | not started (no `WorkshopPartKind` member, `engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`) |

## 2. Declaration

### Bodies and shapes
- **Housing (static, part root).** Base box 1.25 × 0.16 × 1 m at local (0, −0.43, 0); body box 1 × 0.8 × 0.85 m at the origin; housing cylinder radius 0.48 m, length 0.86 m, axis rotated to local Z. Source: `parts/MotorPart.cs@a6c914e:L64-L71`. Use a static `RigidBodyDeclaration` with `ColliderDeclaration` boxes (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L120`).
- **Rotor (dynamic shaft body).** Disc radius 0.3 m, width 0.13 m, centred at local (0, 0, 0.54) m, collider a 24-sided convex prism. Source: `parts/MotorPart.cs@a6c914e:L24-L27`, `parts/MotorPart.cs@a6c914e:L73-L82`. The current `ColliderShapeKind` admits only Sphere, Box and Plane (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L7-L7`). The cylinder collider is scheduled in Story 10.1 (ENGINE-CYLINDER); the rotor primitive itself is open question 2.
- **As shipped (Story 11.1a, `elements/Motor/`).** The rotor is a true cylinder collider (r 0.3 m, height 0.13 m) turned onto the hinge's +Z, its own body under `Visual/Rotor` at (0, 0, 0.54). The housing is static: the base box, the body box, the housing cylinder turned onto Z, the gold PowerIn stud (sphere r 0.1 m at (−0.55, 0, 0)) and the supply lamp (sphere r 0.075 m at (0.32, 0.28, 0.45)); the stud and lamp are drawn, so they are solid. The gold index bar (0.48 × 0.07 m) is printed flush on the rotor face, 4 mm thick, and has no collider.

### Mass and material
- Rotor mass 0.5 kg. Axial inertia m·r²/2 = 0.0225 kg m². Transverse inertia m·(3r² + w²)/12, about 0.01195 kg m². Source: `parts/MotorPart.cs@a6c914e:L19-L26`, with the generic formula in `engine/SceneRotaryShaft.cs@a6c914e:L13-L24`. `RigidMassProperties.Compile` handles only spheres and boxes (`engine/gpu/RigidMassProperties.cs@a6c914e:L17-L50`), so cylinder inertia is needed; it is scheduled in Story 10.1 (ENGINE-CYLINDER).
- The rotor inherits the default part contact material (restitution 1, bounce threshold 0.1 m/s, friction 0.3), recorded at `engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L109-L110`. Declare it as a `ContactMaterialDeclaration` (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L54`). Open question 3.
- **As shipped.** The rotor's declared mass frame is constant: 0.5 kg, moments (0.01195, 0.0225, 0.01195) kg·m² in its cylinder's axes, with the principal frame Rx(90°), the cylinder's own turn, which lays the axial moment on the hinge's +Z. Both bodies take the catalogue's rigid-part contact material: restitution 0, friction 0.3, bounce threshold 0.1 m/s (decision 3).

### Constraints and joints
- One revolute hinge about local +Z at the rotor centre joins the rotor to the housing. Travel is unbounded in both directions, and collision between the connected bodies is disabled. Source: `parts/MotorPart.cs@a6c914e:L30-L35`.
- The hinge drive is a bounded motor row; see Work and energy stores.
- **Generic capabilities the Motor declares (engine slice `engine-revolute-drive`).** The Motor adds no motor code; its package declares one `RevoluteHingeDescriptor` that uses these generic hinge capabilities:
  - `Travel: RevoluteTravel.Continuous` with limits exactly [−π, π] and no axial spring. The stop rows stay inert, so the shaft turns without end.
  - `Drive: RevoluteDriveDescriptor(TargetSpeed, MaxTorque)`, bound to the `speed` and `torque` parameters. This is one velocity-level row on the hinge axis. It drives the rotor's relative spin toward ω_t, and its accumulated impulse is clamped to ±τ_d·h while supplied and to [0, 0] otherwise. The same expression bounds solver and admission.
  - Supply through the owner's `PowerIn` port. The compiler names the hinge's drive as a typed `SupplyConsumer.Revolute` on the Motor's consumer node; the bundle validator refuses a driven hinge without an electrical PowerIn input.
  - **Committed state per hinge:** the supplied flag, the signed travel (the sum of ω_rel·h) and the positive drive work Σ max(0, λ_drive·ω_rel). Run and Reset zero all three. Read them with `PhysicsGpuAbi.ReadRevoluteState`.
  - **Effects:** a stalled or braking drive adds no work, and an unsupplied rotor coasts, slowed only by any declared `FrictionTorque`. Battery drain is deferred to its own slice ([deferred work](../../../_bmad-output/implementation-artifacts/deferred-work.md#powered-part-demand-drain)), and so is a run-time adjustable target.

### Typed sockets and ports
| Socket | Domain | Direction | Local position (m) | Source |
| --- | --- | --- | --- | --- |
| `PowerIn` | Electrical | Input | (−0.55, 0, 0) | `parts/MotorPart.cs@a6c914e:L49-L53` |
| `Drive` | Mechanical | Output | (0, 0, 0.65) | `parts/MotorPart.cs@a6c914e:L49-L53` |

- `Drive` binds to the shaft hinge with −1 coordinate per radian. Mechanical sockets are clockwise-positive viewed from +Z, so they are negative about the hinge's +Z axis. Source: `parts/MotorPart.cs@a6c914e:L54-L54`, `parts/MotorPart.cs@a6c914e:L96-L97`.
- `WorkshopSocket.PowerIn` and the `Electrical` domain exist now (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L13`). The `Mechanical` domain and the `Drive` socket do not; Story 11.1 adds them.

### Sensors and activation
- There is no activation input. The motor is driven only by electrical supply on `PowerIn` (`parts/MotorPart.cs@a6c914e:L91-L99`). An activation command does not create supply (`CuriousContraptions.tests/ElectricalTests.cs@a6c914e:L96-L135`).
- Legacy events: `Powered` while supplied (`parts/MotorPart.cs@a6c914e:L98-L98`). `Turned` fires once cumulative absolute shaft travel reaches 2π (`parts/MotorPart.cs@a6c914e:L100-L103`). These feed the goal kinds `turned` and `powered_after` (`engine/MachineData.cs@a6c914e:L152-L152`, `engine/MachineEvent.cs@a6c914e:L5-L5`).

### Work and energy stores
- Each tick's motor supply is the target shaft speed (= the speed input), the maximum effort (= torque while supplied, else 0), the available work (torque × speed × Δt) and the maximum power (torque × speed). Source: `parts/MotorPart.cs@a6c914e:L91-L97`.
- Legacy only: zero effort disabled actuation, and zero work permitted only dissipative braking, so an unsupplied rotor coasted freely (`engine/physics/PhysicsMotorCommand.cs@a6c914e:L5-L22`). **As shipped:** an unsupplied drive's impulse is clamped to [0, 0], so it neither drives nor brakes; only the declared 0.05 N·m bearing friction slows the rotor (open questions 6 and 9).
- Supplied work is cumulative and committed. It reads back without callbacks and resets to 0 on Reset (`parts/MotorPart.cs@a6c914e:L28-L29`, `CuriousContraptions.tests/MotorAccountingOwnershipTests.cs@a6c914e:L28-L76`).
- Derivation, to be confirmed in 11.1a (open question 6): the generic drive row would meet the legacy allowance and power cap through its bound alone. Each substep's impulse is at most τ_d·h, so it delivers at most τ_d·|ω_t| of power and τ_d·ω·Δt of work, and nothing at stall, with no separate work budget. The committed work is the hinge's positive drive work (Constraints and joints).

### Parameters
| Name | Type | Range | Default | Unit | Source |
| --- | --- | --- | --- | --- | --- |
| `speed` | f32 | 0 to 20 inclusive | 6 | rad/s target shaft speed (`ParameterUnit.RadianPerSecond`) | `parts/MotorPart.cs@a6c914e:L55-L63`, `parts/catalog/motor.tres@a6c914e:L14-L14` |
| `torque` | f32 | 0.1 to 100 inclusive (decision 1; legacy > 0 to 100) | 20 | N·m maximum effort (`ParameterUnit.NewtonMetre`) | `parts/MotorPart.cs@a6c914e:L55-L63`, `parts/catalog/motor.tres@a6c914e:L14-L14` |

The parameter keys are a closed enum `MotorParameter { Speed, Torque }` (`parts/MotorPart.cs@a6c914e:L9-L9`). The legacy also exposes `speed` as a runtime scalar input over the same 0–20 range (`parts/MotorPart.cs@a6c914e:L15-L18`). Open question 5.

### Cosmetic curves and UI bindings
- **Supply lamp.** A sphere of radius 0.075 m at (0.32, 0.28, 0.45) blends from slate `#556573` to gold `#f7cb52` over 0.1 s with a SmoothStep curve, driven by owner activity (`parts/MotorPart.cs@a6c914e:L40-L45`, `parts/MotorPart.cs@a6c914e:L89-L89`). The current `AnimationFeedbackSource` has no supply source (`engine/gpu/WorkshopCosmetic.cs@a6c914e:L7-L16`), so Story 11.1 adds one.
- **As shipped.** The lamp's three colour bindings use `CommittedActuatorDrive` with a 0.1 s SmoothStep. That driver reads the committed actuator read, which now has one row per drive slot (prismatic slots, then revolute slots), keyed by the typed `SupplyConsumer`; its supplied value is the very supply input of the committed tick, the same expression for every slot. A revolute row reads its committed travel and relative spin and never reports moving contact.
- **Rotor and index.** The rotor wheel art follows the committed shaft pose. A decorative gold index bar (0.48 × 0.07 × 0.04 m) spins at the accepted shaft rate and is never a physics target (`parts/MotorPart.cs@a6c914e:L36-L39`, `parts/MotorPart.cs@a6c914e:L83-L88`).
- The pick radius is 0.9 m (`parts/MotorPart.cs@a6c914e:L67-L67`).

### Art
- The scene `parts/scenes/motor.tscn` only attaches the script (`parts/scenes/motor.tscn@a6c914e:L1-L6`). All geometry is procedural, as listed above.
- Palette: catalogue colour Electric motor `#66b8c9`, i.e. (0.40, 0.72, 0.79) (`parts/catalog/motor.tres@a6c914e:L13-L13`; [DESIGN colour system](../../../DESIGN.md#colour-system)). Navy base `#293954`, cream wheel `#fff8e9`, gold `#f7cb52` power stud and index (`parts/MotorPart.cs@a6c914e:L68-L89`). [DESIGN mechanical work feedback](../../../DESIGN.md#mechanical-work-feedback) governs the motion presentation.
- Toolbox pictogram SVG path: `ui/WorkshopIcons.cs@a6c914e:L48-L48`.

### Catalogue and inventory entry
- Id `motor`, title "Electric motor", category Power, description "Electricity turns its shaft. The drive socket is for mechanical connections." (`parts/catalog/motor.tres@a6c914e:L8-L14`). As shipped in Story 11.1a the description is "Electricity turns its shaft."; Story 11.1b adds the drive socket sentence with the socket. The supported domains are Mechanical (the moving rotor, as every package with a moving body declares) and Electrical.
- Declare the package with a counted allowance keyed by its bundle-issued typed id (`engine/gpu/WorkshopInventory.cs@a6c914e:L9-L23`). The motor is always placed (locked) in its 17 levels, never in inventory ([current consumers](../invest/current-consumers.md#cat-042-i)).

## 3. Engine capabilities

The capability families are those in the [CAT-042 map row](../general-engine-element-map.md) and [catalogue-elements.json](../../coverage/catalogue-elements.json); they are not repeated here.

**Exists now**
- Dynamic and static rigid bodies, box colliders and contact materials: `engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L120`.
- Box and sphere mass properties: `engine/gpu/RigidMassProperties.cs@a6c914e:L17-L50`.
- Box2D v3 TGS Soft contact solve with friction at 480 Hz substeps: `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L208-L208` (substep), `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L230-L236` (soft parameters), `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L702-L719` (friction), `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L878-L884` (sweep).
- Electrical `PowerIn` socket identity: `engine/gpu/WorkshopConnections.cs@a6c914e:L8-L13`.
- Save codec: `engine/gpu/WorkshopSaveCodec.cs@a6c914e:L6-L10`. It needs new kinds and parameters.

**Missing**
- A revolute hinge joint row in the worker: Story 10.3 (hinge and end stops) builds it, and Story 11.1 consumes it as a free shaft.
- A bounded motor row (target speed, maximum torque, work and power budget) with committed work totals: Story 11.1.
- Cylinder collider and inertia: Story 10.1 (ENGINE-CYLINDER).
- Electrical supply source and supply snapshot: Story 8.1 (CAT-005 Battery).
- The `Mechanical` connection domain, the `Drive` socket and shaft coupling to consumers: Story 11.1.
- Cosmetic supply-lamp source and the shaft-pose binding for the index: Story 11.1.

**Element dependencies.** The motor needs CAT-005 Battery (Story 8.1) for supply. CAT-019 Conveyor (Story 11.1) is its first mechanical consumer. CAT-057, CAT-018 (Story 11.2) and CAT-071 (Story 11.4) consume its drive. CAT-063 Switch (`switched_motor`), CAT-059 Solar panel (`solar_motor`) and CAT-033 Hold timer (`saved_for_later`) supply it in levels.

## 4. Legacy harvest

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | Rotor 0.5 kg, r 0.3 m, w 0.13 m at (0, 0, 0.54); cylinder inertia formulas | `parts/MotorPart.cs@a6c914e:L19-L27` | carry forward | Authored finite rotor inertia that the requirement demands. |
| 2 | Housing boxes, cylinder and port layout | `parts/MotorPart.cs@a6c914e:L49-L53`, `parts/MotorPart.cs@a6c914e:L64-L72` | carry forward | Declaration geometry. |
| 3 | Free hinge about +Z; connected collision disabled; travel both ways | `parts/MotorPart.cs@a6c914e:L30-L35` | carry forward | Becomes a generic hinge declaration. |
| 4 | Mechanical socket sign: clockwise-positive viewed from +Z, binding −1 | `parts/MotorPart.cs@a6c914e:L54-L54`, `parts/MotorPart.cs@a6c914e:L96-L97` | carry forward | Signed port convention the requirement references. |
| 5 | `speed` 0–20, `torque` (0, 100]; defaults 6 and 20; out-of-range rejected | `parts/MotorPart.cs@a6c914e:L55-L63`, `parts/catalog/motor.tres@a6c914e:L14-L14`, `CuriousContraptions.tests/MechanicalWorkTests.cs@a6c914e:L132-L146` | `speed` and the defaults carried forward; the torque range superseded (decision 1: [0.1, 100] N·m) | f32 admission decided in Story 11.1a. |
| 6 | Supply: effort = torque only while powered; work torque·speed·Δt; power cap torque·speed | `parts/MotorPart.cs@a6c914e:L91-L97` | carry forward (law), do not carry forward (per-part `PreparePhysics` loop) | The bounded-supply law stays; per-element update loops are banned. |
| 7 | Zero effort disables actuation; zero work allows only braking; budgets finite and nonnegative | `engine/physics/PhysicsMotorCommand.cs@a6c914e:L5-L22` | superseded (open question 6: there is no separate work budget or zero-effort mode; an unsupplied drive is clamped to [0, 0] and only bearing friction slows the rotor); "supply loss adds no work" is carried forward | The drive row's bound is the only budget. |
| 8 | Outward-rounded budget spending, `BitDecrement` quotients, error bounds | `engine/physics/PhysicsMotorCommand.cs@a6c914e:L69-L77`, `engine/physics/MotorPredictionSupply.cs@a6c914e:L50-L90` | do not carry forward | Proof-grade CPU rounding; the game-grade envelope applies. |
| 9 | Accepted: supplied motor reaches 6 rad/s; a driven conveyor shares the same shaft speed on the same substep | `CuriousContraptions.tests/MechanicalTests.cs@a6c914e:L62-L143` | carry forward | Positive behaviour. |
| 10 | Accepted: after supply loss an unloaded rotor coasts at constant speed, kinetic energy is unchanged within 1e-8, and no new work is supplied | `CuriousContraptions.tests/MechanicalTests.cs@a6c914e:L62-L143`, `CuriousContraptions.tests/ConveyorRuntimeTests.cs@a6c914e:L114-L135` | superseded: coasting at constant speed (owner, open question 9, 3 November 2026: the rotor spins down under 0.05 N·m bearing friction); "no new work after loss" is carried forward | Loss erases no momentum instantly; the declared friction law removes it. |
| 11 | Accepted: rotor kinetic energy ≤ supplied work; unsupplied rotor energy ≤ 1e-14 over 180 ticks; plain and 25/40/15° rotated mounts | `CuriousContraptions.tests/SourceRotorTests.cs@a6c914e:L80-L123` | carry forward (re-freeze tolerances under f32) | Energy bound and rotated control. |
| 12 | Accepted: locked guide blocks the rotor (speed and travel ≤ 1e-7); unlocking resumes > 0.1 rad/s | `CuriousContraptions.tests/SourceRotorTests.cs@a6c914e:L125-L148` | carry forward (re-freeze tolerances under f32) | Stall control. |
| 13 | Accepted: disconnected motor keeps an applied impulse's speed and energy and reports 0 work | `CuriousContraptions.tests/SourceRotorTests.cs@a6c914e:L150-L167` | superseded: keeping the impulse's speed (owner, open question 9, 3 November 2026: bearing friction spins it down); "0 work while disconnected" is carried forward | The only damping is the declared bearing friction. |
| 14 | Accepted: torque 0.01 gives first-tick speed = torque × axial inverse inertia × tick | `CuriousContraptions.tests/SourceRotorTests.cs@a6c914e:L214-L230` | superseded (decision 1: the torque floor is 0.1 N·m, twice the bearing friction; the low-torque boundary is 0.1 N·m accelerating at (τ − τ_f)/I) | Low-torque acceleration boundary. |
| 15 | Accepted: contact blocking a drive spends no work; reverse command releases | `CuriousContraptions.tests/PhysicsMotorTests.cs@a6c914e:L52-L78`, `CuriousContraptions.tests/MotorPredictionTests.cs@a6c914e:L207-L217` | carry forward | Stall and obstruction. |
| 16 | Accepted: finite work caps acceleration; braking spends no supply; zero supply brakes but never reverses | `CuriousContraptions.tests/PhysicsMotorTests.cs@a6c914e:L169-L193` | superseded (open question 6: the drive's τ_d·h impulse bound caps acceleration with no work budget; an unsupplied drive is clamped to [0, 0] and does not brake, only bearing friction slows the rotor); "braking spends no supply" is carried forward | Torque-bounded drive. |
| 17 | Accepted: a peak-power cap is not replaceable by average work; zero watts cannot start but can brake | `CuriousContraptions.tests/MotorPredictionTests.cs@a6c914e:L52-L81` | superseded (open question 6: the power bound is τ_d·\|ω_t\| from the drive's impulse bound; an unsupplied drive is clamped to [0, 0] and cannot brake, only bearing friction slows the rotor); prediction solver not carried forward | The CPU continuous prediction is not the current solver. |
| 18 | Accepted: motor runs only with a connected, enabled supply; reversed wire rejected; Reset zeroes angle and speed | `CuriousContraptions.tests/ElectricalTests.cs@a6c914e:L96-L135` | carry forward | Negative control and Reset. |
| 19 | Accepted: runtime speed input 0 and 20 applied; −1 and 20.001 out of range; NaN and ∞ rejected; rollback with the tick | `CuriousContraptions.tests/RuntimeBinaryInputTests.cs@a6c914e:L28-L69` | carry forward (bounds) | Open question 5 decides the runtime control. |
| 20 | Accepted: powered motor reaches commanded speed 3 within 10 ticks (±1e-5); speed 0 supplies no work | `CuriousContraptions.tests/RuntimeBinaryInputTests.cs@a6c914e:L70-L85` | carry forward (re-freeze tolerances under f32) | Speed-regulated bounded drive. |
| 21 | Supply lamp slate→gold over 0.1 s SmoothStep; presentation never alters physics, work or travel | `parts/MotorPart.cs@a6c914e:L40-L45`, `CuriousContraptions.tests/MotorIndicatorTests.cs@a6c914e:L21-L68` | carry forward | Cosmetic binding to committed state. |
| 22 | Index spins at the accepted rate and stays at baseline when speed is 0; Reset restores it | `CuriousContraptions.tests/AngularVelocityAnimationTests.cs@a6c914e:L107-L170` | carry forward | Committed-motion presentation. |
| 23 | Work reads from owned physics, is not double counted, and restores with capture/restore | `CuriousContraptions.tests/MotorAccountingOwnershipTests.cs@a6c914e:L28-L76` | carry forward | Exact Run/Reset. |
| 24 | `Turned` after travel ≥ 2π; `Powered` while supplied; goal kinds `turned` and `powered_after` | `parts/MotorPart.cs@a6c914e:L98-L103`, `engine/MachineData.cs@a6c914e:L152-L152` | carry forward | Objective inputs for the motor levels. |
| 25 | Socket and goal names as string wire tokens (`power_in`, `drive`) | `engine/MachineData.cs@a6c914e:L104-L108` | do not carry forward (string domain use) | Enums end to end; strings only at the serialization boundary. |
| 26 | Level `battery_motor`: locked motor at (3, 1, 0); place a battery; Supply→PowerIn; goal `turned` | `content/puzzles.json@a6c914e:L3425-L3504`, `content/puzzles.json@a6c914e:L3570-L3578` | carry forward | First lesson set-up. |
| 27 | Level `switched_motor`: a bowling ball triggers a placed switch that gates supply; goals `turned` and `powered_after` | `content/puzzles.json@a6c914e:L3581-L3591`, `content/puzzles.json@a6c914e:L3780-L3791`, `content/puzzles.json@a6c914e:L3857-L3872` | carry forward | Switched-supply integration. |
| 28 | Rotated mount: `solar_motor` motor yawed 180° at (−4, 1, −2), supplied by a solar panel | `content/puzzles.json@a6c914e:L6080-L6142`, `content/puzzles.json@a6c914e:L6216-L6224` | carry forward | Rotated control fixture (Epic 13 dependency). |
| 29 | `motor` drives `launcher` (wound spring) in `wind_then_release` | `content/puzzles.json@a6c914e:L31442-L31504`, `content/puzzles.json@a6c914e:L31830-L31859` | carry forward | Story 11.4 integration. |

### Files harvested
- `parts/MotorPart.cs`
- `parts/catalog/motor.tres`
- `parts/scenes/motor.tscn` (script attachment only)
- `engine/SceneRotaryShaft.cs`
- `engine/physics/PhysicsMotorCommand.cs`
- `engine/physics/MotorPredictionSupply.cs`
- `engine/MachineData.cs`
- `engine/MachineEvent.cs`
- `engine/MechanicalNetwork.cs` (coupling; harvested in CAT-019)
- `engine/ConnectionPort.cs` (`MechanicalBinding`; harvested in CAT-019)
- `CuriousContraptions.tests/SourceRotorTests.cs`
- `CuriousContraptions.tests/PhysicsMotorTests.cs`
- `CuriousContraptions.tests/MotorPredictionTests.cs`
- `CuriousContraptions.tests/MotorIndicatorTests.cs`
- `CuriousContraptions.tests/MotorAccountingOwnershipTests.cs`
- `CuriousContraptions.tests/ElectricalTests.cs`
- `CuriousContraptions.tests/RuntimeBinaryInputTests.cs`
- `CuriousContraptions.tests/AngularVelocityAnimationTests.cs`
- `CuriousContraptions.tests/MechanicalTests.cs`
- `CuriousContraptions.tests/MechanicalWorkTests.cs`
- `CuriousContraptions.tests/ConveyorRuntimeTests.cs`
- `content/puzzles.json` (levels `battery_motor`, `switched_motor`, `solar_motor`, `wind_then_release`)
- `ui/WorkshopIcons.cs` (pictogram)
- `engine/physics/AccelerationDrive.cs`, `engine/physics/MechanicalPowerPort.cs`, `engine/physics/MechanicalPortSpeedPath.cs`, `CuriousContraptions.tests/AccelerationDriveTests.cs` (checked, generic CPU drive solver, no motor-specific values)
- `reference/cpu/MachineWorld.cs` (checked, duplicate of the `DriveMotor` command path)
- `reference/p054-guide/puzzles-candidate.json` (checked, duplicate candidate copy of the same levels)
- `reference/p025-production-isolated-20261004/src/ui/WorkshopIcons.cs` (checked, duplicate of the pictogram)

## 5. Acceptance outline

Follow the [CAT-042 row](../requirements.md#current-cat-042) and Story 11.1; do not restate them.
- **Chrome UI recipe.** Open `battery_motor` in the Workshop. Place the battery from the toolbox with actual pointer controls. Use the contextual Connect from battery `Supply` to motor `PowerIn`, then Run. Record bundle identity, the committed rotor pose sequence and the `turned` goal.
- **Positive.** The rotor accelerates to its bounded speed, the lamp turns gold, and supplied work grows (facts 9 and 20).
- **Negative/control.** No battery, a disabled battery, or an activation wire to the motor gives no rotation and zero work (fact 18). A wire into `Drive` from a non-mechanical domain is rejected (Story 11.1b).
- **Boundaries.** Low torque 0.1 N·m, accelerating at (τ − τ_f)/I (decision 1); a stalled or locked shaft (facts 12, 15); supply loss spinning down under the 0.05 N·m bearing friction with no new work (open question 9); rotated mount (`solar_motor`, fact 28); speed 0 and 20.
- **Run/Reset.** Rotor angle, speed, lamp and supplied work return exactly to authored state (facts 22, 23).
- **Save/Load.** Speed, torque, pose and typed connections round-trip through `WorkshopSaveCodec`.
- **Integrations.** Motor→Conveyor (`conveyor_courier`, CAT-019), `switched_motor`, and later the CAT-057, CAT-018 and CAT-071 consumers.

## 6. Open questions

1. Admitted f32 ranges and units for `speed` (legacy 0–20 rad/s) and `torque` (legacy (0, 100] N·m): **decided by game intent (Story 11.1a).** `speed` admits [0, 20] rad/s inclusive, default 6; `torque` admits [0.1, 100] N·m inclusive, default 20. The torque floor is twice the 0.05 N·m bearing friction, so the weakest Motor still visibly starts (0.0185 rad/s per tick); it replaces the legacy open interval and harvest row 14's 0.01. Non-finite and out-of-range values are refused at every seam. +`speed` turns the rotor counterclockwise about its +Z (the engine convention); the Drive socket's clockwise-positive sign belongs to 11.1b.
2. Rotor collider primitive (legacy 24-gon prism) under the current Sphere/Box/Plane set: **decided by game intent (Story 11.1a):** the true cylinder collider (Story 10.1), r 0.3 m, height 0.13 m, with its exact inertia.
3. Rotor contact material: legacy inherits restitution 1, friction 0.3: **decided by game intent (Story 11.1a):** the catalogue's rigid-part material, restitution 0, friction 0.3, bounce threshold 0.1 m/s, for the rotor and the housing. A spinning rotor should not bounce what it touches.
4. Whether motor work debits a finite battery store (Story 8.1) or only needs a binary supply: **decided and deferred** (owner, 3 November 2026). Supply is binary for now; the drain becomes one demand law for every powered part (EL-196) in its own slice ([deferred work](../../../_bmad-output/implementation-artifacts/deferred-work.md#powered-part-demand-drain)).
5. Whether `speed` stays a runtime scalar control during Run (legacy `SpeedInput`) or is configuration only: **moved to its own slice** (owner, 3 November 2026). A run-time adjustable drive target is the engine slice `engine-revolute-drive-target` after 11.1a ([TODO](../../../TODO.md)); until it lands, `speed` is a placement parameter.
6. Whether the per-tick work allowance torque·speed·Δt and power cap torque·speed are retained as the bounded-supply law: **confirmed by derivation and test (Story 11.1a).** The generic drive row's impulse is at most τ_d·h each substep, so it delivers at most τ_d·|ω|·Δt of work and τ_d·|ω_t| of power with no separate budget; see Work and energy stores. The Motor tests check the committed work against τ_d·|ω_t|·t at every tick, and a held Motor pays only its bearing loss τ_f·ω_t.
7. `turned` goal semantics: cumulative absolute travel ≥ 2π (legacy) versus signed winding: **moved to level work** with the `Turned`/`Powered` events and goal kinds. The committed signed travel the goal will read already exists (`RevoluteStateRead.Travel`).
8. Cylinder collider and cylinder inertia for the rotor: Story 10.1 (ENGINE-CYLINDER), scheduled before the first shaft wheel by the owner on 9 Oct 2026.
9. **Epic story vs requirement conflict.** Story 11.1 says cutting motor power "decelerates the belt to a halt under friction". CAT-042 says supply loss adds no work and does not erase momentum, and any damping is an explicit physical law ([CAT-042](../requirements.md#current-cat-042)); the legacy rotor coasts at constant speed (fact 10). Which governs, and which explicit friction law, if any, stops the belt? **Decided (owner, 3 November 2026): the rotor spins down** under the generic hinge's declared Coulomb bearing friction, τ_f = 0.05 N·m: from 6 rad/s it decelerates at τ_f/I = 2.2 rad/s² and stops after about 2.7 s, with no work added after the cut. Supply loss erases no momentum instantly; the explicit friction law removes it. The coasting claims of facts 10 (constant speed after a cut) and 13 (an impulse kept with no damping) are superseded by this law; fact 11's energy bound still holds.
10. **Epic story vs requirement conflict.** Story 11.1 supplies the motor from a "12V battery", but no voltage is declared by CAT-005, CAT-042 or the legacy binary supply. Is voltage a declared quantity? **Decided by game intent (Story 11.1a):** no. Supply is on/off (owner, 3 November 2026, with drain deferred, question 4). The Motor's electrical node keeps inert data: 12 V rated, 2000 W, the drive's peak mechanical power 100 N·m × 20 rad/s.
11. **The `Drive` port, the `Mechanical` connection domain and socket, and the "non-mechanical wire into Drive refused" negative** belong to Story 11.1b. Story 11.1a ships PowerIn only; an Activation wire into PowerIn is refused (`InvalidConnection`).
12. **Rope ↔ drive creep (found in Story 11.1a).** A rope tied to a supplied Motor's rim did not hold it at rest. Story 11.1a recorded a rim-tied rotor creeping at about 0.0082 rad/s per N·m of torque, and a supplied speed-0 Motor slowly lifting a rope-hung load. The diagnosis corrected both symptoms:
    - The stall creep was a ledger artefact. The drive's committed travel and work grew, while the rotor's pose turned only about 1.5e-4 rad/s.
    - The speed-0 hold really slipped, in the load's direction, while the ledger read the opposite sign.

    No player can build this yet. **Owner decision, 4 November 2026:** 11.1a proves its stall row against a locked hinge and its speed-0 row on an unloaded shaft; the rope variants are the acceptance of the next engine slice, `engine-tensile-drive-coupling` ([deferred work](../../../_bmad-output/implementation-artifacts/deferred-work.md#tensile-revolute-drive-creep)). **Resolved** by that slice (4 November 2026): the rope's length row was soft in the solver's relax pass and committed a false velocity; it is now rigid there, like contacts. `MotorTests.AStalledMotorDoesNoWorkOnceItsShaftIsHeld` and `ASuppliedMotorAtSpeedZeroHoldsItsLoadedShaftStill` prove both rope variants on the drive's ledger and on the committed pose. `ASuppliedMotorThatWinsLiftsItsRopeHungLoadAndChargesOnlyWhatItLifts` proves a supplied Motor lifting its rope-hung load at its target speed, charging no more work than the lift and the bearing loss.
13. **Index bar (decided by game intent, Story 11.1a).** The gold index bar (0.48 × 0.07 m) is printed flush on the rotor face, 4 mm thick rather than the legacy 4 cm, and decorative with no collider. It is part of the rotor art, so it turns with the committed rotor pose rather than on its own cosmetic phase (fact 22); a bar standing proud of the face would be visible but not solid.
