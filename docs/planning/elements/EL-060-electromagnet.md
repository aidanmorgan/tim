# EL-060 · Electromagnet declaration readiness spec

Story 7.0 named-identity spec. Baseline commit `a6c914e`. Legacy citations use `path@a6c914e:Lstart-Lend` and stay retrievable from git history after the Epic 7 purge. Values marked **proposed** are design values the owner may revise.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-060 |
| Name | Electromagnet |
| Type | Mechanical |
| Requirement anchor | [element-060](../requirements.md#element-060); scope index [todo-320](../requirements.md#todo-320) |
| Named entry | [named-elements.md#element-060](../invest/named-elements.md#element-060); proof owner S642 |
| CAT spec refined or extended | none. Related: [CAT-005 battery](CAT-005-battery.md) (supply, Story 8.1), [CAT-063 switch](CAT-063-switch.md), [CAT-067 weight](CAT-067-weight.md) (proposed ferromagnetic responder), [CAT-014 bowling](CAT-014-bowling.md) and [CAT-001 ball](CAT-001-ball.md) (non-responsive controls) |
| Related identities | [EL-058 size grate](EL-058-size-grate.md) and [EL-059 weight tray](EL-059-weight-tray.md) (todo-320 sorting by material); [EL-063 cable winch](EL-063-cable-winch.md) (crane pickup) |
| Roadmap story | unscheduled |
| Status | not started |

## 2. Declaration

### Bodies and shapes

- Static housing: cylinder r 0.35 m, height 0.4 m (or box 0.7 × 0.4 × 0.7 m until a cylinder collider exists) with the pole face down. **Proposed**: a puck-sized coil comparable to the 0.4 m pulley radius (`engine/MachineData.cs@a6c914e:L97-L97`).
- Field region: cylinder coaxial with the pole, radius 0.5 m, reaching `range` below the face. **Proposed**.

### Mass and material

Static housing, default static material (restitution 1, threshold 0.1 m/s, friction 0.3; `engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L109-L110`). Pole face friction 0.6 (**proposed**: a held load does not slide off).

New material property on every contact material: `MagneticResponse` enum { None, Ferromagnetic }. **Proposed** assignments: Weight = Ferromagnetic (an iron load); Basketball, Bowling ball, Domino, Wall = None.

### Constraints and joints

None owned. A held body is supported by the field force plus pole-face contact, not a joint (Open question 2).

### Typed ports

| Socket | Domain | Direction | Local position (m) |
| --- | --- | --- | --- |
| `PowerIn` | Electrical | Input | (−0.4, 0.1, 0) |
| `Supply` | Electrical | Output (pass-through) | (0.4, 0.1, 0) |

Identities exist (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L13`). Positions **proposed**.

### Sensors and activation

None required. The committed supplied state drives the coil lamp.

### Work and energy stores

None stored. While supplied the coil debits `power` from the electrical supply each tick; an exhausted or disconnected supply produces no field.

### Parameters

| Name | Type | Range | Default | Unit | Source |
| --- | --- | --- | --- | --- | --- |
| `max_force` | f32 | 5–120 | 100 | N | **Proposed**. A Weight touching the pole face has its centre one radius (0.32·∛m, `engine/MachineData.cs@a6c914e:L99-L99`) from the face, so at contact the 4 kg Weight (r 0.51 m) feels 100 × (1 − 0.51/1.2) = 57.7 N > its 39 N weight and is held, while the 8 kg Weight (r 0.64 m) feels 100 × (1 − 0.64/1.2) = 46.7 N < 78 N and is not lifted |
| `range` | f32 | 0.2–2 | 1.2 | m | **Proposed**: a little under two Basketball diameters, so placement matters |
| `power` | f32 | 1–50 | 10 | W | **Proposed**: a visible draw on a finite battery |

Force law (**proposed**, to be frozen by LAW-FIELD-I): attraction toward the pole = `max_force` × (1 − d / `range`) for a Ferromagnetic body whose centre is inside the region at distance d from the face; zero for None or outside; total |a| ≤ 64 m/s² (force-region clamp in `docs/gpu-f32-physics.md`). At default values a 4 kg Weight (39.2 N) rises only when 100 × (1 − d/1.2) > 39.2 N, that is with its centre within 0.73 m of the face.

### Cosmetic curves and UI bindings

Cyan coil ring and a slate/gold lamp follow the committed supply; three faint field arcs appear only while supplied (presentation, not collision).

### Art

Navy housing `#293954`, cyan coil `#66b8c9`, pale grey pole face `#ccd9df`, gold lamp `#f7cb52`. Catalogue colour **proposed**: cyan `#66b8c9` (electric motor family, `DESIGN.md@a6c914e:L180-L180`).

### Catalogue and inventory entry

Id `electromagnet`, title "Electromagnet", category "Power"; `WorkshopPartKind.Electromagnet` appended last to the free inventory (`engine/gpu/WorkshopInventory.cs@a6c914e:L49-L60`). **Proposed**.

### Variants

The requirements row names no variants or modes. One element is specified.

## 3. Engine capabilities

Binding: ElectricalPower, EnvironmentState, FieldForce, FiniteLedger, FiniteWorkActuation, JointConstraint, RigidBodyDynamics (map); coverage JSON adds StateTransaction. Map decisions: LAW-FIELD-D (supplied field/material response) and S257 (typed power versus signal).

**Exists now:** electrical socket identities (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L13`); contact materials (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L54`); gravity force integration (`CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L1271-L1278`).

**Missing**

- FieldForce region acting on a material response: decision row "field-response" → LAW-FIELD-I ([decisions.md#s635](../invest/decisions.md#s635)); no story.
- `MagneticResponse` material property: same decision.
- Electrical supply and finite power debit: Story 8.1 (CAT-005).
- Cylinder collider (housing): no owning story; owner decision 9 Oct 2026: a new cylinder-collider story comes before the first shaft wheel.

**Dependencies.** CAT-005 Battery (8.1); a responsive body (CAT-067 Weight, Story 10.4, if declared Ferromagnetic); optionally CAT-063 Switch for switched supply.

## 4. Sources and legacy

- Requirements row: "Nonresponsive material and absent supply do not satisfy pickup." No variants.
- Research row: "Electromagnet / Magnetic pickup — electrical node + force region on compatible material; force, range; attracts steel while supplied, releases on loss; wooden ball unaffected" (`docs/component-research.md@a6c914e:L81-L81`); recipe "Sorting Office: magnet removes steel ball, remaining ball reaches a different chute" (`docs/component-research.md@a6c914e:L86-L86`).
- Decision: S635 field-response — "A supported responder moves in the field, an unsupported one does not; zero source or reversed field reverses/halts motion" ([decisions.md](../invest/decisions.md#s635)).

No legacy implementation. The only legacy mention is a coverage scope note naming a future magnet element (`reference/P0-022-before/docs/coverage/engine/task-017.json@a6c914e:L1575-L1575`), with no element knowledge. Searched `parts/`, `engine/`, `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/Campaign` (terms: magnet, ferro, field).

**Files harvested:** `reference/P0-022-before/docs/coverage/engine/task-017.json` (checked; no element knowledge).

## 5. Acceptance outline

Requirement row: [element-060](../requirements.md#element-060).

- **Chrome UI recipe.** Place a Battery, a Switch, an Electromagnet mounted above the bench, a 4 kg Weight and a Bowling ball under the pole, with the pole face no more than 0.2 m above the Weight's top so the Weight's centre starts within 0.71 m of the face (inside the 0.73 m lift distance); wire Battery → Electromagnet `PowerIn` (through the Switch where tested). Verify placement, parameters and electrical links.
- **Positive.** Run with supply: the 4 kg Weight rises to the pole face and stays held (57.7 N at contact against its 39 N weight); the coil lamp is gold.
- **Negative/control.** The Bowling ball (None) never moves toward the pole. Unsupplied (no wire, or exhausted battery): the Weight stays on the bench. Supply cut mid-hold: the Weight falls.
- **Boundaries.** 4 kg Weight started with its centre 0.70 m below the face: 100 × (1 − 0.70/1.2) = 41.7 N > 39.2 N, it lifts; started at 0.76 m: 36.7 N < 39.2 N, it stays on the bench although it is inside `range`. 8 kg Weight not lifted even at contact (46.7 N against 78 N). Out-of-range parameters rejected; rotated magnet (field follows the pole axis).
- **Run/Reset and Save/Load.** Body poses and battery charge restore; parameters and wires round-trip.
- **Integrations.** Cross-element task [sequence-task-371](../requirements.md#sequence-task-371) under [todo-320](../requirements.md#todo-320): "pickup/drop requires the declared power/material".

## 6. Open questions

1. Which catalogue bodies are Ferromagnetic? Proposed: Weight only; a dedicated steel ball would need a new ball kind. Owner decision.
2. Hold by field plus contact (proposed) or by a lifecycle attachment joint once touching? Owner decision.
3. Force law shape (linear falloff, inverse-square with clamp) is a LAW-FIELD-I decision. Owner decision.
4. Does the field act through intervening bodies (no occlusion, proposed) or is it blocked? Owner decision.
