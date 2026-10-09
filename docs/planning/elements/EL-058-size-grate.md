# EL-058 · Size grate declaration readiness spec

Story 7.0 named-identity spec. Baseline commit `a6c914e`. Legacy citations use `path@a6c914e:Lstart-Lend` and stay retrievable from git history after the Epic 7 purge. Values marked **proposed** are design values the owner may revise.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-058 |
| Name | Size grate |
| Type | Mechanical |
| Requirement anchor | [element-058](../requirements.md#element-058); scope index [todo-320](../requirements.md#todo-320) |
| Named entry | [named-elements.md#element-058](../invest/named-elements.md#element-058); proof owner S367 |
| CAT spec refined or extended | none. Related: [CAT-001 ball](CAT-001-ball.md) and [CAT-014 bowling](CAT-014-bowling.md) (the two sizes it separates), [CAT-004 basket](CAT-004-basket.md) (catch below), [CAT-066 wall](CAT-066-wall.md) (static panel family) |
| Related identities | [EL-071 straight metal ball pipe](EL-071-straight-metal-ball-pipe.md) (same 0.62 m gauge), [EL-059 weight tray](EL-059-weight-tray.md), [EL-060 electromagnet](EL-060-electromagnet.md) (sorting by material, todo-320 integration) |
| Roadmap story | unscheduled |
| Status | not started |

## 2. Declaration

### Bodies and shapes

- One static body: a rectangular frame with a 3 × 3 grid of square openings of side `opening`, separated by bars of square section 0.08 m; plate thickness 0.08 m. Footprint = 3·opening + 4·0.08 m (2.18 m at default). **Proposed**: three openings per side give a target wide enough for a rolling ball; the 0.08 m bar is ⅔ of the 0.12 m Wall minimum thickness (`engine/gpu/WorkshopWall.cs@a6c914e:L9-L9`), so the grid reads lighter than a wall while staying well above the 1 cm tunnelling bound.
- Compiled as 4 + 4 static box colliders (frame edges and inner bars). With current capacity 64 colliders (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L166-L172`) one grate costs 8.

### Mass and material

Static. Bars: restitution 0.3, bounce threshold 0.1 m/s, friction 0.3, rolling resistance 0. **Proposed**: a dull metal grid so oversized balls settle on it rather than bounce off the bench.

### Constraints and joints

None: a static obstacle.

### Typed ports

None. Passage is purely geometric; identity labels never select passage (row outcome).

### Sensors and activation

None required. The binding lists SignalPropagation (shared with todo-320 sorters); no signal port is required by the row (Open question 2).

### Work and energy stores

None.

### Parameters

| Name | Type | Range | Default | Unit | Source |
| --- | --- | --- | --- | --- | --- |
| `opening` | f32 | 0.3–1.2 | 0.62 | m (square side) | **Proposed**: passes a Bowling ball (diameter 0.56 m) with 0.03 m clearance per side and blocks a Basketball (0.68 m), using current ball radii (`engine/gpu/WorkshopConstruction.cs@a6c914e:L45-L53`) |

Closed parameter enum `GrateParameter { Opening }`.

### Cosmetic curves and UI bindings

None (static). The opening width is shown by the geometry; no inspector.

### Art

Pale grey metal bars `#ccd9df` on a navy frame `#293954`, cream corner studs `#fff8e9`. A small engraved gauge mark (circle inside a square) shows the opening as a shape cue. Catalogue colour **proposed**: navy `#293954`.

### Catalogue and inventory entry

Id `size_grate`, title "Sorting grate", category "Motion"; `WorkshopPartKind.SizeGrate` appended last to the free inventory (`engine/gpu/WorkshopInventory.cs@a6c914e:L49-L60`). **Proposed**.

### Variants

The requirements row names no variants or modes. One element with an `opening` parameter.

## 3. Engine capabilities

Binding: ContactImpulse, EnvironmentState, GeometryQuery, RigidBodyDynamics, SignalPropagation (map and coverage JSON agree). Map decision: S257 typed power versus signal.

**Exists now**

- Static box colliders and sphere–box contact manifolds with speculative margin: `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L120`; `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L345-L407`.
- Rolling resistance and friction rows that let a ball roll across bars: `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L702-L766`.

**Missing**

- A static multi-box (compound) part compiled from one placed instance. The Receiver already compiles five static boxes from one part (`engine/gpu/ReceiverGeometry.cs@a6c914e:L10-L18`), so the pattern exists; only the grate's parameterised layout is new. No story.
- Resize control for `opening` (or fixed size; Open question 1).

**Dependencies.** Two ball sizes: CAT-001 Basketball and CAT-014 Bowling ball (both delivered).

## 4. Sources and legacy

- Requirements row: "Oversized object remains blocked; identity labels do not select passage." No variants.
- Integration task (todo-320): "Sorting depends on observable size, mass or material" ([requirements.md#todo-320](../requirements.md#todo-320)).
- Research row: "Material/size sorter — aperture geometry … physical apertures sort actual bodies; no hidden ID-based routing" (`docs/component-research.md@a6c914e:L82-L82`).

No legacy grate exists. Shared aperture facts:

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | Hollow apertures report a minimum clearance that a passing ball must respect (bore ≥ 0.64 m for the 0.65 m tube). | `CuriousContraptions.tests/HollowGeometryTests.cs@a6c914e:L54-L55` | carry forward (pattern) | Aperture pass/fail is clearance against the body's actual radius. |
| 2 | Hollow contact surfaces were proof-graded with maximum surface error bounds. | `CuriousContraptions.tests/HollowGeometryTests.cs@a6c914e:L209-L213` | do not carry forward | Proof-grade certificate; game-grade envelope applies. |

**Files harvested:** `CuriousContraptions.tests/HollowGeometryTests.cs`. Searched with no hit: `parts/`, `engine/`, `content/puzzles.json`, `tools/Campaign`, `reference/` (terms: grate, sieve, sorter, aperture).

## 5. Acceptance outline

Requirement row: [element-058](../requirements.md#element-058).

- **Chrome UI recipe.** Place a Ramp feeding onto a Sorting grate laid flat on two Walls above a Basket; place a Bowling ball and a Basketball at the ramp top. Verify placement and `opening` from the committed read.
- **Positive.** Run: the Bowling ball drops through an opening into the Basket (capture event).
- **Negative/control.** The Basketball rolls across or rests on the bars and never reaches the Basket. A Bowling ball released beside the grate does not count. Swapping the balls' catalogue identity is impossible; passage depends only on radius.
- **Boundaries.** `opening` 0.55 m (Bowling ball blocked), 0.58 m (passes with 0.01 m side clearance), 0.68 m (Basketball still blocked at exactly its diameter, passes at 0.70 m); fast impact (8 m/s) onto bars does not tunnel; out-of-range `opening` rejected.
- **Run/Reset and Save/Load.** Ball poses restore; `opening` round-trips.
- **Integrations.** Cross-element task [sequence-task-371](../requirements.md#sequence-task-371) under [todo-320](../requirements.md#todo-320): "Sorting depends on observable size, mass or material", with every named sorter keeping its own obligation.

## 6. Open questions

1. Is `opening` player-adjustable (resize handle) or fixed per catalogue entry? Owner decision.
2. Should the grate emit a typed signal (for example, an object-passed occurrence), given the binding lists SignalPropagation? Owner decision.
3. Round holes versus square openings (square passes a ball whose diameter ≤ side; round is stricter on diagonal cargo such as Dominoes). Proposed: square. Owner decision.
