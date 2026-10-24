# EL-193 · Domino named-identity spec

**Current delivery (24 October 2026):** [EL-193 delivery slice](../invest/named-elements.md#element-193). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 named-identity spec (Batch J). Baseline commit `a6c914e`; every citation is `path@a6c914e:Lstart-Lend` and resolves with `git show a6c914e:<path> | sed -n 'start,endp'`. **Proposed** marks an unsourced design value with a one-line justification; the owner may revise it. All values are canonical IEEE-754 f32 game values inside the [game-grade envelope](../../gpu-f32-physics.md#game-grade-envelope); the current declaration still stores them as binary16 ([f32 migration status](../../gpu-f32-physics.md#f32-migration-status)) and they are recorded here by decimal meaning.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-193 |
| Name | Domino |
| Type | Mechanical |
| Anchor | [requirements.md#element-193](../requirements.md#element-193); [named entry](../invest/named-elements.md#element-193); scope [campaign-element-coverage](../requirements.md#campaign-element-coverage) |
| Proof owner | S299 |
| Refines / extends | Refines [CAT-023 domino](CAT-023-domino.md) ([requirement](../requirements.md#current-cat-023)) |
| Related | CAT-035 Lamp (the `domino_effect` goal), EL-205 Rope and EL-209 Fan (domino lanes in composite levels) |
| Roadmap story | [EL-193 delivery slice](../invest/named-elements.md#element-193); canonical proof owner and prerequisite decisions are recorded there. |
| Status | partial: delivered except the offset centre of mass |

## 2. Declaration

### Bodies and shapes
- One dynamic box, half extents 0.125 × 0.55 × 0.325 m (full 0.25 × 1.1 × 0.65 m), body origin at the box centre: the scene-baked box of `elements/Domino/domino.tscn`. Compiled from the package as one dynamic body and one Box collider by the shared generic compiler (`engine/gpu/WorkshopPhysicsCompiler.cs`); there is no Domino record (Story 9A.32).

### Mass and material
- Mass 0.4 kg; restitution 0.05; friction 0.6; bounce threshold 0.1 m/s; rolling resistance 0, as boxes declare, all declared by the package (`elements/Domino/DominoElementDeclaration.cs`). The placed tile has no parameters, so no authored value can carry another material.
- Inertia: homogeneous box law m(b² + c²)/3 on half extents (`engine/gpu/RigidMassProperties.cs@a6c914e:L41-L47`).
- Offset centre of mass: required by the CAT-023 row but not delivered. Local offset (0, −0.05, 0) m — **proposed**: lowering the centre by under 10% of the 0.55 m half-height keeps a standing tile stable to small nudges while a real strike still topples it.

### Constraints and joints
None. Standing, toppling and resting are contact behaviour only.

### Typed ports
| Socket | Domain | Direction | Source |
| --- | --- | --- | --- |
| `ActivationOut` | Activation | Output | `engine/gpu/WorkshopConnections.cs@a6c914e:L27-L31` |

There is no input: wiring into a domino is rejected at the port check (same lines).

### Sensors and activation
An orientation-threshold sensor fires once when the tile has turned 45° from its admitted pose (|⟨q, q₀⟩| ≤ cos 22.5°), evaluated at substep endpoints, sticky until Reset; it compiles only while the tile is wired. The threshold is the package's declared `SignalOrientationDescriptor(45)` (f32), compiled by `WorkshopPhysicsCompiler.OrientationSensorFor` and evaluated by the shipped worker engine (`CuriousContraptions.Simulation/WorkshopPhysicsEngine.cs`) (Story 9A.32).

### Work and energy stores
None. A falling tile converts its own potential energy; neighbours fall only through real contact.

### Parameters
None: the package declares `Parameters: []`, so the placed tile is a zero-parameter generic instance and the editor shows no panel.

### Cosmetic curves and UI bindings
- No cosmetic curve (the package declares no presentation binding); tile and pips follow the committed pose.
- Pick radius 0.6 m (`parts/DominoPart.cs@a6c914e:L14-L14`).

### Art
Tile in the Domino colour `#e8d4a6`; two pips, spheres of radius 0.045 m in `#384757` at (0.14, ±0.25, 0) (`parts/DominoPart.cs@a6c914e:L16-L18`; `parts/catalog/domino.tres@a6c914e:L13-L13`; [DESIGN colour system](../../../DESIGN.md#colour-system)).

### Catalogue and inventory entry
Id `domino`, title "Domino", category Motion, description "A physical tile: knock it over to hit the next domino. Its activation output signals once it has turned 45° from its placed pose." (`parts/catalog/domino.tres@a6c914e:L8-L14`). Historical registration was `WorkshopPartKind.Domino` (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`).

### Variants
One outcome in the row; no variants. The offset centre of mass is part of the CAT-023 declaration, not a variant.

## 3. Engine capabilities

Families: ContactImpulse, EnvironmentState, FiniteLedger, GeometryQuery, JointConstraint, RigidBodyDynamics, SlidingFriction, plus StateTransaction ([map row](../general-engine-element-map.md); [element-02.json](../../coverage/engine/element-02.json)).

**Exists now**
- Dynamic box, 15-axis SAT box–box manifolds and friction: `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L503-L620`, `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L701-L719`.
- Orientation-threshold sensor: `engine/gpu/OrientationSensorDeclaration.cs@a6c914e:L7-L22`.
- The `domino_effect` level as typed data: `engine/gpu/DominoEffect.cs@a6c914e:L1-L56`.

**Missing**
- Non-homogeneous mass properties (an offset centre of mass inside one box): `RigidMassProperties.Compile` takes the collider centre as the centre of mass (`engine/gpu/RigidMassProperties.cs@a6c914e:L49-L49`). Owner S299; follow-up of CAT-023.

**Dependencies.** None outstanding for the delivered behaviour.

## 4. Sources and legacy

- Requirement: "Finite rigid body tips through real contact, mass and centre of gravity"; outcome "Separated neighbour does not fall from a scripted propagation event" ([element-193](../requirements.md#element-193)).
- CAT-023 row: offset centre of mass, 45° output, input rejected, grounded close-gap chain versus separated gap ([current-cat-023](../requirements.md#current-cat-023)).

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | Tile artwork only; the shared solver owns standing and toppling | `parts/DominoPart.cs@a6c914e:L6-L19` | carry forward (art) | Declaration-only element. |
| 2 | Accepted: wiring into a domino, foreign inventory, exhausted tiles and changed fixtures reject | `CuriousContraptions.tests/DominoEffectTests.cs@a6c914e:L60-L79` | carry forward | Input rejection. |
| 3 | Accepted: the goal solves only when the end domino and the lamp are both latched | `CuriousContraptions.tests/DominoEffectTests.cs@a6c914e:L80-L96` | carry forward | Real propagation required. |
| 4 | Level `domino_effect`: "Fill the gap with four dominoes"; inventory domino 4 | `content/puzzles.json@a6c914e:L1129-L1528` | carry forward | First lesson. |

**Files harvested:**
- `parts/DominoPart.cs`
- `parts/catalog/domino.tres`
- `CuriousContraptions.tests/DominoEffectTests.cs`
- `content/puzzles.json` (level `domino_effect`)
- The full domino harvest, including the composite domino lanes of `tools/Campaign/Program.cs`, belongs to [CAT-023](CAT-023-domino.md) (Batch A). Searched with no further EL-193 knowledge: `engine/physics/`, `engine/bridge/`, `reference/`, `diagnostics/`.

## 5. Acceptance outline

Follow [element-193](../requirements.md#element-193) and [current-cat-023](../requirements.md#current-cat-023).
- **Chrome UI recipe.** Open `domino_effect`; place four dominoes from the toolbox with the move gizmo at close gaps; Run.
- **Positive.** Each tile tips the next by contact; the locked end tile turns 45° and lights the wired lamp.
- **Negative/control.** A gap wider than the tile height leaves the next tile standing; wiring into a domino is refused.
- **Boundaries.** Gap just under and over 1.1 m; tile on a ramp; offset centre of mass when delivered.
- **Run/Reset.** Tiles stand at authored poses; the sensor rearms.
- **Save/Load.** Poses and the end-to-lamp wire round-trip.
- **Integrations.** Campaign row 1 (domino) first use 1–10, reuse 21–30, 41–50, 91–100, 136–150 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)); the `domino_effect` lesson with CAT-035 lamp; composite domino lanes alongside fans, springboards and conveyors (CAT-023 spec).

## 6. Open questions

1. Offset centre of mass value and direction (proposed (0, −0.05, 0) m): unspecified — owner decision.
2. Should players be able to resize dominoes, or is the single admitted material final? Owner decision.
