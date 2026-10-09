# EL-202 · Conveyor named-identity spec

Story 7.0 named-identity spec (Batch J). Baseline commit `a6c914e`; every citation is `path@a6c914e:Lstart-Lend` and resolves with `git show a6c914e:<path> | sed -n 'start,endp'`. **Proposed** marks an unsourced design value with a one-line justification; the owner may revise it. All values are canonical IEEE-754 f32 game values inside the [game-grade envelope](../../gpu-f32-physics.md#game-grade-envelope).

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-202 |
| Name | Conveyor |
| Type | Mechanical |
| Anchor | [requirements.md#element-202](../requirements.md#element-202); [named entry](../invest/named-elements.md#element-202); scope [campaign-element-coverage](../requirements.md#campaign-element-coverage) |
| Proof owner | S311 |
| Refines / extends | Refines [CAT-019 conveyor](CAT-019-conveyor.md) ([requirement](../requirements.md#current-cat-019)); the full legacy harvest is in that spec |
| Related | EL-199 Electric motor (drive), EL-200 Drive belt (link), EL-203 Reverse transmission |
| Roadmap story | Story 11.1 (CAT-042 with CAT-019) |
| Status | not started |

## 2. Declaration

All values are sourced through [CAT-019](CAT-019-conveyor.md); this EL adds no new value.

### Bodies and shapes
- **Carrier (static).** Box full size length × 0.24 × width m at the origin; belt top at +0.12 m (`parts/ConveyorPart.cs@a6c914e:L84-L90`).
- **Driven roller (dynamic shaft).** Radius 0.16 m, axial length width + 0.1 m, at (−length/2 + 0.15, −0.07, 0) (`parts/ConveyorPart.cs@a6c914e:L20-L24`).

### Mass and material
Roller 0.5 kg with cylinder inertia (`parts/ConveyorPart.cs@a6c914e:L20-L20`, [CAT-019](CAT-019-conveyor.md)); belt material restitution 0.05, bounce threshold 0.1 m/s, friction 0.3 (`parts/ConveyorPart.cs@a6c914e:L72-L72`).

### Constraints and joints
- Free roller hinge about local +Z, connected collision disabled (`parts/ConveyorPart.cs@a6c914e:L40-L45`).
- Driven surface: the carrier face with normal +Y moves along +X at −`surface_per_radian` × the roller's relative hinge rate (`parts/ConveyorPart.cs@a6c914e:L46-L49`; law `engine/physics/DrivenSurface.cs@a6c914e:L9-L20`). Cargo moves only through contact friction on that face.

### Typed ports
| Socket | Domain | Direction | Source |
| --- | --- | --- | --- |
| `DriveIn` | Mechanical | Input, −1 per radian | `parts/ConveyorPart.cs@a6c914e:L64-L71` |
| `Drive` | Mechanical | Output, −1 per radian (relay) | same |

### Sensors and activation
None. An activation command is not mechanical energy ([CAT-019](CAT-019-conveyor.md)).

### Work and energy stores
None of its own; work arrives only through `DriveIn`, and the roller's finite kinetic energy coasts after supply loss.

### Parameters

| Name | Type | Range | Default | Unit | Source |
| --- | --- | --- | --- | --- | --- |
| `length` | f32 | > 0 | 3.0 | m | `parts/ConveyorPart.cs@a6c914e:L74-L82`, `parts/catalog/conveyor.tres@a6c914e:L14-L14` |
| `width` | f32 | > 0 | 1.2 | m | same |
| `surface_per_radian` | f32 | ≠ 0, signed | 0.666666667 | m per rad | same |

Upper bounds are a [CAT-019 open question](CAT-019-conveyor.md#6-open-questions).

### Cosmetic curves and UI bindings
Treads, port pulleys (radius 0.22 m, `parts/ConveyorPart.cs@a6c914e:L111-L117`) and the direction arrow follow committed roller motion ([CAT-019](CAT-019-conveyor.md)). UI: parameters through the configuration pattern (`ui/WorkshopConfiguration.cs@a6c914e:L43-L75`).

### Art
Conveyor colour `#d69c47` (`parts/catalog/conveyor.tres@a6c914e:L13-L13`); carrier `#273744` (`parts/ConveyorPart.cs@a6c914e:L90-L90`); [DESIGN colour system](../../../DESIGN.md#colour-system); [mechanical work feedback](../../../DESIGN.md#mechanical-work-feedback).

### Catalogue and inventory entry
Id `conveyor`, title "Conveyor belt", category Motion (`parts/catalog/conveyor.tres@a6c914e:L8-L14`). Add `WorkshopPartKind.Conveyor` with a counted allowance (`engine/gpu/WorkshopInventory.cs@a6c914e:L9-L23`).

### Variants
One outcome in the row; no variants.

## 3. Engine capabilities

Families: ContactImpulse, EnvironmentState, FiniteLedger, FiniteWorkActuation, GeometryQuery, JointConstraint, RigidBodyDynamics, ShaftTorque, SlidingFriction, plus StateTransaction ([map row](../general-engine-element-map.md); [element-03.json](../../coverage/engine/element-03.json)).

**Exists now**
- Static carrier box, dynamic cargo and Coulomb friction: `engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L120`; `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L701-L719`.

**Missing** ([CAT-019](CAT-019-conveyor.md#3-engine-capabilities))
- Roller hinge (Story 10.3), driven-surface friction law with shaft reaction, mechanical domain and sockets, cylinder collider, cosmetic bindings: Story 11.1. Owner S311.

**Dependencies.** CAT-042 motor (Story 11.1) and through it CAT-005 battery (Story 8.1).

## 4. Sources and legacy

- Requirement: "Finite-work driven surface moves cargo through actual contact and friction"; outcome "Unpowered or noncontact cargo is not carried along a hidden path" ([element-202](../requirements.md#element-202)).

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | Driven surface speed = travel-per-radian × axis·(ω_shaft − ω_carrier) | `engine/physics/DrivenSurface.cs@a6c914e:L9-L20` | carry forward (law), do not carry forward (CPU class) | Contact transport without copied speed. |
| 2 | Accepted: an unpowered belt holds the ball until a switched motor drives it, then the ball moves past x = 0.5 | `CuriousContraptions.tests/ConveyorTests.cs@a6c914e:L57-L83` | carry forward | Unpowered control then positive. |
| 3 | Accepted: a body in another depth plane is not transported | `CuriousContraptions.tests/ConveyorTests.cs@a6c914e:L85-L106` | carry forward | Noncontact control. |
| 4 | Accepted: after supply loss the belt coasts with constant energy; Reset zeroes angles | `CuriousContraptions.tests/MechanicalTests.cs@a6c914e:L113-L136` | carry forward | Finite inertia. |

**Files harvested:**
- `parts/ConveyorPart.cs`
- `parts/catalog/conveyor.tres`
- `engine/physics/DrivenSurface.cs`
- `CuriousContraptions.tests/ConveyorTests.cs`
- `CuriousContraptions.tests/MechanicalTests.cs`
- The remaining conveyor files are harvested in [CAT-019](CAT-019-conveyor.md#4-legacy-harvest) (Batch C). Searched with no further EL-202 knowledge: `engine/bridge/`, `reference/`, `diagnostics/`.

## 5. Acceptance outline

Follow [element-202](../requirements.md#element-202) and the [CAT-019 outline](CAT-019-conveyor.md#5-acceptance-outline).
- **Chrome UI recipe.** Open `conveyor_courier`; place the conveyor under the ball; connect battery → motor `PowerIn` and motor `Drive` → conveyor `DriveIn`; Run.
- **Positive.** The ball is carried along +X into the receiver.
- **Negative/control.** Unpowered motor or no link gives no transport; a ball beside the belt is untouched.
- **Boundaries.** Loaded and stalled belt, coasting, reversed ratio, rotated belt.
- **Run/Reset.** Roller and payload return exactly.
- **Save/Load.** Parameters, pose and links round-trip.
- **Integrations.** Retained belt and conveyor behaviour [todo-151](../requirements.md#todo-151) and conveyor teaching routes [todo-152](../requirements.md#todo-152); campaign row 2 ("... conveyor ..."): first use 11–20, moving/load variants into 41–50, reuse 31–50, 61–80, 136–150 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)); driven by CAT-042 motor, reversed by CAT-057.

## 6. Open questions

None beyond the [CAT-019 open questions](CAT-019-conveyor.md#6-open-questions).
