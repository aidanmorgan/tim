# RAD-21 · Radiation-responsive material latch — element readiness spec

Story 7.0 Batch O named-identity spec. Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`. Requirement row: [radiation-21](../requirements.md#radiation-21) (P3 potential). The **threshold-to-release behaviour is authored material fiction** inspired by radiation processing; never real-world material data.

## 1. Identity

| Item | Value |
| --- | --- |
| ID / name | RAD-21 · Radiation-responsive material latch |
| Type | Radiation + Mechanical (exposure-weakened load-bearing insert) |
| Anchor | [requirements.md#radiation-21](../requirements.md#radiation-21); [named-elements.md#radiation-21](../invest/named-elements.md#radiation-21) |
| Related identities | Sources [RAD-01](RAD-01-gamma-source-capsule.md), [RAD-02](RAD-02-powered-x-ray-emitter.md); shields [EL-128](EL-128-dense-shield.md); exposure accounting shared with [RAD-09](RAD-09-integrating-dosimeter.md)/[RAD-10](RAD-10-exposure-sensitive-cargo-badge.md); temperature-driven analogue [TH-16 Fusible link](../invest/named-elements.md#thermal-16) (not a substitute); payloads such as [CAT-067 Weight](CAT-067-weight.md). No CAT spec refined. |
| Proof owner | S632 |
| Roadmap story | unscheduled; campaign 133, 134 (research slots 108, 109) |
| Status | not started |

## 2. Declaration

The requirement fixes: a fictional polymer insert weakens after accumulated exposure and releases a real loaded latch; enough exposure releases; shielding or insufficient exposure retains; Reset restores material state, latch pose and stored load. Values are proposals.

- **Bodies and shapes.**
  - Frame: static, a back post 0.20 × 1.20 × 0.40 m and a hinge bracket (proposed: tall enough to drop a payload into a catcher below).
  - Shelf: dynamic box 0.60 × 0.05 × 0.50 m hinged at the post (proposed: a trapdoor-style shelf that swings down when released).
  - Insert: a 0.10 × 0.10 × 0.10 m polymer pin block on the front edge, part of the frame, with an exposed sensing face (proposed: visibly the thing being irradiated).
- **Mass and material.** Shelf 0.8 kg; restitution 0.1, friction 0.5 (proposed: a light wooden shelf). Frame static, zero mass, static default contact material (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L109-L110`).
- **Constraints.** Revolute joint shelf ↔ post along the shelf's back edge; a pin support constraint shelf ↔ insert that holds the shelf horizontal until it fails; on failure a TopologyTransaction removes the pin constraint atomically (no teleport).
- **Typed ports.** none.
- **Sensors and activation.** Passive exposure D at the insert face (0.10 m, sampled 3 × 3 under the [shared rate law](RAD-08-radiation-rate-meter.md#2-declaration); ≈ 16.0 at 1 m from a Gamma capsule), weights Gamma/XRay 1.0, BetaMinus 0.5, Alpha 0, Neutron 0 (proposed: same weighting as RAD-09/RAD-10). `IrradiatedMaterialState` (Intact, Weakened, Failed) (research typed name). Pin strength S(D) = 40 N · max(0, 1 − D / 64) (proposed: with a 2 kg payload centred on the 0.8 kg shelf the pin carries ≈ 13.7 N and fails at D ≈ 42, about 2.6 s at 1 m from a Gamma capsule). Failure when the committed pin load exceeds S(D). Weakening never reverses within a Run.
- **Work and energy stores.** Stored load: the payload's potential energy held by the shelf (released as real motion); accumulated D.
- **Parameters.** none player-editable; strength curve fixed by identity (authored fiction).
- **Cosmetic curves and UI bindings.** Insert crack engraving advances in three steps ← committed state/strength (research row "Insert crack ← state"); selected-only card "weakens with exposure; never heals during a Run".
- **Art.** Cream `#fff8e9` frame, navy `#45639c` shelf, gold `#f7cb52` insert with engraved crack lines (proposed: shape-coded wear, no colour-only cue).
- **Catalogue and inventory.** Id `radiation_material_latch`, title "Exposure latch", category `Radiation` (proposed: snake_case id; [grouped menu](../requirements.md#palette-type-groups)).

**Variants.** None in the row.

## 3. Engine capabilities

Families from the [map row](../general-engine-element-map.md) and [binding](../../coverage/engine/radiation-01.json):

| Family | Status | Basis or builder |
| --- | --- | --- |
| EnvironmentState | exists now | gravity lane of `RigidBodyDeclaration`, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L86` (loads the pin) |
| FiniteLedger | missing | unscheduled (S010-D); accumulated D at the insert; the only current finite store is bumper contact work, `engine/gpu/ContactWorkDeclaration.cs@a6c914e:L5-L9` |
| GeometryQuery | exists now for contact only; missing for radiation paths | BVH `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L74-L204` and narrowphase `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L325-L620` (the coverage JSON's `engine/gpu/physics.wgsl` basis is not in the tree); sampled path queries: unscheduled (S606-D) |
| IonizingTransport | missing | unscheduled (S606-D/F); deposition at the insert |
| JointConstraint | missing | revolute shelf hinge Story 10.3; pin support constraint unscheduled (S018-D; legacy `engine/physics/PhysicsJoint.cs` is deleted by Epic 7) |
| RigidBodyDynamics | exists now | bodies `engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L86`; integration `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L216-L285` |
| SensibleHeat | missing | unscheduled (S491-D); see open question 3 |
| StateTransaction | exists now | `engine/gpu/WorkshopSimulation.cs@a6c914e:L44-L161`; material state, shelf pose and load restore with Reset |
| StructuralFracture | missing | unscheduled (S563-D); constraint failure at the strength limit |
| TemperatureStrength | missing | unscheduled (S566-D); the analogous family — the map says the exposure-to-strength response must be frozen separately, "temperature-only strength is not a substitute" |
| TopologyTransaction | exists now for construction admission only | `engine/gpu/WorkshopSimulation.cs@a6c914e:L113-L151`; in-Run constraint removal: unscheduled |

**Dependencies.** A photon source; a payload (CAT-067 Weight or a ball); a catcher (Basket/Receiver).

## 4. Sources and legacy

- [radiation-21](../requirements.md#radiation-21): "Enough exposure releases the load; shielding or insufficient accumulated exposure retains it. Reset restores material state, latch pose and stored load."
- [Named entry](../invest/named-elements.md#radiation-21), owner S632; map row (S605, S543); binding `radiation-01.json`.
- Research "Decay and material memory" law ("removing exposure never repairs it; Reset restores both") and slots 108 "The Weakening Pin", 109 "Release, Then Shelter".
- **Legacy.** None. Hits are coverage snapshots only, e.g. `reference/P0-022-before/docs/coverage/engine/capabilities-02.json@a6c914e:L15442-L15445`, plus the aggregate task lists — no element knowledge; do not carry forward.

**Files harvested** (all "no element knowledge"):
- `reference/P0-022-before/docs/coverage/engine/capabilities-02.json`
- `reference/P0-022-before/docs/coverage/engine/task-002.json`
- `reference/P0-022-before/docs/coverage/engine/task-003.json`
- `reference/P0-022-before/docs/coverage/engine/task-013.json`

## 5. Acceptance outline

- **Construction (actual Chrome UI).** Place the latch with a Weight on its shelf above a Basket; place a Gamma capsule 1 m from the insert.
- **Positive.** Insert weakens over ≈ 2.6 s, fails, the shelf swings down and the Weight lands in the Basket.
- **Negative / controls.** Thick dense shield between capsule and insert: rate ≈ 2.2, so failure needs ≈ 19 s; within a 10 s lesson window the latch holds. A 1 s exposure then removal of the capsule: D ≈ 16, strength stays reduced at ≈ 30 N (no repair) and the latch holds. Heating alone (if a heat source is near) does not weaken it.
- **Boundaries.** Payload just under and just over the remaining strength; D exactly at failure.
- **Run/Reset.** Insert state Intact, D = 0, shelf horizontal, payload back on the shelf.
- **Save/Load.** Pose round-trips; no in-Run D or state is saved.
- **Integrations.** No row-level cross-element task names the latch; it integrates through interaction processes [IX-15 Ionizing transport](../requirements.md#interaction-15), [IX-03 Joint constraint](../requirements.md#interaction-03), [IX-37 Structural fracture](../requirements.md#interaction-37) and, if open question 3 keeps it, [IX-42 Temperature-dependent strength](../requirements.md#interaction-42); family tasks [radiation-foundations](../requirements.md#radiation-foundations), [radiation-proof](../requirements.md#radiation-proof) and [radiation-campaign](../requirements.md#radiation-campaign); campaign first use in levels 131–135 ([campaign element coverage](../requirements.md#campaign-element-coverage); chapter 14 of the [campaign plan](../requirements.md#campaign-plan), latch at 133–134), reuse 136–150.

## 6. Open questions

1. Exposure-to-strength curve shape and values (linear proposed) — owner decision with S563-D.
2. Is the payload part of the element (a fixed loaded latch) or player-placed — owner decision.
3. Why the map lists SensibleHeat and TemperatureStrength: confirm the insert also weakens with temperature, or drop them — owner decision.
