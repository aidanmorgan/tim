# EL-072 · Curved metal ball pipe declaration readiness spec

Story 7.0 named-identity spec. Baseline commit `a6c914e`. Legacy citations use `path@a6c914e:Lstart-Lend` and stay retrievable from git history after the Epic 7 purge. Values marked **proposed** are design values the owner may revise.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-072 |
| Name | Curved metal ball pipe |
| Type | Mechanical |
| Requirement anchor | [element-072](../requirements.md#element-072); scope index [todo-199](../requirements.md#todo-199) |
| Named entry | [named-elements.md#element-072](../invest/named-elements.md#element-072); proof owner S361 |
| CAT spec refined or extended | [CAT-049 pipe bend 45](CAT-049-pipe_bend_45.md) and [CAT-050 pipe bend 90](CAT-050-pipe_bend_90.md) both list EL-072 as their mapped identity; those bends use the common fixed 1.3 m bore (radius 0.65) and a 2.4 m centreline radius. **Conflict:** this spec proposes the [EL-071](EL-071-straight-metal-ball-pipe.md) 0.62 m metal gauge, which cannot share that bore or join clear mouths. Either EL-072 keeps the 1.3 m bore and refines the clear bends (material and opacity only), or it is a separate part. Owner decision, Open question 3 |
| Related identities | [EL-071 straight metal ball pipe](EL-071-straight-metal-ball-pipe.md) (seam partner), [CAT-048 pipe](CAT-048-pipe.md) |
| Roadmap story | unscheduled |
| Status | not started |

## 2. Declaration

### Bodies and shapes

- One static hollow torus segment: bore diameter 0.62 m (radius 0.31), wall 0.04 m (outer radius 0.35), centreline radius 1.2 m, sweep 90°, with an end collar (outer radius 0.4 m, half-width 0.05 m) at each mouth. **Proposed**: same bore as EL-071 so they join; centreline radius scales the clear bend's 2.4 m (`parts/PipeBendPart.cs@a6c914e:L9-L9`) by the bore ratio (0.62/1.3 ≈ 0.48 → 1.15, rounded up to 1.2 so the Bowling ball turns without wedging). If the owner keeps the 1.3 m bore, the clear bend geometry (centreline 2.4 m, bore 0.65) applies unchanged.
- Mouths at angles 0 and sweep, each offset 0.05 m outward along the tangent (the clear bend offsets 0.09 m, its collar half-width; `parts/PipeBendPart.cs@a6c914e:L21-L26`).

### Mass and material

Static, same steel material as EL-071: restitution 0.3, bounce threshold 0.1 m/s, friction 0.2, rolling resistance 0. **Proposed**. (The clear bend used restitution 0.15, threshold 0.1, friction 0.3, `parts/PipeBendPart.cs@a6c914e:L12-L12`.)

### Constraints and joints

None.

### Typed ports

None. Two mouths (start, end) with outward normal and bore radius for build-time snapping to equal-bore mouths (`engine/TubeMouth.cs@a6c914e:L9-L9`).

### Sensors and activation

None.

### Work and energy stores

None. Direction changes come only from contact; speed never exceeds the entry speed.

### Parameters

None. **Proposed**: fixed sweep and radius, like the clear bends ("no resize controls", `DESIGN.md@a6c914e:L298-L298`).

### Cosmetic curves and UI bindings

None (static, opaque shell).

### Art

Pale grey metal shell `#ccd9df`, cream collars `#fff8e9`, thin navy rails `#293954` following the arc (clear-bend language, `DESIGN.md@a6c914e:L298-L298`). Catalogue colour **proposed**: pale grey; icon shows the 90° arc.

### Catalogue and inventory entry

Id `metal_pipe_bend`, title "Metal bend", category "Motion"; `WorkshopPartKind.MetalPipeBend` appended last to the free inventory (`engine/gpu/WorkshopInventory.cs@a6c914e:L49-L60`). **Proposed** (applies if EL-072 is a separate part).

### Variants

The requirements row names no variants or modes. One 90° bend is specified. The clear bends exist at 45° and 90° (`engine/MachineData.cs@a6c914e:L8-L8`); whether a 45° metal bend is required is Open question 1.

## 3. Engine capabilities

Binding: ContactImpulse, EnvironmentState, FiniteLedger, GeometryQuery, JointConstraint, RigidBodyDynamics, SlidingFriction (map); coverage JSON adds StateTransaction.

**Exists now:** sphere contacts, friction, rolling resistance and speculative margins (`CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L208-L236`, `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L702-L766`).

**Missing**

- Hollow torus-segment collider: Story 6.9 (45°) and Story 6.10 (90° and joined pipe route).
- Equal-bore mouth snapping and seam continuity: Story 6.10.

**Dependencies.** EL-071 (seam partner) and the CAT-048/049/050 hollow colliders.

## 4. Sources and legacy

- Requirements row: "Fast and queued cargo remain conserved at seams." No variants.

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | Clear bend: centreline radius 2.4 m, sweep from the `TubeBendAngle` enum {45, 90}, bend wall radius 0.70 with bore 0.65, collar tubes half-width 0.09 to radius 0.78 at both mouths. | `parts/PipeBendPart.cs@a6c914e:L8-L36`; `engine/MachineData.cs@a6c914e:L8-L8` | carry forward (geometry pattern, scaled) | Shape of a hollow bend. |
| 2 | Fast balls (6 and 40 m/s) cross a straight-to-bend or bend-to-bend seam and exit the outlet; per-tick travel ≤ speed × tick + 0.002 m and speed never exceeds entry + 0.002 m/s. | `CuriousContraptions.tests/PipeBendTests.cs@a6c914e:L40-L85` | carry forward (re-freeze tolerances under f32) | Directly the row's "fast cargo conserved at seams". |
| 3 | A ball at 6 m/s turns through a 45° or 90° bend without added energy (speed ≤ 6.001 m/s) and Reset restores its initial pose and velocity exactly. | `CuriousContraptions.tests/PipeBendTests.cs@a6c914e:L150-L193` | carry forward | Energy bound and Reset. |
| 4 | Bend mouths snap to straight pipes and other bends; a joined gravity route carries the ball across its seam. | `CuriousContraptions.tests/PipeBendTests.cs@a6c914e:L117-L117`, `CuriousContraptions.tests/PipeBendTests.cs@a6c914e:L198-L198` | carry forward | Seam joining. |
| 5 | Hollow compound distinguishes bore, shell exterior and open end. | `CuriousContraptions.tests/HollowSurfaceTests.cs@a6c914e:L61-L71` | carry forward (behaviour) | Real collision surfaces, no transport. |
| 6 | Curve tessellation steps (32 × 48) for collision. | `parts/PipeBendPart.cs@a6c914e:L37-L37` | do not carry forward | Legacy CPU hull approximation; the hollow SDF of Story 6.7 replaces it. |

**Files harvested:** `parts/PipeBendPart.cs`, `parts/catalog/pipe_bend_90.tres` (checked), `engine/MachineData.cs`, `engine/TubeMouth.cs`, `CuriousContraptions.tests/PipeBendTests.cs`, `CuriousContraptions.tests/HollowSurfaceTests.cs`. `CuriousContraptions.tests/TubePlacementSnapTests.cs` checked (snapping, harvested by CAT-049/050).

## 5. Acceptance outline

Requirement row: [element-072](../requirements.md#element-072).

- **Chrome UI recipe.** Place a Metal pipe tilted downhill, snap a Metal bend to its outlet (mouths snap only to equal bores), and a second Metal pipe out of the bend into a Basket; queue three Bowling balls on a Ramp feeding the first pipe. Verify placement and snapped seams.
- **Positive.** All three balls traverse the bend and reach the Basket; the count out equals the count in.
- **Negative/control.** A Basketball jams at the first mouth. A bend left 0.1 m out of line at a seam: balls strike the collar instead of passing (no logical transfer).
- **Boundaries.** Entry at 6 and 40 m/s (no tunnelling, no speed gain); two balls in contact through the bend; rotated route in depth.
- **Run/Reset and Save/Load.** Ball poses restore; part poses and snaps round-trip.
- **Integrations.** The scope index [todo-199](../requirements.md#todo-199) defines no separate integration task; shared interactions use [IX-01 contact impulse](../requirements.md#interaction-01) and [IX-02 sliding friction](../requirements.md#interaction-02). The [campaign coverage ledger](../requirements.md#campaign-element-coverage) reserves first use of both bend angles in levels 21–30.

## 6. Open questions

1. Is a 45° metal bend also required (the clear family has both)? Owner decision.
2. Centreline radius 1.2 m (proposed) or another value? Owner decision.
3. **Bore conflict with CAT-049/050.** Both clear-bend specs map EL-072 to their fixed 1.3 m bore bends; this spec proposes the 0.62 m metal gauge. Keep the 1.3 m bore (EL-072 refines the clear bends) or adopt 0.62 m (separate part, joins only metal pipes)? Must match the EL-071 decision. Owner decision.
