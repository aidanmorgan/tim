# EL-053 · Rotary-to-linear converter declaration readiness spec

Story 7.0 named-identity spec. Baseline commit `a6c914e`. Legacy citations use `path@a6c914e:Lstart-Lend` and stay retrievable from git history after the Epic 7 purge. Values marked **proposed** are design values the owner may revise.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-053 |
| Name | Rotary-to-linear converter |
| Type | Mechanical |
| Requirement anchor | [element-053](../requirements.md#element-053); scope index [todo-211](../requirements.md#todo-211) |
| Named entry | [named-elements.md#element-053](../invest/named-elements.md#element-053); proof owner S339 |
| CAT spec refined or extended | none (no catalogue converter). Related: [CAT-042 motor](CAT-042-motor.md) (shaft source), [CAT-039 linear pusher](CAT-039-linear_pusher.md) (electric linear actuator, different law), [CAT-071 wound spring](CAT-071-wound_spring.md) (legacy shaft-to-slider winding lead) |
| Related identities | [EL-054](EL-054-linear-to-rotary-converter.md) (inverse conversion); EL-156/157/158 cam, crank and rack variants under todo-424 (Batch J) |
| Roadmap story | unscheduled |
| Status | not started |

## 2. Declaration

### Bodies and shapes

- Static housing and rail: one box, 1.6 × 0.3 × 0.5 m (half-extents 0.8, 0.15, 0.25). **Proposed**: spans the stroke plus the shaft bearing at the scale of the 1.25 m motor base.
- Dynamic input shaft (lead screw): cylinder r 0.26 m, width 0.1 m, mass 0.25 kg. **Proposed**, reusing the legacy wound-spring winding shaft (`parts/WoundSpringPart.cs@a6c914e:L71-L71`; `engine/SceneRotaryShaft.cs@a6c914e:L13-L24`) so shaft inertia matches an existing mechanism.
- Dynamic output carriage: box 0.5 × 0.3 × 0.5 m, mass 1 kg. **Proposed**: one Basketball-mass slide whose face pushes cargo of catalogue size.

### Mass and material

- Carriage and housing material: restitution 0.2, bounce threshold 0.1 m/s, friction 0.4, rolling resistance 0. **Proposed**: a dull pusher face that moves cargo without bouncing it; bounce threshold matches every current declaration (`engine/gpu/WorkshopConstruction.cs@a6c914e:L45-L53`).
- Use `ContactMaterialDeclaration` (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L54`) and `RigidBodyDeclaration` (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L86`).

### Constraints and joints

- Shaft: hinge about its local axis to the housing (legacy shaft guide pattern `engine/SceneRotaryShaft.cs@a6c914e:L25-L28`).
- Carriage: slider along the rail with travel [0, stroke] end stops.
- Coupling: ratio row, carriage speed = lead × shaft angular speed, bidirectional and phase-free (legacy law `engine/physics/PhysicsTransmissionJoint.cs@a6c914e:L7-L13`; legacy use `parts/WoundSpringPart.cs@a6c914e:L89-L98`). Because the row is bidirectional, a blocked carriage loads the shaft; this is the requirement's "blocked output loads the input".

### Typed ports

| Socket | Domain | Direction | Local position (m) | Source |
| --- | --- | --- | --- | --- |
| `DriveIn` | Mechanical | Input | (−0.8, 0, 0.3) | Legacy socket identity `engine/MachineData.cs@a6c914e:L104-L108`; position **proposed** (shaft end, front face) |

One belt per mechanical input (`engine/MechanicalNetwork.cs@a6c914e:L35-L35`). No output socket: the carriage acts on cargo through contact.

### Sensors and activation

None required by the row. The committed carriage travel is a read for cosmetics and for the end-of-stroke boundary.

### Work and energy stores

None. The converter stores only kinetic energy of its shaft and carriage; all work comes from the upstream shaft (IX-05).

### Parameters

| Name | Type | Range | Default | Unit | Source |
| --- | --- | --- | --- | --- | --- |
| `lead` | f32 | 0.025–0.2 | 0.05 | m/rad | Range: legacy winding lead (`parts/WoundSpringPart.cs@a6c914e:L118-L124`). Default **proposed**: a default motor (6 rad/s) moves the carriage 0.3 m/s, slow enough to read |
| `stroke` | f32 | 0.4–1.5 | 1.0 | m | **Proposed**: longer than a Basketball diameter (0.68 m), shorter than the housing |

Parameter keys are a closed enum `ConverterParameter { Lead, Stroke }`.

### Cosmetic curves and UI bindings

- Gold witness marks on the screw follow the committed shaft angle; the carriage is drawn at its committed physics pose. No cosmetic easing of the carriage.
- Needs a committed shaft-angle binding (absent from `AnimationFeedbackSource`, `engine/gpu/WorkshopCosmetic.cs@a6c914e:L7-L7`).

### Art

Cream housing `#fff8e9`, navy foot `#293954`, pale grey metal screw `#ccd9df` (legacy silver helix `parts/SpringPart.cs@a6c914e:L94-L94`), ochre carriage `#d69c47` (Conveyor and Reverse transmission colour, `DESIGN.md@a6c914e:L176-L183`), gold witness marks `#f7cb52`. Catalogue colour **proposed**: ochre `#d69c47`, shared with the existing drive-train parts.

### Catalogue and inventory entry

Id `rotary_linear_converter`, title "Screw slide", category "Motion". New `WorkshopPartKind.RotaryLinearConverter` appended last to `WorkshopInventoryPolicy.Free` (`engine/gpu/WorkshopInventory.cs@a6c914e:L49-L60`). Title and id **proposed**.

### Variants

The requirements row names no variants or modes. One element is specified.

## 3. Engine capabilities

Binding: EnvironmentState, FiniteLedger, FiniteWorkActuation, JointConstraint, RigidBodyDynamics, ShaftTorque ([element map](../general-engine-element-map.md)); the [coverage binding](../../coverage/engine/element-01.json) adds StateTransaction.

**Exists now**

- Static and dynamic box bodies and colliders: `engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L120`.
- TGS Soft contact rows with friction: `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L208-L236`, `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L702-L766`.

**Missing**

- Cylinder collider and cylinder inertia: no owning story; owner decision 9 Oct 2026: a new cylinder-collider story comes before the first shaft wheel.
- Slider joint with end stops: Story 6.4. Hinge: Story 10.3.
- `Mechanical` domain, `DriveIn` socket, shaft torque and finite source work: Story 11.1.
- Ratio coupling row between two axial coordinates: Story 11.2 (rotary-to-rotary); the hinge-to-slider form is first exercised by Story 11.4 (wound spring winding lead).
- Shaft-angle cosmetic binding: Story 11.1.

**Dependencies.** A shaft source: CAT-042 Motor (11.1) with CAT-005 Battery (8.1). EL-054 shares the coupling law.

## 4. Sources and legacy

- Requirements row: outcome "Blocked output loads the input; no command-only displacement". No variants.
- Element map row: Source D owner freezes the linked mechanism. No decisions.md row applies.
- Current research note: crank and cam conversions are separate EL-156..158 rows (`docs/component-research.md@a6c914e:L76-L76`).

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | A transmission couples two axial coordinates bidirectionally: output speed = ratio × input speed; shared inertia sets both speeds and reactions; no copied speed or work. | `engine/physics/PhysicsTransmissionJoint.cs@a6c914e:L7-L13` | carry forward (law) | This is the converter law and gives "blocked output loads the input". |
| 2 | A shaft-to-slider coupling uses a winding lead in m/rad, admitted 0.025–0.2. | `parts/WoundSpringPart.cs@a6c914e:L97-L98`, `parts/WoundSpringPart.cs@a6c914e:L118-L124` | carry forward | Sourced lead range. |
| 3 | A mechanical socket binds one owned axial guide with a nonzero coordinate-per-radian. | `engine/ConnectionPort.cs@a6c914e:L23-L38`; `engine/MechanicalNetwork.cs@a6c914e:L22-L25` | carry forward | Typed port to joint binding. |
| 4 | Finite-inertia shaft: axial inertia m r²/2, transverse m(3r² + w²)/12. | `engine/SceneRotaryShaft.cs@a6c914e:L13-L19` | carry forward | Standard cylinder inertia. |
| 5 | Without supply the motor accepts no work and the plunger does not move. | `CuriousContraptions.tests/WoundSpringRuntimeTests.cs@a6c914e:L102-L141` | carry forward | Acceptance fact for "no command-only displacement". |
| 6 | Transmission and joint rows solved by the CPU prediction solver. | `engine/physics/PhysicsTransmissionJoint.cs@a6c914e:L38-L40` | do not carry forward | CPU solver path; replaced by a TGS Soft row in the worker. |

**Files harvested:** `engine/physics/PhysicsTransmissionJoint.cs`, `engine/SceneTransmissionJoint.cs`, `engine/SceneRotaryShaft.cs`, `engine/ConnectionPort.cs`, `engine/MechanicalNetwork.cs`, `engine/MachineData.cs`, `parts/WoundSpringPart.cs`, `parts/SpringPart.cs`, `CuriousContraptions.tests/WoundSpringRuntimeTests.cs`. Searched with no converter-specific hit: `content/puzzles.json`, `tools/Campaign`, `reference/` (terms: crank, rack, pinion, screw, converter).

## 5. Acceptance outline

Requirement row: [element-053](../requirements.md#element-053).

- **Chrome UI recipe.** In free play place a Battery, a Motor and the converter from the toolbox; place a Basketball in front of the carriage. Wire Battery → Motor (`PowerIn`) and belt Motor `Drive` → converter `DriveIn` with the contextual wiring control. Verify the placed parts, lead and stroke and the typed connections from the committed read.
- **Positive.** Run: the carriage travels lead × shaft angle and pushes the ball; the shaft witness marks and the carriage agree.
- **Negative/control.** No belt or no supply: the carriage stays at its authored position for the whole run. A wall in the carriage path: the carriage stops at contact, the motor stalls at its torque limit and its supplied work stops rising.
- **Boundaries.** Carriage reaches each end stop and stops the shaft; reversed motor retracts; lead 0.025 and 0.2; out-of-range lead or stroke rejected atomically; rotated placement.
- **Run/Reset.** Reset restores carriage at travel 0 (or authored position) and shaft angle exactly.
- **Save/Load.** Lead, stroke and the `DriveIn` link round-trip; an unknown socket rejects.
- **Integrations.** The scope index [todo-211](../requirements.md#todo-211) defines no separate integration task; shared interactions use the generic process register, here [IX-05 shaft torque transmission](../requirements.md#interaction-05) (blocked loads receive no free rotation) and [IX-01 contact impulse](../requirements.md#interaction-01) (carriage pushes cargo).

## 6. Open questions

1. Is the carriage's start position authored (anywhere in the stroke) or always travel 0? Proposed: travel 0. Owner decision.
2. Does the converter also expose a pass-through `Drive` output so a second consumer can share the shaft? Owner decision.
3. The binding omits ContactImpulse, yet the carriage pushes cargo by contact. Confirm contact is an implied shared capability. Owner decision.
4. Should the coupling allow back-driving (a pushed carriage turns the shaft)? The legacy law is bidirectional; a self-locking screw would need a one-way friction model. Owner decision.
