# EL-162 · Fixed ball diverter named-identity spec

Story 7.0 named-identity spec (Batch J). Baseline commit `a6c914e`; every citation is `path@a6c914e:Lstart-Lend` and resolves with `git show a6c914e:<path> | sed -n 'start,endp'`. **Proposed** marks an unsourced design value with a one-line justification; the owner may revise it. All values are canonical IEEE-754 f32 game values inside the [game-grade envelope](../../gpu-f32-physics.md#game-grade-envelope).

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-162 |
| Name | Fixed ball diverter |
| Type | Mechanical |
| Anchor | [requirements.md#element-162](../requirements.md#element-162); [named entry](../invest/named-elements.md#element-162); scope index [todo-246](../requirements.md#todo-246) |
| Proof owner | S373 |
| Refines / extends | No CAT spec. Routes cargo between ramps ([CAT-054 ramp](CAT-054-ramp.md)) and receivers ([CAT-004 basket](CAT-004-basket.md)) |
| Related | EL-163 Powered ball diverter and EL-164 Alternating ball diverter (separate identities sharing the junction geometry) |
| Roadmap story | unscheduled |
| Status | not started |

## 2. Declaration

### Bodies and shapes
All full extents.
- **Junction (static).** An open Y channel: inlet floor 1.5 × 0.1 × 0.9 m, two branch floors 1.5 × 0.1 × 0.9 m at ±30° from the inlet axis, side walls 0.5 m high and 0.06 m thick, the floor falling 6° along the flow — **proposed**: a 0.9 m channel clears the 0.68 m Basketball and 0.56 m Bowling ball (`engine/gpu/WorkshopConstruction.cs@a6c914e:L45-L53`); a 6° fall keeps a Basketball rolling against its 0.035 rolling resistance.
- **Blade.** A box 1.0 × 0.5 × 0.06 m pivoting at the fork apex about local +Y. In `Left` it seals the right branch mouth and in `Right` the left one, at ±15° — **proposed**: a 1.0 m blade overlaps the 0.9 m mouth, so the closed branch has no gap for a ball.
- For this fixed identity the blade is a second collider on the static junction body, posed from the selection at compile time; current static bodies already carry several box colliders (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L111-L115`).

### Mass and material
Static, no mass. Channel and blade restitution 0.1, bounce threshold 0.1 m/s, friction 0.3, rolling resistance 0 — **proposed**: a dead track surface near the bend material (`parts/PipeBendPart.cs@a6c914e:L12-L12`) so diverted balls do not bounce over walls.

### Constraints and joints
None. The blade is fixed for the whole Run.

### Typed ports
None. The selection is authored, not signalled.

### Sensors and activation
None.

### Work and energy stores
None. Routing uses only cargo motion and contact.

### Parameters

| Name | Type | Range | Default | Unit | Source |
| --- | --- | --- | --- | --- | --- |
| `branch` | enum `DiverterBranch { Left, Right }` | 2 values | Left | — | **proposed**: the requirement's "player-set blade ... one chosen branch"; a closed set, converted to text only at serialization |

The selection is editable only in build mode; Run never changes it.

### Cosmetic curves and UI bindings
No cosmetic curve; the blade pose is the state cue. UI: `branch` through the contextual configuration pattern (`ui/WorkshopConfiguration.cs@a6c914e:L43-L75`) as a two-choice control.

### Art
Channel in the Ramp colour `#c28f52`; blade gold `#f7cb52` like the powered gate shutter (`parts/PoweredGatePart.cs@a6c914e:L47-L47`); navy `#293954` pivot cap; an arrow decal on the inlet in cream `#fff8e9` pointing to the open branch ([DESIGN colour system](../../../DESIGN.md#colour-system)). **Proposed**.

### Catalogue and inventory entry
Id `diverter_fixed`, title "Ball diverter", category Motion — **proposed**. Add `WorkshopPartKind.FixedDiverter` with a counted allowance (`engine/gpu/WorkshopInventory.cs@a6c914e:L9-L23`).

### Variants
One outcome in the row. Powered (EL-163) and alternating (EL-164) modes are separate identities and are taught after fixed selection ([todo-246](../requirements.md#todo-246) integration task).

## 3. Engine capabilities

Families: ContactImpulse, EnvironmentState, FiniteLedger, FiniteWorkActuation, GeometryQuery, JointConstraint, RigidBodyDynamics, SignalPropagation, plus StateTransaction ([map row](../general-engine-element-map.md); [element-02.json](../../coverage/engine/element-02.json)). The map notes "Fixed authored blade uses geometry/contact; inherited FiniteWorkActuation does not require powered movement."

**Exists now**
- Static multi-box bodies, dynamic sphere cargo, friction and rolling resistance: `engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L120`; `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L701-L719`.
- Rotated static boxes from authored rotation (the wall): `engine/gpu/WorkshopWall.cs@a6c914e:L30-L39`.

**Missing**
- A `WorkshopPartKind` member, a typed selection field in the construction record and save codec (`engine/gpu/WorkshopSaveCodec.cs@a6c914e:L6-L10`). Owner S373.

**Dependencies.** None beyond delivered ramps, walls and balls; it can be built first among the three diverters.

## 4. Sources and legacy

- Requirement: "Player-set blade routes actual balls into one chosen branch"; outcome "Wrong branch remains unavailable without physical leakage" ([element-162](../requirements.md#element-162)).
- Integration task under [todo-246](../requirements.md#todo-246): "A visible Y-junction selection sends the next ball down the chosen physical branch. Teach fixed selection first." Campaign: first use 21–30, reuse 41–50, 111–120, 136–150 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)). Decision S257 applies to the inherited SignalPropagation only ([decisions](../invest/decisions.md#s257)).

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | Gold blade art on the powered gate | `parts/PoweredGatePart.cs@a6c914e:L46-L47` | carry forward (palette) | Blade colour identity. |
| 2 | Accepted: a closed gate blocks the ball and only an open gate lets it through, also when the part is rotated (20°, 30°, 40°) | `CuriousContraptions.tests/PoweredGateTests.cs@a6c914e:L152-L178` | carry forward (behaviour) | A sealing blade, including rotated placement. |

No legacy diverter exists.

**Files harvested:**
- `parts/PoweredGatePart.cs` (blade art)
- `parts/PipeBendPart.cs` (material reference)
- `CuriousContraptions.tests/PoweredGateTests.cs`
- Searched with no hit for `diverter` or `junction`: `parts/`, `engine/` (including `engine/physics/` and `engine/bridge/`), `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/`, `reference/`, `diagnostics/`.

## 5. Acceptance outline

Follow [element-162](../requirements.md#element-162) and the [mechanics profile](../invest/profiles.md#mechanics).
- **Chrome UI recipe.** Place a ramp, the diverter at its foot and a receiver under each branch; set `branch` to Right with the configuration control. Verify the committed blade pose. Run.
- **Positive.** The ball reaches the right receiver.
- **Negative/control.** With `branch` Left the same ball reaches the left receiver; the closed branch's receiver stays empty in both runs; a ball dropped beside the junction is not routed.
- **Boundaries.** Bowling ball and Basketball; a fast ball hitting the blade; rotated placement; two balls in quick succession both take the selected branch.
- **Run/Reset.** Cargo returns; the blade never moves.
- **Save/Load.** `branch` and pose round-trip; an unknown `branch` value is rejected at load.
- **Integrations.** Y-junction integration task under [todo-246](../requirements.md#todo-246) (fixed selection taught first, then EL-163 and EL-164); campaign row "... fixed/powered/alternating diverter ...": first use 21–30, reuse 41–50, 111–120, 136–150 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)); fed by ramps (CAT-054), delivering to receivers (CAT-004).

## 6. Open questions

1. Open channel (proposed) or enclosed tube junction with tube mouths? Unspecified — owner decision.
2. Branch angle ±30° and 6° fall (proposed): confirm. Owner decision.
