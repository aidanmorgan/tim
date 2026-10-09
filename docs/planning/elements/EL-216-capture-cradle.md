# EL-216 · Capture cradle named-identity spec

Story 7.0 named-identity spec (Batch J). Baseline commit `a6c914e`; every citation is `path@a6c914e:Lstart-Lend` and resolves with `git show a6c914e:<path> | sed -n 'start,endp'`. **Proposed** marks an unsourced design value with a one-line justification; the owner may revise it. All values are canonical IEEE-754 f32 game values inside the [game-grade envelope](../../gpu-f32-physics.md#game-grade-envelope).

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-216 |
| Name | Capture cradle |
| Type | Mechanical |
| Anchor | [requirements.md#element-216](../requirements.md#element-216); [named entry](../invest/named-elements.md#element-216); campaign 1–10, practice 41–50, reuse 96 and 149 ([campaign reservations](../invest/campaign-reservations.md)) |
| Proof owner | S382 |
| Refines / extends | No CAT spec. Box-wall cup like [CAT-004 basket](CAT-004-basket.md) but without the receiver's capture sensor or guiding force; released through a separately placed gate of the [CAT-051 powered gate](CAT-051-powered_gate.md) / [EL-184](EL-184-mechanical-ball-gate.md) slider family, sized to the cup |
| Related | EL-215 Damped cushion (same campaign slots), EL-184 Mechanical ball gate |
| Roadmap story | unscheduled |
| Status | not started |

## 2. Declaration

### Bodies and shapes
One static body of box colliders, all full extents — **proposed** sizes:
- floor 0.78 × 0.1 × 0.78 m, falling 3° toward the outlet side;
- four walls 0.08 m thick rising 0.3 m above the floor, enclosing an inner square of 0.62 m.

Justification: the 0.62 m inner square seats the 0.56 m Bowling ball but not the 0.68 m Basketball (`engine/gpu/WorkshopConstruction.cs@a6c914e:L45-L53`), so the requirement's "oversize cargo can escape" control uses an existing catalogue part; the 0.3 m lip is above the Bowling ball's 0.28 m radius, so a gently seated ball stays, and below its 0.56 m diameter, so an energetic one can leave. Multi-box static bodies are admitted now (the receiver uses five, `engine/gpu/ReceiverGeometry.cs@a6c914e:L10-L18`).
- With `outlet` = SideGate, the outlet wall has a floor-level opening 0.6 m wide and 0.3 m high (the full wall height) — **proposed**: wide enough for the 0.56 m Bowling ball to roll out, and no taller than the cup. It is not a tube mouth: the standard 1.3 m bore (`engine/gpu/WorkshopPipe.cs@a6c914e:L8-L10`) and the 1.44 m legacy gate blade (`parts/PoweredGatePart.cs@a6c914e:L22-L22`) are larger than this cup.

### Mass and material
Static, no mass. Material restitution 0.1, threshold 0.1 m/s, friction 0.5, rolling resistance 0 — **proposed**: soaks up a gentle landing so cargo settles, while an energetic one still escapes over the lip.

### Constraints and joints
None. Cargo is held only by gravity and contact; no attachment or ownership transfer.

### Typed ports
None. The release is the separately placed gate's job.

### Sensors and activation
None. Explicitly no capture radius, residence sensor or guiding force: the receiver's capture dwell and planar guide (`engine/gpu/WorkshopConstruction.cs@a6c914e:L79-L101`) are not part of the cradle.

### Work and energy stores
None.

### Parameters

| Name | Type | Range | Default | Unit | Source |
| --- | --- | --- | --- | --- | --- |
| `outlet` | enum `CradleOutlet { Closed, SideGate }` | 2 values | Closed | — | **proposed**: a closed cup, or one whose release is taught with a placed gate |

### Release gate (separate part, SideGate only)
A sliding blade of full extents 0.7 × 0.4 × 0.06 m placed against the outside of the opening, overlapping it by 0.05 m on each side and 0.1 m above the lip — **proposed**: the CAT-051/EL-184 slider and finite-work drive at cup scale (`engine/SlidingBlade.cs@a6c914e:L12-L34`). Whether that is a size option of CAT-051/EL-184 or a separate gate part is open question 2.

### Cosmetic curves and UI bindings
None: the cup's geometry is its whole state. UI: `outlet` through the configuration pattern (`ui/WorkshopConfiguration.cs@a6c914e:L43-L75`).

### Art
Walls in the Basket colour `#4aab94` with a cream `#fff8e9` rim and navy `#293954` base ([DESIGN colour system](../../../DESIGN.md#colour-system)); no halo, which distinguishes it from the receiver's capture halo (`parts/BasketPart.cs@a6c914e:L16-L22`). **Proposed**.

### Catalogue and inventory entry
Id `cradle`, title "Capture cradle", category Motion — **proposed**. Add `WorkshopPartKind.Cradle` with a counted allowance (`engine/gpu/WorkshopInventory.cs@a6c914e:L9-L23`).

### Variants
One outcome in the row; `outlet` is configuration.

## 3. Engine capabilities

Families: ContactImpulse, EnvironmentState, FiniteLedger, GeometryQuery, JointConstraint, RigidBodyDynamics, SlidingFriction, plus StateTransaction ([map row](../general-engine-element-map.md); [element-03.json](../../coverage/engine/element-03.json)).

**Exists now**
- Static multi-box bodies, sphere–box contact, friction, restitution and rolling resistance: `engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L120`; `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L701-L719`, `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L886-L900`.

**Missing**
- A `WorkshopPartKind` member and save fields. A cup-scale release gate (slider, finite-work drive): Stories 6.4 and 8.2. Owner S382.

**Dependencies.** For SideGate: a cup-scale gate from the CAT-051 (Story 8.2) or EL-184 family.

## 4. Sources and legacy

- Requirement: "A physical retaining cup supports cargo after contact without remote ownership transfer; a separately placed/taught gate can release it"; outcome "Oversize or energetic cargo can escape; no invisible capture radius or forced attachment" ([element-216](../requirements.md#element-216)).

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | Receiver walls as static boxes (floor half (0.75, 0.075, 0.75) at y −0.45; walls 0.5 m half-height) | `engine/gpu/ReceiverGeometry.cs@a6c914e:L10-L18` | carry forward (construction pattern), not the sizes | Static box cup. |
| 2 | Receiver capture: margin 0.02, speed limit 1.5 m/s, dwell 0.35 s; planar guide force region | `engine/gpu/WorkshopConstruction.cs@a6c914e:L79-L101` | do not carry forward | A capture sensor and guiding force would be the "invisible capture" the cradle forbids. |
| 3 | Powered gate blade half extents (0.06, 0.72, 0.72), stroke 1.55 m, slider controller | `parts/PoweredGatePart.cs@a6c914e:L15-L22`; `engine/SlidingBlade.cs@a6c914e:L12-L34` | carry forward (slider family), not the sizes | Release-gate mechanism at cup scale. |
| 4 | Accepted: a horizontal gate supports settled cargo while closed and releases it when opened | `CuriousContraptions.tests/PoweredGateTests.cs@a6c914e:L79-L117` | carry forward | Release by a separate gate. |

**Files harvested:**
- `parts/BasketPart.cs` (halo art contrast)
- `parts/PoweredGatePart.cs` (gate blade size)
- `engine/SlidingBlade.cs` (slider gate family)
- `CuriousContraptions.tests/PoweredGateTests.cs`
- Searched with no hit for `cradle`: `parts/`, `engine/` (including `engine/physics/` and `engine/bridge/`), `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/`, `reference/`, `diagnostics/`.

## 5. Acceptance outline

Follow [element-216](../requirements.md#element-216) and the [mechanics profile](../invest/profiles.md#mechanics).
- **Chrome UI recipe.** Place the cradle under a ramp exit; set `outlet` SideGate and place the cup-scale gate against the opening, wired to a battery and switch. Run.
- **Positive.** A gently arriving Bowling ball settles in the cup and stays; the gate opens on command and the ball rolls out.
- **Negative/control.** A fast Bowling ball bounces over the lip and escapes; a Basketball (0.68 m, larger than the 0.62 m cup) never seats below the lip; it is not retained inside the cup; with `outlet` SideGate and no gate placed the ball rolls out at once.
- **Boundaries.** Arrival speed just below and above escape; tilted cradle; gate closing on a ball in the opening.
- **Run/Reset.** Cargo returns to its authored pose.
- **Save/Load.** `outlet`, pose and the separately placed gate round-trip.
- **Integrations.** Element reservations 1–10, practice 41–50, reuse 96 and 149 ([element-216](../requirements.md#element-216)); campaign row 1 ("cushion/cradle") first use 1–10, reuse 21–30, 41–50, 91–100, 136–150 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)); paired with EL-215 cushion and a CAT-051/EL-184 gate.

## 6. Open questions

1. Box cup (proposed) or a rounded bowl collider? Unspecified — owner decision.
2. Is the cup-scale release gate a size option of CAT-051/EL-184 or a separate gate part? Owner decision.
3. Inner size 0.62 m (proposed) makes the Basketball the oversize control; should the cradle instead hold Basketballs, with oversize cargo left to a future part? Owner decision.
