# CAT-052 · pressure_plate — declaration readiness spec

Story 7.0 Batch B declaration spec (CAT-052-D). Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`. Requirement row: [CAT-052](../requirements.md#current-cat-052). Supplied-network facts (N1–N24) are in [CAT-005](CAT-005-battery.md#4-legacy-harvest).

## 1. Identity

| Item | Value |
| --- | --- |
| CAT ID / kind | CAT-052 · `pressure_plate` |
| Requirement anchor | [current-cat-052](../requirements.md#current-cat-052); retained behaviour [todo-265](../requirements.md#todo-265) |
| Mapped identities | none owned. Related but distinct: [EL-059 Weight tray](../invest/named-elements.md#element-059) ("loaded support exposes measured force or threshold state"); no legacy implementation links them, so its requirement row stands as the source. |
| Roadmap story | 9.1 "Pressure Plate Surface Contact Load Sensor" (Epic 9) |
| Status | not started |

## 2. Declaration

| Item | Declaration (value · source) |
| --- | --- |
| Bodies and shapes | One static body with two collision boxes: base 2.2 × 0.2 × 2.2 m centred at y −0.12 (top at −0.02) and top plate 1.8 × 0.1 × 1.8 m centred at y 0.03, so the sensing face is at y 0.08 with half size 0.9 (`parts/PressurePlatePart.cs@a6c914e:L14-L15`, `parts/PressurePlatePart.cs@a6c914e:L45-L46`; legacy `AddBox` = collision box, [CAT-005 §2](CAT-005-battery.md#2-declaration)). The top stays physically and visually fixed (no decorative depression). |
| Mass and material | Static. Contact material restitution 0.1, bounce threshold 0.1 m/s, friction 0.3 (`parts/PressurePlatePart.cs@a6c914e:L28-L28`; field order restitution, bounce threshold, friction: `engine/physics/PersistentContactPair.cs@a6c914e:L18-L29`), unlike the static default restitution 1. Declare through `ContactMaterialDeclaration` (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L45`) as f32; that record still stores binary16, listed as remaining f32 migration (`docs/gpu-f32-physics.md@a6c914e:L94-L94`). |
| Constraints and joints | none |
| Typed sockets and ports | `PowerIn` Electrical Input at (−1.18, −0.08, 0); `Supply` Electrical Output at (1.18, −0.08, 0) (`parts/PressurePlatePart.cs@a6c914e:L29-L33`). Electrical route `PowerIn`→`Supply` closed while the load phase is Loaded (`parts/PressurePlatePart.cs@a6c914e:L34-L35`). No activation port: a delay→plate link is refused (harvest P13). |
| Sensors and activation | Contact-load sensor on the plate's own frame: region x ∈ [−0.9 − d, 0.9 + d], y ∈ [0.08 − d, 0.08 + d], z ∈ [−0.9 − 10⁻⁶, 0.9 + 10⁻⁶] with d = contact distance 0.0001 m; local normal +Y; minimum alignment 1 − 10⁻⁶; minimum mass = parameter (`parts/PressurePlatePart.cs@a6c914e:L21-L25`; `engine/physics/ConvexSweep.cs@a6c914e:L46-L46`). Phases `Empty` (no qualifying mass), `Underweight` (0 < mass < minimum), `Loaded` (mass ≥ minimum) (`engine/physics/PhysicsContactLoadSensor.cs@a6c914e:L7-L7`, `engine/physics/PhysicsContactLoadSensor.cs@a6c914e:L46-L47`). |
| Work and energy stores | none; the plate closes a supplied contact and creates no supply, latch or impact command ([todo-265](../requirements.md#todo-265)). |
| Parameters | `minimum_mass`: float kilograms, range 0.1–16 inclusive, default 0.5 (`parts/PressurePlatePart.cs@a6c914e:L36-L41`, `parts/catalog/pressure_plate.tres@a6c914e:L14-L14`). Captured at Run; changing it during Run is rejected (P15). Carry forward as f32 kg. |
| Cosmetic curves and UI bindings | Four corner studs: slate `#556573` when Empty, ochre `#e8b764` when Underweight, easing to gold `#f7cb52` when Loaded (eased at rate 8 per second with SmoothStep) (`parts/PressurePlatePart.cs@a6c914e:L56-L62`). No current cosmetic source for a contact-load phase (`engine/gpu/WorkshopCosmetic.cs@a6c914e:L7-L7`). |
| Art | Navy `#293954` base; cream `#fff8e9` top; four stud spheres r 0.055 at (±1, 0.015, ±1); `#e8b764` socket spheres r 0.075 at (±1.18, −0.08, 0); `#e8b764` centre ring r 0.3, thickness 0.018 at (0, 0.084, 0) — "a quiet central load target, not a control overlay" (`parts/PressurePlatePart.cs@a6c914e:L42-L54`). Selection ring 1.5 (`parts/PressurePlatePart.cs@a6c914e:L44-L44`). Catalogue colour (0.91, 0.72, 0.39) is not used on a visible surface. Toolbox icon (current, survives): `ui/WorkshopIcons.cs@a6c914e:L84-L84`. Design: "Pressure plate" row of [DESIGN.md · Motion and state feedback](../../../DESIGN.md#motion-and-state-feedback). |
| Catalogue / inventory | Id `pressure_plate`, Title "Pressure plate", Category Control, colour (0.91, 0.72, 0.39), Parameters `{"minimum_mass": 0.5}`; Description: "Closes its electrical contact while enough mass rests directly on the top face. Needs a battery or other supply. The default threshold is 0.5: one tennis ball is too light." (`parts/catalog/pressure_plate.tres@a6c914e:L8-L14`). No level places or grants it. |

Reference masses for acceptance: Basketball 1 kg and Bowling ball 4 kg (`engine/gpu/WorkshopConstruction.cs@a6c914e:L48-L51`); Domino 0.4 kg (`engine/gpu/WorkshopDomino.cs@a6c914e:L9-L9`); legacy tennis ball 0.35 kg (`parts/catalog/tennis.tres@a6c914e:L14-L14`) and weight 4 kg (`parts/catalog/weight.tres@a6c914e:L14-L14`).

## 3. Engine capabilities

Families ([map row](../general-engine-element-map.md)): ContactImpulse, ElectricalPower, EnvironmentState, FiniteLedger, GeometryQuery, RigidBodyDynamics, SignalPropagation, SlidingFriction. Map note: "Shared contact-load sensor counts bodies once with declared region/normal filters; not proximity or free supply."

**Exists now**
- Static box bodies, colliders and materials: `engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L119`, `engine/gpu/WorkshopPhysicsCompiler.cs`.
- Other declared sensors on the shared solver, evaluated at substep endpoints: residence (one named body, dwell) `engine/gpu/PhysicsDeclarations.cs@a6c914e:L122-L142`; contact trigger (impact approach speed) `engine/gpu/ContactTriggerDeclaration.cs@a6c914e:L7-L18`; orientation `engine/gpu/OrientationSensorDeclaration.cs@a6c914e:L24-L34`. None measures resting mass.

**Missing**
- A generic contact-load sensor declaration: owned frame, local region, local normal and minimum alignment, minimum mass; per tick it sums the mass of each distinct dynamic body in direct qualifying contact and publishes `Empty`/`Underweight`/`Loaded` — Story 9.1.
- Stud colour feedback from the committed load phase — Story 9.1.
- Supplied contact `PowerIn`→`Supply` closed while Loaded — Story 8.1 network (open question 2).

**Element dependencies**: dynamic loads (Basketball CAT-001, Bowling CAT-014, Domino CAT-023 delivered; Tennis CAT-064 and Weight CAT-067 later); Battery (CAT-005) and a supplied consumer for an observable output ([current-consumers](../invest/current-consumers.md#cat-052-i) names Motor, CAT-042; the legacy tests used Powered gate, CAT-051).

## 4. Legacy harvest

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| P1 | Continuous electrical contact; sums masses directly resting on the top face — not impacts, stacked load transmission or a generated supply. | `parts/PressurePlatePart.cs@a6c914e:L8-L9` | carry forward |
| P2 | Top 0.08 m, half size 0.9 m; readable supported mass and phase (Empty before Run). | `parts/PressurePlatePart.cs@a6c914e:L14-L20` | carry forward |
| P3 | Sensor region, normal, alignment and minimum mass as in section 2. | `parts/PressurePlatePart.cs@a6c914e:L21-L25` | carry forward |
| P4 | Plate material restitution 0.1, bounce threshold 0.1, friction 0.3. | `parts/PressurePlatePart.cs@a6c914e:L28-L28` | carry forward |
| P5 | Ports and contact route as in section 2. | `parts/PressurePlatePart.cs@a6c914e:L29-L35` | carry forward |
| P6 | Minimum mass must be finite and within 0.1–16. | `parts/PressurePlatePart.cs@a6c914e:L36-L41` | carry forward |
| P7 | Geometry and art as in section 2. | `parts/PressurePlatePart.cs@a6c914e:L42-L54` | carry forward |
| P8 | Active exactly while Loaded; studs eased per frame toward gold (rate 8/s, SmoothStep) from ochre (Underweight) or slate. | `parts/PressurePlatePart.cs@a6c914e:L55-L62` | carry forward the colours and timing; do not carry forward the per-part `PreparePhysics`/`BeforeNetworks` update loop or the hand-written material writes (declared cosmetic binding instead) |
| P9 | Measurement: a contact counts only if the other body is dynamic, the contact normal aligns with the frame normal to at least the minimum alignment, and the contact point (frame-local) lies inside the region; each body counts once (by identity); mass = sum of body masses; Empty if 0, Underweight if below the minimum, Loaded otherwise (inclusive). | `engine/physics/PhysicsContactLoadSensor.cs@a6c914e:L30-L49` | carry forward the rule; do not carry forward the CPU contact-gap source (the shared solver's committed contacts feed it) |
| P10 | Declaration validation: non-empty finite region, finite non-zero normal (normalised), alignment in (0, 1], minimum mass > 0; one sensor per frame body, installation atomic. | `engine/physics/PhysicsContactLoadSensor.cs@a6c914e:L18-L29` and `engine/physics/PhysicsWorld.cs@a6c914e:L1033-L1044` | carry forward |
| P11 | Supply/load matrix after 240 ticks, with a Powered gate (`powered_gate`, CAT-051) as the supplied load (`CuriousContraptions.tests/PressurePlateTests.cs@a6c914e:L15-L15`): one ball → Loaded (with and without supply; the Powered gate is powered and opens only with supply); one tennis ball → Underweight; two tennis balls → Loaded; one weight → Loaded; reported mass = sum of resting bodies; disabling the loads' collision → Empty on the next tick and the gate closes; Reset → mass 0, Empty, inactive, wires kept. | `CuriousContraptions.tests/PressurePlateTests.cs@a6c914e:L38-L82` | carry forward |
| P12 | Rotated plate (20°, 30°, 40°) measures only its own top face: ball centre at local (0, 0.42, 0) presses; (0, 0.5, 0) (a gap), (1.2, 0.42, 0) (outside) and (0, −0.66, 0) (underside) do not. | `CuriousContraptions.tests/PressurePlateTests.cs@a6c914e:L83-L107` | carry forward |
| P13 | A delay cannot connect to the plate (no activation input, so a trigger cannot replace supply); minimum mass 3 round-trips through save/load. | `CuriousContraptions.tests/PressurePlateTests.cs@a6c914e:L108-L123` | carry forward |
| P14 | Thresholds 0, −1, 17, NaN and +∞ are rejected and the part is not added. | `CuriousContraptions.tests/PressurePlateTests.cs@a6c914e:L124-L140` | carry forward |
| P15 | A compound load (weight) or ball resting on the plate, level or tilted 37°, counts its mass once with exactly one body id; reading does not mutate contacts; moving or hiding scene nodes does not change the reading; changing `minimum_mass` during Run is rejected and the phase is unchanged; an upward impulse equal to the body's momentum per unit speed (1 m/s) lifts it and the reading becomes Empty after one step. | `CuriousContraptions.tests/PressureContactOwnershipTests.cs@a6c914e:L19-L71` | carry forward |
| P16 | Sphere, box and two-sphere compound loads of mass 2 count once (Loaded at threshold 1, Underweight at 3); side, below, gap, outside-region and static loads do not count. | `CuriousContraptions.tests/PhysicsContactLoadSensorTests.cs@a6c914e:L50-L92` | carry forward |
| P17 | A disabled frame collider or an opposite sensor normal reads 0. | `CuriousContraptions.tests/PhysicsContactLoadSensorTests.cs@a6c914e:L94-L103` | carry forward |
| P18 | Two bodies of 2 kg and 3 kg sum to 5 kg and are Loaded at minimum 5 (inclusive threshold); body ids reported in identity order. | `CuriousContraptions.tests/PhysicsContactLoadSensorTests.cs@a6c914e:L105-L119` | carry forward |
| P19 | Duplicate frame, unknown body and null declarations reject atomically; querying an undeclared frame rejects. | `CuriousContraptions.tests/PhysicsContactLoadSensorTests.cs@a6c914e:L121-L146` | carry forward |
| P20 | A failed physics step restores the reading. | `CuriousContraptions.tests/PhysicsContactLoadSensorTests.cs@a6c914e:L148-L160` | do not carry forward (CPU transaction rollback) |
| P21 | Physics-side query and declaration record. | `engine/physics/PhysicsWorld.cs@a6c914e:L118-L125` and `engine/SceneContactLoadSensorDeclaration.cs@a6c914e:L5-L6` | carry forward the declaration fields; not the CPU world |
| P22 | Parameter enum `MinimumMass` (wire name `minimum_mass`). | `engine/MachineData.cs@a6c914e:L86-L86` | carry forward at the serialization boundary |
| P23 | The contact reads the committed load phase (`Loaded`). | `engine/SceneElectricalContact.cs@a6c914e:L122-L124` and `engine/SceneElectricalContact.cs@a6c914e:L139-L139` | carry forward |
| P24 | Catalogue entry and scene. | `parts/catalog/pressure_plate.tres@a6c914e:L1-L14` and `parts/scenes/pressure_plate.tscn@a6c914e:L1-L6` | carry forward the data |

`reference/P0-022-before/docs/coverage/engine/*.json` list `pressure_plate` only as a catalogue id; `tools/P0-007-actual-path-probe/*` only compiles the legacy sources: no additional element knowledge. `engine/physics/CompliantContactLoad.cs`, `CompliantContactState.cs` and `PhysicsLoadSet.cs` mention contact load but implement compliant surfaces (trampoline/springboard), not this sensor.

### Files harvested

- `parts/PressurePlatePart.cs`
- `parts/catalog/pressure_plate.tres`
- `parts/scenes/pressure_plate.tscn`
- `parts/catalog/tennis.tres` (mass value only)
- `parts/catalog/weight.tres` (mass value only)
- `engine/physics/PhysicsContactLoadSensor.cs`
- `engine/physics/PhysicsWorld.cs`
- `engine/physics/ConvexSweep.cs`
- `engine/physics/PersistentContactPair.cs`
- `engine/SceneContactLoadSensorDeclaration.cs`
- `engine/SceneElectricalContact.cs`
- `engine/MachineData.cs`
- `CuriousContraptions.tests/PressurePlateTests.cs`
- `CuriousContraptions.tests/PressureContactOwnershipTests.cs`
- `CuriousContraptions.tests/PhysicsContactLoadSensorTests.cs`

## 5. Acceptance outline

Authority: [CAT-052](../requirements.md#current-cat-052), [todo-265](../requirements.md#todo-265); Story 9.1 adds `tools/e2e/cat-052.test.ts`.

- **Construction (Chrome UI).** Place Pressure plate, Battery and the consumer; Connect battery→plate (`Supply`→`PowerIn`) and plate→consumer; drop or place a Basketball/Bowling ball above the top face with the ordinary placement tools.
- **Positive.** A body of at least the threshold mass resting directly on the top face closes the contact; studs ease to gold.
- **Negative / controls.** Rolling off or lifting opens it on the next tick (P11, P15); a below-threshold body leaves the studs ochre and the contact open (P11); contact on the side or underside, or a body beside the plate, does not count (P12, P16); no supply → studs gold but consumer off (P11).
- **Boundaries.** Exactly-at-threshold mass is Loaded (P18); two light bodies together reach the threshold (P11); a compound or stacked load counts each directly touching body once (P15); thresholds 0.1 and 16 accepted, out-of-range rejected (P14); rotated mounting (P12).
- **Run/Reset.** Reset gives mass 0, Empty, slate studs, inactive (P11).
- **Save/Load.** `minimum_mass` round-trips (P13).
- **Integrations.** Battery-fed contact to the consumer; bouncing impact control (open question 3).

## 6. Open questions

1. **Threshold.** The Story 9.1 AC says activation for "mass > 0.2 kg"; the requirement row and the legacy say default 0.5 kg with an inclusive threshold (mass ≥ minimum). A Domino (0.4 kg) presses at 0.2 but not at 0.5. Which default and which comparison: unspecified — owner decision.
2. **Output to a Signal lamp.** The Story 9.1 AC wires the plate "to a Signal lamp"; the legacy and the requirement give the plate only a supplied electrical contact, the lamp is activation-only (CAT-035), and the supply network (Story 8.1) now precedes Story 9.1. Whether 9.1 proves the plate through the Story 8.1 supplied contact, or the plate gains an activation output the requirement does not define: unspecified — owner decision.
3. **Bouncing impacts.** The Story 9.1 AC and the requirement require that impact-only contact does not give sustained activation. The legacy counted any qualifying contact present at the tick, with no rest or dwell qualifier, so a bounce could close the contact for the ticks it touches. Whether a dwell or rest-speed qualifier is required, and its value: unspecified — owner decision.
