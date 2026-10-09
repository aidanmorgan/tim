# EL-065 · Load hook declaration readiness spec

Story 7.0 named-identity spec. Baseline commit `a6c914e`. Legacy citations use `path@a6c914e:Lstart-Lend` and stay retrievable from git history after the Epic 7 purge. Values marked **proposed** are design values the owner may revise.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-065 |
| Name | Load hook |
| Type | Mechanical |
| Requirement anchor | [element-065](../requirements.md#element-065); scope index [todo-193](../requirements.md#todo-193) |
| Named entry | [named-elements.md#element-065](../invest/named-elements.md#element-065); proof owner S333 |
| CAT spec refined or extended | none. Related: [CAT-067 weight](CAT-067-weight.md) (its gold eye is the engagement target), [CAT-058 rope anchor](CAT-058-rope_anchor.md) and [CAT-053 pulley](CAT-053-pulley.md) (rope law) |
| Related identities | [EL-063 cable winch](EL-063-cable-winch.md) (crane), [EL-067 steel cable](EL-067-steel-cable.md), [EL-069 moving bucket](EL-069-moving-bucket.md) and [EL-070 moving cage](EL-070-moving-cage.md) (bail targets) |
| Roadmap story | unscheduled |
| Status | not started |

## 2. Declaration

### Bodies and shapes

- One dynamic compound body, a J-hook in its local XY plane: shank box 0.06 × 0.4 × 0.06 m; bowl bottom box 0.3 × 0.06 × 0.06 m; tip box 0.06 × 0.15 × 0.06 m. The throat between shank and tip is 0.3 − 0.06 − 0.06 = 0.18 m. **Proposed**: the 0.18 m throat admits the legacy weight eye bar (tube 0.035 m, `parts/WeightPart.cs@a6c914e:L38-L39`) and a bucket bail with margin, but not a Basketball (0.68 m).
- Mass 0.3 kg. **Proposed**: light next to the 0.25–8 kg Weight range so the hook barely changes the lifted load.
- Engagement target: an engageable bar collider declared on carried parts (Weight eye, bucket and cage bails); a thin box 0.2 × 0.035 × 0.035 m. **Proposed**.

### Mass and material

Hook material: restitution 0.1, bounce threshold 0.1 m/s, friction 0.6, rolling resistance 0. **Proposed**: high friction and low bounce keep an engaged bar seated in the bowl.

### Constraints and joints

None while carrying: the load is carried by bowl–bar contact only (actual supported engagement). The hook hangs from its rope or cable route like a legacy `Load` (`parts/WeightPart.cs@a6c914e:L16-L16`).

### Typed ports

| Socket | Domain | Direction | Local position (m) | Source |
| --- | --- | --- | --- | --- |
| `Tie` | Rope | Bidirectional | (0, 0.22, 0) top eye | Identity `engine/MachineData.cs@a6c914e:L104-L108`; position **proposed** |

Attachment role `Load` (one end, `engine/RopeNetwork.cs@a6c914e:L69-L78`).

### Sensors and activation

None required. A committed "engaged" read (a declared target bar in bowl contact) is optional for cosmetics and goals (Open question 2).

### Work and energy stores

None.

### Parameters

None. **Proposed**: fixed geometry; capacity follows from contact and rope limits.

### Cosmetic curves and UI bindings

None beyond the committed pose.

### Art

Pale grey metal hook `#ccd9df`, gold top eye `#f7cb52` (eye language of the rope systems, `DESIGN.md@a6c914e:L284-L284`). Catalogue colour **proposed**: pale grey `#ccd9df`.

### Catalogue and inventory entry

Id `load_hook`, title "Hook", category "Ropes"; `WorkshopPartKind.LoadHook` appended last to the free inventory (`engine/gpu/WorkshopInventory.cs@a6c914e:L49-L60`). **Proposed**.

### Variants

The requirements row names no variants or modes. One element is specified.

## 3. Engine capabilities

Binding: ContactImpulse, EnvironmentState, FiniteLedger, FiniteWorkActuation, GeometryQuery, JointConstraint, RigidBodyDynamics, TensionTransmission (map); coverage JSON adds StateTransaction.

**Exists now:** dynamic boxes, box–box manifolds with friction (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L120`; `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L503-L622`, `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L702-L766`).

**Missing**

- Compound dynamic body (three boxes): `engine/gpu/RigidMassProperties.cs@a6c914e:L26-L31` admits one primitive; no story.
- Rope domain, `Tie`, `Load` role: Story 10.2.
- Engageable bar collider on carried parts (Weight eye): CAT-067 declaration in Story 10.4 or later.

**Dependencies.** A rope route with a lifter (EL-063 Winch, or CAT-053 Pulley with a counterweight); a target with a bar (CAT-067 Weight, EL-069, EL-070).

## 4. Sources and legacy

- Requirements row: "Unengaged hook cannot carry a remote body." No variants.
- todo-193 scope index groups EL-064..066.

No legacy hook exists. Shared facts:

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | The Weight's gold eye is a ring r 0.14 m, tube 0.035 m at height 0.72 r, rotated 90° about X; the rope tie sits at r + 0.08. | `parts/WeightPart.cs@a6c914e:L38-L39`; `engine/MachineData.cs@a6c914e:L99-L100` | carry forward | Defines the engagement target size and position. |
| 2 | Weight is a rope `Load` with one `Tie`; Locked means fixed during editing, not immovable during simulation. | `parts/WeightPart.cs@a6c914e:L7-L21` | carry forward | Hook uses the same role. |
| 3 | Connected-body collision stays enabled on rope routes. | via [CAT-058](CAT-058-rope_anchor.md) R17 | carry forward | The hook must collide with its target to engage. |

**Files harvested:** `parts/WeightPart.cs`, `engine/MachineData.cs`, `engine/RopeNetwork.cs`. Searched with no hit: `parts/`, `engine/`, `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/Campaign`, `reference/` (hook in the hook-removal sense only, `engine/SceneImpactEffect.cs@a6c914e:L9-L9`, unrelated).

## 5. Acceptance outline

Requirement row: [element-065](../requirements.md#element-065).

- **Chrome UI recipe.** Place a Winch high on a Wall, connect Winch `Tie` → Hook `Tie`; place a Weight so its eye sits in the hook throat; place a second Weight 1 m away on the bench. Verify placement and link.
- **Positive.** Run: the Winch lifts; the engaged Weight rises with the hook.
- **Negative/control.** The remote Weight never leaves the bench. A Weight whose eye is beside, not in, the throat: the hook rises past it and it stays down.
- **Boundaries.** Eye at the throat lip (slips out vs seats); fast lift (jerk) keeps the bar seated within friction; hook swung into a wall collides; rope `Tie` with a second end rejected.
- **Run/Reset and Save/Load.** Hook and load poses restore; link round-trips.
- **Integrations.** The scope index [todo-193](../requirements.md#todo-193) defines no separate integration task; shared interactions use [IX-04 tension transmission](../requirements.md#interaction-04) and [IX-01 contact impulse](../requirements.md#interaction-01) (bowl–bar engagement). The [campaign coverage ledger](../requirements.md#campaign-element-coverage) reserves first use of hook/anchor in levels 11–20.

## 6. Open questions

1. Engagement by contact only (proposed) or by a lifecycle attachment joint created on seating? Owner decision.
2. Expose an `Engaged` read or activation output? Owner decision.
3. Which parts declare an engageable bar (Weight eye, bucket and cage bails, others)? Owner decision.
