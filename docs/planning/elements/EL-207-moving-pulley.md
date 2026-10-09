# EL-207 · Moving pulley named-identity spec

Story 7.0 named-identity spec (Batch J). Baseline commit `a6c914e`; every citation is `path@a6c914e:Lstart-Lend` and resolves with `git show a6c914e:<path> | sed -n 'start,endp'`. **Proposed** marks an unsourced design value with a one-line justification; the owner may revise it. All values are canonical IEEE-754 f32 game values inside the [game-grade envelope](../../gpu-f32-physics.md#game-grade-envelope).

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-207 |
| Name | Moving pulley |
| Type | Mechanical |
| Anchor | [requirements.md#element-207](../requirements.md#element-207); [named entry](../invest/named-elements.md#element-207); scope [campaign-element-coverage](../requirements.md#campaign-element-coverage) ("fixed/moving pulley roles") |
| Proof owner | S309 |
| Refines / extends | Extends [CAT-053 pulley](CAT-053-pulley.md) (its open questions ask whether this is a variant or a separate element); rope law from [CAT-058](CAT-058-rope_anchor.md) |
| Related | EL-205 Rope, EL-206 Fixed pulley, CAT-067 Weight, EL-204 Weight |
| Roadmap story | unscheduled (Story 10.2 covers fixed pulleys only) |
| Status | not started |

## 2. Declaration

### Bodies and shapes
- **Sheave (dynamic).** One sphere collider of radius 0.4 m standing in for the wheel, with groove radius 0.4 m and rope plane z = 0.12 m — sourced fixed-pulley geometry (`engine/MachineData.cs@a6c914e:L95-L98`; legacy wheel sphere `parts/PulleyPart.cs@a6c914e:L24-L24`). A dynamic body admits one sphere today (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L226-L229`).
- **Hook block.** Art only, a cream box 0.3 × 0.2 × 0.2 m (full extents) below the wheel — **proposed**: shows where the load hangs.

### Mass and material
- Sheave 0.5 kg — **proposed**: light beside the 0.25–8 kg Weights (`parts/WeightPart.cs@a6c914e:L22-L27`) so the 2:1 force ratio stays visible, but finite so the block shares reactions.
- Material restitution 0.1, friction 0.3 — **proposed**: dead contact if loads swing into it. Rope–groove friction zero, as the fixed pulley.

### Constraints and joints
None of its own. It is a dynamic guide inside one rope route (spans pass under the groove) and the load end of a second route from its `Hook`. The guide shares rope tension and reaction with both loads in one solve (row 2 below); its displacement ratio follows the routing alone.

### Typed ports
| Socket | Domain | Direction | Role | Source |
| --- | --- | --- | --- | --- |
| `Tie` | Rope | Bidirectional | Guide (two spans) at (0, −0.4, 0.12) | **proposed** position under the groove; role from `engine/RopeNetwork.cs@a6c914e:L69-L78` |
| `Hook` | Rope | Bidirectional | Load (one end) at (0, −0.6, 0.12) | **proposed**: a second socket so a Weight can hang from the block |

### Sensors and activation
None.

### Work and energy stores
None: the sheave's own potential and kinetic energy only. No mechanical-advantage bonus: work in equals work out plus the block's own energy change.

### Parameters
None — **proposed**: geometry matches the fixed pulley; the ratio comes from routing, not a setting.

### Cosmetic curves and UI bindings
Wheel turns with relative rope travel ÷ 0.4 m while the route is taut, as the fixed pulley (`parts/PulleyPart.cs@a6c914e:L34-L38`); the whole block follows its committed pose. UI: rope connections with the contextual Connect.

### Art
Wheel in the Pulley colour `#d69c47` with cream `#fff8e9` rim, navy `#293954` spoke and gold `#f7cb52` marker as the fixed pulley (`parts/PulleyPart.cs@a6c914e:L27-L32`); cream hook block — **proposed**.

### Catalogue and inventory entry
Id `moving_pulley`, title "Moving pulley", category Ropes — **proposed**. Add `WorkshopPartKind.MovingPulley` with an allowance (`engine/gpu/WorkshopInventory.cs@a6c914e:L9-L23`).

### Variants
One outcome in the row; no variants.

## 3. Engine capabilities

Families: EnvironmentState, GeometryQuery, JointConstraint, RigidBodyDynamics, TensionTransmission ([map row](../general-engine-element-map.md); [element-03.json](../../coverage/engine/element-03.json)).

**Exists now**
- Dynamic sphere body: `engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L120`.

**Missing**
- Rope route with a dynamic guide whose groove tangents move with the body (TensionTransmission/JointConstraint): Story 10.2 builds static guides; a dynamic guide is unscheduled. Decision S257 rope-port ([decisions](../invest/decisions.md#s257)). Owner S309.

**Dependencies.** EL-205 rope and EL-206 fixed pulley (Story 10.2); CAT-067 weight (Story 10.4).

## 4. Sources and legacy

- Requirement: "Load-attached sheave moves with the routed rope constraints and shared reaction forces"; outcome "Its displacement/work ratio follows routing rather than a named pulley bonus" ([element-207](../requirements.md#element-207)).

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | The legacy pulley is fixed with "no moving-block ratio or wheel inertia"; moving-block ratios were future work | `parts/PulleyPart.cs@a6c914e:L6-L6`; `reference/P0-022-before/source-README.md@a6c914e:L99-L99` | carry forward (as gap) | Confirms no legacy moving pulley. |
| 2 | Accepted: a dynamic guide and both loads react in one solve; with unit masses the velocities are −7/3, −4/3 and −1/3 (total momentum conserved) and the guide gains no angular momentum; route length stays 4 | `CuriousContraptions.tests/RoutedRopeTests.cs@a6c914e:L18-L31` | carry forward (behaviour), do not carry forward (point-guide numbers) | Shared reactions without a bonus. |
| 3 | Accepted: a fixed guide transfers tension but never pushes | `CuriousContraptions.tests/RoutedRopeTests.cs@a6c914e:L33-L45` | carry forward | Tension-only through guides. |

**Files harvested:**
- `CuriousContraptions.tests/RoutedRopeTests.cs`
- `parts/PulleyPart.cs`
- `engine/MachineData.cs` (pulley geometry constants)
- `engine/RopeNetwork.cs` (guide role)
- `parts/WeightPart.cs` (load mass range)
- `reference/P0-022-before/source-README.md` (future-work note)
- Searched with no hit for `moving pulley` or a moving-block implementation (`moving-block` appears only in the two notes above): `parts/`, `engine/` (including `engine/physics/` and `engine/bridge/`), `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/`, `reference/`, `diagnostics/`.

## 5. Acceptance outline

Follow [element-207](../requirements.md#element-207) and the [mechanics profile](../invest/profiles.md#mechanics).
- **Chrome UI recipe.** Place an anchor and a fixed pulley high; hang the moving pulley from a rope running anchor → under moving pulley → over fixed pulley → counterweight; hang a 4 kg Weight from the moving pulley's `Hook`. Run.
- **Positive.** A 2.25 kg counterweight balances the 4 kg load plus the 0.5 kg block; a heavier one lifts it, and pulling the free end down 1 m raises the load 0.5 m.
- **Negative/control.** Replacing the moving pulley with a fixed pulley gives 1:1 travel; a slack route moves nothing; the block's work in equals work out.
- **Boundaries.** Block mass included in balance; swinging load; route ending at the moving pulley (Open) refused to carry tension.
- **Run/Reset.** Block, loads and lengths restored.
- **Save/Load.** Both rope routes and lengths round-trip.
- **Integrations.** Retained rope behaviour [todo-140](../requirements.md#todo-140) and qualification [todo-141](../requirements.md#todo-141); campaign row 2 ("... fixed/moving pulley roles ..."): first use 11–20, moving/load variants into 41–50, reuse 31–50, 61–80, 136–150 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)); paired with EL-206 and CAT-067 weights.

## 6. Open questions

1. A CAT-053 variant or a separate catalogue part (proposed separate)? Unspecified — owner decision (see the [CAT-053 open questions](CAT-053-pulley.md#6-open-questions)).
2. Two rope sockets (`Tie` guide and `Hook` load, proposed) or a rigid hook joint to the load? Owner decision.
