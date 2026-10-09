# EL-184 · Mechanical ball gate named-identity spec

Story 7.0 named-identity spec (Batch J). Baseline commit `a6c914e`; every citation is `path@a6c914e:Lstart-Lend` and resolves with `git show a6c914e:<path> | sed -n 'start,endp'`. **Proposed** marks an unsourced design value with a one-line justification; the owner may revise it. All values are canonical IEEE-754 f32 game values inside the [game-grade envelope](../../gpu-f32-physics.md#game-grade-envelope).

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-184 |
| Name | Mechanical ball gate |
| Type | Mechanical |
| Anchor | [requirements.md#element-184](../requirements.md#element-184); [named entry](../invest/named-elements.md#element-184); scope [campaign-element-coverage](../requirements.md#campaign-element-coverage) ("powered and mechanical gates") |
| Proof owner | S366 |
| Refines / extends | No CAT spec of its own. Shares the conduit, blade and slider of [CAT-051 powered gate](CAT-051-powered_gate.md) but is driven by linkage work instead of electricity; rope input per [CAT-058 rope anchor](CAT-058-rope_anchor.md) and [CAT-053 pulley](CAT-053-pulley.md) |
| Related | EL-205 Rope, EL-206/EL-207 pulleys, EL-111 governor and EL-156–EL-158 cams (linear linkage sources), CAT-067 Weight |
| Roadmap story | unscheduled (CAT-051 is Story 8.2) |
| Status | not started |

## 2. Declaration

### Bodies and shapes
Sourced from the legacy powered gate; `AddBox` sizes are full extents, `BladeHalf` and tube values are halves.
- **Conduit (static).** Clear tube section, half-length 0.4 m (0.8 m long), bore 0.65 m, wall 0.70 m; collars half-length 0.09 m, outer 0.78 m at x = ±0.4; mouths `Start`/`End` at x = ∓0.49 (`parts/PoweredGatePart.cs@a6c914e:L26-L30`, `parts/PoweredGatePart.cs@a6c914e:L35-L42`; `TubeProxy` second field is HalfLength, `engine/TubeProxy.cs@a6c914e:L6-L6`).
- **Frame (static).** Header box 0.8 × 0.65 × 1.8 m at (0, 1.65, 0); two posts 0.24 × 1.7 × 0.12 m at z = ±0.82 (`parts/PoweredGatePart.cs@a6c914e:L43-L45`).
- **Blade (dynamic).** Box with half extents 0.06 × 0.72 × 0.72 m (full 0.12 × 1.44 × 1.44 m) across the bore (`parts/PoweredGatePart.cs@a6c914e:L22-L22`, `parts/PoweredGatePart.cs@a6c914e:L46-L47`).
- **Pull rod.** A cream box 0.08 × 0.6 × 0.08 m on top of the blade carrying the rope tie — **proposed**: gives the rope a visible attachment above the header.

### Mass and material
- Blade 1 kg; inertia of a homogeneous box (`engine/SlidingBlade.cs@a6c914e:L14-L14`, `engine/SlidingBlade.cs@a6c914e:L23-L26`).
- Material restitution 0.1, bounce threshold 0.1 m/s, friction 0.3 (`parts/PoweredGatePart.cs@a6c914e:L23-L23`).

### Constraints and joints
- Blade slider along the part's +Y, travel 0 to 1.55 m, connected collision disabled (`parts/PoweredGatePart.cs@a6c914e:L15-L15`; `engine/SlidingBlade.cs@a6c914e:L28-L34`).
- Return spring 14 N/m and damping 4 N·s/m close the blade when the linkage slackens (`engine/SlidingBlade.cs@a6c914e:L17-L18`).
- No motor row: the only drive is linkage tension.

### Typed ports
| Socket | Domain | Direction | Role | Source |
| --- | --- | --- | --- | --- |
| `Tie` | Rope | Bidirectional | rope end on the pull rod (attachment role Load) | **proposed** position (0, 2.2, 0); rope socket contract from `engine/ConnectionPort.cs@a6c914e:L47-L52` and [CAT-058](CAT-058-rope_anchor.md) |

Tube mouths are geometric. No electrical or activation port: the requirement is "actual linkage work".

### Sensors and activation
None. Gate state is classified from committed slider motion as Closed, Opening, Open, Closing or Blocked (`engine/SlidingBlade.cs@a6c914e:L7-L7`, `engine/SlidingBlade.cs@a6c914e:L49-L53`) for presentation only.

### Work and energy stores
Spring energy ½·k·x² (up to 16.8 J at full stroke) and blade potential energy, both supplied by rope tension. Opening needs more than the 9.81 N blade weight plus spring force, up to 31.5 N at full stroke: a 4 kg Weight (39.2 N) opens it fully; a 1 kg Weight cannot lift it.

### Parameters
None — **proposed**: the sourced blade, spring and stroke fix the required pull, which is the lesson.

### Cosmetic curves and UI bindings
- Blade art follows the committed slider pose. The indicator sphere at (0.42, 1.65, 0.55) shows amber `#e8b764` when Blocked, gold `#f7cb52` while open and slate `#556573` closed (`parts/PoweredGatePart.cs@a6c914e:L48-L50`, `parts/PoweredGatePart.cs@a6c914e:L57-L63`), driven by a new committed slider-state source — **proposed** colour mapping without supply.
- UI: rope connection through the contextual Connect.

### Art
Clear shell `#66b8c9` alpha 0.16, cream `#fff8e9` collars and posts, navy `#293954` header, gold `#f7cb52` blade (`parts/PoweredGatePart.cs@a6c914e:L35-L47`); catalogue colour of the powered gate (0.97, 0.796, 0.322) (`parts/catalog/powered_gate.tres@a6c914e:L13-L13`). A gold rope eye on the pull rod — **proposed**.

### Catalogue and inventory entry
Id `mechanical_gate`, title "Mechanical gate", category Ropes — **proposed**: placed next to the rope parts it needs. Add `WorkshopPartKind.MechanicalGate` with a counted allowance (`engine/gpu/WorkshopInventory.cs@a6c914e:L9-L23`).

### Variants
One outcome in the row; the powered gate is CAT-051.

## 3. Engine capabilities

Families: ContactImpulse, EnvironmentState, FiniteLedger, FiniteWorkActuation, GeometryQuery, JointConstraint, RigidBodyDynamics, SignalPropagation, plus StateTransaction ([map row](../general-engine-element-map.md); [element-02.json](../../coverage/engine/element-02.json)).

**Exists now**
- Dynamic box and static boxes with contact and friction: `engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L120`.
- Tube profile declaration (uncompiled): `engine/gpu/WorkshopPipe.cs@a6c914e:L6-L20`.

**Missing**
- Slider with spring and limits (Story 6.4); hollow tube collider (Stories 6.6–6.7); rope domain, tension-only route and `Tie` socket (Story 10.2; decision S257 rope-port, next implementation S688, [decisions](../invest/decisions.md#s257)). Owner S366.
- A slider-state cosmetic source (closed set `engine/gpu/WorkshopCosmetic.cs@a6c914e:L7-L7`).

**Dependencies.** CAT-058/CAT-053 ropes and pulleys (Story 10.2), CAT-067 weight (Story 10.4), CAT-048 tube (Story 6.6); shares geometry with CAT-051 (Story 8.2).

## 4. Sources and legacy

- Requirement: "Actual linkage work moves a collision blade controlling a conduit opening"; outcome "Slack linkage or blocked blade prevents instantaneous release" ([element-184](../requirements.md#element-184)).
- Campaign: "powered and mechanical gates", first use 21–30 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)).

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | Stroke 1.55 m; blade half extents (0.06, 0.72, 0.72); material (0.1, 0.1, 0.3); mouths at ±0.49 with the standard bore | `parts/PoweredGatePart.cs@a6c914e:L15-L30` | carry forward | Shared gate geometry. |
| 2 | Blade 1 kg, spring 14, damping 4; slider frame and stroke; connected collision disabled | `engine/SlidingBlade.cs@a6c914e:L12-L34` | carry forward (geometry and spring), do not carry forward (motor constants 60 N, 120 W) | No motor in the mechanical gate. |
| 3 | Gate states and Blocked classification | `engine/SlidingBlade.cs@a6c914e:L49-L53` | carry forward (states) | Observable jam. |
| 4 | Accepted: a horizontal gate supports settled cargo while closed and releases it when opened; Reset restores the construction | `CuriousContraptions.tests/PoweredGateTests.cs@a6c914e:L79-L117` | carry forward | Support then release. |
| 5 | Accepted: only an open gate lets the ball through, also rotated (20°, 30°, 40°) | `CuriousContraptions.tests/PoweredGateTests.cs@a6c914e:L152-L178` | carry forward | Positive and control. |
| 6 | Accepted: a slack rope does not push; a taut rope holds the load; motion toward the anchor is not resisted | `CuriousContraptions.tests/RopeTests.cs@a6c914e:L126-L156` | carry forward | "Slack linkage" control. |

No legacy mechanical gate exists: the only gate part is the electrically powered one.

**Files harvested:**
- `parts/PoweredGatePart.cs`
- `parts/catalog/powered_gate.tres`
- `engine/SlidingBlade.cs`
- `engine/TubeProxy.cs`
- `engine/ConnectionPort.cs` (rope link validation)
- `CuriousContraptions.tests/PoweredGateTests.cs`
- `CuriousContraptions.tests/RopeTests.cs`
- Searched with no hit for `mechanical gate` or a rope- or lever-driven gate: `parts/`, `engine/` (including `engine/physics/` and `engine/bridge/`), `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/`, `reference/`, `diagnostics/`.

## 5. Acceptance outline

Follow [element-184](../requirements.md#element-184) and the [mechanics profile](../invest/profiles.md#mechanics).
- **Chrome UI recipe.** Place a tube route with the mechanical gate, a pulley above it and a Weight on a ledge; connect gate `Tie` → pulley → Weight `Tie` with rope links. Push the Weight off with a ball. Run.
- **Positive.** The falling 4 kg Weight tightens the rope, lifts the blade and the queued ball passes.
- **Negative/control.** With extra rope length the Weight falls freely first and the blade only starts rising when the rope goes taut; a 1 kg Weight cannot open it; a ball wedged under the blade blocks closing.
- **Boundaries.** Partial opening smaller than the ball; rotated gate; rope cut or disconnected after Reset.
- **Run/Reset.** Blade closed; rope lengths and endpoints restored.
- **Save/Load.** Pose and rope links with lengths round-trip.
- **Integrations.** Campaign row "... powered and mechanical gates ...": first use 21–30, reuse 41–50, 111–120, 136–150 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)); rope and pulley behaviour [todo-140](../requirements.md#todo-140) and [todo-141](../requirements.md#todo-141) with CAT-053, CAT-058 and CAT-067; release gate for EL-216.

## 6. Open questions

1. Linkage input: rope only (proposed), or also a lever/linear `LinkageIn` from EL-111/EL-156? Unspecified — owner decision.
2. Should the blade travel vertically like CAT-051 (proposed) or swing on a hinge? Owner decision.
