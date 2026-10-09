# EL-073 · Brick barrier declaration readiness spec

Story 7.0 named-identity spec. Baseline commit `a6c914e`. Legacy citations use `path@a6c914e:Lstart-Lend` and stay retrievable from git history after the Epic 7 purge. Values marked **proposed** are design values the owner may revise.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-073 |
| Name | Brick barrier |
| Type | Mechanical |
| Requirement anchor | [element-073](../requirements.md#element-073); scope index [todo-199](../requirements.md#todo-199) |
| Named entry | [named-elements.md#element-073](../invest/named-elements.md#element-073); proof owner S643 |
| CAT spec refined or extended | Extends [CAT-066 wall](CAT-066-wall.md) (delivered static resizable box, Stories 1.4/1.5) with its own declared contact material and strength properties |
| Related identities | [EL-074 wood barrier](EL-074-wood-barrier.md) (independent material; must not share brick values), structural fracture consumers (S563), thermal identities TH-* (Batch N) for temperature-dependent strength |
| Roadmap story | unscheduled |
| Status | not started |

## 2. Declaration

### Bodies and shapes

One static box with the Wall's resizable local dimensions and bounds: width 0.4–8, height 0.4–6, thickness 0.12–2 m (`engine/gpu/WorkshopWall.cs@a6c914e:L6-L16`). Default 2.0 × 1.5 × 0.3 m. **Proposed** default: a squat, thick block reads as masonry, distinct from the 3 × 2 × 0.25 m Wall default (`engine/gpu/WorkshopWall.cs@a6c914e:L8-L8`).

### Mass and material

Static (no mass). Declared brick material:

| Property | Value | Unit | Source |
| --- | --- | --- | --- |
| Restitution | 0.3 | — | **Proposed**: a Basketball (0.55) rebounds at 0.165 of approach speed under product mixing, visibly deader than the Wall's restitution 1 (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L109-L110`) |
| Bounce threshold | 0.1 | m/s | Common to all current declarations (`engine/gpu/WorkshopConstruction.cs@a6c914e:L45-L53`) |
| Friction | 0.7 | — | **Proposed**: rough masonry; geometric mean with a ball (0.3) gives 0.46 |
| Rolling resistance | 0 | — | Non-sphere surfaces declare zero (`docs/gpu-f32-physics.md` materials row) |
| `fracture_work` | 400 | J | **Proposed** strength: more than a 4 kg Bowling ball at 10 m/s (200 J), so it is intact in every current construction; consumed by S563 once fracture exists |
| Density | 40 | kg/m³ | **Proposed** on the catalogue's game scale, whose bodies span about 2–44 kg/m³ (Domino 2.2, Basketball 6.1, Bowling ball 43.5; from `engine/gpu/WorkshopConstruction.cs@a6c914e:L45-L53` and `engine/gpu/WorkshopDomino.cs@a6c914e:L9-L9`): brick sits at the heavy end, near the Bowling ball. The default panel (0.9 m³) is 36 kg. Real brick (~1900 kg/m³) is reference only: it would make the default panel 1710 kg, above the 1024 kg dynamic-mass bound (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L78-L78`) |

### Constraints and joints

None.

### Typed ports

None.

### Sensors and activation

None.

### Work and energy stores

None.

### Parameters

| Name | Type | Range | Default | Unit | Source |
| --- | --- | --- | --- | --- | --- |
| width / height / thickness | f32 | 0.4–8 / 0.4–6 / 0.12–2 | 2.0 / 1.5 / 0.3 | m | Bounds from `engine/gpu/WorkshopWall.cs@a6c914e:L9-L10`; defaults **proposed** |

### Cosmetic curves and UI bindings

None. The Wall's resize handles and Undo apply unchanged.

### Art

Terracotta brick courses `#a96f5c` on the Physical-wall body `#c28f52` with cream mortar lines `#f9e8c9` (the Wall's stripe colour, `parts/WallPart.cs@a6c914e:L15-L17`); terracotta is an existing palette token (`DESIGN.md@a6c914e:L159-L159`). The course pattern is the shape cue that tells brick from wood. Catalogue colour **proposed**: terracotta `#a96f5c`.

### Catalogue and inventory entry

Id `brick_barrier`, title "Brick wall", category "Structure"; `WorkshopPartKind.BrickBarrier` appended last to the free inventory (`engine/gpu/WorkshopInventory.cs@a6c914e:L49-L60`). **Proposed**.

### Variants

The requirements row names no variants or modes. One element is specified.

## 3. Engine capabilities

Binding: ContactImpulse, EnvironmentState, FiniteLedger, GeometryQuery, JointConstraint, RigidBodyDynamics, SlidingFriction (map); coverage JSON adds StateTransaction.

**Exists now**

- Static resizable box (the delivered Wall): `engine/gpu/WorkshopWall.cs@a6c914e:L6-L40`.
- Per-part `ContactMaterialDeclaration` and pair mixing (product restitution, geometric-mean friction): `engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L54`; `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L702-L766`.
- Tunnelling bound: no penetration through barriers ≥ 1 cm thick at ≤ 64 m/s (game-grade envelope, `docs/gpu-f32-physics.md`).

**Missing**

- Static parts with a non-default material: the compiler gives every static surface (1, 0.1, 0.3) (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L109-L110`); a declared per-kind static material is needed. No story; decision owner S643.
- Strength and density material fields and the fracture criterion: S563; no story.

**Dependencies.** None for the intact barrier (balls CAT-001/014 delivered).

## 4. Sources and legacy

- Requirements row: "No passage through intact geometry." No variants.

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | Legacy static surfaces use restitution 1, bounce threshold 0.1, friction 0.3 (the source `MachinePart.InitialContactMaterial`). | `engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L109-L110` | do not carry forward for brick | The row requires the barrier's own declared contact properties. |
| 2 | Wall: three resizable local dimensions; the common box declaration owns all contact physics. | `parts/WallPart.cs@a6c914e:L6-L12` | carry forward | Same resize model. |
| 3 | Wall catalogue dimensions stored as binary16 bits (`WidthBits = 16896` = 3 m, `HeightBits = 16384` = 2 m, `ThicknessBits = 13312` = 0.25 m). | `parts/catalog/wall.tres@a6c914e:L8-L11` | do not carry forward the bits | f32 contract: carry the decimal meaning only. |

**Files harvested:** `parts/WallPart.cs`, `parts/catalog/wall.tres` (both harvested in full by CAT-066), `engine/WallDimensionsResource.cs` (checked). Searched for brick, masonry: no hit.

## 5. Acceptance outline

Requirement row: [element-073](../requirements.md#element-073).

- **Chrome UI recipe.** Place a Brick wall upright across a Ramp's run-out with a Basket behind it; release a Bowling ball from the ramp top. Place a Basketball 1 m above a horizontal Brick wall and another above a Wall. Verify placement, dimensions and declared materials from the committed read.
- **Positive.** The Bowling ball stops at the brick face and never reaches the Basket; the Basketball rebounds from brick to about 0.03 m (e² × 1 m) and from the Wall to about 0.3 m.
- **Negative/control.** Same construction with the brick moved aside: the ball reaches the Basket.
- **Boundaries.** Thickness 0.12 m at 20 m/s impact (no tunnelling); resize to bounds; out-of-range dimensions rejected.
- **Run/Reset and Save/Load.** Ball poses restore; dimensions and kind round-trip.
- **Integrations.** The scope index [todo-199](../requirements.md#todo-199) defines no separate integration task; shared interactions use [IX-01 contact impulse](../requirements.md#interaction-01), [IX-02 sliding friction](../requirements.md#interaction-02) and, once admitted, [IX-37 structural fracture](../requirements.md#interaction-37). The [campaign coverage ledger](../requirements.md#campaign-element-coverage) reserves first use of ramp/wall/material variants in levels 1–10.

## 6. Open questions

1. Resizable like the Wall (proposed) or fixed brick blocks that stack? Owner decision.
2. Strength value and failure mode belong to S563; confirm 400 J as "intact in current play". Owner decision.
3. Optical opacity: brick blocks light like the Wall (proposed). Owner decision.
4. **Material density scale.** Declared densities are proposed on the catalogue's game scale (about 2–44 kg/m³), not real values. Confirm that scale for all structural materials, and decide what happens to a large panel (an 8 × 6 × 2 m brick panel is 3840 kg at 40 kg/m³, above the 1024 kg dynamic bound) if fracture ever makes it dynamic. Owner decision.
