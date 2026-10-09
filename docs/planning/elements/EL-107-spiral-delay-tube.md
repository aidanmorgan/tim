# EL-107 · Spiral gravity-delay tube named-identity spec

Story 7.0 named-identity spec (Batch J). Baseline commit `a6c914e`; every citation is `path@a6c914e:Lstart-Lend` and resolves with `git show a6c914e:<path> | sed -n 'start,endp'`. **Proposed** marks an unsourced design value with a one-line justification; the owner may revise it. All values are canonical IEEE-754 f32 game values ([numeric authority](../../gpu-f32-physics.md#numeric-representation-and-precision)) inside the [game-grade envelope](../../gpu-f32-physics.md#game-grade-envelope).

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-107 |
| Name | Spiral gravity-delay tube |
| Type | Mechanical |
| Anchor | [requirements.md#element-107](../requirements.md#element-107); [named entry](../invest/named-elements.md#element-107); scope index [todo-248](../requirements.md#todo-248) |
| Proof owner | S380 |
| Refines / extends | No CAT spec. Extends the clear-tube family: [CAT-048 pipe](CAT-048-pipe.md), [CAT-049 45° bend](CAT-049-pipe_bend_45.md), [CAT-050 90° bend](CAT-050-pipe_bend_90.md) |
| Related | EL-108 Powered airlift, EL-109 Speed-sensitive trapdoor (same scope index) |
| Roadmap story | unscheduled |
| Status | not started |

## 2. Declaration

### Bodies and shapes
- One static body: a hollow helical tube. Bore radius 0.65 m, wall middle radius 0.70 m and end collars radius 0.78 m × half-width 0.09 m, matching the standard tube profile (`engine/gpu/WorkshopPipe.cs@a6c914e:L8-L19`, an uncompiled current-tree declaration; `parts/PipeBendPart.cs@a6c914e:L25-L35`). Sourced, so every tube mouth snaps to it.
- Helix centreline radius 2.4 m — **proposed**: equals the sourced bend centreline radius (`parts/PipeBendPart.cs@a6c914e:L9-L9`), so the helix reuses the hollow torus-segment curvature of Story 6.9.
- Pitch 1.6 m per turn — **proposed**: twice the 0.78 m collar radius plus 0.04 m, so adjacent coils never intersect.
- Turns 1–3, default 2 (see Parameters). Two mouths: `Inlet` at the top, tangent to the helix; `Outlet` at the bottom.
- Current shapes are Sphere, Box and Plane only (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L6-L7`); a hollow helical collider is missing.

### Mass and material
- Static, no mass (static bodies require zero mass, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L70-L77`).
- Inner wall material: restitution 0.15, bounce threshold 0.1 m/s, friction 0.3, rolling resistance 0 — sourced from the legacy bend material (`parts/PipeBendPart.cs@a6c914e:L12-L12`) and declared through `ContactMaterialDeclaration` (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L54`). The ball's own rolling resistance (Basketball 0.035) provides the per-contact loss (`engine/gpu/WorkshopConstruction.cs@a6c914e:L45-L53`).

### Constraints and joints
None: the tube is a static guide. Delay arises only from travel distance, gravity, contact and friction.

### Typed ports
None in the activation or electrical domains. Two geometric tube mouths (`Inlet`, `Outlet`) of bore 0.65 m, the same mouth contract as the legacy tube parts (`parts/PoweredGatePart.cs@a6c914e:L26-L30`).

### Sensors and activation
None. The requirement forbids a hidden timer; arrival is observed by a separate detector (CAT-002) or goal.

### Work and energy stores
None. Cargo potential energy converts to kinetic energy and is dissipated by contact, friction and rolling resistance.

### Parameters

| Name | Type | Range | Default | Unit | Source |
| --- | --- | --- | --- | --- | --- |
| `turns` | enum `SpiralTurns { One, Two, Three }` | 1–3 | Two | turns | **proposed**: whole turns keep both mouths on the same vertical side, so routes stay readable |
| `handedness` | enum `Handedness { Clockwise, CounterClockwise }` | 2 values | Clockwise | — | **proposed**: lets the outlet face either direction without a mirrored copy |

Height follows: turns × 1.6 m (3.2 m by default), within the 16.8 × 9.8 m bench ([DESIGN geometry](../../../DESIGN.md#geometry-materials-and-lighting)).

### Cosmetic curves and UI bindings
- No cosmetic curve: the cargo is visible through the clear shell, which is the only timing cue.
- UI: placement and rotation gizmo; `turns` and `handedness` through the contextual configuration pattern used by the bumper strength (`ui/WorkshopConfiguration.cs@a6c914e:L43-L75`).

### Art
Clear pipe palette `#66b8c9`, shell alpha 0.16, cream `#fff8e9` collars and navy `#293954` rails ([DESIGN colour system](../../../DESIGN.md#colour-system); legacy art `parts/PipePart.cs@a6c914e:L21-L27`). **Proposed**: reuse the pipe identity so players read it as a tube.

### Catalogue and inventory entry
Id `spiral_tube`, title "Spiral delay tube", category Motion — **proposed**, following the catalogue fields of `parts/catalog/conveyor.tres@a6c914e:L8-L14`. Add `WorkshopPartKind.SpiralTube` and a counted `PartAllowance` (`engine/gpu/WorkshopInventory.cs@a6c914e:L9-L23`).

### Variants
The requirement row names one outcome and no variants. The campaign coverage row lists "spiral delay ... variants" without naming them ([campaign-element-coverage](../requirements.md#campaign-element-coverage)); `turns` and `handedness` are configuration, not separate variants (open question 1).

## 3. Engine capabilities

Families: ContactImpulse, EnvironmentState, FiniteLedger, GeometryQuery, JointConstraint, RigidBodyDynamics, SlidingFriction, plus StateTransaction from the binding ([map row](../general-engine-element-map.md); [element-02.json](../../coverage/engine/element-02.json), proof owner S380).

**Exists now**
- Dynamic sphere cargo, gravity, drag and contact with friction and rolling resistance: `engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L120`; `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L701-L719`, `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L1271-L1279`.
- Speculative contacts against tunnelling at 480 Hz substeps: `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L208-L215`.

**Missing**
- Hollow tube collider (GeometryQuery/ContactImpulse family): Stories 6.6, 6.7, 6.9 and 6.10 build straight and torus-segment bores; a helical bore extends them. Decision owner: S380.
- Mouth snapping for a helix: same family, Story 6.10 joined-route rules.

**Dependencies.** CAT-048/049/050 tube contract (Stories 6.6, 6.7, 6.9 and 6.10); CAT-001 Basketball and CAT-014 Bowling ball as cargo; CAT-002 ball detector (Story 6.8) for an observable arrival.

## 4. Sources and legacy

- Requirement: "Long physical descent delays arrival through travel distance and contact"; outcome "Changing speed changes transit time; no precise hidden timer" ([element-107](../requirements.md#element-107)).
- Scope record [todo-248](../requirements.md#todo-248) is navigation only; campaign coverage reserves spiral delay for first use 21–30, specialist routes 91–100, reuse 41–50, 111–120 and 136–150 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)).
- Decisions: none bounded for this element in [decisions.md](../invest/decisions.md); the map's decision column is "Source D owner".

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | Standard bore 0.65 m, wall 0.70 m, collars 0.78 m × 0.09 m | `engine/gpu/WorkshopPipe.cs@a6c914e:L8-L19` | carry forward | Mouths must match every other tube. |
| 2 | Bend centreline radius 2.4 m; bend material (0.15, 0.1, 0.3) | `parts/PipeBendPart.cs@a6c914e:L9-L12` | carry forward | Curvature and material of the existing curved tube. |
| 3 | Bend collider is a hollow torus segment built per-part (`Bends.Add`) | `parts/PipeBendPart.cs@a6c914e:L30-L30` | do not carry forward | Per-part collider assembly; the helix must be a generic declared shape. |

No legacy spiral, helix or delay-tube implementation exists.

**Files harvested:**
- `parts/PipeBendPart.cs`
- `parts/PipePart.cs` (tube art)
- `parts/PoweredGatePart.cs` (tube mouth contract)
- `parts/catalog/conveyor.tres` (catalogue field format only)
- Searched with no hit for `spiral`, `helix` or `delay tube`: `parts/`, `engine/` (including `engine/physics/` and `engine/bridge/`), `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/`, `reference/`, `diagnostics/`.

## 5. Acceptance outline

Follow [element-107](../requirements.md#element-107) and the [mechanics profile](../invest/profiles.md#mechanics).
- **Chrome UI recipe.** Place the spiral tube from the toolbox, rotate it with the gizmo, place a ramp feeding its `Inlet` and a receiver under its `Outlet`. Verify the committed tube pose, turns and mouth alignment, then Run.
- **Positive.** A Basketball enters, descends the helix visibly and exits; the arrival time is longer than a straight drop of the same height.
- **Negative/control.** A ball released at a higher entry speed arrives sooner; a ball released beside the tube is not delayed; no event fires without a physical arrival.
- **Boundaries.** One and three turns; both handedness values; Bowling ball versus Basketball; a ball that stalls at low speed stays in the tube rather than teleporting.
- **Run/Reset.** Cargo returns to its authored pose; the tube is unchanged.
- **Save/Load.** `turns`, `handedness` and pose round-trip through `WorkshopSaveCodec` (`engine/gpu/WorkshopSaveCodec.cs@a6c914e:L6-L10`).
- **Integrations.** Scope index [todo-248](../requirements.md#todo-248) (no separate integration task); campaign row "spiral delay, airlift and trapdoor variants": first use 21–30, specialist routes 91–100, reuse 41–50, 111–120, 136–150 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)); joined tube routes with CAT-048–CAT-050 and arrival detection by CAT-002.

## 6. Open questions

1. Are the "spiral delay variants" in the campaign coverage row distinct identities or only `turns`/`handedness` configuration? Unspecified — owner decision.
2. Helix centreline radius 2.4 m and pitch 1.6 m (proposed): confirm or resize for bench readability. Owner decision.
3. Should the shell be fully clear or carry index rings so players can count turns? Owner decision.
