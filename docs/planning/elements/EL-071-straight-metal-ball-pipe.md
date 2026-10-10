# EL-071 · Straight metal ball pipe declaration readiness spec

Story 7.0 named-identity spec. Baseline commit `a6c914e`. Legacy citations use `path@a6c914e:Lstart-Lend` and stay retrievable from git history after the Epic 7 purge. Values marked **proposed** are design values the owner may revise.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-071 |
| Name | Straight metal ball pipe |
| Type | Mechanical |
| Requirement anchor | [element-071](../requirements.md#element-071); scope index [todo-199](../requirements.md#todo-199) |
| Named entry | [named-elements.md#element-071](../invest/named-elements.md#element-071); proof owner S360 |
| CAT spec refined or extended | [CAT-048 pipe](CAT-048-pipe.md) lists EL-071 as refining its straight tube, whose bore is fixed at 1.3 m (radius 0.65). **Conflict:** this spec proposes an opaque metal tube with a 0.62 m gauge, which cannot share CAT-048's fixed bore or join its mouths. Either EL-071 keeps the 1.3 m bore and refines CAT-048 (material and opacity only), or it is a separate part beside CAT-048. Owner decision, Open question 1 |
| Related identities | [EL-072 curved metal ball pipe](EL-072-curved-metal-ball-pipe.md) (same bore, joins at seams), [EL-058 size grate](EL-058-size-grate.md) (same 0.62 m gauge), [CAT-049](CAT-049-pipe_bend_45.md)/[CAT-050](CAT-050-pipe_bend_90.md) clear bends |
| Roadmap story | unscheduled |
| Status | not started |

## 2. Declaration

### Bodies and shapes

- One static hollow tube along local X: inner bore diameter 0.62 m (radius 0.31), wall 0.04 m (outer radius 0.35), end collars outer radius 0.4 m, collar half-width 0.05 m. **Proposed**: admits the Bowling ball (0.56 m) with 0.03 m radial clearance and jams the Basketball (0.68 m) at the mouth, using current ball radii (`engine/gpu/WorkshopConstruction.cs@a6c914e:L45-L53`); proportions follow the clear pipe's bore/middle/end ratios (0.65/0.70/0.78, `engine/gpu/WorkshopPipe.cs@a6c914e:L9-L18`). If the owner keeps the CAT-048 1.3 m bore instead, these become radius 0.65, middle 0.70, end 0.78, half-width 0.09.
- Length `length` along the axis.

### Mass and material

Static. Steel interior: restitution 0.3, bounce threshold 0.1 m/s, friction 0.2, rolling resistance 0. **Proposed**: a smooth hard shell, lower friction than the 0.3 static default (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L109-L110`) so balls run freely. Optically opaque (unlike the clear pipe).

### Constraints and joints

None.

### Typed ports

None (ball routing is contact, not a connection). Two open mouths (negative and positive ends) with outward normals and bore radius 0.31 m for build-time snapping to matching mouths only (legacy mouth record `engine/TubeMouth.cs@a6c914e:L9-L9`).

### Sensors and activation

None.

### Work and energy stores

None.

### Parameters

| Name | Type | Range | Default | Unit | Source |
| --- | --- | --- | --- | --- | --- |
| `length` | f32 | 1–8 | 3 | m | Range: the clear pipe length bounds (`engine/gpu/WorkshopPipe.cs@a6c914e:L11-L11`); default **proposed** (a little shorter than the clear pipe's 3.6 m, `engine/gpu/WorkshopPipe.cs@a6c914e:L8-L8`, to fit beside it) |

The bore is fixed; only length resizes (as the clear pipe, `engine/gpu/WorkshopPipe.cs@a6c914e:L21-L28`).

### Cosmetic curves and UI bindings

None. The ball inside is hidden by the opaque shell; two small viewing slots (cosmetic cut-outs, **proposed**) show passage without changing collision.

### Art

Pale grey metal shell `#ccd9df`, cream open collars `#fff8e9`, two thin navy rails `#293954` (the clear pipe's collar and rail language, `DESIGN.md@a6c914e:L299-L299`). Catalogue colour **proposed**: pale grey.

### Catalogue and inventory entry

Id `metal_pipe`, title "Metal pipe", category "Motion"; `WorkshopPartKind.MetalPipe` appended last to the free inventory (`engine/gpu/WorkshopInventory.cs@a6c914e:L49-L60`). **Proposed** (applies if EL-071 is a separate part).

### Variants

The requirements row names no variants or modes. One element with a `length` parameter.

## 3. Engine capabilities

Binding: ContactImpulse, EnvironmentState, FiniteLedger, GeometryQuery, JointConstraint, RigidBodyDynamics, SlidingFriction (map); coverage JSON adds StateTransaction.

**Exists now:** sphere contacts with friction and rolling resistance (`CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L702-L766`); no hollow collider runs today. Historical `PipeDimensions` and `AnnularProfile` were excluded (`CuriousContraptions.csproj@a6c914e:L29-L29`) and are removed by Story 7.4 under the 10 Oct owner decision; Story 6.6 rebuilds fresh on the canonical f32/WASM SIMD engine.

**Missing**

- Compound cylindrical hollow collider: Story 6.6; hollow SDF with torus rim caps: Story 6.7 (CAT-048).
- Mouth snapping restricted to equal bores: with Story 6.6/6.10 (joined pipe route).
- Opaque optical occlusion by the shell: Epic 13 optics (Story 13.1+).

**Dependencies.** CAT-048 hollow collider (6.6/6.7); balls (CAT-001/014 delivered).

## 4. Sources and legacy

- Requirements row: "Oversized cargo jams rather than traversing a logical connection." No variants.

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | Clear pipe: bore radius 0.65 (diameter 1.3), length 1–8 m default 3.6, profile middle radius 0.70, end radius 0.78, end half-width 0.09; only length may change. | `engine/gpu/WorkshopPipe.cs@a6c914e:L5-L28` | carry forward (proportions and length rule) | Pattern for the metal pipe; the historical file was excluded from builds and is removed by Story 7.4. |
| 2 | A hollow tube keeps its reported minimum bore clearance (≥ 0.6475 m for r 0.65) so a ball passes through the open bore. | `CuriousContraptions.tests/HollowGeometryTests.cs@a6c914e:L209-L213` | carry forward (behaviour); do not carry forward the surface-error certificate | Pass/jam is clearance against the ball radius; proof-grade bounds are not acceptance. |
| 3 | Catalogue length stored as binary16 bits (`LengthBits = 17203`, i.e. 3.6). | `parts/catalog/pipe.tres@a6c914e:L8-L10` | do not carry forward the bits | f32 contract: carry the decimal meaning only. |
| 4 | Mouth record: id, position, outward normal, bore radius. | `engine/TubeMouth.cs@a6c914e:L9-L9` | carry forward | Seam snapping identity. |
| 5 | "Gravity carries balls through the open bore; tilt it downhill to guide them." | `parts/catalog/pipe.tres@a6c914e:L17-L17` | carry forward | No transport boost: gravity and contact only. |
| 6 | A 0.34 m-radius ball at 40 m/s passes the clear bore; a 0.8 m-radius ball at 4 m/s is stopped at the mouth. | `CuriousContraptions.tests/PipeTests.cs@a6c914e:L55-L80` | carry forward (acceptance pattern) | The legacy oversize control used a non-catalogue 0.8 m ball; with a 1.3 m bore no catalogue ball is oversized, which is why this spec proposes the narrower gauge. |

**Files harvested:** `engine/gpu/WorkshopPipe.cs`, `engine/gpu/AnnularProfile.cs` (both build-excluded at a6c914e), `parts/catalog/pipe.tres`, `parts/PipePart.cs` (checked; harvested by CAT-048), `engine/TubeMouth.cs`, `CuriousContraptions.tests/HollowGeometryTests.cs`, `CuriousContraptions.tests/PipeTests.cs`, `CuriousContraptions.csproj`. Searched for metal pipe: no hit.

## 5. Acceptance outline

Requirement row: [element-071](../requirements.md#element-071).

- **Chrome UI recipe.** Place a Ramp feeding the mouth of a Metal pipe tilted 10° downhill, a Basket at the far end; release a Bowling ball and, separately, a Basketball from the ramp top. Verify placement and length.
- **Positive.** The Bowling ball rolls through the bore and lands in the Basket.
- **Negative/control.** The Basketball stops at the mouth (jams) and never appears at the far end; no logical connection moves it. A clear pipe mouth does not snap to a metal pipe mouth.
- **Boundaries.** Length 1 and 8 m; ball at 8 m/s entering the bore does not tunnel through the 0.04 m wall; level pipe (ball stops inside); reversed tilt.
- **Run/Reset and Save/Load.** Ball poses restore; length round-trips; an unsupported bore value rejects.
- **Integrations.** The scope index [todo-199](../requirements.md#todo-199) defines no separate integration task; shared interactions use [IX-01 contact impulse](../requirements.md#interaction-01) and [IX-02 sliding friction](../requirements.md#interaction-02). The [campaign coverage ledger](../requirements.md#campaign-element-coverage) reserves first use of the ball straight pipe in levels 21–30.

## 6. Open questions

1. **Bore conflict with CAT-048.** CAT-048 maps EL-071 as a refinement of its fixed 1.3 m bore tube; this spec proposes a 0.62 m gauge so the Basketball jams with today's catalogue. Keep the 1.3 m bore (EL-071 refines CAT-048, and the oversize control needs a new larger body), or adopt the 0.62 m gauge (EL-071 is a separate part that cannot join clear pipes)? Owner decision.
2. Is EL-071 a separate catalogue part from CAT-048 or a material variant of it? Batch A mapping and owner decision.
3. Viewing slots: cosmetic only (proposed) or real openings? Owner decision.
