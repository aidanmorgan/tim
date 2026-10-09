# EL-205 · Rope named-identity spec

Story 7.0 named-identity spec (Batch J). Baseline commit `a6c914e`; every citation is `path@a6c914e:Lstart-Lend` and resolves with `git show a6c914e:<path> | sed -n 'start,endp'`. **Proposed** marks an unsourced design value with a one-line justification; the owner may revise it. All values are canonical IEEE-754 f32 game values inside the [game-grade envelope](../../gpu-f32-physics.md#game-grade-envelope).

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-205 |
| Name | Rope |
| Type | Mechanical |
| Anchor | [requirements.md#element-205](../requirements.md#element-205); [named entry](../invest/named-elements.md#element-205); scope [campaign-element-coverage](../requirements.md#campaign-element-coverage) |
| Proof owner | S307 |
| Refines / extends | No CAT spec of its own: the rope is a connection. Specified jointly with [CAT-058 rope anchor](CAT-058-rope_anchor.md) (rope harvest) and [CAT-053 pulley](CAT-053-pulley.md) |
| Related | EL-206 Fixed pulley, EL-207 Moving pulley, EL-064 Metal loop anchor, CAT-067 Weight, EL-184 Mechanical ball gate |
| Roadmap story | Story 10.2 (CAT-053 with CAT-058) |
| Status | not started |

## 2. Declaration

### Bodies and shapes
No collider and no mass: the rope is a massless, tension-only route between attachment points (legacy `engine/physics/PhysicsRopeJoint.cs@a6c914e:L114-L122`). Its artwork is a length-matched approximation, never collision geometry (`DESIGN.md@a6c914e:L284-L284`).

### Mass and material
None (massless, legacy). Rope–groove friction is zero at fixed pulleys ([CAT-053](CAT-053-pulley.md)).

### Constraints and joints
- One unilateral length constraint per complete route: inactive while the current route length is below the authored maximum; when taut it only resists lengthening (`engine/physics/PhysicsRopeJoint.cs@a6c914e:L114-L122`). Realised as a TGS Soft unilateral row in the worker.
- Route length = sum of span lengths through guides; spans attached to the same rigid body contribute a constant length (see [CAT-058 R11, R15](CAT-058-rope_anchor.md#4-legacy-harvest)).
- Connected-body collision stays enabled: a slack rope never suppresses contact between its loads ([CAT-058 R17](CAT-058-rope_anchor.md#4-legacy-harvest)).

### Typed ports
A rope is a connection in the Rope domain between `Tie` sockets. Loads and anchors accept one rope end, guides (pulleys) two; a third end, a duplicate span or a part with no attachment role is refused ("Ropes cannot branch") (`engine/RopeNetwork.cs@a6c914e:L69-L78`). Attachment roles {None, Load, Anchor, Guide} and states {Open, Slack, Taut} are closed enums (`engine/ConnectionPort.cs@a6c914e:L11-L12`).

### Sensors and activation
None. State is derived: Open when either route end is a guide, Slack when shorter than its length, else Taut (`engine/RopeNetwork.cs@a6c914e:L34-L38`).

### Work and energy stores
None: a massless inextensible rope stores no energy and transmits only the tension the loads create.

### Parameters

| Name | Type | Range | Default | Unit | Source |
| --- | --- | --- | --- | --- | --- |
| span length | f32, per span | 0.05–200 | straight-line distance between the two `Tie` sockets at connect time | m | `engine/ConnectionPort.cs@a6c914e:L47-L52`; `reference/cpu/MachineWorld.cs@a6c914e:L686-L691` |
| slack threshold | constant | — | 0.001 legacy | m | do not carry forward the literal ([CAT-058 R13](CAT-058-rope_anchor.md#4-legacy-harvest)); an f32 threshold is frozen in Story 10.2 |

### Cosmetic curves and UI bindings
Warm-wood rope; gold endpoint knots and no pulley knots; free spans straight when taut and curved when slack; unfinished threading dashed and carrying no tension; tangent legs meet arcs outside the wheel rims (`DESIGN.md@a6c914e:L284-L284`). Artwork reads committed poses only. UI: connect `Tie` to `Tie` with the contextual Connect; length is set on connect.

### Art
Warm-wood rope with gold `#f7cb52` knots; pulleys and anchors keep their `#d69c47` identity ([DESIGN colour system](../../../DESIGN.md#colour-system)).

### Catalogue and inventory entry
Not a catalogue part; a Rope-domain connection kind added with `WorkshopConnectionDomain.Rope` (current domains are Activation and Electrical, `engine/gpu/WorkshopConnections.cs@a6c914e:L8-L8`).

### Variants
One outcome in the row; no variants. Rope cutting is a separate obligation ([CAT-058 R29](CAT-058-rope_anchor.md#4-legacy-harvest)).

## 3. Engine capabilities

Families: EnvironmentState, GeometryQuery, JointConstraint, RigidBodyDynamics, TensionTransmission ([map row](../general-engine-element-map.md); [element-03.json](../../coverage/engine/element-03.json)).

**Exists now**
- Dynamic loads (spheres and boxes) and typed connection storage: `engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L120`; `engine/gpu/WorkshopConnections.cs@a6c914e:L8-L13`.

**Missing**
- Rope domain, `Tie` sockets, attachment roles and the tension-only route row (TensionTransmission/JointConstraint): Story 10.2; decision S257 rope-port, next implementation S688 ([decisions](../invest/decisions.md#s257)). Owner S307.
- Committed route read for rope artwork: Story 10.2.

**Dependencies.** CAT-058 anchor and CAT-067 weight as endpoints; CAT-053 pulley as guide (Story 10.2).

## 4. Sources and legacy

- Requirement: "Finite-length tension-only connector couples actual routed endpoints"; outcome "Slack rope cannot push or pull until tension develops" ([element-205](../requirements.md#element-205)).

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | Tension-only: no equality row; inactive below the maximum; resists only lengthening | `engine/physics/PhysicsRopeJoint.cs@a6c914e:L114-L122` | carry forward (law), do not carry forward (CPU impulse code) | The EL behaviour. |
| 2 | Span length finite, 0.05–200 m; non-rope links carry no length | `engine/ConnectionPort.cs@a6c914e:L47-L52` | carry forward | Admission bounds. |
| 3 | Length set from socket distance at connect time | `reference/cpu/MachineWorld.cs@a6c914e:L686-L691` | carry forward | Measured route. |
| 4 | No branching; loads and anchors one end, guides two | `engine/RopeNetwork.cs@a6c914e:L69-L78` | carry forward | Topology. |
| 5 | Open routes and closed loops rejected | `engine/RopeNetwork.cs@a6c914e:L100-L112` | carry forward | Cycle admission. |
| 6 | Accepted: a lengthened rope is Slack and the weight falls; it becomes Taut and holds; an upward impulse is not resisted | `CuriousContraptions.tests/RopeTests.cs@a6c914e:L126-L156` | carry forward (re-freeze tolerances) | Slack cannot push. |
| 7 | Accepted: a fixed guide transfers tension but never pushes | `CuriousContraptions.tests/RoutedRopeTests.cs@a6c914e:L33-L45` | carry forward | Tension only through guides. |

The remaining rope facts (3D pendulum bounds, Reset identities, trampoline tether, rejected point-guide geometry) are harvested in [CAT-058](CAT-058-rope_anchor.md#4-legacy-harvest).

**Files harvested:**
- `engine/physics/PhysicsRopeJoint.cs`
- `engine/RopeNetwork.cs`
- `engine/ConnectionPort.cs`
- `reference/cpu/MachineWorld.cs` (connect-time length)
- `CuriousContraptions.tests/RopeTests.cs`
- `CuriousContraptions.tests/RoutedRopeTests.cs`
- The remaining rope files (for example `engine/SceneRopeJoint.cs`, `CuriousContraptions.tests/SceneRopeBindingTests.cs`, `CuriousContraptions.tests/TrampolineRopeTests.cs`) are harvested in [CAT-058](CAT-058-rope_anchor.md#4-legacy-harvest) (Batch C). Searched with no further rope knowledge for this identity: `engine/bridge/`, `diagnostics/`.

## 5. Acceptance outline

Follow [element-205](../requirements.md#element-205), [CAT-058](CAT-058-rope_anchor.md) and Story 10.2.
- **Chrome UI recipe.** Place a rope anchor high and a Weight below; connect `Tie` → `Tie`; then edit the construction so the rope is longer than the gap. Run.
- **Positive.** The taut rope holds the Weight at its length.
- **Negative/control.** A slack rope lets the Weight fall freely until taut; pushing the Weight toward the anchor meets no resistance; a third rope end on a Weight is refused.
- **Boundaries.** Length 0.05 and 200 m; a route ending at a pulley (Open, no tension); 3D pendulum.
- **Run/Reset.** Endpoints and lengths restored exactly.
- **Save/Load.** Rope links with endpoints and lengths round-trip.
- **Integrations.** Retained rope behaviour [todo-140](../requirements.md#todo-140), qualification [todo-141](../requirements.md#todo-141) and rope artwork [todo-469](../requirements.md#todo-469); campaign row 2 ("... rope ..."): first use 11–20, moving/load variants into 41–50, reuse 31–50, 61–80, 136–150 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)); routed through EL-206/EL-207 pulleys to CAT-067 weights and EL-184.

## 6. Open questions

1. Is the rope extensible (a compliant tension row) or inextensible as in the legacy? Unspecified — owner decision.
2. May the player edit a rope's length after connecting, or only by moving parts before connecting? Owner decision.
