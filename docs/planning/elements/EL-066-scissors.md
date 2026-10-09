# EL-066 · Scissors declaration readiness spec

Story 7.0 named-identity spec. Baseline commit `a6c914e`. Legacy citations use `path@a6c914e:Lstart-Lend` and stay retrievable from git history after the Epic 7 purge. Values marked **proposed** are design values the owner may revise.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-066 |
| Name | Scissors |
| Type | Mechanical |
| Requirement anchor | [element-066](../requirements.md#element-066); scope index [todo-193](../requirements.md#todo-193) |
| Named entry | [named-elements.md#element-066](../invest/named-elements.md#element-066); proof owner S335 |
| CAT spec refined or extended | none. Related: [CAT-058 rope anchor](CAT-058-rope_anchor.md) (rope route and topology), [CAT-067 weight](CAT-067-weight.md) (actuating and released loads), [CAT-034 impact lever](CAT-034-impact_lever.md) (hinged arm pattern) |
| Related identities | [EL-068 tin snips](EL-068-tin-snips.md) (powered cutter; same criterion), [EL-067 steel cable](EL-067-steel-cable.md) (resistant material), EL-205 rope (Batch J) |
| Roadmap story | unscheduled |
| Status | not started |

## 2. Declaration

### Bodies and shapes

- Static base with fixed lower blade: box 0.6 × 0.08 × 0.2 m (blade) on a 0.3 × 0.3 × 0.3 m post. **Proposed**.
- Dynamic upper blade-and-handle: one compound dynamic body (two boxes: blade 0.5 × 0.06 × 0.04 m plus handle 0.4 × 0.06 × 0.06 m on the far side of the pivot; see Missing); mass 0.3 kg. **Proposed**: a long handle so a modest pull closes the blades.
- Cutting throat: the region between the open blades, 0.3 m long. **Proposed**.

### Mass and material

Blade material: restitution 0.1, bounce threshold 0.1 m/s, friction 0.3, rolling resistance 0. **Proposed**.

### Constraints and joints

- Upper blade hinge at the pivot, travel [0°, 40°] (40° open, 0° closed); a weak return spring holds it open (**proposed** 0.5 N·m/rad: open at rest, closed by any deliberate pull).
- Closing input: rope tension on the handle `Tie`.

### Typed ports

| Socket | Domain | Direction | Local position (m) | Source |
| --- | --- | --- | --- | --- |
| `Tie` | Rope | Bidirectional | handle end (−0.45, 0, 0) | Identity `engine/MachineData.cs@a6c914e:L104-L108`; position **proposed** |

The rope or cable to be cut is not connected to the scissors; it merely passes through the throat (geometry query).

### Sensors and activation

Cut criterion (generic, not by connector identity): a rope or cable span intersects the throat, the blades close past 5° (**proposed**), and the work done by the blade over the closing stroke while the span is in the throat ≥ the span material's `cutting_resistance`. Then one topology transaction removes that span; the route becomes open and carries no tension.

### Work and energy stores

None stored. Cutting debits the delivered closing work into the material failure; surplus remains kinetic.

### Parameters

None on the scissors. **Proposed**: the cutting capacity comes only from the delivered work. Connector `cutting_resistance` (declared on the connector material): rope 1 J (**proposed** here; EL-205 owns it), steel cable 25 J ([EL-067](EL-067-steel-cable.md)).

### Cosmetic curves and UI bindings

Blade pose is the committed hinge pose. The cut span disappears in the committed tick the transaction commits; frayed ends are cosmetic only.

### Art

Pale grey blades `#ccd9df`, gold pivot `#f7cb52`, ochre handle `#d69c47`, navy post `#293954`. Catalogue colour **proposed**: pale grey.

### Catalogue and inventory entry

Id `scissors`, title "Scissors", category "Ropes"; `WorkshopPartKind.Scissors` appended last to the free inventory (`engine/gpu/WorkshopInventory.cs@a6c914e:L49-L60`). **Proposed**.

### Variants

The requirements row names no variants or modes. One element is specified.

## 3. Engine capabilities

Binding: EnvironmentState, FiniteLedger, GeometryQuery, JointConstraint, RigidBodyDynamics, StructuralFracture, TensionTransmission, TopologyTransaction (map); coverage JSON adds StateTransaction. Map decisions: S543 (material/enthalpy/rate/topology coupling); "Source D: cutting work and material criterion, no counterpart identity test".

**Exists now:** box bodies (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L120`).

**Missing**

- Compound dynamic body (blade plus handle boxes) with combined mass properties: `RigidMassProperties.Compile` admits one homogeneous sphere or box per dynamic body (`engine/gpu/RigidMassProperties.cs@a6c914e:L26-L31`); no story. Decision owner S335.
- Hinge with limits and return spring: Story 10.3.
- Rope domain and route: Story 10.2.
- Span-in-region geometry query: no story.
- Structural fracture criterion (work vs resistance): decision row "fracture" → S563 ([decisions.md#s543](../invest/decisions.md#s543)); no story.
- Topology transaction removing a route span atomically: decision row "phase-topology" → S565 pattern; no story.

**Dependencies.** A rope route under tension to cut (CAT-058/CAT-067, Stories 10.2/10.4); a closing input (a falling Weight on the handle rope).

## 4. Sources and legacy

- Requirements row: "Wrong placement or insufficient cutting work leaves it intact." No variants.
- Current design note: "Scissors declare contact geometry, cutting work and material failure, updating a rope's topology only after the shared fracture criterion … EL-066/068 need LAW-CONTROLLER/S563 models, not … counterpart-type checks" (`docs/general-engine-design.md@a6c914e:L149-L149`).
- Decision S563 fracture: "Just above the break condition the object splits into bounded pieces; just below it holds; unsupported material refused" ([decisions.md](../invest/decisions.md#s543)).

No legacy implementation; legacy records rope cutting as future work (`reference/P0-022-before/source-README.md@a6c914e:L99-L99`). Searched `parts/`, `engine/`, `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/Campaign`, `reference/` (terms: scissor, cut, snip, sever). Rope route facts: [CAT-058](CAT-058-rope_anchor.md) R11–R17.

**Files harvested:** `reference/P0-022-before/source-README.md`.

## 5. Acceptance outline

Requirement row: [element-066](../requirements.md#element-066).

- **Chrome UI recipe.** Place an Anchor high, a Weight A hanging from it by a rope that passes through the Scissors' throat; tie the Scissors' handle `Tie` by a second rope over a Pulley to Weight B held on a ledge that a ball knocks off. Verify placement and both routes.
- **Positive.** Run: Weight B falls, the handle closes the blades, the rope through the throat is severed in one tick and Weight A falls.
- **Negative/control.** Rope routed beside the throat: the blades close and Weight A stays hanging. Steel cable through the throat with the same Weight B: delivered work < 25 J, cable intact.
- **Boundaries.** Delivered work just below and above the rope's resistance (vary Weight B mass and drop); blades close only to 4° (no cut); a slack rope in the throat is cut and the load stays where it rests.
- **Run/Reset and Save/Load.** Reset restores the uncut route, its length and the open blades; save round-trips the uncut construction.
- **Integrations.** The scope index [todo-193](../requirements.md#todo-193) defines no separate integration task; shared interactions use [IX-04 tension transmission](../requirements.md#interaction-04) and [IX-37 structural fracture](../requirements.md#interaction-37) ("failure cannot create mass/energy"). The [campaign coverage ledger](../requirements.md#campaign-element-coverage) reserves first use of scissors and tin snips in levels 11–20.

## 6. Open questions

1. Can contact (a ball striking the handle) close the scissors as well as a rope pull? The binding has no ContactImpulse. Owner decision.
2. Rope `cutting_resistance` value belongs to EL-205; confirm 1 J. Owner decision.
3. Does a cut span become two free rope ends (art only) or disappear? Proposed: art-only frayed ends, no physics. Owner decision.
