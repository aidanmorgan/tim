# EL-064 · Metal loop anchor declaration readiness spec

Story 7.0 named-identity spec. Baseline commit `a6c914e`. Legacy citations use `path@a6c914e:Lstart-Lend` and stay retrievable from git history after the Epic 7 purge. Values marked **proposed** are design values the owner may revise.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-064 |
| Name | Metal loop anchor |
| Type | Mechanical |
| Requirement anchor | [element-064](../requirements.md#element-064); scope index [todo-193](../requirements.md#todo-193) |
| Named entry | [named-elements.md#element-064](../invest/named-elements.md#element-064); proof owner S332 |
| CAT spec refined or extended | Refines [CAT-058 rope anchor](CAT-058-rope_anchor.md), which lists EL-064 as a candidate mapping (Batch A confirms). The rope route law, admission rules and legacy harvest R1–R29 live in CAT-058 and are not repeated here |
| Related identities | [EL-067 steel cable](EL-067-steel-cable.md) (second connector it must accept), EL-205 rope (Batch J), [EL-065 load hook](EL-065-load-hook.md), [EL-063 cable winch](EL-063-cable-winch.md) |
| Roadmap story | unscheduled as EL-064. CAT-058 itself is Story 10.2; if Batch A confirms the mapping, EL-064 is delivered by Story 10.2 plus the steel-cable admission below |
| Status | not started |

## 2. Declaration

### Bodies and shapes

One static box: full size 0.55 × 0.55 × 0.16 m (half-extents 0.275, 0.275, 0.08) centred at (0, 0, −0.12) m — legacy rope anchor (`parts/RopeAnchorPart.cs@a6c914e:L16-L16`; `AddBox` stores half of the authored size, see CAT-058 R2). Pick radius 0.55 m (`parts/RopeAnchorPart.cs@a6c914e:L15-L15`).

### Mass and material

Static, so no mass. Contact material: legacy static default restitution 1, threshold 0.1 m/s, friction 0.3 (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L109-L110`); CAT-058 Open question 4 owns any change.

### Constraints and joints

None owned. It is the fixed endpoint of one rope or cable route (CAT-058 R11–R17).

### Typed ports

| Socket | Domain | Direction | Local position (m) | Source |
| --- | --- | --- | --- | --- |
| `Tie` | Rope | Bidirectional | (0, 0, 0.18) | `parts/RopeAnchorPart.cs@a6c914e:L8-L12` |

Attachment role `Anchor`: exactly one rope end (`engine/RopeNetwork.cs@a6c914e:L69-L78`). The socket accepts both connector materials, rope (EL-205) and steel cable (EL-067); this admission is the delta over the legacy anchor.

### Sensors and activation

None.

### Work and energy stores

None. Disconnected rope carries no anchored tension; a connected taut route reacts on the anchor without moving it.

### Parameters

None (legacy `Parameters = {}`, `parts/catalog/rope_anchor.tres@a6c914e:L14-L14`).

### Cosmetic curves and UI bindings

None on the anchor. Rope or cable artwork follows the committed route (CAT-058; `DESIGN.md@a6c914e:L284-L284`).

### Art

Legacy: cream mount `#fff8e9`, ring r 0.2 m, tube 0.055 m at z 0.16 m, rotated 90° about X, in catalogue colour (`parts/RopeAnchorPart.cs@a6c914e:L16-L18`); "Anchor eyes are gold on cream mounts" (`DESIGN.md@a6c914e:L284-L284`). For the **metal** loop: ring in pale grey metal `#ccd9df` (`parts/SpringPart.cs@a6c914e:L94-L94`) on the cream mount. **Proposed**: the name "metal loop" asks for a metal read, distinguishing it from the gold-eyed rope anchor if both remain in the palette (Open question 1).

### Catalogue and inventory entry

If EL-064 is the CAT-058 part: id `rope_anchor`, title "Rope anchor", category "Ropes" (`parts/catalog/rope_anchor.tres@a6c914e:L8-L14`), `WorkshopPartKind.RopeAnchor` appended last to the free inventory. If separate: id `metal_loop_anchor`, title "Metal loop", category "Ropes" (**proposed**).

### Variants

The requirements row names no variants or modes. One element is specified.

## 3. Engine capabilities

Binding: ContactImpulse, EnvironmentState, FiniteLedger, FiniteWorkActuation, GeometryQuery, JointConstraint, RigidBodyDynamics, TensionTransmission (map); coverage JSON adds StateTransaction. See CAT-058 for the shared capability list.

**Exists now:** static box body and collider (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L120`); TGS Soft contacts (`CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L208-L236`).

**Missing:** rope domain, `Tie` socket, `Anchor` role and tension-only route — Story 10.2; connector material enum distinguishing rope and steel cable — no story (EL-067 decision, owner S336).

**Dependencies.** A load (CAT-067 Weight, 10.4) and a connector (EL-205 rope or EL-067 cable).

## 4. Sources and legacy

- Requirements row: "Disconnected rope carries no anchored tension." No variants.
- todo-193 scope index groups EL-064, EL-065 and EL-066; each keeps its own obligation.
- Legacy harvest: the full rope-anchor harvest is [CAT-058 §4](CAT-058-rope_anchor.md) (R1–R29). Facts specific to this spec:

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | Anchor is a static `Anchor` endpoint with one bidirectional `Tie` at (0, 0, 0.18). | `parts/RopeAnchorPart.cs@a6c914e:L8-L12` | carry forward | Typed fixed socket. |
| 2 | Box full size 0.55 × 0.55 × 0.16 m at z −0.12; ring r 0.2, tube 0.055 at z 0.16; pick radius 0.55. | `parts/RopeAnchorPart.cs@a6c914e:L15-L18` | carry forward | Geometry. |
| 3 | A route is complete only if neither end is a guide; an untied guide end cannot hold a load. | `engine/RopeNetwork.cs@a6c914e:L34-L35`; `CuriousContraptions.tests/SceneRopeBindingTests.cs@a6c914e:L187-L202` | carry forward | Row outcome "disconnected rope carries no anchored tension". |

**Files harvested:** `parts/RopeAnchorPart.cs`, `parts/catalog/rope_anchor.tres`, `engine/RopeNetwork.cs`, `CuriousContraptions.tests/SceneRopeBindingTests.cs` (all also in CAT-058).

## 5. Acceptance outline

Requirement row: [element-064](../requirements.md#element-064).

- **Chrome UI recipe.** Place the anchor high on a Wall and a Weight below; connect anchor `Tie` → Weight `tie` with rope, then repeat with steel cable. Verify placement, connector material and the typed link.
- **Positive.** Run: the Weight hangs at the connect-time length; the anchor does not move.
- **Negative/control.** No connection: the Weight falls to the bench. A rope from the Weight to a lone Pulley (incomplete route): dashed, no tension, Weight falls.
- **Boundaries.** Second rope end on the same anchor rejected atomically; anchor rotated (wall-mounted) still anchors; cable and rope both admitted, an electrical wire to `Tie` rejected.
- **Run/Reset and Save/Load.** Link, length and Weight pose restore; connector material round-trips.
- **Integrations.** The scope index [todo-193](../requirements.md#todo-193) defines no separate integration task; shared interactions use [IX-04 tension transmission](../requirements.md#interaction-04) ("slack cannot push or create work"). The [campaign coverage ledger](../requirements.md#campaign-element-coverage) reserves first use of hook/anchor in levels 11–20.

## 6. Open questions

1. Is EL-064 the same catalogue part as CAT-058 (rope anchor) or a separate metal anchor beside it? Batch A mapping and owner decision.
2. Should the anchor have a load rating (break above a force) once fracture exists? Proposed: no. Owner decision.
