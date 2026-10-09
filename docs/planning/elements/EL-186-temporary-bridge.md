# EL-186 · Temporary bridge named-identity spec

Story 7.0 named-identity spec (Batch J). Baseline commit `a6c914e`; every citation is `path@a6c914e:Lstart-Lend` and resolves with `git show a6c914e:<path> | sed -n 'start,endp'`. **Proposed** marks an unsourced design value with a one-line justification; the owner may revise it. All values are canonical IEEE-754 f32 game values inside the [game-grade envelope](../../gpu-f32-physics.md#game-grade-envelope).

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-186 |
| Name | Temporary bridge |
| Type | Mechanical |
| Anchor | [requirements.md#element-186](../requirements.md#element-186); [named entry](../invest/named-elements.md#element-186); scope [todo-321](../requirements.md#todo-321) |
| Proof owner | S365 |
| Refines / extends | No CAT spec. Built from the [EL-185 releasable joint](EL-185-releasable-assembly-joint.md); deck sized like [CAT-054 ramp](CAT-054-ramp.md) |
| Related | EL-185; EL-159/EL-160 load-limited connectors (the material-failure mode); GAP-05 load-limited connector lesson |
| Roadmap story | unscheduled |
| Status | not started |

## 2. Declaration

### Bodies and shapes
All full extents.
- **Deck (dynamic).** One box 3.0 × 0.12 × 1.3 m — **proposed**: the sourced ramp default length and width (`engine/gpu/WorkshopInstances.cs@a6c914e:L22-L30`), so it spans the same gaps ramps do.
- **Two abutments (static).** Boxes 0.6 × 1.0 × 1.3 m at each end of the deck — **proposed**: a visible support the deck rests on, under the 0.6 m end overlap.
- One homogeneous box per dynamic body is admitted now (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L226-L229`).

### Mass and material
- Deck 2 kg — **proposed**: the sourced default beam mass of the impact lever (`parts/catalog/impact_lever.tres@a6c914e:L12-L12`).
- Deck and abutment restitution 0.1, friction 0.5 — **proposed**: a deck that carries rolling cargo without bouncing it off.

### Constraints and joints
- Each deck end is attached to its abutment by one attachment joint (a weld from EL-185) — **proposed**. With both attachments present the deck is a rigid crossing; it loses support only through the attachments, never because a goal or timer says so.
- Connected collision between deck and abutment is enabled after an attachment goes, so a released end drops onto or off the abutment physically.

### Typed ports
| Socket | Domain | Direction | Variant | Source |
| --- | --- | --- | --- | --- |
| `ReleaseLeftIn`, `ReleaseRightIn` | Activation | Input, command Release | Released support | **proposed**: one typed release per declared attachment, as EL-185 |

### Sensors and activation
A release command removes exactly its own attachment; it never moves the abutments or any other structure.

### Work and energy stores
None. All motion after release or failure comes from gravity and the load.

### Parameters

| Name | Type | Range | Default | Unit | Source |
| --- | --- | --- | --- | --- | --- |
| `support` | enum `BridgeSupport { Released, Failing }` | 2 values | Released | — | **proposed**: the row's two loss-of-support causes, each a declared mode |
| `end_capacity` | f32 | 20–200 | 40 | N per end (Failing only) | **proposed**: holds the 2 kg deck plus one resting Bowling ball at mid-span (about 29 N per end) and fails with two (about 49 N per end) |

### Variants
- **Released support.** Both ends welded; each `Release…In` removes one weld. Releasing one end makes the deck hinge down about the other support; releasing both drops it.
- **Failing support.** No command ports. Each end attachment fails when its carried load exceeds `end_capacity`; below the threshold it holds regardless of display (structural-material profile). Failure removes only that attachment.

### Cosmetic curves and UI bindings
Deck follows committed pose; attachment clamps open on release or show a snapped pin on failure, cosmetic only. UI: `support` and `end_capacity` through the configuration pattern (`ui/WorkshopConfiguration.cs@a6c914e:L43-L75`).

### Art
Deck in the Ramp colour `#c28f52`; abutments cream `#fff8e9`; navy `#293954` clamps with gold `#f7cb52` pins ([DESIGN colour system](../../../DESIGN.md#colour-system)). **Proposed**.

### Catalogue and inventory entry
Id `temporary_bridge`, title "Temporary bridge", category Motion — **proposed**. Add `WorkshopPartKind.TemporaryBridge` with a counted allowance (`engine/gpu/WorkshopInventory.cs@a6c914e:L9-L23`).

## 3. Engine capabilities

Families: EnvironmentState, FiniteLedger, GeometryQuery, JointConstraint, PhaseTopology, RigidBodyDynamics, StructuralFracture, TopologyTransaction, plus StateTransaction ([map row](../general-engine-element-map.md); [element-02.json](../../coverage/engine/element-02.json)). Map: "Actual supports/attachments carry load; removal/failure changes topology, no disappearance based on level goal."

**Exists now**
- Dynamic box deck on static boxes with contact and friction: `engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L120`; `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L701-L719`.

**Missing**
- Weld joint and runtime joint removal (JointConstraint/TopologyTransaction): as EL-185, unscheduled. Owner S365.
- Load-limited attachment failure (StructuralFracture): decision S543 fracture, next implementation S563 ([decisions](../invest/decisions.md#s543)); GAP-05 lesson.

**Dependencies.** EL-185 releasable joint; EL-159/EL-160 load-limited connector law for the Failing variant.

## 4. Sources and legacy

- Requirement: "Assembled load-bearing crossing loses support only when its declared attachment releases or material fails"; outcome "A release signal cannot move unrelated structures" ([element-186](../requirements.md#element-186)).
- [todo-321](../requirements.md#todo-321): defer until joint lifecycle is reliable; Reset restores authored attachments. Campaign: "releasable bridge/joint", first use 41–50 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)); [structure-material profile](../invest/profiles.md#structure-material).

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | Explicit Detach of an existing joint identity | `engine/physics/PhysicsJointChange.cs@a6c914e:L5-L28` | carry forward | Release removes a specific attachment. |
| 2 | Accepted: an impact releases a rope and the load falls; a missed impact leaves it held | `CuriousContraptions.tests/PhysicsImpactJointTests.cs@a6c914e:L84-L104` | carry forward | Released versus held control. |
| 3 | Accepted: simultaneous conflicting detaches reject | `CuriousContraptions.tests/PhysicsImpactJointTests.cs@a6c914e:L223-L238` | carry forward | One owner per attachment. |

No legacy bridge element exists: searches for `bridge` in parts, engine and content at `a6c914e` return only the `engine/bridge/` interop namespace and level text that calls domino chains "bridges" (level `double_bridge`, `content/puzzles.json@a6c914e:L26885-L26890`).

**Files harvested:**
- `engine/physics/PhysicsJointChange.cs`
- `CuriousContraptions.tests/PhysicsImpactJointTests.cs`
- `parts/catalog/impact_lever.tres` (beam mass reference)
- Checked, no element knowledge: `content/puzzles.json` (level `double_bridge`, domino "bridges"), `engine/bridge/` (host interop namespace).
- Searched with no hit for `temporary bridge` or a bridge part: `parts/`, `engine/` (including `engine/physics/`), `CuriousContraptions.tests/`, `tools/`, `reference/`, `diagnostics/`.

## 5. Acceptance outline

Follow [element-186](../requirements.md#element-186) and the [mechanics](../invest/profiles.md#mechanics) and structure-material profiles.
- **Chrome UI recipe.** Place the bridge across a gap between two ramps, a ball at the top ramp, a switch wired to `ReleaseRightIn`, and a second untouched structure (a wall) nearby. Run.
- **Positive (Released).** The ball crosses; a later switch hit releases the right end and the deck swings down, dropping cargo into the gap.
- **Positive (Failing).** One Bowling ball crosses; two at once break one end and fall.
- **Negative/control.** No command, no release; the wall and abutments never move on release; below `end_capacity` the deck holds.
- **Boundaries.** Release with cargo mid-span; `end_capacity` 20 and 200; both ends released in one tick.
- **Run/Reset.** Deck and both attachments restored.
- **Save/Load.** `support`, `end_capacity`, pose and release wires round-trip.
- **Integrations.** Releasable assembly and bridge task [todo-321](../requirements.md#todo-321) with EL-185; campaign row "... releasable bridge/joint": first use 41–50, reuse 61–100, 111–120, 136–150 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)); GAP-05 load-limited connector lessons 43–44 ([campaign gap allocation](../requirements.md#campaign-gap-allocation)) for the Failing variant.

## 6. Open questions

1. Is the failing mode a separate identity (EL-159/EL-160 connectors) used by the bridge, or a bridge setting (proposed)? Unspecified — owner decision.
2. Which load does `end_capacity` limit: tension, shear or the total joint force? Owner decision.
