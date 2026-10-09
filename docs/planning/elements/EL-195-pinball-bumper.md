# EL-195 · Pinball bumper named-identity spec

Story 7.0 named-identity spec (Batch J). Baseline commit `a6c914e`; every citation is `path@a6c914e:Lstart-Lend` and resolves with `git show a6c914e:<path> | sed -n 'start,endp'`. **Proposed** marks an unsourced design value with a one-line justification; the owner may revise it. All values are canonical IEEE-754 f32 game values inside the [game-grade envelope](../../gpu-f32-physics.md#game-grade-envelope); current declarations still store them as binary16 ([f32 migration status](../../gpu-f32-physics.md#f32-migration-status)) and they are recorded here by decimal meaning.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-195 |
| Name | Pinball bumper |
| Type | Mechanical |
| Anchor | [requirements.md#element-195](../requirements.md#element-195); [named entry](../invest/named-elements.md#element-195); scope [campaign-element-coverage](../requirements.md#campaign-element-coverage) |
| Proof owner | S301 |
| Refines / extends | Refines [CAT-015 bumper](CAT-015-bumper.md) ([requirement](../requirements.md#current-cat-015)); the supplied variant extends it with [CAT-005 battery](CAT-005-battery.md) supply |
| Related | CAT-066 Wall (`wall_and_bumper`), EL-194 Springboard (finite elastic store) |
| Roadmap story | Charged variant: Stories 6.2 and 6.3 (CAT-015). Supplied variant: unscheduled |
| Status | partial: `WorkshopPartKind.PinballBumper` exists with a contact-work declaration; the finite work law (Story 6.2) and the supplied variant are not delivered |

## 2. Declaration

### Bodies and shapes
One static body with one Sphere collider, radius 0.65 m (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L119-L122`; legacy head radius `parts/BumperPart.cs@a6c914e:L16-L16`).

### Mass and material
Static, no mass. Material restitution 1, bounce threshold 0.1 m/s, friction 0.3, rolling resistance 0 (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L109-L110`).

### Constraints and joints
None.

### Typed ports
| Socket | Domain | Direction | Variant | Source |
| --- | --- | --- | --- | --- |
| recharge connection (mapping pending) | owner decision pending | Input | Charged | Baseline has no ports (`engine/gpu/WorkshopConnections.cs@a6c914e:L24-L24`); owner now requires recharge in Story 6.2, per CAT-015 §6 |
| `PowerIn` | Electrical | Input | Supplied | **proposed**: the row's "bounded supplied ... actuator"; identity exists in `engine/gpu/WorkshopConnections.cs@a6c914e:L8-L9` |

### Sensors and activation
A contact-work declaration qualifies each dynamic target's impact at approach speed ≥ 0.05 m/s, with a per-target cooldown of 72 physics steps (0.15 s at 480 Hz) (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L123-L125`; `engine/gpu/ContactWorkDeclaration.cs@a6c914e:L19-L36`).

### Work and energy stores
- **Charged.** One finite reservoir shared by all targets, preload ½·m_ref·s² with m_ref = 1 kg, so 32 J at the default strength 8 m/s; at most 200 J (`engine/gpu/WorkshopBumper.cs@a6c914e:L5-L28`; `engine/gpu/ContactWorkDeclaration.cs@a6c914e:L21-L36`). Owner decision (9 Oct 2026): insufficient positive energy pays only an affordable partial boost; when exhausted the bumper is an ordinary restitution-1 sphere. Recharging requires actual work from the connected source; the source and rate remain pending in CAT-015 §6.
- **Supplied.** No reservoir; each qualified kick draws the same calibrated energy from the connected supply — **proposed**: keeps one strength meaning for both variants. Without supply it is an ordinary sphere.

### Parameters

| Name | Type | Range | Default | Unit | Source |
| --- | --- | --- | --- | --- | --- |
| `strength` | f32 | 0–20 | 8 | m/s target kick for the 1 kg reference mass | `engine/gpu/WorkshopBumper.cs@a6c914e:L8-L27`; legacy bits `parts/catalog/bumper.tres@a6c914e:L8-L12` |
| `supply` | enum `BumperSupply { Charged, Supplied }` | 2 values | Charged | — | **proposed**: selects the variant, a closed set |

### Cosmetic curves and UI bindings
- Contact-work cosmetic: Linear, 0.32 s, sine-squared pulse, saturating sum (`engine/gpu/WorkshopCosmetic.cs@a6c914e:L56-L57`). Head scales 1 → 0.88; impact ring scales 1 → 1.24 and blends cream to gold (`parts/BumperPart.cs@a6c914e:L25-L30`). Only accepted kicks pulse.
- UI: strength SpinBox 0–20 with Apply (`ui/WorkshopConfiguration.cs@a6c914e:L43-L75`). Pick radius 0.8 m (`parts/BumperPart.cs@a6c914e:L17-L17`).

### Art
Head in the Pinball bumper colour `#de7059`; navy-slate base cylinder `#273744`; gold cap `#f7cb52`; cream rings `#fff0c2` (`parts/BumperPart.cs@a6c914e:L18-L24`; [DESIGN colour system](../../../DESIGN.md#colour-system)). The supplied variant adds a gold `PowerIn` stud — **proposed**.

### Catalogue and inventory entry
Id `bumper`; `WorkshopPartKind.PinballBumper` exists (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`); catalogue resource `parts/catalog/bumper.tres@a6c914e:L8-L16`.

### Variants
- **Charged.** Baseline declaration: finite preload and no ports; Story 6.2 now adds the owner-requested recharge connection (mapping pending). Positive: a ball touching it is repelled radially, debiting the store. Control: an exhausted store gives an ordinary bounce.
- **Supplied.** Same geometry and strength, plus `PowerIn`. Positive: with a battery each qualified contact kicks and drains the battery. Control: disconnected or depleted supply gives an ordinary bounce.

## 3. Engine capabilities

Families: ContactImpulse, EnvironmentState, FiniteLedger, FiniteWorkActuation, GeometryQuery, JointConstraint, RigidBodyDynamics, plus StateTransaction ([map row](../general-engine-element-map.md); [element-02.json](../../coverage/engine/element-02.json)).

**Exists now**
- Contact-work declaration, owner state and occurrences: `engine/gpu/ContactWorkDeclaration.cs@a6c914e:L19-L80`.
- Worker contact-work pass: on a qualified contact it sets the target's velocity to the target speed along the contact normal and records a zero debit (`CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L1155-L1222`).

**Missing**
- The finite work law (radial impulse debiting the store, no target-velocity launch): Story 6.2. Multi-angle and glancing contacts: Story 6.3.
- Electrical supply for the Supplied variant: Story 8.1, then unscheduled. Owner S301.

**Dependencies.** Story 6.2 before 6.3; CAT-005 battery for the supplied variant.

## 4. Sources and legacy

- Requirement: "Bounded supplied or explicitly charged contact actuator repels touching cargo"; outcome "No available energy or missed contact means no powered kick" ([element-195](../requirements.md#element-195)). CAT-015 row: strength, per-body cooldown, missed/grazing/resting controls, expanding gold rings ([current-cat-015](../requirements.md#current-cat-015)).

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | Current worker sets velocity to target speed on contact with zero debit | `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L1155-L1222` | do not carry forward | A target-velocity launch is not finite work; Story 6.2 replaces it. |
| 2 | Accepted: the round bumper launches away from the contact point on every axis | `CuriousContraptions.tests/BumperTests.cs@a6c914e:L35-L72` | carry forward | Radial repulsion. |
| 3 | Accepted: a glancing contact turns motion into spin; a ball in another depth plane misses | `CuriousContraptions.tests/BumperTests.cs@a6c914e:L73-L114` | carry forward | Grazing and depth controls. |
| 4 | Accepted: cooldown is per ball; the pulse finishes after physics stops | `CuriousContraptions.tests/BumperTests.cs@a6c914e:L115-L181` | carry forward | Per-body cooldown. |
| 5 | Accepted: stationary or separating bodies do not trigger; replay identical | `CuriousContraptions.tests/BumperTests.cs@a6c914e:L221-L254` | carry forward | Resting control. |
| 6 | Levels `bumper_sidekick`, `bumper_depth`, `wall_and_bumper` | `content/puzzles.json@a6c914e:L2445-L2655`, `content/puzzles.json@a6c914e:L2656-L2866`, `content/puzzles.json@a6c914e:L3145-L3423` | carry forward | Lessons for Stories 6.2–6.3. |

**Files harvested:**
- `parts/BumperPart.cs`
- `parts/catalog/bumper.tres`
- `CuriousContraptions.tests/BumperTests.cs`
- `content/puzzles.json` (levels `bumper_sidekick`, `bumper_depth`, `wall_and_bumper`)
- The remaining bumper files (for example `engine/BumperWorkResource.cs` and `CuriousContraptions.tests/BumperOccurrenceTests.cs`) belong to [CAT-015](CAT-015-bumper.md) (Batch A). Searched with no hit for an electrically supplied bumper: `parts/`, `engine/` (including `engine/physics/` and `engine/bridge/`), `CuriousContraptions.tests/`, `tools/`, `reference/`, `diagnostics/`.

## 5. Acceptance outline

Follow [element-195](../requirements.md#element-195) and [current-cat-015](../requirements.md#current-cat-015).
- **Chrome UI recipe.** Open `bumper_sidekick`; place the bumper left of the falling ball with the gizmo; set strength; Run. For Supplied, also place a battery and connect `Supply` → `PowerIn`.
- **Positive.** The ball is kicked away from the contact point into the receiver; the ring pulses once per accepted kick.
- **Negative/control.** A missed ball, a resting ball and a separating ball get no kick; an exhausted store or an unsupplied Supplied bumper gives an ordinary bounce with no pulse.
- **Boundaries.** Strength 0 and 20; glancing and other-depth contacts; repeated returns within the cooldown.
- **Run/Reset.** Store refilled to preload; supply restored.
- **Save/Load.** `strength`, `supply`, pose and the power wire round-trip.
- **Integrations.** Campaign row 1 (bumper) first use 1–10, reuse 21–30, 41–50, 91–100, 136–150 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)); retained bumper lessons [todo-157](../requirements.md#todo-157); `wall_and_bumper` with CAT-066 wall; battery supply (CAT-005) for the Supplied variant.

## 6. Open questions

1. Recharge is required now for Charged (owner, 9 Oct 2026; CAT-015 §6), with source and rate pending. Is the Supplied variant an EL-195 requirement for this catalogue part, or a separate part? Unspecified — owner decision.
2. Supplied energy per kick: the charged calibration (proposed) or a separate power limit? Owner decision.
