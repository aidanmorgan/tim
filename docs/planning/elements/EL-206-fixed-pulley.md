# EL-206 · Fixed pulley named-identity spec

Story 7.0 named-identity spec (Batch J). Baseline commit `a6c914e`; every citation is `path@a6c914e:Lstart-Lend` and resolves with `git show a6c914e:<path> | sed -n 'start,endp'`. **Proposed** marks an unsourced design value with a one-line justification; the owner may revise it. All values are canonical IEEE-754 f32 game values inside the [game-grade envelope](../../gpu-f32-physics.md#game-grade-envelope).

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-206 |
| Name | Fixed pulley |
| Type | Mechanical |
| Anchor | [requirements.md#element-206](../requirements.md#element-206); [named entry](../invest/named-elements.md#element-206); scope [campaign-element-coverage](../requirements.md#campaign-element-coverage) |
| Proof owner | S308 |
| Refines / extends | Refines [CAT-053 pulley](CAT-053-pulley.md) ([requirement](../requirements.md#current-cat-053)); the full harvest is in that spec |
| Related | EL-205 Rope, EL-207 Moving pulley, CAT-058 Rope anchor, CAT-067 Weight |
| Roadmap story | Story 10.2 (CAT-053 with CAT-058) |
| Status | not started |

## 2. Declaration

All values are sourced through [CAT-053](CAT-053-pulley.md); this EL adds no new value.

### Bodies and shapes
- One static body: navy mount box full size 0.5 × 0.15 × 0.4 m at (0, 0.55, −0.1) (`AddBox` stores halves) and a wheel collider; legacy used a 0.4 m sphere for the wheel (`parts/PulleyPart.cs@a6c914e:L21-L24`; the wheel collider is a [CAT-053 open question](CAT-053-pulley.md#6-open-questions)).
- Groove radius 0.4 m; rope plane at local z = 0.12 m (`engine/MachineData.cs@a6c914e:L95-L98`).

### Mass and material
None: fixed, frictionless and without wheel inertia (`parts/PulleyPart.cs@a6c914e:L6-L6`). Contact material: a [CAT-053 open question](CAT-053-pulley.md#6-open-questions).

### Constraints and joints
None of its own. It is a guide inside a rope route; the route length includes finite-radius tangent legs and the wrapped arc ([CAT-053](CAT-053-pulley.md)), superseding the legacy point guide.

### Typed ports
| Socket | Domain | Direction | Local position (m) | Role | Source |
| --- | --- | --- | --- | --- | --- |
| `Tie` | Rope | Bidirectional | (0, 0.4, 0.12) | Guide: two rope spans, never an end | `parts/PulleyPart.cs@a6c914e:L14-L18`; `engine/RopeNetwork.cs@a6c914e:L69-L78` |

### Sensors and activation
None.

### Work and energy stores
None: redirects tension without storing or dissipating work.

### Parameters
None (`parts/catalog/pulley.tres@a6c914e:L14-L14`).

### Cosmetic curves and UI bindings
Wheel angle advances by route travel ÷ 0.4 m, wrapped to 2π, only while the route is complete and taut (`parts/PulleyPart.cs@a6c914e:L34-L38`; `engine/RopeNetwork.cs@a6c914e:L50-L58`). Rope tangents and arcs per `DESIGN.md@a6c914e:L284-L284`. Pick radius 0.65 m (`parts/PulleyPart.cs@a6c914e:L21-L21`).

### Art
Navy `#293954` mount, cream `#fff8e9` post and rim, wheel in the Pulley colour `#d69c47`, navy spoke bar, gold `#f7cb52` marker (`parts/PulleyPart.cs@a6c914e:L22-L32`; `parts/catalog/pulley.tres@a6c914e:L13-L13`; [DESIGN colour system](../../../DESIGN.md#colour-system)).

### Catalogue and inventory entry
Id `pulley`, title "Pulley", category Ropes, "Redirect a continuous rope. Connect both sides to loads or anchors; unfinished ropes cannot carry tension." (`parts/catalog/pulley.tres@a6c914e:L8-L14`). Add `WorkshopPartKind.Pulley` with an allowance (`engine/gpu/WorkshopInventory.cs@a6c914e:L9-L23`).

### Variants
One outcome in the row; the load-attached sheave is EL-207.

## 3. Engine capabilities

Families: EnvironmentState, GeometryQuery, JointConstraint, RigidBodyDynamics, TensionTransmission ([map row](../general-engine-element-map.md); [element-03.json](../../coverage/engine/element-03.json)).

**Exists now**
- Static box and sphere bodies: `engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L120`.

**Missing** ([CAT-053](CAT-053-pulley.md#3-engine-capabilities))
- Rope domain, guide role and the tension-only route with groove tangents and arcs, plus the committed rope-travel read: Story 10.2; decision S257 rope-port ([decisions](../invest/decisions.md#s257)). Owner S308.

**Dependencies.** EL-205 rope, CAT-058 anchor, CAT-067 weight (Stories 10.2 and 10.4).

## 4. Sources and legacy

- Requirement: "Supported rotating sheave redirects a routed rope through actual tangent geometry"; outcome "Disconnected or misrouted rope receives no invisible coupling" ([element-206](../requirements.md#element-206)).

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | Fixed, frictionless point-guide pulley; no moving block or wheel inertia | `parts/PulleyPart.cs@a6c914e:L6-L6` | carry forward (fixed, frictionless); do not carry forward (point guide) | Tangent geometry supersedes the point guide. |
| 2 | Groove radius 0.4 m; tie socket depth 0.12 m | `engine/MachineData.cs@a6c914e:L95-L98` | carry forward | Geometry. |
| 3 | A route ending at a guide is incomplete and carries no tension | `engine/RopeNetwork.cs@a6c914e:L34-L38` | carry forward | "Misrouted rope receives no coupling." |
| 4 | Accepted: two fixed pulleys between masses (1, 4), (4, 1), (1, 1): rope taut at its length ±0.001 m; the wheel turns only for unequal masses; Reset returns wheels to 0 and replays | `CuriousContraptions.tests/RopeTests.cs@a6c914e:L21-L68` | carry forward (re-freeze tolerances) | Positive and balanced control. |

**Files harvested:**
- `parts/PulleyPart.cs`
- `parts/catalog/pulley.tres`
- `engine/MachineData.cs` (pulley geometry constants)
- `engine/RopeNetwork.cs`
- `CuriousContraptions.tests/RopeTests.cs`
- The remaining pulley files are harvested in [CAT-053](CAT-053-pulley.md#4-legacy-harvest) (Batch C). Searched with no further EL-206 knowledge: `engine/bridge/`, `diagnostics/`.

## 5. Acceptance outline

Follow [element-206](../requirements.md#element-206) and the [CAT-053 outline](CAT-053-pulley.md#5-acceptance-outline).
- **Chrome UI recipe.** Place two pulleys high and Weights of 1 kg and 4 kg below; connect weight → pulley → pulley → weight with rope links. Run.
- **Positive.** The heavy Weight descends, the light one rises, both wheels turn with rope travel.
- **Negative/control.** Equal masses stay balanced and the wheels still; a route ending at a pulley is dashed, carries nothing, and the load falls.
- **Boundaries.** Reversed link order; depth route (`pulley_depth`); invalid branch or loop refused.
- **Run/Reset.** Weights, wheel angles and span lengths restored.
- **Save/Load.** Identities, `Tie` links and lengths round-trip.
- **Integrations.** Retained rope behaviour [todo-140](../requirements.md#todo-140) and qualification [todo-141](../requirements.md#todo-141); campaign row 2 ("... fixed/moving pulley roles ..."): first use 11–20, moving/load variants into 41–50, reuse 31–50, 61–80, 136–150 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)); counterweight lessons with CAT-067 and the switch-and-lamp chain (CAT-063, CAT-035).

## 6. Open questions

None beyond the [CAT-053 open questions](CAT-053-pulley.md#6-open-questions).
